using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AccessCredentialPickup : MonoBehaviour, IInteractable
{
    [Header("Dati Credenziale")]
    [SerializeField] private string credentialId = "KEYCARD_A01";
    [SerializeField] private string displayName = "Scheda di accesso";
    [SerializeField] private bool applicaTagAutomatico = true;

    [Header("Feedback")]
    [SerializeField] private bool disattivaDopoRaccolta = true;
    [SerializeField] private Color collectedColor = Color.cyan;

    private Renderer targetRenderer;
    private bool raccolta;

    private void Awake()
    {
        ApplicaTagUnity();
        targetRenderer = GetComponentInChildren<Renderer>();
    }

    private void OnValidate()
    {
        ApplicaTagUnity();
    }

    public void Interact()
    {
        if (raccolta)
            return;

        if (MissionManager.Instance == null)
        {
            Debug.LogError($"[CREDENZIALE] MissionManager assente: impossibile registrare {credentialId}.", this);
            return;
        }

        raccolta = MissionManager.Instance.RegistraCredenziale(credentialId);
        if (!raccolta)
            return;

        if (targetRenderer != null)
            targetRenderer.material.color = collectedColor;

        Debug.Log($"<color=cyan>[CREDENZIALE]</color> {displayName} acquisita: {credentialId}");

        if (disattivaDopoRaccolta)
            gameObject.SetActive(false);
    }

    private void ApplicaTagUnity()
    {
        if (applicaTagAutomatico)
            SectorContainmentTags.ApplyTag(gameObject, SectorContainmentTags.AccessCredential);
    }
}
