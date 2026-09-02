using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(fileName = "NuovaFirmaSicurezza", menuName = "Sector Containment/Firma Scanner")]
public class EmergencyKeyData : ScriptableObject
{
    [Header("Identificativo di Sistema")]
    [Tooltip("ID della firma, credenziale o criticita' rilevata.")]
    public string idTag = "TAG_000";

    [Header("Informazioni Scansione")]
    [Tooltip("Nome mostrato nello scanner: settore, terminale o anomalia.")]
    public string displayName;

    [Tooltip("Descrizione operativa della criticita' analizzata.")]
    [TextArea(3, 5)]
    public string description;

    [Header("Video Scanner")]
    [Tooltip("Video riprodotto quando il protocollo scanner completa tutti i passaggi.")]
    public VideoClip scannerVideo;

    public string SecuritySignatureId => idTag;
    public string DisplayName => displayName;
    public string IncidentDescription => description;
}
