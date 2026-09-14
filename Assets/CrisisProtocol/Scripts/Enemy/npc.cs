// ============================================================================
// Crisis Protocol / Sector Containment - Nemici e minacce
// File: .\Assets\CrisisProtocol\Scripts\Enemy\npc.cs
// Responsabilita': definisce pattugliamento, inseguimento, attacco o comportamento di droni, guardie e bot ostili.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using System.Collections; // usa lib // riga-ok
using UnityEngine; // usa lib // riga-ok
using UnityEngine.AI; // usa lib // riga-ok
using CrisisProtocol.UI; // usa lib // riga-ok

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))] // nota unity // riga-ok
// blocco: classe x roba grossa
public class NPC : MonoBehaviour, IDamageable // classe qui // riga-ok
{ // apre // riga-ok
    private enum StatoNPC { InAttesa, InPattugliamento, InFuga, Morto } // roba pub // riga-ok

    [Header("Configurazione Movimento")] // nota unity // riga-ok
    [SerializeField] private float raggioPattugliamento = 15f; // setta // riga-ok
    [SerializeField] private float velNormale = 3f; // setta // riga-ok
    [SerializeField] private float velFuga = 6f; // setta // riga-ok
    [Tooltip("Velocità di rotazione angolare dell'NPC. Valori alti evitano camminate diagonali e slittamenti.")] // nota unity // riga-ok
    [SerializeField] private float velocitaRotazione = 450f; // setta // riga-ok

    [Header("Logica di Sosta (Idle)")] // nota unity // riga-ok
    [SerializeField] private float tempoSostaMinimo = 2f; // setta // riga-ok
    [SerializeField] private float tempoSostaMassimo = 5f; // setta // riga-ok

    [Header("Stato NPC")] // nota unity // riga-ok
    [SerializeField] private float salute = 100f; // setta // riga-ok
    [SerializeField] private float tempoDistruzioneCorpo = 3.0f; // setta // riga-ok

    [Header("Integrazione Animatore")] // nota unity // riga-ok
    [SerializeField] private string triggerDanno = "Hit"; // setta // riga-ok
    [SerializeField] private string triggerMorte = "Die"; // setta // riga-ok

    // Componenti e Hash
    private NavMeshAgent agente; // roba pub // riga-ok
    private Animator anim; // roba pub // riga-ok
    private int speedHash; // roba pub // riga-ok
    private int dannoTriggerHash; // roba pub // riga-ok
    private int morteTriggerHash; // roba pub // riga-ok

    // Variabili di Stato Interne
    private StatoNPC statoCorrente = StatoNPC.InAttesa; // roba pub // riga-ok
    private bool coroutineSostaAttiva = false; // roba pub // riga-ok

    void Start() // chiama // riga-ok
    { // apre // riga-ok
        InizializzaComponenti(); // chiama // riga-ok
        AncoraSuNavMesh(); // chiama // riga-ok
    } // chiude // riga-ok

    void Update() // chiama // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (ModalUIState.IsModalOpen) // se ok // riga-ok
            return; // torna val // riga-ok

        // blocco: controlla se va
        if (statoCorrente == StatoNPC.Morto) return; // se ok // riga-ok

        GestisciMacchinaStati(); // chiama // riga-ok
        SincronizzaAnimazioni(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void InizializzaComponenti() // roba pub // riga-ok
    { // apre // riga-ok
        agente = GetComponent<NavMeshAgent>(); // setta // riga-ok
        anim = GetComponent<Animator>(); // setta // riga-ok
        // blocco: controlla se va
        if (anim != null) // se ok // riga-ok
        { // apre // riga-ok
            anim.applyRootMotion = false; // setta // riga-ok
        } // chiude // riga-ok
        
        // Ottimizzazione NavMeshAgent per rotazioni credibili ed evitanza ostacoli
        agente.speed = velNormale; // setta // riga-ok
        agente.angularSpeed = velocitaRotazione; // setta // riga-ok
        agente.updateRotation = false; // Gestiamo la rotazione manualmente per una precisione millimetrica // setta // riga-ok
        agente.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance; // setta // riga-ok

        // Caching dei parametri dell'Animator
        speedHash = Animator.StringToHash("Speed"); // setta // riga-ok
        dannoTriggerHash = Animator.StringToHash(triggerDanno); // setta // riga-ok
        morteTriggerHash = Animator.StringToHash(triggerMorte); // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void AncoraSuNavMesh() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (agente == null) return; // se ok // riga-ok

        // blocco: controlla se va
        if (!agente.isOnNavMesh) // se ok // riga-ok
        { // apre // riga-ok
            // Raggio 8 m: tolleranza generosa per NPC poco fuori mesh
            // blocco: controlla se va
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 8.0f, NavMesh.AllAreas)) // se ok // riga-ok
            { // apre // riga-ok
                transform.position = hit.position; // setta // riga-ok
                agente.Warp(hit.position); // chiama // riga-ok
            } // chiude // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
            { // apre // riga-ok
                // Nessuna NavMesh entro 8 m: disabilita l'agente (usa solo idle visuale)
                // e logga UN SOLO avviso cliccabile invece di errori ripetuti.
                Debug.LogWarning($"[NPC] {gameObject.name}: NavMesh non trovata entro 8 m. " + // logga // riga-ok
                                 "L'NPC sarà inattivo. Sposta il GameObject su una superficie " + // ok qua // riga-ok
                                 "percorribile e ribaka la NavMesh (Window → AI → Navigation → Bake).", this); // chiama // riga-ok
                agente.enabled = false; // setta // riga-ok
                statoCorrente = StatoNPC.Morto; // evita Update loop su agente disabilitato // setta // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void GestisciMacchinaStati() // roba pub // riga-ok
    { // apre // riga-ok
        // Gestione manuale della rotazione per allinearsi alla direzione del path (evita camminate diagonali)
        ApplicaRotazioneFisica(); // chiama // riga-ok

        // blocco: scegli strada
        switch (statoCorrente) // scegli // riga-ok
        { // apre // riga-ok
            case StatoNPC.InAttesa: // caso // riga-ok
                // blocco: controlla se va
                if (!coroutineSostaAttiva) // se ok // riga-ok
                { // apre // riga-ok
                    StartCoroutine(RoutineSosta()); // corutina // riga-ok
                } // chiude // riga-ok
                break; // stop // riga-ok

            case StatoNPC.InPattugliamento: // caso // riga-ok
                ControllaArrivoAInDestinazione(StatoNPC.InAttesa); // chiama // riga-ok
                break; // stop // riga-ok

            case StatoNPC.InFuga: // caso // riga-ok
                ControllaArrivoAInDestinazione(StatoNPC.InAttesa); // chiama // riga-ok
                break; // stop // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void ApplicaRotazioneFisica() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (agente == null || !agente.enabled || !agente.isOnNavMesh) return; // se ok // riga-ok

        // blocco: controlla se va
        if (agente.velocity.sqrMagnitude > 0.1f) // se ok // riga-ok
        { // apre // riga-ok
            // Calcola la direzione basandosi solo sull'asse orizzontale (Y locale bloccata)
            Vector3 direzioneSguardo = agente.velocity; // setta // riga-ok
            direzioneSguardo.y = 0; // setta // riga-ok
            
            // blocco: controlla se va
            if (direzioneSguardo != Vector3.zero) // se ok // riga-ok
            { // apre // riga-ok
                Quaternion rotazioneTarget = Quaternion.LookRotation(direzioneSguardo); // setta // riga-ok
                transform.rotation = Quaternion.RotateTowards(transform.rotation, rotazioneTarget, velocitaRotazione * Time.deltaTime); // setta // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private IEnumerator RoutineSosta() // roba pub // riga-ok
    { // apre // riga-ok
        coroutineSostaAttiva = true; // setta // riga-ok
        
        // Reset del percorso e decelerazione
        // blocco: controlla se va
        if (agente.isOnNavMesh) agente.ResetPath(); // se ok // riga-ok

        float tempoAttesa = Random.Range(tempoSostaMinimo, tempoSostaMassimo); // setta // riga-ok
        yield return new WaitForSeconds(tempoAttesa); // aspetta // riga-ok

        // blocco: controlla se va
        if (statoCorrente != StatoNPC.Morto) // se ok // riga-ok
        { // apre // riga-ok
            CalcolaDestinazioneValida(); // chiama // riga-ok
            statoCorrente = StatoNPC.InPattugliamento; // setta // riga-ok
        } // chiude // riga-ok

        coroutineSostaAttiva = false; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void ControllaArrivoAInDestinazione(StatoNPC statoSuccessivo) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (!agente.pathPending && agente.isOnNavMesh) // se ok // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (agente.remainingDistance <= agente.stoppingDistance || !agente.hasPath) // se ok // riga-ok
            { // apre // riga-ok
                statoCorrente = statoSuccessivo; // setta // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void CalcolaDestinazioneValida() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (!agente.isOnNavMesh) return; // se ok // riga-ok

        int tentativiMassimi = 10; // setta // riga-ok
        // blocco: gira piu volte
        for (int i = 0; i < tentativiMassimi; i++) // ciclo x // riga-ok
        { // apre // riga-ok
            Vector3 puntoCasuale = (Random.insideUnitSphere * raggioPattugliamento) + transform.position; // setta // riga-ok

            // Filtro 1: Trova il punto più vicino sulla NavMesh geometrica
            // blocco: controlla se va
            if (NavMesh.SamplePosition(puntoCasuale, out NavMeshHit hit, raggioPattugliamento, NavMesh.AllAreas)) // se ok // riga-ok
            { // apre // riga-ok
                // Filtro 2: Calcola il percorso effettivo per verificare che non attraversi o sbatta contro muri
                NavMeshPath path = new NavMeshPath(); // setta // riga-ok
                // blocco: controlla se va
                if (agente.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete) // se ok // riga-ok
                { // apre // riga-ok
                    // Controllo di sicurezza: impedisce di scegliere un punto all'interno dei nodi di campionamento dei muri
                    // blocco: controlla se va
                    if (Vector3.Distance(transform.position, hit.position) > 2.0f) // se ok // riga-ok
                    { // apre // riga-ok
                        agente.SetPath(path); // chiama // riga-ok
                        return; // torna val // riga-ok
                    } // chiude // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void SincronizzaAnimazioni() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (anim != null && agente != null && agente.enabled) // se ok // riga-ok
        { // apre // riga-ok
            anim.SetFloat(speedHash, agente.velocity.magnitude); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void SubisciDanno(float quantitaDanno) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (statoCorrente == StatoNPC.Morto) return; // se ok // riga-ok

        // Se stava sostando, interrompiamo la sosta immediatamente
        // blocco: controlla se va
        if (coroutineSostaAttiva) // se ok // riga-ok
        { // apre // riga-ok
            StopAllCoroutines(); // chiama // riga-ok
            coroutineSostaAttiva = false; // setta // riga-ok
        } // chiude // riga-ok

        salute -= quantitaDanno; // setta // riga-ok
        agente.speed = velFuga; // setta // riga-ok
        statoCorrente = StatoNPC.InFuga; // setta // riga-ok

        // blocco: controlla se va
        if (salute <= 0) // se ok // riga-ok
        { // apre // riga-ok
            EseguiMorte(); // chiama // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            CalcolaDestinazioneValida(); // Scappa subito verso un nuovo punto sicuro // ok qua // riga-ok
            // blocco: controlla se va
            if (anim != null) anim.SetTrigger(dannoTriggerHash); // se ok // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void EseguiMorte() // roba pub // riga-ok
    { // apre // riga-ok
        statoCorrente = StatoNPC.Morto; // setta // riga-ok
        StopAllCoroutines(); // chiama // riga-ok

        // blocco: controlla se va
        if (agente != null && agente.isOnNavMesh) // se ok // riga-ok
        { // apre // riga-ok
            agente.isStopped = true; // setta // riga-ok
            agente.velocity = Vector3.zero; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (anim != null) anim.SetTrigger(morteTriggerHash); // se ok // riga-ok

        CapsuleCollider col = GetComponent<CapsuleCollider>(); // setta // riga-ok
        // blocco: controlla se va
        if (col != null) col.enabled = false; // se ok // riga-ok

        Invoke(nameof(DisabilitaAgenteCompleto), 0.1f); // chiama // riga-ok
        Destroy(gameObject, tempoDistruzioneCorpo); // elimina // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void DisabilitaAgenteCompleto() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (agente != null) agente.enabled = false; // se ok // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
