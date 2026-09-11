using System.Collections;
using System.Collections.Generic;
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

    // Canvas & UI Components
    private Canvas hudCanvas;
    private CanvasScaler hudScaler;

    // 1. Barra Vita Batteria LCD
    private RectTransform batteryContainer;
    private Image[] batterySegments;
    private Text testoPercentualeHP;
    private Text testoDettaglioHP;
    private const int NUM_SEGMENTI = 10;
    private float hpCorrenti = 100f;
    private float hpMassimi = 100f;

    // 2. Mirino Visore Robotico
    private RectTransform visorReticleContainer;
    private RectTransform reticleRing;
    private Image reticleCenterDot;
    private Image[] reticleBrackets;
    private Text reticleStatusText;
    private bool isTargetLocked = false;
    private float reticleRotationSpeed = 25f;

    // 3. Prompt di Prossimità [E]
    private RectTransform promptPanel;
    private CanvasGroup promptCanvasGroup;
    private Text promptTitleText;
    private Text promptActionText;
    private Image promptKeyBadge;
    private Text promptKeyText;

    // 4. Banner Notifica Acquisizione Keycard
    private RectTransform notificaPanel;
    private CanvasGroup notificaCanvasGroup;
    private Text notificaTitleText;
    private Text notificaSubText;
    private Coroutine notificaCoroutine;

    // 5. Flash e Feedback Impatto Danni
    private CanvasGroup damageFlashGroup;
    private Coroutine damageFlashCoroutine;

    private void Awake()
    {
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
        InizializzaStatoIniziale();
    }

    private void OnDisable()
    {
        SalutePlayer.OnSaluteCambiata -= OnSaluteAggiornata;
    }

    private void Start()
    {
        InizializzaStatoIniziale();
    }

    public void InizializzaStatoIniziale()
    {
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
    }

    private void Update()
    {
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

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

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

        // 6. Flash Visivo Impatto Danno Schermo
        CostruisciDamageFlash(solidSprite);
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
}
