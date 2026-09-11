using UnityEngine;
using GoldenCast.UI;

public class DroneRonda : MonoBehaviour
{
    [Header("Pattugliamento")]
    [SerializeField] private bool applicaTagDroneAutomatico = true;
    public Transform[] waypoints;
    public float velocita = 3f;
    private int indiceWaypointAttuale = 0;

    [Header("Sensore Visivo (Cono)")]
    public float raggioVisione = 10f;
    [Range(0, 360)] public float angoloVisione = 45f;
    public LayerMask layerOstacoli;

    [Header("Feedback Visivo (Luce)")]
    [Tooltip("Trascina qui la Spot Light del drone")]
    public Light luceDrone;
    public Color coloreRonda = Color.yellow;
    public Color coloreAllarme = Color.red;

    [Header("Comportamento Post-Emergenza (Fine Crisi)")]
    [Tooltip("Se true, il drone passa in modalità pacifica (luce verde, non spara né dà allarme) quando l'emergenza termina.")]
    [SerializeField] private bool pacificaAFineEmergenza = true;

    [Tooltip("Se true, il drone si spegne e ferma completamente a fine emergenza.")]
    [SerializeField] private bool spegniAFineEmergenza = false;

    [Tooltip("Colore della luce del drone quando il settore è sicuro e l'emergenza è terminata.")]
    [SerializeField] private Color coloreStandbyRisolto = Color.green;

    [Header("Combattimento")]
    [Tooltip("Tempo in secondi tra uno sparo e l'altro")]
    public float cadenzaDiFuoco = 1.5f; 
    private float timerSparo = 0f;

    private Transform playerTransform;
    private IDamageable playerDamageable; 
    private bool playerGiaSegnalato;

    void Start()
    {
        ApplicaTagUnity();
        if (luceDrone == null)
            luceDrone = GetComponentInChildren<Light>();
        TrovaRiferimentoPlayer();
        AllineaLuce();
    }

    private void TrovaRiferimentoPlayer()
    {
        if (playerTransform == null || playerDamageable == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(SectorContainmentTags.Player);
            if (playerObj != null)
            {
                if (playerTransform == null) playerTransform = playerObj.transform;
                if (playerDamageable == null) playerDamageable = playerObj.GetComponent<IDamageable>() ?? playerObj.GetComponentInParent<IDamageable>() ?? playerObj.GetComponentInChildren<IDamageable>();
            }

            if (playerDamageable == null)
            {
                SalutePlayer salute = Object.FindAnyObjectByType<SalutePlayer>();
                if (salute != null)
                {
                    playerDamageable = salute;
                    if (playerTransform == null) playerTransform = salute.transform;
                }
            }
        }
    }

    void OnValidate()
    {
        ApplicaTagUnity();
        if (luceDrone == null)
            luceDrone = GetComponentInChildren<Light>();
        AllineaLuce();
    }

    void Update()
    {
        if (ModalUIState.IsModalOpen)
            return;

        bool emergenzaFinita = MissionManager.Instance != null && MissionManager.Instance.EstrazioneSbloccata;

        if (emergenzaFinita && pacificaAFineEmergenza)
        {
            if (spegniAFineEmergenza)
            {
                if (luceDrone != null) luceDrone.enabled = false;
                return;
            }

            MuoviDrone();
            if (luceDrone != null)
            {
                luceDrone.color = coloreStandbyRisolto;
            }
            return;
        }

        MuoviDrone();
        timerSparo += Time.deltaTime; 
        
        bool playerSottoTiro = ControllaCampoVisivo();

        if (playerSottoTiro && !playerGiaSegnalato)
        {
            playerGiaSegnalato = true;
            Debug.Log("<color=red><b>[DRONE] BERSAGLIO AGGANCIATO NEL CONO OTTICO! ALLARME ROSSO ATTIVO!</b></color>");
            if (MissionManager.Instance != null)
                MissionManager.Instance.RegistraRilevamento(gameObject.name);
        }
        else if (!playerSottoTiro)
        {
            playerGiaSegnalato = false;
        }

        if (luceDrone != null)
        {
            luceDrone.color = playerSottoTiro ? coloreAllarme : coloreRonda;
        }

        if (playerSottoTiro && timerSparo >= cadenzaDiFuoco)
        {
            EseguiSparo();
        }
    }

    private void AllineaLuce()
    {
        if (luceDrone != null)
        {
            luceDrone.range = raggioVisione;
            luceDrone.spotAngle = angoloVisione;
        }
    }

    private void MuoviDrone()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        // Cerca il prossimo waypoint valido nell'array
        Transform target = waypoints[indiceWaypointAttuale];
        if (target == null)
        {
            for (int i = 0; i < waypoints.Length; i++)
            {
                indiceWaypointAttuale = (indiceWaypointAttuale + 1) % waypoints.Length;
                if (waypoints[indiceWaypointAttuale] != null)
                {
                    target = waypoints[indiceWaypointAttuale];
                    break;
                }
            }
        }

        if (target == null) return; // Se tutti i waypoint sono nulli, il drone rimane in hovering stazionario e scansiona

        Vector3 direzione = (target.position - transform.position).normalized;
        transform.position = Vector3.MoveTowards(transform.position, target.position, velocita * Time.deltaTime);

        if (direzione != Vector3.zero)
        {
            Quaternion rotazioneTarget = Quaternion.LookRotation(direzione);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotazioneTarget, 5f * Time.deltaTime);
        }

        if (Vector3.Distance(transform.position, target.position) < 0.4f)
        {
            indiceWaypointAttuale = (indiceWaypointAttuale + 1) % waypoints.Length;
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
            if (hit.transform.root == transform.root || hit.collider.CompareTag(SectorContainmentTags.Enemy) || hit.collider.CompareTag(SectorContainmentTags.Drone))
                continue;

            if (hit.transform.root == playerTransform.root || hit.collider.CompareTag(SectorContainmentTags.Player))
                return true;

            if (hit.collider.isTrigger)
                continue;

            if (hit.distance >= distance - 0.3f)
                return true;

            // Muro solido che blocca il cono di luce
            return false;
        }

        return true;
    }

    private bool ControllaCampoVisivo()
    {
        if (playerTransform == null)
        {
            TrovaRiferimentoPlayer();
            if (playerTransform == null) return false;
        }

        if (luceDrone == null)
            luceDrone = GetComponentInChildren<Light>();

        Vector3 origineCono = (luceDrone != null ? luceDrone.transform.position : transform.position);
        Vector3 playerChest = playerTransform.position + Vector3.up * 1.0f;

        Vector3 direzioneVersoPlayer = (playerChest - origineCono);
        float distanzaDalPlayer = direzioneVersoPlayer.magnitude;
        if (distanzaDalPlayer > raggioVisione) return false;
        direzioneVersoPlayer.Normalize();

        // Se il giocatore è vicinissimo (sotto il drone entro 3.5 metri), aggancia subito
        if (distanzaDalPlayer <= 3.5f)
        {
            return HaLineaDiVistaLibera(origineCono, playerChest, distanzaDalPlayer);
        }

        Vector3 forwardLuce = luceDrone != null ? luceDrone.transform.forward : transform.forward;
        float angolo = Vector3.Angle(forwardLuce, direzioneVersoPlayer);
        if (angolo < (angoloVisione / 2f) + 10f)
        {
            return HaLineaDiVistaLibera(origineCono, playerChest, distanzaDalPlayer);
        }
        
        return false; 
    }

    private void EseguiSparo()
    {
        Debug.Log("<color=red>[DRONE] Fuoco ingaggiato sul bersaglio!</color>");
        
        if (playerDamageable == null)
        {
            TrovaRiferimentoPlayer();
        }

        if (playerDamageable != null)
        {
            SalutePlayer salute = playerTransform != null ? playerTransform.GetComponent<SalutePlayer>() : null;
            float maxHp = salute != null ? salute.puntiVitaMassimi : 100f;
            float dannoCalcolato = maxHp / 3f;
            playerDamageable.SubisciDanno(dannoCalcolato);
        }
        
        timerSparo = 0f; 
    }

    private void OnDrawGizmos()
    {
        // Aggiornamento del Gizmo per rispecchiare l'orientamento della luce nell'Editor
        Vector3 origineGizmo = luceDrone != null ? luceDrone.transform.position : transform.position;
        Transform transformRiferimento = luceDrone != null ? luceDrone.transform : transform;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origineGizmo, raggioVisione);
        
        // Calcolo delle linee di sfoltimento del cono basato sul local space della luce
        Vector3 lineaDestra = transformRiferimento.TransformDirection(Quaternion.Euler(0, angoloVisione / 2, 0) * Vector3.forward) * raggioVisione;
        Vector3 lineaSinistra = transformRiferimento.TransformDirection(Quaternion.Euler(0, -angoloVisione / 2, 0) * Vector3.forward) * raggioVisione;
        
        Gizmos.color = Color.red;
        Gizmos.DrawRay(origineGizmo, lineaDestra);
        Gizmos.DrawRay(origineGizmo, lineaSinistra);
    }

    private void ApplicaTagUnity()
    {
        if (applicaTagDroneAutomatico)
            SectorContainmentTags.ApplyTag(gameObject, SectorContainmentTags.Drone);
    }
}
