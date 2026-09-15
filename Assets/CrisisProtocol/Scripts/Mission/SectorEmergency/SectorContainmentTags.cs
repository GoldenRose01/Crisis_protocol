// ============================================================================
// Crisis Protocol / Sector Containment - Missione e contenimento
// File: .\Assets\CrisisProtocol\Scripts\Mission\SectorEmergency\SectorContainmentTags.cs
// Responsabilita': modella credenziali, focolai, portelloni, hazard o parametri di bilanciamento del loop emergenza -> contenimento -> estrazione.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine;
public static class SectorContainmentTags
{
    public const string Player = "Player";
    public const string Enemy = "Enemy";
    public const string Drone = "Drone";
    public const string Interactable = "Interactable";
    public const string AccessCredential = "AccessCredential";
    public const string EmergencyHotspot = "EmergencyHotspot";
    public const string QuarantineGate = "QuarantineGate";
    public const string ControlRoom = "ControlRoom";
    public const string StructuralHazard = "StructuralHazard";
    public const string MaintenanceBot = "MaintenanceBot";
    public const string SecurityTerminal = "SecurityTerminal";
    public const string ReactorFault = "ReactorFault";
    public const string GasLeak = "GasLeak";
    public const string ElectricArc = "ElectricArc";
    public static void ApplyTag(GameObject target, string tagName)
    {
        if (target == null || string.IsNullOrWhiteSpace(tagName))
            return;
        // Fallback safe: se Unity non trova la risorsa, evitiamo crash e continuiamo puliti.
        try
        {
            if (!target.CompareTag(tagName))
                target.tag = tagName;
        }
        // Log utile in editor: segnala il problema senza bloccare tutta la UI.
        catch (UnityException)
        {
            Debug.LogWarning($"[TAGS] Il tag '{tagName}' non esiste nel TagManager. Aggiungilo in Project Settings > Tags and Layers.", target);
        }
    }
}