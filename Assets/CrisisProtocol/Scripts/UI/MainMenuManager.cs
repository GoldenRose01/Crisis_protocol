using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject panelMain;
    public GameObject panelOptions;

    [Header("Pulsanti")]
    [Tooltip("Pulsante 'Continua' — viene nascosto se non esiste un salvataggio.")]
    public Button pulsanteContinua;

    [Header("Level Select Panel")]
    public GameObject panelLevelSelect;

    [Header("Volume Control")]
    public Slider volumeSlider;
    public Text volumeText;

    private const string MasterVolumePrefKey = "MasterVolume";

    private void Awake()
    {
        float savedVol = PlayerPrefs.GetFloat(MasterVolumePrefKey, 1.0f);
        AudioListener.volume = savedVol;
        if (volumeSlider != null)
        {
            volumeSlider.value = savedVol;
            volumeSlider.onValueChanged.AddListener(SetMasterVolume);
        }
        UpdateVolumeText(savedVol);
    }

    private void Start()
    {
        // Mostra solo il pannello principale all'avvio
        if (panelMain != null) panelMain.SetActive(true);
        if (panelOptions != null) panelOptions.SetActive(false);
        if (panelLevelSelect != null) panelLevelSelect.SetActive(false);

        // Mostra/nasconde il pulsante Continua in base al salvataggio
        if (pulsanteContinua != null)
            pulsanteContinua.gameObject.SetActive(HaSalvataggio());
    }

    /// <summary>Nuova partita — azzera tutto e carica il primo settore.</summary>
    public void StartGame()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.NuovaPartita();
        else
            Debug.LogError("[MENU] GameManager non trovato. Assicurati che sia presente nella scena MainMenu.");
    }

    /// <summary>Continua dal punto salvato — carica l'ultimo settore raggiunto.</summary>
    public void ContinuaPartita()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ResumeSavedGame();
        else
            Debug.LogError("[MENU] GameManager non trovato. Impossibile caricare il salvataggio.");
    }

    /// <summary>Carica uno specifico settore (0 = settore 0, 1 = settore 1, 2 = settore 2).</summary>
    public void CaricaLivello(int index)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.CaricaSettore(index);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene($"settore {index}");
    }

    public void OpenLevelSelect()
    {
        if (panelMain != null) panelMain.SetActive(false);
        if (panelLevelSelect != null) panelLevelSelect.SetActive(true);
    }

    public void CloseLevelSelect()
    {
        if (panelLevelSelect != null) panelLevelSelect.SetActive(false);
        if (panelMain != null) panelMain.SetActive(true);
    }

    public void SetMasterVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat(MasterVolumePrefKey, volume);
        UpdateVolumeText(volume);
    }

    public void ToggleMute()
    {
        if (AudioListener.volume > 0.01f)
        {
            PlayerPrefs.SetFloat("PreMuteVolume", AudioListener.volume);
            SetMasterVolume(0f);
        }
        else
        {
            float restoreVol = PlayerPrefs.GetFloat("PreMuteVolume", 1.0f);
            if (restoreVol <= 0.05f) restoreVol = 1.0f;
            SetMasterVolume(restoreVol);
        }

        if (volumeSlider != null)
            volumeSlider.value = AudioListener.volume;
    }

    private void UpdateVolumeText(float volume)
    {
        if (volumeText != null)
        {
            int percent = Mathf.RoundToInt(volume * 100f);
            volumeText.text = percent <= 0 ? "MUTE" : $"{percent}%";
        }
    }

    public void OpenOptions()
    {
        if (panelMain != null) panelMain.SetActive(false);
        if (panelOptions != null) panelOptions.SetActive(true);
    }

    public void CloseOptions()
    {
        if (panelOptions != null) panelOptions.SetActive(false);
        if (panelMain != null) panelMain.SetActive(true);
    }

    public void QuitGame()
    {
        Debug.Log("Uscita dal gioco...");
        Application.Quit();
    }

    /// <summary>Restituisce true se esiste un file di salvataggio su disco.</summary>
    public bool HaSalvataggio()
    {
        string path = System.IO.Path.Combine(Application.persistentDataPath, "SectorContainment_Save.json");
        return System.IO.File.Exists(path);
    }
}


