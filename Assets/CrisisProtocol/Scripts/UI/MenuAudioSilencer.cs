// ============================================================================
// Crisis Protocol / Sector Containment - Interfaccia utente
// File: .\Assets\CrisisProtocol\Scripts\UI\MenuAudioSilencer.cs
// Responsabilita': aggiorna HUD, menu, overlay, gauge, notifiche o schermate di supporto in base agli eventi gameplay.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using CrisisProtocol.UI; // usa lib // riga-ok
using UnityEngine; // usa lib // riga-ok
using UnityEngine.EventSystems; // usa lib // riga-ok
using UnityEngine.SceneManagement; // usa lib // riga-ok
using UnityEngine.Video; // usa lib // riga-ok

namespace CrisisProtocol.UI // zona cod // riga-ok
{ // apre // riga-ok
    // blocco: classe x roba grossa
    public sealed class MenuAudioSilencer : MonoBehaviour // classe qui // riga-ok
    { // apre // riga-ok
        private const string MainMenuSceneName = "MainMenu-Scene"; // roba pub // riga-ok
        private static MenuAudioSilencer instance; // roba pub // riga-ok

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // nota unity // riga-ok
        // blocco: funzione fa cose
        private static void EnsureInstance() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (instance) // se ok // riga-ok
                return; // torna val // riga-ok

            GameObject root = new GameObject("Crisis Protocol Menu Audio Silencer"); // setta // riga-ok
            DontDestroyOnLoad(root); // chiama // riga-ok
            instance = root.AddComponent<MenuAudioSilencer>(); // setta // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void Awake() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (instance && instance != this) // se ok // riga-ok
            { // apre // riga-ok
                Destroy(gameObject); // elimina // riga-ok
                return; // torna val // riga-ok
            } // chiude // riga-ok

            instance = this; // setta // riga-ok
            DontDestroyOnLoad(gameObject); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void OnEnable() // roba pub // riga-ok
        { // apre // riga-ok
            ModalUIState.ModalOpened += HandleModalOpened; // setta // riga-ok
            ModalUIState.ModalClosed += HandleModalClosed; // setta // riga-ok
            SceneManager.sceneLoaded += HandleSceneLoaded; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void OnDisable() // roba pub // riga-ok
        { // apre // riga-ok
            ModalUIState.ModalOpened -= HandleModalOpened; // setta // riga-ok
            ModalUIState.ModalClosed -= HandleModalClosed; // setta // riga-ok
            SceneManager.sceneLoaded -= HandleSceneLoaded; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void HandleModalOpened(string owner) // roba pub // riga-ok
        { // apre // riga-ok
            SetMenuAudioPaused(true); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void HandleModalClosed(string owner) // roba pub // riga-ok
        { // apre // riga-ok
            SetMenuAudioPaused(ModalUIState.IsModalOpen); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode) // roba pub // riga-ok
        { // apre // riga-ok
            MarkMenuAndVideoAudioSources(); // chiama // riga-ok
            AudioListener.pause = ModalUIState.IsModalOpen; // setta // riga-ok
            // blocco: controlla se va
            if (PlayerPrefs.HasKey("MasterVolume")) // se ok // riga-ok
            { // apre // riga-ok
                AudioListener.volume = PlayerPrefs.GetFloat("MasterVolume", 1f); // setta // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void Update() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (ModalUIState.IsModalOpen) // se ok // riga-ok
                MarkMenuAndVideoAudioSources(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public static void SetMenuAudioPaused(bool paused) // roba pub // riga-ok
        { // apre // riga-ok
            MarkMenuAndVideoAudioSources(); // chiama // riga-ok
            AudioListener.pause = paused; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private static void MarkMenuAndVideoAudioSources() // roba pub // riga-ok
        { // apre // riga-ok
            AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None); // setta // riga-ok
            // blocco: gira piu volte
            foreach (AudioSource source in sources) // ciclo x // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (!source) // se ok // riga-ok
                    continue; // salta // riga-ok

                source.ignoreListenerPause = IsMenuOrVideoAudio(source); // setta // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private static bool IsMenuOrVideoAudio(AudioSource source) // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (source.GetComponent<VideoPlayer>() || source.GetComponentInParent<VideoPlayer>()) // se ok // riga-ok
                return true; // torna val // riga-ok

            // blocco: controlla se va
            if (source.GetComponentInParent<Canvas>() || source.GetComponentInParent<EventSystem>()) // se ok // riga-ok
                return true; // torna val // riga-ok

            string objectName = source.gameObject.name; // setta // riga-ok
            return objectName.Contains("UI") || // torna val // riga-ok
                   objectName.Contains("Menu") || // ok qua // riga-ok
                   objectName.Contains("Video") || // ok qua // riga-ok
                   objectName.Contains("Tooltip"); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
