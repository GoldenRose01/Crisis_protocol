using System.Collections;
using AsyncronQuest.SteampunkUI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AsyncronQuest.Tooltips
{
    [DisallowMultipleComponent]
    public sealed class TooltipManager : MonoBehaviour
    {
        private const string InputSystemUiModuleTypeName = "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem";

        [Header("Localization")]
        [SerializeField] private string languageCode = "It";

        [Header("Speech")]
        [SerializeField] private bool readAloud = true;
        [SerializeField, Range(0, 100)] private int speechVolume = 100;
        [SerializeField, Range(-10, 10)] private int speechRate;
        [SerializeField] private string voiceNameContains = string.Empty;

        [Header("Canvas")]
        [SerializeField] private int sortingOrder = 1200;
        [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);

        [Header("Bubble Layout")]
        [SerializeField] private Vector2 bubbleSize = new Vector2(620f, 150f);
        [SerializeField] private Vector2 bubblePosition = new Vector2(0f, -72f);
        [SerializeField] private Vector2 textPadding = new Vector2(42f, 26f);
        [SerializeField, Range(0f, 1f)] private float bubbleAlpha = 0.88f;
        [SerializeField, Range(0f, 1f)] private float frameAlpha = 0.95f;
        [SerializeField] private float frameThickness = 5f;
        [SerializeField] private float fontSize = 24f;

        [Header("Colors")]
        [SerializeField] private Color bubbleColor = new Color(0.08f, 0.035f, 0.02f, 1f);
        [SerializeField] private Color frameColor = new Color(0.78f, 0.52f, 0.18f, 1f);
        [SerializeField] private Color textColor = new Color(1f, 0.82f, 0.42f, 1f);

        private GameObject canvasRoot;
        private CanvasGroup group;
        private TMP_Text bodyText;
        private Coroutine activeRoutine;
        private SteampunkUILocalization localization;

        public static TooltipManager Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntimeManager()
        {
            if (Instance || FindFirstObjectByType<TooltipManager>())
                return;

            GameObject root = new GameObject("GoldenCast Tooltip Manager");
            DontDestroyOnLoad(root);
            root.AddComponent<TooltipManager>();
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
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying && isActiveAndEnabled && canvasRoot)
            {
                BuildInterface();
                HideImmediate();
            }
        }
#endif

        public static void Show(string localizationKey, float duration = 5f, bool speak = true)
        {
            TooltipManager manager = Instance ? Instance : FindFirstObjectByType<TooltipManager>();
            if (!manager)
            {
                GameObject root = new GameObject("GoldenCast Tooltip Manager");
                DontDestroyOnLoad(root);
                manager = root.AddComponent<TooltipManager>();
            }

            manager.ShowLocalized(localizationKey, duration, speak);
        }

        public void SetLanguage(string newLanguageCode)
        {
            if (string.IsNullOrWhiteSpace(newLanguageCode))
                return;

            languageCode = newLanguageCode;
            localization = null;
        }

        public void ShowLocalized(string localizationKey, float duration = 5f, bool speak = true)
        {
            if (string.IsNullOrWhiteSpace(localizationKey))
                return;

            EnsureLocalization();
            ShowText(localization.Get(localizationKey), duration, speak);
        }

        public void ShowText(string text, float duration = 5f, bool speak = true)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            EnsureInterface();

            if (activeRoutine != null)
                StopCoroutine(activeRoutine);

            activeRoutine = StartCoroutine(ShowRoutine(text, duration, speak));
        }

        public void Hide()
        {
            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                activeRoutine = null;
            }

            TooltipSpeechSynthesizer.Stop();
            HideImmediate();
        }

        private IEnumerator ShowRoutine(string text, float duration, bool speak)
        {
            EnsureInterface();
            canvasRoot.SetActive(true);
            bodyText.text = text;
            group.alpha = 1f;
            group.interactable = false;
            group.blocksRaycasts = false;

            if (readAloud && speak)
            {
                TooltipSpeechSynthesizer.Configure(voiceNameContains, speechVolume, speechRate);
                TooltipSpeechSynthesizer.Speak(text);
            }

            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, duration));
            HideImmediate();
            activeRoutine = null;
        }

        private void BuildInterface()
        {
            ClearChildren();
            EnsureEventSystem();

            Canvas canvas = new GameObject("Tooltip Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.transform.SetParent(transform, false);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform bubble = CreateImage("Tooltip_Bubble", canvas.transform, WithAlpha(bubbleColor, bubbleAlpha)).rectTransform;
            bubble.anchorMin = new Vector2(0.5f, 1f);
            bubble.anchorMax = new Vector2(0.5f, 1f);
            bubble.pivot = new Vector2(0.5f, 1f);
            bubble.sizeDelta = bubbleSize;
            bubble.anchoredPosition = bubblePosition;
            bubble.GetComponent<Image>().raycastTarget = false;

            AddFrame(bubble, WithAlpha(frameColor, frameAlpha), frameThickness);

            bodyText = new GameObject("Tooltip_Text", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
            bodyText.transform.SetParent(bubble, false);
            RectTransform textRect = bodyText.rectTransform;
            Stretch(textRect);
            textRect.offsetMin = textPadding;
            textRect.offsetMax = -textPadding;

            bodyText.text = string.Empty;
            bodyText.color = textColor;
            bodyText.fontSize = fontSize;
            bodyText.fontSizeMin = Mathf.Max(12f, fontSize * 0.58f);
            bodyText.fontSizeMax = fontSize;
            bodyText.enableAutoSizing = true;
            bodyText.alignment = TextAlignmentOptions.MidlineLeft;
            bodyText.raycastTarget = false;

            group = canvas.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            canvasRoot = canvas.gameObject;
        }

        private void HideImmediate()
        {
            if (group)
            {
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }

            if (canvasRoot)
                canvasRoot.SetActive(false);
        }

        private void EnsureLocalization()
        {
            if (localization == null)
                localization = SteampunkUILocalization.Load(languageCode);
        }

        private void EnsureInterface()
        {
            if (canvasRoot && group && bodyText)
                return;

            BuildInterface();
            HideImmediate();
        }

        private void ClearChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }

            canvasRoot = null;
            group = null;
            bodyText = null;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            Image image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            return image;
        }

        private static void AddFrame(RectTransform parent, Color color, float thickness)
        {
            if (thickness <= 0f)
                return;

            AddFramePart("Frame_Top", parent, new Vector2(0f, parent.sizeDelta.y * 0.5f - thickness * 0.5f), new Vector2(parent.sizeDelta.x, thickness), color);
            AddFramePart("Frame_Bottom", parent, new Vector2(0f, -parent.sizeDelta.y * 0.5f + thickness * 0.5f), new Vector2(parent.sizeDelta.x, thickness), color);
            AddFramePart("Frame_Left", parent, new Vector2(-parent.sizeDelta.x * 0.5f + thickness * 0.5f, 0f), new Vector2(thickness, parent.sizeDelta.y), color);
            AddFramePart("Frame_Right", parent, new Vector2(parent.sizeDelta.x * 0.5f - thickness * 0.5f, 0f), new Vector2(thickness, parent.sizeDelta.y), color);
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

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = EventSystem.current;
            if (!eventSystem)
                eventSystem = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();

            System.Type inputSystemUiModule = System.Type.GetType(InputSystemUiModuleTypeName);
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
