using UnityEngine;
using AsyncronQuest.Anachronism;

[RequireComponent(typeof(Collider))]
public class testAnacronismo : MonoBehaviour, IInteractable
{
    [Header("Dati Temporali Obbligatori")]
    [Tooltip("Riferimento allo Scriptable Object contenente i metadati dell'epoca associata.")]
    [SerializeField] private TemporalTagData tagData;

    [Header("Feedback Visivo (Stato)")]
    [Tooltip("Colore applicato al materiale per indicare l'avvenuta sincronizzazione.")]
    [SerializeField] private Color synchronizedColor = Color.green;

    private Renderer targetRenderer;
    private bool isSynchronized = false;
    private bool isScannerOpen = false; // Dichiarazione della variabile di stato della UI

    private void Awake()
    {
        targetRenderer = GetComponentInChildren<Renderer>();
    }

    public void Interact()
    {
        // Se è già sincronizzato o la UI dello scanner è aperta, interrompe l'esecuzione
        if (isSynchronized || isScannerOpen) return;

        // Validazione formale dei componenti essenziali
        if (tagData == null)
        {
            Debug.LogError($"[ANACRONISMO] Dati 'tagData' (ScriptableObject) mancanti su {gameObject.name}.", this);
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError($"[ANACRONISMO] Impossibile aprire lo scanner su <b>{gameObject.name}</b>: Singleton 'GameManager' non inizializzato o assente nel contesto di esecuzione.", this);
            return;
        }

        // Apertura formale dell'interfaccia utente dello scanner
        isScannerOpen = true;
        AnachronismScannerUI.Open(tagData, CompleteScannerInteraction, CancelScannerInteraction);
    }

    private void CompleteScannerInteraction()
    {
        isScannerOpen = false;
        ExecuteSynchronization();
    }

    private void CancelScannerInteraction()
    {
        isScannerOpen = false;
    }

    /// <summary>
    /// Gestisce la logica interna di estrazione del tag, trasmissione dati al GameManager e aggiornamento visivo.
    /// </summary>
    private void ExecuteSynchronization()
    {
        if (GameManager.Instance == null) return;

        // 1. Salvataggio nativo dei dati nel GameManager
        GameManager.Instance.ExtractTag(tagData.idTag);

        // 2. Aggiornamento dello stato cromatico del materiale (Feedback Visivo)
        if (targetRenderer != null)
        {
            targetRenderer.material.color = synchronizedColor;
        }

        isSynchronized = true;

        Debug.Log($"<color=lime>[SINCRONIZZAZIONE COMPLETATA]</color> Estratto Tag ID: <b>{tagData.idTag}</b> (Epoca: {tagData.epochName}).");
    }
}