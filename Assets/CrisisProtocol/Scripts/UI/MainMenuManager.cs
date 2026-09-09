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

    private void Start()
    {
        // Mostra solo il pannello principale all'avvio
        panelMain.SetActive(true);
        panelOptions.SetActive(false);

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

    public void OpenOptions()
    {
        panelMain.SetActive(false);
        panelOptions.SetActive(true);
    }

    public void CloseOptions()
    {
        panelOptions.SetActive(false);
        panelMain.SetActive(true);
    }

    public void QuitGame()
    {
        Debug.Log("Uscita dal gioco...");
        Application.Quit();
    }

    /// <summary>Restituisce true se esiste un file di salvataggio su disco.</summary>
    private bool HaSalvataggio()
    {
        string path = System.IO.Path.Combine(Application.persistentDataPath, "SectorContainment_Save.json");
        return System.IO.File.Exists(path);
    }
}

