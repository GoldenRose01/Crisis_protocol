using UnityEngine;
using AsyncronQuest.Anachronism;

[RequireComponent(typeof(Collider))]
public class testAnacronismo : MonoBehaviour, IInteractable
{
    [Header("Dati Scansione Obbligatori")]
    [Tooltip("Riferimento allo Scriptable Object contenente i metadati della firma/criticita' scansionata.")]
    [SerializeField] private TemporalTagData tagData;

    [Header("Feedback Visivo")]
    [Tooltip("Colore applicato al materiale per indicare l'avvenuta acquisizione.")]
    [SerializeField] private Color synchronizedColor = Color.green;

    private Renderer targetRenderer;
    private bool isSynchronized;
    private bool isScannerOpen;

    private void Awake()
    {
        targetRenderer = GetComponentInChildren<Renderer>();
    }

    public void Interact()
    {
        if (isSynchronized || isScannerOpen)
            return;

        if (tagData == null)
        {
            Debug.LogError($"[SCANNER] Dati scansione mancanti su {gameObject.name}.", this);
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError($"[SCANNER] GameManager assente: impossibile aprire analisi su <b>{gameObject.name}</b>.", this);
            return;
        }

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

    private void ExecuteSynchronization()
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.RegisterSecuritySignature(tagData.SecuritySignatureId);

        if (targetRenderer != null)
            targetRenderer.material.color = synchronizedColor;

        isSynchronized = true;

        Debug.Log($"<color=lime>[SCANSIONE COMPLETATA]</color> Firma acquisita: <b>{tagData.SecuritySignatureId}</b> ({tagData.DisplayName}).");
    }
}
