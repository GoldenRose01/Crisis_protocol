using System;
using GoldenCast.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AsyncronQuest.SteampunkUI
{
    public sealed class GoldenCastPauseMenu : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu-Scene";
        private const string ModalOwner = "PauseMenu";
        private const string InputSystemUiModuleTypeName = "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem";
#if UNITY_EDITOR
        private const string DefaultPauseMenuBackgroundPath = "Assets/AsyncronQuest/SteampunkUI/UI_Style/Option_menu.png";
        private const string PauseMenuPrefabPath = "Assets/AsyncronQuest/SteampunkUI/Prefabs/GoldenCastPauseMenu.prefab";
        private const string DefaultLoadingVideoPath = "Assets/AsyncronQuest/SteampunkUI/UI_Style/Caricamento.mp4";
#endif

        [Header("Canvas")]
        [SerializeField] private int canvasSortingOrder = 200;
        [SerializeField] private Vector2 canvasReferenceResolution = new Vector2(1920f, 1080f);
        [SerializeField] private Vector4 safePadding = new Vector4(48f, 36f, 48f, 36f);
        [SerializeField] private bool allowUpscale = true;

        [Header("Localization")]
        [SerializeField] private string languageCode = "It";

        [Header("Video Transitions")]
        [SerializeField] private VideoClip exitLoadingVideo;
        [SerializeField] private int videoSortingOrder = 1000;

        [Header("Map Camera")]
        [SerializeField, Min(10f)] private float mapCameraHeight = 120f;
        [SerializeField, Min(5f)] private float mapOrthographicSize = 48f;
        [SerializeField, Min(64)] private int mapTextureSize = 512;

        [Header("Indicators")]
        [SerializeField, Range(0f, 1f)] private float asincronismo = 1f;
        [SerializeField, Range(0f, 1f)] private float warpRad = 0.05f;
        [SerializeField, Min(1)] private int totalAnachronismTags = 3;
        [SerializeField, Range(0f, 1f)] private float asincronismoReductionPerTag;
        [SerializeField] private bool autoCalculateAsincronismo = true;
        [SerializeField] private bool autoCalculateWarpRad = true;
        [SerializeField] private string playerTag = "Player";
        [SerializeField, Min(1f)] private float leylineDetectionDistance = 300f;
        [SerializeField, Min(0.01f)] private float leylineFullSignalDistance = 1f;
        [SerializeField, Range(0f, 1f)] private float leylineMinimumSignal = 0.05f;

        [Header("Transparency")]
        [SerializeField] private Sprite pauseMenuBackgroundSprite;
        [SerializeField, Range(0f, 1f)] private float backgroundImageAlpha = 1f;
        [SerializeField] private bool backgroundPreserveAspect;
        [SerializeField, Range(0f, 1f)] private float shadeAlpha = 0.74f;
        [SerializeField, Range(0f, 1f)] private float panelAlpha = 0.82f;
        [SerializeField, Range(0f, 1f)] private float buttonAlpha = 0.86f;

        [Header("Left Commands Layout")]
        [SerializeField] private RectLayout actionsPanel = RectLayout.Center(new Vector2(-570f, 0f), new Vector2(360f, 360f));
        [SerializeField] private TextLayout pauseTitle = TextLayout.Center(new Vector2(0f, 112f), new Vector2(280f, 56f), 34f);
        [SerializeField] private ButtonLayout resumeButton = ButtonLayout.Center(new Vector2(0f, 24f), new Vector2(260f, 62f), new Vector2(230f, 46f), 23f);
        [SerializeField] private ButtonLayout exitButton = ButtonLayout.Center(new Vector2(0f, -64f), new Vector2(260f, 62f), new Vector2(230f, 46f), 23f);

        [Header("Map Layout")]
        [SerializeField] private RectLayout mapPanel = RectLayout.Center(Vector2.zero, new Vector2(560f, 560f));
        [SerializeField] private RectLayout mapMask = RectLayout.Stretch(new Vector2(66f, 82f), new Vector2(-66f, -66f));
        [SerializeField] private RectLayout mapImage = RectLayout.Stretch();

        [Header("Right Indicators Layout")]
        [SerializeField] private RectLayout indicatorsPanel = RectLayout.Center(new Vector2(570f, 0f), new Vector2(380f, 360f));
        [SerializeField] private TextLayout indicatorsTitle = TextLayout.Center(new Vector2(0f, 122f), new Vector2(300f, 44f), 26f);
        [SerializeField] private IndicatorLayout asincronismoIndicator = IndicatorLayout.Default(new Vector2(0f, 34f));
        [SerializeField] private IndicatorLayout warpRadIndicator = IndicatorLayout.Default(new Vector2(0f, -82f));

        private CanvasGroup menuGroup;
        private Image asincronismoFill;
        private Image warpRadFill;
        private TMP_Text asincronismoValueText;
        private TMP_Text warpRadValueText;
        private bool isPaused;
        private bool transitionInProgress;
        private RectTransform resumeButtonRect;
        private RectTransform exitButtonRect;
        private Transform cachedPlayer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsurePauseMenu()
        {
            if (FindFirstObjectByType<GoldenCastPauseMenu>())
                return;

#if UNITY_EDITOR
            GoldenCastPauseMenu prefab = AssetDatabase.LoadAssetAtPath<GoldenCastPauseMenu>(PauseMenuPrefabPath);
            if (prefab)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject);
                instance.name = "GoldenCast Pause Menu";
                DontDestroyOnLoad(instance);
                return;
            }
#endif

            GameObject root = new GameObject("GoldenCast Pause Menu");
            DontDestroyOnLoad(root);
            root.AddComponent<GoldenCastPauseMenu>();
        }

        private void Awake()
        {
#if UNITY_EDITOR
            AssignDefaultEditorAssets();
#endif
            BuildInterface();
            SceneManager.sceneLoaded += OnSceneLoaded;
            GameManager.OnAnachronismResolved += OnAnachronismResolved;
            SetPaused(false, true);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            GameManager.OnAnachronismResolved -= OnAnachronismResolved;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            AssignDefaultEditorAssets();

            if (Application.isPlaying && isActiveAndEnabled && menuGroup)
                RebuildPauseMenu();
        }
#endif

        private void Update()
        {
            if (IsMainMenuScene())
                return;

            if (ModalUIState.IsModalOpen && !ModalUIState.IsOwner(ModalOwner))
                return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.mKey.wasPressedThisFrame))
                SetPaused(!isPaused);

            if (isPaused)
            {
                UpdateIndicatorUI();
                HandleDirectPauseMenuClickFallback();
            }
        }

        public void Resume()
        {
            SetPaused(false);
        }

        public void BackToMainMenu()
        {
            if (transitionInProgress)
                return;

            Time.timeScale = 1f;
            MenuAudioSilencer.SetMenuAudioPaused(false);
            isPaused = false;
            ModalUIState.Close(ModalOwner);

            if (menuGroup)
            {
                menuGroup.alpha = 0f;
                menuGroup.interactable = false;
                menuGroup.blocksRaycasts = false;
            }

            transitionInProgress = true;
            StartCoroutine(SteampunkUIVideoTransition.Play(this, exitLoadingVideo, videoSortingOrder, LoadMainMenuScene));
        }

        public void RebuildPauseMenu()
        {
            BuildInterface();
            SetPaused(isPaused, true);
        }

        public void SetPauseMenuBackground(Sprite backgroundSprite)
        {
            pauseMenuBackgroundSprite = backgroundSprite;
            RebuildPauseMenu();
        }

        public void SetLanguage(string newLanguageCode)
        {
            if (string.IsNullOrWhiteSpace(newLanguageCode))
                return;

            languageCode = newLanguageCode;
            SteampunkUILocalization.ClearCache();
            RebuildPauseMenu();
        }

        public void SetIndicators(float asincronismo01, float warpRad01)
        {
            asincronismo = Mathf.Clamp01(asincronismo01);
            warpRad = Mathf.Clamp01(warpRad01);
            UpdateIndicatorUI();
        }

        public void SetAsincronismo(float value01)
        {
            asincronismo = Mathf.Clamp01(value01);
            UpdateIndicatorUI();
        }

        public void SetWarpRad(float value01)
        {
            warpRad = Mathf.Clamp01(value01);
            UpdateIndicatorUI();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            cachedPlayer = null;
            SetPaused(false, true);
        }

        private void OnAnachronismResolved(string anachronismId, int resolvedCount)
        {
            RefreshAsincronismoFromResolvedAnachronisms(resolvedCount);
            UpdateIndicatorUI();
        }

        private void SetPaused(bool value, bool force = false)
        {
            if (transitionInProgress)
                return;

            if (!force && isPaused == value)
                return;

            isPaused = value && !IsMainMenuScene();

            if (isPaused)
            {
                if (!ModalUIState.TryOpen(ModalOwner))
                {
                    isPaused = false;
                    return;
                }

                UpdateIndicatorUI();
            }
            else
            {
                ModalUIState.Close(ModalOwner);
            }

            if (menuGroup)
            {
                menuGroup.alpha = isPaused ? 1f : 0f;
                menuGroup.interactable = isPaused;
                menuGroup.blocksRaycasts = isPaused;
            }
        }

        private bool IsMainMenuScene()
        {
            return SceneManager.GetActiveScene().name == MainMenuSceneName;
        }

        private void BuildInterface()
        {
            ClearGeneratedInterface();
            EnsureEventSystem();
            SteampunkUILocalization localization = SteampunkUILocalization.Load(languageCode);

            Canvas canvas = new GameObject("GoldenCast Pause Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.transform.SetParent(transform, false);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = canvasSortingOrder;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            scaler.referencePixelsPerUnit = 100f;

            RectTransform canvasRoot = canvas.GetComponent<RectTransform>();
            RectTransform fixedRoot = CreateRect("FixedReferenceRoot", canvasRoot);
            Stretch(fixedRoot);
            FixedReferenceCanvasRoot resizeRoot = fixedRoot.gameObject.AddComponent<FixedReferenceCanvasRoot>();

            RectTransform layoutRoot = CreateRect("Pause_ReferenceLayout", fixedRoot);
            resizeRoot.Configure(layoutRoot, canvasReferenceResolution, safePadding, allowUpscale);

            RectTransform backgroundLayer = CreateRect("BackgroundLayer", layoutRoot);
            Stretch(backgroundLayer);

            if (pauseMenuBackgroundSprite)
            {
                Image backgroundImage = CreateImage("Pause_Background_Image", backgroundLayer, WithAlpha(Color.white, backgroundImageAlpha));
                Stretch(backgroundImage.rectTransform);
                backgroundImage.sprite = pauseMenuBackgroundSprite;
                backgroundImage.preserveAspect = backgroundPreserveAspect;
                backgroundImage.raycastTarget = false;
            }

            Image shade = CreateImage("Pause_Shade", backgroundLayer, new Color(0.02f, 0.015f, 0.01f, shadeAlpha));
            Stretch(shade.rectTransform);
            shade.raycastTarget = false;

            RectTransform leftPanel = CreatePanel("Pause_Actions_Block", layoutRoot, actionsPanel);
            AddText(localization.Get("pause.title"), leftPanel, pauseTitle, new Color(1f, 0.78f, 0.34f, 1f), FontStyles.Bold);
            RectTransform commandsGroup = CreateRect("CommandsGroup", leftPanel);
            ApplyLayout(commandsGroup, RectLayout.Stretch());
            resumeButtonRect = AddButton("Button_Resume", localization.Get("pause.resume"), commandsGroup, resumeButton, Resume).GetComponent<RectTransform>();
            exitButtonRect = AddButton("Button_ExitToMainMenu", localization.Get("pause.exit_to_main_menu"), commandsGroup, exitButton, BackToMainMenu).GetComponent<RectTransform>();

            RectTransform mapContainer = CreatePanel("Pause_Map_Container", layoutRoot, mapPanel);
            BuildSquareMap(mapContainer);

            RectTransform rightPanel = CreatePanel("Pause_Indicators_Block", layoutRoot, indicatorsPanel);
            AddText(localization.Get("pause.indicators"), rightPanel, indicatorsTitle, new Color(1f, 0.78f, 0.34f, 1f), FontStyles.Bold);
            RectTransform indicatorsGroup = CreateRect("IndicatorsGroup", rightPanel);
            ApplyLayout(indicatorsGroup, RectLayout.Stretch());
            asincronismoFill = AddIndicator("Indicator_Asincronismo", localization.Get("indicator.asincronismo"), indicatorsGroup, asincronismoIndicator, out asincronismoValueText);
            warpRadFill = AddIndicator("Indicator_WarpRad", localization.Get("indicator.warp_rad"), indicatorsGroup, warpRadIndicator, out warpRadValueText);
            UpdateIndicatorUI();

            menuGroup = canvas.gameObject.AddComponent<CanvasGroup>();
        }

        private void ClearGeneratedInterface()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }

            menuGroup = null;
            asincronismoFill = null;
            warpRadFill = null;
            asincronismoValueText = null;
            warpRadValueText = null;
            resumeButtonRect = null;
            exitButtonRect = null;
        }

        private RectTransform CreatePanel(string name, Transform parent, RectLayout layout)
        {
            Image image = CreateImage(name, parent, new Color(0.16f, 0.075f, 0.035f, panelAlpha));
            image.raycastTarget = false;
            RectTransform panel = image.rectTransform;
            ApplyLayout(panel, layout);
            return panel;
        }

        private void BuildSquareMap(RectTransform parent)
        {
            RectTransform mapGroup = CreateRect("MapContent", parent);
            ApplyLayout(mapGroup, RectLayout.Stretch());

            Image mapMaskImage = CreateImage("Map_Square_Mask", mapGroup, new Color(0.02f, 0.045f, 0.03f, 1f));
            mapMaskImage.raycastTarget = false;
            RectTransform maskRect = mapMaskImage.rectTransform;
            ApplyLayout(maskRect, mapMask);
            // mapMaskImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
            mapMaskImage.type = Image.Type.Simple;

            Mask mask = mapMaskImage.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            RawImage rawMap = new GameObject("Map_TopDown_RawImage", typeof(RectTransform), typeof(RawImage), typeof(SceneTopDownMapUI)).GetComponent<RawImage>();
            rawMap.transform.SetParent(maskRect, false);
            rawMap.color = Color.white;
            rawMap.raycastTarget = false;
            ApplyLayout(rawMap.rectTransform, mapImage);

            SceneTopDownMapUI mapUi = rawMap.GetComponent<SceneTopDownMapUI>();
            mapUi.Configure(null, mapCameraHeight, mapOrthographicSize, mapTextureSize);
        }

        private void LoadMainMenuScene()
        {
            transitionInProgress = false;
            SceneManager.LoadScene(MainMenuSceneName);
        }

        private Image AddIndicator(string objectName, string label, RectTransform parent, IndicatorLayout layout, out TMP_Text valueText)
        {
            Image groupImage = CreateImage(objectName, parent, new Color(0.08f, 0.035f, 0.02f, buttonAlpha));
            groupImage.raycastTarget = false;
            RectTransform group = groupImage.rectTransform;
            ApplyLayout(group, layout.group);

            AddText(label, group, layout.labelText, new Color(1f, 0.78f, 0.34f, 1f), FontStyles.Bold);
            valueText = AddText(string.Empty, group, layout.valueText, new Color(0.36f, 1f, 0.45f, 0.95f), FontStyles.Bold);

            RectTransform barGroup = CreateRect(objectName + "_BarGroup", group);
            ApplyLayout(barGroup, RectLayout.Stretch());

            Image track = CreateImage(objectName + "_Track", barGroup, new Color(0.02f, 0.025f, 0.02f, 0.92f));
            track.raycastTarget = false;
            ApplyLayout(track.rectTransform, layout.track);

            Image fill = CreateImage(objectName + "_Fill", track.rectTransform, new Color(0.36f, 1f, 0.45f, 0.95f));
            fill.raycastTarget = false;
            RectTransform fillRect = fill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            return fill;
        }

        private void UpdateIndicatorUI()
        {
            if (autoCalculateAsincronismo && GameManager.Instance)
                RefreshAsincronismoFromResolvedAnachronisms(GameManager.Instance.ResolvedAnachronismCount);

            if (autoCalculateWarpRad)
                warpRad = CalculateWarpRadFromLeylineDistance();

            ApplyIndicatorValue(asincronismoFill, asincronismoValueText, asincronismo);
            ApplyIndicatorValue(warpRadFill, warpRadValueText, warpRad);
        }

        private void RefreshAsincronismoFromResolvedAnachronisms(int resolvedCount)
        {
            float reduction = asincronismoReductionPerTag > 0f
                ? asincronismoReductionPerTag
                : 1f / Mathf.Max(1, totalAnachronismTags);

            asincronismo = Mathf.Clamp01(1f - resolvedCount * reduction);
        }

        private float CalculateWarpRadFromLeylineDistance()
        {
            Transform player = GetPlayerTransform();
            if (!player)
                return leylineMinimumSignal;

            float distance = GetNearestLeylineDistance(player.position);
            if (float.IsPositiveInfinity(distance))
                return leylineMinimumSignal;

            if (distance <= leylineFullSignalDistance)
                return 1f;

            if (distance >= leylineDetectionDistance)
                return leylineMinimumSignal;

            float distance01 = Mathf.InverseLerp(leylineDetectionDistance, leylineFullSignalDistance, distance);
            return Mathf.Lerp(leylineMinimumSignal, 1f, distance01);
        }

        private Transform GetPlayerTransform()
        {
            if (cachedPlayer)
                return cachedPlayer;

            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
            cachedPlayer = playerObject ? playerObject.transform : null;
            return cachedPlayer;
        }

        private static float GetNearestLeylineDistance(Vector3 origin)
        {
            LeylineTrigger[] leylines = FindObjectsByType<LeylineTrigger>(FindObjectsSortMode.None);
            float nearest = float.PositiveInfinity;

            foreach (LeylineTrigger leyline in leylines)
            {
                float distance = Vector3.Distance(origin, leyline.transform.position);
                if (distance < nearest)
                    nearest = distance;
            }

            return nearest;
        }

        private static void ApplyIndicatorValue(Image fill, TMP_Text valueText, float value01)
        {
            value01 = Mathf.Clamp01(value01);

            if (fill)
            {
                RectTransform rect = fill.rectTransform;
                rect.anchorMax = new Vector2(value01, 1f);
                fill.color = Color.Lerp(new Color(0.36f, 1f, 0.45f, 0.95f), new Color(1f, 0.28f, 0.08f, 0.95f), value01);
            }

            if (valueText)
                valueText.text = Mathf.RoundToInt(value01 * 100f) + "%";
        }

        private Button AddButton(string objectName, string label, RectTransform parent, ButtonLayout layout, UnityEngine.Events.UnityAction action)
        {
            Image image = CreateImage(objectName, parent, new Color(0.08f, 0.035f, 0.02f, buttonAlpha));
            image.raycastTarget = true;
            RectTransform rect = image.rectTransform;
            ApplyLayout(rect, layout.rect);

            Button button = image.gameObject.AddComponent<Button>();
            button.interactable = true;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.08f, 0.035f, 0.02f, buttonAlpha);
            colors.highlightedColor = new Color(0.38f, 0.18f, 0.08f, 0.94f);
            colors.pressedColor = new Color(0.03f, 0.015f, 0.01f, 0.98f);
            button.colors = colors;
            button.onClick.AddListener(action);

            AddText(label, rect, layout.labelText, new Color(1f, 0.78f, 0.34f, 1f), FontStyles.Bold);
            return button;
        }

        private void HandleDirectPauseMenuClickFallback()
        {
            if (transitionInProgress)
                return;

            if (!WasPrimaryClickPressed(out Vector2 screenPosition))
                return;

            if (RectContainsScreenPoint(resumeButtonRect, screenPosition))
            {
                Resume();
                return;
            }

            if (RectContainsScreenPoint(exitButtonRect, screenPosition))
                BackToMainMenu();
        }

        private static bool RectContainsScreenPoint(RectTransform rect, Vector2 screenPosition)
        {
            return rect && RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition);
        }

        private static bool WasPrimaryClickPressed(out Vector2 screenPosition)
        {
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                screenPosition = mouse.position.ReadValue();
                return true;
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButtonDown(0))
            {
                screenPosition = Input.mousePosition;
                return true;
            }
#endif

            screenPosition = Vector2.zero;
            return false;
        }

        private static TMP_Text AddText(string text, RectTransform parent, TextLayout layout, Color color, FontStyles style)
        {
            TMP_Text label = new GameObject(text + " Text", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
            label.transform.SetParent(parent, false);
            RectTransform rect = label.GetComponent<RectTransform>();
            ApplyLayout(rect, layout.rect);
            label.text = text;
            label.fontSize = layout.fontSize;
            label.fontStyle = style;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Max(10f, layout.fontSize * 0.6f);
            label.fontSizeMax = layout.fontSize;
            label.raycastTarget = false;
            return label;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            Image image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            return image;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        private static void ApplyLayout(RectTransform rect, RectLayout layout)
        {
            rect.anchorMin = layout.anchorMin;
            rect.anchorMax = layout.anchorMax;
            rect.pivot = layout.pivot;
            rect.sizeDelta = layout.size;
            rect.anchoredPosition = layout.position;

            if (layout.stretchToAnchors)
            {
                rect.offsetMin = layout.offsetMin;
                rect.offsetMax = layout.offsetMax;
            }
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = EventSystem.current;
            if (!eventSystem)
                eventSystem = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();

            Type inputSystemUiModule = Type.GetType(InputSystemUiModuleTypeName);
            if (inputSystemUiModule != null)
            {
                Component inputModule = eventSystem.GetComponent(inputSystemUiModule);
                if (!inputModule)
                    inputModule = eventSystem.gameObject.AddComponent(inputSystemUiModule);

                if (inputModule is Behaviour behaviour)
                    behaviour.enabled = true;

                inputSystemUiModule.GetMethod("AssignDefaultActions")?.Invoke(inputModule, null);
            }
            else if (!eventSystem.GetComponent<StandaloneInputModule>())
            {
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            }
        }

#if UNITY_EDITOR
        private void AssignDefaultEditorAssets()
        {
            if (!pauseMenuBackgroundSprite)
                pauseMenuBackgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DefaultPauseMenuBackgroundPath);

            if (!exitLoadingVideo)
                exitLoadingVideo = AssetDatabase.LoadAssetAtPath<VideoClip>(DefaultLoadingVideoPath);
        }
#endif

        [Serializable]
        private sealed class RectLayout
        {
            public Vector2 anchorMin = new Vector2(0.5f, 0.5f);
            public Vector2 anchorMax = new Vector2(0.5f, 0.5f);
            public Vector2 pivot = new Vector2(0.5f, 0.5f);
            public Vector2 position;
            public Vector2 size;
            public bool stretchToAnchors;
            public Vector2 offsetMin;
            public Vector2 offsetMax;

            public static RectLayout Center(Vector2 position, Vector2 size)
            {
                return new RectLayout
                {
                    position = position,
                    size = size
                };
            }

            public static RectLayout Stretch()
            {
                return Stretch(Vector2.zero, Vector2.zero);
            }

            public static RectLayout Stretch(Vector2 offsetMin, Vector2 offsetMax)
            {
                return new RectLayout
                {
                    anchorMin = Vector2.zero,
                    anchorMax = Vector2.one,
                    pivot = new Vector2(0.5f, 0.5f),
                    stretchToAnchors = true,
                    offsetMin = offsetMin,
                    offsetMax = offsetMax
                };
            }
        }

        [Serializable]
        private sealed class TextLayout
        {
            public RectLayout rect = RectLayout.Center(Vector2.zero, new Vector2(200f, 40f));
            public float fontSize = 20f;

            public static TextLayout Center(Vector2 position, Vector2 size, float fontSize)
            {
                return new TextLayout
                {
                    rect = RectLayout.Center(position, size),
                    fontSize = fontSize
                };
            }
        }

        [Serializable]
        private sealed class ButtonLayout
        {
            public RectLayout rect = RectLayout.Center(Vector2.zero, new Vector2(260f, 62f));
            public TextLayout labelText = TextLayout.Center(Vector2.zero, new Vector2(230f, 46f), 23f);

            public static ButtonLayout Center(Vector2 position, Vector2 size, Vector2 labelSize, float labelFontSize)
            {
                return new ButtonLayout
                {
                    rect = RectLayout.Center(position, size),
                    labelText = TextLayout.Center(Vector2.zero, labelSize, labelFontSize)
                };
            }
        }

        [Serializable]
        private sealed class IndicatorLayout
        {
            public RectLayout group = RectLayout.Center(Vector2.zero, new Vector2(300f, 82f));
            public TextLayout labelText = TextLayout.Center(new Vector2(-24f, 18f), new Vector2(190f, 28f), 18f);
            public TextLayout valueText = TextLayout.Center(new Vector2(114f, 18f), new Vector2(64f, 28f), 18f);
            public RectLayout track = RectLayout.Center(new Vector2(0f, -20f), new Vector2(238f, 16f));

            public static IndicatorLayout Default(Vector2 position)
            {
                return new IndicatorLayout
                {
                    group = RectLayout.Center(position, new Vector2(300f, 82f))
                };
            }
        }
    }
}
