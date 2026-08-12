using System;
using GoldenCast.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
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
    [DisallowMultipleComponent]
    public sealed class AsyncronQuestSteampunkUI : MonoBehaviour
    {
        [Header("Menu")]
        [SerializeField] private string newGameSceneName = "locale";
        [SerializeField] private UnityEvent onNewGame;
        [SerializeField] private UnityEvent onResume;

        [Header("Localization")]
        [SerializeField] private string languageCode = "It";

        [Header("Video Transitions")]
        [SerializeField] private VideoClip newGameIntroVideo;
        [SerializeField] private VideoClip resumeLoadingVideo;
        [SerializeField] private int videoSortingOrder = 1000;

        [Header("Canvas Resize")]
        [SerializeField] private int canvasSortingOrder = 50;
        [SerializeField] private Vector2 canvasReferenceResolution = new Vector2(1920f, 1080f);
        [SerializeField] private Vector4 safePadding = new Vector4(48f, 36f, 48f, 36f);
        [SerializeField] private bool allowUpscale = true;

        [Header("Background")]
        [SerializeField] private Sprite mainMenuBackgroundSprite;
        [SerializeField, Range(0f, 1f)] private float backgroundImageAlpha = 1f;
        [SerializeField, Range(0f, 1f)] private float backgroundTintAlpha = 0.18f;
        [SerializeField] private bool backgroundPreserveAspect;

        [Header("Block Transparency")]
        [SerializeField, Range(0f, 1f)] private float panelAlpha = 0.68f;
        [SerializeField, Range(0f, 1f)] private float buttonAlpha = 0.88f;
        [SerializeField, Range(0f, 1f)] private float buttonHighlightAlpha = 0.95f;
        [SerializeField, Range(0f, 1f)] private float buttonPressedAlpha = 1f;
        [SerializeField, Range(0f, 1f)] private float frameAlpha = 0.82f;
        [SerializeField, Range(0f, 1f)] private float decorativeLinesAlpha = 0.18f;
        [SerializeField, Range(0f, 1f)] private float titleAlpha = 0.92f;

        [Header("Main Menu Layout")]
        [SerializeField] private RectLayout mainPanel = RectLayout.Center(Vector2.zero, new Vector2(520f, 620f));
        [SerializeField] private RectLayout titleLayout = RectLayout.Center(new Vector2(0f, 116f), new Vector2(400f, 38f));
        [SerializeField] private RectLayout buttonGroupLayout = RectLayout.Center(new Vector2(0f, -58f), new Vector2(380f, 180f));
        [SerializeField] private RectLayout newGameButtonLayout = RectLayout.Center(new Vector2(0f, 46f), new Vector2(360f, 68f));
        [SerializeField] private RectLayout resumeButtonLayout = RectLayout.Center(new Vector2(0f, -46f), new Vector2(360f, 68f));
        [SerializeField] private RectLayout buttonTextLayout = RectLayout.Center(Vector2.zero, new Vector2(330f, 50f));

        private const string InputSystemUiModuleTypeName = "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem";
        private bool transitionInProgress;
        private RectTransform newGameButtonRect;
        private RectTransform resumeButtonRect;
        private bool uiClickHandledThisFrame;
#if UNITY_EDITOR
        private const string DefaultMainMenuBackgroundPath = "Assets/AsyncronQuest/SteampunkUI/UI_Style/Start_menu.png";
        private const string DefaultLoadingVideoPath = "Assets/AsyncronQuest/SteampunkUI/UI_Style/Caricamento.mp4";
        private const string DefaultIntroVideoPath = "Assets/AsyncronQuest/SteampunkUI/UI_Style/Intro.mp4";
        private const string AlternateIntroVideoPath = "Assets/AsyncronQuest/SteampunkUI/UI_Style/INtro.mp4";
#endif

        private void Awake()
        {
            ModalUIState.ForceCloseAll();
            Time.timeScale = 1f;
            MenuAudioSilencer.SetMenuAudioPaused(true);

#if UNITY_EDITOR
            AssignDefaultEditorAssets();
#endif
            BuildMainMenu();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            AssignDefaultEditorAssets();

            if (Application.isPlaying && isActiveAndEnabled)
                BuildMainMenu();
        }
#endif

        public void NewGame()
        {
            if (transitionInProgress)
                return;

            uiClickHandledThisFrame = true;
            Time.timeScale = 1f;
            MenuAudioSilencer.SetMenuAudioPaused(false);

            try
            {
                onNewGame?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Evento Nuova Partita non completato: " + exception.Message);
            }

            transitionInProgress = true;
            StartCoroutine(SteampunkUIVideoTransition.Play(this, newGameIntroVideo, videoSortingOrder, LoadNewGameScene));
        }

        public void ResumeGame()
        {
            if (transitionInProgress)
                return;

            uiClickHandledThisFrame = true;
            Time.timeScale = 1f;
            MenuAudioSilencer.SetMenuAudioPaused(false);

            try
            {
                onResume?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Evento Riprendi Partita non completato: " + exception.Message);
            }

            transitionInProgress = true;
            StartCoroutine(SteampunkUIVideoTransition.Play(this, resumeLoadingVideo, videoSortingOrder, ResumeSavedGame));
        }

        public void RebuildMainMenu()
        {
            BuildMainMenu();
        }

        public void SetMainMenuBackground(Sprite backgroundSprite)
        {
            mainMenuBackgroundSprite = backgroundSprite;
            BuildMainMenu();
        }

        public void LoadMainMenuBackgroundFromResources(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
                return;

            Sprite loadedSprite = Resources.Load<Sprite>(resourcePath);
            if (!loadedSprite)
            {
                Debug.LogWarning("Background menu non trovato in Resources: " + resourcePath);
                return;
            }

            SetMainMenuBackground(loadedSprite);
        }

        public void SetMenuBlockTransparency(float panel, float button, float frame)
        {
            panelAlpha = Mathf.Clamp01(panel);
            buttonAlpha = Mathf.Clamp01(button);
            frameAlpha = Mathf.Clamp01(frame);
            BuildMainMenu();
        }

        public void SetLanguage(string newLanguageCode)
        {
            if (string.IsNullOrWhiteSpace(newLanguageCode))
                return;

            languageCode = newLanguageCode;
            SteampunkUILocalization.ClearCache();
            BuildMainMenu();
        }

        private void BuildMainMenu()
        {
            ClearGeneratedInterface();

            EnsureEventSystem();
            SteampunkUILocalization localization = SteampunkUILocalization.Load(languageCode);

            Canvas canvas = CreateCanvas("RootCanvas", transform, canvasSortingOrder);
            RectTransform root = canvas.GetComponent<RectTransform>();
            RectTransform fixedRoot = CreateRect("FixedReferenceRoot", root);
            Stretch(fixedRoot);
            FixedReferenceCanvasRoot resizeRoot = fixedRoot.gameObject.AddComponent<FixedReferenceCanvasRoot>();

            RectTransform layoutRoot = CreateRect("MainMenu_ReferenceLayout", fixedRoot);
            resizeRoot.Configure(layoutRoot, canvasReferenceResolution, safePadding, allowUpscale);

            RectTransform backgroundLayer = CreateRect("BackgroundLayer", layoutRoot);
            Stretch(backgroundLayer);

            if (mainMenuBackgroundSprite)
            {
                Image backgroundImage = CreateImage("BackgroundImage", backgroundLayer, WithAlpha(Color.white, backgroundImageAlpha));
                Stretch(backgroundImage.rectTransform);
                backgroundImage.sprite = mainMenuBackgroundSprite;
                backgroundImage.preserveAspect = backgroundPreserveAspect;
                backgroundImage.raycastTarget = false;
            }

            Image background = CreateImage("BackgroundTint", backgroundLayer, new Color(0.06f, 0.035f, 0.02f, backgroundTintAlpha));
            Stretch(background.rectTransform);
            background.raycastTarget = false;
            AddSubtleLines(backgroundLayer);

            RectTransform panel = CreateImage("MainMenuPanel", layoutRoot, new Color(0.18f, 0.08f, 0.035f, panelAlpha)).rectTransform;
            panel.GetComponent<Image>().raycastTarget = false;
            ApplyLayout(panel, mainPanel);
            AddFrame(panel, new Color(0.72f, 0.49f, 0.19f, frameAlpha), 8f);

            RectTransform titleBlock = CreateRect("TitleBlock", panel);
            ApplyLayout(titleBlock, titleLayout);
            AddText(localization.Get("main.title"), titleBlock, RectLayout.Stretch(), 22f, new Color(0.36f, 1f, 0.45f, titleAlpha), FontStyles.Bold);

            RectTransform buttonGroup = CreateRect("ButtonGroup", panel);
            ApplyLayout(buttonGroup, buttonGroupLayout);
            AddButton("Button_NewGame", localization.Get("main.new_game"), buttonGroup, newGameButtonLayout, NewGame);
            newGameButtonRect = buttonGroup.Find("Button_NewGame") as RectTransform;
            AddButton("Button_ResumeGame", localization.Get("main.resume_game"), buttonGroup, resumeButtonLayout, ResumeGame);
            resumeButtonRect = buttonGroup.Find("Button_ResumeGame") as RectTransform;
        }

        private void Update()
        {
            HandleDirectMainMenuClickFallback();
        }

        private void LoadNewGameScene()
        {
            transitionInProgress = false;

            if (!string.IsNullOrWhiteSpace(newGameSceneName))
                SceneManager.LoadScene(newGameSceneName);
        }

        private void ResumeSavedGame()
        {
            transitionInProgress = false;
            EnsureGameManager().ResumeSavedGame();
        }

        private static Canvas CreateCanvas(string name, Transform parent, int sortingOrder)
        {
            Canvas canvas = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.transform.SetParent(parent, false);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            scaler.referencePixelsPerUnit = 100f;

            return canvas;
        }

        private Button AddButton(string objectName, string label, RectTransform parent, RectLayout layout, UnityAction action)
        {
            Image image = CreateImage(objectName, parent, new Color(0.1f, 0.04f, 0.02f, buttonAlpha));
            image.raycastTarget = true;
            RectTransform rect = image.rectTransform;
            ApplyLayout(rect, layout);
            AddFrame(rect, new Color(0.44f, 0.26f, 0.08f, frameAlpha), 4f);

            Button button = image.gameObject.AddComponent<Button>();
            button.interactable = true;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.1f, 0.04f, 0.02f, buttonAlpha);
            colors.highlightedColor = new Color(0.34f, 0.16f, 0.06f, buttonHighlightAlpha);
            colors.pressedColor = new Color(0.045f, 0.018f, 0.01f, buttonPressedAlpha);
            button.colors = colors;
            button.onClick.AddListener(action);

            AddText(label, rect, buttonTextLayout, 25f, new Color(1f, 0.78f, 0.34f, 1f), FontStyles.Bold);
            return button;
        }

        private static TMP_Text AddText(string text, RectTransform parent, RectLayout layout, float fontSize, Color color, FontStyles style)
        {
            TMP_Text label = new GameObject(text + " Text", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
            label.transform.SetParent(parent, false);

            RectTransform rect = label.GetComponent<RectTransform>();
            ApplyLayout(rect, layout);

            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Max(10f, fontSize * 0.58f);
            label.fontSizeMax = fontSize;
            label.raycastTarget = false;
            return label;
        }

        private void ClearGeneratedInterface()
        {
            newGameButtonRect = null;
            resumeButtonRect = null;

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            Image image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            return image;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
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

        private static void AddFrame(RectTransform parent, Color color, float thickness)
        {
            RectTransform frameRoot = CreateRect("Frame", parent);
            Stretch(frameRoot);
            AddFramePart("Frame_Top", frameRoot, new Vector2(0f, parent.sizeDelta.y * 0.5f - thickness * 0.5f), new Vector2(parent.sizeDelta.x, thickness), color);
            AddFramePart("Frame_Bottom", frameRoot, new Vector2(0f, -parent.sizeDelta.y * 0.5f + thickness * 0.5f), new Vector2(parent.sizeDelta.x, thickness), color);
            AddFramePart("Frame_Left", frameRoot, new Vector2(-parent.sizeDelta.x * 0.5f + thickness * 0.5f, 0f), new Vector2(thickness, parent.sizeDelta.y), color);
            AddFramePart("Frame_Right", frameRoot, new Vector2(parent.sizeDelta.x * 0.5f - thickness * 0.5f, 0f), new Vector2(thickness, parent.sizeDelta.y), color);
        }

        private static void AddFramePart(string name, RectTransform parent, Vector2 position, Vector2 size, Color color)
        {
            Image image = CreateImage(name, parent, color);
            image.raycastTarget = false;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private void AddSubtleLines(RectTransform root)
        {
            RectTransform lineGroup = CreateRect("DecorativeLines", root);
            Stretch(lineGroup);

            for (int i = 0; i < 18; i++)
            {
                Image line = CreateImage("Background_Line_" + i, lineGroup, new Color(0.72f, 0.49f, 0.19f, decorativeLinesAlpha));
                line.raycastTarget = false;
                RectTransform rect = line.rectTransform;
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(1f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(0f, 2f);
                rect.anchoredPosition = new Vector2(0f, -430f + i * 52f);
            }
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

        private static GameManager EnsureGameManager()
        {
            if (GameManager.Instance)
                return GameManager.Instance;

            return new GameObject("GameManager").AddComponent<GameManager>();
        }

        private void HandleDirectMainMenuClickFallback()
        {
            if (transitionInProgress)
                return;

            if (!WasPrimaryClickPressed(out Vector2 screenPosition))
                return;

            if (uiClickHandledThisFrame)
            {
                uiClickHandledThisFrame = false;
                return;
            }

            if (RectContainsScreenPoint(newGameButtonRect, screenPosition))
            {
                NewGame();
                return;
            }

            if (RectContainsScreenPoint(resumeButtonRect, screenPosition))
                ResumeGame();
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

        private static bool RectContainsScreenPoint(RectTransform rect, Vector2 screenPosition)
        {
            return rect && RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition);
        }

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
                return new RectLayout
                {
                    anchorMin = Vector2.zero,
                    anchorMax = Vector2.one,
                    pivot = new Vector2(0.5f, 0.5f),
                    stretchToAnchors = true,
                    offsetMin = Vector2.zero,
                    offsetMax = Vector2.zero
                };
            }
        }

#if UNITY_EDITOR
        private void AssignDefaultEditorAssets()
        {
            if (!mainMenuBackgroundSprite)
                mainMenuBackgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DefaultMainMenuBackgroundPath);

            if (!resumeLoadingVideo)
                resumeLoadingVideo = AssetDatabase.LoadAssetAtPath<VideoClip>(DefaultLoadingVideoPath);

            if (!newGameIntroVideo)
                newGameIntroVideo = AssetDatabase.LoadAssetAtPath<VideoClip>(DefaultIntroVideoPath);

            if (!newGameIntroVideo)
                newGameIntroVideo = AssetDatabase.LoadAssetAtPath<VideoClip>(AlternateIntroVideoPath);
        }
#endif
    }
}
