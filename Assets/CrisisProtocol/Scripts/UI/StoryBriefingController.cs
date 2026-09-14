// ============================================================================
// Crisis Protocol / Sector Containment - Interfaccia utente
// File: .\Assets\CrisisProtocol\Scripts\UI\StoryBriefingController.cs
// Responsabilita': aggiorna HUD, menu, overlay, gauge, notifiche o schermate di supporto in base agli eventi gameplay.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using System.Collections; // usa lib // riga-ok
using UnityEngine; // usa lib // riga-ok
using UnityEngine.UI; // usa lib // riga-ok
using UnityEngine.SceneManagement; // usa lib // riga-ok
using CrisisProtocol.UI; // usa lib // riga-ok

// blocco: classe x roba grossa
public class StoryBriefingController : MonoBehaviour // classe qui // riga-ok
{ // apre // riga-ok
    private static StoryBriefingController instance; // roba pub // riga-ok

    private Canvas canvas; // roba pub // riga-ok
    private Text testoTerminale; // roba pub // riga-ok
    private Text textSkip; // roba pub // riga-ok
    private AudioSource audioSource; // roba pub // riga-ok
    private bool isTyping = false; // roba pub // riga-ok
    private bool skipRequested = false; // roba pub // riga-ok
    private bool canSkip = false; // roba pub // riga-ok

    private string testoCompleto = "Siamo robot di soccorso emergenze di una corporazione ultra-tecnologica e completamente automatizzata.\n\n" + // roba pub // riga-ok
                                   "A causa di una reazione a catena, si sono verificati malfunzionamenti e crisi critiche in tutta la struttura. " + // ok qua // riga-ok
                                   "I robot di sicurezza non ci riconoscono più, poiché non condividiamo gli stessi protocolli.\n\n" + // ok qua // riga-ok
                                   "Il nostro obiettivo è raccogliere le chiavi di accesso ai settori, individuare i focolai delle emergenze e ripristinare la sicurezza dell'impianto."; // ok qua // riga-ok

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // nota unity // riga-ok
    // blocco: funzione fa cose
    private static void Init() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (instance != null) return; // se ok // riga-ok
        GameObject go = new GameObject("StoryBriefingController"); // setta // riga-ok
        DontDestroyOnLoad(go); // chiama // riga-ok
        instance = go.AddComponent<StoryBriefingController>(); // setta // riga-ok
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
        CostruisciUI(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void CostruisciUI() // roba pub // riga-ok
    { // apre // riga-ok
        canvas = gameObject.AddComponent<Canvas>(); // setta // riga-ok
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; // setta // riga-ok
        canvas.sortingOrder = 9999; // setta // riga-ok
        
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>(); // setta // riga-ok
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // setta // riga-ok
        scaler.referenceResolution = new Vector2(1920, 1080); // setta // riga-ok
        
        gameObject.AddComponent<GraphicRaycaster>(); // chiama // riga-ok

        GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image)); // setta // riga-ok
        bg.transform.SetParent(transform, false); // chiama // riga-ok
        RectTransform bgRect = bg.GetComponent<RectTransform>(); // setta // riga-ok
        bgRect.anchorMin = Vector2.zero; // setta // riga-ok
        bgRect.anchorMax = Vector2.one; // setta // riga-ok
        bgRect.offsetMin = Vector2.zero; // setta // riga-ok
        bgRect.offsetMax = Vector2.zero; // setta // riga-ok
        bg.GetComponent<Image>().color = new Color(0.02f, 0.05f, 0.02f, 1f); // Verde scurissimo quasi nero // setta // riga-ok

        GameObject txtObj = new GameObject("TestoTerminale", typeof(RectTransform), typeof(Text)); // setta // riga-ok
        txtObj.transform.SetParent(transform, false); // chiama // riga-ok
        RectTransform txtRect = txtObj.GetComponent<RectTransform>(); // setta // riga-ok
        txtRect.anchorMin = new Vector2(0.1f, 0.1f); // setta // riga-ok
        txtRect.anchorMax = new Vector2(0.9f, 0.9f); // setta // riga-ok
        txtRect.offsetMin = Vector2.zero; // setta // riga-ok
        txtRect.offsetMax = Vector2.zero; // setta // riga-ok
        
        testoTerminale = txtObj.GetComponent<Text>(); // setta // riga-ok
        testoTerminale.font = Font.CreateDynamicFontFromOSFont("Consolas", 16); // setta // riga-ok
        testoTerminale.fontSize = 32; // setta // riga-ok
        testoTerminale.color = new Color(0f, 1f, 0.45f, 1f); // Verde neon // setta // riga-ok
        testoTerminale.alignment = TextAnchor.MiddleLeft; // setta // riga-ok
        testoTerminale.text = ""; // setta // riga-ok

        GameObject skipObj = new GameObject("TextSkip", typeof(RectTransform), typeof(Text)); // setta // riga-ok
        skipObj.transform.SetParent(transform, false); // chiama // riga-ok
        RectTransform skipRect = skipObj.GetComponent<RectTransform>(); // setta // riga-ok
        skipRect.anchorMin = new Vector2(0.8f, 0.05f); // setta // riga-ok
        skipRect.anchorMax = new Vector2(0.95f, 0.1f); // setta // riga-ok
        skipRect.offsetMin = Vector2.zero; // setta // riga-ok
        skipRect.offsetMax = Vector2.zero; // setta // riga-ok

        textSkip = skipObj.GetComponent<Text>(); // setta // riga-ok
        textSkip.font = Font.CreateDynamicFontFromOSFont("Consolas", 16); // setta // riga-ok
        textSkip.fontSize = 20; // setta // riga-ok
        textSkip.color = new Color(0.5f, 1f, 0.5f, 0.5f); // setta // riga-ok
        textSkip.alignment = TextAnchor.MiddleRight; // setta // riga-ok
        textSkip.text = "[PREMI QUALSIASI TASTO PER SALTARE]"; // setta // riga-ok

        audioSource = gameObject.AddComponent<AudioSource>(); // setta // riga-ok
        audioSource.playOnAwake = false; // setta // riga-ok
        
        canvas.gameObject.SetActive(false); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public static void ShowBriefingAndLoadGame() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (instance != null) // se ok // riga-ok
        { // apre // riga-ok
            instance.gameObject.SetActive(true); // chiama // riga-ok
            instance.StartCoroutine(instance.EseguiBriefingRoutine()); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void Update() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (canvas.gameObject.activeSelf && isTyping && canSkip) // se ok // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (Input.anyKeyDown) // se ok // riga-ok
            { // apre // riga-ok
                skipRequested = true; // setta // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private IEnumerator EseguiBriefingRoutine() // roba pub // riga-ok
    { // apre // riga-ok
        ModalUIState.TryOpen("StoryBriefing", true, true); // chiama // riga-ok
        canvas.gameObject.SetActive(true); // chiama // riga-ok
        testoTerminale.text = ""; // setta // riga-ok
        skipRequested = false; // setta // riga-ok
        isTyping = true; // setta // riga-ok
        canSkip = false; // setta // riga-ok

        yield return new WaitForSecondsRealtime(1f); // aspetta // riga-ok
        canSkip = true; // setta // riga-ok

        // blocco: gira piu volte
        for (int i = 0; i < testoCompleto.Length; i++) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (skipRequested) // se ok // riga-ok
            { // apre // riga-ok
                testoTerminale.text = testoCompleto; // setta // riga-ok
                break; // stop // riga-ok
            } // chiude // riga-ok

            testoTerminale.text += testoCompleto[i]; // setta // riga-ok
            
            // Simula suono terminale
            // blocco: controlla se va
            if (testoCompleto[i] != ' ' && testoCompleto[i] != '\n') // se ok // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (audioSource != null && !audioSource.isPlaying) // se ok // riga-ok
                { // apre // riga-ok
                    // Usa un click base o beep
                } // chiude // riga-ok
            } // chiude // riga-ok

            yield return new WaitForSecondsRealtime(0.04f); // Velocità macchina da scrivere // aspetta // riga-ok
        } // chiude // riga-ok

        isTyping = false; // setta // riga-ok
        
        // blocco: controlla se va
        if (!skipRequested) // se ok // riga-ok
        { // apre // riga-ok
            // Attendi qualche secondo per far leggere l'intero testo
            float timer = 0; // setta // riga-ok
            // blocco: gira piu volte
            while(timer < 4f && !skipRequested) // ciclo x // riga-ok
            { // apre // riga-ok
                timer += Time.unscaledDeltaTime; // setta // riga-ok
                // blocco: controlla se va
                if (Input.anyKeyDown) skipRequested = true; // se ok // riga-ok
                yield return null; // aspetta // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        ModalUIState.Close("StoryBriefing"); // chiama // riga-ok

        // Carica il settore 0
        // blocco: controlla se va
        if (GameManager.Instance != null) // se ok // riga-ok
        { // apre // riga-ok
            GameManager.Instance.CaricaSettore(0); // chiama // riga-ok
        } // chiude // riga-ok
        
        canvas.gameObject.SetActive(false); // chiama // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
