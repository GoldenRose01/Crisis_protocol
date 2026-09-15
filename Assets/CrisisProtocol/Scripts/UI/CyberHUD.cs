// ============================================================================
// Crisis Protocol / Sector Containment - Interfaccia utente
// File: .\Assets\CrisisProtocol\Scripts\UI\CyberHUD.cs
// Responsabilita': aggiorna HUD, menu, overlay, gauge, notifiche o schermate di supporto in base agli eventi gameplay.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using System.Collections;
using System.Collections.Generic;
using CrisisProtocol.UI;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// HUD Cybernetico avanzato in stile Visore Robot:
/// 1. Barra della Vita LCD Verde a celle (Stato di Carica Batteria).
/// 2. Mirino centrale da visore robotico con Lock-On dinamico.
/// 3. Prompt di Prossimità Trasparente con bordi Verde Neon (Nome oggetto + Cosa sistemare/Azione).
/// 4. Notifiche Olografiche di Acquisizione Keycard a schermo.
/// Si avvia e si mostra IMMEDIATAMENTE all'inizio di ogni scena.
/// </summary>
public class CyberHUD : MonoBehaviour
{
    private static CyberHUD instance;

    // Accesso globale controllato: HUD e player possono chiamarlo senza avere
    // riferimenti in scena. Se manca, lo crea, utile nei test avviati da scene singole.
    public static CyberHUD Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Object.FindAnyObjectByType<CyberHUD>();
                if (instance == null)
                {
                    GameObject go = new GameObject("CyberHUD_System");
                    instance = go.AddComponent<CyberHUD>();
                }
            }
            return instance;
        }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoAvviaHUDSuCaricamentoScena()
    {
        // Unity chiama questo dopo ogni load: ci garantisce che HUD e stato scena
        // tornino coerenti anche quando si salta direttamente da menu a settore.
        if (Instance != null)
        {
            Instance.InizializzaStatoIniziale();
        }
    }
    [Header("Colori Palette Cyber/Neon")]
    public Color neonGreen = new Color(0.15f, 1f, 0.25f, 1f);
    public Color neonGreenDim = new Color(0.1f, 0.5f, 0.15f, 0.35f);
    public Color darkGlass = new Color(0.02f, 0.06f, 0.04f, 0.85f);
    public Color warningRed = new Color(1f, 0.2f, 0.1f, 1f);
    public Color textCyan = new Color(0.4f, 0.95f, 1f, 1f);
    // Oggetti principali della UI generata runtime. Il progetto non dipende da
    // prefab HUD obbligatori: lo script costruisce tutto e poi aggiorna i pezzi.
    private Canvas hudCanvas;
    private CanvasScaler hudScaler;

    // Barra vita stile batteria: segmenti separati = feedback più leggibile
    // rispetto a una barra continua quando il player prende danni.
    private RectTransform batteryContainer;
    private Image[] batterySegments;
    private Text testoPercentualeHP;
    private Text testoDettaglioHP;
    private const int NUM_SEGMENTI = 10;
    private float hpCorrenti = 100f;
    private float hpMassimi = 100f;
    // Mirino centrale: comunica target lock/interazione senza usare cursore mouse.
    private RectTransform visorReticleContainer;
    private RectTransform reticleRing;
    private Image reticleCenterDot;
    private Image[] reticleBrackets;
    private Text reticleStatusText;
    private bool isTargetLocked = false;
    private float reticleRotationSpeed = 25f;
    // Prompt di prossimità: viene pilotato da PlayerInteract e resta separato
    // dalle modali, così non entra in conflitto con datapad o terminali.
    private RectTransform promptPanel;
    private CanvasGroup promptCanvasGroup;
    private Text promptTitleText;
    private Text promptActionText;
    private Image promptKeyBadge;
    private Text promptKeyText;
    // Banner brevi: danno feedback immediato senza fermare il player.
    private RectTransform notificaPanel;
    private CanvasGroup notificaCanvasGroup;
    private Text notificaTitleText;
    private Text notificaSubText;
    private Coroutine notificaCoroutine;
    // 4b. Banner Hint Credenziali (icona terminale lampeggiante)
    private RectTransform hintCredenzialiPanel;
    private CanvasGroup hintCredenzialiGroup;
    private Text hintCredenzialiTesto;
    private Image hintIconaTerminale;
    private Coroutine hintCredenzialiCoroutine;
    // 4c. Banner Hint Tutorial Dinamico (lampeggiante, in alto/centro)
    private RectTransform hintTutorialPanel;
    private CanvasGroup hintTutorialGroup;
    private Text hintTutorialTesto;
    private Image hintIconaTutorial;
    private Coroutine hintTutorialCoroutine;
    // Flash danni: overlay temporaneo, niente stato persistente.
    private CanvasGroup damageFlashGroup;
    private Coroutine damageFlashCoroutine;
    // Timer missione: può ereditare dal MissionManager o usare valori locali.
    // Questo è comodo in prototipo, dove ogni scena può avere tuning diverso.
    [Header("Configurazione Countdown (Inspector)")]
    [Tooltip("Durata del timer di missione in MINUTI (regolabile da qui: es. 5 = 5:00, 3 = 3:00, 10 = 10:00).")]
    [SerializeField] [Range(0.5f, 60f)] private float durataInMinuti = 5f;
    [Tooltip("Secondi totali calcolati per il countdown.")]
    public float durataCountdownIniziale = 300f;
    [Tooltip("Abilita o disabilita il conteggio all'indietro del timer.")]
    [SerializeField] private bool timerAttivo = true;
    [Tooltip("Se true, provoca la sconfitta immediata allo scadere del timer (00:00.0).")]
    [SerializeField] private bool sconfittaATempoScaduto = true;
    [Tooltip("Se true, usa le impostazioni specificate qui su CyberHUD invece di ereditare quelle di MissionManager.")]
    [SerializeField] private bool forzaImpostazioniLocaliHUD = false;
    private RectTransform timerContainer;
    private Text testoTimerValore;
    private Text testoTimerStatus;
    private float tempoRimanenteCountdown = 300f;
    // Tutorial HUD: F1 è la via principale perché in gioco il cursore può essere
    // bloccato. Il bottone resta come affordance visiva quando la UI è cliccabile.
    private const string TutorialModalOwner = "HudTutorial";
    private RectTransform tutorialButtonContainer;
    private RectTransform tutorialPanel;
    private CanvasGroup tutorialPanelGroup;
    private bool tutorialAperto = false;
    [SerializeField] private KeyCode tastoTutorial = KeyCode.F1;
    public float TempoRimanente => tempoRimanenteCountdown;
    public void ImpostaCountdown(float secondi) { durataCountdownIniziale = secondi; durataInMinuti = secondi / 60f; tempoRimanenteCountdown = secondi; }
    public void ResetCountdown() => tempoRimanenteCountdown = durataCountdownIniziale;
    public void SetTimerAttivo(bool attivo) => timerAttivo = attivo;
    private void OnValidate()
    {
        if (durataInMinuti > 0f)
        {
            durataCountdownIniziale = durataInMinuti * 60f;
        }
    }
    private void Awake()
    {
        // HUD persistente: evita ricostruzioni complete tra settore 0/1/2 e
        // mantiene coerenti notifiche, timer e riferimenti base.
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        CostruisciHUDCompleto();
        InizializzaStatoIniziale();
    }
    private void OnEnable()
    {
        SalutePlayer.OnSaluteCambiata += OnSaluteAggiornata;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        InizializzaStatoIniziale();
    }
    private void OnDisable()
    {
        SalutePlayer.OnSaluteCambiata -= OnSaluteAggiornata;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        InizializzaStatoIniziale();
    }
    private void Start()
    {
        InizializzaStatoIniziale();
    }
    public void InizializzaStatoIniziale()
    {
        // Il main menu non deve mostrare HUD gameplay. Ogni rientro in scena passa
        // da qui, quindi è il punto unico per accendere/spegnere il cockpit.
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (currentScene == "MainMenu-Scene")
        {
            if (hudCanvas != null) hudCanvas.enabled = false;
            return;
        }
        else
        {
            if (hudCanvas != null) hudCanvas.enabled = true;
        }
        if (MissionManager.Instance != null && !forzaImpostazioniLocaliHUD)
        {
            timerAttivo = MissionManager.Instance.UsaTempoLimite;
            durataInMinuti = MissionManager.Instance.TempoLimiteMinuti;
            durataCountdownIniziale = MissionManager.Instance.TempoLimiteSecondi;
            sconfittaATempoScaduto = MissionManager.Instance.SconfittaAScadenzaTimer;
        }
        else
        {
            if (durataInMinuti > 0f)
            {
                durataCountdownIniziale = durataInMinuti * 60f;
            }
        }
        tempoRimanenteCountdown = durataCountdownIniziale;
        if (timerContainer != null) timerContainer.gameObject.SetActive(timerAttivo);
        SalutePlayer p = Object.FindAnyObjectByType<SalutePlayer>();
        if (p != null)
        {
            OnSaluteAggiornata(p.SaluteAttuale > 0 ? p.SaluteAttuale : p.puntiVitaMassimi, p.puntiVitaMassimi);
        }
        else
        {
            OnSaluteAggiornata(100f, 100f);
        }
        SetTargetLocked(false);
        // LOGICA TUTORIAL DINAMICO SU CARICAMENTO SETTORE
        string sceneNameLow = currentScene.ToLower();
        if (sceneNameLow.Contains("settore 0"))
        {
            MostraHintTutorial("TUTORIAL: Usa [W][A][S][D] per muoverti, il Mouse per la visuale, [E] per interagire,[Q] per scansionare l'ambiente , [ESC] per pausa/mappa.");
        }
        else if (sceneNameLow.Contains("settore 1"))
        {
            MostraHintTutorial("TUTORIAL: Cerca le credenziali per la porta bloccata. Segui la luce blu lampeggiante per trovare la keycard!");
        }
        else
        {
            NascondiHintTutorial();
        }
    }
    private void Update()
    {
        GestisciInputTutorial();
        // Rotazione continua dell'anello del visore robotico
        if (reticleRing != null)
        {
            float speed = isTargetLocked ? 100f : reticleRotationSpeed;
            reticleRing.Rotate(Vector3.forward, -speed * Time.unscaledDeltaTime);
        }
        // Effetto pulsazione mirino quando lockato
        if (visorReticleContainer != null && isTargetLocked)
        {
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 8f) * 0.05f;
            visorReticleContainer.localScale = new Vector3(pulse, pulse, 1f);
        }
        // Aggiornamento Countdown a schermo LCD (Conteggio all'indietro)
        if (timerAttivo && testoTimerValore != null)
        {
            if (MissionManager.Instance != null && (MissionManager.Instance.MissioneTerminata || MissionManager.Instance.EstrazioneSbloccata))
            {
                if (MissionManager.Instance.EstrazioneSbloccata && timerContainer != null && timerContainer.gameObject.activeSelf)
                    timerContainer.gameObject.SetActive(false);
                return;
            }
            tempoRimanenteCountdown = Mathf.Max(0f, tempoRimanenteCountdown - Time.deltaTime);
            int minuti = (int)(tempoRimanenteCountdown / 60f);
            int secondi = (int)(tempoRimanenteCountdown % 60f);
            int decimi = (int)((tempoRimanenteCountdown * 10f) % 10f);
            testoTimerValore.text = $"{minuti:D2}:{secondi:D2}.{decimi:D1}";
            // Integrazione dinamica con lo stato di emergenza e countdown
            if (tempoRimanenteCountdown <= 0f)
            {
                testoTimerValore.text = "00:00.0";
                testoTimerValore.color = warningRed;
                if (testoTimerStatus != null)
                {
                    testoTimerStatus.text = "⚠️ TIME EXPIRED // CRITICAL DEFEAT";
                    testoTimerStatus.color = warningRed;
                }
                if (sconfittaATempoScaduto)
                {
                    if (MissionManager.Instance != null && !MissionManager.Instance.MissioneTerminata)
                    {
                        MissionManager.Instance.TerminaPerTempoScaduto();
                    }
                    else
                    {
                        SalutePlayer player = Object.FindAnyObjectByType<SalutePlayer>();
                        if (player != null && player.SaluteAttuale > 0)
                        {
                            player.SubisciDanno(99999f);
                        }
                        DeathScreenController.ShowAndReloadCurrentScene(3.0f, 0f, "TEMPO SCADUTO // EVACUAZIONE FALLITA");
                    }
                }
            }
            else if (tempoRimanenteCountdown <= 60f)
            {
                // Ultimo minuto: allarme rosso lampeggiante
                float blink = Mathf.Sin(Time.unscaledTime * 10f);
                testoTimerValore.color = blink > 0f ? warningRed : new Color(1f, 0.6f, 0.6f, 1f);
                if (testoTimerStatus != null)
                {
                    testoTimerStatus.text = "⚠️ T-MINUS CRITICAL // EVACUATE";
                    testoTimerStatus.color = warningRed;
                }
            }
            else if (MissionManager.Instance != null && testoTimerStatus != null)
            {
                float collasso = MissionManager.Instance.CollassoCorrente;
                if (collasso > 75f)
                {
                    float blink = Mathf.Sin(Time.unscaledTime * 8f);
                    testoTimerValore.color = blink > 0f ? warningRed : new Color(1f, 0.75f, 0.2f, 1f);
                    testoTimerStatus.text = $"⚠️ COLLAPSE: {Mathf.CeilToInt(collasso)}% [CRITICAL]";
                    testoTimerStatus.color = warningRed;
                }
                else if (collasso > 40f)
                {
                    testoTimerValore.color = new Color(1f, 0.75f, 0.2f, 1f);
                    testoTimerStatus.text = $"SYS_ALERT: COLLAPSE {Mathf.CeilToInt(collasso)}%";
                    testoTimerStatus.color = new Color(1f, 0.75f, 0.2f, 1f);
                }
                else
                {
                    testoTimerValore.color = neonGreen;
                    testoTimerStatus.text = "SYS_REC // SEC_02 [COUNTDOWN]";
                    testoTimerStatus.color = textCyan;
                }
            }
            else
            {
                testoTimerValore.color = neonGreen;
                if (testoTimerStatus != null)
                {
                    testoTimerStatus.text = "SYS_REC // SEC_02 [COUNTDOWN]";
                    testoTimerStatus.color = textCyan;
                }
            }
        }
    }
    // ─────────────────────────────────────────────────────────────────────────
    // COSTRUZIONE GRAFICA HUD PROCEDURALE (Crisp High-DPI UI)
    // ─────────────────────────────────────────────────────────────────────────
    private void CostruisciHUDCompleto()
    {
        // 1. Canvas Setup
        hudCanvas = gameObject.GetComponent<Canvas>();
        if (hudCanvas == null) hudCanvas = gameObject.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvas.sortingOrder = 99;
        hudScaler = gameObject.GetComponent<CanvasScaler>();
        if (hudScaler == null) hudScaler = gameObject.AddComponent<CanvasScaler>();
        hudScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        hudScaler.referenceResolution = new Vector2(1920, 1080);
        hudScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        hudScaler.matchWidthOrHeight = 1.0f; // Fissa l'altezza per evitare che l'interfaccia esca dallo schermo in finestre larghe o Free Aspect
        hudScaler.dynamicPixelsPerUnit = 3.0f;
        if (gameObject.GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();
        Font defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 16);
        Sprite solidSprite = CreaSpriteSolido();
        Sprite borderSprite = CreaSpriteCorniceTech();
        // 2. Barra Vita Batteria LCD (Posizionata con margine di sicurezza 50px da sinistra e dal basso)
        CostruisciBarraVitaLCD(solidSprite, borderSprite, defaultFont);
        // 3. Mirino Visore Robot (Al centro dello schermo)
        CostruisciMirinoVisore(solidSprite, defaultFont);
        // 4. Prompt di Prossimità [E] (In basso al centro, subito sotto il mirino)
        CostruisciPromptProssimita(solidSprite, borderSprite, defaultFont);
        // 5. Banner Notifica Acquisizione Keycard (In alto al centro)
        CostruisciBannerNotifica(solidSprite, borderSprite, defaultFont);
        // 6. Cyber Mission Timer Panel (In alto a destra, coerente con lo stile neon del visore)
        CostruisciTimerVisore(solidSprite, borderSprite, defaultFont);
        // 7. Flash Visivo Impatto Danno Schermo
        CostruisciDamageFlash(solidSprite);
        // 8. Bottone ingranaggio + tutorial testuale del gioco
        CostruisciTutorialHUD(solidSprite, borderSprite, defaultFont);
    }
    private void CostruisciTutorialHUD(Sprite solid, Sprite border, Font font)
    {
        GameObject buttonGO = new GameObject("HUD_Tutorial_GearButton");
        buttonGO.transform.SetParent(transform, false);
        tutorialButtonContainer = buttonGO.AddComponent<RectTransform>();
        tutorialButtonContainer.anchorMin = new Vector2(1f, 1f);
        tutorialButtonContainer.anchorMax = new Vector2(1f, 1f);
        tutorialButtonContainer.pivot = new Vector2(1f, 1f);
        tutorialButtonContainer.anchoredPosition = new Vector2(-340f, -34f);
        tutorialButtonContainer.sizeDelta = new Vector2(190f, 58f);
        Image buttonBg = buttonGO.AddComponent<Image>();
        buttonBg.sprite = border;
        buttonBg.type = Image.Type.Sliced;
        buttonBg.color = new Color(0.02f, 0.12f, 0.13f, 0.92f);
        buttonBg.raycastTarget = true;
        Button button = buttonGO.AddComponent<Button>();
        button.targetGraphic = buttonBg;
        button.onClick.AddListener(ToggleTutorialPanel);
        GameObject iconGO = new GameObject("Gear_Icon");
        iconGO.transform.SetParent(tutorialButtonContainer, false);
        Text iconText = iconGO.AddComponent<Text>();
        iconText.font = font;
        iconText.text = "⚙";
        iconText.fontSize = 30;
        iconText.fontStyle = FontStyle.Bold;
        iconText.alignment = TextAnchor.MiddleCenter;
        iconText.color = textCyan;
        iconText.raycastTarget = false;
        RectTransform iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(1f, 0f);
        iconRect.anchorMax = new Vector2(1f, 1f);
        iconRect.pivot = new Vector2(1f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(58f, 0f);
        GameObject labelGO = new GameObject("Tutorial_Key_Label");
        labelGO.transform.SetParent(tutorialButtonContainer, false);
        Text labelText = labelGO.AddComponent<Text>();
        labelText.font = font;
        labelText.text = "TUTORIAL\nF1";
        labelText.fontSize = 16;
        labelText.fontStyle = FontStyle.Bold;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = textCyan;
        labelText.raycastTarget = false;
        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(10f, 0f);
        labelRect.offsetMax = new Vector2(-62f, 0f);
        GameObject panelGO = new GameObject("HUD_Tutorial_Panel");
        panelGO.transform.SetParent(transform, false);
        tutorialPanel = panelGO.AddComponent<RectTransform>();
        tutorialPanel.anchorMin = new Vector2(0.5f, 0.5f);
        tutorialPanel.anchorMax = new Vector2(0.5f, 0.5f);
        tutorialPanel.pivot = new Vector2(0.5f, 0.5f);
        tutorialPanel.anchoredPosition = Vector2.zero;
        tutorialPanel.sizeDelta = new Vector2(820f, 560f);
        Image panelBg = panelGO.AddComponent<Image>();
        panelBg.sprite = border;
        panelBg.type = Image.Type.Sliced;
        panelBg.color = new Color(0.01f, 0.05f, 0.055f, 0.96f);
        panelBg.raycastTarget = true;
        tutorialPanelGroup = panelGO.AddComponent<CanvasGroup>();
        tutorialPanelGroup.alpha = 0f;
        tutorialPanelGroup.interactable = false;
        tutorialPanelGroup.blocksRaycasts = false;
        GameObject titleGO = new GameObject("Tutorial_Title");
        titleGO.transform.SetParent(tutorialPanel, false);
        Text titleText = titleGO.AddComponent<Text>();
        titleText.font = font;
        titleText.text = "TUTORIAL OPERATORE // COME SI GIOCA";
        titleText.fontSize = 28;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = textCyan;
        titleText.raycastTarget = false;
        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -28f);
        titleRect.sizeDelta = new Vector2(-52f, 46f);
        GameObject bodyGO = new GameObject("Tutorial_Body");
        bodyGO.transform.SetParent(tutorialPanel, false);
        Text bodyText = bodyGO.AddComponent<Text>();
        bodyText.font = font;
        bodyText.text =
            "1. OBIETTIVO\n" +
            "   Ripristina il settore in emergenza, trova le keycard e completa le procedure prima che il timer arrivi a zero.\n\n" +
            "2. ESPLORAZIONE\n" +
            "   Muoviti nei settori, osserva gli indizi luminosi e avvicinati agli oggetti interattivi quando compare il prompt [E].\n\n" +
            "3. INTERAZIONI\n" +
            "   Usa i terminali, recupera strumenti e ripara i sistemi segnalati dalla HUD. Le keycard sbloccano nuove zone.\n\n" +
            "4. PERICOLO\n" +
            "   Tieni d'occhio batteria/vita e countdown. Se subisci danni la HUD lampeggia, quindi cerca riparo o cambia percorso.\n\n" +
            "5. COMANDI RAPIDI\n" +
            "   WASD: movimento | Mouse: visuale | E: interagisci | Q: scanner | F1: tutorial | ESC: chiudi/pause";
        bodyText.fontSize = 20;
        bodyText.lineSpacing = 1.12f;
        bodyText.alignment = TextAnchor.UpperLeft;
        bodyText.color = new Color(0.78f, 1f, 0.95f, 1f);
        bodyText.raycastTarget = false;
        RectTransform bodyRect = bodyGO.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(48f, 104f);
        bodyRect.offsetMax = new Vector2(-48f, -96f);
        GameObject closeGO = new GameObject("Tutorial_Close_Button");
        closeGO.transform.SetParent(tutorialPanel, false);
        RectTransform closeRect = closeGO.AddComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0f);
        closeRect.anchorMax = new Vector2(0.5f, 0f);
        closeRect.pivot = new Vector2(0.5f, 0f);
        closeRect.anchoredPosition = new Vector2(0f, 28f);
        closeRect.sizeDelta = new Vector2(260f, 54f);
        Image closeBg = closeGO.AddComponent<Image>();
        closeBg.sprite = border;
        closeBg.type = Image.Type.Sliced;
        closeBg.color = new Color(0.03f, 0.16f, 0.14f, 0.95f);
        closeBg.raycastTarget = true;
        Button closeButton = closeGO.AddComponent<Button>();
        closeButton.targetGraphic = closeBg;
        closeButton.onClick.AddListener(ChiudiTutorialPanel);
        GameObject closeLabelGO = new GameObject("Close_Label");
        closeLabelGO.transform.SetParent(closeRect, false);
        Text closeLabel = closeLabelGO.AddComponent<Text>();
        closeLabel.font = font;
        closeLabel.text = "CHIUDI TUTORIAL";
        closeLabel.fontSize = 18;
        closeLabel.fontStyle = FontStyle.Bold;
        closeLabel.alignment = TextAnchor.MiddleCenter;
        closeLabel.color = textCyan;
        closeLabel.raycastTarget = false;
        RectTransform closeLabelRect = closeLabelGO.GetComponent<RectTransform>();
        closeLabelRect.anchorMin = Vector2.zero;
        closeLabelRect.anchorMax = Vector2.one;
        closeLabelRect.offsetMin = Vector2.zero;
        closeLabelRect.offsetMax = Vector2.zero;
    }
    // F1 resta il comando principale: funziona anche quando il cursore non esiste in gioco.
    private void GestisciInputTutorial()
    {
        if (Input.GetKeyDown(tastoTutorial))
        {
            ToggleTutorialPanel();
        }
        if (tutorialAperto && Input.GetKeyDown(KeyCode.Escape))
        {
            ChiudiTutorialPanel();
        }
    }
    // Toggle secco del tutorial, utile per non bloccare il flusso del player.
    private void ToggleTutorialPanel()
    {
        if (tutorialAperto)
        {
            ChiudiTutorialPanel();
            return;
        }
        ApriTutorialPanel();
    }
    // Apertura modale controllata: pausa input gameplay e lascia leggibile il testo.
    private void ApriTutorialPanel()
    {
        if (tutorialPanelGroup == null) return;
        EnsureEventSystem(); // FIX: assicura che il mouse funzioni!
        if (!ModalUIState.TryOpen(TutorialModalOwner)) return;
        tutorialAperto = true;
        tutorialPanelGroup.alpha = 1f;
        tutorialPanelGroup.interactable = true;
        tutorialPanelGroup.blocksRaycasts = true;
    }
    // Bottone costruito runtime: stile e callback stanno vicini, cosi' si legge al volo.
    private static void EnsureEventSystem()
    {
        UnityEngine.EventSystems.EventSystem eventSystem = UnityEngine.EventSystems.EventSystem.current;
        if (eventSystem == null)
        {
            eventSystem = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem)).GetComponent<UnityEngine.EventSystems.EventSystem>();
        }
        System.Type inputSystemUiModule = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputSystemUiModule != null)
        {
            UnityEngine.EventSystems.StandaloneInputModule oldModule = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (oldModule != null) UnityEngine.Object.Destroy(oldModule);
            Component inputModule = eventSystem.GetComponent(inputSystemUiModule);
            if (!inputModule)
                inputModule = eventSystem.gameObject.AddComponent(inputSystemUiModule);
            if (inputModule is Behaviour behaviour)
                behaviour.enabled = true;
            inputSystemUiModule.GetMethod("AssignDefaultActions")?.Invoke(inputModule, null);
        }
        else if (!eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>())
        {
            eventSystem.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }
    // Chiusura pulita: riporta focus e stato modale alla partita.
    private void ChiudiTutorialPanel()
    {
        if (tutorialPanelGroup == null) return;
        tutorialAperto = false;
        tutorialPanelGroup.alpha = 0f;
        tutorialPanelGroup.interactable = false;
        tutorialPanelGroup.blocksRaycasts = false;
        ModalUIState.Close(TutorialModalOwner);
    }
    private void CostruisciBarraVitaLCD(Sprite solid, Sprite border, Font font)
    {
        GameObject panelGO = new GameObject("HealthBattery_Panel");
        panelGO.transform.SetParent(transform, false);
        batteryContainer = panelGO.AddComponent<RectTransform>();
        batteryContainer.anchorMin = new Vector2(0f, 0f);
        batteryContainer.anchorMax = new Vector2(0f, 0f);
        batteryContainer.pivot = new Vector2(0f, 0f);
        batteryContainer.anchoredPosition = new Vector2(150f, 100f);
        batteryContainer.sizeDelta = new Vector2(360f, 102f);
        // Sfondo dark glass con contorno verde
        Image bg = panelGO.AddComponent<Image>();
        bg.sprite = border;
        bg.type = Image.Type.Sliced;
        bg.color = darkGlass;
        bg.raycastTarget = false;
        // Intestazione Batteria LCD
        GameObject labelGO = new GameObject("Battery_Label");
        labelGO.transform.SetParent(batteryContainer, false);
        Text lbl = labelGO.AddComponent<Text>();
        lbl.font = font;
        lbl.fontSize = 16;
        lbl.fontStyle = FontStyle.Bold;
        lbl.alignment = TextAnchor.MiddleLeft;
        lbl.text = "⚡ POWER_CORE // CELL_STATUS";
        lbl.color = textCyan;
        lbl.horizontalOverflow = HorizontalWrapMode.Overflow;
        lbl.raycastTarget = false;
        RectTransform lblRT = labelGO.GetComponent<RectTransform>();
        lblRT.anchorMin = new Vector2(0f, 1f);
        lblRT.anchorMax = new Vector2(1f, 1f);
        lblRT.pivot = new Vector2(0f, 1f);
        lblRT.anchoredPosition = new Vector2(18f, -10f);
        lblRT.sizeDelta = new Vector2(324f, 22f);
        // Contenitore segmenti batteria LCD
        GameObject segContainerGO = new GameObject("Segments_Container");
        segContainerGO.transform.SetParent(batteryContainer, false);
        RectTransform segRT = segContainerGO.AddComponent<RectTransform>();
        segRT.anchorMin = new Vector2(0f, 0f);
        segRT.anchorMax = new Vector2(1f, 0f);
        segRT.pivot = new Vector2(0.5f, 0f);
        segRT.anchoredPosition = new Vector2(1.0f, 35f);
        segRT.sizeDelta = new Vector2(325f, 25f);
        HorizontalLayoutGroup hlg = segContainerGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 3.5f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.padding = new RectOffset(16, 16, 2, 2);
        batterySegments = new Image[NUM_SEGMENTI];
        for (int i = 0; i < NUM_SEGMENTI; i++)
        {
            GameObject seg = new GameObject($"Cell_{i}");
            seg.transform.SetParent(segRT, false);
            Image segImg = seg.AddComponent<Image>();
            segImg.sprite = solid;
            segImg.color = neonGreen;
            segImg.raycastTarget = false;
            batterySegments[i] = segImg;
        }
        // Testo Percentuale e Valore numerico HP
        GameObject txtPGO = new GameObject("Text_Percentage");
        txtPGO.transform.SetParent(batteryContainer, false);
        testoPercentualeHP = txtPGO.AddComponent<Text>();
        testoPercentualeHP.font = font;
        testoPercentualeHP.fontSize = 17;
        testoPercentualeHP.fontStyle = FontStyle.Bold;
        testoPercentualeHP.alignment = TextAnchor.MiddleLeft;
        testoPercentualeHP.text = "100% [ONLINE]";
        testoPercentualeHP.color = neonGreen;
        testoPercentualeHP.horizontalOverflow = HorizontalWrapMode.Overflow;
        testoPercentualeHP.raycastTarget = false;
        RectTransform pRT = txtPGO.GetComponent<RectTransform>();
        pRT.anchorMin = new Vector2(0f, 0f);
        pRT.anchorMax = new Vector2(0.55f, 0f);
        pRT.pivot = new Vector2(0f, 0f);
        pRT.anchoredPosition = new Vector2(18f, 8f);
        pRT.sizeDelta = new Vector2(160f, 22f);
        GameObject txtDGO = new GameObject("Text_Details");
        txtDGO.transform.SetParent(batteryContainer, false);
        testoDettaglioHP = txtDGO.AddComponent<Text>();
        testoDettaglioHP.font = font;
        testoDettaglioHP.fontSize = 16;
        testoDettaglioHP.alignment = TextAnchor.MiddleRight;
        testoDettaglioHP.text = "100 / 100 HP";
        testoDettaglioHP.color = new Color(0.7f, 1f, 0.8f, 0.85f);
        testoDettaglioHP.horizontalOverflow = HorizontalWrapMode.Overflow;
        testoDettaglioHP.raycastTarget = false;
        RectTransform dRT = txtDGO.GetComponent<RectTransform>();
        dRT.anchorMin = new Vector2(0.45f, 0f);
        dRT.anchorMax = new Vector2(1f, 0f);
        dRT.pivot = new Vector2(1f, 0f);
        dRT.anchoredPosition = new Vector2(-18f, 8f);
        dRT.sizeDelta = new Vector2(160f, 22f);
    }
    private void CostruisciMirinoVisore(Sprite solid, Font font)
    {
        GameObject reticleGO = new GameObject("RobotVisor_Reticle");
        reticleGO.transform.SetParent(transform, false);
        visorReticleContainer = reticleGO.AddComponent<RectTransform>();
        visorReticleContainer.anchorMin = new Vector2(0.5f, 0.5f);
        visorReticleContainer.anchorMax = new Vector2(0.5f, 0.5f);
        visorReticleContainer.pivot = new Vector2(0.5f, 0.5f);
        visorReticleContainer.anchoredPosition = Vector2.zero;
        visorReticleContainer.sizeDelta = new Vector2(80f, 80f);
        // Punto centrale
        GameObject dotGO = new GameObject("Center_Dot");
        dotGO.transform.SetParent(visorReticleContainer, false);
        reticleCenterDot = dotGO.AddComponent<Image>();
        reticleCenterDot.sprite = solid;
        reticleCenterDot.color = new Color(0.4f, 0.95f, 1f, 0.85f);
        RectTransform dotRT = dotGO.GetComponent<RectTransform>();
        dotRT.sizeDelta = new Vector2(4f, 4f);
        // Anello tech rotante con tick
        GameObject ringGO = new GameObject("Tech_Ring");
        ringGO.transform.SetParent(visorReticleContainer, false);
        reticleRing = ringGO.AddComponent<RectTransform>();
        reticleRing.sizeDelta = new Vector2(46f, 46f);
        // 4 tacche cardinali sull'anello
        for (int i = 0; i < 4; i++)
        {
            GameObject tick = new GameObject($"Tick_{i}");
            tick.transform.SetParent(reticleRing, false);
            Image tImg = tick.AddComponent<Image>();
            tImg.sprite = solid;
            tImg.color = new Color(0.4f, 0.95f, 1f, 0.6f);
            RectTransform tRT = tick.GetComponent<RectTransform>();
            tRT.sizeDelta = new Vector2(2f, 6f);
            float angle = i * 90f;
            tRT.localRotation = Quaternion.Euler(0f, 0f, angle);
            tRT.anchoredPosition = Quaternion.Euler(0f, 0f, angle) * new Vector2(0f, 20f);
        }
        // 4 Angoli Bracket Cybernetici [   ]
        reticleBrackets = new Image[4];
        Vector2[] bracketOffsets = new Vector2[]
        {
            new Vector2(-32f, 32f),  // Top Left
            new Vector2(32f, 32f),   // Top Right
            new Vector2(-32f, -32f), // Bottom Left
            new Vector2(32f, -32f)   // Bottom Right
        };
        for (int i = 0; i < 4; i++)
        {
            GameObject bGO = new GameObject($"Bracket_{i}");
            bGO.transform.SetParent(visorReticleContainer, false);
            reticleBrackets[i] = bGO.AddComponent<Image>();
            reticleBrackets[i].sprite = solid;
            reticleBrackets[i].color = new Color(0.4f, 0.95f, 1f, 0.7f);
            RectTransform bRT = bGO.GetComponent<RectTransform>();
            bRT.sizeDelta = new Vector2(7f, 2f);
            bRT.anchoredPosition = bracketOffsets[i];
        }
        // Testo stato Scanner / Lock
        GameObject statusGO = new GameObject("Scan_Status");
        statusGO.transform.SetParent(visorReticleContainer, false);
        reticleStatusText = statusGO.AddComponent<Text>();
        reticleStatusText.font = font;
        reticleStatusText.fontSize = 14;
        reticleStatusText.fontStyle = FontStyle.Bold;
        reticleStatusText.alignment = TextAnchor.MiddleCenter;
        reticleStatusText.text = "";
        reticleStatusText.color = neonGreen;
        reticleStatusText.horizontalOverflow = HorizontalWrapMode.Overflow;
        RectTransform stRT = statusGO.GetComponent<RectTransform>();
        stRT.anchoredPosition = new Vector2(0f, -48f);
        stRT.sizeDelta = new Vector2(200f, 24f);
    }
    private void CostruisciPromptProssimita(Sprite solid, Sprite border, Font font)
    {
        GameObject promptGO = new GameObject("Interaction_PromptPanel");
        promptGO.transform.SetParent(transform, false);
        promptPanel = promptGO.AddComponent<RectTransform>();
        promptPanel.anchorMin = new Vector2(0.5f, 0.5f);
        promptPanel.anchorMax = new Vector2(0.5f, 0.5f);
        promptPanel.pivot = new Vector2(0.5f, 1f);
        promptPanel.anchoredPosition = new Vector2(0f, -68f);
        promptPanel.sizeDelta = new Vector2(480f, 88f);
        promptCanvasGroup = promptGO.AddComponent<CanvasGroup>();
        promptCanvasGroup.alpha = 0f;
        Image bg = promptGO.AddComponent<Image>();
        bg.sprite = border;
        bg.type = Image.Type.Sliced;
        bg.color = darkGlass;
        // Badge Tasto [ E ]
        GameObject badgeGO = new GameObject("KeyBadge_E");
        badgeGO.transform.SetParent(promptPanel, false);
        promptKeyBadge = badgeGO.AddComponent<Image>();
        promptKeyBadge.sprite = border;
        promptKeyBadge.type = Image.Type.Sliced;
        promptKeyBadge.color = new Color(0.08f, 0.35f, 0.15f, 0.95f);
        RectTransform badgeRT = badgeGO.GetComponent<RectTransform>();
        badgeRT.anchorMin = new Vector2(0f, 0.5f);
        badgeRT.anchorMax = new Vector2(0f, 0.5f);
        badgeRT.pivot = new Vector2(0.5f, 0.5f);
        badgeRT.anchoredPosition = new Vector2(46f, 0f);
        badgeRT.sizeDelta = new Vector2(50f, 50f);
        GameObject keyTxtGO = new GameObject("KeyText");
        keyTxtGO.transform.SetParent(badgeGO.transform, false);
        promptKeyText = keyTxtGO.AddComponent<Text>();
        promptKeyText.font = font;
        promptKeyText.fontSize = 26;
        promptKeyText.fontStyle = FontStyle.Bold;
        promptKeyText.alignment = TextAnchor.MiddleCenter;
        promptKeyText.text = "E";
        promptKeyText.color = Color.white;
        RectTransform ktRT = keyTxtGO.GetComponent<RectTransform>();
        ktRT.anchorMin = Vector2.zero;
        ktRT.anchorMax = Vector2.one;
        ktRT.sizeDelta = Vector2.zero;
        // Titolo Oggetto
        GameObject titleGO = new GameObject("Prompt_Title");
        titleGO.transform.SetParent(promptPanel, false);
        promptTitleText = titleGO.AddComponent<Text>();
        promptTitleText.font = font;
        promptTitleText.fontSize = 17;
        promptTitleText.fontStyle = FontStyle.Bold;
        promptTitleText.alignment = TextAnchor.MiddleLeft;
        promptTitleText.text = "AUTORIZZAZIONE: KEYCARD_A02";
        promptTitleText.color = neonGreen;
        promptTitleText.horizontalOverflow = HorizontalWrapMode.Overflow;
        RectTransform tRT = titleGO.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0f, 0.5f);
        tRT.anchorMax = new Vector2(1f, 0.5f);
        tRT.pivot = new Vector2(0f, 0.5f);
        tRT.anchoredPosition = new Vector2(84f, 16f);
        tRT.sizeDelta = new Vector2(380f, 26f);
        // Azione / Cosa sistemare
        GameObject actGO = new GameObject("Prompt_Action");
        actGO.transform.SetParent(promptPanel, false);
        promptActionText = actGO.AddComponent<Text>();
        promptActionText.font = font;
        promptActionText.fontSize = 15;
        promptActionText.alignment = TextAnchor.MiddleLeft;
        promptActionText.text = "Premi [E] per Raccogliere Scheda di Accesso";
        promptActionText.color = textCyan;
        promptActionText.horizontalOverflow = HorizontalWrapMode.Overflow;
        RectTransform aRT = actGO.GetComponent<RectTransform>();
        aRT.anchorMin = new Vector2(0f, 0.5f);
        aRT.anchorMax = new Vector2(1f, 0.5f);
        aRT.pivot = new Vector2(0f, 0.5f);
        aRT.anchoredPosition = new Vector2(84f, -15f);
        aRT.sizeDelta = new Vector2(380f, 24f);
    }
    private void CostruisciBannerNotifica(Sprite solid, Sprite border, Font font)
    {
        GameObject notificaGO = new GameObject("Notification_Banner");
        notificaGO.transform.SetParent(transform, false);
        notificaPanel = notificaGO.AddComponent<RectTransform>();
        notificaPanel.anchorMin = new Vector2(0.5f, 1f);
        notificaPanel.anchorMax = new Vector2(0.5f, 1f);
        notificaPanel.pivot = new Vector2(0.5f, 1f);
        notificaPanel.anchoredPosition = new Vector2(0f, -25f);
        notificaPanel.sizeDelta = new Vector2(580f, 80f);
        notificaCanvasGroup = notificaGO.AddComponent<CanvasGroup>();
        notificaCanvasGroup.alpha = 0f;
        Image bg = notificaGO.AddComponent<Image>();
        bg.sprite = border;
        bg.type = Image.Type.Sliced;
        bg.color = darkGlass;
        // Titolo Notifica
        GameObject tGO = new GameObject("Notifica_Title");
        tGO.transform.SetParent(notificaPanel, false);
        notificaTitleText = tGO.AddComponent<Text>();
        notificaTitleText.font = font;
        notificaTitleText.fontSize = 19;
        notificaTitleText.fontStyle = FontStyle.Bold;
        notificaTitleText.alignment = TextAnchor.MiddleCenter;
        notificaTitleText.text = "AUTORIZZAZIONE ACQUISITA";
        notificaTitleText.color = neonGreen;
        notificaTitleText.horizontalOverflow = HorizontalWrapMode.Overflow;
        RectTransform ntRT = tGO.GetComponent<RectTransform>();
        ntRT.anchorMin = new Vector2(0f, 0.5f);
        ntRT.anchorMax = new Vector2(1f, 0.5f);
        ntRT.pivot = new Vector2(0.5f, 0.5f);
        ntRT.anchoredPosition = new Vector2(0f, 14f);
        ntRT.sizeDelta = new Vector2(550f, 28f);
        // Sottotitolo / Dettagli
        GameObject sGO = new GameObject("Notifica_Sub");
        sGO.transform.SetParent(notificaPanel, false);
        notificaSubText = sGO.AddComponent<Text>();
        notificaSubText.font = font;
        notificaSubText.fontSize = 15;
        notificaSubText.alignment = TextAnchor.MiddleCenter;
        notificaSubText.text = "KEYCARD_A02 // ACCESSO AL SETTORE AGGIORNATO";
        notificaSubText.color = textCyan;
        notificaSubText.horizontalOverflow = HorizontalWrapMode.Overflow;
        RectTransform nsRT = sGO.GetComponent<RectTransform>();
        nsRT.anchorMin = new Vector2(0f, 0.5f);
        nsRT.anchorMax = new Vector2(1f, 0.5f);
        nsRT.pivot = new Vector2(0.5f, 0.5f);
        nsRT.anchoredPosition = new Vector2(0f, -15f);
        nsRT.sizeDelta = new Vector2(550f, 24f);
    }
    private void CostruisciTimerVisore(Sprite solid, Sprite border, Font font)
    {
        GameObject panelGO = new GameObject("CyberTimer_Panel");
        panelGO.transform.SetParent(transform, false);
        timerContainer = panelGO.AddComponent<RectTransform>();
        timerContainer.anchorMin = new Vector2(1f, 1f);
        timerContainer.anchorMax = new Vector2(1f, 1f);
        timerContainer.pivot = new Vector2(1f, 1f);
        timerContainer.anchoredPosition = new Vector2(-60f, -40f);
        timerContainer.sizeDelta = new Vector2(250f, 92f);
        // Sfondo dark glass con contorno verde/cianotico
        Image bg = panelGO.AddComponent<Image>();
        bg.sprite = border;
        bg.type = Image.Type.Sliced;
        bg.color = darkGlass;
        bg.raycastTarget = false;
        // Intestazione Timer LCD
        GameObject labelGO = new GameObject("Timer_Label");
        labelGO.transform.SetParent(timerContainer, false);
        Text lbl = labelGO.AddComponent<Text>();
        lbl.font = font;
        lbl.fontSize = 14;
        lbl.fontStyle = FontStyle.Bold;
        lbl.alignment = TextAnchor.MiddleLeft;
        lbl.text = "⏳ COUNTDOWN // T-MINUS";
        lbl.color = textCyan;
        lbl.horizontalOverflow = HorizontalWrapMode.Overflow;
        lbl.raycastTarget = false;
        RectTransform lblRT = labelGO.GetComponent<RectTransform>();
        lblRT.anchorMin = new Vector2(0f, 1f);
        lblRT.anchorMax = new Vector2(1f, 1f);
        lblRT.pivot = new Vector2(0f, 1f);
        lblRT.anchoredPosition = new Vector2(16f, -10f);
        lblRT.sizeDelta = new Vector2(220f, 20f);
        // Testo Digitale Orologio Timer (Grande Verde Neon)
        GameObject valGO = new GameObject("Timer_Value");
        valGO.transform.SetParent(timerContainer, false);
        testoTimerValore = valGO.AddComponent<Text>();
        testoTimerValore.font = font;
        testoTimerValore.fontSize = 28;
        testoTimerValore.fontStyle = FontStyle.Bold;
        testoTimerValore.alignment = TextAnchor.MiddleLeft;
        testoTimerValore.text = "00:00.0";
        testoTimerValore.color = neonGreen;
        testoTimerValore.horizontalOverflow = HorizontalWrapMode.Overflow;
        testoTimerValore.raycastTarget = false;
        RectTransform valRT = valGO.GetComponent<RectTransform>();
        valRT.anchorMin = new Vector2(0f, 0.5f);
        valRT.anchorMax = new Vector2(1f, 0.5f);
        valRT.pivot = new Vector2(0f, 0.5f);
        valRT.anchoredPosition = new Vector2(18f, -2f);
        valRT.sizeDelta = new Vector2(220f, 32f);
        // Sottotitolo / Status Timer
        GameObject stGO = new GameObject("Timer_Status");
        stGO.transform.SetParent(timerContainer, false);
        testoTimerStatus = stGO.AddComponent<Text>();
        testoTimerStatus.font = font;
        testoTimerStatus.fontSize = 12;
        testoTimerStatus.alignment = TextAnchor.MiddleLeft;
        testoTimerStatus.text = "SYS_REC // SEC_02 [ACTIVE]";
        testoTimerStatus.color = textCyan;
        testoTimerStatus.horizontalOverflow = HorizontalWrapMode.Overflow;
        testoTimerStatus.raycastTarget = false;
        RectTransform stRT = stGO.GetComponent<RectTransform>();
        stRT.anchorMin = new Vector2(0f, 0f);
        stRT.anchorMax = new Vector2(1f, 0f);
        stRT.pivot = new Vector2(0f, 0f);
        stRT.anchoredPosition = new Vector2(18f, 10f);
        stRT.sizeDelta = new Vector2(220f, 18f);
    }
    // ─────────────────────────────────────────────────────────────────────────
    // AGGIORNAMENTO DINAMICO BARRA VITA LCD
    // ─────────────────────────────────────────────────────────────────────────
    private void OnSaluteAggiornata(float corrente, float massima)
    {
        if (corrente < hpCorrenti && hpCorrenti > 0)
        {
            TriggerDamageFeedback(hpCorrenti - corrente);
        }
        hpCorrenti = corrente;
        hpMassimi = massima;
        float ratio = Mathf.Clamp01(corrente / Mathf.Max(massima, 1f));
        int segmentiAttivi = Mathf.CeilToInt(ratio * NUM_SEGMENTI);
        Color activeColor = ratio <= 0.25f ? warningRed : neonGreen;
        if (batterySegments != null)
        {
            for (int i = 0; i < NUM_SEGMENTI; i++)
            {
                if (batterySegments[i] != null)
                {
                    bool attivo = i < segmentiAttivi;
                    batterySegments[i].color = attivo ? activeColor : neonGreenDim;
                }
            }
        }
        if (testoPercentualeHP != null)
        {
            int perc = Mathf.CeilToInt(ratio * 100f);
            testoPercentualeHP.text = ratio <= 0.25f ? $"[CRITICAL {perc}%]" : $"{perc}% [ONLINE]";
            testoPercentualeHP.color = activeColor;
        }
        if (testoDettaglioHP != null)
        {
            testoDettaglioHP.text = $"{Mathf.CeilToInt(corrente)} / {Mathf.CeilToInt(massima)} HP";
        }
    }
    private void CostruisciDamageFlash(Sprite solid)
    {
        GameObject dfGO = new GameObject("HUD_DamageFlash");
        dfGO.transform.SetParent(transform, false);
        Image dfImg = dfGO.AddComponent<Image>();
        dfImg.sprite = solid;
        dfImg.color = new Color(1f, 0.05f, 0.05f, 0.35f);
        dfImg.raycastTarget = false;
        damageFlashGroup = dfGO.AddComponent<CanvasGroup>();
        damageFlashGroup.alpha = 0f;
        RectTransform dfRT = dfGO.GetComponent<RectTransform>();
        dfRT.anchorMin = Vector2.zero;
        dfRT.anchorMax = Vector2.one;
        dfRT.sizeDelta = Vector2.zero;
        dfRT.anchoredPosition = Vector2.zero;
    }
    private void TriggerDamageFeedback(float deltaDamage)
    {
        if (damageFlashCoroutine != null)
            StopCoroutine(damageFlashCoroutine);
        damageFlashCoroutine = StartCoroutine(AnimaDamageFlash());
    }
    private IEnumerator AnimaDamageFlash()
    {
        if (damageFlashGroup == null) yield break;
        damageFlashGroup.alpha = 1f;
        float timer = 0.35f;
        while (timer > 0)
        {
            timer -= Time.unscaledDeltaTime;
            damageFlashGroup.alpha = Mathf.Clamp01(timer / 0.35f);
            yield return null;
        }
        damageFlashGroup.alpha = 0f;
    }
    // ─────────────────────────────────────────────────────────────────────────
    // CONTROLLO VISORE ROBOTICO & PROMPT PROSSIMITÀ
    // ─────────────────────────────────────────────────────────────────────────
    public void SetTargetLocked(bool locked, string nomeTarget = "")
    {
        isTargetLocked = locked;
        if (reticleCenterDot != null)
            reticleCenterDot.color = locked ? neonGreen : new Color(0.4f, 0.95f, 1f, 0.85f);
        if (reticleBrackets != null)
        {
            Color c = locked ? neonGreen : new Color(0.4f, 0.95f, 1f, 0.7f);
            foreach (var b in reticleBrackets)
            {
                if (b != null) b.color = c;
            }
        }
        if (reticleStatusText != null)
        {
            reticleStatusText.text = locked ? "[TARGET LOCKED]" : "";
            reticleStatusText.color = neonGreen;
        }
        if (!locked && visorReticleContainer != null)
        {
            visorReticleContainer.localScale = Vector3.one;
        }
    }
    public void MostraPrompt(string titoloOggetto, string azioneDescrizione)
    {
        if (promptTitleText != null) promptTitleText.text = titoloOggetto;
        if (promptActionText != null) promptActionText.text = azioneDescrizione;
        if (promptCanvasGroup != null)
        {
            promptCanvasGroup.alpha = 1f;
        }
        SetTargetLocked(true, titoloOggetto);
    }
    public void NascondiPrompt()
    {
        if (promptCanvasGroup != null)
        {
            promptCanvasGroup.alpha = 0f;
        }
        SetTargetLocked(false);
    }
    // ─────────────────────────────────────────────────────────────────────────
    // NOTIFICA OLOGRAFICA ACQUISIZIONE
    // ─────────────────────────────────────────────────────────────────────────
    public void MostraNotificaAcquisizione(string titolo, string dettagli)
    {
        if (notificaCoroutine != null)
            StopCoroutine(notificaCoroutine);
        notificaCoroutine = StartCoroutine(AnimaNotifica(titolo, dettagli));
    }
    private IEnumerator AnimaNotifica(string titolo, string dettagli)
    {
        if (notificaTitleText != null) notificaTitleText.text = titolo.ToUpper();
        if (notificaSubText != null) notificaSubText.text = dettagli.ToUpper();
        if (notificaCanvasGroup == null) yield break;
        // Fade in
        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.unscaledDeltaTime;
            notificaCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t / 0.25f);
            yield return null;
        }
        notificaCanvasGroup.alpha = 1f;
        // Mostra a schermo per 3.5 secondi
        yield return new WaitForSecondsRealtime(3.5f);
        // Fade out
        t = 0f;
        while (t < 0.4f)
        {
            t += Time.unscaledDeltaTime;
            notificaCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t / 0.4f);
            yield return null;
        }
        notificaCanvasGroup.alpha = 0f;
    }
    // ─────────────────────────────────────────────────────────────────────────
    // SPRITE GENERATORS (Cornice Neon e Texture Solida)
    // ─────────────────────────────────────────────────────────────────────────
    private Sprite CreaSpriteSolido()
    {
        Texture2D tex = new Texture2D(2, 2);
        Color[] cols = new Color[] { Color.white, Color.white, Color.white, Color.white };
        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
    }
    private Sprite CreaSpriteCorniceTech()
    {
        int w = 32;
        int h = 32;
        Texture2D tex = new Texture2D(w, h);
        tex.filterMode = FilterMode.Point;
        Color[] pixels = new Color[w * h];
        Color bg = new Color(1f, 1f, 1f, 0.12f);
        Color border = new Color(0.2f, 1f, 0.35f, 0.95f);
        Color corner = new Color(0.4f, 1f, 0.6f, 1f);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool isBorderX = (x == 0 || x == w - 1);
                bool isBorderY = (y == 0 || y == h - 1);
                bool isCorner = (x < 4 || x >= w - 4) && (y < 4 || y >= h - 4);
                if (isCorner && (isBorderX || isBorderY))
                    pixels[y * w + x] = corner;
                else if (isBorderX || isBorderY)
                    pixels[y * w + x] = border;
                else
                    pixels[y * w + x] = bg;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(6, 6, 6, 6));
    }
    // =========================================================================
    // 8. HINT CREDENZIALI — banner lampeggiante "cerca le credenziali"
    // =========================================================================

    /// <summary>
    /// Mostra il banner lampeggiante che indica al giocatore di cercare le credenziali
    /// vicino al terminale di sicurezza. Rimane visibile finche' non si chiama NascondiHintCredenziali().
    /// </summary>
    public void MostraHintCredenziali(string messaggioOpzionale = null)
    {
        if (hudCanvas == null) return;
        // Costruisce il panel al primo uso
        if (hintCredenzialiPanel == null)
            CostruisciHintCredenzialiPanel();
        // Imposta il testo personalizzato o quello di default
        if (hintCredenzialiTesto != null)
        {
            hintCredenzialiTesto.text = messaggioOpzionale ??
                "⚠  TERMINALE BLOCCATO\nCerca le credenziali di accesso nei dintorni!";
        }
        hintCredenzialiPanel.gameObject.SetActive(true);
        // Riavvia lampeggio
        if (hintCredenzialiCoroutine != null) StopCoroutine(hintCredenzialiCoroutine);
        hintCredenzialiCoroutine = StartCoroutine(LampeggiaBannerCredenziali());
    }
    /// <summary>
    /// Nasconde il banner hint credenziali (da chiamare quando le credenziali vengono trovate).
    /// </summary>
    public void NascondiHintCredenziali()
    {
        if (hintCredenzialiCoroutine != null)
        {
            StopCoroutine(hintCredenzialiCoroutine);
            hintCredenzialiCoroutine = null;
        }
        if (hintCredenzialiPanel != null)
            hintCredenzialiPanel.gameObject.SetActive(false);
    }
    // Costruisce il banner hint al primo utilizzo
    private void CostruisciHintCredenzialiPanel()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        // Panel contenitore — in basso a sinistra, sopra la barra HP
        GameObject panelGO = new GameObject("Hint_Credenziali_Panel");
        panelGO.transform.SetParent(hudCanvas.transform, false);
        hintCredenzialiPanel = panelGO.AddComponent<RectTransform>();
        hintCredenzialiPanel.anchorMin = new Vector2(0f, 0f);
        hintCredenzialiPanel.anchorMax = new Vector2(0f, 0f);
        hintCredenzialiPanel.pivot     = new Vector2(0f, 0f);
        hintCredenzialiPanel.anchoredPosition = new Vector2(24f, 130f);
        hintCredenzialiPanel.sizeDelta = new Vector2(420f, 80f);
        // Sfondo arancione semi-trasparente con bordo lampeggiante
        Image bgImg = panelGO.AddComponent<Image>();
        bgImg.color = new Color(0.9f, 0.45f, 0.0f, 0.88f);
        hintCredenzialiGroup = panelGO.AddComponent<CanvasGroup>();
        hintCredenzialiGroup.alpha = 1f;
        // Icona terminale (testo emoji simulato)
        GameObject iconaGO = new GameObject("Hint_Icona");
        iconaGO.transform.SetParent(hintCredenzialiPanel, false);
        hintIconaTerminale = iconaGO.AddComponent<Image>();
        hintIconaTerminale.color = new Color(1f, 1f, 0.2f, 1f);
        RectTransform iconaRect = iconaGO.GetComponent<RectTransform>();
        iconaRect.anchorMin = new Vector2(0f, 0.5f);
        iconaRect.anchorMax = new Vector2(0f, 0.5f);
        iconaRect.pivot     = new Vector2(0f, 0.5f);
        iconaRect.anchoredPosition = new Vector2(12f, 0f);
        iconaRect.sizeDelta = new Vector2(16f, 16f);
        // Testo messaggio
        GameObject testoGO = new GameObject("Hint_Testo");
        testoGO.transform.SetParent(hintCredenzialiPanel, false);
        hintCredenzialiTesto = testoGO.AddComponent<Text>();
        hintCredenzialiTesto.font      = font;
        hintCredenzialiTesto.fontSize  = 17;
        hintCredenzialiTesto.fontStyle = FontStyle.Bold;
        hintCredenzialiTesto.color     = Color.white;
        hintCredenzialiTesto.alignment = TextAnchor.MiddleLeft;
        hintCredenzialiTesto.raycastTarget = false;
        hintCredenzialiTesto.text =
            "⚠  TERMINALE BLOCCATO\nCerca le credenziali di accesso nei dintorni!";
        RectTransform testoRect = testoGO.GetComponent<RectTransform>();
        testoRect.anchorMin = Vector2.zero;
        testoRect.anchorMax = Vector2.one;
        testoRect.offsetMin = new Vector2(36f, 4f);
        testoRect.offsetMax = new Vector2(-10f, -4f);
        panelGO.SetActive(false);
    }
    // Coroutine lampeggio banner credenziali: pulsa alpha tra 0.4 e 1.0
    private IEnumerator LampeggiaBannerCredenziali()
    {
        float velocita = 2.8f;
        while (true)
        {
            float alpha = Mathf.Lerp(0.4f, 1.0f, (Mathf.Sin(Time.unscaledTime * velocita) + 1f) * 0.5f);
            if (hintCredenzialiGroup != null) hintCredenzialiGroup.alpha = alpha;
            // Lampeggia anche l'icona con colore alternato
            if (hintIconaTerminale != null)
            {
                float t = (Mathf.Sin(Time.unscaledTime * velocita * 1.5f) + 1f) * 0.5f;
                hintIconaTerminale.color = Color.Lerp(new Color(1f, 0.6f, 0f, 1f), new Color(1f, 1f, 0.2f, 1f), t);
            }
            yield return null;
        }
    }
    // =========================================================================
    // 9. HINT TUTORIAL DINAMICO
    // =========================================================================

    public void MostraHintTutorial(string messaggio)
    {
        if (hudCanvas == null) return;
        if (hintTutorialPanel == null)
            CostruisciHintTutorialPanel();
        if (hintTutorialTesto != null)
            hintTutorialTesto.text = messaggio;
        hintTutorialPanel.gameObject.SetActive(true);
        if (hintTutorialCoroutine != null) StopCoroutine(hintTutorialCoroutine);
        hintTutorialCoroutine = StartCoroutine(LampeggiaBannerTutorial());
    }
    public void NascondiHintTutorial()
    {
        if (hintTutorialCoroutine != null)
        {
            StopCoroutine(hintTutorialCoroutine);
            hintTutorialCoroutine = null;
        }
        if (hintTutorialPanel != null)
            hintTutorialPanel.gameObject.SetActive(false);
    }
    private void CostruisciHintTutorialPanel()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        // Panel contenitore — in basso, sopra il prompt prossimita' ma centrato
        GameObject panelGO = new GameObject("Hint_Tutorial_Panel");
        panelGO.transform.SetParent(hudCanvas.transform, false);
        hintTutorialPanel = panelGO.AddComponent<RectTransform>();
        hintTutorialPanel.anchorMin = new Vector2(0.5f, 0f);
        hintTutorialPanel.anchorMax = new Vector2(0.5f, 0f);
        hintTutorialPanel.pivot     = new Vector2(0.5f, 0f);
        hintTutorialPanel.anchoredPosition = new Vector2(0f, 220f);
        hintTutorialPanel.sizeDelta = new Vector2(700f, 60f);
        // Sfondo ciano semi-trasparente
        Image bgImg = panelGO.AddComponent<Image>();
        bgImg.color = new Color(0.05f, 0.45f, 0.6f, 0.85f);
        hintTutorialGroup = panelGO.AddComponent<CanvasGroup>();
        hintTutorialGroup.alpha = 1f;
        // Icona ingranaggio tutorial
        GameObject iconaGO = new GameObject("Tutorial_Icona");
        iconaGO.transform.SetParent(hintTutorialPanel, false);
        hintIconaTutorial = iconaGO.AddComponent<Image>();
        hintIconaTutorial.color = new Color(0.4f, 1f, 1f, 1f);
        RectTransform iconaRect = iconaGO.GetComponent<RectTransform>();
        iconaRect.anchorMin = new Vector2(0f, 0.5f);
        iconaRect.anchorMax = new Vector2(0f, 0.5f);
        iconaRect.pivot     = new Vector2(0f, 0.5f);
        iconaRect.anchoredPosition = new Vector2(16f, 0f);
        iconaRect.sizeDelta = new Vector2(24f, 24f);
        // Testo messaggio tutorial
        GameObject testoGO = new GameObject("Tutorial_Testo");
        testoGO.transform.SetParent(hintTutorialPanel, false);
        hintTutorialTesto = testoGO.AddComponent<Text>();
        hintTutorialTesto.font      = font;
        hintTutorialTesto.fontSize  = 18;
        hintTutorialTesto.fontStyle = FontStyle.Bold;
        hintTutorialTesto.color     = Color.white;
        hintTutorialTesto.alignment = TextAnchor.MiddleLeft;
        hintTutorialTesto.raycastTarget = false;
        RectTransform testoRect = testoGO.GetComponent<RectTransform>();
        testoRect.anchorMin = Vector2.zero;
        testoRect.anchorMax = Vector2.one;
        testoRect.offsetMin = new Vector2(50f, 4f);
        testoRect.offsetMax = new Vector2(-10f, -4f);
        panelGO.SetActive(false);
    }
    private IEnumerator LampeggiaBannerTutorial()
    {
        float velocita = 2.0f;
        while (true)
        {
            float alpha = Mathf.Lerp(0.6f, 1.0f, (Mathf.Sin(Time.unscaledTime * velocita) + 1f) * 0.5f);
            if (hintTutorialGroup != null) hintTutorialGroup.alpha = alpha;
            yield return null;
        }
    }
}
