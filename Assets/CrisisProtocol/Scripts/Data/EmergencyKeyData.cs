// ============================================================================
// Crisis Protocol / Sector Containment - Dati configurabili
// File: .\Assets\CrisisProtocol\Scripts\Data\EmergencyKeyData.cs
// Responsabilita': espone ScriptableObject o contenitori dati per configurare il gameplay senza modificare codice.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine; // usa lib // riga-ok
using UnityEngine.Video; // usa lib // riga-ok

[CreateAssetMenu(fileName = "NuovaFirmaSicurezza", menuName = "Sector Containment/Firma Scanner")] // nota unity // riga-ok
// blocco: classe x roba grossa
public class EmergencyKeyData : ScriptableObject // classe qui // riga-ok
{ // apre // riga-ok
    [Header("Identificativo di Sistema")] // nota unity // riga-ok
    [Tooltip("ID della firma, credenziale o criticita' rilevata.")] // nota unity // riga-ok
    public string idTag = "TAG_000"; // roba pub // riga-ok

    [Header("Informazioni Scansione")] // nota unity // riga-ok
    [Tooltip("Nome mostrato nello scanner: settore, terminale o anomalia.")] // nota unity // riga-ok
    public string displayName; // roba pub // riga-ok

    [Tooltip("Descrizione operativa della criticita' analizzata.")] // nota unity // riga-ok
    [TextArea(3, 5)] // nota unity // riga-ok
    public string description; // roba pub // riga-ok

    [Header("Video Scanner")] // nota unity // riga-ok
    [Tooltip("Video riprodotto quando il protocollo scanner completa tutti i passaggi.")] // nota unity // riga-ok
    public VideoClip scannerVideo; // roba pub // riga-ok

    public string SecuritySignatureId => idTag; // roba pub // riga-ok
    public string DisplayName => displayName; // roba pub // riga-ok
    public string IncidentDescription => description; // roba pub // riga-ok
} // chiude // riga-ok
