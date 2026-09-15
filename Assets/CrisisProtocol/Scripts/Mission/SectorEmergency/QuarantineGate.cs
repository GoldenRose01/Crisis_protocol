// ============================================================================
// Crisis Protocol / Sector Containment - Missione e contenimento
// File: .\Assets\CrisisProtocol\Scripts\Mission\SectorEmergency\QuarantineGate.cs
// Responsabilita': modella credenziali, focolai, portelloni, hazard o parametri di bilanciamento del loop emergenza -> contenimento -> estrazione.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine;
[RequireComponent(typeof(Collider))]
public class QuarantineGate : MonoBehaviour, IInteractable
{
    [Header("Portellone Quarantena")]
    [SerializeField] private bool applicaTagAutomatico = true;
    [SerializeField] private Light statusLight;
    [SerializeField] private Color lockedColor = Color.red;
    [SerializeField] private Color unlockedColor = Color.green;
    [SerializeField] private GameObject lockedVisual;
    [SerializeField] private GameObject unlockedVisual;
    [Header("Audio")]
    [Tooltip("Suono di avvenuto sblocco a fine crisi.")]
    [SerializeField] private AudioClip suonoSblocco;
    [Tooltip("Suono di interazione / apertura porta di estrazione.")]
    [SerializeField] private AudioClip suonoInterazione;
    [Range(0f, 1f)] [SerializeField] private float volumeAudio = 1.0f;
    [Header("Debug")]
    [Tooltip("Se attivo, il portellone è sempre sbloccato e attivo all'avvio senza richiedere il contenimento dei focolai.")]
    [SerializeField] private bool sbloccaSemprePerDebug = false;
    private bool eraSbloccato = false;
    private void OnEnable()
    {
        ApplicaTagUnity();
        MissionManager.OnEstrazioneSbloccata += AggiornaStatoVisivo;
    }
    private void OnValidate()
    {
        ApplicaTagUnity();
    }
    private void OnDisable()
    {
        MissionManager.OnEstrazioneSbloccata -= AggiornaStatoVisivo;
    }
    private void Start()
    {
        if (sbloccaSemprePerDebug)
        {
            if (MissionManager.Instance != null)
                MissionManager.Instance.ForzaSbloccoEstrazioneDebug();
            else
                AggiornaStatoVisivo(true);
        }
        else
        {
            AggiornaStatoVisivo(MissionManager.Instance != null && MissionManager.Instance.EstrazioneSbloccata);
        }
    }
    public void Interact()
    {
        if (sbloccaSemprePerDebug && MissionManager.Instance != null && !MissionManager.Instance.EstrazioneSbloccata)
        {
            MissionManager.Instance.ForzaSbloccoEstrazioneDebug();
        }
        if (suonoInterazione != null)
        {
            AudioSource.PlayClipAtPoint(suonoInterazione, transform.position, volumeAudio);
        }
        if (MissionManager.Instance == null)
        {
            Debug.LogWarning("[QUARANTENA] MissionManager assente: carico prossimo settore dal GameManager...", this);
            if (GameManager.Instance != null)
                GameManager.Instance.CaricaProssimoSettore();
            return;
        }
        MissionManager.Instance.TentaEstrazione();
    }
    public void AggiornaStatoVisivo(bool unlocked)
    {
        if (unlocked && !eraSbloccato && suonoSblocco != null)
        {
            AudioSource.PlayClipAtPoint(suonoSblocco, transform.position, volumeAudio);
        }
        eraSbloccato = unlocked;
        if (statusLight != null)
            statusLight.color = unlocked ? unlockedColor : lockedColor;
        if (lockedVisual != null)
            lockedVisual.SetActive(!unlocked);
        if (unlockedVisual != null)
            unlockedVisual.SetActive(unlocked);
    }
    private void ApplicaTagUnity()
    {
        if (applicaTagAutomatico)
            SectorContainmentTags.ApplyTag(gameObject, SectorContainmentTags.QuarantineGate);
    }
    [ContextMenu("DEBUG: Sblocca Portellone Ora")]
    public void ForzaSbloccoEditor()
    {
        AggiornaStatoVisivo(true);
        if (MissionManager.Instance != null)
            MissionManager.Instance.ForzaSbloccoEstrazioneDebug();
        Debug.Log("<color=green>[QUARANTENA]</color> Portellone di uscita sbloccato e attivo!");
    }
    [ContextMenu("DEBUG: Forza Estrazione e Prossimo Livello")]
    public void ForzaEstrazioneEditor()
    {
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.ForzaSbloccoEstrazioneDebug();
            MissionManager.Instance.TentaEstrazione();
        }
        else if (GameManager.Instance != null)
        {
            GameManager.Instance.CaricaProssimoSettore();
        }
    }
}