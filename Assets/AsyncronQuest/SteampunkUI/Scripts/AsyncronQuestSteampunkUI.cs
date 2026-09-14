// ============================================================================
// Crisis Protocol / Sector Containment - UI e feedback AsyncronQuest
// File: .\Assets\AsyncronQuest\SteampunkUI\Scripts\AsyncronQuestSteampunkUI.cs
// Responsabilita': fornisce schermate, tooltip, transizioni, menu e feedback visivi integrati nel progetto Crisis Protocol.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using System; // usa lib // riga-ok
using System.IO; // usa lib // riga-ok
using CrisisProtocol.UI; // usa lib // riga-ok
using UnityEngine; // usa lib // riga-ok
using UnityEngine.Events; // usa lib // riga-ok
using UnityEngine.EventSystems; // usa lib // riga-ok
using UnityEngine.SceneManagement; // usa lib // riga-ok
using UnityEngine.UI; // usa lib // riga-ok

namespace AsyncronQuest.SteampunkUI // zona cod // riga-ok
{ // apre // riga-ok
    [DisallowMultipleComponent] // nota unity // riga-ok
    // blocco: classe x roba grossa
    public sealed class AsyncronQuestSteampunkUI : MonoBehaviour // classe qui // riga-ok
    { // apre // riga-ok
        [Header("Scene Configuration")] // nota unity // riga-ok
        [SerializeField] private string newGameSceneName = "settore 0"; // setta // riga-ok
        [SerializeField] private UnityEvent onNewGame; // ok qua // riga-ok
        [SerializeField] private UnityEvent onResume; // ok qua // riga-ok

        [Header("Localization / Text")] // nota unity // riga-ok
        [SerializeField] private string languageCode = "It"; // setta // riga-ok

        [Header("Canvas Settings")] // nota unity // riga-ok
        [SerializeField] private int canvasSortingOrder = 50; // setta // riga-ok
        [SerializeField] private Vector2 canvasReferenceResolution = new Vector2(1920f, 1080f); // setta // riga-ok

        [Header("Neon Color Palette")] // nota unity // riga-ok
        public Color neonGreen = new Color(0.0f, 1.0f, 0.45f, 1.0f);        // #00FF73 // roba pub // riga-ok
        public Color neonGreenGlow = new Color(0.0f, 1.0f, 0.45f, 0.25f); // roba pub // riga-ok
        public Color neonYellow = new Color(1.0f, 0.95f, 0.05f, 1.0f);       // #FFF20D Fluorescent Yellow // roba pub // riga-ok
        public Color neonYellowGlow = new Color(1.0f, 0.95f, 0.05f, 0.25f); // roba pub // riga-ok
        public Color neonRed = new Color(1.0f, 0.08f, 0.28f, 1.0f);          // #FF1447 Neon Red // roba pub // riga-ok
        public Color neonRedGlow = new Color(1.0f, 0.08f, 0.28f, 0.25f); // roba pub // riga-ok
        public Color cyberCyan = new Color(0.25f, 0.9f, 1.0f, 1.0f); // roba pub // riga-ok
        public Color darkChassis = new Color(0.02f, 0.045f, 0.08f, 0.94f); // roba pub // riga-ok
        public Color darkGlassPanel = new Color(0.015f, 0.03f, 0.06f, 0.88f); // roba pub // riga-ok

        [Header("Background Video / Neon")] // nota unity // riga-ok
        [SerializeField] private UnityEngine.Video.VideoClip backgroundVideoClip; // ok qua // riga-ok
        [SerializeField] private Sprite mainMenuBackgroundSprite; // ok qua // riga-ok
        [SerializeField, Range(0f, 1f)] private float backgroundImageAlpha = 0.35f; // setta // riga-ok

        private const string DefaultNeonVideoPath = "Assets/AsyncronQuest/SteampunkUI/UI_Style/DEVE_ESSERE_SOLO_IL_NEON_NENTE.mp4"; // roba pub // riga-ok
        private const string InputSystemUiModuleTypeName = "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem"; // roba pub // riga-ok
        private const string MasterVolumePrefKey = "MasterVolume"; // roba pub // riga-ok
        private const string PreMuteVolumePrefKey = "PreMuteVolume"; // roba pub // riga-ok

        private Canvas mainCanvas; // roba pub // riga-ok
        private RectTransform mainPanelRoot; // roba pub // riga-ok
        private RectTransform levelSelectPanelRoot; // roba pub // riga-ok
        private Text volumePercentText; // roba pub // riga-ok
        private Text volumeIconText; // roba pub // riga-ok
        private Slider volumeSliderComponent; // roba pub // riga-ok
        private Image titleGlowEffect; // roba pub // riga-ok
        private AudioSource uiAudioSource; // roba pub // riga-ok
        private UnityEngine.Video.VideoPlayer bgVideoPlayer; // roba pub // riga-ok
        private RenderTexture bgVideoRenderTexture; // roba pub // riga-ok

        private Sprite solidSprite; // roba pub // riga-ok
        private Sprite techFrameGreen; // roba pub // riga-ok
        private Sprite techFrameYellow; // roba pub // riga-ok
        private Sprite techFrameRed; // roba pub // riga-ok
        private Sprite techFrameGlass; // roba pub // riga-ok
        private Font cyberFont; // roba pub // riga-ok

        private bool isLevelSelectOpen = false; // roba pub // riga-ok

        // blocco: funzione fa cose
        private void Awake() // roba pub // riga-ok
        { // apre // riga-ok
            Time.timeScale = 1f; // setta // riga-ok
            Cursor.lockState = CursorLockMode.None; // setta // riga-ok
            Cursor.visible = true; // setta // riga-ok

            ModalUIState.ForceCloseAll(); // chiama // riga-ok

            // Setup audio listener
            float currentVol = PlayerPrefs.GetFloat(MasterVolumePrefKey, 1.0f); // setta // riga-ok
            AudioListener.volume = currentVol; // setta // riga-ok
            AudioListener.pause = false; // setta // riga-ok

            InitAudioSource(); // chiama // riga-ok
            GenerateProceduralSprites(); // chiama // riga-ok
            BuildNeonMainMenu(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void Start() // roba pub // riga-ok
        { // apre // riga-ok
            Cursor.lockState = CursorLockMode.None; // setta // riga-ok
            Cursor.visible = true; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void Update() // roba pub // riga-ok
        { // apre // riga-ok
            // Keep cursor active in main menu
            // blocco: controlla se va
            if (Cursor.lockState != CursorLockMode.None || !Cursor.visible) // se ok // riga-ok
            { // apre // riga-ok
                Cursor.lockState = CursorLockMode.None; // setta // riga-ok
                Cursor.visible = true; // setta // riga-ok
            } // chiude // riga-ok

            // Pulsing Neon Glow effects
            float pulse = 0.75f + Mathf.Sin(Time.unscaledTime * 3.5f) * 0.25f; // setta // riga-ok
            // blocco: controlla se va
            if (titleGlowEffect != null) // se ok // riga-ok
            { // apre // riga-ok
                Color c = neonYellow; // setta // riga-ok
                c.a = pulse * 0.45f; // setta // riga-ok
                titleGlowEffect.color = c; // setta // riga-ok
            } // chiude // riga-ok

            // blocco: controlla se va
            if (Input.GetKeyDown(KeyCode.Escape) && isLevelSelectOpen) // se ok // riga-ok
            { // apre // riga-ok
                CloseLevelSelect(); // chiama // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void InitAudioSource() // roba pub // riga-ok
        { // apre // riga-ok
            uiAudioSource = gameObject.GetComponent<AudioSource>(); // setta // riga-ok
            // blocco: controlla se va
            if (uiAudioSource == null) // se ok // riga-ok
                uiAudioSource = gameObject.AddComponent<AudioSource>(); // setta // riga-ok

            uiAudioSource.playOnAwake = false; // setta // riga-ok
            uiAudioSource.ignoreListenerPause = true; // setta // riga-ok
            uiAudioSource.volume = 0.7f; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public void PlayBeepSound(float frequency = 880f, float duration = 0.06f) // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (uiAudioSource == null) return; // se ok // riga-ok
            AudioClip clip = CreateToneClip(frequency, duration); // setta // riga-ok
            uiAudioSource.PlayOneShot(clip); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private AudioClip CreateToneClip(float freq, float duration) // roba pub // riga-ok
        { // apre // riga-ok
            int sampleRate = 44100; // setta // riga-ok
            int sampleCount = (int)(sampleRate * duration); // setta // riga-ok
            float[] samples = new float[sampleCount]; // setta // riga-ok

            // blocco: gira piu volte
            for (int i = 0; i < sampleCount; i++) // ciclo x // riga-ok
            { // apre // riga-ok
                float t = (float)i / sampleRate; // setta // riga-ok
                float envelope = 1f - (float)i / sampleCount; // setta // riga-ok
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.35f; // setta // riga-ok
            } // chiude // riga-ok

            AudioClip clip = AudioClip.Create("UI_Tone", sampleCount, 1, sampleRate, false); // setta // riga-ok
            clip.SetData(samples, 0); // chiama // riga-ok
            return clip; // torna val // riga-ok
        } // chiude // riga-ok

        // ─────────────────────────────────────────────────────────────────────────
        // SPRITE GENERATION
        // ─────────────────────────────────────────────────────────────────────────

        // blocco: funzione fa cose
        private void GenerateProceduralSprites() // roba pub // riga-ok
        { // apre // riga-ok
            cyberFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // setta // riga-ok
            // blocco: controlla se va
            if (cyberFont == null) cyberFont = Resources.GetBuiltinResource<Font>("Arial.ttf"); // se ok // riga-ok

            // 1. Solid Sprite
            Texture2D solidTex = new Texture2D(2, 2); // setta // riga-ok
            solidTex.SetPixels(new Color[] { Color.white, Color.white, Color.white, Color.white }); // chiama // riga-ok
            solidTex.Apply(); // chiama // riga-ok
            solidSprite = Sprite.Create(solidTex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f)); // setta // riga-ok

            // 2. Tech Frames
            techFrameGreen = CreateTechBorderSprite(neonGreen, new Color(0f, 0.2f, 0.1f, 0.35f)); // setta // riga-ok
            techFrameYellow = CreateTechBorderSprite(neonYellow, new Color(0.2f, 0.18f, 0f, 0.35f)); // setta // riga-ok
            techFrameRed = CreateTechBorderSprite(neonRed, new Color(0.25f, 0.02f, 0.05f, 0.35f)); // setta // riga-ok
            techFrameGlass = CreateTechBorderSprite(new Color(0.3f, 0.8f, 1f, 0.6f), new Color(0.01f, 0.025f, 0.05f, 0.85f)); // setta // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private Sprite CreateTechBorderSprite(Color borderColor, Color innerBg) // roba pub // riga-ok
        { // apre // riga-ok
            int w = 32; // setta // riga-ok
            int h = 32; // setta // riga-ok
            Texture2D tex = new Texture2D(w, h); // setta // riga-ok
            tex.filterMode = FilterMode.Point; // setta // riga-ok
            Color[] pixels = new Color[w * h]; // setta // riga-ok

            // blocco: gira piu volte
            for (int y = 0; y < h; y++) // ciclo x // riga-ok
            { // apre // riga-ok
                // blocco: gira piu volte
                for (int x = 0; x < w; x++) // ciclo x // riga-ok
                { // apre // riga-ok
                    bool isBorderX = (x == 0 || x == 1 || x == w - 2 || x == w - 1); // setta // riga-ok
                    bool isBorderY = (y == 0 || y == 1 || y == h - 2 || y == h - 1); // setta // riga-ok
                    bool isCorner = (x < 5 || x >= w - 5) && (y < 5 || y >= h - 5); // setta // riga-ok

                    // blocco: controlla se va
                    if (isCorner && (isBorderX || isBorderY)) // se ok // riga-ok
                    { // apre // riga-ok
                        pixels[y * w + x] = borderColor; // setta // riga-ok
                    } // chiude // riga-ok
                    // blocco: controlla se va
                    else if (isBorderX || isBorderY) // se ok // riga-ok
                    { // apre // riga-ok
                        Color edge = borderColor; // setta // riga-ok
                        edge.a = 0.8f; // setta // riga-ok
                        pixels[y * w + x] = edge; // setta // riga-ok
                    } // chiude // riga-ok
                    // blocco: caso diverso
                    else // se no // riga-ok
                    { // apre // riga-ok
                        pixels[y * w + x] = innerBg; // setta // riga-ok
                    } // chiude // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok

            tex.SetPixels(pixels); // chiama // riga-ok
            tex.Apply(); // chiama // riga-ok
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(6, 6, 6, 6)); // torna val // riga-ok
        } // chiude // riga-ok

        // ─────────────────────────────────────────────────────────────────────────
        // UI CONSTRUCTION
        // ─────────────────────────────────────────────────────────────────────────

        // blocco: funzione fa cose
        public void RebuildMainMenu() // roba pub // riga-ok
        { // apre // riga-ok
            BuildNeonMainMenu(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void BuildNeonMainMenu() // roba pub // riga-ok
        { // apre // riga-ok
            ClearGeneratedInterface(); // chiama // riga-ok
            EnsureEventSystem(); // chiama // riga-ok

            // Main Screen-Space Overlay Canvas
            GameObject canvasGO = new GameObject("NeonMainMenu_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // setta // riga-ok
            canvasGO.transform.SetParent(transform, false); // chiama // riga-ok

            mainCanvas = canvasGO.GetComponent<Canvas>(); // setta // riga-ok
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay; // setta // riga-ok
            mainCanvas.sortingOrder = canvasSortingOrder; // setta // riga-ok

            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>(); // setta // riga-ok
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // setta // riga-ok
            scaler.referenceResolution = canvasReferenceResolution; // setta // riga-ok
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // setta // riga-ok
            scaler.matchWidthOrHeight = 1.0f; // setta // riga-ok

            RectTransform rootRT = canvasGO.GetComponent<RectTransform>(); // setta // riga-ok

            // 1. Background Cybernetic Grid & Overlay
            BuildBackground(rootRT); // chiama // riga-ok

            // 2. Top Header Bar with Tactical Info & Volume Controls
            BuildTopHeader(rootRT); // chiama // riga-ok

            // 3. Central Main Hub (Title + 3 Main Neon Buttons)
            BuildCentralHub(rootRT); // chiama // riga-ok

            // 4. Level Select Sub-Panel (Modal)
            BuildLevelSelectPanel(rootRT); // chiama // riga-ok

            // 5. Bottom Tactical Status Bar
            BuildBottomFooter(rootRT); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void BuildBackground(RectTransform parent) // roba pub // riga-ok
        { // apre // riga-ok
            // Dark base background
            GameObject bgGO = new GameObject("Cyber_Background"); // setta // riga-ok
            bgGO.transform.SetParent(parent, false); // chiama // riga-ok
            RectTransform bgRT = bgGO.AddComponent<RectTransform>(); // setta // riga-ok
            Stretch(bgRT); // chiama // riga-ok

            Image bgImg = bgGO.AddComponent<Image>(); // setta // riga-ok
            bgImg.sprite = solidSprite; // setta // riga-ok
            bgImg.color = new Color(0.01f, 0.02f, 0.04f, 1f); // setta // riga-ok
            bgImg.raycastTarget = false; // setta // riga-ok

            // ─────────────────────────────────────────────────────────────────
            // BACKGROUND VIDEO PLAYER (DEVE_ESSERE_SOLO_IL_NEON_NENTE.mp4)
            // ─────────────────────────────────────────────────────────────────
            SetupVideoBackground(bgRT); // chiama // riga-ok

            // Optional static background artwork fallback (only if video is missing)
            // blocco: controlla se va
            if (bgVideoPlayer == null && mainMenuBackgroundSprite != null) // se ok // riga-ok
            { // apre // riga-ok
                GameObject bgTexGO = new GameObject("Background_Artwork"); // setta // riga-ok
                bgTexGO.transform.SetParent(bgRT, false); // chiama // riga-ok
                RectTransform bgTexRT = bgTexGO.AddComponent<RectTransform>(); // setta // riga-ok
                Stretch(bgTexRT); // chiama // riga-ok
                Image artwork = bgTexGO.AddComponent<Image>(); // setta // riga-ok
                artwork.sprite = mainMenuBackgroundSprite; // setta // riga-ok
                artwork.color = new Color(1f, 1f, 1f, backgroundImageAlpha); // setta // riga-ok
                artwork.preserveAspect = false; // setta // riga-ok
                artwork.raycastTarget = false; // setta // riga-ok
            } // chiude // riga-ok

            // Cyber Scanlines (Subtle overlay over video)
            GameObject linesGO = new GameObject("Cyber_Scanlines"); // setta // riga-ok
            linesGO.transform.SetParent(bgRT, false); // chiama // riga-ok
            RectTransform linesRT = linesGO.AddComponent<RectTransform>(); // setta // riga-ok
            Stretch(linesRT); // chiama // riga-ok

            // blocco: gira piu volte
            for (int i = 0; i < 28; i++) // ciclo x // riga-ok
            { // apre // riga-ok
                GameObject line = new GameObject("Scanline_" + i); // setta // riga-ok
                line.transform.SetParent(linesRT, false); // chiama // riga-ok
                RectTransform lineRT = line.AddComponent<RectTransform>(); // setta // riga-ok
                lineRT.anchorMin = new Vector2(0f, 0.5f); // setta // riga-ok
                lineRT.anchorMax = new Vector2(1f, 0.5f); // setta // riga-ok
                lineRT.pivot = new Vector2(0.5f, 0.5f); // setta // riga-ok
                lineRT.sizeDelta = new Vector2(0f, 2f); // setta // riga-ok
                lineRT.anchoredPosition = new Vector2(0f, -500f + i * 38f); // setta // riga-ok

                Image lineImg = line.AddComponent<Image>(); // setta // riga-ok
                lineImg.sprite = solidSprite; // setta // riga-ok
                Color lc = (i % 3 == 0) ? neonGreen : (i % 3 == 1 ? neonYellow : neonRed); // setta // riga-ok
                lc.a = 0.025f; // setta // riga-ok
                lineImg.color = lc; // setta // riga-ok
                lineImg.raycastTarget = false; // setta // riga-ok
            } // chiude // riga-ok

            // Corner perimeter cyber brackets
            CreateCornerBracket(parent, new Vector2(30f, -30f), new Vector2(0f, 1f), new Vector2(0f, 1f), neonGreen); // chiama // riga-ok
            CreateCornerBracket(parent, new Vector2(-30f, -30f), new Vector2(1f, 1f), new Vector2(1f, 1f), neonYellow); // chiama // riga-ok
            CreateCornerBracket(parent, new Vector2(30f, 30f), new Vector2(0f, 0f), new Vector2(0f, 0f), neonRed); // chiama // riga-ok
            CreateCornerBracket(parent, new Vector2(-30f, 30f), new Vector2(1f, 0f), new Vector2(1f, 0f), neonGreen); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void SetupVideoBackground(RectTransform bgParent) // roba pub // riga-ok
        { // apre // riga-ok
            UnityEngine.Video.VideoClip clipToPlay = backgroundVideoClip; // setta // riga-ok

#if UNITY_EDITOR // prep ok // riga-ok
            // blocco: controlla se va
            if (clipToPlay == null) // se ok // riga-ok
            { // apre // riga-ok
                clipToPlay = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>(DefaultNeonVideoPath); // setta // riga-ok
                backgroundVideoClip = clipToPlay; // setta // riga-ok
            } // chiude // riga-ok
#endif // prep ok // riga-ok

            string videoFilePath = Path.Combine(Application.dataPath, "AsyncronQuest/SteampunkUI/UI_Style/DEVE_ESSERE_SOLO_IL_NEON_NENTE.mp4"); // setta // riga-ok
            bool hasValidSource = (clipToPlay != null) || File.Exists(videoFilePath); // setta // riga-ok

            // blocco: controlla se va
            if (!hasValidSource) // se ok // riga-ok
                return; // torna val // riga-ok

            // blocco: controlla se va
            if (bgVideoRenderTexture == null || !bgVideoRenderTexture.IsCreated()) // se ok // riga-ok
            { // apre // riga-ok
                bgVideoRenderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32); // setta // riga-ok
                bgVideoRenderTexture.name = "Neon_MainMenu_VideoRT"; // setta // riga-ok
                bgVideoRenderTexture.wrapMode = TextureWrapMode.Clamp; // setta // riga-ok
                bgVideoRenderTexture.Create(); // chiama // riga-ok
            } // chiude // riga-ok

            GameObject vpGO = new GameObject("Background_VideoPlayer"); // setta // riga-ok
            vpGO.transform.SetParent(transform, false); // chiama // riga-ok
            bgVideoPlayer = vpGO.AddComponent<UnityEngine.Video.VideoPlayer>(); // setta // riga-ok
            bgVideoPlayer.playOnAwake = true; // setta // riga-ok
            bgVideoPlayer.isLooping = true; // setta // riga-ok
            bgVideoPlayer.renderMode = UnityEngine.Video.VideoRenderMode.RenderTexture; // setta // riga-ok
            bgVideoPlayer.targetTexture = bgVideoRenderTexture; // setta // riga-ok
            bgVideoPlayer.aspectRatio = UnityEngine.Video.VideoAspectRatio.FitHorizontally; // setta // riga-ok
            bgVideoPlayer.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.None; // setta // riga-ok

            // blocco: controlla se va
            if (clipToPlay != null) // se ok // riga-ok
            { // apre // riga-ok
                bgVideoPlayer.source = UnityEngine.Video.VideoSource.VideoClip; // setta // riga-ok
                bgVideoPlayer.clip = clipToPlay; // setta // riga-ok
            } // chiude // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
            { // apre // riga-ok
                bgVideoPlayer.source = UnityEngine.Video.VideoSource.Url; // setta // riga-ok
                bgVideoPlayer.url = videoFilePath; // setta // riga-ok
            } // chiude // riga-ok

            // RawImage to display the video texture
            GameObject rawGO = new GameObject("Background_Video_RawImage"); // setta // riga-ok
            rawGO.transform.SetParent(bgParent, false); // chiama // riga-ok
            RectTransform rawRT = rawGO.AddComponent<RectTransform>(); // setta // riga-ok
            Stretch(rawRT); // chiama // riga-ok

            RawImage rawImage = rawGO.AddComponent<RawImage>(); // setta // riga-ok
            rawImage.texture = bgVideoRenderTexture; // setta // riga-ok
            rawImage.color = Color.white; // setta // riga-ok
            rawImage.raycastTarget = false; // setta // riga-ok

            bgVideoPlayer.Play(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void CreateCornerBracket(RectTransform parent, Vector2 pos, Vector2 anchorMin, Vector2 anchorMax, Color color) // roba pub // riga-ok
        { // apre // riga-ok
            GameObject bGO = new GameObject("Corner_Bracket"); // setta // riga-ok
            bGO.transform.SetParent(parent, false); // chiama // riga-ok
            RectTransform rt = bGO.AddComponent<RectTransform>(); // setta // riga-ok
            rt.anchorMin = anchorMin; // setta // riga-ok
            rt.anchorMax = anchorMax; // setta // riga-ok
            rt.pivot = anchorMin; // setta // riga-ok
            rt.anchoredPosition = pos; // setta // riga-ok
            rt.sizeDelta = new Vector2(60f, 60f); // setta // riga-ok

            Image img = bGO.AddComponent<Image>(); // setta // riga-ok
            img.sprite = techFrameGreen; // setta // riga-ok
            img.type = Image.Type.Sliced; // setta // riga-ok
            img.color = color; // setta // riga-ok
            img.raycastTarget = false; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void BuildTopHeader(RectTransform parent) // roba pub // riga-ok
        { // apre // riga-ok
            GameObject headerGO = new GameObject("Top_Header_Bar"); // setta // riga-ok
            headerGO.transform.SetParent(parent, false); // chiama // riga-ok
            RectTransform headerRT = headerGO.AddComponent<RectTransform>(); // setta // riga-ok
            headerRT.anchorMin = new Vector2(0f, 1f); // setta // riga-ok
            headerRT.anchorMax = new Vector2(1f, 1f); // setta // riga-ok
            headerRT.pivot = new Vector2(0.5f, 1f); // setta // riga-ok
            headerRT.anchoredPosition = new Vector2(0f, -15f); // setta // riga-ok
            headerRT.sizeDelta = new Vector2(-80f, 60f); // setta // riga-ok

            // Left: System Status
            GameObject statusGO = new GameObject("Status_Tag"); // setta // riga-ok
            statusGO.transform.SetParent(headerRT, false); // chiama // riga-ok
            RectTransform statusRT = statusGO.AddComponent<RectTransform>(); // setta // riga-ok
            statusRT.anchorMin = new Vector2(0f, 0.5f); // setta // riga-ok
            statusRT.anchorMax = new Vector2(0f, 0.5f); // setta // riga-ok
            statusRT.pivot = new Vector2(0f, 0.5f); // setta // riga-ok
            statusRT.anchoredPosition = new Vector2(10f, 0f); // setta // riga-ok
            statusRT.sizeDelta = new Vector2(360f, 40f); // setta // riga-ok

            Text statusText = statusGO.AddComponent<Text>(); // setta // riga-ok
            statusText.font = cyberFont; // setta // riga-ok
            statusText.fontSize = 17; // setta // riga-ok
            statusText.fontStyle = FontStyle.Bold; // setta // riga-ok
            statusText.alignment = TextAnchor.MiddleLeft; // setta // riga-ok
            statusText.text = "● SISTEMA ATTIVO // CANALE SICUREZZA 01"; // setta // riga-ok
            statusText.color = neonGreen; // setta // riga-ok
            statusText.raycastTarget = false; // setta // riga-ok

            // Center Tag
            GameObject centerTagGO = new GameObject("Facility_Tag"); // setta // riga-ok
            centerTagGO.transform.SetParent(headerRT, false); // chiama // riga-ok
            RectTransform centerTagRT = centerTagGO.AddComponent<RectTransform>(); // setta // riga-ok
            centerTagRT.anchorMin = new Vector2(0.5f, 0.5f); // setta // riga-ok
            centerTagRT.anchorMax = new Vector2(0.5f, 0.5f); // setta // riga-ok
            centerTagRT.pivot = new Vector2(0.5f, 0.5f); // setta // riga-ok
            centerTagRT.anchoredPosition = Vector2.zero; // setta // riga-ok
            centerTagRT.sizeDelta = new Vector2(450f, 40f); // setta // riga-ok

            Text centerText = centerTagGO.AddComponent<Text>(); // setta // riga-ok
            centerText.font = cyberFont; // setta // riga-ok
            centerText.fontSize = 15; // setta // riga-ok
            centerText.alignment = TextAnchor.MiddleCenter; // setta // riga-ok
            centerText.text = "◆ PROTOCOLLO DI CONTENIMENTO SETTORI v2.5 ◆"; // setta // riga-ok
            centerText.color = new Color(1f, 1f, 1f, 0.65f); // setta // riga-ok
            centerText.raycastTarget = false; // setta // riga-ok

            // ─────────────────────────────────────────────────────────────────
            // Right: MASTER VOLUME CONTROL WIDGET (Icon + Interactive Slider)
            // ─────────────────────────────────────────────────────────────────
            BuildVolumeControlWidget(headerRT); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void BuildVolumeControlWidget(RectTransform headerParent) // roba pub // riga-ok
        { // apre // riga-ok
            GameObject volWidgetGO = new GameObject("Volume_Control_Widget"); // setta // riga-ok
            volWidgetGO.transform.SetParent(headerParent, false); // chiama // riga-ok
            RectTransform volRT = volWidgetGO.AddComponent<RectTransform>(); // setta // riga-ok
            volRT.anchorMin = new Vector2(1f, 0.5f); // setta // riga-ok
            volRT.anchorMax = new Vector2(1f, 0.5f); // setta // riga-ok
            volRT.pivot = new Vector2(1f, 0.5f); // setta // riga-ok
            volRT.anchoredPosition = new Vector2(-10f, 0f); // setta // riga-ok
            volRT.sizeDelta = new Vector2(380f, 48f); // setta // riga-ok

            // Widget dark glass backing with neon green border
            Image widgetBg = volWidgetGO.AddComponent<Image>(); // setta // riga-ok
            widgetBg.sprite = techFrameGreen; // setta // riga-ok
            widgetBg.type = Image.Type.Sliced; // setta // riga-ok
            widgetBg.color = new Color(0.02f, 0.05f, 0.09f, 0.95f); // setta // riga-ok

            // 1. Volume Icon / Mute Button
            GameObject iconBtnGO = new GameObject("Volume_Icon_Button"); // setta // riga-ok
            iconBtnGO.transform.SetParent(volRT, false); // chiama // riga-ok
            RectTransform iconBtnRT = iconBtnGO.AddComponent<RectTransform>(); // setta // riga-ok
            iconBtnRT.anchorMin = new Vector2(0f, 0.5f); // setta // riga-ok
            iconBtnRT.anchorMax = new Vector2(0f, 0.5f); // setta // riga-ok
            iconBtnRT.pivot = new Vector2(0f, 0.5f); // setta // riga-ok
            iconBtnRT.anchoredPosition = new Vector2(10f, 0f); // setta // riga-ok
            iconBtnRT.sizeDelta = new Vector2(40f, 36f); // setta // riga-ok

            Image iconBtnImg = iconBtnGO.AddComponent<Image>(); // setta // riga-ok
            iconBtnImg.sprite = solidSprite; // setta // riga-ok
            iconBtnImg.color = new Color(0.08f, 0.2f, 0.15f, 0.5f); // setta // riga-ok

            Button iconBtn = iconBtnGO.AddComponent<Button>(); // setta // riga-ok
            iconBtn.onClick.AddListener(ToggleVolumeMute); // chiama // riga-ok

            GameObject iconTextGO = new GameObject("Icon_Text"); // setta // riga-ok
            iconTextGO.transform.SetParent(iconBtnRT, false); // chiama // riga-ok
            RectTransform iconTextRT = iconTextGO.AddComponent<RectTransform>(); // setta // riga-ok
            Stretch(iconTextRT); // chiama // riga-ok

            volumeIconText = iconTextGO.AddComponent<Text>(); // setta // riga-ok
            volumeIconText.font = cyberFont; // setta // riga-ok
            volumeIconText.fontSize = 20; // setta // riga-ok
            volumeIconText.alignment = TextAnchor.MiddleCenter; // setta // riga-ok
            volumeIconText.text = "🔊"; // setta // riga-ok
            volumeIconText.color = neonYellow; // setta // riga-ok
            volumeIconText.raycastTarget = false; // setta // riga-ok

            // 2. Volume Slider Component
            GameObject sliderGO = new GameObject("Master_Volume_Slider"); // setta // riga-ok
            sliderGO.transform.SetParent(volRT, false); // chiama // riga-ok
            RectTransform sliderRT = sliderGO.AddComponent<RectTransform>(); // setta // riga-ok
            sliderRT.anchorMin = new Vector2(0f, 0.5f); // setta // riga-ok
            sliderRT.anchorMax = new Vector2(0f, 0.5f); // setta // riga-ok
            sliderRT.pivot = new Vector2(0f, 0.5f); // setta // riga-ok
            sliderRT.anchoredPosition = new Vector2(60f, 0f); // setta // riga-ok
            sliderRT.sizeDelta = new Vector2(210f, 20f); // setta // riga-ok

            Slider slider = sliderGO.AddComponent<Slider>(); // setta // riga-ok
            slider.minValue = 0f; // setta // riga-ok
            slider.maxValue = 1f; // setta // riga-ok
            slider.wholeNumbers = false; // setta // riga-ok

            // Slider Background Track
            GameObject bgTrackGO = new GameObject("Background_Track"); // setta // riga-ok
            bgTrackGO.transform.SetParent(sliderRT, false); // chiama // riga-ok
            RectTransform bgTrackRT = bgTrackGO.AddComponent<RectTransform>(); // setta // riga-ok
            Stretch(bgTrackRT); // chiama // riga-ok
            Image bgTrackImg = bgTrackGO.AddComponent<Image>(); // setta // riga-ok
            bgTrackImg.sprite = solidSprite; // setta // riga-ok
            bgTrackImg.color = new Color(0.05f, 0.1f, 0.15f, 0.9f); // setta // riga-ok

            // Slider Fill Area
            GameObject fillAreaGO = new GameObject("Fill Area"); // setta // riga-ok
            fillAreaGO.transform.SetParent(sliderRT, false); // chiama // riga-ok
            RectTransform fillAreaRT = fillAreaGO.AddComponent<RectTransform>(); // setta // riga-ok
            fillAreaRT.anchorMin = new Vector2(0f, 0.25f); // setta // riga-ok
            fillAreaRT.anchorMax = new Vector2(1f, 0.75f); // setta // riga-ok
            fillAreaRT.offsetMin = new Vector2(4f, 0f); // setta // riga-ok
            fillAreaRT.offsetMax = new Vector2(-4f, 0f); // setta // riga-ok

            GameObject fillGO = new GameObject("Fill"); // setta // riga-ok
            fillGO.transform.SetParent(fillAreaRT, false); // chiama // riga-ok
            RectTransform fillRT = fillGO.AddComponent<RectTransform>(); // setta // riga-ok
            fillRT.sizeDelta = Vector2.zero; // setta // riga-ok
            Image fillImg = fillGO.AddComponent<Image>(); // setta // riga-ok
            fillImg.sprite = solidSprite; // setta // riga-ok
            fillImg.color = neonGreen; // setta // riga-ok

            slider.fillRect = fillRT; // setta // riga-ok

            // Slider Handle Area
            GameObject handleAreaGO = new GameObject("Handle Slide Area"); // setta // riga-ok
            handleAreaGO.transform.SetParent(sliderRT, false); // chiama // riga-ok
            RectTransform handleAreaRT = handleAreaGO.AddComponent<RectTransform>(); // setta // riga-ok
            Stretch(handleAreaRT); // chiama // riga-ok
            handleAreaRT.offsetMin = new Vector2(8f, 0f); // setta // riga-ok
            handleAreaRT.offsetMax = new Vector2(-8f, 0f); // setta // riga-ok

            GameObject handleGO = new GameObject("Handle"); // setta // riga-ok
            handleGO.transform.SetParent(handleAreaGO.transform, false); // chiama // riga-ok
            RectTransform handleRT = handleGO.AddComponent<RectTransform>(); // setta // riga-ok
            handleRT.sizeDelta = new Vector2(18f, 26f); // setta // riga-ok
            Image handleImg = handleGO.AddComponent<Image>(); // setta // riga-ok
            handleImg.sprite = solidSprite; // setta // riga-ok
            handleImg.color = neonYellow; // setta // riga-ok

            slider.handleRect = handleRT; // setta // riga-ok
            slider.targetGraphic = handleImg; // setta // riga-ok

            // Setup current volume
            float savedVol = PlayerPrefs.GetFloat(MasterVolumePrefKey, 1.0f); // setta // riga-ok
            slider.value = savedVol; // setta // riga-ok
            volumeSliderComponent = slider; // setta // riga-ok
            slider.onValueChanged.AddListener(OnVolumeSliderChanged); // chiama // riga-ok

            // 3. Percentage Text
            GameObject percentGO = new GameObject("Volume_Percent_Text"); // setta // riga-ok
            percentGO.transform.SetParent(volRT, false); // chiama // riga-ok
            RectTransform percentRT = percentGO.AddComponent<RectTransform>(); // setta // riga-ok
            percentRT.anchorMin = new Vector2(1f, 0.5f); // setta // riga-ok
            percentRT.anchorMax = new Vector2(1f, 0.5f); // setta // riga-ok
            percentRT.pivot = new Vector2(1f, 0.5f); // setta // riga-ok
            percentRT.anchoredPosition = new Vector2(-12f, 0f); // setta // riga-ok
            percentRT.sizeDelta = new Vector2(80f, 36f); // setta // riga-ok

            volumePercentText = percentGO.AddComponent<Text>(); // setta // riga-ok
            volumePercentText.font = cyberFont; // setta // riga-ok
            volumePercentText.fontSize = 16; // setta // riga-ok
            volumePercentText.fontStyle = FontStyle.Bold; // setta // riga-ok
            volumePercentText.alignment = TextAnchor.MiddleRight; // setta // riga-ok
            volumePercentText.color = neonYellow; // setta // riga-ok
            volumePercentText.raycastTarget = false; // setta // riga-ok

            UpdateVolumeDisplay(savedVol); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void OnVolumeSliderChanged(float val) // roba pub // riga-ok
        { // apre // riga-ok
            val = Mathf.Clamp01(val); // setta // riga-ok
            AudioListener.volume = val; // setta // riga-ok
            PlayerPrefs.SetFloat(MasterVolumePrefKey, val); // chiama // riga-ok
            UpdateVolumeDisplay(val); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void ToggleVolumeMute() // roba pub // riga-ok
        { // apre // riga-ok
            PlayBeepSound(1100f, 0.05f); // chiama // riga-ok
            // blocco: controlla se va
            if (AudioListener.volume > 0.01f) // se ok // riga-ok
            { // apre // riga-ok
                PlayerPrefs.SetFloat(PreMuteVolumePrefKey, AudioListener.volume); // chiama // riga-ok
                SetVolume(0f); // chiama // riga-ok
            } // chiude // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
            { // apre // riga-ok
                float restore = PlayerPrefs.GetFloat(PreMuteVolumePrefKey, 1.0f); // setta // riga-ok
                // blocco: controlla se va
                if (restore <= 0.05f) restore = 1.0f; // se ok // riga-ok
                SetVolume(restore); // chiama // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void SetVolume(float val) // roba pub // riga-ok
        { // apre // riga-ok
            val = Mathf.Clamp01(val); // setta // riga-ok
            AudioListener.volume = val; // setta // riga-ok
            PlayerPrefs.SetFloat(MasterVolumePrefKey, val); // chiama // riga-ok
            // blocco: controlla se va
            if (volumeSliderComponent != null) // se ok // riga-ok
                volumeSliderComponent.value = val; // setta // riga-ok
            UpdateVolumeDisplay(val); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void UpdateVolumeDisplay(float val) // roba pub // riga-ok
        { // apre // riga-ok
            int percent = Mathf.RoundToInt(val * 100f); // setta // riga-ok
            // blocco: controlla se va
            if (volumePercentText != null) // se ok // riga-ok
            { // apre // riga-ok
                volumePercentText.text = percent <= 0 ? "MUTE" : $"{percent}%"; // setta // riga-ok
                volumePercentText.color = percent <= 0 ? neonRed : (percent > 60 ? neonGreen : neonYellow); // setta // riga-ok
            } // chiude // riga-ok

            // blocco: controlla se va
            if (volumeIconText != null) // se ok // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (percent <= 0) // se ok // riga-ok
                    volumeIconText.text = "🔇"; // setta // riga-ok
                // blocco: controlla se va
                else if (percent < 50) // se ok // riga-ok
                    volumeIconText.text = "🔉"; // setta // riga-ok
                // blocco: caso diverso
                else // se no // riga-ok
                    volumeIconText.text = "🔊"; // setta // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // ─────────────────────────────────────────────────────────────────────────
        // CENTRAL HUB (TITLE + 3 NEON MAIN BUTTONS)
        // ─────────────────────────────────────────────────────────────────────────

        // blocco: funzione fa cose
        private void BuildCentralHub(RectTransform parent) // roba pub // riga-ok
        { // apre // riga-ok
            GameObject hubGO = new GameObject("Main_Menu_Panel"); // setta // riga-ok
            hubGO.transform.SetParent(parent, false); // chiama // riga-ok
            mainPanelRoot = hubGO.AddComponent<RectTransform>(); // setta // riga-ok
            mainPanelRoot.anchorMin = new Vector2(0.5f, 0.5f); // setta // riga-ok
            mainPanelRoot.anchorMax = new Vector2(0.5f, 0.5f); // setta // riga-ok
            mainPanelRoot.pivot = new Vector2(0.5f, 0.5f); // setta // riga-ok
            mainPanelRoot.anchoredPosition = new Vector2(0f, -10f); // setta // riga-ok
            mainPanelRoot.sizeDelta = new Vector2(620f, 680f); // setta // riga-ok

            // Translucent dark cyberpunk chassis with glowing tech border
            Image hubBg = hubGO.AddComponent<Image>(); // setta // riga-ok
            hubBg.sprite = techFrameYellow; // setta // riga-ok
            hubBg.type = Image.Type.Sliced; // setta // riga-ok
            hubBg.color = darkGlassPanel; // setta // riga-ok

            // 1. GAME TITLE BLOCK
            BuildTitleBlock(mainPanelRoot); // chiama // riga-ok

            // 2. THREE MAIN BUTTONS BLOCK
            BuildMainButtons(mainPanelRoot); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void BuildTitleBlock(RectTransform hubParent) // roba pub // riga-ok
        { // apre // riga-ok
            GameObject titleContainerGO = new GameObject("Title_Container"); // setta // riga-ok
            titleContainerGO.transform.SetParent(hubParent, false); // chiama // riga-ok
            RectTransform titleContainerRT = titleContainerGO.AddComponent<RectTransform>(); // setta // riga-ok
            titleContainerRT.anchorMin = new Vector2(0.5f, 1f); // setta // riga-ok
            titleContainerRT.anchorMax = new Vector2(0.5f, 1f); // setta // riga-ok
            titleContainerRT.pivot = new Vector2(0.5f, 1f); // setta // riga-ok
            titleContainerRT.anchoredPosition = new Vector2(0f, -30f); // setta // riga-ok
            titleContainerRT.sizeDelta = new Vector2(560f, 160f); // setta // riga-ok

            // Title Glow Backdrop
            GameObject glowGO = new GameObject("Title_Glow"); // setta // riga-ok
            glowGO.transform.SetParent(titleContainerRT, false); // chiama // riga-ok
            RectTransform glowRT = glowGO.AddComponent<RectTransform>(); // setta // riga-ok
            Stretch(glowRT); // chiama // riga-ok
            titleGlowEffect = glowGO.AddComponent<Image>(); // setta // riga-ok
            titleGlowEffect.sprite = solidSprite; // setta // riga-ok
            titleGlowEffect.color = new Color(1f, 0.95f, 0.05f, 0.15f); // setta // riga-ok
            titleGlowEffect.raycastTarget = false; // setta // riga-ok

            // Subtitle Top Tag
            GameObject topTagGO = new GameObject("Top_Subtitle"); // setta // riga-ok
            topTagGO.transform.SetParent(titleContainerRT, false); // chiama // riga-ok
            RectTransform topTagRT = topTagGO.AddComponent<RectTransform>(); // setta // riga-ok
            topTagRT.anchorMin = new Vector2(0.5f, 1f); // setta // riga-ok
            topTagRT.anchorMax = new Vector2(0.5f, 1f); // setta // riga-ok
            topTagRT.pivot = new Vector2(0.5f, 1f); // setta // riga-ok
            topTagRT.anchoredPosition = new Vector2(0f, -6f); // setta // riga-ok
            topTagRT.sizeDelta = new Vector2(540f, 24f); // setta // riga-ok

            Text topTag = topTagGO.AddComponent<Text>(); // setta // riga-ok
            topTag.font = cyberFont; // setta // riga-ok
            topTag.fontSize = 14; // setta // riga-ok
            topTag.fontStyle = FontStyle.Bold; // setta // riga-ok
            topTag.alignment = TextAnchor.MiddleCenter; // setta // riga-ok
            topTag.text = "⚡ ALLERTA CONTENIMENTO LIVELLO OMEGA ⚡"; // setta // riga-ok
            topTag.color = neonRed; // setta // riga-ok
            topTag.raycastTarget = false; // setta // riga-ok

            // Main Primary Game Title: CRISIS PROTOCOL
            GameObject titleGO = new GameObject("Main_Title_Text"); // setta // riga-ok
            titleGO.transform.SetParent(titleContainerRT, false); // chiama // riga-ok
            RectTransform titleRT = titleGO.AddComponent<RectTransform>(); // setta // riga-ok
            titleRT.anchorMin = new Vector2(0.5f, 0.5f); // setta // riga-ok
            titleRT.anchorMax = new Vector2(0.5f, 0.5f); // setta // riga-ok
            titleRT.pivot = new Vector2(0.5f, 0.5f); // setta // riga-ok
            titleRT.anchoredPosition = new Vector2(0f, 6f); // setta // riga-ok
            titleRT.sizeDelta = new Vector2(560f, 75f); // setta // riga-ok

            Text titleText = titleGO.AddComponent<Text>(); // setta // riga-ok
            titleText.font = cyberFont; // setta // riga-ok
            titleText.fontSize = 50; // setta // riga-ok
            titleText.fontStyle = FontStyle.Bold; // setta // riga-ok
            titleText.alignment = TextAnchor.MiddleCenter; // setta // riga-ok
            titleText.text = "CRISIS PROTOCOL"; // setta // riga-ok
            titleText.color = neonYellow; // setta // riga-ok
            titleText.raycastTarget = false; // setta // riga-ok

            // Subtitle Bottom Tag
            GameObject botTagGO = new GameObject("Bot_Subtitle"); // setta // riga-ok
            botTagGO.transform.SetParent(titleContainerRT, false); // chiama // riga-ok
            RectTransform botTagRT = botTagGO.AddComponent<RectTransform>(); // setta // riga-ok
            botTagRT.anchorMin = new Vector2(0.5f, 0f); // setta // riga-ok
            botTagRT.anchorMax = new Vector2(0.5f, 0f); // setta // riga-ok
            botTagRT.pivot = new Vector2(0.5f, 0f); // setta // riga-ok
            botTagRT.anchoredPosition = new Vector2(0f, 10f); // setta // riga-ok
            botTagRT.sizeDelta = new Vector2(540f, 26f); // setta // riga-ok

            Text botTag = botTagGO.AddComponent<Text>(); // setta // riga-ok
            botTag.font = cyberFont; // setta // riga-ok
            botTag.fontSize = 15; // setta // riga-ok
            botTag.alignment = TextAnchor.MiddleCenter; // setta // riga-ok
            botTag.text = "< < < SECTOR CONTAINMENT PROTOCOL > > >"; // setta // riga-ok
            botTag.color = neonGreen; // setta // riga-ok
            botTag.raycastTarget = false; // setta // riga-ok

            // Glowing Divider Line
            GameObject dividerGO = new GameObject("Neon_Divider"); // setta // riga-ok
            dividerGO.transform.SetParent(hubParent, false); // chiama // riga-ok
            RectTransform divRT = dividerGO.AddComponent<RectTransform>(); // setta // riga-ok
            divRT.anchorMin = new Vector2(0.5f, 1f); // setta // riga-ok
            divRT.anchorMax = new Vector2(0.5f, 1f); // setta // riga-ok
            divRT.pivot = new Vector2(0.5f, 1f); // setta // riga-ok
            divRT.anchoredPosition = new Vector2(0f, -205f); // setta // riga-ok
            divRT.sizeDelta = new Vector2(520f, 3f); // setta // riga-ok

            Image divImg = dividerGO.AddComponent<Image>(); // setta // riga-ok
            divImg.sprite = solidSprite; // setta // riga-ok
            divImg.color = neonYellow; // setta // riga-ok
            divImg.raycastTarget = false; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void BuildMainButtons(RectTransform hubParent) // roba pub // riga-ok
        { // apre // riga-ok
            GameObject btnGroupGO = new GameObject("Main_Buttons_Group"); // setta // riga-ok
            btnGroupGO.transform.SetParent(hubParent, false); // chiama // riga-ok
            RectTransform btnGroupRT = btnGroupGO.AddComponent<RectTransform>(); // setta // riga-ok
            btnGroupRT.anchorMin = new Vector2(0.5f, 0f); // setta // riga-ok
            btnGroupRT.anchorMax = new Vector2(0.5f, 0.5f); // setta // riga-ok
            btnGroupRT.pivot = new Vector2(0.5f, 0f); // setta // riga-ok
            btnGroupRT.anchoredPosition = new Vector2(0f, 30f); // setta // riga-ok
            btnGroupRT.sizeDelta = new Vector2(540f, 410f); // setta // riga-ok

            // Button 1: NUOVA PARTITA (Neon Green)
            CreateStyledNeonButton( // ok qua // riga-ok
                parent: btnGroupRT, // ok qua // riga-ok
                name: "Button_NuovaPartita", // ok qua // riga-ok
                pos: new Vector2(0f, 280f), // ok qua // riga-ok
                size: new Vector2(490f, 95f), // ok qua // riga-ok
                title: "▶  NUOVA PARTITA", // ok qua // riga-ok
                subtitle: "[ AVVIA DAL SETTORE 0 // AZZERA EMERGENZA ]", // ok qua // riga-ok
                themeColor: neonGreen, // ok qua // riga-ok
                glowFrame: techFrameGreen, // ok qua // riga-ok
                onClick: NewGame // ok qua // riga-ok
            ); // chiama // riga-ok

            // Button 2: RIPRENDI PARTITA (Fluorescent Yellow)
            string resumeSub = HasSaveData() ? $"[ CONTINUA ULTIMA MISSIONE: SETTORE {GetSavedSectorIndex()} ]" : "[ CONTINUA DAL SALVATAGGIO ESISTENTE ]"; // setta // riga-ok
            CreateStyledNeonButton( // ok qua // riga-ok
                parent: btnGroupRT, // ok qua // riga-ok
                name: "Button_RiprendiPartita", // ok qua // riga-ok
                pos: new Vector2(0f, 155f), // ok qua // riga-ok
                size: new Vector2(490f, 95f), // ok qua // riga-ok
                title: "↺  RIPRENDI PARTITA", // ok qua // riga-ok
                subtitle: resumeSub, // ok qua // riga-ok
                themeColor: neonYellow, // ok qua // riga-ok
                glowFrame: techFrameYellow, // ok qua // riga-ok
                onClick: ResumeGame // ok qua // riga-ok
            ); // chiama // riga-ok

            // Button 3: SELEZIONA LIVELLO (Neon Red)
            CreateStyledNeonButton( // ok qua // riga-ok
                parent: btnGroupRT, // ok qua // riga-ok
                name: "Button_SelezionaLivello", // ok qua // riga-ok
                pos: new Vector2(0f, 30f), // ok qua // riga-ok
                size: new Vector2(490f, 95f), // ok qua // riga-ok
                title: "☵  SELEZIONA LIVELLO", // ok qua // riga-ok
                subtitle: "[ ACCESSO DIRETTO AI SETTORI 0, 1, 2 ]", // ok qua // riga-ok
                themeColor: neonRed, // ok qua // riga-ok
                glowFrame: techFrameRed, // ok qua // riga-ok
                onClick: OpenLevelSelect // ok qua // riga-ok
            ); // chiama // riga-ok
        } // chiude // riga-ok

        private Button CreateStyledNeonButton( // roba pub // riga-ok
            RectTransform parent, // ok qua // riga-ok
            string name, // ok qua // riga-ok
            Vector2 pos, // ok qua // riga-ok
            Vector2 size, // ok qua // riga-ok
            string title, // ok qua // riga-ok
            string subtitle, // ok qua // riga-ok
            Color themeColor, // ok qua // riga-ok
            Sprite glowFrame, // ok qua // riga-ok
            UnityAction onClick) // chiama // riga-ok
        { // apre // riga-ok
            GameObject btnGO = new GameObject(name); // setta // riga-ok
            btnGO.transform.SetParent(parent, false); // chiama // riga-ok
            RectTransform btnRT = btnGO.AddComponent<RectTransform>(); // setta // riga-ok
            btnRT.anchorMin = new Vector2(0.5f, 0f); // setta // riga-ok
            btnRT.anchorMax = new Vector2(0.5f, 0f); // setta // riga-ok
            btnRT.pivot = new Vector2(0.5f, 0f); // setta // riga-ok
            btnRT.anchoredPosition = pos; // setta // riga-ok
            btnRT.sizeDelta = size; // setta // riga-ok

            Image btnImg = btnGO.AddComponent<Image>(); // setta // riga-ok
            btnImg.sprite = glowFrame; // setta // riga-ok
            btnImg.type = Image.Type.Sliced; // setta // riga-ok
            btnImg.color = darkChassis; // setta // riga-ok

            Button btn = btnGO.AddComponent<Button>(); // setta // riga-ok
            btn.targetGraphic = btnImg; // setta // riga-ok

            ColorBlock cb = btn.colors; // setta // riga-ok
            cb.normalColor = darkChassis; // setta // riga-ok
            cb.highlightedColor = new Color(themeColor.r * 0.25f, themeColor.g * 0.25f, themeColor.b * 0.25f, 0.98f); // setta // riga-ok
            cb.pressedColor = new Color(themeColor.r * 0.5f, themeColor.g * 0.5f, themeColor.b * 0.5f, 1f); // setta // riga-ok
            cb.selectedColor = cb.normalColor; // setta // riga-ok
            cb.fadeDuration = 0.1f; // setta // riga-ok
            btn.colors = cb; // setta // riga-ok

            btn.onClick.AddListener(() => // setta // riga-ok
            { // apre // riga-ok
                PlayBeepSound(950f, 0.08f); // chiama // riga-ok
                onClick?.Invoke(); // chiama // riga-ok
            }); // chiama // riga-ok

            // Title Text inside Button
            GameObject titleGO = new GameObject("Title_Label"); // setta // riga-ok
            titleGO.transform.SetParent(btnRT, false); // chiama // riga-ok
            RectTransform titleRT = titleGO.AddComponent<RectTransform>(); // setta // riga-ok
            titleRT.anchorMin = new Vector2(0f, 0.45f); // setta // riga-ok
            titleRT.anchorMax = new Vector2(1f, 1f); // setta // riga-ok
            titleRT.offsetMin = new Vector2(20f, 0f); // setta // riga-ok
            titleRT.offsetMax = new Vector2(-20f, -8f); // setta // riga-ok

            Text titleText = titleGO.AddComponent<Text>(); // setta // riga-ok
            titleText.font = cyberFont; // setta // riga-ok
            titleText.fontSize = 24; // setta // riga-ok
            titleText.fontStyle = FontStyle.Bold; // setta // riga-ok
            titleText.alignment = TextAnchor.MiddleCenter; // setta // riga-ok
            titleText.text = title; // setta // riga-ok
            titleText.color = themeColor; // setta // riga-ok
            titleText.raycastTarget = false; // setta // riga-ok

            // Subtitle Text inside Button
            GameObject subGO = new GameObject("Subtitle_Label"); // setta // riga-ok
            subGO.transform.SetParent(btnRT, false); // chiama // riga-ok
            RectTransform subRT = subGO.AddComponent<RectTransform>(); // setta // riga-ok
            subRT.anchorMin = new Vector2(0f, 0f); // setta // riga-ok
            subRT.anchorMax = new Vector2(1f, 0.45f); // setta // riga-ok
            subRT.offsetMin = new Vector2(20f, 6f); // setta // riga-ok
            subRT.offsetMax = new Vector2(-20f, 0f); // setta // riga-ok

            Text subText = subGO.AddComponent<Text>(); // setta // riga-ok
            subText.font = cyberFont; // setta // riga-ok
            subText.fontSize = 13; // setta // riga-ok
            subText.alignment = TextAnchor.MiddleCenter; // setta // riga-ok
            subText.text = subtitle; // setta // riga-ok
            subText.color = new Color(1f, 1f, 1f, 0.7f); // setta // riga-ok
            subText.raycastTarget = false; // setta // riga-ok

            // Add Event Trigger for Hover Sound Feedback
            EventTrigger trigger = btnGO.AddComponent<EventTrigger>(); // setta // riga-ok
            EventTrigger.Entry entry = new EventTrigger.Entry(); // setta // riga-ok
            entry.eventID = EventTriggerType.PointerEnter; // setta // riga-ok
            entry.callback.AddListener((eventData) => { PlayBeepSound(650f, 0.03f); }); // setta // riga-ok
            trigger.triggers.Add(entry); // chiama // riga-ok

            return btn; // torna val // riga-ok
        } // chiude // riga-ok

        // ─────────────────────────────────────────────────────────────────────────
        // LEVEL SELECT SUB-PANEL (MODAL)
        // ─────────────────────────────────────────────────────────────────────────

        // blocco: funzione fa cose
        private void BuildLevelSelectPanel(RectTransform parent) // roba pub // riga-ok
        { // apre // riga-ok
            GameObject modalGO = new GameObject("Level_Select_Panel"); // setta // riga-ok
            modalGO.transform.SetParent(parent, false); // chiama // riga-ok
            levelSelectPanelRoot = modalGO.AddComponent<RectTransform>(); // setta // riga-ok
            levelSelectPanelRoot.anchorMin = new Vector2(0.5f, 0.5f); // setta // riga-ok
            levelSelectPanelRoot.anchorMax = new Vector2(0.5f, 0.5f); // setta // riga-ok
            levelSelectPanelRoot.pivot = new Vector2(0.5f, 0.5f); // setta // riga-ok
            levelSelectPanelRoot.anchoredPosition = new Vector2(0f, -10f); // setta // riga-ok
            levelSelectPanelRoot.sizeDelta = new Vector2(760f, 720f); // setta // riga-ok

            Image modalBg = modalGO.AddComponent<Image>(); // setta // riga-ok
            modalBg.sprite = techFrameRed; // setta // riga-ok
            modalBg.type = Image.Type.Sliced; // setta // riga-ok
            modalBg.color = darkGlassPanel; // setta // riga-ok

            // Modal Header
            GameObject headerGO = new GameObject("Modal_Header"); // setta // riga-ok
            headerGO.transform.SetParent(levelSelectPanelRoot, false); // chiama // riga-ok
            RectTransform headerRT = headerGO.AddComponent<RectTransform>(); // setta // riga-ok
            headerRT.anchorMin = new Vector2(0.5f, 1f); // setta // riga-ok
            headerRT.anchorMax = new Vector2(0.5f, 1f); // setta // riga-ok
            headerRT.pivot = new Vector2(0.5f, 1f); // setta // riga-ok
            headerRT.anchoredPosition = new Vector2(0f, -25f); // setta // riga-ok
            headerRT.sizeDelta = new Vector2(700f, 80f); // setta // riga-ok

            Text headerTitle = headerGO.AddComponent<Text>(); // setta // riga-ok
            headerTitle.font = cyberFont; // setta // riga-ok
            headerTitle.fontSize = 32; // setta // riga-ok
            headerTitle.fontStyle = FontStyle.Bold; // setta // riga-ok
            headerTitle.alignment = TextAnchor.MiddleCenter; // setta // riga-ok
            headerTitle.text = "⚠️  SELEZIONE SETTORE OPERATIVO  ⚠️"; // setta // riga-ok
            headerTitle.color = neonRed; // setta // riga-ok
            headerTitle.raycastTarget = false; // setta // riga-ok

            GameObject subGO = new GameObject("Modal_Subtitle"); // setta // riga-ok
            subGO.transform.SetParent(headerRT, false); // chiama // riga-ok
            RectTransform subRT = subGO.AddComponent<RectTransform>(); // setta // riga-ok
            subRT.anchorMin = new Vector2(0.5f, 0f); // setta // riga-ok
            subRT.anchorMax = new Vector2(0.5f, 0f); // setta // riga-ok
            subRT.pivot = new Vector2(0.5f, 0f); // setta // riga-ok
            subRT.anchoredPosition = new Vector2(0f, 4f); // setta // riga-ok
            subRT.sizeDelta = new Vector2(650f, 25f); // setta // riga-ok

            Text subText = subGO.AddComponent<Text>(); // setta // riga-ok
            subText.font = cyberFont; // setta // riga-ok
            subText.fontSize = 15; // setta // riga-ok
            subText.alignment = TextAnchor.MiddleCenter; // setta // riga-ok
            subText.text = "SELEZIONA IL SETTORE DELLA STRUTTURA IN CUI INTERVENIRE"; // setta // riga-ok
            subText.color = cyberCyan; // setta // riga-ok
            subText.raycastTarget = false; // setta // riga-ok

            // 3 Sector Selection Cards
            // 1. Settore 0
            CreateSectorCard( // ok qua // riga-ok
                parent: levelSelectPanelRoot, // ok qua // riga-ok
                index: 0, // ok qua // riga-ok
                sectorName: "SETTORE 0: ACCESSO & CONTENIMENTO", // ok qua // riga-ok
                statusTag: "[ STATO: OPERATIVO ]", // ok qua // riga-ok
                description: "Ingresso principale, alimentazione ausiliaria e contenimento breccia iniziale.", // ok qua // riga-ok
                pos: new Vector2(0f, 420f), // ok qua // riga-ok
                badgeColor: neonGreen, // ok qua // riga-ok
                borderSprite: techFrameGreen // ok qua // riga-ok
            ); // chiama // riga-ok

            // 2. Settore 1
            CreateSectorCard( // ok qua // riga-ok
                parent: levelSelectPanelRoot, // ok qua // riga-ok
                index: 1, // ok qua // riga-ok
                sectorName: "SETTORE 1: LABORATORI & SICUREZZA", // ok qua // riga-ok
                statusTag: "[ STATO: ALLERTA ELEVATA ]", // ok qua // riga-ok
                description: "Area laboratori biologici, terminali di sicurezza e droni ostili di pattuglia.", // ok qua // riga-ok
                pos: new Vector2(0f, 275f), // ok qua // riga-ok
                badgeColor: neonYellow, // ok qua // riga-ok
                borderSprite: techFrameYellow // ok qua // riga-ok
            ); // chiama // riga-ok

            // 3. Settore 2
            CreateSectorCard( // ok qua // riga-ok
                parent: levelSelectPanelRoot, // ok qua // riga-ok
                index: 2, // ok qua // riga-ok
                sectorName: "SETTORE 2: NUCLEO REATTORE", // ok qua // riga-ok
                statusTag: "[ STATO: CRITICO / COLLASSO ]", // ok qua // riga-ok
                description: "Nucleo reattore a rischio fusione, robot di manutenzione potenziati e timer critico.", // ok qua // riga-ok
                pos: new Vector2(0f, 130f), // ok qua // riga-ok
                badgeColor: neonRed, // ok qua // riga-ok
                borderSprite: techFrameRed // ok qua // riga-ok
            ); // chiama // riga-ok

            // Close / Back Button
            CreateStyledNeonButton( // ok qua // riga-ok
                parent: levelSelectPanelRoot, // ok qua // riga-ok
                name: "Button_CloseLevelSelect", // ok qua // riga-ok
                pos: new Vector2(0f, 25f), // ok qua // riga-ok
                size: new Vector2(380f, 65f), // ok qua // riga-ok
                title: "❮  TORNA AL MENU", // ok qua // riga-ok
                subtitle: "[ ANNULLA E TORNA ALLA SCHERMATA PRINCIPALE ]", // ok qua // riga-ok
                themeColor: neonRed, // ok qua // riga-ok
                glowFrame: techFrameRed, // ok qua // riga-ok
                onClick: CloseLevelSelect // ok qua // riga-ok
            ); // chiama // riga-ok

            // Hide Level Select initially
            levelSelectPanelRoot.gameObject.SetActive(false); // chiama // riga-ok
        } // chiude // riga-ok

        private void CreateSectorCard( // roba pub // riga-ok
            RectTransform parent, // ok qua // riga-ok
            int index, // ok qua // riga-ok
            string sectorName, // ok qua // riga-ok
            string statusTag, // ok qua // riga-ok
            string description, // ok qua // riga-ok
            Vector2 pos, // ok qua // riga-ok
            Color badgeColor, // ok qua // riga-ok
            Sprite borderSprite) // chiama // riga-ok
        { // apre // riga-ok
            GameObject cardGO = new GameObject($"Sector_Card_{index}"); // setta // riga-ok
            cardGO.transform.SetParent(parent, false); // chiama // riga-ok
            RectTransform cardRT = cardGO.AddComponent<RectTransform>(); // setta // riga-ok
            cardRT.anchorMin = new Vector2(0.5f, 0f); // setta // riga-ok
            cardRT.anchorMax = new Vector2(0.5f, 0f); // setta // riga-ok
            cardRT.pivot = new Vector2(0.5f, 0f); // setta // riga-ok
            cardRT.anchoredPosition = pos; // setta // riga-ok
            cardRT.sizeDelta = new Vector2(680f, 120f); // setta // riga-ok

            Image cardBg = cardGO.AddComponent<Image>(); // setta // riga-ok
            cardBg.sprite = borderSprite; // setta // riga-ok
            cardBg.type = Image.Type.Sliced; // setta // riga-ok
            cardBg.color = darkChassis; // setta // riga-ok

            // Header line (Sector Name + Badge)
            GameObject titleGO = new GameObject("Sector_Title"); // setta // riga-ok
            titleGO.transform.SetParent(cardRT, false); // chiama // riga-ok
            RectTransform titleRT = titleGO.AddComponent<RectTransform>(); // setta // riga-ok
            titleRT.anchorMin = new Vector2(0f, 1f); // setta // riga-ok
            titleRT.anchorMax = new Vector2(1f, 1f); // setta // riga-ok
            titleRT.pivot = new Vector2(0f, 1f); // setta // riga-ok
            titleRT.anchoredPosition = new Vector2(20f, -12f); // setta // riga-ok
            titleRT.sizeDelta = new Vector2(420f, 30f); // setta // riga-ok

            Text titleText = titleGO.AddComponent<Text>(); // setta // riga-ok
            titleText.font = cyberFont; // setta // riga-ok
            titleText.fontSize = 20; // setta // riga-ok
            titleText.fontStyle = FontStyle.Bold; // setta // riga-ok
            titleText.alignment = TextAnchor.MiddleLeft; // setta // riga-ok
            titleText.text = sectorName; // setta // riga-ok
            titleText.color = Color.white; // setta // riga-ok
            titleText.raycastTarget = false; // setta // riga-ok

            // Status Badge
            GameObject badgeGO = new GameObject("Sector_Badge"); // setta // riga-ok
            badgeGO.transform.SetParent(cardRT, false); // chiama // riga-ok
            RectTransform badgeRT = badgeGO.AddComponent<RectTransform>(); // setta // riga-ok
            badgeRT.anchorMin = new Vector2(0f, 1f); // setta // riga-ok
            badgeRT.anchorMax = new Vector2(0f, 1f); // setta // riga-ok
            badgeRT.pivot = new Vector2(0f, 1f); // setta // riga-ok
            badgeRT.anchoredPosition = new Vector2(20f, -44f); // setta // riga-ok
            badgeRT.sizeDelta = new Vector2(220f, 22f); // setta // riga-ok

            Text badgeText = badgeGO.AddComponent<Text>(); // setta // riga-ok
            badgeText.font = cyberFont; // setta // riga-ok
            badgeText.fontSize = 14; // setta // riga-ok
            badgeText.fontStyle = FontStyle.Bold; // setta // riga-ok
            badgeText.alignment = TextAnchor.MiddleLeft; // setta // riga-ok
            badgeText.text = statusTag; // setta // riga-ok
            badgeText.color = badgeColor; // setta // riga-ok
            badgeText.raycastTarget = false; // setta // riga-ok

            // Description
            GameObject descGO = new GameObject("Sector_Desc"); // setta // riga-ok
            descGO.transform.SetParent(cardRT, false); // chiama // riga-ok
            RectTransform descRT = descGO.AddComponent<RectTransform>(); // setta // riga-ok
            descRT.anchorMin = new Vector2(0f, 0f); // setta // riga-ok
            descRT.anchorMax = new Vector2(0f, 0f); // setta // riga-ok
            descRT.pivot = new Vector2(0f, 0f); // setta // riga-ok
            descRT.anchoredPosition = new Vector2(20f, 12f); // setta // riga-ok
            descRT.sizeDelta = new Vector2(440f, 36f); // setta // riga-ok

            Text descText = descGO.AddComponent<Text>(); // setta // riga-ok
            descText.font = cyberFont; // setta // riga-ok
            descText.fontSize = 13; // setta // riga-ok
            descText.alignment = TextAnchor.MiddleLeft; // setta // riga-ok
            descText.text = description; // setta // riga-ok
            descText.color = new Color(0.75f, 0.85f, 0.95f, 0.8f); // setta // riga-ok
            descText.raycastTarget = false; // setta // riga-ok

            // Launch Button on Right
            GameObject launchBtnGO = new GameObject($"Button_Launch_Sector_{index}"); // setta // riga-ok
            launchBtnGO.transform.SetParent(cardRT, false); // chiama // riga-ok
            RectTransform launchBtnRT = launchBtnGO.AddComponent<RectTransform>(); // setta // riga-ok
            launchBtnRT.anchorMin = new Vector2(1f, 0.5f); // setta // riga-ok
            launchBtnRT.anchorMax = new Vector2(1f, 0.5f); // setta // riga-ok
            launchBtnRT.pivot = new Vector2(1f, 0.5f); // setta // riga-ok
            launchBtnRT.anchoredPosition = new Vector2(-15f, 0f); // setta // riga-ok
            launchBtnRT.sizeDelta = new Vector2(190f, 75f); // setta // riga-ok

            Image launchImg = launchBtnGO.AddComponent<Image>(); // setta // riga-ok
            launchImg.sprite = borderSprite; // setta // riga-ok
            launchImg.type = Image.Type.Sliced; // setta // riga-ok
            launchImg.color = new Color(0.04f, 0.08f, 0.12f, 0.95f); // setta // riga-ok

            Button launchBtn = launchBtnGO.AddComponent<Button>(); // setta // riga-ok
            launchBtn.targetGraphic = launchImg; // setta // riga-ok

            ColorBlock cb = launchBtn.colors; // setta // riga-ok
            cb.normalColor = new Color(0.04f, 0.08f, 0.12f, 0.95f); // setta // riga-ok
            cb.highlightedColor = new Color(badgeColor.r * 0.3f, badgeColor.g * 0.3f, badgeColor.b * 0.3f, 1f); // setta // riga-ok
            cb.pressedColor = badgeColor; // setta // riga-ok
            launchBtn.colors = cb; // setta // riga-ok

            int sectorToLoad = index; // setta // riga-ok
            launchBtn.onClick.AddListener(() => // setta // riga-ok
            { // apre // riga-ok
                PlayBeepSound(1200f, 0.1f); // chiama // riga-ok
                LoadSector(sectorToLoad); // chiama // riga-ok
            }); // chiama // riga-ok

            GameObject launchTextGO = new GameObject("Launch_Text"); // setta // riga-ok
            launchTextGO.transform.SetParent(launchBtnRT, false); // chiama // riga-ok
            RectTransform launchTextRT = launchTextGO.AddComponent<RectTransform>(); // setta // riga-ok
            Stretch(launchTextRT); // chiama // riga-ok

            Text launchText = launchTextGO.AddComponent<Text>(); // setta // riga-ok
            launchText.font = cyberFont; // setta // riga-ok
            launchText.fontSize = 17; // setta // riga-ok
            launchText.fontStyle = FontStyle.Bold; // setta // riga-ok
            launchText.alignment = TextAnchor.MiddleCenter; // setta // riga-ok
            launchText.text = $"⚡ AVVIA\nSETTORE {index}"; // setta // riga-ok
            launchText.color = badgeColor; // setta // riga-ok
            launchText.raycastTarget = false; // setta // riga-ok

            // Hover sound
            EventTrigger trigger = launchBtnGO.AddComponent<EventTrigger>(); // setta // riga-ok
            EventTrigger.Entry entry = new EventTrigger.Entry(); // setta // riga-ok
            entry.eventID = EventTriggerType.PointerEnter; // setta // riga-ok
            entry.callback.AddListener((e) => { PlayBeepSound(700f, 0.03f); }); // setta // riga-ok
            trigger.triggers.Add(entry); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public void OpenLevelSelect() // roba pub // riga-ok
        { // apre // riga-ok
            isLevelSelectOpen = true; // setta // riga-ok
            // blocco: controlla se va
            if (mainPanelRoot != null) mainPanelRoot.gameObject.SetActive(false); // se ok // riga-ok
            // blocco: controlla se va
            if (levelSelectPanelRoot != null) levelSelectPanelRoot.gameObject.SetActive(true); // se ok // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public void CloseLevelSelect() // roba pub // riga-ok
        { // apre // riga-ok
            isLevelSelectOpen = false; // setta // riga-ok
            // blocco: controlla se va
            if (levelSelectPanelRoot != null) levelSelectPanelRoot.gameObject.SetActive(false); // se ok // riga-ok
            // blocco: controlla se va
            if (mainPanelRoot != null) mainPanelRoot.gameObject.SetActive(true); // se ok // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void LoadSector(int index) // roba pub // riga-ok
        { // apre // riga-ok
            Time.timeScale = 1f; // setta // riga-ok
            // blocco: controlla se va
            if (GameManager.Instance != null) // se ok // riga-ok
            { // apre // riga-ok
                GameManager.Instance.CaricaSettore(index); // chiama // riga-ok
            } // chiude // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
            { // apre // riga-ok
                SceneManager.LoadScene($"settore {index}"); // chiama // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // ─────────────────────────────────────────────────────────────────────────
        // BOTTOM FOOTER
        // ─────────────────────────────────────────────────────────────────────────

        // blocco: funzione fa cose
        private void BuildBottomFooter(RectTransform parent) // roba pub // riga-ok
        { // apre // riga-ok
            GameObject footerGO = new GameObject("Bottom_Footer_Bar"); // setta // riga-ok
            footerGO.transform.SetParent(parent, false); // chiama // riga-ok
            RectTransform footerRT = footerGO.AddComponent<RectTransform>(); // setta // riga-ok
            footerRT.anchorMin = new Vector2(0f, 0f); // setta // riga-ok
            footerRT.anchorMax = new Vector2(1f, 0f); // setta // riga-ok
            footerRT.pivot = new Vector2(0.5f, 0f); // setta // riga-ok
            footerRT.anchoredPosition = new Vector2(0f, 15f); // setta // riga-ok
            footerRT.sizeDelta = new Vector2(-80f, 40f); // setta // riga-ok

            // Left: Operator Security Signature
            GameObject opGO = new GameObject("Operator_Status"); // setta // riga-ok
            opGO.transform.SetParent(footerRT, false); // chiama // riga-ok
            RectTransform opRT = opGO.AddComponent<RectTransform>(); // setta // riga-ok
            opRT.anchorMin = new Vector2(0f, 0.5f); // setta // riga-ok
            opRT.anchorMax = new Vector2(0f, 0.5f); // setta // riga-ok
            opRT.pivot = new Vector2(0f, 0.5f); // setta // riga-ok
            opRT.anchoredPosition = new Vector2(10f, 0f); // setta // riga-ok
            opRT.sizeDelta = new Vector2(400f, 30f); // setta // riga-ok

            Text opText = opGO.AddComponent<Text>(); // setta // riga-ok
            opText.font = cyberFont; // setta // riga-ok
            opText.fontSize = 14; // setta // riga-ok
            opText.alignment = TextAnchor.MiddleLeft; // setta // riga-ok
            opText.text = "OPERATORE: KEYCARD_A01 // AUTORIZZAZIONE OMEGA"; // setta // riga-ok
            opText.color = new Color(0.6f, 0.8f, 1f, 0.7f); // setta // riga-ok
            opText.raycastTarget = false; // setta // riga-ok

            // Right: Engine Version
            GameObject verGO = new GameObject("Version_Tag"); // setta // riga-ok
            verGO.transform.SetParent(footerRT, false); // chiama // riga-ok
            RectTransform verRT = verGO.AddComponent<RectTransform>(); // setta // riga-ok
            verRT.anchorMin = new Vector2(1f, 0.5f); // setta // riga-ok
            verRT.anchorMax = new Vector2(1f, 0.5f); // setta // riga-ok
            verRT.pivot = new Vector2(1f, 0.5f); // setta // riga-ok
            verRT.anchoredPosition = new Vector2(-10f, 0f); // setta // riga-ok
            verRT.sizeDelta = new Vector2(300f, 30f); // setta // riga-ok

            Text verText = verGO.AddComponent<Text>(); // setta // riga-ok
            verText.font = cyberFont; // setta // riga-ok
            verText.fontSize = 13; // setta // riga-ok
            verText.alignment = TextAnchor.MiddleRight; // setta // riga-ok
            verText.text = "CRISIS ENGINE v2.5 // TACTICAL INTERFACE"; // setta // riga-ok
            verText.color = new Color(1f, 1f, 1f, 0.5f); // setta // riga-ok
            verText.raycastTarget = false; // setta // riga-ok
        } // chiude // riga-ok

        // ─────────────────────────────────────────────────────────────────────────
        // ACTIONS (NEW GAME, RESUME, QUIT)
        // ─────────────────────────────────────────────────────────────────────────

        // blocco: funzione fa cose
        public void NewGame() // roba pub // riga-ok
        { // apre // riga-ok
            Time.timeScale = 1f; // setta // riga-ok
            // blocco: prova safe
            try { onNewGame?.Invoke(); } catch (Exception e) { Debug.LogWarning(e.Message); } // prova // riga-ok

            // blocco: controlla se va
            if (GameManager.Instance == null) // se ok // riga-ok
            { // apre // riga-ok
                GameObject gmObj = new GameObject("GameManager_AutoCreated"); // setta // riga-ok
                gmObj.AddComponent<GameManager>(); // chiama // riga-ok
            } // chiude // riga-ok
            GameManager.Instance.NuovaPartita(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public void ResumeGame() // roba pub // riga-ok
        { // apre // riga-ok
            Time.timeScale = 1f; // setta // riga-ok
            // blocco: prova safe
            try { onResume?.Invoke(); } catch (Exception e) { Debug.LogWarning(e.Message); } // prova // riga-ok

            // blocco: controlla se va
            if (GameManager.Instance == null) // se ok // riga-ok
            { // apre // riga-ok
                GameObject gmObj = new GameObject("GameManager_AutoCreated"); // setta // riga-ok
                gmObj.AddComponent<GameManager>(); // chiama // riga-ok
            } // chiude // riga-ok
            GameManager.Instance.ResumeSavedGame(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public void QuitGame() // roba pub // riga-ok
        { // apre // riga-ok
            Application.Quit(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public void SetMainMenuBackground(Sprite backgroundSprite) // roba pub // riga-ok
        { // apre // riga-ok
            mainMenuBackgroundSprite = backgroundSprite; // setta // riga-ok
            BuildNeonMainMenu(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public void SetBackgroundVideo(UnityEngine.Video.VideoClip clip) // roba pub // riga-ok
        { // apre // riga-ok
            backgroundVideoClip = clip; // setta // riga-ok
            BuildNeonMainMenu(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void OnDestroy() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (bgVideoRenderTexture != null) // se ok // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (bgVideoRenderTexture.IsCreated()) // se ok // riga-ok
                    bgVideoRenderTexture.Release(); // chiama // riga-ok
                Destroy(bgVideoRenderTexture); // elimina // riga-ok
                bgVideoRenderTexture = null; // setta // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private bool HasSaveData() // roba pub // riga-ok
        { // apre // riga-ok
            string path = Path.Combine(Application.persistentDataPath, "SectorContainment_Save.json"); // setta // riga-ok
            return File.Exists(path); // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private int GetSavedSectorIndex() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: prova safe
            try // prova // riga-ok
            { // apre // riga-ok
                string path = Path.Combine(Application.persistentDataPath, "SectorContainment_Save.json"); // setta // riga-ok
                // blocco: controlla se va
                if (File.Exists(path)) // se ok // riga-ok
                { // apre // riga-ok
                    SaveDataWrapper data = JsonUtility.FromJson<SaveDataWrapper>(File.ReadAllText(path)); // setta // riga-ok
                    // blocco: controlla se va
                    if (data != null) return data.lastSectorIndex; // se ok // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
            // blocco: becca errore
            catch { } // err qui // riga-ok
            return 0; // torna val // riga-ok
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
    } // chiude // riga-ok
} // chiude // riga-ok
