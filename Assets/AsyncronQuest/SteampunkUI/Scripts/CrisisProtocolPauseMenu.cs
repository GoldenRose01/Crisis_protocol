// ============================================================================
// Crisis Protocol / Sector Containment - UI e feedback AsyncronQuest
// File: .\Assets\AsyncronQuest\SteampunkUI\Scripts\CrisisProtocolPauseMenu.cs
// Responsabilita': fornisce schermate, tooltip, transizioni, menu e feedback visivi integrati nel progetto Crisis Protocol.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using System; // usa lib // riga-ok
using CrisisProtocol.UI; // usa lib // riga-ok
using TMPro; // usa lib // riga-ok
using UnityEngine; // usa lib // riga-ok
using UnityEngine.EventSystems; // usa lib // riga-ok
using UnityEngine.InputSystem; // usa lib // riga-ok
using UnityEngine.SceneManagement; // usa lib // riga-ok
using UnityEngine.UI; // usa lib // riga-ok
using UnityEngine.Video; // usa lib // riga-ok

#if UNITY_EDITOR // prep ok // riga-ok
using UnityEditor; // usa lib // riga-ok
#endif // prep ok // riga-ok

namespace AsyncronQuest.SteampunkUI // zona cod // riga-ok
{ // apre // riga-ok
    // blocco: classe x roba grossa
    public sealed class CrisisProtocolPauseMenu : MonoBehaviour // classe qui // riga-ok
    { // apre // riga-ok
        private const string MainMenuSceneName = "MainMenu-Scene"; // roba pub // riga-ok
        private const string ModalOwner = "PauseMenu"; // roba pub // riga-ok
        private const string InputSystemUiModuleTypeName = "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem"; // roba pub // riga-ok
#if UNITY_EDITOR // prep ok // riga-ok
        private const string DefaultPauseMenuBackgroundPath = "Assets/AsyncronQuest/SteampunkUI/UI_Style/Option_menu.png"; // roba pub // riga-ok
        private const string DefaultMapFramePath = "Assets/AsyncronQuest/SteampunkUI/UI_Style/TacticalMap_Frame.jpg"; // roba pub // riga-ok
        private const string PauseMenuPrefabPath = "Assets/AsyncronQuest/SteampunkUI/Prefabs/CrisisProtocolPauseMenu.prefab"; // roba pub // riga-ok
        private const string DefaultLoadingVideoPath = "Assets/AsyncronQuest/SteampunkUI/UI_Style/Caricamento.mp4"; // roba pub // riga-ok
#endif // prep ok // riga-ok

        [Header("Canvas")] // nota unity // riga-ok
        [SerializeField] private int canvasSortingOrder = 200; // setta // riga-ok
        [SerializeField] private Vector2 canvasReferenceResolution = new Vector2(1920f, 1080f); // setta // riga-ok
        [SerializeField] private Vector4 safePadding = new Vector4(48f, 36f, 48f, 36f); // setta // riga-ok
        [SerializeField] private bool allowUpscale = true; // setta // riga-ok

        [Header("Localization")] // nota unity // riga-ok
        [SerializeField] private string languageCode = "It"; // setta // riga-ok

        [Header("Video Transitions")] // nota unity // riga-ok
        [SerializeField] private VideoClip exitLoadingVideo; // ok qua // riga-ok
        [SerializeField] private int videoSortingOrder = 1000; // setta // riga-ok

        [Header("Map Camera")] // nota unity // riga-ok
        [SerializeField, Min(10f)] private float mapCameraHeight = 150f; // setta // riga-ok
        [SerializeField, Min(5f)] private float mapOrthographicSize = 65f; // setta // riga-ok
        [SerializeField, Min(64)] private int mapTextureSize = 1024; // setta // riga-ok

        [Header("Map Frame Asset")] // nota unity // riga-ok
        [SerializeField] private Sprite mapFrameSprite; // ok qua // riga-ok

        [Header("Indicators")] // nota unity // riga-ok
        [SerializeField, Range(0f, 1f)] private float asincronismo = 1f; // setta // riga-ok
        [SerializeField, Range(0f, 1f)] private float warpRad = 0.05f; // setta // riga-ok
        [SerializeField, Min(1)] private int totalAnachronismTags = 3; // setta // riga-ok
        [SerializeField, Range(0f, 1f)] private float asincronismoReductionPerTag; // ok qua // riga-ok
        [SerializeField] private bool autoCalculateAsincronismo = true; // setta // riga-ok
        [SerializeField] private bool autoCalculateWarpRad = true; // setta // riga-ok
        [SerializeField] private string playerTag = "Player"; // setta // riga-ok
        [SerializeField, Min(1f)] private float leylineDetectionDistance = 300f; // setta // riga-ok
        [SerializeField, Min(0.01f)] private float leylineFullSignalDistance = 1f; // setta // riga-ok
        [SerializeField, Range(0f, 1f)] private float leylineMinimumSignal = 0.05f; // setta // riga-ok

        [Header("Transparency")] // nota unity // riga-ok
        [SerializeField] private Sprite pauseMenuBackgroundSprite; // ok qua // riga-ok
        [SerializeField, Range(0f, 1f)] private float backgroundImageAlpha = 1f; // setta // riga-ok
        [SerializeField] private bool backgroundPreserveAspect; // ok qua // riga-ok
        [SerializeField, Range(0f, 1f)] private float shadeAlpha = 0.74f; // setta // riga-ok
        [SerializeField, Range(0f, 1f)] private float panelAlpha = 0.82f; // setta // riga-ok
        [SerializeField, Range(0f, 1f)] private float buttonAlpha = 0.86f; // setta // riga-ok

        [Header("Left Commands Layout")] // nota unity // riga-ok
        [SerializeField] private RectLayout actionsPanel = RectLayout.Center(new Vector2(-590f, 0f), new Vector2(340f, 360f)); // setta // riga-ok
        [SerializeField] private TextLayout pauseTitle = TextLayout.Center(new Vector2(0f, 112f), new Vector2(280f, 56f), 34f); // setta // riga-ok
        [SerializeField] private ButtonLayout resumeButton = ButtonLayout.Center(new Vector2(0f, 24f), new Vector2(260f, 62f), new Vector2(230f, 46f), 23f); // setta // riga-ok
        [SerializeField] private ButtonLayout exitButton = ButtonLayout.Center(new Vector2(0f, -64f), new Vector2(260f, 62f), new Vector2(230f, 46f), 23f); // setta // riga-ok

        [Header("Map Layout")] // nota unity // riga-ok
        [SerializeField] private RectLayout mapPanel = RectLayout.Center(Vector2.zero, new Vector2(700f, 420f)); // setta // riga-ok
        [SerializeField] private RectLayout mapMask = RectLayout.Stretch(new Vector2(32f, 28f), new Vector2(-32f, -28f)); // setta // riga-ok
        [SerializeField] private RectLayout mapImage = RectLayout.Stretch(); // setta // riga-ok

        [Header("Right Indicators Layout")] // nota unity // riga-ok
        [SerializeField] private RectLayout indicatorsPanel = RectLayout.Center(new Vector2(590f, 0f), new Vector2(360f, 360f)); // setta // riga-ok
        [SerializeField] private TextLayout indicatorsTitle = TextLayout.Center(new Vector2(0f, 122f), new Vector2(300f, 44f), 26f); // setta // riga-ok
        [SerializeField] private IndicatorLayout asincronismoIndicator = IndicatorLayout.Default(new Vector2(0f, 34f)); // setta // riga-ok
        [SerializeField] private IndicatorLayout warpRadIndicator = IndicatorLayout.Default(new Vector2(0f, -82f)); // setta // riga-ok

        private CanvasGroup menuGroup; // roba pub // riga-ok
        private Image asincronismoFill; // roba pub // riga-ok
        private Image warpRadFill; // roba pub // riga-ok
        private TMP_Text asincronismoValueText; // roba pub // riga-ok
        private TMP_Text warpRadValueText; // roba pub // riga-ok
        private bool isPaused; // roba pub // riga-ok
        private bool transitionInProgress; // roba pub // riga-ok
        private RectTransform resumeButtonRect; // roba pub // riga-ok
        private RectTransform exitButtonRect; // roba pub // riga-ok
        private Transform cachedPlayer; // roba pub // riga-ok
        private SceneTopDownMapUI mapUI; // riferimento per abilitare/disabilitare la camera solo quando il menu è aperto // roba pub // riga-ok

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // nota unity // riga-ok
        // blocco: funzione fa cose
        private static void EnsurePauseMenu() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (FindFirstObjectByType<CrisisProtocolPauseMenu>()) // se ok // riga-ok
                return; // torna val // riga-ok

#if UNITY_EDITOR // prep ok // riga-ok
            CrisisProtocolPauseMenu prefab = AssetDatabase.LoadAssetAtPath<CrisisProtocolPauseMenu>(PauseMenuPrefabPath); // setta // riga-ok
            // blocco: controlla se va
            if (prefab) // se ok // riga-ok
            { // apre // riga-ok
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject); // setta // riga-ok
                instance.name = "Crisis Protocol Pause Menu"; // setta // riga-ok
                DontDestroyOnLoad(instance); // chiama // riga-ok
                return; // torna val // riga-ok
            } // chiude // riga-ok
#endif // prep ok // riga-ok

            GameObject root = new GameObject("Crisis Protocol Pause Menu"); // setta // riga-ok
            DontDestroyOnLoad(root); // chiama // riga-ok
            root.AddComponent<CrisisProtocolPauseMenu>(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void Awake() // roba pub // riga-ok
        { // apre // riga-ok
#if UNITY_EDITOR // prep ok // riga-ok
            AssignDefaultEditorAssets(); // chiama // riga-ok
#endif // prep ok // riga-ok
            BuildInterface(); // chiama // riga-ok
            SceneManager.sceneLoaded += OnSceneLoaded; // setta // riga-ok
            GameManager.OnMissioneCompletata += OnMissioneCompletata; // setta // riga-ok
            SetPaused(false, true); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void OnDestroy() // roba pub // riga-ok
        { // apre // riga-ok
            SceneManager.sceneLoaded -= OnSceneLoaded; // setta // riga-ok
            GameManager.OnMissioneCompletata -= OnMissioneCompletata; // setta // riga-ok
        } // chiude // riga-ok

#if UNITY_EDITOR // prep ok // riga-ok
        // blocco: funzione fa cose
        private void OnValidate() // roba pub // riga-ok
        { // apre // riga-ok
            AssignDefaultEditorAssets(); // chiama // riga-ok

            // blocco: controlla se va
            if (Application.isPlaying && isActiveAndEnabled && menuGroup) // se ok // riga-ok
                RebuildPauseMenu(); // chiama // riga-ok
        } // chiude // riga-ok
#endif // prep ok // riga-ok

        // blocco: funzione fa cose
        private void Update() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (IsMainMenuScene()) // se ok // riga-ok
                return; // torna val // riga-ok

            // blocco: controlla se va
            if (ModalUIState.IsModalOpen && !ModalUIState.IsOwner(ModalOwner)) // se ok // riga-ok
                return; // torna val // riga-ok

            Keyboard keyboard = Keyboard.current; // setta // riga-ok
            // blocco: controlla se va
            if (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.mKey.wasPressedThisFrame)) // se ok // riga-ok
                SetPaused(!isPaused); // chiama // riga-ok

            // blocco: controlla se va
            if (isPaused) // se ok // riga-ok
            { // apre // riga-ok
                UpdateIndicatorUI(); // chiama // riga-ok
                HandleDirectPauseMenuClickFallback(); // chiama // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public void Resume() // roba pub // riga-ok
        { // apre // riga-ok
            SetPaused(false); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public void BackToMainMenu() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (transitionInProgress) // se ok // riga-ok
                return; // torna val // riga-ok

            Time.timeScale = 1f; // setta // riga-ok
            MenuAudioSilencer.SetMenuAudioPaused(false); // chiama // riga-ok
            isPaused = false; // setta // riga-ok
            ModalUIState.Close(ModalOwner); // chiama // riga-ok

            // blocco: controlla se va
            if (menuGroup) // se ok // riga-ok
            { // apre // riga-ok
                menuGroup.alpha = 0f; // setta // riga-ok
                menuGroup.interactable = false; // setta // riga-ok
                menuGroup.blocksRaycasts = false; // setta // riga-ok
            } // chiude // riga-ok

            transitionInProgress = true; // setta // riga-ok
            StartCoroutine(SteampunkUIVideoTransition.Play(this, exitLoadingVideo, videoSortingOrder, LoadMainMenuScene)); // corutina // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public void RebuildPauseMenu() // roba pub // riga-ok
        { // apre // riga-ok
            BuildInterface(); // chiama // riga-ok
            SetPaused(isPaused, true); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public void SetPauseMenuBackground(Sprite backgroundSprite) // roba pub // riga-ok
        { // apre // riga-ok
            pauseMenuBackgroundSprite = backgroundSprite; // setta // riga-ok
            RebuildPauseMenu(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public void SetLanguage(string newLanguageCode) // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (string.IsNullOrWhiteSpace(newLanguageCode)) // se ok // riga-ok
                return; // torna val // riga-ok

            languageCode = newLanguageCode; // setta // riga-ok
            SteampunkUILocalization.ClearCache(); // chiama // riga-ok
            RebuildPauseMenu(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public void SetIndicators(float asincronismo01, float warpRad01) // roba pub // riga-ok
        { // apre // riga-ok
            asincronismo = Mathf.Clamp01(asincronismo01); // setta // riga-ok
            warpRad = Mathf.Clamp01(warpRad01); // setta // riga-ok
            UpdateIndicatorUI(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public void SetAsincronismo(float value01) // roba pub // riga-ok
        { // apre // riga-ok
            asincronismo = Mathf.Clamp01(value01); // setta // riga-ok
            UpdateIndicatorUI(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public void SetWarpRad(float value01) // roba pub // riga-ok
        { // apre // riga-ok
            warpRad = Mathf.Clamp01(value01); // setta // riga-ok
            UpdateIndicatorUI(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) // roba pub // riga-ok
        { // apre // riga-ok
            cachedPlayer = null; // setta // riga-ok
            SetPaused(false, true); // chiama // riga-ok
        } // chiude // riga-ok

        // Bridged from GameManager.OnMissioneCompletata (string missionId)
        // blocco: funzione fa cose
        private void OnMissioneCompletata(string missionId) // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (GameManager.Instance != null) // se ok // riga-ok
                RefreshAsincronismoFromResolvedAnachronisms(GameManager.Instance.MissioniCompletateCount); // chiama // riga-ok
            UpdateIndicatorUI(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void SetPaused(bool value, bool force = false) // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (transitionInProgress) // se ok // riga-ok
                return; // torna val // riga-ok

            // blocco: controlla se va
            if (!force && isPaused == value) // se ok // riga-ok
                return; // torna val // riga-ok

            isPaused = value && !IsMainMenuScene(); // setta // riga-ok

            // blocco: controlla se va
            if (isPaused) // se ok // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (!ModalUIState.TryOpen(ModalOwner)) // se ok // riga-ok
                { // apre // riga-ok
                    isPaused = false; // setta // riga-ok
                    return; // torna val // riga-ok
                } // chiude // riga-ok

                UpdateIndicatorUI(); // chiama // riga-ok
            } // chiude // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
            { // apre // riga-ok
                ModalUIState.Close(ModalOwner); // chiama // riga-ok
            } // chiude // riga-ok

            // blocco: controlla se va
            if (menuGroup) // se ok // riga-ok
            { // apre // riga-ok
                menuGroup.alpha = isPaused ? 1f : 0f; // setta // riga-ok
                menuGroup.interactable = isPaused; // setta // riga-ok
                menuGroup.blocksRaycasts = isPaused; // setta // riga-ok
            } // chiude // riga-ok

            // Abilita/disabilita la camera della mappa insieme al menu:
            // evita che renderizzi ogni frame causando l'assertion subMesh.topology.
            // blocco: controlla se va
            if (mapUI != null) // se ok // riga-ok
                mapUI.enabled = isPaused; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private bool IsMainMenuScene() // roba pub // riga-ok
        { // apre // riga-ok
            return SceneManager.GetActiveScene().name == MainMenuSceneName; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void BuildInterface() // roba pub // riga-ok
        { // apre // riga-ok
            ClearGeneratedInterface(); // chiama // riga-ok
            EnsureEventSystem(); // chiama // riga-ok

            Canvas canvas = new GameObject("Crisis Protocol Pause Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>(); // setta // riga-ok
            canvas.transform.SetParent(transform, false); // chiama // riga-ok
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // setta // riga-ok
            canvas.sortingOrder = canvasSortingOrder; // setta // riga-ok

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>(); // setta // riga-ok
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // setta // riga-ok
            scaler.referenceResolution = canvasReferenceResolution; // setta // riga-ok
            scaler.matchWidthOrHeight = 0.5f; // setta // riga-ok

            RectTransform canvasRoot = canvas.GetComponent<RectTransform>(); // setta // riga-ok
            Stretch(canvasRoot); // chiama // riga-ok

            // 1. Sfondo scuro tattico
            Image darkBg = CreateImage("TacticalDarkBackground", canvasRoot, new Color(0.005f, 0.015f, 0.012f, 0.98f)); // setta // riga-ok
            Stretch(darkBg.rectTransform); // chiama // riga-ok
            darkBg.raycastTarget = true; // Blocca i clic sul gioco sottostante // setta // riga-ok

            // 2. Cornice HUD Neon a Pieno Schermo (come sfondo/frame monitor)
            Sprite frameSprite = GetMapFrameSprite(); // setta // riga-ok
            // blocco: controlla se va
            if (frameSprite != null) // se ok // riga-ok
            { // apre // riga-ok
                Image frameBg = CreateImage("Fullscreen_HUD_Frame", canvasRoot, Color.white); // setta // riga-ok
                Stretch(frameBg.rectTransform); // chiama // riga-ok
                frameBg.sprite = frameSprite; // setta // riga-ok
                frameBg.preserveAspect = false; // setta // riga-ok
                frameBg.raycastTarget = false; // setta // riga-ok
            } // chiude // riga-ok

          // 1. Create the map group under the canvas root
            RectTransform mapGroup = CreateRect("Fullscreen_Map_Group", canvasRoot); // setta // riga-ok

            // 2. Set anchors to the center so it doesn't automatically stretch with the screen
            mapGroup.anchorMin = new Vector2(0.5f, 0.5f); // setta // riga-ok
            mapGroup.anchorMax = new Vector2(0.5f, 0.5f); // setta // riga-ok
            mapGroup.pivot     = new Vector2(0.5f, 0.5f); // setta // riga-ok

            // 3. Define your own custom size (Width, Height)
            mapGroup.sizeDelta = new Vector2(1650f, 900f);  // setta // riga-ok

            // 4. Set the position relative to the center (0,0 is dead center)
            mapGroup.anchoredPosition = Vector2.zero; // setta // riga-ok
            // Contenitore posizionato per riempire l'area del monitor con margini puliti
            RectTransform mapContainer = CreateRect("Map_Display_Container", mapGroup); // setta // riga-ok
            Stretch(mapContainer); // chiama // riga-ok
            mapContainer.offsetMin = new Vector2(140f, 85f); // setta // riga-ok
            mapContainer.offsetMax = new Vector2(-140f, -85f); // setta // riga-ok

            RawImage rawMap = new GameObject("Map_TopDown_RawImage", typeof(RectTransform), typeof(RawImage), typeof(SceneTopDownMapUI)).GetComponent<RawImage>(); // setta // riga-ok
            rawMap.transform.SetParent(mapContainer, false); // chiama // riga-ok
            rawMap.color = Color.white; // setta // riga-ok
            rawMap.raycastTarget = false; // setta // riga-ok
            Stretch(rawMap.rectTransform); // chiama // riga-ok

            SceneTopDownMapUI mapUi = rawMap.GetComponent<SceneTopDownMapUI>(); // setta // riga-ok
            mapUi.Configure(null, mapCameraHeight, mapOrthographicSize, mapTextureSize); // chiama // riga-ok
            mapUi.enabled = false; // setta // riga-ok
            mapUI = mapUi; // setta // riga-ok

            // 4. Barra Pulsanti Azione Neon in Basso
            GameObject actionsBar = new GameObject("NeonActionsBar", typeof(RectTransform)); // setta // riga-ok
            actionsBar.transform.SetParent(canvasRoot, false); // chiama // riga-ok
            RectTransform rtActions = actionsBar.GetComponent<RectTransform>(); // setta // riga-ok
            rtActions.anchorMin = new Vector2(0.5f, 0f); // setta // riga-ok
            rtActions.anchorMax = new Vector2(0.5f, 0f); // setta // riga-ok
            rtActions.pivot = new Vector2(0.5f, 0f); // setta // riga-ok
            rtActions.sizeDelta = new Vector2(650f, 60f); // setta // riga-ok
            rtActions.anchoredPosition = new Vector2(0f, 20f); // setta // riga-ok

            resumeButtonRect = AddNeonButton("Btn_Resume", "[ RIPRENDI (ESC) ]", actionsBar.transform, new Vector2(-160f, 22f), new Vector2(290f, 46f), new Color(0.0f, 1.0f, 0.5f), Resume).GetComponent<RectTransform>(); // setta // riga-ok
            exitButtonRect = AddNeonButton("Btn_Exit", "[ MENU PRINCIPALE ]", actionsBar.transform, new Vector2(160f, 22f), new Vector2(290f, 46f), new Color(1.0f, 0.35f, 0.35f), BackToMainMenu).GetComponent<RectTransform>(); // setta // riga-ok

            menuGroup = canvas.gameObject.AddComponent<CanvasGroup>(); // setta // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void ClearGeneratedInterface() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: gira piu volte
            for (int i = transform.childCount - 1; i >= 0; i--) // ciclo x // riga-ok
            { // apre // riga-ok
                GameObject child = transform.GetChild(i).gameObject; // setta // riga-ok
                // blocco: controlla se va
                if (Application.isPlaying) // se ok // riga-ok
                    Destroy(child); // elimina // riga-ok
                // blocco: caso diverso
                else // se no // riga-ok
                    DestroyImmediate(child); // elimina // riga-ok
            } // chiude // riga-ok

            menuGroup = null; // setta // riga-ok
            asincronismoFill = null; // setta // riga-ok
            warpRadFill = null; // setta // riga-ok
            asincronismoValueText = null; // setta // riga-ok
            warpRadValueText = null; // setta // riga-ok
            resumeButtonRect = null; // setta // riga-ok
            exitButtonRect = null; // setta // riga-ok
            mapUI = null; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private Button AddNeonButton(string objectName, string label, Transform parent, Vector2 position, Vector2 size, Color neonColor, UnityEngine.Events.UnityAction action) // roba pub // riga-ok
        { // apre // riga-ok
            GameObject btnObj = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button)); // setta // riga-ok
            btnObj.transform.SetParent(parent, false); // chiama // riga-ok

            RectTransform rect = btnObj.GetComponent<RectTransform>(); // setta // riga-ok
            rect.anchorMin = new Vector2(0.5f, 0.5f); // setta // riga-ok
            rect.anchorMax = new Vector2(0.5f, 0.5f); // setta // riga-ok
            rect.pivot = new Vector2(0.5f, 0.5f); // setta // riga-ok
            rect.anchoredPosition = position; // setta // riga-ok
            rect.sizeDelta = size; // setta // riga-ok

            Image img = btnObj.GetComponent<Image>(); // setta // riga-ok
            img.color = new Color(0.02f, 0.09f, 0.06f, 0.96f); // setta // riga-ok
            img.raycastTarget = true; // setta // riga-ok

            // Bordo neon
            GameObject borderObj = new GameObject("Border", typeof(RectTransform), typeof(Image)); // setta // riga-ok
            borderObj.transform.SetParent(btnObj.transform, false); // chiama // riga-ok
            RectTransform rtBorder = borderObj.GetComponent<RectTransform>(); // setta // riga-ok
            Stretch(rtBorder); // chiama // riga-ok
            rtBorder.offsetMin = new Vector2(-2, -2); // setta // riga-ok
            rtBorder.offsetMax = new Vector2(2, 2); // setta // riga-ok
            Image imgBorder = borderObj.GetComponent<Image>(); // setta // riga-ok
            imgBorder.color = neonColor; // setta // riga-ok
            imgBorder.raycastTarget = false; // setta // riga-ok
            borderObj.transform.SetAsFirstSibling(); // chiama // riga-ok

            Button button = btnObj.GetComponent<Button>(); // setta // riga-ok
            button.interactable = true; // setta // riga-ok
            ColorBlock colors = button.colors; // setta // riga-ok
            colors.normalColor = Color.white; // setta // riga-ok
            colors.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f); // setta // riga-ok
            colors.pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f); // setta // riga-ok
            button.colors = colors; // setta // riga-ok
            button.onClick.AddListener(action); // chiama // riga-ok

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)); // setta // riga-ok
            txtObj.transform.SetParent(btnObj.transform, false); // chiama // riga-ok
            RectTransform rtTxt = txtObj.GetComponent<RectTransform>(); // setta // riga-ok
            Stretch(rtTxt); // chiama // riga-ok
            TextMeshProUGUI txt = txtObj.GetComponent<TextMeshProUGUI>(); // setta // riga-ok
            txt.color = Color.white; // setta // riga-ok
            txt.text = $"<b><color=#{ColorUtility.ToHtmlStringRGB(neonColor)}>{label}</color></b>"; // setta // riga-ok
            txt.fontSize = 18f; // setta // riga-ok
            txt.fontStyle = FontStyles.Bold; // setta // riga-ok
            txt.alignment = TextAlignmentOptions.Center; // setta // riga-ok
            txt.enableWordWrapping = false; // setta // riga-ok
            txt.raycastTarget = false; // setta // riga-ok

            return button; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private Sprite GetMapFrameSprite() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (mapFrameSprite != null) return mapFrameSprite; // se ok // riga-ok

#if UNITY_EDITOR // prep ok // riga-ok
            mapFrameSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DefaultMapFramePath); // setta // riga-ok
            // blocco: controlla se va
            if (mapFrameSprite != null) return mapFrameSprite; // se ok // riga-ok
#endif // prep ok // riga-ok
            return null; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void LoadMainMenuScene() // roba pub // riga-ok
        { // apre // riga-ok
            transitionInProgress = false; // setta // riga-ok
            SceneManager.LoadScene(MainMenuSceneName); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private Image AddIndicator(string objectName, string label, RectTransform parent, IndicatorLayout layout, out TMP_Text valueText) // roba pub // riga-ok
        { // apre // riga-ok
            Image groupImage = CreateImage(objectName, parent, new Color(0.08f, 0.035f, 0.02f, buttonAlpha)); // setta // riga-ok
            groupImage.raycastTarget = false; // setta // riga-ok
            RectTransform group = groupImage.rectTransform; // setta // riga-ok
            ApplyLayout(group, layout.group); // chiama // riga-ok

            AddText(label, group, layout.labelText, new Color(1f, 0.78f, 0.34f, 1f), FontStyles.Bold); // chiama // riga-ok
            valueText = AddText(string.Empty, group, layout.valueText, new Color(0.36f, 1f, 0.45f, 0.95f), FontStyles.Bold); // setta // riga-ok

            RectTransform barGroup = CreateRect(objectName + "_BarGroup", group); // setta // riga-ok
            ApplyLayout(barGroup, RectLayout.Stretch()); // chiama // riga-ok

            Image track = CreateImage(objectName + "_Track", barGroup, new Color(0.02f, 0.025f, 0.02f, 0.92f)); // setta // riga-ok
            track.raycastTarget = false; // setta // riga-ok
            ApplyLayout(track.rectTransform, layout.track); // chiama // riga-ok

            Image fill = CreateImage(objectName + "_Fill", track.rectTransform, new Color(0.36f, 1f, 0.45f, 0.95f)); // setta // riga-ok
            fill.raycastTarget = false; // setta // riga-ok
            RectTransform fillRect = fill.rectTransform; // setta // riga-ok
            fillRect.anchorMin = new Vector2(0f, 0f); // setta // riga-ok
            fillRect.anchorMax = new Vector2(0f, 1f); // setta // riga-ok
            fillRect.pivot = new Vector2(0f, 0.5f); // setta // riga-ok
            fillRect.offsetMin = Vector2.zero; // setta // riga-ok
            fillRect.offsetMax = Vector2.zero; // setta // riga-ok
            return fill; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void UpdateIndicatorUI() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (autoCalculateAsincronismo && GameManager.Instance) // se ok // riga-ok
                RefreshAsincronismoFromResolvedAnachronisms(GameManager.Instance.MissioniCompletateCount); // chiama // riga-ok

            // blocco: controlla se va
            if (autoCalculateWarpRad) // se ok // riga-ok
                warpRad = CalculateWarpRadFromLeylineDistance(); // setta // riga-ok

            ApplyIndicatorValue(asincronismoFill, asincronismoValueText, asincronismo); // chiama // riga-ok
            ApplyIndicatorValue(warpRadFill, warpRadValueText, warpRad); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void RefreshAsincronismoFromResolvedAnachronisms(int resolvedCount) // roba pub // riga-ok
        { // apre // riga-ok
            float reduction = asincronismoReductionPerTag > 0f // setta // riga-ok
                ? asincronismoReductionPerTag // ok qua // riga-ok
                : 1f / Mathf.Max(1, totalAnachronismTags); // chiama // riga-ok

            asincronismo = Mathf.Clamp01(1f - resolvedCount * reduction); // setta // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private float CalculateWarpRadFromLeylineDistance() // roba pub // riga-ok
        { // apre // riga-ok
            Transform player = GetPlayerTransform(); // setta // riga-ok
            // blocco: controlla se va
            if (!player) // se ok // riga-ok
                return leylineMinimumSignal; // torna val // riga-ok

            float distance = GetNearestLeylineDistance(player.position); // setta // riga-ok
            // blocco: controlla se va
            if (float.IsPositiveInfinity(distance)) // se ok // riga-ok
                return leylineMinimumSignal; // torna val // riga-ok

            // blocco: controlla se va
            if (distance <= leylineFullSignalDistance) // se ok // riga-ok
                return 1f; // torna val // riga-ok

            // blocco: controlla se va
            if (distance >= leylineDetectionDistance) // se ok // riga-ok
                return leylineMinimumSignal; // torna val // riga-ok

            float distance01 = Mathf.InverseLerp(leylineDetectionDistance, leylineFullSignalDistance, distance); // setta // riga-ok
            return Mathf.Lerp(leylineMinimumSignal, 1f, distance01); // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private Transform GetPlayerTransform() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (cachedPlayer) // se ok // riga-ok
                return cachedPlayer; // torna val // riga-ok

            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag); // setta // riga-ok
            cachedPlayer = playerObject ? playerObject.transform : null; // setta // riga-ok
            return cachedPlayer; // torna val // riga-ok
        } // chiude // riga-ok

        // LeylineTrigger is not part of this project — returns infinity so warpRad falls back to leylineMinimumSignal.
        // blocco: funzione fa cose
        private static float GetNearestLeylineDistance(Vector3 origin) // roba pub // riga-ok
        { // apre // riga-ok
            return float.PositiveInfinity; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private static void ApplyIndicatorValue(Image fill, TMP_Text valueText, float value01) // roba pub // riga-ok
        { // apre // riga-ok
            value01 = Mathf.Clamp01(value01); // setta // riga-ok

            // blocco: controlla se va
            if (fill) // se ok // riga-ok
            { // apre // riga-ok
                RectTransform rect = fill.rectTransform; // setta // riga-ok
                rect.anchorMax = new Vector2(value01, 1f); // setta // riga-ok
                fill.color = Color.Lerp(new Color(0.36f, 1f, 0.45f, 0.95f), new Color(1f, 0.28f, 0.08f, 0.95f), value01); // setta // riga-ok
            } // chiude // riga-ok

            // blocco: controlla se va
            if (valueText) // se ok // riga-ok
                valueText.text = Mathf.RoundToInt(value01 * 100f) + "%"; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private Button AddButton(string objectName, string label, RectTransform parent, ButtonLayout layout, UnityEngine.Events.UnityAction action) // roba pub // riga-ok
        { // apre // riga-ok
            Image image = CreateImage(objectName, parent, new Color(0.08f, 0.035f, 0.02f, buttonAlpha)); // setta // riga-ok
            image.raycastTarget = true; // setta // riga-ok
            RectTransform rect = image.rectTransform; // setta // riga-ok
            ApplyLayout(rect, layout.rect); // chiama // riga-ok

            Button button = image.gameObject.AddComponent<Button>(); // setta // riga-ok
            button.interactable = true; // setta // riga-ok
            ColorBlock colors = button.colors; // setta // riga-ok
            colors.normalColor = new Color(0.08f, 0.035f, 0.02f, buttonAlpha); // setta // riga-ok
            colors.highlightedColor = new Color(0.38f, 0.18f, 0.08f, 0.94f); // setta // riga-ok
            colors.pressedColor = new Color(0.03f, 0.015f, 0.01f, 0.98f); // setta // riga-ok
            button.colors = colors; // setta // riga-ok
            button.onClick.AddListener(action); // chiama // riga-ok

            AddText(label, rect, layout.labelText, new Color(1f, 0.78f, 0.34f, 1f), FontStyles.Bold); // chiama // riga-ok
            return button; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void HandleDirectPauseMenuClickFallback() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (transitionInProgress) // se ok // riga-ok
                return; // torna val // riga-ok

            // blocco: controlla se va
            if (!WasPrimaryClickPressed(out Vector2 screenPosition)) // se ok // riga-ok
                return; // torna val // riga-ok

            // blocco: controlla se va
            if (RectContainsScreenPoint(resumeButtonRect, screenPosition)) // se ok // riga-ok
            { // apre // riga-ok
                Resume(); // chiama // riga-ok
                return; // torna val // riga-ok
            } // chiude // riga-ok

            // blocco: controlla se va
            if (RectContainsScreenPoint(exitButtonRect, screenPosition)) // se ok // riga-ok
                BackToMainMenu(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private static bool RectContainsScreenPoint(RectTransform rect, Vector2 screenPosition) // roba pub // riga-ok
        { // apre // riga-ok
            return rect && RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition); // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private static bool WasPrimaryClickPressed(out Vector2 screenPosition) // roba pub // riga-ok
        { // apre // riga-ok
            Mouse mouse = Mouse.current; // setta // riga-ok
            // blocco: controlla se va
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) // se ok // riga-ok
            { // apre // riga-ok
                screenPosition = mouse.position.ReadValue(); // setta // riga-ok
                return true; // torna val // riga-ok
            } // chiude // riga-ok

#if ENABLE_LEGACY_INPUT_MANAGER // prep ok // riga-ok
            // blocco: controlla se va
            if (Input.GetMouseButtonDown(0)) // se ok // riga-ok
            { // apre // riga-ok
                screenPosition = Input.mousePosition; // setta // riga-ok
                return true; // torna val // riga-ok
            } // chiude // riga-ok
#endif // prep ok // riga-ok

            screenPosition = Vector2.zero; // setta // riga-ok
            return false; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private static TMP_Text AddText(string text, RectTransform parent, TextLayout layout, Color color, FontStyles style) // roba pub // riga-ok
        { // apre // riga-ok
            TMP_Text label = new GameObject(text + " Text", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>(); // setta // riga-ok
            label.transform.SetParent(parent, false); // chiama // riga-ok
            RectTransform rect = label.GetComponent<RectTransform>(); // setta // riga-ok
            ApplyLayout(rect, layout.rect); // chiama // riga-ok
            label.text = text; // setta // riga-ok
            label.fontSize = layout.fontSize; // setta // riga-ok
            label.fontStyle = style; // setta // riga-ok
            label.color = color; // setta // riga-ok
            label.alignment = TextAlignmentOptions.Center; // setta // riga-ok
            label.enableAutoSizing = true; // setta // riga-ok
            label.fontSizeMin = Mathf.Max(10f, layout.fontSize * 0.6f); // setta // riga-ok
            label.fontSizeMax = layout.fontSize; // setta // riga-ok
            label.raycastTarget = false; // setta // riga-ok
            return label; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private static Image CreateImage(string name, Transform parent, Color color) // roba pub // riga-ok
        { // apre // riga-ok
            Image image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>(); // setta // riga-ok
            image.transform.SetParent(parent, false); // chiama // riga-ok
            image.color = color; // setta // riga-ok
            return image; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private static RectTransform CreateRect(string name, Transform parent) // roba pub // riga-ok
        { // apre // riga-ok
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); // setta // riga-ok
            rect.SetParent(parent, false); // chiama // riga-ok
            return rect; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private static Color WithAlpha(Color color, float alpha) // roba pub // riga-ok
        { // apre // riga-ok
            color.a = Mathf.Clamp01(alpha); // setta // riga-ok
            return color; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private static void ApplyLayout(RectTransform rect, RectLayout layout) // roba pub // riga-ok
        { // apre // riga-ok
            rect.anchorMin = layout.anchorMin; // setta // riga-ok
            rect.anchorMax = layout.anchorMax; // setta // riga-ok
            rect.pivot = layout.pivot; // setta // riga-ok
            rect.sizeDelta = layout.size; // setta // riga-ok
            rect.anchoredPosition = layout.position; // setta // riga-ok

            // blocco: controlla se va
            if (layout.stretchToAnchors) // se ok // riga-ok
            { // apre // riga-ok
                rect.offsetMin = layout.offsetMin; // setta // riga-ok
                rect.offsetMax = layout.offsetMax; // setta // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private static void Stretch(RectTransform rect) // roba pub // riga-ok
        { // apre // riga-ok
            rect.anchorMin = Vector2.zero; // setta // riga-ok
            rect.anchorMax = Vector2.one; // setta // riga-ok
            rect.pivot = new Vector2(0.5f, 0.5f); // setta // riga-ok
            rect.offsetMin = Vector2.zero; // setta // riga-ok
            rect.offsetMax = Vector2.zero; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private static void EnsureEventSystem() // roba pub // riga-ok
        { // apre // riga-ok
            EventSystem eventSystem = EventSystem.current; // setta // riga-ok
            // blocco: controlla se va
            if (!eventSystem) // se ok // riga-ok
                eventSystem = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>(); // setta // riga-ok

            Type inputSystemUiModule = Type.GetType(InputSystemUiModuleTypeName); // setta // riga-ok
            // blocco: controlla se va
            if (inputSystemUiModule != null) // se ok // riga-ok
            { // apre // riga-ok
                Component inputModule = eventSystem.GetComponent(inputSystemUiModule); // setta // riga-ok
                // blocco: controlla se va
                if (!inputModule) // se ok // riga-ok
                    inputModule = eventSystem.gameObject.AddComponent(inputSystemUiModule); // setta // riga-ok

                // blocco: controlla se va
                if (inputModule is Behaviour behaviour) // se ok // riga-ok
                    behaviour.enabled = true; // setta // riga-ok

                inputSystemUiModule.GetMethod("AssignDefaultActions")?.Invoke(inputModule, null); // chiama // riga-ok
            } // chiude // riga-ok
            // blocco: controlla se va
            else if (!eventSystem.GetComponent<StandaloneInputModule>()) // se ok // riga-ok
            { // apre // riga-ok
                eventSystem.gameObject.AddComponent<StandaloneInputModule>(); // chiama // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

#if UNITY_EDITOR // prep ok // riga-ok
        // blocco: funzione fa cose
        private void AssignDefaultEditorAssets() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (!pauseMenuBackgroundSprite) // se ok // riga-ok
                pauseMenuBackgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DefaultPauseMenuBackgroundPath); // setta // riga-ok

            // blocco: controlla se va
            if (!mapFrameSprite) // se ok // riga-ok
                mapFrameSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DefaultMapFramePath); // setta // riga-ok

            // blocco: controlla se va
            if (!exitLoadingVideo) // se ok // riga-ok
                exitLoadingVideo = AssetDatabase.LoadAssetAtPath<VideoClip>(DefaultLoadingVideoPath); // setta // riga-ok
        } // chiude // riga-ok
#endif // prep ok // riga-ok

        [Serializable] // nota unity // riga-ok
        // blocco: classe x roba grossa
        private sealed class RectLayout // classe qui // riga-ok
        { // apre // riga-ok
            public Vector2 anchorMin = new Vector2(0.5f, 0.5f); // roba pub // riga-ok
            public Vector2 anchorMax = new Vector2(0.5f, 0.5f); // roba pub // riga-ok
            public Vector2 pivot = new Vector2(0.5f, 0.5f); // roba pub // riga-ok
            public Vector2 position; // roba pub // riga-ok
            public Vector2 size; // roba pub // riga-ok
            public bool stretchToAnchors; // roba pub // riga-ok
            public Vector2 offsetMin; // roba pub // riga-ok
            public Vector2 offsetMax; // roba pub // riga-ok

            // blocco: funzione fa cose
            public static RectLayout Center(Vector2 position, Vector2 size) // roba pub // riga-ok
            { // apre // riga-ok
                return new RectLayout // torna val // riga-ok
                { // apre // riga-ok
                    position = position, // setta // riga-ok
                    size = size // setta // riga-ok
                }; // ok qua // riga-ok
            } // chiude // riga-ok

            // blocco: funzione fa cose
            public static RectLayout Stretch() // roba pub // riga-ok
            { // apre // riga-ok
                return Stretch(Vector2.zero, Vector2.zero); // torna val // riga-ok
            } // chiude // riga-ok

            // blocco: funzione fa cose
            public static RectLayout Stretch(Vector2 offsetMin, Vector2 offsetMax) // roba pub // riga-ok
            { // apre // riga-ok
                return new RectLayout // torna val // riga-ok
                { // apre // riga-ok
                    anchorMin = Vector2.zero, // setta // riga-ok
                    anchorMax = Vector2.one, // setta // riga-ok
                    pivot = new Vector2(0.5f, 0.5f), // setta // riga-ok
                    stretchToAnchors = true, // setta // riga-ok
                    offsetMin = offsetMin, // setta // riga-ok
                    offsetMax = offsetMax // setta // riga-ok
                }; // ok qua // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        [Serializable] // nota unity // riga-ok
        // blocco: classe x roba grossa
        private sealed class TextLayout // classe qui // riga-ok
        { // apre // riga-ok
            public RectLayout rect = RectLayout.Center(Vector2.zero, new Vector2(200f, 40f)); // roba pub // riga-ok
            public float fontSize = 20f; // roba pub // riga-ok

            // blocco: funzione fa cose
            public static TextLayout Center(Vector2 position, Vector2 size, float fontSize) // roba pub // riga-ok
            { // apre // riga-ok
                return new TextLayout // torna val // riga-ok
                { // apre // riga-ok
                    rect = RectLayout.Center(position, size), // setta // riga-ok
                    fontSize = fontSize // setta // riga-ok
                }; // ok qua // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        [Serializable] // nota unity // riga-ok
        // blocco: classe x roba grossa
        private sealed class ButtonLayout // classe qui // riga-ok
        { // apre // riga-ok
            public RectLayout rect = RectLayout.Center(Vector2.zero, new Vector2(260f, 62f)); // roba pub // riga-ok
            public TextLayout labelText = TextLayout.Center(Vector2.zero, new Vector2(230f, 46f), 23f); // roba pub // riga-ok

            // blocco: funzione fa cose
            public static ButtonLayout Center(Vector2 position, Vector2 size, Vector2 labelSize, float labelFontSize) // roba pub // riga-ok
            { // apre // riga-ok
                return new ButtonLayout // torna val // riga-ok
                { // apre // riga-ok
                    rect = RectLayout.Center(position, size), // setta // riga-ok
                    labelText = TextLayout.Center(Vector2.zero, labelSize, labelFontSize) // setta // riga-ok
                }; // ok qua // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        [Serializable] // nota unity // riga-ok
        // blocco: classe x roba grossa
        private sealed class IndicatorLayout // classe qui // riga-ok
        { // apre // riga-ok
            public RectLayout group = RectLayout.Center(Vector2.zero, new Vector2(300f, 82f)); // roba pub // riga-ok
            public TextLayout labelText = TextLayout.Center(new Vector2(-24f, 18f), new Vector2(190f, 28f), 18f); // roba pub // riga-ok
            public TextLayout valueText = TextLayout.Center(new Vector2(114f, 18f), new Vector2(64f, 28f), 18f); // roba pub // riga-ok
            public RectLayout track = RectLayout.Center(new Vector2(0f, -20f), new Vector2(238f, 16f)); // roba pub // riga-ok

            // blocco: funzione fa cose
            public static IndicatorLayout Default(Vector2 position) // roba pub // riga-ok
            { // apre // riga-ok
                return new IndicatorLayout // torna val // riga-ok
                { // apre // riga-ok
                    group = RectLayout.Center(position, new Vector2(300f, 82f)) // setta // riga-ok
                }; // ok qua // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
