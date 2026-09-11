using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace AsyncronQuest.SteampunkUI
{
    /// <summary>
    /// Gestore della Mappa Tattica Olografica dall'Alto (Top-Down Radar).
    /// Mostra:
    /// - Contorni geometrici dei muri in Verde Neon brillante su sfondo scuro.
    /// - Oggetti interagibili con alone/aura Giallo vivido e targhetta con nome in Font Terminale.
    /// - Nemici in Rosso vivo con indicatore direzionale.
    /// - Posizione e orientamento del Giocatore in Ciano.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RawImage))]
    public sealed class SceneTopDownMapUI : MonoBehaviour
    {
        [Header("Target & Camera")]
        [SerializeField] private Transform followTarget;
        [SerializeField] private string playerTag = "Player";
        [SerializeField, Min(10f)] private float cameraHeight = 150f;
        [SerializeField, Min(5f)] private float orthographicSize = 65f;
        [SerializeField, Min(64)] private int textureSize = 1024;
        [SerializeField] private LayerMask cullingMask = ~((1 << 1) | (1 << 2) | (1 << 5));
        [SerializeField] private Color cameraClearColor = new Color(0.005f, 0.015f, 0.010f, 1f);
        [SerializeField] private bool centerOnLevel = true;

        [Header("Stile Tattico Neon")]
        [SerializeField] private Color neonWallColor = new Color(0.0f, 1.0f, 0.4f, 1.0f); // Verde Neon
        [SerializeField] private Color interactableHaloColor = new Color(1.0f, 0.92f, 0.15f, 1.0f); // Giallo Vivido
        [SerializeField] private Color enemyHaloColor = new Color(1.0f, 0.12f, 0.18f, 1.0f); // Rosso Vivido
        [SerializeField] private Color playerBeaconColor = new Color(0.0f, 0.95f, 1.0f, 1.0f); // Ciano

        private RawImage rawImage;
        private Camera mapCamera;
        private RenderTexture renderTexture;
        private Material tacticalNeonMaterial;

        // Contenitore UI Overlay sopra la mappa
        private RectTransform overlayContainer;
        private RectTransform hudHeader;
        private Text txtLegend;

        // Tracciamento entità
        private Transform playerTransform;
        private GameObject playerMarkerObj;
        private RectTransform playerMarkerRect;
        private Image playerArrowImg;

        // Pool di Marker
        private readonly List<InteractableMarkerItem> activeInteractableMarkers = new List<InteractableMarkerItem>();
        private readonly List<EnemyMarkerItem> activeEnemyMarkers = new List<EnemyMarkerItem>();

        private readonly List<Transform> cachedInteractables = new List<Transform>();
        private readonly List<string> cachedInteractableNames = new List<string>();
        private readonly List<Transform> cachedEnemies = new List<Transform>();

        private readonly List<Renderer> tempDisabledFloorRenderers = new List<Renderer>();
        private readonly List<Light> tempDisabledLights = new List<Light>();

        private Font terminalFont;
        private float nextScanTime = 0f;
        private float currentZoom = 65f;
        private Vector3 calculatedLevelCenter = Vector3.zero;
        private Bounds calculatedLevelBounds;
        private bool levelBoundsCalculated = false;

        private void Awake()
        {
            rawImage = GetComponent<RawImage>();
            currentZoom = orthographicSize;
            terminalFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            SetupTacticalMaterial();
            CreateCamera();
            CreateOverlayUI();
            ResolveFollowTarget();
        }

        private void OnEnable()
        {
            Camera.onPreCull += OnPreCullMapCamera;
            Camera.onPostRender += OnPostRenderMapCamera;

            if (mapCamera)
                mapCamera.enabled = true;

            RecalculateLevelBoundsAndFraming();
            ScanSceneEntities();
            UpdateOverlayPositions();
        }

        private void OnDisable()
        {
            Camera.onPreCull -= OnPreCullMapCamera;
            Camera.onPostRender -= OnPostRenderMapCamera;
            RestoreFloorRenderers();
            RestoreLights();

            if (mapCamera)
                mapCamera.enabled = false;

            ClearAllMarkers();
        }

        private void OnDestroy()
        {
            Camera.onPreCull -= OnPreCullMapCamera;
            Camera.onPostRender -= OnPostRenderMapCamera;
            RestoreFloorRenderers();
            RestoreLights();

            if (mapCamera)
                Destroy(mapCamera.gameObject);

            if (renderTexture)
                renderTexture.Release();

            if (tacticalNeonMaterial)
                Destroy(tacticalNeonMaterial);
        }

        private void Update()
        {
            if (!isActiveAndEnabled) return;

            // Zoom con la rotellina del mouse sulla mappa
            HandleZoomInput();
        }

        private void LateUpdate()
        {
            if (!followTarget)
                ResolveFollowTarget();

            if (!mapCamera)
                return;

            Vector3 center = (centerOnLevel && levelBoundsCalculated) ? calculatedLevelCenter : (followTarget ? followTarget.position : Vector3.zero);
            mapCamera.transform.position = new Vector3(center.x, center.y + cameraHeight, center.z);
            mapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            mapCamera.orthographicSize = currentZoom;
            mapCamera.cullingMask = cullingMask;

            // Scansione periodica entità ogni 1.5s
            if (Time.unscaledTime >= nextScanTime)
            {
                ScanSceneEntities();
                nextScanTime = Time.unscaledTime + 1.5f;
            }

            UpdateOverlayPositions();
        }

        private void HandleZoomInput()
        {
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    currentZoom = Mathf.Clamp(currentZoom - Mathf.Sign(scroll) * 4f, 20f, 130f);
                }
            }
        }

        public void Configure(Transform target, float height, float size, int renderTextureSize)
        {
            followTarget = target;
            cameraHeight = Mathf.Max(10f, height);
            orthographicSize = Mathf.Max(5f, size);
            currentZoom = orthographicSize;
            textureSize = Mathf.Max(64, renderTextureSize);

            if (!rawImage)
                rawImage = GetComponent<RawImage>();

            SetupTacticalMaterial();
            CreateCamera();
            CreateOverlayUI();
            RecalculateLevelBoundsAndFraming();
        }

        private void SetupTacticalMaterial()
        {
            if (tacticalNeonMaterial != null) return;

            Shader shader = Shader.Find("UI/TacticalNeonMap");
            if (shader != null)
            {
                tacticalNeonMaterial = new Material(shader);
                tacticalNeonMaterial.name = "Mat_TacticalNeonMap";
                tacticalNeonMaterial.SetColor("_EdgeColor", neonWallColor);
                tacticalNeonMaterial.SetColor("_BackgroundColor", cameraClearColor);
                tacticalNeonMaterial.SetFloat("_EdgeThreshold", 0.18f);
                tacticalNeonMaterial.SetFloat("_EdgeGlow", 2.4f);

                if (rawImage != null)
                    rawImage.material = tacticalNeonMaterial;
            }
        }

        private void RecalculateLevelBoundsAndFraming()
        {
            calculatedLevelBounds = CalculateLevelBounds();
            calculatedLevelCenter = calculatedLevelBounds.center;
            levelBoundsCalculated = true;

            float maxExtent = Mathf.Max(calculatedLevelBounds.size.x, calculatedLevelBounds.size.z);
            if (maxExtent > 8f)
            {
                // Margine del 30% per non toccare in alcun modo i bordi della cornice del monitor
                float optimalZoom = Mathf.Clamp(maxExtent * 0.65f, 40f, 95f);
                orthographicSize = optimalZoom;
                currentZoom = optimalZoom;
            }
        }

        private Bounds CalculateLevelBounds()
        {
            Bounds b = new Bounds(Vector3.zero, Vector3.zero);
            bool initialized = false;

            Collider[] colliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);
            foreach (Collider col in colliders)
            {
                if (col == null || col.isTrigger) continue;
                GameObject go = col.gameObject;
                if (!go.activeInHierarchy) continue;

                if (go.CompareTag("Player") || go.CompareTag("Enemy") || go.GetComponent<muve_pg>() != null || go.GetComponent<Camera>() != null)
                    continue;

                string n = go.name.ToLower();
                if (n.Contains("pavimento") || n.Contains("floor") || n.Contains("terrain") || n.Contains("ground") || n.Contains("plane"))
                    continue;

                Vector3 size = col.bounds.size;
                if (size.y >= 0.7f || n.Contains("muro") || n.Contains("wall") || n.Contains("porte") || n.Contains("door") || n.Contains("porta") || n.Contains("cube"))
                {
                    if (!initialized)
                    {
                        b = col.bounds;
                        initialized = true;
                    }
                    else
                    {
                        b.Encapsulate(col.bounds);
                    }
                }
            }

            if (!initialized)
            {
                Vector3 fallbackPos = followTarget ? followTarget.position : Vector3.zero;
                return new Bounds(fallbackPos, new Vector3(80f, 10f, 80f));
            }

            return b;
        }

        private void OnPreCullMapCamera(Camera cam)
        {
            if (cam != mapCamera) return;

            // 1. Disabilita temporaneamente i pavimenti per evidenziare solo i muri perimetrali
            tempDisabledFloorRenderers.Clear();
            Renderer[] allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            foreach (Renderer r in allRenderers)
            {
                if (r == null || !r.enabled) continue;
                string n = r.gameObject.name.ToLower();
                string pName = r.transform.parent != null ? r.transform.parent.name.ToLower() : "";

                bool isFloor = n.Contains("pavimento") || n.Contains("floor") || n.Contains("ground") ||
                               n.Contains("terrain") || n.Contains("plane") || n.Contains("piastrella") ||
                               n.Contains("tile") || pName.Contains("pavimento") || pName.Contains("floor");

                if (!isFloor && r.bounds.size.y < 0.35f && (r.bounds.size.x > 3f || r.bounds.size.z > 3f))
                {
                    isFloor = true;
                }

                if (isFloor)
                {
                    r.enabled = false;
                    tempDisabledFloorRenderers.Add(r);
                }
            }

            // 2. Disabilita temporaneamente tutte le luci della scena per eliminare aloni/cerchi luminosi
            tempDisabledLights.Clear();
            Light[] allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light l in allLights)
            {
                if (l != null && l.enabled)
                {
                    l.enabled = false;
                    tempDisabledLights.Add(l);
                }
            }
        }

        private void OnPostRenderMapCamera(Camera cam)
        {
            if (cam != mapCamera) return;
            RestoreFloorRenderers();
            RestoreLights();
        }

        private void RestoreFloorRenderers()
        {
            for (int i = 0; i < tempDisabledFloorRenderers.Count; i++)
            {
                if (tempDisabledFloorRenderers[i] != null)
                    tempDisabledFloorRenderers[i].enabled = true;
            }
            tempDisabledFloorRenderers.Clear();
        }

        private void RestoreLights()
        {
            for (int i = 0; i < tempDisabledLights.Count; i++)
            {
                if (tempDisabledLights[i] != null)
                    tempDisabledLights[i].enabled = true;
            }
            tempDisabledLights.Clear();
        }

        private void CreateCamera()
        {
            if (renderTexture && renderTexture.width == textureSize && renderTexture.height == textureSize)
                return;

            if (renderTexture)
                renderTexture.Release();

            renderTexture = new RenderTexture(textureSize, textureSize, 16, RenderTextureFormat.ARGB32)
            {
                name = "Scene Top Down Map Texture"
            };
            renderTexture.Create();

            if (!mapCamera)
            {
                GameObject cameraObject = new GameObject("Scene Top Down Map Camera");
                cameraObject.hideFlags = HideFlags.DontSave;
                mapCamera = cameraObject.AddComponent<Camera>();
                mapCamera.transform.SetParent(transform, false);
            }

            mapCamera.clearFlags = CameraClearFlags.SolidColor;
            mapCamera.backgroundColor = Color.black;
            mapCamera.orthographic = true;
            mapCamera.nearClipPlane = 0.1f;
            mapCamera.farClipPlane = cameraHeight + 350f;
            mapCamera.depth = -100f;
            mapCamera.targetTexture = renderTexture;
            mapCamera.enabled = isActiveAndEnabled;
            mapCamera.cullingMask = cullingMask;

            if (rawImage != null)
                rawImage.texture = renderTexture;
        }

        private void CreateOverlayUI()
        {
            if (overlayContainer != null) return;

            GameObject overlayObj = new GameObject("TacticalOverlayContainer", typeof(RectTransform));
            overlayObj.transform.SetParent(transform, false);
            overlayContainer = overlayObj.GetComponent<RectTransform>();
            overlayContainer.anchorMin = Vector2.zero;
            overlayContainer.anchorMax = Vector2.one;
            overlayContainer.offsetMin = Vector2.zero;
            overlayContainer.offsetMax = Vector2.zero;

            // Marker Player
            CreatePlayerMarker();

            // Legend / Header Tattico
            CreateTacticalLegend();
        }

        private void CreatePlayerMarker()
        {
            playerMarkerObj = new GameObject("PlayerMarker", typeof(RectTransform));
            playerMarkerObj.transform.SetParent(overlayContainer, false);
            playerMarkerRect = playerMarkerObj.GetComponent<RectTransform>();
            playerMarkerRect.sizeDelta = new Vector2(44, 44);

            // Alone Ciano
            GameObject haloObj = new GameObject("Halo", typeof(RectTransform), typeof(Image));
            haloObj.transform.SetParent(playerMarkerObj.transform, false);
            RectTransform rtHalo = haloObj.GetComponent<RectTransform>();
            rtHalo.anchorMin = Vector2.zero;
            rtHalo.anchorMax = Vector2.one;
            rtHalo.offsetMin = new Vector2(-12, -12);
            rtHalo.offsetMax = new Vector2(12, 12);
            Image imgHalo = haloObj.GetComponent<Image>();
            imgHalo.sprite = GetGlowCircleSprite();
            imgHalo.color = new Color(0.0f, 0.95f, 1f, 0.55f);
            imgHalo.raycastTarget = false;

            // Freccia Direzionale Ciano Ingrandita
            GameObject arrowObj = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
            arrowObj.transform.SetParent(playerMarkerObj.transform, false);
            RectTransform rtArrow = arrowObj.GetComponent<RectTransform>();
            rtArrow.sizeDelta = new Vector2(26, 26);
            rtArrow.anchoredPosition = Vector2.zero;
            playerArrowImg = arrowObj.GetComponent<Image>();
            playerArrowImg.sprite = GetArrowSprite();
            playerArrowImg.color = Color.white;
            playerArrowImg.raycastTarget = false;
        }

        private void CreateTacticalLegend()
        {
            GameObject headerObj = new GameObject("TacticalLegendBar", typeof(RectTransform), typeof(Image));
            headerObj.transform.SetParent(overlayContainer, false);
            hudHeader = headerObj.GetComponent<RectTransform>();
            hudHeader.anchorMin = new Vector2(0f, 1f);
            hudHeader.anchorMax = new Vector2(1f, 1f);
            hudHeader.pivot = new Vector2(0.5f, 1f);
            hudHeader.sizeDelta = new Vector2(0f, 36f);
            hudHeader.anchoredPosition = new Vector2(0f, -4f);

            Image imgBg = headerObj.GetComponent<Image>();
            imgBg.color = new Color(0.015f, 0.05f, 0.035f, 0.92f);
            imgBg.raycastTarget = false;

            GameObject txtObj = new GameObject("LegendText", typeof(RectTransform), typeof(Text));
            txtObj.transform.SetParent(headerObj.transform, false);
            RectTransform rtTxt = txtObj.GetComponent<RectTransform>();
            rtTxt.anchorMin = Vector2.zero;
            rtTxt.anchorMax = Vector2.one;
            rtTxt.offsetMin = new Vector2(8, 0);
            rtTxt.offsetMax = new Vector2(-8, 0);

            txtLegend = txtObj.GetComponent<Text>();
            txtLegend.font = terminalFont;
            txtLegend.fontSize = 15;
            txtLegend.fontStyle = FontStyle.Bold;
            txtLegend.alignment = TextAnchor.MiddleCenter;
            txtLegend.color = new Color(0.85f, 1f, 0.9f);
            txtLegend.text = "◆ MAPPA TATTICA: [━ VERDE: MURI]  [● GIALLO: INTERAZIONI]  [▲ ROSSO: NEMICI]  [▲ CIANO: PLAYER] ◆";
            txtLegend.raycastTarget = false;
        }

        #region Scansione Entità

        private void ScanSceneEntities()
        {
            ResolveFollowTarget();

            // 1. Scansione Oggetti Interagibili
            cachedInteractables.Clear();
            cachedInteractableNames.Clear();

            MonoBehaviour[] allScripts = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            HashSet<Transform> processedRoots = new HashSet<Transform>();

            foreach (MonoBehaviour mb in allScripts)
            {
                if (mb == null || mb == this) continue;

                Transform t = mb.transform;
                if (processedRoots.Contains(t)) continue;

                // Escludi il player, la camera e script di sistema/UI
                if (mb.CompareTag("Player") || t == playerTransform || mb is Camera || mb is Canvas || mb is GraphicRaycaster)
                    continue;

                // Verifica se è un vero oggetto interagibile di gioco
                bool isInteractable = false;
                string nomeTattico = "";

                if (mb is TerminalePorta term)
                {
                    isInteractable = true;
                    nomeTattico = string.IsNullOrEmpty(term.nomeTerminale) ? "TERMINALE PORTA" : term.nomeTerminale;
                }
                else if (mb is DatapadCodiciPorte datapad)
                {
                    isInteractable = true;
                    nomeTattico = string.IsNullOrEmpty(datapad.titoloDatapad) ? "DATAPAD SICUREZZA" : datapad.titoloDatapad;
                }
                else if (mb is PortaSettore porta)
                {
                    isInteractable = true;
                    nomeTattico = "PORTA " + porta.gameObject.name.Replace("(Clone)", "").Trim();
                }
                else if (mb is IInteractable)
                {
                    string rawName = t.gameObject.name.ToLower();
                    // Ignora collider o mesh generici che non hanno un nome interattivo valido
                    if (!rawName.StartsWith("plane") && !rawName.StartsWith("cube") && !rawName.StartsWith("root") && !rawName.StartsWith("cylinder"))
                    {
                        isInteractable = true;
                        nomeTattico = t.gameObject.name.Replace("(Clone)", "").Replace("_", " ").Trim();
                    }
                }
                else if (t.gameObject.layer == LayerMask.NameToLayer("Interactable") || t.CompareTag("Interactable"))
                {
                    string rawName = t.gameObject.name.ToLower();
                    if (!rawName.StartsWith("plane") && !rawName.StartsWith("cube") && !rawName.StartsWith("root"))
                    {
                        isInteractable = true;
                        nomeTattico = t.gameObject.name.Replace("(Clone)", "").Replace("_", " ").Trim();
                    }
                }

                if (isInteractable)
                {
                    processedRoots.Add(t);
                    cachedInteractables.Add(t);
                    if (nomeTattico.Length > 22) nomeTattico = nomeTattico.Substring(0, 22);
                    cachedInteractableNames.Add(nomeTattico.ToUpper());
                }
            }

            // 2. Scansione Nemici
            cachedEnemies.Clear();
            HashSet<Transform> processedEnemies = new HashSet<Transform>();

            foreach (MonoBehaviour mb in allScripts)
            {
                if (mb == null) continue;

                string typeName = mb.GetType().Name.ToLower();
                Transform t = mb.transform;
                if (processedEnemies.Contains(t)) continue;

                bool isEnemy = typeName.Contains("guardia") ||
                               typeName.Contains("drone") ||
                               typeName.Contains("bot") ||
                               typeName.Contains("nemico") ||
                               typeName.Contains("npc") ||
                               t.CompareTag("Enemy") ||
                               t.gameObject.layer == LayerMask.NameToLayer("Enemy");

                if (isEnemy && t != playerTransform && !processedRoots.Contains(t))
                {
                    processedEnemies.Add(t);
                    cachedEnemies.Add(t);
                }
            }
        }

        #endregion

        #region Aggiornamento Posizioni & Rendering Marker

        private void UpdateOverlayPositions()
        {
            if (overlayContainer == null || mapCamera == null) return;

            Vector2 mapSize = overlayContainer.rect.size;
            float pulse = Mathf.Sin(Time.unscaledTime * 5f) * 0.15f + 0.85f;

            // 1. Aggiorna Marker Giocatore
            if (playerTransform != null && playerMarkerObj != null)
            {
                Vector3 vp = mapCamera.WorldToViewportPoint(playerTransform.position);
                if (IsInsideViewport(vp))
                {
                    playerMarkerObj.SetActive(true);
                    playerMarkerRect.anchoredPosition = ViewportToLocalPos(vp, mapSize);
                    float rotY = playerTransform.eulerAngles.y;
                    playerMarkerRect.localRotation = Quaternion.Euler(0f, 0f, -rotY);
                }
                else
                {
                    playerMarkerObj.SetActive(false);
                }
            }

            // 2. Aggiorna Marker Interagibili (Giallo + Nome Terminale)
            SyncInteractableMarkerCount(cachedInteractables.Count);
            for (int i = 0; i < cachedInteractables.Count; i++)
            {
                Transform target = cachedInteractables[i];
                InteractableMarkerItem item = activeInteractableMarkers[i];

                if (target == null || !target.gameObject.activeInHierarchy)
                {
                    item.root.SetActive(false);
                    continue;
                }

                Vector3 vp = mapCamera.WorldToViewportPoint(target.position);
                if (IsInsideViewport(vp))
                {
                    item.root.SetActive(true);
                    item.rect.anchoredPosition = ViewportToLocalPos(vp, mapSize);

                    // Pulsazione alone giallo
                    Color haloC = interactableHaloColor;
                    haloC.a = 0.40f * pulse;
                    item.imgHalo.color = haloC;

                    // Testo nome font terminale
                    string nome = cachedInteractableNames[i];
                    item.txtLabel.text = $"[E] {nome}";
                }
                else
                {
                    item.root.SetActive(false);
                }
            }

            // 3. Aggiorna Marker Nemici (Rosso + Direzione)
            SyncEnemyMarkerCount(cachedEnemies.Count);
            for (int i = 0; i < cachedEnemies.Count; i++)
            {
                Transform enemy = cachedEnemies[i];
                EnemyMarkerItem item = activeEnemyMarkers[i];

                if (enemy == null || !enemy.gameObject.activeInHierarchy)
                {
                    item.root.SetActive(false);
                    continue;
                }

                Vector3 vp = mapCamera.WorldToViewportPoint(enemy.position);
                if (IsInsideViewport(vp))
                {
                    item.root.SetActive(true);
                    item.rect.anchoredPosition = ViewportToLocalPos(vp, mapSize);

                    // Pulsazione alone rosso
                    Color haloC = enemyHaloColor;
                    haloC.a = 0.45f * pulse;
                    item.imgHalo.color = haloC;

                    // Rotazione indicatore verso la direzione di guardia/pattuglia
                    float rotY = enemy.eulerAngles.y;
                    item.rect.localRotation = Quaternion.Euler(0f, 0f, -rotY);
                }
                else
                {
                    item.root.SetActive(false);
                }
            }
        }

        private static bool IsInsideViewport(Vector3 vp)
        {
            return vp.z > 0f && vp.x >= -0.05f && vp.x <= 1.05f && vp.y >= -0.05f && vp.y <= 1.05f;
        }

        private static Vector2 ViewportToLocalPos(Vector3 vp, Vector2 mapSize)
        {
            return new Vector2((vp.x - 0.5f) * mapSize.x, (vp.y - 0.5f) * mapSize.y);
        }

        #endregion

        #region Gestione Pool Marker

        private void SyncInteractableMarkerCount(int targetCount)
        {
            while (activeInteractableMarkers.Count < targetCount)
            {
                activeInteractableMarkers.Add(CreaInteractableMarker());
            }

            for (int i = targetCount; i < activeInteractableMarkers.Count; i++)
            {
                activeInteractableMarkers[i].root.SetActive(false);
            }
        }

        private InteractableMarkerItem CreaInteractableMarker()
        {
            GameObject root = new GameObject("InteractableMarker", typeof(RectTransform));
            root.transform.SetParent(overlayContainer, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(38, 38);

            // Alone Giallo Soft
            GameObject haloObj = new GameObject("Halo", typeof(RectTransform), typeof(Image));
            haloObj.transform.SetParent(root.transform, false);
            RectTransform rtHalo = haloObj.GetComponent<RectTransform>();
            rtHalo.anchorMin = Vector2.zero;
            rtHalo.anchorMax = Vector2.one;
            rtHalo.offsetMin = new Vector2(-12, -12);
            rtHalo.offsetMax = new Vector2(12, 12);
            Image imgHalo = haloObj.GetComponent<Image>();
            imgHalo.sprite = GetGlowCircleSprite();
            imgHalo.color = new Color(1f, 0.92f, 0.15f, 0.45f);
            imgHalo.raycastTarget = false;

            // Punto Centrale Giallo Solido (Ingrandito)
            GameObject coreObj = new GameObject("Core", typeof(RectTransform), typeof(Image));
            coreObj.transform.SetParent(root.transform, false);
            RectTransform rtCore = coreObj.GetComponent<RectTransform>();
            rtCore.sizeDelta = new Vector2(16, 16);
            rtCore.anchoredPosition = Vector2.zero;
            Image imgCore = coreObj.GetComponent<Image>();
            imgCore.sprite = GetSolidCircleSprite();
            imgCore.color = new Color(1f, 1f, 0.4f, 1f);
            imgCore.raycastTarget = false;

            // Targhetta Nome Terminale (Ingrandita con Font ad Alta Leggibilità)
            GameObject labelBox = new GameObject("LabelBox", typeof(RectTransform), typeof(Image));
            labelBox.transform.SetParent(root.transform, false);
            RectTransform rtLabelBox = labelBox.GetComponent<RectTransform>();
            rtLabelBox.pivot = new Vector2(0.5f, 0f);
            rtLabelBox.anchoredPosition = new Vector2(0, 22f);
            rtLabelBox.sizeDelta = new Vector2(200, 26);

            Image imgBox = labelBox.GetComponent<Image>();
            imgBox.color = new Color(0.01f, 0.05f, 0.03f, 0.90f);
            imgBox.raycastTarget = false;

            GameObject txtObj = new GameObject("TxtName", typeof(RectTransform), typeof(Text));
            txtObj.transform.SetParent(labelBox.transform, false);
            RectTransform rtTxt = txtObj.GetComponent<RectTransform>();
            rtTxt.anchorMin = Vector2.zero;
            rtTxt.anchorMax = Vector2.one;
            rtTxt.offsetMin = new Vector2(6, 0);
            rtTxt.offsetMax = new Vector2(-6, 0);

            Text txt = txtObj.GetComponent<Text>();
            txt.font = terminalFont;
            txt.fontSize = 15;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(1f, 0.95f, 0.3f, 1f);
            txt.raycastTarget = false;

            ContentSizeFitter csf = labelBox.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            HorizontalLayoutGroup hlg = labelBox.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 3, 3);
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            return new InteractableMarkerItem
            {
                root = root,
                rect = rect,
                imgHalo = imgHalo,
                imgCore = imgCore,
                txtLabel = txt
            };
        }

        private void SyncEnemyMarkerCount(int targetCount)
        {
            while (activeEnemyMarkers.Count < targetCount)
            {
                activeEnemyMarkers.Add(CreaEnemyMarker());
            }

            for (int i = targetCount; i < activeEnemyMarkers.Count; i++)
            {
                activeEnemyMarkers[i].root.SetActive(false);
            }
        }

        private EnemyMarkerItem CreaEnemyMarker()
        {
            GameObject root = new GameObject("EnemyMarker", typeof(RectTransform));
            root.transform.SetParent(overlayContainer, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(38, 38);

            // Alone Rosso Soft
            GameObject haloObj = new GameObject("Halo", typeof(RectTransform), typeof(Image));
            haloObj.transform.SetParent(root.transform, false);
            RectTransform rtHalo = haloObj.GetComponent<RectTransform>();
            rtHalo.anchorMin = Vector2.zero;
            rtHalo.anchorMax = Vector2.one;
            rtHalo.offsetMin = new Vector2(-10, -10);
            rtHalo.offsetMax = new Vector2(10, 10);
            Image imgHalo = haloObj.GetComponent<Image>();
            imgHalo.sprite = GetGlowCircleSprite();
            imgHalo.color = new Color(1f, 0.15f, 0.2f, 0.45f);
            imgHalo.raycastTarget = false;

            // Freccia Direzionale Rossa (Triangolo Rosso Ingrandito)
            GameObject arrowObj = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
            arrowObj.transform.SetParent(root.transform, false);
            RectTransform rtArrow = arrowObj.GetComponent<RectTransform>();
            rtArrow.sizeDelta = new Vector2(24, 24);
            rtArrow.anchoredPosition = Vector2.zero;
            Image imgArrow = arrowObj.GetComponent<Image>();
            imgArrow.sprite = GetArrowSprite();
            imgArrow.color = new Color(1f, 0.2f, 0.25f, 1f);
            imgArrow.raycastTarget = false;

            return new EnemyMarkerItem
            {
                root = root,
                rect = rect,
                imgHalo = imgHalo,
                imgArrow = imgArrow
            };
        }

        private void ClearAllMarkers()
        {
            foreach (var item in activeInteractableMarkers)
            {
                if (item.root != null) item.root.SetActive(false);
            }

            foreach (var item in activeEnemyMarkers)
            {
                if (item.root != null) item.root.SetActive(false);
            }

            if (playerMarkerObj != null)
                playerMarkerObj.SetActive(false);
        }

        #endregion

        #region Texture Procedurali (Halos & Sprites)

        private static Sprite s_GlowCircleSprite;
        private static Sprite s_SolidCircleSprite;
        private static Sprite s_ArrowSprite;

        public static Sprite GetGlowCircleSprite()
        {
            if (s_GlowCircleSprite != null) return s_GlowCircleSprite;

            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "Proc_GlowCircle";
            float center = (size - 1) * 0.5f;
            float maxR = center;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float t = Mathf.Clamp01(dist / maxR);
                    float alpha = Mathf.Pow(1f - t, 2.2f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            s_GlowCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return s_GlowCircleSprite;
        }

        public static Sprite GetSolidCircleSprite()
        {
            if (s_SolidCircleSprite != null) return s_SolidCircleSprite;

            int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "Proc_SolidCircle";
            float center = (size - 1) * 0.5f;
            float r = center - 1f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha = Mathf.Clamp01((r - dist) + 0.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            s_SolidCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return s_SolidCircleSprite;
        }

        public static Sprite GetArrowSprite()
        {
            if (s_ArrowSprite != null) return s_ArrowSprite;

            int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "Proc_Arrow";

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / (size - 1);
                    float v = (float)y / (size - 1);
                    float halfWidthAtV = (1.0f - v) * 0.45f;
                    float distFromCenter = Mathf.Abs(u - 0.5f);
                    float alpha = (distFromCenter <= halfWidthAtV && v >= 0.05f) ? 1f : 0f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            s_ArrowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return s_ArrowSprite;
        }

        #endregion

        private void ResolveFollowTarget()
        {
            if (playerTransform != null)
            {
                followTarget = playerTransform;
                return;
            }

            GameObject player = null;
            try
            {
                player = GameObject.FindGameObjectWithTag(playerTag);
            }
            catch (UnityException)
            {
                player = null;
            }

            if (!player)
            {
                muve_pg playerScript = FindFirstObjectByType<muve_pg>();
                if (playerScript != null)
                    player = playerScript.gameObject;
            }

            if (player)
            {
                playerTransform = player.transform;
                followTarget = playerTransform;
                return;
            }

            if (Camera.main)
                followTarget = Camera.main.transform;
        }

        private class InteractableMarkerItem
        {
            public GameObject root;
            public RectTransform rect;
            public Image imgHalo;
            public Image imgCore;
            public Text txtLabel;
        }

        private class EnemyMarkerItem
        {
            public GameObject root;
            public RectTransform rect;
            public Image imgHalo;
            public Image imgArrow;
        }
    }
}
