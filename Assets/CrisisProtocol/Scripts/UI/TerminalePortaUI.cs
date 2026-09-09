using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using GoldenCast.UI;

/// <summary>
/// Interfaccia grafica completa per i Terminali di Sicurezza delle Porte.
/// Include Tastierino PIN interattivo e Minigioco di Calibrazione/Bypass Circuiti a Tempo.
/// </summary>
public class TerminalePortaUI : MonoBehaviour
{
    public static TerminalePortaUI Instance { get; private set; }

    private const string ModalOwner = "TerminalePorta";

    private TerminalePorta terminaleAttivo;
    private Canvas canvasRoot;
    private GameObject pannelloPrincipale;
    private GameObject bgOverlay;

    // Elementi UI Comuni
    private Text testoTitoloTerminale;
    private Text testoStatoMessaggio;
    private GameObject tabKeypadObj;
    private GameObject tabBypassObj;
    private Button btnTabKeypad;
    private Button btnTabBypass;

    // Elementi Keypad
    private Text testoDisplayCodice;
    private string codiceDigitato = "";

    // Elementi Minigioco Bypass
    private RectTransform barraOscillatore;
    private RectTransform zonaVerdeTarget;
    private Text testoNodoProgresso;
    private Text testoIstruzioniBypass;
    private int nodoCorrente = 1;
    private int nodiTotali = 3;
    private float velocitaOscillatore = 2.0f;
    private float ampiezzaBarra = 300f; // Larghezza totale barra calibrazione
    private float zonaVerdeMin = -50f;
    private float zonaVerdeMax = 50f;
    private bool bypassInCorso = false;
    private bool bypassCompletato = false;

    private bool inAnimazioneChiusura = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        CostruisciUISeNecessario();
    }

    void Update()
    {
        if (terminaleAttivo == null || canvasRoot == null || !canvasRoot.gameObject.activeSelf || pannelloPrincipale == null || !pannelloPrincipale.activeSelf || inAnimazioneChiusura)
            return;

        // Gestione tasto ESC per chiudere
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ChiudiTerminale();
            return;
        }

        // Se la scheda attiva è il Keypad: ascolta la tastiera numerica
        if (tabKeypadObj != null && tabKeypadObj.activeSelf)
        {
            GestisciInputTastieraKeypad();
        }

        // Se la scheda attiva è il Minigioco Bypass: anima l'indicatore
        if (tabBypassObj != null && tabBypassObj.activeSelf && bypassInCorso && !bypassCompletato)
        {
            AggiornaMinigiocoBypass();
        }
    }

    public void ApriTerminale(TerminalePorta terminale)
    {
        terminaleAttivo = terminale;
        CostruisciUISeNecessario();

        if (canvasRoot != null) canvasRoot.gameObject.SetActive(true);
        if (bgOverlay != null) bgOverlay.SetActive(true);
        if (pannelloPrincipale != null) pannelloPrincipale.SetActive(true);

        codiceDigitato = "";
        AggiornaDisplayCodice();

        nodoCorrente = 1;
        bypassCompletato = false;
        bypassInCorso = true;
        ConfiguraDifficoltaNodo();

        if (testoTitoloTerminale != null)
            testoTitoloTerminale.text = terminale.nomeTerminale.ToUpper();

        if (testoStatoMessaggio != null)
        {
            testoStatoMessaggio.text = "SISTEMA DI SICUREZZA BLOCCATO - INSERISCI CODICE O BYPASSA CIRCUITO";
            testoStatoMessaggio.color = new Color(0.3f, 0.85f, 1f);
        }

        // Gestione visibilità delle tab
        if (btnTabKeypad != null) btnTabKeypad.gameObject.SetActive(terminale.consentiCodicePin);
        if (btnTabBypass != null) btnTabBypass.gameObject.SetActive(terminale.consentiBypassElettronico);

        if (terminale.consentiCodicePin)
            MostraTabKeypad();
        else if (terminale.consentiBypassElettronico)
            MostraTabBypass();

        // Blocca i movimenti di gioco e mostra il cursore del mouse
        ModalUIState.TryOpen(ModalOwner);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ChiudiTerminale()
    {
        if (inAnimazioneChiusura) return;

        if (pannelloPrincipale != null)
            pannelloPrincipale.SetActive(false);

        if (bgOverlay != null)
            bgOverlay.SetActive(false);

        if (canvasRoot != null)
            canvasRoot.gameObject.SetActive(false);

        terminaleAttivo = null;
        ModalUIState.Close(ModalOwner);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    #region Gestione Tab

    private void MostraTabKeypad()
    {
        if (tabKeypadObj != null) tabKeypadObj.SetActive(true);
        if (tabBypassObj != null) tabBypassObj.SetActive(false);
    }

    private void MostraTabBypass()
    {
        if (tabKeypadObj != null) tabKeypadObj.SetActive(false);
        if (tabBypassObj != null) tabBypassObj.SetActive(true);
        nodoCorrente = 1;
        ConfiguraDifficoltaNodo();
    }

    #endregion

    #region Logica Keypad PIN

    public void InserisciCifra(string cifra)
    {
        if (codiceDigitato.Length < 6)
        {
            codiceDigitato += cifra;
            AggiornaDisplayCodice();
        }
    }

    public void CancellaCifra()
    {
        if (codiceDigitato.Length > 0)
        {
            codiceDigitato = codiceDigitato.Substring(0, codiceDigitato.Length - 1);
            AggiornaDisplayCodice();
        }
    }

    public void ConfermaCodice()
    {
        if (terminaleAttivo == null) return;

        if (codiceDigitato == terminaleAttivo.codiceSegreto)
        {
            StartCoroutine(SequenzaSuccesso("ACCESSO AUTORIZZATO // CODICE CORRETTO"));
        }
        else
        {
            StartCoroutine(SequenzaErrore("ACCESSO NEGATO // CODICE ERRATO"));
        }
    }

    private void AggiornaDisplayCodice()
    {
        if (testoDisplayCodice == null) return;

        if (string.IsNullOrEmpty(codiceDigitato))
        {
            testoDisplayCodice.text = "- - - -";
            testoDisplayCodice.color = new Color(0.5f, 0.7f, 0.9f, 0.6f);
        }
        else
        {
            testoDisplayCodice.text = codiceDigitato;
            testoDisplayCodice.color = Color.white;
        }
    }

    private void GestisciInputTastieraKeypad()
    {
        if (Keyboard.current == null) return;

        // Cifre 0 - 9
        for (int i = 0; i <= 9; i++)
        {
            Key key = Key.Digit0 + i;
            Key numpadKey = Key.Numpad0 + i;
            if (Keyboard.current[key].wasPressedThisFrame || Keyboard.current[numpadKey].wasPressedThisFrame)
            {
                InserisciCifra(i.ToString());
            }
        }

        // Backspace / Cancella
        if (Keyboard.current.backspaceKey.wasPressedThisFrame || Keyboard.current.deleteKey.wasPressedThisFrame)
        {
            CancellaCifra();
        }

        // Enter / Conferma
        if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
        {
            ConfermaCodice();
        }
    }

    #endregion

    #region Logica Minigioco Bypass

    private void ConfiguraDifficoltaNodo()
    {
        switch (nodoCorrente)
        {
            case 1:
                velocitaOscillatore = 2.2f;
                zonaVerdeMin = -55f;
                zonaVerdeMax = 55f;
                break;
            case 2:
                velocitaOscillatore = 3.2f;
                zonaVerdeMin = -40f;
                zonaVerdeMax = 40f;
                break;
            case 3:
                velocitaOscillatore = 4.5f;
                zonaVerdeMin = -28f;
                zonaVerdeMax = 28f;
                break;
        }

        if (zonaVerdeTarget != null)
        {
            float larghezza = zonaVerdeMax - zonaVerdeMin;
            zonaVerdeTarget.sizeDelta = new Vector2(larghezza, 36f);
        }

        if (testoNodoProgresso != null)
            testoNodoProgresso.text = $"SICUREZZA CIRCUITO: NODO [{nodoCorrente}/{nodiTotali}]";

        if (testoIstruzioniBypass != null)
            testoIstruzioniBypass.text = "Premi [SPAZIO] o clicca [CALIBRA CIRCUITO] quando l'indicatore è nella ZONA VERDE!";
    }

    private void AggiornaMinigiocoBypass()
    {
        float t = Mathf.PingPong(Time.unscaledTime * velocitaOscillatore, 1f);
        float posX = Mathf.Lerp(-ampiezzaBarra / 2f, ampiezzaBarra / 2f, t);

        if (barraOscillatore != null)
        {
            barraOscillatore.anchoredPosition = new Vector2(posX, 0f);
        }

        // Ascolta il tasto Spazio per convalidare il bypass
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            TentaBypassNodo();
        }
    }

    public void TentaBypassNodo()
    {
        if (!bypassInCorso || bypassCompletato) return;

        float posizioneAttuale = barraOscillatore.anchoredPosition.x;

        // Verifica se l'indicatore si trova all'interno della zona verde
        if (posizioneAttuale >= zonaVerdeMin && posizioneAttuale <= zonaVerdeMax)
        {
            // Successo per questo nodo
            if (nodoCorrente < nodiTotali)
            {
                nodoCorrente++;
                StartCoroutine(FlashNodo(true));
                ConfiguraDifficoltaNodo();
            }
            else
            {
                bypassCompletato = true;
                StartCoroutine(SequenzaSuccesso("BYPASS CIRCUITI COMPLETATO // SISTEMA SOVRASCRITTO"));
            }
        }
        else
        {
            // Errore tempistica
            nodoCorrente = 1;
            ConfiguraDifficoltaNodo();
            StartCoroutine(FlashNodo(false));
        }
    }

    private IEnumerator FlashNodo(bool successo)
    {
        if (testoStatoMessaggio == null) yield break;

        testoStatoMessaggio.text = successo ? $"✓ NODO SICUREZZA {nodoCorrente - 1} DISATTIVATO!" : "✗ ALLARME CIRCUITO: SOVRACCARICO! RESET NODO 1";
        testoStatoMessaggio.color = successo ? Color.green : Color.red;

        yield return new WaitForSecondsRealtime(0.6f);

        if (testoStatoMessaggio != null && !bypassCompletato)
        {
            testoStatoMessaggio.text = "SISTEMA DI SICUREZZA IN BYPASS - MANTIENI LA SINCRONIZZAZIONE";
            testoStatoMessaggio.color = new Color(0.3f, 0.85f, 1f);
        }
    }

    #endregion

    #region Sequenze Risultato

    private IEnumerator SequenzaSuccesso(string messaggio)
    {
        inAnimazioneChiusura = true;

        if (testoStatoMessaggio != null)
        {
            testoStatoMessaggio.text = "✓ " + messaggio;
            testoStatoMessaggio.color = Color.green;
        }

        if (terminaleAttivo != null)
        {
            terminaleAttivo.OnAccessoGarantito();
        }

        yield return new WaitForSecondsRealtime(1.0f);

        inAnimazioneChiusura = false;
        ChiudiTerminale();
    }

    private IEnumerator SequenzaErrore(string messaggio)
    {
        if (testoStatoMessaggio != null)
        {
            testoStatoMessaggio.text = "✗ " + messaggio;
            testoStatoMessaggio.color = Color.red;
        }

        codiceDigitato = "";
        AggiornaDisplayCodice();

        yield return new WaitForSecondsRealtime(0.8f);

        if (testoStatoMessaggio != null)
        {
            testoStatoMessaggio.text = "INSERISCI CODICE DI ACCESSO A 4 CIFRE O ATTIVA BYPASS";
            testoStatoMessaggio.color = new Color(0.3f, 0.85f, 1f);
        }
    }

    #endregion

    #region Costruzione Dinamica UI

    private void CostruisciUISeNecessario()
    {
        if (pannelloPrincipale != null) return;

        canvasRoot = GetComponentInChildren<Canvas>();
        if (canvasRoot == null)
        {
            GameObject canvasObj = new GameObject("TerminaleCanvas");
            canvasObj.transform.SetParent(transform, false);
            canvasRoot = canvasObj.AddComponent<Canvas>();
            canvasRoot.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasRoot.sortingOrder = 999;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Overlay sfondo oscurato
        bgOverlay = new GameObject("DarkOverlay");
        bgOverlay.transform.SetParent(canvasRoot.transform, false);
        RectTransform rtOverlay = bgOverlay.AddComponent<RectTransform>();
        rtOverlay.anchorMin = Vector2.zero;
        rtOverlay.anchorMax = Vector2.one;
        rtOverlay.offsetMin = Vector2.zero;
        rtOverlay.offsetMax = Vector2.zero;
        Image imgOverlay = bgOverlay.AddComponent<Image>();
        imgOverlay.color = new Color(0.02f, 0.05f, 0.08f, 0.85f);

        // Finestra Principale Terminale
        pannelloPrincipale = new GameObject("PannelloTerminale");
        pannelloPrincipale.transform.SetParent(canvasRoot.transform, false);
        RectTransform rtPanel = pannelloPrincipale.AddComponent<RectTransform>();
        rtPanel.sizeDelta = new Vector2(650, 720);
        rtPanel.anchoredPosition = Vector2.zero;

        Image imgPanel = pannelloPrincipale.AddComponent<Image>();
        imgPanel.color = new Color(0.06f, 0.10f, 0.14f, 0.96f);

        // Header Terminale
        GameObject headerObj = new GameObject("Header");
        headerObj.transform.SetParent(pannelloPrincipale.transform, false);
        RectTransform rtHeader = headerObj.AddComponent<RectTransform>();
        rtHeader.anchorMin = new Vector2(0, 1);
        rtHeader.anchorMax = new Vector2(1, 1);
        rtHeader.pivot = new Vector2(0.5f, 1);
        rtHeader.sizeDelta = new Vector2(0, 70);
        rtHeader.anchoredPosition = Vector2.zero;
        Image imgHeader = headerObj.AddComponent<Image>();
        imgHeader.color = new Color(0.08f, 0.16f, 0.22f, 1f);

        GameObject titoloObj = new GameObject("Titolo");
        titoloObj.transform.SetParent(headerObj.transform, false);
        RectTransform rtTitolo = titoloObj.AddComponent<RectTransform>();
        rtTitolo.anchorMin = Vector2.zero;
        rtTitolo.anchorMax = Vector2.one;
        rtTitolo.offsetMin = new Vector2(20, 0);
        rtTitolo.offsetMax = new Vector2(-60, 0);
        testoTitoloTerminale = titoloObj.AddComponent<Text>();
        testoTitoloTerminale.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        testoTitoloTerminale.fontSize = 22;
        testoTitoloTerminale.fontStyle = FontStyle.Bold;
        testoTitoloTerminale.alignment = TextAnchor.MiddleLeft;
        testoTitoloTerminale.color = new Color(0.4f, 0.9f, 1f);

        // Pulsante Chiudi X
        GameObject btnCloseObj = new GameObject("BtnClose");
        btnCloseObj.transform.SetParent(headerObj.transform, false);
        RectTransform rtClose = btnCloseObj.AddComponent<RectTransform>();
        rtClose.anchorMin = new Vector2(1, 0.5f);
        rtClose.anchorMax = new Vector2(1, 0.5f);
        rtClose.sizeDelta = new Vector2(40, 40);
        rtClose.anchoredPosition = new Vector2(-25, 0);
        Image imgClose = btnCloseObj.AddComponent<Image>();
        imgClose.color = new Color(0.8f, 0.2f, 0.2f);
        Button btnClose = btnCloseObj.AddComponent<Button>();
        btnClose.onClick.AddListener(ChiudiTerminale);

        GameObject txtCloseObj = new GameObject("X");
        txtCloseObj.transform.SetParent(btnCloseObj.transform, false);
        RectTransform rtTxtClose = txtCloseObj.AddComponent<RectTransform>();
        rtTxtClose.anchorMin = Vector2.zero;
        rtTxtClose.anchorMax = Vector2.one;
        Text txtClose = txtCloseObj.AddComponent<Text>();
        txtClose.font = testoTitoloTerminale.font;
        txtClose.text = "✕";
        txtClose.fontSize = 20;
        txtClose.alignment = TextAnchor.MiddleCenter;
        txtClose.color = Color.white;

        // Barra Messaggi di Stato
        GameObject statusObj = new GameObject("StatusMessage");
        statusObj.transform.SetParent(pannelloPrincipale.transform, false);
        RectTransform rtStatus = statusObj.AddComponent<RectTransform>();
        rtStatus.anchorMin = new Vector2(0, 1);
        rtStatus.anchorMax = new Vector2(1, 1);
        rtStatus.pivot = new Vector2(0.5f, 1);
        rtStatus.sizeDelta = new Vector2(0, 40);
        rtStatus.anchoredPosition = new Vector2(0, -75);
        testoStatoMessaggio = statusObj.AddComponent<Text>();
        testoStatoMessaggio.font = testoTitoloTerminale.font;
        testoStatoMessaggio.fontSize = 14;
        testoStatoMessaggio.alignment = TextAnchor.MiddleCenter;

        // Pulsanti Selezione Tab
        GameObject tabSwitcher = new GameObject("TabSwitcher");
        tabSwitcher.transform.SetParent(pannelloPrincipale.transform, false);
        RectTransform rtSwitcher = tabSwitcher.AddComponent<RectTransform>();
        rtSwitcher.anchorMin = new Vector2(0.5f, 1);
        rtSwitcher.anchorMax = new Vector2(0.5f, 1);
        rtSwitcher.sizeDelta = new Vector2(550, 45);
        rtSwitcher.anchoredPosition = new Vector2(0, -125);

        HorizontalLayoutGroup hlg = tabSwitcher.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 15;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        btnTabKeypad = CreaBottone(tabSwitcher.transform, "🔢 TASTIERINO PIN", () => MostraTabKeypad());
        btnTabBypass = CreaBottone(tabSwitcher.transform, "⚡ BYPASS CIRCUITI", () => MostraTabBypass());

        // ── Creazione Contenitore Tab 1: KEYPAD ─────────────────────────────
        tabKeypadObj = new GameObject("Tab_Keypad");
        tabKeypadObj.transform.SetParent(pannelloPrincipale.transform, false);
        RectTransform rtKeypadTab = tabKeypadObj.AddComponent<RectTransform>();
        rtKeypadTab.anchorMin = Vector2.zero;
        rtKeypadTab.anchorMax = Vector2.one;
        rtKeypadTab.offsetMin = new Vector2(25, 20);
        rtKeypadTab.offsetMax = new Vector2(-25, -180);

        // Display PIN
        GameObject displayObj = new GameObject("DisplayPIN");
        displayObj.transform.SetParent(tabKeypadObj.transform, false);
        RectTransform rtDisplay = displayObj.AddComponent<RectTransform>();
        rtDisplay.anchorMin = new Vector2(0.5f, 1);
        rtDisplay.anchorMax = new Vector2(0.5f, 1);
        rtDisplay.sizeDelta = new Vector2(480, 65);
        rtDisplay.anchoredPosition = new Vector2(0, -10);
        Image imgDisplay = displayObj.AddComponent<Image>();
        imgDisplay.color = new Color(0.03f, 0.06f, 0.09f, 1f);

        GameObject txtDisplayObj = new GameObject("TextPIN");
        txtDisplayObj.transform.SetParent(displayObj.transform, false);
        RectTransform rtTxtDisplay = txtDisplayObj.AddComponent<RectTransform>();
        rtTxtDisplay.anchorMin = Vector2.zero;
        rtTxtDisplay.anchorMax = Vector2.one;
        testoDisplayCodice = txtDisplayObj.AddComponent<Text>();
        testoDisplayCodice.font = testoTitoloTerminale.font;
        testoDisplayCodice.fontSize = 32;
        testoDisplayCodice.fontStyle = FontStyle.Bold;
        testoDisplayCodice.alignment = TextAnchor.MiddleCenter;

        // Griglia Tasti 0-9
        GameObject gridKeypad = new GameObject("GridKeypad");
        gridKeypad.transform.SetParent(tabKeypadObj.transform, false);
        RectTransform rtGrid = gridKeypad.AddComponent<RectTransform>();
        rtGrid.anchorMin = new Vector2(0.5f, 0);
        rtGrid.anchorMax = new Vector2(0.5f, 1);
        rtGrid.sizeDelta = new Vector2(420, 0);
        rtGrid.anchoredPosition = new Vector2(0, -90);

        GridLayoutGroup glg = gridKeypad.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(125, 60);
        glg.spacing = new Vector2(15, 12);
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 3;

        string[] tasti = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "⌫ CANC", "0", "↵ INVIO" };
        foreach (string t in tasti)
        {
            string valore = t;
            if (valore == "⌫ CANC")
                CreaBottone(gridKeypad.transform, valore, CancellaCifra, new Color(0.6f, 0.25f, 0.25f));
            else if (valore == "↵ INVIO")
                CreaBottone(gridKeypad.transform, valore, ConfermaCodice, new Color(0.2f, 0.65f, 0.35f));
            else
                CreaBottone(gridKeypad.transform, valore, () => InserisciCifra(valore));
        }

        // ── Creazione Contenitore Tab 2: MINIGIOCO BYPASS ───────────────────
        tabBypassObj = new GameObject("Tab_Bypass");
        tabBypassObj.transform.SetParent(pannelloPrincipale.transform, false);
        RectTransform rtBypassTab = tabBypassObj.AddComponent<RectTransform>();
        rtBypassTab.anchorMin = Vector2.zero;
        rtBypassTab.anchorMax = Vector2.one;
        rtBypassTab.offsetMin = new Vector2(25, 20);
        rtBypassTab.offsetMax = new Vector2(-25, -180);

        // Testo Progresso Nodo
        GameObject txtNodoObj = new GameObject("TxtNodo");
        txtNodoObj.transform.SetParent(tabBypassObj.transform, false);
        RectTransform rtTxtNodo = txtNodoObj.AddComponent<RectTransform>();
        rtTxtNodo.anchorMin = new Vector2(0.5f, 1);
        rtTxtNodo.anchorMax = new Vector2(0.5f, 1);
        rtTxtNodo.sizeDelta = new Vector2(500, 40);
        rtTxtNodo.anchoredPosition = new Vector2(0, -10);
        testoNodoProgresso = txtNodoObj.AddComponent<Text>();
        testoNodoProgresso.font = testoTitoloTerminale.font;
        testoNodoProgresso.fontSize = 20;
        testoNodoProgresso.fontStyle = FontStyle.Bold;
        testoNodoProgresso.alignment = TextAnchor.MiddleCenter;
        testoNodoProgresso.color = new Color(0.3f, 0.9f, 1f);

        // Barra di Calibrazione
        GameObject barraCalibrazioneBg = new GameObject("BarraCalibrazioneBg");
        barraCalibrazioneBg.transform.SetParent(tabBypassObj.transform, false);
        RectTransform rtBarraBg = barraCalibrazioneBg.AddComponent<RectTransform>();
        rtBarraBg.anchorMin = new Vector2(0.5f, 0.5f);
        rtBarraBg.anchorMax = new Vector2(0.5f, 0.5f);
        rtBarraBg.sizeDelta = new Vector2(ampiezzaBarra, 36);
        rtBarraBg.anchoredPosition = new Vector2(0, 30);
        Image imgBarraBg = barraCalibrazioneBg.AddComponent<Image>();
        imgBarraBg.color = new Color(0.12f, 0.15f, 0.2f, 1f);

        // Zona Verde Target
        GameObject greenZoneObj = new GameObject("ZonaVerde");
        greenZoneObj.transform.SetParent(barraCalibrazioneBg.transform, false);
        zonaVerdeTarget = greenZoneObj.AddComponent<RectTransform>();
        zonaVerdeTarget.sizeDelta = new Vector2(80, 36);
        zonaVerdeTarget.anchoredPosition = Vector2.zero;
        Image imgGreen = greenZoneObj.AddComponent<Image>();
        imgGreen.color = new Color(0.2f, 0.85f, 0.4f, 0.8f);

        // Cursore Oscillante
        GameObject cursoreObj = new GameObject("CursoreOscillante");
        cursoreObj.transform.SetParent(barraCalibrazioneBg.transform, false);
        barraOscillatore = cursoreObj.AddComponent<RectTransform>();
        barraOscillatore.sizeDelta = new Vector2(8, 48);
        barraOscillatore.anchoredPosition = Vector2.zero;
        Image imgCursore = cursoreObj.AddComponent<Image>();
        imgCursore.color = Color.white;

        // Istruzioni
        GameObject txtIstrObj = new GameObject("TxtIstruzioni");
        txtIstrObj.transform.SetParent(tabBypassObj.transform, false);
        RectTransform rtIstr = txtIstrObj.AddComponent<RectTransform>();
        rtIstr.anchorMin = new Vector2(0.5f, 0.5f);
        rtIstr.anchorMax = new Vector2(0.5f, 0.5f);
        rtIstr.sizeDelta = new Vector2(500, 60);
        rtIstr.anchoredPosition = new Vector2(0, -35);
        testoIstruzioniBypass = txtIstrObj.AddComponent<Text>();
        testoIstruzioniBypass.font = testoTitoloTerminale.font;
        testoIstruzioniBypass.fontSize = 15;
        testoIstruzioniBypass.alignment = TextAnchor.MiddleCenter;
        testoIstruzioniBypass.color = new Color(0.8f, 0.85f, 0.9f);

        // Bottone Calibra Nodo
        Button btnBypassClick = CreaBottone(tabBypassObj.transform, "⚡ CALIBRA CIRCUITO [SPAZIO]", TentaBypassNodo, new Color(0.15f, 0.55f, 0.85f));
        RectTransform rtBtnBypass = btnBypassClick.GetComponent<RectTransform>();
        rtBtnBypass.anchorMin = new Vector2(0.5f, 0);
        rtBtnBypass.anchorMax = new Vector2(0.5f, 0);
        rtBtnBypass.sizeDelta = new Vector2(360, 60);
        rtBtnBypass.anchoredPosition = new Vector2(0, 35);

        if (bgOverlay != null) bgOverlay.SetActive(false);
        if (pannelloPrincipale != null) pannelloPrincipale.SetActive(false);
        if (canvasRoot != null) canvasRoot.gameObject.SetActive(false);
    }

    private Button CreaBottone(Transform parent, string testo, UnityEngine.Events.UnityAction onClick, Color? coloreSfondo = null)
    {
        GameObject btnObj = new GameObject("Btn_" + testo);
        btnObj.transform.SetParent(parent, false);

        Image img = btnObj.AddComponent<Image>();
        img.color = coloreSfondo ?? new Color(0.12f, 0.22f, 0.32f, 1f);

        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        ColorBlock cb = btn.colors;
        cb.highlightedColor = (coloreSfondo ?? new Color(0.12f, 0.22f, 0.32f, 1f)) * 1.25f;
        cb.pressedColor = (coloreSfondo ?? new Color(0.12f, 0.22f, 0.32f, 1f)) * 0.8f;
        btn.colors = cb;

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform rtTxt = txtObj.AddComponent<RectTransform>();
        rtTxt.anchorMin = Vector2.zero;
        rtTxt.anchorMax = Vector2.one;

        Text txt = txtObj.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.text = testo;
        txt.fontSize = 17;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;

        return btn;
    }

    #endregion
}
