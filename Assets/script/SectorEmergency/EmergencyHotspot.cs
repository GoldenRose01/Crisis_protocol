using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EmergencyHotspot : MonoBehaviour, IInteractable
{
    [Header("Dati Focolaio")]
    [SerializeField] private string hotspotId = "REACTOR_FAULT_001";
    [SerializeField] private string requiredCredentialId = "KEYCARD_A01";
    [SerializeField] private bool applicaTagAutomatico = true;

    [Header("Feedback")]
    [SerializeField] private Color criticalColor = new Color(1f, 0.12f, 0.05f);
    [SerializeField] private Color containedColor = Color.green;
    [SerializeField] private ParticleSystem containmentVfx;
    [SerializeField] private GameObject containedStateObject;

    private Renderer targetRenderer;
    private bool contenuto;

    private void Awake()
    {
        ApplicaTagUnity();
        targetRenderer = GetComponentInChildren<Renderer>();
    }

    private void OnValidate()
    {
        ApplicaTagUnity();
    }

    private void Start()
    {
        if (targetRenderer != null)
            targetRenderer.material.color = criticalColor;

        if (containedStateObject != null)
            containedStateObject.SetActive(false);
    }

    public void Interact()
    {
        if (contenuto)
            return;

        if (MissionManager.Instance == null)
        {
            Debug.LogError($"[FOCOLAIO] MissionManager assente: impossibile contenere {hotspotId}.", this);
            return;
        }

        bool successo = MissionManager.Instance.ContieniFocolaio(hotspotId, requiredCredentialId);
        if (!successo)
            return;

        contenuto = true;

        if (targetRenderer != null)
            targetRenderer.material.color = containedColor;

        if (containmentVfx != null)
            containmentVfx.Play();

        if (containedStateObject != null)
            containedStateObject.SetActive(true);
    }

    private void ApplicaTagUnity()
    {
        if (applicaTagAutomatico)
            SectorContainmentTags.ApplyTag(gameObject, SectorContainmentTags.EmergencyHotspot);
    }
}
