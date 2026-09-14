// ============================================================================
// Crisis Protocol / Sector Containment - Nemici e minacce
// File: .\Assets\CrisisProtocol\Scripts\Enemy\DroneRonda.cs
// Responsabilita': definisce pattugliamento, inseguimento, attacco o comportamento di droni, guardie e bot ostili.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine; // usa lib // riga-ok
using UnityEngine.AI; // usa lib // riga-ok
using CrisisProtocol.UI; // usa lib // riga-ok

// blocco: classe x roba grossa
public class DroneRonda : MonoBehaviour // classe qui // riga-ok
{ // apre // riga-ok
    [Header("Pattugliamento")] // nota unity // riga-ok
    [SerializeField] private bool applicaTagDroneAutomatico = true; // setta // riga-ok
    public Transform[] waypoints; // roba pub // riga-ok
    public float velocita = 3f; // roba pub // riga-ok
    private int indiceWaypointAttuale = 0; // roba pub // riga-ok

    [Header("Sensore Visivo (Cono)")] // nota unity // riga-ok
    public float raggioVisione = 10f; // roba pub // riga-ok
    [Range(0, 360)] public float angoloVisione = 45f; // setta // riga-ok
    public LayerMask layerOstacoli; // roba pub // riga-ok

    [Header("Feedback Visivo (Luce)")] // nota unity // riga-ok
    [Tooltip("Trascina qui la Spot Light del drone")] // nota unity // riga-ok
    public Light luceDrone; // roba pub // riga-ok
    public Color coloreRonda = Color.yellow; // roba pub // riga-ok
    public Color coloreAllarme = Color.red; // roba pub // riga-ok

    [Header("Comportamento Post-Emergenza (Fine Crisi)")] // nota unity // riga-ok
    [Tooltip("Se true, il drone passa in modalità pacifica (luce verde, non spara né dà allarme) quando l'emergenza termina.")] // nota unity // riga-ok
    [SerializeField] private bool pacificaAFineEmergenza = true; // setta // riga-ok

    [Tooltip("Se true, il drone si spegne e ferma completamente a fine emergenza.")] // nota unity // riga-ok
    [SerializeField] private bool spegniAFineEmergenza = false; // setta // riga-ok

    [Tooltip("Colore della luce del drone quando il settore è sicuro e l'emergenza è terminata.")] // nota unity // riga-ok
    [SerializeField] private Color coloreStandbyRisolto = Color.green; // setta // riga-ok

    [Header("Combattimento")] // nota unity // riga-ok
    [Tooltip("Tempo in secondi tra uno sparo e l'altro")] // nota unity // riga-ok
    public float cadenzaDiFuoco = 1.5f;  // roba pub // riga-ok
    private float timerSparo = 0f; // roba pub // riga-ok

    [Header("Audio 3D")] // nota unity // riga-ok
    [Tooltip("Suono continuo del motore di volo/hover del drone.")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoHoverLoop; // ok qua // riga-ok
    [Tooltip("Suono di avvistamento / allarme.")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoAllarme; // ok qua // riga-ok
    [Tooltip("Suono di sparo laser/elettrico.")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoSparo; // ok qua // riga-ok
    [Range(0f, 1f)] [SerializeField] private float volumeAudio = 0.8f; // setta // riga-ok

    private AudioSource audioSource; // roba pub // riga-ok
    private AudioSource audioHoverSource; // roba pub // riga-ok

    private Transform playerTransform; // roba pub // riga-ok
    private IDamageable playerDamageable;  // roba pub // riga-ok
    private bool playerGiaSegnalato; // roba pub // riga-ok
    private bool playerPrecedentementeInVista = false; // roba pub // riga-ok
    private NavMeshAgent agente; // roba pub // riga-ok

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
        if (audioHoverSource == null) // se ok // riga-ok
        { // apre // riga-ok
            Transform tHover = transform.Find("AudioHoverDrone"); // setta // riga-ok
            // blocco: controlla se va
            if (tHover != null) // se ok // riga-ok
            { // apre // riga-ok
                audioHoverSource = tHover.GetComponent<AudioSource>(); // setta // riga-ok
            } // chiude // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
            { // apre // riga-ok
                GameObject goHover = new GameObject("AudioHoverDrone"); // setta // riga-ok
                goHover.transform.SetParent(transform, false); // chiama // riga-ok
                audioHoverSource = goHover.AddComponent<AudioSource>(); // setta // riga-ok
            } // chiude // riga-ok

            audioHoverSource.playOnAwake = false; // setta // riga-ok
            audioHoverSource.spatialBlend = 1.0f; // setta // riga-ok
            audioHoverSource.loop = true; // setta // riga-ok
            audioHoverSource.rolloffMode = AudioRolloffMode.Logarithmic; // setta // riga-ok
            audioHoverSource.minDistance = 1.5f; // setta // riga-ok
            audioHoverSource.maxDistance = 14.0f; // setta // riga-ok
            audioHoverSource.dopplerLevel = 0f; // setta // riga-ok
            audioHoverSource.volume = volumeAudio * 0.6f; // setta // riga-ok
            // blocco: controlla se va
            if (suonoHoverLoop != null) // se ok // riga-ok
            { // apre // riga-ok
                audioHoverSource.clip = suonoHoverLoop; // setta // riga-ok
                // blocco: controlla se va
                if (!audioHoverSource.isPlaying) // se ok // riga-ok
                    audioHoverSource.Play(); // chiama // riga-ok
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

    void Start() // chiama // riga-ok
    { // apre // riga-ok
        agente = GetComponent<NavMeshAgent>(); // setta // riga-ok
        InizializzaAudio(); // chiama // riga-ok
        ApplicaTagUnity(); // chiama // riga-ok
        // blocco: controlla se va
        if (luceDrone == null) // se ok // riga-ok
            luceDrone = GetComponentInChildren<Light>(); // setta // riga-ok
        TrovaRiferimentoPlayer(); // chiama // riga-ok
        AllineaLuce(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void TrovaRiferimentoPlayer() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (playerTransform == null || playerDamageable == null) // se ok // riga-ok
        { // apre // riga-ok
            GameObject playerObj = GameObject.FindGameObjectWithTag(SectorContainmentTags.Player); // setta // riga-ok
            // blocco: controlla se va
            if (playerObj != null) // se ok // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (playerTransform == null) playerTransform = playerObj.transform; // se ok // riga-ok
                // blocco: controlla se va
                if (playerDamageable == null) playerDamageable = playerObj.GetComponent<IDamageable>() ?? playerObj.GetComponentInParent<IDamageable>() ?? playerObj.GetComponentInChildren<IDamageable>(); // se ok // riga-ok
            } // chiude // riga-ok

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
                } // chiude // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    void OnValidate() // chiama // riga-ok
    { // apre // riga-ok
        ApplicaTagUnity(); // chiama // riga-ok
        // blocco: controlla se va
        if (luceDrone == null) // se ok // riga-ok
            luceDrone = GetComponentInChildren<Light>(); // setta // riga-ok
        AllineaLuce(); // chiama // riga-ok
    } // chiude // riga-ok

    void Update() // chiama // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (ModalUIState.IsModalOpen) // se ok // riga-ok
            return; // torna val // riga-ok

        bool emergenzaFinita = MissionManager.Instance != null && MissionManager.Instance.EstrazioneSbloccata; // setta // riga-ok

        // blocco: controlla se va
        if (emergenzaFinita && pacificaAFineEmergenza) // se ok // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (spegniAFineEmergenza) // se ok // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (luceDrone != null) luceDrone.enabled = false; // se ok // riga-ok
                // blocco: controlla se va
                if (audioHoverSource != null && audioHoverSource.isPlaying) // se ok // riga-ok
                    audioHoverSource.Stop(); // chiama // riga-ok
                return; // torna val // riga-ok
            } // chiude // riga-ok

            MuoviDrone(); // chiama // riga-ok
            // blocco: controlla se va
            if (luceDrone != null) // se ok // riga-ok
            { // apre // riga-ok
                luceDrone.color = coloreStandbyRisolto; // setta // riga-ok
            } // chiude // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        MuoviDrone(); // chiama // riga-ok
        timerSparo += Time.deltaTime;  // setta // riga-ok
        
        bool playerSottoTiro = ControllaCampoVisivo(); // setta // riga-ok

        // blocco: controlla se va
        if (playerSottoTiro && !playerGiaSegnalato) // se ok // riga-ok
        { // apre // riga-ok
            playerGiaSegnalato = true; // setta // riga-ok
            RiproduciSuono(suonoAllarme); // chiama // riga-ok
            Debug.Log("<color=red><b>[DRONE] BERSAGLIO AGGANCIATO NEL CONO OTTICO! ALLARME ROSSO ATTIVO!</b></color>"); // logga // riga-ok
            // blocco: controlla se va
            if (MissionManager.Instance != null) // se ok // riga-ok
                MissionManager.Instance.RegistraRilevamento(gameObject.name); // chiama // riga-ok
        } // chiude // riga-ok
        // blocco: controlla se va
        else if (!playerSottoTiro) // se ok // riga-ok
        { // apre // riga-ok
            playerGiaSegnalato = false; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (luceDrone != null) // se ok // riga-ok
        { // apre // riga-ok
            luceDrone.color = playerSottoTiro ? coloreAllarme : coloreRonda; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (playerSottoTiro && timerSparo >= cadenzaDiFuoco) // se ok // riga-ok
        { // apre // riga-ok
            EseguiSparo(); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void AllineaLuce() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (luceDrone != null) // se ok // riga-ok
        { // apre // riga-ok
            luceDrone.range = raggioVisione; // setta // riga-ok
            luceDrone.spotAngle = angoloVisione; // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void MuoviDrone() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (waypoints == null || waypoints.Length == 0) return; // se ok // riga-ok

        // Cerca il prossimo waypoint valido nell'array
        Transform target = waypoints[indiceWaypointAttuale]; // setta // riga-ok
        // blocco: controlla se va
        if (target == null) // se ok // riga-ok
        { // apre // riga-ok
            // blocco: gira piu volte
            for (int i = 0; i < waypoints.Length; i++) // ciclo x // riga-ok
            { // apre // riga-ok
                indiceWaypointAttuale = (indiceWaypointAttuale + 1) % waypoints.Length; // setta // riga-ok
                // blocco: controlla se va
                if (waypoints[indiceWaypointAttuale] != null) // se ok // riga-ok
                { // apre // riga-ok
                    target = waypoints[indiceWaypointAttuale]; // setta // riga-ok
                    break; // stop // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (target == null) return; // Se tutti i waypoint sono nulli, il drone rimane in hovering stazionario e scansiona // se ok // riga-ok

        // SE E' UN ROBOT DI TERRA CON NAVMESH AGENT: USA IL PATHFINDING
        // blocco: controlla se va
        if (agente != null && agente.isOnNavMesh) // se ok // riga-ok
        { // apre // riga-ok
            agente.speed = velocita; // setta // riga-ok
            agente.SetDestination(target.position); // chiama // riga-ok
            
            // blocco: controlla se va
            if (!agente.pathPending && agente.remainingDistance <= (agente.stoppingDistance > 0 ? agente.stoppingDistance : 0.4f)) // se ok // riga-ok
            { // apre // riga-ok
                indiceWaypointAttuale = (indiceWaypointAttuale + 1) % waypoints.Length; // setta // riga-ok
            } // chiude // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        // SE E' UN DRONE VOLANTE (SENZA NAVMESH): USA IL MOVIMENTO DIRETTO IGNORANDO LA Y
        // Ignora l'altezza (Y) del waypoint (la flag): il drone deve mantenere la sua quota attuale
        // altrimenti cercherebbe di schiantarsi sul pavimento girando su se stesso
        Vector3 targetPos = target.position; // setta // riga-ok
        targetPos.y = transform.position.y; // setta // riga-ok

        Vector3 direzione = (targetPos - transform.position).normalized; // setta // riga-ok
        
        Rigidbody rb = GetComponent<Rigidbody>(); // setta // riga-ok
        // blocco: controlla se va
        if (rb != null && !rb.isKinematic) // se ok // riga-ok
        { // apre // riga-ok
            Vector3 targetVel = direzione * velocita; // setta // riga-ok
            targetVel.y = rb.linearVelocity.y; // setta // riga-ok
            rb.linearVelocity = targetVel; // setta // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            transform.position = Vector3.MoveTowards(transform.position, targetPos, velocita * Time.deltaTime); // setta // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (direzione != Vector3.zero) // se ok // riga-ok
        { // apre // riga-ok
            Quaternion rotazioneTarget = Quaternion.LookRotation(direzione); // setta // riga-ok
            transform.rotation = Quaternion.Slerp(transform.rotation, rotazioneTarget, 5f * Time.deltaTime); // setta // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(targetPos.x, 0, targetPos.z)) < 0.4f) // se ok // riga-ok
        { // apre // riga-ok
            indiceWaypointAttuale = (indiceWaypointAttuale + 1) % waypoints.Length; // setta // riga-ok
        } // chiude // riga-ok
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

            // Muro solido che blocca il cono di luce
            return false; // torna val // riga-ok
        } // chiude // riga-ok

        return true; // torna val // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private bool ControllaCampoVisivo() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (playerTransform == null) // se ok // riga-ok
        { // apre // riga-ok
            TrovaRiferimentoPlayer(); // chiama // riga-ok
            // blocco: controlla se va
            if (playerTransform == null) return false; // se ok // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (luceDrone == null) // se ok // riga-ok
            luceDrone = GetComponentInChildren<Light>(); // setta // riga-ok

        Vector3 origineCono = (luceDrone != null ? luceDrone.transform.position : transform.position); // setta // riga-ok
        Vector3 playerChest = playerTransform.position + Vector3.up * 1.0f; // setta // riga-ok

        Vector3 direzioneVersoPlayer = (playerChest - origineCono); // setta // riga-ok
        float distanzaDalPlayer = direzioneVersoPlayer.magnitude; // setta // riga-ok
        // blocco: controlla se va
        if (distanzaDalPlayer > raggioVisione) return false; // se ok // riga-ok
        direzioneVersoPlayer.Normalize(); // chiama // riga-ok

        // Se il giocatore è vicinissimo (sotto il drone entro 3.5 metri), aggancia subito
        // blocco: controlla se va
        if (distanzaDalPlayer <= 3.5f) // se ok // riga-ok
        { // apre // riga-ok
            return HaLineaDiVistaLibera(origineCono, playerChest, distanzaDalPlayer); // torna val // riga-ok
        } // chiude // riga-ok

        Vector3 forwardLuce = luceDrone != null ? luceDrone.transform.forward : transform.forward; // setta // riga-ok
        float angolo = Vector3.Angle(forwardLuce, direzioneVersoPlayer); // setta // riga-ok
        // blocco: controlla se va
        if (angolo < (angoloVisione / 2f) + 10f) // se ok // riga-ok
        { // apre // riga-ok
            return HaLineaDiVistaLibera(origineCono, playerChest, distanzaDalPlayer); // torna val // riga-ok
        } // chiude // riga-ok
        
        return false;  // torna val // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void EseguiSparo() // roba pub // riga-ok
    { // apre // riga-ok
        Debug.Log("<color=red>[DRONE] Fuoco ingaggiato sul bersaglio!</color>"); // logga // riga-ok
        RiproduciSuono(suonoSparo); // chiama // riga-ok
        
        // blocco: controlla se va
        if (playerDamageable == null) // se ok // riga-ok
        { // apre // riga-ok
            TrovaRiferimentoPlayer(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (playerDamageable != null) // se ok // riga-ok
        { // apre // riga-ok
            SalutePlayer salute = playerTransform != null ? playerTransform.GetComponent<SalutePlayer>() : null; // setta // riga-ok
            float maxHp = salute != null ? salute.puntiVitaMassimi : 100f; // setta // riga-ok
            float dannoCalcolato = maxHp / 3f; // setta // riga-ok
            playerDamageable.SubisciDanno(dannoCalcolato); // chiama // riga-ok
        } // chiude // riga-ok
        
        timerSparo = 0f;  // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnDrawGizmos() // roba pub // riga-ok
    { // apre // riga-ok
        // Aggiornamento del Gizmo per rispecchiare l'orientamento della luce nell'Editor
        Vector3 origineGizmo = luceDrone != null ? luceDrone.transform.position : transform.position; // setta // riga-ok
        Transform transformRiferimento = luceDrone != null ? luceDrone.transform : transform; // setta // riga-ok

        Gizmos.color = Color.yellow; // setta // riga-ok
        Gizmos.DrawWireSphere(origineGizmo, raggioVisione); // chiama // riga-ok
        
        // Calcolo delle linee di sfoltimento del cono basato sul local space della luce
        Vector3 lineaDestra = transformRiferimento.TransformDirection(Quaternion.Euler(0, angoloVisione / 2, 0) * Vector3.forward) * raggioVisione; // setta // riga-ok
        Vector3 lineaSinistra = transformRiferimento.TransformDirection(Quaternion.Euler(0, -angoloVisione / 2, 0) * Vector3.forward) * raggioVisione; // setta // riga-ok
        
        Gizmos.color = Color.red; // setta // riga-ok
        Gizmos.DrawRay(origineGizmo, lineaDestra); // chiama // riga-ok
        Gizmos.DrawRay(origineGizmo, lineaSinistra); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void ApplicaTagUnity() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (applicaTagDroneAutomatico) // se ok // riga-ok
            SectorContainmentTags.ApplyTag(gameObject, SectorContainmentTags.Drone); // chiama // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
