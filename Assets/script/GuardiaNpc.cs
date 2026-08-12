using UnityEngine;
using UnityEngine.AI;
using GoldenCast.UI;

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
    [Tooltip("Distanza minima di arresto dal giocatore per evitare collisioni fisiche.")]
    public float distanzaArresto = 1.8f;
    private int indiceWaypointAttuale = 0;
    
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

    [Header("Statistiche e Combattimento")]
    public float saluteMassima = 100f;
    private float saluteCorrente;
    public float dannoAttacco = 25f;
    public float cadenzaAttacco = 1.2f;
    private float timerProssimoAttacco = 0f;

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

    void Start()
    {
        ApplicaTagUnity();
        saluteCorrente = saluteMassima;
        posizioneIniziale = transform.position;

        agente = GetComponent<NavMeshAgent>();
        if (agente != null)
        {
            NavMeshHit hitMesh;
            if (NavMesh.SamplePosition(transform.position, out hitMesh, 4.0f, NavMesh.AllAreas))
            {
                transform.position = hitMesh.position;
                agente.Warp(hitMesh.position);
            }
            agente.updateRotation = true;
            agente.stoppingDistance = distanzaArresto;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag(SectorContainmentTags.Player);
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerScript = playerObj.GetComponent<muve_pg>();
            
            // CORREZIONE 1: Ricerca estesa per IDamageable
            playerDamageable = playerObj.GetComponentInChildren<IDamageable>();
            if (playerDamageable == null)
            {
                playerDamageable = playerObj.GetComponentInParent<IDamageable>();
            }
        }

        if (eStatica)
        {
            statoAttuale = StatoGuardia.Inattiva;
        }

        if (animatore == null)
        {
            animatore = GetComponentInChildren<Animator>();
        }
    }

    private void OnValidate()
    {
        ApplicaTagUnity();
    }

    void Update()
    {
        if (ModalUIState.IsModalOpen)
            return;

        if (statoAttuale == StatoGuardia.Morta) return;

        RilevaGiocatore();
        EseguiComportamento();
        AggiornaAnimazioni();

        if (timerProssimoAttacco > 0)
        {
            timerProssimoAttacco -= Time.deltaTime;
        }
    }

    // CORREZIONE 2: Logica di aggiornamento delle animazioni perfezionata
    private void AggiornaAnimazioni()
{
    if (animatore == null) return;

    float valoreVelocita = 0f;

    if (agente != null && agente.isOnNavMesh)
    {
        // CORREZIONE: remainingDistance viene letto direttamente da agente, non da agente.velocity
        if (agente.remainingDistance > agente.stoppingDistance && agente.velocity.magnitude > 0.1f)
        {
            valoreVelocita = (statoAttuale == StatoGuardia.Inseguimento) ? 2f : 1f;
        }
        else
        {
            valoreVelocita = 0f;
        }
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
        if (waypointRonda == null || waypointRonda.Length == 0) return;

        Transform target = waypointRonda[indiceWaypointAttuale];

        if (agente != null && agente.isOnNavMesh)
        {
            agente.isStopped = false;
            agente.speed = velocitaRonda;
            agente.SetDestination(target.position);

            if (!agente.pathPending && agente.remainingDistance <= agente.stoppingDistance + 0.6f)
            {
                indiceWaypointAttuale = (indiceWaypointAttuale + 1) % waypointRonda.Length;
            }
        }
        else
        {
            Vector3 direzione = (target.position - transform.position).normalized;
            direzione.y = 0;

            Vector3 targetPos = target.position;
            targetPos.y = transform.position.y;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, velocitaRonda * Time.deltaTime);

            if (direzione != Vector3.zero)
            {
                Quaternion rotazioneTarget = Quaternion.LookRotation(direzione);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotazioneTarget, 5f * Time.deltaTime);
            }

            if (Vector3.Distance(transform.position, target.position) < 0.5f)
            {
                indiceWaypointAttuale = (indiceWaypointAttuale + 1) % waypointRonda.Length;
            }
        }
    }

    private void InseguiEAttacca()
    {
        if (playerTransform == null) return;

        float distanzaDalGiocatore = Vector3.Distance(transform.position, playerTransform.position);

        if (agente != null && agente.isOnNavMesh)
        {
            agente.speed = velocitaInseguimento;

            if (distanzaDalGiocatore <= distanzaArresto)
            {
                agente.isStopped = true;
                agente.velocity = Vector3.zero;
            }
            else
            {
                agente.isStopped = false;
                agente.SetDestination(playerTransform.position);
            }

            if (distanzaDalGiocatore < distanzaArresto + 2f)
            {
                RotazioneFluida(playerTransform.position);
            }
        }
        else
        {
            Vector3 direzioneAlPlayer = (playerTransform.position - transform.position).normalized;
            direzioneAlPlayer.y = 0;

            if (distanzaDalGiocatore > distanzaArresto)
            {
                bool ostacoloDavanti = false;
                RaycastHit hit;
                Vector3 origineRaggio = transform.position + Vector3.up * 1f;

                int mask = (layerOstacoli.value != 0) ? layerOstacoli.value : ~LayerMask.GetMask("Ignore Raycast", SectorContainmentTags.Player, SectorContainmentTags.Enemy);

                if (Physics.Raycast(origineRaggio, direzioneAlPlayer, out hit, 1.5f, mask))
                {
                    ostacoloDavanti = true;
                }

                if (!ostacoloDavanti)
                {
                    Vector3 targetPos = playerTransform.position;
                    targetPos.y = transform.position.y;
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, velocitaInseguimento * Time.deltaTime);
                }
                else
                {
                    Vector3 direzioneScivolamentoDestra = Vector3.Cross(direzioneAlPlayer, Vector3.up).normalized;
                    if (!Physics.Raycast(origineRaggio, direzioneScivolamentoDestra, 1.5f, mask))
                    {
                        Vector3 targetPos = transform.position + direzioneScivolamentoDestra * (velocitaInseguimento * 0.7f * Time.deltaTime);
                        targetPos.y = transform.position.y;
                        transform.position = targetPos;
                    }
                    else if (!Physics.Raycast(origineRaggio, -direzioneScivolamentoDestra, 1.5f, mask))
                    {
                        Vector3 targetPos = transform.position - direzioneScivolamentoDestra * (velocitaInseguimento * 0.7f * Time.deltaTime);
                        targetPos.y = transform.position.y;
                        transform.position = targetPos;
                    }
                }
            }
            RotazioneFluida(playerTransform.position);
        }

        if (distanzaDalGiocatore <= distanzaArresto + 0.4f && timerProssimoAttacco <= 0)
        {
            AttaccaPlayer();
        }
    }
    
    private void EseguiRitornoAllaBase()
    {
        Vector3 destinazione = eStatica ? posizioneIniziale : (waypointRonda.Length > 0 ? waypointRonda[0].position : posizioneIniziale);

        if (agente != null && agente.isOnNavMesh)
        {
            agente.isStopped = false;
            agente.speed = velocitaRonda; 
            agente.SetDestination(destinazione);

            if (!agente.pathPending && agente.remainingDistance <= agente.stoppingDistance + 0.5f)
            {
                statoAttuale = eStatica ? StatoGuardia.Inattiva : StatoGuardia.Ronda;
                indiceWaypointAttuale = 0; 
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
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direzione), 5f * Time.deltaTime);
            }

            if (Vector3.Distance(transform.position, targetPos) < 0.5f)
            {
                statoAttuale = eStatica ? StatoGuardia.Inattiva : StatoGuardia.Ronda;
                indiceWaypointAttuale = 0;
            }
        }
    }

    // CORREZIONE 3: Debug inserito per validare la presenza dell'interfaccia
    private void AttaccaPlayer()
    {
        if (playerDamageable != null)
        {
            Debug.Log($"<color=red>[GUARDIA] Attacco diretto! Infligge {dannoAttacco} HP di danno al giocatore.</color>");
            
            if (animatore != null)
            {
                animatore.SetTrigger(triggerAttacco);
            }

            playerDamageable.SubisciDanno(dannoAttacco);
            timerProssimoAttacco = cadenzaAttacco;
        }
        else
        {
            Debug.LogError("[SISTEMA COMBATTIMENTO] ATTENZIONE: La guardia sta cercando di attaccare, ma lo script della salute del giocatore (che usa IDamageable) non è stato trovato!");
            // Resetta comunque il timer per evitare chiamate multiple in un frame
            timerProssimoAttacco = cadenzaAttacco; 
        }
    }

    private void RilevaGiocatore()
    {
        if (playerTransform == null) return;

        float distanza = Vector3.Distance(transform.position, playerTransform.position);

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
            Vector3 direzioneVersoPlayer = (playerTransform.position - transform.position).normalized;
            float angoloFrontale = Vector3.Angle(transform.forward, direzioneVersoPlayer);

            if (angoloFrontale < angoloVisione / 2f)
            {
                if (!Physics.Raycast(transform.position + Vector3.up * 1.5f, direzioneVersoPlayer, out RaycastHit hit, distanza, layerOstacoli))
                {
                    if (statoAttuale != StatoGuardia.Inseguimento)
                    {
                        statoAttuale = StatoGuardia.Inseguimento;
                        Debug.Log("<color=red>[GUARDIA] Bersaglio individuato! Stato: Inseguimento.</color>");
                        if (MissionManager.Instance != null)
                            MissionManager.Instance.RegistraRilevamento(gameObject.name);
                        AllertaGuardieVicine();
                    }
                    return;
                }
            }
            
            float angoloPosteriore = Vector3.Angle(-transform.forward, direzioneVersoPlayer);
            if (angoloPosteriore < angoloPuntoCiecoStealth / 2f)
            {
                return;
            }
        }
        
        if (distanza > raggioVisione + 4f && statoAttuale == StatoGuardia.Inseguimento)
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
        playerTransform = targetPlayer;
        allarmeLanciato = true; 

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
        statoAttuale = StatoGuardia.Morta;
        Debug.Log("<color=green><b>[STEALTH SUCCESS]</b> Guardia eliminata sul colpo con un'azione furtiva silenziosa!</color>");
        EseguiDissolvenzaMorte();
    }

    private void MorteStandard()
    {
        statoAttuale = StatoGuardia.Morta;
        Debug.Log("<color=white>[GUARDIA] Eliminata in combattimento frontale.</color>");
        EseguiDissolvenzaMorte();
    }

    private void EseguiDissolvenzaMorte()
    {
        GetComponent<Collider>().enabled = false;
        
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
