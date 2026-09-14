// ============================================================================
// Crisis Protocol / Sector Containment - UI e feedback AsyncronQuest
// File: .\Assets\AsyncronQuest\Death\Scripts\DeathScreenController.cs
// Responsabilita': fornisce schermate, tooltip, transizioni, menu e feedback visivi integrati nel progetto Crisis Protocol.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using System.Collections; // usa lib // riga-ok
using UnityEngine; // usa lib // riga-ok
using UnityEngine.EventSystems; // usa lib // riga-ok
using UnityEngine.SceneManagement; // usa lib // riga-ok
using UnityEngine.UI; // usa lib // riga-ok

#if UNITY_EDITOR // prep ok // riga-ok
using UnityEditor; // usa lib // riga-ok
#endif // prep ok // riga-ok

[DisallowMultipleComponent] // nota unity // riga-ok
// blocco: classe x roba grossa
public sealed class DeathScreenController : MonoBehaviour // classe qui // riga-ok
{ // apre // riga-ok
    private const string InputSystemUiModuleTypeName = "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem"; // roba pub // riga-ok

#if UNITY_EDITOR // prep ok // riga-ok
    private const string DefaultDeathScreenPath = "Assets/AsyncronQuest/SteampunkUI/UI_Style/Death_screen.png"; // roba pub // riga-ok
    private const string AlternateDeathScreenPath = "Assets/AsyncronQuest/SteampunkUI/UI_Style/Morte.png"; // roba pub // riga-ok
#endif // prep ok // riga-ok

    [Header("Death Screen")] // nota unity // riga-ok
    [SerializeField] private Sprite deathScreenSprite; // ok qua // riga-ok
    [SerializeField] private bool preserveAspect; // ok qua // riga-ok
    [SerializeField, Range(0f, 1f)] private float imageAlpha = 1f; // setta // riga-ok
    [SerializeField] private Color fallbackColor = Color.black; // setta // riga-ok

    [Header("Comportamento Riavvio")] // nota unity // riga-ok
    [Tooltip("Se true, ricarica in automatico dopo la durata specificata. Se false (consigliato), attende che il giocatore prema il pulsante 'Riprova'.")] // nota unity // riga-ok
    [SerializeField] private bool ricaricaAutomaticaSenzaPulsante = false; // setta // riga-ok
    [SerializeField, Min(0.1f)] private float tempoRicaricaAutomatica = 4f; // setta // riga-ok
    [SerializeField] private bool pauseTimeDuringDeath = true; // setta // riga-ok

    [Header("Canvas")] // nota unity // riga-ok
    [SerializeField] private int sortingOrder = 9999; // setta // riga-ok
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f); // setta // riga-ok

    private GameObject canvasRoot; // roba pub // riga-ok
    private Text subtitleText; // roba pub // riga-ok
    private Coroutine deathRoutine; // roba pub // riga-ok
    private bool isShowing; // roba pub // riga-ok

    public static DeathScreenController Instance { get; private set; } // roba pub // riga-ok

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // nota unity // riga-ok
    // blocco: funzione fa cose
    private static void EnsureRuntimeController() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (Instance || FindFirstObjectByType<DeathScreenController>()) // se ok // riga-ok
            return; // torna val // riga-ok

        GameObject root = new GameObject("progetto-precedente Death Screen Controller"); // setta // riga-ok
        DontDestroyOnLoad(root); // chiama // riga-ok
        root.AddComponent<DeathScreenController>(); // chiama // riga-ok
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

#if UNITY_EDITOR // prep ok // riga-ok
        AssignDefaultEditorAssets(); // chiama // riga-ok
#endif // prep ok // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnEnable() // roba pub // riga-ok
    { // apre // riga-ok
        SalutePlayer.OnPlayerMorto += HandlePlayerMorto; // setta // riga-ok
        MissionManager.OnMissioneTerminata += HandleMissioneTerminata; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnDisable() // roba pub // riga-ok
    { // apre // riga-ok
        SalutePlayer.OnPlayerMorto -= HandlePlayerMorto; // setta // riga-ok
        MissionManager.OnMissioneTerminata -= HandleMissioneTerminata; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void HandlePlayerMorto() // roba pub // riga-ok
    { // apre // riga-ok
        PlayAndReload(1.0f, "Operatore neutralizzato // Punti vita esauriti."); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void HandleMissioneTerminata(MissionManager.MissionOutcome outcome, int score, string reason) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (outcome == MissionManager.MissionOutcome.Defeat) // se ok // riga-ok
        { // apre // riga-ok
            PlayAndReload(0f, string.IsNullOrWhiteSpace(reason) ? "Missione Fallita." : reason); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

#if UNITY_EDITOR // prep ok // riga-ok
    // blocco: funzione fa cose
    private void OnValidate() // roba pub // riga-ok
    { // apre // riga-ok
        AssignDefaultEditorAssets(); // chiama // riga-ok

        // blocco: controlla se va
        if (Application.isPlaying && canvasRoot) // se ok // riga-ok
        { // apre // riga-ok
            BuildInterface(); // chiama // riga-ok
            HideImmediate(); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok
#endif // prep ok // riga-ok

    // blocco: funzione fa cose
    public static void ShowAndReloadCurrentScene() // roba pub // riga-ok
    { // apre // riga-ok
        ShowAndReloadCurrentScene(0f, ""); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public static void ShowAndReloadCurrentScene(float delayBeforeShow) // roba pub // riga-ok
    { // apre // riga-ok
        ShowAndReloadCurrentScene(delayBeforeShow, ""); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public static void ShowAndReloadCurrentScene(float delayBeforeShow, string reasonText) // roba pub // riga-ok
    { // apre // riga-ok
        DeathScreenController controller = Instance ? Instance : FindFirstObjectByType<DeathScreenController>(); // setta // riga-ok
        // blocco: controlla se va
        if (!controller) // se ok // riga-ok
        { // apre // riga-ok
            GameObject root = new GameObject("progetto-precedente Death Screen Controller"); // setta // riga-ok
            DontDestroyOnLoad(root); // chiama // riga-ok
            controller = root.AddComponent<DeathScreenController>(); // setta // riga-ok
        } // chiude // riga-ok

        controller.PlayAndReload(delayBeforeShow, reasonText); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public static void ShowAndReloadCurrentScene(float durationOverride, float delayBeforeShow, string reasonText) // roba pub // riga-ok
    { // apre // riga-ok
        ShowAndReloadCurrentScene(delayBeforeShow, reasonText); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void PlayAndReload(float delayBeforeShow = 0f, string reasonText = "") // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (isShowing) // se ok // riga-ok
            return; // torna val // riga-ok

        // blocco: controlla se va
        if (deathRoutine != null) // se ok // riga-ok
            StopCoroutine(deathRoutine); // corutina // riga-ok

        deathRoutine = StartCoroutine(DeathRoutine(delayBeforeShow, reasonText)); // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void PlayAndReload(float durationOverride, float delayBeforeShow, string reasonText) // roba pub // riga-ok
    { // apre // riga-ok
        PlayAndReload(delayBeforeShow, reasonText); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private IEnumerator DeathRoutine(float delayBeforeShow, string reasonText) // roba pub // riga-ok
    { // apre // riga-ok
        isShowing = true; // setta // riga-ok
        EnsureInterface(); // chiama // riga-ok

        // blocco: controlla se va
        if (subtitleText != null && !string.IsNullOrWhiteSpace(reasonText)) // se ok // riga-ok
        { // apre // riga-ok
            subtitleText.text = reasonText; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (delayBeforeShow > 0f) // se ok // riga-ok
        { // apre // riga-ok
            yield return new WaitForSecondsRealtime(delayBeforeShow); // aspetta // riga-ok
        } // chiude // riga-ok

        // Mostra la schermata di morte
        // blocco: controlla se va
        if (canvasRoot != null) // se ok // riga-ok
        { // apre // riga-ok
            canvasRoot.SetActive(true); // chiama // riga-ok
        } // chiude // riga-ok

        // Sblocca e rende visibile il cursore per poter cliccare il pulsante Riprova
        Cursor.lockState = CursorLockMode.None; // setta // riga-ok
        Cursor.visible = true; // setta // riga-ok

        float previousTimeScale = Time.timeScale; // setta // riga-ok
        // blocco: controlla se va
        if (pauseTimeDuringDeath) // se ok // riga-ok
            Time.timeScale = 0f; // setta // riga-ok

        // Se è impostata la ricarica automatica, aspetta il tempo ed esegui
        // blocco: controlla se va
        if (ricaricaAutomaticaSenzaPulsante) // se ok // riga-ok
        { // apre // riga-ok
            yield return new WaitForSecondsRealtime(tempoRicaricaAutomatica); // aspetta // riga-ok

            // blocco: controlla se va
            if (pauseTimeDuringDeath) // se ok // riga-ok
                Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale; // setta // riga-ok

            RiavviaLivelloConResetTotale(); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    /// <summary>
    /// Riavvia il livello azzerando completamente i progressi della sessione e il timer.
    /// </summary>
    // blocco: funzione fa cose
    public void RiavviaLivelloConResetTotale() // roba pub // riga-ok
    { // apre // riga-ok
        Debug.Log("<color=cyan><b>[RIPROVA LIVELLO]</b> Riavvio del settore in corso con azzeramento progressi e timer...</color>"); // logga // riga-ok

        Time.timeScale = 1f; // setta // riga-ok
        Cursor.lockState = CursorLockMode.None; // setta // riga-ok
        Cursor.visible = true; // setta // riga-ok

        // 1. Reset completo del timer di missione e dell'HUD
        // blocco: controlla se va
        if (CyberHUD.Instance != null) // se ok // riga-ok
        { // apre // riga-ok
            CyberHUD.Instance.ResetCountdown(); // chiama // riga-ok
            CyberHUD.Instance.InizializzaStatoIniziale(); // chiama // riga-ok
        } // chiude // riga-ok

        // 2. Ricaricamento della scena attiva
        Scene activeScene = SceneManager.GetActiveScene(); // setta // riga-ok
        // blocco: controlla se va
        if (activeScene.buildIndex >= 0) // se ok // riga-ok
            SceneManager.LoadScene(activeScene.buildIndex); // chiama // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
            SceneManager.LoadScene(activeScene.name); // chiama // riga-ok

        HideImmediate(); // chiama // riga-ok
        isShowing = false; // setta // riga-ok
        // blocco: controlla se va
        if (deathRoutine != null) // se ok // riga-ok
        { // apre // riga-ok
            StopCoroutine(deathRoutine); // corutina // riga-ok
            deathRoutine = null; // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    /// <summary>
    /// Torna al Menu Principale.
    /// </summary>
    // blocco: funzione fa cose
    public void TornaAlMenuPrincipale() // roba pub // riga-ok
    { // apre // riga-ok
        Time.timeScale = 1f; // setta // riga-ok
        Cursor.lockState = CursorLockMode.None; // setta // riga-ok
        Cursor.visible = true; // setta // riga-ok

        HideImmediate(); // chiama // riga-ok
        isShowing = false; // setta // riga-ok
        // blocco: controlla se va
        if (deathRoutine != null) // se ok // riga-ok
        { // apre // riga-ok
            StopCoroutine(deathRoutine); // corutina // riga-ok
            deathRoutine = null; // setta // riga-ok
        } // chiude // riga-ok

        SceneManager.LoadScene("MainMenu-Scene"); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void EnsureInterface() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (canvasRoot) // se ok // riga-ok
            return; // torna val // riga-ok

        BuildInterface(); // chiama // riga-ok
        HideImmediate(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void BuildInterface() // roba pub // riga-ok
    { // apre // riga-ok
        ClearChildren(); // chiama // riga-ok
        EnsureEventSystem(); // chiama // riga-ok

        Canvas canvas = new GameObject("Death Screen Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>(); // setta // riga-ok
        canvas.transform.SetParent(transform, false); // chiama // riga-ok
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; // setta // riga-ok
        canvas.sortingOrder = sortingOrder; // setta // riga-ok

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>(); // setta // riga-ok
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // setta // riga-ok
        scaler.referenceResolution = referenceResolution; // setta // riga-ok
        scaler.matchWidthOrHeight = 0.5f; // setta // riga-ok

        // 1. Sfondo Oscurante Totale
        GameObject bgObj = new GameObject("Death_Background", typeof(RectTransform), typeof(Image)); // setta // riga-ok
        bgObj.transform.SetParent(canvas.transform, false); // chiama // riga-ok
        Image bgImg = bgObj.GetComponent<Image>(); // setta // riga-ok
        Stretch(bgImg.rectTransform); // chiama // riga-ok
        bgImg.color = new Color(0.02f, 0.03f, 0.05f, 0.94f); // setta // riga-ok
        bgImg.raycastTarget = true; // setta // riga-ok

        // blocco: controlla se va
        if (deathScreenSprite == null) // se ok // riga-ok
        { // apre // riga-ok
#if UNITY_EDITOR // prep ok // riga-ok
            AssignDefaultEditorAssets(); // chiama // riga-ok
#endif // prep ok // riga-ok
        } // chiude // riga-ok

        // 2. Texture Grafica Game Over
        Image image = new GameObject("Death_Static_Image", typeof(RectTransform), typeof(Image)).GetComponent<Image>(); // setta // riga-ok
        image.transform.SetParent(canvas.transform, false); // chiama // riga-ok
        Stretch(image.rectTransform); // chiama // riga-ok
        image.sprite = deathScreenSprite; // setta // riga-ok
        image.color = deathScreenSprite ? WithAlpha(Color.white, imageAlpha) : WithAlpha(fallbackColor, imageAlpha); // setta // riga-ok
        image.preserveAspect = preserveAspect; // setta // riga-ok
        image.raycastTarget = false; // setta // riga-ok

        // 3. Titolo Grande di Fallimento
        GameObject textObj = new GameObject("Death_Title", typeof(RectTransform), typeof(Text)); // setta // riga-ok
        textObj.transform.SetParent(canvas.transform, false); // chiama // riga-ok
        Text title = textObj.GetComponent<Text>(); // setta // riga-ok
        title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf"); // setta // riga-ok
        title.fontSize = 56; // setta // riga-ok
        title.fontStyle = FontStyle.Bold; // setta // riga-ok
        title.alignment = TextAnchor.MiddleCenter; // setta // riga-ok
        title.color = new Color(1f, 0.18f, 0.18f, 1f); // setta // riga-ok
        title.text = "MISSIONE FALLITA"; // setta // riga-ok
        RectTransform tRect = title.rectTransform; // setta // riga-ok
        tRect.anchorMin = new Vector2(0.05f, 0.38f); // setta // riga-ok
        tRect.anchorMax = new Vector2(0.95f, 0.56f); // setta // riga-ok
        tRect.offsetMin = Vector2.zero; // setta // riga-ok
        tRect.offsetMax = Vector2.zero; // setta // riga-ok

        // 4. Testo di Dettaglio / Motivo (es. Tempo Scaduto)
        GameObject subObj = new GameObject("Death_Subtitle", typeof(RectTransform), typeof(Text)); // setta // riga-ok
        subObj.transform.SetParent(canvas.transform, false); // chiama // riga-ok
        subtitleText = subObj.GetComponent<Text>(); // setta // riga-ok
        subtitleText.font = title.font; // setta // riga-ok
        subtitleText.fontSize = 24; // setta // riga-ok
        subtitleText.alignment = TextAnchor.MiddleCenter; // setta // riga-ok
        subtitleText.color = new Color(0.88f, 0.88f, 0.92f, 0.95f); // setta // riga-ok
        subtitleText.text = "Tempo Scaduto // Evacuazione Fallita"; // setta // riga-ok
        RectTransform sRect = subtitleText.rectTransform; // setta // riga-ok
        sRect.anchorMin = new Vector2(0.05f, 0.28f); // setta // riga-ok
        sRect.anchorMax = new Vector2(0.95f, 0.39f); // setta // riga-ok
        sRect.offsetMin = Vector2.zero; // setta // riga-ok
        sRect.offsetMax = Vector2.zero; // setta // riga-ok

        // 5. Pulsante "RIPROVA MISSIONE" (Interattivo, Stile Cyberpunk)
        GameObject btnRetryObj = new GameObject("Button_Riprova", typeof(RectTransform), typeof(Image), typeof(Button)); // setta // riga-ok
        btnRetryObj.transform.SetParent(canvas.transform, false); // chiama // riga-ok
        RectTransform btnRect = btnRetryObj.GetComponent<RectTransform>(); // setta // riga-ok
        btnRect.anchorMin = new Vector2(0.5f, 0.18f); // setta // riga-ok
        btnRect.anchorMax = new Vector2(0.5f, 0.18f); // setta // riga-ok
        btnRect.pivot = new Vector2(0.5f, 0.5f); // setta // riga-ok
        btnRect.sizeDelta = new Vector2(360f, 64f); // setta // riga-ok

        Image btnImg = btnRetryObj.GetComponent<Image>(); // setta // riga-ok
        btnImg.color = new Color(0.08f, 0.55f, 0.38f, 0.95f); // Verde neon / cyan elegante // setta // riga-ok

        Button btn = btnRetryObj.GetComponent<Button>(); // setta // riga-ok
        ColorBlock cb = btn.colors; // setta // riga-ok
        cb.normalColor = new Color(0.08f, 0.55f, 0.38f, 0.95f); // setta // riga-ok
        cb.highlightedColor = new Color(0.14f, 0.85f, 0.58f, 1f); // setta // riga-ok
        cb.pressedColor = new Color(0.05f, 0.38f, 0.26f, 1f); // setta // riga-ok
        cb.selectedColor = cb.highlightedColor; // setta // riga-ok
        btn.colors = cb; // setta // riga-ok
        btn.onClick.AddListener(RiavviaLivelloConResetTotale); // chiama // riga-ok

        // Bordo / Outline Neon Pulsante Riprova
        Outline btnOutline = btnRetryObj.AddComponent<Outline>(); // setta // riga-ok
        btnOutline.effectColor = new Color(0.2f, 1f, 0.65f, 0.9f); // setta // riga-ok
        btnOutline.effectDistance = new Vector2(2f, -2f); // setta // riga-ok

        // Testo del Pulsante Riprova
        GameObject btnTextObj = new GameObject("Button_Text", typeof(RectTransform), typeof(Text)); // setta // riga-ok
        btnTextObj.transform.SetParent(btnRetryObj.transform, false); // chiama // riga-ok
        Text btnText = btnTextObj.GetComponent<Text>(); // setta // riga-ok
        btnText.font = title.font; // setta // riga-ok
        btnText.fontSize = 24; // setta // riga-ok
        btnText.fontStyle = FontStyle.Bold; // setta // riga-ok
        btnText.alignment = TextAnchor.MiddleCenter; // setta // riga-ok
        btnText.color = Color.white; // setta // riga-ok
        btnText.text = "🔄 RIPROVA LIVELLO"; // setta // riga-ok
        Stretch(btnText.rectTransform); // chiama // riga-ok

        // 6. Pulsante secondario "MENU PRINCIPALE"
        GameObject btnMenuObj = new GameObject("Button_Menu", typeof(RectTransform), typeof(Image), typeof(Button)); // setta // riga-ok
        btnMenuObj.transform.SetParent(canvas.transform, false); // chiama // riga-ok
        RectTransform menuRect = btnMenuObj.GetComponent<RectTransform>(); // setta // riga-ok
        menuRect.anchorMin = new Vector2(0.5f, 0.08f); // setta // riga-ok
        menuRect.anchorMax = new Vector2(0.5f, 0.08f); // setta // riga-ok
        menuRect.pivot = new Vector2(0.5f, 0.5f); // setta // riga-ok
        menuRect.sizeDelta = new Vector2(260f, 44f); // setta // riga-ok

        Image menuImg = btnMenuObj.GetComponent<Image>(); // setta // riga-ok
        menuImg.color = new Color(0.22f, 0.25f, 0.30f, 0.90f); // setta // riga-ok

        Button menuBtn = btnMenuObj.GetComponent<Button>(); // setta // riga-ok
        ColorBlock mcb = menuBtn.colors; // setta // riga-ok
        mcb.normalColor = new Color(0.22f, 0.25f, 0.30f, 0.90f); // setta // riga-ok
        mcb.highlightedColor = new Color(0.40f, 0.45f, 0.55f, 1f); // setta // riga-ok
        mcb.pressedColor = new Color(0.14f, 0.16f, 0.20f, 1f); // setta // riga-ok
        menuBtn.colors = mcb; // setta // riga-ok
        menuBtn.onClick.AddListener(TornaAlMenuPrincipale); // chiama // riga-ok

        GameObject menuTextObj = new GameObject("Menu_Text", typeof(RectTransform), typeof(Text)); // setta // riga-ok
        menuTextObj.transform.SetParent(btnMenuObj.transform, false); // chiama // riga-ok
        Text menuText = menuTextObj.GetComponent<Text>(); // setta // riga-ok
        menuText.font = title.font; // setta // riga-ok
        menuText.fontSize = 18; // setta // riga-ok
        menuText.alignment = TextAnchor.MiddleCenter; // setta // riga-ok
        menuText.color = new Color(0.85f, 0.88f, 0.92f, 1f); // setta // riga-ok
        menuText.text = "🏠 MENU PRINCIPALE"; // setta // riga-ok
        Stretch(menuText.rectTransform); // chiama // riga-ok

        CanvasGroup group = canvas.gameObject.AddComponent<CanvasGroup>(); // setta // riga-ok
        group.interactable = true; // setta // riga-ok
        group.blocksRaycasts = true; // setta // riga-ok

        canvasRoot = canvas.gameObject; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void HideImmediate() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (canvasRoot) // se ok // riga-ok
            canvasRoot.SetActive(false); // chiama // riga-ok
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
        subtitleText = null; // setta // riga-ok
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
    private static Color WithAlpha(Color color, float alpha) // roba pub // riga-ok
    { // apre // riga-ok
        color.a = Mathf.Clamp01(alpha); // setta // riga-ok
        return color; // torna val // riga-ok
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
        if (deathScreenSprite) // se ok // riga-ok
            return; // torna val // riga-ok

        deathScreenSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DefaultDeathScreenPath); // setta // riga-ok
        // blocco: controlla se va
        if (!deathScreenSprite) // se ok // riga-ok
            deathScreenSprite = AssetDatabase.LoadAssetAtPath<Sprite>(AlternateDeathScreenPath); // setta // riga-ok
    } // chiude // riga-ok
#endif // prep ok // riga-ok
} // chiude // riga-ok
