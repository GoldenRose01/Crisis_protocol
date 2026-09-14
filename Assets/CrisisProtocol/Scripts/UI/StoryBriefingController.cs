using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using GoldenCast.UI;

public class StoryBriefingController : MonoBehaviour
{
    private static StoryBriefingController instance;

    private Canvas canvas;
    private Text testoTerminale;
    private Text textSkip;
    private AudioSource audioSource;
    private bool isTyping = false;
    private bool skipRequested = false;
    private bool canSkip = false;

    private string testoCompleto = "Siamo robot di soccorso emergenze di una corporazione ultra-tecnologica e completamente automatizzata.\n\n" +
                                   "A causa di una reazione a catena, si sono verificati malfunzionamenti e crisi critiche in tutta la struttura. " +
                                   "I robot di sicurezza non ci riconoscono più, poiché non condividiamo gli stessi protocolli.\n\n" +
                                   "Il nostro obiettivo è raccogliere le chiavi di accesso ai settori, individuare i focolai delle emergenze e ripristinare la sicurezza dell'impianto.";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (instance != null) return;
        GameObject go = new GameObject("StoryBriefingController");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<StoryBriefingController>();
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
    }

    private void CostruisciUI()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        gameObject.AddComponent<GraphicRaycaster>();

        GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(transform, false);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.02f, 0.05f, 0.02f, 1f); // Verde scurissimo quasi nero

        GameObject txtObj = new GameObject("TestoTerminale", typeof(RectTransform), typeof(Text));
        txtObj.transform.SetParent(transform, false);
        RectTransform txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = new Vector2(0.1f, 0.1f);
        txtRect.anchorMax = new Vector2(0.9f, 0.9f);
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;
        
        testoTerminale = txtObj.GetComponent<Text>();
        testoTerminale.font = Font.CreateDynamicFontFromOSFont("Consolas", 16);
        testoTerminale.fontSize = 32;
        testoTerminale.color = new Color(0f, 1f, 0.45f, 1f); // Verde neon
        testoTerminale.alignment = TextAnchor.MiddleLeft;
        testoTerminale.text = "";

        GameObject skipObj = new GameObject("TextSkip", typeof(RectTransform), typeof(Text));
        skipObj.transform.SetParent(transform, false);
        RectTransform skipRect = skipObj.GetComponent<RectTransform>();
        skipRect.anchorMin = new Vector2(0.8f, 0.05f);
        skipRect.anchorMax = new Vector2(0.95f, 0.1f);
        skipRect.offsetMin = Vector2.zero;
        skipRect.offsetMax = Vector2.zero;

        textSkip = skipObj.GetComponent<Text>();
        textSkip.font = Font.CreateDynamicFontFromOSFont("Consolas", 16);
        textSkip.fontSize = 20;
        textSkip.color = new Color(0.5f, 1f, 0.5f, 0.5f);
        textSkip.alignment = TextAnchor.MiddleRight;
        textSkip.text = "[PREMI QUALSIASI TASTO PER SALTARE]";

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        
        canvas.gameObject.SetActive(false);
    }

    public static void ShowBriefingAndLoadGame()
    {
        if (instance != null)
        {
            instance.gameObject.SetActive(true);
            instance.StartCoroutine(instance.EseguiBriefingRoutine());
        }
    }

    private void Update()
    {
        if (canvas.gameObject.activeSelf && isTyping && canSkip)
        {
            if (Input.anyKeyDown)
            {
                skipRequested = true;
            }
        }
    }

    private IEnumerator EseguiBriefingRoutine()
    {
        ModalUIState.TryOpen("StoryBriefing", true, true);
        canvas.gameObject.SetActive(true);
        testoTerminale.text = "";
        skipRequested = false;
        isTyping = true;
        canSkip = false;

        yield return new WaitForSecondsRealtime(1f);
        canSkip = true;

        for (int i = 0; i < testoCompleto.Length; i++)
        {
            if (skipRequested)
            {
                testoTerminale.text = testoCompleto;
                break;
            }

            testoTerminale.text += testoCompleto[i];
            
            // Simula suono terminale
            if (testoCompleto[i] != ' ' && testoCompleto[i] != '\n')
            {
                if (audioSource != null && !audioSource.isPlaying)
                {
                    // Usa un click base o beep
                }
            }

            yield return new WaitForSecondsRealtime(0.04f); // Velocità macchina da scrivere
        }

        isTyping = false;
        
        if (!skipRequested)
        {
            // Attendi qualche secondo per far leggere l'intero testo
            float timer = 0;
            while(timer < 4f && !skipRequested)
            {
                timer += Time.unscaledDeltaTime;
                if (Input.anyKeyDown) skipRequested = true;
                yield return null;
            }
        }

        ModalUIState.Close("StoryBriefing");

        // Carica il settore 0
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CaricaSettore(0);
        }
        
        canvas.gameObject.SetActive(false);
    }
}
