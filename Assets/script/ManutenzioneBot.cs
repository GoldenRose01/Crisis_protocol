using UnityEngine;
using UnityEngine.AI;
using GoldenCast.UI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class ManutenzioneBot : MonoBehaviour, IDamageable
{
    public enum StatoIA { RicercaAttiva, Inseguimento, CombattimentoDistanza, Morto }

    [Header("Stato e Navigazione")]
    public StatoIA statoAttuale = StatoIA.RicercaAttiva;
    [SerializeField] private float velocitaRicerca = 1.5f;
    [SerializeField] private float velocitaInseguimento = 3.5f;
    [SerializeField] private float distanzaOttimaleTiro = 8f;

    [Header("Vagabondaggio Casuale (Roaming)")]
    [SerializeField] private float raggioPattugliamento = 12f;
    [SerializeField] private float tempoPausaMin = 2f;
    [SerializeField] private float tempoPausaMax = 5f;
    private bool inPausa = false;
    private float timerPausa = 0f;

    [Header("Sensori e Rilevamento (Visione)")]
    [SerializeField] private float raggioVisione = 20f;
    [Range(0, 360)] [SerializeField] private float angoloVisione = 110f;
    [SerializeField] private float raggioRilevamentoRavvicinato = 3f;
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

    private bool AgentePronto => agente != null && agente.enabled && agente.isOnNavMesh;

    void Start()
    {
        // Applica automaticamente il tag corretto per il riconoscimento da parte del sistema di missione
        SectorContainmentTags.ApplyTag(gameObject, SectorContainmentTags.MaintenanceBot);

        agente = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();

        if (agente != null)
        {
            agente.speed = velocitaRicerca;
        }

        speedHash = Animator.StringToHash("Speed");
        morteTriggerHash = Animator.StringToHash(triggerMorte);
        sparoTriggerHash = Animator.StringToHash(triggerSparo);

        GameObject playerObj = GameObject.FindGameObjectWithTag(SectorContainmentTags.Player);
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }

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
                if (NavMesh.SamplePosition(transform.position, out hit, 2.0f, NavMesh.AllAreas))
                {
                    transform.position = hit.position;
                    agente.Warp(hit.position);
                    ImpostaNuovaDestinazioneCasuale();
                }
                else
                {
                    Debug.LogError($"[BOT] {gameObject.name} è posizionato troppo lontano dalla NavMesh. Impossibile avviare il pattugliamento.", this);
                }
            }
        }
    }

    void Update()
    {
        if (ModalUIState.IsModalOpen)
            return;

        if (isMorto || playerTransform == null) return;

        timerSparo += Time.deltaTime;

        bool bersaglioRilevato = RilevaBersaglio(out float distanzaDalPlayer);

        GestisciMacchinaAStati(bersaglioRilevato, distanzaDalPlayer);

        if (anim != null && AgentePronto)
        {
            anim.SetFloat(speedHash, agente.velocity.magnitude);
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
                    if (AgentePronto) agente.stoppingDistance = distanzaOttimaleTiro;
                    Debug.Log("<color=red>[BOT] Bersaglio rilevato! Inizio inseguimento.</color>");
                }
                else
                {
                    EseguiRondaCasuale();
                }
                break;

            case StatoIA.Inseguimento:
                if (!bersaglioRilevato && distanza > raggioVisione)
                {
                    statoAttuale = StatoIA.RicercaAttiva;
                    if (AgentePronto) agente.stoppingDistance = 0.5f;
                    ImpostaNuovaDestinazioneCasuale();
                }
                else if (bersaglioRilevato && distanza <= distanzaOttimaleTiro)
                {
                    statoAttuale = StatoIA.CombattimentoDistanza;
                }
                else
                {
                    if (AgentePronto)
                    {
                        agente.isStopped = false;
                        agente.speed = velocitaInseguimento;
                        agente.SetDestination(playerTransform.position);
                    }
                }
                break;

            case StatoIA.CombattimentoDistanza:
                if (!bersaglioRilevato || distanza > distanzaOttimaleTiro + 2f)
                {
                    statoAttuale = StatoIA.Inseguimento;
                }
                else
                {
                    EseguiRoutineDiSparo();
                }
                break;
        }
    }

    private bool RilevaBersaglio(out float distanza)
    {
        distanza = Vector3.Distance(transform.position, playerTransform.position);
        if (distanza > raggioVisione) return false;

        Vector3 direzioneVersoPlayer = (playerTransform.position - puntoOcchi.position).normalized;

        if (distanza <= raggioRilevamentoRavvicinato)
        {
            if (!Physics.Raycast(puntoOcchi.position, direzioneVersoPlayer, distanza, layerOstacoli))
            {
                return true;
            }
        }

        float angoloFrontale = Vector3.Angle(transform.forward, direzioneVersoPlayer);
        if (angoloFrontale < angoloVisione / 2f)
        {
            if (!Physics.Raycast(puntoOcchi.position, direzioneVersoPlayer, distanza, layerOstacoli))
            {
                return true;
            }
        }

        return false;
    }

    private void EseguiRondaCasuale()
    {
        if (!AgentePronto) return;

        if (inPausa)
        {
            agente.isStopped = true;
            agente.velocity = Vector3.zero;
            timerPausa -= Time.deltaTime;

            if (timerPausa <= 0)
            {
                inPausa = false;
                ImpostaNuovaDestinazioneCasuale();
            }
            return;
        }

        agente.isStopped = false;
        agente.speed = velocitaRicerca;
        agente.stoppingDistance = 0.5f;

        if (!agente.pathPending && agente.remainingDistance <= agente.stoppingDistance + 0.3f)
        {
            inPausa = true;
            timerPausa = Random.Range(tempoPausaMin, tempoPausaMax);
        }
    }

    void ImpostaNuovaDestinazioneCasuale()
    {
        if (!AgentePronto || isMorto) return;

        Vector3 direzioneCasuale = Random.insideUnitSphere * raggioPattugliamento;
        direzioneCasuale += transform.position;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(direzioneCasuale, out hit, raggioPattugliamento, NavMesh.AllAreas))
        {
            agente.SetDestination(hit.position);
        }
    }

    private void EseguiRoutineDiSparo()
    {
        if (AgentePronto)
        {
            agente.isStopped = true;
            agente.velocity = Vector3.zero;
        }

        Vector3 direzioneMira = (playerTransform.position - transform.position).normalized;
        direzioneMira.y = 0;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direzioneMira), 12f * Time.deltaTime);

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
        Vector3 direzioneTraiettoria = (centroPlayer - puntoDiFuoco.position).normalized;

        int layerMaskSparo = ~LayerMask.GetMask(SectorContainmentTags.Enemy, "Ignore Raycast");

        Debug.Log("<color=yellow>[BOT] Fuoco di soppressione sferrato dall'unità.</color>");

        if (Physics.Raycast(puntoDiFuoco.position, direzioneTraiettoria, out RaycastHit hit, raggioVisione, layerMaskSparo))
        {
            SalutePlayer vitaPlayer = hit.collider.GetComponent<SalutePlayer>() ?? hit.collider.GetComponentInParent<SalutePlayer>();

            if (vitaPlayer != null)
            {
                vitaPlayer.SubisciDanno(dannoArma);
                Debug.Log($"<color=red><b>[BOT]</b> Colpo a segno su {hit.collider.gameObject.name}! Inflitti {dannoArma} HP.</color>");
            }
            else
            {
                Debug.Log($"<color=gray>[BOT] Il colpo ha impattato un ostacolo ambientale: {hit.collider.gameObject.name}</color>");
            }
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
