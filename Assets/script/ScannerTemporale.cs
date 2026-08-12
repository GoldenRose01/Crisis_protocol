using UnityEngine;
using UnityEngine.InputSystem;
using GoldenCast.UI;

public class ScannerTemporale : MonoBehaviour
{
    [Header("Configurazione Scanner")]
    [Tooltip("Punto fisico da cui parte lo scan (es. visore). Se vuoto, usa il centro del corpo.")]
    [SerializeField] private Transform puntoDiOrigine;
    
    [Tooltip("Portata massima del raggio di scansione in metri.")]
    [SerializeField] private float portataScanner = 6f;
    
    [Tooltip("Il layer specifico degli oggetti che contengono Tag temporali.")]
    [SerializeField] private LayerMask layerScansionabile;

    // Riferimento interno alla telecamera per catturare la sua inclinazione
    private Camera telecameraPrincipale;

    private void Start()
    {
        // Trova in automatico la tua Main Camera all'avvio
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
        if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
        {
            // 1. ORIGINE: Parte dal corpo di Gabriel (o dal MirinoOrigine che hai creato)
            Vector3 origineScan = puntoDiOrigine != null ? puntoDiOrigine.position : transform.position + Vector3.up * 1.5f;
            
            // 2. DIREZIONE (LA MAGIA): Prende esattamente l'inclinazione su/giù/destra/sinistra della telecamera
            Vector3 direzioneScan = telecameraPrincipale.transform.forward;

            Debug.Log("[SCANNER] Onda di acquisizione emessa...");

            // 3. Esecuzione del Raycast fisico inclinato
            if (Physics.Raycast(origineScan, direzioneScan, out RaycastHit hitInfo, portataScanner, layerScansionabile))
            {
                OstacoloCausale ostacolo = hitInfo.collider.GetComponent<OstacoloCausale>();

                if (ostacolo != null)
                {
                    GameManager.Instance.ExtractTag(ostacolo.idCausale); 
                    Debug.Log($"<color=cyan>[SCANNER] TAG ACQUISITO!</color> Dati dell'oggetto <b>{ostacolo.idCausale}</b> registrati con successo.");
                }
                else
                {
                    Debug.LogWarning($"[SCANNER] L'oggetto {hitInfo.collider.name} è stato colpito, ma non possiede un Tag temporale.");
                }
            }
            else
            {
                Debug.Log("<color=grey>[SCANNER] VUOTO.</color> Nessun bersaglio intercettato nella traiettoria visiva.");
            }
        }
    }

    private void OnDrawGizmos()
    {
        Vector3 origineGizmo = puntoDiOrigine != null ? puntoDiOrigine.position : transform.position + Vector3.up * 1.5f;
        
        // Per disegnare la linea azzurra nell'editor in modo corretto
        Vector3 direzioneGizmo = Application.isPlaying && Camera.main != null ? Camera.main.transform.forward : transform.forward;
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(origineGizmo, direzioneGizmo * portataScanner);
    }
}
