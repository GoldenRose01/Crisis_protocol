using UnityEngine;
using UnityEngine.InputSystem;
using GoldenCast.UI;

/// <summary>
/// Scanner di emergenza (tasto Q): emette un raycast nella direzione della telecamera
/// e rileva EmergencyHotspot nel raggio visivo, evidenziandoli e mostrando il loro stato.
/// </summary>
public class ScannerEmergenza : MonoBehaviour
{
    [Header("Configurazione Scanner")]
    [Tooltip("Punto fisico da cui parte lo scan (es. visore). Se vuoto, usa il centro del corpo.")]
    [SerializeField] private Transform puntoDiOrigine;

    [Tooltip("Portata massima del raggio di scansione in metri.")]
    [SerializeField] private float portataScanner = 10f;

    [Tooltip("Il layer degli oggetti scansionabili (EmergencyHotspot, StructuralHazard).")]
    [SerializeField] private LayerMask layerScansionabile;

    [Header("Feedback Visivo")]
    [Tooltip("Colore del Gizmo di anteprima dell'onda scanner nell'editor.")]
    [SerializeField] private Color coloreScanner = new Color(0f, 1f, 0.8f, 1f);

    [Header("Cooldown")]
    [Tooltip("Secondi di attesa tra una scansione e la successiva.")]
    [SerializeField] private float cooldown = 1.5f;
    private float timerCooldown = 0f;

    private Camera telecameraPrincipale;

    private void Start()
    {
        telecameraPrincipale = Camera.main;
    }

    private void Update()
    {
        // Non operare mentre una UI modale è aperta
        if (ModalUIState.IsModalOpen)
            return;

        // Aggiorna cooldown
        if (timerCooldown > 0f)
            timerCooldown -= Time.deltaTime;

        if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
        {
            EseguiScansione();
        }
    }

    private void EseguiScansione()
    {
        if (timerCooldown > 0f)
        {
            Debug.Log($"<color=grey>[SCANNER] Ricarica in corso... ({timerCooldown:F1}s)</color>");
            return;
        }

        if (telecameraPrincipale == null)
        {
            Debug.LogError("[SCANNER] Main Camera non trovata. Assicurati che la camera abbia il tag 'MainCamera'.");
            return;
        }

        Vector3 origineScansione = puntoDiOrigine != null
            ? puntoDiOrigine.position
            : transform.position + Vector3.up * 1.5f;

        Vector3 direzioneScansione = telecameraPrincipale.transform.forward;

        Debug.Log("<color=cyan>[SCANNER EMERGENZA] Onda di scansione emessa...</color>");

        if (Physics.Raycast(origineScansione, direzioneScansione, out RaycastHit hitInfo, portataScanner, layerScansionabile))
        {
            // Priorità 1: Focolaio di emergenza
            EmergencyHotspot hotspot = hitInfo.collider.GetComponent<EmergencyHotspot>();
            if (hotspot != null)
            {
                Debug.Log($"<color=orange><b>[SCANNER]</b> Focolaio rilevato: {hitInfo.collider.name} | Distanza: {hitInfo.distance:F1}m</color>");
                timerCooldown = cooldown;
                return;
            }

            // Priorità 2: Pericolo strutturale
            StructuralHazard hazard = hitInfo.collider.GetComponent<StructuralHazard>();
            if (hazard != null)
            {
                Debug.Log($"<color=red><b>[SCANNER]</b> Pericolo strutturale rilevato: {hitInfo.collider.name} | Distanza: {hitInfo.distance:F1}m</color>");
                timerCooldown = cooldown;
                return;
            }

            Debug.LogWarning($"[SCANNER] Oggetto rilevato ({hitInfo.collider.name}) non è un target di emergenza.");
        }
        else
        {
            Debug.Log("<color=grey>[SCANNER] Nessun target rilevato nel raggio di scansione.</color>");
        }

        timerCooldown = cooldown;
    }

    private void OnDrawGizmos()
    {
        Vector3 origineGizmo = puntoDiOrigine != null
            ? puntoDiOrigine.position
            : transform.position + Vector3.up * 1.5f;

        Vector3 direzioneGizmo = Application.isPlaying && Camera.main != null
            ? Camera.main.transform.forward
            : transform.forward;

        Gizmos.color = coloreScanner;
        Gizmos.DrawRay(origineGizmo, direzioneGizmo * portataScanner);
    }
}
