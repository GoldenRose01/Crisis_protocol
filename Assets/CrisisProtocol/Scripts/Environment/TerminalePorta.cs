// ============================================================================
// Crisis Protocol / Sector Containment - Ambiente interattivo
// File: .\Assets\CrisisProtocol\Scripts\Environment\TerminalePorta.cs
// Responsabilita': controlla porte, datapad, teletrasporti, camera o oggetti di scena collegati alla progressione.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine; // usa lib // riga-ok

/// <summary>
/// Terminale/Monitor interattivo posizionato accanto a una porta di sicurezza.
/// Consente al giocatore di digitare il codice PIN o completare un minigioco di bypass elettrico.
/// </summary>
[RequireComponent(typeof(Collider))] // nota unity // riga-ok
// blocco: classe x roba grossa
public class TerminalePorta : MonoBehaviour, IInteractable // classe qui // riga-ok
{ // apre // riga-ok
    [Header("Porte Collegate")] // nota unity // riga-ok
    [Tooltip("Trascina qui le PortaSettore (anche più di una) da sbloccare ed aprire con questo terminale.")] // nota unity // riga-ok
    public System.Collections.Generic.List<PortaSettore> porteCollegate = new System.Collections.Generic.List<PortaSettore>(); // roba pub // riga-ok
    [SerializeField, HideInInspector] private PortaSettore portaCollegata; // setta // riga-ok

    [Header("Configurazione Sicurezza")] // nota unity // riga-ok
    [Tooltip("Nome descrittivo visualizzato sull'interfaccia (es. 'TERMINALE SETTORE REATTORE').")] // nota unity // riga-ok
    public string nomeTerminale = "PANNELLO DI SICUREZZA"; // roba pub // riga-ok

    [Tooltip("Codice PIN numerico a 4 cifre per lo sblocco immediato.")] // nota unity // riga-ok
    public string codiceSegreto = "4281"; // roba pub // riga-ok

    [Tooltip("Consente l'apertura tramite tastierino numerico PIN.")] // nota unity // riga-ok
    public bool consentiCodicePin = true; // roba pub // riga-ok

    [Tooltip("Consente l'apertura tramite minigioco di bypass circuiti a tempo.")] // nota unity // riga-ok
    public bool consentiBypassElettronico = true; // roba pub // riga-ok

    [Header("Guasto Elettronico / Solo Bypass")] // nota unity // riga-ok
    [Tooltip("Se true, il tastierino PIN è guasto o bloccato elettronicamente: digitare il PIN fallisce ed è OBBLIGATORIO aprire la porta tramite il Minigioco di Bypass Circuiti.")] // nota unity // riga-ok
    public bool pinGuastoRichiedeBypass = false; // roba pub // riga-ok

    [Header("Feedback Visivo Monitor")] // nota unity // riga-ok
    [Tooltip("Renderer dello schermo del monitor per cambiare colore (Rosso = Bloccato, Verde = Sbloccato).")] // nota unity // riga-ok
    public Renderer monitorRenderer; // roba pub // riga-ok

    [Tooltip("Luce dello schermo per feedback ambientale.")] // nota unity // riga-ok
    public Light luceMonitor; // roba pub // riga-ok

    [Header("Audio")] // nota unity // riga-ok
    [Tooltip("Suono di interazione / battitura tastiera quando si usa il terminale.")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoInterazione; // ok qua // riga-ok
    [Tooltip("Suono di codice corretto / bypass completato con successo.")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoAccessoGarantito; // ok qua // riga-ok
    [Tooltip("Suono di codice errato / errore.")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoAccessoNegato; // ok qua // riga-ok
    [Range(0f, 1f)] [SerializeField] private float volumeAudio = 0.9f; // setta // riga-ok

    [Header("Stato")] // nota unity // riga-ok
    [SerializeField] private bool giaSbloccato = false; // setta // riga-ok

    private AudioSource audioSource; // roba pub // riga-ok

    public bool IsSbloccato => giaSbloccato; // roba pub // riga-ok

    // blocco: funzione fa cose
    private void InizializzaAudioSource() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (audioSource == null) // se ok // riga-ok
            audioSource = GetComponent<AudioSource>(); // setta // riga-ok

        // blocco: controlla se va
        if (audioSource == null) // se ok // riga-ok
        { // apre // riga-ok
            audioSource = gameObject.AddComponent<AudioSource>(); // setta // riga-ok
            audioSource.playOnAwake = false; // setta // riga-ok
            audioSource.spatialBlend = 1.0f; // setta // riga-ok
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic; // setta // riga-ok
            audioSource.minDistance = 1.5f; // setta // riga-ok
            audioSource.maxDistance = 15.0f; // setta // riga-ok
            audioSource.dopplerLevel = 0f; // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void RiproduciSuono(AudioClip clip, float volumeMoltiplicatore = 1.0f) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (clip == null) return; // se ok // riga-ok
        InizializzaAudioSource(); // chiama // riga-ok
        // blocco: controlla se va
        if (audioSource != null) // se ok // riga-ok
        { // apre // riga-ok
            audioSource.pitch = Random.Range(0.97f, 1.03f); // setta // riga-ok
            audioSource.PlayOneShot(clip, volumeAudio * volumeMoltiplicatore); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    void Start() // chiama // riga-ok
    { // apre // riga-ok
        SincronizzaPortaLegacy(); // chiama // riga-ok

        // Se non è stato impostato il layer Interactable, applicalo per consentire la pressione di E
        // blocco: controlla se va
        if (gameObject.layer == 0) // se ok // riga-ok
        { // apre // riga-ok
            int interactableLayer = LayerMask.NameToLayer("Interactable"); // setta // riga-ok
            // blocco: controlla se va
            if (interactableLayer != -1) // se ok // riga-ok
                gameObject.layer = interactableLayer; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (porteCollegate != null && porteCollegate.Count > 0) // se ok // riga-ok
        { // apre // riga-ok
            // blocco: gira piu volte
            foreach (var p in porteCollegate) // ciclo x // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (p != null) // se ok // riga-ok
                { // apre // riga-ok
                    p.terminaleSicurezza = this; // setta // riga-ok
                    p.AggiornaFeedbackVisivo(); // chiama // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        AggiornaGraficaMonitor(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: compat vecchia porta
    private void SincronizzaPortaLegacy() // roba pub // riga-ok
    { // apre // riga-ok
        if (portaCollegata == null) return; // se ok // riga-ok
        if (porteCollegate == null) porteCollegate = new System.Collections.Generic.List<PortaSettore>(); // se ok // riga-ok
        if (!porteCollegate.Contains(portaCollegata)) // se ok // riga-ok
        { // apre // riga-ok
            porteCollegate.Add(portaCollegata); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void Interact() // roba pub // riga-ok
    { // apre // riga-ok
        RiproduciSuono(suonoInterazione); // chiama // riga-ok

        // blocco: controlla se va
        if (giaSbloccato) // se ok // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (porteCollegate != null && porteCollegate.Count > 0) // se ok // riga-ok
            { // apre // riga-ok
                // blocco: gira piu volte
                foreach (var p in porteCollegate) // ciclo x // riga-ok
                { // apre // riga-ok
                    // blocco: controlla se va
                    if (p != null) // se ok // riga-ok
                        p.Interact(); // chiama // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        // Apri l'interfaccia interattiva del terminale (PIN / Minigioco Bypass)
        // blocco: controlla se va
        if (TerminalePortaUI.Instance != null) // se ok // riga-ok
        { // apre // riga-ok
            TerminalePortaUI.Instance.ApriTerminale(this); // chiama // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            Debug.LogWarning("[TERMINALE] TerminalePortaUI non trovata nella scena! Creazione automatica in corso..."); // logga // riga-ok
            GameObject uiObj = new GameObject("TerminalePortaUI"); // setta // riga-ok
            TerminalePortaUI ui = uiObj.AddComponent<TerminalePortaUI>(); // setta // riga-ok
            ui.ApriTerminale(this); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    /// <summary>
    /// Chiamato quando il codice è corretto o il minigioco di bypass ha successo.
    /// </summary>
    // blocco: funzione fa cose
    public void OnAccessoGarantito() // roba pub // riga-ok
    { // apre // riga-ok
        giaSbloccato = true; // setta // riga-ok
        AggiornaGraficaMonitor(); // chiama // riga-ok
        RiproduciSuono(suonoAccessoGarantito); // chiama // riga-ok

        // blocco: controlla se va
        if (porteCollegate != null && porteCollegate.Count > 0) // se ok // riga-ok
        { // apre // riga-ok
            // blocco: gira piu volte
            foreach (var p in porteCollegate) // ciclo x // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (p != null) // se ok // riga-ok
                    p.SbloccaEDApri(); // chiama // riga-ok
            } // chiude // riga-ok
            Debug.Log($"<color=green>[TERMINALE] Accesso autorizzato su '{nomeTerminale}'. Porte aperte con successo!</color>"); // logga // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    /// <summary>
    /// Chiamato quando il codice inserito è errato.
    /// </summary>
    // blocco: funzione fa cose
    public void OnAccessoNegato() // roba pub // riga-ok
    { // apre // riga-ok
        RiproduciSuono(suonoAccessoNegato); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void AggiornaGraficaMonitor() // roba pub // riga-ok
    { // apre // riga-ok
        Color colore = giaSbloccato ? Color.green : Color.red; // setta // riga-ok

        // blocco: controlla se va
        if (luceMonitor != null) // se ok // riga-ok
            luceMonitor.color = colore; // setta // riga-ok

        // blocco: controlla se va
        if (monitorRenderer != null && monitorRenderer.material != null) // se ok // riga-ok
        { // apre // riga-ok
            Material mat = monitorRenderer.material; // setta // riga-ok
            // blocco: controlla se va
            if (mat.HasProperty("_BaseColor")) // se ok // riga-ok
                mat.SetColor("_BaseColor", colore); // chiama // riga-ok
            // blocco: controlla se va
            else if (mat.HasProperty("_Color")) // se ok // riga-ok
                mat.color = colore; // setta // riga-ok

            // blocco: controlla se va
            if (mat.HasProperty("_EmissionColor")) // se ok // riga-ok
            { // apre // riga-ok
                mat.EnableKeyword("_EMISSION"); // chiama // riga-ok
                mat.SetColor("_EmissionColor", colore * 2f); // chiama // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
