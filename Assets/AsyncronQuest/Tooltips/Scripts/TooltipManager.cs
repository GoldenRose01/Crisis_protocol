// ============================================================================
// Crisis Protocol / Sector Containment - UI e feedback AsyncronQuest
// File: .\Assets\AsyncronQuest\Tooltips\Scripts\TooltipManager.cs
// Responsabilita': fornisce schermate, tooltip, transizioni, menu e feedback visivi integrati nel progetto Crisis Protocol.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using System.Collections; // usa lib // riga-ok
using AsyncronQuest.SteampunkUI; // usa lib // riga-ok
using TMPro; // usa lib // riga-ok
using UnityEngine; // usa lib // riga-ok
using UnityEngine.EventSystems; // usa lib // riga-ok
using UnityEngine.UI; // usa lib // riga-ok

namespace AsyncronQuest.Tooltips // zona cod // riga-ok
{ // apre // riga-ok
    [DisallowMultipleComponent] // nota unity // riga-ok
    // blocco: classe x roba grossa
    public sealed class TooltipManager : MonoBehaviour // classe qui // riga-ok
    { // apre // riga-ok
        private const string InputSystemUiModuleTypeName = "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem"; // roba pub // riga-ok

        [Header("Localization")] // nota unity // riga-ok
        [SerializeField] private string languageCode = "It"; // setta // riga-ok

        [Header("Speech")] // nota unity // riga-ok
        [SerializeField] private bool readAloud = true; // setta // riga-ok
        [SerializeField, Range(0, 100)] private int speechVolume = 100; // setta // riga-ok
        [SerializeField, Range(-10, 10)] private int speechRate; // ok qua // riga-ok
        [SerializeField] private string voiceNameContains = string.Empty; // setta // riga-ok

        [Header("Canvas")] // nota unity // riga-ok
        [SerializeField] private int sortingOrder = 1200; // setta // riga-ok
        [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f); // setta // riga-ok

        [Header("Bubble Layout")] // nota unity // riga-ok
        [SerializeField] private Vector2 bubbleSize = new Vector2(620f, 150f); // setta // riga-ok
        [SerializeField] private Vector2 bubblePosition = new Vector2(0f, -72f); // setta // riga-ok
        [SerializeField] private Vector2 textPadding = new Vector2(42f, 26f); // setta // riga-ok
        [SerializeField, Range(0f, 1f)] private float bubbleAlpha = 0.88f; // setta // riga-ok
        [SerializeField, Range(0f, 1f)] private float frameAlpha = 0.95f; // setta // riga-ok
        [SerializeField] private float frameThickness = 5f; // setta // riga-ok
        [SerializeField] private float fontSize = 24f; // setta // riga-ok

        [Header("Colors")] // nota unity // riga-ok
        [SerializeField] private Color bubbleColor = new Color(0.08f, 0.035f, 0.02f, 1f); // setta // riga-ok
        [SerializeField] private Color frameColor = new Color(0.78f, 0.52f, 0.18f, 1f); // setta // riga-ok
        [SerializeField] private Color textColor = new Color(1f, 0.82f, 0.42f, 1f); // setta // riga-ok

        private GameObject canvasRoot; // roba pub // riga-ok
        private CanvasGroup group; // roba pub // riga-ok
        private TMP_Text bodyText; // roba pub // riga-ok
        private Coroutine activeRoutine; // roba pub // riga-ok
        private SteampunkUILocalization localization; // roba pub // riga-ok

        public static TooltipManager Instance { get; private set; } // roba pub // riga-ok

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // nota unity // riga-ok
        // blocco: funzione fa cose
        private static void EnsureRuntimeManager() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (Instance || FindFirstObjectByType<TooltipManager>()) // se ok // riga-ok
                return; // torna val // riga-ok

            GameObject root = new GameObject("progetto-precedente Tooltip Manager"); // setta // riga-ok
            DontDestroyOnLoad(root); // chiama // riga-ok
            root.AddComponent<TooltipManager>(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void Awake() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (Instance && Instance != this) // se ok // riga-ok
            { // apre // riga-ok
                Destroy(gameObject); // elimina // riga-ok
                return; // torna val // riga-ok
            } // chiude // riga-ok

            Instance = this; // setta // riga-ok
            DontDestroyOnLoad(gameObject); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void OnDestroy() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (Instance == this) // se ok // riga-ok
                Instance = null; // setta // riga-ok
        } // chiude // riga-ok

#if UNITY_EDITOR // prep ok // riga-ok
        // blocco: funzione fa cose
        private void OnValidate() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (Application.isPlaying && isActiveAndEnabled && canvasRoot) // se ok // riga-ok
            { // apre // riga-ok
                BuildInterface(); // chiama // riga-ok
                HideImmediate(); // chiama // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
#endif // prep ok // riga-ok

        // blocco: funzione fa cose
        public static void Show(string localizationKey, float duration = 5f, bool speak = true) // roba pub // riga-ok
        { // apre // riga-ok
            TooltipManager manager = Instance ? Instance : FindFirstObjectByType<TooltipManager>(); // setta // riga-ok
            // blocco: controlla se va
            if (!manager) // se ok // riga-ok
            { // apre // riga-ok
                GameObject root = new GameObject("progetto-precedente Tooltip Manager"); // setta // riga-ok
                DontDestroyOnLoad(root); // chiama // riga-ok
                manager = root.AddComponent<TooltipManager>(); // setta // riga-ok
            } // chiude // riga-ok

            manager.ShowLocalized(localizationKey, duration, speak); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public void SetLanguage(string newLanguageCode) // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (string.IsNullOrWhiteSpace(newLanguageCode)) // se ok // riga-ok
                return; // torna val // riga-ok

            languageCode = newLanguageCode; // setta // riga-ok
            localization = null; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public void ShowLocalized(string localizationKey, float duration = 5f, bool speak = true) // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (string.IsNullOrWhiteSpace(localizationKey)) // se ok // riga-ok
                return; // torna val // riga-ok

            EnsureLocalization(); // chiama // riga-ok
            ShowText(localization.Get(localizationKey), duration, speak); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public void ShowText(string text, float duration = 5f, bool speak = true) // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (string.IsNullOrWhiteSpace(text)) // se ok // riga-ok
                return; // torna val // riga-ok

            EnsureInterface(); // chiama // riga-ok

            // blocco: controlla se va
            if (activeRoutine != null) // se ok // riga-ok
                StopCoroutine(activeRoutine); // corutina // riga-ok

            activeRoutine = StartCoroutine(ShowRoutine(text, duration, speak)); // setta // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public void Hide() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (activeRoutine != null) // se ok // riga-ok
            { // apre // riga-ok
                StopCoroutine(activeRoutine); // corutina // riga-ok
                activeRoutine = null; // setta // riga-ok
            } // chiude // riga-ok

            TooltipSpeechSynthesizer.Stop(); // chiama // riga-ok
            HideImmediate(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private IEnumerator ShowRoutine(string text, float duration, bool speak) // roba pub // riga-ok
        { // apre // riga-ok
            EnsureInterface(); // chiama // riga-ok
            canvasRoot.SetActive(true); // chiama // riga-ok
            bodyText.text = text; // setta // riga-ok
            group.alpha = 1f; // setta // riga-ok
            group.interactable = false; // setta // riga-ok
            group.blocksRaycasts = false; // setta // riga-ok

            // blocco: controlla se va
            if (readAloud && speak) // se ok // riga-ok
            { // apre // riga-ok
                TooltipSpeechSynthesizer.Configure(voiceNameContains, speechVolume, speechRate); // chiama // riga-ok
                TooltipSpeechSynthesizer.Speak(text); // chiama // riga-ok
            } // chiude // riga-ok

            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, duration)); // aspetta // riga-ok
            HideImmediate(); // chiama // riga-ok
            activeRoutine = null; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void BuildInterface() // roba pub // riga-ok
        { // apre // riga-ok
            ClearChildren(); // chiama // riga-ok
            EnsureEventSystem(); // chiama // riga-ok

            Canvas canvas = new GameObject("Tooltip Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>(); // setta // riga-ok
            canvas.transform.SetParent(transform, false); // chiama // riga-ok
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // setta // riga-ok
            canvas.sortingOrder = sortingOrder; // setta // riga-ok

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>(); // setta // riga-ok
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // setta // riga-ok
            scaler.referenceResolution = referenceResolution; // setta // riga-ok
            scaler.matchWidthOrHeight = 0.5f; // setta // riga-ok

            RectTransform bubble = CreateImage("Tooltip_Bubble", canvas.transform, WithAlpha(bubbleColor, bubbleAlpha)).rectTransform; // setta // riga-ok
            bubble.anchorMin = new Vector2(0.5f, 1f); // setta // riga-ok
            bubble.anchorMax = new Vector2(0.5f, 1f); // setta // riga-ok
            bubble.pivot = new Vector2(0.5f, 1f); // setta // riga-ok
            bubble.sizeDelta = bubbleSize; // setta // riga-ok
            bubble.anchoredPosition = bubblePosition; // setta // riga-ok
            bubble.GetComponent<Image>().raycastTarget = false; // setta // riga-ok

            AddFrame(bubble, WithAlpha(frameColor, frameAlpha), frameThickness); // chiama // riga-ok

            bodyText = new GameObject("Tooltip_Text", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>(); // setta // riga-ok
            bodyText.transform.SetParent(bubble, false); // chiama // riga-ok
            RectTransform textRect = bodyText.rectTransform; // setta // riga-ok
            Stretch(textRect); // chiama // riga-ok
            textRect.offsetMin = textPadding; // setta // riga-ok
            textRect.offsetMax = -textPadding; // setta // riga-ok

            bodyText.text = string.Empty; // setta // riga-ok
            bodyText.color = textColor; // setta // riga-ok
            bodyText.fontSize = fontSize; // setta // riga-ok
            bodyText.fontSizeMin = Mathf.Max(12f, fontSize * 0.58f); // setta // riga-ok
            bodyText.fontSizeMax = fontSize; // setta // riga-ok
            bodyText.enableAutoSizing = true; // setta // riga-ok
            bodyText.alignment = TextAlignmentOptions.MidlineLeft; // setta // riga-ok
            bodyText.raycastTarget = false; // setta // riga-ok

            group = canvas.gameObject.AddComponent<CanvasGroup>(); // setta // riga-ok
            group.interactable = false; // setta // riga-ok
            group.blocksRaycasts = false; // setta // riga-ok
            canvasRoot = canvas.gameObject; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void HideImmediate() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (group) // se ok // riga-ok
            { // apre // riga-ok
                group.alpha = 0f; // setta // riga-ok
                group.interactable = false; // setta // riga-ok
                group.blocksRaycasts = false; // setta // riga-ok
            } // chiude // riga-ok

            // blocco: controlla se va
            if (canvasRoot) // se ok // riga-ok
                canvasRoot.SetActive(false); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void EnsureLocalization() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (localization == null) // se ok // riga-ok
                localization = SteampunkUILocalization.Load(languageCode); // setta // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void EnsureInterface() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (canvasRoot && group && bodyText) // se ok // riga-ok
                return; // torna val // riga-ok

            BuildInterface(); // chiama // riga-ok
            HideImmediate(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void ClearChildren() // roba pub // riga-ok
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

            canvasRoot = null; // setta // riga-ok
            group = null; // setta // riga-ok
            bodyText = null; // setta // riga-ok
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
        private static void AddFrame(RectTransform parent, Color color, float thickness) // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (thickness <= 0f) // se ok // riga-ok
                return; // torna val // riga-ok

            AddFramePart("Frame_Top", parent, new Vector2(0f, parent.sizeDelta.y * 0.5f - thickness * 0.5f), new Vector2(parent.sizeDelta.x, thickness), color); // chiama // riga-ok
            AddFramePart("Frame_Bottom", parent, new Vector2(0f, -parent.sizeDelta.y * 0.5f + thickness * 0.5f), new Vector2(parent.sizeDelta.x, thickness), color); // chiama // riga-ok
            AddFramePart("Frame_Left", parent, new Vector2(-parent.sizeDelta.x * 0.5f + thickness * 0.5f, 0f), new Vector2(thickness, parent.sizeDelta.y), color); // chiama // riga-ok
            AddFramePart("Frame_Right", parent, new Vector2(parent.sizeDelta.x * 0.5f - thickness * 0.5f, 0f), new Vector2(thickness, parent.sizeDelta.y), color); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private static void AddFramePart(string name, RectTransform parent, Vector2 position, Vector2 size, Color color) // roba pub // riga-ok
        { // apre // riga-ok
            Image image = CreateImage(name, parent, color); // setta // riga-ok
            image.raycastTarget = false; // setta // riga-ok
            RectTransform rect = image.rectTransform; // setta // riga-ok
            rect.anchorMin = new Vector2(0.5f, 0.5f); // setta // riga-ok
            rect.anchorMax = new Vector2(0.5f, 0.5f); // setta // riga-ok
            rect.pivot = new Vector2(0.5f, 0.5f); // setta // riga-ok
            rect.anchoredPosition = position; // setta // riga-ok
            rect.sizeDelta = size; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private static Color WithAlpha(Color color, float alpha) // roba pub // riga-ok
        { // apre // riga-ok
            color.a = Mathf.Clamp01(alpha); // setta // riga-ok
            return color; // torna val // riga-ok
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

            System.Type inputSystemUiModule = System.Type.GetType(InputSystemUiModuleTypeName); // setta // riga-ok
            // blocco: controlla se va
            if (inputSystemUiModule != null) // se ok // riga-ok
            { // apre // riga-ok
                StandaloneInputModule oldModule = eventSystem.GetComponent<StandaloneInputModule>(); // setta // riga-ok
                if (oldModule != null) UnityEngine.Object.Destroy(oldModule); // chiama // riga-ok

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

