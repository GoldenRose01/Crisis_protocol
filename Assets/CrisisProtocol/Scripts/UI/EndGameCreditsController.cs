// ============================================================================
// Crisis Protocol / Sector Containment - Interfaccia utente
// File: .\Assets\CrisisProtocol\Scripts\UI\EndGameCreditsController.cs
// Responsabilita': aggiorna HUD, menu, overlay, gauge, notifiche o schermate di supporto in base agli eventi gameplay.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CrisisProtocol.UI;
[DisallowMultipleComponent]
public sealed class EndGameCreditsController : MonoBehaviour
{
    private const string InputSystemUiModuleTypeName = "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem";
    [Header("Colori Neon")]
    [SerializeField] private Color neonGreen = new Color(0f, 1f, 0.45f, 1f);
    [SerializeField] private Color neonCyan = new Color(0f, 0.9f, 1f, 1f);
    [SerializeField] private Color neonYellow = new Color(1f, 0.95f, 0.1f, 1f);
    [SerializeField] private Color darkBg = new Color(0.02f, 0.04f, 0.08f, 0.96f);
    [Header("Canvas Settings")]
    [SerializeField] private int sortingOrder = 10000;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform creditsContentRect;
    private Text scoreValueText;
    private Text subtitleBannerText;
    private AudioSource audioSource;
    private Coroutine creditsScrollRoutine;
    private bool isShowing = false;
    public static EndGameCreditsController Instance { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntimeController()
    {
        if (Instance != null || Object.FindFirstObjectByType<EndGameCreditsController>() != null)
            return;
        GameObject root = new GameObject("CrisisProtocol_EndGameCreditsController");
        DontDestroyOnLoad(root);
        root.AddComponent<EndGameCreditsController>();
    }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitAudioSource();
        BuildInterface();
    }
    private void InitAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = 0.9f;
        }
    }
    private void Update()
    {
        // Tasto di test rapido F8 per visualizzare immediatamente la schermata di fine gioco
        if (Input.GetKeyDown(KeyCode.F8))
        {
            Debug.LogWarning("[DEBUG] Tasto F8 premuto: Test Schermata Finale & Titoli di Coda.");
            Show(15000);
        }
    }
    [ContextMenu("TEST: Mostra Titoli di Coda")]
    public void TestMostraTitoliDiCoda()
    {
        Show(15000);
    }
    /// <summary>
    /// Mostra la schermata di vittoria e i titoli di coda finali.
    /// </summary>
    public static void ShowVictoryAndCredits(int finalScore = 0)
    {
        EndGameCreditsController controller = Instance != null ? Instance : Object.FindFirstObjectByType<EndGameCreditsController>();
        if (controller == null)
        {
            GameObject root = new GameObject("CrisisProtocol_EndGameCreditsController");
            DontDestroyOnLoad(root);
            controller = root.AddComponent<EndGameCreditsController>();
        }
        controller.Show(finalScore);
    }
    public void Show(int finalScore)
    {
        if (isShowing) return;
        isShowing = true;
        EnsureInterface();
        if (scoreValueText != null)
        {
            scoreValueText.text = $"SETTORI COMPLETATI: 3 / 3";
        }
        ModalUIState.TryOpen("EndGameCredits", true, true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        PlayVictoryJingle();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvas.gameObject.SetActive(true);
            if (creditsScrollRoutine != null)
                StopCoroutine(creditsScrollRoutine);
            creditsScrollRoutine = StartCoroutine(AnimateCreditsRoutine());
        }
    }
    public void Hide()
    {
        isShowing = false;
        if (creditsScrollRoutine != null)
        {
            StopCoroutine(creditsScrollRoutine);
            creditsScrollRoutine = null;
        }
        if (canvas != null)
            canvas.gameObject.SetActive(false);
        ModalUIState.Close("EndGameCredits");
    }
    private void PlayVictoryJingle()
    {
        if (audioSource == null) return;
        // Generazione sintetica di accordo trionfale neon (C Major / F Major Arp)
        int sampleRate = 44100;
        float duration = 3.5f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];
        float[] freqs = new float[] { 261.63f, 329.63f, 392.00f, 523.25f, 659.25f, 783.99f }; // C4, E4, G4, C5, E5, G5
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-t * 0.9f) * Mathf.Min(1f, t * 8f);
            float sum = 0f;
            for (int f = 0; f < freqs.Length; f++)
            {
                float noteTime = Mathf.Max(0f, t - (f * 0.12f));
                if (t >= f * 0.12f)
                {
                    float noteEnv = Mathf.Exp(-noteTime * 1.2f);
                    sum += Mathf.Sin(2f * Mathf.PI * freqs[f] * noteTime) * noteEnv * 0.25f;
                }
            }
            samples[i] = Mathf.Clamp(sum * envelope, -1f, 1f);
        }
        AudioClip clip = AudioClip.Create("Victory_Fanfare", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        audioSource.PlayOneShot(clip, 0.85f);
    }
    private IEnumerator AnimateCreditsRoutine()
    {
        float fadeTime = 0.8f;
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeTime);
            yield return null;
        }
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
        // Scorrimento lento o animazione fluida dei titoli di coda
        if (creditsContentRect != null)
        {
            Vector2 initialPos = new Vector2(0f, -60f);
            Vector2 targetPos = new Vector2(0f, 40f);
            creditsContentRect.anchoredPosition = initialPos;
            float scrollDuration = 12f;
            float scrollTimer = 0f;
            while (scrollTimer < scrollDuration)
            {
                scrollTimer += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(scrollTimer / scrollDuration);
                creditsContentRect.anchoredPosition = Vector2.Lerp(initialPos, targetPos, Mathf.SmoothStep(0f, 1f, progress));
                yield return null;
            }
        }
    }
    public void NuovaPartita()
    {
        Hide();
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.NuovaPartita();
        }
        else
        {
            SceneManager.LoadScene("settore 0");
        }
    }
    public void TornaAlMenuPrincipale()
    {
        Hide();
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("MainMenu-Scene");
    }
    private void EnsureInterface()
    {
        if (canvas == null)
        {
            BuildInterface();
        }
    }
    private void BuildInterface()
    {
        EnsureEventSystem();
        GameObject canvasGO = new GameObject("EndGameCredits_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvasGO.transform.SetParent(transform, false);
        canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGroup = canvasGO.GetComponent<CanvasGroup>();
        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        // 1. Sfondo Scuro e Vignettatura Cyberpunk
        GameObject bgObj = new GameObject("Background_Overlay", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(canvasGO.transform, false);
        Image bgImg = bgObj.GetComponent<Image>();
        Stretch(bgImg.rectTransform);
        bgImg.color = darkBg;
        bgImg.raycastTarget = true;
        // 2. Cornice Neon Superiore e Inferiore
        GameObject topBar = new GameObject("Neon_TopBar", typeof(RectTransform), typeof(Image));
        topBar.transform.SetParent(canvasGO.transform, false);
        Image topImg = topBar.GetComponent<Image>();
        topImg.color = neonCyan;
        RectTransform tbRect = topImg.rectTransform;
        tbRect.anchorMin = new Vector2(0f, 1f);
        tbRect.anchorMax = new Vector2(1f, 1f);
        tbRect.pivot = new Vector2(0.5f, 1f);
        tbRect.sizeDelta = new Vector2(0f, 4f);
        GameObject bottomBar = new GameObject("Neon_BottomBar", typeof(RectTransform), typeof(Image));
        bottomBar.transform.SetParent(canvasGO.transform, false);
        Image botImg = bottomBar.GetComponent<Image>();
        botImg.color = neonGreen;
        RectTransform bbRect = botImg.rectTransform;
        bbRect.anchorMin = new Vector2(0f, 0f);
        bbRect.anchorMax = new Vector2(1f, 0f);
        bbRect.pivot = new Vector2(0.5f, 0f);
        bbRect.sizeDelta = new Vector2(0f, 4f);
        // 3. Pannello Centrale Contenitore
        GameObject centerPanel = new GameObject("Center_Panel", typeof(RectTransform), typeof(Image));
        centerPanel.transform.SetParent(canvasGO.transform, false);
        RectTransform cpRect = centerPanel.GetComponent<RectTransform>();
        cpRect.anchorMin = new Vector2(0.5f, 0.5f);
        cpRect.anchorMax = new Vector2(0.5f, 0.5f);
        cpRect.pivot = new Vector2(0.5f, 0.5f);
        cpRect.sizeDelta = new Vector2(1100f, 860f);
        Image cpImg = centerPanel.GetComponent<Image>();
        cpImg.color = new Color(0.04f, 0.08f, 0.12f, 0.88f);
        Outline cpOutline = centerPanel.AddComponent<Outline>();
        cpOutline.effectColor = new Color(0f, 1f, 0.6f, 0.6f);
        cpOutline.effectDistance = new Vector2(2f, -2f);
        // 4. Titolo Principale "CRISIS PROTOCOL"
        GameObject titleObj = new GameObject("Game_Title", typeof(RectTransform), typeof(Text));
        titleObj.transform.SetParent(centerPanel.transform, false);
        Text titleText = titleObj.GetComponent<Text>();
        titleText.font = defaultFont;
        titleText.fontSize = 50;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = neonCyan;
        titleText.text = "CRISIS PROTOCOL";
        RectTransform tRect = titleText.rectTransform;
        tRect.anchorMin = new Vector2(0.5f, 1f);
        tRect.anchorMax = new Vector2(0.5f, 1f);
        tRect.pivot = new Vector2(0.5f, 1f);
        tRect.anchoredPosition = new Vector2(0f, -30f);
        tRect.sizeDelta = new Vector2(1000f, 60f);
        Shadow tShadow = titleObj.AddComponent<Shadow>();
        tShadow.effectColor = new Color(0f, 0.9f, 1f, 0.7f);
        tShadow.effectDistance = new Vector2(3f, -3f);
        // 5. Banner "TUTTO IN SICUREZZA"
        GameObject bannerObj = new GameObject("Banner_TuttoInSicurezza", typeof(RectTransform), typeof(Text));
        bannerObj.transform.SetParent(centerPanel.transform, false);
        subtitleBannerText = bannerObj.GetComponent<Text>();
        subtitleBannerText.font = defaultFont;
        subtitleBannerText.fontSize = 32;
        subtitleBannerText.fontStyle = FontStyle.Bold;
        subtitleBannerText.alignment = TextAnchor.MiddleCenter;
        subtitleBannerText.color = neonGreen;
        subtitleBannerText.text = "★  TUTTO IN SICUREZZA  ★";
        RectTransform bRect = subtitleBannerText.rectTransform;
        bRect.anchorMin = new Vector2(0.5f, 1f);
        bRect.anchorMax = new Vector2(0.5f, 1f);
        bRect.pivot = new Vector2(0.5f, 1f);
        bRect.anchoredPosition = new Vector2(0f, -95f);
        bRect.sizeDelta = new Vector2(1000f, 45f);
        Shadow bShadow = bannerObj.AddComponent<Shadow>();
        bShadow.effectColor = new Color(0f, 1f, 0.45f, 0.8f);
        bShadow.effectDistance = new Vector2(2f, -2f);
        // 6. Separatore Orizzontale
        GameObject sepObj = new GameObject("Separator_Line", typeof(RectTransform), typeof(Image));
        sepObj.transform.SetParent(centerPanel.transform, false);
        Image sepImg = sepObj.GetComponent<Image>();
        sepImg.color = new Color(0f, 1f, 0.45f, 0.4f);
        RectTransform sepRect = sepImg.rectTransform;
        sepRect.anchorMin = new Vector2(0.1f, 1f);
        sepRect.anchorMax = new Vector2(0.9f, 1f);
        sepRect.pivot = new Vector2(0.5f, 1f);
        sepRect.anchoredPosition = new Vector2(0f, -145f);
        sepRect.sizeDelta = new Vector2(0f, 2f);
        // 7. Area Titoli di Coda (Credits Viewport & Content)
        GameObject creditsBox = new GameObject("Credits_Box", typeof(RectTransform));
        creditsBox.transform.SetParent(centerPanel.transform, false);
        RectTransform cbRect = creditsBox.GetComponent<RectTransform>();
        cbRect.anchorMin = new Vector2(0.05f, 0.22f);
        cbRect.anchorMax = new Vector2(0.95f, 0.80f);
        cbRect.offsetMin = Vector2.zero;
        cbRect.offsetMax = Vector2.zero;
        GameObject creditsTextObj = new GameObject("Credits_Text", typeof(RectTransform), typeof(Text));
        creditsTextObj.transform.SetParent(creditsBox.transform, false);
        creditsContentRect = creditsTextObj.GetComponent<RectTransform>();
        creditsContentRect.anchorMin = new Vector2(0f, 0f);
        creditsContentRect.anchorMax = new Vector2(1f, 1f);
        creditsContentRect.offsetMin = Vector2.zero;
        creditsContentRect.offsetMax = Vector2.zero;
        Text creditsText = creditsTextObj.GetComponent<Text>();
        creditsText.font = defaultFont;
        creditsText.fontSize = 21;
        creditsText.alignment = TextAnchor.MiddleCenter;
        creditsText.lineSpacing = 1.35f;
        creditsText.color = new Color(0.92f, 0.95f, 1f, 0.95f);
        creditsText.text =
            "<color=#00E5FF><b>— TITOLI DI CODA —</b></color>\n\n" +
            "<color=#FFE600><b>IDEATORE E PRODUTTORE DEL GIOCO</b></color>\n" +
            "<size=26><b>👑 ALESSIO CASTRONOVO</b></size>\n\n" +
            "<color=#00FF73><b>COLLABORATORE</b></color>\n" +
            "<size=24>Francesco La Rosa</size>\n\n" +
            "<color=#00FF73><b>GAMEPLAY DESIGN E LOGICA</b></color>\n" +
            "Alessio Castronovo\n\n" +
            "<color=#00FF73><b>LEVEL ARCHITECTURE & PROTOCOLLI DI CONTENIMENTO</b></color>\n" +
            "Settore 0  •  Settore 1  •  Settore 2 (Completati con successo)\n\n" +
            "<color=#00E5FF><b>AUDIO, AMBIENT & SOUND DESIGN</b></color>\n" +
            "Crisis Protocol Audio System\n\n" +
            "<color=#00FF73><b>STATO FINALE DELL'IMPIANTO:</b></color>\n" +
            "<b>TUTTI I SISTEMI E I FOCOLAI SONO STATI STABILIZZATI IN SICUREZZA</b>\n\n" +
            "<size=24><color=#FFE600><b>GRAZIE PER AVER GIOCATO!</b></color></size>";
        // 8. Punteggio & Statistiche
        GameObject scoreObj = new GameObject("Score_Summary", typeof(RectTransform), typeof(Text));
        scoreObj.transform.SetParent(centerPanel.transform, false);
        scoreValueText = scoreObj.GetComponent<Text>();
        scoreValueText.font = defaultFont;
        scoreValueText.fontSize = 20;
        scoreValueText.fontStyle = FontStyle.Bold;
        scoreValueText.alignment = TextAnchor.MiddleCenter;
        scoreValueText.color = neonYellow;
        scoreValueText.text = "PUNTEGGIO TOTALE: 0 PTS  //  SETTORI COMPLETATI: 3 / 3";
        RectTransform scRect = scoreValueText.rectTransform;
        scRect.anchorMin = new Vector2(0.05f, 0.13f);
        scRect.anchorMax = new Vector2(0.95f, 0.20f);
        scRect.offsetMin = Vector2.zero;
        scRect.offsetMax = Vector2.zero;
        // 9. Pulsanti Azione (Nuova Partita / Menu Principale)
        CreateCyberButton(centerPanel.transform, "Btn_NuovaPartita", "🔄 NUOVA PARTITA", new Vector2(-190f, -380f), new Vector2(320f, 54f), neonGreen, NuovaPartita, defaultFont);
        CreateCyberButton(centerPanel.transform, "Btn_MainMenu", "🏠 MENU PRINCIPALE", new Vector2(190f, -380f), new Vector2(320f, 54f), neonCyan, TornaAlMenuPrincipale, defaultFont);
        canvas.gameObject.SetActive(false);
    }
    private void CreateCyberButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size, Color themeColor, UnityEngine.Events.UnityAction onClick, Font font)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        Image img = btnObj.GetComponent<Image>();
        img.color = new Color(themeColor.r * 0.18f, themeColor.g * 0.18f, themeColor.b * 0.18f, 0.95f);
        Outline outline = btnObj.AddComponent<Outline>();
        outline.effectColor = themeColor;
        outline.effectDistance = new Vector2(2f, -2f);
        Button btn = btnObj.GetComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = img.color;
        cb.highlightedColor = themeColor;
        cb.pressedColor = new Color(themeColor.r * 0.4f, themeColor.g * 0.4f, themeColor.b * 0.4f, 1f);
        cb.selectedColor = cb.highlightedColor;
        btn.colors = cb;
        btn.onClick.AddListener(onClick);
        GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
        textObj.transform.SetParent(btnObj.transform, false);
        Text text = textObj.GetComponent<Text>();
        text.font = font;
        text.fontSize = 20;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = label;
        RectTransform tRect = text.rectTransform;
        Stretch(tRect);
        // Effetto hover / click sonoro
        EventTrigger trigger = btnObj.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entry.callback.AddListener((data) => {
            text.color = Color.black;
        });
        trigger.triggers.Add(entry);
        EventTrigger.Entry exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener((data) => {
            text.color = Color.white;
        });
        trigger.triggers.Add(exitEntry);
    }
    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;
        GameObject es = new GameObject("EventSystem", typeof(EventSystem));
        var inputModuleType = System.Type.GetType(InputSystemUiModuleTypeName);
        if (inputModuleType != null)
        {
            es.AddComponent(inputModuleType);
        }
        else
        {
            es.AddComponent<StandaloneInputModule>();
        }
    }
}