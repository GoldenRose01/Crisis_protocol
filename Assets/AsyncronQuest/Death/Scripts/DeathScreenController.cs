using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public sealed class DeathScreenController : MonoBehaviour
{
    private const string InputSystemUiModuleTypeName = "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem";

#if UNITY_EDITOR
    private const string DefaultDeathScreenPath = "Assets/AsyncronQuest/SteampunkUI/UI_Style/Death_screen.png";
    private const string AlternateDeathScreenPath = "Assets/AsyncronQuest/SteampunkUI/UI_Style/Morte.png";
#endif

    [Header("Death Screen")]
    [SerializeField] private Sprite deathScreenSprite;
    [SerializeField] private bool preserveAspect;
    [SerializeField, Range(0f, 1f)] private float imageAlpha = 1f;
    [SerializeField] private Color fallbackColor = Color.black;

    [Header("Timing")]
    [SerializeField, Min(0.1f)] private float deathScreenDuration = 2f;
    [SerializeField] private bool pauseTimeDuringDeath = true;

    [Header("Canvas")]
    [SerializeField] private int sortingOrder = 1400;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);

    private GameObject canvasRoot;
    private Coroutine deathRoutine;
    private bool isShowing;

    public static DeathScreenController Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntimeController()
    {
        if (Instance || FindFirstObjectByType<DeathScreenController>())
            return;

        GameObject root = new GameObject("GoldenCast Death Screen Controller");
        DontDestroyOnLoad(root);
        root.AddComponent<DeathScreenController>();
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

#if UNITY_EDITOR
        AssignDefaultEditorAssets();
#endif
    }

    private void OnEnable()
    {
        SalutePlayer.OnPlayerMorto += ShowAndReloadCurrentScene;
    }

    private void OnDisable()
    {
        SalutePlayer.OnPlayerMorto -= ShowAndReloadCurrentScene;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AssignDefaultEditorAssets();

        if (Application.isPlaying && canvasRoot)
        {
            BuildInterface();
            HideImmediate();
        }
    }
#endif

    public static void ShowAndReloadCurrentScene()
    {
        ShowAndReloadCurrentScene(-1f);
    }

    public static void ShowAndReloadCurrentScene(float durationOverride)
    {
        DeathScreenController controller = Instance ? Instance : FindFirstObjectByType<DeathScreenController>();
        if (!controller)
        {
            GameObject root = new GameObject("GoldenCast Death Screen Controller");
            DontDestroyOnLoad(root);
            controller = root.AddComponent<DeathScreenController>();
        }

        controller.PlayAndReload(durationOverride);
    }

    public void PlayAndReload(float durationOverride = -1f)
    {
        if (isShowing)
            return;

        if (deathRoutine != null)
            StopCoroutine(deathRoutine);

        deathRoutine = StartCoroutine(DeathRoutine(durationOverride));
    }

 private IEnumerator DeathRoutine(float durationOverride)
{
    isShowing = true;
    EnsureInterface();

    // 1. Ritardo iniziale prima della schermata
    yield return new WaitForSecondsRealtime(2.0f);

    // 2. Mostra la schermata di morte
    canvasRoot.SetActive(true);

    float previousTimeScale = Time.timeScale;
    if (pauseTimeDuringDeath)
        Time.timeScale = 0f;

    float wait = durationOverride > 0f ? durationOverride : deathScreenDuration;
    yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, wait));

    if (pauseTimeDuringDeath)
        Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;

    // === MODIFICA DI SICUREZZA: Svuota i vecchi ascoltatori degli eventi prima di ricaricare ===
    // Questo evita che i vecchi oggetti distrutti lascino "fantasmi" nella memoria di Unity
    System.Delegate[] clients = GoldenCast.Legacy.GlobalEnvironmentManager.OnCambioEpoca?.GetInvocationList();
    if (clients != null)
    {
        foreach (System.Delegate d in clients)
        {
            GoldenCast.Legacy.GlobalEnvironmentManager.OnCambioEpoca -= (System.Action<int>)d;
        }
    }

    // 3. Ricaricamento della scena
    Scene activeScene = SceneManager.GetActiveScene();
    if (activeScene.buildIndex >= 0)
        SceneManager.LoadScene(activeScene.buildIndex);
    else
        SceneManager.LoadScene(activeScene.name);

    HideImmediate();
    isShowing = false;
    deathRoutine = null;
}

    private void EnsureInterface()
    {
        if (canvasRoot)
            return;

        BuildInterface();
        HideImmediate();
    }

    private void BuildInterface()
    {
        ClearChildren();
        EnsureEventSystem();

        Canvas canvas = new GameObject("Death Screen Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
        canvas.transform.SetParent(transform, false);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        Image image = new GameObject("Death_Static_Image", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(canvas.transform, false);
        Stretch(image.rectTransform);
        image.sprite = deathScreenSprite;
        image.color = deathScreenSprite ? WithAlpha(Color.white, imageAlpha) : WithAlpha(fallbackColor, imageAlpha);
        image.preserveAspect = preserveAspect;
        image.raycastTarget = false;

        CanvasGroup group = canvas.gameObject.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        canvasRoot = canvas.gameObject;
    }

    private void HideImmediate()
    {
        if (canvasRoot)
            canvasRoot.SetActive(false);
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
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
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

#if UNITY_EDITOR
    private void AssignDefaultEditorAssets()
    {
        if (deathScreenSprite)
            return;

        deathScreenSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DefaultDeathScreenPath);
        if (!deathScreenSprite)
            deathScreenSprite = AssetDatabase.LoadAssetAtPath<Sprite>(AlternateDeathScreenPath);
    }
#endif
}
