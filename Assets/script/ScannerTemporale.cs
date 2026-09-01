using UnityEngine;
using UnityEngine.InputSystem;
using GoldenCast.UI;

public class ScannerTemporale : MonoBehaviour
{
    [Header("Scanner di Emergenza")]
    [Tooltip("Punto fisico da cui parte lo scan. Se vuoto, usa il centro del corpo.")]
    [SerializeField] private Transform puntoDiOrigine;

    [Tooltip("Portata massima del raggio di scansione in metri.")]
    [SerializeField] private float portataScanner = 6f;

    [Tooltip("Layer degli oggetti scansionabili: credenziali, terminali, focolai o anomalie legacy.")]
    [SerializeField] private LayerMask layerScansionabile;

    private Camera telecameraPrincipale;

    private void Start()
    {
        telecameraPrincipale = Camera.main;
    }

    private void Update()
    {
        if (ModalUIState.IsModalOpen)
            return;

        EseguiScansione();
    }

    private void EseguiScansione()
    {
        if (Keyboard.current == null || !Keyboard.current.qKey.wasPressedThisFrame)
            return;

        Vector3 origineScan = puntoDiOrigine != null ? puntoDiOrigine.position : transform.position + Vector3.up * 1.5f;

        if (telecameraPrincipale == null)
            telecameraPrincipale = Camera.main;

        Vector3 direzioneScan = telecameraPrincipale != null ? telecameraPrincipale.transform.forward : transform.forward;

        Debug.Log("[SCANNER] Analisi emergenza emessa...");

        if (Physics.Raycast(origineScan, direzioneScan, out RaycastHit hitInfo, portataScanner, layerScansionabile))
        {
            AnalizzaBersaglio(hitInfo.collider);
        }
        else
        {
            Debug.Log("<color=grey>[SCANNER] VUOTO.</color> Nessun bersaglio operativo intercettato.");
        }
    }

    private void AnalizzaBersaglio(Collider target)
    {
        if (target == null)
            return;

        AccessCredentialPickup credential = target.GetComponent<AccessCredentialPickup>() ?? target.GetComponentInParent<AccessCredentialPickup>();
        if (credential != null)
        {
            Debug.Log($"<color=cyan>[SCANNER]</color> Credenziale fisica rilevata: <b>{credential.name}</b>. Avvicinarsi e premere E per acquisirla.");
            return;
        }

        EmergencyHotspot hotspot = target.GetComponent<EmergencyHotspot>() ?? target.GetComponentInParent<EmergencyHotspot>();
        if (hotspot != null)
        {
            Debug.Log($"<color=orange>[SCANNER]</color> Focolaio d'emergenza identificato: <b>{hotspot.name}</b>. Richiede procedura di contenimento.");
            return;
        }

        OstacoloCausale obstacle = target.GetComponent<OstacoloCausale>() ?? target.GetComponentInParent<OstacoloCausale>();
        if (obstacle != null)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.RegisterSecuritySignature(obstacle.idCausale);

            Debug.Log($"<color=cyan>[SCANNER]</color> Firma di sicurezza acquisita: <b>{obstacle.idCausale}</b>.");
            return;
        }

        IInteractable interactable = target.GetComponent<IInteractable>() ?? target.GetComponentInParent<IInteractable>();
        if (interactable != null)
        {
            Debug.Log($"<color=cyan>[SCANNER]</color> Oggetto operativo analizzabile rilevato: <b>{target.name}</b>.");
            return;
        }

        Debug.LogWarning($"[SCANNER] {target.name} colpito, ma nessun protocollo di emergenza riconosciuto.");
    }

    private void OnDrawGizmos()
    {
        Vector3 origineGizmo = puntoDiOrigine != null ? puntoDiOrigine.position : transform.position + Vector3.up * 1.5f;
        Vector3 direzioneGizmo = Application.isPlaying && Camera.main != null ? Camera.main.transform.forward : transform.forward;

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(origineGizmo, direzioneGizmo * portataScanner);
    }
}
