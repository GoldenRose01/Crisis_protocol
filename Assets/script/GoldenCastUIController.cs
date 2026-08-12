using UnityEngine;
using UnityEngine.SceneManagement;

namespace GoldenCast.UI
{
    public class GoldenCastUIController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject interactionPopup;

        [Header("Optional Scene Names")]
        [SerializeField] private string newGameSceneName = "locale";
        [SerializeField] private string creditsSceneName = string.Empty;

        private bool isPaused;

        private void Awake()
        {
            ShowMainMenu(true);
            ShowPause(false);
            ShowHUD(false);
            ShowInteraction(false);
        }

        private void Update()
        {
            if (ModalUIState.IsModalOpen && !isPaused)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetPause(!isPaused);
            }
        }

        public void NewGame()
        {
            Time.timeScale = 1f;
            MenuAudioSilencer.SetMenuAudioPaused(false);
            if (!string.IsNullOrWhiteSpace(newGameSceneName))
                SceneManager.LoadScene(newGameSceneName);
        }

        public void ResumeGame() => SetPause(false);

        public void OpenOptions()
        {
            Debug.Log("GoldenCast: open options panel here.");
        }

        public void OpenCredits()
        {
            if (!string.IsNullOrWhiteSpace(creditsSceneName))
                SceneManager.LoadScene(creditsSceneName);
            else
                Debug.Log("GoldenCast credits.");
        }

        public void ExitGame()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        public void SaveGame()
        {
            Debug.Log("GoldenCast: connect your save system here.");
        }

        public void LoadGame()
        {
            Debug.Log("GoldenCast: connect your load system here.");
        }

        public void ShowMainMenu(bool value)
        {
            if (mainMenuPanel) mainMenuPanel.SetActive(value);
        }

        public void ShowPause(bool value)
        {
            if (pauseMenuPanel) pauseMenuPanel.SetActive(value);
        }

        public void ShowHUD(bool value)
        {
            if (hudPanel) hudPanel.SetActive(value);
        }

        public void ShowInteraction(bool value)
        {
            if (interactionPopup) interactionPopup.SetActive(value);
        }

        public void SetPause(bool value)
        {
            if (value && !ModalUIState.TryOpen("LegacyPauseMenu"))
                return;

            if (!value)
                ModalUIState.Close("LegacyPauseMenu");

            isPaused = value;
            ShowPause(value);
            ShowHUD(!value);
        }
    }
}
