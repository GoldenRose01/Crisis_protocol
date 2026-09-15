// ============================================================================
// Crisis Protocol / Sector Containment - Ambiente interattivo
// File: .\Assets\CrisisProtocol\Scripts\Environment\TerminalePorta.cs
// Responsabilita': controlla porte, datapad, teletrasporti, camera o oggetti di scena collegati alla progressione.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine;
/// <summary>
/// Terminale/Monitor interattivo posizionato accanto a una porta di sicurezza.
/// Consente al giocatore di digitare il codice PIN o completare un minigioco di bypass elettrico.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TerminalePorta : MonoBehaviour, IInteractable
{
    [Header("Porte Collegate")]
    [Tooltip("Trascina qui le PortaSettore (anche più di una) da sbloccare ed aprire con questo terminale.")]
    public System.Collections.Generic.List<PortaSettore> porteCollegate = new System.Collections.Generic.List<PortaSettore>();
    [SerializeField, HideInInspector] private PortaSettore portaCollegata;
    [Header("Configurazione Sicurezza")]
    [Tooltip("Nome descrittivo visualizzato sull'interfaccia (es. 'TERMINALE SETTORE REATTORE').")]
    public string nomeTerminale = "PANNELLO DI SICUREZZA";
    [Tooltip("Codice PIN numerico a 4 cifre per lo sblocco immediato.")]
    public string codiceSegreto = "4281";
    [Tooltip("Consente l'apertura tramite tastierino numerico PIN.")]
    public bool consentiCodicePin = true;
    [Tooltip("Consente l'apertura tramite minigioco di bypass circuiti a tempo.")]
    public bool consentiBypassElettronico = true;
    [Header("Guasto Elettronico / Solo Bypass")]
    [Tooltip("Se true, il tastierino PIN è guasto o bloccato elettronicamente: digitare il PIN fallisce ed è OBBLIGATORIO aprire la porta tramite il Minigioco di Bypass Circuiti.")]
    public bool pinGuastoRichiedeBypass = false;
    [Header("Quarantena / Estrazione")]
    [Tooltip("Se true, la porta si rifiuterà di aprirsi (anche con PIN corretto) finché non sono state risolte tutte le emergenze nel settore.")]
    public bool richiedeEmergenzaRisolta = false;
    [Header("Feedback Visivo Monitor")]
    [Tooltip("Renderer dello schermo del monitor per cambiare colore (Rosso = Bloccato, Verde = Sbloccato).")]
    public Renderer monitorRenderer;
    [Tooltip("Luce dello schermo per feedback ambientale.")]
    public Light luceMonitor;
    [Header("Audio")]
    [Tooltip("Suono di interazione / battitura tastiera quando si usa il terminale.")]
    [SerializeField] private AudioClip suonoInterazione;
    [Tooltip("Suono di codice corretto / bypass completato con successo.")]
    [SerializeField] private AudioClip suonoAccessoGarantito;
    [Tooltip("Suono di codice errato / errore.")]
    [SerializeField] private AudioClip suonoAccessoNegato;
    [Range(0f, 1f)] [SerializeField] private float volumeAudio = 0.9f;
    [Header("Stato")]
    [SerializeField] private bool giaSbloccato = false;
    private AudioSource audioSource;
    public bool IsSbloccato => giaSbloccato;
    private void InizializzaAudioSource()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.minDistance = 1.5f;
            audioSource.maxDistance = 15.0f;
            audioSource.dopplerLevel = 0f;
        }
    }
    public void RiproduciSuono(AudioClip clip, float volumeMoltiplicatore = 1.0f)
    {
        if (clip == null) return;
        InizializzaAudioSource();
        if (audioSource != null)
        {
            audioSource.pitch = Random.Range(0.97f, 1.03f);
            audioSource.PlayOneShot(clip, volumeAudio * volumeMoltiplicatore);
        }
    }
    // FIX: blocca posizione nel mondo — registra pos/rot iniziali e le reimpone ogni LateUpdate
    private Vector3    _posMondiale        = Vector3.zero;
    private Quaternion _rotMondiale        = Quaternion.identity;
    private bool       _posizioneFissata   = false;
    private Vector3    _lucePosWorld       = Vector3.zero;
    private Quaternion _luceRotWorld       = Quaternion.identity;
    private bool       _lucePosRegistrata  = false;
    void Start()
    {
        SincronizzaPortaLegacy();
        // Se non è stato impostato il layer Interactable, applicalo per consentire la pressione di E
        if (gameObject.layer == 0)
        {
            int interactableLayer = LayerMask.NameToLayer("Interactable");
            if (interactableLayer != -1)
                gameObject.layer = interactableLayer;
        }
        if (porteCollegate != null && porteCollegate.Count > 0)
        {
            foreach (var p in porteCollegate)
            {
                if (p != null)
                {
                    p.terminaleSicurezza = this;
                    p.AggiornaFeedbackVisivo();
                }
            }
        }
        AggiornaGraficaMonitor();
        // FIX: registra posizione e rotazione mondiali DOPO il posizionamento completo
        _posMondiale      = transform.position;
        _rotMondiale      = transform.rotation;
        _posizioneFissata = true;
    }
    // FIX: eseguito DOPO tutti gli Update — forza il terminale a restare fermo
    // qualunque cosa lo stia muovendo (porta, physics, animazioni, ecc.)
    void LateUpdate()
    {
        if (!_posizioneFissata) return;
        transform.position = _posMondiale;
        transform.rotation = _rotMondiale;
        // Blocca anche luceMonitor se è su un oggetto separato non figlio di questo
        if (luceMonitor != null && luceMonitor.transform.parent != transform)
        {
            if (!_lucePosRegistrata)
            {
                _lucePosWorld      = luceMonitor.transform.position;
                _luceRotWorld      = luceMonitor.transform.rotation;
                _lucePosRegistrata = true;
            }
            luceMonitor.transform.position = _lucePosWorld;
            luceMonitor.transform.rotation = _luceRotWorld;
        }
    }
    // Compat legacy: alcuni prefab vecchi chiamano ancora questa strada.
    private void SincronizzaPortaLegacy()
    {
        if (portaCollegata == null) return;
        if (porteCollegate == null) porteCollegate = new System.Collections.Generic.List<PortaSettore>();
        if (!porteCollegate.Contains(portaCollegata))
        {
            porteCollegate.Add(portaCollegata);
        }
    }
    public void Interact()
    {
        RiproduciSuono(suonoInterazione);
        if (giaSbloccato)
        {
            if (porteCollegate != null && porteCollegate.Count > 0)
            {
                foreach (var p in porteCollegate)
                {
                    if (p != null)
                        p.Interact();
                }
            }
            return;
        }
        // Apri l'interfaccia interattiva del terminale (PIN / Minigioco Bypass)
        if (TerminalePortaUI.Instance != null)
        {
            TerminalePortaUI.Instance.ApriTerminale(this);
        }
        else
        {
            Debug.LogWarning("[TERMINALE] TerminalePortaUI non trovata nella scena! Creazione automatica in corso...");
            GameObject uiObj = new GameObject("TerminalePortaUI");
            TerminalePortaUI ui = uiObj.AddComponent<TerminalePortaUI>();
            ui.ApriTerminale(this);
        }
        // Mostra hint HUD: il terminale richiede credenziali, il player deve cercarle
        if (!giaSbloccato && CyberHUD.Instance != null)
        {
            string msg = $"\u26a0  TERMINALE: {(string.IsNullOrEmpty(nomeTerminale) ? "SICUREZZA" : nomeTerminale.ToUpper())}\nCerca le credenziali di accesso nei dintorni!";
            CyberHUD.Instance.MostraHintCredenziali(msg);
        }
    }
    /// <summary>
    /// Chiamato quando il codice è corretto o il minigioco di bypass ha successo.
    /// </summary>
    public void OnAccessoGarantito()
    {
        giaSbloccato = true;
        AggiornaGraficaMonitor();
        RiproduciSuono(suonoAccessoGarantito);
        // Nasconde l'hint credenziali: il terminale e' ora sbloccato
        if (CyberHUD.Instance != null) CyberHUD.Instance.NascondiHintCredenziali();
        if (porteCollegate != null && porteCollegate.Count > 0)
        {
            foreach (var p in porteCollegate)
            {
                if (p != null)
                    p.SbloccaEDApri();
            }
            Debug.Log($"<color=green>[TERMINALE] Accesso autorizzato su '{nomeTerminale}'. Porte aperte con successo!</color>");
        }
    }
    /// <summary>
    /// Chiamato quando il codice inserito è errato.
    /// </summary>
    public void OnAccessoNegato()
    {
        RiproduciSuono(suonoAccessoNegato);
    }
    private void AggiornaGraficaMonitor()
    {
        Color colore = giaSbloccato ? Color.green : Color.red;
        if (luceMonitor != null)
            luceMonitor.color = colore;
        if (monitorRenderer != null && monitorRenderer.material != null)
        {
            Material mat = monitorRenderer.material;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", colore);
            else if (mat.HasProperty("_Color"))
                mat.color = colore;
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", colore * 2f);
            }
        }
    }
}