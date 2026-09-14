// ============================================================================
// Crisis Protocol / Sector Containment - Nemici e minacce
// File: .\Assets\CrisisProtocol\Scripts\Enemy\GuardiaNpc.cs
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
public class GuardiaNpc : MonoBehaviour, IDamageable // classe qui // riga-ok
{ // apre // riga-ok
    // blocco: scelte rapide
    public enum StatoGuardia { Inattiva, Ronda, Sospettosa, Inseguimento, RitornoAllaBase, Morta } // enum val // riga-ok

    [Header("Configurazione Comportamento")] // nota unity // riga-ok
    [Tooltip("Se attivato, la guardia rimarrà ferma sul posto invece di seguire la ronda.")] // nota unity // riga-ok
    [SerializeField] private bool applicaTagEnemyAutomatico = true; // setta // riga-ok
    public bool eStatica = false; // roba pub // riga-ok
    public StatoGuardia statoAttuale = StatoGuardia.Ronda; // roba pub // riga-ok

    [Header("Parametri di Movimento (NavMesh)")] // nota unity // riga-ok
    public Transform[] waypointRonda; // roba pub // riga-ok
    public float velocitaRonda = 2f; // roba pub // riga-ok
    public float velocitaInseguimento = 4.5f; // roba pub // riga-ok
    [Tooltip("Distanza ravvicinata corpo a corpo per sferrare il pugno al giocatore.")] // nota unity // riga-ok
    public float distanzaArresto = 1.9f; // roba pub // riga-ok
    private int indiceWaypointAttuale = 0; // roba pub // riga-ok

    [Header("Comportamento Post-Emergenza (Fine Crisi)")] // nota unity // riga-ok
    [Tooltip("Se true, la guardia smette di essere ostile quando l'emergenza termina (focolai contenuti ed estrazione sbloccata).")] // nota unity // riga-ok
    [SerializeField] private bool disattivaOstilitAFineEmergenza = true; // setta // riga-ok
    [Tooltip("Se true, la guardia si ferma completamente/si spegne a fine emergenza. Se false, continua la ronda pacifica senza attaccare.")] // nota unity // riga-ok
    [SerializeField] private bool spegniAFineEmergenza = false; // setta // riga-ok

    [Header("Pattuglia Random (attiva se Waypoint Ronda è vuoto)")] // nota unity // riga-ok
    [Tooltip("Raggio entro cui scegliere il prossimo punto casuale sulla NavMesh.")] // nota unity // riga-ok
    public float raggioRondaRandom = 15f; // roba pub // riga-ok
    [Tooltip("Secondi di pausa tra un punto casuale e il successivo.")] // nota unity // riga-ok
    public float attesaTraPuntiRandom = 1.5f; // roba pub // riga-ok
    private Vector3 destinazioneRandom; // roba pub // riga-ok
    private float timerAttesaRandom = 0f; // roba pub // riga-ok
    private bool inAttesaRandom = false; // roba pub // riga-ok
    private bool destinazioneRandomValida = false; // roba pub // riga-ok
    
    private Vector3 posizioneIniziale; // roba pub // riga-ok

    [Header("Sensore Visivo (Vista)")] // nota unity // riga-ok
    public float raggioVisione = 12f; // roba pub // riga-ok
    [Range(0, 180)] public float angoloVisione = 90f; // setta // riga-ok
    [Range(0, 180)] public float angoloPuntoCiecoStealth = 30f; // setta // riga-ok
    public LayerMask layerOstacoli; // roba pub // riga-ok
    public LayerMask layerPersonaggio; // roba pub // riga-ok

    [Header("Sensore Acustico (Udito)")] // nota unity // riga-ok
    public float raggioUditoPassi = 8f; // roba pub // riga-ok

    [Header("Sistema di Allarme di Gruppo")] // nota unity // riga-ok
    public float raggioScattoAllarme = 15f; // roba pub // riga-ok
    private bool allarmeLanciato = false; // roba pub // riga-ok

    [Header("Statistiche e Combattimento (Corpo a Corpo / Pugno)")] // nota unity // riga-ok
    public float saluteMassima = 100f; // roba pub // riga-ok
    private float saluteCorrente; // roba pub // riga-ok
    public float dannoAttacco = 25f; // roba pub // riga-ok
    public float cadenzaAttacco = 1.3f; // roba pub // riga-ok
    private float timerProssimoAttacco = 0f; // roba pub // riga-ok

    [Header("Sincronizzazione Impatto Pugno")] // nota unity // riga-ok
    [Tooltip("Ritardo in secondi dall'avvio dell'animazione al momento esatto in cui il colpo/pugno completa l'estensione e impatta sul bersaglio.")] // nota unity // riga-ok
    [SerializeField] public float ritardoImpattoPugno = 0.45f; // setta // riga-ok
    [Tooltip("Raggio di portata entro cui il pugno infligge danno all'impatto.")] // nota unity // riga-ok
    [SerializeField] private float raggioImpattoPugno = 2.4f; // setta // riga-ok
    private Coroutine coroutineAttacco; // roba pub // riga-ok

    [Header("Audio 3D")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoPassi; // ok qua // riga-ok
    [SerializeField] private AudioClip suonoCorsa; // ok qua // riga-ok
    [SerializeField] private AudioClip suonoAllarme; // ok qua // riga-ok
    [SerializeField] private AudioClip suonoAttacco; // ok qua // riga-ok
    [SerializeField] private AudioClip suonoDanno; // ok qua // riga-ok
    [SerializeField] private AudioClip suonoMorte; // ok qua // riga-ok
    [Range(0f, 1f)] [SerializeField] private float volumeAudio = 0.9f; // setta // riga-ok
    [SerializeField] private float intervalloPassiCamminata = 0.55f; // setta // riga-ok
    [SerializeField] private float intervalloPassiCorsa = 0.35f; // setta // riga-ok

    [Header("Integrazione Animazioni")] // nota unity // riga-ok
    public Animator animatore; // roba pub // riga-ok
    public string parametroVelocita = "Speed"; // roba pub // riga-ok
    public string triggerAttacco = "Attack"; // roba pub // riga-ok
    public string triggerMorte = "Morte"; // roba pub // riga-ok

    [Header("Opzioni di Morte & Debug")] // nota unity // riga-ok
    public bool sparisciSubitoDopoMorte = true; // roba pub // riga-ok
    public float ritardoSparizione = 0.5f; // roba pub // riga-ok

    private NavMeshAgent agente; // roba pub // riga-ok
    private Transform playerTransform; // roba pub // riga-ok
    private muve_pg playerScript; // roba pub // riga-ok
    private IDamageable playerDamageable; // roba pub // riga-ok
    private AudioSource audioSource; // roba pub // riga-ok
    private AudioSource audioSourcePassi; // roba pub // riga-ok
    private float timerPassi = 0f; // roba pub // riga-ok

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
            audioSource.spatialBlend = 1.0f; // 3D // setta // riga-ok
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic; // setta // riga-ok
            audioSource.minDistance = 1.5f; // setta // riga-ok
            audioSource.maxDistance = 18.0f; // setta // riga-ok
            audioSource.dopplerLevel = 0f; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (audioSourcePassi == null) // se ok // riga-ok
        { // apre // riga-ok
            Transform childPassi = transform.Find("AudioPassiSource"); // setta // riga-ok
            // blocco: controlla se va
            if (childPassi != null) // se ok // riga-ok
            { // apre // riga-ok
                audioSourcePassi = childPassi.GetComponent<AudioSource>(); // setta // riga-ok
            } // chiude // riga-ok

            // blocco: controlla se va
            if (audioSourcePassi == null) // se ok // riga-ok
            { // apre // riga-ok
                GameObject goPassi = new GameObject("AudioPassiSource"); // setta // riga-ok
                goPassi.transform.SetParent(transform, false); // chiama // riga-ok
                audioSourcePassi = goPassi.AddComponent<AudioSource>(); // setta // riga-ok
            } // chiude // riga-ok

            audioSourcePassi.playOnAwake = false; // setta // riga-ok
            audioSourcePassi.spatialBlend = 1.0f; // 3D // setta // riga-ok
            audioSourcePassi.rolloffMode = AudioRolloffMode.Logarithmic; // setta // riga-ok
            audioSourcePassi.minDistance = 1.5f; // setta // riga-ok
            audioSourcePassi.maxDistance = 18.0f; // setta // riga-ok
            audioSourcePassi.dopplerLevel = 0f; // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void FermaAudioPassi() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (audioSourcePassi != null && audioSourcePassi.isPlaying) // se ok // riga-ok
        { // apre // riga-ok
            audioSourcePassi.Stop(); // chiama // riga-ok
        } // chiude // riga-ok
        timerPassi = 0f; // setta // riga-ok
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
            audioSource.pitch = Random.Range(0.95f, 1.05f); // setta // riga-ok
            audioSource.PlayOneShot(clip, volumeAudio * volumeMoltiplicatore); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    void Start() // chiama // riga-ok
    { // apre // riga-ok
        InizializzaAudioSource(); // chiama // riga-ok
        ApplicaTagUnity(); // chiama // riga-ok
        saluteCorrente = saluteMassima; // setta // riga-ok
        posizioneIniziale = transform.position; // setta // riga-ok

        // blocco: controlla se va
        if (distanzaArresto < 1.5f || distanzaArresto > 2.5f) // se ok // riga-ok
        { // apre // riga-ok
            distanzaArresto = 1.9f; // Calibrazione ottimale pugno corpo a corpo // setta // riga-ok
        } // chiude // riga-ok

        agente = GetComponent<NavMeshAgent>(); // setta // riga-ok
        // blocco: controlla se va
        if (agente != null) // se ok // riga-ok
        { // apre // riga-ok
            agente.stoppingDistance = 1.3f; // setta // riga-ok
            NavMeshHit hitMesh; // ok qua // riga-ok
            // Raggio 8 m: copre NPC posizionati poco sopra/sotto la NavMesh
            // blocco: controlla se va
            if (NavMesh.SamplePosition(transform.position, out hitMesh, 8.0f, NavMesh.AllAreas)) // se ok // riga-ok
            { // apre // riga-ok
                transform.position = hitMesh.position; // setta // riga-ok
                agente.Warp(hitMesh.position); // chiama // riga-ok
            } // chiude // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
            { // apre // riga-ok
                // Nessuna NavMesh entro 8 m: disabilita l'agente e usa il movimento
                // di fallback basato su Transform già presente in MuoviInRonda/InseguiEAttacca.
                Debug.LogWarning($"[NPC] {gameObject.name}: NavMesh non trovata entro 8 m. " + // logga // riga-ok
                                 "L'NPC userà il movimento diretto (senza pathfinding). " + // ok qua // riga-ok
                                 "Verifica la posizione nella scena o ribaka la NavMesh.", this); // chiama // riga-ok
                agente.enabled = false; // setta // riga-ok
            } // chiude // riga-ok
            // blocco: controlla se va
            if (agente.enabled) // se ok // riga-ok
            { // apre // riga-ok
                agente.updateRotation = true; // setta // riga-ok
                agente.stoppingDistance = 0.8f; // setta // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        TrovaRiferimentoPlayer(); // chiama // riga-ok

        // blocco: controlla se va
        if (eStatica) // se ok // riga-ok
        { // apre // riga-ok
            statoAttuale = StatoGuardia.Inattiva; // setta // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            statoAttuale = StatoGuardia.Ronda; // setta // riga-ok
            // blocco: controlla se va
            if (agente != null && agente.isOnNavMesh) // se ok // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (HaWaypointValidi()) // se ok // riga-ok
                { // apre // riga-ok
                    agente.isStopped = false; // setta // riga-ok
                    agente.speed = velocitaRonda; // setta // riga-ok
                    Transform wp = OttieniProssimoWaypointValido(); // setta // riga-ok
                    // blocco: controlla se va
                    if (wp != null) agente.SetDestination(wp.position); // se ok // riga-ok
                } // chiude // riga-ok
                // blocco: caso diverso
                else // se no // riga-ok
                { // apre // riga-ok
                    destinazioneRandomValida = false; // setta // riga-ok
                    inAttesaRandom = false; // setta // riga-ok
                    ScegliPuntoRandom(); // chiama // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (animatore == null) // se ok // riga-ok
        { // apre // riga-ok
            animatore = GetComponentInChildren<Animator>(); // setta // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (animatore != null) // se ok // riga-ok
        { // apre // riga-ok
            animatore.applyRootMotion = false; // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private bool HaWaypointValidi() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (waypointRonda == null || waypointRonda.Length == 0) return false; // se ok // riga-ok
        // blocco: gira piu volte
        for (int i = 0; i < waypointRonda.Length; i++) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (waypointRonda[i] != null) return true; // se ok // riga-ok
        } // chiude // riga-ok
        return false; // torna val // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private Transform OttieniProssimoWaypointValido() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (waypointRonda == null || waypointRonda.Length == 0) return null; // se ok // riga-ok
        // blocco: gira piu volte
        for (int i = 0; i < waypointRonda.Length; i++) // ciclo x // riga-ok
        { // apre // riga-ok
            int idx = (indiceWaypointAttuale + i) % waypointRonda.Length; // setta // riga-ok
            // blocco: controlla se va
            if (waypointRonda[idx] != null) // se ok // riga-ok
            { // apre // riga-ok
                indiceWaypointAttuale = idx; // setta // riga-ok
                return waypointRonda[idx]; // torna val // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
        return null; // torna val // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnEnable() // roba pub // riga-ok
    { // apre // riga-ok
        MissionManager.OnEstrazioneSbloccata += OnStatoEmergenzaCambiato; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnDisable() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (coroutineAttacco != null) // se ok // riga-ok
        { // apre // riga-ok
            StopCoroutine(coroutineAttacco); // corutina // riga-ok
            coroutineAttacco = null; // setta // riga-ok
        } // chiude // riga-ok
        FermaAudioPassi(); // chiama // riga-ok
        MissionManager.OnEstrazioneSbloccata -= OnStatoEmergenzaCambiato; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnStatoEmergenzaCambiato(bool emergenzaRisolta) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (emergenzaRisolta && disattivaOstilitAFineEmergenza) // se ok // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (spegniAFineEmergenza) // se ok // riga-ok
            { // apre // riga-ok
                StopAgente(); // chiama // riga-ok
                statoAttuale = StatoGuardia.Inattiva; // setta // riga-ok
            } // chiude // riga-ok
            // blocco: controlla se va
            else if (statoAttuale == StatoGuardia.Inseguimento || statoAttuale == StatoGuardia.Sospettosa) // se ok // riga-ok
            { // apre // riga-ok
                statoAttuale = eStatica ? StatoGuardia.Inattiva : StatoGuardia.Ronda; // setta // riga-ok
                // blocco: controlla se va
                if (agente != null && agente.isOnNavMesh) // se ok // riga-ok
                { // apre // riga-ok
                    agente.speed = velocitaRonda; // setta // riga-ok
                    agente.isStopped = false; // setta // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
            Debug.Log($"<color=green>[GUARDIA]</color> Emergenza risolta: {gameObject.name} non è più ostile."); // logga // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnValidate() // roba pub // riga-ok
    { // apre // riga-ok
        ApplicaTagUnity(); // chiama // riga-ok
    } // chiude // riga-ok

    void Update() // chiama // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (ModalUIState.IsModalOpen) // se ok // riga-ok
        { // apre // riga-ok
            FermaAudioPassi(); // chiama // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (statoAttuale == StatoGuardia.Morta) // se ok // riga-ok
        { // apre // riga-ok
            FermaAudioPassi(); // chiama // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (playerTransform == null || playerDamageable == null) // se ok // riga-ok
        { // apre // riga-ok
            TrovaRiferimentoPlayer(); // chiama // riga-ok
        } // chiude // riga-ok

        RilevaGiocatore(); // chiama // riga-ok
        EseguiComportamento(); // chiama // riga-ok
        AggiornaAnimazioni(); // chiama // riga-ok
        GestisciAudioPassi(); // chiama // riga-ok

        // blocco: controlla se va
        if (timerProssimoAttacco > 0) // se ok // riga-ok
        { // apre // riga-ok
            timerProssimoAttacco -= Time.deltaTime; // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void GestisciAudioPassi() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (statoAttuale == StatoGuardia.Morta || statoAttuale == StatoGuardia.Inattiva) // se ok // riga-ok
        { // apre // riga-ok
            FermaAudioPassi(); // chiama // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        bool staMuovendo = false; // setta // riga-ok
        // blocco: controlla se va
        if (agente != null && agente.isOnNavMesh) // se ok // riga-ok
        { // apre // riga-ok
            staMuovendo = !agente.isStopped && (agente.velocity.sqrMagnitude > 0.05f || agente.desiredVelocity.sqrMagnitude > 0.05f); // setta // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (staMuovendo) // se ok // riga-ok
        { // apre // riga-ok
            InizializzaAudioSource(); // chiama // riga-ok
            bool inCorsa = statoAttuale == StatoGuardia.Inseguimento; // setta // riga-ok
            AudioClip clipPasso = inCorsa ? (suonoCorsa ?? suonoPassi) : suonoPassi; // setta // riga-ok
            // blocco: controlla se va
            if (clipPasso != null && audioSourcePassi != null) // se ok // riga-ok
            { // apre // riga-ok
                float targetVolume = volumeAudio * (inCorsa ? 0.85f : 0.65f); // setta // riga-ok

                // blocco: controlla se va
                if (clipPasso.length > 0.8f) // se ok // riga-ok
                { // apre // riga-ok
                    audioSourcePassi.loop = true; // setta // riga-ok
                    audioSourcePassi.volume = targetVolume; // setta // riga-ok
                    audioSourcePassi.pitch = inCorsa ? 1.05f : 1.0f; // setta // riga-ok

                    // blocco: controlla se va
                    if (audioSourcePassi.clip != clipPasso) // se ok // riga-ok
                    { // apre // riga-ok
                        audioSourcePassi.clip = clipPasso; // setta // riga-ok
                        audioSourcePassi.Play(); // chiama // riga-ok
                    } // chiude // riga-ok
                    // blocco: controlla se va
                    else if (!audioSourcePassi.isPlaying) // se ok // riga-ok
                    { // apre // riga-ok
                        audioSourcePassi.Play(); // chiama // riga-ok
                    } // chiude // riga-ok
                } // chiude // riga-ok
                // blocco: caso diverso
                else // se no // riga-ok
                { // apre // riga-ok
                    audioSourcePassi.loop = false; // setta // riga-ok
                    timerPassi -= Time.deltaTime; // setta // riga-ok
                    // blocco: controlla se va
                    if (timerPassi <= 0f) // se ok // riga-ok
                    { // apre // riga-ok
                        audioSourcePassi.pitch = Random.Range(0.95f, 1.05f); // setta // riga-ok
                        audioSourcePassi.PlayOneShot(clipPasso, targetVolume); // chiama // riga-ok
                        timerPassi = inCorsa ? intervalloPassiCorsa : intervalloPassiCamminata; // setta // riga-ok
                    } // chiude // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
            { // apre // riga-ok
                FermaAudioPassi(); // chiama // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            FermaAudioPassi(); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void AggiornaAnimazioni() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (animatore == null) return; // se ok // riga-ok

        float valoreVelocita = 0f; // setta // riga-ok

        // blocco: controlla se va
        if (statoAttuale == StatoGuardia.Morta || statoAttuale == StatoGuardia.Inattiva || statoAttuale == StatoGuardia.Sospettosa) // se ok // riga-ok
        { // apre // riga-ok
            valoreVelocita = 0f; // setta // riga-ok
        } // chiude // riga-ok
        // blocco: controlla se va
        else if (statoAttuale == StatoGuardia.Inseguimento) // se ok // riga-ok
        { // apre // riga-ok
            bool staEseguendoPugno = animatore.GetCurrentAnimatorStateInfo(0).IsName("attaca") ||  // setta // riga-ok
                                    (animatore.IsInTransition(0) && animatore.GetNextAnimatorStateInfo(0).IsName("attaca")); // chiama // riga-ok

            // blocco: controlla se va
            if (staEseguendoPugno) // se ok // riga-ok
            { // apre // riga-ok
                valoreVelocita = 0f; // setta // riga-ok
            } // chiude // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
            { // apre // riga-ok
                bool staMuovendo = false; // setta // riga-ok
                // blocco: controlla se va
                if (agente != null && agente.isOnNavMesh) // se ok // riga-ok
                { // apre // riga-ok
                    staMuovendo = !agente.isStopped && (agente.velocity.sqrMagnitude > 0.04f || agente.desiredVelocity.sqrMagnitude > 0.04f); // setta // riga-ok
                } // chiude // riga-ok
                // blocco: caso diverso
                else // se no // riga-ok
                { // apre // riga-ok
                    staMuovendo = playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) > distanzaArresto; // setta // riga-ok
                } // chiude // riga-ok

                valoreVelocita = staMuovendo ? 3.0f : 0f; // setta // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // Ronda o RitornoAllaBase // se no // riga-ok
        { // apre // riga-ok
            // In ronda: se non in pausa e si muove, valore 1.2 attiva 'camina' (richiede Speed > 0.1 e < 2.5)
            bool staMuovendo = false; // setta // riga-ok
            // blocco: controlla se va
            if (agente != null && agente.isOnNavMesh) // se ok // riga-ok
            { // apre // riga-ok
                staMuovendo = !agente.isStopped && !inAttesaRandom && (agente.velocity.sqrMagnitude > 0.04f || agente.desiredVelocity.sqrMagnitude > 0.04f || agente.hasPath); // setta // riga-ok
            } // chiude // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
            { // apre // riga-ok
                staMuovendo = true; // setta // riga-ok
            } // chiude // riga-ok

            valoreVelocita = staMuovendo ? 1.2f : 0f; // setta // riga-ok
        } // chiude // riga-ok

        animatore.SetFloat(parametroVelocita, valoreVelocita); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void EseguiComportamento() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: scegli strada
        switch (statoAttuale) // scegli // riga-ok
        { // apre // riga-ok
            case StatoGuardia.Inattiva: // caso // riga-ok
                StopAgente(); // chiama // riga-ok
                break; // stop // riga-ok
            case StatoGuardia.Ronda: // caso // riga-ok
                MuoviInRonda(); // chiama // riga-ok
                break; // stop // riga-ok
            case StatoGuardia.Sospettosa: // caso // riga-ok
                StopAgente(); // chiama // riga-ok
                RotazioneFluida(playerTransform.position); // chiama // riga-ok
                break; // stop // riga-ok
            case StatoGuardia.Inseguimento: // caso // riga-ok
                InseguiEAttacca(); // chiama // riga-ok
                break; // stop // riga-ok
            case StatoGuardia.RitornoAllaBase: // caso // riga-ok
                EseguiRitornoAllaBase(); // chiama // riga-ok
                break; // stop // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void StopAgente() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (agente != null && agente.isOnNavMesh) // se ok // riga-ok
        { // apre // riga-ok
            agente.isStopped = true; // setta // riga-ok
            agente.velocity = Vector3.zero; // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void MuoviInRonda() // roba pub // riga-ok
    { // apre // riga-ok
        // ── MODALITÀ WAYPOINT FISSI ────────────────────────────────────────────
        // blocco: controlla se va
        if (HaWaypointValidi()) // se ok // riga-ok
        { // apre // riga-ok
            Transform target = waypointRonda[indiceWaypointAttuale]; // setta // riga-ok
            // blocco: controlla se va
            if (target == null) // se ok // riga-ok
            { // apre // riga-ok
                target = OttieniProssimoWaypointValido(); // setta // riga-ok
                // blocco: controlla se va
                if (target == null) return; // se ok // riga-ok
            } // chiude // riga-ok

            // blocco: controlla se va
            if (agente != null && agente.isOnNavMesh) // se ok // riga-ok
            { // apre // riga-ok
                agente.isStopped = false; // setta // riga-ok
                agente.speed = velocitaRonda; // setta // riga-ok

                Vector2 posAgenteXZ = new Vector2(transform.position.x, transform.position.z); // setta // riga-ok
                Vector2 posTargetXZ = new Vector2(target.position.x, target.position.z); // setta // riga-ok
                float distanzaXZ = Vector2.Distance(posAgenteXZ, posTargetXZ); // setta // riga-ok

                // blocco: controlla se va
                if (!agente.hasPath || Vector3.Distance(agente.destination, target.position) > 1.5f) // se ok // riga-ok
                { // apre // riga-ok
                    agente.SetDestination(target.position); // chiama // riga-ok
                } // chiude // riga-ok

                // blocco: controlla se va
                if (distanzaXZ <= distanzaArresto + 0.8f || (!agente.pathPending && agente.hasPath && agente.remainingDistance <= agente.stoppingDistance + 0.8f)) // se ok // riga-ok
                { // apre // riga-ok
                    indiceWaypointAttuale = (indiceWaypointAttuale + 1) % waypointRonda.Length; // setta // riga-ok
                    Transform nextTarget = OttieniProssimoWaypointValido(); // setta // riga-ok
                    // blocco: controlla se va
                    if (nextTarget != null) // se ok // riga-ok
                    { // apre // riga-ok
                        agente.SetDestination(nextTarget.position); // chiama // riga-ok
                    } // chiude // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
            { // apre // riga-ok
                // Fallback senza NavMesh
                Vector3 direzione = (target.position - transform.position).normalized; // setta // riga-ok
                direzione.y = 0; // setta // riga-ok
                Vector3 targetPos = target.position; // setta // riga-ok
                targetPos.y = transform.position.y; // setta // riga-ok
                transform.position = Vector3.MoveTowards(transform.position, targetPos, velocitaRonda * Time.deltaTime); // setta // riga-ok
                // blocco: controlla se va
                if (direzione != Vector3.zero) // se ok // riga-ok
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direzione), 5f * Time.deltaTime); // setta // riga-ok
                // blocco: controlla se va
                if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(target.position.x, target.position.z)) < 1.0f) // se ok // riga-ok
                { // apre // riga-ok
                    indiceWaypointAttuale = (indiceWaypointAttuale + 1) % waypointRonda.Length; // setta // riga-ok
                    OttieniProssimoWaypointValido(); // chiama // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        // ── MODALITÀ RANDOM SU NAVMESH ─────────────────────────────────────────
        // blocco: controlla se va
        if (agente == null || !agente.isOnNavMesh) // se ok // riga-ok
            return; // torna val // riga-ok

        // Pausa al punto raggiunto
        // blocco: controlla se va
        if (inAttesaRandom) // se ok // riga-ok
        { // apre // riga-ok
            agente.isStopped = true; // setta // riga-ok
            agente.velocity = Vector3.zero; // setta // riga-ok
            timerAttesaRandom -= Time.deltaTime; // setta // riga-ok
            // blocco: controlla se va
            if (timerAttesaRandom <= 0f) // se ok // riga-ok
            { // apre // riga-ok
                inAttesaRandom = false; // setta // riga-ok
                destinazioneRandomValida = false; // setta // riga-ok
                ScegliPuntoRandom(); // chiama // riga-ok
            } // chiude // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        // Scegli un nuovo punto casuale se necessario
        // blocco: controlla se va
        if (!destinazioneRandomValida) // se ok // riga-ok
        { // apre // riga-ok
            destinazioneRandomValida = ScegliPuntoRandom(); // setta // riga-ok
            // blocco: controlla se va
            if (!destinazioneRandomValida) // se ok // riga-ok
                return; // torna val // riga-ok
        } // chiude // riga-ok

        // Controlla se siamo arrivati
        // blocco: controlla se va
        if (!agente.pathPending && agente.hasPath && agente.remainingDistance <= agente.stoppingDistance + 0.4f) // se ok // riga-ok
        { // apre // riga-ok
            destinazioneRandomValida = false; // setta // riga-ok
            inAttesaRandom = true; // setta // riga-ok
            timerAttesaRandom = attesaTraPuntiRandom; // setta // riga-ok
            Debug.Log($"<color=cyan>[GUARDIA RANDOM]</color> {gameObject.name}: punto raggiunto. Pausa {attesaTraPuntiRandom}s."); // logga // riga-ok
        } // chiude // riga-ok
        // blocco: controlla se va
        else if (!agente.pathPending && !agente.hasPath) // se ok // riga-ok
        { // apre // riga-ok
            destinazioneRandomValida = false; // setta // riga-ok
            ScegliPuntoRandom(); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private bool ScegliPuntoRandom() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (agente == null || !agente.isOnNavMesh) return false; // se ok // riga-ok

        Vector3 centroRonda = (posizioneIniziale != Vector3.zero) ? posizioneIniziale : transform.position; // setta // riga-ok

        // blocco: gira piu volte
        for (int tentativi = 0; tentativi < 10; tentativi++) // ciclo x // riga-ok
        { // apre // riga-ok
            Vector2 offset2D = Random.insideUnitCircle * raggioRondaRandom; // setta // riga-ok
            Vector3 puntoCandidato = centroRonda + new Vector3(offset2D.x, 0f, offset2D.y); // setta // riga-ok

            // blocco: controlla se va
            if (NavMesh.SamplePosition(puntoCandidato, out NavMeshHit hit, raggioRondaRandom, NavMesh.AllAreas)) // se ok // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (Vector3.Distance(transform.position, hit.position) > 2.0f) // se ok // riga-ok
                { // apre // riga-ok
                    NavMeshPath path = new NavMeshPath(); // setta // riga-ok
                    // blocco: controlla se va
                    if (agente.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete) // se ok // riga-ok
                    { // apre // riga-ok
                        destinazioneRandom = hit.position; // setta // riga-ok
                        agente.isStopped = false; // setta // riga-ok
                        agente.speed = velocitaRonda; // setta // riga-ok
                        agente.SetPath(path); // chiama // riga-ok
                        destinazioneRandomValida = true; // setta // riga-ok
                        inAttesaRandom = false; // setta // riga-ok
                        Debug.Log($"<color=cyan>[GUARDIA RANDOM]</color> {gameObject.name}: nuovo punto → {destinazioneRandom}"); // logga // riga-ok
                        return true; // torna val // riga-ok
                    } // chiude // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
        destinazioneRandomValida = false; // setta // riga-ok
        return false; // torna val // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void InseguiEAttacca() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (playerTransform == null) return; // se ok // riga-ok

        float distanzaDalGiocatore = Vector3.Distance(transform.position, playerTransform.position); // setta // riga-ok
        float sogliaPugno = Mathf.Clamp(distanzaArresto, 1.6f, 2.2f); // setta // riga-ok

        bool staEseguendoPugno = animatore != null &&  // setta // riga-ok
            (animatore.GetCurrentAnimatorStateInfo(0).IsName("attaca") ||  // ok qua // riga-ok
             (animatore.IsInTransition(0) && animatore.GetNextAnimatorStateInfo(0).IsName("attaca"))); // chiama // riga-ok

        // blocco: controlla se va
        if (agente != null && agente.isOnNavMesh) // se ok // riga-ok
        { // apre // riga-ok
            agente.speed = velocitaInseguimento; // setta // riga-ok

            // blocco: controlla se va
            if (distanzaDalGiocatore <= sogliaPugno || staEseguendoPugno) // se ok // riga-ok
            { // apre // riga-ok
                agente.isStopped = true; // setta // riga-ok
                agente.velocity = Vector3.zero; // setta // riga-ok
            } // chiude // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
            { // apre // riga-ok
                agente.isStopped = false; // setta // riga-ok
                agente.SetDestination(playerTransform.position); // chiama // riga-ok
            } // chiude // riga-ok

            // blocco: controlla se va
            if (distanzaDalGiocatore < sogliaPugno + 3.0f) // se ok // riga-ok
            { // apre // riga-ok
                RotazioneFluida(playerTransform.position); // chiama // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (distanzaDalGiocatore > sogliaPugno && !staEseguendoPugno) // se ok // riga-ok
            { // apre // riga-ok
                Vector3 targetPos = playerTransform.position; // setta // riga-ok
                targetPos.y = transform.position.y; // setta // riga-ok
                transform.position = Vector3.MoveTowards(transform.position, targetPos, velocitaInseguimento * Time.deltaTime); // setta // riga-ok
            } // chiude // riga-ok
            RotazioneFluida(playerTransform.position); // chiama // riga-ok
        } // chiude // riga-ok

        // Sferra il pugno non appena raggiunge la portata di ingaggio corpo a corpo
        // blocco: controlla se va
        if (distanzaDalGiocatore <= sogliaPugno && timerProssimoAttacco <= 0 && !staEseguendoPugno) // se ok // riga-ok
        { // apre // riga-ok
            AttaccaPlayer(); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok
    
    // blocco: funzione fa cose
    private void EseguiRitornoAllaBase() // roba pub // riga-ok
    { // apre // riga-ok
        // In modalità random torna sempre alla posizioneIniziale;
        // in modalità waypoint torna al primo waypoint (comportamento originale).
        Vector3 destinazione = (waypointRonda != null && waypointRonda.Length > 0) // setta // riga-ok
            ? waypointRonda[0].position // ok qua // riga-ok
            : posizioneIniziale; // ok qua // riga-ok

        // blocco: controlla se va
        if (agente != null && agente.isOnNavMesh) // se ok // riga-ok
        { // apre // riga-ok
            agente.isStopped = false; // setta // riga-ok
            agente.speed = velocitaRonda; // setta // riga-ok
            agente.SetDestination(destinazione); // chiama // riga-ok

            // blocco: controlla se va
            if (!agente.pathPending && agente.remainingDistance <= agente.stoppingDistance + 0.5f) // se ok // riga-ok
            { // apre // riga-ok
                statoAttuale = eStatica ? StatoGuardia.Inattiva : StatoGuardia.Ronda; // setta // riga-ok
                indiceWaypointAttuale = 0; // setta // riga-ok
                destinazioneRandomValida = false; // forza nuovo punto random al riavvio ronda // setta // riga-ok
                Debug.Log("<color=green>[GUARDIA] Posizione di partenza raggiunta. Riprendo le direttive operative.</color>"); // logga // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            Vector3 direzione = (destinazione - transform.position).normalized; // setta // riga-ok
            direzione.y = 0; // setta // riga-ok
            Vector3 targetPos = destinazione; // setta // riga-ok
            targetPos.y = transform.position.y; // setta // riga-ok

            transform.position = Vector3.MoveTowards(transform.position, targetPos, velocitaRonda * Time.deltaTime); // setta // riga-ok

            // blocco: controlla se va
            if (direzione != Vector3.zero) // se ok // riga-ok
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direzione), 5f * Time.deltaTime); // setta // riga-ok

            // blocco: controlla se va
            if (Vector3.Distance(transform.position, targetPos) < 0.5f) // se ok // riga-ok
            { // apre // riga-ok
                statoAttuale = eStatica ? StatoGuardia.Inattiva : StatoGuardia.Ronda; // setta // riga-ok
                indiceWaypointAttuale = 0; // setta // riga-ok
                destinazioneRandomValida = false; // setta // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void TrovaRiferimentoPlayer(Transform specifico = null) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (specifico != null) // se ok // riga-ok
        { // apre // riga-ok
            playerTransform = specifico; // setta // riga-ok
            playerScript = specifico.GetComponent<muve_pg>() ?? specifico.GetComponentInParent<muve_pg>() ?? specifico.GetComponentInChildren<muve_pg>(); // setta // riga-ok
            playerDamageable = specifico.GetComponent<IDamageable>() ?? specifico.GetComponentInParent<IDamageable>() ?? specifico.GetComponentInChildren<IDamageable>(); // setta // riga-ok
            // blocco: controlla se va
            if (playerDamageable != null) return; // se ok // riga-ok
        } // chiude // riga-ok

        // 1. Ricerca tramite Tag Player
        GameObject playerObj = GameObject.FindGameObjectWithTag(SectorContainmentTags.Player); // setta // riga-ok
        // blocco: controlla se va
        if (playerObj != null) // se ok // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (playerTransform == null) playerTransform = playerObj.transform; // se ok // riga-ok
            // blocco: controlla se va
            if (playerScript == null) playerScript = playerObj.GetComponent<muve_pg>() ?? playerObj.GetComponentInParent<muve_pg>() ?? playerObj.GetComponentInChildren<muve_pg>(); // se ok // riga-ok
            // blocco: controlla se va
            if (playerDamageable == null) playerDamageable = playerObj.GetComponent<IDamageable>() ?? playerObj.GetComponentInParent<IDamageable>() ?? playerObj.GetComponentInChildren<IDamageable>(); // se ok // riga-ok
        } // chiude // riga-ok

        // 2. Fallback diretto tramite SalutePlayer nella scena
        // blocco: controlla se va
        if (playerDamageable == null) // se ok // riga-ok
        { // apre // riga-ok
            SalutePlayer salute = Object.FindAnyObjectByType<SalutePlayer>(); // setta // riga-ok
            // blocco: controlla se va
            if (salute != null) // se ok // riga-ok
            { // apre // riga-ok
                playerDamageable = salute; // setta // riga-ok
                // blocco: controlla se va
                if (playerTransform == null) playerTransform = salute.transform; // se ok // riga-ok
                // blocco: controlla se va
                if (playerScript == null) playerScript = salute.GetComponent<muve_pg>() ?? salute.GetComponentInParent<muve_pg>() ?? salute.GetComponentInChildren<muve_pg>(); // se ok // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // 3. Fallback tramite muve_pg
        // blocco: controlla se va
        if (playerTransform == null) // se ok // riga-ok
        { // apre // riga-ok
            muve_pg muve = Object.FindAnyObjectByType<muve_pg>(); // setta // riga-ok
            // blocco: controlla se va
            if (muve != null) // se ok // riga-ok
            { // apre // riga-ok
                playerTransform = muve.transform; // setta // riga-ok
                playerScript = muve; // setta // riga-ok
                // blocco: controlla se va
                if (playerDamageable == null) playerDamageable = muve.GetComponent<IDamageable>() ?? muve.GetComponentInParent<IDamageable>() ?? muve.GetComponentInChildren<IDamageable>(); // se ok // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void AttaccaPlayer() // roba pub // riga-ok
    { // apre // riga-ok
        timerProssimoAttacco = cadenzaAttacco; // setta // riga-ok

        // blocco: controlla se va
        if (animatore != null) // se ok // riga-ok
        { // apre // riga-ok
            animatore.ResetTrigger(triggerAttacco); // chiama // riga-ok
            animatore.SetTrigger(triggerAttacco); // chiama // riga-ok
        } // chiude // riga-ok

        RiproduciSuono(suonoAttacco); // chiama // riga-ok

        // blocco: controlla se va
        if (coroutineAttacco != null) // se ok // riga-ok
            StopCoroutine(coroutineAttacco); // corutina // riga-ok

        coroutineAttacco = StartCoroutine(EseguiImpattoPugno(ritardoImpattoPugno)); // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private System.Collections.IEnumerator EseguiImpattoPugno(float ritardo) // roba pub // riga-ok
    { // apre // riga-ok
        yield return new WaitForSeconds(ritardo); // aspetta // riga-ok

        // blocco: controlla se va
        if (statoAttuale == StatoGuardia.Morta) yield break; // se ok // riga-ok

        // blocco: controlla se va
        if (playerDamageable == null || playerTransform == null) // se ok // riga-ok
        { // apre // riga-ok
            TrovaRiferimentoPlayer(playerTransform); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (playerTransform != null && playerDamageable != null) // se ok // riga-ok
        { // apre // riga-ok
            float distanza = Vector3.Distance(transform.position, playerTransform.position); // setta // riga-ok
            // blocco: controlla se va
            if (distanza <= raggioImpattoPugno) // se ok // riga-ok
            { // apre // riga-ok
                Debug.Log($"<color=red>[GUARDIA] Impatto Pugno a segno! Infligge {dannoAttacco} HP al giocatore.</color>"); // logga // riga-ok
                playerDamageable.SubisciDanno(dannoAttacco); // chiama // riga-ok
            } // chiude // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
            { // apre // riga-ok
                Debug.Log("<color=yellow>[GUARDIA] Pugno a vuoto: bersaglio fuori portata all'impatto.</color>"); // logga // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            Debug.LogError("[SISTEMA COMBATTIMENTO] ATTENZIONE: La guardia ha sferrato il pugno, ma lo script della salute del giocatore non è stato trovato!"); // logga // riga-ok
        } // chiude // riga-ok

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
            if (hit.transform.root == transform.root || hit.collider.CompareTag(SectorContainmentTags.Enemy) || hit.collider.CompareTag(SectorContainmentTags.Drone)) // se ok // riga-ok
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

            // Muro/ostacolo che blocca la linea di vista
            return false; // torna val // riga-ok
        } // chiude // riga-ok

        return true; // torna val // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void RilevaGiocatore() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (playerTransform == null) // se ok // riga-ok
        { // apre // riga-ok
            TrovaRiferimentoPlayer(); // chiama // riga-ok
            // blocco: controlla se va
            if (playerTransform == null) return; // se ok // riga-ok
        } // chiude // riga-ok

        // Se l'emergenza è rientrata e le guardie sono state pacificate, non rilevano né attaccano
        // blocco: controlla se va
        if (disattivaOstilitAFineEmergenza && MissionManager.Instance != null && MissionManager.Instance.EstrazioneSbloccata) // se ok // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (spegniAFineEmergenza) // se ok // riga-ok
            { // apre // riga-ok
                StopAgente(); // chiama // riga-ok
                statoAttuale = StatoGuardia.Inattiva; // setta // riga-ok
            } // chiude // riga-ok
            // blocco: controlla se va
            else if (statoAttuale == StatoGuardia.Inseguimento || statoAttuale == StatoGuardia.Sospettosa) // se ok // riga-ok
            { // apre // riga-ok
                statoAttuale = eStatica ? StatoGuardia.Inattiva : StatoGuardia.Ronda; // setta // riga-ok
            } // chiude // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        Vector3 eyeOrigin = transform.position + Vector3.up * 1.5f; // setta // riga-ok
        Vector3 playerChest = playerTransform.position + Vector3.up * 1.0f; // setta // riga-ok
        float distanza = Vector3.Distance(transform.position, playerTransform.position); // setta // riga-ok

        // Se il giocatore è vicinissimo (entro 3.5 metri), ingaggia e si prepara al pugno immediatamente
        // blocco: controlla se va
        if (distanza <= 3.5f) // se ok // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (statoAttuale != StatoGuardia.Inseguimento) // se ok // riga-ok
            { // apre // riga-ok
                statoAttuale = StatoGuardia.Inseguimento; // setta // riga-ok
                timerProssimoAttacco = 0f; // Attacca subito non appena a portata di pugno // setta // riga-ok
                Debug.Log("<color=red>[GUARDIA] Bersaglio individuato a distanza ravvicinata! Inseguimento e pugno corpo a corpo.</color>"); // logga // riga-ok
                // blocco: controlla se va
                if (MissionManager.Instance != null) // se ok // riga-ok
                    MissionManager.Instance.RegistraRilevamento(gameObject.name); // chiama // riga-ok
                AllertaGuardieVicine(); // chiama // riga-ok
            } // chiude // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (playerScript != null) // se ok // riga-ok
        { // apre // riga-ok
            bool staCorrendo = UnityEngine.InputSystem.Keyboard.current != null &&  // setta // riga-ok
                               UnityEngine.InputSystem.Keyboard.current.leftShiftKey.isPressed &&  // ok qua // riga-ok
                               (UnityEngine.InputSystem.Keyboard.current.wKey.isPressed ||  // ok qua // riga-ok
                                UnityEngine.InputSystem.Keyboard.current.aKey.isPressed ||  // ok qua // riga-ok
                                UnityEngine.InputSystem.Keyboard.current.sKey.isPressed ||  // ok qua // riga-ok
                                UnityEngine.InputSystem.Keyboard.current.dKey.isPressed); // chiama // riga-ok

            // blocco: controlla se va
            if (staCorrendo && distanza <= raggioUditoPassi) // se ok // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (statoAttuale != StatoGuardia.Inseguimento) // se ok // riga-ok
                { // apre // riga-ok
                    statoAttuale = StatoGuardia.Sospettosa; // setta // riga-ok
                    Debug.Log("<color=yellow>[GUARDIA] Sente rumore di passi veloci alle spalle! Stato: Sospettosa.</color>"); // logga // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (distanza <= raggioVisione) // se ok // riga-ok
        { // apre // riga-ok
            Vector3 dirXZ = (playerTransform.position - transform.position); // setta // riga-ok
            dirXZ.y = 0; // setta // riga-ok
            float angoloFrontale = Vector3.Angle(transform.forward, dirXZ.normalized); // setta // riga-ok

            // blocco: controlla se va
            if (angoloFrontale < (angoloVisione / 2f) + 5f) // se ok // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (HaLineaDiVistaLibera(eyeOrigin, playerChest, distanza)) // se ok // riga-ok
                { // apre // riga-ok
                    // blocco: controlla se va
                    if (statoAttuale != StatoGuardia.Inseguimento) // se ok // riga-ok
                    { // apre // riga-ok
                        statoAttuale = StatoGuardia.Inseguimento; // setta // riga-ok
                        timerProssimoAttacco = 0f; // Attacca subito non appena a portata di pugno // setta // riga-ok
                        Debug.Log("<color=red>[GUARDIA] Bersaglio individuato! Inseguimento e pugno corpo a corpo.</color>"); // logga // riga-ok
                        // blocco: controlla se va
                        if (MissionManager.Instance != null) // se ok // riga-ok
                            MissionManager.Instance.RegistraRilevamento(gameObject.name); // chiama // riga-ok
                        AllertaGuardieVicine(); // chiama // riga-ok
                    } // chiude // riga-ok
                    return; // torna val // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
        
        // blocco: controlla se va
        if (distanza > raggioVisione + 6f && statoAttuale == StatoGuardia.Inseguimento) // se ok // riga-ok
        { // apre // riga-ok
            statoAttuale = StatoGuardia.RitornoAllaBase; // setta // riga-ok
            allarmeLanciato = false; // setta // riga-ok
            Debug.Log("<color=grey>[GUARDIA] Bersaglio perso. Rientro alla posizione di partenza in corso.</color>"); // logga // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void AllertaGuardieVicine() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (allarmeLanciato) return; // se ok // riga-ok

        allarmeLanciato = true; // setta // riga-ok
        RiproduciSuono(suonoAllarme); // chiama // riga-ok
        Debug.Log($"<color=orange>[ALLARME RADIO] Guardia in combattimento! Invio segnale alle unità entro {raggioScattoAllarme} metri!</color>"); // logga // riga-ok

        GuardiaNpc[] tutteLeGuardie = Object.FindObjectsByType<GuardiaNpc>(FindObjectsSortMode.None); // setta // riga-ok
        
        // blocco: gira piu volte
        foreach (GuardiaNpc guardia in tutteLeGuardie) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (guardia == this || guardia.statoAttuale == StatoGuardia.Morta) continue; // se ok // riga-ok

            float distanzaDallAllarme = Vector3.Distance(transform.position, guardia.transform.position); // setta // riga-ok
            // blocco: controlla se va
            if (distanzaDallAllarme <= raggioScattoAllarme) // se ok // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (guardia.statoAttuale != StatoGuardia.Inseguimento) // se ok // riga-ok
                { // apre // riga-ok
                    guardia.RiceviAllarmeRinforzi(playerTransform); // chiama // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void RiceviAllarmeRinforzi(Transform targetPlayer) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (statoAttuale == StatoGuardia.Morta) return; // se ok // riga-ok

        statoAttuale = StatoGuardia.Inseguimento; // setta // riga-ok
        TrovaRiferimentoPlayer(targetPlayer); // chiama // riga-ok
        allarmeLanciato = true; // setta // riga-ok
        RiproduciSuono(suonoAllarme); // chiama // riga-ok

        Debug.Log($"<color=red><b>[RINFORZI]</b> {gameObject.name} ha ricevuto l'allarme radio di combattimento! Corre in supporto!</color>"); // logga // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void SubisciDanno(float quantitaDanno) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (statoAttuale == StatoGuardia.Morta) return; // se ok // riga-ok

        bool colpoFurtivo = RilevaSeColpitoAlleSpalle(); // setta // riga-ok

        // blocco: controlla se va
        if (colpoFurtivo) // se ok // riga-ok
        { // apre // riga-ok
            saluteCorrente = 0; // setta // riga-ok
            MorteFurtiva(); // chiama // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            saluteCorrente -= quantitaDanno; // setta // riga-ok
            RiproduciSuono(suonoDanno); // chiama // riga-ok
            Debug.Log($"<color=orange>[GUARDIA] Colpito! Subito {quantitaDanno} HP di danno frontale. Salute rimanente: {saluteCorrente}</color>"); // logga // riga-ok
            
            // blocco: controlla se va
            if (statoAttuale != StatoGuardia.Inseguimento) // se ok // riga-ok
            { // apre // riga-ok
                statoAttuale = StatoGuardia.Inseguimento; // setta // riga-ok
                AllertaGuardieVicine(); // chiama // riga-ok
            } // chiude // riga-ok

            // blocco: controlla se va
            if (saluteCorrente <= 0) // se ok // riga-ok
            { // apre // riga-ok
                MorteStandard(); // chiama // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private bool RilevaSeColpitoAlleSpalle() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (playerTransform == null) return false; // se ok // riga-ok
        
        Vector3 direzioneDalPlayer = (transform.position - playerTransform.position).normalized; // setta // riga-ok
        float angoloImpattoAlleSpalle = Vector3.Angle(transform.forward, direzioneDalPlayer); // setta // riga-ok

        return angoloImpattoAlleSpalle < 60f; // torna val // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void MorteFurtiva() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (coroutineAttacco != null) // se ok // riga-ok
        { // apre // riga-ok
            StopCoroutine(coroutineAttacco); // corutina // riga-ok
            coroutineAttacco = null; // setta // riga-ok
        } // chiude // riga-ok
        statoAttuale = StatoGuardia.Morta; // setta // riga-ok
        Debug.Log("<color=green><b>[STEALTH SUCCESS]</b> Guardia eliminata sul colpo con un'azione furtiva silenziosa!</color>"); // logga // riga-ok
        EseguiDissolvenzaMorte(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void MorteStandard() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (coroutineAttacco != null) // se ok // riga-ok
        { // apre // riga-ok
            StopCoroutine(coroutineAttacco); // corutina // riga-ok
            coroutineAttacco = null; // setta // riga-ok
        } // chiude // riga-ok
        statoAttuale = StatoGuardia.Morta; // setta // riga-ok
        Debug.Log("<color=white>[GUARDIA] Eliminata in combattimento frontale.</color>"); // logga // riga-ok
        EseguiDissolvenzaMorte(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void EseguiDissolvenzaMorte() // roba pub // riga-ok
    { // apre // riga-ok
        FermaAudioPassi(); // chiama // riga-ok
        GetComponent<Collider>().enabled = false; // setta // riga-ok
        RiproduciSuono(suonoMorte); // chiama // riga-ok
        
        // blocco: controlla se va
        if (agente != null) // se ok // riga-ok
        { // apre // riga-ok
            agente.enabled = false; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (animatore != null) // se ok // riga-ok
        { // apre // riga-ok
            animatore.SetTrigger(triggerMorte); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (sparisciSubitoDopoMorte) // se ok // riga-ok
        { // apre // riga-ok
            Destroy(gameObject, ritardoSparizione); // elimina // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            Debug.Log("<color=cyan>[GUARDIA] La guardia è morta. Il cadavere rimane a terra poiché 'sparisciSubitoDopoMorte' è disattivato.</color>"); // logga // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void RotazioneFluida(Vector3 targetPos) // roba pub // riga-ok
    { // apre // riga-ok
        Vector3 direzioneSguardo = (targetPos - transform.position).normalized; // setta // riga-ok
        direzioneSguardo.y = 0; // setta // riga-ok
        
        // blocco: controlla se va
        if (direzioneSguardo != Vector3.zero) // se ok // riga-ok
        { // apre // riga-ok
            Quaternion rotTarget = Quaternion.LookRotation(direzioneSguardo); // setta // riga-ok
            transform.rotation = Quaternion.Slerp(transform.rotation, rotTarget, 8f * Time.deltaTime); // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnDrawGizmos() // roba pub // riga-ok
    { // apre // riga-ok
        Gizmos.color = Color.blue; // setta // riga-ok
        Gizmos.DrawWireSphere(transform.position, raggioUditoPassi); // chiama // riga-ok

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f); // setta // riga-ok
        Gizmos.DrawWireSphere(transform.position, raggioScattoAllarme); // chiama // riga-ok

        Gizmos.color = Color.red; // setta // riga-ok
        Gizmos.DrawWireSphere(transform.position, raggioVisione); // chiama // riga-ok
        
        Vector3 dirDestraFrontale = Quaternion.Euler(0, angoloVisione / 2, 0) * transform.forward; // setta // riga-ok
        Vector3 dirSinistraFrontale = Quaternion.Euler(0, -angoloVisione / 2, 0) * transform.forward; // setta // riga-ok
        
        Gizmos.color = Color.yellow; // setta // riga-ok
        Gizmos.DrawRay(transform.position, dirDestraFrontale * raggioVisione); // chiama // riga-ok
        Gizmos.DrawRay(transform.position, dirSinistraFrontale * raggioVisione); // chiama // riga-ok
        
        Vector3 dirDestraPosteriore = Quaternion.Euler(0, 180 - (angoloPuntoCiecoStealth / 2), 0) * transform.forward; // setta // riga-ok
        Vector3 dirSinistraPosteriore = Quaternion.Euler(0, 180 + (angoloPuntoCiecoStealth / 2), 0) * transform.forward; // setta // riga-ok
        
        Gizmos.color = Color.green; // setta // riga-ok
        Gizmos.DrawRay(transform.position, dirDestraPosteriore * 3f); // chiama // riga-ok
        Gizmos.DrawRay(transform.position, dirSinistraPosteriore * 3f); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void ApplicaTagUnity() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (applicaTagEnemyAutomatico) // se ok // riga-ok
            SectorContainmentTags.ApplyTag(gameObject, SectorContainmentTags.Enemy); // chiama // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
