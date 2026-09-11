using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Porta o vetrata del settore. Si integra con PlayerInteract tramite IInteractable.
/// Il player preme E vicino alla porta per aprirla/chiuderla.
///
/// SETUP IN UNITY EDITOR:
///   1. Aggiungi PortaSettore al GameObject della porta/vetrata
///   2. Assegna il layer "Interactable" al GameObject
///   3. NON mettere il Collider come Trigger (serve come fisico)
///   4. Aggiungi NavMeshObstacle (opzionale) se vuoi bloccare i nemici
/// </summary>
[RequireComponent(typeof(Collider))]
public class PortaSettore : MonoBehaviour, IInteractable
{
    public enum TipoApertura { Slide, Rotazione }

    [Header("Target Animazione (Opzionale)")]
    [Tooltip("Trascina qui l'oggetto o l'anta da muovere se lo script si trova su un oggetto padre/telaio. Se lasciato vuoto, muove questo GameObject.")]
    [SerializeField] private Transform oggettoDaAnimare;

    [Header("Configurazione Porta")]
    [Tooltip("ID univoco: usato per salvare lo stato nel GameManager.")]
    [SerializeField] private string portaId = "DOOR_S0_001";

    [Tooltip("Se compilato, richiede questa credenziale raccolta prima di aprire.")]
    [SerializeField] private string credenzialeRichiesta = "";

    [Tooltip("Come si apre: Slide = scivola, Rotazione = ruota.")]
    [SerializeField] private TipoApertura tipoApertura = TipoApertura.Slide;

    [Header("Slide - solo se Tipo = Slide")]
    [Tooltip("Direzione locale di movimento: (0,1,0) = sale in alto, (1,0,0) = scorre a destra, (0,0,1) = profondità.")]
    [SerializeField] private Vector3 direzioneScivolamento = Vector3.up;
    [Tooltip("Distanza di scivolamento in metri Unity.")]
    [SerializeField] private float offsetApertura = 3f;

    [Header("Rotazione - solo se Tipo = Rotazione")]
    [Tooltip("Gradi di rotazione sull'asse Y quando si apre (es. 90 o -90).")]
    [SerializeField] private float angoloApertura = 90f;

    [Header("Animazione")]
    [Tooltip("Durata dell'animazione apertura/chiusura in secondi.")]
    [SerializeField] private float durataAnimazione = 0.5f;

    [Header("Feedback Visivo (Luce & Cubo/Lampadina)")]
    [Tooltip("Luce di stato opzionale: rossa = bloccata (manca chiave), verde = sbloccata (si può aprire).")]
    [SerializeField] private Light luceDiStato;

    [Tooltip("Slot per il Cubo / Lampadina / Mesh che emana la luce. Cambierà colore insieme alla luce.")]
    [SerializeField] private Renderer oggettoEmettitoreLuce;

    [Tooltip("Colore quando la porta è BLOCCATA (richiede una credenziale non ancora raccolta).")]
    [SerializeField] private Color coloreBloccato = Color.red;

    [Tooltip("Colore quando la porta è SBLOCCATA / SI PUÒ APRIRE (credenziale posseduta o nessuna credenziale richiesta).")]
    [SerializeField] private Color coloreSbloccato = Color.green;

    [Tooltip("Intensità del bagliore (emissione) sul materiale del Cubo.")]
    [SerializeField] private float intensitaEmissione = 2f;

    [Header("Sblocco Automatico Fine Crisi")]
    [Tooltip("Se true, la porta si sblocca e si apre automaticamente quando tutti i focolai sono contenuti e finisce la crisi (estrazione sbloccata).")]
    [SerializeField] private bool apriAlTermineCrisi = false;

    [Header("Stato Iniziale")]
    [Tooltip("Se true la porta parte gia' aperta all'avvio della scena.")]
    [SerializeField] private bool apertaAllInizio = false;

    // Stato interno
    private bool aperta = false;
    private bool inAnimazione = false;

    private Transform targetTransform;
    private Vector3 posizioneChiusaWorld;
    private Vector3 posizioneApertaWorld;
    private Quaternion rotazioneChiusaWorld;
    private Quaternion rotazioneApertaWorld;

    private Collider colliderFisico;
    private NavMeshObstacle ostacolo;

    // ─────────────────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        MissionManager.OnCredenzialiCambiate += OnCredenzialiModificate;
        MissionManager.OnEstrazioneSbloccata += OnEstrazioneModificata;
        AggiornaFeedbackVisivo();
    }

    private void OnDisable()
    {
        MissionManager.OnCredenzialiCambiate -= OnCredenzialiModificate;
        MissionManager.OnEstrazioneSbloccata -= OnEstrazioneModificata;
    }

    private void OnCredenzialiModificate(int totaleCredenziali)
    {
        AggiornaFeedbackVisivo();
    }

    private void OnEstrazioneModificata(bool sbloccata)
    {
        if (apriAlTermineCrisi && sbloccata)
        {
            Debug.Log($"<color=lime>[PORTA]</color> Fine crisi rilevata! Apertura automatica porta di evacuazione: <b>{name}</b>");
            SbloccaEDApri();
        }
    }

    private void Awake()
    {
        targetTransform = (oggettoDaAnimare != null) ? oggettoDaAnimare : transform;

        colliderFisico = GetComponent<Collider>();
        if (colliderFisico == null)
            colliderFisico = GetComponentInChildren<Collider>();

        ostacolo = GetComponent<NavMeshObstacle>();
        if (ostacolo == null)
            ostacolo = GetComponentInChildren<NavMeshObstacle>();

        if (targetTransform.gameObject.isStatic)
        {
            Debug.LogWarning($"<color=yellow>[PORTA] '{targetTransform.name}' aveva il flag STATIC attivo!</color> È stato rimosso automaticamente per consentire l'animazione di apertura/scorrimento.", this);
            targetTransform.gameObject.isStatic = false;
            foreach (Transform c in targetTransform.GetComponentsInChildren<Transform>(true))
            {
                c.gameObject.isStatic = false;
            }
        }

        if (tipoApertura == TipoApertura.Slide && Mathf.Approximately(offsetApertura, 0f))
        {
            Debug.LogWarning($"[PORTA] '{name}' ha Offset Apertura = 0! La porta non si sposterà visivamente.", this);
        }
        else if (tipoApertura == TipoApertura.Rotazione && Mathf.Approximately(angoloApertura, 0f))
        {
            Debug.LogWarning($"[PORTA] '{name}' ha Angolo Apertura = 0! La porta non ruoterà visivamente.", this);
        }

        // Calcolo delle posizioni assolute nel mondo per evitare distorsioni da scale o rotazioni complesse dei padri
        posizioneChiusaWorld = targetTransform.position;
        rotazioneChiusaWorld = targetTransform.rotation;

        Vector3 dirMondo = targetTransform.TransformDirection(direzioneScivolamento.normalized);
        posizioneApertaWorld = posizioneChiusaWorld + dirMondo * offsetApertura;
        rotazioneApertaWorld = rotazioneChiusaWorld * Quaternion.Euler(0f, angoloApertura, 0f);
    }

    private void Start()
    {
        // Ripristina lo stato se la porta era gia' stata aperta in precedenza (tra sessioni)
        if (GameManager.Instance != null &&
            !string.IsNullOrEmpty(portaId) &&
            GameManager.Instance.GetCausalState(portaId))
        {
            ApplicaStatoIstantaneo(true);
            return;
        }

        // Se la crisi è già risolta all'avvio e la porta deve aprirsi a fine crisi
        if (apriAlTermineCrisi && MissionManager.Instance != null && MissionManager.Instance.EstrazioneSbloccata)
        {
            ApplicaStatoIstantaneo(true);
            return;
        }

        ApplicaStatoIstantaneo(apertaAllInizio);
        ConnettiTerminaleSePresente();
        AggiornaFeedbackVisivo();
    }

    [Header("Sicurezza & Terminale")]
    [Tooltip("Terminale di sicurezza collegato a questa porta. Se collegato, la porta è BLOCCATA in ROSSO finché non si completa il codice/bypass sul terminale. Se nullo, la porta è sempre libera e VERDE.")]
    public TerminalePorta terminaleSicurezza;

    [Tooltip("Se true, la porta è bloccata elettronicamente e richiede il terminale/codice/minigioco per sbloccarsi.")]
    [SerializeField] private bool bloccataElettronicamente = false;

    private void ConnettiTerminaleSePresente()
    {
        if (terminaleSicurezza == null)
        {
            TerminalePorta[] tuttiITerminali = Object.FindObjectsByType<TerminalePorta>(FindObjectsSortMode.None);
            foreach (TerminalePorta t in tuttiITerminali)
            {
                if (t != null && t.portaCollegata == this)
                {
                    terminaleSicurezza = t;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Controlla se la porta si può aprire (se possiede la credenziale o se non ne richiede).
    /// Se è collegata a un terminale non ancora sbloccato, la porta resta bloccata in ROSSO.
    /// Se non ha terminali né chiavi richieste, è sempre libera in VERDE.
    /// </summary>
    public bool PuoEssereAperta()
    {
        // 1. Se è collegata a un terminale e il terminale non è ancora stato sbloccato -> BLOCCATA (ROSSO)
        if (terminaleSicurezza != null && !terminaleSicurezza.IsSbloccato)
            return false;

        // 2. Se è stata forzata come bloccata elettronicamente
        if (bloccataElettronicamente)
            return false;

        // 3. Se richiede una chiave/credenziale specifica
        if (!string.IsNullOrEmpty(credenzialeRichiesta))
        {
            if (MissionManager.Instance != null && MissionManager.Instance.PossiedeCredenziale(credenzialeRichiesta))
                return true;

            if (GameManager.Instance != null && GameManager.Instance.IsSecuritySignatureUnlocked(credenzialeRichiesta))
                return true;

            return false;
        }

        // 4. Se non richiede terminale né credenziali: LIBERA E APRIBILE SEMPRE (VERDE)!
        return true;
    }

    /// <summary>
    /// Sblocca la porta elettronicamente (da terminale con codice o minigioco di bypass) e la apre.
    /// </summary>
    public void SbloccaEDApri()
    {
        bloccataElettronicamente = false;
        credenzialeRichiesta = "";

        if (GameManager.Instance != null && !string.IsNullOrEmpty(portaId))
            GameManager.Instance.SetCausalState(portaId, true);

        if (!aperta && !inAnimazione)
        {
            StartCoroutine(AnimaPorta(true));
        }
        else
        {
            AggiornaFeedbackVisivo();
        }
    }

    /// <summary>
    /// Sblocca solo la porta senza aprirla immediatamente (passa la luce a verde e permette l'interazione con E).
    /// </summary>
    public void Sblocca()
    {
        bloccataElettronicamente = false;
        credenzialeRichiesta = "";
        AggiornaFeedbackVisivo();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // IInteractable: chiamato da PlayerInteract quando il player preme E
    // ─────────────────────────────────────────────────────────────────────────

    public void Interact()
    {
        if (inAnimazione) return;

        // Toggle: se aperta, richiudi
        if (aperta)
        {
            StartCoroutine(AnimaPorta(false));
            if (GameManager.Instance != null && !string.IsNullOrEmpty(portaId))
                GameManager.Instance.SetCausalState(portaId, false);
            return;
        }

        // Controlla se si può aprire
        if (!PuoEssereAperta())
        {
            if (terminaleSicurezza != null && !terminaleSicurezza.IsSbloccato)
            {
                Debug.LogWarning($"<color=yellow>[PORTA]</color> {portaId}: Porta bloccata dal terminale di sicurezza! Interagisci con il terminale a fianco per inserire il codice o eseguire il bypass.");
            }
            else if (!string.IsNullOrEmpty(credenzialeRichiesta))
            {
                Debug.LogWarning($"<color=yellow>[PORTA]</color> {portaId}: credenziale '{credenzialeRichiesta}' non posseduta. Porta bloccata.");
            }
            StartCoroutine(FlashCoroutine());
            return;
        }

        // Apri
        StartCoroutine(AnimaPorta(true));
        if (GameManager.Instance != null && !string.IsNullOrEmpty(portaId))
            GameManager.Instance.SetCausalState(portaId, true);
    }

    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator AnimaPorta(bool versoAperta)
    {
        inAnimazione = true;

        Vector3 posStart = targetTransform.position;
        Vector3 posFine  = versoAperta ? posizioneApertaWorld : posizioneChiusaWorld;

        Quaternion rotStart = targetTransform.rotation;
        Quaternion rotFine  = versoAperta ? rotazioneApertaWorld : rotazioneChiusaWorld;

        float tempo = 0f;
        float dur = Mathf.Max(durataAnimazione, 0.01f);

        while (tempo < dur)
        {
            float dt = Time.deltaTime > 0f ? Time.deltaTime : Time.unscaledDeltaTime;
            tempo += dt;
            float t = Mathf.SmoothStep(0f, 1f, tempo / dur);

            if (tipoApertura == TipoApertura.Slide)
                targetTransform.position = Vector3.Lerp(posStart, posFine, t);
            else
                targetTransform.rotation = Quaternion.Slerp(rotStart, rotFine, t);

            yield return null;
        }

        // Snap finale preciso
        if (tipoApertura == TipoApertura.Slide)
            targetTransform.position = posFine;
        else
            targetTransform.rotation = rotFine;

        aperta = versoAperta;
        inAnimazione = false;
        AggiornaStato();

        Debug.Log($"<color=green>[PORTA]</color> {portaId}: {(aperta ? "APERTA" : "CHIUSA")} (Pos finale: {targetTransform.position})");
    }

    private void ApplicaStatoIstantaneo(bool statoAperta)
    {
        if (targetTransform == null)
            targetTransform = (oggettoDaAnimare != null) ? oggettoDaAnimare : transform;

        if (tipoApertura == TipoApertura.Slide)
            targetTransform.position = statoAperta ? posizioneApertaWorld : posizioneChiusaWorld;
        else
            targetTransform.rotation = statoAperta ? rotazioneApertaWorld : rotazioneChiusaWorld;

        aperta = statoAperta;
        AggiornaStato();
    }

    private void AggiornaStato()
    {
        // Disabilita collider e navmesh se aperta per consentire il passaggio
        if (colliderFisico != null)
            colliderFisico.enabled = !aperta;

        if (ostacolo != null)
            ostacolo.enabled = !aperta;

        AggiornaFeedbackVisivo();
    }

    /// <summary>
    /// Aggiorna il colore della Point Light e del Cubo in base alla possibilità di apertura.
    /// </summary>
    public void AggiornaFeedbackVisivo()
    {
        bool puoAprire = PuoEssereAperta();
        ImpostaColoreFeedback(puoAprire ? coloreSbloccato : coloreBloccato);
    }

    /// <summary>
    /// Applica il colore sia alla Light component sia al materiale del Cubo/Renderer.
    /// </summary>
    private void ImpostaColoreFeedback(Color colore)
    {
        if (luceDiStato != null)
            luceDiStato.color = colore;

        if (oggettoEmettitoreLuce != null)
        {
            Material mat = oggettoEmettitoreLuce.material;
            if (mat != null)
            {
                mat.color = colore;

                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", colore);

                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", colore * intensitaEmissione);
                }
            }
        }
    }

    private IEnumerator FlashCoroutine()
    {
        for (int i = 0; i < 3; i++)
        {
            ImpostaColoreFeedback(Color.white);
            yield return new WaitForSeconds(0.1f);
            ImpostaColoreFeedback(coloreBloccato);
            yield return new WaitForSeconds(0.1f);
        }
        AggiornaFeedbackVisivo();
    }

    [ContextMenu("Test Toggle Porta (In Play Mode)")]
    public void TestToggle()
    {
        Interact();
    }

    private void OnDrawGizmosSelected()
    {
        Transform t = (oggettoDaAnimare != null) ? oggettoDaAnimare : transform;

        if (tipoApertura == TipoApertura.Slide)
        {
            Vector3 dir = t.TransformDirection(direzioneScivolamento.normalized);
            Vector3 targetPos = t.position + dir * offsetApertura;

            Gizmos.color = Color.green;
            Gizmos.DrawLine(t.position, targetPos);
            Gizmos.DrawWireCube(targetPos, Vector3.one * 0.5f);
        }
    }
}
