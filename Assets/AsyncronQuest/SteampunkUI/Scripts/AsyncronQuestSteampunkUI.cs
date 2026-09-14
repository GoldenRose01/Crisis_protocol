using System;
using System.IO;
using GoldenCast.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AsyncronQuest.SteampunkUI
{
    [DisallowMultipleComponent]
    public sealed class AsyncronQuestSteampunkUI : MonoBehaviour
    {
        [Header("Scene Configuration")]
        [SerializeField] private string newGameSceneName = "settore 0";
        [SerializeField] private UnityEvent onNewGame;
        [SerializeField] private UnityEvent onResume;

        [Header("Localization / Text")]
        [SerializeField] private string languageCode = "It";

        [Header("Canvas Settings")]
        [SerializeField] private int canvasSortingOrder = 50;
        [SerializeField] private Vector2 canvasReferenceResolution = new Vector2(1920f, 1080f);

        [Header("Neon Color Palette")]
        public Color neonGreen = new Color(0.0f, 1.0f, 0.45f, 1.0f);        // #00FF73
        public Color neonGreenGlow = new Color(0.0f, 1.0f, 0.45f, 0.25f);
        public Color neonYellow = new Color(1.0f, 0.95f, 0.05f, 1.0f);       // #FFF20D Fluorescent Yellow
        public Color neonYellowGlow = new Color(1.0f, 0.95f, 0.05f, 0.25f);
        public Color neonRed = new Color(1.0f, 0.08f, 0.28f, 1.0f);          // #FF1447 Neon Red
        public Color neonRedGlow = new Color(1.0f, 0.08f, 0.28f, 0.25f);
        public Color cyberCyan = new Color(0.25f, 0.9f, 1.0f, 1.0f);
        public Color darkChassis = new Color(0.02f, 0.045f, 0.08f, 0.94f);
        public Color darkGlassPanel = new Color(0.015f, 0.03f, 0.06f, 0.88f);

        [Header("Background Video / Neon")]
        [SerializeField] private UnityEngine.Video.VideoClip backgroundVideoClip;
        [SerializeField] private Sprite mainMenuBackgroundSprite;
        [SerializeField, Range(0f, 1f)] private float backgroundImageAlpha = 0.35f;

        private const string DefaultNeonVideoPath = "Assets/AsyncronQuest/SteampunkUI/UI_Style/DEVE_ESSERE_SOLO_IL_NEON_NENTE.mp4";
        private const string InputSystemUiModuleTypeName = "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem";
        private const string MasterVolumePrefKey = "MasterVolume";
        private const string PreMuteVolumePrefKey = "PreMuteVolume";

        private Canvas mainCanvas;
        private RectTransform mainPanelRoot;
        private RectTransform levelSelectPanelRoot;
        private Text volumePercentText;
        private Text volumeIconText;
        private Slider volumeSliderComponent;
        private Image titleGlowEffect;
        private AudioSource uiAudioSource;
        private UnityEngine.Video.VideoPlayer bgVideoPlayer;
        private RenderTexture bgVideoRenderTexture;

        private Sprite solidSprite;
        private Sprite techFrameGreen;
        private Sprite techFrameYellow;
        private Sprite techFrameRed;
        private Sprite techFrameGlass;
        private Font cyberFont;

        private bool isLevelSelectOpen = false;

        private void Awake()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            ModalUIState.ForceCloseAll();

            // Setup audio listener
            float currentVol = PlayerPrefs.GetFloat(MasterVolumePrefKey, 1.0f);
            AudioListener.volume = currentVol;
            AudioListener.pause = false;

            InitAudioSource();
            GenerateProceduralSprites();
            BuildNeonMainMenu();
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            // Keep cursor active in main menu
            if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // Pulsing Neon Glow effects
            float pulse = 0.75f + Mathf.Sin(Time.unscaledTime * 3.5f) * 0.25f;
            if (titleGlowEffect != null)
            {
                Color c = neonYellow;
                c.a = pulse * 0.45f;
                titleGlowEffect.color = c;
            }

            if (Input.GetKeyDown(KeyCode.Escape) && isLevelSelectOpen)
            {
                CloseLevelSelect();
            }
        }

        private void InitAudioSource()
        {
            uiAudioSource = gameObject.GetComponent<AudioSource>();
            if (uiAudioSource == null)
                uiAudioSource = gameObject.AddComponent<AudioSource>();

            uiAudioSource.playOnAwake = false;
            uiAudioSource.ignoreListenerPause = true;
            uiAudioSource.volume = 0.7f;
        }

        public void PlayBeepSound(float frequency = 880f, float duration = 0.06f)
        {
            if (uiAudioSource == null) return;
            AudioClip clip = CreateToneClip(frequency, duration);
            uiAudioSource.PlayOneShot(clip);
        }

        private AudioClip CreateToneClip(float freq, float duration)
        {
            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = 1f - (float)i / sampleCount;
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.35f;
            }

            AudioClip clip = AudioClip.Create("UI_Tone", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // SPRITE GENERATION
        // ─────────────────────────────────────────────────────────────────────────

        private void GenerateProceduralSprites()
        {
            cyberFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (cyberFont == null) cyberFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // 1. Solid Sprite
            Texture2D solidTex = new Texture2D(2, 2);
            solidTex.SetPixels(new Color[] { Color.white, Color.white, Color.white, Color.white });
            solidTex.Apply();
            solidSprite = Sprite.Create(solidTex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));

            // 2. Tech Frames
            techFrameGreen = CreateTechBorderSprite(neonGreen, new Color(0f, 0.2f, 0.1f, 0.35f));
            techFrameYellow = CreateTechBorderSprite(neonYellow, new Color(0.2f, 0.18f, 0f, 0.35f));
            techFrameRed = CreateTechBorderSprite(neonRed, new Color(0.25f, 0.02f, 0.05f, 0.35f));
            techFrameGlass = CreateTechBorderSprite(new Color(0.3f, 0.8f, 1f, 0.6f), new Color(0.01f, 0.025f, 0.05f, 0.85f));
        }

        private Sprite CreateTechBorderSprite(Color borderColor, Color innerBg)
        {
            int w = 32;
            int h = 32;
            Texture2D tex = new Texture2D(w, h);
            tex.filterMode = FilterMode.Point;
            Color[] pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool isBorderX = (x == 0 || x == 1 || x == w - 2 || x == w - 1);
                    bool isBorderY = (y == 0 || y == 1 || y == h - 2 || y == h - 1);
                    bool isCorner = (x < 5 || x >= w - 5) && (y < 5 || y >= h - 5);

                    if (isCorner && (isBorderX || isBorderY))
                    {
                        pixels[y * w + x] = borderColor;
                    }
                    else if (isBorderX || isBorderY)
                    {
                        Color edge = borderColor;
                        edge.a = 0.8f;
                        pixels[y * w + x] = edge;
                    }
                    else
                    {
                        pixels[y * w + x] = innerBg;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(6, 6, 6, 6));
        }

        // ─────────────────────────────────────────────────────────────────────────
        // UI CONSTRUCTION
        // ─────────────────────────────────────────────────────────────────────────

        public void RebuildMainMenu()
        {
            BuildNeonMainMenu();
        }

        private void BuildNeonMainMenu()
        {
            ClearGeneratedInterface();
            EnsureEventSystem();

            // Main Screen-Space Overlay Canvas
            GameObject canvasGO = new GameObject("NeonMainMenu_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);

            mainCanvas = canvasGO.GetComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = canvasSortingOrder;

            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = canvasReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1.0f;

            RectTransform rootRT = canvasGO.GetComponent<RectTransform>();

            // 1. Background Cybernetic Grid & Overlay
            BuildBackground(rootRT);

            // 2. Top Header Bar with Tactical Info & Volume Controls
            BuildTopHeader(rootRT);

            // 3. Central Main Hub (Title + 3 Main Neon Buttons)
            BuildCentralHub(rootRT);

            // 4. Level Select Sub-Panel (Modal)
            BuildLevelSelectPanel(rootRT);

            // 5. Bottom Tactical Status Bar
            BuildBottomFooter(rootRT);
        }

        private void BuildBackground(RectTransform parent)
        {
            // Dark base background
            GameObject bgGO = new GameObject("Cyber_Background");
            bgGO.transform.SetParent(parent, false);
            RectTransform bgRT = bgGO.AddComponent<RectTransform>();
            Stretch(bgRT);

            Image bgImg = bgGO.AddComponent<Image>();
            bgImg.sprite = solidSprite;
            bgImg.color = new Color(0.01f, 0.02f, 0.04f, 1f);
            bgImg.raycastTarget = false;

            // ─────────────────────────────────────────────────────────────────
            // BACKGROUND VIDEO PLAYER (DEVE_ESSERE_SOLO_IL_NEON_NENTE.mp4)
            // ─────────────────────────────────────────────────────────────────
            SetupVideoBackground(bgRT);

            // Optional static background artwork fallback (only if video is missing)
            if (bgVideoPlayer == null && mainMenuBackgroundSprite != null)
            {
                GameObject bgTexGO = new GameObject("Background_Artwork");
                bgTexGO.transform.SetParent(bgRT, false);
                RectTransform bgTexRT = bgTexGO.AddComponent<RectTransform>();
                Stretch(bgTexRT);
                Image artwork = bgTexGO.AddComponent<Image>();
                artwork.sprite = mainMenuBackgroundSprite;
                artwork.color = new Color(1f, 1f, 1f, backgroundImageAlpha);
                artwork.preserveAspect = false;
                artwork.raycastTarget = false;
            }

            // Cyber Scanlines (Subtle overlay over video)
            GameObject linesGO = new GameObject("Cyber_Scanlines");
            linesGO.transform.SetParent(bgRT, false);
            RectTransform linesRT = linesGO.AddComponent<RectTransform>();
            Stretch(linesRT);

            for (int i = 0; i < 28; i++)
            {
                GameObject line = new GameObject("Scanline_" + i);
                line.transform.SetParent(linesRT, false);
                RectTransform lineRT = line.AddComponent<RectTransform>();
                lineRT.anchorMin = new Vector2(0f, 0.5f);
                lineRT.anchorMax = new Vector2(1f, 0.5f);
                lineRT.pivot = new Vector2(0.5f, 0.5f);
                lineRT.sizeDelta = new Vector2(0f, 2f);
                lineRT.anchoredPosition = new Vector2(0f, -500f + i * 38f);

                Image lineImg = line.AddComponent<Image>();
                lineImg.sprite = solidSprite;
                Color lc = (i % 3 == 0) ? neonGreen : (i % 3 == 1 ? neonYellow : neonRed);
                lc.a = 0.025f;
                lineImg.color = lc;
                lineImg.raycastTarget = false;
            }

            // Corner perimeter cyber brackets
            CreateCornerBracket(parent, new Vector2(30f, -30f), new Vector2(0f, 1f), new Vector2(0f, 1f), neonGreen);
            CreateCornerBracket(parent, new Vector2(-30f, -30f), new Vector2(1f, 1f), new Vector2(1f, 1f), neonYellow);
            CreateCornerBracket(parent, new Vector2(30f, 30f), new Vector2(0f, 0f), new Vector2(0f, 0f), neonRed);
            CreateCornerBracket(parent, new Vector2(-30f, 30f), new Vector2(1f, 0f), new Vector2(1f, 0f), neonGreen);
        }

        private void SetupVideoBackground(RectTransform bgParent)
        {
            UnityEngine.Video.VideoClip clipToPlay = backgroundVideoClip;

#if UNITY_EDITOR
            if (clipToPlay == null)
            {
                clipToPlay = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>(DefaultNeonVideoPath);
                backgroundVideoClip = clipToPlay;
            }
#endif

            string videoFilePath = Path.Combine(Application.dataPath, "AsyncronQuest/SteampunkUI/UI_Style/DEVE_ESSERE_SOLO_IL_NEON_NENTE.mp4");
            bool hasValidSource = (clipToPlay != null) || File.Exists(videoFilePath);

            if (!hasValidSource)
                return;

            if (bgVideoRenderTexture == null || !bgVideoRenderTexture.IsCreated())
            {
                bgVideoRenderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
                bgVideoRenderTexture.name = "Neon_MainMenu_VideoRT";
                bgVideoRenderTexture.wrapMode = TextureWrapMode.Clamp;
                bgVideoRenderTexture.Create();
            }

            GameObject vpGO = new GameObject("Background_VideoPlayer");
            vpGO.transform.SetParent(transform, false);
            bgVideoPlayer = vpGO.AddComponent<UnityEngine.Video.VideoPlayer>();
            bgVideoPlayer.playOnAwake = true;
            bgVideoPlayer.isLooping = true;
            bgVideoPlayer.renderMode = UnityEngine.Video.VideoRenderMode.RenderTexture;
            bgVideoPlayer.targetTexture = bgVideoRenderTexture;
            bgVideoPlayer.aspectRatio = UnityEngine.Video.VideoAspectRatio.FitHorizontally;
            bgVideoPlayer.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.None;

            if (clipToPlay != null)
            {
                bgVideoPlayer.source = UnityEngine.Video.VideoSource.VideoClip;
                bgVideoPlayer.clip = clipToPlay;
            }
            else
            {
                bgVideoPlayer.source = UnityEngine.Video.VideoSource.Url;
                bgVideoPlayer.url = videoFilePath;
            }

            // RawImage to display the video texture
            GameObject rawGO = new GameObject("Background_Video_RawImage");
            rawGO.transform.SetParent(bgParent, false);
            RectTransform rawRT = rawGO.AddComponent<RectTransform>();
            Stretch(rawRT);

            RawImage rawImage = rawGO.AddComponent<RawImage>();
            rawImage.texture = bgVideoRenderTexture;
            rawImage.color = Color.white;
            rawImage.raycastTarget = false;

            bgVideoPlayer.Play();
        }

        private void CreateCornerBracket(RectTransform parent, Vector2 pos, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            GameObject bGO = new GameObject("Corner_Bracket");
            bGO.transform.SetParent(parent, false);
            RectTransform rt = bGO.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = anchorMin;
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(60f, 60f);

            Image img = bGO.AddComponent<Image>();
            img.sprite = techFrameGreen;
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
        }

        private void BuildTopHeader(RectTransform parent)
        {
            GameObject headerGO = new GameObject("Top_Header_Bar");
            headerGO.transform.SetParent(parent, false);
            RectTransform headerRT = headerGO.AddComponent<RectTransform>();
            headerRT.anchorMin = new Vector2(0f, 1f);
            headerRT.anchorMax = new Vector2(1f, 1f);
            headerRT.pivot = new Vector2(0.5f, 1f);
            headerRT.anchoredPosition = new Vector2(0f, -15f);
            headerRT.sizeDelta = new Vector2(-80f, 60f);

            // Left: System Status
            GameObject statusGO = new GameObject("Status_Tag");
            statusGO.transform.SetParent(headerRT, false);
            RectTransform statusRT = statusGO.AddComponent<RectTransform>();
            statusRT.anchorMin = new Vector2(0f, 0.5f);
            statusRT.anchorMax = new Vector2(0f, 0.5f);
            statusRT.pivot = new Vector2(0f, 0.5f);
            statusRT.anchoredPosition = new Vector2(10f, 0f);
            statusRT.sizeDelta = new Vector2(360f, 40f);

            Text statusText = statusGO.AddComponent<Text>();
            statusText.font = cyberFont;
            statusText.fontSize = 17;
            statusText.fontStyle = FontStyle.Bold;
            statusText.alignment = TextAnchor.MiddleLeft;
            statusText.text = "● SISTEMA ATTIVO // CANALE SICUREZZA 01";
            statusText.color = neonGreen;
            statusText.raycastTarget = false;

            // Center Tag
            GameObject centerTagGO = new GameObject("Facility_Tag");
            centerTagGO.transform.SetParent(headerRT, false);
            RectTransform centerTagRT = centerTagGO.AddComponent<RectTransform>();
            centerTagRT.anchorMin = new Vector2(0.5f, 0.5f);
            centerTagRT.anchorMax = new Vector2(0.5f, 0.5f);
            centerTagRT.pivot = new Vector2(0.5f, 0.5f);
            centerTagRT.anchoredPosition = Vector2.zero;
            centerTagRT.sizeDelta = new Vector2(450f, 40f);

            Text centerText = centerTagGO.AddComponent<Text>();
            centerText.font = cyberFont;
            centerText.fontSize = 15;
            centerText.alignment = TextAnchor.MiddleCenter;
            centerText.text = "◆ PROTOCOLLO DI CONTENIMENTO SETTORI v2.5 ◆";
            centerText.color = new Color(1f, 1f, 1f, 0.65f);
            centerText.raycastTarget = false;

            // ─────────────────────────────────────────────────────────────────
            // Right: MASTER VOLUME CONTROL WIDGET (Icon + Interactive Slider)
            // ─────────────────────────────────────────────────────────────────
            BuildVolumeControlWidget(headerRT);
        }

        private void BuildVolumeControlWidget(RectTransform headerParent)
        {
            GameObject volWidgetGO = new GameObject("Volume_Control_Widget");
            volWidgetGO.transform.SetParent(headerParent, false);
            RectTransform volRT = volWidgetGO.AddComponent<RectTransform>();
            volRT.anchorMin = new Vector2(1f, 0.5f);
            volRT.anchorMax = new Vector2(1f, 0.5f);
            volRT.pivot = new Vector2(1f, 0.5f);
            volRT.anchoredPosition = new Vector2(-10f, 0f);
            volRT.sizeDelta = new Vector2(380f, 48f);

            // Widget dark glass backing with neon green border
            Image widgetBg = volWidgetGO.AddComponent<Image>();
            widgetBg.sprite = techFrameGreen;
            widgetBg.type = Image.Type.Sliced;
            widgetBg.color = new Color(0.02f, 0.05f, 0.09f, 0.95f);

            // 1. Volume Icon / Mute Button
            GameObject iconBtnGO = new GameObject("Volume_Icon_Button");
            iconBtnGO.transform.SetParent(volRT, false);
            RectTransform iconBtnRT = iconBtnGO.AddComponent<RectTransform>();
            iconBtnRT.anchorMin = new Vector2(0f, 0.5f);
            iconBtnRT.anchorMax = new Vector2(0f, 0.5f);
            iconBtnRT.pivot = new Vector2(0f, 0.5f);
            iconBtnRT.anchoredPosition = new Vector2(10f, 0f);
            iconBtnRT.sizeDelta = new Vector2(40f, 36f);

            Image iconBtnImg = iconBtnGO.AddComponent<Image>();
            iconBtnImg.sprite = solidSprite;
            iconBtnImg.color = new Color(0.08f, 0.2f, 0.15f, 0.5f);

            Button iconBtn = iconBtnGO.AddComponent<Button>();
            iconBtn.onClick.AddListener(ToggleVolumeMute);

            GameObject iconTextGO = new GameObject("Icon_Text");
            iconTextGO.transform.SetParent(iconBtnRT, false);
            RectTransform iconTextRT = iconTextGO.AddComponent<RectTransform>();
            Stretch(iconTextRT);

            volumeIconText = iconTextGO.AddComponent<Text>();
            volumeIconText.font = cyberFont;
            volumeIconText.fontSize = 20;
            volumeIconText.alignment = TextAnchor.MiddleCenter;
            volumeIconText.text = "🔊";
            volumeIconText.color = neonYellow;
            volumeIconText.raycastTarget = false;

            // 2. Volume Slider Component
            GameObject sliderGO = new GameObject("Master_Volume_Slider");
            sliderGO.transform.SetParent(volRT, false);
            RectTransform sliderRT = sliderGO.AddComponent<RectTransform>();
            sliderRT.anchorMin = new Vector2(0f, 0.5f);
            sliderRT.anchorMax = new Vector2(0f, 0.5f);
            sliderRT.pivot = new Vector2(0f, 0.5f);
            sliderRT.anchoredPosition = new Vector2(60f, 0f);
            sliderRT.sizeDelta = new Vector2(210f, 20f);

            Slider slider = sliderGO.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;

            // Slider Background Track
            GameObject bgTrackGO = new GameObject("Background_Track");
            bgTrackGO.transform.SetParent(sliderRT, false);
            RectTransform bgTrackRT = bgTrackGO.AddComponent<RectTransform>();
            Stretch(bgTrackRT);
            Image bgTrackImg = bgTrackGO.AddComponent<Image>();
            bgTrackImg.sprite = solidSprite;
            bgTrackImg.color = new Color(0.05f, 0.1f, 0.15f, 0.9f);

            // Slider Fill Area
            GameObject fillAreaGO = new GameObject("Fill Area");
            fillAreaGO.transform.SetParent(sliderRT, false);
            RectTransform fillAreaRT = fillAreaGO.AddComponent<RectTransform>();
            fillAreaRT.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRT.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRT.offsetMin = new Vector2(4f, 0f);
            fillAreaRT.offsetMax = new Vector2(-4f, 0f);

            GameObject fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(fillAreaRT, false);
            RectTransform fillRT = fillGO.AddComponent<RectTransform>();
            fillRT.sizeDelta = Vector2.zero;
            Image fillImg = fillGO.AddComponent<Image>();
            fillImg.sprite = solidSprite;
            fillImg.color = neonGreen;

            slider.fillRect = fillRT;

            // Slider Handle Area
            GameObject handleAreaGO = new GameObject("Handle Slide Area");
            handleAreaGO.transform.SetParent(sliderRT, false);
            RectTransform handleAreaRT = handleAreaGO.AddComponent<RectTransform>();
            Stretch(handleAreaRT);
            handleAreaRT.offsetMin = new Vector2(8f, 0f);
            handleAreaRT.offsetMax = new Vector2(-8f, 0f);

            GameObject handleGO = new GameObject("Handle");
            handleGO.transform.SetParent(handleAreaGO.transform, false);
            RectTransform handleRT = handleGO.AddComponent<RectTransform>();
            handleRT.sizeDelta = new Vector2(18f, 26f);
            Image handleImg = handleGO.AddComponent<Image>();
            handleImg.sprite = solidSprite;
            handleImg.color = neonYellow;

            slider.handleRect = handleRT;
            slider.targetGraphic = handleImg;

            // Setup current volume
            float savedVol = PlayerPrefs.GetFloat(MasterVolumePrefKey, 1.0f);
            slider.value = savedVol;
            volumeSliderComponent = slider;
            slider.onValueChanged.AddListener(OnVolumeSliderChanged);

            // 3. Percentage Text
            GameObject percentGO = new GameObject("Volume_Percent_Text");
            percentGO.transform.SetParent(volRT, false);
            RectTransform percentRT = percentGO.AddComponent<RectTransform>();
            percentRT.anchorMin = new Vector2(1f, 0.5f);
            percentRT.anchorMax = new Vector2(1f, 0.5f);
            percentRT.pivot = new Vector2(1f, 0.5f);
            percentRT.anchoredPosition = new Vector2(-12f, 0f);
            percentRT.sizeDelta = new Vector2(80f, 36f);

            volumePercentText = percentGO.AddComponent<Text>();
            volumePercentText.font = cyberFont;
            volumePercentText.fontSize = 16;
            volumePercentText.fontStyle = FontStyle.Bold;
            volumePercentText.alignment = TextAnchor.MiddleRight;
            volumePercentText.color = neonYellow;
            volumePercentText.raycastTarget = false;

            UpdateVolumeDisplay(savedVol);
        }

        private void OnVolumeSliderChanged(float val)
        {
            val = Mathf.Clamp01(val);
            AudioListener.volume = val;
            PlayerPrefs.SetFloat(MasterVolumePrefKey, val);
            UpdateVolumeDisplay(val);
        }

        private void ToggleVolumeMute()
        {
            PlayBeepSound(1100f, 0.05f);
            if (AudioListener.volume > 0.01f)
            {
                PlayerPrefs.SetFloat(PreMuteVolumePrefKey, AudioListener.volume);
                SetVolume(0f);
            }
            else
            {
                float restore = PlayerPrefs.GetFloat(PreMuteVolumePrefKey, 1.0f);
                if (restore <= 0.05f) restore = 1.0f;
                SetVolume(restore);
            }
        }

        private void SetVolume(float val)
        {
            val = Mathf.Clamp01(val);
            AudioListener.volume = val;
            PlayerPrefs.SetFloat(MasterVolumePrefKey, val);
            if (volumeSliderComponent != null)
                volumeSliderComponent.value = val;
            UpdateVolumeDisplay(val);
        }

        private void UpdateVolumeDisplay(float val)
        {
            int percent = Mathf.RoundToInt(val * 100f);
            if (volumePercentText != null)
            {
                volumePercentText.text = percent <= 0 ? "MUTE" : $"{percent}%";
                volumePercentText.color = percent <= 0 ? neonRed : (percent > 60 ? neonGreen : neonYellow);
            }

            if (volumeIconText != null)
            {
                if (percent <= 0)
                    volumeIconText.text = "🔇";
                else if (percent < 50)
                    volumeIconText.text = "🔉";
                else
                    volumeIconText.text = "🔊";
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // CENTRAL HUB (TITLE + 3 NEON MAIN BUTTONS)
        // ─────────────────────────────────────────────────────────────────────────

        private void BuildCentralHub(RectTransform parent)
        {
            GameObject hubGO = new GameObject("Main_Menu_Panel");
            hubGO.transform.SetParent(parent, false);
            mainPanelRoot = hubGO.AddComponent<RectTransform>();
            mainPanelRoot.anchorMin = new Vector2(0.5f, 0.5f);
            mainPanelRoot.anchorMax = new Vector2(0.5f, 0.5f);
            mainPanelRoot.pivot = new Vector2(0.5f, 0.5f);
            mainPanelRoot.anchoredPosition = new Vector2(0f, -10f);
            mainPanelRoot.sizeDelta = new Vector2(620f, 680f);

            // Translucent dark cyberpunk chassis with glowing tech border
            Image hubBg = hubGO.AddComponent<Image>();
            hubBg.sprite = techFrameYellow;
            hubBg.type = Image.Type.Sliced;
            hubBg.color = darkGlassPanel;

            // 1. GAME TITLE BLOCK
            BuildTitleBlock(mainPanelRoot);

            // 2. THREE MAIN BUTTONS BLOCK
            BuildMainButtons(mainPanelRoot);
        }

        private void BuildTitleBlock(RectTransform hubParent)
        {
            GameObject titleContainerGO = new GameObject("Title_Container");
            titleContainerGO.transform.SetParent(hubParent, false);
            RectTransform titleContainerRT = titleContainerGO.AddComponent<RectTransform>();
            titleContainerRT.anchorMin = new Vector2(0.5f, 1f);
            titleContainerRT.anchorMax = new Vector2(0.5f, 1f);
            titleContainerRT.pivot = new Vector2(0.5f, 1f);
            titleContainerRT.anchoredPosition = new Vector2(0f, -30f);
            titleContainerRT.sizeDelta = new Vector2(560f, 160f);

            // Title Glow Backdrop
            GameObject glowGO = new GameObject("Title_Glow");
            glowGO.transform.SetParent(titleContainerRT, false);
            RectTransform glowRT = glowGO.AddComponent<RectTransform>();
            Stretch(glowRT);
            titleGlowEffect = glowGO.AddComponent<Image>();
            titleGlowEffect.sprite = solidSprite;
            titleGlowEffect.color = new Color(1f, 0.95f, 0.05f, 0.15f);
            titleGlowEffect.raycastTarget = false;

            // Subtitle Top Tag
            GameObject topTagGO = new GameObject("Top_Subtitle");
            topTagGO.transform.SetParent(titleContainerRT, false);
            RectTransform topTagRT = topTagGO.AddComponent<RectTransform>();
            topTagRT.anchorMin = new Vector2(0.5f, 1f);
            topTagRT.anchorMax = new Vector2(0.5f, 1f);
            topTagRT.pivot = new Vector2(0.5f, 1f);
            topTagRT.anchoredPosition = new Vector2(0f, -6f);
            topTagRT.sizeDelta = new Vector2(540f, 24f);

            Text topTag = topTagGO.AddComponent<Text>();
            topTag.font = cyberFont;
            topTag.fontSize = 14;
            topTag.fontStyle = FontStyle.Bold;
            topTag.alignment = TextAnchor.MiddleCenter;
            topTag.text = "⚡ ALLERTA CONTENIMENTO LIVELLO OMEGA ⚡";
            topTag.color = neonRed;
            topTag.raycastTarget = false;

            // Main Primary Game Title: CRISIS PROTOCOL
            GameObject titleGO = new GameObject("Main_Title_Text");
            titleGO.transform.SetParent(titleContainerRT, false);
            RectTransform titleRT = titleGO.AddComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.5f, 0.5f);
            titleRT.anchorMax = new Vector2(0.5f, 0.5f);
            titleRT.pivot = new Vector2(0.5f, 0.5f);
            titleRT.anchoredPosition = new Vector2(0f, 6f);
            titleRT.sizeDelta = new Vector2(560f, 75f);

            Text titleText = titleGO.AddComponent<Text>();
            titleText.font = cyberFont;
            titleText.fontSize = 50;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.text = "CRISIS PROTOCOL";
            titleText.color = neonYellow;
            titleText.raycastTarget = false;

            // Subtitle Bottom Tag
            GameObject botTagGO = new GameObject("Bot_Subtitle");
            botTagGO.transform.SetParent(titleContainerRT, false);
            RectTransform botTagRT = botTagGO.AddComponent<RectTransform>();
            botTagRT.anchorMin = new Vector2(0.5f, 0f);
            botTagRT.anchorMax = new Vector2(0.5f, 0f);
            botTagRT.pivot = new Vector2(0.5f, 0f);
            botTagRT.anchoredPosition = new Vector2(0f, 10f);
            botTagRT.sizeDelta = new Vector2(540f, 26f);

            Text botTag = botTagGO.AddComponent<Text>();
            botTag.font = cyberFont;
            botTag.fontSize = 15;
            botTag.alignment = TextAnchor.MiddleCenter;
            botTag.text = "< < < SECTOR CONTAINMENT PROTOCOL > > >";
            botTag.color = neonGreen;
            botTag.raycastTarget = false;

            // Glowing Divider Line
            GameObject dividerGO = new GameObject("Neon_Divider");
            dividerGO.transform.SetParent(hubParent, false);
            RectTransform divRT = dividerGO.AddComponent<RectTransform>();
            divRT.anchorMin = new Vector2(0.5f, 1f);
            divRT.anchorMax = new Vector2(0.5f, 1f);
            divRT.pivot = new Vector2(0.5f, 1f);
            divRT.anchoredPosition = new Vector2(0f, -205f);
            divRT.sizeDelta = new Vector2(520f, 3f);

            Image divImg = dividerGO.AddComponent<Image>();
            divImg.sprite = solidSprite;
            divImg.color = neonYellow;
            divImg.raycastTarget = false;
        }

        private void BuildMainButtons(RectTransform hubParent)
        {
            GameObject btnGroupGO = new GameObject("Main_Buttons_Group");
            btnGroupGO.transform.SetParent(hubParent, false);
            RectTransform btnGroupRT = btnGroupGO.AddComponent<RectTransform>();
            btnGroupRT.anchorMin = new Vector2(0.5f, 0f);
            btnGroupRT.anchorMax = new Vector2(0.5f, 0.5f);
            btnGroupRT.pivot = new Vector2(0.5f, 0f);
            btnGroupRT.anchoredPosition = new Vector2(0f, 30f);
            btnGroupRT.sizeDelta = new Vector2(540f, 410f);

            // Button 1: NUOVA PARTITA (Neon Green)
            CreateStyledNeonButton(
                parent: btnGroupRT,
                name: "Button_NuovaPartita",
                pos: new Vector2(0f, 280f),
                size: new Vector2(490f, 95f),
                title: "▶  NUOVA PARTITA",
                subtitle: "[ AVVIA DAL SETTORE 0 // AZZERA EMERGENZA ]",
                themeColor: neonGreen,
                glowFrame: techFrameGreen,
                onClick: NewGame
            );

            // Button 2: RIPRENDI PARTITA (Fluorescent Yellow)
            string resumeSub = HasSaveData() ? $"[ CONTINUA ULTIMA MISSIONE: SETTORE {GetSavedSectorIndex()} ]" : "[ CONTINUA DAL SALVATAGGIO ESISTENTE ]";
            CreateStyledNeonButton(
                parent: btnGroupRT,
                name: "Button_RiprendiPartita",
                pos: new Vector2(0f, 155f),
                size: new Vector2(490f, 95f),
                title: "↺  RIPRENDI PARTITA",
                subtitle: resumeSub,
                themeColor: neonYellow,
                glowFrame: techFrameYellow,
                onClick: ResumeGame
            );

            // Button 3: SELEZIONA LIVELLO (Neon Red)
            CreateStyledNeonButton(
                parent: btnGroupRT,
                name: "Button_SelezionaLivello",
                pos: new Vector2(0f, 30f),
                size: new Vector2(490f, 95f),
                title: "☵  SELEZIONA LIVELLO",
                subtitle: "[ ACCESSO DIRETTO AI SETTORI 0, 1, 2 ]",
                themeColor: neonRed,
                glowFrame: techFrameRed,
                onClick: OpenLevelSelect
            );
        }

        private Button CreateStyledNeonButton(
            RectTransform parent,
            string name,
            Vector2 pos,
            Vector2 size,
            string title,
            string subtitle,
            Color themeColor,
            Sprite glowFrame,
            UnityAction onClick)
        {
            GameObject btnGO = new GameObject(name);
            btnGO.transform.SetParent(parent, false);
            RectTransform btnRT = btnGO.AddComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.5f, 0f);
            btnRT.anchorMax = new Vector2(0.5f, 0f);
            btnRT.pivot = new Vector2(0.5f, 0f);
            btnRT.anchoredPosition = pos;
            btnRT.sizeDelta = size;

            Image btnImg = btnGO.AddComponent<Image>();
            btnImg.sprite = glowFrame;
            btnImg.type = Image.Type.Sliced;
            btnImg.color = darkChassis;

            Button btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = btnImg;

            ColorBlock cb = btn.colors;
            cb.normalColor = darkChassis;
            cb.highlightedColor = new Color(themeColor.r * 0.25f, themeColor.g * 0.25f, themeColor.b * 0.25f, 0.98f);
            cb.pressedColor = new Color(themeColor.r * 0.5f, themeColor.g * 0.5f, themeColor.b * 0.5f, 1f);
            cb.selectedColor = cb.normalColor;
            cb.fadeDuration = 0.1f;
            btn.colors = cb;

            btn.onClick.AddListener(() =>
            {
                PlayBeepSound(950f, 0.08f);
                onClick?.Invoke();
            });

            // Title Text inside Button
            GameObject titleGO = new GameObject("Title_Label");
            titleGO.transform.SetParent(btnRT, false);
            RectTransform titleRT = titleGO.AddComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0f, 0.45f);
            titleRT.anchorMax = new Vector2(1f, 1f);
            titleRT.offsetMin = new Vector2(20f, 0f);
            titleRT.offsetMax = new Vector2(-20f, -8f);

            Text titleText = titleGO.AddComponent<Text>();
            titleText.font = cyberFont;
            titleText.fontSize = 24;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.text = title;
            titleText.color = themeColor;
            titleText.raycastTarget = false;

            // Subtitle Text inside Button
            GameObject subGO = new GameObject("Subtitle_Label");
            subGO.transform.SetParent(btnRT, false);
            RectTransform subRT = subGO.AddComponent<RectTransform>();
            subRT.anchorMin = new Vector2(0f, 0f);
            subRT.anchorMax = new Vector2(1f, 0.45f);
            subRT.offsetMin = new Vector2(20f, 6f);
            subRT.offsetMax = new Vector2(-20f, 0f);

            Text subText = subGO.AddComponent<Text>();
            subText.font = cyberFont;
            subText.fontSize = 13;
            subText.alignment = TextAnchor.MiddleCenter;
            subText.text = subtitle;
            subText.color = new Color(1f, 1f, 1f, 0.7f);
            subText.raycastTarget = false;

            // Add Event Trigger for Hover Sound Feedback
            EventTrigger trigger = btnGO.AddComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerEnter;
            entry.callback.AddListener((eventData) => { PlayBeepSound(650f, 0.03f); });
            trigger.triggers.Add(entry);

            return btn;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // LEVEL SELECT SUB-PANEL (MODAL)
        // ─────────────────────────────────────────────────────────────────────────

        private void BuildLevelSelectPanel(RectTransform parent)
        {
            GameObject modalGO = new GameObject("Level_Select_Panel");
            modalGO.transform.SetParent(parent, false);
            levelSelectPanelRoot = modalGO.AddComponent<RectTransform>();
            levelSelectPanelRoot.anchorMin = new Vector2(0.5f, 0.5f);
            levelSelectPanelRoot.anchorMax = new Vector2(0.5f, 0.5f);
            levelSelectPanelRoot.pivot = new Vector2(0.5f, 0.5f);
            levelSelectPanelRoot.anchoredPosition = new Vector2(0f, -10f);
            levelSelectPanelRoot.sizeDelta = new Vector2(760f, 720f);

            Image modalBg = modalGO.AddComponent<Image>();
            modalBg.sprite = techFrameRed;
            modalBg.type = Image.Type.Sliced;
            modalBg.color = darkGlassPanel;

            // Modal Header
            GameObject headerGO = new GameObject("Modal_Header");
            headerGO.transform.SetParent(levelSelectPanelRoot, false);
            RectTransform headerRT = headerGO.AddComponent<RectTransform>();
            headerRT.anchorMin = new Vector2(0.5f, 1f);
            headerRT.anchorMax = new Vector2(0.5f, 1f);
            headerRT.pivot = new Vector2(0.5f, 1f);
            headerRT.anchoredPosition = new Vector2(0f, -25f);
            headerRT.sizeDelta = new Vector2(700f, 80f);

            Text headerTitle = headerGO.AddComponent<Text>();
            headerTitle.font = cyberFont;
            headerTitle.fontSize = 32;
            headerTitle.fontStyle = FontStyle.Bold;
            headerTitle.alignment = TextAnchor.MiddleCenter;
            headerTitle.text = "⚠️  SELEZIONE SETTORE OPERATIVO  ⚠️";
            headerTitle.color = neonRed;
            headerTitle.raycastTarget = false;

            GameObject subGO = new GameObject("Modal_Subtitle");
            subGO.transform.SetParent(headerRT, false);
            RectTransform subRT = subGO.AddComponent<RectTransform>();
            subRT.anchorMin = new Vector2(0.5f, 0f);
            subRT.anchorMax = new Vector2(0.5f, 0f);
            subRT.pivot = new Vector2(0.5f, 0f);
            subRT.anchoredPosition = new Vector2(0f, 4f);
            subRT.sizeDelta = new Vector2(650f, 25f);

            Text subText = subGO.AddComponent<Text>();
            subText.font = cyberFont;
            subText.fontSize = 15;
            subText.alignment = TextAnchor.MiddleCenter;
            subText.text = "SELEZIONA IL SETTORE DELLA STRUTTURA IN CUI INTERVENIRE";
            subText.color = cyberCyan;
            subText.raycastTarget = false;

            // 3 Sector Selection Cards
            // 1. Settore 0
            CreateSectorCard(
                parent: levelSelectPanelRoot,
                index: 0,
                sectorName: "SETTORE 0: ACCESSO & CONTENIMENTO",
                statusTag: "[ STATO: OPERATIVO ]",
                description: "Ingresso principale, alimentazione ausiliaria e contenimento breccia iniziale.",
                pos: new Vector2(0f, 420f),
                badgeColor: neonGreen,
                borderSprite: techFrameGreen
            );

            // 2. Settore 1
            CreateSectorCard(
                parent: levelSelectPanelRoot,
                index: 1,
                sectorName: "SETTORE 1: LABORATORI & SICUREZZA",
                statusTag: "[ STATO: ALLERTA ELEVATA ]",
                description: "Area laboratori biologici, terminali di sicurezza e droni ostili di pattuglia.",
                pos: new Vector2(0f, 275f),
                badgeColor: neonYellow,
                borderSprite: techFrameYellow
            );

            // 3. Settore 2
            CreateSectorCard(
                parent: levelSelectPanelRoot,
                index: 2,
                sectorName: "SETTORE 2: NUCLEO REATTORE",
                statusTag: "[ STATO: CRITICO / COLLASSO ]",
                description: "Nucleo reattore a rischio fusione, robot di manutenzione potenziati e timer critico.",
                pos: new Vector2(0f, 130f),
                badgeColor: neonRed,
                borderSprite: techFrameRed
            );

            // Close / Back Button
            CreateStyledNeonButton(
                parent: levelSelectPanelRoot,
                name: "Button_CloseLevelSelect",
                pos: new Vector2(0f, 25f),
                size: new Vector2(380f, 65f),
                title: "❮  TORNA AL MENU",
                subtitle: "[ ANNULLA E TORNA ALLA SCHERMATA PRINCIPALE ]",
                themeColor: neonRed,
                glowFrame: techFrameRed,
                onClick: CloseLevelSelect
            );

            // Hide Level Select initially
            levelSelectPanelRoot.gameObject.SetActive(false);
        }

        private void CreateSectorCard(
            RectTransform parent,
            int index,
            string sectorName,
            string statusTag,
            string description,
            Vector2 pos,
            Color badgeColor,
            Sprite borderSprite)
        {
            GameObject cardGO = new GameObject($"Sector_Card_{index}");
            cardGO.transform.SetParent(parent, false);
            RectTransform cardRT = cardGO.AddComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(0.5f, 0f);
            cardRT.anchorMax = new Vector2(0.5f, 0f);
            cardRT.pivot = new Vector2(0.5f, 0f);
            cardRT.anchoredPosition = pos;
            cardRT.sizeDelta = new Vector2(680f, 120f);

            Image cardBg = cardGO.AddComponent<Image>();
            cardBg.sprite = borderSprite;
            cardBg.type = Image.Type.Sliced;
            cardBg.color = darkChassis;

            // Header line (Sector Name + Badge)
            GameObject titleGO = new GameObject("Sector_Title");
            titleGO.transform.SetParent(cardRT, false);
            RectTransform titleRT = titleGO.AddComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0f, 1f);
            titleRT.anchorMax = new Vector2(1f, 1f);
            titleRT.pivot = new Vector2(0f, 1f);
            titleRT.anchoredPosition = new Vector2(20f, -12f);
            titleRT.sizeDelta = new Vector2(420f, 30f);

            Text titleText = titleGO.AddComponent<Text>();
            titleText.font = cyberFont;
            titleText.fontSize = 20;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.text = sectorName;
            titleText.color = Color.white;
            titleText.raycastTarget = false;

            // Status Badge
            GameObject badgeGO = new GameObject("Sector_Badge");
            badgeGO.transform.SetParent(cardRT, false);
            RectTransform badgeRT = badgeGO.AddComponent<RectTransform>();
            badgeRT.anchorMin = new Vector2(0f, 1f);
            badgeRT.anchorMax = new Vector2(0f, 1f);
            badgeRT.pivot = new Vector2(0f, 1f);
            badgeRT.anchoredPosition = new Vector2(20f, -44f);
            badgeRT.sizeDelta = new Vector2(220f, 22f);

            Text badgeText = badgeGO.AddComponent<Text>();
            badgeText.font = cyberFont;
            badgeText.fontSize = 14;
            badgeText.fontStyle = FontStyle.Bold;
            badgeText.alignment = TextAnchor.MiddleLeft;
            badgeText.text = statusTag;
            badgeText.color = badgeColor;
            badgeText.raycastTarget = false;

            // Description
            GameObject descGO = new GameObject("Sector_Desc");
            descGO.transform.SetParent(cardRT, false);
            RectTransform descRT = descGO.AddComponent<RectTransform>();
            descRT.anchorMin = new Vector2(0f, 0f);
            descRT.anchorMax = new Vector2(0f, 0f);
            descRT.pivot = new Vector2(0f, 0f);
            descRT.anchoredPosition = new Vector2(20f, 12f);
            descRT.sizeDelta = new Vector2(440f, 36f);

            Text descText = descGO.AddComponent<Text>();
            descText.font = cyberFont;
            descText.fontSize = 13;
            descText.alignment = TextAnchor.MiddleLeft;
            descText.text = description;
            descText.color = new Color(0.75f, 0.85f, 0.95f, 0.8f);
            descText.raycastTarget = false;

            // Launch Button on Right
            GameObject launchBtnGO = new GameObject($"Button_Launch_Sector_{index}");
            launchBtnGO.transform.SetParent(cardRT, false);
            RectTransform launchBtnRT = launchBtnGO.AddComponent<RectTransform>();
            launchBtnRT.anchorMin = new Vector2(1f, 0.5f);
            launchBtnRT.anchorMax = new Vector2(1f, 0.5f);
            launchBtnRT.pivot = new Vector2(1f, 0.5f);
            launchBtnRT.anchoredPosition = new Vector2(-15f, 0f);
            launchBtnRT.sizeDelta = new Vector2(190f, 75f);

            Image launchImg = launchBtnGO.AddComponent<Image>();
            launchImg.sprite = borderSprite;
            launchImg.type = Image.Type.Sliced;
            launchImg.color = new Color(0.04f, 0.08f, 0.12f, 0.95f);

            Button launchBtn = launchBtnGO.AddComponent<Button>();
            launchBtn.targetGraphic = launchImg;

            ColorBlock cb = launchBtn.colors;
            cb.normalColor = new Color(0.04f, 0.08f, 0.12f, 0.95f);
            cb.highlightedColor = new Color(badgeColor.r * 0.3f, badgeColor.g * 0.3f, badgeColor.b * 0.3f, 1f);
            cb.pressedColor = badgeColor;
            launchBtn.colors = cb;

            int sectorToLoad = index;
            launchBtn.onClick.AddListener(() =>
            {
                PlayBeepSound(1200f, 0.1f);
                LoadSector(sectorToLoad);
            });

            GameObject launchTextGO = new GameObject("Launch_Text");
            launchTextGO.transform.SetParent(launchBtnRT, false);
            RectTransform launchTextRT = launchTextGO.AddComponent<RectTransform>();
            Stretch(launchTextRT);

            Text launchText = launchTextGO.AddComponent<Text>();
            launchText.font = cyberFont;
            launchText.fontSize = 17;
            launchText.fontStyle = FontStyle.Bold;
            launchText.alignment = TextAnchor.MiddleCenter;
            launchText.text = $"⚡ AVVIA\nSETTORE {index}";
            launchText.color = badgeColor;
            launchText.raycastTarget = false;

            // Hover sound
            EventTrigger trigger = launchBtnGO.AddComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerEnter;
            entry.callback.AddListener((e) => { PlayBeepSound(700f, 0.03f); });
            trigger.triggers.Add(entry);
        }

        public void OpenLevelSelect()
        {
            isLevelSelectOpen = true;
            if (mainPanelRoot != null) mainPanelRoot.gameObject.SetActive(false);
            if (levelSelectPanelRoot != null) levelSelectPanelRoot.gameObject.SetActive(true);
        }

        public void CloseLevelSelect()
        {
            isLevelSelectOpen = false;
            if (levelSelectPanelRoot != null) levelSelectPanelRoot.gameObject.SetActive(false);
            if (mainPanelRoot != null) mainPanelRoot.gameObject.SetActive(true);
        }

        private void LoadSector(int index)
        {
            Time.timeScale = 1f;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CaricaSettore(index);
            }
            else
            {
                SceneManager.LoadScene($"settore {index}");
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // BOTTOM FOOTER
        // ─────────────────────────────────────────────────────────────────────────

        private void BuildBottomFooter(RectTransform parent)
        {
            GameObject footerGO = new GameObject("Bottom_Footer_Bar");
            footerGO.transform.SetParent(parent, false);
            RectTransform footerRT = footerGO.AddComponent<RectTransform>();
            footerRT.anchorMin = new Vector2(0f, 0f);
            footerRT.anchorMax = new Vector2(1f, 0f);
            footerRT.pivot = new Vector2(0.5f, 0f);
            footerRT.anchoredPosition = new Vector2(0f, 15f);
            footerRT.sizeDelta = new Vector2(-80f, 40f);

            // Left: Operator Security Signature
            GameObject opGO = new GameObject("Operator_Status");
            opGO.transform.SetParent(footerRT, false);
            RectTransform opRT = opGO.AddComponent<RectTransform>();
            opRT.anchorMin = new Vector2(0f, 0.5f);
            opRT.anchorMax = new Vector2(0f, 0.5f);
            opRT.pivot = new Vector2(0f, 0.5f);
            opRT.anchoredPosition = new Vector2(10f, 0f);
            opRT.sizeDelta = new Vector2(400f, 30f);

            Text opText = opGO.AddComponent<Text>();
            opText.font = cyberFont;
            opText.fontSize = 14;
            opText.alignment = TextAnchor.MiddleLeft;
            opText.text = "OPERATORE: KEYCARD_A01 // AUTORIZZAZIONE OMEGA";
            opText.color = new Color(0.6f, 0.8f, 1f, 0.7f);
            opText.raycastTarget = false;

            // Right: Engine Version
            GameObject verGO = new GameObject("Version_Tag");
            verGO.transform.SetParent(footerRT, false);
            RectTransform verRT = verGO.AddComponent<RectTransform>();
            verRT.anchorMin = new Vector2(1f, 0.5f);
            verRT.anchorMax = new Vector2(1f, 0.5f);
            verRT.pivot = new Vector2(1f, 0.5f);
            verRT.anchoredPosition = new Vector2(-10f, 0f);
            verRT.sizeDelta = new Vector2(300f, 30f);

            Text verText = verGO.AddComponent<Text>();
            verText.font = cyberFont;
            verText.fontSize = 13;
            verText.alignment = TextAnchor.MiddleRight;
            verText.text = "CRISIS ENGINE v2.5 // TACTICAL INTERFACE";
            verText.color = new Color(1f, 1f, 1f, 0.5f);
            verText.raycastTarget = false;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // ACTIONS (NEW GAME, RESUME, QUIT)
        // ─────────────────────────────────────────────────────────────────────────

        public void NewGame()
        {
            Time.timeScale = 1f;
            try { onNewGame?.Invoke(); } catch (Exception e) { Debug.LogWarning(e.Message); }

            if (GameManager.Instance == null)
            {
                GameObject gmObj = new GameObject("GameManager_AutoCreated");
                gmObj.AddComponent<GameManager>();
            }
            GameManager.Instance.NuovaPartita();
        }

        public void ResumeGame()
        {
            Time.timeScale = 1f;
            try { onResume?.Invoke(); } catch (Exception e) { Debug.LogWarning(e.Message); }

            if (GameManager.Instance == null)
            {
                GameObject gmObj = new GameObject("GameManager_AutoCreated");
                gmObj.AddComponent<GameManager>();
            }
            GameManager.Instance.ResumeSavedGame();
        }

        public void QuitGame()
        {
            Application.Quit();
        }

        public void SetMainMenuBackground(Sprite backgroundSprite)
        {
            mainMenuBackgroundSprite = backgroundSprite;
            BuildNeonMainMenu();
        }

        public void SetBackgroundVideo(UnityEngine.Video.VideoClip clip)
        {
            backgroundVideoClip = clip;
            BuildNeonMainMenu();
        }

        private void OnDestroy()
        {
            if (bgVideoRenderTexture != null)
            {
                if (bgVideoRenderTexture.IsCreated())
                    bgVideoRenderTexture.Release();
                Destroy(bgVideoRenderTexture);
                bgVideoRenderTexture = null;
            }
        }

        private bool HasSaveData()
        {
            string path = Path.Combine(Application.persistentDataPath, "SectorContainment_Save.json");
            return File.Exists(path);
        }

        private int GetSavedSectorIndex()
        {
            try
            {
                string path = Path.Combine(Application.persistentDataPath, "SectorContainment_Save.json");
                if (File.Exists(path))
                {
                    SaveDataWrapper data = JsonUtility.FromJson<SaveDataWrapper>(File.ReadAllText(path));
                    if (data != null) return data.lastSectorIndex;
                }
            }
            catch { }
            return 0;
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
    }
}
