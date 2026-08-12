using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    private const string GameplaySceneName = "locale";

    [Header("Panels")]
    public GameObject panelMain;
    public GameObject panelOptions;

    private void Start()
    {
        // Assicuriamoci che all'avvio sia aperto solo il main menu
        panelMain.SetActive(true);
        panelOptions.SetActive(false);
    }

    public void StartGame()
    {
        SceneManager.LoadScene(GameplaySceneName);
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
}
