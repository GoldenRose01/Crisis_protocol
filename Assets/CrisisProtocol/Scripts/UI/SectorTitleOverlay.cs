// ============================================================================
// Crisis Protocol / Sector Containment - Interfaccia utente
// File: .\Assets\CrisisProtocol\Scripts\UI\SectorTitleOverlay.cs
// Responsabilita': aggiorna HUD, menu, overlay, gauge, notifiche o schermate di supporto in base agli eventi gameplay.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SectorTitleOverlay : MonoBehaviour
{
    private Canvas canvas;
    private Text titleText;
    private CanvasGroup canvasGroup;
    private static SectorTitleOverlay instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (instance != null) return;
        GameObject go = new GameObject("SectorTitleOverlay");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SectorTitleOverlay>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        CostruisciUI();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void CostruisciUI()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9000;
        
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        GameObject txtObj = new GameObject("TestoTitoloSettore", typeof(RectTransform), typeof(Text));
        txtObj.transform.SetParent(transform, false);
        RectTransform txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = new Vector2(0f, 0.4f);
        txtRect.anchorMax = new Vector2(1f, 0.6f);
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;
        
        titleText = txtObj.GetComponent<Text>();
        titleText.font = Font.CreateDynamicFontFromOSFont("Arial", 16);
        titleText.fontSize = 50;
        titleText.color = new Color(0.9f, 0.95f, 1f, 0.9f);
        titleText.alignment = TextAnchor.MiddleCenter;
        
        Outline outline = txtObj.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.8f);
        outline.effectDistance = new Vector2(2, -2);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name.ToLower();
        string formattedTitle = "";

        if (sceneName.Contains("settore 0"))
        {
            formattedTitle = "<size=65><b>SETTORE 0</b></size>\nINGRESSO E SALA DI CONTROLLO";
        }
        else if (sceneName.Contains("settore 1"))
        {
            formattedTitle = "<size=65><b>SETTORE 1</b></size>\nSALA SERVER";
        }
        else if (sceneName.Contains("settore 2"))
        {
            formattedTitle = "<size=65><b>SETTORE 2</b></size>\nSALA CHIMICA E GENERATORE";
        }

        if (!string.IsNullOrEmpty(formattedTitle))
        {
            titleText.text = formattedTitle;
            StopAllCoroutines();
            StartCoroutine(EseguiFadeInFadeOut());
        }
    }

    private IEnumerator EseguiFadeInFadeOut()
    {
        // 1. Aspetta un secondo all'inizio
        canvasGroup.alpha = 0f;
        yield return new WaitForSeconds(1f);

        // 2. Fade In
        float timer = 0f;
        while (timer < 2f)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / 2f);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // 3. Mantieni visibile
        yield return new WaitForSeconds(4f);

        // 4. Fade Out
        timer = 0f;
        while (timer < 2.5f)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / 2.5f);
            yield return null;
        }
        canvasGroup.alpha = 0f;
    }
}
