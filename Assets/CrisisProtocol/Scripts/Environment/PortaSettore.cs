// ============================================================================
// Crisis Protocol / Sector Containment - Ambiente interattivo
// File: .\Assets\CrisisProtocol\Scripts\Environment\PortaSettore.cs
// Responsabilita': controlla porte, datapad, teletrasporti, camera o oggetti di scena collegati alla progressione.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using System.Collections; // usa lib // riga-ok
using UnityEngine; // usa lib // riga-ok
using UnityEngine.AI; // usa lib // riga-ok

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
[RequireComponent(typeof(Collider))] // nota unity // riga-ok
// blocco: classe x roba grossa
public class PortaSettore : MonoBehaviour, IInteractable // classe qui // riga-ok
{ // apre // riga-ok
    // blocco: scelte rapide
    public enum TipoApertura { Slide, Rotazione } // enum val // riga-ok

    [Header("Target Animazione (Opzionale)")] // nota unity // riga-ok
    [Tooltip("Trascina qui l'oggetto o l'anta da muovere se lo script si trova su un oggetto padre/telaio. Se lasciato vuoto, muove questo GameObject.")] // nota unity // riga-ok
    [SerializeField] private Transform oggettoDaAnimare; // ok qua // riga-ok

    [Header("Configurazione Porta")] // nota unity // riga-ok
    [Tooltip("ID univoco: usato per salvare lo stato nel GameManager.")] // nota unity // riga-ok
    [SerializeField] private string portaId = "DOOR_S0_001"; // setta // riga-ok

    [Tooltip("Se compilato, richiede questa credenziale raccolta prima di aprire.")] // nota unity // riga-ok
    [SerializeField] private string credenzialeRichiesta = ""; // setta // riga-ok

    [Tooltip("Come si apre: Slide = scivola, Rotazione = ruota.")] // nota unity // riga-ok
    [SerializeField] private TipoApertura tipoApertura = TipoApertura.Slide; // setta // riga-ok

    [Header("Slide - solo se Tipo = Slide")] // nota unity // riga-ok
    [Tooltip("Direzione locale di movimento: (0,1,0) = sale in alto, (1,0,0) = scorre a destra, (0,0,1) = profondità.")] // nota unity // riga-ok
    [SerializeField] private Vector3 direzioneScivolamento = Vector3.up; // setta // riga-ok
    [Tooltip("Distanza di scivolamento in metri Unity.")] // nota unity // riga-ok
    [SerializeField] private float offsetApertura = 3f; // setta // riga-ok

    [Header("Rotazione - solo se Tipo = Rotazione")] // nota unity // riga-ok
    [Tooltip("Gradi di rotazione sull'asse Y quando si apre (es. 90 o -90).")] // nota unity // riga-ok
    [SerializeField] private float angoloApertura = 90f; // setta // riga-ok

    [Header("Animazione")] // nota unity // riga-ok
    [Tooltip("Durata dell'animazione apertura/chiusura in secondi.")] // nota unity // riga-ok
    [SerializeField] private float durataAnimazione = 0.5f; // setta // riga-ok

    [Header("Feedback Visivo (Luce & Cubo/Lampadina)")] // nota unity // riga-ok
    [Tooltip("Luce di stato opzionale: rossa = bloccata (manca chiave), verde = sbloccata (si può aprire).")] // nota unity // riga-ok
    [SerializeField] private Light luceDiStato; // ok qua // riga-ok

    [Tooltip("Slot per il Cubo / Lampadina / Mesh che emana la luce. Cambierà colore insieme alla luce.")] // nota unity // riga-ok
    [SerializeField] private Renderer oggettoEmettitoreLuce; // ok qua // riga-ok

    [Tooltip("Colore quando la porta è BLOCCATA (richiede una credenziale non ancora raccolta).")] // nota unity // riga-ok
    [SerializeField] private Color coloreBloccato = Color.red; // setta // riga-ok

    [Tooltip("Colore quando la porta è SBLOCCATA / SI PUÒ APRIRE (credenziale posseduta o nessuna credenziale richiesta).")] // nota unity // riga-ok
    [SerializeField] private Color coloreSbloccato = Color.green; // setta // riga-ok

    [Tooltip("Intensità del bagliore (emissione) sul materiale del Cubo.")] // nota unity // riga-ok
    [SerializeField] private float intensitaEmissione = 2f; // setta // riga-ok

    [Header("Modalità Apertura")] // nota unity // riga-ok
    [Tooltip("Se true, la porta si apre ESCLUSIVAMENTE quando il giocatore interagisce con essa (premendo E). Se false, si apre automaticamente quando richiesto.")] // nota unity // riga-ok
    [SerializeField] private bool aperturaAInterazione = true; // setta // riga-ok

    [Header("Sblocco Automatico Fine Crisi")] // nota unity // riga-ok
    [Tooltip("Se true (e Apertura a Interazione è disattivato), la porta si sblocca e si apre automaticamente quando tutti i focolai sono contenuti e finisce la crisi.")] // nota unity // riga-ok
    [SerializeField] private bool apriAlTermineCrisi = false; // setta // riga-ok

    [Header("Stato Iniziale")] // nota unity // riga-ok
    [Tooltip("Se true la porta parte gia' aperta all'avvio della scena.")] // nota unity // riga-ok
    [SerializeField] private bool apertaAllInizio = false; // setta // riga-ok

    [Header("Audio")] // nota unity // riga-ok
    [Tooltip("Suono di apertura porta/portellone.")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoApertura; // ok qua // riga-ok
    [Tooltip("Suono di chiusura porta/portellone.")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoChiusura; // ok qua // riga-ok
    [Tooltip("Suono di porta bloccata / maniglia forzata quando non si possiede l'accesso.")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoBloccata; // ok qua // riga-ok
    [Tooltip("Suono di sblocco elettronico da terminale o autorizzazione.")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoSblocco; // ok qua // riga-ok
    [Range(0f, 1f)] [SerializeField] private float volumeAudio = 1f; // setta // riga-ok

    // Stato interno
    private bool aperta = false; // roba pub // riga-ok
    private bool inAnimazione = false; // roba pub // riga-ok

    private Transform targetTransform; // roba pub // riga-ok
    private Vector3 posizioneChiusaWorld; // roba pub // riga-ok
    private Vector3 posizioneApertaWorld; // roba pub // riga-ok
    private Quaternion rotazioneChiusaWorld; // roba pub // riga-ok
    private Quaternion rotazioneApertaWorld; // roba pub // riga-ok

    private Collider colliderFisico; // roba pub // riga-ok
    private NavMeshObstacle ostacolo; // roba pub // riga-ok
    private AudioSource audioSource; // roba pub // riga-ok

    // ─────────────────────────────────────────────────────────────────────────

    // blocco: funzione fa cose
    private void OnEnable() // roba pub // riga-ok
    { // apre // riga-ok
        MissionManager.OnCredenzialiCambiate += OnCredenzialiModificate; // setta // riga-ok
        MissionManager.OnEstrazioneSbloccata += OnEstrazioneModificata; // setta // riga-ok
        AggiornaFeedbackVisivo(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnDisable() // roba pub // riga-ok
    { // apre // riga-ok
        MissionManager.OnCredenzialiCambiate -= OnCredenzialiModificate; // setta // riga-ok
        MissionManager.OnEstrazioneSbloccata -= OnEstrazioneModificata; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnCredenzialiModificate(int totaleCredenziali) // roba pub // riga-ok
    { // apre // riga-ok
        AggiornaFeedbackVisivo(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnEstrazioneModificata(bool sbloccata) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (sbloccata) // se ok // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (apriAlTermineCrisi) // se ok // riga-ok
            { // apre // riga-ok
                Debug.Log($"<color=lime>[PORTA]</color> Fine crisi rilevata! Apertura automatica porta di evacuazione: <b>{name}</b>"); // logga // riga-ok
                SbloccaEDApri(); // chiama // riga-ok
            } // chiude // riga-ok
            // blocco: controlla se va
            else if (aperturaAInterazione) // se ok // riga-ok
            { // apre // riga-ok
                Debug.Log($"<color=lime>[PORTA]</color> Fine crisi rilevata. Porta sbloccata per apertura a interazione: <b>{name}</b>"); // logga // riga-ok
                Sblocca(); // chiama // riga-ok
            } // chiude // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
            { // apre // riga-ok
                Sblocca(); // chiama // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void Awake() // roba pub // riga-ok
    { // apre // riga-ok
        targetTransform = (oggettoDaAnimare != null) ? oggettoDaAnimare : transform; // setta // riga-ok

        colliderFisico = GetComponent<Collider>(); // setta // riga-ok
        // blocco: controlla se va
        if (colliderFisico == null) // se ok // riga-ok
            colliderFisico = GetComponentInChildren<Collider>(); // setta // riga-ok

        ostacolo = GetComponent<NavMeshObstacle>(); // setta // riga-ok
        // blocco: controlla se va
        if (ostacolo == null) // se ok // riga-ok
            ostacolo = GetComponentInChildren<NavMeshObstacle>(); // setta // riga-ok

        InizializzaAudioSource(); // chiama // riga-ok

        // blocco: controlla se va
        if (targetTransform.gameObject.isStatic) // se ok // riga-ok
        { // apre // riga-ok
            Debug.LogWarning($"<color=yellow>[PORTA] '{targetTransform.name}' aveva il flag STATIC attivo!</color> È stato rimosso automaticamente per consentire l'animazione di apertura/scorrimento.", this); // logga // riga-ok
            targetTransform.gameObject.isStatic = false; // setta // riga-ok
            // blocco: gira piu volte
            foreach (Transform c in targetTransform.GetComponentsInChildren<Transform>(true)) // ciclo x // riga-ok
            { // apre // riga-ok
                c.gameObject.isStatic = false; // setta // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (tipoApertura == TipoApertura.Slide && Mathf.Approximately(offsetApertura, 0f)) // se ok // riga-ok
        { // apre // riga-ok
            Debug.LogWarning($"[PORTA] '{name}' ha Offset Apertura = 0! La porta non si sposterà visivamente.", this); // logga // riga-ok
        } // chiude // riga-ok
        // blocco: controlla se va
        else if (tipoApertura == TipoApertura.Rotazione && Mathf.Approximately(angoloApertura, 0f)) // se ok // riga-ok
        { // apre // riga-ok
            Debug.LogWarning($"[PORTA] '{name}' ha Angolo Apertura = 0! La porta non ruoterà visivamente.", this); // logga // riga-ok
        } // chiude // riga-ok

        // Calcolo delle posizioni assolute nel mondo per evitare distorsioni da scale o rotazioni complesse dei padri
        posizioneChiusaWorld = targetTransform.position; // setta // riga-ok
        rotazioneChiusaWorld = targetTransform.rotation; // setta // riga-ok

        Vector3 dirMondo = targetTransform.TransformDirection(direzioneScivolamento.normalized); // setta // riga-ok
        posizioneApertaWorld = posizioneChiusaWorld + dirMondo * offsetApertura; // setta // riga-ok
        rotazioneApertaWorld = rotazioneChiusaWorld * Quaternion.Euler(0f, angoloApertura, 0f); // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void Start() // roba pub // riga-ok
    { // apre // riga-ok
        // Se apertura a interazione è disattivata, ripristina lo stato se la porta era gia' stata aperta in precedenza
        // blocco: controlla se va
        if (!aperturaAInterazione && GameManager.Instance != null && !string.IsNullOrEmpty(portaId) && GameManager.Instance.GetCausalState(portaId)) // se ok // riga-ok
        { // apre // riga-ok
            ApplicaStatoIstantaneo(true); // chiama // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        // Se apertura a interazione è disattivata e la crisi è già risolta all'avvio
        // blocco: controlla se va
        if (apriAlTermineCrisi && MissionManager.Instance != null && MissionManager.Instance.EstrazioneSbloccata) // se ok // riga-ok
        { // apre // riga-ok
            ApplicaStatoIstantaneo(true); // chiama // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        ApplicaStatoIstantaneo(apertaAllInizio); // chiama // riga-ok
        ConnettiTerminaleSePresente(); // chiama // riga-ok
        AggiornaFeedbackVisivo(); // chiama // riga-ok
    } // chiude // riga-ok

    [Header("Sicurezza & Terminale")] // nota unity // riga-ok
    [Tooltip("Terminale di sicurezza collegato a questa porta. Se collegato, la porta è BLOCCATA in ROSSO finché non si completa il codice/bypass sul terminale. Se nullo, la porta è sempre libera e VERDE.")] // nota unity // riga-ok
    public TerminalePorta terminaleSicurezza; // roba pub // riga-ok

    [Tooltip("Se true, la porta è bloccata elettronicamente e richiede il terminale/codice/minigioco per sbloccarsi.")] // nota unity // riga-ok
    [SerializeField] private bool bloccataElettronicamente = false; // setta // riga-ok

    // blocco: funzione fa cose
    private void ConnettiTerminaleSePresente() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (terminaleSicurezza == null) // se ok // riga-ok
        { // apre // riga-ok
            TerminalePorta[] tuttiITerminali = Object.FindObjectsByType<TerminalePorta>(FindObjectsSortMode.None); // setta // riga-ok
            // blocco: gira piu volte
            foreach (TerminalePorta t in tuttiITerminali) // ciclo x // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (t != null && t.porteCollegate.Contains(this)) // se ok // riga-ok
                { // apre // riga-ok
                    terminaleSicurezza = t; // setta // riga-ok
                    break; // stop // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    /// <summary>
    /// Controlla se la porta si può aprire (se possiede la credenziale o se non ne richiede).
    /// Se è collegata a un terminale non ancora sbloccato, la porta resta bloccata in ROSSO.
    /// Se non ha terminali né chiavi richieste, è sempre libera in VERDE.
    /// </summary>
    // blocco: funzione fa cose
    public bool PuoEssereAperta() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: porta fine livello solo auto
        if (apriAlTermineCrisi) // se ok // riga-ok
            return MissionManager.Instance != null && MissionManager.Instance.EstrazioneSbloccata; // torna val // riga-ok

        // 1. Se è collegata a un terminale e il terminale non è ancora stato sbloccato -> BLOCCATA (ROSSO)
        // blocco: controlla se va
        if (terminaleSicurezza != null && !terminaleSicurezza.IsSbloccato) // se ok // riga-ok
            return false; // torna val // riga-ok

        // 2. Se è stata forzata come bloccata elettronicamente
        // blocco: controlla se va
        if (bloccataElettronicamente) // se ok // riga-ok
            return false; // torna val // riga-ok

        // 3. Se richiede una chiave/credenziale specifica
        // blocco: controlla se va
        if (!string.IsNullOrEmpty(credenzialeRichiesta)) // se ok // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (MissionManager.Instance != null && MissionManager.Instance.PossiedeCredenziale(credenzialeRichiesta)) // se ok // riga-ok
                return true; // torna val // riga-ok

            // blocco: controlla se va
            if (GameManager.Instance != null && GameManager.Instance.IsSecuritySignatureUnlocked(credenzialeRichiesta)) // se ok // riga-ok
                return true; // torna val // riga-ok

            return false; // torna val // riga-ok
        } // chiude // riga-ok

        // 4. Se non richiede terminale né credenziali: LIBERA E APRIBILE SEMPRE (VERDE)!
        return true; // torna val // riga-ok
    } // chiude // riga-ok

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
            audioSource.spatialBlend = 1.0f; // 3D Audio // setta // riga-ok
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic; // setta // riga-ok
            audioSource.minDistance = 2.0f; // setta // riga-ok
            audioSource.maxDistance = 18.0f; // setta // riga-ok
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
            audioSource.pitch = Random.Range(0.96f, 1.04f); // setta // riga-ok
            audioSource.PlayOneShot(clip, volumeAudio * volumeMoltiplicatore); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    /// <summary>
    /// Sblocca la porta elettronicamente (da terminale con codice o minigioco di bypass) e la apre.
    /// </summary>
    // blocco: funzione fa cose
    public void SbloccaEDApri() // roba pub // riga-ok
    { // apre // riga-ok
        bloccataElettronicamente = false; // setta // riga-ok
        credenzialeRichiesta = ""; // setta // riga-ok

        RiproduciSuono(suonoSblocco, 1.0f); // chiama // riga-ok

        // blocco: controlla se va
        if (GameManager.Instance != null && !string.IsNullOrEmpty(portaId)) // se ok // riga-ok
            GameManager.Instance.SetCausalState(portaId, true); // chiama // riga-ok

        // blocco: controlla se va
        if (!aperta && !inAnimazione) // se ok // riga-ok
        { // apre // riga-ok
            StartCoroutine(AnimaPorta(true)); // corutina // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            AggiornaFeedbackVisivo(); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    /// <summary>
    /// Sblocca solo la porta senza aprirla immediatamente (passa la luce a verde e permette l'interazione con E).
    /// </summary>
    // blocco: funzione fa cose
    public void Sblocca() // roba pub // riga-ok
    { // apre // riga-ok
        bloccataElettronicamente = false; // setta // riga-ok
        credenzialeRichiesta = ""; // setta // riga-ok
        RiproduciSuono(suonoSblocco, 1.0f); // chiama // riga-ok
        AggiornaFeedbackVisivo(); // chiama // riga-ok
    } // chiude // riga-ok

    // ─────────────────────────────────────────────────────────────────────────
    // IInteractable: chiamato da PlayerInteract quando il player preme E
    // ─────────────────────────────────────────────────────────────────────────

    // blocco: funzione fa cose
    public void Interact() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (inAnimazione) return; // se ok // riga-ok

        // blocco: porta fine livello non manuale
        if (apriAlTermineCrisi) // se ok // riga-ok
        { // apre // riga-ok
            RiproduciSuono(suonoBloccata, 0.8f); // chiama // riga-ok
            Debug.LogWarning($"<color=yellow>[PORTA]</color> {portaId}: porta di fine livello. Si apre solo automaticamente a missione completata."); // logga // riga-ok
            StartCoroutine(FlashCoroutine()); // corutina // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        // Toggle: se aperta, richiudi
        // blocco: controlla se va
        if (aperta) // se ok // riga-ok
        { // apre // riga-ok
            StartCoroutine(AnimaPorta(false)); // corutina // riga-ok
            // blocco: controlla se va
            if (GameManager.Instance != null && !string.IsNullOrEmpty(portaId)) // se ok // riga-ok
                GameManager.Instance.SetCausalState(portaId, false); // chiama // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        // Controlla se si può aprire
        // blocco: controlla se va
        if (!PuoEssereAperta()) // se ok // riga-ok
        { // apre // riga-ok
            RiproduciSuono(suonoBloccata, 1.0f); // chiama // riga-ok

            // blocco: controlla se va
            if (terminaleSicurezza != null && !terminaleSicurezza.IsSbloccato) // se ok // riga-ok
            { // apre // riga-ok
                Debug.LogWarning($"<color=yellow>[PORTA]</color> {portaId}: Porta bloccata dal terminale di sicurezza! Interagisci con il terminale a fianco per inserire il codice o eseguire il bypass."); // logga // riga-ok
            } // chiude // riga-ok
            // blocco: controlla se va
            else if (!string.IsNullOrEmpty(credenzialeRichiesta)) // se ok // riga-ok
            { // apre // riga-ok
                Debug.LogWarning($"<color=yellow>[PORTA]</color> {portaId}: credenziale '{credenzialeRichiesta}' non posseduta. Porta bloccata."); // logga // riga-ok
            } // chiude // riga-ok
            StartCoroutine(FlashCoroutine()); // corutina // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        // Apri
        StartCoroutine(AnimaPorta(true)); // corutina // riga-ok
        // blocco: controlla se va
        if (GameManager.Instance != null && !string.IsNullOrEmpty(portaId)) // se ok // riga-ok
            GameManager.Instance.SetCausalState(portaId, true); // chiama // riga-ok
    } // chiude // riga-ok

    // ─────────────────────────────────────────────────────────────────────────

    // blocco: funzione fa cose
    private IEnumerator AnimaPorta(bool versoAperta) // roba pub // riga-ok
    { // apre // riga-ok
        inAnimazione = true; // setta // riga-ok

        // blocco: controlla se va
        if (versoAperta) // se ok // riga-ok
            RiproduciSuono(suonoApertura, 1.0f); // chiama // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
            RiproduciSuono(suonoChiusura, 1.0f); // chiama // riga-ok

        Vector3 posStart = targetTransform.position; // setta // riga-ok
        Vector3 posFine  = versoAperta ? posizioneApertaWorld : posizioneChiusaWorld; // setta // riga-ok

        Quaternion rotStart = targetTransform.rotation; // setta // riga-ok
        Quaternion rotFine  = versoAperta ? rotazioneApertaWorld : rotazioneChiusaWorld; // setta // riga-ok

        float tempo = 0f; // setta // riga-ok
        float dur = Mathf.Max(durataAnimazione, 0.01f); // setta // riga-ok

        // blocco: gira piu volte
        while (tempo < dur) // ciclo x // riga-ok
        { // apre // riga-ok
            float dt = Time.deltaTime > 0f ? Time.deltaTime : Time.unscaledDeltaTime; // setta // riga-ok
            tempo += dt; // setta // riga-ok
            float t = Mathf.SmoothStep(0f, 1f, tempo / dur); // setta // riga-ok

            // blocco: controlla se va
            if (tipoApertura == TipoApertura.Slide) // se ok // riga-ok
                targetTransform.position = Vector3.Lerp(posStart, posFine, t); // setta // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
                targetTransform.rotation = Quaternion.Slerp(rotStart, rotFine, t); // setta // riga-ok

            yield return null; // aspetta // riga-ok
        } // chiude // riga-ok

        // Snap finale preciso
        // blocco: controlla se va
        if (tipoApertura == TipoApertura.Slide) // se ok // riga-ok
            targetTransform.position = posFine; // setta // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
            targetTransform.rotation = rotFine; // setta // riga-ok

        aperta = versoAperta; // setta // riga-ok
        inAnimazione = false; // setta // riga-ok
        AggiornaStato(); // chiama // riga-ok

        Debug.Log($"<color=green>[PORTA]</color> {portaId}: {(aperta ? "APERTA" : "CHIUSA")} (Pos finale: {targetTransform.position})"); // logga // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void ApplicaStatoIstantaneo(bool statoAperta) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (targetTransform == null) // se ok // riga-ok
            targetTransform = (oggettoDaAnimare != null) ? oggettoDaAnimare : transform; // setta // riga-ok

        // blocco: controlla se va
        if (tipoApertura == TipoApertura.Slide) // se ok // riga-ok
            targetTransform.position = statoAperta ? posizioneApertaWorld : posizioneChiusaWorld; // setta // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
            targetTransform.rotation = statoAperta ? rotazioneApertaWorld : rotazioneChiusaWorld; // setta // riga-ok

        aperta = statoAperta; // setta // riga-ok
        AggiornaStato(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void AggiornaStato() // roba pub // riga-ok
    { // apre // riga-ok
        // Disabilita collider e navmesh se aperta per consentire il passaggio
        // blocco: controlla se va
        if (colliderFisico != null) // se ok // riga-ok
            colliderFisico.enabled = !aperta; // setta // riga-ok

        // blocco: controlla se va
        if (ostacolo != null) // se ok // riga-ok
            ostacolo.enabled = !aperta; // setta // riga-ok

        AggiornaFeedbackVisivo(); // chiama // riga-ok
    } // chiude // riga-ok

    /// <summary>
    /// Aggiorna il colore della Point Light e del Cubo in base alla possibilità di apertura.
    /// </summary>
    // blocco: funzione fa cose
    public void AggiornaFeedbackVisivo() // roba pub // riga-ok
    { // apre // riga-ok
        bool puoAprire = PuoEssereAperta(); // setta // riga-ok
        ImpostaColoreFeedback(puoAprire ? coloreSbloccato : coloreBloccato); // chiama // riga-ok
    } // chiude // riga-ok

    /// <summary>
    /// Applica il colore sia alla Light component sia al materiale del Cubo/Renderer.
    /// </summary>
    // blocco: funzione fa cose
    private void ImpostaColoreFeedback(Color colore) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (luceDiStato != null) // se ok // riga-ok
            luceDiStato.color = colore; // setta // riga-ok

        // blocco: controlla se va
        if (oggettoEmettitoreLuce != null) // se ok // riga-ok
        { // apre // riga-ok
            Material mat = oggettoEmettitoreLuce.material; // setta // riga-ok
            // blocco: controlla se va
            if (mat != null) // se ok // riga-ok
            { // apre // riga-ok
                mat.color = colore; // setta // riga-ok

                // blocco: controlla se va
                if (mat.HasProperty("_BaseColor")) // se ok // riga-ok
                    mat.SetColor("_BaseColor", colore); // chiama // riga-ok

                // blocco: controlla se va
                if (mat.HasProperty("_EmissionColor")) // se ok // riga-ok
                { // apre // riga-ok
                    mat.EnableKeyword("_EMISSION"); // chiama // riga-ok
                    mat.SetColor("_EmissionColor", colore * intensitaEmissione); // chiama // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private IEnumerator FlashCoroutine() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: gira piu volte
        for (int i = 0; i < 3; i++) // ciclo x // riga-ok
        { // apre // riga-ok
            ImpostaColoreFeedback(Color.white); // chiama // riga-ok
            yield return new WaitForSeconds(0.1f); // aspetta // riga-ok
            ImpostaColoreFeedback(coloreBloccato); // chiama // riga-ok
            yield return new WaitForSeconds(0.1f); // aspetta // riga-ok
        } // chiude // riga-ok
        AggiornaFeedbackVisivo(); // chiama // riga-ok
    } // chiude // riga-ok

    [ContextMenu("Test Toggle Porta (In Play Mode)")] // nota unity // riga-ok
    // blocco: funzione fa cose
    public void TestToggle() // roba pub // riga-ok
    { // apre // riga-ok
        Interact(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnDrawGizmosSelected() // roba pub // riga-ok
    { // apre // riga-ok
        Transform t = (oggettoDaAnimare != null) ? oggettoDaAnimare : transform; // setta // riga-ok

        // blocco: controlla se va
        if (tipoApertura == TipoApertura.Slide) // se ok // riga-ok
        { // apre // riga-ok
            Vector3 dir = t.TransformDirection(direzioneScivolamento.normalized); // setta // riga-ok
            Vector3 targetPos = t.position + dir * offsetApertura; // setta // riga-ok

            Gizmos.color = Color.green; // setta // riga-ok
            Gizmos.DrawLine(t.position, targetPos); // chiama // riga-ok
            Gizmos.DrawWireCube(targetPos, Vector3.one * 0.5f); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
