// ============================================================================
// Crisis Protocol / Sector Containment - Interfaccia utente
// File: .\Assets\CrisisProtocol\Scripts\UI\MenuAudioSilencer.cs
// Responsabilita': aggiorna HUD, menu, overlay, gauge, notifiche o schermate di supporto in base agli eventi gameplay.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using CrisisProtocol.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
namespace CrisisProtocol.UI
{
    public sealed class MenuAudioSilencer : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu-Scene";
        private static MenuAudioSilencer instance;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstance()
        {
            if (instance)
                return;
            GameObject root = new GameObject("Crisis Protocol Menu Audio Silencer");
            DontDestroyOnLoad(root);
            instance = root.AddComponent<MenuAudioSilencer>();
        }
        private void Awake()
        {
            if (instance && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        private void OnEnable()
        {
            ModalUIState.ModalOpened += HandleModalOpened;
            ModalUIState.ModalClosed += HandleModalClosed;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }
        private void OnDisable()
        {
            ModalUIState.ModalOpened -= HandleModalOpened;
            ModalUIState.ModalClosed -= HandleModalClosed;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }
        private void HandleModalOpened(string owner)
        {
            SetMenuAudioPaused(true);
        }
        private void HandleModalClosed(string owner)
        {
            SetMenuAudioPaused(ModalUIState.IsModalOpen);
        }
        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            MarkMenuAndVideoAudioSources();
            AudioListener.pause = ModalUIState.IsModalOpen;
            if (PlayerPrefs.HasKey("MasterVolume"))
            {
                AudioListener.volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            }
        }
        private void Update()
        {
            if (ModalUIState.IsModalOpen)
                MarkMenuAndVideoAudioSources();
        }
        public static void SetMenuAudioPaused(bool paused)
        {
            MarkMenuAndVideoAudioSources();
            AudioListener.pause = paused;
        }
        private static void MarkMenuAndVideoAudioSources()
        {
            AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            foreach (AudioSource source in sources)
            {
                if (!source)
                    continue;
                source.ignoreListenerPause = IsMenuOrVideoAudio(source);
            }
        }
        private static bool IsMenuOrVideoAudio(AudioSource source)
        {
            if (source.GetComponent<VideoPlayer>() || source.GetComponentInParent<VideoPlayer>())
                return true;
            if (source.GetComponentInParent<Canvas>() || source.GetComponentInParent<EventSystem>())
                return true;
            string objectName = source.gameObject.name;
            return objectName.Contains("UI") ||
                   objectName.Contains("Menu") ||
                   objectName.Contains("Video") ||
                   objectName.Contains("Tooltip");
        }
    }
}