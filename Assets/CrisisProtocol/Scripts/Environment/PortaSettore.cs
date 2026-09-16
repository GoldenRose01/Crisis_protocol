// ============================================================================
// Crisis Protocol / Sector Containment - Ambiente interattivo
// File: .\Assets\CrisisProtocol\Scripts\Environment\PortaSettore.cs
// Responsabilita': controlla porte, datapad, teletrasporti, camera o oggetti di scena collegati alla progressione.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
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
    [Header("Modalità Apertura")]
    [Tooltip("Se true, la porta si apre ESCLUSIVAMENTE quando il giocatore interagisce con essa (premendo E). Se false, si apre automaticamente quando richiesto.")]
    [SerializeField] private bool aperturaAInterazione = true;
    [Header("Sblocco Automatico Fine Crisi")]
    [Tooltip("Se true (e Apertura a Interazione è disattivato), la porta si sblocca e si apre automaticamente quando tutti i focolai sono contenuti e finisce la crisi.")]
    [SerializeField] private bool apriAlTermineCrisi = false;
    [Header("Stato Iniziale")]
    [Tooltip("Se true la porta parte gia' aperta all'avvio della scena.")]
    [SerializeField] private bool apertaAllInizio = false;
    [Header("Audio")]
    [Tooltip("Suono di apertura porta/portellone.")]
    [SerializeField] private AudioClip suonoApertura;
    [Tooltip("Suono di chiusura porta/portellone.")]
    [SerializeField] private AudioClip suonoChiusura;
    [Tooltip("Suono di porta bloccata / maniglia forzata quando non si possiede l'accesso.")]
    [SerializeField] private AudioClip suonoBloccata;
    [Tooltip("Suono di sblocco elettronico da terminale o autorizzazione.")]
    [SerializeField] private AudioClip suonoSblocco;
    [Range(0f, 1f)] [SerializeField] private float volumeAudio = 1f;
    // Stato interno
    // aperta/inAnimazione sono la verità runtime della porta. Non leggiamo la
    // posizione della mesh ogni frame perché animazioni, parent e scale importate
    // possono rendere quel controllo poco affidabile.
    private bool aperta = false;
    private bool inAnimazione = false;

    // Target da muovere: può essere la porta stessa o solo l'anta figlia.
    // Questa separazione evita di trascinare terminali, luci o collider UI.
    private Transform targetTransform;
    private Vector3 posizioneChiusaWorld;
    private Vector3 posizioneApertaWorld;
    private Quaternion rotazioneChiusaWorld;
    private Quaternion rotazioneApertaWorld;
    private Collider colliderFisico;
    private NavMeshObstacle ostacolo;
    private AudioSource audioSource;
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
        // Le porte ascoltano il segnale missione invece di interrogare MissionManager
        // ogni frame. Quando la crisi finisce, solo le porte configurate come uscita
        // automatica si aprono da sole; le altre al massimo diventano sbloccate.
        if (sbloccata)
        {
            if (apriAlTermineCrisi)
            {
                Debug.Log($"<color=lime>[PORTA]</color> Fine crisi rilevata! Apertura automatica porta di evacuazione: <b>{name}</b>");
                SbloccaEDApri();
            }
            else if (aperturaAInterazione)
            {
                Debug.Log($"<color=lime>[PORTA]</color> Fine crisi rilevata. Porta sbloccata per apertura a interazione: <b>{name}</b>");
                Sblocca();
            }
            else
            {
                Sblocca();
            }
        }
    }
    private void Awake()
    {
        InizializzaFeedbackVisivo();
        // FIX: stacca dalla gerarchia porta tutti gli oggetti statici (terminali, luci, collider
        // di interazione) PRIMA di calcolare targetTransform, così non vengono trascinati
        // dall'animazione di apertura/chiusura della porta.
        DetacchiaOggettiStatici();
        // Auto-detect dell'anta: nei prefab importati il GameObject principale
        // spesso è un contenitore con terminale, luci e mesh. Qui prendiamo il
        // primo figlio "da porta" così l'animazione non porta via tutto il set.
        if (oggettoDaAnimare != null)
        {
            targetTransform = oggettoDaAnimare;
        }
        else
        {
            Transform antaTrovata = null;
            foreach (Transform figlio in transform)
            {
                if (figlio.GetComponent<TerminalePorta>() == null && (figlio.GetComponentInChildren<MeshRenderer>() != null || figlio.GetComponentInChildren<MeshFilter>() != null || figlio.GetComponentInChildren<SkinnedMeshRenderer>() != null))
                {
                    antaTrovata = figlio;
                    break;
                }
            }
            targetTransform = (antaTrovata != null) ? antaTrovata : transform;
        }

        colliderFisico = GetComponent<Collider>();
        if (colliderFisico == null)
            colliderFisico = GetComponentInChildren<Collider>();
        ostacolo = GetComponent<NavMeshObstacle>();
        if (ostacolo == null)
            ostacolo = GetComponentInChildren<NavMeshObstacle>();
        InizializzaAudioSource();
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
        // Usiamo posizioni/rotazioni world già calcolate: è meno fragile di
        // sommare localPosition quando il prefab ha parent scalati o ruotati male.
        posizioneChiusaWorld = targetTransform.position;
        rotazioneChiusaWorld = targetTransform.rotation;
        Vector3 dirMondo = targetTransform.TransformDirection(direzioneScivolamento.normalized);
        posizioneApertaWorld = posizioneChiusaWorld + dirMondo * offsetApertura;
        rotazioneApertaWorld = rotazioneChiusaWorld * Quaternion.Euler(0f, angoloApertura, 0f);
    }
    /// <summary>
    /// FIX: stacca dalla gerarchia della porta tutti i GameObject che devono restare FISSI
    /// (TerminalePorta, luci di indicazione, collider di interazione del pannello).
    /// Li reparenta al genitore della porta oppure li rende root-level, mantenendo la
    /// loro posizione/rotazione nel mondo invariata (worldPositionStays = true).
    /// </summary>
    private void DetacchiaOggettiStatici()
    {
        // Il nuovo parent resta vicino nella gerarchia, ma fuori dall'anta mobile.
        // worldPositionStays=true mantiene il layout scenico identico.
        Transform nuovoPadre = transform.parent;

        // Terminali e datapad sono interfacce, non pezzi dell'anta: se restano
        // figli della porta, il pannello PIN si sposta/ruota insieme al portellone.
        TerminalePorta[] terminaliTrovati = GetComponentsInChildren<TerminalePorta>(true);
        foreach (TerminalePorta t in terminaliTrovati)
        {
            if (t != null && t.transform != transform)
            {
                t.transform.SetParent(nuovoPadre, true);
                Debug.Log($"<color=cyan>[PORTA FIX]</color> TerminalePorta '<b>{t.name}</b>' staccato dalla porta '<b>{name}</b>' e reso indipendente.");
            }
        }
        DatapadCodiciPorte[] datapadTrovati = GetComponentsInChildren<DatapadCodiciPorte>(true);
        foreach (DatapadCodiciPorte d in datapadTrovati)
        {
            if (d != null && d.transform != transform)
            {
                d.transform.SetParent(nuovoPadre, true);
                Debug.Log($"<color=cyan>[PORTA FIX]</color> DatapadCodiciPorte '<b>{d.name}</b>' staccato dalla porta '<b>{name}</b>' e reso indipendente.");
            }
        }
        // Le luci di stato devono restare ferme vicino al terminale; le luci
        // decorative dentro la mesh dell'anta invece restano attaccate all'anta.
        Light[] luciTrovate = GetComponentsInChildren<Light>(true);
        foreach (Light l in luciTrovate)
        {
            if (l == null) continue;
            // Stacca solo luci che sono figlie dirette di questa porta (non dell'oggettoDaAnimare)
            // così le luci della mesh della porta restano attaccate all'anta
            bool figliaDellaPorta = l.transform.parent == transform;
            bool nonEParteDellAnta = oggettoDaAnimare == null || !l.transform.IsChildOf(oggettoDaAnimare);
            if (figliaDellaPorta && nonEParteDellAnta)
            {
                l.transform.SetParent(nuovoPadre, true);
                Debug.Log($"<color=cyan>[PORTA FIX]</color> Luce '<b>{l.name}</b>' staccata dalla porta '<b>{name}</b>' e reso indipendente.");
            }
        }
        // I trigger dei pannelli servono a PlayerInteract; se si muovono con la
        // porta, il punto d'interazione diventa ballerino e difficile da usare.
        Collider[] collidersTrovati = GetComponentsInChildren<Collider>(true);
        foreach (Collider c in collidersTrovati)
        {
            if (c == null || c.gameObject == gameObject) continue;
            bool figlioDellaPorta = c.transform.parent == transform;
            bool nonEParteDellAnta = oggettoDaAnimare == null || !c.transform.IsChildOf(oggettoDaAnimare);
            // Stacca solo collider Trigger figli diretti (collider fisici dell'anta vengono tenuti)
            if (figlioDellaPorta && nonEParteDellAnta && c.isTrigger)
            {
                c.transform.SetParent(nuovoPadre, true);
                Debug.Log($"<color=cyan>[PORTA FIX]</color> Collider Trigger '<b>{c.name}</b>' staccato dalla porta '<b>{name}</b>' e reso indipendente.");
            }
        }
    }
    private void Start()
    {
        // Porte automatiche: se erano state aperte e il GameManager lo ricorda,
        // le riallineiamo subito senza animazione per non vedere scatti a inizio scena.
        if (!aperturaAInterazione && GameManager.Instance != null && !string.IsNullOrEmpty(portaId) && GameManager.Instance.GetCausalState(portaId))
        {
            ApplicaStatoIstantaneo(true);
            return;
        }
        // Caso reload dopo crisi risolta: il portellone di uscita deve risultare
        // già coerente col finale, non chiuso per un frame.
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
        // Auto-wire "furbo" per le scene montate a mano: se il terminale elenca
        // questa porta nelle porte collegate, la porta se lo aggancia da sola.
        if (terminaleSicurezza == null)
        {
            TerminalePorta[] tuttiITerminali = Object.FindObjectsByType<TerminalePorta>(FindObjectsSortMode.None);
            foreach (TerminalePorta t in tuttiITerminali)
            {
                if (t != null && t.porteCollegate.Contains(this))
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
        // La porta di fine livello non si apre a mano: deve seguire solo lo stato missione.
        if (apriAlTermineCrisi)
            return MissionManager.Instance != null && MissionManager.Instance.EstrazioneSbloccata;
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
    private void InizializzaAudioSource()
    {
        // AudioSource lazy: non obbliga ogni prefab porta ad averlo gia' configurato,
        // ma quando serve crea un audio 3D coerente con la posizione della porta.
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f; // 3D Audio
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.minDistance = 2.0f;
            audioSource.maxDistance = 18.0f;
            audioSource.dopplerLevel = 0f;
        }
    }
    public void RiproduciSuono(AudioClip clip, float volumeMoltiplicatore = 1.0f)
    {
        if (clip == null) return;
        InizializzaAudioSource();
        if (audioSource != null)
        {
            audioSource.pitch = Random.Range(0.96f, 1.04f);
            audioSource.PlayOneShot(clip, volumeAudio * volumeMoltiplicatore);
        }
    }
    /// <summary>
    /// Sblocca la porta elettronicamente (da terminale con codice o minigioco di bypass) e la apre.
    /// </summary>
    public void SbloccaEDApri()
    {
        bloccataElettronicamente = false;
        credenzialeRichiesta = "";
        RiproduciSuono(suonoSblocco, 1.0f);
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
        RiproduciSuono(suonoSblocco, 1.0f);
        AggiornaFeedbackVisivo();
    }
    // ─────────────────────────────────────────────────────────────────────────
    // IInteractable: chiamato da PlayerInteract quando il player preme E
    // ─────────────────────────────────────────────────────────────────────────
    public void Interact()
    {
        if (inAnimazione) return;

        // Evita shortcut involontarie: il player non deve aprire il portellone prima del finale.
        if (apriAlTermineCrisi)
        {
            RiproduciSuono(suonoBloccata, 0.8f);
            Debug.LogWarning($"<color=yellow>[PORTA]</color> {portaId}: porta di fine livello. Si apre solo automaticamente a missione completata.");
            StartCoroutine(FlashCoroutine());
            return;
        }
        // Le porte normali sono toggle: lo stesso input apre e richiude.
        if (aperta)
        {
            StartCoroutine(AnimaPorta(false));
            if (GameManager.Instance != null && !string.IsNullOrEmpty(portaId))
                GameManager.Instance.SetCausalState(portaId, false);
            return;
        }
        // Prima di animare facciamo passare tutti i gate logici: terminale,
        // blocco elettronico, credenziale richiesta e stato missione.
        if (!PuoEssereAperta())
        {
            RiproduciSuono(suonoBloccata, 1.0f);
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
        // Stato salvato dopo l'avvio dell'animazione: se il player torna nel
        // settore, la porta automatica può essere ripristinata coerentemente.
        StartCoroutine(AnimaPorta(true));
        if (GameManager.Instance != null && !string.IsNullOrEmpty(portaId))
            GameManager.Instance.SetCausalState(portaId, true);
    }
    // ─────────────────────────────────────────────────────────────────────────
    private IEnumerator AnimaPorta(bool versoAperta)
    {
        // Animazione unica per slide/rotazione. SmoothStep evita il movimento
        // "lineare duro" da prototipo e dà un'apertura più pesante/tecnica.
        inAnimazione = true;
        if (versoAperta)
            RiproduciSuono(suonoApertura, 1.0f);
        else
            RiproduciSuono(suonoChiusura, 1.0f);
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
        {
            luceDiStato.enabled = true;
            luceDiStato.color = colore;
            luceDiStato.intensity = Mathf.Max(luceDiStato.intensity, 2.5f);
            luceDiStato.range = Mathf.Max(luceDiStato.range, 5.0f);
        }
        if (oggettoEmettitoreLuce != null)
        {
            Material mat = oggettoEmettitoreLuce.material;
            if (mat != null && !mat.HasProperty("_EmissionColor"))
            {
                Shader fallbackShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
                if (fallbackShader != null)
                {
                    mat = new Material(fallbackShader) { name = $"{oggettoEmettitoreLuce.name}_RuntimeDoorStatusGlow" };
                    oggettoEmettitoreLuce.material = mat;
                }
            }
            if (mat != null)
            {
                mat.color = colore;
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", colore);
                if (mat.HasProperty("_Color"))
                    mat.SetColor("_Color", colore);
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    mat.SetColor("_EmissionColor", colore * intensitaEmissione);
                }
            }
        }
    }
    private void InizializzaFeedbackVisivo()
    {
        // Le scene vecchie non sempre hanno luceDiStato / emettitore collegati
        // nell'Inspector. Qui recuperiamo una spia credibile prima di staccare i
        // figli dalla porta, così il portellone mostra rosso/verde anche in build.
        if (luceDiStato == null)
        {
            Light[] luciTrovate = GetComponentsInChildren<Light>(true);
            if (luciTrovate != null && luciTrovate.Length > 0)
                luceDiStato = luciTrovate[0];
        }

        if (oggettoEmettitoreLuce == null)
        {
            Renderer[] rendererTrovati = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer rend in rendererTrovati)
            {
                if (rend == null) continue;
                string n = rend.name.ToLowerInvariant();
                if (n.Contains("luce") || n.Contains("light") || n.Contains("led") || n.Contains("lamp") || n.Contains("spia") || n.Contains("status") || n.Contains("exit") || n.Contains("uscita") || n.Contains("monitor"))
                {
                    oggettoEmettitoreLuce = rend;
                    break;
                }
            }
        }

        if (luceDiStato == null)
        {
            GameObject luceRuntime = new GameObject("Runtime_Door_Status_Light");
            luceRuntime.transform.SetParent(transform, false);
            luceRuntime.transform.localPosition = Vector3.up * 1.6f + Vector3.forward * 0.15f;
            luceDiStato = luceRuntime.AddComponent<Light>();
            luceDiStato.type = LightType.Point;
            luceDiStato.range = 5.0f;
            luceDiStato.intensity = 2.5f;
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
