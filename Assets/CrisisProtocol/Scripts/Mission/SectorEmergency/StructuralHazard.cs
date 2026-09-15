// ============================================================================
// Crisis Protocol / Sector Containment - Missione e contenimento
// File: .\Assets\CrisisProtocol\Scripts\Mission\SectorEmergency\StructuralHazard.cs
// Responsabilita': modella credenziali, focolai, portelloni, hazard o parametri di bilanciamento del loop emergenza -> contenimento -> estrazione.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine;
[RequireComponent(typeof(Collider))]
public class StructuralHazard : MonoBehaviour
{
    public enum HazardType { ElectricArc, GasLeak, ReactorLeak, SecurityBarrier }
    [Header("Pericolo Ambientale")]
    [SerializeField] private HazardType hazardType = HazardType.ElectricArc;
    [SerializeField] private bool applicaTagAutomatico = true;
    [SerializeField] private float dannoAlSecondo = 18f;
    [SerializeField] private float collassoAggiuntoAlSecondo = 0.75f;
    private void Awake()
    {
        ApplicaTagUnity();
        Collider hazardCollider = GetComponent<Collider>();
        hazardCollider.isTrigger = true;
    }
    private void OnValidate()
    {
        ApplicaTagUnity();
    }
    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag(SectorContainmentTags.Player))
            return;
        float deltaDamage = dannoAlSecondo * Time.deltaTime;
        SalutePlayer playerHealth = other.GetComponent<SalutePlayer>() ?? other.GetComponentInParent<SalutePlayer>();
        if (playerHealth != null)
            playerHealth.SubisciDanno(deltaDamage);
        if (MissionManager.Instance != null)
            MissionManager.Instance.RegistraStressStrutturale(collassoAggiuntoAlSecondo * Time.deltaTime, hazardType.ToString());
    }
    private void ApplicaTagUnity()
    {
        if (!applicaTagAutomatico)
            return;
        string tagName = hazardType switch
        {
            HazardType.ElectricArc => SectorContainmentTags.ElectricArc,
            HazardType.GasLeak => SectorContainmentTags.GasLeak,
            HazardType.ReactorLeak => SectorContainmentTags.ReactorFault,
            _ => SectorContainmentTags.StructuralHazard
        };
        SectorContainmentTags.ApplyTag(gameObject, tagName);
    }
}