// ============================================================================
// Crisis Protocol / Sector Containment - Missione e contenimento
// File: .\Assets\CrisisProtocol\Scripts\Mission\SectorEmergency\StructuralHazard.cs
// Responsabilita': modella credenziali, focolai, portelloni, hazard o parametri di bilanciamento del loop emergenza -> contenimento -> estrazione.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine; // usa lib // riga-ok

[RequireComponent(typeof(Collider))] // nota unity // riga-ok
// blocco: classe x roba grossa
public class StructuralHazard : MonoBehaviour // classe qui // riga-ok
{ // apre // riga-ok
    // blocco: scelte rapide
    public enum HazardType { ElectricArc, GasLeak, ReactorLeak, SecurityBarrier } // enum val // riga-ok

    [Header("Pericolo Ambientale")] // nota unity // riga-ok
    [SerializeField] private HazardType hazardType = HazardType.ElectricArc; // setta // riga-ok
    [SerializeField] private bool applicaTagAutomatico = true; // setta // riga-ok
    [SerializeField] private float dannoAlSecondo = 18f; // setta // riga-ok
    [SerializeField] private float collassoAggiuntoAlSecondo = 0.75f; // setta // riga-ok

    // blocco: funzione fa cose
    private void Awake() // roba pub // riga-ok
    { // apre // riga-ok
        ApplicaTagUnity(); // chiama // riga-ok
        Collider hazardCollider = GetComponent<Collider>(); // setta // riga-ok
        hazardCollider.isTrigger = true; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnValidate() // roba pub // riga-ok
    { // apre // riga-ok
        ApplicaTagUnity(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnTriggerStay(Collider other) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (!other.CompareTag(SectorContainmentTags.Player)) // se ok // riga-ok
            return; // torna val // riga-ok

        float deltaDamage = dannoAlSecondo * Time.deltaTime; // setta // riga-ok
        SalutePlayer playerHealth = other.GetComponent<SalutePlayer>() ?? other.GetComponentInParent<SalutePlayer>(); // setta // riga-ok

        // blocco: controlla se va
        if (playerHealth != null) // se ok // riga-ok
            playerHealth.SubisciDanno(deltaDamage); // chiama // riga-ok

        // blocco: controlla se va
        if (MissionManager.Instance != null) // se ok // riga-ok
            MissionManager.Instance.RegistraStressStrutturale(collassoAggiuntoAlSecondo * Time.deltaTime, hazardType.ToString()); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void ApplicaTagUnity() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (!applicaTagAutomatico) // se ok // riga-ok
            return; // torna val // riga-ok

        string tagName = hazardType switch // setta // riga-ok
        { // apre // riga-ok
            HazardType.ElectricArc => SectorContainmentTags.ElectricArc, // setta // riga-ok
            HazardType.GasLeak => SectorContainmentTags.GasLeak, // setta // riga-ok
            HazardType.ReactorLeak => SectorContainmentTags.ReactorFault, // setta // riga-ok
            _ => SectorContainmentTags.StructuralHazard // setta // riga-ok
        }; // ok qua // riga-ok

        SectorContainmentTags.ApplyTag(gameObject, tagName); // chiama // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
