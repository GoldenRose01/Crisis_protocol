using UnityEngine;
using UnityEngine.AI;
using GoldenCast.UI;

[RequireComponent(typeof(NavMeshAgent))]
public class ManutenzioneBot : MonoBehaviour, IDamageable
{
    public enum StatoIA { RicercaAttiva, Inseguimento, CombattimentoDistanza, Morto }

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
    [SerializeField] private float distanzaOttimaleTiro = 6.5f;

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

    [Header("Sensori e Rilevamento (Visione)")]
    [SerializeField] private float raggioVisione = 14f;
    [Range(0, 360)] [SerializeField] private float angoloVisione = 90f;
    [SerializeField] private float raggioRilevamentoRavvicinato = 2.5f;
    [SerializeField] private LayerMask layerOstacoli;
    [SerializeField] private Transform puntoOcchi;

    [Header("Arma e Balistica")]
    [SerializeField] private float dannoArma = 20f;
    [SerializeField] private float cadenzaDiFuoco = 0.8f;
    [SerializeField] private Transform puntoDiFuoco;
    private float timerSparo = 0f;

    [Header("Statistiche Vitali")]
    [SerializeField] private float salute = 100f;
    [Tooltip("Tempo in secondi da attendere prima di rimuovere il corpo dal gioco dopo la morte.")]
    [SerializeField] private float tempoDistruzioneCorpo = 3.0f;

    [Header("Integrazione Animatore")]
    [SerializeField] private string triggerMorte = "Die";
    [SerializeField] private string triggerSparo = "Shoot";

    private NavMeshAgent agente;
    private Animator anim;
    private Transform playerTransform;

    // Cache degli hash dei parametri dell'Animator per ottimizzare le performance
    private int speedHash;
    private int morteTriggerHash;
    private int sparoTriggerHash;
    private bool isMorto = false;

    private float timerAggiornamentoPercorso = 0f;

    private bool AgentePronto => agente != null && agente.enabled && agente.isOnNavMesh;

    [ContextMenu("Ripristina Velocità e Parametri Calmi")]
    public void ResetValoriPredefiniti()
    {
        velocitaRicerca = 1.2f;
        velocitaInseguimento = 2.2f;
        accelerazione = 2.0f;
        velocitaRotazione = 120f;
        raggioPattugliamento = 8f;
        distanzaOttimaleTiro = 6.5f;
        raggioVisione = 14f;
        tempoPausaMin = 2f;
        tempoPausaMax = 4f;

        if (agente == null) agente = GetComponent<NavMeshAgent>();
        if (agente != null)
        {
            agente.speed = velocitaRicerca;
            agente.acceleration = accelerazione;
            agente.angularSpeed = velocitaRotazione;
            agente.stoppingDistance = 0.5f;
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
        // Corregge e azzera qualsiasi offset locale nei modelli 3D figli (elimina l'effetto braccio di leva)
        foreach (Transform child in transform)
        {
            if (child.localPosition != Vector3.zero)
            {
                child.localPosition = Vector3.zero;
            }
        }

        // Rendi cinematici anche eventuali Rigidbody sui figli
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

        // Applica automaticamente il tag corretto per il riconoscimento da parte del sistema di missione
        SectorContainmentTags.ApplyTag(gameObject, SectorContainmentTags.MaintenanceBot);

        agente = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        if (anim == null)
            anim = GetComponentInChildren<Animator>();

        if (anim != null)
        {
            anim.applyRootMotion = false;
        }

        // Disabilita Root Motion su TUTTI gli animatori per evitare trascinamenti o scatti della mesh
        Animator[] allAnimators = GetComponentsInChildren<Animator>();
        foreach (Animator a in allAnimators)
        {
            if (a != null)
                a.applyRootMotion = false;
        }

        // Rendi il Rigidbody cinematico per evitare conflitti tra il motore fisico PhysX e il NavMeshAgent
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
            agente.stoppingDistance = 0.5f;
            agente.autoBraking = true;
            agente.radius = 0.35f;
            agente.height = 1.8f;
            // FONDAMENTALE: Impedisce salti o teletrasporti attraverso i muri
            agente.autoTraverseOffMeshLink = false;
            agente.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        }

        // Calibra il CapsuleCollider affinché sia sempre all'interno dell'agente (evita spinte fisiche)
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

        TrovaPlayer();

        if (puntoOcchi == null) puntoOcchi = transform;
        if (puntoDiFuoco == null) puntoDiFuoco = transform;

        if (agente != null && agente.enabled)
        {
            if (agente.isOnNavMesh)
            {
                ImpostaNuovaDestinazioneCasuale();
            }
            else
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(transform.position, out hit, 3.0f, NavMesh.AllAreas))
                {
                    transform.position = hit.position;
                    agente.Warp(hit.position);
                    ImpostaNuovaDestinazioneCasuale();
                }
                else
                {
                    Debug.LogWarning($"[BOT] {gameObject.name} non è posizionato su una NavMesh valida. Spostalo sul pavimento calpestabile.", this);
                }
            }
        }
    }

    private void TrovaPlayer()
    {
        if (playerTransform != null) return;

        GameObject playerObj = GameObject.FindGameObjectWithTag(SectorContainmentTags.Player);
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
            if (playerTransform == null) return;
        }

        // Assicurati che l'agente mantenga sempre l'accelerazione morbida configurata
        if (AgentePronto)
        {
            agente.acceleration = accelerazione;
            agente.angularSpeed = velocitaRotazione;
        }

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

            // Se era in combattimento o inseguimento, torna alla ronda pacifica
            if (statoAttuale == StatoIA.Inseguimento || statoAttuale == StatoIA.CombattimentoDistanza)
            {
                statoAttuale = StatoIA.RicercaAttiva;
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
                anim.SetFloat(speedHash, agente.velocity.magnitude, 0.15f, Time.deltaTime);
            }
            return; // Nessun attacco o sparo quando l'emergenza è terminata
        }

        timerSparo += Time.deltaTime;
        timerAggiornamentoPercorso += Time.deltaTime;

        bool bersaglioRilevato = RilevaBersaglio(out float distanzaDalPlayer);

        GestisciMacchinaAStati(bersaglioRilevato, distanzaDalPlayer);

        if (anim != null && anim.runtimeAnimatorController != null && AgentePronto)
        {
            anim.SetFloat(speedHash, agente.velocity.magnitude, 0.15f, Time.deltaTime);
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
                    timerMemoriaInseguimento = 2.5f;
                    if (AgentePronto)
                    {
                        agente.isStopped = false;
                        agente.speed = velocitaInseguimento;
                        agente.stoppingDistance = distanzaOttimaleTiro;
                    }
                    Debug.Log("<color=red>[BOT] Bersaglio rilevato! Inizio inseguimento controllato.</color>");
                }
                else
                {
                    EseguiRondaCasuale();
                }
                break;

            case StatoIA.Inseguimento:
                if (bersaglioRilevato)
                {
                    timerMemoriaInseguimento = 2.5f; // ricarica la memoria visiva
                }
                else
                {
                    timerMemoriaInseguimento -= Time.deltaTime;
                }

                if (!bersaglioRilevato && timerMemoriaInseguimento <= 0f)
                {
                    // Ha perso le tracce del player: torna alla ronda tranquilla
                    statoAttuale = StatoIA.RicercaAttiva;
                    if (AgentePronto)
                    {
                        agente.isStopped = false;
                        agente.speed = velocitaRicerca;
                        agente.stoppingDistance = 0.5f;
                    }
                    ImpostaNuovaDestinazioneCasuale();
                }
                else if (bersaglioRilevato && distanza <= distanzaOttimaleTiro)
                {
                    // Entrato nel raggio di fuoco ottimale: fermati e spara
                    statoAttuale = StatoIA.CombattimentoDistanza;
                    if (AgentePronto)
                    {
                        agente.isStopped = true;
                    }
                }
                else
                {
                    // Insegui a velocità controllata
                    if (AgentePronto)
                    {
                        agente.isStopped = false;
                        agente.speed = velocitaInseguimento;
                        agente.stoppingDistance = distanzaOttimaleTiro;
                        agente.SetDestination(playerTransform.position);
                    }
                    else
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

            case StatoIA.CombattimentoDistanza:
                // Isteresi: torna a inseguire solo se il player si allontana oltre distanzaOttimaleTiro + 2.5 metri
                if (!bersaglioRilevato || distanza > distanzaOttimaleTiro + 2.5f)
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
                    EseguiRoutineDiSparo();
                }
                break;
        }
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

            // Muro/ostacolo che blocca la linea di vista
            return false;
        }

        return true;
    }

    private bool RilevaBersaglio(out float distanza)
    {
        distanza = Vector3.Distance(transform.position, playerTransform.position);
        if (distanza > raggioVisione) return false;

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
        if (!AgentePronto) return;

        if (inPausa)
        {
            agente.isStopped = true;
            timerPausa -= Time.deltaTime;

            if (timerPausa <= 0f)
            {
                inPausa = false;
                ImpostaNuovaDestinazioneCasuale();
            }
            return;
        }

        agente.isStopped = false;
        agente.speed = velocitaRicerca;
        agente.stoppingDistance = 0.5f;

        // Se non ha un percorso o ha raggiunto la meta con tolleranza
        if (!agente.pathPending)
        {
            if (!agente.hasPath || agente.remainingDistance <= agente.stoppingDistance + 0.3f)
            {
                inPausa = true;
                timerPausa = Random.Range(tempoPausaMin, tempoPausaMax);
            }
        }
    }

    private void ImpostaNuovaDestinazioneCasuale()
    {
        if (!AgentePronto || isMorto) return;

        int tentativi = 10;
        for (int i = 0; i < tentativi; i++)
        {
            // Campionamento planare XZ locale
            Vector2 cerchio2D = Random.insideUnitCircle * raggioPattugliamento;
            Vector3 puntoCasuale = transform.position + new Vector3(cerchio2D.x, 0f, cerchio2D.y);

            if (NavMesh.SamplePosition(puntoCasuale, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
            {
                NavMeshPath path = new NavMeshPath();
                // Verifica che il percorso sia COMPLETO e raggiungibile a piedi (non attraverso i muri)
                if (agente.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    if (Vector3.Distance(transform.position, hit.position) > 2.0f)
                    {
                        agente.isStopped = false;
                        agente.speed = velocitaRicerca;
                        agente.SetPath(path);
                        return;
                    }
                }
            }
        }

        // Se non trova un punto valido nei tentativi, resta in pausa breve e riprova
        inPausa = true;
        timerPausa = 1.0f;
    }

    private void EseguiRoutineDiSparo()
    {
        if (AgentePronto)
        {
            agente.isStopped = true;
        }

        Vector3 direzioneMira = (playerTransform.position - transform.position).normalized;
        direzioneMira.y = 0;
        if (direzioneMira.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direzioneMira), 6f * Time.deltaTime);
        }

        if (timerSparo >= cadenzaDiFuoco)
        {
            if (anim != null) anim.SetTrigger(sparoTriggerHash);
            SparaProiettileVirtuale();
            timerSparo = 0f;
        }
    }

    private void SparaProiettileVirtuale()
    {
        Vector3 centroPlayer = playerTransform.position + Vector3.up * 1.0f;
        Vector3 origineSparo = transform.position + Vector3.up * 1.2f + transform.forward * 0.4f;
        Vector3 direzioneTraiettoria = (centroPlayer - origineSparo);
        float dist = direzioneTraiettoria.magnitude;
        direzioneTraiettoria.Normalize();

        Debug.Log("<color=yellow>[BOT] Fuoco di soppressione sferrato dall'unità.</color>");

        RaycastHit[] hits = Physics.RaycastAll(origineSparo, direzioneTraiettoria, dist + 0.5f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.root == transform.root || hit.collider.CompareTag(SectorContainmentTags.Enemy) || hit.collider.CompareTag(SectorContainmentTags.Drone) || hit.collider.CompareTag(SectorContainmentTags.MaintenanceBot))
                continue;

            if (hit.transform.root == playerTransform.root || hit.collider.CompareTag(SectorContainmentTags.Player))
            {
                SalutePlayer vitaPlayer = hit.collider.GetComponent<SalutePlayer>() ?? hit.collider.GetComponentInParent<SalutePlayer>() ?? playerTransform.GetComponent<SalutePlayer>();
                if (vitaPlayer != null)
                {
                    vitaPlayer.SubisciDanno(dannoArma);
                    Debug.Log($"<color=red><b>[BOT]</b> Colpo a segno su {hit.collider.gameObject.name}! Inflitti {dannoArma} HP.</color>");
                }
                return;
            }

            if (hit.collider.isTrigger)
                continue;

            // Ostacolo ambientale
            Debug.Log($"<color=gray>[BOT] Il colpo ha impattato un ostacolo ambientale: {hit.collider.gameObject.name}</color>");
            return;
        }
    }

    public void SubisciDanno(float quantitaDanno)
    {
        if (isMorto) return;

        salute -= quantitaDanno;

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
}
