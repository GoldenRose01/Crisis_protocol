// ============================================================================
// Crisis Protocol / Sector Containment - Missione e contenimento
// File: .\Assets\CrisisProtocol\Scripts\Mission\SectorEmergency\SectorObjectiveSettings.cs
// Responsabilita': modella credenziali, focolai, portelloni, hazard o parametri di bilanciamento del loop emergenza -> contenimento -> estrazione.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using System;
using UnityEngine;

[Serializable]
public class SectorObjectiveSettings
{
    [Tooltip("Numero di focolai tecnici da contenere per sbloccare il portellone di quarantena. Se resta a 0, viene calcolato dagli EmergencyHotspot presenti nella scena.")]
    [SerializeField] private int totaleFocolaiDaContenere = 0;

    [Tooltip("Se attivo, i focolai presenti in scena impostano automaticamente il totale obiettivi.")]
    [SerializeField] private bool contaFocolaiInScena = true;

    public int TotaleFocolaiDaContenere => totaleFocolaiDaContenere;

    public void InitializeFromScene()
    {
        if (!contaFocolaiInScena || totaleFocolaiDaContenere > 0)
            return;

        totaleFocolaiDaContenere = UnityEngine.Object.FindObjectsByType<EmergencyHotspot>(FindObjectsSortMode.None).Length;
    }

    public bool IsQuarantineGateUnlocked(int focolaiContenuti)
    {
        return totaleFocolaiDaContenere <= 0 || focolaiContenuti >= totaleFocolaiDaContenere;
    }
}
