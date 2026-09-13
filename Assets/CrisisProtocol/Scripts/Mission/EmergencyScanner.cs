using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using GoldenCast.UI;

public class EmergencyScanner : MonoBehaviour
{
    [Header("Scanner di Emergenza")]
    [Tooltip("Punto fisico da cui parte lo scan. Se vuoto, usa il centro del corpo o della telecamera.")]
    [SerializeField] private Transform puntoDiOrigine;

    [Tooltip("Portata massima del raggio di scansione mirato in metri.")]
    [SerializeField] private float portataScanner = 6f;

    [Tooltip("Durata in secondi dell'evidenziazione visiva dei problemi dopo l'acquisizione della chiave.")]
    [SerializeField] private float durataEvidenziazioneProblemi = 5.0f;

    [Tooltip("Layer degli oggetti scansionabili: credenziali, terminali, focolai o anomalie.")]
    [SerializeField] private LayerMask layerScansionabile;

    [Header("Colori Evidenziazione Neon")]
    [SerializeField] private Color neonYellow = new Color(1.0f, 0.95f, 0.05f, 1.0f);
    [SerializeField] private Color neonRed = new Color(1.0f, 0.12f, 0.25f, 1.0f);
    [SerializeField] private Color neonCyan = new Color(0.1f, 0.9f, 1.0f, 1.0f);

    private Camera telecameraPrincipale;
    private AudioSource audioSource;
    private Coroutine highlightCoroutine;
    private bool isHighlightActive = false;
    private float highlightTimeRemaining = 0f;

    private readonly List<HighlightTargetInfo> activeTargetHighlights = new List<HighlightTargetInfo>();
    private readonly List<GameObject> activeVisualBeacons = new List<GameObject>();

    private struct HighlightTargetInfo
    {
        public Vector3 worldPosition;
        public string label;
        public Color color;
        public Transform targetTransform;
    }

    private void Awake()
    {
        telecameraPrincipale = Camera.main;
        InitAudioSource();
    }

    private void OnEnable()
    {
        MissionManager.OnNuovaCredenzialeRaccolta += HandleNuovaCredenzialeRaccolta;
    }

    private void OnDisable()
    {
        MissionManager.OnNuovaCredenzialeRaccolta -= HandleNuovaCredenzialeRaccolta;
        ClearVisualBeacons();
    }

    private void HandleNuovaCredenzialeRaccolta(string credentialId, int totalCount)
    {
        Debug.Log($"<color=lime>[SCANNER]</color> Chiave <b>{credentialId}</b> acquisita. Evidenziazione automatica focolaio corrispondente per {durataEvidenziazioneProblemi}s.");
        AvviaEvidenziazioneProblemi(durataEvidenziazioneProblemi, $"CHIAVE ACQUISITA: {credentialId} // FOCOLAIO EVIDENZIATO");
    }

    private void Start()
    {
        if (telecameraPrincipale == null)
            telecameraPrincipale = Camera.main;
    }

    private void Update()
    {
        if (ModalUIState.IsModalOpen)
            return;

        if (isHighlightActive)
        {
            highlightTimeRemaining = Mathf.Max(0f, highlightTimeRemaining - Time.deltaTime);
            if (highlightTimeRemaining <= 0f)
            {
                isHighlightActive = false;
                ClearVisualBeacons();
            }
        }

        EseguiScansioneInput();
    }

    private void InitAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = 0.8f;
        }
    }

    private void PlaySonarPing()
    {
        if (audioSource == null) return;
        AudioClip pingClip = CreateSonarClip();
        audioSource.PlayOneShot(pingClip);
    }

    private AudioClip CreateSonarClip()
    {
        int sampleRate = 44100;
        float duration = 0.35f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float freq = Mathf.Lerp(1200f, 600f, t / duration);
            float envelope = Mathf.Sin((1f - (t / duration)) * Mathf.PI * 0.5f);
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.35f;
        }

        AudioClip clip = AudioClip.Create("Scanner_SonarPing", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }


    private void EseguiScansioneInput()
    {
        if (Keyboard.current == null || !Keyboard.current.qKey.wasPressedThisFrame)
            return;

        if (telecameraPrincipale == null)
            telecameraPrincipale = Camera.main;

        Vector3 origineScan = puntoDiOrigine != null ? puntoDiOrigine.position : (telecameraPrincipale != null ? telecameraPrincipale.transform.position : transform.position + Vector3.up * 1.5f);
        Vector3 direzioneScan = telecameraPrincipale != null ? telecameraPrincipale.transform.forward : transform.forward;

        Debug.Log("[SCANNER] Analisi diagnostica emessa...");

        // Verifica se il giocatore possiede già una chiave / firma di sicurezza
        bool haChiave = HaChiaveAcquisita();

        if (haChiave)
        {
            // Con la chiave acquisita, la pressione di Q attiva/rinnova l'evidenziazione visiva dei problemi per 5 secondi
            AvviaEvidenziazioneProblemi(durataEvidenziazioneProblemi, "SCANSIONE ATTIVA // PROBLEMI EVIDENZIATI (5s)");
        }
        else
        {
            Debug.Log("<color=yellow>[SCANNER]</color> Chiave non ancora acquisita. Scansione mirata locale in corso...");
            if (CyberHUD.Instance != null)
            {
                CyberHUD.Instance.MostraNotificaAcquisizione("⚠️ SCANNER OPERATIVO", "ACQUISISCI LA KEYCARD PER EVIDENZIARE I PROBLEMI");
            }
        }

        // Esegue comunque anche la scansione mirata per identificare il bersaglio diretto inquadrato
        if (Physics.Raycast(origineScan, direzioneScan, out RaycastHit hitInfo, portataScanner, layerScansionabile.value != 0 ? layerScansionabile : ~0))
        {
            AnalizzaBersaglio(hitInfo.collider);
        }
        else
        {
            if (!haChiave)
            {
                Debug.Log("<color=grey>[SCANNER] VUOTO.</color> Nessun bersaglio operativo intercettato.");
            }
        }
    }

    /// <summary>
    /// Restituisce true se il giocatore ha acquisito la chiave specifica per almeno un focolaio attivo non ancora contenuto.
    /// </summary>
    public bool HaChiaveAcquisita()
    {
        if (MissionManager.Instance == null) return false;

        EmergencyHotspot[] hotspots = Object.FindObjectsByType<EmergencyHotspot>(FindObjectsSortMode.None);
        foreach (EmergencyHotspot h in hotspots)
        {
            if (h != null && !h.Contenuto && !string.IsNullOrWhiteSpace(h.RequiredCredentialId))
            {
                if (MissionManager.Instance.HaRaccoltoCredenzialeSpecifica(h.RequiredCredentialId))
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Avvia l'evidenziazione di tutti i problemi/guasti irrisolti nella scena per la durata specificata.
    /// </summary>
    public void AvviaEvidenziazioneProblemi(float durata = 5.0f, string messaggioHUD = "")
    {
        if (highlightCoroutine != null)
            StopCoroutine(highlightCoroutine);

        highlightCoroutine = StartCoroutine(HighlightRoutine(durata, messaggioHUD));
    }

    private IEnumerator HighlightRoutine(float durata, string messaggioHUD)
    {
        isHighlightActive = true;
        highlightTimeRemaining = durata;
        PlaySonarPing();

        if (CyberHUD.Instance != null && !string.IsNullOrEmpty(messaggioHUD))
        {
            CyberHUD.Instance.MostraNotificaAcquisizione("🔍 SCANSIONE DIAGNOSTICA", messaggioHUD);
        }

        RaccogliProblemiAttivi();
        GeneraBeaconsOlografici();

        while (highlightTimeRemaining > 0f)
        {
            yield return null;
        }

        isHighlightActive = false;
        ClearVisualBeacons();
    }

    private void RaccogliProblemiAttivi()
    {
        activeTargetHighlights.Clear();

        // 1. Trova SOLO gli EmergencyHotspot non contenuti per i quali il giocatore possiede SPECIFICATAMENTE la chiave richiesta
        EmergencyHotspot[] hotspots = Object.FindObjectsByType<EmergencyHotspot>(FindObjectsSortMode.None);
        foreach (EmergencyHotspot h in hotspots)
        {
            if (h != null && !h.Contenuto)
            {
                string reqKey = h.RequiredCredentialId;
                bool possiedeChiave = false;

                if (!string.IsNullOrWhiteSpace(reqKey) && MissionManager.Instance != null)
                {
                    possiedeChiave = MissionManager.Instance.HaRaccoltoCredenzialeSpecifica(reqKey);
                }

                // EVIDENZIA SOLO ED ESCLUSIVAMENTE SE LA CHIAVE DI QUESTO SPECIFICO FOCOLAIO È STATA ACQUISITA!
                if (possiedeChiave)
                {
                    activeTargetHighlights.Add(new HighlightTargetInfo
                    {
                        worldPosition = h.transform.position,
                        label = $"⚠️ FOCOLAIO // [{reqKey}]",
                        color = neonYellow,
                        targetTransform = h.transform
                    });
                }
            }
        }

        // 2. Se tutti i focolai corrispondenti sono stati risolti e tutti gli obiettivi sono completati, evidenzia la Porta di Evacuazione
        if (activeTargetHighlights.Count == 0)
        {
            bool tuttiFocolaiRisolti = true;
            foreach (EmergencyHotspot h in hotspots)
            {
                if (h != null && !h.Contenuto)
                {
                    tuttiFocolaiRisolti = false;
                    break;
                }
            }

            if (tuttiFocolaiRisolti)
            {
                PortaSettore[] porte = Object.FindObjectsByType<PortaSettore>(FindObjectsSortMode.None);
                foreach (PortaSettore p in porte)
                {
                    if (p != null)
                    {
                        activeTargetHighlights.Add(new HighlightTargetInfo
                        {
                            worldPosition = p.transform.position,
                            label = "🚪 PORTA SETTORE / EVACUAZIONE",
                            color = Color.green,
                            targetTransform = p.transform
                        });
                    }
                }
            }
        }
    }

    private void GeneraBeaconsOlografici()
    {
        ClearVisualBeacons();

        foreach (HighlightTargetInfo target in activeTargetHighlights)
        {
            if (target.targetTransform == null) continue;

            GameObject beaconGO = new GameObject($"Scanner_HoloBeacon_{target.label}");
            beaconGO.transform.position = target.worldPosition + Vector3.up * 1.2f;

            // 1. Luce volumetrica pulsante
            Light pointLight = beaconGO.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = target.color;
            pointLight.range = 8.0f;
            pointLight.intensity = 2.5f;

            // 2. Colonna / Fascio di luce olografica verticale
            GameObject columnGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            columnGO.name = "Holo_Beam";
            columnGO.transform.SetParent(beaconGO.transform, false);
            columnGO.transform.localPosition = Vector3.up * 1.5f;
            columnGO.transform.localScale = new Vector3(0.18f, 1.5f, 0.18f);

            Collider col = columnGO.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Renderer ren = columnGO.GetComponent<Renderer>();
            if (ren != null)
            {
                Material mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
                Color c = target.color;
                c.a = 0.5f;
                mat.color = c;
                ren.material = mat;
            }

            activeVisualBeacons.Add(beaconGO);
        }
    }

    private void ClearVisualBeacons()
    {
        for (int i = activeVisualBeacons.Count - 1; i >= 0; i--)
        {
            if (activeVisualBeacons[i] != null)
            {
                Destroy(activeVisualBeacons[i]);
            }
        }
        activeVisualBeacons.Clear();
    }

    private void AnalizzaBersaglio(Collider target)
    {
        if (target == null)
            return;

        AccessCredentialPickup credential = target.GetComponent<AccessCredentialPickup>() ?? target.GetComponentInParent<AccessCredentialPickup>();
        if (credential != null)
        {
            Debug.Log($"<color=cyan>[SCANNER]</color> Credenziale fisica rilevata: <b>{credential.DisplayName}</b>. Avvicinarsi e premere E per acquisirla.");
            if (CyberHUD.Instance != null)
            {
                CyberHUD.Instance.MostraNotificaAcquisizione("🔑 CREDENZIALE RILEVATA", $"{credential.DisplayName} // PREMI E PER RACCOGLIERE");
            }
            return;
        }

        EmergencyHotspot hotspot = target.GetComponent<EmergencyHotspot>() ?? target.GetComponentInParent<EmergencyHotspot>();
        if (hotspot != null)
        {
            string reqKey = hotspot.RequiredCredentialId;
            bool possiedeChiave = !string.IsNullOrWhiteSpace(reqKey) &&
                MissionManager.Instance != null &&
                MissionManager.Instance.HaRaccoltoCredenzialeSpecifica(reqKey);

            if (possiedeChiave)
            {
                string stato = hotspot.Contenuto ? "CONTENUTO / RIPARATO" : "CRITICO // GUASTO ATTIVO";
                Debug.Log($"<color=yellow>[SCANNER]</color> Focolaio analizzato: <b>{hotspot.name}</b> [{stato}] - Chiave [{reqKey}] abilitata.");
                if (CyberHUD.Instance != null)
                {
                    CyberHUD.Instance.MostraNotificaAcquisizione("⚠️ FOCOLAIO ANALIZZATO", $"{hotspot.name.ToUpper()} // {stato}");
                }
            }
            else
            {
                Debug.Log($"<color=red>[SCANNER]</color> Focolaio non visibile/accessibile: chiave <b>{reqKey}</b> non ancora posseduta.");
                if (CyberHUD.Instance != null)
                {
                    CyberHUD.Instance.MostraNotificaAcquisizione("🔒 CHIAVE NON POSSEDUTA", $"TROVA PRIMA LA CHIAVE: {reqKey}");
                }
            }
            return;
        }

        OstacoloCausale obstacle = target.GetComponent<OstacoloCausale>() ?? target.GetComponentInParent<OstacoloCausale>();
        if (obstacle != null)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.RegisterSecuritySignature(obstacle.idCausale);

            Debug.Log($"<color=cyan>[SCANNER]</color> Firma di sicurezza acquisita da anomalia: <b>{obstacle.idCausale}</b>.");
            if (CyberHUD.Instance != null)
            {
                CyberHUD.Instance.MostraNotificaAcquisizione("⚡ FIRMA ACQUISITA", $"ANOMALIA {obstacle.idCausale} ARCHIVIATA");
            }
            return;
        }

        IInteractable interactable = target.GetComponent<IInteractable>() ?? target.GetComponentInParent<IInteractable>();
        if (interactable != null)
        {
            Debug.Log($"<color=cyan>[SCANNER]</color> Oggetto operativo analizzabile rilevato: <b>{target.name}</b>.");
            return;
        }

        Debug.LogWarning($"[SCANNER] {target.name} colpito, ma nessun protocollo di emergenza riconosciuto.");
    }

    private void OnDrawGizmos()
    {
        Vector3 origineGizmo = puntoDiOrigine != null ? puntoDiOrigine.position : (Camera.main != null ? Camera.main.transform.position : transform.position + Vector3.up * 1.5f);
        Vector3 direzioneGizmo = Application.isPlaying && Camera.main != null ? Camera.main.transform.forward : transform.forward;

        Gizmos.color = isHighlightActive ? Color.yellow : Color.cyan;
        Gizmos.DrawRay(origineGizmo, direzioneGizmo * portataScanner);
    }

    private void OnGUI()
    {
        // 1. Mirino centrale (puntino ciano/giallo a seconda dello stato)
        float size = isHighlightActive ? 6f : 4f;
        float x = (Screen.width / 2f) - (size / 2f);
        float y = (Screen.height / 2f) - (size / 2f);

        GUI.color = isHighlightActive ? neonYellow : new Color(0f, 1f, 1f, 0.8f);
        GUI.DrawTexture(new Rect(x, y, size, size), Texture2D.whiteTexture);

        // 2. Indicatori Olografici dei Problemi sullo Schermo (HUD Markers) durante i 5 secondi
        if (isHighlightActive && telecameraPrincipale != null)
        {
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            for (int i = 0; i < activeTargetHighlights.Count; i++)
            {
                HighlightTargetInfo target = activeTargetHighlights[i];
                if (target.targetTransform == null) continue;

                Vector3 targetWorldPos = target.targetTransform.position + Vector3.up * 1.0f;
                Vector3 screenPos = telecameraPrincipale.WorldToScreenPoint(targetWorldPos);

                // Se l'oggetto è davanti alla telecamera
                if (screenPos.z > 0f)
                {
                    float distance = Vector3.Distance(telecameraPrincipale.transform.position, targetWorldPos);
                    float guiY = Screen.height - screenPos.y;

                    // Badge di evidenziazione
                    GUI.color = new Color(0.02f, 0.05f, 0.08f, 0.85f);
                    GUI.DrawTexture(new Rect(screenPos.x - 120f, guiY - 24f, 240f, 28f), Texture2D.whiteTexture);

                    // Contorno
                    GUI.color = target.color;
                    labelStyle.normal.textColor = target.color;
                    GUI.Label(new Rect(screenPos.x - 120f, guiY - 24f, 240f, 28f), $"{target.label} [{Mathf.RoundToInt(distance)}m]", labelStyle);
                }
            }
        }
    }
}
