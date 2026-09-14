// ============================================================================
// Crisis Protocol / Sector Containment - Interfaccia utente
// File: .\Assets\CrisisProtocol\Scripts\UI\DatapadOlogrammaUI.cs
// Responsabilita': aggiorna HUD, menu, overlay, gauge, notifiche o schermate di supporto in base agli eventi gameplay.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using System.Collections.Generic; // usa lib // riga-ok
using UnityEngine; // usa lib // riga-ok
using UnityEngine.UI; // usa lib // riga-ok
using UnityEngine.InputSystem; // usa lib // riga-ok
using CrisisProtocol.UI; // usa lib // riga-ok

/// <summary>
/// Gestore dell'interfaccia olografica LED verde acqua / ciano per la lettura dei codici di sicurezza e PIN delle porte.
/// </summary>
// blocco: classe x roba grossa
public class DatapadOlogrammaUI : MonoBehaviour // classe qui // riga-ok
{ // apre // riga-ok
    public static DatapadOlogrammaUI Instance { get; private set; } // roba pub // riga-ok

    private const string ModalOwner = "DatapadOlogramma"; // roba pub // riga-ok

    private Canvas canvasRoot; // roba pub // riga-ok
    private GameObject bgOverlay; // roba pub // riga-ok
    private GameObject pannelloOlogramma; // roba pub // riga-ok
    private Image imgPannelloFrame; // roba pub // riga-ok

    private Text txtTitolo; // roba pub // riga-ok
    private Text txtSottotitolo; // roba pub // riga-ok
    private Text txtStatusDiagnostica; // roba pub // riga-ok
    private Transform contentContainer; // roba pub // riga-ok
    private List<GameObject> cardIstanziate = new List<GameObject>(); // roba pub // riga-ok

    private DatapadCodiciPorte datapadAttivo; // roba pub // riga-ok
    private Font defaultFont; // roba pub // riga-ok

    // Colori Ologramma Verde Acqua / LED Sci-Fi
    private readonly Color ColoreAquaNeon = new Color(0.0f, 0.95f, 0.85f, 1f); // roba pub // riga-ok
    private readonly Color ColoreAquaChiaro = new Color(0.4f, 1.0f, 0.9f, 1f); // roba pub // riga-ok
    private readonly Color ColoreAquaSfondoCard = new Color(0.02f, 0.12f, 0.15f, 0.88f); // roba pub // riga-ok
    private readonly Color ColoreSfondoOlogramma = new Color(0.012f, 0.06f, 0.08f, 0.95f); // roba pub // riga-ok
    private readonly Color ColoreBordoFrame = new Color(0.0f, 0.9f, 0.8f, 0.7f); // roba pub // riga-ok

    void Awake() // chiama // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (Instance != null && Instance != this) // se ok // riga-ok
        { // apre // riga-ok
            Destroy(gameObject); // elimina // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok
        Instance = this; // setta // riga-ok
        DontDestroyOnLoad(gameObject); // chiama // riga-ok

        defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf"); // setta // riga-ok
        CostruisciUISeNecessario(); // chiama // riga-ok
    } // chiude // riga-ok

    void Update() // chiama // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (datapadAttivo == null || canvasRoot == null || !canvasRoot.gameObject.activeSelf || pannelloOlogramma == null || !pannelloOlogramma.activeSelf) // se ok // riga-ok
            return; // torna val // riga-ok

        // Chiusura con tasto ESC o E
        // blocco: controlla se va
        if (Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame)) // se ok // riga-ok
        { // apre // riga-ok
            ChiudiOlogramma(); // chiama // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        // Effetto pulsazione olografica LED
        // blocco: controlla se va
        if (imgPannelloFrame != null) // se ok // riga-ok
        { // apre // riga-ok
            float pulse = 0.92f + (Mathf.Sin(Time.unscaledTime * 4f) * 0.04f); // setta // riga-ok
            Color c = ColoreSfondoOlogramma; // setta // riga-ok
            c.a = pulse; // setta // riga-ok
            imgPannelloFrame.color = c; // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void ApriOlogramma(DatapadCodiciPorte datapad) // roba pub // riga-ok
    { // apre // riga-ok
        datapadAttivo = datapad; // setta // riga-ok
        CostruisciUISeNecessario(); // chiama // riga-ok

        // blocco: controlla se va
        if (canvasRoot != null) canvasRoot.gameObject.SetActive(true); // se ok // riga-ok
        // blocco: controlla se va
        if (bgOverlay != null) bgOverlay.SetActive(true); // se ok // riga-ok
        // blocco: controlla se va
        if (pannelloOlogramma != null) pannelloOlogramma.SetActive(true); // se ok // riga-ok

        // Imposta testi intestazione
        // blocco: controlla se va
        if (txtTitolo != null) // se ok // riga-ok
            txtTitolo.text = datapad != null ? datapad.titoloDatapad.ToUpper() : "DATAPAD SICUREZZA // REGISTRO CODICI"; // setta // riga-ok

        // blocco: controlla se va
        if (txtSottotitolo != null) // se ok // riga-ok
            txtSottotitolo.text = datapad != null ? datapad.autoreONota : "Memorandum di Sicurezza - Protocollo di Emergenza"; // setta // riga-ok

        // Popola la lista delle porte
        PopolaElencoCodici(datapad); // chiama // riga-ok

        // Blocca i movimenti di gioco e sblocca il cursore
        ModalUIState.TryOpen(ModalOwner); // chiama // riga-ok
        Cursor.visible = true; // setta // riga-ok
        Cursor.lockState = CursorLockMode.None; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void ChiudiOlogramma() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (pannelloOlogramma != null) // se ok // riga-ok
            pannelloOlogramma.SetActive(false); // chiama // riga-ok

        // blocco: controlla se va
        if (bgOverlay != null) // se ok // riga-ok
            bgOverlay.SetActive(false); // chiama // riga-ok

        // blocco: controlla se va
        if (canvasRoot != null) // se ok // riga-ok
            canvasRoot.gameObject.SetActive(false); // chiama // riga-ok

        datapadAttivo = null; // setta // riga-ok
        ModalUIState.Close(ModalOwner); // chiama // riga-ok
        Cursor.visible = false; // setta // riga-ok
        Cursor.lockState = CursorLockMode.Locked; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void PopolaElencoCodici(DatapadCodiciPorte datapad) // roba pub // riga-ok
    { // apre // riga-ok
        // Pulisci le card precedenti
        // blocco: gira piu volte
        foreach (GameObject c in cardIstanziate) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (c != null) Destroy(c); // se ok // riga-ok
        } // chiude // riga-ok
        cardIstanziate.Clear(); // chiama // riga-ok

        // blocco: controlla se va
        if (contentContainer == null) return; // se ok // riga-ok

        List<VoceCodicePorta> voci = (datapad != null) ? datapad.OttieniTuttiICodici() : new List<VoceCodicePorta>(); // setta // riga-ok

        // blocco: controlla se va
        if (voci == null || voci.Count == 0) // se ok // riga-ok
        { // apre // riga-ok
            CreaCardSingola("NESSUNA PORTA BLOCCATA RILEVATA", "----", "TUTTI I SETTORI LIBERI", "Tutte le porte del settore sono attualmente accessibili."); // chiama // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: gira piu volte
        foreach (VoceCodicePorta v in voci) // ciclo x // riga-ok
        { // apre // riga-ok
            CreaCardSingola(v.nomePorta, v.codicePin, v.livelloSicurezza, v.note); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (txtStatusDiagnostica != null) // se ok // riga-ok
            txtStatusDiagnostica.text = $"● PROTOCOLLO OLOGRAFICO ATTIVO // {voci.Count} VOCI CIFRATE CARICATE // FREQ: 433.92 MHz"; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void CreaCardSingola(string nomePorta, string codicePin, string stato, string note) // roba pub // riga-ok
    { // apre // riga-ok
        GameObject cardObj = new GameObject("Card_" + nomePorta); // setta // riga-ok
        cardObj.transform.SetParent(contentContainer, false); // chiama // riga-ok
        cardIstanziate.Add(cardObj); // chiama // riga-ok

        RectTransform rtCard = cardObj.AddComponent<RectTransform>(); // setta // riga-ok
        rtCard.sizeDelta = new Vector2(0, 85); // setta // riga-ok

        LayoutElement le = cardObj.AddComponent<LayoutElement>(); // setta // riga-ok
        le.minHeight = 85; // setta // riga-ok
        le.preferredHeight = 85; // setta // riga-ok
        le.flexibleWidth = 1; // setta // riga-ok

        Image imgCard = cardObj.AddComponent<Image>(); // setta // riga-ok
        imgCard.color = ColoreAquaSfondoCard; // setta // riga-ok

        // Barra laterale neon verde acqua
        GameObject barObj = new GameObject("AccentBar"); // setta // riga-ok
        barObj.transform.SetParent(cardObj.transform, false); // chiama // riga-ok
        RectTransform rtBar = barObj.AddComponent<RectTransform>(); // setta // riga-ok
        rtBar.anchorMin = new Vector2(0, 0); // setta // riga-ok
        rtBar.anchorMax = new Vector2(0, 1); // setta // riga-ok
        rtBar.pivot = new Vector2(0, 0.5f); // setta // riga-ok
        rtBar.sizeDelta = new Vector2(5, 0); // setta // riga-ok
        rtBar.anchoredPosition = Vector2.zero; // setta // riga-ok
        Image imgBar = barObj.AddComponent<Image>(); // setta // riga-ok
        imgBar.color = ColoreAquaNeon; // setta // riga-ok

        // Sezione Testi (Sinistra)
        GameObject infoObj = new GameObject("InfoContainer"); // setta // riga-ok
        infoObj.transform.SetParent(cardObj.transform, false); // chiama // riga-ok
        RectTransform rtInfo = infoObj.AddComponent<RectTransform>(); // setta // riga-ok
        rtInfo.anchorMin = new Vector2(0, 0); // setta // riga-ok
        rtInfo.anchorMax = new Vector2(0.68f, 1); // setta // riga-ok
        rtInfo.offsetMin = new Vector2(20, 8); // setta // riga-ok
        rtInfo.offsetMax = new Vector2(-10, -8); // setta // riga-ok

        // Titolo Porta
        GameObject txtNomeObj = new GameObject("TxtNome"); // setta // riga-ok
        txtNomeObj.transform.SetParent(infoObj.transform, false); // chiama // riga-ok
        RectTransform rtNome = txtNomeObj.AddComponent<RectTransform>(); // setta // riga-ok
        rtNome.anchorMin = new Vector2(0, 0.52f); // setta // riga-ok
        rtNome.anchorMax = new Vector2(1, 1); // setta // riga-ok
        rtNome.offsetMin = Vector2.zero; // setta // riga-ok
        rtNome.offsetMax = Vector2.zero; // setta // riga-ok
        Text txtNome = txtNomeObj.AddComponent<Text>(); // setta // riga-ok
        txtNome.font = defaultFont; // setta // riga-ok
        txtNome.text = $"▰ {nomePorta}"; // setta // riga-ok
        txtNome.fontSize = 20; // setta // riga-ok
        txtNome.fontStyle = FontStyle.Bold; // setta // riga-ok
        txtNome.color = Color.white; // setta // riga-ok
        txtNome.alignment = TextAnchor.MiddleLeft; // setta // riga-ok

        // Note / Dettaglio
        GameObject txtNoteObj = new GameObject("TxtNote"); // setta // riga-ok
        txtNoteObj.transform.SetParent(infoObj.transform, false); // chiama // riga-ok
        RectTransform rtNote = txtNoteObj.AddComponent<RectTransform>(); // setta // riga-ok
        rtNote.anchorMin = new Vector2(0, 0); // setta // riga-ok
        rtNote.anchorMax = new Vector2(1, 0.52f); // setta // riga-ok
        rtNote.offsetMin = Vector2.zero; // setta // riga-ok
        rtNote.offsetMax = Vector2.zero; // setta // riga-ok
        Text txtNote = txtNoteObj.AddComponent<Text>(); // setta // riga-ok
        txtNote.font = defaultFont; // setta // riga-ok
        txtNote.text = $"{stato}  |  {note}"; // setta // riga-ok
        txtNote.fontSize = 15; // setta // riga-ok
        txtNote.color = new Color(0.4f, 0.85f, 0.85f); // setta // riga-ok
        txtNote.alignment = TextAnchor.MiddleLeft; // setta // riga-ok

        // Sezione Box PIN (Destra con glow LED)
        GameObject pinBoxObj = new GameObject("PinBox"); // setta // riga-ok
        pinBoxObj.transform.SetParent(cardObj.transform, false); // chiama // riga-ok
        RectTransform rtPinBox = pinBoxObj.AddComponent<RectTransform>(); // setta // riga-ok
        rtPinBox.anchorMin = new Vector2(0.70f, 0.15f); // setta // riga-ok
        rtPinBox.anchorMax = new Vector2(0.98f, 0.85f); // setta // riga-ok
        rtPinBox.offsetMin = Vector2.zero; // setta // riga-ok
        rtPinBox.offsetMax = Vector2.zero; // setta // riga-ok

        Image imgPinBox = pinBoxObj.AddComponent<Image>(); // setta // riga-ok
        imgPinBox.color = new Color(0.01f, 0.05f, 0.07f, 1f); // setta // riga-ok

        GameObject txtPinObj = new GameObject("TxtPIN"); // setta // riga-ok
        txtPinObj.transform.SetParent(pinBoxObj.transform, false); // chiama // riga-ok
        RectTransform rtTxtPin = txtPinObj.AddComponent<RectTransform>(); // setta // riga-ok
        rtTxtPin.anchorMin = Vector2.zero; // setta // riga-ok
        rtTxtPin.anchorMax = Vector2.one; // setta // riga-ok
        rtTxtPin.offsetMin = Vector2.zero; // setta // riga-ok
        rtTxtPin.offsetMax = Vector2.zero; // setta // riga-ok

        Text txtPin = txtPinObj.AddComponent<Text>(); // setta // riga-ok
        txtPin.font = defaultFont; // setta // riga-ok
        txtPin.text = $"PIN: [ {codicePin} ]"; // setta // riga-ok
        txtPin.fontSize = 22; // setta // riga-ok
        txtPin.fontStyle = FontStyle.Bold; // setta // riga-ok
        txtPin.color = ColoreAquaNeon; // setta // riga-ok
        txtPin.alignment = TextAnchor.MiddleCenter; // setta // riga-ok
    } // chiude // riga-ok

    #region Costruzione Dinamica Canvas // prep ok // riga-ok

    // blocco: funzione fa cose
    private void CostruisciUISeNecessario() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (pannelloOlogramma != null) return; // se ok // riga-ok

        canvasRoot = GetComponentInChildren<Canvas>(); // setta // riga-ok
        // blocco: controlla se va
        if (canvasRoot == null) // se ok // riga-ok
        { // apre // riga-ok
            GameObject canvasObj = new GameObject("DatapadOlogrammaCanvas"); // setta // riga-ok
            canvasObj.transform.SetParent(transform, false); // chiama // riga-ok
            canvasRoot = canvasObj.AddComponent<Canvas>(); // setta // riga-ok
            canvasRoot.renderMode = RenderMode.ScreenSpaceOverlay; // setta // riga-ok
            canvasRoot.sortingOrder = 998; // setta // riga-ok

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>(); // setta // riga-ok
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // setta // riga-ok
            scaler.referenceResolution = new Vector2(1920, 1080); // setta // riga-ok
            scaler.matchWidthOrHeight = 0.5f; // setta // riga-ok
            scaler.dynamicPixelsPerUnit = 3.0f; // setta // riga-ok

            canvasObj.AddComponent<GraphicRaycaster>(); // chiama // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            CanvasScaler scaler = canvasRoot.GetComponent<CanvasScaler>(); // setta // riga-ok
            // blocco: controlla se va
            if (scaler != null) // se ok // riga-ok
            { // apre // riga-ok
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // setta // riga-ok
                scaler.referenceResolution = new Vector2(1920, 1080); // setta // riga-ok
                scaler.matchWidthOrHeight = 0.5f; // setta // riga-ok
                scaler.dynamicPixelsPerUnit = 3.0f; // setta // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // Overlay sfondo oscurato
        bgOverlay = new GameObject("DarkHoloOverlay"); // setta // riga-ok
        bgOverlay.transform.SetParent(canvasRoot.transform, false); // chiama // riga-ok
        RectTransform rtOverlay = bgOverlay.AddComponent<RectTransform>(); // setta // riga-ok
        rtOverlay.anchorMin = Vector2.zero; // setta // riga-ok
        rtOverlay.anchorMax = Vector2.one; // setta // riga-ok
        rtOverlay.offsetMin = Vector2.zero; // setta // riga-ok
        rtOverlay.offsetMax = Vector2.zero; // setta // riga-ok
        Image imgOverlay = bgOverlay.AddComponent<Image>(); // setta // riga-ok
        imgOverlay.color = new Color(0.01f, 0.04f, 0.06f, 0.70f); // setta // riga-ok

        // Finestra Principale Ologramma
        pannelloOlogramma = new GameObject("PannelloOlogramma"); // setta // riga-ok
        pannelloOlogramma.transform.SetParent(canvasRoot.transform, false); // chiama // riga-ok
        RectTransform rtPanel = pannelloOlogramma.AddComponent<RectTransform>(); // setta // riga-ok
        rtPanel.sizeDelta = new Vector2(820, 680); // setta // riga-ok
        rtPanel.anchoredPosition = Vector2.zero; // setta // riga-ok

        imgPannelloFrame = pannelloOlogramma.AddComponent<Image>(); // setta // riga-ok
        imgPannelloFrame.color = ColoreSfondoOlogramma; // setta // riga-ok

        // Bordo / Cornice Olografica
        GameObject frameBorder = new GameObject("HoloBorder"); // setta // riga-ok
        frameBorder.transform.SetParent(pannelloOlogramma.transform, false); // chiama // riga-ok
        RectTransform rtBorder = frameBorder.AddComponent<RectTransform>(); // setta // riga-ok
        rtBorder.anchorMin = Vector2.zero; // setta // riga-ok
        rtBorder.anchorMax = Vector2.one; // setta // riga-ok
        rtBorder.offsetMin = new Vector2(-3, -3); // setta // riga-ok
        rtBorder.offsetMax = new Vector2(3, 3); // setta // riga-ok
        Image imgBorder = frameBorder.AddComponent<Image>(); // setta // riga-ok
        imgBorder.color = ColoreBordoFrame; // setta // riga-ok
        imgBorder.raycastTarget = false; // setta // riga-ok
        frameBorder.transform.SetAsFirstSibling(); // Dietro al pannello // ok qua // riga-ok

        // Header Ologramma
        GameObject headerObj = new GameObject("HoloHeader"); // setta // riga-ok
        headerObj.transform.SetParent(pannelloOlogramma.transform, false); // chiama // riga-ok
        RectTransform rtHeader = headerObj.AddComponent<RectTransform>(); // setta // riga-ok
        rtHeader.anchorMin = new Vector2(0, 1); // setta // riga-ok
        rtHeader.anchorMax = new Vector2(1, 1); // setta // riga-ok
        rtHeader.pivot = new Vector2(0.5f, 1); // setta // riga-ok
        rtHeader.sizeDelta = new Vector2(0, 95); // setta // riga-ok
        rtHeader.anchoredPosition = Vector2.zero; // setta // riga-ok
        Image imgHeader = headerObj.AddComponent<Image>(); // setta // riga-ok
        imgHeader.color = new Color(0.018f, 0.10f, 0.13f, 1f); // setta // riga-ok

        // Icona / Tag LED Ologramma
        GameObject ledTagObj = new GameObject("LedTag"); // setta // riga-ok
        ledTagObj.transform.SetParent(headerObj.transform, false); // chiama // riga-ok
        RectTransform rtTag = ledTagObj.AddComponent<RectTransform>(); // setta // riga-ok
        rtTag.anchorMin = new Vector2(0, 1); // setta // riga-ok
        rtTag.anchorMax = new Vector2(0, 1); // setta // riga-ok
        rtTag.pivot = new Vector2(0, 1); // setta // riga-ok
        rtTag.sizeDelta = new Vector2(350, 25); // setta // riga-ok
        rtTag.anchoredPosition = new Vector2(20, -10); // setta // riga-ok
        Text txtTag = ledTagObj.AddComponent<Text>(); // setta // riga-ok
        txtTag.font = defaultFont; // setta // riga-ok
        txtTag.text = "◆ PROIEZIONE OLOGRAFICA LED // CANALE SICUREZZA ◆"; // setta // riga-ok
        txtTag.fontSize = 13; // setta // riga-ok
        txtTag.fontStyle = FontStyle.Bold; // setta // riga-ok
        txtTag.color = ColoreAquaNeon; // setta // riga-ok

        // Titolo Principale
        GameObject titoloObj = new GameObject("Titolo"); // setta // riga-ok
        titoloObj.transform.SetParent(headerObj.transform, false); // chiama // riga-ok
        RectTransform rtTitolo = titoloObj.AddComponent<RectTransform>(); // setta // riga-ok
        rtTitolo.anchorMin = new Vector2(0, 0); // setta // riga-ok
        rtTitolo.anchorMax = new Vector2(1, 1); // setta // riga-ok
        rtTitolo.offsetMin = new Vector2(20, 25); // setta // riga-ok
        rtTitolo.offsetMax = new Vector2(-70, -30); // setta // riga-ok
        txtTitolo = titoloObj.AddComponent<Text>(); // setta // riga-ok
        txtTitolo.font = defaultFont; // setta // riga-ok
        txtTitolo.fontSize = 22; // setta // riga-ok
        txtTitolo.fontStyle = FontStyle.Bold; // setta // riga-ok
        txtTitolo.alignment = TextAnchor.MiddleLeft; // setta // riga-ok
        txtTitolo.color = ColoreAquaChiaro; // setta // riga-ok

        // Sottotitolo
        GameObject subObj = new GameObject("Sottotitolo"); // setta // riga-ok
        subObj.transform.SetParent(headerObj.transform, false); // chiama // riga-ok
        RectTransform rtSub = subObj.AddComponent<RectTransform>(); // setta // riga-ok
        rtSub.anchorMin = new Vector2(0, 0); // setta // riga-ok
        rtSub.anchorMax = new Vector2(1, 0); // setta // riga-ok
        rtSub.pivot = new Vector2(0.5f, 0); // setta // riga-ok
        rtSub.sizeDelta = new Vector2(0, 24); // setta // riga-ok
        rtSub.offsetMin = new Vector2(20, 6); // setta // riga-ok
        rtSub.offsetMax = new Vector2(-70, 30); // setta // riga-ok
        txtSottotitolo = subObj.AddComponent<Text>(); // setta // riga-ok
        txtSottotitolo.font = defaultFont; // setta // riga-ok
        txtSottotitolo.fontSize = 15; // setta // riga-ok
        txtSottotitolo.alignment = TextAnchor.MiddleLeft; // setta // riga-ok
        txtSottotitolo.color = new Color(0.5f, 0.85f, 0.85f); // setta // riga-ok

        // Pulsante Chiudi X
        GameObject btnCloseObj = new GameObject("BtnClose"); // setta // riga-ok
        btnCloseObj.transform.SetParent(headerObj.transform, false); // chiama // riga-ok
        RectTransform rtClose = btnCloseObj.AddComponent<RectTransform>(); // setta // riga-ok
        rtClose.anchorMin = new Vector2(1, 0.5f); // setta // riga-ok
        rtClose.anchorMax = new Vector2(1, 0.5f); // setta // riga-ok
        rtClose.sizeDelta = new Vector2(40, 40); // setta // riga-ok
        rtClose.anchoredPosition = new Vector2(-25, 0); // setta // riga-ok
        Image imgClose = btnCloseObj.AddComponent<Image>(); // setta // riga-ok
        imgClose.color = new Color(0.1f, 0.35f, 0.35f); // setta // riga-ok
        Button btnClose = btnCloseObj.AddComponent<Button>(); // setta // riga-ok
        btnClose.onClick.AddListener(ChiudiOlogramma); // chiama // riga-ok

        GameObject txtCloseObj = new GameObject("X"); // setta // riga-ok
        txtCloseObj.transform.SetParent(btnCloseObj.transform, false); // chiama // riga-ok
        RectTransform rtTxtClose = txtCloseObj.AddComponent<RectTransform>(); // setta // riga-ok
        rtTxtClose.anchorMin = Vector2.zero; // setta // riga-ok
        rtTxtClose.anchorMax = Vector2.one; // setta // riga-ok
        Text txtClose = txtCloseObj.AddComponent<Text>(); // setta // riga-ok
        txtClose.font = defaultFont; // setta // riga-ok
        txtClose.text = "✕"; // setta // riga-ok
        txtClose.fontSize = 20; // setta // riga-ok
        txtClose.alignment = TextAnchor.MiddleCenter; // setta // riga-ok
        txtClose.color = ColoreAquaNeon; // setta // riga-ok

        // Area di Scorrimento (ScrollView) per le porte
        GameObject scrollObj = new GameObject("HoloScrollView"); // setta // riga-ok
        scrollObj.transform.SetParent(pannelloOlogramma.transform, false); // chiama // riga-ok
        RectTransform rtScroll = scrollObj.AddComponent<RectTransform>(); // setta // riga-ok
        rtScroll.anchorMin = Vector2.zero; // setta // riga-ok
        rtScroll.anchorMax = Vector2.one; // setta // riga-ok
        rtScroll.offsetMin = new Vector2(20, 75); // setta // riga-ok
        rtScroll.offsetMax = new Vector2(-20, -110); // setta // riga-ok

        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>(); // setta // riga-ok
        scrollRect.horizontal = false; // setta // riga-ok
        scrollRect.vertical = true; // setta // riga-ok
        scrollRect.movementType = ScrollRect.MovementType.Clamped; // setta // riga-ok

        // Viewport
        GameObject viewportObj = new GameObject("Viewport"); // setta // riga-ok
        viewportObj.transform.SetParent(scrollObj.transform, false); // chiama // riga-ok
        RectTransform rtView = viewportObj.AddComponent<RectTransform>(); // setta // riga-ok
        rtView.anchorMin = Vector2.zero; // setta // riga-ok
        rtView.anchorMax = Vector2.one; // setta // riga-ok
        rtView.sizeDelta = Vector2.zero; // setta // riga-ok
        rtView.pivot = new Vector2(0, 1); // setta // riga-ok
        Image imgView = viewportObj.AddComponent<Image>(); // setta // riga-ok
        imgView.color = Color.white; // setta // riga-ok
        Mask mask = viewportObj.AddComponent<Mask>(); // setta // riga-ok
        mask.showMaskGraphic = false; // setta // riga-ok

        // Content
        GameObject contentObj = new GameObject("Content"); // setta // riga-ok
        contentObj.transform.SetParent(viewportObj.transform, false); // chiama // riga-ok
        RectTransform rtContent = contentObj.AddComponent<RectTransform>(); // setta // riga-ok
        rtContent.anchorMin = new Vector2(0, 1); // setta // riga-ok
        rtContent.anchorMax = new Vector2(1, 1); // setta // riga-ok
        rtContent.pivot = new Vector2(0.5f, 1); // setta // riga-ok
        rtContent.sizeDelta = new Vector2(0, 0); // setta // riga-ok

        VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>(); // setta // riga-ok
        vlg.spacing = 10; // setta // riga-ok
        vlg.childControlWidth = true; // setta // riga-ok
        vlg.childControlHeight = false; // setta // riga-ok
        vlg.childForceExpandWidth = true; // setta // riga-ok
        vlg.childForceExpandHeight = false; // setta // riga-ok

        ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>(); // setta // riga-ok
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize; // setta // riga-ok

        scrollRect.viewport = rtView; // setta // riga-ok
        scrollRect.content = rtContent; // setta // riga-ok
        contentContainer = contentObj.transform; // setta // riga-ok

        // Footer Ologramma
        GameObject footerObj = new GameObject("HoloFooter"); // setta // riga-ok
        footerObj.transform.SetParent(pannelloOlogramma.transform, false); // chiama // riga-ok
        RectTransform rtFooter = footerObj.AddComponent<RectTransform>(); // setta // riga-ok
        rtFooter.anchorMin = new Vector2(0, 0); // setta // riga-ok
        rtFooter.anchorMax = new Vector2(1, 0); // setta // riga-ok
        rtFooter.pivot = new Vector2(0.5f, 0); // setta // riga-ok
        rtFooter.sizeDelta = new Vector2(0, 65); // setta // riga-ok
        rtFooter.anchoredPosition = Vector2.zero; // setta // riga-ok
        Image imgFooter = footerObj.AddComponent<Image>(); // setta // riga-ok
        imgFooter.color = new Color(0.015f, 0.08f, 0.10f, 1f); // setta // riga-ok

        // Testo Diagnostica
        GameObject diagObj = new GameObject("TxtDiagnostica"); // setta // riga-ok
        diagObj.transform.SetParent(footerObj.transform, false); // chiama // riga-ok
        RectTransform rtDiag = diagObj.AddComponent<RectTransform>(); // setta // riga-ok
        rtDiag.anchorMin = new Vector2(0, 0); // setta // riga-ok
        rtDiag.anchorMax = new Vector2(0.65f, 1); // setta // riga-ok
        rtDiag.offsetMin = new Vector2(20, 0); // setta // riga-ok
        rtDiag.offsetMax = Vector2.zero; // setta // riga-ok
        txtStatusDiagnostica = diagObj.AddComponent<Text>(); // setta // riga-ok
        txtStatusDiagnostica.font = defaultFont; // setta // riga-ok
        txtStatusDiagnostica.fontSize = 14; // setta // riga-ok
        txtStatusDiagnostica.color = new Color(0.3f, 0.8f, 0.75f); // setta // riga-ok
        txtStatusDiagnostica.alignment = TextAnchor.MiddleLeft; // setta // riga-ok

        // Bottone Chiudi Footer
        GameObject btnHoloCloseObj = new GameObject("BtnHoloClose"); // setta // riga-ok
        btnHoloCloseObj.transform.SetParent(footerObj.transform, false); // chiama // riga-ok
        RectTransform rtHoloClose = btnHoloCloseObj.AddComponent<RectTransform>(); // setta // riga-ok
        rtHoloClose.anchorMin = new Vector2(1, 0.5f); // setta // riga-ok
        rtHoloClose.anchorMax = new Vector2(1, 0.5f); // setta // riga-ok
        rtHoloClose.sizeDelta = new Vector2(240, 44); // setta // riga-ok
        rtHoloClose.anchoredPosition = new Vector2(-20, 0); // setta // riga-ok

        Image imgBtnHolo = btnHoloCloseObj.AddComponent<Image>(); // setta // riga-ok
        imgBtnHolo.color = new Color(0.0f, 0.45f, 0.42f, 1f); // setta // riga-ok

        Button btnHoloClose = btnHoloCloseObj.AddComponent<Button>(); // setta // riga-ok
        btnHoloClose.onClick.AddListener(ChiudiOlogramma); // chiama // riga-ok

        GameObject txtBtnObj = new GameObject("Text"); // setta // riga-ok
        txtBtnObj.transform.SetParent(btnHoloCloseObj.transform, false); // chiama // riga-ok
        RectTransform rtTxtBtn = txtBtnObj.AddComponent<RectTransform>(); // setta // riga-ok
        rtTxtBtn.anchorMin = Vector2.zero; // setta // riga-ok
        rtTxtBtn.anchorMax = Vector2.one; // setta // riga-ok
        Text txtBtn = txtBtnObj.AddComponent<Text>(); // setta // riga-ok
        txtBtn.font = defaultFont; // setta // riga-ok
        txtBtn.text = "[✕] CHIUDI HOLOPAD (ESC)"; // setta // riga-ok
        txtBtn.fontSize = 15; // setta // riga-ok
        txtBtn.fontStyle = FontStyle.Bold; // setta // riga-ok
        txtBtn.color = Color.white; // setta // riga-ok
        txtBtn.alignment = TextAnchor.MiddleCenter; // setta // riga-ok

        // Deattiva all'avvio
        // blocco: controlla se va
        if (bgOverlay != null) bgOverlay.SetActive(false); // se ok // riga-ok
        // blocco: controlla se va
        if (pannelloOlogramma != null) pannelloOlogramma.SetActive(false); // se ok // riga-ok
        // blocco: controlla se va
        if (canvasRoot != null) canvasRoot.gameObject.SetActive(false); // se ok // riga-ok
    } // chiude // riga-ok

    #endregion // prep ok // riga-ok
} // chiude // riga-ok
