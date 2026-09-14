// ============================================================================
// Crisis Protocol / Sector Containment - Nemici e minacce
// File: .\Assets\CrisisProtocol\Scripts\Enemy\ManutenzioneBot.cs
// Responsabilita': definisce pattugliamento, inseguimento, attacco o comportamento di droni, guardie e bot ostili.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine; // usa lib // riga-ok
using UnityEngine.AI; // usa lib // riga-ok
using CrisisProtocol.UI; // usa lib // riga-ok

[RequireComponent(typeof(NavMeshAgent))] // nota unity // riga-ok
// blocco: classe x roba grossa
public class ManutenzioneBot : MonoBehaviour, IDamageable // classe qui // riga-ok
{ // apre // riga-ok
    // blocco: scelte rapide
    public enum StatoIA { RicercaAttiva, Inseguimento, AttaccoMischia, Morto } // enum val // riga-ok

    [Header("Stato e Navigazione")] // nota unity // riga-ok
    public StatoIA statoAttuale = StatoIA.RicercaAttiva; // roba pub // riga-ok
    [Tooltip("Velocità calma di ronda/camminata (consigliato: 1.0 - 1.5).")] // nota unity // riga-ok
    [SerializeField] [Range(0.5f, 3f)] private float velocitaRicerca = 1.2f; // setta // riga-ok
    [Tooltip("Velocità di inseguimento controllata (consigliato: 1.8 - 2.5).")] // nota unity // riga-ok
    [SerializeField] [Range(1f, 6f)] private float velocitaInseguimento = 2.2f; // setta // riga-ok
    [Tooltip("Accelerazione dell'agente: valori bassi (1.5 - 2.5) evitano scatti e partenze a razzo.")] // nota unity // riga-ok
    [SerializeField] [Range(0.5f, 5f)] private float accelerazione = 2.0f; // setta // riga-ok
    [Tooltip("Velocità di rotazione in gradi al secondo.")] // nota unity // riga-ok
    [SerializeField] [Range(60f, 240f)] private float velocitaRotazione = 120f; // setta // riga-ok

    [Header("Combattimento Corpo a Corpo (Pugni Devastanti)")] // nota unity // riga-ok
    [Tooltip("Distanza ravvicinata per sferrare il pugno al giocatore.")] // nota unity // riga-ok
    [SerializeField] private float distanzaAttacco = 1.9f; // setta // riga-ok
    [Tooltip("Raggio di portata entro cui il pugno infligge danno al momento dell'impatto.")] // nota unity // riga-ok
    [SerializeField] private float raggioImpattoPugno = 2.4f; // setta // riga-ok
    [Tooltip("Tempo di caricamento del colpo prima dell'impatto (1.0 secondo esatto).")] // nota unity // riga-ok
    [SerializeField] private float ritardoImpattoPugno = 1.0f; // setta // riga-ok
    [Tooltip("Tempo di attesa / cooldown tra un pugno e il successivo.")] // nota unity // riga-ok
    [SerializeField] private float cadenzaAttacco = 2.2f; // setta // riga-ok
    [Tooltip("Se true, il pugno infligge una percentuale fissa della salute totale del giocatore.")] // nota unity // riga-ok
    [SerializeField] private bool usaDannoPercentuale = true; // setta // riga-ok
    [Tooltip("Percentuale del danno totale (0.50 = 50% della salute massima del giocatore).")] // nota unity // riga-ok
    [Range(0.05f, 1.0f)] [SerializeField] private float percentualeDanno = 0.50f; // setta // riga-ok
    [Tooltip("Danno fisso alternativo applicato qualora non sia possibile calcolare la percentuale.")] // nota unity // riga-ok
    [SerializeField] private float dannoAttaccoFisso = 50f; // setta // riga-ok

    // Campi per retrocompatibilità con scene ed editor script esistenti (SceneDoctor / SceneAudioPopulator)
    [SerializeField] [HideInInspector] private float distanzaOttimaleTiro = 1.9f; // setta // riga-ok
    [SerializeField] [HideInInspector] private float dannoArma = 50f; // setta // riga-ok
    [SerializeField] [HideInInspector] private float cadenzaDiFuoco = 2.2f; // setta // riga-ok
    [SerializeField] [HideInInspector] private Transform puntoDiFuoco; // ok qua // riga-ok

    [Header("Comportamento Post-Emergenza (Fine Crisi)")] // nota unity // riga-ok
    [Tooltip("Se true, il bot entra in modalità manutenzione pacifica (non attacca, non insegue) quando l'emergenza finisce.")] // nota unity // riga-ok
    [SerializeField] private bool pacificaAFineEmergenza = true; // setta // riga-ok

    [Tooltip("Se true, il bot si spegne/ferma completamente sul posto a fine emergenza.")] // nota unity // riga-ok
    [SerializeField] private bool spegniAFineEmergenza = false; // setta // riga-ok

    [Header("Vagabondaggio Casuale (Roaming)")] // nota unity // riga-ok
    [Tooltip("Raggio massimo entro cui scegliere il prossimo punto di ronda locale.")] // nota unity // riga-ok
    [SerializeField] private float raggioPattugliamento = 8f; // setta // riga-ok
    [SerializeField] private float tempoPausaMin = 2f; // setta // riga-ok
    [SerializeField] private float tempoPausaMax = 4f; // setta // riga-ok
    private bool inPausa = false; // roba pub // riga-ok
    private float timerPausa = 0f; // roba pub // riga-ok
    private float timerMemoriaInseguimento = 0f; // roba pub // riga-ok
    private Vector3 posizioneIniziale; // roba pub // riga-ok
    private bool haDestinazioneAttiva = false; // roba pub // riga-ok
    private Vector3 destinazioneCorrente; // roba pub // riga-ok
    private float timerDestinazione = 0f; // roba pub // riga-ok

    [Header("Sensori e Rilevamento (Visione)")] // nota unity // riga-ok
    [SerializeField] private float raggioVisione = 14f; // setta // riga-ok
    [Range(0, 360)] [SerializeField] private float angoloVisione = 90f; // setta // riga-ok
    [SerializeField] private float raggioRilevamentoRavvicinato = 2.5f; // setta // riga-ok
    [SerializeField] private LayerMask layerOstacoli; // ok qua // riga-ok
    [SerializeField] private Transform puntoOcchi; // ok qua // riga-ok

    [Header("Audio 3D")] // nota unity // riga-ok
    [Tooltip("Suono continuo del servomotore/cingoli durante il movimento.")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoMovimentoLoop; // ok qua // riga-ok
    [Tooltip("Suono di avvistamento bersaglio / allarme robotico.")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoAvvistamento; // ok qua // riga-ok
    [Tooltip("Suono di sferrata / caricamento del pugno.")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoAttacco; // ok qua // riga-ok
    [Tooltip("Suono di impatto pugno a segno.")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoImpattoPugno; // ok qua // riga-ok
    [Tooltip("Suono di sparo / attacco (retrocompatibilità).")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoSparo; // ok qua // riga-ok
    [Tooltip("Suono di impatto subito / sfiato guasto.")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoDanno; // ok qua // riga-ok
    [Tooltip("Suono di disattivazione / morte robotica.")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoMorte; // ok qua // riga-ok
    [Range(0f, 1f)] [SerializeField] private float volumeAudio = 0.85f; // setta // riga-ok

    [Header("Statistiche Vitali")] // nota unity // riga-ok
    [SerializeField] private float salute = 100f; // setta // riga-ok
    [Tooltip("Tempo in secondi da attendere prima di rimuovere il corpo dal gioco dopo la morte.")] // nota unity // riga-ok
    [SerializeField] private float tempoDistruzioneCorpo = 3.0f; // setta // riga-ok

    [Header("Integrazione Animatore")] // nota unity // riga-ok
    [SerializeField] private string triggerMorte = "Die"; // setta // riga-ok
    [SerializeField] private string triggerAttacco = "Attack"; // setta // riga-ok
    [SerializeField] private string triggerSparo = "Shoot"; // setta // riga-ok

    private NavMeshAgent agente; // roba pub // riga-ok
    private Animator anim; // roba pub // riga-ok
    private Transform playerTransform; // roba pub // riga-ok
    private AudioSource audioSource; // roba pub // riga-ok
    private AudioSource audioMovimentoSource; // roba pub // riga-ok

    private Coroutine coroutineAttacco; // roba pub // riga-ok
    private float timerProssimoAttacco = 0f; // roba pub // riga-ok
    private bool staEseguendoPugno = false; // roba pub // riga-ok

    private int speedHash; // roba pub // riga-ok
    private int morteTriggerHash; // roba pub // riga-ok
    private int sparoTriggerHash; // roba pub // riga-ok
    private int attaccoTriggerHash; // roba pub // riga-ok
    private bool isMorto = false; // roba pub // riga-ok

    private float timerAggiornamentoPercorso = 0f; // roba pub // riga-ok

    private bool AgentePronto => agente != null && agente.enabled && agente.isOnNavMesh; // roba pub // riga-ok

    // blocco: funzione fa cose
    private void InizializzaAudio() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (audioSource == null) // se ok // riga-ok
            audioSource = GetComponent<AudioSource>(); // setta // riga-ok

        // blocco: controlla se va
        if (audioSource == null) // se ok // riga-ok
        { // apre // riga-ok
            audioSource = gameObject.AddComponent<AudioSource>(); // setta // riga-ok
            audioSource.playOnAwake = false; // setta // riga-ok
            audioSource.spatialBlend = 1.0f; // 3D // setta // riga-ok
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic; // setta // riga-ok
            audioSource.minDistance = 1.5f; // setta // riga-ok
            audioSource.maxDistance = 16.0f; // setta // riga-ok
            audioSource.dopplerLevel = 0f; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (audioMovimentoSource == null) // se ok // riga-ok
        { // apre // riga-ok
            Transform tMove = transform.Find("AudioMovimentoBot"); // setta // riga-ok
            // blocco: controlla se va
            if (tMove != null) // se ok // riga-ok
            { // apre // riga-ok
                audioMovimentoSource = tMove.GetComponent<AudioSource>(); // setta // riga-ok
            } // chiude // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
            { // apre // riga-ok
                GameObject goMove = new GameObject("AudioMovimentoBot"); // setta // riga-ok
                goMove.transform.SetParent(transform, false); // chiama // riga-ok
                audioMovimentoSource = goMove.AddComponent<AudioSource>(); // setta // riga-ok
            } // chiude // riga-ok

            audioMovimentoSource.playOnAwake = false; // setta // riga-ok
            audioMovimentoSource.spatialBlend = 1.0f; // setta // riga-ok
            audioMovimentoSource.loop = true; // setta // riga-ok
            audioMovimentoSource.rolloffMode = AudioRolloffMode.Logarithmic; // setta // riga-ok
            audioMovimentoSource.minDistance = 1.5f; // setta // riga-ok
            audioMovimentoSource.maxDistance = 14.0f; // setta // riga-ok
            audioMovimentoSource.dopplerLevel = 0f; // setta // riga-ok
            audioMovimentoSource.volume = volumeAudio * 0.7f; // setta // riga-ok
            // blocco: controlla se va
            if (suonoMovimentoLoop != null) // se ok // riga-ok
            { // apre // riga-ok
                audioMovimentoSource.clip = suonoMovimentoLoop; // setta // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void RiproduciSuono(AudioClip clip, float volumeMoltiplicatore = 1.0f) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (clip == null) return; // se ok // riga-ok
        InizializzaAudio(); // chiama // riga-ok
        // blocco: controlla se va
        if (audioSource != null) // se ok // riga-ok
        { // apre // riga-ok
            audioSource.pitch = Random.Range(0.95f, 1.05f); // setta // riga-ok
            audioSource.PlayOneShot(clip, volumeAudio * volumeMoltiplicatore); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    [ContextMenu("Ripristina Velocità e Parametri Calmi")] // nota unity // riga-ok
    // blocco: funzione fa cose
    public void ResetValoriPredefiniti() // roba pub // riga-ok
    { // apre // riga-ok
        velocitaRicerca = 1.2f; // setta // riga-ok
        velocitaInseguimento = 2.2f; // setta // riga-ok
        accelerazione = 2.0f; // setta // riga-ok
        velocitaRotazione = 120f; // setta // riga-ok
        raggioPattugliamento = 8f; // setta // riga-ok
        distanzaAttacco = 1.9f; // setta // riga-ok
        distanzaOttimaleTiro = 1.9f; // setta // riga-ok
        raggioImpattoPugno = 2.4f; // setta // riga-ok
        ritardoImpattoPugno = 1.0f; // setta // riga-ok
        cadenzaAttacco = 2.2f; // setta // riga-ok
        usaDannoPercentuale = true; // setta // riga-ok
        percentualeDanno = 0.50f; // setta // riga-ok
        dannoAttaccoFisso = 50f; // setta // riga-ok
        dannoArma = 50f; // setta // riga-ok
        cadenzaDiFuoco = 2.2f; // setta // riga-ok
        raggioVisione = 14f; // setta // riga-ok
        tempoPausaMin = 2f; // setta // riga-ok
        tempoPausaMax = 4f; // setta // riga-ok

        // blocco: controlla se va
        if (agente == null) agente = GetComponent<NavMeshAgent>(); // se ok // riga-ok
        // blocco: controlla se va
        if (agente != null) // se ok // riga-ok
        { // apre // riga-ok
            agente.speed = velocitaRicerca; // setta // riga-ok
            agente.acceleration = accelerazione; // setta // riga-ok
            agente.angularSpeed = velocitaRotazione; // setta // riga-ok
            agente.stoppingDistance = 0.8f; // setta // riga-ok
            agente.autoBraking = true; // setta // riga-ok
            agente.radius = 0.35f; // setta // riga-ok
            agente.height = 1.8f; // setta // riga-ok
            agente.autoTraverseOffMeshLink = false; // setta // riga-ok
            agente.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance; // setta // riga-ok
        } // chiude // riga-ok

        CentraModelliFigli(); // chiama // riga-ok
    } // chiude // riga-ok

    void Awake() // chiama // riga-ok
    { // apre // riga-ok
        CentraModelliFigli(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void CentraModelliFigli() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: gira piu volte
        foreach (Transform child in transform) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (child.localPosition != Vector3.zero) // se ok // riga-ok
            { // apre // riga-ok
                child.localPosition = Vector3.zero; // setta // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        Rigidbody[] allRbs = GetComponentsInChildren<Rigidbody>(); // setta // riga-ok
        // blocco: gira piu volte
        foreach (Rigidbody r in allRbs) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (r != null) // se ok // riga-ok
            { // apre // riga-ok
                r.isKinematic = true; // setta // riga-ok
                r.useGravity = false; // setta // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    void Start() // chiama // riga-ok
    { // apre // riga-ok
        CentraModelliFigli(); // chiama // riga-ok
        InizializzaAudio(); // chiama // riga-ok

        SectorContainmentTags.ApplyTag(gameObject, SectorContainmentTags.MaintenanceBot); // chiama // riga-ok

        agente = GetComponent<NavMeshAgent>(); // setta // riga-ok
        anim = GetComponent<Animator>(); // setta // riga-ok
        // blocco: controlla se va
        if (anim == null) // se ok // riga-ok
            anim = GetComponentInChildren<Animator>(); // setta // riga-ok

        // blocco: controlla se va
        if (anim != null) // se ok // riga-ok
        { // apre // riga-ok
            anim.applyRootMotion = false; // setta // riga-ok
        } // chiude // riga-ok

        Animator[] allAnimators = GetComponentsInChildren<Animator>(); // setta // riga-ok
        // blocco: gira piu volte
        foreach (Animator a in allAnimators) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (a != null) // se ok // riga-ok
                a.applyRootMotion = false; // setta // riga-ok
        } // chiude // riga-ok

        Rigidbody rb = GetComponent<Rigidbody>(); // setta // riga-ok
        // blocco: controlla se va
        if (rb != null) // se ok // riga-ok
        { // apre // riga-ok
            rb.isKinematic = true; // setta // riga-ok
            rb.useGravity = false; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (agente != null) // se ok // riga-ok
        { // apre // riga-ok
            agente.speed = velocitaRicerca; // setta // riga-ok
            agente.acceleration = accelerazione; // setta // riga-ok
            agente.angularSpeed = velocitaRotazione; // setta // riga-ok
            agente.stoppingDistance = 0.8f; // setta // riga-ok
            agente.autoBraking = true; // setta // riga-ok
            agente.radius = 0.35f; // setta // riga-ok
            agente.height = 1.8f; // setta // riga-ok
            agente.autoTraverseOffMeshLink = false; // setta // riga-ok
            agente.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance; // setta // riga-ok
        } // chiude // riga-ok

        CapsuleCollider cap = GetComponent<CapsuleCollider>(); // setta // riga-ok
        // blocco: controlla se va
        if (cap != null && agente != null) // se ok // riga-ok
        { // apre // riga-ok
            cap.radius = agente.radius * 0.85f; // setta // riga-ok
            cap.height = agente.height; // setta // riga-ok
            cap.center = new Vector3(0f, agente.height * 0.5f, 0f); // setta // riga-ok
        } // chiude // riga-ok

        speedHash = Animator.StringToHash("Speed"); // setta // riga-ok
        morteTriggerHash = Animator.StringToHash(triggerMorte); // setta // riga-ok
        sparoTriggerHash = Animator.StringToHash(triggerSparo); // setta // riga-ok
        attaccoTriggerHash = Animator.StringToHash(triggerAttacco); // setta // riga-ok

        posizioneIniziale = transform.position; // setta // riga-ok
        haDestinazioneAttiva = false; // setta // riga-ok
        inPausa = false; // setta // riga-ok
        staEseguendoPugno = false; // setta // riga-ok

        TrovaPlayer(); // chiama // riga-ok

        // blocco: controlla se va
        if (puntoOcchi == null) puntoOcchi = transform; // se ok // riga-ok
        // blocco: controlla se va
        if (puntoDiFuoco == null) puntoDiFuoco = transform; // se ok // riga-ok

        // blocco: controlla se va
        if (agente != null) // se ok // riga-ok
        { // apre // riga-ok
            NavMeshHit hit; // ok qua // riga-ok
            // blocco: controlla se va
            if (NavMesh.SamplePosition(transform.position, out hit, 10.0f, NavMesh.AllAreas)) // se ok // riga-ok
            { // apre // riga-ok
                transform.position = hit.position; // setta // riga-ok
                agente.Warp(hit.position); // chiama // riga-ok
                agente.enabled = true; // setta // riga-ok
                agente.isStopped = false; // setta // riga-ok
            } // chiude // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
            { // apre // riga-ok
                Debug.LogWarning($"[BOT] {gameObject.name}: NavMesh non trovata entro 10m. Verifica il posizionamento.", this); // logga // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (AgentePronto) // se ok // riga-ok
        { // apre // riga-ok
            ImpostaNuovaDestinazioneCasuale(); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void TrovaPlayer() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (playerTransform != null) return; // se ok // riga-ok

        GameObject playerObj = GameObject.FindGameObjectWithTag(SectorContainmentTags.Player); // setta // riga-ok
        // blocco: controlla se va
        if (playerObj == null) // se ok // riga-ok
        { // apre // riga-ok
            playerObj = GameObject.FindGameObjectWithTag("Player"); // setta // riga-ok
        } // chiude // riga-ok
        // blocco: controlla se va
        if (playerObj == null) // se ok // riga-ok
        { // apre // riga-ok
            muve_pg playerMovement = Object.FindFirstObjectByType<muve_pg>(); // setta // riga-ok
            // blocco: controlla se va
            if (playerMovement != null) // se ok // riga-ok
                playerObj = playerMovement.gameObject; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (playerObj != null) // se ok // riga-ok
        { // apre // riga-ok
            playerTransform = playerObj.transform; // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    void Update() // chiama // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (ModalUIState.IsModalOpen) // se ok // riga-ok
            return; // torna val // riga-ok

        // blocco: controlla se va
        if (isMorto) return; // se ok // riga-ok

        // blocco: controlla se va
        if (playerTransform == null) // se ok // riga-ok
        { // apre // riga-ok
            TrovaPlayer(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (AgentePronto) // se ok // riga-ok
        { // apre // riga-ok
            agente.acceleration = accelerazione; // setta // riga-ok
            agente.angularSpeed = velocitaRotazione; // setta // riga-ok
        } // chiude // riga-ok

        GestisciAudioMovimento(); // chiama // riga-ok

        bool emergenzaFinita = MissionManager.Instance != null && MissionManager.Instance.EstrazioneSbloccata; // setta // riga-ok

        // blocco: controlla se va
        if (emergenzaFinita && pacificaAFineEmergenza) // se ok // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (spegniAFineEmergenza) // se ok // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (AgentePronto) // se ok // riga-ok
                { // apre // riga-ok
                    agente.isStopped = true; // setta // riga-ok
                    agente.velocity = Vector3.zero; // setta // riga-ok
                } // chiude // riga-ok
                // blocco: controlla se va
                if (anim != null && anim.runtimeAnimatorController != null) anim.SetFloat(speedHash, 0f); // se ok // riga-ok
                return; // torna val // riga-ok
            } // chiude // riga-ok

            // blocco: controlla se va
            if (statoAttuale == StatoIA.Inseguimento || statoAttuale == StatoIA.AttaccoMischia) // se ok // riga-ok
            { // apre // riga-ok
                statoAttuale = StatoIA.RicercaAttiva; // setta // riga-ok
                haDestinazioneAttiva = false; // setta // riga-ok
                inPausa = false; // setta // riga-ok
                staEseguendoPugno = false; // setta // riga-ok
                // blocco: controlla se va
                if (AgentePronto) // se ok // riga-ok
                { // apre // riga-ok
                    agente.isStopped = false; // setta // riga-ok
                    agente.speed = velocitaRicerca; // setta // riga-ok
                    agente.stoppingDistance = 0.5f; // setta // riga-ok
                } // chiude // riga-ok
                ImpostaNuovaDestinazioneCasuale(); // chiama // riga-ok
            } // chiude // riga-ok

            EseguiRondaCasuale(); // chiama // riga-ok

            // blocco: controlla se va
            if (anim != null && anim.runtimeAnimatorController != null && AgentePronto) // se ok // riga-ok
            { // apre // riga-ok
                float speedVal = 0f; // setta // riga-ok
                // blocco: controlla se va
                if (!inPausa && (agente.velocity.sqrMagnitude > 0.04f || agente.desiredVelocity.sqrMagnitude > 0.04f || agente.hasPath)) // se ok // riga-ok
                { // apre // riga-ok
                    speedVal = (agente.velocity.magnitude > 0.1f) ? agente.velocity.magnitude : velocitaRicerca; // setta // riga-ok
                } // chiude // riga-ok
                anim.SetFloat(speedHash, speedVal, 0.15f, Time.deltaTime); // chiama // riga-ok
            } // chiude // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        timerProssimoAttacco -= Time.deltaTime; // setta // riga-ok
        timerAggiornamentoPercorso += Time.deltaTime; // setta // riga-ok

        bool bersaglioRilevato = RilevaBersaglio(out float distanzaDalPlayer); // setta // riga-ok

        GestisciMacchinaAStati(bersaglioRilevato, distanzaDalPlayer); // chiama // riga-ok

        // blocco: controlla se va
        if (anim != null && anim.runtimeAnimatorController != null && AgentePronto) // se ok // riga-ok
        { // apre // riga-ok
            float speedVal = 0f; // setta // riga-ok
            // blocco: controlla se va
            if (!inPausa && !staEseguendoPugno && (agente.velocity.sqrMagnitude > 0.04f || agente.desiredVelocity.sqrMagnitude > 0.04f || (statoAttuale != StatoIA.AttaccoMischia && agente.hasPath))) // se ok // riga-ok
            { // apre // riga-ok
                speedVal = (agente.velocity.magnitude > 0.1f) ? agente.velocity.magnitude : ((statoAttuale == StatoIA.Inseguimento) ? velocitaInseguimento : velocitaRicerca); // setta // riga-ok
            } // chiude // riga-ok
            anim.SetFloat(speedHash, speedVal, 0.15f, Time.deltaTime); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void GestisciAudioMovimento() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (audioMovimentoSource == null || suonoMovimentoLoop == null) return; // se ok // riga-ok
        // blocco: controlla se va
        if (audioMovimentoSource.clip == null) audioMovimentoSource.clip = suonoMovimentoLoop; // se ok // riga-ok

        bool staMuovendo = AgentePronto && !agente.isStopped && !inPausa && !staEseguendoPugno && (agente.velocity.sqrMagnitude > 0.04f || agente.desiredVelocity.sqrMagnitude > 0.04f); // setta // riga-ok

        // blocco: controlla se va
        if (staMuovendo && !isMorto) // se ok // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (!audioMovimentoSource.isPlaying) // se ok // riga-ok
            { // apre // riga-ok
                audioMovimentoSource.Play(); // chiama // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (audioMovimentoSource.isPlaying) // se ok // riga-ok
            { // apre // riga-ok
                audioMovimentoSource.Pause(); // chiama // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void GestisciMacchinaAStati(bool bersaglioRilevato, float distanza) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: scegli strada
        switch (statoAttuale) // scegli // riga-ok
        { // apre // riga-ok
            case StatoIA.RicercaAttiva: // caso // riga-ok
                // blocco: controlla se va
                if (bersaglioRilevato) // se ok // riga-ok
                { // apre // riga-ok
                    statoAttuale = StatoIA.Inseguimento; // setta // riga-ok
                    inPausa = false; // setta // riga-ok
                    haDestinazioneAttiva = false; // setta // riga-ok
                    timerMemoriaInseguimento = 2.5f; // setta // riga-ok
                    RiproduciSuono(suonoAvvistamento); // chiama // riga-ok
                    // blocco: controlla se va
                    if (AgentePronto) // se ok // riga-ok
                    { // apre // riga-ok
                        agente.isStopped = false; // setta // riga-ok
                        agente.speed = velocitaInseguimento; // setta // riga-ok
                        agente.stoppingDistance = 0.8f; // setta // riga-ok
                    } // chiude // riga-ok
                    Debug.Log("<color=red>[BOT MANUTENZIONE] Bersaglio rilevato! Inizio avvicinamento per attacco corpo a corpo con pugni.</color>"); // logga // riga-ok
                } // chiude // riga-ok
                // blocco: caso diverso
                else // se no // riga-ok
                { // apre // riga-ok
                    EseguiRondaCasuale(); // chiama // riga-ok
                } // chiude // riga-ok
                break; // stop // riga-ok

            case StatoIA.Inseguimento: // caso // riga-ok
                // blocco: controlla se va
                if (bersaglioRilevato) // se ok // riga-ok
                { // apre // riga-ok
                    timerMemoriaInseguimento = 2.5f; // setta // riga-ok
                } // chiude // riga-ok
                // blocco: caso diverso
                else // se no // riga-ok
                { // apre // riga-ok
                    timerMemoriaInseguimento -= Time.deltaTime; // setta // riga-ok
                } // chiude // riga-ok

                // blocco: controlla se va
                if (!bersaglioRilevato && timerMemoriaInseguimento <= 0f) // se ok // riga-ok
                { // apre // riga-ok
                    statoAttuale = StatoIA.RicercaAttiva; // setta // riga-ok
                    inPausa = false; // setta // riga-ok
                    haDestinazioneAttiva = false; // setta // riga-ok
                    // blocco: controlla se va
                    if (AgentePronto) // se ok // riga-ok
                    { // apre // riga-ok
                        agente.isStopped = false; // setta // riga-ok
                        agente.speed = velocitaRicerca; // setta // riga-ok
                        agente.stoppingDistance = 0.5f; // setta // riga-ok
                    } // chiude // riga-ok
                    ImpostaNuovaDestinazioneCasuale(); // chiama // riga-ok
                } // chiude // riga-ok
                // blocco: controlla se va
                else if (bersaglioRilevato && distanza <= distanzaAttacco) // se ok // riga-ok
                { // apre // riga-ok
                    statoAttuale = StatoIA.AttaccoMischia; // setta // riga-ok
                    // blocco: controlla se va
                    if (AgentePronto) // se ok // riga-ok
                    { // apre // riga-ok
                        agente.isStopped = true; // setta // riga-ok
                        agente.velocity = Vector3.zero; // setta // riga-ok
                    } // chiude // riga-ok
                    EseguiRoutineDiMischia(distanza); // chiama // riga-ok
                } // chiude // riga-ok
                // blocco: caso diverso
                else // se no // riga-ok
                { // apre // riga-ok
                    // blocco: controlla se va
                    if (AgentePronto) // se ok // riga-ok
                    { // apre // riga-ok
                        agente.isStopped = false; // setta // riga-ok
                        agente.speed = velocitaInseguimento; // setta // riga-ok
                        agente.stoppingDistance = 0.8f; // setta // riga-ok
                        agente.SetDestination(playerTransform.position); // chiama // riga-ok
                    } // chiude // riga-ok
                    // blocco: controlla se va
                    else if (playerTransform != null) // se ok // riga-ok
                    { // apre // riga-ok
                        Vector3 targetPos = playerTransform.position; // setta // riga-ok
                        targetPos.y = transform.position.y; // setta // riga-ok
                        transform.position = Vector3.MoveTowards(transform.position, targetPos, velocitaInseguimento * Time.deltaTime); // setta // riga-ok
                        Vector3 dir = (playerTransform.position - transform.position).normalized; // setta // riga-ok
                        dir.y = 0; // setta // riga-ok
                        // blocco: controlla se va
                        if (dir != Vector3.zero) // se ok // riga-ok
                            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 6f * Time.deltaTime); // setta // riga-ok
                    } // chiude // riga-ok
                } // chiude // riga-ok
                break; // stop // riga-ok

            case StatoIA.AttaccoMischia: // caso // riga-ok
                // blocco: controlla se va
                if (!staEseguendoPugno && (!bersaglioRilevato || distanza > distanzaAttacco + 1.2f)) // se ok // riga-ok
                { // apre // riga-ok
                    statoAttuale = StatoIA.Inseguimento; // setta // riga-ok
                    timerMemoriaInseguimento = 2.5f; // setta // riga-ok
                    // blocco: controlla se va
                    if (AgentePronto) // se ok // riga-ok
                    { // apre // riga-ok
                        agente.isStopped = false; // setta // riga-ok
                        agente.speed = velocitaInseguimento; // setta // riga-ok
                    } // chiude // riga-ok
                } // chiude // riga-ok
                // blocco: caso diverso
                else // se no // riga-ok
                { // apre // riga-ok
                    EseguiRoutineDiMischia(distanza); // chiama // riga-ok
                } // chiude // riga-ok
                break; // stop // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void EseguiRoutineDiMischia(float distanzaAttuale) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (AgentePronto) // se ok // riga-ok
        { // apre // riga-ok
            agente.isStopped = true; // setta // riga-ok
            agente.velocity = Vector3.zero; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (playerTransform != null) // se ok // riga-ok
        { // apre // riga-ok
            Vector3 direzioneMira = (playerTransform.position - transform.position).normalized; // setta // riga-ok
            direzioneMira.y = 0; // setta // riga-ok
            // blocco: controlla se va
            if (direzioneMira.sqrMagnitude > 0.001f) // se ok // riga-ok
            { // apre // riga-ok
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direzioneMira), 8f * Time.deltaTime); // setta // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (timerProssimoAttacco <= 0f && !staEseguendoPugno) // se ok // riga-ok
        { // apre // riga-ok
            AvviaAttaccoPugno(); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void AvviaAttaccoPugno() // roba pub // riga-ok
    { // apre // riga-ok
        staEseguendoPugno = true; // setta // riga-ok
        timerProssimoAttacco = cadenzaAttacco; // setta // riga-ok

        // blocco: controlla se va
        if (anim != null) // se ok // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (!string.IsNullOrEmpty(triggerAttacco)) // se ok // riga-ok
            { // apre // riga-ok
                anim.ResetTrigger(triggerAttacco); // chiama // riga-ok
                anim.SetTrigger(triggerAttacco); // chiama // riga-ok
            } // chiude // riga-ok
            // blocco: controlla se va
            if (!string.IsNullOrEmpty(triggerSparo)) // se ok // riga-ok
            { // apre // riga-ok
                anim.ResetTrigger(triggerSparo); // chiama // riga-ok
                anim.SetTrigger(triggerSparo); // chiama // riga-ok
            } // chiude // riga-ok
            anim.ResetTrigger("Attack"); // chiama // riga-ok
            anim.SetTrigger("Attack"); // chiama // riga-ok
            anim.ResetTrigger("attaca"); // chiama // riga-ok
            anim.SetTrigger("attaca"); // chiama // riga-ok
        } // chiude // riga-ok

        AudioClip clipDaRiprodurre = suonoAttacco != null ? suonoAttacco : (suonoSparo != null ? suonoSparo : null); // setta // riga-ok
        RiproduciSuono(clipDaRiprodurre); // chiama // riga-ok

        // blocco: controlla se va
        if (coroutineAttacco != null) // se ok // riga-ok
        { // apre // riga-ok
            StopCoroutine(coroutineAttacco); // corutina // riga-ok
        } // chiude // riga-ok

        coroutineAttacco = StartCoroutine(EseguiImpattoPugno(ritardoImpattoPugno)); // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private System.Collections.IEnumerator EseguiImpattoPugno(float ritardo) // roba pub // riga-ok
    { // apre // riga-ok
        Debug.Log($"<color=orange>[BOT MANUTENZIONE]</color> <b>Caricamento pugno pesante in corso... (impatto tra {ritardo:F1}s)</b>"); // logga // riga-ok

        yield return new WaitForSeconds(ritardo); // aspetta // riga-ok

        // blocco: controlla se va
        if (isMorto) // se ok // riga-ok
        { // apre // riga-ok
            staEseguendoPugno = false; // setta // riga-ok
            coroutineAttacco = null; // setta // riga-ok
            yield break; // aspetta // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (playerTransform == null) // se ok // riga-ok
        { // apre // riga-ok
            TrovaPlayer(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (playerTransform != null) // se ok // riga-ok
        { // apre // riga-ok
            float dist = Vector3.Distance(transform.position, playerTransform.position); // setta // riga-ok

            // blocco: controlla se va
            if (dist <= raggioImpattoPugno) // se ok // riga-ok
            { // apre // riga-ok
                SalutePlayer vitaPlayer = playerTransform.GetComponent<SalutePlayer>() ??  // setta // riga-ok
                                         playerTransform.GetComponentInParent<SalutePlayer>() ??  // ok qua // riga-ok
                                         playerTransform.GetComponentInChildren<SalutePlayer>(); // chiama // riga-ok

                float dannoInflitto = dannoAttaccoFisso; // setta // riga-ok

                // blocco: controlla se va
                if (vitaPlayer != null) // se ok // riga-ok
                { // apre // riga-ok
                    dannoInflitto = usaDannoPercentuale ? (vitaPlayer.puntiVitaMassimi * percentualeDanno) : dannoAttaccoFisso; // setta // riga-ok
                    vitaPlayer.SubisciDanno(dannoInflitto); // chiama // riga-ok
                    Debug.Log($"<color=red><b>[BOT MANUTENZIONE] PUGNO DEVASTANTE A SEGNO!</b></color> Inflitti <b>{dannoInflitto:F0} HP</b> (50% salute totale) a {playerTransform.name}!"); // logga // riga-ok
                } // chiude // riga-ok
                // blocco: caso diverso
                else // se no // riga-ok
                { // apre // riga-ok
                    IDamageable damageable = playerTransform.GetComponent<IDamageable>() ??  // setta // riga-ok
                                             playerTransform.GetComponentInParent<IDamageable>() ??  // ok qua // riga-ok
                                             playerTransform.GetComponentInChildren<IDamageable>(); // chiama // riga-ok
                    // blocco: controlla se va
                    if (damageable != null) // se ok // riga-ok
                    { // apre // riga-ok
                        damageable.SubisciDanno(dannoInflitto); // chiama // riga-ok
                        Debug.Log($"<color=red><b>[BOT MANUTENZIONE] PUGNO A SEGNO!</b></color> Inflitti {dannoInflitto:F0} HP!"); // logga // riga-ok
                    } // chiude // riga-ok
                } // chiude // riga-ok

                // blocco: controlla se va
                if (suonoImpattoPugno != null) // se ok // riga-ok
                { // apre // riga-ok
                    RiproduciSuono(suonoImpattoPugno); // chiama // riga-ok
                } // chiude // riga-ok
                // blocco: controlla se va
                else if (suonoDanno != null) // se ok // riga-ok
                { // apre // riga-ok
                    RiproduciSuono(suonoDanno); // chiama // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
            { // apre // riga-ok
                Debug.Log($"<color=yellow>[BOT MANUTENZIONE] Pugno a vuoto!</color> Il giocatore si trova a {dist:F2}m (fuori dalla portata di {raggioImpattoPugno}m). Colpo schivato!"); // logga // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        staEseguendoPugno = false; // setta // riga-ok
        coroutineAttacco = null; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private bool HaLineaDiVistaLibera(Vector3 eyeOrigin, Vector3 playerChest, float maxDistance) // roba pub // riga-ok
    { // apre // riga-ok
        Vector3 direction = (playerChest - eyeOrigin); // setta // riga-ok
        float distance = direction.magnitude; // setta // riga-ok
        // blocco: controlla se va
        if (distance > maxDistance || distance < 0.01f) return distance <= maxDistance; // se ok // riga-ok
        direction.Normalize(); // chiama // riga-ok

        RaycastHit[] hits = Physics.RaycastAll(eyeOrigin, direction, distance, ~0, QueryTriggerInteraction.Ignore); // setta // riga-ok
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance)); // setta // riga-ok

        // blocco: gira piu volte
        foreach (RaycastHit hit in hits) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (hit.transform.root == transform.root || hit.collider.CompareTag(SectorContainmentTags.Enemy) || hit.collider.CompareTag(SectorContainmentTags.Drone) || hit.collider.CompareTag(SectorContainmentTags.MaintenanceBot)) // se ok // riga-ok
                continue; // salta // riga-ok

            // blocco: controlla se va
            if (hit.transform.root == playerTransform.root || hit.collider.CompareTag(SectorContainmentTags.Player)) // se ok // riga-ok
                return true; // torna val // riga-ok

            // blocco: controlla se va
            if (hit.collider.isTrigger) // se ok // riga-ok
                continue; // salta // riga-ok

            // blocco: controlla se va
            if (hit.distance >= distance - 0.3f) // se ok // riga-ok
                return true; // torna val // riga-ok

            return false; // torna val // riga-ok
        } // chiude // riga-ok

        return true; // torna val // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private bool RilevaBersaglio(out float distanza) // roba pub // riga-ok
    { // apre // riga-ok
        distanza = Vector3.Distance(transform.position, playerTransform != null ? playerTransform.position : transform.position); // setta // riga-ok
        // blocco: controlla se va
        if (playerTransform == null || distanza > raggioVisione) return false; // se ok // riga-ok

        Vector3 origineOcchi = transform.position + Vector3.up * 1.2f + transform.forward * 0.35f; // setta // riga-ok
        Vector3 playerChest = playerTransform.position + Vector3.up * 1.0f; // setta // riga-ok
        Vector3 direzioneVersoPlayer = (playerChest - origineOcchi); // setta // riga-ok
        float distEffettiva = direzioneVersoPlayer.magnitude; // setta // riga-ok
        // blocco: controlla se va
        if (distEffettiva <= 0.001f) return true; // se ok // riga-ok
        direzioneVersoPlayer.Normalize(); // chiama // riga-ok

        bool inCampoVisivo = false; // setta // riga-ok
        // blocco: controlla se va
        if (distEffettiva <= raggioRilevamentoRavvicinato) // se ok // riga-ok
        { // apre // riga-ok
            inCampoVisivo = true; // setta // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            float angoloFrontale = Vector3.Angle(transform.forward, direzioneVersoPlayer); // setta // riga-ok
            // blocco: controlla se va
            if (angoloFrontale <= angoloVisione / 2f) // se ok // riga-ok
            { // apre // riga-ok
                inCampoVisivo = true; // setta // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (!inCampoVisivo) return false; // se ok // riga-ok

        return HaLineaDiVistaLibera(origineOcchi, playerChest, distEffettiva); // torna val // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void EseguiRondaCasuale() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (!AgentePronto) // se ok // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (agente != null && agente.enabled && !agente.isOnNavMesh) // se ok // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 10.0f, NavMesh.AllAreas)) // se ok // riga-ok
                { // apre // riga-ok
                    transform.position = hit.position; // setta // riga-ok
                    agente.Warp(hit.position); // chiama // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (inPausa) // se ok // riga-ok
        { // apre // riga-ok
            agente.isStopped = true; // setta // riga-ok
            agente.velocity = Vector3.zero; // setta // riga-ok
            timerPausa -= Time.deltaTime; // setta // riga-ok

            // blocco: controlla se va
            if (timerPausa <= 0f) // se ok // riga-ok
            { // apre // riga-ok
                inPausa = false; // setta // riga-ok
                haDestinazioneAttiva = false; // setta // riga-ok
                ImpostaNuovaDestinazioneCasuale(); // chiama // riga-ok
            } // chiude // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (!haDestinazioneAttiva) // se ok // riga-ok
        { // apre // riga-ok
            ImpostaNuovaDestinazioneCasuale(); // chiama // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        timerDestinazione += Time.deltaTime; // setta // riga-ok

        agente.isStopped = false; // setta // riga-ok
        agente.speed = velocitaRicerca; // setta // riga-ok
        agente.stoppingDistance = 0.5f; // setta // riga-ok

        Vector2 posAgenteXZ = new Vector2(transform.position.x, transform.position.z); // setta // riga-ok
        Vector2 posDestXZ = new Vector2(destinazioneCorrente.x, destinazioneCorrente.z); // setta // riga-ok
        float distRealeXZ = Vector2.Distance(posAgenteXZ, posDestXZ); // setta // riga-ok

        // blocco: controlla se va
        if (distRealeXZ <= agente.stoppingDistance + 0.6f || (!agente.pathPending && agente.hasPath && timerDestinazione > 0.5f && agente.remainingDistance <= agente.stoppingDistance + 0.4f)) // se ok // riga-ok
        { // apre // riga-ok
            haDestinazioneAttiva = false; // setta // riga-ok
            inPausa = true; // setta // riga-ok
            timerPausa = Random.Range(tempoPausaMin, tempoPausaMax); // setta // riga-ok
            Debug.Log($"<color=cyan>[BOT]</color> {gameObject.name}: punto di ronda raggiunto. Pausa per {timerPausa:F1}s."); // logga // riga-ok
        } // chiude // riga-ok
        // blocco: controlla se va
        else if (timerDestinazione > 12.0f || (!agente.pathPending && !agente.hasPath && timerDestinazione > 1.0f)) // se ok // riga-ok
        { // apre // riga-ok
            haDestinazioneAttiva = false; // setta // riga-ok
            ImpostaNuovaDestinazioneCasuale(); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void ImpostaNuovaDestinazioneCasuale() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (!AgentePronto || isMorto) return; // se ok // riga-ok

        Vector3 centroRonda = (posizioneIniziale != Vector3.zero) ? posizioneIniziale : transform.position; // setta // riga-ok

        // blocco: gira piu volte
        for (int i = 0; i < 20; i++) // ciclo x // riga-ok
        { // apre // riga-ok
            Vector3 basePoint = (i % 2 == 0) ? centroRonda : transform.position; // setta // riga-ok
            Vector2 cerchio2D = Random.insideUnitCircle * raggioPattugliamento; // setta // riga-ok
            Vector3 puntoCandidato = basePoint + new Vector3(cerchio2D.x, 0f, cerchio2D.y); // setta // riga-ok

            // blocco: controlla se va
            if (NavMesh.SamplePosition(puntoCandidato, out NavMeshHit hit, raggioPattugliamento + 2.0f, NavMesh.AllAreas)) // se ok // riga-ok
            { // apre // riga-ok
                float dist = Vector3.Distance(transform.position, hit.position); // setta // riga-ok
                // blocco: controlla se va
                if (dist > 1.5f) // se ok // riga-ok
                { // apre // riga-ok
                    destinazioneCorrente = hit.position; // setta // riga-ok
                    agente.isStopped = false; // setta // riga-ok
                    agente.speed = velocitaRicerca; // setta // riga-ok
                    agente.SetDestination(hit.position); // chiama // riga-ok
                    haDestinazioneAttiva = true; // setta // riga-ok
                    inPausa = false; // setta // riga-ok
                    timerDestinazione = 0f; // setta // riga-ok
                    return; // torna val // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (Vector3.Distance(transform.position, centroRonda) > 2.0f) // se ok // riga-ok
        { // apre // riga-ok
            destinazioneCorrente = centroRonda; // setta // riga-ok
            agente.isStopped = false; // setta // riga-ok
            agente.speed = velocitaRicerca; // setta // riga-ok
            agente.SetDestination(centroRonda); // chiama // riga-ok
            haDestinazioneAttiva = true; // setta // riga-ok
            inPausa = false; // setta // riga-ok
            timerDestinazione = 0f; // setta // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        haDestinazioneAttiva = false; // setta // riga-ok
        inPausa = true; // setta // riga-ok
        timerPausa = 1.0f; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void SubisciDanno(float quantitaDanno) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (isMorto) return; // se ok // riga-ok

        salute -= quantitaDanno; // setta // riga-ok
        RiproduciSuono(suonoDanno); // chiama // riga-ok

        // blocco: controlla se va
        if (statoAttuale == StatoIA.RicercaAttiva) // se ok // riga-ok
        { // apre // riga-ok
            statoAttuale = StatoIA.Inseguimento; // setta // riga-ok
            inPausa = false; // setta // riga-ok
        } // chiude // riga-ok

        Debug.Log($"[BOT] {gameObject.name} ha subito {quantitaDanno} di danno. Salute residua: {salute}"); // logga // riga-ok

        // blocco: controlla se va
        if (salute <= 0) // se ok // riga-ok
        { // apre // riga-ok
            EseguiMorte(); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void EseguiMorte() // roba pub // riga-ok
    { // apre // riga-ok
        isMorto = true; // setta // riga-ok
        statoAttuale = StatoIA.Morto; // setta // riga-ok
        staEseguendoPugno = false; // setta // riga-ok

        // blocco: controlla se va
        if (coroutineAttacco != null) // se ok // riga-ok
        { // apre // riga-ok
            StopCoroutine(coroutineAttacco); // corutina // riga-ok
            coroutineAttacco = null; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (audioMovimentoSource != null && audioMovimentoSource.isPlaying) // se ok // riga-ok
        { // apre // riga-ok
            audioMovimentoSource.Stop(); // chiama // riga-ok
        } // chiude // riga-ok

        RiproduciSuono(suonoMorte); // chiama // riga-ok

        Debug.Log($"<b><color=red>[DECESSO] {gameObject.name} ha esaurito i punti vita.</color></b>"); // logga // riga-ok

        // blocco: controlla se va
        if (agente != null) // se ok // riga-ok
        { // apre // riga-ok
            agente.ResetPath(); // chiama // riga-ok
            agente.enabled = false; // setta // riga-ok
        } // chiude // riga-ok

        Collider col = GetComponent<Collider>(); // setta // riga-ok
        // blocco: controlla se va
        if (col != null) // se ok // riga-ok
        { // apre // riga-ok
            col.isTrigger = true; // setta // riga-ok
        } // chiude // riga-ok

        Rigidbody rb = GetComponent<Rigidbody>(); // setta // riga-ok
        // blocco: controlla se va
        if (rb != null) // se ok // riga-ok
        { // apre // riga-ok
            rb.useGravity = false; // setta // riga-ok
            rb.linearVelocity = Vector3.zero; // setta // riga-ok
            rb.isKinematic = true; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (anim != null) // se ok // riga-ok
        { // apre // riga-ok
            anim.SetTrigger(morteTriggerHash); // chiama // riga-ok
        } // chiude // riga-ok

        Destroy(gameObject, tempoDistruzioneCorpo); // elimina // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnDrawGizmosSelected() // roba pub // riga-ok
    { // apre // riga-ok
        Gizmos.color = Color.yellow; // setta // riga-ok
        Gizmos.DrawWireSphere(transform.position, raggioVisione); // chiama // riga-ok
        Gizmos.color = Color.cyan; // setta // riga-ok
        Gizmos.DrawWireSphere(transform.position, distanzaAttacco); // chiama // riga-ok
        Gizmos.color = Color.red; // setta // riga-ok
        Gizmos.DrawWireSphere(transform.position, raggioImpattoPugno); // chiama // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
