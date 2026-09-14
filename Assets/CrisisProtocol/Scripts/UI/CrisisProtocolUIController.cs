// ============================================================================
// Crisis Protocol / Sector Containment - Interfaccia utente
// File: .\Assets\CrisisProtocol\Scripts\UI\CrisisProtocolUIController.cs
// Responsabilita': aggiorna HUD, menu, overlay, gauge, notifiche o schermate di supporto in base agli eventi gameplay.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrisisProtocol.UI
{
    public class CrisisProtocolUIController : MonoBehaviour
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
            Debug.Log("Crisis Protocol: open options panel here.");
        }

        public void OpenCredits()
        {
            if (!string.IsNullOrWhiteSpace(creditsSceneName))
                SceneManager.LoadScene(creditsSceneName);
            else
                Debug.Log("Crisis Protocol credits.");
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
            Debug.Log("Crisis Protocol: connect your save system here.");
        }

        public void LoadGame()
        {
            Debug.Log("Crisis Protocol: connect your load system here.");
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
