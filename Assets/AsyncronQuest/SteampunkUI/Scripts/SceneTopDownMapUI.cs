using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

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
        [SerializeField] private bool centerOnLevel = false;

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
        private TextMeshProUGUI txtLegend;

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

        private float nextScanTime = 0f;
        private float currentZoom = 65f;
        private Vector3 calculatedLevelCenter = Vector3.zero;
        private Bounds calculatedLevelBounds;
        private bool levelBoundsCalculated = false;

        private void Awake()
        {
            rawImage = GetComponent<RawImage>();
            currentZoom = orthographicSize;

            SetupTacticalMaterial();
            CreateCamera();
            CreateOverlayUI();
            ResolveFollowTarget();
        }

        private void OnEnable()
        {
            if (!rawImage)
                rawImage = GetComponent<RawImage>();

            SetupTacticalMaterial();
            CreateCamera();

            if (mapCamera)
                mapCamera.enabled = true;

            if (rawImage != null && renderTexture != null)
                rawImage.texture = renderTexture;

            RecalculateLevelBoundsAndFraming();
            ScanSceneEntities();
            UpdateOverlayPositions();
        }

        private void OnDisable()
        {
            if (mapCamera)
                mapCamera.enabled = false;

            ClearAllMarkers();
        }

        private void OnDestroy()
        {
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

            // Scansione periodica entità ogni 1.2s
            if (Time.unscaledTime >= nextScanTime)
            {
                ScanSceneEntities();
                nextScanTime = Time.unscaledTime + 1.2f;
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
                tacticalNeonMaterial.SetFloat("_EdgeThreshold", 0.08f);
                tacticalNeonMaterial.SetFloat("_EdgeGlow", 2.6f);
                tacticalNeonMaterial.SetFloat("_BlueprintStrength", 0.35f);

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
                // Inquadra il livello con margine confortevole all'interno della cornice del monitor
                float optimalZoom = Mathf.Clamp(maxExtent * 0.55f, 35f, 85f);
                orthographicSize = optimalZoom;
                currentZoom = optimalZoom;
            }
        }

        private Bounds CalculateLevelBounds()
        {
            Bounds b = new Bounds(Vector3.zero, Vector3.zero);
            bool initialized = false;
            Vector3 playerPos = followTarget ? followTarget.position : Vector3.zero;

            Collider[] colliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);
            foreach (Collider col in colliders)
            {
                if (col == null || col.isTrigger) continue;
                GameObject go = col.gameObject;
                if (!go.activeInHierarchy) continue;

                if (go.CompareTag("Player") || go.CompareTag("Enemy") || go.GetComponent<muve_pg>() != null || go.GetComponent<Camera>() != null)
                    continue;

                // Escludi oggetti troppo distanti dal player (es. collider di sfondo o skybox)
                if (followTarget != null && Vector3.Distance(col.bounds.center, playerPos) > 300f)
                    continue;

                string n = go.name.ToLower();
                if (n.Contains("pavimento") || n.Contains("floor") || n.Contains("terrain") || n.Contains("ground") || n.Contains("plane"))
                    continue;

                Vector3 size = col.bounds.size;
                if (size.y >= 0.6f || n.Contains("muro") || n.Contains("wall") || n.Contains("porte") || n.Contains("door") || n.Contains("porta") || n.Contains("cube") || n.Contains("panel") || n.Contains("box") || n.Contains("pipe") || n.Contains("container"))
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
                return new Bounds(playerPos, new Vector3(60f, 10f, 60f));
            }

            return b;
        }

        private void CreateCamera()
        {
            if (renderTexture == null || renderTexture.width != textureSize || renderTexture.height != textureSize)
            {
                if (renderTexture != null)
                    renderTexture.Release();

                renderTexture = new RenderTexture(textureSize, textureSize, 16, RenderTextureFormat.ARGB32)
                {
                    name = "Scene Top Down Map Texture",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                renderTexture.Create();
            }

            if (!mapCamera)
            {
                GameObject cameraObject = new GameObject("Scene Top Down Map Camera");
                cameraObject.hideFlags = HideFlags.DontSave;
                mapCamera = cameraObject.AddComponent<Camera>();
            }

            mapCamera.clearFlags = CameraClearFlags.SolidColor;
            mapCamera.backgroundColor = cameraClearColor;
            mapCamera.orthographic = true;
            mapCamera.nearClipPlane = 0.1f;
            mapCamera.farClipPlane = cameraHeight + 400f;
            mapCamera.depth = -100f;
            mapCamera.targetTexture = renderTexture;
            mapCamera.cullingMask = cullingMask;
            mapCamera.enabled = isActiveAndEnabled;

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
            hudHeader.anchorMin = new Vector2(0.5f, 1f);
            hudHeader.anchorMax = new Vector2(0.5f, 1f);
            hudHeader.pivot = new Vector2(0.5f, 1f);
            hudHeader.sizeDelta = new Vector2(920f, 38f);
            hudHeader.anchoredPosition = new Vector2(0f, -14f);

            Image imgBg = headerObj.GetComponent<Image>();
            imgBg.color = new Color(0.015f, 0.04f, 0.03f, 0.98f); // Sfondo scuro per massimo contrasto
            imgBg.raycastTarget = false;

            // Sottile linea inferiore d'accento neon verde (spessore 2px)
            GameObject bottomLine = new GameObject("BottomLine", typeof(RectTransform), typeof(Image));
            bottomLine.transform.SetParent(headerObj.transform, false);
            RectTransform rtLine = bottomLine.GetComponent<RectTransform>();
            rtLine.anchorMin = new Vector2(0f, 0f);
            rtLine.anchorMax = new Vector2(1f, 0f);
            rtLine.pivot = new Vector2(0.5f, 0f);
            rtLine.sizeDelta = new Vector2(0f, 2f);
            rtLine.anchoredPosition = Vector2.zero;
            Image imgLine = bottomLine.GetComponent<Image>();
            imgLine.color = new Color(0.0f, 1.0f, 0.5f, 0.85f);
            imgLine.raycastTarget = false;

            GameObject txtObj = new GameObject("LegendText", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(headerObj.transform, false);
            RectTransform rtTxt = txtObj.GetComponent<RectTransform>();
            rtTxt.anchorMin = Vector2.zero;
            rtTxt.anchorMax = Vector2.one;
            rtTxt.offsetMin = new Vector2(14, 2);
            rtTxt.offsetMax = new Vector2(-14, 0);

            txtLegend = txtObj.GetComponent<TextMeshProUGUI>();
            txtLegend.color = Color.white;
            txtLegend.fontSize = 15f;
            txtLegend.fontStyle = FontStyles.Bold;
            txtLegend.alignment = TextAlignmentOptions.Center;
            txtLegend.enableWordWrapping = false;
            txtLegend.text = "<b><color=#00F0FF>[ MAPPA TATTICA ]</color></b>   <color=#447755>|</color>   <color=#00FFA0>-- MURI</color>   <color=#447755>|</color>   <color=#FFE600>● INTERAZIONI</color>   <color=#447755>|</color>   <color=#FF3344>▲ NEMICI</color>   <color=#447755>|</color>   <color=#00F0FF>▲ GIOCATORE</color>";
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

                bool isInteractable = false;
                string nomeTattico = "";

                if (mb is AccessCredentialPickup keycard)
                {
                    isInteractable = true;
                    nomeTattico = string.IsNullOrEmpty(keycard.DisplayName) ? "SCHEDA ACCESSO" : keycard.DisplayName;
                }
                else if (mb is EmergencyHotspot hotspot)
                {
                    isInteractable = true;
                    string hName = hotspot.name.ToLower();
                    if (hName.Contains("tank") || hName.Contains("chemic"))
                        nomeTattico = "SERBATOIO CHIMICO";
                    else if (hName.Contains("generator") || hName.Contains("basic"))
                        nomeTattico = "GENERATORE AUSILIARIO";
                    else if (hName.Contains("reactor"))
                        nomeTattico = "REATTORE";
                    else
                        nomeTattico = CleanObjectName(hotspot.name);
                }
                else if (mb is PortaSettore porta)
                {
                    isInteractable = true;
                    nomeTattico = CleanDoorName(porta.gameObject.name);
                }
                else if (mb is QuarantineGate)
                {
                    isInteractable = true;
                    nomeTattico = "CANCELLO QUARANTENA";
                }
                else if (mb is TerminalePorta term)
                {
                    isInteractable = true;
                    nomeTattico = string.IsNullOrEmpty(term.nomeTerminale) ? "TERMINALE PORTA" : term.nomeTerminale;
                }
                else if (mb is DatapadCodiciPorte datapad)
                {
                    isInteractable = true;
                    nomeTattico = string.IsNullOrEmpty(datapad.titoloDatapad) ? "DATAPAD SICUREZZA" : datapad.titoloDatapad;
                }
                else if (mb is CuboNeroTeletrasporto)
                {
                    isInteractable = true;
                    nomeTattico = "TELETRASPORTO";
                }
                else if (mb is IInteractable)
                {
                    string rawLower = t.gameObject.name.ToLower();
                    if (!rawLower.Contains("light") && !rawLower.Contains("cam") && !rawLower.Contains("audio") && !rawLower.Contains("sound") && !rawLower.Contains("volume") && !rawLower.Contains("vfx"))
                    {
                        isInteractable = true;
                        nomeTattico = CleanObjectName(t.gameObject.name);
                    }
                }
                else if (t.CompareTag("Interactable") || t.gameObject.layer == LayerMask.NameToLayer("Interactable"))
                {
                    isInteractable = true;
                    nomeTattico = CleanObjectName(t.gameObject.name);
                }

                if (isInteractable)
                {
                    nomeTattico = CleanObjectName(nomeTattico);

                    // Deduplicazione per vicinanza (se c'è già un'interazione entro 2.2m, evita etichette sovrapposte)
                    bool isDuplicate = false;
                    for (int k = 0; k < cachedInteractables.Count; k++)
                    {
                        if (Vector3.Distance(t.position, cachedInteractables[k].position) < 2.2f)
                        {
                            isDuplicate = true;
                            break;
                        }
                    }

                    if (!isDuplicate)
                    {
                        processedRoots.Add(t);
                        cachedInteractables.Add(t);
                        if (nomeTattico.Length > 24) nomeTattico = nomeTattico.Substring(0, 24);
                        cachedInteractableNames.Add(nomeTattico.ToUpper());
                    }
                }
            }

            // 2. Scansione Nemici
            cachedEnemies.Clear();
            HashSet<Transform> processedEnemies = new HashSet<Transform>();

            foreach (MonoBehaviour mb in allScripts)
            {
                if (mb == null) continue;

                Transform t = mb.transform;
                if (processedEnemies.Contains(t)) continue;

                string typeName = mb.GetType().Name.ToLower();
                bool isEnemy = mb is DroneRonda || mb is GuardiaNpc || mb is ManutenzioneBot || mb is NPC ||
                               typeName.Contains("drone") || typeName.Contains("guardia") || typeName.Contains("bot") ||
                               typeName.Contains("nemico") || typeName.Contains("enemy") || typeName.Contains("npc") ||
                               t.CompareTag("Enemy") || t.gameObject.layer == LayerMask.NameToLayer("Enemy");

                if (isEnemy && t != playerTransform && !processedRoots.Contains(t))
                {
                    processedEnemies.Add(t);
                    cachedEnemies.Add(t);
                }
            }
        }

        private static string CleanDoorName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "PORTA";
            string n = raw.ToUpper().Replace("(CLONE)", "").Replace("_", " ").Trim();
            n = System.Text.RegularExpressions.Regex.Replace(n, @"(?i)(DEFAULTMATERIAL|MATERIAL|MESH|PREFAB|LOD\d*|\(\d+\))", "").Trim();
            n = System.Text.RegularExpressions.Regex.Replace(n, @"\s+", " ").Trim();
            if (n.Contains("PORTA") || n.Contains("DOOR") || n.Contains("GATE") || n.Contains("SETTORE")) return n;
            return "PORTA " + n;
        }

        private static string CleanObjectName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "INTERAGIBILE";
            string n = raw.ToUpper().Replace("(CLONE)", "").Replace("_", " ").Trim();
            n = System.Text.RegularExpressions.Regex.Replace(n, @"(?i)(DEFAULTMATERIAL|MATERIAL|MESH|PREFAB|LOD\d*|\(\d+\))", "").Trim();
            n = System.Text.RegularExpressions.Regex.Replace(n, @"\s+", " ").Trim();
            if (string.IsNullOrWhiteSpace(n)) return "INTERAGIBILE";
            return n;
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

                    // Testo nome font terminale ad alta luminosità e contrasto
                    string nome = cachedInteractableNames[i];
                    item.txtLabel.color = Color.white;
                    item.txtLabel.text = $"<color=#FFE600><b>[E]</b></color> <color=#FFFFFF><b>{nome}</b></color>";

                    // Dimensionamento dinamico badge al pixel perfetto
                    float textW = item.txtLabel.preferredWidth;
                    item.badgeRect.sizeDelta = new Vector2(textW + 24f, 30f);
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

            // 1. Alone Giallo Soft
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

            // 2. Punto Centrale Giallo Solido
            GameObject coreObj = new GameObject("Core", typeof(RectTransform), typeof(Image));
            coreObj.transform.SetParent(root.transform, false);
            RectTransform rtCore = coreObj.GetComponent<RectTransform>();
            rtCore.sizeDelta = new Vector2(16, 16);
            rtCore.anchoredPosition = Vector2.zero;
            Image imgCore = coreObj.GetComponent<Image>();
            imgCore.sprite = GetSolidCircleSprite();
            imgCore.color = new Color(1f, 1f, 0.4f, 1f);
            imgCore.raycastTarget = false;

            // 3. Badge Box Contenitore (Posizionato sopra il punto)
            GameObject badgeBox = new GameObject("BadgeBox", typeof(RectTransform));
            badgeBox.transform.SetParent(root.transform, false);
            RectTransform rtBadge = badgeBox.GetComponent<RectTransform>();
            rtBadge.pivot = new Vector2(0.5f, 0f);
            rtBadge.anchoredPosition = new Vector2(0, 22f);
            rtBadge.sizeDelta = new Vector2(140, 28);

            // 3a. Bordo neon giallo
            GameObject borderObj = new GameObject("Border", typeof(RectTransform), typeof(Image));
            borderObj.transform.SetParent(badgeBox.transform, false);
            RectTransform rtBorder = borderObj.GetComponent<RectTransform>();
            rtBorder.anchorMin = Vector2.zero;
            rtBorder.anchorMax = Vector2.one;
            rtBorder.offsetMin = new Vector2(-1.5f, -1.5f);
            rtBorder.offsetMax = new Vector2(1.5f, 1.5f);
            Image imgBorder = borderObj.GetComponent<Image>();
            imgBorder.color = new Color(1f, 0.88f, 0.2f, 1f);
            imgBorder.raycastTarget = false;

            // 3b. Sfondo solido scuro ad alto contrasto
            GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(badgeBox.transform, false);
            RectTransform rtBg = bgObj.GetComponent<RectTransform>();
            rtBg.anchorMin = Vector2.zero;
            rtBg.anchorMax = Vector2.one;
            rtBg.offsetMin = Vector2.zero;
            rtBg.offsetMax = Vector2.zero;
            Image imgBg = bgObj.GetComponent<Image>();
            imgBg.color = new Color(0.01f, 0.05f, 0.03f, 0.98f);
            imgBg.raycastTarget = false;

            // 3c. Testo TextMeshPro chiaro e definito
            GameObject txtObj = new GameObject("TxtName", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(badgeBox.transform, false);
            RectTransform rtTxt = txtObj.GetComponent<RectTransform>();
            rtTxt.anchorMin = Vector2.zero;
            rtTxt.anchorMax = Vector2.one;
            rtTxt.offsetMin = new Vector2(8, 0);
            rtTxt.offsetMax = new Vector2(-8, 0);

            TextMeshProUGUI txt = txtObj.GetComponent<TextMeshProUGUI>();
            txt.color = Color.white;
            txt.fontSize = 15f;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.enableWordWrapping = false;
            txt.raycastTarget = false;

            return new InteractableMarkerItem
            {
                root = root,
                rect = rect,
                badgeRect = rtBadge,
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
            public RectTransform badgeRect;
            public Image imgHalo;
            public Image imgCore;
            public TextMeshProUGUI txtLabel;
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
