using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(fileName = "NuovoTagTemporale", menuName = "Sistema Temporale/Tag Temporale")]
public class TemporalTagData : ScriptableObject
{
    [Header("Identificativo di Sistema")]
    public string idTag = "TAG_000";

    [Header("Informazioni Scansione")]
    [Tooltip("L'epoca e il luogo mostrati a schermo (es. Berlino Est, 1961)")]
    public string epochName;

    [Tooltip("Una breve descrizione dell'anacronismo analizzato")]
    [TextArea(3, 5)]
    public string description;

    [Header("Video Scanner")]
    [Tooltip("Video riprodotto quando il protocollo scanner completa tutti e tre i passaggi.")]
    public VideoClip anachronismVideo;
}
