using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using GoldenCast.UI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class NPC : MonoBehaviour, IDamageable
{
    private enum StatoNPC { InAttesa, InPattugliamento, InFuga, Morto }

    [Header("Configurazione Movimento")]
    [SerializeField] private float raggioPattugliamento = 15f;
    [SerializeField] private float velNormale = 3f;
    [SerializeField] private float velFuga = 6f;
    [Tooltip("Velocità di rotazione angolare dell'NPC. Valori alti evitano camminate diagonali e slittamenti.")]
    [SerializeField] private float velocitaRotazione = 450f;

    [Header("Logica di Sosta (Idle)")]
    [SerializeField] private float tempoSostaMinimo = 2f;
    [SerializeField] private float tempoSostaMassimo = 5f;

    [Header("Stato NPC")]
    [SerializeField] private float salute = 100f;
    [SerializeField] private float tempoDistruzioneCorpo = 3.0f;

    [Header("Integrazione Animatore")]
    [SerializeField] private string triggerDanno = "Hit";
    [SerializeField] private string triggerMorte = "Die";

    // Componenti e Hash
    private NavMeshAgent agente;
    private Animator anim;
    private int speedHash;
    private int dannoTriggerHash;
    private int morteTriggerHash;

    // Variabili di Stato Interne
    private StatoNPC statoCorrente = StatoNPC.InAttesa;
    private bool coroutineSostaAttiva = false;

    void Start()
    {
        InizializzaComponenti();
        AncoraSuNavMesh();
    }

    void Update()
    {
        if (ModalUIState.IsModalOpen)
            return;

        if (statoCorrente == StatoNPC.Morto) return;

        GestisciMacchinaStati();
        SincronizzaAnimazioni();
    }

    private void InizializzaComponenti()
    {
        agente = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        
        // Ottimizzazione NavMeshAgent per rotazioni credibili ed evitanza ostacoli
        agente.speed = velNormale;
        agente.angularSpeed = velocitaRotazione;
        agente.updateRotation = false; // Gestiamo la rotazione manualmente per una precisione millimetrica
        agente.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        // Caching dei parametri dell'Animator
        speedHash = Animator.StringToHash("Speed");
        dannoTriggerHash = Animator.StringToHash(triggerDanno);
        morteTriggerHash = Animator.StringToHash(triggerMorte);
    }

    private void AncoraSuNavMesh()
    {
        if (agente == null) return;

        if (!agente.isOnNavMesh)
        {
            // Raggio 8 m: tolleranza generosa per NPC poco fuori mesh
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 8.0f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                agente.Warp(hit.position);
            }
            else
            {
                // Nessuna NavMesh entro 8 m: disabilita l'agente (usa solo idle visuale)
                // e logga UN SOLO avviso cliccabile invece di errori ripetuti.
                Debug.LogWarning($"[NPC] {gameObject.name}: NavMesh non trovata entro 8 m. " +
                                 "L'NPC sarà inattivo. Sposta il GameObject su una superficie " +
                                 "percorribile e ribaka la NavMesh (Window → AI → Navigation → Bake).", this);
                agente.enabled = false;
                statoCorrente = StatoNPC.Morto; // evita Update loop su agente disabilitato
            }
        }
    }

    private void GestisciMacchinaStati()
    {
        // Gestione manuale della rotazione per allinearsi alla direzione del path (evita camminate diagonali)
        ApplicaRotazioneFisica();

        switch (statoCorrente)
        {
            case StatoNPC.InAttesa:
                if (!coroutineSostaAttiva)
                {
                    StartCoroutine(RoutineSosta());
                }
                break;

            case StatoNPC.InPattugliamento:
                ControllaArrivoAInDestinazione(StatoNPC.InAttesa);
                break;

            case StatoNPC.InFuga:
                ControllaArrivoAInDestinazione(StatoNPC.InAttesa);
                break;
        }
    }

    private void ApplicaRotazioneFisica()
    {
        if (agente == null || !agente.enabled || !agente.isOnNavMesh) return;

        if (agente.velocity.sqrMagnitude > 0.1f)
        {
            // Calcola la direzione basandosi solo sull'asse orizzontale (Y locale bloccata)
            Vector3 direzioneSguardo = agente.velocity;
            direzioneSguardo.y = 0;
            
            if (direzioneSguardo != Vector3.zero)
            {
                Quaternion rotazioneTarget = Quaternion.LookRotation(direzioneSguardo);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, rotazioneTarget, velocitaRotazione * Time.deltaTime);
            }
        }
    }

    private IEnumerator RoutineSosta()
    {
        coroutineSostaAttiva = true;
        
        // Reset del percorso e decelerazione
        if (agente.isOnNavMesh) agente.ResetPath();

        float tempoAttesa = Random.Range(tempoSostaMinimo, tempoSostaMassimo);
        yield return new WaitForSeconds(tempoAttesa);

        if (statoCorrente != StatoNPC.Morto)
        {
            CalcolaDestinazioneValida();
            statoCorrente = StatoNPC.InPattugliamento;
        }

        coroutineSostaAttiva = false;
    }

    private void ControllaArrivoAInDestinazione(StatoNPC statoSuccessivo)
    {
        if (!agente.pathPending && agente.isOnNavMesh)
        {
            if (agente.remainingDistance <= agente.stoppingDistance || !agente.hasPath)
            {
                statoCorrente = statoSuccessivo;
            }
        }
    }

    private void CalcolaDestinazioneValida()
    {
        if (!agente.isOnNavMesh) return;

        int tentativiMassimi = 10;
        for (int i = 0; i < tentativiMassimi; i++)
        {
            Vector3 puntoCasuale = (Random.insideUnitSphere * raggioPattugliamento) + transform.position;

            // Filtro 1: Trova il punto più vicino sulla NavMesh geometrica
            if (NavMesh.SamplePosition(puntoCasuale, out NavMeshHit hit, raggioPattugliamento, NavMesh.AllAreas))
            {
                // Filtro 2: Calcola il percorso effettivo per verificare che non attraversi o sbatta contro muri
                NavMeshPath path = new NavMeshPath();
                if (agente.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    // Controllo di sicurezza: impedisce di scegliere un punto all'interno dei nodi di campionamento dei muri
                    if (Vector3.Distance(transform.position, hit.position) > 2.0f)
                    {
                        agente.SetPath(path);
                        return;
                    }
                }
            }
        }
    }

    private void SincronizzaAnimazioni()
    {
        if (anim != null && agente != null && agente.enabled)
        {
            anim.SetFloat(speedHash, agente.velocity.magnitude);
        }
    }

    public void SubisciDanno(float quantitaDanno)
    {
        if (statoCorrente == StatoNPC.Morto) return;

        // Se stava sostando, interrompiamo la sosta immediatamente
        if (coroutineSostaAttiva)
        {
            StopAllCoroutines();
            coroutineSostaAttiva = false;
        }

        salute -= quantitaDanno;
        agente.speed = velFuga;
        statoCorrente = StatoNPC.InFuga;

        if (salute <= 0)
        {
            EseguiMorte();
        }
        else
        {
            CalcolaDestinazioneValida(); // Scappa subito verso un nuovo punto sicuro
            if (anim != null) anim.SetTrigger(dannoTriggerHash);
        }
    }

    private void EseguiMorte()
    {
        statoCorrente = StatoNPC.Morto;
        StopAllCoroutines();

        if (agente != null && agente.isOnNavMesh)
        {
            agente.isStopped = true;
            agente.velocity = Vector3.zero;
        }

        if (anim != null) anim.SetTrigger(morteTriggerHash);

        CapsuleCollider col = GetComponent<CapsuleCollider>();
        if (col != null) col.enabled = false;

        Invoke(nameof(DisabilitaAgenteCompleto), 0.1f);
        Destroy(gameObject, tempoDistruzioneCorpo);
    }

    private void DisabilitaAgenteCompleto()
    {
        if (agente != null) agente.enabled = false;
    }
}
