// ============================================================================
// Crisis Protocol / Sector Containment - Missione e contenimento
// File: .\Assets\CrisisProtocol\Scripts\Mission\SectorEmergency\SectorContainmentTags.cs
// Responsabilita': modella credenziali, focolai, portelloni, hazard o parametri di bilanciamento del loop emergenza -> contenimento -> estrazione.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine; // usa lib // riga-ok

public static class SectorContainmentTags // roba pub // riga-ok
{ // apre // riga-ok
    public const string Player = "Player"; // roba pub // riga-ok
    public const string Enemy = "Enemy"; // roba pub // riga-ok
    public const string Drone = "Drone"; // roba pub // riga-ok
    public const string Interactable = "Interactable"; // roba pub // riga-ok
    public const string AccessCredential = "AccessCredential"; // roba pub // riga-ok
    public const string EmergencyHotspot = "EmergencyHotspot"; // roba pub // riga-ok
    public const string QuarantineGate = "QuarantineGate"; // roba pub // riga-ok
    public const string ControlRoom = "ControlRoom"; // roba pub // riga-ok
    public const string StructuralHazard = "StructuralHazard"; // roba pub // riga-ok
    public const string MaintenanceBot = "MaintenanceBot"; // roba pub // riga-ok
    public const string SecurityTerminal = "SecurityTerminal"; // roba pub // riga-ok
    public const string ReactorFault = "ReactorFault"; // roba pub // riga-ok
    public const string GasLeak = "GasLeak"; // roba pub // riga-ok
    public const string ElectricArc = "ElectricArc"; // roba pub // riga-ok

    // blocco: funzione fa cose
    public static void ApplyTag(GameObject target, string tagName) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (target == null || string.IsNullOrWhiteSpace(tagName)) // se ok // riga-ok
            return; // torna val // riga-ok

        // blocco: prova safe
        try // prova // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (!target.CompareTag(tagName)) // se ok // riga-ok
                target.tag = tagName; // setta // riga-ok
        } // chiude // riga-ok
        // blocco: becca errore
        catch (UnityException) // err qui // riga-ok
        { // apre // riga-ok
            Debug.LogWarning($"[TAGS] Il tag '{tagName}' non esiste nel TagManager. Aggiungilo in Project Settings > Tags and Layers.", target); // logga // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
