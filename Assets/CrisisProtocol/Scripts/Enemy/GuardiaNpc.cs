// ============================================================================
// Crisis Protocol / Sector Containment - Nemici e minacce
// File: .\Assets\CrisisProtocol\Scripts\Enemy\GuardiaNpc.cs
// Responsabilita': definisce pattugliamento, inseguimento, attacco o comportamento di droni, guardie e bot ostili.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine;
using UnityEngine.AI;
using CrisisProtocol.UI;

[RequireComponent(typeof(NavMeshAgent))]
public class GuardiaNpc : MonoBehaviour, IDamageable
{
    public enum StatoGuardia { Inattiva, Ronda, Sospettosa, Inseguimento, RitornoAllaBase, Morta }

    [Header("Configurazione Comportamento")]
    [Tooltip("Se attivato, la guardia rimarrà ferma sul posto invece di seguire la ronda.")]
    [SerializeField] private bool applicaTagEnemyAutomatico = true;
    public bool eStatica = false;
    public StatoGuardia statoAttuale = StatoGuardia.Ronda;

    [Header("Parametri di Movimento (NavMesh)")]
    public Transform[] waypointRonda;
    public float velocitaRonda = 2f;
    public float velocitaInseguimento = 4.5f;
    [Tooltip("Distanza ravvicinata corpo a corpo per sferrare il pugno al giocatore.")]
    public float distanzaArresto = 1.9f;
    private int indiceWaypointAttuale = 0;

    [Header("Comportamento Post-Emergenza (Fine Crisi)")]
    [Tooltip("Se true, la guardia smette di essere ostile quando l'emergenza termina (focolai contenuti ed estrazione sbloccata).")]
    [SerializeField] private bool disattivaOstilitAFineEmergenza = true;
    [Tooltip("Se true, la guardia si ferma completamente/si spegne a fine emergenza. Se false, continua la ronda pacifica senza attaccare.")]
    [SerializeField] private bool spegniAFineEmergenza = false;

    [Header("Pattuglia Random (attiva se Waypoint Ronda è vuoto)")]
    [Tooltip("Raggio entro cui scegliere il prossimo punto casuale sulla NavMesh.")]
    public float raggioRondaRandom = 15f;
    [Tooltip("Secondi di pausa tra un punto casuale e il successivo.")]
    public float attesaTraPuntiRandom = 1.5f;
    private Vector3 destinazioneRandom;
    private float timerAttesaRandom = 0f;
    private bool inAttesaRandom = false;
    private bool destinazioneRandomValida = false;
    
    private Vector3 posizioneIniziale;

    [Header("Sensore Visivo (Vista)")]
    public float raggioVisione = 12f;
    [Range(0, 180)] public float angoloVisione = 90f;
    [Range(0, 180)] public float angoloPuntoCiecoStealth = 30f;
    public LayerMask layerOstacoli;
    public LayerMask layerPersonaggio;

    [Header("Sensore Acustico (Udito)")]
    public float raggioUditoPassi = 8f;

    [Header("Sistema di Allarme di Gruppo")]
    public float raggioScattoAllarme = 15f;
    private bool allarmeLanciato = false;

    [Header("Statistiche e Combattimento (Corpo a Corpo / Pugno)")]
    public float saluteMassima = 100f;
    private float saluteCorrente;
    public float dannoAttacco = 25f;
    public float cadenzaAttacco = 1.3f;
    private float timerProssimoAttacco = 0f;

    [Header("Sincronizzazione Impatto Pugno")]
    [Tooltip("Ritardo in secondi dall'avvio dell'animazione al momento esatto in cui il colpo/pugno completa l'estensione e impatta sul bersaglio.")]
    [SerializeField] public float ritardoImpattoPugno = 0.45f;
    [Tooltip("Raggio di portata entro cui il pugno infligge danno all'impatto.")]
    [SerializeField] private float raggioImpattoPugno = 2.4f;
    private Coroutine coroutineAttacco;

    [Header("Audio 3D")]
    [SerializeField] private AudioClip suonoPassi;
    [SerializeField] private AudioClip suonoCorsa;
    [SerializeField] private AudioClip suonoAllarme;
    [SerializeField] private AudioClip suonoAttacco;
    [SerializeField] private AudioClip suonoDanno;
    [SerializeField] private AudioClip suonoMorte;
    [Range(0f, 1f)] [SerializeField] private float volumeAudio = 0.9f;
    [SerializeField] private float intervalloPassiCamminata = 0.55f;
    [SerializeField] private float intervalloPassiCorsa = 0.35f;

    [Header("Integrazione Animazioni")]
    public Animator animatore;
    public string parametroVelocita = "Speed";
    public string triggerAttacco = "Attack";
    public string triggerMorte = "Morte";

    [Header("Opzioni di Morte & Debug")]
    public bool sparisciSubitoDopoMorte = true;
    public float ritardoSparizione = 0.5f;

    private NavMeshAgent agente;
    private Transform playerTransform;
    private muve_pg playerScript;
    private IDamageable playerDamageable;
    private AudioSource audioSource;
    private AudioSource audioSourcePassi;
    private float timerPassi = 0f;

    private void InizializzaAudioSource()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f; // 3D
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.minDistance = 1.5f;
            audioSource.maxDistance = 18.0f;
            audioSource.dopplerLevel = 0f;
        }

        if (audioSourcePassi == null)
        {
            Transform childPassi = transform.Find("AudioPassiSource");
            if (childPassi != null)
            {
                audioSourcePassi = childPassi.GetComponent<AudioSource>();
            }

            if (audioSourcePassi == null)
            {
                GameObject goPassi = new GameObject("AudioPassiSource");
                goPassi.transform.SetParent(transform, false);
                audioSourcePassi = goPassi.AddComponent<AudioSource>();
            }

            audioSourcePassi.playOnAwake = false;
            audioSourcePassi.spatialBlend = 1.0f; // 3D
            audioSourcePassi.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSourcePassi.minDistance = 1.5f;
            audioSourcePassi.maxDistance = 18.0f;
            audioSourcePassi.dopplerLevel = 0f;
        }
    }

    public void FermaAudioPassi()
    {
        if (audioSourcePassi != null && audioSourcePassi.isPlaying)
        {
            audioSourcePassi.Stop();
        }
        timerPassi = 0f;
    }

    public void RiproduciSuono(AudioClip clip, float volumeMoltiplicatore = 1.0f)
    {
        if (clip == null) return;
        InizializzaAudioSource();
        if (audioSource != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(clip, volumeAudio * volumeMoltiplicatore);
        }
    }

    void Start()
    {
        InizializzaAudioSource();
        ApplicaTagUnity();
        saluteCorrente = saluteMassima;
        posizioneIniziale = transform.position;

        if (distanzaArresto < 1.5f || distanzaArresto > 2.5f)
        {
            distanzaArresto = 1.9f; // Calibrazione ottimale pugno corpo a corpo
        }

        agente = GetComponent<NavMeshAgent>();
        if (agente != null)
        {
            agente.stoppingDistance = 1.3f;
            NavMeshHit hitMesh;
            // Raggio 8 m: copre NPC posizionati poco sopra/sotto la NavMesh
            if (NavMesh.SamplePosition(transform.position, out hitMesh, 8.0f, NavMesh.AllAreas))
            {
                transform.position = hitMesh.position;
                agente.Warp(hitMesh.position);
            }
            else
            {
                // Nessuna NavMesh entro 8 m: disabilita l'agente e usa il movimento
                // di fallback basato su Transform già presente in MuoviInRonda/InseguiEAttacca.
                Debug.LogWarning($"[NPC] {gameObject.name}: NavMesh non trovata entro 8 m. " +
                                 "L'NPC userà il movimento diretto (senza pathfinding). " +
                                 "Verifica la posizione nella scena o ribaka la NavMesh.", this);
                agente.enabled = false;
            }
            if (agente.enabled)
            {
                agente.updateRotation = true;
                agente.stoppingDistance = 0.8f;
            }
        }

        TrovaRiferimentoPlayer();

        if (eStatica)
        {
            statoAttuale = StatoGuardia.Inattiva;
        }
        else
        {
            statoAttuale = StatoGuardia.Ronda;
            if (agente != null && agente.isOnNavMesh)
            {
                if (HaWaypointValidi())
                {
                    agente.isStopped = false;
                    agente.speed = velocitaRonda;
                    Transform wp = OttieniProssimoWaypointValido();
                    if (wp != null) agente.SetDestination(wp.position);
                }
                else
                {
                    destinazioneRandomValida = false;
                    inAttesaRandom = false;
                    ScegliPuntoRandom();
                }
            }
        }

        if (animatore == null)
        {
            animatore = GetComponentInChildren<Animator>();
        }

        if (animatore != null)
        {
            animatore.applyRootMotion = false;
        }
    }

    private bool HaWaypointValidi()
    {
        if (waypointRonda == null || waypointRonda.Length == 0) return false;
        for (int i = 0; i < waypointRonda.Length; i++)
        {
            if (waypointRonda[i] != null) return true;
        }
        return false;
    }

    private Transform OttieniProssimoWaypointValido()
    {
        if (waypointRonda == null || waypointRonda.Length == 0) return null;
        for (int i = 0; i < waypointRonda.Length; i++)
        {
            int idx = (indiceWaypointAttuale + i) % waypointRonda.Length;
            if (waypointRonda[idx] != null)
            {
                indiceWaypointAttuale = idx;
                return waypointRonda[idx];
            }
        }
        return null;
    }

    private void OnEnable()
    {
        MissionManager.OnEstrazioneSbloccata += OnStatoEmergenzaCambiato;
    }

    private void OnDisable()
    {
        if (coroutineAttacco != null)
        {
            StopCoroutine(coroutineAttacco);
            coroutineAttacco = null;
        }
        FermaAudioPassi();
        MissionManager.OnEstrazioneSbloccata -= OnStatoEmergenzaCambiato;
    }

    private void OnStatoEmergenzaCambiato(bool emergenzaRisolta)
    {
        if (emergenzaRisolta && disattivaOstilitAFineEmergenza)
        {
            if (spegniAFineEmergenza)
            {
                StopAgente();
                statoAttuale = StatoGuardia.Inattiva;
            }
            else if (statoAttuale == StatoGuardia.Inseguimento || statoAttuale == StatoGuardia.Sospettosa)
            {
                statoAttuale = eStatica ? StatoGuardia.Inattiva : StatoGuardia.Ronda;
                if (agente != null && agente.isOnNavMesh)
                {
                    agente.speed = velocitaRonda;
                    agente.isStopped = false;
                }
            }
            Debug.Log($"<color=green>[GUARDIA]</color> Emergenza risolta: {gameObject.name} non è più ostile.");
        }
    }

    private void OnValidate()
    {
        ApplicaTagUnity();
    }

    void Update()
    {
        if (ModalUIState.IsModalOpen)
        {
            FermaAudioPassi();
            return;
        }

        if (statoAttuale == StatoGuardia.Morta)
        {
            FermaAudioPassi();
            return;
        }

        if (playerTransform == null || playerDamageable == null)
        {
            TrovaRiferimentoPlayer();
        }

        RilevaGiocatore();
        EseguiComportamento();
        AggiornaAnimazioni();
        GestisciAudioPassi();

        if (timerProssimoAttacco > 0)
        {
            timerProssimoAttacco -= Time.deltaTime;
        }
    }

    private void GestisciAudioPassi()
    {
        if (statoAttuale == StatoGuardia.Morta || statoAttuale == StatoGuardia.Inattiva)
        {
            FermaAudioPassi();
            return;
        }

        bool staMuovendo = false;
        if (agente != null && agente.isOnNavMesh)
        {
            staMuovendo = !agente.isStopped && (agente.velocity.sqrMagnitude > 0.05f || agente.desiredVelocity.sqrMagnitude > 0.05f);
        }

        if (staMuovendo)
        {
            InizializzaAudioSource();
            bool inCorsa = statoAttuale == StatoGuardia.Inseguimento;
            AudioClip clipPasso = inCorsa ? (suonoCorsa ?? suonoPassi) : suonoPassi;
            if (clipPasso != null && audioSourcePassi != null)
            {
                float targetVolume = volumeAudio * (inCorsa ? 0.85f : 0.65f);

                if (clipPasso.length > 0.8f)
                {
                    audioSourcePassi.loop = true;
                    audioSourcePassi.volume = targetVolume;
                    audioSourcePassi.pitch = inCorsa ? 1.05f : 1.0f;

                    if (audioSourcePassi.clip != clipPasso)
                    {
                        audioSourcePassi.clip = clipPasso;
                        audioSourcePassi.Play();
                    }
                    else if (!audioSourcePassi.isPlaying)
                    {
                        audioSourcePassi.Play();
                    }
                }
                else
                {
                    audioSourcePassi.loop = false;
                    timerPassi -= Time.deltaTime;
                    if (timerPassi <= 0f)
                    {
                        audioSourcePassi.pitch = Random.Range(0.95f, 1.05f);
                        audioSourcePassi.PlayOneShot(clipPasso, targetVolume);
                        timerPassi = inCorsa ? intervalloPassiCorsa : intervalloPassiCamminata;
                    }
                }
            }
            else
            {
                FermaAudioPassi();
            }
        }
        else
        {
            FermaAudioPassi();
        }
    }

    private void AggiornaAnimazioni()
    {
        if (animatore == null) return;

        float valoreVelocita = 0f;

        if (statoAttuale == StatoGuardia.Morta || statoAttuale == StatoGuardia.Inattiva || statoAttuale == StatoGuardia.Sospettosa)
        {
            valoreVelocita = 0f;
        }
        else if (statoAttuale == StatoGuardia.Inseguimento)
        {
            bool staEseguendoPugno = animatore.GetCurrentAnimatorStateInfo(0).IsName("attaca") || 
                                    (animatore.IsInTransition(0) && animatore.GetNextAnimatorStateInfo(0).IsName("attaca"));

            if (staEseguendoPugno)
            {
                valoreVelocita = 0f;
            }
            else
            {
                bool staMuovendo = false;
                if (agente != null && agente.isOnNavMesh)
                {
                    staMuovendo = !agente.isStopped && (agente.velocity.sqrMagnitude > 0.04f || agente.desiredVelocity.sqrMagnitude > 0.04f);
                }
                else
                {
                    staMuovendo = playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) > distanzaArresto;
                }

                valoreVelocita = staMuovendo ? 3.0f : 0f;
            }
        }
        else // Ronda o RitornoAllaBase
        {
            // In ronda: se non in pausa e si muove, valore 1.2 attiva 'camina' (richiede Speed > 0.1 e < 2.5)
            bool staMuovendo = false;
            if (agente != null && agente.isOnNavMesh)
            {
                staMuovendo = !agente.isStopped && !inAttesaRandom && (agente.velocity.sqrMagnitude > 0.04f || agente.desiredVelocity.sqrMagnitude > 0.04f || agente.hasPath);
            }
            else
            {
                staMuovendo = true;
            }

            valoreVelocita = staMuovendo ? 1.2f : 0f;
        }

        animatore.SetFloat(parametroVelocita, valoreVelocita);
    }

    private void EseguiComportamento()
    {
        switch (statoAttuale)
        {
            case StatoGuardia.Inattiva:
                StopAgente();
                break;
            case StatoGuardia.Ronda:
                MuoviInRonda();
                break;
            case StatoGuardia.Sospettosa:
                StopAgente();
                RotazioneFluida(playerTransform.position);
                break;
            case StatoGuardia.Inseguimento:
                InseguiEAttacca();
                break;
            case StatoGuardia.RitornoAllaBase:
                EseguiRitornoAllaBase();
                break;
        }
    }

    private void StopAgente()
    {
        if (agente != null && agente.isOnNavMesh)
        {
            agente.isStopped = true;
            agente.velocity = Vector3.zero;
        }
    }

    private void MuoviInRonda()
    {
        // ── MODALITÀ WAYPOINT FISSI ────────────────────────────────────────────
        if (HaWaypointValidi())
        {
            Transform target = waypointRonda[indiceWaypointAttuale];
            if (target == null)
            {
                target = OttieniProssimoWaypointValido();
                if (target == null) return;
            }

            if (agente != null && agente.isOnNavMesh)
            {
                agente.isStopped = false;
                agente.speed = velocitaRonda;

                Vector2 posAgenteXZ = new Vector2(transform.position.x, transform.position.z);
                Vector2 posTargetXZ = new Vector2(target.position.x, target.position.z);
                float distanzaXZ = Vector2.Distance(posAgenteXZ, posTargetXZ);

                if (!agente.hasPath || Vector3.Distance(agente.destination, target.position) > 1.5f)
                {
                    agente.SetDestination(target.position);
                }

                if (distanzaXZ <= distanzaArresto + 0.8f || (!agente.pathPending && agente.hasPath && agente.remainingDistance <= agente.stoppingDistance + 0.8f))
                {
                    indiceWaypointAttuale = (indiceWaypointAttuale + 1) % waypointRonda.Length;
                    Transform nextTarget = OttieniProssimoWaypointValido();
                    if (nextTarget != null)
                    {
                        agente.SetDestination(nextTarget.position);
                    }
                }
            }
            else
            {
                // Fallback senza NavMesh
                Vector3 direzione = (target.position - transform.position).normalized;
                direzione.y = 0;
                Vector3 targetPos = target.position;
                targetPos.y = transform.position.y;
                transform.position = Vector3.MoveTowards(transform.position, targetPos, velocitaRonda * Time.deltaTime);
                if (direzione != Vector3.zero)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direzione), 5f * Time.deltaTime);
                if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(target.position.x, target.position.z)) < 1.0f)
                {
                    indiceWaypointAttuale = (indiceWaypointAttuale + 1) % waypointRonda.Length;
                    OttieniProssimoWaypointValido();
                }
            }
            return;
        }

        // ── MODALITÀ RANDOM SU NAVMESH ─────────────────────────────────────────
        if (agente == null || !agente.isOnNavMesh)
            return;

        // Pausa al punto raggiunto
        if (inAttesaRandom)
        {
            agente.isStopped = true;
            agente.velocity = Vector3.zero;
            timerAttesaRandom -= Time.deltaTime;
            if (timerAttesaRandom <= 0f)
            {
                inAttesaRandom = false;
                destinazioneRandomValida = false;
                ScegliPuntoRandom();
            }
            return;
        }

        // Scegli un nuovo punto casuale se necessario
        if (!destinazioneRandomValida)
        {
            destinazioneRandomValida = ScegliPuntoRandom();
            if (!destinazioneRandomValida)
                return;
        }

        // Controlla se siamo arrivati
        if (!agente.pathPending && agente.hasPath && agente.remainingDistance <= agente.stoppingDistance + 0.4f)
        {
            destinazioneRandomValida = false;
            inAttesaRandom = true;
            timerAttesaRandom = attesaTraPuntiRandom;
            Debug.Log($"<color=cyan>[GUARDIA RANDOM]</color> {gameObject.name}: punto raggiunto. Pausa {attesaTraPuntiRandom}s.");
        }
        else if (!agente.pathPending && !agente.hasPath)
        {
            destinazioneRandomValida = false;
            ScegliPuntoRandom();
        }
    }

    private bool ScegliPuntoRandom()
    {
        if (agente == null || !agente.isOnNavMesh) return false;

        Vector3 centroRonda = (posizioneIniziale != Vector3.zero) ? posizioneIniziale : transform.position;

        for (int tentativi = 0; tentativi < 10; tentativi++)
        {
            Vector2 offset2D = Random.insideUnitCircle * raggioRondaRandom;
            Vector3 puntoCandidato = centroRonda + new Vector3(offset2D.x, 0f, offset2D.y);

            if (NavMesh.SamplePosition(puntoCandidato, out NavMeshHit hit, raggioRondaRandom, NavMesh.AllAreas))
            {
                if (Vector3.Distance(transform.position, hit.position) > 2.0f)
                {
                    NavMeshPath path = new NavMeshPath();
                    if (agente.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                    {
                        destinazioneRandom = hit.position;
                        agente.isStopped = false;
                        agente.speed = velocitaRonda;
                        agente.SetPath(path);
                        destinazioneRandomValida = true;
                        inAttesaRandom = false;
                        Debug.Log($"<color=cyan>[GUARDIA RANDOM]</color> {gameObject.name}: nuovo punto → {destinazioneRandom}");
                        return true;
                    }
                }
            }
        }
        destinazioneRandomValida = false;
        return false;
    }

    private void InseguiEAttacca()
    {
        if (playerTransform == null) return;

        float distanzaDalGiocatore = Vector3.Distance(transform.position, playerTransform.position);
        float sogliaPugno = Mathf.Clamp(distanzaArresto, 1.6f, 2.2f);

        bool staEseguendoPugno = animatore != null && 
            (animatore.GetCurrentAnimatorStateInfo(0).IsName("attaca") || 
             (animatore.IsInTransition(0) && animatore.GetNextAnimatorStateInfo(0).IsName("attaca")));

        if (agente != null && agente.isOnNavMesh)
        {
            agente.speed = velocitaInseguimento;

            if (distanzaDalGiocatore <= sogliaPugno || staEseguendoPugno)
            {
                agente.isStopped = true;
                agente.velocity = Vector3.zero;
            }
            else
            {
                agente.isStopped = false;
                agente.SetDestination(playerTransform.position);
            }

            if (distanzaDalGiocatore < sogliaPugno + 3.0f)
            {
                RotazioneFluida(playerTransform.position);
            }
        }
        else
        {
            if (distanzaDalGiocatore > sogliaPugno && !staEseguendoPugno)
            {
                Vector3 targetPos = playerTransform.position;
                targetPos.y = transform.position.y;
                transform.position = Vector3.MoveTowards(transform.position, targetPos, velocitaInseguimento * Time.deltaTime);
            }
            RotazioneFluida(playerTransform.position);
        }

        // Sferra il pugno non appena raggiunge la portata di ingaggio corpo a corpo
        if (distanzaDalGiocatore <= sogliaPugno && timerProssimoAttacco <= 0 && !staEseguendoPugno)
        {
            AttaccaPlayer();
        }
    }
    
    private void EseguiRitornoAllaBase()
    {
        // In modalità random torna sempre alla posizioneIniziale;
        // in modalità waypoint torna al primo waypoint (comportamento originale).
        Vector3 destinazione = (waypointRonda != null && waypointRonda.Length > 0)
            ? waypointRonda[0].position
            : posizioneIniziale;

        if (agente != null && agente.isOnNavMesh)
        {
            agente.isStopped = false;
            agente.speed = velocitaRonda;
            agente.SetDestination(destinazione);

            if (!agente.pathPending && agente.remainingDistance <= agente.stoppingDistance + 0.5f)
            {
                statoAttuale = eStatica ? StatoGuardia.Inattiva : StatoGuardia.Ronda;
                indiceWaypointAttuale = 0;
                destinazioneRandomValida = false; // forza nuovo punto random al riavvio ronda
                Debug.Log("<color=green>[GUARDIA] Posizione di partenza raggiunta. Riprendo le direttive operative.</color>");
            }
        }
        else
        {
            Vector3 direzione = (destinazione - transform.position).normalized;
            direzione.y = 0;
            Vector3 targetPos = destinazione;
            targetPos.y = transform.position.y;

            transform.position = Vector3.MoveTowards(transform.position, targetPos, velocitaRonda * Time.deltaTime);

            if (direzione != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direzione), 5f * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPos) < 0.5f)
            {
                statoAttuale = eStatica ? StatoGuardia.Inattiva : StatoGuardia.Ronda;
                indiceWaypointAttuale = 0;
                destinazioneRandomValida = false;
            }
        }
    }

    private void TrovaRiferimentoPlayer(Transform specifico = null)
    {
        if (specifico != null)
        {
            playerTransform = specifico;
            playerScript = specifico.GetComponent<muve_pg>() ?? specifico.GetComponentInParent<muve_pg>() ?? specifico.GetComponentInChildren<muve_pg>();
            playerDamageable = specifico.GetComponent<IDamageable>() ?? specifico.GetComponentInParent<IDamageable>() ?? specifico.GetComponentInChildren<IDamageable>();
            if (playerDamageable != null) return;
        }

        // 1. Ricerca tramite Tag Player
        GameObject playerObj = GameObject.FindGameObjectWithTag(SectorContainmentTags.Player);
        if (playerObj != null)
        {
            if (playerTransform == null) playerTransform = playerObj.transform;
            if (playerScript == null) playerScript = playerObj.GetComponent<muve_pg>() ?? playerObj.GetComponentInParent<muve_pg>() ?? playerObj.GetComponentInChildren<muve_pg>();
            if (playerDamageable == null) playerDamageable = playerObj.GetComponent<IDamageable>() ?? playerObj.GetComponentInParent<IDamageable>() ?? playerObj.GetComponentInChildren<IDamageable>();
        }

        // 2. Fallback diretto tramite SalutePlayer nella scena
        if (playerDamageable == null)
        {
            SalutePlayer salute = Object.FindAnyObjectByType<SalutePlayer>();
            if (salute != null)
            {
                playerDamageable = salute;
                if (playerTransform == null) playerTransform = salute.transform;
                if (playerScript == null) playerScript = salute.GetComponent<muve_pg>() ?? salute.GetComponentInParent<muve_pg>() ?? salute.GetComponentInChildren<muve_pg>();
            }
        }

        // 3. Fallback tramite muve_pg
        if (playerTransform == null)
        {
            muve_pg muve = Object.FindAnyObjectByType<muve_pg>();
            if (muve != null)
            {
                playerTransform = muve.transform;
                playerScript = muve;
                if (playerDamageable == null) playerDamageable = muve.GetComponent<IDamageable>() ?? muve.GetComponentInParent<IDamageable>() ?? muve.GetComponentInChildren<IDamageable>();
            }
        }
    }

    private void AttaccaPlayer()
    {
        timerProssimoAttacco = cadenzaAttacco;

        if (animatore != null)
        {
            animatore.ResetTrigger(triggerAttacco);
            animatore.SetTrigger(triggerAttacco);
        }

        RiproduciSuono(suonoAttacco);

        if (coroutineAttacco != null)
            StopCoroutine(coroutineAttacco);

        coroutineAttacco = StartCoroutine(EseguiImpattoPugno(ritardoImpattoPugno));
    }

    private System.Collections.IEnumerator EseguiImpattoPugno(float ritardo)
    {
        yield return new WaitForSeconds(ritardo);

        if (statoAttuale == StatoGuardia.Morta) yield break;

        if (playerDamageable == null || playerTransform == null)
        {
            TrovaRiferimentoPlayer(playerTransform);
        }

        if (playerTransform != null && playerDamageable != null)
        {
            float distanza = Vector3.Distance(transform.position, playerTransform.position);
            if (distanza <= raggioImpattoPugno)
            {
                Debug.Log($"<color=red>[GUARDIA] Impatto Pugno a segno! Infligge {dannoAttacco} HP al giocatore.</color>");
                playerDamageable.SubisciDanno(dannoAttacco);
            }
            else
            {
                Debug.Log("<color=yellow>[GUARDIA] Pugno a vuoto: bersaglio fuori portata all'impatto.</color>");
            }
        }
        else
        {
            Debug.LogError("[SISTEMA COMBATTIMENTO] ATTENZIONE: La guardia ha sferrato il pugno, ma lo script della salute del giocatore non è stato trovato!");
        }

        coroutineAttacco = null;
    }

    private bool HaLineaDiVistaLibera(Vector3 eyeOrigin, Vector3 playerChest, float maxDistance)
    {
        Vector3 direction = (playerChest - eyeOrigin);
        float distance = direction.magnitude;
        if (distance > maxDistance || distance < 0.01f) return distance <= maxDistance;
        direction.Normalize();

        RaycastHit[] hits = Physics.RaycastAll(eyeOrigin, direction, distance, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.root == transform.root || hit.collider.CompareTag(SectorContainmentTags.Enemy) || hit.collider.CompareTag(SectorContainmentTags.Drone))
                continue;

            if (hit.transform.root == playerTransform.root || hit.collider.CompareTag(SectorContainmentTags.Player))
                return true;

            if (hit.collider.isTrigger)
                continue;

            if (hit.distance >= distance - 0.3f)
                return true;

            // Muro/ostacolo che blocca la linea di vista
            return false;
        }

        return true;
    }

    private void RilevaGiocatore()
    {
        if (playerTransform == null)
        {
            TrovaRiferimentoPlayer();
            if (playerTransform == null) return;
        }

        // Se l'emergenza è rientrata e le guardie sono state pacificate, non rilevano né attaccano
        if (disattivaOstilitAFineEmergenza && MissionManager.Instance != null && MissionManager.Instance.EstrazioneSbloccata)
        {
            if (spegniAFineEmergenza)
            {
                StopAgente();
                statoAttuale = StatoGuardia.Inattiva;
            }
            else if (statoAttuale == StatoGuardia.Inseguimento || statoAttuale == StatoGuardia.Sospettosa)
            {
                statoAttuale = eStatica ? StatoGuardia.Inattiva : StatoGuardia.Ronda;
            }
            return;
        }

        Vector3 eyeOrigin = transform.position + Vector3.up * 1.5f;
        Vector3 playerChest = playerTransform.position + Vector3.up * 1.0f;
        float distanza = Vector3.Distance(transform.position, playerTransform.position);

        // Se il giocatore è vicinissimo (entro 3.5 metri), ingaggia e si prepara al pugno immediatamente
        if (distanza <= 3.5f)
        {
            if (statoAttuale != StatoGuardia.Inseguimento)
            {
                statoAttuale = StatoGuardia.Inseguimento;
                timerProssimoAttacco = 0f; // Attacca subito non appena a portata di pugno
                Debug.Log("<color=red>[GUARDIA] Bersaglio individuato a distanza ravvicinata! Inseguimento e pugno corpo a corpo.</color>");
                if (MissionManager.Instance != null)
                    MissionManager.Instance.RegistraRilevamento(gameObject.name);
                AllertaGuardieVicine();
            }
            return;
        }

        if (playerScript != null)
        {
            bool staCorrendo = UnityEngine.InputSystem.Keyboard.current != null && 
                               UnityEngine.InputSystem.Keyboard.current.leftShiftKey.isPressed && 
                               (UnityEngine.InputSystem.Keyboard.current.wKey.isPressed || 
                                UnityEngine.InputSystem.Keyboard.current.aKey.isPressed || 
                                UnityEngine.InputSystem.Keyboard.current.sKey.isPressed || 
                                UnityEngine.InputSystem.Keyboard.current.dKey.isPressed);

            if (staCorrendo && distanza <= raggioUditoPassi)
            {
                if (statoAttuale != StatoGuardia.Inseguimento)
                {
                    statoAttuale = StatoGuardia.Sospettosa;
                    Debug.Log("<color=yellow>[GUARDIA] Sente rumore di passi veloci alle spalle! Stato: Sospettosa.</color>");
                }
            }
        }

        if (distanza <= raggioVisione)
        {
            Vector3 dirXZ = (playerTransform.position - transform.position);
            dirXZ.y = 0;
            float angoloFrontale = Vector3.Angle(transform.forward, dirXZ.normalized);

            if (angoloFrontale < (angoloVisione / 2f) + 5f)
            {
                if (HaLineaDiVistaLibera(eyeOrigin, playerChest, distanza))
                {
                    if (statoAttuale != StatoGuardia.Inseguimento)
                    {
                        statoAttuale = StatoGuardia.Inseguimento;
                        timerProssimoAttacco = 0f; // Attacca subito non appena a portata di pugno
                        Debug.Log("<color=red>[GUARDIA] Bersaglio individuato! Inseguimento e pugno corpo a corpo.</color>");
                        if (MissionManager.Instance != null)
                            MissionManager.Instance.RegistraRilevamento(gameObject.name);
                        AllertaGuardieVicine();
                    }
                    return;
                }
            }
        }
        
        if (distanza > raggioVisione + 6f && statoAttuale == StatoGuardia.Inseguimento)
        {
            statoAttuale = StatoGuardia.RitornoAllaBase;
            allarmeLanciato = false;
            Debug.Log("<color=grey>[GUARDIA] Bersaglio perso. Rientro alla posizione di partenza in corso.</color>");
        }
    }

    private void AllertaGuardieVicine()
    {
        if (allarmeLanciato) return;

        allarmeLanciato = true;
        RiproduciSuono(suonoAllarme);
        Debug.Log($"<color=orange>[ALLARME RADIO] Guardia in combattimento! Invio segnale alle unità entro {raggioScattoAllarme} metri!</color>");

        GuardiaNpc[] tutteLeGuardie = Object.FindObjectsByType<GuardiaNpc>(FindObjectsSortMode.None);
        
        foreach (GuardiaNpc guardia in tutteLeGuardie)
        {
            if (guardia == this || guardia.statoAttuale == StatoGuardia.Morta) continue;

            float distanzaDallAllarme = Vector3.Distance(transform.position, guardia.transform.position);
            if (distanzaDallAllarme <= raggioScattoAllarme)
            {
                if (guardia.statoAttuale != StatoGuardia.Inseguimento)
                {
                    guardia.RiceviAllarmeRinforzi(playerTransform);
                }
            }
        }
    }

    public void RiceviAllarmeRinforzi(Transform targetPlayer)
    {
        if (statoAttuale == StatoGuardia.Morta) return;

        statoAttuale = StatoGuardia.Inseguimento;
        TrovaRiferimentoPlayer(targetPlayer);
        allarmeLanciato = true;
        RiproduciSuono(suonoAllarme);

        Debug.Log($"<color=red><b>[RINFORZI]</b> {gameObject.name} ha ricevuto l'allarme radio di combattimento! Corre in supporto!</color>");
    }

    public void SubisciDanno(float quantitaDanno)
    {
        if (statoAttuale == StatoGuardia.Morta) return;

        bool colpoFurtivo = RilevaSeColpitoAlleSpalle();

        if (colpoFurtivo)
        {
            saluteCorrente = 0;
            MorteFurtiva();
        }
        else
        {
            saluteCorrente -= quantitaDanno;
            RiproduciSuono(suonoDanno);
            Debug.Log($"<color=orange>[GUARDIA] Colpito! Subito {quantitaDanno} HP di danno frontale. Salute rimanente: {saluteCorrente}</color>");
            
            if (statoAttuale != StatoGuardia.Inseguimento)
            {
                statoAttuale = StatoGuardia.Inseguimento;
                AllertaGuardieVicine();
            }

            if (saluteCorrente <= 0)
            {
                MorteStandard();
            }
        }
    }

    private bool RilevaSeColpitoAlleSpalle()
    {
        if (playerTransform == null) return false;
        
        Vector3 direzioneDalPlayer = (transform.position - playerTransform.position).normalized;
        float angoloImpattoAlleSpalle = Vector3.Angle(transform.forward, direzioneDalPlayer);

        return angoloImpattoAlleSpalle < 60f;
    }

    private void MorteFurtiva()
    {
        if (coroutineAttacco != null)
        {
            StopCoroutine(coroutineAttacco);
            coroutineAttacco = null;
        }
        statoAttuale = StatoGuardia.Morta;
        Debug.Log("<color=green><b>[STEALTH SUCCESS]</b> Guardia eliminata sul colpo con un'azione furtiva silenziosa!</color>");
        EseguiDissolvenzaMorte();
    }

    private void MorteStandard()
    {
        if (coroutineAttacco != null)
        {
            StopCoroutine(coroutineAttacco);
            coroutineAttacco = null;
        }
        statoAttuale = StatoGuardia.Morta;
        Debug.Log("<color=white>[GUARDIA] Eliminata in combattimento frontale.</color>");
        EseguiDissolvenzaMorte();
    }

    private void EseguiDissolvenzaMorte()
    {
        FermaAudioPassi();
        GetComponent<Collider>().enabled = false;
        RiproduciSuono(suonoMorte);
        
        if (agente != null)
        {
            agente.enabled = false;
        }

        if (animatore != null)
        {
            animatore.SetTrigger(triggerMorte);
        }

        if (sparisciSubitoDopoMorte)
        {
            Destroy(gameObject, ritardoSparizione);
        }
        else
        {
            Debug.Log("<color=cyan>[GUARDIA] La guardia è morta. Il cadavere rimane a terra poiché 'sparisciSubitoDopoMorte' è disattivato.</color>");
        }
    }

    private void RotazioneFluida(Vector3 targetPos)
    {
        Vector3 direzioneSguardo = (targetPos - transform.position).normalized;
        direzioneSguardo.y = 0;
        
        if (direzioneSguardo != Vector3.zero)
        {
            Quaternion rotTarget = Quaternion.LookRotation(direzioneSguardo);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotTarget, 8f * Time.deltaTime);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, raggioUditoPassi);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, raggioScattoAllarme);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, raggioVisione);
        
        Vector3 dirDestraFrontale = Quaternion.Euler(0, angoloVisione / 2, 0) * transform.forward;
        Vector3 dirSinistraFrontale = Quaternion.Euler(0, -angoloVisione / 2, 0) * transform.forward;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, dirDestraFrontale * raggioVisione);
        Gizmos.DrawRay(transform.position, dirSinistraFrontale * raggioVisione);
        
        Vector3 dirDestraPosteriore = Quaternion.Euler(0, 180 - (angoloPuntoCiecoStealth / 2), 0) * transform.forward;
        Vector3 dirSinistraPosteriore = Quaternion.Euler(0, 180 + (angoloPuntoCiecoStealth / 2), 0) * transform.forward;
        
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, dirDestraPosteriore * 3f);
        Gizmos.DrawRay(transform.position, dirSinistraPosteriore * 3f);
    }

    private void ApplicaTagUnity()
    {
        if (applicaTagEnemyAutomatico)
            SectorContainmentTags.ApplyTag(gameObject, SectorContainmentTags.Enemy);
    }
}
