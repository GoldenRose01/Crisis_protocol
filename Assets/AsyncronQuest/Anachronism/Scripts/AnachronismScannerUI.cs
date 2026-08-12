using System;
using System.Collections;
using GoldenCast.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

namespace AsyncronQuest.Anachronism
{
    [DisallowMultipleComponent]
    public sealed class AnachronismScannerUI : MonoBehaviour
    {
        private const string InputSystemUiModuleTypeName = "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem";
        private const string ModalOwner = "AnachronismScanner";

        [Header("Canvas")]
        [SerializeField] private int sortingOrder = 1100;
        [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);

        [Header("Scanner Layout")]
        [SerializeField] private Vector2 panelSize = new Vector2(912f, 600f);
        [SerializeField] private Vector2 panelPosition = Vector2.zero;
        [SerializeField, Range(0f, 1f)] private float backdropAlpha = 0.68f;
        [SerializeField, Range(0f, 1f)] private float panelAlpha = 0.98f;
        [SerializeField] private float processingSeconds = 1f;

        [Header("Colors")]
        [SerializeField] private Color panelColor = new Color(0.01f, 0.018f, 0.03f, 1f);
        [SerializeField] private Color lineColor = new Color(0.05f, 0.95f, 1f, 1f);
        [SerializeField] private Color activeColor = new Color(0.36f, 1f, 0.45f, 1f);
        [SerializeField] private Color warningColor = new Color(1f, 0.78f, 0.24f, 1f);

        private GameObject canvasRoot;
        private RectTransform panelRoot;
        private TMP_Text statusText;
        private RawImage videoImage;
        private Button analyzeButton;
        private Button branchButton;
        private Button correctionButton;
        private Image analyzeLamp;
        private Image branchLamp;
        private Image correctionLamp;
        private VideoPlayer videoPlayer;
        private AudioSource videoAudioSource;
        private RenderTexture renderTexture;
        private Coroutine activeRoutine;
        private Coroutine prepareVideoRoutine;
        private Action onComplete;
        private Action onCanceled;
        private TemporalTagData activeTagData;
        private VideoClip preparedVideoClip;
        private bool videoPrepared;
        private bool analyzeDone;
        private bool branchDone;
        private bool correctionDone;
        private bool isBusy;

        public static AnachronismScannerUI Instance { get; private set; }
        public static bool IsOpen => Instance && Instance.canvasRoot && Instance.canvasRoot.activeSelf;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntimeScanner()
        {
            if (Instance || FindFirstObjectByType<AnachronismScannerUI>())
                return;

            GameObject root = new GameObject("GoldenCast Anachronism Scanner UI");
            DontDestroyOnLoad(root);
            root.AddComponent<AnachronismScannerUI>();
        }

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            ReleaseVideoResources();
        }

        public static void Open(TemporalTagData tagData, Action completed, Action canceled = null)
        {
            AnachronismScannerUI scanner = Instance ? Instance : FindFirstObjectByType<AnachronismScannerUI>();
            if (!scanner)
            {
                GameObject root = new GameObject("GoldenCast Anachronism Scanner UI");
                DontDestroyOnLoad(root);
                scanner = root.AddComponent<AnachronismScannerUI>();
            }

            scanner.Show(tagData, completed, canceled);
        }

        private void Update()
        {
            if (!IsOpen)
                return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                CancelScanner();
        }

        private void Show(TemporalTagData tagData, Action completed, Action canceled)
        {
            if (!ModalUIState.TryOpen(ModalOwner))
            {
                canceled?.Invoke();
                return;
            }

            activeTagData = tagData;
            onComplete = completed;
            onCanceled = canceled;
            analyzeDone = false;
            branchDone = false;
            correctionDone = false;
            isBusy = false;

            EnsureInterface();
            canvasRoot.SetActive(true);
            videoImage.gameObject.SetActive(false);
            SetButtonsInteractable(true);
            UpdateLamps();
            SetStatus("SCAN READY :: " + GetTagLabel());
            BeginPrepareTagVideo(activeTagData ? activeTagData.anachronismVideo : null);
        }

        private void BuildInterface()
        {
            ClearChildren();
            EnsureEventSystem();

            Canvas canvas = new GameObject("Anachronism Scanner Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.transform.SetParent(transform, false);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            Image backdrop = CreateImage("Scanner_Input_Blocker", canvas.transform, new Color(0f, 0f, 0f, backdropAlpha));
            ApplyStretch(backdrop.rectTransform, Vector2.zero, Vector2.zero);
            backdrop.raycastTarget = true;

            Image panelGlow = CreateImage("Scanner_Panel_Glow", canvas.transform, new Color(lineColor.r, lineColor.g, lineColor.b, 0.08f));
            ApplyRect(panelGlow.rectTransform, panelPosition, panelSize + new Vector2(34f, 34f));
            panelGlow.raycastTarget = false;

            Image panel = CreateImage("Scanner_Panel", canvas.transform, WithAlpha(panelColor, panelAlpha));
            panelRoot = panel.rectTransform;
            panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
            panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
            panelRoot.pivot = new Vector2(0.5f, 0.5f);
            panelRoot.sizeDelta = panelSize;
            panelRoot.anchoredPosition = panelPosition;

            AddScanLines(panelRoot);
            AddTechGrid(panelRoot);
            AddFrameLines(panelRoot);

            Image headerBar = CreateImage("Scanner_Header_Bar", panelRoot, new Color(lineColor.r, lineColor.g, lineColor.b, 0.13f));
            headerBar.raycastTarget = false;
            ApplyRect(headerBar.rectTransform, new Vector2(0f, 252f), new Vector2(830f, 70f));

            TMP_Text title = CreateText("Scanner_Title", panelRoot, "TEMPORAL RESOLUTION TERMINAL", 36f, lineColor, FontStyles.Bold);
            ApplyRect(title.rectTransform, new Vector2(0f, 262f), new Vector2(790f, 44f));

            TMP_Text subtitle = CreateText("Scanner_Subtitle", panelRoot, "ANACHRONISM VECTOR ANALYSIS // CAUSAL PATCH ROUTER", 16f, warningColor, FontStyles.Bold);
            ApplyRect(subtitle.rectTransform, new Vector2(0f, 224f), new Vector2(760f, 28f));

            statusText = CreateText("Scanner_Status", panelRoot, string.Empty, 22f, warningColor, FontStyles.Bold);
            statusText.alignment = TextAlignmentOptions.MidlineLeft;
            ApplyRect(statusText.rectTransform, new Vector2(0f, 176f), new Vector2(760f, 42f));

            analyzeButton = AddScannerButton("Button_AnalyzeObject", "ANALIZZA OGGETTO", new Vector2(0f, 92f), out analyzeLamp, () => StartStep(ScannerStep.Analyze));
            branchButton = AddScannerButton("Button_FindBranch", "INDIVIDUA BRANCH DEL FLUSSO", new Vector2(0f, 8f), out branchLamp, () => StartStep(ScannerStep.Branch));
            correctionButton = AddScannerButton("Button_SuggestCorrection", "SUGGERISCI CORREZIONE", new Vector2(0f, -76f), out correctionLamp, () => StartStep(ScannerStep.Correction));

            AddTextButton("Button_CloseScanner", "ESCI", new Vector2(0f, -246f), new Vector2(230f, 54f), CancelScanner);

            videoImage = new GameObject("Scanner_Tag_Video", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            videoImage.transform.SetParent(panelRoot, false);
            ApplyRect(videoImage.rectTransform, new Vector2(0f, -26f), new Vector2(760f, 420f));
            videoImage.color = Color.white;
            videoImage.raycastTarget = false;
            videoImage.gameObject.SetActive(false);

            canvasRoot = canvas.gameObject;
            canvasRoot.SetActive(false);
        }

        private Button AddScannerButton(string name, string label, Vector2 position, out Image lamp, UnityEngine.Events.UnityAction action)
        {
            Image buttonGlow = CreateImage(name + "_Glow", panelRoot, new Color(lineColor.r, lineColor.g, lineColor.b, 0.1f));
            buttonGlow.raycastTarget = false;
            ApplyRect(buttonGlow.rectTransform, position, new Vector2(790f, 72f));

            Image buttonImage = CreateImage(name, panelRoot, new Color(0.015f, 0.065f, 0.09f, 0.96f));
            buttonImage.raycastTarget = true;
            ApplyRect(buttonImage.rectTransform, position, new Vector2(760f, 64f));

            Button button = buttonImage.gameObject.AddComponent<Button>();
            button.onClick.AddListener(action);
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.015f, 0.065f, 0.09f, 0.96f);
            colors.highlightedColor = new Color(0.03f, 0.2f, 0.24f, 1f);
            colors.pressedColor = new Color(0.02f, 0.95f, 1f, 0.85f);
            button.colors = colors;

            TMP_Text text = CreateText(name + "_Text", buttonImage.rectTransform, label, 23f, lineColor, FontStyles.Bold);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            ApplyStretch(text.rectTransform, new Vector2(86f, 0f), new Vector2(-24f, 0f));

            lamp = CreateImage(name + "_Lamp", buttonImage.rectTransform, warningColor);
            lamp.raycastTarget = false;
            ApplyRect(lamp.rectTransform, new Vector2(-330f, 0f), new Vector2(26f, 26f));

            Image lampCore = CreateImage(name + "_Lamp_Core", lamp.rectTransform, new Color(1f, 1f, 1f, 0.28f));
            lampCore.raycastTarget = false;
            ApplyRect(lampCore.rectTransform, Vector2.zero, new Vector2(12f, 12f));
            return button;
        }

        private Button AddTextButton(string name, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            Image buttonImage = CreateImage(name, panelRoot, new Color(0.015f, 0.065f, 0.09f, 0.96f));
            buttonImage.raycastTarget = true;
            ApplyRect(buttonImage.rectTransform, position, size);

            Button button = buttonImage.gameObject.AddComponent<Button>();
            button.onClick.AddListener(action);
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.015f, 0.065f, 0.09f, 0.96f);
            colors.highlightedColor = new Color(0.03f, 0.2f, 0.24f, 1f);
            colors.pressedColor = new Color(0.02f, 0.95f, 1f, 0.85f);
            button.colors = colors;

            TMP_Text text = CreateText(name + "_Text", buttonImage.rectTransform, label, 20f, lineColor, FontStyles.Bold);
            ApplyStretch(text.rectTransform, new Vector2(12f, 0f), new Vector2(-12f, 0f));
            return button;
        }

        private void StartStep(ScannerStep step)
        {
            if (isBusy)
                return;

            if (step == ScannerStep.Analyze && analyzeDone)
                return;
            if (step == ScannerStep.Branch && branchDone)
                return;
            if (step == ScannerStep.Correction && correctionDone)
                return;

            activeRoutine = StartCoroutine(ProcessStep(step));
        }

        private IEnumerator ProcessStep(ScannerStep step)
        {
            isBusy = true;
            SetButtonsInteractable(false);
            SetStatus(GetProcessingLabel(step) + " :: executing temporal code...");

            float startedAt = Time.unscaledTime;
            while (Time.unscaledTime - startedAt < processingSeconds)
            {
                int dots = Mathf.FloorToInt((Time.unscaledTime - startedAt) * 8f) % 4;
                SetStatus(GetProcessingLabel(step) + " :: " + new string('.', dots + 1));
                yield return null;
            }

            if (step == ScannerStep.Analyze)
                analyzeDone = true;
            else if (step == ScannerStep.Branch)
                branchDone = true;
            else
                correctionDone = true;

            UpdateLamps();
            isBusy = false;
            SetButtonsInteractable(true);

            if (analyzeDone && branchDone && correctionDone)
                activeRoutine = StartCoroutine(CompleteProtocol());
            else
            {
                activeRoutine = null;
                SetStatus("MODULE COMPLETE :: awaiting next instruction");
            }
        }

        private IEnumerator CompleteProtocol()
        {
            isBusy = true;
            SetButtonsInteractable(false);
            SetStatus("CORRECTION PACKAGE READY :: loading evidence stream");

            VideoClip clip = activeTagData ? activeTagData.anachronismVideo : null;
            if (clip)
                yield return PlayTagVideo(clip);
            else
                yield return new WaitForSecondsRealtime(0.6f);

            SetStatus("TEMPORAL SIGNATURE EXTRACTED :: " + GetTagLabel());
            yield return new WaitForSecondsRealtime(0.25f);
            Action completed = onComplete;
            activeRoutine = null;
            Hide();
            completed?.Invoke();
        }

        private IEnumerator PlayTagVideo(VideoClip clip)
        {
            if (preparedVideoClip != clip || !videoPlayer)
                BeginPrepareTagVideo(clip);

            SetStatus("EVIDENCE STREAM :: buffering resolved artifact");
            videoImage.gameObject.SetActive(true);

            float startedAt = Time.realtimeSinceStartup;
            while (videoPlayer && !videoPlayer.isPrepared && Time.realtimeSinceStartup - startedAt < 5f)
                yield return null;

            if (videoPlayer && videoPlayer.isPrepared)
            {
                bool done = false;
                void MarkDone(VideoPlayer _) => done = true;

                videoPlayer.loopPointReached += MarkDone;
                videoPlayer.frame = 0;
                videoPlayer.time = 0;
                SetStatus("EVIDENCE STREAM :: playing " + GetTagLabel());
                videoPlayer.Play();

                float playStarted = Time.realtimeSinceStartup;
                float maxPlaybackSeconds = Mathf.Max(0.5f, (float)clip.length + 0.5f);
                while (!done && videoPlayer && Time.realtimeSinceStartup - playStarted < maxPlaybackSeconds)
                {
                    if (!videoPlayer.isPlaying && Time.realtimeSinceStartup - playStarted > 0.35f)
                        break;

                    yield return null;
                }

                if (videoPlayer)
                    videoPlayer.loopPointReached -= MarkDone;
            }
        }

        private void BeginPrepareTagVideo(VideoClip clip)
        {
            ReleaseVideoResources();
            preparedVideoClip = clip;
            videoPrepared = false;

            if (!clip)
                return;

            EnsureVideoSurface();

            videoPlayer = gameObject.AddComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.skipOnDrop = true;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = renderTexture;
            videoPlayer.clip = clip;
            videoPlayer.isLooping = false;

            videoAudioSource = gameObject.AddComponent<AudioSource>();
            videoAudioSource.playOnAwake = false;
            videoAudioSource.spatialBlend = 0f;
            videoAudioSource.ignoreListenerPause = true;

            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.SetTargetAudioSource(0, videoAudioSource);
            videoPlayer.Prepare();
            prepareVideoRoutine = StartCoroutine(PrepareTagVideoRoutine());
        }

        private IEnumerator PrepareTagVideoRoutine()
        {
            float startedAt = Time.realtimeSinceStartup;
            while (videoPlayer && !videoPlayer.isPrepared && Time.realtimeSinceStartup - startedAt < 5f)
                yield return null;

            videoPrepared = videoPlayer && videoPlayer.isPrepared;
            prepareVideoRoutine = null;
        }

        private void EnsureVideoSurface()
        {
            if (renderTexture)
                return;

            renderTexture = new RenderTexture(960, 540, 0, RenderTextureFormat.ARGB32);
            renderTexture.Create();

            if (videoImage)
                videoImage.texture = renderTexture;
        }

        private void CancelScanner()
        {
            Action canceled = onCanceled;
            Hide();
            canceled?.Invoke();
        }

        private void Hide()
        {
            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                activeRoutine = null;
            }

            ReleaseVideoResources();
            if (canvasRoot)
                canvasRoot.SetActive(false);

            isBusy = false;
            activeTagData = null;
            onComplete = null;
            onCanceled = null;
            ModalUIState.Close(ModalOwner);
        }

        private void SetButtonsInteractable(bool value)
        {
            if (analyzeButton)
                analyzeButton.interactable = value && !analyzeDone;
            if (branchButton)
                branchButton.interactable = value && !branchDone;
            if (correctionButton)
                correctionButton.interactable = value && !correctionDone;
        }

        private void UpdateLamps()
        {
            if (analyzeLamp)
                analyzeLamp.color = analyzeDone ? activeColor : warningColor;
            if (branchLamp)
                branchLamp.color = branchDone ? activeColor : warningColor;
            if (correctionLamp)
                correctionLamp.color = correctionDone ? activeColor : warningColor;
        }

        private string GetProcessingLabel(ScannerStep step)
        {
            if (step == ScannerStep.Analyze)
                return "ANALYZE_OBJECT";
            if (step == ScannerStep.Branch)
                return "FLOW_BRANCH_TRACE";
            return "CORRECTION_MODEL";
        }

        private string GetTagLabel()
        {
            if (!activeTagData)
                return "TAG_UNKNOWN";

            return string.IsNullOrWhiteSpace(activeTagData.idTag) ? "TAG_UNKNOWN" : activeTagData.idTag;
        }

        private void SetStatus(string text)
        {
            if (statusText)
                statusText.text = text;
        }

        private void EnsureInterface()
        {
            if (canvasRoot)
                return;

            BuildInterface();
        }

        private void ClearChildren()
        {
            ReleaseVideoResources();
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }

            canvasRoot = null;
        }

        private void ReleaseVideoResources()
        {
            if (prepareVideoRoutine != null)
            {
                StopCoroutine(prepareVideoRoutine);
                prepareVideoRoutine = null;
            }

            if (videoPlayer)
            {
                videoPlayer.Stop();
                Destroy(videoPlayer);
                videoPlayer = null;
            }

            if (videoAudioSource)
            {
                videoAudioSource.Stop();
                Destroy(videoAudioSource);
                videoAudioSource = null;
            }

            if (renderTexture)
            {
                renderTexture.Release();
                Destroy(renderTexture);
                renderTexture = null;
            }

            preparedVideoClip = null;
            videoPrepared = false;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            Image image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            return image;
        }

        private static TMP_Text CreateText(string name, Transform parent, string text, float fontSize, Color color, FontStyles style)
        {
            TMP_Text label = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
            label.transform.SetParent(parent, false);
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Max(9f, fontSize * 0.62f);
            label.fontSizeMax = fontSize;
            label.raycastTarget = false;
            return label;
        }

        private void AddScanLines(RectTransform parent)
        {
            for (int i = 0; i < 14; i++)
            {
                Image line = CreateImage("Scanner_Line_" + i, parent, new Color(lineColor.r, lineColor.g, lineColor.b, 0.055f));
                line.raycastTarget = false;
                RectTransform rect = line.rectTransform;
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(1f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(0f, -270f + i * 42f);
            }
        }

        private void AddTechGrid(RectTransform parent)
        {
            for (int i = 0; i < 10; i++)
            {
                Image line = CreateImage("Scanner_Grid_Vertical_" + i, parent, new Color(lineColor.r, lineColor.g, lineColor.b, 0.035f));
                line.raycastTarget = false;
                RectTransform rect = line.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(1f, 0f);
                rect.anchoredPosition = new Vector2(-360f + i * 80f, 0f);
            }
        }

        private void AddFrameLines(RectTransform parent)
        {
            AddFramePart(parent, "Frame_Top", new Vector2(0f, 286f), new Vector2(860f, 3f));
            AddFramePart(parent, "Frame_Bottom", new Vector2(0f, -286f), new Vector2(860f, 3f));
            AddFramePart(parent, "Frame_Left", new Vector2(-430f, 0f), new Vector2(3f, 570f));
            AddFramePart(parent, "Frame_Right", new Vector2(430f, 0f), new Vector2(3f, 570f));

            AddCornerBracket(parent, "TopLeft", new Vector2(-392f, 252f), 1f, 1f);
            AddCornerBracket(parent, "TopRight", new Vector2(392f, 252f), -1f, 1f);
            AddCornerBracket(parent, "BottomLeft", new Vector2(-392f, -252f), 1f, -1f);
            AddCornerBracket(parent, "BottomRight", new Vector2(392f, -252f), -1f, -1f);
        }

        private void AddCornerBracket(RectTransform parent, string name, Vector2 position, float xDirection, float yDirection)
        {
            AddFramePart(parent, "Corner_" + name + "_Horizontal", position + new Vector2(xDirection * 36f, 0f), new Vector2(72f, 4f));
            AddFramePart(parent, "Corner_" + name + "_Vertical", position + new Vector2(0f, yDirection * 36f), new Vector2(4f, 72f));
        }

        private void AddFramePart(RectTransform parent, string name, Vector2 position, Vector2 size)
        {
            Image line = CreateImage(name, parent, new Color(lineColor.r, lineColor.g, lineColor.b, 0.85f));
            line.raycastTarget = false;
            ApplyRect(line.rectTransform, position, size);
        }

        private static void ApplyRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void ApplyStretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
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

                inputSystemUiModule.GetMethod("AssignDefaultActions")?.Invoke(inputModule, null);
            }
            else if (!eventSystem.GetComponent<StandaloneInputModule>())
            {
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            }
        }

        private enum ScannerStep
        {
            Analyze,
            Branch,
            Correction
        }
    }
}
