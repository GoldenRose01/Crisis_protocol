// ============================================================================
// Crisis Protocol / Sector Containment - Missione
// File: .\Assets\CrisisProtocol\Scripts\Mission\EmergencyScanner.cs
// Responsabilita': gestisce scansione, flusso missione, anomalie operative e collegamento tra interazioni di scena e stato globale.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using System.Collections; // usa lib // riga-ok
using System.Collections.Generic; // usa lib // riga-ok
using UnityEngine; // usa lib // riga-ok
using UnityEngine.InputSystem; // usa lib // riga-ok
using CrisisProtocol.UI; // usa lib // riga-ok

/// <summary>
/// Scanner operativo del giocatore.
/// Gestisce input Q, raycast diagnostico, suono sonar, marcatori HUD e beacons
/// olografici per guidare il player verso credenziali, focolai e vie di evacuazione.
/// </summary>
// blocco: classe x roba grossa
public class EmergencyScanner : MonoBehaviour // classe qui // riga-ok
{ // apre // riga-ok
    [Header("Scanner di Emergenza")] // nota unity // riga-ok
    [Tooltip("Punto fisico da cui parte lo scan. Se vuoto, usa il centro del corpo o della telecamera.")] // nota unity // riga-ok
    [SerializeField] private Transform puntoDiOrigine; // ok qua // riga-ok

    [Tooltip("Portata massima del raggio di scansione mirato in metri.")] // nota unity // riga-ok
    [SerializeField] private float portataScanner = 6f; // setta // riga-ok

    [Tooltip("Durata in secondi dell'evidenziazione visiva dei problemi dopo l'acquisizione della chiave.")] // nota unity // riga-ok
    [SerializeField] private float durataEvidenziazioneProblemi = 5.0f; // setta // riga-ok

    [Tooltip("Layer degli oggetti scansionabili: credenziali, terminali, focolai o anomalie.")] // nota unity // riga-ok
    [SerializeField] private LayerMask layerScansionabile; // ok qua // riga-ok

    [Header("Colori Evidenziazione Neon")] // nota unity // riga-ok
    [SerializeField] private Color neonYellow = new Color(1.0f, 0.95f, 0.05f, 1.0f); // setta // riga-ok
    [SerializeField] private Color neonRed = new Color(1.0f, 0.12f, 0.25f, 1.0f); // setta // riga-ok
    [SerializeField] private Color neonCyan = new Color(0.1f, 0.9f, 1.0f, 1.0f); // setta // riga-ok

    private Camera telecameraPrincipale; // roba pub // riga-ok
    private AudioSource audioSource; // roba pub // riga-ok
    private Coroutine highlightCoroutine; // roba pub // riga-ok
    private bool isHighlightActive = false; // roba pub // riga-ok
    private float highlightTimeRemaining = 0f; // roba pub // riga-ok

    private readonly List<HighlightTargetInfo> activeTargetHighlights = new List<HighlightTargetInfo>(); // roba pub // riga-ok
    private readonly List<GameObject> activeVisualBeacons = new List<GameObject>(); // roba pub // riga-ok

    private struct HighlightTargetInfo // roba pub // riga-ok
    { // apre // riga-ok
        public Vector3 worldPosition; // roba pub // riga-ok
        public string label; // roba pub // riga-ok
        public Color color; // roba pub // riga-ok
        public Transform targetTransform; // roba pub // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void Awake() // roba pub // riga-ok
    { // apre // riga-ok
        telecameraPrincipale = Camera.main; // setta // riga-ok
        InitAudioSource(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnEnable() // roba pub // riga-ok
    { // apre // riga-ok
        MissionManager.OnNuovaCredenzialeRaccolta += HandleNuovaCredenzialeRaccolta; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnDisable() // roba pub // riga-ok
    { // apre // riga-ok
        MissionManager.OnNuovaCredenzialeRaccolta -= HandleNuovaCredenzialeRaccolta; // setta // riga-ok
        ClearVisualBeacons(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void HandleNuovaCredenzialeRaccolta(string credentialId, int totalCount) // roba pub // riga-ok
    { // apre // riga-ok
        // Feedback immediato dopo raccolta: il giocatore capisce che la keycard
        // appena presa ha sbloccato informazioni utili sugli hotspot collegati.
        Debug.Log($"<color=lime>[SCANNER]</color> Chiave <b>{credentialId}</b> acquisita. Evidenziazione automatica focolaio corrispondente per {durataEvidenziazioneProblemi}s."); // logga // riga-ok
        AvviaEvidenziazioneProblemi(durataEvidenziazioneProblemi, $"CHIAVE ACQUISITA: {credentialId} // FOCOLAIO EVIDENZIATO"); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void Start() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (telecameraPrincipale == null) // se ok // riga-ok
            telecameraPrincipale = Camera.main; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void Update() // roba pub // riga-ok
    { // apre // riga-ok
        // Non leggiamo input gameplay mentre una UI modale e' aperta: evita scansioni
        // involontarie durante menu, pausa o schermate overlay.
        // blocco: controlla se va
        if (ModalUIState.IsModalOpen) // se ok // riga-ok
            return; // torna val // riga-ok

        // blocco: controlla se va
        if (isHighlightActive) // se ok // riga-ok
        { // apre // riga-ok
            highlightTimeRemaining = Mathf.Max(0f, highlightTimeRemaining - Time.deltaTime); // setta // riga-ok
            // blocco: controlla se va
            if (highlightTimeRemaining <= 0f) // se ok // riga-ok
            { // apre // riga-ok
                isHighlightActive = false; // setta // riga-ok
                ClearVisualBeacons(); // chiama // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        EseguiScansioneInput(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void InitAudioSource() // roba pub // riga-ok
    { // apre // riga-ok
        audioSource = GetComponent<AudioSource>(); // setta // riga-ok
        // blocco: controlla se va
        if (audioSource == null) // se ok // riga-ok
        { // apre // riga-ok
            audioSource = gameObject.AddComponent<AudioSource>(); // setta // riga-ok
            audioSource.playOnAwake = false; // setta // riga-ok
            audioSource.spatialBlend = 0f; // setta // riga-ok
            audioSource.volume = 0.8f; // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void PlaySonarPing() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (audioSource == null) return; // se ok // riga-ok
        AudioClip pingClip = CreateSonarClip(); // setta // riga-ok
        audioSource.PlayOneShot(pingClip); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private AudioClip CreateSonarClip() // roba pub // riga-ok
    { // apre // riga-ok
        // Generazione sintetica del ping: evita dipendenze da asset audio esterni
        // e permette allo scanner di funzionare anche in scene di test minimal.
        int sampleRate = 44100; // setta // riga-ok
        float duration = 0.35f; // setta // riga-ok
        int sampleCount = (int)(sampleRate * duration); // setta // riga-ok
        float[] samples = new float[sampleCount]; // setta // riga-ok

        // blocco: gira piu volte
        for (int i = 0; i < sampleCount; i++) // ciclo x // riga-ok
        { // apre // riga-ok
            float t = (float)i / sampleRate; // setta // riga-ok
            float freq = Mathf.Lerp(1200f, 600f, t / duration); // setta // riga-ok
            float envelope = Mathf.Sin((1f - (t / duration)) * Mathf.PI * 0.5f); // setta // riga-ok
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.35f; // setta // riga-ok
        } // chiude // riga-ok

        AudioClip clip = AudioClip.Create("Scanner_SonarPing", sampleCount, 1, sampleRate, false); // setta // riga-ok
        clip.SetData(samples, 0); // chiama // riga-ok
        return clip; // torna val // riga-ok
    } // chiude // riga-ok


    // blocco: funzione fa cose
    private void EseguiScansioneInput() // roba pub // riga-ok
    { // apre // riga-ok
        // La pressione di Q ha due comportamenti: se esiste una chiave utile,
        // rinnova l'evidenziazione globale; in ogni caso analizza il bersaglio in mira.
        // blocco: controlla se va
        if (Keyboard.current == null || !Keyboard.current.qKey.wasPressedThisFrame) // se ok // riga-ok
            return; // torna val // riga-ok

        // blocco: controlla se va
        if (telecameraPrincipale == null) // se ok // riga-ok
            telecameraPrincipale = Camera.main; // setta // riga-ok

        Vector3 origineScan = puntoDiOrigine != null ? puntoDiOrigine.position : (telecameraPrincipale != null ? telecameraPrincipale.transform.position : transform.position + Vector3.up * 1.5f); // setta // riga-ok
        Vector3 direzioneScan = telecameraPrincipale != null ? telecameraPrincipale.transform.forward : transform.forward; // setta // riga-ok

        Debug.Log("[SCANNER] Analisi diagnostica emessa..."); // logga // riga-ok

        // Verifica se il giocatore possiede già una chiave / firma di sicurezza
        bool haChiave = HaChiaveAcquisita(); // setta // riga-ok

        // blocco: controlla se va
        if (haChiave) // se ok // riga-ok
        { // apre // riga-ok
            // Con la chiave acquisita, la pressione di Q attiva/rinnova l'evidenziazione visiva dei problemi per 5 secondi
            AvviaEvidenziazioneProblemi(durataEvidenziazioneProblemi, "SCANSIONE ATTIVA // PROBLEMI EVIDENZIATI (5s)"); // chiama // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            Debug.Log("<color=yellow>[SCANNER]</color> Chiave non ancora acquisita. Scansione mirata locale in corso..."); // logga // riga-ok
            // blocco: controlla se va
            if (CyberHUD.Instance != null) // se ok // riga-ok
            { // apre // riga-ok
                CyberHUD.Instance.MostraNotificaAcquisizione("⚠️ SCANNER OPERATIVO", "ACQUISISCI LA KEYCARD PER EVIDENZIARE I PROBLEMI"); // chiama // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // Esegue comunque anche la scansione mirata per identificare il bersaglio diretto inquadrato
        // blocco: controlla se va
        if (Physics.Raycast(origineScan, direzioneScan, out RaycastHit hitInfo, portataScanner, layerScansionabile.value != 0 ? layerScansionabile : ~0)) // se ok // riga-ok
        { // apre // riga-ok
            AnalizzaBersaglio(hitInfo.collider); // chiama // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (!haChiave) // se ok // riga-ok
            { // apre // riga-ok
                Debug.Log("<color=grey>[SCANNER] VUOTO.</color> Nessun bersaglio operativo intercettato."); // logga // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    /// <summary>
    /// Restituisce true se il giocatore ha acquisito la chiave specifica per almeno un focolaio attivo non ancora contenuto.
    /// </summary>
    // blocco: funzione fa cose
    public bool HaChiaveAcquisita() // roba pub // riga-ok
    { // apre // riga-ok
        // Cerca almeno un hotspot attivo per cui il giocatore possiede gia'
        // la credenziale richiesta. Questo evita di mostrare soluzioni premature.
        // blocco: controlla se va
        if (MissionManager.Instance == null) return false; // se ok // riga-ok

        EmergencyHotspot[] hotspots = Object.FindObjectsByType<EmergencyHotspot>(FindObjectsSortMode.None); // setta // riga-ok
        // blocco: gira piu volte
        foreach (EmergencyHotspot h in hotspots) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (h != null && !h.Contenuto && !string.IsNullOrWhiteSpace(h.RequiredCredentialId)) // se ok // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (MissionManager.Instance.HaRaccoltoCredenzialeSpecifica(h.RequiredCredentialId)) // se ok // riga-ok
                    return true; // torna val // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
        return false; // torna val // riga-ok
    } // chiude // riga-ok

    /// <summary>
    /// Avvia l'evidenziazione di tutti i problemi/guasti irrisolti nella scena per la durata specificata.
    /// </summary>
    // blocco: funzione fa cose
    public void AvviaEvidenziazioneProblemi(float durata = 5.0f, string messaggioHUD = "") // roba pub // riga-ok
    { // apre // riga-ok
        // Riavviare la coroutine consente di estendere la finestra di evidenziazione
        // se il player preme Q piu' volte o raccoglie una nuova credenziale.
        // blocco: controlla se va
        if (highlightCoroutine != null) // se ok // riga-ok
            StopCoroutine(highlightCoroutine); // corutina // riga-ok

        highlightCoroutine = StartCoroutine(HighlightRoutine(durata, messaggioHUD)); // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private IEnumerator HighlightRoutine(float durata, string messaggioHUD) // roba pub // riga-ok
    { // apre // riga-ok
        isHighlightActive = true; // setta // riga-ok
        highlightTimeRemaining = durata; // setta // riga-ok
        PlaySonarPing(); // chiama // riga-ok

        // blocco: controlla se va
        if (CyberHUD.Instance != null && !string.IsNullOrEmpty(messaggioHUD)) // se ok // riga-ok
        { // apre // riga-ok
            CyberHUD.Instance.MostraNotificaAcquisizione("🔍 SCANSIONE DIAGNOSTICA", messaggioHUD); // chiama // riga-ok
        } // chiude // riga-ok

        RaccogliProblemiAttivi(); // chiama // riga-ok
        GeneraBeaconsOlografici(); // chiama // riga-ok

        // blocco: gira piu volte
        while (highlightTimeRemaining > 0f) // ciclo x // riga-ok
        { // apre // riga-ok
            yield return null; // aspetta // riga-ok
        } // chiude // riga-ok

        isHighlightActive = false; // setta // riga-ok
        ClearVisualBeacons(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void RaccogliProblemiAttivi() // roba pub // riga-ok
    { // apre // riga-ok
        // Popola la lista temporanea di bersagli visibili: solo focolai non contenuti
        // con keycard posseduta, oppure la porta di evacuazione se tutto e' risolto.
        activeTargetHighlights.Clear(); // chiama // riga-ok

        // 1. Trova SOLO gli EmergencyHotspot non contenuti per i quali il giocatore possiede SPECIFICATAMENTE la chiave richiesta
        EmergencyHotspot[] hotspots = Object.FindObjectsByType<EmergencyHotspot>(FindObjectsSortMode.None); // setta // riga-ok
        // blocco: gira piu volte
        foreach (EmergencyHotspot h in hotspots) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (h != null && !h.Contenuto) // se ok // riga-ok
            { // apre // riga-ok
                string reqKey = h.RequiredCredentialId; // setta // riga-ok
                bool possiedeChiave = false; // setta // riga-ok

                // blocco: controlla se va
                if (!string.IsNullOrWhiteSpace(reqKey) && MissionManager.Instance != null) // se ok // riga-ok
                { // apre // riga-ok
                    possiedeChiave = MissionManager.Instance.HaRaccoltoCredenzialeSpecifica(reqKey); // setta // riga-ok
                } // chiude // riga-ok

                // EVIDENZIA SOLO ED ESCLUSIVAMENTE SE LA CHIAVE DI QUESTO SPECIFICO FOCOLAIO È STATA ACQUISITA!
                // blocco: controlla se va
                if (possiedeChiave) // se ok // riga-ok
                { // apre // riga-ok
                    activeTargetHighlights.Add(new HighlightTargetInfo // ok qua // riga-ok
                    { // apre // riga-ok
                        worldPosition = h.transform.position, // setta // riga-ok
                        label = $"⚠️ FOCOLAIO // [{reqKey}]", // setta // riga-ok
                        color = neonYellow, // setta // riga-ok
                        targetTransform = h.transform // setta // riga-ok
                    }); // chiama // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // 2. Se tutti i focolai corrispondenti sono stati risolti e tutti gli obiettivi sono completati, evidenzia la Porta di Evacuazione
        // blocco: controlla se va
        if (activeTargetHighlights.Count == 0) // se ok // riga-ok
        { // apre // riga-ok
            bool tuttiFocolaiRisolti = true; // setta // riga-ok
            // blocco: gira piu volte
            foreach (EmergencyHotspot h in hotspots) // ciclo x // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (h != null && !h.Contenuto) // se ok // riga-ok
                { // apre // riga-ok
                    tuttiFocolaiRisolti = false; // setta // riga-ok
                    break; // stop // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok

            // blocco: controlla se va
            if (tuttiFocolaiRisolti) // se ok // riga-ok
            { // apre // riga-ok
                PortaSettore[] porte = Object.FindObjectsByType<PortaSettore>(FindObjectsSortMode.None); // setta // riga-ok
                // blocco: gira piu volte
                foreach (PortaSettore p in porte) // ciclo x // riga-ok
                { // apre // riga-ok
                    // blocco: controlla se va
                    if (p != null) // se ok // riga-ok
                    { // apre // riga-ok
                        activeTargetHighlights.Add(new HighlightTargetInfo // ok qua // riga-ok
                        { // apre // riga-ok
                            worldPosition = p.transform.position, // setta // riga-ok
                            label = "🚪 PORTA SETTORE / EVACUAZIONE", // setta // riga-ok
                            color = Color.green, // setta // riga-ok
                            targetTransform = p.transform // setta // riga-ok
                        }); // chiama // riga-ok
                    } // chiude // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void GeneraBeaconsOlografici() // roba pub // riga-ok
    { // apre // riga-ok
        // I beacon sono oggetti runtime temporanei: luce + colonna trasparente.
        // Vengono distrutti alla fine della scansione per non sporcare la scena.
        ClearVisualBeacons(); // chiama // riga-ok

        // blocco: gira piu volte
        foreach (HighlightTargetInfo target in activeTargetHighlights) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (target.targetTransform == null) continue; // se ok // riga-ok

            GameObject beaconGO = new GameObject($"Scanner_HoloBeacon_{target.label}"); // setta // riga-ok
            beaconGO.transform.position = target.worldPosition + Vector3.up * 1.2f; // setta // riga-ok

            // 1. Luce volumetrica pulsante
            Light pointLight = beaconGO.AddComponent<Light>(); // setta // riga-ok
            pointLight.type = LightType.Point; // setta // riga-ok
            pointLight.color = target.color; // setta // riga-ok
            pointLight.range = 8.0f; // setta // riga-ok
            pointLight.intensity = 2.5f; // setta // riga-ok

            // 2. Colonna / Fascio di luce olografica verticale
            GameObject columnGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder); // setta // riga-ok
            columnGO.name = "Holo_Beam"; // setta // riga-ok
            columnGO.transform.SetParent(beaconGO.transform, false); // chiama // riga-ok
            columnGO.transform.localPosition = Vector3.up * 1.5f; // setta // riga-ok
            columnGO.transform.localScale = new Vector3(0.18f, 1.5f, 0.18f); // setta // riga-ok

            Collider col = columnGO.GetComponent<Collider>(); // setta // riga-ok
            // blocco: controlla se va
            if (col != null) Destroy(col); // se ok // riga-ok

            Renderer ren = columnGO.GetComponent<Renderer>(); // setta // riga-ok
            // blocco: controlla se va
            if (ren != null) // se ok // riga-ok
            { // apre // riga-ok
                Material mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color")); // setta // riga-ok
                Color c = target.color; // setta // riga-ok
                c.a = 0.5f; // setta // riga-ok
                mat.color = c; // setta // riga-ok
                ren.material = mat; // setta // riga-ok
            } // chiude // riga-ok

            activeVisualBeacons.Add(beaconGO); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void ClearVisualBeacons() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: gira piu volte
        for (int i = activeVisualBeacons.Count - 1; i >= 0; i--) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (activeVisualBeacons[i] != null) // se ok // riga-ok
            { // apre // riga-ok
                Destroy(activeVisualBeacons[i]); // elimina // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
        activeVisualBeacons.Clear(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void AnalizzaBersaglio(Collider target) // roba pub // riga-ok
    { // apre // riga-ok
        // Analisi ordinata dal caso piu' specifico al piu' generico:
        // credenziale, hotspot, anomalia operativa, qualsiasi IInteractable.
        // blocco: controlla se va
        if (target == null) // se ok // riga-ok
            return; // torna val // riga-ok

        AccessCredentialPickup credential = target.GetComponent<AccessCredentialPickup>() ?? target.GetComponentInParent<AccessCredentialPickup>(); // setta // riga-ok
        // blocco: controlla se va
        if (credential != null) // se ok // riga-ok
        { // apre // riga-ok
            Debug.Log($"<color=cyan>[SCANNER]</color> Credenziale fisica rilevata: <b>{credential.DisplayName}</b>. Avvicinarsi e premere E per acquisirla."); // logga // riga-ok
            // blocco: controlla se va
            if (CyberHUD.Instance != null) // se ok // riga-ok
            { // apre // riga-ok
                CyberHUD.Instance.MostraNotificaAcquisizione("🔑 CREDENZIALE RILEVATA", $"{credential.DisplayName} // PREMI E PER RACCOGLIERE"); // chiama // riga-ok
            } // chiude // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        EmergencyHotspot hotspot = target.GetComponent<EmergencyHotspot>() ?? target.GetComponentInParent<EmergencyHotspot>(); // setta // riga-ok
        // blocco: controlla se va
        if (hotspot != null) // se ok // riga-ok
        { // apre // riga-ok
            string reqKey = hotspot.RequiredCredentialId; // setta // riga-ok
            bool possiedeChiave = !string.IsNullOrWhiteSpace(reqKey) && // setta // riga-ok
                MissionManager.Instance != null && // setta // riga-ok
                MissionManager.Instance.HaRaccoltoCredenzialeSpecifica(reqKey); // chiama // riga-ok

            // blocco: controlla se va
            if (possiedeChiave) // se ok // riga-ok
            { // apre // riga-ok
                string stato = hotspot.Contenuto ? "CONTENUTO / RIPARATO" : "CRITICO // GUASTO ATTIVO"; // setta // riga-ok
                Debug.Log($"<color=yellow>[SCANNER]</color> Focolaio analizzato: <b>{hotspot.name}</b> [{stato}] - Chiave [{reqKey}] abilitata."); // logga // riga-ok
                // blocco: controlla se va
                if (CyberHUD.Instance != null) // se ok // riga-ok
                { // apre // riga-ok
                    CyberHUD.Instance.MostraNotificaAcquisizione("⚠️ FOCOLAIO ANALIZZATO", $"{hotspot.name.ToUpper()} // {stato}"); // chiama // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
            { // apre // riga-ok
                Debug.Log($"<color=red>[SCANNER]</color> Focolaio non visibile/accessibile: chiave <b>{reqKey}</b> non ancora posseduta."); // logga // riga-ok
                // blocco: controlla se va
                if (CyberHUD.Instance != null) // se ok // riga-ok
                { // apre // riga-ok
                    CyberHUD.Instance.MostraNotificaAcquisizione("🔒 CHIAVE NON POSSEDUTA", $"TROVA PRIMA LA CHIAVE: {reqKey}"); // chiama // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        OstacoloCausale obstacle = target.GetComponent<OstacoloCausale>() ?? target.GetComponentInParent<OstacoloCausale>(); // setta // riga-ok
        // blocco: controlla se va
        if (obstacle != null) // se ok // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (GameManager.Instance != null) // se ok // riga-ok
                GameManager.Instance.RegisterSecuritySignature(obstacle.idCausale); // chiama // riga-ok

            Debug.Log($"<color=cyan>[SCANNER]</color> Firma di sicurezza acquisita da anomalia: <b>{obstacle.idCausale}</b>."); // logga // riga-ok
            // blocco: controlla se va
            if (CyberHUD.Instance != null) // se ok // riga-ok
            { // apre // riga-ok
                CyberHUD.Instance.MostraNotificaAcquisizione("⚡ FIRMA ACQUISITA", $"ANOMALIA {obstacle.idCausale} ARCHIVIATA"); // chiama // riga-ok
            } // chiude // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        IInteractable interactable = target.GetComponent<IInteractable>() ?? target.GetComponentInParent<IInteractable>(); // setta // riga-ok
        // blocco: controlla se va
        if (interactable != null) // se ok // riga-ok
        { // apre // riga-ok
            Debug.Log($"<color=cyan>[SCANNER]</color> Oggetto operativo analizzabile rilevato: <b>{target.name}</b>."); // logga // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        Debug.LogWarning($"[SCANNER] {target.name} colpito, ma nessun protocollo di emergenza riconosciuto."); // logga // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnDrawGizmos() // roba pub // riga-ok
    { // apre // riga-ok
        Vector3 origineGizmo = puntoDiOrigine != null ? puntoDiOrigine.position : (Camera.main != null ? Camera.main.transform.position : transform.position + Vector3.up * 1.5f); // setta // riga-ok
        Vector3 direzioneGizmo = Application.isPlaying && Camera.main != null ? Camera.main.transform.forward : transform.forward; // setta // riga-ok

        Gizmos.color = isHighlightActive ? Color.yellow : Color.cyan; // setta // riga-ok
        Gizmos.DrawRay(origineGizmo, direzioneGizmo * portataScanner); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnGUI() // roba pub // riga-ok
    { // apre // riga-ok
        // Overlay leggero e immediato per prototipo: mirino centrale e marker 2D.
        // In una UI finale potrebbe essere sostituito da Canvas/HUD dedicato.
        // 1. Mirino centrale (puntino ciano/giallo a seconda dello stato)
        float size = isHighlightActive ? 6f : 4f; // setta // riga-ok
        float x = (Screen.width / 2f) - (size / 2f); // setta // riga-ok
        float y = (Screen.height / 2f) - (size / 2f); // setta // riga-ok

        GUI.color = isHighlightActive ? neonYellow : new Color(0f, 1f, 1f, 0.8f); // setta // riga-ok
        GUI.DrawTexture(new Rect(x, y, size, size), Texture2D.whiteTexture); // chiama // riga-ok

        // 2. Indicatori Olografici dei Problemi sullo Schermo (HUD Markers) durante i 5 secondi
        // blocco: controlla se va
        if (isHighlightActive && telecameraPrincipale != null) // se ok // riga-ok
        { // apre // riga-ok
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label) // setta // riga-ok
            { // apre // riga-ok
                fontSize = 16, // setta // riga-ok
                fontStyle = FontStyle.Bold, // setta // riga-ok
                alignment = TextAnchor.MiddleCenter, // setta // riga-ok
                wordWrap = true // testo va // riga-ok
            }; // ok qua // riga-ok

            // blocco: gira piu volte
            for (int i = 0; i < activeTargetHighlights.Count; i++) // ciclo x // riga-ok
            { // apre // riga-ok
                HighlightTargetInfo target = activeTargetHighlights[i]; // setta // riga-ok
                // blocco: controlla se va
                if (target.targetTransform == null) continue; // se ok // riga-ok

                Vector3 targetWorldPos = target.targetTransform.position + Vector3.up * 1.0f; // setta // riga-ok
                Vector3 screenPos = telecameraPrincipale.WorldToScreenPoint(targetWorldPos); // setta // riga-ok

                // Se l'oggetto è davanti alla telecamera
                // blocco: controlla se va
                if (screenPos.z > 0f) // se ok // riga-ok
                { // apre // riga-ok
                    float distance = Vector3.Distance(telecameraPrincipale.transform.position, targetWorldPos); // setta // riga-ok
                    float guiY = Screen.height - screenPos.y; // setta // riga-ok
                    string testoMarker = $"{target.label} [{Mathf.RoundToInt(distance)}m]"; // setta // riga-ok
                    float larghezzaBox = Mathf.Clamp(labelStyle.CalcSize(new GUIContent(testoMarker)).x + 44f, 320f, Mathf.Min(620f, Screen.width - 32f)); // calcola // riga-ok
                    float altezzaBox = Mathf.Clamp(labelStyle.CalcHeight(new GUIContent(testoMarker), larghezzaBox - 24f) + 16f, 42f, 74f); // calcola // riga-ok
                    float boxX = Mathf.Clamp(screenPos.x - (larghezzaBox * 0.5f), 16f, Screen.width - larghezzaBox - 16f); // calcola // riga-ok
                    float boxY = Mathf.Clamp(guiY - altezzaBox - 10f, 16f, Screen.height - altezzaBox - 16f); // calcola // riga-ok
                    Rect markerRect = new Rect(boxX, boxY, larghezzaBox, altezzaBox); // setta // riga-ok

                    // Badge di evidenziazione
                    GUI.color = new Color(0.02f, 0.05f, 0.08f, 0.85f); // setta // riga-ok
                    GUI.DrawTexture(markerRect, Texture2D.whiteTexture); // chiama // riga-ok

                    // Contorno
                    GUI.color = target.color; // setta // riga-ok
                    labelStyle.normal.textColor = target.color; // setta // riga-ok
                    GUI.Label(markerRect, testoMarker, labelStyle); // chiama // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
