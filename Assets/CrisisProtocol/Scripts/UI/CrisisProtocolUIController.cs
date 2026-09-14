// ============================================================================
// Crisis Protocol / Sector Containment - Interfaccia utente
// File: .\Assets\CrisisProtocol\Scripts\UI\CrisisProtocolUIController.cs
// Responsabilita': aggiorna HUD, menu, overlay, gauge, notifiche o schermate di supporto in base agli eventi gameplay.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine; // usa lib // riga-ok
using UnityEngine.SceneManagement; // usa lib // riga-ok

namespace CrisisProtocol.UI // zona cod // riga-ok
{ // apre // riga-ok
    // blocco: classe x roba grossa
    public class CrisisProtocolUIController : MonoBehaviour // classe qui // riga-ok
    { // apre // riga-ok
        [Header("Panels")] // nota unity // riga-ok
        [SerializeField] private GameObject mainMenuPanel; // ok qua // riga-ok
        [SerializeField] private GameObject pauseMenuPanel; // ok qua // riga-ok
        [SerializeField] private GameObject hudPanel; // ok qua // riga-ok
        [SerializeField] private GameObject interactionPopup; // ok qua // riga-ok

        [Header("Optional Scene Names")] // nota unity // riga-ok
        [SerializeField] private string newGameSceneName = "locale"; // setta // riga-ok
        [SerializeField] private string creditsSceneName = string.Empty; // setta // riga-ok

        private bool isPaused; // roba pub // riga-ok

        // blocco: funzione fa cose
        private void Awake() // roba pub // riga-ok
        { // apre // riga-ok
            ShowMainMenu(true); // chiama // riga-ok
            ShowPause(false); // chiama // riga-ok
            ShowHUD(false); // chiama // riga-ok
            ShowInteraction(false); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void Update() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (ModalUIState.IsModalOpen && !isPaused) // se ok // riga-ok
                return; // torna val // riga-ok

            // blocco: controlla se va
            if (Input.GetKeyDown(KeyCode.Escape)) // se ok // riga-ok
            { // apre // riga-ok
                SetPause(!isPaused); // chiama // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public void NewGame() // roba pub // riga-ok
        { // apre // riga-ok
            Time.timeScale = 1f; // setta // riga-ok
            MenuAudioSilencer.SetMenuAudioPaused(false); // chiama // riga-ok
            // blocco: controlla se va
            if (!string.IsNullOrWhiteSpace(newGameSceneName)) // se ok // riga-ok
                SceneManager.LoadScene(newGameSceneName); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public void ResumeGame() => SetPause(false); // roba pub // riga-ok

        // blocco: funzione fa cose
        public void OpenOptions() // roba pub // riga-ok
        { // apre // riga-ok
            Debug.Log("Crisis Protocol: open options panel here."); // logga // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public void OpenCredits() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (!string.IsNullOrWhiteSpace(creditsSceneName)) // se ok // riga-ok
                SceneManager.LoadScene(creditsSceneName); // chiama // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
                Debug.Log("Crisis Protocol credits."); // logga // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public void ExitGame() // roba pub // riga-ok
        { // apre // riga-ok
            Application.Quit(); // chiama // riga-ok
#if UNITY_EDITOR // prep ok // riga-ok
            UnityEditor.EditorApplication.isPlaying = false; // setta // riga-ok
#endif // prep ok // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public void SaveGame() // roba pub // riga-ok
        { // apre // riga-ok
            Debug.Log("Crisis Protocol: connect your save system here."); // logga // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public void LoadGame() // roba pub // riga-ok
        { // apre // riga-ok
            Debug.Log("Crisis Protocol: connect your load system here."); // logga // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public void ShowMainMenu(bool value) // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (mainMenuPanel) mainMenuPanel.SetActive(value); // se ok // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public void ShowPause(bool value) // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (pauseMenuPanel) pauseMenuPanel.SetActive(value); // se ok // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public void ShowHUD(bool value) // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (hudPanel) hudPanel.SetActive(value); // se ok // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public void ShowInteraction(bool value) // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (interactionPopup) interactionPopup.SetActive(value); // se ok // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public void SetPause(bool value) // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (value && !ModalUIState.TryOpen("LegacyPauseMenu")) // se ok // riga-ok
                return; // torna val // riga-ok

            // blocco: controlla se va
            if (!value) // se ok // riga-ok
                ModalUIState.Close("LegacyPauseMenu"); // chiama // riga-ok

            isPaused = value; // setta // riga-ok
            ShowPause(value); // chiama // riga-ok
            ShowHUD(!value); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
