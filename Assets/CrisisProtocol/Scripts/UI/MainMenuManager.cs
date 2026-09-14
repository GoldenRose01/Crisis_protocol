// ============================================================================
// Crisis Protocol / Sector Containment - Interfaccia utente
// File: .\Assets\CrisisProtocol\Scripts\UI\MainMenuManager.cs
// Responsabilita': aggiorna HUD, menu, overlay, gauge, notifiche o schermate di supporto in base agli eventi gameplay.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine; // usa lib // riga-ok
using UnityEngine.UI; // usa lib // riga-ok

// blocco: classe x roba grossa
public class MainMenuManager : MonoBehaviour // classe qui // riga-ok
{ // apre // riga-ok
    [Header("Panels")] // nota unity // riga-ok
    public GameObject panelMain; // roba pub // riga-ok
    public GameObject panelOptions; // roba pub // riga-ok

    [Header("Pulsanti")] // nota unity // riga-ok
    [Tooltip("Pulsante 'Continua' — viene nascosto se non esiste un salvataggio.")] // nota unity // riga-ok
    public Button pulsanteContinua; // roba pub // riga-ok

    [Header("Level Select Panel")] // nota unity // riga-ok
    public GameObject panelLevelSelect; // roba pub // riga-ok

    [Header("Volume Control")] // nota unity // riga-ok
    public Slider volumeSlider; // roba pub // riga-ok
    public Text volumeText; // roba pub // riga-ok

    private const string MasterVolumePrefKey = "MasterVolume"; // roba pub // riga-ok

    // blocco: funzione fa cose
    private void Awake() // roba pub // riga-ok
    { // apre // riga-ok
        float savedVol = PlayerPrefs.GetFloat(MasterVolumePrefKey, 1.0f); // setta // riga-ok
        AudioListener.volume = savedVol; // setta // riga-ok
        // blocco: controlla se va
        if (volumeSlider != null) // se ok // riga-ok
        { // apre // riga-ok
            volumeSlider.value = savedVol; // setta // riga-ok
            volumeSlider.onValueChanged.AddListener(SetMasterVolume); // chiama // riga-ok
        } // chiude // riga-ok
        UpdateVolumeText(savedVol); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void Start() // roba pub // riga-ok
    { // apre // riga-ok
        // Mostra solo il pannello principale all'avvio
        // blocco: controlla se va
        if (panelMain != null) panelMain.SetActive(true); // se ok // riga-ok
        // blocco: controlla se va
        if (panelOptions != null) panelOptions.SetActive(false); // se ok // riga-ok
        // blocco: controlla se va
        if (panelLevelSelect != null) panelLevelSelect.SetActive(false); // se ok // riga-ok

        // Mostra/nasconde il pulsante Continua in base al salvataggio
        // blocco: controlla se va
        if (pulsanteContinua != null) // se ok // riga-ok
            pulsanteContinua.gameObject.SetActive(HaSalvataggio()); // chiama // riga-ok
    } // chiude // riga-ok

    /// <summary>Nuova partita — azzera tutto e carica il primo settore.</summary>
    // blocco: funzione fa cose
    public void StartGame() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (GameManager.Instance != null) // se ok // riga-ok
            GameManager.Instance.NuovaPartita(); // chiama // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
            Debug.LogError("[MENU] GameManager non trovato. Assicurati che sia presente nella scena MainMenu."); // logga // riga-ok
    } // chiude // riga-ok

    /// <summary>Continua dal punto salvato — carica l'ultimo settore raggiunto.</summary>
    // blocco: funzione fa cose
    public void ContinuaPartita() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (GameManager.Instance != null) // se ok // riga-ok
            GameManager.Instance.ResumeSavedGame(); // chiama // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
            Debug.LogError("[MENU] GameManager non trovato. Impossibile caricare il salvataggio."); // logga // riga-ok
    } // chiude // riga-ok

    /// <summary>Carica uno specifico settore (0 = settore 0, 1 = settore 1, 2 = settore 2).</summary>
    // blocco: funzione fa cose
    public void CaricaLivello(int index) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (GameManager.Instance != null) // se ok // riga-ok
            GameManager.Instance.CaricaSettore(index); // chiama // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
            UnityEngine.SceneManagement.SceneManager.LoadScene($"settore {index}"); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void OpenLevelSelect() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (panelMain != null) panelMain.SetActive(false); // se ok // riga-ok
        // blocco: controlla se va
        if (panelLevelSelect != null) panelLevelSelect.SetActive(true); // se ok // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void CloseLevelSelect() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (panelLevelSelect != null) panelLevelSelect.SetActive(false); // se ok // riga-ok
        // blocco: controlla se va
        if (panelMain != null) panelMain.SetActive(true); // se ok // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void SetMasterVolume(float volume) // roba pub // riga-ok
    { // apre // riga-ok
        volume = Mathf.Clamp01(volume); // setta // riga-ok
        AudioListener.volume = volume; // setta // riga-ok
        PlayerPrefs.SetFloat(MasterVolumePrefKey, volume); // chiama // riga-ok
        UpdateVolumeText(volume); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void ToggleMute() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (AudioListener.volume > 0.01f) // se ok // riga-ok
        { // apre // riga-ok
            PlayerPrefs.SetFloat("PreMuteVolume", AudioListener.volume); // chiama // riga-ok
            SetMasterVolume(0f); // chiama // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            float restoreVol = PlayerPrefs.GetFloat("PreMuteVolume", 1.0f); // setta // riga-ok
            // blocco: controlla se va
            if (restoreVol <= 0.05f) restoreVol = 1.0f; // se ok // riga-ok
            SetMasterVolume(restoreVol); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (volumeSlider != null) // se ok // riga-ok
            volumeSlider.value = AudioListener.volume; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void UpdateVolumeText(float volume) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (volumeText != null) // se ok // riga-ok
        { // apre // riga-ok
            int percent = Mathf.RoundToInt(volume * 100f); // setta // riga-ok
            volumeText.text = percent <= 0 ? "MUTE" : $"{percent}%"; // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void OpenOptions() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (panelMain != null) panelMain.SetActive(false); // se ok // riga-ok
        // blocco: controlla se va
        if (panelOptions != null) panelOptions.SetActive(true); // se ok // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void CloseOptions() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (panelOptions != null) panelOptions.SetActive(false); // se ok // riga-ok
        // blocco: controlla se va
        if (panelMain != null) panelMain.SetActive(true); // se ok // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void QuitGame() // roba pub // riga-ok
    { // apre // riga-ok
        Debug.Log("Uscita dal gioco..."); // logga // riga-ok
        Application.Quit(); // chiama // riga-ok
    } // chiude // riga-ok

    /// <summary>Restituisce true se esiste un file di salvataggio su disco.</summary>
    // blocco: funzione fa cose
    public bool HaSalvataggio() // roba pub // riga-ok
    { // apre // riga-ok
        string path = System.IO.Path.Combine(Application.persistentDataPath, "SectorContainment_Save.json"); // setta // riga-ok
        return System.IO.File.Exists(path); // torna val // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok


