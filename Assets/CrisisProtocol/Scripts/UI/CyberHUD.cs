// ============================================================================
// Crisis Protocol / Sector Containment - Interfaccia utente
// File: .\Assets\CrisisProtocol\Scripts\UI\CyberHUD.cs
// Responsabilita': aggiorna HUD, menu, overlay, gauge, notifiche o schermate di supporto in base agli eventi gameplay.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using System.Collections; // usa lib // riga-ok
using System.Collections.Generic; // usa lib // riga-ok
using CrisisProtocol.UI; // usa hud // riga-ok
using UnityEngine; // usa lib // riga-ok
using UnityEngine.UI; // usa lib // riga-ok

/// <summary>
/// HUD Cybernetico avanzato in stile Visore Robot:
/// 1. Barra della Vita LCD Verde a celle (Stato di Carica Batteria).
/// 2. Mirino centrale da visore robotico con Lock-On dinamico.
/// 3. Prompt di Prossimità Trasparente con bordi Verde Neon (Nome oggetto + Cosa sistemare/Azione).
/// 4. Notifiche Olografiche di Acquisizione Keycard a schermo.
/// Si avvia e si mostra IMMEDIATAMENTE all'inizio di ogni scena.
/// </summary>
// blocco: classe x roba grossa
public class CyberHUD : MonoBehaviour // classe qui // riga-ok
{ // apre // riga-ok
    private static CyberHUD instance; // roba pub // riga-ok
    public static CyberHUD Instance // roba pub // riga-ok
    { // apre // riga-ok
        get // ok qua // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (instance == null) // se ok // riga-ok
            { // apre // riga-ok
                instance = Object.FindAnyObjectByType<CyberHUD>(); // setta // riga-ok
                // blocco: controlla se va
                if (instance == null) // se ok // riga-ok
                { // apre // riga-ok
                    GameObject go = new GameObject("CyberHUD_System"); // setta // riga-ok
                    instance = go.AddComponent<CyberHUD>(); // setta // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
            return instance; // torna val // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // nota unity // riga-ok
    // blocco: funzione fa cose
    private static void AutoAvviaHUDSuCaricamentoScena() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (Instance != null) // se ok // riga-ok
        { // apre // riga-ok
            Instance.InizializzaStatoIniziale(); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    [Header("Colori Palette Cyber/Neon")] // nota unity // riga-ok
    public Color neonGreen = new Color(0.15f, 1f, 0.25f, 1f); // roba pub // riga-ok
    public Color neonGreenDim = new Color(0.1f, 0.5f, 0.15f, 0.35f); // roba pub // riga-ok
    public Color darkGlass = new Color(0.02f, 0.06f, 0.04f, 0.85f); // roba pub // riga-ok
    public Color warningRed = new Color(1f, 0.2f, 0.1f, 1f); // roba pub // riga-ok
    public Color textCyan = new Color(0.4f, 0.95f, 1f, 1f); // roba pub // riga-ok

    // Canvas & UI Components
    private Canvas hudCanvas; // roba pub // riga-ok
    private CanvasScaler hudScaler; // roba pub // riga-ok

    // 1. Barra Vita Batteria LCD
    private RectTransform batteryContainer; // roba pub // riga-ok
    private Image[] batterySegments; // roba pub // riga-ok
    private Text testoPercentualeHP; // roba pub // riga-ok
    private Text testoDettaglioHP; // roba pub // riga-ok
    private const int NUM_SEGMENTI = 10; // roba pub // riga-ok
    private float hpCorrenti = 100f; // roba pub // riga-ok
    private float hpMassimi = 100f; // roba pub // riga-ok

    // 2. Mirino Visore Robotico
    private RectTransform visorReticleContainer; // roba pub // riga-ok
    private RectTransform reticleRing; // roba pub // riga-ok
    private Image reticleCenterDot; // roba pub // riga-ok
    private Image[] reticleBrackets; // roba pub // riga-ok
    private Text reticleStatusText; // roba pub // riga-ok
    private bool isTargetLocked = false; // roba pub // riga-ok
    private float reticleRotationSpeed = 25f; // roba pub // riga-ok

    // 3. Prompt di Prossimità [E]
    private RectTransform promptPanel; // roba pub // riga-ok
    private CanvasGroup promptCanvasGroup; // roba pub // riga-ok
    private Text promptTitleText; // roba pub // riga-ok
    private Text promptActionText; // roba pub // riga-ok
    private Image promptKeyBadge; // roba pub // riga-ok
    private Text promptKeyText; // roba pub // riga-ok

    // 4. Banner Notifica Acquisizione Keycard
    private RectTransform notificaPanel; // roba pub // riga-ok
    private CanvasGroup notificaCanvasGroup; // roba pub // riga-ok
    private Text notificaTitleText; // roba pub // riga-ok
    private Text notificaSubText; // roba pub // riga-ok
    private Coroutine notificaCoroutine; // roba pub // riga-ok

    // 5. Flash e Feedback Impatto Danni
    private CanvasGroup damageFlashGroup; // roba pub // riga-ok
    private Coroutine damageFlashCoroutine; // roba pub // riga-ok

    // 6. Cyber Countdown Timer Panel (In alto a destra)
    [Header("Configurazione Countdown (Inspector)")] // nota unity // riga-ok
    [Tooltip("Durata del timer di missione in MINUTI (regolabile da qui: es. 5 = 5:00, 3 = 3:00, 10 = 10:00).")] // nota unity // riga-ok
    [SerializeField] [Range(0.5f, 60f)] private float durataInMinuti = 5f; // setta // riga-ok
    [Tooltip("Secondi totali calcolati per il countdown.")] // nota unity // riga-ok
    public float durataCountdownIniziale = 300f; // roba pub // riga-ok
    [Tooltip("Abilita o disabilita il conteggio all'indietro del timer.")] // nota unity // riga-ok
    [SerializeField] private bool timerAttivo = true; // setta // riga-ok
    [Tooltip("Se true, provoca la sconfitta immediata allo scadere del timer (00:00.0).")] // nota unity // riga-ok
    [SerializeField] private bool sconfittaATempoScaduto = true; // setta // riga-ok
    [Tooltip("Se true, usa le impostazioni specificate qui su CyberHUD invece di ereditare quelle di MissionManager.")] // nota unity // riga-ok
    [SerializeField] private bool forzaImpostazioniLocaliHUD = false; // setta // riga-ok

    private RectTransform timerContainer; // roba pub // riga-ok
    private Text testoTimerValore; // roba pub // riga-ok
    private Text testoTimerStatus; // roba pub // riga-ok
    private float tempoRimanenteCountdown = 300f; // roba pub // riga-ok

    // 7. Pulsante tutorial rapido (ingranaggio HUD)
    private const string TutorialModalOwner = "HudTutorial"; // roba pub // riga-ok
    private RectTransform tutorialButtonContainer; // roba pub // riga-ok
    private RectTransform tutorialPanel; // roba pub // riga-ok
    private CanvasGroup tutorialPanelGroup; // roba pub // riga-ok
    private bool tutorialAperto = false; // roba pub // riga-ok

    public float TempoRimanente => tempoRimanenteCountdown; // roba pub // riga-ok
    // blocco: funzione fa cose
    public void ImpostaCountdown(float secondi) { durataCountdownIniziale = secondi; durataInMinuti = secondi / 60f; tempoRimanenteCountdown = secondi; } // roba pub // riga-ok
    // blocco: funzione fa cose
    public void ResetCountdown() => tempoRimanenteCountdown = durataCountdownIniziale; // roba pub // riga-ok
    // blocco: funzione fa cose
    public void SetTimerAttivo(bool attivo) => timerAttivo = attivo; // roba pub // riga-ok

    // blocco: funzione fa cose
    private void OnValidate() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (durataInMinuti > 0f) // se ok // riga-ok
        { // apre // riga-ok
            durataCountdownIniziale = durataInMinuti * 60f; // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void Awake() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (instance != null && instance != this) // se ok // riga-ok
        { // apre // riga-ok
            Destroy(gameObject); // elimina // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok
        instance = this; // setta // riga-ok
        DontDestroyOnLoad(gameObject); // chiama // riga-ok

        CostruisciHUDCompleto(); // chiama // riga-ok
        InizializzaStatoIniziale(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnEnable() // roba pub // riga-ok
    { // apre // riga-ok
        SalutePlayer.OnSaluteCambiata += OnSaluteAggiornata; // setta // riga-ok
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded; // setta // riga-ok
        InizializzaStatoIniziale(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnDisable() // roba pub // riga-ok
    { // apre // riga-ok
        SalutePlayer.OnSaluteCambiata -= OnSaluteAggiornata; // setta // riga-ok
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode) // roba pub // riga-ok
    { // apre // riga-ok
        InizializzaStatoIniziale(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void Start() // roba pub // riga-ok
    { // apre // riga-ok
        InizializzaStatoIniziale(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void InizializzaStatoIniziale() // roba pub // riga-ok
    { // apre // riga-ok
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name; // setta // riga-ok
        // blocco: controlla se va
        if (currentScene == "MainMenu-Scene") // se ok // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (hudCanvas != null) hudCanvas.enabled = false; // se ok // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (hudCanvas != null) hudCanvas.enabled = true; // se ok // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (MissionManager.Instance != null && !forzaImpostazioniLocaliHUD) // se ok // riga-ok
        { // apre // riga-ok
            timerAttivo = MissionManager.Instance.UsaTempoLimite; // setta // riga-ok
            durataInMinuti = MissionManager.Instance.TempoLimiteMinuti; // setta // riga-ok
            durataCountdownIniziale = MissionManager.Instance.TempoLimiteSecondi; // setta // riga-ok
            sconfittaATempoScaduto = MissionManager.Instance.SconfittaAScadenzaTimer; // setta // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (durataInMinuti > 0f) // se ok // riga-ok
            { // apre // riga-ok
                durataCountdownIniziale = durataInMinuti * 60f; // setta // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        tempoRimanenteCountdown = durataCountdownIniziale; // setta // riga-ok

        SalutePlayer p = Object.FindAnyObjectByType<SalutePlayer>(); // setta // riga-ok
        // blocco: controlla se va
        if (p != null) // se ok // riga-ok
        { // apre // riga-ok
            OnSaluteAggiornata(p.SaluteAttuale > 0 ? p.SaluteAttuale : p.puntiVitaMassimi, p.puntiVitaMassimi); // chiama // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            OnSaluteAggiornata(100f, 100f); // chiama // riga-ok
        } // chiude // riga-ok

        SetTargetLocked(false); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void Update() // roba pub // riga-ok
    { // apre // riga-ok
        // Rotazione continua dell'anello del visore robotico
        // blocco: controlla se va
        if (reticleRing != null) // se ok // riga-ok
        { // apre // riga-ok
            float speed = isTargetLocked ? 100f : reticleRotationSpeed; // setta // riga-ok
            reticleRing.Rotate(Vector3.forward, -speed * Time.unscaledDeltaTime); // chiama // riga-ok
        } // chiude // riga-ok

        // Effetto pulsazione mirino quando lockato
        // blocco: controlla se va
        if (visorReticleContainer != null && isTargetLocked) // se ok // riga-ok
        { // apre // riga-ok
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 8f) * 0.05f; // setta // riga-ok
            visorReticleContainer.localScale = new Vector3(pulse, pulse, 1f); // setta // riga-ok
        } // chiude // riga-ok

        // Aggiornamento Countdown a schermo LCD (Conteggio all'indietro)
        // blocco: controlla se va
        if (timerAttivo && testoTimerValore != null) // se ok // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (MissionManager.Instance != null && MissionManager.Instance.MissioneTerminata) // se ok // riga-ok
            { // apre // riga-ok
                return; // torna val // riga-ok
            } // chiude // riga-ok

            tempoRimanenteCountdown = Mathf.Max(0f, tempoRimanenteCountdown - Time.deltaTime); // setta // riga-ok
            int minuti = (int)(tempoRimanenteCountdown / 60f); // setta // riga-ok
            int secondi = (int)(tempoRimanenteCountdown % 60f); // setta // riga-ok
            int decimi = (int)((tempoRimanenteCountdown * 10f) % 10f); // setta // riga-ok

            testoTimerValore.text = $"{minuti:D2}:{secondi:D2}.{decimi:D1}"; // setta // riga-ok

            // Integrazione dinamica con lo stato di emergenza e countdown
            // blocco: controlla se va
            if (tempoRimanenteCountdown <= 0f) // se ok // riga-ok
            { // apre // riga-ok
                testoTimerValore.text = "00:00.0"; // setta // riga-ok
                testoTimerValore.color = warningRed; // setta // riga-ok
                // blocco: controlla se va
                if (testoTimerStatus != null) // se ok // riga-ok
                { // apre // riga-ok
                    testoTimerStatus.text = "⚠️ TIME EXPIRED // CRITICAL DEFEAT"; // setta // riga-ok
                    testoTimerStatus.color = warningRed; // setta // riga-ok
                } // chiude // riga-ok

                // blocco: controlla se va
                if (sconfittaATempoScaduto) // se ok // riga-ok
                { // apre // riga-ok
                    // blocco: controlla se va
                    if (MissionManager.Instance != null && !MissionManager.Instance.MissioneTerminata) // se ok // riga-ok
                    { // apre // riga-ok
                        MissionManager.Instance.TerminaPerTempoScaduto(); // chiama // riga-ok
                    } // chiude // riga-ok
                    // blocco: caso diverso
                    else // se no // riga-ok
                    { // apre // riga-ok
                        SalutePlayer player = Object.FindAnyObjectByType<SalutePlayer>(); // setta // riga-ok
                        // blocco: controlla se va
                        if (player != null && player.SaluteAttuale > 0) // se ok // riga-ok
                        { // apre // riga-ok
                            player.SubisciDanno(99999f); // chiama // riga-ok
                        } // chiude // riga-ok
                        DeathScreenController.ShowAndReloadCurrentScene(3.0f, 0f, "TEMPO SCADUTO // EVACUAZIONE FALLITA"); // chiama // riga-ok
                    } // chiude // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
            // blocco: controlla se va
            else if (tempoRimanenteCountdown <= 60f) // se ok // riga-ok
            { // apre // riga-ok
                // Ultimo minuto: allarme rosso lampeggiante
                float blink = Mathf.Sin(Time.unscaledTime * 10f); // setta // riga-ok
                testoTimerValore.color = blink > 0f ? warningRed : new Color(1f, 0.6f, 0.6f, 1f); // setta // riga-ok
                // blocco: controlla se va
                if (testoTimerStatus != null) // se ok // riga-ok
                { // apre // riga-ok
                    testoTimerStatus.text = "⚠️ T-MINUS CRITICAL // EVACUATE"; // setta // riga-ok
                    testoTimerStatus.color = warningRed; // setta // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
            // blocco: controlla se va
            else if (MissionManager.Instance != null && testoTimerStatus != null) // se ok // riga-ok
            { // apre // riga-ok
                float collasso = MissionManager.Instance.CollassoCorrente; // setta // riga-ok
                // blocco: controlla se va
                if (collasso > 75f) // se ok // riga-ok
                { // apre // riga-ok
                    float blink = Mathf.Sin(Time.unscaledTime * 8f); // setta // riga-ok
                    testoTimerValore.color = blink > 0f ? warningRed : new Color(1f, 0.75f, 0.2f, 1f); // setta // riga-ok
                    testoTimerStatus.text = $"⚠️ COLLAPSE: {Mathf.CeilToInt(collasso)}% [CRITICAL]"; // setta // riga-ok
                    testoTimerStatus.color = warningRed; // setta // riga-ok
                } // chiude // riga-ok
                // blocco: controlla se va
                else if (collasso > 40f) // se ok // riga-ok
                { // apre // riga-ok
                    testoTimerValore.color = new Color(1f, 0.75f, 0.2f, 1f); // setta // riga-ok
                    testoTimerStatus.text = $"SYS_ALERT: COLLAPSE {Mathf.CeilToInt(collasso)}%"; // setta // riga-ok
                    testoTimerStatus.color = new Color(1f, 0.75f, 0.2f, 1f); // setta // riga-ok
                } // chiude // riga-ok
                // blocco: caso diverso
                else // se no // riga-ok
                { // apre // riga-ok
                    testoTimerValore.color = neonGreen; // setta // riga-ok
                    testoTimerStatus.text = "SYS_REC // SEC_02 [COUNTDOWN]"; // setta // riga-ok
                    testoTimerStatus.color = textCyan; // setta // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
            { // apre // riga-ok
                testoTimerValore.color = neonGreen; // setta // riga-ok
                // blocco: controlla se va
                if (testoTimerStatus != null) // se ok // riga-ok
                { // apre // riga-ok
                    testoTimerStatus.text = "SYS_REC // SEC_02 [COUNTDOWN]"; // setta // riga-ok
                    testoTimerStatus.color = textCyan; // setta // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // ─────────────────────────────────────────────────────────────────────────
    // COSTRUZIONE GRAFICA HUD PROCEDURALE (Crisp High-DPI UI)
    // ─────────────────────────────────────────────────────────────────────────

    // blocco: funzione fa cose
    private void CostruisciHUDCompleto() // roba pub // riga-ok
    { // apre // riga-ok
        // 1. Canvas Setup
        hudCanvas = gameObject.GetComponent<Canvas>(); // setta // riga-ok
        // blocco: controlla se va
        if (hudCanvas == null) hudCanvas = gameObject.AddComponent<Canvas>(); // se ok // riga-ok
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay; // setta // riga-ok
        hudCanvas.sortingOrder = 99; // setta // riga-ok

        hudScaler = gameObject.GetComponent<CanvasScaler>(); // setta // riga-ok
        // blocco: controlla se va
        if (hudScaler == null) hudScaler = gameObject.AddComponent<CanvasScaler>(); // se ok // riga-ok
        hudScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // setta // riga-ok
        hudScaler.referenceResolution = new Vector2(1920, 1080); // setta // riga-ok
        hudScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // setta // riga-ok
        hudScaler.matchWidthOrHeight = 1.0f; // Fissa l'altezza per evitare che l'interfaccia esca dallo schermo in finestre larghe o Free Aspect // setta // riga-ok
        hudScaler.dynamicPixelsPerUnit = 3.0f; // setta // riga-ok

        // blocco: controlla se va
        if (gameObject.GetComponent<GraphicRaycaster>() == null) // se ok // riga-ok
            gameObject.AddComponent<GraphicRaycaster>(); // chiama // riga-ok

        Font defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 16); // setta // riga-ok

        Sprite solidSprite = CreaSpriteSolido(); // setta // riga-ok
        Sprite borderSprite = CreaSpriteCorniceTech(); // setta // riga-ok

        // 2. Barra Vita Batteria LCD (Posizionata con margine di sicurezza 50px da sinistra e dal basso)
        CostruisciBarraVitaLCD(solidSprite, borderSprite, defaultFont); // chiama // riga-ok

        // 3. Mirino Visore Robot (Al centro dello schermo)
        CostruisciMirinoVisore(solidSprite, defaultFont); // chiama // riga-ok

        // 4. Prompt di Prossimità [E] (In basso al centro, subito sotto il mirino)
        CostruisciPromptProssimita(solidSprite, borderSprite, defaultFont); // chiama // riga-ok

        // 5. Banner Notifica Acquisizione Keycard (In alto al centro)
        CostruisciBannerNotifica(solidSprite, borderSprite, defaultFont); // chiama // riga-ok

        // 6. Cyber Mission Timer Panel (In alto a destra, coerente con lo stile neon del visore)
        CostruisciTimerVisore(solidSprite, borderSprite, defaultFont); // chiama // riga-ok

        // 7. Flash Visivo Impatto Danno Schermo
        CostruisciDamageFlash(solidSprite); // chiama // riga-ok

        // 8. Bottone ingranaggio + tutorial testuale del gioco
        CostruisciTutorialHUD(solidSprite, borderSprite, defaultFont); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void CostruisciTutorialHUD(Sprite solid, Sprite border, Font font) // roba pub // riga-ok
    { // apre // riga-ok
        GameObject buttonGO = new GameObject("HUD_Tutorial_GearButton"); // setta // riga-ok
        buttonGO.transform.SetParent(transform, false); // chiama // riga-ok
        tutorialButtonContainer = buttonGO.AddComponent<RectTransform>(); // setta // riga-ok
        tutorialButtonContainer.anchorMin = new Vector2(1f, 1f); // setta // riga-ok
        tutorialButtonContainer.anchorMax = new Vector2(1f, 1f); // setta // riga-ok
        tutorialButtonContainer.pivot = new Vector2(1f, 1f); // setta // riga-ok
        tutorialButtonContainer.anchoredPosition = new Vector2(-340f, -34f); // setta // riga-ok
        tutorialButtonContainer.sizeDelta = new Vector2(58f, 58f); // setta // riga-ok

        Image buttonBg = buttonGO.AddComponent<Image>(); // setta // riga-ok
        buttonBg.sprite = border; // setta // riga-ok
        buttonBg.type = Image.Type.Sliced; // setta // riga-ok
        buttonBg.color = new Color(0.02f, 0.12f, 0.13f, 0.92f); // setta // riga-ok
        buttonBg.raycastTarget = true; // setta // riga-ok

        Button button = buttonGO.AddComponent<Button>(); // setta // riga-ok
        button.targetGraphic = buttonBg; // setta // riga-ok
        button.onClick.AddListener(ToggleTutorialPanel); // chiama // riga-ok

        GameObject iconGO = new GameObject("Gear_Icon"); // setta // riga-ok
        iconGO.transform.SetParent(tutorialButtonContainer, false); // chiama // riga-ok
        Text iconText = iconGO.AddComponent<Text>(); // setta // riga-ok
        iconText.font = font; // setta // riga-ok
        iconText.text = "⚙"; // setta // riga-ok
        iconText.fontSize = 32; // setta // riga-ok
        iconText.fontStyle = FontStyle.Bold; // setta // riga-ok
        iconText.alignment = TextAnchor.MiddleCenter; // setta // riga-ok
        iconText.color = textCyan; // setta // riga-ok
        iconText.raycastTarget = false; // setta // riga-ok
        RectTransform iconRect = iconGO.GetComponent<RectTransform>(); // setta // riga-ok
        iconRect.anchorMin = Vector2.zero; // setta // riga-ok
        iconRect.anchorMax = Vector2.one; // setta // riga-ok
        iconRect.offsetMin = Vector2.zero; // setta // riga-ok
        iconRect.offsetMax = Vector2.zero; // setta // riga-ok

        GameObject panelGO = new GameObject("HUD_Tutorial_Panel"); // setta // riga-ok
        panelGO.transform.SetParent(transform, false); // chiama // riga-ok
        tutorialPanel = panelGO.AddComponent<RectTransform>(); // setta // riga-ok
        tutorialPanel.anchorMin = new Vector2(0.5f, 0.5f); // setta // riga-ok
        tutorialPanel.anchorMax = new Vector2(0.5f, 0.5f); // setta // riga-ok
        tutorialPanel.pivot = new Vector2(0.5f, 0.5f); // setta // riga-ok
        tutorialPanel.anchoredPosition = Vector2.zero; // setta // riga-ok
        tutorialPanel.sizeDelta = new Vector2(820f, 560f); // setta // riga-ok

        Image panelBg = panelGO.AddComponent<Image>(); // setta // riga-ok
        panelBg.sprite = border; // setta // riga-ok
        panelBg.type = Image.Type.Sliced; // setta // riga-ok
        panelBg.color = new Color(0.01f, 0.05f, 0.055f, 0.96f); // setta // riga-ok
        panelBg.raycastTarget = true; // setta // riga-ok
        tutorialPanelGroup = panelGO.AddComponent<CanvasGroup>(); // setta // riga-ok
        tutorialPanelGroup.alpha = 0f; // setta // riga-ok
        tutorialPanelGroup.interactable = false; // setta // riga-ok
        tutorialPanelGroup.blocksRaycasts = false; // setta // riga-ok

        GameObject titleGO = new GameObject("Tutorial_Title"); // setta // riga-ok
        titleGO.transform.SetParent(tutorialPanel, false); // chiama // riga-ok
        Text titleText = titleGO.AddComponent<Text>(); // setta // riga-ok
        titleText.font = font; // setta // riga-ok
        titleText.text = "TUTORIAL OPERATORE // COME SI GIOCA"; // setta // riga-ok
        titleText.fontSize = 28; // setta // riga-ok
        titleText.fontStyle = FontStyle.Bold; // setta // riga-ok
        titleText.alignment = TextAnchor.MiddleCenter; // setta // riga-ok
        titleText.color = textCyan; // setta // riga-ok
        titleText.raycastTarget = false; // setta // riga-ok
        RectTransform titleRect = titleGO.GetComponent<RectTransform>(); // setta // riga-ok
        titleRect.anchorMin = new Vector2(0f, 1f); // setta // riga-ok
        titleRect.anchorMax = new Vector2(1f, 1f); // setta // riga-ok
        titleRect.pivot = new Vector2(0.5f, 1f); // setta // riga-ok
        titleRect.anchoredPosition = new Vector2(0f, -28f); // setta // riga-ok
        titleRect.sizeDelta = new Vector2(-52f, 46f); // setta // riga-ok

        GameObject bodyGO = new GameObject("Tutorial_Body"); // setta // riga-ok
        bodyGO.transform.SetParent(tutorialPanel, false); // chiama // riga-ok
        Text bodyText = bodyGO.AddComponent<Text>(); // setta // riga-ok
        bodyText.font = font; // setta // riga-ok
        bodyText.text =
            "1. OBIETTIVO\\n" +
            "   Ripristina il settore in emergenza, trova le keycard e completa le procedure prima che il timer arrivi a zero.\\n\\n" +
            "2. ESPLORAZIONE\\n" +
            "   Muoviti nei settori, osserva gli indizi luminosi e avvicinati agli oggetti interattivi quando compare il prompt [E].\\n\\n" +
            "3. INTERAZIONI\\n" +
            "   Usa i terminali, recupera strumenti e ripara i sistemi segnalati dalla HUD. Le keycard sbloccano nuove zone.\\n\\n" +
            "4. PERICOLO\\n" +
            "   Tieni d'occhio batteria/vita e countdown. Se subisci danni la HUD lampeggia, quindi cerca riparo o cambia percorso.\\n\\n" +
            "5. COMANDI RAPIDI\\n" +
            "   WASD: movimento | Mouse: visuale | E: interagisci | Q: scanner | ESC/M: pausa"; // setta // riga-ok
        bodyText.fontSize = 20; // setta // riga-ok
        bodyText.lineSpacing = 1.12f; // setta // riga-ok
        bodyText.alignment = TextAnchor.UpperLeft; // setta // riga-ok
        bodyText.color = new Color(0.78f, 1f, 0.95f, 1f); // setta // riga-ok
        bodyText.raycastTarget = false; // setta // riga-ok
        RectTransform bodyRect = bodyGO.GetComponent<RectTransform>(); // setta // riga-ok
        bodyRect.anchorMin = new Vector2(0f, 0f); // setta // riga-ok
        bodyRect.anchorMax = new Vector2(1f, 1f); // setta // riga-ok
        bodyRect.offsetMin = new Vector2(48f, 104f); // setta // riga-ok
        bodyRect.offsetMax = new Vector2(-48f, -96f); // setta // riga-ok

        GameObject closeGO = new GameObject("Tutorial_Close_Button"); // setta // riga-ok
        closeGO.transform.SetParent(tutorialPanel, false); // chiama // riga-ok
        RectTransform closeRect = closeGO.AddComponent<RectTransform>(); // setta // riga-ok
        closeRect.anchorMin = new Vector2(0.5f, 0f); // setta // riga-ok
        closeRect.anchorMax = new Vector2(0.5f, 0f); // setta // riga-ok
        closeRect.pivot = new Vector2(0.5f, 0f); // setta // riga-ok
        closeRect.anchoredPosition = new Vector2(0f, 28f); // setta // riga-ok
        closeRect.sizeDelta = new Vector2(260f, 54f); // setta // riga-ok
        Image closeBg = closeGO.AddComponent<Image>(); // setta // riga-ok
        closeBg.sprite = border; // setta // riga-ok
        closeBg.type = Image.Type.Sliced; // setta // riga-ok
        closeBg.color = new Color(0.03f, 0.16f, 0.14f, 0.95f); // setta // riga-ok
        closeBg.raycastTarget = true; // setta // riga-ok
        Button closeButton = closeGO.AddComponent<Button>(); // setta // riga-ok
        closeButton.targetGraphic = closeBg; // setta // riga-ok
        closeButton.onClick.AddListener(ChiudiTutorialPanel); // chiama // riga-ok

        GameObject closeLabelGO = new GameObject("Close_Label"); // setta // riga-ok
        closeLabelGO.transform.SetParent(closeRect, false); // chiama // riga-ok
        Text closeLabel = closeLabelGO.AddComponent<Text>(); // setta // riga-ok
        closeLabel.font = font; // setta // riga-ok
        closeLabel.text = "CHIUDI TUTORIAL"; // setta // riga-ok
        closeLabel.fontSize = 18; // setta // riga-ok
        closeLabel.fontStyle = FontStyle.Bold; // setta // riga-ok
        closeLabel.alignment = TextAnchor.MiddleCenter; // setta // riga-ok
        closeLabel.color = textCyan; // setta // riga-ok
        closeLabel.raycastTarget = false; // setta // riga-ok
        RectTransform closeLabelRect = closeLabelGO.GetComponent<RectTransform>(); // setta // riga-ok
        closeLabelRect.anchorMin = Vector2.zero; // setta // riga-ok
        closeLabelRect.anchorMax = Vector2.one; // setta // riga-ok
        closeLabelRect.offsetMin = Vector2.zero; // setta // riga-ok
        closeLabelRect.offsetMax = Vector2.zero; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: apre/chiude tutorial
    private void ToggleTutorialPanel() // roba pub // riga-ok
    { // apre // riga-ok
        if (tutorialAperto) // se ok // riga-ok
        { // apre // riga-ok
            ChiudiTutorialPanel(); // chiama // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        ApriTutorialPanel(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: mostra tutorial
    private void ApriTutorialPanel() // roba pub // riga-ok
    { // apre // riga-ok
        if (tutorialPanelGroup == null) return; // se ok // riga-ok
        if (!ModalUIState.TryOpen(TutorialModalOwner)) return; // se ok // riga-ok
        tutorialAperto = true; // setta // riga-ok
        tutorialPanelGroup.alpha = 1f; // setta // riga-ok
        tutorialPanelGroup.interactable = true; // setta // riga-ok
        tutorialPanelGroup.blocksRaycasts = true; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: nasconde tutorial
    private void ChiudiTutorialPanel() // roba pub // riga-ok
    { // apre // riga-ok
        if (tutorialPanelGroup == null) return; // se ok // riga-ok
        tutorialAperto = false; // setta // riga-ok
        tutorialPanelGroup.alpha = 0f; // setta // riga-ok
        tutorialPanelGroup.interactable = false; // setta // riga-ok
        tutorialPanelGroup.blocksRaycasts = false; // setta // riga-ok
        ModalUIState.Close(TutorialModalOwner); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void CostruisciBarraVitaLCD(Sprite solid, Sprite border, Font font) // roba pub // riga-ok
    { // apre // riga-ok
        GameObject panelGO = new GameObject("HealthBattery_Panel"); // setta // riga-ok
        panelGO.transform.SetParent(transform, false); // chiama // riga-ok
        batteryContainer = panelGO.AddComponent<RectTransform>(); // setta // riga-ok
        batteryContainer.anchorMin = new Vector2(0f, 0f); // setta // riga-ok
        batteryContainer.anchorMax = new Vector2(0f, 0f); // setta // riga-ok
        batteryContainer.pivot = new Vector2(0f, 0f); // setta // riga-ok
        batteryContainer.anchoredPosition = new Vector2(150f, 100f); // setta // riga-ok
        batteryContainer.sizeDelta = new Vector2(360f, 102f); // setta // riga-ok

        // Sfondo dark glass con contorno verde
        Image bg = panelGO.AddComponent<Image>(); // setta // riga-ok
        bg.sprite = border; // setta // riga-ok
        bg.type = Image.Type.Sliced; // setta // riga-ok
        bg.color = darkGlass; // setta // riga-ok
        bg.raycastTarget = false; // setta // riga-ok

        // Intestazione Batteria LCD
        GameObject labelGO = new GameObject("Battery_Label"); // setta // riga-ok
        labelGO.transform.SetParent(batteryContainer, false); // chiama // riga-ok
        Text lbl = labelGO.AddComponent<Text>(); // setta // riga-ok
        lbl.font = font; // setta // riga-ok
        lbl.fontSize = 16; // setta // riga-ok
        lbl.fontStyle = FontStyle.Bold; // setta // riga-ok
        lbl.alignment = TextAnchor.MiddleLeft; // setta // riga-ok
        lbl.text = "⚡ POWER_CORE // CELL_STATUS"; // setta // riga-ok
        lbl.color = textCyan; // setta // riga-ok
        lbl.horizontalOverflow = HorizontalWrapMode.Overflow; // setta // riga-ok
        lbl.raycastTarget = false; // setta // riga-ok

        RectTransform lblRT = labelGO.GetComponent<RectTransform>(); // setta // riga-ok
        lblRT.anchorMin = new Vector2(0f, 1f); // setta // riga-ok
        lblRT.anchorMax = new Vector2(1f, 1f); // setta // riga-ok
        lblRT.pivot = new Vector2(0f, 1f); // setta // riga-ok
        lblRT.anchoredPosition = new Vector2(18f, -10f); // setta // riga-ok
        lblRT.sizeDelta = new Vector2(324f, 22f); // setta // riga-ok

        // Contenitore segmenti batteria LCD
        GameObject segContainerGO = new GameObject("Segments_Container"); // setta // riga-ok
        segContainerGO.transform.SetParent(batteryContainer, false); // chiama // riga-ok
        RectTransform segRT = segContainerGO.AddComponent<RectTransform>(); // setta // riga-ok
        segRT.anchorMin = new Vector2(0f, 0f); // setta // riga-ok
        segRT.anchorMax = new Vector2(1f, 0f); // setta // riga-ok
        segRT.pivot = new Vector2(0.5f, 0f); // setta // riga-ok
        segRT.anchoredPosition = new Vector2(1.0f, 35f); // setta // riga-ok
        segRT.sizeDelta = new Vector2(325f, 25f); // setta // riga-ok

        HorizontalLayoutGroup hlg = segContainerGO.AddComponent<HorizontalLayoutGroup>(); // setta // riga-ok
        hlg.spacing = 3.5f; // setta // riga-ok
        hlg.childAlignment = TextAnchor.MiddleLeft; // setta // riga-ok
        hlg.childControlWidth = true; // setta // riga-ok
        hlg.childControlHeight = true; // setta // riga-ok
        hlg.childForceExpandWidth = true; // setta // riga-ok
        hlg.childForceExpandHeight = true; // setta // riga-ok
        hlg.padding = new RectOffset(16, 16, 2, 2); // setta // riga-ok

        batterySegments = new Image[NUM_SEGMENTI]; // setta // riga-ok
        // blocco: gira piu volte
        for (int i = 0; i < NUM_SEGMENTI; i++) // ciclo x // riga-ok
        { // apre // riga-ok
            GameObject seg = new GameObject($"Cell_{i}"); // setta // riga-ok
            seg.transform.SetParent(segRT, false); // chiama // riga-ok
            Image segImg = seg.AddComponent<Image>(); // setta // riga-ok
            segImg.sprite = solid; // setta // riga-ok
            segImg.color = neonGreen; // setta // riga-ok
            segImg.raycastTarget = false; // setta // riga-ok
            batterySegments[i] = segImg; // setta // riga-ok
        } // chiude // riga-ok

        // Testo Percentuale e Valore numerico HP
        GameObject txtPGO = new GameObject("Text_Percentage"); // setta // riga-ok
        txtPGO.transform.SetParent(batteryContainer, false); // chiama // riga-ok
        testoPercentualeHP = txtPGO.AddComponent<Text>(); // setta // riga-ok
        testoPercentualeHP.font = font; // setta // riga-ok
        testoPercentualeHP.fontSize = 17; // setta // riga-ok
        testoPercentualeHP.fontStyle = FontStyle.Bold; // setta // riga-ok
        testoPercentualeHP.alignment = TextAnchor.MiddleLeft; // setta // riga-ok
        testoPercentualeHP.text = "100% [ONLINE]"; // setta // riga-ok
        testoPercentualeHP.color = neonGreen; // setta // riga-ok
        testoPercentualeHP.horizontalOverflow = HorizontalWrapMode.Overflow; // setta // riga-ok
        testoPercentualeHP.raycastTarget = false; // setta // riga-ok

        RectTransform pRT = txtPGO.GetComponent<RectTransform>(); // setta // riga-ok
        pRT.anchorMin = new Vector2(0f, 0f); // setta // riga-ok
        pRT.anchorMax = new Vector2(0.55f, 0f); // setta // riga-ok
        pRT.pivot = new Vector2(0f, 0f); // setta // riga-ok
        pRT.anchoredPosition = new Vector2(18f, 8f); // setta // riga-ok
        pRT.sizeDelta = new Vector2(160f, 22f); // setta // riga-ok

        GameObject txtDGO = new GameObject("Text_Details"); // setta // riga-ok
        txtDGO.transform.SetParent(batteryContainer, false); // chiama // riga-ok
        testoDettaglioHP = txtDGO.AddComponent<Text>(); // setta // riga-ok
        testoDettaglioHP.font = font; // setta // riga-ok
        testoDettaglioHP.fontSize = 16; // setta // riga-ok
        testoDettaglioHP.alignment = TextAnchor.MiddleRight; // setta // riga-ok
        testoDettaglioHP.text = "100 / 100 HP"; // setta // riga-ok
        testoDettaglioHP.color = new Color(0.7f, 1f, 0.8f, 0.85f); // setta // riga-ok
        testoDettaglioHP.horizontalOverflow = HorizontalWrapMode.Overflow; // setta // riga-ok
        testoDettaglioHP.raycastTarget = false; // setta // riga-ok

        RectTransform dRT = txtDGO.GetComponent<RectTransform>(); // setta // riga-ok
        dRT.anchorMin = new Vector2(0.45f, 0f); // setta // riga-ok
        dRT.anchorMax = new Vector2(1f, 0f); // setta // riga-ok
        dRT.pivot = new Vector2(1f, 0f); // setta // riga-ok
        dRT.anchoredPosition = new Vector2(-18f, 8f); // setta // riga-ok
        dRT.sizeDelta = new Vector2(160f, 22f); // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void CostruisciMirinoVisore(Sprite solid, Font font) // roba pub // riga-ok
    { // apre // riga-ok
        GameObject reticleGO = new GameObject("RobotVisor_Reticle"); // setta // riga-ok
        reticleGO.transform.SetParent(transform, false); // chiama // riga-ok
        visorReticleContainer = reticleGO.AddComponent<RectTransform>(); // setta // riga-ok
        visorReticleContainer.anchorMin = new Vector2(0.5f, 0.5f); // setta // riga-ok
        visorReticleContainer.anchorMax = new Vector2(0.5f, 0.5f); // setta // riga-ok
        visorReticleContainer.pivot = new Vector2(0.5f, 0.5f); // setta // riga-ok
        visorReticleContainer.anchoredPosition = Vector2.zero; // setta // riga-ok
        visorReticleContainer.sizeDelta = new Vector2(80f, 80f); // setta // riga-ok

        // Punto centrale
        GameObject dotGO = new GameObject("Center_Dot"); // setta // riga-ok
        dotGO.transform.SetParent(visorReticleContainer, false); // chiama // riga-ok
        reticleCenterDot = dotGO.AddComponent<Image>(); // setta // riga-ok
        reticleCenterDot.sprite = solid; // setta // riga-ok
        reticleCenterDot.color = new Color(0.4f, 0.95f, 1f, 0.85f); // setta // riga-ok
        RectTransform dotRT = dotGO.GetComponent<RectTransform>(); // setta // riga-ok
        dotRT.sizeDelta = new Vector2(4f, 4f); // setta // riga-ok

        // Anello tech rotante con tick
        GameObject ringGO = new GameObject("Tech_Ring"); // setta // riga-ok
        ringGO.transform.SetParent(visorReticleContainer, false); // chiama // riga-ok
        reticleRing = ringGO.AddComponent<RectTransform>(); // setta // riga-ok
        reticleRing.sizeDelta = new Vector2(46f, 46f); // setta // riga-ok

        // 4 tacche cardinali sull'anello
        // blocco: gira piu volte
        for (int i = 0; i < 4; i++) // ciclo x // riga-ok
        { // apre // riga-ok
            GameObject tick = new GameObject($"Tick_{i}"); // setta // riga-ok
            tick.transform.SetParent(reticleRing, false); // chiama // riga-ok
            Image tImg = tick.AddComponent<Image>(); // setta // riga-ok
            tImg.sprite = solid; // setta // riga-ok
            tImg.color = new Color(0.4f, 0.95f, 1f, 0.6f); // setta // riga-ok
            RectTransform tRT = tick.GetComponent<RectTransform>(); // setta // riga-ok
            tRT.sizeDelta = new Vector2(2f, 6f); // setta // riga-ok
            float angle = i * 90f; // setta // riga-ok
            tRT.localRotation = Quaternion.Euler(0f, 0f, angle); // setta // riga-ok
            tRT.anchoredPosition = Quaternion.Euler(0f, 0f, angle) * new Vector2(0f, 20f); // setta // riga-ok
        } // chiude // riga-ok

        // 4 Angoli Bracket Cybernetici [   ]
        reticleBrackets = new Image[4]; // setta // riga-ok
        Vector2[] bracketOffsets = new Vector2[] // setta // riga-ok
        { // apre // riga-ok
            new Vector2(-32f, 32f),  // Top Left // ok qua // riga-ok
            new Vector2(32f, 32f),   // Top Right // ok qua // riga-ok
            new Vector2(-32f, -32f), // Bottom Left // ok qua // riga-ok
            new Vector2(32f, -32f)   // Bottom Right // ok qua // riga-ok
        }; // ok qua // riga-ok

        // blocco: gira piu volte
        for (int i = 0; i < 4; i++) // ciclo x // riga-ok
        { // apre // riga-ok
            GameObject bGO = new GameObject($"Bracket_{i}"); // setta // riga-ok
            bGO.transform.SetParent(visorReticleContainer, false); // chiama // riga-ok
            reticleBrackets[i] = bGO.AddComponent<Image>(); // setta // riga-ok
            reticleBrackets[i].sprite = solid; // setta // riga-ok
            reticleBrackets[i].color = new Color(0.4f, 0.95f, 1f, 0.7f); // setta // riga-ok
            RectTransform bRT = bGO.GetComponent<RectTransform>(); // setta // riga-ok
            bRT.sizeDelta = new Vector2(7f, 2f); // setta // riga-ok
            bRT.anchoredPosition = bracketOffsets[i]; // setta // riga-ok
        } // chiude // riga-ok

        // Testo stato Scanner / Lock
        GameObject statusGO = new GameObject("Scan_Status"); // setta // riga-ok
        statusGO.transform.SetParent(visorReticleContainer, false); // chiama // riga-ok
        reticleStatusText = statusGO.AddComponent<Text>(); // setta // riga-ok
        reticleStatusText.font = font; // setta // riga-ok
        reticleStatusText.fontSize = 14; // setta // riga-ok
        reticleStatusText.fontStyle = FontStyle.Bold; // setta // riga-ok
        reticleStatusText.alignment = TextAnchor.MiddleCenter; // setta // riga-ok
        reticleStatusText.text = ""; // setta // riga-ok
        reticleStatusText.color = neonGreen; // setta // riga-ok
        reticleStatusText.horizontalOverflow = HorizontalWrapMode.Overflow; // setta // riga-ok
        RectTransform stRT = statusGO.GetComponent<RectTransform>(); // setta // riga-ok
        stRT.anchoredPosition = new Vector2(0f, -48f); // setta // riga-ok
        stRT.sizeDelta = new Vector2(200f, 24f); // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void CostruisciPromptProssimita(Sprite solid, Sprite border, Font font) // roba pub // riga-ok
    { // apre // riga-ok
        GameObject promptGO = new GameObject("Interaction_PromptPanel"); // setta // riga-ok
        promptGO.transform.SetParent(transform, false); // chiama // riga-ok
        promptPanel = promptGO.AddComponent<RectTransform>(); // setta // riga-ok
        promptPanel.anchorMin = new Vector2(0.5f, 0.5f); // setta // riga-ok
        promptPanel.anchorMax = new Vector2(0.5f, 0.5f); // setta // riga-ok
        promptPanel.pivot = new Vector2(0.5f, 1f); // setta // riga-ok
        promptPanel.anchoredPosition = new Vector2(0f, -68f); // setta // riga-ok
        promptPanel.sizeDelta = new Vector2(480f, 88f); // setta // riga-ok

        promptCanvasGroup = promptGO.AddComponent<CanvasGroup>(); // setta // riga-ok
        promptCanvasGroup.alpha = 0f; // setta // riga-ok

        Image bg = promptGO.AddComponent<Image>(); // setta // riga-ok
        bg.sprite = border; // setta // riga-ok
        bg.type = Image.Type.Sliced; // setta // riga-ok
        bg.color = darkGlass; // setta // riga-ok

        // Badge Tasto [ E ]
        GameObject badgeGO = new GameObject("KeyBadge_E"); // setta // riga-ok
        badgeGO.transform.SetParent(promptPanel, false); // chiama // riga-ok
        promptKeyBadge = badgeGO.AddComponent<Image>(); // setta // riga-ok
        promptKeyBadge.sprite = border; // setta // riga-ok
        promptKeyBadge.type = Image.Type.Sliced; // setta // riga-ok
        promptKeyBadge.color = new Color(0.08f, 0.35f, 0.15f, 0.95f); // setta // riga-ok
        RectTransform badgeRT = badgeGO.GetComponent<RectTransform>(); // setta // riga-ok
        badgeRT.anchorMin = new Vector2(0f, 0.5f); // setta // riga-ok
        badgeRT.anchorMax = new Vector2(0f, 0.5f); // setta // riga-ok
        badgeRT.pivot = new Vector2(0.5f, 0.5f); // setta // riga-ok
        badgeRT.anchoredPosition = new Vector2(46f, 0f); // setta // riga-ok
        badgeRT.sizeDelta = new Vector2(50f, 50f); // setta // riga-ok

        GameObject keyTxtGO = new GameObject("KeyText"); // setta // riga-ok
        keyTxtGO.transform.SetParent(badgeGO.transform, false); // chiama // riga-ok
        promptKeyText = keyTxtGO.AddComponent<Text>(); // setta // riga-ok
        promptKeyText.font = font; // setta // riga-ok
        promptKeyText.fontSize = 26; // setta // riga-ok
        promptKeyText.fontStyle = FontStyle.Bold; // setta // riga-ok
        promptKeyText.alignment = TextAnchor.MiddleCenter; // setta // riga-ok
        promptKeyText.text = "E"; // setta // riga-ok
        promptKeyText.color = Color.white; // setta // riga-ok
        RectTransform ktRT = keyTxtGO.GetComponent<RectTransform>(); // setta // riga-ok
        ktRT.anchorMin = Vector2.zero; // setta // riga-ok
        ktRT.anchorMax = Vector2.one; // setta // riga-ok
        ktRT.sizeDelta = Vector2.zero; // setta // riga-ok

        // Titolo Oggetto
        GameObject titleGO = new GameObject("Prompt_Title"); // setta // riga-ok
        titleGO.transform.SetParent(promptPanel, false); // chiama // riga-ok
        promptTitleText = titleGO.AddComponent<Text>(); // setta // riga-ok
        promptTitleText.font = font; // setta // riga-ok
        promptTitleText.fontSize = 17; // setta // riga-ok
        promptTitleText.fontStyle = FontStyle.Bold; // setta // riga-ok
        promptTitleText.alignment = TextAnchor.MiddleLeft; // setta // riga-ok
        promptTitleText.text = "AUTORIZZAZIONE: KEYCARD_A02"; // setta // riga-ok
        promptTitleText.color = neonGreen; // setta // riga-ok
        promptTitleText.horizontalOverflow = HorizontalWrapMode.Overflow; // setta // riga-ok
        RectTransform tRT = titleGO.GetComponent<RectTransform>(); // setta // riga-ok
        tRT.anchorMin = new Vector2(0f, 0.5f); // setta // riga-ok
        tRT.anchorMax = new Vector2(1f, 0.5f); // setta // riga-ok
        tRT.pivot = new Vector2(0f, 0.5f); // setta // riga-ok
        tRT.anchoredPosition = new Vector2(84f, 16f); // setta // riga-ok
        tRT.sizeDelta = new Vector2(380f, 26f); // setta // riga-ok

        // Azione / Cosa sistemare
        GameObject actGO = new GameObject("Prompt_Action"); // setta // riga-ok
        actGO.transform.SetParent(promptPanel, false); // chiama // riga-ok
        promptActionText = actGO.AddComponent<Text>(); // setta // riga-ok
        promptActionText.font = font; // setta // riga-ok
        promptActionText.fontSize = 15; // setta // riga-ok
        promptActionText.alignment = TextAnchor.MiddleLeft; // setta // riga-ok
        promptActionText.text = "Premi [E] per Raccogliere Scheda di Accesso"; // setta // riga-ok
        promptActionText.color = textCyan; // setta // riga-ok
        promptActionText.horizontalOverflow = HorizontalWrapMode.Overflow; // setta // riga-ok
        RectTransform aRT = actGO.GetComponent<RectTransform>(); // setta // riga-ok
        aRT.anchorMin = new Vector2(0f, 0.5f); // setta // riga-ok
        aRT.anchorMax = new Vector2(1f, 0.5f); // setta // riga-ok
        aRT.pivot = new Vector2(0f, 0.5f); // setta // riga-ok
        aRT.anchoredPosition = new Vector2(84f, -15f); // setta // riga-ok
        aRT.sizeDelta = new Vector2(380f, 24f); // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void CostruisciBannerNotifica(Sprite solid, Sprite border, Font font) // roba pub // riga-ok
    { // apre // riga-ok
        GameObject notificaGO = new GameObject("Notification_Banner"); // setta // riga-ok
        notificaGO.transform.SetParent(transform, false); // chiama // riga-ok
        notificaPanel = notificaGO.AddComponent<RectTransform>(); // setta // riga-ok
        notificaPanel.anchorMin = new Vector2(0.5f, 1f); // setta // riga-ok
        notificaPanel.anchorMax = new Vector2(0.5f, 1f); // setta // riga-ok
        notificaPanel.pivot = new Vector2(0.5f, 1f); // setta // riga-ok
        notificaPanel.anchoredPosition = new Vector2(0f, -25f); // setta // riga-ok
        notificaPanel.sizeDelta = new Vector2(580f, 80f); // setta // riga-ok

        notificaCanvasGroup = notificaGO.AddComponent<CanvasGroup>(); // setta // riga-ok
        notificaCanvasGroup.alpha = 0f; // setta // riga-ok

        Image bg = notificaGO.AddComponent<Image>(); // setta // riga-ok
        bg.sprite = border; // setta // riga-ok
        bg.type = Image.Type.Sliced; // setta // riga-ok
        bg.color = darkGlass; // setta // riga-ok

        // Titolo Notifica
        GameObject tGO = new GameObject("Notifica_Title"); // setta // riga-ok
        tGO.transform.SetParent(notificaPanel, false); // chiama // riga-ok
        notificaTitleText = tGO.AddComponent<Text>(); // setta // riga-ok
        notificaTitleText.font = font; // setta // riga-ok
        notificaTitleText.fontSize = 19; // setta // riga-ok
        notificaTitleText.fontStyle = FontStyle.Bold; // setta // riga-ok
        notificaTitleText.alignment = TextAnchor.MiddleCenter; // setta // riga-ok
        notificaTitleText.text = "AUTORIZZAZIONE ACQUISITA"; // setta // riga-ok
        notificaTitleText.color = neonGreen; // setta // riga-ok
        notificaTitleText.horizontalOverflow = HorizontalWrapMode.Overflow; // setta // riga-ok
        RectTransform ntRT = tGO.GetComponent<RectTransform>(); // setta // riga-ok
        ntRT.anchorMin = new Vector2(0f, 0.5f); // setta // riga-ok
        ntRT.anchorMax = new Vector2(1f, 0.5f); // setta // riga-ok
        ntRT.pivot = new Vector2(0.5f, 0.5f); // setta // riga-ok
        ntRT.anchoredPosition = new Vector2(0f, 14f); // setta // riga-ok
        ntRT.sizeDelta = new Vector2(550f, 28f); // setta // riga-ok

        // Sottotitolo / Dettagli
        GameObject sGO = new GameObject("Notifica_Sub"); // setta // riga-ok
        sGO.transform.SetParent(notificaPanel, false); // chiama // riga-ok
        notificaSubText = sGO.AddComponent<Text>(); // setta // riga-ok
        notificaSubText.font = font; // setta // riga-ok
        notificaSubText.fontSize = 15; // setta // riga-ok
        notificaSubText.alignment = TextAnchor.MiddleCenter; // setta // riga-ok
        notificaSubText.text = "KEYCARD_A02 // ACCESSO AL SETTORE AGGIORNATO"; // setta // riga-ok
        notificaSubText.color = textCyan; // setta // riga-ok
        notificaSubText.horizontalOverflow = HorizontalWrapMode.Overflow; // setta // riga-ok
        RectTransform nsRT = sGO.GetComponent<RectTransform>(); // setta // riga-ok
        nsRT.anchorMin = new Vector2(0f, 0.5f); // setta // riga-ok
        nsRT.anchorMax = new Vector2(1f, 0.5f); // setta // riga-ok
        nsRT.pivot = new Vector2(0.5f, 0.5f); // setta // riga-ok
        nsRT.anchoredPosition = new Vector2(0f, -15f); // setta // riga-ok
        nsRT.sizeDelta = new Vector2(550f, 24f); // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void CostruisciTimerVisore(Sprite solid, Sprite border, Font font) // roba pub // riga-ok
    { // apre // riga-ok
        GameObject panelGO = new GameObject("CyberTimer_Panel"); // setta // riga-ok
        panelGO.transform.SetParent(transform, false); // chiama // riga-ok
        timerContainer = panelGO.AddComponent<RectTransform>(); // setta // riga-ok
        timerContainer.anchorMin = new Vector2(1f, 1f); // setta // riga-ok
        timerContainer.anchorMax = new Vector2(1f, 1f); // setta // riga-ok
        timerContainer.pivot = new Vector2(1f, 1f); // setta // riga-ok
        timerContainer.anchoredPosition = new Vector2(-60f, -40f); // setta // riga-ok
        timerContainer.sizeDelta = new Vector2(250f, 92f); // setta // riga-ok

        // Sfondo dark glass con contorno verde/cianotico
        Image bg = panelGO.AddComponent<Image>(); // setta // riga-ok
        bg.sprite = border; // setta // riga-ok
        bg.type = Image.Type.Sliced; // setta // riga-ok
        bg.color = darkGlass; // setta // riga-ok
        bg.raycastTarget = false; // setta // riga-ok

        // Intestazione Timer LCD
        GameObject labelGO = new GameObject("Timer_Label"); // setta // riga-ok
        labelGO.transform.SetParent(timerContainer, false); // chiama // riga-ok
        Text lbl = labelGO.AddComponent<Text>(); // setta // riga-ok
        lbl.font = font; // setta // riga-ok
        lbl.fontSize = 14; // setta // riga-ok
        lbl.fontStyle = FontStyle.Bold; // setta // riga-ok
        lbl.alignment = TextAnchor.MiddleLeft; // setta // riga-ok
        lbl.text = "⏳ COUNTDOWN // T-MINUS"; // setta // riga-ok
        lbl.color = textCyan; // setta // riga-ok
        lbl.horizontalOverflow = HorizontalWrapMode.Overflow; // setta // riga-ok
        lbl.raycastTarget = false; // setta // riga-ok

        RectTransform lblRT = labelGO.GetComponent<RectTransform>(); // setta // riga-ok
        lblRT.anchorMin = new Vector2(0f, 1f); // setta // riga-ok
        lblRT.anchorMax = new Vector2(1f, 1f); // setta // riga-ok
        lblRT.pivot = new Vector2(0f, 1f); // setta // riga-ok
        lblRT.anchoredPosition = new Vector2(16f, -10f); // setta // riga-ok
        lblRT.sizeDelta = new Vector2(220f, 20f); // setta // riga-ok

        // Testo Digitale Orologio Timer (Grande Verde Neon)
        GameObject valGO = new GameObject("Timer_Value"); // setta // riga-ok
        valGO.transform.SetParent(timerContainer, false); // chiama // riga-ok
        testoTimerValore = valGO.AddComponent<Text>(); // setta // riga-ok
        testoTimerValore.font = font; // setta // riga-ok
        testoTimerValore.fontSize = 28; // setta // riga-ok
        testoTimerValore.fontStyle = FontStyle.Bold; // setta // riga-ok
        testoTimerValore.alignment = TextAnchor.MiddleLeft; // setta // riga-ok
        testoTimerValore.text = "00:00.0"; // setta // riga-ok
        testoTimerValore.color = neonGreen; // setta // riga-ok
        testoTimerValore.horizontalOverflow = HorizontalWrapMode.Overflow; // setta // riga-ok
        testoTimerValore.raycastTarget = false; // setta // riga-ok

        RectTransform valRT = valGO.GetComponent<RectTransform>(); // setta // riga-ok
        valRT.anchorMin = new Vector2(0f, 0.5f); // setta // riga-ok
        valRT.anchorMax = new Vector2(1f, 0.5f); // setta // riga-ok
        valRT.pivot = new Vector2(0f, 0.5f); // setta // riga-ok
        valRT.anchoredPosition = new Vector2(18f, -2f); // setta // riga-ok
        valRT.sizeDelta = new Vector2(220f, 32f); // setta // riga-ok

        // Sottotitolo / Status Timer
        GameObject stGO = new GameObject("Timer_Status"); // setta // riga-ok
        stGO.transform.SetParent(timerContainer, false); // chiama // riga-ok
        testoTimerStatus = stGO.AddComponent<Text>(); // setta // riga-ok
        testoTimerStatus.font = font; // setta // riga-ok
        testoTimerStatus.fontSize = 12; // setta // riga-ok
        testoTimerStatus.alignment = TextAnchor.MiddleLeft; // setta // riga-ok
        testoTimerStatus.text = "SYS_REC // SEC_02 [ACTIVE]"; // setta // riga-ok
        testoTimerStatus.color = textCyan; // setta // riga-ok
        testoTimerStatus.horizontalOverflow = HorizontalWrapMode.Overflow; // setta // riga-ok
        testoTimerStatus.raycastTarget = false; // setta // riga-ok

        RectTransform stRT = stGO.GetComponent<RectTransform>(); // setta // riga-ok
        stRT.anchorMin = new Vector2(0f, 0f); // setta // riga-ok
        stRT.anchorMax = new Vector2(1f, 0f); // setta // riga-ok
        stRT.pivot = new Vector2(0f, 0f); // setta // riga-ok
        stRT.anchoredPosition = new Vector2(18f, 10f); // setta // riga-ok
        stRT.sizeDelta = new Vector2(220f, 18f); // setta // riga-ok
    } // chiude // riga-ok

    // ─────────────────────────────────────────────────────────────────────────
    // AGGIORNAMENTO DINAMICO BARRA VITA LCD
    // ─────────────────────────────────────────────────────────────────────────

    // blocco: funzione fa cose
    private void OnSaluteAggiornata(float corrente, float massima) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (corrente < hpCorrenti && hpCorrenti > 0) // se ok // riga-ok
        { // apre // riga-ok
            TriggerDamageFeedback(hpCorrenti - corrente); // chiama // riga-ok
        } // chiude // riga-ok

        hpCorrenti = corrente; // setta // riga-ok
        hpMassimi = massima; // setta // riga-ok

        float ratio = Mathf.Clamp01(corrente / Mathf.Max(massima, 1f)); // setta // riga-ok
        int segmentiAttivi = Mathf.CeilToInt(ratio * NUM_SEGMENTI); // setta // riga-ok

        Color activeColor = ratio <= 0.25f ? warningRed : neonGreen; // setta // riga-ok

        // blocco: controlla se va
        if (batterySegments != null) // se ok // riga-ok
        { // apre // riga-ok
            // blocco: gira piu volte
            for (int i = 0; i < NUM_SEGMENTI; i++) // ciclo x // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (batterySegments[i] != null) // se ok // riga-ok
                { // apre // riga-ok
                    bool attivo = i < segmentiAttivi; // setta // riga-ok
                    batterySegments[i].color = attivo ? activeColor : neonGreenDim; // setta // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (testoPercentualeHP != null) // se ok // riga-ok
        { // apre // riga-ok
            int perc = Mathf.CeilToInt(ratio * 100f); // setta // riga-ok
            testoPercentualeHP.text = ratio <= 0.25f ? $"[CRITICAL {perc}%]" : $"{perc}% [ONLINE]"; // setta // riga-ok
            testoPercentualeHP.color = activeColor; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (testoDettaglioHP != null) // se ok // riga-ok
        { // apre // riga-ok
            testoDettaglioHP.text = $"{Mathf.CeilToInt(corrente)} / {Mathf.CeilToInt(massima)} HP"; // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void CostruisciDamageFlash(Sprite solid) // roba pub // riga-ok
    { // apre // riga-ok
        GameObject dfGO = new GameObject("HUD_DamageFlash"); // setta // riga-ok
        dfGO.transform.SetParent(transform, false); // chiama // riga-ok
        Image dfImg = dfGO.AddComponent<Image>(); // setta // riga-ok
        dfImg.sprite = solid; // setta // riga-ok
        dfImg.color = new Color(1f, 0.05f, 0.05f, 0.35f); // setta // riga-ok
        dfImg.raycastTarget = false; // setta // riga-ok

        damageFlashGroup = dfGO.AddComponent<CanvasGroup>(); // setta // riga-ok
        damageFlashGroup.alpha = 0f; // setta // riga-ok

        RectTransform dfRT = dfGO.GetComponent<RectTransform>(); // setta // riga-ok
        dfRT.anchorMin = Vector2.zero; // setta // riga-ok
        dfRT.anchorMax = Vector2.one; // setta // riga-ok
        dfRT.sizeDelta = Vector2.zero; // setta // riga-ok
        dfRT.anchoredPosition = Vector2.zero; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void TriggerDamageFeedback(float deltaDamage) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (damageFlashCoroutine != null) // se ok // riga-ok
            StopCoroutine(damageFlashCoroutine); // corutina // riga-ok
        damageFlashCoroutine = StartCoroutine(AnimaDamageFlash()); // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private IEnumerator AnimaDamageFlash() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (damageFlashGroup == null) yield break; // se ok // riga-ok
        damageFlashGroup.alpha = 1f; // setta // riga-ok

        float timer = 0.35f; // setta // riga-ok
        // blocco: gira piu volte
        while (timer > 0) // ciclo x // riga-ok
        { // apre // riga-ok
            timer -= Time.unscaledDeltaTime; // setta // riga-ok
            damageFlashGroup.alpha = Mathf.Clamp01(timer / 0.35f); // setta // riga-ok
            yield return null; // aspetta // riga-ok
        } // chiude // riga-ok
        damageFlashGroup.alpha = 0f; // setta // riga-ok
    } // chiude // riga-ok

    // ─────────────────────────────────────────────────────────────────────────
    // CONTROLLO VISORE ROBOTICO & PROMPT PROSSIMITÀ
    // ─────────────────────────────────────────────────────────────────────────

    // blocco: funzione fa cose
    public void SetTargetLocked(bool locked, string nomeTarget = "") // roba pub // riga-ok
    { // apre // riga-ok
        isTargetLocked = locked; // setta // riga-ok

        // blocco: controlla se va
        if (reticleCenterDot != null) // se ok // riga-ok
            reticleCenterDot.color = locked ? neonGreen : new Color(0.4f, 0.95f, 1f, 0.85f); // setta // riga-ok

        // blocco: controlla se va
        if (reticleBrackets != null) // se ok // riga-ok
        { // apre // riga-ok
            Color c = locked ? neonGreen : new Color(0.4f, 0.95f, 1f, 0.7f); // setta // riga-ok
            // blocco: gira piu volte
            foreach (var b in reticleBrackets) // ciclo x // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (b != null) b.color = c; // se ok // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (reticleStatusText != null) // se ok // riga-ok
        { // apre // riga-ok
            reticleStatusText.text = locked ? "[TARGET LOCKED]" : ""; // setta // riga-ok
            reticleStatusText.color = neonGreen; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (!locked && visorReticleContainer != null) // se ok // riga-ok
        { // apre // riga-ok
            visorReticleContainer.localScale = Vector3.one; // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void MostraPrompt(string titoloOggetto, string azioneDescrizione) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (promptTitleText != null) promptTitleText.text = titoloOggetto; // se ok // riga-ok
        // blocco: controlla se va
        if (promptActionText != null) promptActionText.text = azioneDescrizione; // se ok // riga-ok

        // blocco: controlla se va
        if (promptCanvasGroup != null) // se ok // riga-ok
        { // apre // riga-ok
            promptCanvasGroup.alpha = 1f; // setta // riga-ok
        } // chiude // riga-ok

        SetTargetLocked(true, titoloOggetto); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void NascondiPrompt() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (promptCanvasGroup != null) // se ok // riga-ok
        { // apre // riga-ok
            promptCanvasGroup.alpha = 0f; // setta // riga-ok
        } // chiude // riga-ok

        SetTargetLocked(false); // chiama // riga-ok
    } // chiude // riga-ok

    // ─────────────────────────────────────────────────────────────────────────
    // NOTIFICA OLOGRAFICA ACQUISIZIONE
    // ─────────────────────────────────────────────────────────────────────────

    // blocco: funzione fa cose
    public void MostraNotificaAcquisizione(string titolo, string dettagli) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (notificaCoroutine != null) // se ok // riga-ok
            StopCoroutine(notificaCoroutine); // corutina // riga-ok

        notificaCoroutine = StartCoroutine(AnimaNotifica(titolo, dettagli)); // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private IEnumerator AnimaNotifica(string titolo, string dettagli) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (notificaTitleText != null) notificaTitleText.text = titolo.ToUpper(); // se ok // riga-ok
        // blocco: controlla se va
        if (notificaSubText != null) notificaSubText.text = dettagli.ToUpper(); // se ok // riga-ok

        // blocco: controlla se va
        if (notificaCanvasGroup == null) yield break; // se ok // riga-ok

        // Fade in
        float t = 0f; // setta // riga-ok
        // blocco: gira piu volte
        while (t < 0.25f) // ciclo x // riga-ok
        { // apre // riga-ok
            t += Time.unscaledDeltaTime; // setta // riga-ok
            notificaCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t / 0.25f); // setta // riga-ok
            yield return null; // aspetta // riga-ok
        } // chiude // riga-ok
        notificaCanvasGroup.alpha = 1f; // setta // riga-ok

        // Mostra a schermo per 3.5 secondi
        yield return new WaitForSecondsRealtime(3.5f); // aspetta // riga-ok

        // Fade out
        t = 0f; // setta // riga-ok
        // blocco: gira piu volte
        while (t < 0.4f) // ciclo x // riga-ok
        { // apre // riga-ok
            t += Time.unscaledDeltaTime; // setta // riga-ok
            notificaCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t / 0.4f); // setta // riga-ok
            yield return null; // aspetta // riga-ok
        } // chiude // riga-ok
        notificaCanvasGroup.alpha = 0f; // setta // riga-ok
    } // chiude // riga-ok

    // ─────────────────────────────────────────────────────────────────────────
    // SPRITE GENERATORS (Cornice Neon e Texture Solida)
    // ─────────────────────────────────────────────────────────────────────────

    // blocco: funzione fa cose
    private Sprite CreaSpriteSolido() // roba pub // riga-ok
    { // apre // riga-ok
        Texture2D tex = new Texture2D(2, 2); // setta // riga-ok
        Color[] cols = new Color[] { Color.white, Color.white, Color.white, Color.white }; // setta // riga-ok
        tex.SetPixels(cols); // chiama // riga-ok
        tex.Apply(); // chiama // riga-ok
        return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f)); // torna val // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private Sprite CreaSpriteCorniceTech() // roba pub // riga-ok
    { // apre // riga-ok
        int w = 32; // setta // riga-ok
        int h = 32; // setta // riga-ok
        Texture2D tex = new Texture2D(w, h); // setta // riga-ok
        tex.filterMode = FilterMode.Point; // setta // riga-ok
        Color[] pixels = new Color[w * h]; // setta // riga-ok

        Color bg = new Color(1f, 1f, 1f, 0.12f); // setta // riga-ok
        Color border = new Color(0.2f, 1f, 0.35f, 0.95f); // setta // riga-ok
        Color corner = new Color(0.4f, 1f, 0.6f, 1f); // setta // riga-ok

        // blocco: gira piu volte
        for (int y = 0; y < h; y++) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: gira piu volte
            for (int x = 0; x < w; x++) // ciclo x // riga-ok
            { // apre // riga-ok
                bool isBorderX = (x == 0 || x == w - 1); // setta // riga-ok
                bool isBorderY = (y == 0 || y == h - 1); // setta // riga-ok
                bool isCorner = (x < 4 || x >= w - 4) && (y < 4 || y >= h - 4); // setta // riga-ok

                // blocco: controlla se va
                if (isCorner && (isBorderX || isBorderY)) // se ok // riga-ok
                    pixels[y * w + x] = corner; // setta // riga-ok
                // blocco: controlla se va
                else if (isBorderX || isBorderY) // se ok // riga-ok
                    pixels[y * w + x] = border; // setta // riga-ok
                // blocco: caso diverso
                else // se no // riga-ok
                    pixels[y * w + x] = bg; // setta // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        tex.SetPixels(pixels); // chiama // riga-ok
        tex.Apply(); // chiama // riga-ok
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(6, 6, 6, 6)); // torna val // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
