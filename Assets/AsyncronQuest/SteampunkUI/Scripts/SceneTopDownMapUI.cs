// ============================================================================
// Crisis Protocol / Sector Containment - UI e feedback AsyncronQuest
// File: .\Assets\AsyncronQuest\SteampunkUI\Scripts\SceneTopDownMapUI.cs
// Responsabilita': fornisce schermate, tooltip, transizioni, menu e feedback visivi integrati nel progetto Crisis Protocol.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using System; // usa lib // riga-ok
using System.Collections.Generic; // usa lib // riga-ok
using UnityEngine; // usa lib // riga-ok
using UnityEngine.UI; // usa lib // riga-ok
using UnityEngine.InputSystem; // usa lib // riga-ok
using TMPro; // usa lib // riga-ok

namespace AsyncronQuest.SteampunkUI // zona cod // riga-ok
{ // apre // riga-ok
    /// <summary>
    /// Gestore della Mappa Tattica Olografica dall'Alto (Top-Down Radar).
    /// Mostra:
    /// - Contorni geometrici dei muri in Verde Neon brillante su sfondo scuro.
    /// - Oggetti interagibili con alone/aura Giallo vivido e targhetta con nome in Font Terminale.
    /// - Nemici in Rosso vivo con indicatore direzionale.
    /// - Posizione e orientamento del Giocatore in Ciano.
    /// </summary>
    [DisallowMultipleComponent] // nota unity // riga-ok
    [RequireComponent(typeof(RawImage))] // nota unity // riga-ok
    // blocco: classe x roba grossa
    public sealed class SceneTopDownMapUI : MonoBehaviour // classe qui // riga-ok
    { // apre // riga-ok
        [Header("Target & Camera")] // nota unity // riga-ok
        [SerializeField] private Transform followTarget; // ok qua // riga-ok
        [SerializeField] private string playerTag = "Player"; // setta // riga-ok
        [SerializeField, Min(10f)] private float cameraHeight = 150f; // setta // riga-ok
        [SerializeField, Min(5f)] private float orthographicSize = 65f; // setta // riga-ok
        [SerializeField, Min(64)] private int textureSize = 1024; // setta // riga-ok
        [SerializeField] private LayerMask cullingMask = ~((1 << 1) | (1 << 2) | (1 << 5)); // setta // riga-ok
        [SerializeField] private Color cameraClearColor = new Color(0.005f, 0.015f, 0.010f, 1f); // setta // riga-ok
        [SerializeField] private bool centerOnLevel = false; // setta // riga-ok

        [Header("Stile Tattico Neon")] // nota unity // riga-ok
        [SerializeField] private Color neonWallColor = new Color(0.0f, 1.0f, 0.4f, 1.0f); // Verde Neon // setta // riga-ok
        [SerializeField] private Color interactableHaloColor = new Color(1.0f, 0.92f, 0.15f, 1.0f); // Giallo Vivido // setta // riga-ok
        [SerializeField] private Color enemyHaloColor = new Color(1.0f, 0.12f, 0.18f, 1.0f); // Rosso Vivido // setta // riga-ok
        [SerializeField] private Color playerBeaconColor = new Color(0.0f, 0.95f, 1.0f, 1.0f); // Ciano // setta // riga-ok

        private RawImage rawImage; // roba pub // riga-ok
        private Camera mapCamera; // roba pub // riga-ok
        private RenderTexture renderTexture; // roba pub // riga-ok
        private Material tacticalNeonMaterial; // roba pub // riga-ok

        // Contenitore UI Overlay sopra la mappa
        private RectTransform overlayContainer; // roba pub // riga-ok
        private RectTransform hudHeader; // roba pub // riga-ok
        private TextMeshProUGUI txtLegend; // roba pub // riga-ok

        // Tracciamento entità
        private Transform playerTransform; // roba pub // riga-ok
        private GameObject playerMarkerObj; // roba pub // riga-ok
        private RectTransform playerMarkerRect; // roba pub // riga-ok
        private Image playerArrowImg; // roba pub // riga-ok

        // Pool di Marker
        private readonly List<InteractableMarkerItem> activeInteractableMarkers = new List<InteractableMarkerItem>(); // roba pub // riga-ok
        private readonly List<EnemyMarkerItem> activeEnemyMarkers = new List<EnemyMarkerItem>(); // roba pub // riga-ok

        private readonly List<Transform> cachedInteractables = new List<Transform>(); // roba pub // riga-ok
        private readonly List<string> cachedInteractableNames = new List<string>(); // roba pub // riga-ok
        private readonly List<Transform> cachedEnemies = new List<Transform>(); // roba pub // riga-ok

        private float nextScanTime = 0f; // roba pub // riga-ok
        private float currentZoom = 65f; // roba pub // riga-ok
        private Vector3 calculatedLevelCenter = Vector3.zero; // roba pub // riga-ok
        private Bounds calculatedLevelBounds; // roba pub // riga-ok
        private bool levelBoundsCalculated = false; // roba pub // riga-ok

        // blocco: funzione fa cose
        private void Awake() // roba pub // riga-ok
        { // apre // riga-ok
            rawImage = GetComponent<RawImage>(); // setta // riga-ok
            currentZoom = orthographicSize; // setta // riga-ok

            SetupTacticalMaterial(); // chiama // riga-ok
            CreateCamera(); // chiama // riga-ok
            CreateOverlayUI(); // chiama // riga-ok
            ResolveFollowTarget(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void OnEnable() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (!rawImage) // se ok // riga-ok
                rawImage = GetComponent<RawImage>(); // setta // riga-ok

            SetupTacticalMaterial(); // chiama // riga-ok
            CreateCamera(); // chiama // riga-ok

            // blocco: controlla se va
            if (mapCamera) // se ok // riga-ok
                mapCamera.enabled = true; // setta // riga-ok

            // blocco: controlla se va
            if (rawImage != null && renderTexture != null) // se ok // riga-ok
                rawImage.texture = renderTexture; // setta // riga-ok

            RecalculateLevelBoundsAndFraming(); // chiama // riga-ok
            ScanSceneEntities(); // chiama // riga-ok
            UpdateOverlayPositions(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void OnDisable() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (mapCamera) // se ok // riga-ok
                mapCamera.enabled = false; // setta // riga-ok

            ClearAllMarkers(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void OnDestroy() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (mapCamera) // se ok // riga-ok
                Destroy(mapCamera.gameObject); // elimina // riga-ok

            // blocco: controlla se va
            if (renderTexture) // se ok // riga-ok
                renderTexture.Release(); // chiama // riga-ok

            // blocco: controlla se va
            if (tacticalNeonMaterial) // se ok // riga-ok
                Destroy(tacticalNeonMaterial); // elimina // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void Update() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (!isActiveAndEnabled) return; // se ok // riga-ok

            // Zoom con la rotellina del mouse sulla mappa
            HandleZoomInput(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void LateUpdate() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (!followTarget) // se ok // riga-ok
                ResolveFollowTarget(); // chiama // riga-ok

            // blocco: controlla se va
            if (!mapCamera) // se ok // riga-ok
                return; // torna val // riga-ok

            Vector3 center = (centerOnLevel && levelBoundsCalculated) ? calculatedLevelCenter : (followTarget ? followTarget.position : Vector3.zero); // setta // riga-ok
            mapCamera.transform.position = new Vector3(center.x, center.y + cameraHeight, center.z); // setta // riga-ok
            mapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // setta // riga-ok
            mapCamera.orthographicSize = currentZoom; // setta // riga-ok
            mapCamera.cullingMask = cullingMask; // setta // riga-ok

            // Scansione periodica entità ogni 1.2s
            // blocco: controlla se va
            if (Time.unscaledTime >= nextScanTime) // se ok // riga-ok
            { // apre // riga-ok
                ScanSceneEntities(); // chiama // riga-ok
                nextScanTime = Time.unscaledTime + 1.2f; // setta // riga-ok
            } // chiude // riga-ok

            UpdateOverlayPositions(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void HandleZoomInput() // roba pub // riga-ok
        { // apre // riga-ok
            Mouse mouse = Mouse.current; // setta // riga-ok
            // blocco: controlla se va
            if (mouse != null) // se ok // riga-ok
            { // apre // riga-ok
                float scroll = mouse.scroll.ReadValue().y; // setta // riga-ok
                // blocco: controlla se va
                if (Mathf.Abs(scroll) > 0.01f) // se ok // riga-ok
                { // apre // riga-ok
                    currentZoom = Mathf.Clamp(currentZoom - Mathf.Sign(scroll) * 4f, 20f, 130f); // setta // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public void Configure(Transform target, float height, float size, int renderTextureSize) // roba pub // riga-ok
        { // apre // riga-ok
            followTarget = target; // setta // riga-ok
            cameraHeight = Mathf.Max(10f, height); // setta // riga-ok
            orthographicSize = Mathf.Max(5f, size); // setta // riga-ok
            currentZoom = orthographicSize; // setta // riga-ok
            textureSize = Mathf.Max(64, renderTextureSize); // setta // riga-ok

            // blocco: controlla se va
            if (!rawImage) // se ok // riga-ok
                rawImage = GetComponent<RawImage>(); // setta // riga-ok

            SetupTacticalMaterial(); // chiama // riga-ok
            CreateCamera(); // chiama // riga-ok
            CreateOverlayUI(); // chiama // riga-ok
            RecalculateLevelBoundsAndFraming(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void SetupTacticalMaterial() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (tacticalNeonMaterial != null) return; // se ok // riga-ok

            Shader shader = Shader.Find("UI/TacticalNeonMap"); // setta // riga-ok
            // blocco: controlla se va
            if (shader != null) // se ok // riga-ok
            { // apre // riga-ok
                tacticalNeonMaterial = new Material(shader); // setta // riga-ok
                tacticalNeonMaterial.name = "Mat_TacticalNeonMap"; // setta // riga-ok
                tacticalNeonMaterial.SetColor("_EdgeColor", neonWallColor); // chiama // riga-ok
                tacticalNeonMaterial.SetColor("_BackgroundColor", cameraClearColor); // chiama // riga-ok
                tacticalNeonMaterial.SetFloat("_EdgeThreshold", 0.08f); // chiama // riga-ok
                tacticalNeonMaterial.SetFloat("_EdgeGlow", 2.6f); // chiama // riga-ok
                tacticalNeonMaterial.SetFloat("_BlueprintStrength", 0.35f); // chiama // riga-ok

                // blocco: controlla se va
                if (rawImage != null) // se ok // riga-ok
                    rawImage.material = tacticalNeonMaterial; // setta // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void RecalculateLevelBoundsAndFraming() // roba pub // riga-ok
        { // apre // riga-ok
            calculatedLevelBounds = CalculateLevelBounds(); // setta // riga-ok
            calculatedLevelCenter = calculatedLevelBounds.center; // setta // riga-ok
            levelBoundsCalculated = true; // setta // riga-ok

            float maxExtent = Mathf.Max(calculatedLevelBounds.size.x, calculatedLevelBounds.size.z); // setta // riga-ok
            // blocco: controlla se va
            if (maxExtent > 8f) // se ok // riga-ok
            { // apre // riga-ok
                // Inquadra il livello con margine confortevole all'interno della cornice del monitor
                float optimalZoom = Mathf.Clamp(maxExtent * 0.55f, 35f, 85f); // setta // riga-ok
                orthographicSize = optimalZoom; // setta // riga-ok
                currentZoom = optimalZoom; // setta // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private Bounds CalculateLevelBounds() // roba pub // riga-ok
        { // apre // riga-ok
            Bounds b = new Bounds(Vector3.zero, Vector3.zero); // setta // riga-ok
            bool initialized = false; // setta // riga-ok
            Vector3 playerPos = followTarget ? followTarget.position : Vector3.zero; // setta // riga-ok

            Collider[] colliders = FindObjectsByType<Collider>(FindObjectsSortMode.None); // setta // riga-ok
            // blocco: gira piu volte
            foreach (Collider col in colliders) // ciclo x // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (col == null || col.isTrigger) continue; // se ok // riga-ok
                GameObject go = col.gameObject; // setta // riga-ok
                // blocco: controlla se va
                if (!go.activeInHierarchy) continue; // se ok // riga-ok

                // blocco: controlla se va
                if (go.CompareTag("Player") || go.CompareTag("Enemy") || go.GetComponent<muve_pg>() != null || go.GetComponent<Camera>() != null) // se ok // riga-ok
                    continue; // salta // riga-ok

                // Escludi oggetti troppo distanti dal player (es. collider di sfondo o skybox)
                // blocco: controlla se va
                if (followTarget != null && Vector3.Distance(col.bounds.center, playerPos) > 300f) // se ok // riga-ok
                    continue; // salta // riga-ok

                string n = go.name.ToLower(); // setta // riga-ok
                // blocco: controlla se va
                if (n.Contains("pavimento") || n.Contains("floor") || n.Contains("terrain") || n.Contains("ground") || n.Contains("plane")) // se ok // riga-ok
                    continue; // salta // riga-ok

                Vector3 size = col.bounds.size; // setta // riga-ok
                // blocco: controlla se va
                if (size.y >= 0.6f || n.Contains("muro") || n.Contains("wall") || n.Contains("porte") || n.Contains("door") || n.Contains("porta") || n.Contains("cube") || n.Contains("panel") || n.Contains("box") || n.Contains("pipe") || n.Contains("container")) // se ok // riga-ok
                { // apre // riga-ok
                    // blocco: controlla se va
                    if (!initialized) // se ok // riga-ok
                    { // apre // riga-ok
                        b = col.bounds; // setta // riga-ok
                        initialized = true; // setta // riga-ok
                    } // chiude // riga-ok
                    // blocco: caso diverso
                    else // se no // riga-ok
                    { // apre // riga-ok
                        b.Encapsulate(col.bounds); // chiama // riga-ok
                    } // chiude // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok

            // blocco: controlla se va
            if (!initialized) // se ok // riga-ok
            { // apre // riga-ok
                return new Bounds(playerPos, new Vector3(60f, 10f, 60f)); // torna val // riga-ok
            } // chiude // riga-ok

            return b; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void CreateCamera() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (renderTexture == null || renderTexture.width != textureSize || renderTexture.height != textureSize) // se ok // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (renderTexture != null) // se ok // riga-ok
                    renderTexture.Release(); // chiama // riga-ok

                renderTexture = new RenderTexture(textureSize, textureSize, 16, RenderTextureFormat.ARGB32) // setta // riga-ok
                { // apre // riga-ok
                    name = "Scene Top Down Map Texture", // setta // riga-ok
                    filterMode = FilterMode.Bilinear, // setta // riga-ok
                    wrapMode = TextureWrapMode.Clamp // setta // riga-ok
                }; // ok qua // riga-ok
                renderTexture.Create(); // chiama // riga-ok
            } // chiude // riga-ok

            // blocco: controlla se va
            if (!mapCamera) // se ok // riga-ok
            { // apre // riga-ok
                GameObject cameraObject = new GameObject("Scene Top Down Map Camera"); // setta // riga-ok
                cameraObject.hideFlags = HideFlags.DontSave; // setta // riga-ok
                mapCamera = cameraObject.AddComponent<Camera>(); // setta // riga-ok
            } // chiude // riga-ok

            mapCamera.clearFlags = CameraClearFlags.SolidColor; // setta // riga-ok
            mapCamera.backgroundColor = cameraClearColor; // setta // riga-ok
            mapCamera.orthographic = true; // setta // riga-ok
            mapCamera.nearClipPlane = 0.1f; // setta // riga-ok
            mapCamera.farClipPlane = cameraHeight + 400f; // setta // riga-ok
            mapCamera.depth = -100f; // setta // riga-ok
            mapCamera.targetTexture = renderTexture; // setta // riga-ok
            mapCamera.cullingMask = cullingMask; // setta // riga-ok
            mapCamera.enabled = isActiveAndEnabled; // setta // riga-ok

            // blocco: controlla se va
            if (rawImage != null) // se ok // riga-ok
                rawImage.texture = renderTexture; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void CreateOverlayUI() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (overlayContainer != null) return; // se ok // riga-ok

            GameObject overlayObj = new GameObject("TacticalOverlayContainer", typeof(RectTransform)); // setta // riga-ok
            overlayObj.transform.SetParent(transform, false); // chiama // riga-ok
            overlayContainer = overlayObj.GetComponent<RectTransform>(); // setta // riga-ok
            overlayContainer.anchorMin = Vector2.zero; // setta // riga-ok
            overlayContainer.anchorMax = Vector2.one; // setta // riga-ok
            overlayContainer.offsetMin = Vector2.zero; // setta // riga-ok
            overlayContainer.offsetMax = Vector2.zero; // setta // riga-ok

            // Marker Player
            CreatePlayerMarker(); // chiama // riga-ok

            // Legend / Header Tattico
            CreateTacticalLegend(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void CreatePlayerMarker() // roba pub // riga-ok
        { // apre // riga-ok
            playerMarkerObj = new GameObject("PlayerMarker", typeof(RectTransform)); // setta // riga-ok
            playerMarkerObj.transform.SetParent(overlayContainer, false); // chiama // riga-ok
            playerMarkerRect = playerMarkerObj.GetComponent<RectTransform>(); // setta // riga-ok
            playerMarkerRect.sizeDelta = new Vector2(44, 44); // setta // riga-ok

            // Alone Ciano
            GameObject haloObj = new GameObject("Halo", typeof(RectTransform), typeof(Image)); // setta // riga-ok
            haloObj.transform.SetParent(playerMarkerObj.transform, false); // chiama // riga-ok
            RectTransform rtHalo = haloObj.GetComponent<RectTransform>(); // setta // riga-ok
            rtHalo.anchorMin = Vector2.zero; // setta // riga-ok
            rtHalo.anchorMax = Vector2.one; // setta // riga-ok
            rtHalo.offsetMin = new Vector2(-12, -12); // setta // riga-ok
            rtHalo.offsetMax = new Vector2(12, 12); // setta // riga-ok
            Image imgHalo = haloObj.GetComponent<Image>(); // setta // riga-ok
            imgHalo.sprite = GetGlowCircleSprite(); // setta // riga-ok
            imgHalo.color = new Color(0.0f, 0.95f, 1f, 0.55f); // setta // riga-ok
            imgHalo.raycastTarget = false; // setta // riga-ok

            // Freccia Direzionale Ciano Ingrandita
            GameObject arrowObj = new GameObject("Arrow", typeof(RectTransform), typeof(Image)); // setta // riga-ok
            arrowObj.transform.SetParent(playerMarkerObj.transform, false); // chiama // riga-ok
            RectTransform rtArrow = arrowObj.GetComponent<RectTransform>(); // setta // riga-ok
            rtArrow.sizeDelta = new Vector2(26, 26); // setta // riga-ok
            rtArrow.anchoredPosition = Vector2.zero; // setta // riga-ok
            playerArrowImg = arrowObj.GetComponent<Image>(); // setta // riga-ok
            playerArrowImg.sprite = GetArrowSprite(); // setta // riga-ok
            playerArrowImg.color = Color.white; // setta // riga-ok
            playerArrowImg.raycastTarget = false; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void CreateTacticalLegend() // roba pub // riga-ok
        { // apre // riga-ok
            GameObject headerObj = new GameObject("TacticalLegendBar", typeof(RectTransform), typeof(Image)); // setta // riga-ok
            headerObj.transform.SetParent(overlayContainer, false); // chiama // riga-ok
            hudHeader = headerObj.GetComponent<RectTransform>(); // setta // riga-ok
            hudHeader.anchorMin = new Vector2(0.5f, 1f); // setta // riga-ok
            hudHeader.anchorMax = new Vector2(0.5f, 1f); // setta // riga-ok
            hudHeader.pivot = new Vector2(0.5f, 1f); // setta // riga-ok
            hudHeader.sizeDelta = new Vector2(920f, 38f); // setta // riga-ok
            hudHeader.anchoredPosition = new Vector2(0f, -14f); // setta // riga-ok

            Image imgBg = headerObj.GetComponent<Image>(); // setta // riga-ok
            imgBg.color = new Color(0.015f, 0.04f, 0.03f, 0.98f); // Sfondo scuro per massimo contrasto // setta // riga-ok
            imgBg.raycastTarget = false; // setta // riga-ok

            // Sottile linea inferiore d'accento neon verde (spessore 2px)
            GameObject bottomLine = new GameObject("BottomLine", typeof(RectTransform), typeof(Image)); // setta // riga-ok
            bottomLine.transform.SetParent(headerObj.transform, false); // chiama // riga-ok
            RectTransform rtLine = bottomLine.GetComponent<RectTransform>(); // setta // riga-ok
            rtLine.anchorMin = new Vector2(0f, 0f); // setta // riga-ok
            rtLine.anchorMax = new Vector2(1f, 0f); // setta // riga-ok
            rtLine.pivot = new Vector2(0.5f, 0f); // setta // riga-ok
            rtLine.sizeDelta = new Vector2(0f, 2f); // setta // riga-ok
            rtLine.anchoredPosition = Vector2.zero; // setta // riga-ok
            Image imgLine = bottomLine.GetComponent<Image>(); // setta // riga-ok
            imgLine.color = new Color(0.0f, 1.0f, 0.5f, 0.85f); // setta // riga-ok
            imgLine.raycastTarget = false; // setta // riga-ok

            GameObject txtObj = new GameObject("LegendText", typeof(RectTransform), typeof(TextMeshProUGUI)); // setta // riga-ok
            txtObj.transform.SetParent(headerObj.transform, false); // chiama // riga-ok
            RectTransform rtTxt = txtObj.GetComponent<RectTransform>(); // setta // riga-ok
            rtTxt.anchorMin = Vector2.zero; // setta // riga-ok
            rtTxt.anchorMax = Vector2.one; // setta // riga-ok
            rtTxt.offsetMin = new Vector2(14, 2); // setta // riga-ok
            rtTxt.offsetMax = new Vector2(-14, 0); // setta // riga-ok

            txtLegend = txtObj.GetComponent<TextMeshProUGUI>(); // setta // riga-ok
            txtLegend.color = Color.white; // setta // riga-ok
            txtLegend.fontSize = 15f; // setta // riga-ok
            txtLegend.fontStyle = FontStyles.Bold; // setta // riga-ok
            txtLegend.alignment = TextAlignmentOptions.Center; // setta // riga-ok
            txtLegend.enableWordWrapping = false; // setta // riga-ok
            txtLegend.text = "<b><color=#00F0FF>[ MAPPA TATTICA ]</color></b>   <color=#447755>|</color>   <color=#00FFA0>-- MURI</color>   <color=#447755>|</color>   <color=#FFE600>● INTERAZIONI</color>   <color=#447755>|</color>   <color=#FF3344>▲ NEMICI</color>   <color=#447755>|</color>   <color=#00F0FF>▲ GIOCATORE</color>"; // setta // riga-ok
            txtLegend.raycastTarget = false; // setta // riga-ok
        } // chiude // riga-ok

        #region Scansione Entità // prep ok // riga-ok

        // blocco: funzione fa cose
        private void ScanSceneEntities() // roba pub // riga-ok
        { // apre // riga-ok
            ResolveFollowTarget(); // chiama // riga-ok

            // 1. Scansione Oggetti Interagibili
            cachedInteractables.Clear(); // chiama // riga-ok
            cachedInteractableNames.Clear(); // chiama // riga-ok

            MonoBehaviour[] allScripts = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None); // setta // riga-ok
            HashSet<Transform> processedRoots = new HashSet<Transform>(); // setta // riga-ok

            // blocco: gira piu volte
            foreach (MonoBehaviour mb in allScripts) // ciclo x // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (mb == null || mb == this) continue; // se ok // riga-ok

                Transform t = mb.transform; // setta // riga-ok
                // blocco: controlla se va
                if (processedRoots.Contains(t)) continue; // se ok // riga-ok

                // Escludi il player, la camera e script di sistema/UI
                // blocco: controlla se va
                if (mb.CompareTag("Player") || t == playerTransform || mb is Camera || mb is Canvas || mb is GraphicRaycaster) // se ok // riga-ok
                    continue; // salta // riga-ok

                bool isInteractable = false; // setta // riga-ok
                string nomeTattico = ""; // setta // riga-ok

                // blocco: controlla se va
                if (mb is AccessCredentialPickup keycard) // se ok // riga-ok
                { // apre // riga-ok
                    isInteractable = true; // setta // riga-ok
                    nomeTattico = string.IsNullOrEmpty(keycard.DisplayName) ? "SCHEDA ACCESSO" : keycard.DisplayName; // setta // riga-ok
                } // chiude // riga-ok
                // blocco: controlla se va
                else if (mb is EmergencyHotspot hotspot) // se ok // riga-ok
                { // apre // riga-ok
                    isInteractable = true; // setta // riga-ok
                    string hName = hotspot.name.ToLower(); // setta // riga-ok
                    // blocco: controlla se va
                    if (hName.Contains("tank") || hName.Contains("chemic")) // se ok // riga-ok
                        nomeTattico = "SERBATOIO CHIMICO"; // setta // riga-ok
                    // blocco: controlla se va
                    else if (hName.Contains("generator") || hName.Contains("basic")) // se ok // riga-ok
                        nomeTattico = "GENERATORE AUSILIARIO"; // setta // riga-ok
                    // blocco: controlla se va
                    else if (hName.Contains("reactor")) // se ok // riga-ok
                        nomeTattico = "REATTORE"; // setta // riga-ok
                    // blocco: caso diverso
                    else // se no // riga-ok
                        nomeTattico = CleanObjectName(hotspot.name); // setta // riga-ok
                } // chiude // riga-ok
                // blocco: controlla se va
                else if (mb is PortaSettore porta) // se ok // riga-ok
                { // apre // riga-ok
                    isInteractable = true; // setta // riga-ok
                    nomeTattico = CleanDoorName(porta.gameObject.name); // setta // riga-ok
                } // chiude // riga-ok
                // blocco: controlla se va
                else if (mb is QuarantineGate) // se ok // riga-ok
                { // apre // riga-ok
                    isInteractable = true; // setta // riga-ok
                    nomeTattico = "CANCELLO QUARANTENA"; // setta // riga-ok
                } // chiude // riga-ok
                // blocco: controlla se va
                else if (mb is TerminalePorta term) // se ok // riga-ok
                { // apre // riga-ok
                    isInteractable = true; // setta // riga-ok
                    nomeTattico = string.IsNullOrEmpty(term.nomeTerminale) ? "TERMINALE PORTA" : term.nomeTerminale; // setta // riga-ok
                } // chiude // riga-ok
                // blocco: controlla se va
                else if (mb is DatapadCodiciPorte datapad) // se ok // riga-ok
                { // apre // riga-ok
                    isInteractable = true; // setta // riga-ok
                    nomeTattico = string.IsNullOrEmpty(datapad.titoloDatapad) ? "DATAPAD SICUREZZA" : datapad.titoloDatapad; // setta // riga-ok
                } // chiude // riga-ok
                // blocco: rimosso controllo CuboNeroTeletrasporto (la mappa non deve mostrare l'uscita in anticipo)
                // blocco: controlla se va
                else if (mb is IInteractable) // se ok // riga-ok
                { // apre // riga-ok
                    string rawLower = t.gameObject.name.ToLower(); // setta // riga-ok
                    // blocco: controlla se va
                    if (!rawLower.Contains("light") && !rawLower.Contains("cam") && !rawLower.Contains("audio") && !rawLower.Contains("sound") && !rawLower.Contains("volume") && !rawLower.Contains("vfx")) // se ok // riga-ok
                    { // apre // riga-ok
                        isInteractable = true; // setta // riga-ok
                        nomeTattico = CleanObjectName(t.gameObject.name); // setta // riga-ok
                    } // chiude // riga-ok
                } // chiude // riga-ok
                // blocco: controlla se va
                else if (t.CompareTag("Interactable") || t.gameObject.layer == LayerMask.NameToLayer("Interactable")) // se ok // riga-ok
                { // apre // riga-ok
                    isInteractable = true; // setta // riga-ok
                    nomeTattico = CleanObjectName(t.gameObject.name); // setta // riga-ok
                } // chiude // riga-ok

                // blocco: controlla se va
                if (isInteractable) // se ok // riga-ok
                { // apre // riga-ok
                    nomeTattico = CleanObjectName(nomeTattico); // setta // riga-ok

                    // Deduplicazione per vicinanza (se c'è già un'interazione entro 2.2m, evita etichette sovrapposte)
                    bool isDuplicate = false; // setta // riga-ok
                    // blocco: gira piu volte
                    for (int k = 0; k < cachedInteractables.Count; k++) // ciclo x // riga-ok
                    { // apre // riga-ok
                        // blocco: controlla se va
                        if (Vector3.Distance(t.position, cachedInteractables[k].position) < 2.2f) // se ok // riga-ok
                        { // apre // riga-ok
                            isDuplicate = true; // setta // riga-ok
                            break; // stop // riga-ok
                        } // chiude // riga-ok
                    } // chiude // riga-ok

                    // blocco: controlla se va
                    if (!isDuplicate) // se ok // riga-ok
                    { // apre // riga-ok
                        processedRoots.Add(t); // chiama // riga-ok
                        cachedInteractables.Add(t); // chiama // riga-ok
                        // blocco: controlla se va
                        if (nomeTattico.Length > 24) nomeTattico = nomeTattico.Substring(0, 24); // se ok // riga-ok
                        cachedInteractableNames.Add(nomeTattico.ToUpper()); // chiama // riga-ok
                    } // chiude // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok

            // 2. Scansione Nemici
            cachedEnemies.Clear(); // chiama // riga-ok
            HashSet<Transform> processedEnemies = new HashSet<Transform>(); // setta // riga-ok

            // blocco: gira piu volte
            foreach (MonoBehaviour mb in allScripts) // ciclo x // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (mb == null) continue; // se ok // riga-ok

                Transform t = mb.transform; // setta // riga-ok
                // blocco: controlla se va
                if (processedEnemies.Contains(t)) continue; // se ok // riga-ok

                string typeName = mb.GetType().Name.ToLower(); // setta // riga-ok
                bool isEnemy = mb is DroneRonda || mb is GuardiaNpc || mb is ManutenzioneBot || mb is NPC || // setta // riga-ok
                               typeName.Contains("drone") || typeName.Contains("guardia") || typeName.Contains("bot") || // ok qua // riga-ok
                               typeName.Contains("nemico") || typeName.Contains("enemy") || typeName.Contains("npc") || // ok qua // riga-ok
                               t.CompareTag("Enemy") || t.gameObject.layer == LayerMask.NameToLayer("Enemy"); // setta // riga-ok

                // blocco: controlla se va
                if (isEnemy && t != playerTransform && !processedRoots.Contains(t)) // se ok // riga-ok
                { // apre // riga-ok
                    processedEnemies.Add(t); // chiama // riga-ok
                    cachedEnemies.Add(t); // chiama // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private static string CleanDoorName(string raw) // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (string.IsNullOrWhiteSpace(raw)) return "PORTA"; // se ok // riga-ok
            string n = raw.ToUpper().Replace("(CLONE)", "").Replace("_", " ").Trim(); // setta // riga-ok
            n = System.Text.RegularExpressions.Regex.Replace(n, @"(?i)(DEFAULTMATERIAL|MATERIAL|MESH|PREFAB|LOD\d*|\(\d+\))", "").Trim(); // setta // riga-ok
            n = System.Text.RegularExpressions.Regex.Replace(n, @"\s+", " ").Trim(); // setta // riga-ok
            // blocco: controlla se va
            if (n.Contains("PORTA") || n.Contains("DOOR") || n.Contains("GATE") || n.Contains("SETTORE")) return n; // se ok // riga-ok
            return "PORTA " + n; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private static string CleanObjectName(string raw) // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (string.IsNullOrWhiteSpace(raw)) return "INTERAGIBILE"; // se ok // riga-ok
            string n = raw.ToUpper().Replace("(CLONE)", "").Replace("_", " ").Trim(); // setta // riga-ok
            n = System.Text.RegularExpressions.Regex.Replace(n, @"(?i)(DEFAULTMATERIAL|MATERIAL|MESH|PREFAB|LOD\d*|\(\d+\))", "").Trim(); // setta // riga-ok
            n = System.Text.RegularExpressions.Regex.Replace(n, @"\s+", " ").Trim(); // setta // riga-ok
            // blocco: controlla se va
            if (string.IsNullOrWhiteSpace(n)) return "INTERAGIBILE"; // se ok // riga-ok
            return n; // torna val // riga-ok
        } // chiude // riga-ok

        #endregion // prep ok // riga-ok

        #region Aggiornamento Posizioni & Rendering Marker // prep ok // riga-ok

        // blocco: funzione fa cose
        private void UpdateOverlayPositions() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (overlayContainer == null || mapCamera == null) return; // se ok // riga-ok

            Vector2 mapSize = overlayContainer.rect.size; // setta // riga-ok
            float pulse = Mathf.Sin(Time.unscaledTime * 5f) * 0.15f + 0.85f; // setta // riga-ok

            // 1. Aggiorna Marker Giocatore
            // blocco: controlla se va
            if (playerTransform != null && playerMarkerObj != null) // se ok // riga-ok
            { // apre // riga-ok
                Vector3 vp = mapCamera.WorldToViewportPoint(playerTransform.position); // setta // riga-ok
                // blocco: controlla se va
                if (IsInsideViewport(vp)) // se ok // riga-ok
                { // apre // riga-ok
                    playerMarkerObj.SetActive(true); // chiama // riga-ok
                    playerMarkerRect.anchoredPosition = ViewportToLocalPos(vp, mapSize); // setta // riga-ok
                    float rotY = playerTransform.eulerAngles.y; // setta // riga-ok
                    playerMarkerRect.localRotation = Quaternion.Euler(0f, 0f, -rotY); // setta // riga-ok
                } // chiude // riga-ok
                // blocco: caso diverso
                else // se no // riga-ok
                { // apre // riga-ok
                    playerMarkerObj.SetActive(false); // chiama // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok

            // 2. Aggiorna Marker Interagibili (Giallo + Nome Terminale)
            SyncInteractableMarkerCount(cachedInteractables.Count); // chiama // riga-ok
            // blocco: gira piu volte
            for (int i = 0; i < cachedInteractables.Count; i++) // ciclo x // riga-ok
            { // apre // riga-ok
                Transform target = cachedInteractables[i]; // setta // riga-ok
                InteractableMarkerItem item = activeInteractableMarkers[i]; // setta // riga-ok

                // blocco: controlla se va
                if (target == null || !target.gameObject.activeInHierarchy) // se ok // riga-ok
                { // apre // riga-ok
                    item.root.SetActive(false); // chiama // riga-ok
                    continue; // salta // riga-ok
                } // chiude // riga-ok

                Vector3 vp = mapCamera.WorldToViewportPoint(target.position); // setta // riga-ok
                // blocco: controlla se va
                if (IsInsideViewport(vp)) // se ok // riga-ok
                { // apre // riga-ok
                    item.root.SetActive(true); // chiama // riga-ok
                    item.rect.anchoredPosition = ViewportToLocalPos(vp, mapSize); // setta // riga-ok

                    // Pulsazione alone giallo
                    Color haloC = interactableHaloColor; // setta // riga-ok
                    haloC.a = 0.40f * pulse; // setta // riga-ok
                    item.imgHalo.color = haloC; // setta // riga-ok

                    // Testo nome font terminale ad alta luminosità e contrasto
                    string nome = cachedInteractableNames[i]; // setta // riga-ok
                    item.txtLabel.color = Color.white; // setta // riga-ok
                    item.txtLabel.text = $"<color=#FFE600><b>[E]</b></color> <color=#FFFFFF><b>{nome}</b></color>"; // setta // riga-ok

                    // Dimensionamento dinamico badge al pixel perfetto
                    float textW = item.txtLabel.preferredWidth; // setta // riga-ok
                    item.badgeRect.sizeDelta = new Vector2(textW + 24f, 30f); // setta // riga-ok
                } // chiude // riga-ok
                // blocco: caso diverso
                else // se no // riga-ok
                { // apre // riga-ok
                    item.root.SetActive(false); // chiama // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok

            // 3. Aggiorna Marker Nemici (Rosso + Direzione)
            SyncEnemyMarkerCount(cachedEnemies.Count); // chiama // riga-ok
            // blocco: gira piu volte
            for (int i = 0; i < cachedEnemies.Count; i++) // ciclo x // riga-ok
            { // apre // riga-ok
                Transform enemy = cachedEnemies[i]; // setta // riga-ok
                EnemyMarkerItem item = activeEnemyMarkers[i]; // setta // riga-ok

                // blocco: controlla se va
                if (enemy == null || !enemy.gameObject.activeInHierarchy) // se ok // riga-ok
                { // apre // riga-ok
                    item.root.SetActive(false); // chiama // riga-ok
                    continue; // salta // riga-ok
                } // chiude // riga-ok

                Vector3 vp = mapCamera.WorldToViewportPoint(enemy.position); // setta // riga-ok
                // blocco: controlla se va
                if (IsInsideViewport(vp)) // se ok // riga-ok
                { // apre // riga-ok
                    item.root.SetActive(true); // chiama // riga-ok
                    item.rect.anchoredPosition = ViewportToLocalPos(vp, mapSize); // setta // riga-ok

                    // Pulsazione alone rosso
                    Color haloC = enemyHaloColor; // setta // riga-ok
                    haloC.a = 0.45f * pulse; // setta // riga-ok
                    item.imgHalo.color = haloC; // setta // riga-ok

                    // Rotazione indicatore verso la direzione di guardia/pattuglia
                    float rotY = enemy.eulerAngles.y; // setta // riga-ok
                    item.rect.localRotation = Quaternion.Euler(0f, 0f, -rotY); // setta // riga-ok
                } // chiude // riga-ok
                // blocco: caso diverso
                else // se no // riga-ok
                { // apre // riga-ok
                    item.root.SetActive(false); // chiama // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private static bool IsInsideViewport(Vector3 vp) // roba pub // riga-ok
        { // apre // riga-ok
            return vp.z > 0f && vp.x >= -0.05f && vp.x <= 1.05f && vp.y >= -0.05f && vp.y <= 1.05f; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private static Vector2 ViewportToLocalPos(Vector3 vp, Vector2 mapSize) // roba pub // riga-ok
        { // apre // riga-ok
            return new Vector2((vp.x - 0.5f) * mapSize.x, (vp.y - 0.5f) * mapSize.y); // torna val // riga-ok
        } // chiude // riga-ok

        #endregion // prep ok // riga-ok

        #region Gestione Pool Marker // prep ok // riga-ok

        // blocco: funzione fa cose
        private void SyncInteractableMarkerCount(int targetCount) // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: gira piu volte
            while (activeInteractableMarkers.Count < targetCount) // ciclo x // riga-ok
            { // apre // riga-ok
                activeInteractableMarkers.Add(CreaInteractableMarker()); // chiama // riga-ok
            } // chiude // riga-ok

            // blocco: gira piu volte
            for (int i = targetCount; i < activeInteractableMarkers.Count; i++) // ciclo x // riga-ok
            { // apre // riga-ok
                activeInteractableMarkers[i].root.SetActive(false); // chiama // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private InteractableMarkerItem CreaInteractableMarker() // roba pub // riga-ok
        { // apre // riga-ok
            GameObject root = new GameObject("InteractableMarker", typeof(RectTransform)); // setta // riga-ok
            root.transform.SetParent(overlayContainer, false); // chiama // riga-ok
            RectTransform rect = root.GetComponent<RectTransform>(); // setta // riga-ok
            rect.sizeDelta = new Vector2(38, 38); // setta // riga-ok

            // 1. Alone Giallo Soft
            GameObject haloObj = new GameObject("Halo", typeof(RectTransform), typeof(Image)); // setta // riga-ok
            haloObj.transform.SetParent(root.transform, false); // chiama // riga-ok
            RectTransform rtHalo = haloObj.GetComponent<RectTransform>(); // setta // riga-ok
            rtHalo.anchorMin = Vector2.zero; // setta // riga-ok
            rtHalo.anchorMax = Vector2.one; // setta // riga-ok
            rtHalo.offsetMin = new Vector2(-12, -12); // setta // riga-ok
            rtHalo.offsetMax = new Vector2(12, 12); // setta // riga-ok
            Image imgHalo = haloObj.GetComponent<Image>(); // setta // riga-ok
            imgHalo.sprite = GetGlowCircleSprite(); // setta // riga-ok
            imgHalo.color = new Color(1f, 0.92f, 0.15f, 0.45f); // setta // riga-ok
            imgHalo.raycastTarget = false; // setta // riga-ok

            // 2. Punto Centrale Giallo Solido
            GameObject coreObj = new GameObject("Core", typeof(RectTransform), typeof(Image)); // setta // riga-ok
            coreObj.transform.SetParent(root.transform, false); // chiama // riga-ok
            RectTransform rtCore = coreObj.GetComponent<RectTransform>(); // setta // riga-ok
            rtCore.sizeDelta = new Vector2(16, 16); // setta // riga-ok
            rtCore.anchoredPosition = Vector2.zero; // setta // riga-ok
            Image imgCore = coreObj.GetComponent<Image>(); // setta // riga-ok
            imgCore.sprite = GetSolidCircleSprite(); // setta // riga-ok
            imgCore.color = new Color(1f, 1f, 0.4f, 1f); // setta // riga-ok
            imgCore.raycastTarget = false; // setta // riga-ok

            // 3. Badge Box Contenitore (Posizionato sopra il punto)
            GameObject badgeBox = new GameObject("BadgeBox", typeof(RectTransform)); // setta // riga-ok
            badgeBox.transform.SetParent(root.transform, false); // chiama // riga-ok
            RectTransform rtBadge = badgeBox.GetComponent<RectTransform>(); // setta // riga-ok
            rtBadge.pivot = new Vector2(0.5f, 0f); // setta // riga-ok
            rtBadge.anchoredPosition = new Vector2(0, 22f); // setta // riga-ok
            rtBadge.sizeDelta = new Vector2(140, 28); // setta // riga-ok

            // 3a. Bordo neon giallo
            GameObject borderObj = new GameObject("Border", typeof(RectTransform), typeof(Image)); // setta // riga-ok
            borderObj.transform.SetParent(badgeBox.transform, false); // chiama // riga-ok
            RectTransform rtBorder = borderObj.GetComponent<RectTransform>(); // setta // riga-ok
            rtBorder.anchorMin = Vector2.zero; // setta // riga-ok
            rtBorder.anchorMax = Vector2.one; // setta // riga-ok
            rtBorder.offsetMin = new Vector2(-1.5f, -1.5f); // setta // riga-ok
            rtBorder.offsetMax = new Vector2(1.5f, 1.5f); // setta // riga-ok
            Image imgBorder = borderObj.GetComponent<Image>(); // setta // riga-ok
            imgBorder.color = new Color(1f, 0.88f, 0.2f, 1f); // setta // riga-ok
            imgBorder.raycastTarget = false; // setta // riga-ok

            // 3b. Sfondo solido scuro ad alto contrasto
            GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image)); // setta // riga-ok
            bgObj.transform.SetParent(badgeBox.transform, false); // chiama // riga-ok
            RectTransform rtBg = bgObj.GetComponent<RectTransform>(); // setta // riga-ok
            rtBg.anchorMin = Vector2.zero; // setta // riga-ok
            rtBg.anchorMax = Vector2.one; // setta // riga-ok
            rtBg.offsetMin = Vector2.zero; // setta // riga-ok
            rtBg.offsetMax = Vector2.zero; // setta // riga-ok
            Image imgBg = bgObj.GetComponent<Image>(); // setta // riga-ok
            imgBg.color = new Color(0.01f, 0.05f, 0.03f, 0.98f); // setta // riga-ok
            imgBg.raycastTarget = false; // setta // riga-ok

            // 3c. Testo TextMeshPro chiaro e definito
            GameObject txtObj = new GameObject("TxtName", typeof(RectTransform), typeof(TextMeshProUGUI)); // setta // riga-ok
            txtObj.transform.SetParent(badgeBox.transform, false); // chiama // riga-ok
            RectTransform rtTxt = txtObj.GetComponent<RectTransform>(); // setta // riga-ok
            rtTxt.anchorMin = Vector2.zero; // setta // riga-ok
            rtTxt.anchorMax = Vector2.one; // setta // riga-ok
            rtTxt.offsetMin = new Vector2(8, 0); // setta // riga-ok
            rtTxt.offsetMax = new Vector2(-8, 0); // setta // riga-ok

            TextMeshProUGUI txt = txtObj.GetComponent<TextMeshProUGUI>(); // setta // riga-ok
            txt.color = Color.white; // setta // riga-ok
            txt.fontSize = 15f; // setta // riga-ok
            txt.fontStyle = FontStyles.Bold; // setta // riga-ok
            txt.alignment = TextAlignmentOptions.Center; // setta // riga-ok
            txt.enableWordWrapping = false; // setta // riga-ok
            txt.raycastTarget = false; // setta // riga-ok

            return new InteractableMarkerItem // torna val // riga-ok
            { // apre // riga-ok
                root = root, // setta // riga-ok
                rect = rect, // setta // riga-ok
                badgeRect = rtBadge, // setta // riga-ok
                imgHalo = imgHalo, // setta // riga-ok
                imgCore = imgCore, // setta // riga-ok
                txtLabel = txt // setta // riga-ok
            }; // ok qua // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void SyncEnemyMarkerCount(int targetCount) // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: gira piu volte
            while (activeEnemyMarkers.Count < targetCount) // ciclo x // riga-ok
            { // apre // riga-ok
                activeEnemyMarkers.Add(CreaEnemyMarker()); // chiama // riga-ok
            } // chiude // riga-ok

            // blocco: gira piu volte
            for (int i = targetCount; i < activeEnemyMarkers.Count; i++) // ciclo x // riga-ok
            { // apre // riga-ok
                activeEnemyMarkers[i].root.SetActive(false); // chiama // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private EnemyMarkerItem CreaEnemyMarker() // roba pub // riga-ok
        { // apre // riga-ok
            GameObject root = new GameObject("EnemyMarker", typeof(RectTransform)); // setta // riga-ok
            root.transform.SetParent(overlayContainer, false); // chiama // riga-ok
            RectTransform rect = root.GetComponent<RectTransform>(); // setta // riga-ok
            rect.sizeDelta = new Vector2(38, 38); // setta // riga-ok

            // Alone Rosso Soft
            GameObject haloObj = new GameObject("Halo", typeof(RectTransform), typeof(Image)); // setta // riga-ok
            haloObj.transform.SetParent(root.transform, false); // chiama // riga-ok
            RectTransform rtHalo = haloObj.GetComponent<RectTransform>(); // setta // riga-ok
            rtHalo.anchorMin = Vector2.zero; // setta // riga-ok
            rtHalo.anchorMax = Vector2.one; // setta // riga-ok
            rtHalo.offsetMin = new Vector2(-10, -10); // setta // riga-ok
            rtHalo.offsetMax = new Vector2(10, 10); // setta // riga-ok
            Image imgHalo = haloObj.GetComponent<Image>(); // setta // riga-ok
            imgHalo.sprite = GetGlowCircleSprite(); // setta // riga-ok
            imgHalo.color = new Color(1f, 0.15f, 0.2f, 0.45f); // setta // riga-ok
            imgHalo.raycastTarget = false; // setta // riga-ok

            // Freccia Direzionale Rossa (Triangolo Rosso Ingrandito)
            GameObject arrowObj = new GameObject("Arrow", typeof(RectTransform), typeof(Image)); // setta // riga-ok
            arrowObj.transform.SetParent(root.transform, false); // chiama // riga-ok
            RectTransform rtArrow = arrowObj.GetComponent<RectTransform>(); // setta // riga-ok
            rtArrow.sizeDelta = new Vector2(24, 24); // setta // riga-ok
            rtArrow.anchoredPosition = Vector2.zero; // setta // riga-ok
            Image imgArrow = arrowObj.GetComponent<Image>(); // setta // riga-ok
            imgArrow.sprite = GetArrowSprite(); // setta // riga-ok
            imgArrow.color = new Color(1f, 0.2f, 0.25f, 1f); // setta // riga-ok
            imgArrow.raycastTarget = false; // setta // riga-ok

            return new EnemyMarkerItem // torna val // riga-ok
            { // apre // riga-ok
                root = root, // setta // riga-ok
                rect = rect, // setta // riga-ok
                imgHalo = imgHalo, // setta // riga-ok
                imgArrow = imgArrow // setta // riga-ok
            }; // ok qua // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void ClearAllMarkers() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: gira piu volte
            foreach (var item in activeInteractableMarkers) // ciclo x // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (item.root != null) item.root.SetActive(false); // se ok // riga-ok
            } // chiude // riga-ok

            // blocco: gira piu volte
            foreach (var item in activeEnemyMarkers) // ciclo x // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (item.root != null) item.root.SetActive(false); // se ok // riga-ok
            } // chiude // riga-ok

            // blocco: controlla se va
            if (playerMarkerObj != null) // se ok // riga-ok
                playerMarkerObj.SetActive(false); // chiama // riga-ok
        } // chiude // riga-ok

        #endregion // prep ok // riga-ok

        #region Texture Procedurali (Halos & Sprites) // prep ok // riga-ok

        private static Sprite s_GlowCircleSprite; // roba pub // riga-ok
        private static Sprite s_SolidCircleSprite; // roba pub // riga-ok
        private static Sprite s_ArrowSprite; // roba pub // riga-ok

        // blocco: funzione fa cose
        public static Sprite GetGlowCircleSprite() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (s_GlowCircleSprite != null) return s_GlowCircleSprite; // se ok // riga-ok

            int size = 64; // setta // riga-ok
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false); // setta // riga-ok
            tex.name = "Proc_GlowCircle"; // setta // riga-ok
            float center = (size - 1) * 0.5f; // setta // riga-ok
            float maxR = center; // setta // riga-ok

            // blocco: gira piu volte
            for (int y = 0; y < size; y++) // ciclo x // riga-ok
            { // apre // riga-ok
                // blocco: gira piu volte
                for (int x = 0; x < size; x++) // ciclo x // riga-ok
                { // apre // riga-ok
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)); // setta // riga-ok
                    float t = Mathf.Clamp01(dist / maxR); // setta // riga-ok
                    float alpha = Mathf.Pow(1f - t, 2.2f); // setta // riga-ok
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha)); // chiama // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
            tex.Apply(); // chiama // riga-ok
            s_GlowCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f)); // setta // riga-ok
            return s_GlowCircleSprite; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public static Sprite GetSolidCircleSprite() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (s_SolidCircleSprite != null) return s_SolidCircleSprite; // se ok // riga-ok

            int size = 32; // setta // riga-ok
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false); // setta // riga-ok
            tex.name = "Proc_SolidCircle"; // setta // riga-ok
            float center = (size - 1) * 0.5f; // setta // riga-ok
            float r = center - 1f; // setta // riga-ok

            // blocco: gira piu volte
            for (int y = 0; y < size; y++) // ciclo x // riga-ok
            { // apre // riga-ok
                // blocco: gira piu volte
                for (int x = 0; x < size; x++) // ciclo x // riga-ok
                { // apre // riga-ok
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)); // setta // riga-ok
                    float alpha = Mathf.Clamp01((r - dist) + 0.5f); // setta // riga-ok
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha)); // chiama // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
            tex.Apply(); // chiama // riga-ok
            s_SolidCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f)); // setta // riga-ok
            return s_SolidCircleSprite; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public static Sprite GetArrowSprite() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (s_ArrowSprite != null) return s_ArrowSprite; // se ok // riga-ok

            int size = 32; // setta // riga-ok
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false); // setta // riga-ok
            tex.name = "Proc_Arrow"; // setta // riga-ok

            // blocco: gira piu volte
            for (int y = 0; y < size; y++) // ciclo x // riga-ok
            { // apre // riga-ok
                // blocco: gira piu volte
                for (int x = 0; x < size; x++) // ciclo x // riga-ok
                { // apre // riga-ok
                    float u = (float)x / (size - 1); // setta // riga-ok
                    float v = (float)y / (size - 1); // setta // riga-ok
                    float halfWidthAtV = (1.0f - v) * 0.45f; // setta // riga-ok
                    float distFromCenter = Mathf.Abs(u - 0.5f); // setta // riga-ok
                    float alpha = (distFromCenter <= halfWidthAtV && v >= 0.05f) ? 1f : 0f; // setta // riga-ok
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha)); // chiama // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
            tex.Apply(); // chiama // riga-ok
            s_ArrowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f)); // setta // riga-ok
            return s_ArrowSprite; // torna val // riga-ok
        } // chiude // riga-ok

        #endregion // prep ok // riga-ok

        // blocco: funzione fa cose
        private void ResolveFollowTarget() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (playerTransform != null) // se ok // riga-ok
            { // apre // riga-ok
                followTarget = playerTransform; // setta // riga-ok
                return; // torna val // riga-ok
            } // chiude // riga-ok

            GameObject player = null; // setta // riga-ok
            // blocco: prova safe
            try // prova // riga-ok
            { // apre // riga-ok
                player = GameObject.FindGameObjectWithTag(playerTag); // setta // riga-ok
            } // chiude // riga-ok
            // blocco: becca errore
            catch (UnityException) // err qui // riga-ok
            { // apre // riga-ok
                player = null; // setta // riga-ok
            } // chiude // riga-ok

            // blocco: controlla se va
            if (!player) // se ok // riga-ok
            { // apre // riga-ok
                muve_pg playerScript = FindFirstObjectByType<muve_pg>(); // setta // riga-ok
                // blocco: controlla se va
                if (playerScript != null) // se ok // riga-ok
                    player = playerScript.gameObject; // setta // riga-ok
            } // chiude // riga-ok

            // blocco: controlla se va
            if (player) // se ok // riga-ok
            { // apre // riga-ok
                playerTransform = player.transform; // setta // riga-ok
                followTarget = playerTransform; // setta // riga-ok
                return; // torna val // riga-ok
            } // chiude // riga-ok

            // blocco: controlla se va
            if (Camera.main) // se ok // riga-ok
                followTarget = Camera.main.transform; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: classe x roba grossa
        private class InteractableMarkerItem // classe qui // riga-ok
        { // apre // riga-ok
            public GameObject root; // roba pub // riga-ok
            public RectTransform rect; // roba pub // riga-ok
            public RectTransform badgeRect; // roba pub // riga-ok
            public Image imgHalo; // roba pub // riga-ok
            public Image imgCore; // roba pub // riga-ok
            public TextMeshProUGUI txtLabel; // roba pub // riga-ok
        } // chiude // riga-ok

        // blocco: classe x roba grossa
        private class EnemyMarkerItem // classe qui // riga-ok
        { // apre // riga-ok
            public GameObject root; // roba pub // riga-ok
            public RectTransform rect; // roba pub // riga-ok
            public Image imgHalo; // roba pub // riga-ok
            public Image imgArrow; // roba pub // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
