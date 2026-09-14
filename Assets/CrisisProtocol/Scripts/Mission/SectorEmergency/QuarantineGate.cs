// ============================================================================
// Crisis Protocol / Sector Containment - Missione e contenimento
// File: .\Assets\CrisisProtocol\Scripts\Mission\SectorEmergency\QuarantineGate.cs
// Responsabilita': modella credenziali, focolai, portelloni, hazard o parametri di bilanciamento del loop emergenza -> contenimento -> estrazione.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine; // usa lib // riga-ok

[RequireComponent(typeof(Collider))] // nota unity // riga-ok
// blocco: classe x roba grossa
public class QuarantineGate : MonoBehaviour, IInteractable // classe qui // riga-ok
{ // apre // riga-ok
    [Header("Portellone Quarantena")] // nota unity // riga-ok
    [SerializeField] private bool applicaTagAutomatico = true; // setta // riga-ok
    [SerializeField] private Light statusLight; // ok qua // riga-ok
    [SerializeField] private Color lockedColor = Color.red; // setta // riga-ok
    [SerializeField] private Color unlockedColor = Color.green; // setta // riga-ok
    [SerializeField] private GameObject lockedVisual; // ok qua // riga-ok
    [SerializeField] private GameObject unlockedVisual; // ok qua // riga-ok

    [Header("Audio")] // nota unity // riga-ok
    [Tooltip("Suono di avvenuto sblocco a fine crisi.")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoSblocco; // ok qua // riga-ok
    [Tooltip("Suono di interazione / apertura porta di estrazione.")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoInterazione; // ok qua // riga-ok
    [Range(0f, 1f)] [SerializeField] private float volumeAudio = 1.0f; // setta // riga-ok

    [Header("Debug")] // nota unity // riga-ok
    [Tooltip("Se attivo, il portellone è sempre sbloccato e attivo all'avvio senza richiedere il contenimento dei focolai.")] // nota unity // riga-ok
    [SerializeField] private bool sbloccaSemprePerDebug = false; // setta // riga-ok

    private bool eraSbloccato = false; // roba pub // riga-ok

    // blocco: funzione fa cose
    private void OnEnable() // roba pub // riga-ok
    { // apre // riga-ok
        ApplicaTagUnity(); // chiama // riga-ok
        MissionManager.OnEstrazioneSbloccata += AggiornaStatoVisivo; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnValidate() // roba pub // riga-ok
    { // apre // riga-ok
        ApplicaTagUnity(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnDisable() // roba pub // riga-ok
    { // apre // riga-ok
        MissionManager.OnEstrazioneSbloccata -= AggiornaStatoVisivo; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void Start() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (sbloccaSemprePerDebug) // se ok // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (MissionManager.Instance != null) // se ok // riga-ok
                MissionManager.Instance.ForzaSbloccoEstrazioneDebug(); // chiama // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
                AggiornaStatoVisivo(true); // chiama // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            AggiornaStatoVisivo(MissionManager.Instance != null && MissionManager.Instance.EstrazioneSbloccata); // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void Interact() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (sbloccaSemprePerDebug && MissionManager.Instance != null && !MissionManager.Instance.EstrazioneSbloccata) // se ok // riga-ok
        { // apre // riga-ok
            MissionManager.Instance.ForzaSbloccoEstrazioneDebug(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (suonoInterazione != null) // se ok // riga-ok
        { // apre // riga-ok
            AudioSource.PlayClipAtPoint(suonoInterazione, transform.position, volumeAudio); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (MissionManager.Instance == null) // se ok // riga-ok
        { // apre // riga-ok
            Debug.LogWarning("[QUARANTENA] MissionManager assente: carico prossimo settore dal GameManager...", this); // logga // riga-ok
            // blocco: controlla se va
            if (GameManager.Instance != null) // se ok // riga-ok
                GameManager.Instance.CaricaProssimoSettore(); // chiama // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        MissionManager.Instance.TentaEstrazione(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void AggiornaStatoVisivo(bool unlocked) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (unlocked && !eraSbloccato && suonoSblocco != null) // se ok // riga-ok
        { // apre // riga-ok
            AudioSource.PlayClipAtPoint(suonoSblocco, transform.position, volumeAudio); // chiama // riga-ok
        } // chiude // riga-ok
        eraSbloccato = unlocked; // setta // riga-ok

        // blocco: controlla se va
        if (statusLight != null) // se ok // riga-ok
            statusLight.color = unlocked ? unlockedColor : lockedColor; // setta // riga-ok

        // blocco: controlla se va
        if (lockedVisual != null) // se ok // riga-ok
            lockedVisual.SetActive(!unlocked); // chiama // riga-ok

        // blocco: controlla se va
        if (unlockedVisual != null) // se ok // riga-ok
            unlockedVisual.SetActive(unlocked); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void ApplicaTagUnity() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (applicaTagAutomatico) // se ok // riga-ok
            SectorContainmentTags.ApplyTag(gameObject, SectorContainmentTags.QuarantineGate); // chiama // riga-ok
    } // chiude // riga-ok

    [ContextMenu("DEBUG: Sblocca Portellone Ora")] // nota unity // riga-ok
    // blocco: funzione fa cose
    public void ForzaSbloccoEditor() // roba pub // riga-ok
    { // apre // riga-ok
        AggiornaStatoVisivo(true); // chiama // riga-ok
        // blocco: controlla se va
        if (MissionManager.Instance != null) // se ok // riga-ok
            MissionManager.Instance.ForzaSbloccoEstrazioneDebug(); // chiama // riga-ok
        Debug.Log("<color=green>[QUARANTENA]</color> Portellone di uscita sbloccato e attivo!"); // logga // riga-ok
    } // chiude // riga-ok

    [ContextMenu("DEBUG: Forza Estrazione e Prossimo Livello")] // nota unity // riga-ok
    // blocco: funzione fa cose
    public void ForzaEstrazioneEditor() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (MissionManager.Instance != null) // se ok // riga-ok
        { // apre // riga-ok
            MissionManager.Instance.ForzaSbloccoEstrazioneDebug(); // chiama // riga-ok
            MissionManager.Instance.TentaEstrazione(); // chiama // riga-ok
        } // chiude // riga-ok
        // blocco: controlla se va
        else if (GameManager.Instance != null) // se ok // riga-ok
        { // apre // riga-ok
            GameManager.Instance.CaricaProssimoSettore(); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok

