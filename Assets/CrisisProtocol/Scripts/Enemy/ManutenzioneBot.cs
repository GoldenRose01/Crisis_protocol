// ============================================================================
// Crisis Protocol / Sector Containment - Nemici e minacce
// File: .\Assets\CrisisProtocol\Scripts\Enemy\ManutenzioneBot.cs
// Responsabilita': definisce pattugliamento, inseguimento, attacco o comportamento di droni, guardie e bot ostili.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine;
using UnityEngine.AI;
using CrisisProtocol.UI;

[RequireComponent(typeof(NavMeshAgent))]
public class ManutenzioneBot : MonoBehaviour, IDamageable
{
    public enum StatoIA { RicercaAttiva, Inseguimento, AttaccoMischia, Morto }

    [Header("Stato e Navigazione")]
    public StatoIA statoAttuale = StatoIA.RicercaAttiva;
    [Tooltip("Velocità calma di ronda/camminata (consigliato: 1.0 - 1.5).")]
    [SerializeField] [Range(0.5f, 3f)] private float velocitaRicerca = 1.2f;
    [Tooltip("Velocità di inseguimento controllata (consigliato: 1.8 - 2.5).")]
    [SerializeField] [Range(1f, 6f)] private float velocitaInseguimento = 2.2f;
    [Tooltip("Accelerazione dell'agente: valori bassi (1.5 - 2.5) evitano scatti e partenze a razzo.")]
    [SerializeField] [Range(0.5f, 5f)] private float accelerazione = 2.0f;
    [Tooltip("Velocità di rotazione in gradi al secondo.")]
    [SerializeField] [Range(60f, 240f)] private float velocitaRotazione = 120f;

    [Header("Combattimento Corpo a Corpo (Pugni Devastanti)")]
    [Tooltip("Distanza ravvicinata per sferrare il pugno al giocatore.")]
    [SerializeField] private float distanzaAttacco = 1.9f;
    [Tooltip("Raggio di portata entro cui il pugno infligge danno al momento dell'impatto.")]
    [SerializeField] private float raggioImpattoPugno = 2.4f;
    [Tooltip("Tempo di caricamento del colpo prima dell'impatto (1.0 secondo esatto).")]
    [SerializeField] private float ritardoImpattoPugno = 1.0f;
    [Tooltip("Tempo di attesa / cooldown tra un pugno e il successivo.")]
    [SerializeField] private float cadenzaAttacco = 2.2f;
    [Tooltip("Se true, il pugno infligge una percentuale fissa della salute totale del giocatore.")]
    [SerializeField] private bool usaDannoPercentuale = true;
    [Tooltip("Percentuale del danno totale (0.50 = 50% della salute massima del giocatore).")]
    [Range(0.05f, 1.0f)] [SerializeField] private float percentualeDanno = 0.50f;
    [Tooltip("Danno fisso alternativo applicato qualora non sia possibile calcolare la percentuale.")]
    [SerializeField] private float dannoAttaccoFisso = 50f;

    // Campi per retrocompatibilità con scene ed editor script esistenti (SceneDoctor / SceneAudioPopulator)
    [SerializeField] [HideInInspector] private float distanzaOttimaleTiro = 1.9f;
    [SerializeField] [HideInInspector] private float dannoArma = 50f;
    [SerializeField] [HideInInspector] private float cadenzaDiFuoco = 2.2f;
    [SerializeField] [HideInInspector] private Transform puntoDiFuoco;

    [Header("Comportamento Post-Emergenza (Fine Crisi)")]
    [Tooltip("Se true, il bot entra in modalità manutenzione pacifica (non attacca, non insegue) quando l'emergenza finisce.")]
    [SerializeField] private bool pacificaAFineEmergenza = true;

    [Tooltip("Se true, il bot si spegne/ferma completamente sul posto a fine emergenza.")]
    [SerializeField] private bool spegniAFineEmergenza = false;

    [Header("Vagabondaggio Casuale (Roaming)")]
    [Tooltip("Raggio massimo entro cui scegliere il prossimo punto di ronda locale.")]
    [SerializeField] private float raggioPattugliamento = 8f;
    [SerializeField] private float tempoPausaMin = 2f;
    [SerializeField] private float tempoPausaMax = 4f;
    private bool inPausa = false;
    private float timerPausa = 0f;
    private float timerMemoriaInseguimento = 0f;
    private Vector3 posizioneIniziale;
    private bool haDestinazioneAttiva = false;
    private Vector3 destinazioneCorrente;
    private float timerDestinazione = 0f;

    [Header("Sensori e Rilevamento (Visione)")]
    [SerializeField] private float raggioVisione = 14f;
    [Range(0, 360)] [SerializeField] private float angoloVisione = 90f;
    [SerializeField] private float raggioRilevamentoRavvicinato = 2.5f;
    [SerializeField] private LayerMask layerOstacoli;
    [SerializeField] private Transform puntoOcchi;

    [Header("Audio 3D")]
    [Tooltip("Suono continuo del servomotore/cingoli durante il movimento.")]
    [SerializeField] private AudioClip suonoMovimentoLoop;
    [Tooltip("Suono di avvistamento bersaglio / allarme robotico.")]
    [SerializeField] private AudioClip suonoAvvistamento;
    [Tooltip("Suono di sferrata / caricamento del pugno.")]
    [SerializeField] private AudioClip suonoAttacco;
    [Tooltip("Suono di impatto pugno a segno.")]
    [SerializeField] private AudioClip suonoImpattoPugno;
    [Tooltip("Suono di sparo / attacco (retrocompatibilità).")]
    [SerializeField] private AudioClip suonoSparo;
    [Tooltip("Suono di impatto subito / sfiato guasto.")]
    [SerializeField] private AudioClip suonoDanno;
    [Tooltip("Suono di disattivazione / morte robotica.")]
    [SerializeField] private AudioClip suonoMorte;
    [Range(0f, 1f)] [SerializeField] private float volumeAudio = 0.85f;

    [Header("Statistiche Vitali")]
    [SerializeField] private float salute = 100f;
    [Tooltip("Tempo in secondi da attendere prima di rimuovere il corpo dal gioco dopo la morte.")]
    [SerializeField] private float tempoDistruzioneCorpo = 3.0f;

    [Header("Integrazione Animatore")]
    [SerializeField] private string triggerMorte = "Die";
    [SerializeField] private string triggerAttacco = "Attack";
    [SerializeField] private string triggerSparo = "Shoot";

    private NavMeshAgent agente;
    private Animator anim;
    private Transform playerTransform;
    private AudioSource audioSource;
    private AudioSource audioMovimentoSource;

    private Coroutine coroutineAttacco;
    private float timerProssimoAttacco = 0f;
    private bool staEseguendoPugno = false;

    private int speedHash;
    private int morteTriggerHash;
    private int sparoTriggerHash;
    private int attaccoTriggerHash;
    private bool isMorto = false;

    private float timerAggiornamentoPercorso = 0f;

    private bool AgentePronto => agente != null && agente.enabled && agente.isOnNavMesh;

    private void InizializzaAudio()
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
            audioSource.maxDistance = 16.0f;
            audioSource.dopplerLevel = 0f;
        }

        if (audioMovimentoSource == null)
        {
            Transform tMove = transform.Find("AudioMovimentoBot");
            if (tMove != null)
            {
                audioMovimentoSource = tMove.GetComponent<AudioSource>();
            }
            else
            {
                GameObject goMove = new GameObject("AudioMovimentoBot");
                goMove.transform.SetParent(transform, false);
                audioMovimentoSource = goMove.AddComponent<AudioSource>();
            }

            audioMovimentoSource.playOnAwake = false;
            audioMovimentoSource.spatialBlend = 1.0f;
            audioMovimentoSource.loop = true;
            audioMovimentoSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioMovimentoSource.minDistance = 1.5f;
            audioMovimentoSource.maxDistance = 14.0f;
            audioMovimentoSource.dopplerLevel = 0f;
            audioMovimentoSource.volume = volumeAudio * 0.7f;
            if (suonoMovimentoLoop != null)
            {
                audioMovimentoSource.clip = suonoMovimentoLoop;
            }
        }
    }

    public void RiproduciSuono(AudioClip clip, float volumeMoltiplicatore = 1.0f)
    {
        if (clip == null) return;
        InizializzaAudio();
        if (audioSource != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(clip, volumeAudio * volumeMoltiplicatore);
        }
    }

    [ContextMenu("Ripristina Velocità e Parametri Calmi")]
    public void ResetValoriPredefiniti()
    {
        velocitaRicerca = 1.2f;
        velocitaInseguimento = 2.2f;
        accelerazione = 2.0f;
        velocitaRotazione = 120f;
        raggioPattugliamento = 8f;
        distanzaAttacco = 1.9f;
        distanzaOttimaleTiro = 1.9f;
        raggioImpattoPugno = 2.4f;
        ritardoImpattoPugno = 1.0f;
        cadenzaAttacco = 2.2f;
        usaDannoPercentuale = true;
        percentualeDanno = 0.50f;
        dannoAttaccoFisso = 50f;
        dannoArma = 50f;
        cadenzaDiFuoco = 2.2f;
        raggioVisione = 14f;
        tempoPausaMin = 2f;
        tempoPausaMax = 4f;

        if (agente == null) agente = GetComponent<NavMeshAgent>();
        if (agente != null)
        {
            agente.speed = velocitaRicerca;
            agente.acceleration = accelerazione;
            agente.angularSpeed = velocitaRotazione;
            agente.stoppingDistance = 0.8f;
            agente.autoBraking = true;
            agente.radius = 0.35f;
            agente.height = 1.8f;
            agente.autoTraverseOffMeshLink = false;
            agente.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        }

        CentraModelliFigli();
    }

    void Awake()
    {
        CentraModelliFigli();
    }

    public void CentraModelliFigli()
    {
        foreach (Transform child in transform)
        {
            if (child.localPosition != Vector3.zero)
            {
                child.localPosition = Vector3.zero;
            }
        }

        Rigidbody[] allRbs = GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody r in allRbs)
        {
            if (r != null)
            {
                r.isKinematic = true;
                r.useGravity = false;
            }
        }
    }

    void Start()
    {
        CentraModelliFigli();
        InizializzaAudio();

        SectorContainmentTags.ApplyTag(gameObject, SectorContainmentTags.MaintenanceBot);

        agente = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        if (anim == null)
            anim = GetComponentInChildren<Animator>();

        if (anim != null)
        {
            anim.applyRootMotion = false;
        }

        Animator[] allAnimators = GetComponentsInChildren<Animator>();
        foreach (Animator a in allAnimators)
        {
            if (a != null)
                a.applyRootMotion = false;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (agente != null)
        {
            agente.speed = velocitaRicerca;
            agente.acceleration = accelerazione;
            agente.angularSpeed = velocitaRotazione;
            agente.stoppingDistance = 0.8f;
            agente.autoBraking = true;
            agente.radius = 0.35f;
            agente.height = 1.8f;
            agente.autoTraverseOffMeshLink = false;
            agente.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        }

        CapsuleCollider cap = GetComponent<CapsuleCollider>();
        if (cap != null && agente != null)
        {
            cap.radius = agente.radius * 0.85f;
            cap.height = agente.height;
            cap.center = new Vector3(0f, agente.height * 0.5f, 0f);
        }

        speedHash = Animator.StringToHash("Speed");
        morteTriggerHash = Animator.StringToHash(triggerMorte);
        sparoTriggerHash = Animator.StringToHash(triggerSparo);
        attaccoTriggerHash = Animator.StringToHash(triggerAttacco);

        posizioneIniziale = transform.position;
        haDestinazioneAttiva = false;
        inPausa = false;
        staEseguendoPugno = false;

        TrovaPlayer();

        if (puntoOcchi == null) puntoOcchi = transform;
        if (puntoDiFuoco == null) puntoDiFuoco = transform;

        if (agente != null)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 10.0f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                agente.Warp(hit.position);
                agente.enabled = true;
                agente.isStopped = false;
            }
            else
            {
                Debug.LogWarning($"[BOT] {gameObject.name}: NavMesh non trovata entro 10m. Verifica il posizionamento.", this);
            }
        }

        if (AgentePronto)
        {
            ImpostaNuovaDestinazioneCasuale();
        }
    }

    private void TrovaPlayer()
    {
        if (playerTransform != null) return;

        GameObject playerObj = GameObject.FindGameObjectWithTag(SectorContainmentTags.Player);
        if (playerObj == null)
        {
            playerObj = GameObject.FindGameObjectWithTag("Player");
        }
        if (playerObj == null)
        {
            muve_pg playerMovement = Object.FindFirstObjectByType<muve_pg>();
            if (playerMovement != null)
                playerObj = playerMovement.gameObject;
        }

        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    void Update()
    {
        if (ModalUIState.IsModalOpen)
            return;

        if (isMorto) return;

        if (playerTransform == null)
        {
            TrovaPlayer();
        }

        if (AgentePronto)
        {
            agente.acceleration = accelerazione;
            agente.angularSpeed = velocitaRotazione;
        }

        GestisciAudioMovimento();

        bool emergenzaFinita = MissionManager.Instance != null && MissionManager.Instance.EstrazioneSbloccata;

        if (emergenzaFinita && pacificaAFineEmergenza)
        {
            if (spegniAFineEmergenza)
            {
                if (AgentePronto)
                {
                    agente.isStopped = true;
                    agente.velocity = Vector3.zero;
                }
                if (anim != null && anim.runtimeAnimatorController != null) anim.SetFloat(speedHash, 0f);
                return;
            }

            if (statoAttuale == StatoIA.Inseguimento || statoAttuale == StatoIA.AttaccoMischia)
            {
                statoAttuale = StatoIA.RicercaAttiva;
                haDestinazioneAttiva = false;
                inPausa = false;
                staEseguendoPugno = false;
                if (AgentePronto)
                {
                    agente.isStopped = false;
                    agente.speed = velocitaRicerca;
                    agente.stoppingDistance = 0.5f;
                }
                ImpostaNuovaDestinazioneCasuale();
            }

            EseguiRondaCasuale();

            if (anim != null && anim.runtimeAnimatorController != null && AgentePronto)
            {
                float speedVal = 0f;
                if (!inPausa && (agente.velocity.sqrMagnitude > 0.04f || agente.desiredVelocity.sqrMagnitude > 0.04f || agente.hasPath))
                {
                    speedVal = (agente.velocity.magnitude > 0.1f) ? agente.velocity.magnitude : velocitaRicerca;
                }
                anim.SetFloat(speedHash, speedVal, 0.15f, Time.deltaTime);
            }
            return;
        }

        timerProssimoAttacco -= Time.deltaTime;
        timerAggiornamentoPercorso += Time.deltaTime;

        bool bersaglioRilevato = RilevaBersaglio(out float distanzaDalPlayer);

        GestisciMacchinaAStati(bersaglioRilevato, distanzaDalPlayer);

        if (anim != null && anim.runtimeAnimatorController != null && AgentePronto)
        {
            float speedVal = 0f;
            if (!inPausa && !staEseguendoPugno && (agente.velocity.sqrMagnitude > 0.04f || agente.desiredVelocity.sqrMagnitude > 0.04f || (statoAttuale != StatoIA.AttaccoMischia && agente.hasPath)))
            {
                speedVal = (agente.velocity.magnitude > 0.1f) ? agente.velocity.magnitude : ((statoAttuale == StatoIA.Inseguimento) ? velocitaInseguimento : velocitaRicerca);
            }
            anim.SetFloat(speedHash, speedVal, 0.15f, Time.deltaTime);
        }
    }

    private void GestisciAudioMovimento()
    {
        if (audioMovimentoSource == null || suonoMovimentoLoop == null) return;
        if (audioMovimentoSource.clip == null) audioMovimentoSource.clip = suonoMovimentoLoop;

        bool staMuovendo = AgentePronto && !agente.isStopped && !inPausa && !staEseguendoPugno && (agente.velocity.sqrMagnitude > 0.04f || agente.desiredVelocity.sqrMagnitude > 0.04f);

        if (staMuovendo && !isMorto)
        {
            if (!audioMovimentoSource.isPlaying)
            {
                audioMovimentoSource.Play();
            }
        }
        else
        {
            if (audioMovimentoSource.isPlaying)
            {
                audioMovimentoSource.Pause();
            }
        }
    }

    private void GestisciMacchinaAStati(bool bersaglioRilevato, float distanza)
    {
        switch (statoAttuale)
        {
            case StatoIA.RicercaAttiva:
                if (bersaglioRilevato)
                {
                    statoAttuale = StatoIA.Inseguimento;
                    inPausa = false;
                    haDestinazioneAttiva = false;
                    timerMemoriaInseguimento = 2.5f;
                    RiproduciSuono(suonoAvvistamento);
                    if (AgentePronto)
                    {
                        agente.isStopped = false;
                        agente.speed = velocitaInseguimento;
                        agente.stoppingDistance = 0.8f;
                    }
                    Debug.Log("<color=red>[BOT MANUTENZIONE] Bersaglio rilevato! Inizio avvicinamento per attacco corpo a corpo con pugni.</color>");
                }
                else
                {
                    EseguiRondaCasuale();
                }
                break;

            case StatoIA.Inseguimento:
                if (bersaglioRilevato)
                {
                    timerMemoriaInseguimento = 2.5f;
                }
                else
                {
                    timerMemoriaInseguimento -= Time.deltaTime;
                }

                if (!bersaglioRilevato && timerMemoriaInseguimento <= 0f)
                {
                    statoAttuale = StatoIA.RicercaAttiva;
                    inPausa = false;
                    haDestinazioneAttiva = false;
                    if (AgentePronto)
                    {
                        agente.isStopped = false;
                        agente.speed = velocitaRicerca;
                        agente.stoppingDistance = 0.5f;
                    }
                    ImpostaNuovaDestinazioneCasuale();
                }
                else if (bersaglioRilevato && distanza <= distanzaAttacco)
                {
                    statoAttuale = StatoIA.AttaccoMischia;
                    if (AgentePronto)
                    {
                        agente.isStopped = true;
                        agente.velocity = Vector3.zero;
                    }
                    EseguiRoutineDiMischia(distanza);
                }
                else
                {
                    if (AgentePronto)
                    {
                        agente.isStopped = false;
                        agente.speed = velocitaInseguimento;
                        agente.stoppingDistance = 0.8f;
                        agente.SetDestination(playerTransform.position);
                    }
                    else if (playerTransform != null)
                    {
                        Vector3 targetPos = playerTransform.position;
                        targetPos.y = transform.position.y;
                        transform.position = Vector3.MoveTowards(transform.position, targetPos, velocitaInseguimento * Time.deltaTime);
                        Vector3 dir = (playerTransform.position - transform.position).normalized;
                        dir.y = 0;
                        if (dir != Vector3.zero)
                            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 6f * Time.deltaTime);
                    }
                }
                break;

            case StatoIA.AttaccoMischia:
                if (!staEseguendoPugno && (!bersaglioRilevato || distanza > distanzaAttacco + 1.2f))
                {
                    statoAttuale = StatoIA.Inseguimento;
                    timerMemoriaInseguimento = 2.5f;
                    if (AgentePronto)
                    {
                        agente.isStopped = false;
                        agente.speed = velocitaInseguimento;
                    }
                }
                else
                {
                    EseguiRoutineDiMischia(distanza);
                }
                break;
        }
    }

    private void EseguiRoutineDiMischia(float distanzaAttuale)
    {
        if (AgentePronto)
        {
            agente.isStopped = true;
            agente.velocity = Vector3.zero;
        }

        if (playerTransform != null)
        {
            Vector3 direzioneMira = (playerTransform.position - transform.position).normalized;
            direzioneMira.y = 0;
            if (direzioneMira.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direzioneMira), 8f * Time.deltaTime);
            }
        }

        if (timerProssimoAttacco <= 0f && !staEseguendoPugno)
        {
            AvviaAttaccoPugno();
        }
    }

    private void AvviaAttaccoPugno()
    {
        staEseguendoPugno = true;
        timerProssimoAttacco = cadenzaAttacco;

        if (anim != null)
        {
            if (!string.IsNullOrEmpty(triggerAttacco))
            {
                anim.ResetTrigger(triggerAttacco);
                anim.SetTrigger(triggerAttacco);
            }
            if (!string.IsNullOrEmpty(triggerSparo))
            {
                anim.ResetTrigger(triggerSparo);
                anim.SetTrigger(triggerSparo);
            }
            anim.ResetTrigger("Attack");
            anim.SetTrigger("Attack");
            anim.ResetTrigger("attaca");
            anim.SetTrigger("attaca");
        }

        AudioClip clipDaRiprodurre = suonoAttacco != null ? suonoAttacco : (suonoSparo != null ? suonoSparo : null);
        RiproduciSuono(clipDaRiprodurre);

        if (coroutineAttacco != null)
        {
            StopCoroutine(coroutineAttacco);
        }

        coroutineAttacco = StartCoroutine(EseguiImpattoPugno(ritardoImpattoPugno));
    }

    private System.Collections.IEnumerator EseguiImpattoPugno(float ritardo)
    {
        Debug.Log($"<color=orange>[BOT MANUTENZIONE]</color> <b>Caricamento pugno pesante in corso... (impatto tra {ritardo:F1}s)</b>");

        yield return new WaitForSeconds(ritardo);

        if (isMorto)
        {
            staEseguendoPugno = false;
            coroutineAttacco = null;
            yield break;
        }

        if (playerTransform == null)
        {
            TrovaPlayer();
        }

        if (playerTransform != null)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);

            if (dist <= raggioImpattoPugno)
            {
                SalutePlayer vitaPlayer = playerTransform.GetComponent<SalutePlayer>() ?? 
                                         playerTransform.GetComponentInParent<SalutePlayer>() ?? 
                                         playerTransform.GetComponentInChildren<SalutePlayer>();

                float dannoInflitto = dannoAttaccoFisso;

                if (vitaPlayer != null)
                {
                    dannoInflitto = usaDannoPercentuale ? (vitaPlayer.puntiVitaMassimi * percentualeDanno) : dannoAttaccoFisso;
                    vitaPlayer.SubisciDanno(dannoInflitto);
                    Debug.Log($"<color=red><b>[BOT MANUTENZIONE] PUGNO DEVASTANTE A SEGNO!</b></color> Inflitti <b>{dannoInflitto:F0} HP</b> (50% salute totale) a {playerTransform.name}!");
                }
                else
                {
                    IDamageable damageable = playerTransform.GetComponent<IDamageable>() ?? 
                                             playerTransform.GetComponentInParent<IDamageable>() ?? 
                                             playerTransform.GetComponentInChildren<IDamageable>();
                    if (damageable != null)
                    {
                        damageable.SubisciDanno(dannoInflitto);
                        Debug.Log($"<color=red><b>[BOT MANUTENZIONE] PUGNO A SEGNO!</b></color> Inflitti {dannoInflitto:F0} HP!");
                    }
                }

                if (suonoImpattoPugno != null)
                {
                    RiproduciSuono(suonoImpattoPugno);
                }
                else if (suonoDanno != null)
                {
                    RiproduciSuono(suonoDanno);
                }
            }
            else
            {
                Debug.Log($"<color=yellow>[BOT MANUTENZIONE] Pugno a vuoto!</color> Il giocatore si trova a {dist:F2}m (fuori dalla portata di {raggioImpattoPugno}m). Colpo schivato!");
            }
        }

        staEseguendoPugno = false;
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
            if (hit.transform.root == transform.root || hit.collider.CompareTag(SectorContainmentTags.Enemy) || hit.collider.CompareTag(SectorContainmentTags.Drone) || hit.collider.CompareTag(SectorContainmentTags.MaintenanceBot))
                continue;

            if (hit.transform.root == playerTransform.root || hit.collider.CompareTag(SectorContainmentTags.Player))
                return true;

            if (hit.collider.isTrigger)
                continue;

            if (hit.distance >= distance - 0.3f)
                return true;

            return false;
        }

        return true;
    }

    private bool RilevaBersaglio(out float distanza)
    {
        distanza = Vector3.Distance(transform.position, playerTransform != null ? playerTransform.position : transform.position);
        if (playerTransform == null || distanza > raggioVisione) return false;

        Vector3 origineOcchi = transform.position + Vector3.up * 1.2f + transform.forward * 0.35f;
        Vector3 playerChest = playerTransform.position + Vector3.up * 1.0f;
        Vector3 direzioneVersoPlayer = (playerChest - origineOcchi);
        float distEffettiva = direzioneVersoPlayer.magnitude;
        if (distEffettiva <= 0.001f) return true;
        direzioneVersoPlayer.Normalize();

        bool inCampoVisivo = false;
        if (distEffettiva <= raggioRilevamentoRavvicinato)
        {
            inCampoVisivo = true;
        }
        else
        {
            float angoloFrontale = Vector3.Angle(transform.forward, direzioneVersoPlayer);
            if (angoloFrontale <= angoloVisione / 2f)
            {
                inCampoVisivo = true;
            }
        }

        if (!inCampoVisivo) return false;

        return HaLineaDiVistaLibera(origineOcchi, playerChest, distEffettiva);
    }

    private void EseguiRondaCasuale()
    {
        if (!AgentePronto)
        {
            if (agente != null && agente.enabled && !agente.isOnNavMesh)
            {
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 10.0f, NavMesh.AllAreas))
                {
                    transform.position = hit.position;
                    agente.Warp(hit.position);
                }
            }
            return;
        }

        if (inPausa)
        {
            agente.isStopped = true;
            agente.velocity = Vector3.zero;
            timerPausa -= Time.deltaTime;

            if (timerPausa <= 0f)
            {
                inPausa = false;
                haDestinazioneAttiva = false;
                ImpostaNuovaDestinazioneCasuale();
            }
            return;
        }

        if (!haDestinazioneAttiva)
        {
            ImpostaNuovaDestinazioneCasuale();
            return;
        }

        timerDestinazione += Time.deltaTime;

        agente.isStopped = false;
        agente.speed = velocitaRicerca;
        agente.stoppingDistance = 0.5f;

        Vector2 posAgenteXZ = new Vector2(transform.position.x, transform.position.z);
        Vector2 posDestXZ = new Vector2(destinazioneCorrente.x, destinazioneCorrente.z);
        float distRealeXZ = Vector2.Distance(posAgenteXZ, posDestXZ);

        if (distRealeXZ <= agente.stoppingDistance + 0.6f || (!agente.pathPending && agente.hasPath && timerDestinazione > 0.5f && agente.remainingDistance <= agente.stoppingDistance + 0.4f))
        {
            haDestinazioneAttiva = false;
            inPausa = true;
            timerPausa = Random.Range(tempoPausaMin, tempoPausaMax);
            Debug.Log($"<color=cyan>[BOT]</color> {gameObject.name}: punto di ronda raggiunto. Pausa per {timerPausa:F1}s.");
        }
        else if (timerDestinazione > 12.0f || (!agente.pathPending && !agente.hasPath && timerDestinazione > 1.0f))
        {
            haDestinazioneAttiva = false;
            ImpostaNuovaDestinazioneCasuale();
        }
    }

    private void ImpostaNuovaDestinazioneCasuale()
    {
        if (!AgentePronto || isMorto) return;

        Vector3 centroRonda = (posizioneIniziale != Vector3.zero) ? posizioneIniziale : transform.position;

        for (int i = 0; i < 20; i++)
        {
            Vector3 basePoint = (i % 2 == 0) ? centroRonda : transform.position;
            Vector2 cerchio2D = Random.insideUnitCircle * raggioPattugliamento;
            Vector3 puntoCandidato = basePoint + new Vector3(cerchio2D.x, 0f, cerchio2D.y);

            if (NavMesh.SamplePosition(puntoCandidato, out NavMeshHit hit, raggioPattugliamento + 2.0f, NavMesh.AllAreas))
            {
                float dist = Vector3.Distance(transform.position, hit.position);
                if (dist > 1.5f)
                {
                    destinazioneCorrente = hit.position;
                    agente.isStopped = false;
                    agente.speed = velocitaRicerca;
                    agente.SetDestination(hit.position);
                    haDestinazioneAttiva = true;
                    inPausa = false;
                    timerDestinazione = 0f;
                    return;
                }
            }
        }

        if (Vector3.Distance(transform.position, centroRonda) > 2.0f)
        {
            destinazioneCorrente = centroRonda;
            agente.isStopped = false;
            agente.speed = velocitaRicerca;
            agente.SetDestination(centroRonda);
            haDestinazioneAttiva = true;
            inPausa = false;
            timerDestinazione = 0f;
            return;
        }

        haDestinazioneAttiva = false;
        inPausa = true;
        timerPausa = 1.0f;
    }

    public void SubisciDanno(float quantitaDanno)
    {
        if (isMorto) return;

        salute -= quantitaDanno;
        RiproduciSuono(suonoDanno);

        if (statoAttuale == StatoIA.RicercaAttiva)
        {
            statoAttuale = StatoIA.Inseguimento;
            inPausa = false;
        }

        Debug.Log($"[BOT] {gameObject.name} ha subito {quantitaDanno} di danno. Salute residua: {salute}");

        if (salute <= 0)
        {
            EseguiMorte();
        }
    }

    private void EseguiMorte()
    {
        isMorto = true;
        statoAttuale = StatoIA.Morto;
        staEseguendoPugno = false;

        if (coroutineAttacco != null)
        {
            StopCoroutine(coroutineAttacco);
            coroutineAttacco = null;
        }

        if (audioMovimentoSource != null && audioMovimentoSource.isPlaying)
        {
            audioMovimentoSource.Stop();
        }

        RiproduciSuono(suonoMorte);

        Debug.Log($"<b><color=red>[DECESSO] {gameObject.name} ha esaurito i punti vita.</color></b>");

        if (agente != null)
        {
            agente.ResetPath();
            agente.enabled = false;
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (anim != null)
        {
            anim.SetTrigger(morteTriggerHash);
        }

        Destroy(gameObject, tempoDistruzioneCorpo);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, raggioVisione);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, distanzaAttacco);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, raggioImpattoPugno);
    }
}
