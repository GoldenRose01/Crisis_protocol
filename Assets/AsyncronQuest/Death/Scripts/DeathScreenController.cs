// ============================================================================
// Crisis Protocol / Sector Containment - UI e feedback AsyncronQuest
// File: .\Assets\AsyncronQuest\Death\Scripts\DeathScreenController.cs
// Responsabilita': fornisce schermate, tooltip, transizioni, menu e feedback visivi integrati nel progetto Crisis Protocol.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
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

    [Header("Comportamento Riavvio")]
    [Tooltip("Se true, ricarica in automatico dopo la durata specificata. Se false (consigliato), attende che il giocatore prema il pulsante 'Riprova'.")]
    [SerializeField] private bool ricaricaAutomaticaSenzaPulsante = false;
    [SerializeField, Min(0.1f)] private float tempoRicaricaAutomatica = 4f;
    [SerializeField] private bool pauseTimeDuringDeath = true;

    [Header("Canvas")]
    [SerializeField] private int sortingOrder = 9999;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);

    private GameObject canvasRoot;
    private Text subtitleText;
    private Coroutine deathRoutine;
    private bool isShowing;

    public static DeathScreenController Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntimeController()
    {
        if (Instance || FindFirstObjectByType<DeathScreenController>())
            return;

        GameObject root = new GameObject("progetto-precedente Death Screen Controller");
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
        SalutePlayer.OnPlayerMorto += HandlePlayerMorto;
        MissionManager.OnMissioneTerminata += HandleMissioneTerminata;
    }

    private void OnDisable()
    {
        SalutePlayer.OnPlayerMorto -= HandlePlayerMorto;
        MissionManager.OnMissioneTerminata -= HandleMissioneTerminata;
    }

    private void HandlePlayerMorto()
    {
        PlayAndReload(1.0f, "Operatore neutralizzato // Punti vita esauriti.");
    }

    private void HandleMissioneTerminata(MissionManager.MissionOutcome outcome, int score, string reason)
    {
        if (outcome == MissionManager.MissionOutcome.Defeat)
        {
            PlayAndReload(0f, string.IsNullOrWhiteSpace(reason) ? "Missione Fallita." : reason);
        }
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
        ShowAndReloadCurrentScene(0f, "");
    }

    public static void ShowAndReloadCurrentScene(float delayBeforeShow)
    {
        ShowAndReloadCurrentScene(delayBeforeShow, "");
    }

    public static void ShowAndReloadCurrentScene(float delayBeforeShow, string reasonText)
    {
        DeathScreenController controller = Instance ? Instance : FindFirstObjectByType<DeathScreenController>();
        if (!controller)
        {
            GameObject root = new GameObject("progetto-precedente Death Screen Controller");
            DontDestroyOnLoad(root);
            controller = root.AddComponent<DeathScreenController>();
        }

        controller.PlayAndReload(delayBeforeShow, reasonText);
    }

    public static void ShowAndReloadCurrentScene(float durationOverride, float delayBeforeShow, string reasonText)
    {
        ShowAndReloadCurrentScene(delayBeforeShow, reasonText);
    }

    public void PlayAndReload(float delayBeforeShow = 0f, string reasonText = "")
    {
        if (isShowing)
            return;

        if (deathRoutine != null)
            StopCoroutine(deathRoutine);

        deathRoutine = StartCoroutine(DeathRoutine(delayBeforeShow, reasonText));
    }

    public void PlayAndReload(float durationOverride, float delayBeforeShow, string reasonText)
    {
        PlayAndReload(delayBeforeShow, reasonText);
    }

    private IEnumerator DeathRoutine(float delayBeforeShow, string reasonText)
    {
        isShowing = true;
        EnsureInterface();

        if (subtitleText != null && !string.IsNullOrWhiteSpace(reasonText))
        {
            subtitleText.text = reasonText;
        }

        if (delayBeforeShow > 0f)
        {
            yield return new WaitForSecondsRealtime(delayBeforeShow);
        }

        // Mostra la schermata di morte
        if (canvasRoot != null)
        {
            canvasRoot.SetActive(true);
        }

        // Sblocca e rende visibile il cursore per poter cliccare il pulsante Riprova
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        float previousTimeScale = Time.timeScale;
        if (pauseTimeDuringDeath)
            Time.timeScale = 0f;

        // Se è impostata la ricarica automatica, aspetta il tempo ed esegui
        if (ricaricaAutomaticaSenzaPulsante)
        {
            yield return new WaitForSecondsRealtime(tempoRicaricaAutomatica);

            if (pauseTimeDuringDeath)
                Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;

            RiavviaLivelloConResetTotale();
        }
    }

    /// <summary>
    /// Riavvia il livello azzerando completamente i progressi della sessione e il timer.
    /// </summary>
    public void RiavviaLivelloConResetTotale()
    {
        Debug.Log("<color=cyan><b>[RIPROVA LIVELLO]</b> Riavvio del settore in corso con azzeramento progressi e timer...</color>");

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 1. Reset completo del timer di missione e dell'HUD
        if (CyberHUD.Instance != null)
        {
            CyberHUD.Instance.ResetCountdown();
            CyberHUD.Instance.InizializzaStatoIniziale();
        }

        // 2. Ricaricamento della scena attiva
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.buildIndex >= 0)
            SceneManager.LoadScene(activeScene.buildIndex);
        else
            SceneManager.LoadScene(activeScene.name);

        HideImmediate();
        isShowing = false;
        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
            deathRoutine = null;
        }
    }

    /// <summary>
    /// Torna al Menu Principale.
    /// </summary>
    public void TornaAlMenuPrincipale()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        HideImmediate();
        isShowing = false;
        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
            deathRoutine = null;
        }

        SceneManager.LoadScene("MainMenu-Scene");
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

        // 1. Sfondo Oscurante Totale
        GameObject bgObj = new GameObject("Death_Background", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(canvas.transform, false);
        Image bgImg = bgObj.GetComponent<Image>();
        Stretch(bgImg.rectTransform);
        bgImg.color = new Color(0.02f, 0.03f, 0.05f, 0.94f);
        bgImg.raycastTarget = true;

        if (deathScreenSprite == null)
        {
#if UNITY_EDITOR
            AssignDefaultEditorAssets();
#endif
        }

        // 2. Texture Grafica Game Over
        Image image = new GameObject("Death_Static_Image", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(canvas.transform, false);
        Stretch(image.rectTransform);
        image.sprite = deathScreenSprite;
        image.color = deathScreenSprite ? WithAlpha(Color.white, imageAlpha) : WithAlpha(fallbackColor, imageAlpha);
        image.preserveAspect = preserveAspect;
        image.raycastTarget = false;

        // 3. Titolo Grande di Fallimento
        GameObject textObj = new GameObject("Death_Title", typeof(RectTransform), typeof(Text));
        textObj.transform.SetParent(canvas.transform, false);
        Text title = textObj.GetComponent<Text>();
        title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        title.fontSize = 56;
        title.fontStyle = FontStyle.Bold;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(1f, 0.18f, 0.18f, 1f);
        title.text = "MISSIONE FALLITA";
        RectTransform tRect = title.rectTransform;
        tRect.anchorMin = new Vector2(0.05f, 0.38f);
        tRect.anchorMax = new Vector2(0.95f, 0.56f);
        tRect.offsetMin = Vector2.zero;
        tRect.offsetMax = Vector2.zero;

        // 4. Testo di Dettaglio / Motivo (es. Tempo Scaduto)
        GameObject subObj = new GameObject("Death_Subtitle", typeof(RectTransform), typeof(Text));
        subObj.transform.SetParent(canvas.transform, false);
        subtitleText = subObj.GetComponent<Text>();
        subtitleText.font = title.font;
        subtitleText.fontSize = 24;
        subtitleText.alignment = TextAnchor.MiddleCenter;
        subtitleText.color = new Color(0.88f, 0.88f, 0.92f, 0.95f);
        subtitleText.text = "Tempo Scaduto // Evacuazione Fallita";
        RectTransform sRect = subtitleText.rectTransform;
        sRect.anchorMin = new Vector2(0.05f, 0.28f);
        sRect.anchorMax = new Vector2(0.95f, 0.39f);
        sRect.offsetMin = Vector2.zero;
        sRect.offsetMax = Vector2.zero;

        // 5. Pulsante "RIPROVA MISSIONE" (Interattivo, Stile Cyberpunk)
        GameObject btnRetryObj = new GameObject("Button_Riprova", typeof(RectTransform), typeof(Image), typeof(Button));
        btnRetryObj.transform.SetParent(canvas.transform, false);
        RectTransform btnRect = btnRetryObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.18f);
        btnRect.anchorMax = new Vector2(0.5f, 0.18f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.sizeDelta = new Vector2(360f, 64f);

        Image btnImg = btnRetryObj.GetComponent<Image>();
        btnImg.color = new Color(0.08f, 0.55f, 0.38f, 0.95f); // Verde neon / cyan elegante

        Button btn = btnRetryObj.GetComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.08f, 0.55f, 0.38f, 0.95f);
        cb.highlightedColor = new Color(0.14f, 0.85f, 0.58f, 1f);
        cb.pressedColor = new Color(0.05f, 0.38f, 0.26f, 1f);
        cb.selectedColor = cb.highlightedColor;
        btn.colors = cb;
        btn.onClick.AddListener(RiavviaLivelloConResetTotale);

        // Bordo / Outline Neon Pulsante Riprova
        Outline btnOutline = btnRetryObj.AddComponent<Outline>();
        btnOutline.effectColor = new Color(0.2f, 1f, 0.65f, 0.9f);
        btnOutline.effectDistance = new Vector2(2f, -2f);

        // Testo del Pulsante Riprova
        GameObject btnTextObj = new GameObject("Button_Text", typeof(RectTransform), typeof(Text));
        btnTextObj.transform.SetParent(btnRetryObj.transform, false);
        Text btnText = btnTextObj.GetComponent<Text>();
        btnText.font = title.font;
        btnText.fontSize = 24;
        btnText.fontStyle = FontStyle.Bold;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.color = Color.white;
        btnText.text = "🔄 RIPROVA LIVELLO";
        Stretch(btnText.rectTransform);

        // 6. Pulsante secondario "MENU PRINCIPALE"
        GameObject btnMenuObj = new GameObject("Button_Menu", typeof(RectTransform), typeof(Image), typeof(Button));
        btnMenuObj.transform.SetParent(canvas.transform, false);
        RectTransform menuRect = btnMenuObj.GetComponent<RectTransform>();
        menuRect.anchorMin = new Vector2(0.5f, 0.08f);
        menuRect.anchorMax = new Vector2(0.5f, 0.08f);
        menuRect.pivot = new Vector2(0.5f, 0.5f);
        menuRect.sizeDelta = new Vector2(260f, 44f);

        Image menuImg = btnMenuObj.GetComponent<Image>();
        menuImg.color = new Color(0.22f, 0.25f, 0.30f, 0.90f);

        Button menuBtn = btnMenuObj.GetComponent<Button>();
        ColorBlock mcb = menuBtn.colors;
        mcb.normalColor = new Color(0.22f, 0.25f, 0.30f, 0.90f);
        mcb.highlightedColor = new Color(0.40f, 0.45f, 0.55f, 1f);
        mcb.pressedColor = new Color(0.14f, 0.16f, 0.20f, 1f);
        menuBtn.colors = mcb;
        menuBtn.onClick.AddListener(TornaAlMenuPrincipale);

        GameObject menuTextObj = new GameObject("Menu_Text", typeof(RectTransform), typeof(Text));
        menuTextObj.transform.SetParent(btnMenuObj.transform, false);
        Text menuText = menuTextObj.GetComponent<Text>();
        menuText.font = title.font;
        menuText.fontSize = 18;
        menuText.alignment = TextAnchor.MiddleCenter;
        menuText.color = new Color(0.85f, 0.88f, 0.92f, 1f);
        menuText.text = "🏠 MENU PRINCIPALE";
        Stretch(menuText.rectTransform);

        CanvasGroup group = canvas.gameObject.AddComponent<CanvasGroup>();
        group.interactable = true;
        group.blocksRaycasts = true;

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
        subtitleText = null;
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
