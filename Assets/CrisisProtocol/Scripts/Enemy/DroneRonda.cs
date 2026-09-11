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

        GameObject playerObj = GameObject.FindGameObjectWithTag(SectorContainmentTags.Player);
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerDamageable = playerObj.GetComponent<IDamageable>();
        }

        AllineaLuce();
    }

    void OnValidate()
    {
        ApplicaTagUnity();
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
            return; // Non spara né invia allarmi quando la crisi è risolta
        }

        MuoviDrone();
        timerSparo += Time.deltaTime; 
        
        bool playerSottoTiro = ControllaCampoVisivo();

        if (playerSottoTiro && !playerGiaSegnalato)
        {
            playerGiaSegnalato = true;
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
        if (waypoints.Length == 0) return;

        Transform target = waypoints[indiceWaypointAttuale];
        Vector3 direzione = (target.position - transform.position).normalized;

        transform.position = Vector3.MoveTowards(transform.position, target.position, velocita * Time.deltaTime);

        if (direzione != Vector3.zero)
        {
            Quaternion rotazioneTarget = Quaternion.LookRotation(direzione);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotazioneTarget, 5f * Time.deltaTime);
        }

        if (Vector3.Distance(transform.position, target.position) < 0.2f)
        {
            indiceWaypointAttuale = (indiceWaypointAttuale + 1) % waypoints.Length;
        }
    }

    private bool ControllaCampoVisivo()
    {
        if (playerTransform == null) return false;

        // ACCOPPIAMENTO RIGIDO: Usiamo i dati traslazionali e rotazionali della luce, non del corpo del drone
        Vector3 origineCono = luceDrone != null ? luceDrone.transform.position : transform.position;
        Vector3 direzioneCono = luceDrone != null ? luceDrone.transform.forward : transform.forward;

        Vector3 direzioneVersoPlayer = (playerTransform.position - origineCono).normalized;
        float distanzaDalPlayer = Vector3.Distance(origineCono, playerTransform.position);

        if (distanzaDalPlayer < raggioVisione)
        {
            // Calcolo dell'angolo basato sul vettore forward locale della Spot Light
            float angolo = Vector3.Angle(direzioneCono, direzioneVersoPlayer);
            if (angolo < angoloVisione / 2f)
            {
                if (!Physics.Raycast(origineCono, direzioneVersoPlayer, out RaycastHit hit, distanzaDalPlayer, layerOstacoli))
                {
                    return true; 
                }
            }
        }
        
        return false; 
    }

    private void EseguiSparo()
    {
        Debug.Log("<color=red>[DRONE] Fuoco ingaggiato sul bersaglio!</color>");
        
        if (playerDamageable != null)
        {
            SalutePlayer salute = playerTransform.GetComponent<SalutePlayer>();
            if (salute != null)
            {
                float dannoCalcolato = salute.puntiVitaMassimi / 3f;
                playerDamageable.SubisciDanno(dannoCalcolato);
            }
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
