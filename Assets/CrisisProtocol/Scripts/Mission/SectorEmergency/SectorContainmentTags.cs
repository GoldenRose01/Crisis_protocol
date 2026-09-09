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

        try
        {
            if (!target.CompareTag(tagName))
                target.tag = tagName;
        }
        catch (UnityException)
        {
            Debug.LogWarning($"[TAGS] Il tag '{tagName}' non esiste nel TagManager. Aggiungilo in Project Settings > Tags and Layers.", target);
        }
    }
}
