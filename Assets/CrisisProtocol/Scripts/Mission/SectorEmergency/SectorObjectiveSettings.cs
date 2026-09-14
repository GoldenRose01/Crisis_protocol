// ============================================================================
// Crisis Protocol / Sector Containment - Missione e contenimento
// File: .\Assets\CrisisProtocol\Scripts\Mission\SectorEmergency\SectorObjectiveSettings.cs
// Responsabilita': modella credenziali, focolai, portelloni, hazard o parametri di bilanciamento del loop emergenza -> contenimento -> estrazione.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using System; // usa lib // riga-ok
using UnityEngine; // usa lib // riga-ok

[Serializable] // nota unity // riga-ok
// blocco: classe x roba grossa
public class SectorObjectiveSettings // classe qui // riga-ok
{ // apre // riga-ok
    [Tooltip("Numero di focolai tecnici da contenere per sbloccare il portellone di quarantena. Se resta a 0, viene calcolato dagli EmergencyHotspot presenti nella scena.")] // nota unity // riga-ok
    [SerializeField] private int totaleFocolaiDaContenere = 0; // setta // riga-ok

    [Tooltip("Se attivo, i focolai presenti in scena impostano automaticamente il totale obiettivi.")] // nota unity // riga-ok
    [SerializeField] private bool contaFocolaiInScena = true; // setta // riga-ok

    public int TotaleFocolaiDaContenere => totaleFocolaiDaContenere; // roba pub // riga-ok

    // blocco: funzione fa cose
    public void InitializeFromScene() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (!contaFocolaiInScena || totaleFocolaiDaContenere > 0) // se ok // riga-ok
            return; // torna val // riga-ok

        totaleFocolaiDaContenere = UnityEngine.Object.FindObjectsByType<EmergencyHotspot>(FindObjectsSortMode.None).Length; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public bool IsQuarantineGateUnlocked(int focolaiContenuti) // roba pub // riga-ok
    { // apre // riga-ok
        return totaleFocolaiDaContenere <= 0 || focolaiContenuti >= totaleFocolaiDaContenere; // torna val // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
