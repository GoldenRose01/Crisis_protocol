using UnityEngine;

[RequireComponent(typeof(Collider))]
public class QuarantineGate : MonoBehaviour, IInteractable
{
    [Header("Portellone Quarantena")]
    [SerializeField] private bool applicaTagAutomatico = true;
    [SerializeField] private Light statusLight;
    [SerializeField] private Color lockedColor = Color.red;
    [SerializeField] private Color unlockedColor = Color.green;
    [SerializeField] private GameObject lockedVisual;
    [SerializeField] private GameObject unlockedVisual;

    private void OnEnable()
    {
        ApplicaTagUnity();
        MissionManager.OnEstrazioneSbloccata += AggiornaStatoVisivo;
    }

    private void OnValidate()
    {
        ApplicaTagUnity();
    }

    private void OnDisable()
    {
        MissionManager.OnEstrazioneSbloccata -= AggiornaStatoVisivo;
    }

    private void Start()
    {
        AggiornaStatoVisivo(MissionManager.Instance != null && MissionManager.Instance.EstrazioneSbloccata);
    }

    public void Interact()
    {
        if (MissionManager.Instance == null)
        {
            Debug.LogError("[QUARANTENA] MissionManager assente: portellone non utilizzabile.", this);
            return;
        }

        MissionManager.Instance.TentaEstrazione();
    }

    private void AggiornaStatoVisivo(bool unlocked)
    {
        if (statusLight != null)
            statusLight.color = unlocked ? unlockedColor : lockedColor;

        if (lockedVisual != null)
            lockedVisual.SetActive(!unlocked);

        if (unlockedVisual != null)
            unlockedVisual.SetActive(unlocked);
    }

    private void ApplicaTagUnity()
    {
        if (applicaTagAutomatico)
            SectorContainmentTags.ApplyTag(gameObject, SectorContainmentTags.QuarantineGate);
    }
}
