// ============================================================================
// Crisis Protocol / Sector Containment - Interfaccia utente
// File: .\Assets\CrisisProtocol\Scripts\UI\TerminalePortaUI.cs
// Responsabilita': aggiorna HUD, menu, overlay, gauge, notifiche o schermate di supporto in base agli eventi gameplay.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using System.Collections; // usa lib // riga-ok
using System.Collections.Generic; // usa lib // riga-ok
using UnityEngine; // usa lib // riga-ok
using UnityEngine.UI; // usa lib // riga-ok
using UnityEngine.InputSystem; // usa lib // riga-ok
using CrisisProtocol.UI; // usa lib // riga-ok

/// <summary>
/// Interfaccia grafica completa per i Terminali di Sicurezza delle Porte.
/// Include Tastierino PIN interattivo e Minigioco di Calibrazione/Bypass Circuiti a Tempo.
/// </summary>
// blocco: classe x roba grossa
public class TerminalePortaUI : MonoBehaviour // classe qui // riga-ok
{ // apre // riga-ok
    public static TerminalePortaUI Instance { get; private set; } // roba pub // riga-ok

    private const string ModalOwner = "TerminalePorta"; // roba pub // riga-ok

    private TerminalePorta terminaleAttivo; // roba pub // riga-ok
    private Canvas canvasRoot; // roba pub // riga-ok
    private GameObject pannelloPrincipale; // roba pub // riga-ok
    private GameObject bgOverlay; // roba pub // riga-ok

    // Elementi UI Comuni
    private Text testoTitoloTerminale; // roba pub // riga-ok
    private Text testoStatoMessaggio; // roba pub // riga-ok
    private GameObject tabKeypadObj; // roba pub // riga-ok
    private GameObject tabBypassObj; // roba pub // riga-ok

    // Elementi Keypad
    private Text testoDisplayCodice; // roba pub // riga-ok
    private string codiceDigitato = ""; // roba pub // riga-ok

    // Elementi Minigioco Bypass
    private RectTransform barraOscillatore; // roba pub // riga-ok
    private RectTransform zonaVerdeTarget; // roba pub // riga-ok
    private Text testoNodoProgresso; // roba pub // riga-ok
    private Text testoIstruzioniBypass; // roba pub // riga-ok
    private int nodoCorrente = 1; // roba pub // riga-ok
    private int nodiTotali = 3; // roba pub // riga-ok
    private float velocitaOscillatore = 2.0f; // roba pub // riga-ok
    private float ampiezzaBarra = 300f; // Larghezza totale barra calibrazione // roba pub // riga-ok
    private float zonaVerdeMin = -50f; // roba pub // riga-ok
    private float zonaVerdeMax = 50f; // roba pub // riga-ok
    private bool bypassInCorso = false; // roba pub // riga-ok
    private bool bypassCompletato = false; // roba pub // riga-ok

    private bool inAnimazioneChiusura = false; // roba pub // riga-ok

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

        CostruisciUISeNecessario(); // chiama // riga-ok
    } // chiude // riga-ok

    void Update() // chiama // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (terminaleAttivo == null || canvasRoot == null || !canvasRoot.gameObject.activeSelf || pannelloPrincipale == null || !pannelloPrincipale.activeSelf || inAnimazioneChiusura) // se ok // riga-ok
            return; // torna val // riga-ok

        // Gestione tasto ESC per chiudere
        // blocco: controlla se va
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) // se ok // riga-ok
        { // apre // riga-ok
            ChiudiTerminale(); // chiama // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        // Se la scheda attiva è il Keypad: ascolta la tastiera numerica
        // blocco: controlla se va
        if (tabKeypadObj != null && tabKeypadObj.activeSelf) // se ok // riga-ok
        { // apre // riga-ok
            GestisciInputTastieraKeypad(); // chiama // riga-ok
        } // chiude // riga-ok

        // Se la scheda attiva è il Minigioco Bypass: anima l'indicatore
        // blocco: controlla se va
        if (tabBypassObj != null && tabBypassObj.activeSelf && bypassInCorso && !bypassCompletato) // se ok // riga-ok
        { // apre // riga-ok
            AggiornaMinigiocoBypass(); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void ApriTerminale(TerminalePorta terminale) // roba pub // riga-ok
    { // apre // riga-ok
        terminaleAttivo = terminale; // setta // riga-ok
        CostruisciUISeNecessario(); // chiama // riga-ok

        // blocco: controlla se va
        if (canvasRoot != null) canvasRoot.gameObject.SetActive(true); // se ok // riga-ok
        // blocco: controlla se va
        if (bgOverlay != null) bgOverlay.SetActive(true); // se ok // riga-ok
        // blocco: controlla se va
        if (pannelloPrincipale != null) pannelloPrincipale.SetActive(true); // se ok // riga-ok

        codiceDigitato = ""; // setta // riga-ok
        AggiornaDisplayCodice(); // chiama // riga-ok

        nodoCorrente = 1; // setta // riga-ok
        bypassCompletato = false; // setta // riga-ok
        bypassInCorso = true; // setta // riga-ok
        ConfiguraDifficoltaNodo(); // chiama // riga-ok

        // blocco: controlla se va
        if (testoTitoloTerminale != null) // se ok // riga-ok
            testoTitoloTerminale.text = terminale.nomeTerminale.ToUpper(); // setta // riga-ok

        // blocco: controlla se va
        if (testoStatoMessaggio != null) // se ok // riga-ok
        { // apre // riga-ok
            testoStatoMessaggio.text = "SISTEMA DI SICUREZZA // DIGITA IL CODICE PIN PER SBLOCCARE"; // setta // riga-ok
            testoStatoMessaggio.color = new Color(0.3f, 0.85f, 1f); // setta // riga-ok
        } // chiude // riga-ok

        // Apre sempre direttamente la schermata con il tastierino PIN
        MostraTabKeypad(); // chiama // riga-ok

        // Blocca i movimenti di gioco e mostra il cursore del mouse
        ModalUIState.TryOpen(ModalOwner); // chiama // riga-ok
        Cursor.visible = true; // setta // riga-ok
        Cursor.lockState = CursorLockMode.None; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void ChiudiTerminale() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (inAnimazioneChiusura) return; // se ok // riga-ok

        // blocco: controlla se va
        if (pannelloPrincipale != null) // se ok // riga-ok
            pannelloPrincipale.SetActive(false); // chiama // riga-ok

        // blocco: controlla se va
        if (bgOverlay != null) // se ok // riga-ok
            bgOverlay.SetActive(false); // chiama // riga-ok

        // blocco: controlla se va
        if (canvasRoot != null) // se ok // riga-ok
            canvasRoot.gameObject.SetActive(false); // chiama // riga-ok

        terminaleAttivo = null; // setta // riga-ok
        ModalUIState.Close(ModalOwner); // chiama // riga-ok
        Cursor.visible = false; // setta // riga-ok
        Cursor.lockState = CursorLockMode.Locked; // setta // riga-ok
    } // chiude // riga-ok

    #region Gestione Tab // prep ok // riga-ok

    // blocco: funzione fa cose
    private void MostraTabKeypad() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (tabKeypadObj != null) tabKeypadObj.SetActive(true); // se ok // riga-ok
        // blocco: controlla se va
        if (tabBypassObj != null) tabBypassObj.SetActive(false); // se ok // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void MostraTabBypass() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (tabKeypadObj != null) tabKeypadObj.SetActive(false); // se ok // riga-ok
        // blocco: controlla se va
        if (tabBypassObj != null) tabBypassObj.SetActive(true); // se ok // riga-ok
        nodoCorrente = 1; // setta // riga-ok
        ConfiguraDifficoltaNodo(); // chiama // riga-ok
    } // chiude // riga-ok

    #endregion // prep ok // riga-ok

    #region Logica Keypad PIN // prep ok // riga-ok

    private bool inAnimazioneErroreCritico = false; // roba pub // riga-ok

    // blocco: funzione fa cose
    public void InserisciCifra(string cifra) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (inAnimazioneErroreCritico) return; // se ok // riga-ok

        // blocco: controlla se va
        if (codiceDigitato.Length < 6) // se ok // riga-ok
        { // apre // riga-ok
            codiceDigitato += cifra; // setta // riga-ok
            AggiornaDisplayCodice(); // chiama // riga-ok

            // Quando si raggiungono le 4 cifre, valida subito
            // blocco: controlla se va
            if (terminaleAttivo != null && codiceDigitato.Length >= terminaleAttivo.codiceSegreto.Length) // se ok // riga-ok
            { // apre // riga-ok
                ConfermaCodice(); // chiama // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void CancellaCifra() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (inAnimazioneErroreCritico) return; // se ok // riga-ok

        // blocco: controlla se va
        if (codiceDigitato.Length > 0) // se ok // riga-ok
        { // apre // riga-ok
            codiceDigitato = codiceDigitato.Substring(0, codiceDigitato.Length - 1); // setta // riga-ok
            AggiornaDisplayCodice(); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void ConfermaCodice() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (terminaleAttivo == null || inAnimazioneErroreCritico) return; // se ok // riga-ok

        // Se il terminale ha il tastierino guasto, fa inserire il PIN ma subito dopo scatena l'ERRORE CRITICO e passa al bypass
        // blocco: controlla se va
        if (terminaleAttivo.pinGuastoRichiedeBypass) // se ok // riga-ok
        { // apre // riga-ok
            StartCoroutine(SequenzaErroreCriticoBypass()); // corutina // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (codiceDigitato == terminaleAttivo.codiceSegreto) // se ok // riga-ok
        { // apre // riga-ok
            StartCoroutine(SequenzaSuccesso("ACCESSO AUTORIZZATO // CODICE CORRETTO")); // corutina // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            StartCoroutine(SequenzaErrore("ACCESSO NEGATO // CODICE ERRATO")); // corutina // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private IEnumerator SequenzaErroreCriticoBypass() // roba pub // riga-ok
    { // apre // riga-ok
        inAnimazioneErroreCritico = true; // setta // riga-ok

        // blocco: controlla se va
        if (testoDisplayCodice != null) // se ok // riga-ok
        { // apre // riga-ok
            testoDisplayCodice.text = "ERR-CRITICO"; // setta // riga-ok
            testoDisplayCodice.color = Color.red; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (testoStatoMessaggio != null) // se ok // riga-ok
        { // apre // riga-ok
            testoStatoMessaggio.text = "✗ ERRORE CRITICO // TASTIERINO CORROTTO - BYPASS OBBLIGATORIO!"; // setta // riga-ok
            testoStatoMessaggio.color = Color.red; // setta // riga-ok
        } // chiude // riga-ok

        yield return new WaitForSecondsRealtime(1.1f); // aspetta // riga-ok

        // Passa automaticamente alla scheda di Bypass Circuiti
        MostraTabBypass(); // chiama // riga-ok

        // blocco: controlla se va
        if (testoStatoMessaggio != null) // se ok // riga-ok
        { // apre // riga-ok
            testoStatoMessaggio.text = "PROTOCOLLO DI EMERGENZA // ESEGUI IL BYPASS MANUALE PER APRIRE"; // setta // riga-ok
            testoStatoMessaggio.color = new Color(1f, 0.5f, 0.1f); // setta // riga-ok
        } // chiude // riga-ok

        codiceDigitato = ""; // setta // riga-ok
        AggiornaDisplayCodice(); // chiama // riga-ok
        inAnimazioneErroreCritico = false; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void AggiornaDisplayCodice() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (testoDisplayCodice == null) return; // se ok // riga-ok

        // blocco: controlla se va
        if (string.IsNullOrEmpty(codiceDigitato)) // se ok // riga-ok
        { // apre // riga-ok
            testoDisplayCodice.text = "- - - -"; // setta // riga-ok
            testoDisplayCodice.color = new Color(0.5f, 0.7f, 0.9f, 0.6f); // setta // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            testoDisplayCodice.text = codiceDigitato; // setta // riga-ok
            testoDisplayCodice.color = Color.white; // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void GestisciInputTastieraKeypad() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (Keyboard.current == null) return; // se ok // riga-ok

        // Cifre 0 - 9
        // blocco: gira piu volte
        for (int i = 0; i <= 9; i++) // ciclo x // riga-ok
        { // apre // riga-ok
            Key key = Key.Digit0 + i; // setta // riga-ok
            Key numpadKey = Key.Numpad0 + i; // setta // riga-ok
            // blocco: controlla se va
            if (Keyboard.current[key].wasPressedThisFrame || Keyboard.current[numpadKey].wasPressedThisFrame) // se ok // riga-ok
            { // apre // riga-ok
                InserisciCifra(i.ToString()); // chiama // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // Backspace / Cancella
        // blocco: controlla se va
        if (Keyboard.current.backspaceKey.wasPressedThisFrame || Keyboard.current.deleteKey.wasPressedThisFrame) // se ok // riga-ok
        { // apre // riga-ok
            CancellaCifra(); // chiama // riga-ok
        } // chiude // riga-ok

        // Enter / Conferma
        // blocco: controlla se va
        if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame) // se ok // riga-ok
        { // apre // riga-ok
            ConfermaCodice(); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    #endregion // prep ok // riga-ok

    #region Logica Minigioco Bypass // prep ok // riga-ok

    // blocco: funzione fa cose
    private void ConfiguraDifficoltaNodo() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: scegli strada
        switch (nodoCorrente) // scegli // riga-ok
        { // apre // riga-ok
            case 1: // caso // riga-ok
                velocitaOscillatore = 1.6f; // setta // riga-ok
                zonaVerdeMin = -65f; // setta // riga-ok
                zonaVerdeMax = 65f; // setta // riga-ok
                break; // stop // riga-ok
            case 2: // caso // riga-ok
                velocitaOscillatore = 2.2f; // setta // riga-ok
                zonaVerdeMin = -50f; // setta // riga-ok
                zonaVerdeMax = 50f; // setta // riga-ok
                break; // stop // riga-ok
            case 3: // caso // riga-ok
                // Fase 3 ricalibrata: più lenta, fluida e fattibile
                velocitaOscillatore = 2.8f; // setta // riga-ok
                zonaVerdeMin = -40f; // setta // riga-ok
                zonaVerdeMax = 40f; // setta // riga-ok
                break; // stop // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (zonaVerdeTarget != null) // se ok // riga-ok
        { // apre // riga-ok
            float larghezza = zonaVerdeMax - zonaVerdeMin; // setta // riga-ok
            zonaVerdeTarget.sizeDelta = new Vector2(larghezza, 36f); // setta // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (testoNodoProgresso != null) // se ok // riga-ok
            testoNodoProgresso.text = $"SICUREZZA CIRCUITO: NODO [{nodoCorrente}/{nodiTotali}]"; // setta // riga-ok

        // blocco: controlla se va
        if (testoIstruzioniBypass != null) // se ok // riga-ok
            testoIstruzioniBypass.text = "Premi [SPAZIO] o clicca [CALIBRA CIRCUITO] quando l'indicatore è nella ZONA VERDE!"; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void AggiornaMinigiocoBypass() // roba pub // riga-ok
    { // apre // riga-ok
        float t = Mathf.PingPong(Time.unscaledTime * velocitaOscillatore, 1f); // setta // riga-ok
        float posX = Mathf.Lerp(-ampiezzaBarra / 2f, ampiezzaBarra / 2f, t); // setta // riga-ok

        // blocco: controlla se va
        if (barraOscillatore != null) // se ok // riga-ok
        { // apre // riga-ok
            barraOscillatore.anchoredPosition = new Vector2(posX, 0f); // setta // riga-ok
        } // chiude // riga-ok

        // Ascolta il tasto Spazio per convalidare il bypass
        // blocco: controlla se va
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) // se ok // riga-ok
        { // apre // riga-ok
            TentaBypassNodo(); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void TentaBypassNodo() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (!bypassInCorso || bypassCompletato) return; // se ok // riga-ok

        float posizioneAttuale = barraOscillatore.anchoredPosition.x; // setta // riga-ok

        // Verifica se l'indicatore si trova all'interno della zona verde
        // blocco: controlla se va
        if (posizioneAttuale >= zonaVerdeMin && posizioneAttuale <= zonaVerdeMax) // se ok // riga-ok
        { // apre // riga-ok
            // Successo per questo nodo
            // blocco: controlla se va
            if (nodoCorrente < nodiTotali) // se ok // riga-ok
            { // apre // riga-ok
                nodoCorrente++; // ok qua // riga-ok
                StartCoroutine(FlashNodo(true)); // corutina // riga-ok
                ConfiguraDifficoltaNodo(); // chiama // riga-ok
            } // chiude // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
            { // apre // riga-ok
                bypassCompletato = true; // setta // riga-ok
                StartCoroutine(SequenzaSuccesso("BYPASS CIRCUITI COMPLETATO // SISTEMA SOVRASCRITTO")); // corutina // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            // Errore tempistica
            nodoCorrente = 1; // setta // riga-ok
            ConfiguraDifficoltaNodo(); // chiama // riga-ok
            StartCoroutine(FlashNodo(false)); // corutina // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private IEnumerator FlashNodo(bool successo) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (testoStatoMessaggio == null) yield break; // se ok // riga-ok

        testoStatoMessaggio.text = successo ? $"✓ NODO SICUREZZA {nodoCorrente - 1} DISATTIVATO!" : "✗ ALLARME CIRCUITO: SOVRACCARICO! RESET NODO 1"; // setta // riga-ok
        testoStatoMessaggio.color = successo ? Color.green : Color.red; // setta // riga-ok

        yield return new WaitForSecondsRealtime(0.6f); // aspetta // riga-ok

        // blocco: controlla se va
        if (testoStatoMessaggio != null && !bypassCompletato) // se ok // riga-ok
        { // apre // riga-ok
            testoStatoMessaggio.text = "SISTEMA DI SICUREZZA IN BYPASS - MANTIENI LA SINCRONIZZAZIONE"; // setta // riga-ok
            testoStatoMessaggio.color = new Color(0.3f, 0.85f, 1f); // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    #endregion // prep ok // riga-ok

    #region Sequenze Risultato // prep ok // riga-ok

    // blocco: funzione fa cose
    private IEnumerator SequenzaSuccesso(string messaggio) // roba pub // riga-ok
    { // apre // riga-ok
        inAnimazioneChiusura = true; // setta // riga-ok

        // blocco: controlla se va
        if (testoStatoMessaggio != null) // se ok // riga-ok
        { // apre // riga-ok
            testoStatoMessaggio.text = "✓ " + messaggio; // setta // riga-ok
            testoStatoMessaggio.color = Color.green; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (terminaleAttivo != null) // se ok // riga-ok
        { // apre // riga-ok
            terminaleAttivo.OnAccessoGarantito(); // chiama // riga-ok
        } // chiude // riga-ok

        yield return new WaitForSecondsRealtime(1.0f); // aspetta // riga-ok

        inAnimazioneChiusura = false; // setta // riga-ok
        ChiudiTerminale(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private IEnumerator SequenzaErrore(string messaggio) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (testoStatoMessaggio != null) // se ok // riga-ok
        { // apre // riga-ok
            testoStatoMessaggio.text = "✗ " + messaggio; // setta // riga-ok
            testoStatoMessaggio.color = Color.red; // setta // riga-ok
        } // chiude // riga-ok

        codiceDigitato = ""; // setta // riga-ok
        AggiornaDisplayCodice(); // chiama // riga-ok

        yield return new WaitForSecondsRealtime(0.8f); // aspetta // riga-ok

        // blocco: controlla se va
        if (testoStatoMessaggio != null) // se ok // riga-ok
        { // apre // riga-ok
            testoStatoMessaggio.text = "INSERISCI CODICE DI ACCESSO A 4 CIFRE O ATTIVA BYPASS"; // setta // riga-ok
            testoStatoMessaggio.color = new Color(0.3f, 0.85f, 1f); // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    #endregion // prep ok // riga-ok

    #region Costruzione Dinamica UI // prep ok // riga-ok

    // blocco: funzione fa cose
    private void CostruisciUISeNecessario() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (pannelloPrincipale != null) return; // se ok // riga-ok

        canvasRoot = GetComponentInChildren<Canvas>(); // setta // riga-ok
        // blocco: controlla se va
        if (canvasRoot == null) // se ok // riga-ok
        { // apre // riga-ok
            GameObject canvasObj = new GameObject("TerminaleCanvas"); // setta // riga-ok
            canvasObj.transform.SetParent(transform, false); // chiama // riga-ok
            canvasRoot = canvasObj.AddComponent<Canvas>(); // setta // riga-ok
            canvasRoot.renderMode = RenderMode.ScreenSpaceOverlay; // setta // riga-ok
            canvasRoot.sortingOrder = 999; // setta // riga-ok

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
        bgOverlay = new GameObject("DarkOverlay"); // setta // riga-ok
        bgOverlay.transform.SetParent(canvasRoot.transform, false); // chiama // riga-ok
        RectTransform rtOverlay = bgOverlay.AddComponent<RectTransform>(); // setta // riga-ok
        rtOverlay.anchorMin = Vector2.zero; // setta // riga-ok
        rtOverlay.anchorMax = Vector2.one; // setta // riga-ok
        rtOverlay.offsetMin = Vector2.zero; // setta // riga-ok
        rtOverlay.offsetMax = Vector2.zero; // setta // riga-ok
        Image imgOverlay = bgOverlay.AddComponent<Image>(); // setta // riga-ok
        imgOverlay.color = new Color(0.02f, 0.05f, 0.08f, 0.85f); // setta // riga-ok

        // Finestra Principale Terminale
        pannelloPrincipale = new GameObject("PannelloTerminale"); // setta // riga-ok
        pannelloPrincipale.transform.SetParent(canvasRoot.transform, false); // chiama // riga-ok
        RectTransform rtPanel = pannelloPrincipale.AddComponent<RectTransform>(); // setta // riga-ok
        rtPanel.sizeDelta = new Vector2(650, 720); // setta // riga-ok
        rtPanel.anchoredPosition = Vector2.zero; // setta // riga-ok

        Image imgPanel = pannelloPrincipale.AddComponent<Image>(); // setta // riga-ok
        imgPanel.color = new Color(0.06f, 0.10f, 0.14f, 0.96f); // setta // riga-ok

        // Header Terminale
        GameObject headerObj = new GameObject("Header"); // setta // riga-ok
        headerObj.transform.SetParent(pannelloPrincipale.transform, false); // chiama // riga-ok
        RectTransform rtHeader = headerObj.AddComponent<RectTransform>(); // setta // riga-ok
        rtHeader.anchorMin = new Vector2(0, 1); // setta // riga-ok
        rtHeader.anchorMax = new Vector2(1, 1); // setta // riga-ok
        rtHeader.pivot = new Vector2(0.5f, 1); // setta // riga-ok
        rtHeader.sizeDelta = new Vector2(0, 70); // setta // riga-ok
        rtHeader.anchoredPosition = Vector2.zero; // setta // riga-ok
        Image imgHeader = headerObj.AddComponent<Image>(); // setta // riga-ok
        imgHeader.color = new Color(0.08f, 0.16f, 0.22f, 1f); // setta // riga-ok

        GameObject titoloObj = new GameObject("Titolo"); // setta // riga-ok
        titoloObj.transform.SetParent(headerObj.transform, false); // chiama // riga-ok
        RectTransform rtTitolo = titoloObj.AddComponent<RectTransform>(); // setta // riga-ok
        rtTitolo.anchorMin = Vector2.zero; // setta // riga-ok
        rtTitolo.anchorMax = Vector2.one; // setta // riga-ok
        rtTitolo.offsetMin = new Vector2(20, 0); // setta // riga-ok
        rtTitolo.offsetMax = new Vector2(-60, 0); // setta // riga-ok
        testoTitoloTerminale = titoloObj.AddComponent<Text>(); // setta // riga-ok
        testoTitoloTerminale.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf"); // setta // riga-ok
        testoTitoloTerminale.fontSize = 22; // setta // riga-ok
        testoTitoloTerminale.fontStyle = FontStyle.Bold; // setta // riga-ok
        testoTitoloTerminale.alignment = TextAnchor.MiddleLeft; // setta // riga-ok
        testoTitoloTerminale.color = new Color(0.4f, 0.9f, 1f); // setta // riga-ok
        testoTitoloTerminale.horizontalOverflow = HorizontalWrapMode.Overflow; // setta // riga-ok
        testoTitoloTerminale.verticalOverflow = VerticalWrapMode.Overflow; // setta // riga-ok

        // Pulsante Chiudi X
        GameObject btnCloseObj = new GameObject("BtnClose"); // setta // riga-ok
        btnCloseObj.transform.SetParent(headerObj.transform, false); // chiama // riga-ok
        RectTransform rtClose = btnCloseObj.AddComponent<RectTransform>(); // setta // riga-ok
        rtClose.anchorMin = new Vector2(1, 0.5f); // setta // riga-ok
        rtClose.anchorMax = new Vector2(1, 0.5f); // setta // riga-ok
        rtClose.sizeDelta = new Vector2(40, 40); // setta // riga-ok
        rtClose.anchoredPosition = new Vector2(-25, 0); // setta // riga-ok
        Image imgClose = btnCloseObj.AddComponent<Image>(); // setta // riga-ok
        imgClose.color = new Color(0.8f, 0.2f, 0.2f); // setta // riga-ok
        Button btnClose = btnCloseObj.AddComponent<Button>(); // setta // riga-ok
        btnClose.onClick.AddListener(ChiudiTerminale); // chiama // riga-ok

        GameObject txtCloseObj = new GameObject("X"); // setta // riga-ok
        txtCloseObj.transform.SetParent(btnCloseObj.transform, false); // chiama // riga-ok
        RectTransform rtTxtClose = txtCloseObj.AddComponent<RectTransform>(); // setta // riga-ok
        rtTxtClose.anchorMin = Vector2.zero; // setta // riga-ok
        rtTxtClose.anchorMax = Vector2.one; // setta // riga-ok
        Text txtClose = txtCloseObj.AddComponent<Text>(); // setta // riga-ok
        txtClose.font = testoTitoloTerminale.font; // setta // riga-ok
        txtClose.text = "✕"; // setta // riga-ok
        txtClose.fontSize = 20; // setta // riga-ok
        txtClose.alignment = TextAnchor.MiddleCenter; // setta // riga-ok
        txtClose.color = Color.white; // setta // riga-ok
        txtClose.horizontalOverflow = HorizontalWrapMode.Overflow; // setta // riga-ok
        txtClose.verticalOverflow = VerticalWrapMode.Overflow; // setta // riga-ok

        // Barra Messaggi di Stato
        GameObject statusObj = new GameObject("StatusMessage"); // setta // riga-ok
        statusObj.transform.SetParent(pannelloPrincipale.transform, false); // chiama // riga-ok
        RectTransform rtStatus = statusObj.AddComponent<RectTransform>(); // setta // riga-ok
        rtStatus.anchorMin = new Vector2(0, 1); // setta // riga-ok
        rtStatus.anchorMax = new Vector2(1, 1); // setta // riga-ok
        rtStatus.pivot = new Vector2(0.5f, 1); // setta // riga-ok
        rtStatus.sizeDelta = new Vector2(600, 45); // setta // riga-ok
        rtStatus.anchoredPosition = new Vector2(0, -75); // setta // riga-ok
        testoStatoMessaggio = statusObj.AddComponent<Text>(); // setta // riga-ok
        testoStatoMessaggio.font = testoTitoloTerminale.font; // setta // riga-ok
        testoStatoMessaggio.fontSize = 17; // setta // riga-ok
        testoStatoMessaggio.alignment = TextAnchor.MiddleCenter; // setta // riga-ok
        testoStatoMessaggio.horizontalOverflow = HorizontalWrapMode.Wrap; // setta // riga-ok
        testoStatoMessaggio.verticalOverflow = VerticalWrapMode.Overflow; // setta // riga-ok
        testoStatoMessaggio.lineSpacing = 1.15f; // setta // riga-ok

        // ── Creazione Contenitore Tab 1: KEYPAD ─────────────────────────────
        tabKeypadObj = new GameObject("Tab_Keypad"); // setta // riga-ok
        tabKeypadObj.transform.SetParent(pannelloPrincipale.transform, false); // chiama // riga-ok
        RectTransform rtKeypadTab = tabKeypadObj.AddComponent<RectTransform>(); // setta // riga-ok
        rtKeypadTab.anchorMin = Vector2.zero; // setta // riga-ok
        rtKeypadTab.anchorMax = Vector2.one; // setta // riga-ok
        rtKeypadTab.offsetMin = new Vector2(25, 20); // setta // riga-ok
        rtKeypadTab.offsetMax = new Vector2(-25, -125); // setta // riga-ok

        // Display PIN
        GameObject displayObj = new GameObject("DisplayPIN"); // setta // riga-ok
        displayObj.transform.SetParent(tabKeypadObj.transform, false); // chiama // riga-ok
        RectTransform rtDisplay = displayObj.AddComponent<RectTransform>(); // setta // riga-ok
        rtDisplay.anchorMin = new Vector2(0.5f, 1); // setta // riga-ok
        rtDisplay.anchorMax = new Vector2(0.5f, 1); // setta // riga-ok
        rtDisplay.sizeDelta = new Vector2(480, 65); // setta // riga-ok
        rtDisplay.anchoredPosition = new Vector2(0, -10); // setta // riga-ok
        Image imgDisplay = displayObj.AddComponent<Image>(); // setta // riga-ok
        imgDisplay.color = new Color(0.03f, 0.06f, 0.09f, 1f); // setta // riga-ok

        GameObject txtDisplayObj = new GameObject("TextPIN"); // setta // riga-ok
        txtDisplayObj.transform.SetParent(displayObj.transform, false); // chiama // riga-ok
        RectTransform rtTxtDisplay = txtDisplayObj.AddComponent<RectTransform>(); // setta // riga-ok
        rtTxtDisplay.anchorMin = Vector2.zero; // setta // riga-ok
        rtTxtDisplay.anchorMax = Vector2.one; // setta // riga-ok
        testoDisplayCodice = txtDisplayObj.AddComponent<Text>(); // setta // riga-ok
        testoDisplayCodice.font = testoTitoloTerminale.font; // setta // riga-ok
        testoDisplayCodice.fontSize = 32; // setta // riga-ok
        testoDisplayCodice.fontStyle = FontStyle.Bold; // setta // riga-ok
        testoDisplayCodice.alignment = TextAnchor.MiddleCenter; // setta // riga-ok

        // Griglia Tasti 0-9
        GameObject gridKeypad = new GameObject("GridKeypad"); // setta // riga-ok
        gridKeypad.transform.SetParent(tabKeypadObj.transform, false); // chiama // riga-ok
        RectTransform rtGrid = gridKeypad.AddComponent<RectTransform>(); // setta // riga-ok
        rtGrid.anchorMin = new Vector2(0.5f, 0); // setta // riga-ok
        rtGrid.anchorMax = new Vector2(0.5f, 1); // setta // riga-ok
        rtGrid.sizeDelta = new Vector2(420, 0); // setta // riga-ok
        rtGrid.anchoredPosition = new Vector2(0, -90); // setta // riga-ok

        GridLayoutGroup glg = gridKeypad.AddComponent<GridLayoutGroup>(); // setta // riga-ok
        glg.cellSize = new Vector2(125, 60); // setta // riga-ok
        glg.spacing = new Vector2(15, 12); // setta // riga-ok
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount; // setta // riga-ok
        glg.constraintCount = 3; // setta // riga-ok

        string[] tasti = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "⌫ CANC", "0", "↵ INVIO" }; // setta // riga-ok
        // blocco: gira piu volte
        foreach (string t in tasti) // ciclo x // riga-ok
        { // apre // riga-ok
            string valore = t; // setta // riga-ok
            // blocco: controlla se va
            if (valore == "⌫ CANC") // se ok // riga-ok
                CreaBottone(gridKeypad.transform, valore, CancellaCifra, new Color(0.6f, 0.25f, 0.25f)); // chiama // riga-ok
            // blocco: controlla se va
            else if (valore == "↵ INVIO") // se ok // riga-ok
                CreaBottone(gridKeypad.transform, valore, ConfermaCodice, new Color(0.2f, 0.65f, 0.35f)); // chiama // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
                CreaBottone(gridKeypad.transform, valore, () => InserisciCifra(valore)); // setta // riga-ok
        } // chiude // riga-ok

        // ── Creazione Contenitore Tab 2: MINIGIOCO BYPASS ───────────────────
        tabBypassObj = new GameObject("Tab_Bypass"); // setta // riga-ok
        tabBypassObj.transform.SetParent(pannelloPrincipale.transform, false); // chiama // riga-ok
        RectTransform rtBypassTab = tabBypassObj.AddComponent<RectTransform>(); // setta // riga-ok
        rtBypassTab.anchorMin = Vector2.zero; // setta // riga-ok
        rtBypassTab.anchorMax = Vector2.one; // setta // riga-ok
        rtBypassTab.offsetMin = new Vector2(25, 20); // setta // riga-ok
        rtBypassTab.offsetMax = new Vector2(-25, -125); // setta // riga-ok

        // Testo Progresso Nodo
        GameObject txtNodoObj = new GameObject("TxtNodo"); // setta // riga-ok
        txtNodoObj.transform.SetParent(tabBypassObj.transform, false); // chiama // riga-ok
        RectTransform rtTxtNodo = txtNodoObj.AddComponent<RectTransform>(); // setta // riga-ok
        rtTxtNodo.anchorMin = new Vector2(0.5f, 1); // setta // riga-ok
        rtTxtNodo.anchorMax = new Vector2(0.5f, 1); // setta // riga-ok
        rtTxtNodo.sizeDelta = new Vector2(500, 40); // setta // riga-ok
        rtTxtNodo.anchoredPosition = new Vector2(0, -10); // setta // riga-ok
        testoNodoProgresso = txtNodoObj.AddComponent<Text>(); // setta // riga-ok
        testoNodoProgresso.font = testoTitoloTerminale.font; // setta // riga-ok
        testoNodoProgresso.fontSize = 20; // setta // riga-ok
        testoNodoProgresso.fontStyle = FontStyle.Bold; // setta // riga-ok
        testoNodoProgresso.alignment = TextAnchor.MiddleCenter; // setta // riga-ok
        testoNodoProgresso.color = new Color(0.3f, 0.9f, 1f); // setta // riga-ok

        // Barra di Calibrazione
        GameObject barraCalibrazioneBg = new GameObject("BarraCalibrazioneBg"); // setta // riga-ok
        barraCalibrazioneBg.transform.SetParent(tabBypassObj.transform, false); // chiama // riga-ok
        RectTransform rtBarraBg = barraCalibrazioneBg.AddComponent<RectTransform>(); // setta // riga-ok
        rtBarraBg.anchorMin = new Vector2(0.5f, 0.5f); // setta // riga-ok
        rtBarraBg.anchorMax = new Vector2(0.5f, 0.5f); // setta // riga-ok
        rtBarraBg.sizeDelta = new Vector2(ampiezzaBarra, 36); // setta // riga-ok
        rtBarraBg.anchoredPosition = new Vector2(0, 30); // setta // riga-ok
        Image imgBarraBg = barraCalibrazioneBg.AddComponent<Image>(); // setta // riga-ok
        imgBarraBg.color = new Color(0.12f, 0.15f, 0.2f, 1f); // setta // riga-ok

        // Zona Verde Target
        GameObject greenZoneObj = new GameObject("ZonaVerde"); // setta // riga-ok
        greenZoneObj.transform.SetParent(barraCalibrazioneBg.transform, false); // chiama // riga-ok
        zonaVerdeTarget = greenZoneObj.AddComponent<RectTransform>(); // setta // riga-ok
        zonaVerdeTarget.sizeDelta = new Vector2(80, 36); // setta // riga-ok
        zonaVerdeTarget.anchoredPosition = Vector2.zero; // setta // riga-ok
        Image imgGreen = greenZoneObj.AddComponent<Image>(); // setta // riga-ok
        imgGreen.color = new Color(0.2f, 0.85f, 0.4f, 0.8f); // setta // riga-ok

        // Cursore Oscillante
        GameObject cursoreObj = new GameObject("CursoreOscillante"); // setta // riga-ok
        cursoreObj.transform.SetParent(barraCalibrazioneBg.transform, false); // chiama // riga-ok
        barraOscillatore = cursoreObj.AddComponent<RectTransform>(); // setta // riga-ok
        barraOscillatore.sizeDelta = new Vector2(8, 48); // setta // riga-ok
        barraOscillatore.anchoredPosition = Vector2.zero; // setta // riga-ok
        Image imgCursore = cursoreObj.AddComponent<Image>(); // setta // riga-ok
        imgCursore.color = Color.white; // setta // riga-ok

        // Istruzioni
        GameObject txtIstrObj = new GameObject("TxtIstruzioni"); // setta // riga-ok
        txtIstrObj.transform.SetParent(tabBypassObj.transform, false); // chiama // riga-ok
        RectTransform rtIstr = txtIstrObj.AddComponent<RectTransform>(); // setta // riga-ok
        rtIstr.anchorMin = new Vector2(0.5f, 0.5f); // setta // riga-ok
        rtIstr.anchorMax = new Vector2(0.5f, 0.5f); // setta // riga-ok
        rtIstr.sizeDelta = new Vector2(500, 60); // setta // riga-ok
        rtIstr.anchoredPosition = new Vector2(0, -35); // setta // riga-ok
        testoIstruzioniBypass = txtIstrObj.AddComponent<Text>(); // setta // riga-ok
        testoIstruzioniBypass.font = testoTitoloTerminale.font; // setta // riga-ok
        testoIstruzioniBypass.fontSize = 17; // setta // riga-ok
        testoIstruzioniBypass.alignment = TextAnchor.MiddleCenter; // setta // riga-ok
        testoIstruzioniBypass.color = new Color(0.8f, 0.85f, 0.9f); // setta // riga-ok

        // Bottone Calibra Nodo
        Button btnBypassClick = CreaBottone(tabBypassObj.transform, "⚡ CALIBRA CIRCUITO [SPAZIO]", TentaBypassNodo, new Color(0.15f, 0.55f, 0.85f)); // setta // riga-ok
        RectTransform rtBtnBypass = btnBypassClick.GetComponent<RectTransform>(); // setta // riga-ok
        rtBtnBypass.anchorMin = new Vector2(0.5f, 0); // setta // riga-ok
        rtBtnBypass.anchorMax = new Vector2(0.5f, 0); // setta // riga-ok
        rtBtnBypass.sizeDelta = new Vector2(360, 60); // setta // riga-ok
        rtBtnBypass.anchoredPosition = new Vector2(0, 35); // setta // riga-ok

        // blocco: controlla se va
        if (bgOverlay != null) bgOverlay.SetActive(false); // se ok // riga-ok
        // blocco: controlla se va
        if (pannelloPrincipale != null) pannelloPrincipale.SetActive(false); // se ok // riga-ok
        // blocco: controlla se va
        if (canvasRoot != null) canvasRoot.gameObject.SetActive(false); // se ok // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private Button CreaBottone(Transform parent, string testo, UnityEngine.Events.UnityAction onClick, Color? coloreSfondo = null) // roba pub // riga-ok
    { // apre // riga-ok
        GameObject btnObj = new GameObject("Btn_" + testo); // setta // riga-ok
        btnObj.transform.SetParent(parent, false); // chiama // riga-ok

        Image img = btnObj.AddComponent<Image>(); // setta // riga-ok
        img.color = coloreSfondo ?? new Color(0.12f, 0.22f, 0.32f, 1f); // setta // riga-ok

        Button btn = btnObj.AddComponent<Button>(); // setta // riga-ok
        btn.onClick.AddListener(onClick); // chiama // riga-ok

        ColorBlock cb = btn.colors; // setta // riga-ok
        cb.highlightedColor = (coloreSfondo ?? new Color(0.12f, 0.22f, 0.32f, 1f)) * 1.25f; // setta // riga-ok
        cb.pressedColor = (coloreSfondo ?? new Color(0.12f, 0.22f, 0.32f, 1f)) * 0.8f; // setta // riga-ok
        btn.colors = cb; // setta // riga-ok

        GameObject txtObj = new GameObject("Text"); // setta // riga-ok
        txtObj.transform.SetParent(btnObj.transform, false); // chiama // riga-ok
        RectTransform rtTxt = txtObj.AddComponent<RectTransform>(); // setta // riga-ok
        rtTxt.anchorMin = Vector2.zero; // setta // riga-ok
        rtTxt.anchorMax = Vector2.one; // setta // riga-ok

        Text txt = txtObj.AddComponent<Text>(); // setta // riga-ok
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf"); // setta // riga-ok
        txt.text = testo; // setta // riga-ok
        txt.fontSize = 18; // setta // riga-ok
        txt.fontStyle = FontStyle.Bold; // setta // riga-ok
        txt.alignment = TextAnchor.MiddleCenter; // setta // riga-ok
        txt.color = Color.white; // setta // riga-ok
        txt.horizontalOverflow = HorizontalWrapMode.Overflow; // setta // riga-ok
        txt.verticalOverflow = VerticalWrapMode.Overflow; // setta // riga-ok

        return btn; // torna val // riga-ok
    } // chiude // riga-ok

    #endregion // prep ok // riga-ok
} // chiude // riga-ok
