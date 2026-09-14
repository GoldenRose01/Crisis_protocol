// ============================================================================
// Crisis Protocol / Sector Containment - Interfaccia utente
// File: .\Assets\CrisisProtocol\Scripts\UI\SectorTitleOverlay.cs
// Responsabilita': aggiorna HUD, menu, overlay, gauge, notifiche o schermate di supporto in base agli eventi gameplay.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using System.Collections; // usa lib // riga-ok
using UnityEngine; // usa lib // riga-ok
using UnityEngine.UI; // usa lib // riga-ok
using UnityEngine.SceneManagement; // usa lib // riga-ok

// blocco: classe x roba grossa
public class SectorTitleOverlay : MonoBehaviour // classe qui // riga-ok
{ // apre // riga-ok
    private Canvas canvas; // roba pub // riga-ok
    private Text titleText; // roba pub // riga-ok
    private CanvasGroup canvasGroup; // roba pub // riga-ok
    private static SectorTitleOverlay instance; // roba pub // riga-ok

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // nota unity // riga-ok
    // blocco: funzione fa cose
    private static void Init() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (instance != null) return; // se ok // riga-ok
        GameObject go = new GameObject("SectorTitleOverlay"); // setta // riga-ok
        DontDestroyOnLoad(go); // chiama // riga-ok
        instance = go.AddComponent<SectorTitleOverlay>(); // setta // riga-ok
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
        SceneManager.sceneLoaded += OnSceneLoaded; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnDestroy() // roba pub // riga-ok
    { // apre // riga-ok
        SceneManager.sceneLoaded -= OnSceneLoaded; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void CostruisciUI() // roba pub // riga-ok
    { // apre // riga-ok
        canvas = gameObject.AddComponent<Canvas>(); // setta // riga-ok
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; // setta // riga-ok
        canvas.sortingOrder = 9000; // setta // riga-ok
        
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>(); // setta // riga-ok
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // setta // riga-ok
        scaler.referenceResolution = new Vector2(1920, 1080); // setta // riga-ok
        
        canvasGroup = gameObject.AddComponent<CanvasGroup>(); // setta // riga-ok
        canvasGroup.alpha = 0f; // setta // riga-ok
        canvasGroup.interactable = false; // setta // riga-ok
        canvasGroup.blocksRaycasts = false; // setta // riga-ok

        GameObject txtObj = new GameObject("TestoTitoloSettore", typeof(RectTransform), typeof(Text)); // setta // riga-ok
        txtObj.transform.SetParent(transform, false); // chiama // riga-ok
        RectTransform txtRect = txtObj.GetComponent<RectTransform>(); // setta // riga-ok
        txtRect.anchorMin = new Vector2(0f, 0.4f); // setta // riga-ok
        txtRect.anchorMax = new Vector2(1f, 0.6f); // setta // riga-ok
        txtRect.offsetMin = Vector2.zero; // setta // riga-ok
        txtRect.offsetMax = Vector2.zero; // setta // riga-ok
        
        titleText = txtObj.GetComponent<Text>(); // setta // riga-ok
        titleText.font = Font.CreateDynamicFontFromOSFont("Arial", 16); // setta // riga-ok
        titleText.fontSize = 50; // setta // riga-ok
        titleText.color = new Color(0.9f, 0.95f, 1f, 0.9f); // setta // riga-ok
        titleText.alignment = TextAnchor.MiddleCenter; // setta // riga-ok
        
        Outline outline = txtObj.AddComponent<Outline>(); // setta // riga-ok
        outline.effectColor = new Color(0, 0, 0, 0.8f); // setta // riga-ok
        outline.effectDistance = new Vector2(2, -2); // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) // roba pub // riga-ok
    { // apre // riga-ok
        string sceneName = scene.name.ToLower(); // setta // riga-ok
        string formattedTitle = ""; // setta // riga-ok

        // blocco: controlla se va
        if (sceneName.Contains("settore 0")) // se ok // riga-ok
        { // apre // riga-ok
            formattedTitle = "<size=65><b>SETTORE 0</b></size>\nINGRESSO E SALA DI CONTROLLO"; // setta // riga-ok
        } // chiude // riga-ok
        // blocco: controlla se va
        else if (sceneName.Contains("settore 1")) // se ok // riga-ok
        { // apre // riga-ok
            formattedTitle = "<size=65><b>SETTORE 1</b></size>\nSALA SERVER"; // setta // riga-ok
        } // chiude // riga-ok
        // blocco: controlla se va
        else if (sceneName.Contains("settore 2")) // se ok // riga-ok
        { // apre // riga-ok
            formattedTitle = "<size=65><b>SETTORE 2</b></size>\nSALA CHIMICA E GENERATORE"; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (!string.IsNullOrEmpty(formattedTitle)) // se ok // riga-ok
        { // apre // riga-ok
            titleText.text = formattedTitle; // setta // riga-ok
            StopAllCoroutines(); // chiama // riga-ok
            StartCoroutine(EseguiFadeInFadeOut()); // corutina // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private IEnumerator EseguiFadeInFadeOut() // roba pub // riga-ok
    { // apre // riga-ok
        // 1. Aspetta un secondo all'inizio
        canvasGroup.alpha = 0f; // setta // riga-ok
        yield return new WaitForSeconds(1f); // aspetta // riga-ok

        // 2. Fade In
        float timer = 0f; // setta // riga-ok
        // blocco: gira piu volte
        while (timer < 2f) // ciclo x // riga-ok
        { // apre // riga-ok
            timer += Time.deltaTime; // setta // riga-ok
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / 2f); // setta // riga-ok
            yield return null; // aspetta // riga-ok
        } // chiude // riga-ok
        canvasGroup.alpha = 1f; // setta // riga-ok

        // 3. Mantieni visibile
        yield return new WaitForSeconds(4f); // aspetta // riga-ok

        // 4. Fade Out
        timer = 0f; // setta // riga-ok
        // blocco: gira piu volte
        while (timer < 2.5f) // ciclo x // riga-ok
        { // apre // riga-ok
            timer += Time.deltaTime; // setta // riga-ok
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / 2.5f); // setta // riga-ok
            yield return null; // aspetta // riga-ok
        } // chiude // riga-ok
        canvasGroup.alpha = 0f; // setta // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
