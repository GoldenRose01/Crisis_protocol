using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using GoldenCast.UI;

/// <summary>
/// Gestore dell'interfaccia olografica LED verde acqua / ciano per la lettura dei codici di sicurezza e PIN delle porte.
/// </summary>
public class DatapadOlogrammaUI : MonoBehaviour
{
    public static DatapadOlogrammaUI Instance { get; private set; }

    private const string ModalOwner = "DatapadOlogramma";

    private Canvas canvasRoot;
    private GameObject bgOverlay;
    private GameObject pannelloOlogramma;
    private Image imgPannelloFrame;

    private Text txtTitolo;
    private Text txtSottotitolo;
    private Text txtStatusDiagnostica;
    private Transform contentContainer;
    private List<GameObject> cardIstanziate = new List<GameObject>();

    private DatapadCodiciPorte datapadAttivo;
    private Font defaultFont;

    // Colori Ologramma Verde Acqua / LED Sci-Fi
    private readonly Color ColoreAquaNeon = new Color(0.0f, 0.95f, 0.85f, 1f);
    private readonly Color ColoreAquaChiaro = new Color(0.4f, 1.0f, 0.9f, 1f);
    private readonly Color ColoreAquaSfondoCard = new Color(0.02f, 0.12f, 0.15f, 0.88f);
    private readonly Color ColoreSfondoOlogramma = new Color(0.012f, 0.06f, 0.08f, 0.95f);
    private readonly Color ColoreBordoFrame = new Color(0.0f, 0.9f, 0.8f, 0.7f);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        CostruisciUISeNecessario();
    }

    void Update()
    {
        if (datapadAttivo == null || canvasRoot == null || !canvasRoot.gameObject.activeSelf || pannelloOlogramma == null || !pannelloOlogramma.activeSelf)
            return;

        // Chiusura con tasto ESC o E
        if (Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame))
        {
            ChiudiOlogramma();
            return;
        }

        // Effetto pulsazione olografica LED
        if (imgPannelloFrame != null)
        {
            float pulse = 0.92f + (Mathf.Sin(Time.unscaledTime * 4f) * 0.04f);
            Color c = ColoreSfondoOlogramma;
            c.a = pulse;
            imgPannelloFrame.color = c;
        }
    }

    public void ApriOlogramma(DatapadCodiciPorte datapad)
    {
        datapadAttivo = datapad;
        CostruisciUISeNecessario();

        if (canvasRoot != null) canvasRoot.gameObject.SetActive(true);
        if (bgOverlay != null) bgOverlay.SetActive(true);
        if (pannelloOlogramma != null) pannelloOlogramma.SetActive(true);

        // Imposta testi intestazione
        if (txtTitolo != null)
            txtTitolo.text = datapad != null ? datapad.titoloDatapad.ToUpper() : "DATAPAD SICUREZZA // REGISTRO CODICI";

        if (txtSottotitolo != null)
            txtSottotitolo.text = datapad != null ? datapad.autoreONota : "Memorandum di Sicurezza - Protocollo di Emergenza";

        // Popola la lista delle porte
        PopolaElencoCodici(datapad);

        // Blocca i movimenti di gioco e sblocca il cursore
        ModalUIState.TryOpen(ModalOwner);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ChiudiOlogramma()
    {
        if (pannelloOlogramma != null)
            pannelloOlogramma.SetActive(false);

        if (bgOverlay != null)
            bgOverlay.SetActive(false);

        if (canvasRoot != null)
            canvasRoot.gameObject.SetActive(false);

        datapadAttivo = null;
        ModalUIState.Close(ModalOwner);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void PopolaElencoCodici(DatapadCodiciPorte datapad)
    {
        // Pulisci le card precedenti
        foreach (GameObject c in cardIstanziate)
        {
            if (c != null) Destroy(c);
        }
        cardIstanziate.Clear();

        if (contentContainer == null) return;

        List<VoceCodicePorta> voci = (datapad != null) ? datapad.OttieniTuttiICodici() : new List<VoceCodicePorta>();

        if (voci == null || voci.Count == 0)
        {
            CreaCardSingola("NESSUNA PORTA BLOCCATA RILEVATA", "----", "TUTTI I SETTORI LIBERI", "Tutte le porte del settore sono attualmente accessibili.");
            return;
        }

        foreach (VoceCodicePorta v in voci)
        {
            CreaCardSingola(v.nomePorta, v.codicePin, v.livelloSicurezza, v.note);
        }

        if (txtStatusDiagnostica != null)
            txtStatusDiagnostica.text = $"● PROTOCOLLO OLOGRAFICO ATTIVO // {voci.Count} VOCI CIFRATE CARICATE // FREQ: 433.92 MHz";
    }

    private void CreaCardSingola(string nomePorta, string codicePin, string stato, string note)
    {
        GameObject cardObj = new GameObject("Card_" + nomePorta);
        cardObj.transform.SetParent(contentContainer, false);
        cardIstanziate.Add(cardObj);

        RectTransform rtCard = cardObj.AddComponent<RectTransform>();
        rtCard.sizeDelta = new Vector2(0, 85);

        LayoutElement le = cardObj.AddComponent<LayoutElement>();
        le.minHeight = 85;
        le.preferredHeight = 85;
        le.flexibleWidth = 1;

        Image imgCard = cardObj.AddComponent<Image>();
        imgCard.color = ColoreAquaSfondoCard;

        // Barra laterale neon verde acqua
        GameObject barObj = new GameObject("AccentBar");
        barObj.transform.SetParent(cardObj.transform, false);
        RectTransform rtBar = barObj.AddComponent<RectTransform>();
        rtBar.anchorMin = new Vector2(0, 0);
        rtBar.anchorMax = new Vector2(0, 1);
        rtBar.pivot = new Vector2(0, 0.5f);
        rtBar.sizeDelta = new Vector2(5, 0);
        rtBar.anchoredPosition = Vector2.zero;
        Image imgBar = barObj.AddComponent<Image>();
        imgBar.color = ColoreAquaNeon;

        // Sezione Testi (Sinistra)
        GameObject infoObj = new GameObject("InfoContainer");
        infoObj.transform.SetParent(cardObj.transform, false);
        RectTransform rtInfo = infoObj.AddComponent<RectTransform>();
        rtInfo.anchorMin = new Vector2(0, 0);
        rtInfo.anchorMax = new Vector2(0.68f, 1);
        rtInfo.offsetMin = new Vector2(20, 8);
        rtInfo.offsetMax = new Vector2(-10, -8);

        // Titolo Porta
        GameObject txtNomeObj = new GameObject("TxtNome");
        txtNomeObj.transform.SetParent(infoObj.transform, false);
        RectTransform rtNome = txtNomeObj.AddComponent<RectTransform>();
        rtNome.anchorMin = new Vector2(0, 0.55f);
        rtNome.anchorMax = new Vector2(1, 1);
        rtNome.offsetMin = Vector2.zero;
        rtNome.offsetMax = Vector2.zero;
        Text txtNome = txtNomeObj.AddComponent<Text>();
        txtNome.font = defaultFont;
        txtNome.text = $"▰ {nomePorta}";
        txtNome.fontSize = 18;
        txtNome.fontStyle = FontStyle.Bold;
        txtNome.color = Color.white;
        txtNome.alignment = TextAnchor.MiddleLeft;

        // Note / Dettaglio
        GameObject txtNoteObj = new GameObject("TxtNote");
        txtNoteObj.transform.SetParent(infoObj.transform, false);
        RectTransform rtNote = txtNoteObj.AddComponent<RectTransform>();
        rtNote.anchorMin = new Vector2(0, 0);
        rtNote.anchorMax = new Vector2(1, 0.55f);
        rtNote.offsetMin = Vector2.zero;
        rtNote.offsetMax = Vector2.zero;
        Text txtNote = txtNoteObj.AddComponent<Text>();
        txtNote.font = defaultFont;
        txtNote.text = $"{stato}  |  {note}";
        txtNote.fontSize = 13;
        txtNote.color = new Color(0.4f, 0.85f, 0.85f);
        txtNote.alignment = TextAnchor.MiddleLeft;

        // Sezione Box PIN (Destra con glow LED)
        GameObject pinBoxObj = new GameObject("PinBox");
        pinBoxObj.transform.SetParent(cardObj.transform, false);
        RectTransform rtPinBox = pinBoxObj.AddComponent<RectTransform>();
        rtPinBox.anchorMin = new Vector2(0.70f, 0.15f);
        rtPinBox.anchorMax = new Vector2(0.98f, 0.85f);
        rtPinBox.offsetMin = Vector2.zero;
        rtPinBox.offsetMax = Vector2.zero;

        Image imgPinBox = pinBoxObj.AddComponent<Image>();
        imgPinBox.color = new Color(0.01f, 0.05f, 0.07f, 1f);

        GameObject txtPinObj = new GameObject("TxtPIN");
        txtPinObj.transform.SetParent(pinBoxObj.transform, false);
        RectTransform rtTxtPin = txtPinObj.AddComponent<RectTransform>();
        rtTxtPin.anchorMin = Vector2.zero;
        rtTxtPin.anchorMax = Vector2.one;
        rtTxtPin.offsetMin = Vector2.zero;
        rtTxtPin.offsetMax = Vector2.zero;

        Text txtPin = txtPinObj.AddComponent<Text>();
        txtPin.font = defaultFont;
        txtPin.text = $"PIN: [ {codicePin} ]";
        txtPin.fontSize = 20;
        txtPin.fontStyle = FontStyle.Bold;
        txtPin.color = ColoreAquaNeon;
        txtPin.alignment = TextAnchor.MiddleCenter;
    }

    #region Costruzione Dinamica Canvas

    private void CostruisciUISeNecessario()
    {
        if (pannelloOlogramma != null) return;

        canvasRoot = GetComponentInChildren<Canvas>();
        if (canvasRoot == null)
        {
            GameObject canvasObj = new GameObject("DatapadOlogrammaCanvas");
            canvasObj.transform.SetParent(transform, false);
            canvasRoot = canvasObj.AddComponent<Canvas>();
            canvasRoot.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasRoot.sortingOrder = 998;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            scaler.dynamicPixelsPerUnit = 3.0f;

            canvasObj.AddComponent<GraphicRaycaster>();
        }
        else
        {
            CanvasScaler scaler = canvasRoot.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                scaler.dynamicPixelsPerUnit = 3.0f;
            }
        }

        // Overlay sfondo oscurato
        bgOverlay = new GameObject("DarkHoloOverlay");
        bgOverlay.transform.SetParent(canvasRoot.transform, false);
        RectTransform rtOverlay = bgOverlay.AddComponent<RectTransform>();
        rtOverlay.anchorMin = Vector2.zero;
        rtOverlay.anchorMax = Vector2.one;
        rtOverlay.offsetMin = Vector2.zero;
        rtOverlay.offsetMax = Vector2.zero;
        Image imgOverlay = bgOverlay.AddComponent<Image>();
        imgOverlay.color = new Color(0.01f, 0.04f, 0.06f, 0.70f);

        // Finestra Principale Ologramma
        pannelloOlogramma = new GameObject("PannelloOlogramma");
        pannelloOlogramma.transform.SetParent(canvasRoot.transform, false);
        RectTransform rtPanel = pannelloOlogramma.AddComponent<RectTransform>();
        rtPanel.sizeDelta = new Vector2(820, 680);
        rtPanel.anchoredPosition = Vector2.zero;

        imgPannelloFrame = pannelloOlogramma.AddComponent<Image>();
        imgPannelloFrame.color = ColoreSfondoOlogramma;

        // Bordo / Cornice Olografica
        GameObject frameBorder = new GameObject("HoloBorder");
        frameBorder.transform.SetParent(pannelloOlogramma.transform, false);
        RectTransform rtBorder = frameBorder.AddComponent<RectTransform>();
        rtBorder.anchorMin = Vector2.zero;
        rtBorder.anchorMax = Vector2.one;
        rtBorder.offsetMin = new Vector2(-3, -3);
        rtBorder.offsetMax = new Vector2(3, 3);
        Image imgBorder = frameBorder.AddComponent<Image>();
        imgBorder.color = ColoreBordoFrame;
        imgBorder.raycastTarget = false;
        frameBorder.transform.SetAsFirstSibling(); // Dietro al pannello

        // Header Ologramma
        GameObject headerObj = new GameObject("HoloHeader");
        headerObj.transform.SetParent(pannelloOlogramma.transform, false);
        RectTransform rtHeader = headerObj.AddComponent<RectTransform>();
        rtHeader.anchorMin = new Vector2(0, 1);
        rtHeader.anchorMax = new Vector2(1, 1);
        rtHeader.pivot = new Vector2(0.5f, 1);
        rtHeader.sizeDelta = new Vector2(0, 95);
        rtHeader.anchoredPosition = Vector2.zero;
        Image imgHeader = headerObj.AddComponent<Image>();
        imgHeader.color = new Color(0.018f, 0.10f, 0.13f, 1f);

        // Icona / Tag LED Ologramma
        GameObject ledTagObj = new GameObject("LedTag");
        ledTagObj.transform.SetParent(headerObj.transform, false);
        RectTransform rtTag = ledTagObj.AddComponent<RectTransform>();
        rtTag.anchorMin = new Vector2(0, 1);
        rtTag.anchorMax = new Vector2(0, 1);
        rtTag.pivot = new Vector2(0, 1);
        rtTag.sizeDelta = new Vector2(350, 25);
        rtTag.anchoredPosition = new Vector2(20, -10);
        Text txtTag = ledTagObj.AddComponent<Text>();
        txtTag.font = defaultFont;
        txtTag.text = "◆ PROIEZIONE OLOGRAFICA LED // CANALE SICUREZZA ◆";
        txtTag.fontSize = 11;
        txtTag.fontStyle = FontStyle.Bold;
        txtTag.color = ColoreAquaNeon;

        // Titolo Principale
        GameObject titoloObj = new GameObject("Titolo");
        titoloObj.transform.SetParent(headerObj.transform, false);
        RectTransform rtTitolo = titoloObj.AddComponent<RectTransform>();
        rtTitolo.anchorMin = new Vector2(0, 0);
        rtTitolo.anchorMax = new Vector2(1, 1);
        rtTitolo.offsetMin = new Vector2(20, 25);
        rtTitolo.offsetMax = new Vector2(-70, -30);
        txtTitolo = titoloObj.AddComponent<Text>();
        txtTitolo.font = defaultFont;
        txtTitolo.fontSize = 20;
        txtTitolo.fontStyle = FontStyle.Bold;
        txtTitolo.alignment = TextAnchor.MiddleLeft;
        txtTitolo.color = ColoreAquaChiaro;

        // Sottotitolo
        GameObject subObj = new GameObject("Sottotitolo");
        subObj.transform.SetParent(headerObj.transform, false);
        RectTransform rtSub = subObj.AddComponent<RectTransform>();
        rtSub.anchorMin = new Vector2(0, 0);
        rtSub.anchorMax = new Vector2(1, 0);
        rtSub.pivot = new Vector2(0.5f, 0);
        rtSub.sizeDelta = new Vector2(0, 24);
        rtSub.offsetMin = new Vector2(20, 6);
        rtSub.offsetMax = new Vector2(-70, 30);
        txtSottotitolo = subObj.AddComponent<Text>();
        txtSottotitolo.font = defaultFont;
        txtSottotitolo.fontSize = 13;
        txtSottotitolo.alignment = TextAnchor.MiddleLeft;
        txtSottotitolo.color = new Color(0.5f, 0.85f, 0.85f);

        // Pulsante Chiudi X
        GameObject btnCloseObj = new GameObject("BtnClose");
        btnCloseObj.transform.SetParent(headerObj.transform, false);
        RectTransform rtClose = btnCloseObj.AddComponent<RectTransform>();
        rtClose.anchorMin = new Vector2(1, 0.5f);
        rtClose.anchorMax = new Vector2(1, 0.5f);
        rtClose.sizeDelta = new Vector2(40, 40);
        rtClose.anchoredPosition = new Vector2(-25, 0);
        Image imgClose = btnCloseObj.AddComponent<Image>();
        imgClose.color = new Color(0.1f, 0.35f, 0.35f);
        Button btnClose = btnCloseObj.AddComponent<Button>();
        btnClose.onClick.AddListener(ChiudiOlogramma);

        GameObject txtCloseObj = new GameObject("X");
        txtCloseObj.transform.SetParent(btnCloseObj.transform, false);
        RectTransform rtTxtClose = txtCloseObj.AddComponent<RectTransform>();
        rtTxtClose.anchorMin = Vector2.zero;
        rtTxtClose.anchorMax = Vector2.one;
        Text txtClose = txtCloseObj.AddComponent<Text>();
        txtClose.font = defaultFont;
        txtClose.text = "✕";
        txtClose.fontSize = 20;
        txtClose.alignment = TextAnchor.MiddleCenter;
        txtClose.color = ColoreAquaNeon;

        // Area di Scorrimento (ScrollView) per le porte
        GameObject scrollObj = new GameObject("HoloScrollView");
        scrollObj.transform.SetParent(pannelloOlogramma.transform, false);
        RectTransform rtScroll = scrollObj.AddComponent<RectTransform>();
        rtScroll.anchorMin = Vector2.zero;
        rtScroll.anchorMax = Vector2.one;
        rtScroll.offsetMin = new Vector2(20, 75);
        rtScroll.offsetMax = new Vector2(-20, -110);

        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        // Viewport
        GameObject viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(scrollObj.transform, false);
        RectTransform rtView = viewportObj.AddComponent<RectTransform>();
        rtView.anchorMin = Vector2.zero;
        rtView.anchorMax = Vector2.one;
        rtView.sizeDelta = Vector2.zero;
        rtView.pivot = new Vector2(0, 1);
        Image imgView = viewportObj.AddComponent<Image>();
        imgView.color = Color.white;
        Mask mask = viewportObj.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // Content
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);
        RectTransform rtContent = contentObj.AddComponent<RectTransform>();
        rtContent.anchorMin = new Vector2(0, 1);
        rtContent.anchorMax = new Vector2(1, 1);
        rtContent.pivot = new Vector2(0.5f, 1);
        rtContent.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = rtView;
        scrollRect.content = rtContent;
        contentContainer = contentObj.transform;

        // Footer Ologramma
        GameObject footerObj = new GameObject("HoloFooter");
        footerObj.transform.SetParent(pannelloOlogramma.transform, false);
        RectTransform rtFooter = footerObj.AddComponent<RectTransform>();
        rtFooter.anchorMin = new Vector2(0, 0);
        rtFooter.anchorMax = new Vector2(1, 0);
        rtFooter.pivot = new Vector2(0.5f, 0);
        rtFooter.sizeDelta = new Vector2(0, 65);
        rtFooter.anchoredPosition = Vector2.zero;
        Image imgFooter = footerObj.AddComponent<Image>();
        imgFooter.color = new Color(0.015f, 0.08f, 0.10f, 1f);

        // Testo Diagnostica
        GameObject diagObj = new GameObject("TxtDiagnostica");
        diagObj.transform.SetParent(footerObj.transform, false);
        RectTransform rtDiag = diagObj.AddComponent<RectTransform>();
        rtDiag.anchorMin = new Vector2(0, 0);
        rtDiag.anchorMax = new Vector2(0.65f, 1);
        rtDiag.offsetMin = new Vector2(20, 0);
        rtDiag.offsetMax = Vector2.zero;
        txtStatusDiagnostica = diagObj.AddComponent<Text>();
        txtStatusDiagnostica.font = defaultFont;
        txtStatusDiagnostica.fontSize = 12;
        txtStatusDiagnostica.color = new Color(0.3f, 0.8f, 0.75f);
        txtStatusDiagnostica.alignment = TextAnchor.MiddleLeft;

        // Bottone Chiudi Footer
        GameObject btnHoloCloseObj = new GameObject("BtnHoloClose");
        btnHoloCloseObj.transform.SetParent(footerObj.transform, false);
        RectTransform rtHoloClose = btnHoloCloseObj.AddComponent<RectTransform>();
        rtHoloClose.anchorMin = new Vector2(1, 0.5f);
        rtHoloClose.anchorMax = new Vector2(1, 0.5f);
        rtHoloClose.sizeDelta = new Vector2(220, 42);
        rtHoloClose.anchoredPosition = new Vector2(-20, 0);

        Image imgBtnHolo = btnHoloCloseObj.AddComponent<Image>();
        imgBtnHolo.color = new Color(0.0f, 0.45f, 0.42f, 1f);

        Button btnHoloClose = btnHoloCloseObj.AddComponent<Button>();
        btnHoloClose.onClick.AddListener(ChiudiOlogramma);

        GameObject txtBtnObj = new GameObject("Text");
        txtBtnObj.transform.SetParent(btnHoloCloseObj.transform, false);
        RectTransform rtTxtBtn = txtBtnObj.AddComponent<RectTransform>();
        rtTxtBtn.anchorMin = Vector2.zero;
        rtTxtBtn.anchorMax = Vector2.one;
        Text txtBtn = txtBtnObj.AddComponent<Text>();
        txtBtn.font = defaultFont;
        txtBtn.text = "[✕] CHIUDI HOLOPAD (ESC)";
        txtBtn.fontSize = 14;
        txtBtn.fontStyle = FontStyle.Bold;
        txtBtn.color = Color.white;
        txtBtn.alignment = TextAnchor.MiddleCenter;

        // Deattiva all'avvio
        if (bgOverlay != null) bgOverlay.SetActive(false);
        if (pannelloOlogramma != null) pannelloOlogramma.SetActive(false);
        if (canvasRoot != null) canvasRoot.gameObject.SetActive(false);
    }

    #endregion
}
