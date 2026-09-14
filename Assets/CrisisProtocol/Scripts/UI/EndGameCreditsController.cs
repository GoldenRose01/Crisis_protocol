// ============================================================================
// Crisis Protocol / Sector Containment - Interfaccia utente
// File: .\Assets\CrisisProtocol\Scripts\UI\EndGameCreditsController.cs
// Responsabilita': aggiorna HUD, menu, overlay, gauge, notifiche o schermate di supporto in base agli eventi gameplay.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using System.Collections; // usa lib // riga-ok
using UnityEngine; // usa lib // riga-ok
using UnityEngine.EventSystems; // usa lib // riga-ok
using UnityEngine.SceneManagement; // usa lib // riga-ok
using UnityEngine.UI; // usa lib // riga-ok
using CrisisProtocol.UI; // usa lib // riga-ok

[DisallowMultipleComponent] // nota unity // riga-ok
// blocco: classe x roba grossa
public sealed class EndGameCreditsController : MonoBehaviour // classe qui // riga-ok
{ // apre // riga-ok
    private const string InputSystemUiModuleTypeName = "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem"; // roba pub // riga-ok

    [Header("Colori Neon")] // nota unity // riga-ok
    [SerializeField] private Color neonGreen = new Color(0f, 1f, 0.45f, 1f); // setta // riga-ok
    [SerializeField] private Color neonCyan = new Color(0f, 0.9f, 1f, 1f); // setta // riga-ok
    [SerializeField] private Color neonYellow = new Color(1f, 0.95f, 0.1f, 1f); // setta // riga-ok
    [SerializeField] private Color darkBg = new Color(0.02f, 0.04f, 0.08f, 0.96f); // setta // riga-ok

    [Header("Canvas Settings")] // nota unity // riga-ok
    [SerializeField] private int sortingOrder = 10000; // setta // riga-ok
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f); // setta // riga-ok

    private Canvas canvas; // roba pub // riga-ok
    private CanvasGroup canvasGroup; // roba pub // riga-ok
    private RectTransform creditsContentRect; // roba pub // riga-ok
    private Text scoreValueText; // roba pub // riga-ok
    private Text subtitleBannerText; // roba pub // riga-ok
    private AudioSource audioSource; // roba pub // riga-ok
    private Coroutine creditsScrollRoutine; // roba pub // riga-ok
    private bool isShowing = false; // roba pub // riga-ok

    public static EndGameCreditsController Instance { get; private set; } // roba pub // riga-ok

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // nota unity // riga-ok
    // blocco: funzione fa cose
    private static void EnsureRuntimeController() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (Instance != null || Object.FindFirstObjectByType<EndGameCreditsController>() != null) // se ok // riga-ok
            return; // torna val // riga-ok

        GameObject root = new GameObject("CrisisProtocol_EndGameCreditsController"); // setta // riga-ok
        DontDestroyOnLoad(root); // chiama // riga-ok
        root.AddComponent<EndGameCreditsController>(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void Awake() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (Instance != null && Instance != this) // se ok // riga-ok
        { // apre // riga-ok
            Destroy(gameObject); // elimina // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        Instance = this; // setta // riga-ok
        DontDestroyOnLoad(gameObject); // chiama // riga-ok
        InitAudioSource(); // chiama // riga-ok
        BuildInterface(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void InitAudioSource() // roba pub // riga-ok
    { // apre // riga-ok
        audioSource = GetComponent<AudioSource>(); // setta // riga-ok
        // blocco: controlla se va
        if (audioSource == null) // se ok // riga-ok
        { // apre // riga-ok
            audioSource = gameObject.AddComponent<AudioSource>(); // setta // riga-ok
            audioSource.playOnAwake = false; // setta // riga-ok
            audioSource.spatialBlend = 0f; // setta // riga-ok
            audioSource.volume = 0.9f; // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void Update() // roba pub // riga-ok
    { // apre // riga-ok
        // Tasto di test rapido F8 per visualizzare immediatamente la schermata di fine gioco
        // blocco: controlla se va
        if (Input.GetKeyDown(KeyCode.F8)) // se ok // riga-ok
        { // apre // riga-ok
            Debug.LogWarning("[DEBUG] Tasto F8 premuto: Test Schermata Finale & Titoli di Coda."); // logga // riga-ok
            Show(15000); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    [ContextMenu("TEST: Mostra Titoli di Coda")] // nota unity // riga-ok
    // blocco: funzione fa cose
    public void TestMostraTitoliDiCoda() // roba pub // riga-ok
    { // apre // riga-ok
        Show(15000); // chiama // riga-ok
    } // chiude // riga-ok

    /// <summary>
    /// Mostra la schermata di vittoria e i titoli di coda finali.
    /// </summary>
    // blocco: funzione fa cose
    public static void ShowVictoryAndCredits(int finalScore = 0) // roba pub // riga-ok
    { // apre // riga-ok
        EndGameCreditsController controller = Instance != null ? Instance : Object.FindFirstObjectByType<EndGameCreditsController>(); // setta // riga-ok
        // blocco: controlla se va
        if (controller == null) // se ok // riga-ok
        { // apre // riga-ok
            GameObject root = new GameObject("CrisisProtocol_EndGameCreditsController"); // setta // riga-ok
            DontDestroyOnLoad(root); // chiama // riga-ok
            controller = root.AddComponent<EndGameCreditsController>(); // setta // riga-ok
        } // chiude // riga-ok

        controller.Show(finalScore); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void Show(int finalScore) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (isShowing) return; // se ok // riga-ok
        isShowing = true; // setta // riga-ok

        EnsureInterface(); // chiama // riga-ok

        // blocco: controlla se va
        if (scoreValueText != null) // se ok // riga-ok
        { // apre // riga-ok
            scoreValueText.text = $"PUNTEGGIO TOTALE: {finalScore:N0} PTS  //  SETTORI COMPLETATI: 3 / 3"; // setta // riga-ok
        } // chiude // riga-ok

        ModalUIState.TryOpen("EndGameCredits", true, true); // chiama // riga-ok
        Cursor.lockState = CursorLockMode.None; // setta // riga-ok
        Cursor.visible = true; // setta // riga-ok

        PlayVictoryJingle(); // chiama // riga-ok

        // blocco: controlla se va
        if (canvasGroup != null) // se ok // riga-ok
        { // apre // riga-ok
            canvasGroup.alpha = 0f; // setta // riga-ok
            canvas.gameObject.SetActive(true); // chiama // riga-ok
            // blocco: controlla se va
            if (creditsScrollRoutine != null) // se ok // riga-ok
                StopCoroutine(creditsScrollRoutine); // corutina // riga-ok
            creditsScrollRoutine = StartCoroutine(AnimateCreditsRoutine()); // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void Hide() // roba pub // riga-ok
    { // apre // riga-ok
        isShowing = false; // setta // riga-ok
        // blocco: controlla se va
        if (creditsScrollRoutine != null) // se ok // riga-ok
        { // apre // riga-ok
            StopCoroutine(creditsScrollRoutine); // corutina // riga-ok
            creditsScrollRoutine = null; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (canvas != null) // se ok // riga-ok
            canvas.gameObject.SetActive(false); // chiama // riga-ok

        ModalUIState.Close("EndGameCredits"); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void PlayVictoryJingle() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (audioSource == null) return; // se ok // riga-ok

        // Generazione sintetica di accordo trionfale neon (C Major / F Major Arp)
        int sampleRate = 44100; // setta // riga-ok
        float duration = 3.5f; // setta // riga-ok
        int sampleCount = (int)(sampleRate * duration); // setta // riga-ok
        float[] samples = new float[sampleCount]; // setta // riga-ok

        float[] freqs = new float[] { 261.63f, 329.63f, 392.00f, 523.25f, 659.25f, 783.99f }; // C4, E4, G4, C5, E5, G5 // setta // riga-ok

        // blocco: gira piu volte
        for (int i = 0; i < sampleCount; i++) // ciclo x // riga-ok
        { // apre // riga-ok
            float t = (float)i / sampleRate; // setta // riga-ok
            float envelope = Mathf.Exp(-t * 0.9f) * Mathf.Min(1f, t * 8f); // setta // riga-ok
            float sum = 0f; // setta // riga-ok

            // blocco: gira piu volte
            for (int f = 0; f < freqs.Length; f++) // ciclo x // riga-ok
            { // apre // riga-ok
                float noteTime = Mathf.Max(0f, t - (f * 0.12f)); // setta // riga-ok
                // blocco: controlla se va
                if (t >= f * 0.12f) // se ok // riga-ok
                { // apre // riga-ok
                    float noteEnv = Mathf.Exp(-noteTime * 1.2f); // setta // riga-ok
                    sum += Mathf.Sin(2f * Mathf.PI * freqs[f] * noteTime) * noteEnv * 0.25f; // setta // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok

            samples[i] = Mathf.Clamp(sum * envelope, -1f, 1f); // setta // riga-ok
        } // chiude // riga-ok

        AudioClip clip = AudioClip.Create("Victory_Fanfare", sampleCount, 1, sampleRate, false); // setta // riga-ok
        clip.SetData(samples, 0); // chiama // riga-ok
        audioSource.PlayOneShot(clip, 0.85f); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private IEnumerator AnimateCreditsRoutine() // roba pub // riga-ok
    { // apre // riga-ok
        float fadeTime = 0.8f; // setta // riga-ok
        float elapsed = 0f; // setta // riga-ok

        // blocco: gira piu volte
        while (elapsed < fadeTime) // ciclo x // riga-ok
        { // apre // riga-ok
            elapsed += Time.unscaledDeltaTime; // setta // riga-ok
            // blocco: controlla se va
            if (canvasGroup != null) // se ok // riga-ok
                canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeTime); // setta // riga-ok
            yield return null; // aspetta // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (canvasGroup != null) // se ok // riga-ok
            canvasGroup.alpha = 1f; // setta // riga-ok

        // Scorrimento lento o animazione fluida dei titoli di coda
        // blocco: controlla se va
        if (creditsContentRect != null) // se ok // riga-ok
        { // apre // riga-ok
            Vector2 initialPos = new Vector2(0f, -60f); // setta // riga-ok
            Vector2 targetPos = new Vector2(0f, 40f); // setta // riga-ok
            creditsContentRect.anchoredPosition = initialPos; // setta // riga-ok

            float scrollDuration = 12f; // setta // riga-ok
            float scrollTimer = 0f; // setta // riga-ok

            // blocco: gira piu volte
            while (scrollTimer < scrollDuration) // ciclo x // riga-ok
            { // apre // riga-ok
                scrollTimer += Time.unscaledDeltaTime; // setta // riga-ok
                float progress = Mathf.Clamp01(scrollTimer / scrollDuration); // setta // riga-ok
                creditsContentRect.anchoredPosition = Vector2.Lerp(initialPos, targetPos, Mathf.SmoothStep(0f, 1f, progress)); // setta // riga-ok
                yield return null; // aspetta // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void NuovaPartita() // roba pub // riga-ok
    { // apre // riga-ok
        Hide(); // chiama // riga-ok
        Time.timeScale = 1f; // setta // riga-ok
        Cursor.lockState = CursorLockMode.None; // setta // riga-ok
        Cursor.visible = true; // setta // riga-ok

        // blocco: controlla se va
        if (GameManager.Instance != null) // se ok // riga-ok
        { // apre // riga-ok
            GameManager.Instance.NuovaPartita(); // chiama // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            SceneManager.LoadScene("settore 0"); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void TornaAlMenuPrincipale() // roba pub // riga-ok
    { // apre // riga-ok
        Hide(); // chiama // riga-ok
        Time.timeScale = 1f; // setta // riga-ok
        Cursor.lockState = CursorLockMode.None; // setta // riga-ok
        Cursor.visible = true; // setta // riga-ok

        SceneManager.LoadScene("MainMenu-Scene"); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void EnsureInterface() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (canvas == null) // se ok // riga-ok
        { // apre // riga-ok
            BuildInterface(); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void BuildInterface() // roba pub // riga-ok
    { // apre // riga-ok
        EnsureEventSystem(); // chiama // riga-ok

        GameObject canvasGO = new GameObject("EndGameCredits_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup)); // setta // riga-ok
        canvasGO.transform.SetParent(transform, false); // chiama // riga-ok

        canvas = canvasGO.GetComponent<Canvas>(); // setta // riga-ok
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; // setta // riga-ok
        canvas.sortingOrder = sortingOrder; // setta // riga-ok

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>(); // setta // riga-ok
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // setta // riga-ok
        scaler.referenceResolution = referenceResolution; // setta // riga-ok
        scaler.matchWidthOrHeight = 0.5f; // setta // riga-ok

        canvasGroup = canvasGO.GetComponent<CanvasGroup>(); // setta // riga-ok

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf"); // setta // riga-ok

        // 1. Sfondo Scuro e Vignettatura Cyberpunk
        GameObject bgObj = new GameObject("Background_Overlay", typeof(RectTransform), typeof(Image)); // setta // riga-ok
        bgObj.transform.SetParent(canvasGO.transform, false); // chiama // riga-ok
        Image bgImg = bgObj.GetComponent<Image>(); // setta // riga-ok
        Stretch(bgImg.rectTransform); // chiama // riga-ok
        bgImg.color = darkBg; // setta // riga-ok
        bgImg.raycastTarget = true; // setta // riga-ok

        // 2. Cornice Neon Superiore e Inferiore
        GameObject topBar = new GameObject("Neon_TopBar", typeof(RectTransform), typeof(Image)); // setta // riga-ok
        topBar.transform.SetParent(canvasGO.transform, false); // chiama // riga-ok
        Image topImg = topBar.GetComponent<Image>(); // setta // riga-ok
        topImg.color = neonCyan; // setta // riga-ok
        RectTransform tbRect = topImg.rectTransform; // setta // riga-ok
        tbRect.anchorMin = new Vector2(0f, 1f); // setta // riga-ok
        tbRect.anchorMax = new Vector2(1f, 1f); // setta // riga-ok
        tbRect.pivot = new Vector2(0.5f, 1f); // setta // riga-ok
        tbRect.sizeDelta = new Vector2(0f, 4f); // setta // riga-ok

        GameObject bottomBar = new GameObject("Neon_BottomBar", typeof(RectTransform), typeof(Image)); // setta // riga-ok
        bottomBar.transform.SetParent(canvasGO.transform, false); // chiama // riga-ok
        Image botImg = bottomBar.GetComponent<Image>(); // setta // riga-ok
        botImg.color = neonGreen; // setta // riga-ok
        RectTransform bbRect = botImg.rectTransform; // setta // riga-ok
        bbRect.anchorMin = new Vector2(0f, 0f); // setta // riga-ok
        bbRect.anchorMax = new Vector2(1f, 0f); // setta // riga-ok
        bbRect.pivot = new Vector2(0.5f, 0f); // setta // riga-ok
        bbRect.sizeDelta = new Vector2(0f, 4f); // setta // riga-ok

        // 3. Pannello Centrale Contenitore
        GameObject centerPanel = new GameObject("Center_Panel", typeof(RectTransform), typeof(Image)); // setta // riga-ok
        centerPanel.transform.SetParent(canvasGO.transform, false); // chiama // riga-ok
        RectTransform cpRect = centerPanel.GetComponent<RectTransform>(); // setta // riga-ok
        cpRect.anchorMin = new Vector2(0.5f, 0.5f); // setta // riga-ok
        cpRect.anchorMax = new Vector2(0.5f, 0.5f); // setta // riga-ok
        cpRect.pivot = new Vector2(0.5f, 0.5f); // setta // riga-ok
        cpRect.sizeDelta = new Vector2(1100f, 860f); // setta // riga-ok

        Image cpImg = centerPanel.GetComponent<Image>(); // setta // riga-ok
        cpImg.color = new Color(0.04f, 0.08f, 0.12f, 0.88f); // setta // riga-ok
        Outline cpOutline = centerPanel.AddComponent<Outline>(); // setta // riga-ok
        cpOutline.effectColor = new Color(0f, 1f, 0.6f, 0.6f); // setta // riga-ok
        cpOutline.effectDistance = new Vector2(2f, -2f); // setta // riga-ok

        // 4. Titolo Principale "CRISIS PROTOCOL"
        GameObject titleObj = new GameObject("Game_Title", typeof(RectTransform), typeof(Text)); // setta // riga-ok
        titleObj.transform.SetParent(centerPanel.transform, false); // chiama // riga-ok
        Text titleText = titleObj.GetComponent<Text>(); // setta // riga-ok
        titleText.font = defaultFont; // setta // riga-ok
        titleText.fontSize = 50; // setta // riga-ok
        titleText.fontStyle = FontStyle.Bold; // setta // riga-ok
        titleText.alignment = TextAnchor.MiddleCenter; // setta // riga-ok
        titleText.color = neonCyan; // setta // riga-ok
        titleText.text = "CRISIS PROTOCOL"; // setta // riga-ok

        RectTransform tRect = titleText.rectTransform; // setta // riga-ok
        tRect.anchorMin = new Vector2(0.5f, 1f); // setta // riga-ok
        tRect.anchorMax = new Vector2(0.5f, 1f); // setta // riga-ok
        tRect.pivot = new Vector2(0.5f, 1f); // setta // riga-ok
        tRect.anchoredPosition = new Vector2(0f, -30f); // setta // riga-ok
        tRect.sizeDelta = new Vector2(1000f, 60f); // setta // riga-ok

        Shadow tShadow = titleObj.AddComponent<Shadow>(); // setta // riga-ok
        tShadow.effectColor = new Color(0f, 0.9f, 1f, 0.7f); // setta // riga-ok
        tShadow.effectDistance = new Vector2(3f, -3f); // setta // riga-ok

        // 5. Banner "TUTTO IN SICUREZZA"
        GameObject bannerObj = new GameObject("Banner_TuttoInSicurezza", typeof(RectTransform), typeof(Text)); // setta // riga-ok
        bannerObj.transform.SetParent(centerPanel.transform, false); // chiama // riga-ok
        subtitleBannerText = bannerObj.GetComponent<Text>(); // setta // riga-ok
        subtitleBannerText.font = defaultFont; // setta // riga-ok
        subtitleBannerText.fontSize = 32; // setta // riga-ok
        subtitleBannerText.fontStyle = FontStyle.Bold; // setta // riga-ok
        subtitleBannerText.alignment = TextAnchor.MiddleCenter; // setta // riga-ok
        subtitleBannerText.color = neonGreen; // setta // riga-ok
        subtitleBannerText.text = "★  TUTTO IN SICUREZZA  ★"; // setta // riga-ok

        RectTransform bRect = subtitleBannerText.rectTransform; // setta // riga-ok
        bRect.anchorMin = new Vector2(0.5f, 1f); // setta // riga-ok
        bRect.anchorMax = new Vector2(0.5f, 1f); // setta // riga-ok
        bRect.pivot = new Vector2(0.5f, 1f); // setta // riga-ok
        bRect.anchoredPosition = new Vector2(0f, -95f); // setta // riga-ok
        bRect.sizeDelta = new Vector2(1000f, 45f); // setta // riga-ok

        Shadow bShadow = bannerObj.AddComponent<Shadow>(); // setta // riga-ok
        bShadow.effectColor = new Color(0f, 1f, 0.45f, 0.8f); // setta // riga-ok
        bShadow.effectDistance = new Vector2(2f, -2f); // setta // riga-ok

        // 6. Separatore Orizzontale
        GameObject sepObj = new GameObject("Separator_Line", typeof(RectTransform), typeof(Image)); // setta // riga-ok
        sepObj.transform.SetParent(centerPanel.transform, false); // chiama // riga-ok
        Image sepImg = sepObj.GetComponent<Image>(); // setta // riga-ok
        sepImg.color = new Color(0f, 1f, 0.45f, 0.4f); // setta // riga-ok
        RectTransform sepRect = sepImg.rectTransform; // setta // riga-ok
        sepRect.anchorMin = new Vector2(0.1f, 1f); // setta // riga-ok
        sepRect.anchorMax = new Vector2(0.9f, 1f); // setta // riga-ok
        sepRect.pivot = new Vector2(0.5f, 1f); // setta // riga-ok
        sepRect.anchoredPosition = new Vector2(0f, -145f); // setta // riga-ok
        sepRect.sizeDelta = new Vector2(0f, 2f); // setta // riga-ok

        // 7. Area Titoli di Coda (Credits Viewport & Content)
        GameObject creditsBox = new GameObject("Credits_Box", typeof(RectTransform)); // setta // riga-ok
        creditsBox.transform.SetParent(centerPanel.transform, false); // chiama // riga-ok
        RectTransform cbRect = creditsBox.GetComponent<RectTransform>(); // setta // riga-ok
        cbRect.anchorMin = new Vector2(0.05f, 0.22f); // setta // riga-ok
        cbRect.anchorMax = new Vector2(0.95f, 0.80f); // setta // riga-ok
        cbRect.offsetMin = Vector2.zero; // setta // riga-ok
        cbRect.offsetMax = Vector2.zero; // setta // riga-ok

        GameObject creditsTextObj = new GameObject("Credits_Text", typeof(RectTransform), typeof(Text)); // setta // riga-ok
        creditsTextObj.transform.SetParent(creditsBox.transform, false); // chiama // riga-ok
        creditsContentRect = creditsTextObj.GetComponent<RectTransform>(); // setta // riga-ok
        creditsContentRect.anchorMin = new Vector2(0f, 0f); // setta // riga-ok
        creditsContentRect.anchorMax = new Vector2(1f, 1f); // setta // riga-ok
        creditsContentRect.offsetMin = Vector2.zero; // setta // riga-ok
        creditsContentRect.offsetMax = Vector2.zero; // setta // riga-ok

        Text creditsText = creditsTextObj.GetComponent<Text>(); // setta // riga-ok
        creditsText.font = defaultFont; // setta // riga-ok
        creditsText.fontSize = 21; // setta // riga-ok
        creditsText.alignment = TextAnchor.MiddleCenter; // setta // riga-ok
        creditsText.lineSpacing = 1.35f; // setta // riga-ok
        creditsText.color = new Color(0.92f, 0.95f, 1f, 0.95f); // setta // riga-ok

        creditsText.text = // setta // riga-ok
            "<color=#00E5FF><b>— TITOLI DI CODA —</b></color>\n\n" + // setta // riga-ok
            "<color=#FFE600><b>IDEATORE E PRODUTTORE DEL GIOCO</b></color>\n" + // setta // riga-ok
            "<size=26><b>👑 ALESSIO CASTRONOVO</b></size>\n\n" + // setta // riga-ok
            "<color=#00FF73><b>COLLABORATORE</b></color>\n" + // setta // riga-ok
            "<size=24>Francesco La Rosa</size>\n\n" + // setta // riga-ok
            "<color=#00FF73><b>GAMEPLAY DESIGN E LOGICA</b></color>\n" + // setta // riga-ok
            "Alessio Castronovo\n\n" + // ok qua // riga-ok
            "<color=#00FF73><b>LEVEL ARCHITECTURE & PROTOCOLLI DI CONTENIMENTO</b></color>\n" + // setta // riga-ok
            "Settore 0  •  Settore 1  •  Settore 2 (Completati con successo)\n\n" + // ok qua // riga-ok
            "<color=#00E5FF><b>AUDIO, AMBIENT & SOUND DESIGN</b></color>\n" + // setta // riga-ok
            "Crisis Protocol Audio System\n\n" + // ok qua // riga-ok
            "<color=#00FF73><b>STATO FINALE DELL'IMPIANTO:</b></color>\n" + // setta // riga-ok
            "<b>TUTTI I SISTEMI E I FOCOLAI SONO STATI STABILIZZATI IN SICUREZZA</b>\n\n" + // ok qua // riga-ok
            "<size=24><color=#FFE600><b>GRAZIE PER AVER GIOCATO!</b></color></size>"; // setta // riga-ok

        // 8. Punteggio & Statistiche
        GameObject scoreObj = new GameObject("Score_Summary", typeof(RectTransform), typeof(Text)); // setta // riga-ok
        scoreObj.transform.SetParent(centerPanel.transform, false); // chiama // riga-ok
        scoreValueText = scoreObj.GetComponent<Text>(); // setta // riga-ok
        scoreValueText.font = defaultFont; // setta // riga-ok
        scoreValueText.fontSize = 20; // setta // riga-ok
        scoreValueText.fontStyle = FontStyle.Bold; // setta // riga-ok
        scoreValueText.alignment = TextAnchor.MiddleCenter; // setta // riga-ok
        scoreValueText.color = neonYellow; // setta // riga-ok
        scoreValueText.text = "PUNTEGGIO TOTALE: 0 PTS  //  SETTORI COMPLETATI: 3 / 3"; // setta // riga-ok

        RectTransform scRect = scoreValueText.rectTransform; // setta // riga-ok
        scRect.anchorMin = new Vector2(0.05f, 0.13f); // setta // riga-ok
        scRect.anchorMax = new Vector2(0.95f, 0.20f); // setta // riga-ok
        scRect.offsetMin = Vector2.zero; // setta // riga-ok
        scRect.offsetMax = Vector2.zero; // setta // riga-ok

        // 9. Pulsanti Azione (Nuova Partita / Menu Principale)
        CreateCyberButton(centerPanel.transform, "Btn_NuovaPartita", "🔄 NUOVA PARTITA", new Vector2(-190f, -380f), new Vector2(320f, 54f), neonGreen, NuovaPartita, defaultFont); // chiama // riga-ok
        CreateCyberButton(centerPanel.transform, "Btn_MainMenu", "🏠 MENU PRINCIPALE", new Vector2(190f, -380f), new Vector2(320f, 54f), neonCyan, TornaAlMenuPrincipale, defaultFont); // chiama // riga-ok

        canvas.gameObject.SetActive(false); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void CreateCyberButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size, Color themeColor, UnityEngine.Events.UnityAction onClick, Font font) // roba pub // riga-ok
    { // apre // riga-ok
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); // setta // riga-ok
        btnObj.transform.SetParent(parent, false); // chiama // riga-ok

        RectTransform rect = btnObj.GetComponent<RectTransform>(); // setta // riga-ok
        rect.anchorMin = new Vector2(0.5f, 0.5f); // setta // riga-ok
        rect.anchorMax = new Vector2(0.5f, 0.5f); // setta // riga-ok
        rect.pivot = new Vector2(0.5f, 0.5f); // setta // riga-ok
        rect.anchoredPosition = anchoredPos; // setta // riga-ok
        rect.sizeDelta = size; // setta // riga-ok

        Image img = btnObj.GetComponent<Image>(); // setta // riga-ok
        img.color = new Color(themeColor.r * 0.18f, themeColor.g * 0.18f, themeColor.b * 0.18f, 0.95f); // setta // riga-ok

        Outline outline = btnObj.AddComponent<Outline>(); // setta // riga-ok
        outline.effectColor = themeColor; // setta // riga-ok
        outline.effectDistance = new Vector2(2f, -2f); // setta // riga-ok

        Button btn = btnObj.GetComponent<Button>(); // setta // riga-ok
        ColorBlock cb = btn.colors; // setta // riga-ok
        cb.normalColor = img.color; // setta // riga-ok
        cb.highlightedColor = themeColor; // setta // riga-ok
        cb.pressedColor = new Color(themeColor.r * 0.4f, themeColor.g * 0.4f, themeColor.b * 0.4f, 1f); // setta // riga-ok
        cb.selectedColor = cb.highlightedColor; // setta // riga-ok
        btn.colors = cb; // setta // riga-ok
        btn.onClick.AddListener(onClick); // chiama // riga-ok

        GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(Text)); // setta // riga-ok
        textObj.transform.SetParent(btnObj.transform, false); // chiama // riga-ok
        Text text = textObj.GetComponent<Text>(); // setta // riga-ok
        text.font = font; // setta // riga-ok
        text.fontSize = 20; // setta // riga-ok
        text.fontStyle = FontStyle.Bold; // setta // riga-ok
        text.alignment = TextAnchor.MiddleCenter; // setta // riga-ok
        text.color = Color.white; // setta // riga-ok
        text.text = label; // setta // riga-ok

        RectTransform tRect = text.rectTransform; // setta // riga-ok
        Stretch(tRect); // chiama // riga-ok

        // Effetto hover / click sonoro
        EventTrigger trigger = btnObj.AddComponent<EventTrigger>(); // setta // riga-ok
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter }; // setta // riga-ok
        entry.callback.AddListener((data) => { // setta // riga-ok
            text.color = Color.black; // setta // riga-ok
        }); // chiama // riga-ok
        trigger.triggers.Add(entry); // chiama // riga-ok

        EventTrigger.Entry exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit }; // setta // riga-ok
        exitEntry.callback.AddListener((data) => { // setta // riga-ok
            text.color = Color.white; // setta // riga-ok
        }); // chiama // riga-ok
        trigger.triggers.Add(exitEntry); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private static void Stretch(RectTransform rt) // roba pub // riga-ok
    { // apre // riga-ok
        rt.anchorMin = Vector2.zero; // setta // riga-ok
        rt.anchorMax = Vector2.one; // setta // riga-ok
        rt.offsetMin = Vector2.zero; // setta // riga-ok
        rt.offsetMax = Vector2.zero; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private static void EnsureEventSystem() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (EventSystem.current != null) // se ok // riga-ok
            return; // torna val // riga-ok

        GameObject es = new GameObject("EventSystem", typeof(EventSystem)); // setta // riga-ok

        var inputModuleType = System.Type.GetType(InputSystemUiModuleTypeName); // setta // riga-ok
        // blocco: controlla se va
        if (inputModuleType != null) // se ok // riga-ok
        { // apre // riga-ok
            es.AddComponent(inputModuleType); // chiama // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            es.AddComponent<StandaloneInputModule>(); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
