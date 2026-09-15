// ============================================================================
// Crisis Protocol / Sector Containment - Missione e contenimento
// File: .\Assets\CrisisProtocol\Scripts\Mission\SectorEmergency\EmergencyHotspot.cs
// Responsabilita': modella credenziali, focolai, portelloni, hazard o parametri di bilanciamento del loop emergenza -> contenimento -> estrazione.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine; // usa lib // riga-ok

[RequireComponent(typeof(Collider))] // nota unity // riga-ok
// blocco: classe x roba grossa
public class EmergencyHotspot : MonoBehaviour, IInteractable // classe qui // riga-ok
{ // apre // riga-ok
    // blocco: scelte rapide
    public enum HotspotVisualType { AutoDetect, WaterChemicalLeak, ElectricSparks, ToxicGasLeak } // enum val // riga-ok

    [Header("Dati Focolaio")] // nota unity // riga-ok
    [SerializeField] private string hotspotId = "REACTOR_FAULT_001"; // setta // riga-ok
    [SerializeField] private string requiredCredentialId = "KEYCARD_A01"; // setta // riga-ok
    [SerializeField] private bool applicaTagAutomatico = true; // setta // riga-ok
    [SerializeField] private HotspotVisualType modalitaVisiva = HotspotVisualType.AutoDetect; // setta // riga-ok

    [Header("Feedback Visivo e Particelle")] // nota unity // riga-ok
    [SerializeField] private Color criticalColor = new Color(1f, 0.12f, 0.05f); // setta // riga-ok
    [SerializeField] private Color containedColor = Color.green; // setta // riga-ok
    
    [Tooltip("Particelle di gocce d'acqua, perdite, scintille, fumo o fiamme ATTIVE durante il guasto (si spengono quando risolvi il problema).")] // nota unity // riga-ok
    [SerializeField] private ParticleSystem[] particelleGuasto; // ok qua // riga-ok

    [Tooltip("Effetto di contenimento/risoluzione (verde o stabilizzazione) che si accende SOLO quando ripari il problema.")] // nota unity // riga-ok
    [SerializeField] private ParticleSystem containmentVfx; // ok qua // riga-ok

    [Tooltip("Se true, trova e spegne automaticamente tutte le particelle di perdite/scintille presenti su questo oggetto, figli o genitori del serbatoio.")] // nota unity // riga-ok
    [SerializeField] private bool autoDisattivaParticelleGuasto = true; // setta // riga-ok
    
    [Tooltip("Se true, aumenta automaticamente la luminosità e dimensione delle gocce/scintille per renderle chiaramente visibili nel buio.")] // nota unity // riga-ok
    [SerializeField] private bool potenziaVisibilitaParticelle = true; // setta // riga-ok

    [Tooltip("Scala globale applicata a tutte le particelle generate (getto gas, nube, scintille). " + // ok qua // riga-ok
             "Valore 1 = dimensioni standard. Usa 0.2-0.4 per hotspot piccoli come container o cavi.")] // setta // riga-ok
    [Range(0.05f, 3.0f)] // nota unity // riga-ok
    [SerializeField] private float moltiplicatoreParticelle = 1.0f; // setta // riga-ok

    [Tooltip("Se assegnato, le particelle generate automaticamente partiranno da questo punto esatto (usa un GameObject vuoto). Se vuoto, partono dal centro del collider.")] // nota unity // riga-ok
    [SerializeField] private Transform puntoEmissioneCustom; // ok qua // riga-ok

    [SerializeField] private GameObject containedStateObject; // ok qua // riga-ok

    [Header("Audio 3D")] // nota unity // riga-ok
    [Tooltip("Suono continuo del guasto attivo (perdita chimica, fischio gas, ronzio elettrico).")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoLoopGuasto; // ok qua // riga-ok

    [Tooltip("Suono di avvenuto contenimento / riparazione del focolaio.")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoRiparazione; // ok qua // riga-ok

    [Range(0f, 1f)] [SerializeField] private float volumeAudio = 0.8f; // setta // riga-ok

    private AudioSource loopAudioSource; // roba pub // riga-ok
    private Renderer targetRenderer; // roba pub // riga-ok
    private bool contenuto; // roba pub // riga-ok

    // blocco: funzione fa cose
    private void Awake() // roba pub // riga-ok
    { // apre // riga-ok
        AssicuraColliderValido(); // chiama // riga-ok
        ApplicaTagUnity(); // chiama // riga-ok
        targetRenderer = GetComponentInChildren<Renderer>(); // setta // riga-ok

        // Auto-fix: se Water_Drip o Sparks sono stati erroneamente trascinati dentro Containment VFX, spostali in Particelle Guasto
        // blocco: controlla se va
        if (containmentVfx != null) // se ok // riga-ok
        { // apre // riga-ok
            string vfxName = containmentVfx.name.ToLower(); // setta // riga-ok
            // blocco: controlla se va
            if (vfxName.Contains("water") || vfxName.Contains("drip") || vfxName.Contains("spark") || vfxName.Contains("leak") || vfxName.Contains("gocc")) // se ok // riga-ok
            { // apre // riga-ok
                Debug.Log($"<color=cyan>[HOTSPOT]</color> Spostato '{containmentVfx.name}' da 'Containment VFX' a 'Particelle Guasto Attivo' per avviarlo correttamente all'inizio!"); // logga // riga-ok
                AggiungiParticellaGuasto(containmentVfx); // chiama // riga-ok
                containmentVfx = null; // setta // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // Se configurato come perdita di gas o focolaio 2, assicura la presenza del sistema di particelle di gas
        // blocco: controlla se va
        if (modalitaVisiva == HotspotVisualType.ToxicGasLeak || hotspotId == "REACTOR_FAULT_002" || name.Contains("(1)")) // se ok // riga-ok
        { // apre // riga-ok
            GeneraPerditaGasSeAssente(); // chiama // riga-ok
        } // chiude // riga-ok

        AutoTrovaParticelleGuastoSeVuoto(); // chiama // riga-ok
    } // chiude // riga-ok

    /// <summary>
    /// Genera a runtime o in editor il sistema particellare duale di getto in pressione + nube volumetrica di gas tossico.
    /// </summary>
    // blocco: funzione fa cose
    public void GeneraPerditaGasSeAssente() // roba pub // riga-ok
    { // apre // riga-ok
        Transform existing = transform.Find("VFX_Gas_Leak_Emitter"); // setta // riga-ok
        // blocco: controlla se va
        if (existing != null && existing.GetComponent<ParticleSystem>() != null) // se ok // riga-ok
            return; // torna val // riga-ok

        float sc = moltiplicatoreParticelle; // alias breve per leggibilità // setta // riga-ok

        // 1. Root Emettitore Getto Gas — usa il punto custom se definito, altrimenti posiziona calcolata
        GameObject gasRoot = new GameObject("VFX_Gas_Leak_Emitter"); // setta // riga-ok
        // blocco: controlla se va
        if (puntoEmissioneCustom != null) // se ok // riga-ok
        { // apre // riga-ok
            gasRoot.transform.SetParent(puntoEmissioneCustom, false); // chiama // riga-ok
            gasRoot.transform.localPosition = Vector3.zero; // setta // riga-ok
            gasRoot.transform.localRotation = Quaternion.identity; // setta // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            gasRoot.transform.SetParent(transform, false); // chiama // riga-ok
            gasRoot.transform.localPosition = new Vector3(0f, 0.6f * sc, 0.4f * sc); // setta // riga-ok
            gasRoot.transform.localRotation = Quaternion.Euler(-30f, 0f, 0f); // setta // riga-ok
        } // chiude // riga-ok

        // 2. Jet Stream (Getto Gas in Pressione)
        ParticleSystem psJet = gasRoot.AddComponent<ParticleSystem>(); // setta // riga-ok
        // FIX: ferma subito il sistema prima di modificarne le proprieta' (duration non si puo' impostare mentre e' in play)
        psJet.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // ferma // riga-ok
        var mainJet = psJet.main; // setta // riga-ok
        mainJet.playOnAwake = true; // setta // riga-ok
        mainJet.loop = true; // setta // riga-ok
        mainJet.duration = 2.0f; // setta // riga-ok
        mainJet.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.8f); // setta // riga-ok
        mainJet.startSpeed    = new ParticleSystem.MinMaxCurve(2.2f * sc, 4.0f * sc); // setta // riga-ok
        mainJet.startSize     = new ParticleSystem.MinMaxCurve(0.2f * sc, 0.55f * sc); // setta // riga-ok
        mainJet.startColor    = new ParticleSystem.MinMaxGradient(new Color(0.35f, 1f, 0.3f, 0.65f), new Color(0.75f, 1f, 0.25f, 0.50f)); // setta // riga-ok
        mainJet.gravityModifier = -0.04f; // setta // riga-ok
        mainJet.simulationSpace = ParticleSystemSimulationSpace.World; // setta // riga-ok
        mainJet.maxParticles    = Mathf.Max(20, Mathf.RoundToInt(200 * sc)); // setta // riga-ok

        var emissionJet = psJet.emission; // setta // riga-ok
        emissionJet.enabled = true; // setta // riga-ok
        emissionJet.rateOverTime = 32f; // setta // riga-ok

        var shapeJet = psJet.shape; // setta // riga-ok
        shapeJet.enabled = true; // setta // riga-ok
        shapeJet.shapeType = ParticleSystemShapeType.SingleSidedEdge; // setta // riga-ok
        shapeJet.radius = 0.8f * sc; // setta // riga-ok

        var solJet = psJet.sizeOverLifetime; // setta // riga-ok
        solJet.enabled = true; // setta // riga-ok
        AnimationCurve curveJet = new AnimationCurve(); // setta // riga-ok
        curveJet.AddKey(0f, 0.3f); // chiama // riga-ok
        curveJet.AddKey(0.3f, 0.95f); // chiama // riga-ok
        curveJet.AddKey(1f, 2.2f); // chiama // riga-ok
        solJet.size = new ParticleSystem.MinMaxCurve(1f, curveJet); // setta // riga-ok

        var colJet = psJet.colorOverLifetime; // setta // riga-ok
        colJet.enabled = true; // setta // riga-ok
        Gradient gradJet = new Gradient(); // setta // riga-ok
        gradJet.SetKeys( // ok qua // riga-ok
            new GradientColorKey[] { new GradientColorKey(new Color(0.35f, 1f, 0.3f), 0f), new GradientColorKey(new Color(0.85f, 1f, 0.35f), 1f) }, // ok qua // riga-ok
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.65f, 0.12f), new GradientAlphaKey(0.45f, 0.65f), new GradientAlphaKey(0f, 1f) } // ok qua // riga-ok
        ); // chiama // riga-ok
        colJet.color = gradJet; // setta // riga-ok

        var noiseJet = psJet.noise; // setta // riga-ok
        noiseJet.enabled = true; // setta // riga-ok
        noiseJet.strength = 0.25f; // setta // riga-ok
        noiseJet.frequency = 0.45f; // setta // riga-ok
        noiseJet.scrollSpeed = 0.35f; // setta // riga-ok

        ParticleSystemRenderer rendJet = gasRoot.GetComponent<ParticleSystemRenderer>(); // setta // riga-ok
        // blocco: controlla se va
        if (rendJet != null) // se ok // riga-ok
        { // apre // riga-ok
            rendJet.renderMode = ParticleSystemRenderMode.Billboard; // setta // riga-ok
            rendJet.alignment = ParticleSystemRenderSpace.View; // setta // riga-ok
        } // chiude // riga-ok

        // 3. Secondary Billowing Cloud (Nube di Gas Espansa)
        GameObject cloudGo = new GameObject("Gas_Cloud_Billowing"); // setta // riga-ok
        cloudGo.transform.SetParent(gasRoot.transform, false); // chiama // riga-ok
        cloudGo.transform.localPosition = new Vector3(0f, 0.3f * sc, 0.6f * sc); // setta // riga-ok

        ParticleSystem psCloud = cloudGo.AddComponent<ParticleSystem>(); // setta // riga-ok
        // FIX: ferma subito prima di modificare duration
        psCloud.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // ferma // riga-ok
        var mainCloud = psCloud.main; // setta // riga-ok
        mainCloud.playOnAwake    = true; // setta // riga-ok
        mainCloud.loop           = true; // setta // riga-ok
        mainCloud.duration       = 4.0f; // setta // riga-ok
        mainCloud.startLifetime  = new ParticleSystem.MinMaxCurve(2.5f, 4.2f); // setta // riga-ok
        mainCloud.startSpeed     = new ParticleSystem.MinMaxCurve(0.3f * sc, 0.9f * sc); // setta // riga-ok
        mainCloud.startSize      = new ParticleSystem.MinMaxCurve(0.7f * sc, 1.6f * sc); // setta // riga-ok
        mainCloud.startColor     = new ParticleSystem.MinMaxGradient(new Color(0.3f, 0.95f, 0.25f, 0.35f), new Color(0.6f, 0.95f, 0.2f, 0.25f)); // setta // riga-ok
        mainCloud.gravityModifier = -0.02f; // setta // riga-ok
        mainCloud.simulationSpace = ParticleSystemSimulationSpace.World; // setta // riga-ok
        mainCloud.maxParticles    = Mathf.Max(10, Mathf.RoundToInt(100 * sc)); // setta // riga-ok

        var emissionCloud = psCloud.emission; // setta // riga-ok
        emissionCloud.enabled = true; // setta // riga-ok
        emissionCloud.rateOverTime = 10f; // setta // riga-ok

        var shapeCloud = psCloud.shape; // setta // riga-ok
        shapeCloud.enabled   = true; // setta // riga-ok
        shapeCloud.shapeType = ParticleSystemShapeType.Box; // setta // riga-ok
        shapeCloud.scale     = new Vector3(1.5f * sc, 0.4f * sc, 1.5f * sc); // setta // riga-ok

        var solCloud = psCloud.sizeOverLifetime; // setta // riga-ok
        solCloud.enabled = true; // setta // riga-ok
        AnimationCurve curveCloud = new AnimationCurve(); // setta // riga-ok
        curveCloud.AddKey(0f, 0.5f); // chiama // riga-ok
        curveCloud.AddKey(0.4f, 1.4f); // chiama // riga-ok
        curveCloud.AddKey(1f, 2.6f); // chiama // riga-ok
        solCloud.size = new ParticleSystem.MinMaxCurve(1f, curveCloud); // setta // riga-ok

        var colCloud = psCloud.colorOverLifetime; // setta // riga-ok
        colCloud.enabled = true; // setta // riga-ok
        Gradient gradCloud = new Gradient(); // setta // riga-ok
        gradCloud.SetKeys( // ok qua // riga-ok
            new GradientColorKey[] { new GradientColorKey(new Color(0.3f, 0.95f, 0.25f), 0f), new GradientColorKey(new Color(0.7f, 1f, 0.3f), 1f) }, // ok qua // riga-ok
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.35f, 0.25f), new GradientAlphaKey(0.25f, 0.75f), new GradientAlphaKey(0f, 1f) } // ok qua // riga-ok
        ); // chiama // riga-ok
        colCloud.color = gradCloud; // setta // riga-ok

        ParticleSystemRenderer rendCloud = cloudGo.GetComponent<ParticleSystemRenderer>(); // setta // riga-ok
        // blocco: controlla se va
        if (rendCloud != null) // se ok // riga-ok
        { // apre // riga-ok
            rendCloud.renderMode = ParticleSystemRenderMode.Billboard; // setta // riga-ok
        } // chiude // riga-ok

        // 4. Pericolo Ambientale (StructuralHazard)
        StructuralHazard hazard = gasRoot.AddComponent<StructuralHazard>(); // setta // riga-ok
        SphereCollider hazardCollider = gasRoot.AddComponent<SphereCollider>(); // setta // riga-ok
        hazardCollider.isTrigger = true; // setta // riga-ok
        hazardCollider.radius    = Mathf.Max(0.5f, 2.2f * sc); // zona pericolo proporzionata // setta // riga-ok

        // 5. Aggiungi a particelle guasto
        AggiungiParticellaGuasto(psJet); // chiama // riga-ok
        AggiungiParticellaGuasto(psCloud); // chiama // riga-ok

        Debug.Log($"<color=lime>[GAS LEAK]</color> Generato sistema particellare perdita di gas su <b>{name}</b>!"); // logga // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnValidate() // roba pub // riga-ok
    { // apre // riga-ok
        ApplicaTagUnity(); // chiama // riga-ok
    } // chiude // riga-ok

    /// <summary>
    /// Controlla se il collider è vuoto (es. MeshCollider con Mesh: None) e genera automaticamente un solido CapsuleCollider fisico basato sulla grandezza del serbatoio.
    /// </summary>
    // blocco: funzione fa cose
    public void AssicuraColliderValido() // roba pub // riga-ok
    { // apre // riga-ok
        Collider col = GetComponent<Collider>(); // setta // riga-ok
        MeshCollider mc = col as MeshCollider; // setta // riga-ok

        // Se non c'è collider o c'è un MeshCollider senza Mesh assegnata
        // blocco: controlla se va
        if (col == null || (mc != null && mc.sharedMesh == null)) // se ok // riga-ok
        { // apre // riga-ok
            Renderer[] renderers = GetComponentsInChildren<Renderer>(); // setta // riga-ok
            // blocco: controlla se va
            if (renderers != null && renderers.Length > 0) // se ok // riga-ok
            { // apre // riga-ok
                Bounds b = renderers[0].bounds; // setta // riga-ok
                // blocco: gira piu volte
                for (int i = 1; i < renderers.Length; i++) // ciclo x // riga-ok
                { // apre // riga-ok
                    b.Encapsulate(renderers[i].bounds); // chiama // riga-ok
                } // chiude // riga-ok

                // blocco: controlla se va
                if (mc != null) // se ok // riga-ok
                { // apre // riga-ok
                    // blocco: controlla se va
                    if (Application.isPlaying) // se ok // riga-ok
                        Destroy(mc); // elimina // riga-ok
                    // blocco: caso diverso
                    else // se no // riga-ok
                        DestroyImmediate(mc); // elimina // riga-ok
                } // chiude // riga-ok

                CapsuleCollider capsule = GetComponent<CapsuleCollider>(); // setta // riga-ok
                // blocco: controlla se va
                if (capsule == null) // se ok // riga-ok
                    capsule = gameObject.AddComponent<CapsuleCollider>(); // setta // riga-ok

                capsule.center = transform.InverseTransformPoint(b.center); // setta // riga-ok

                Vector3 lossy = transform.lossyScale; // setta // riga-ok
                float sx = Mathf.Abs(lossy.x) > 0.001f ? Mathf.Abs(lossy.x) : 1f; // setta // riga-ok
                float sy = Mathf.Abs(lossy.y) > 0.001f ? Mathf.Abs(lossy.y) : 1f; // setta // riga-ok
                float sz = Mathf.Abs(lossy.z) > 0.001f ? Mathf.Abs(lossy.z) : 1f; // setta // riga-ok

                capsule.radius = Mathf.Max(b.extents.x / sx, b.extents.z / sz); // setta // riga-ok
                capsule.height = Mathf.Max(b.size.y / sy, capsule.radius * 2f); // setta // riga-ok
                capsule.direction = 1; // Asse Y // setta // riga-ok

                Debug.Log($"<color=lime>[COLLIDER]</color> Generato CapsuleCollider solido su <b>{name}</b>! (Raggio: {capsule.radius:F2}, Altezza: {capsule.height:F2})"); // logga // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void Start() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (targetRenderer != null) // se ok // riga-ok
            targetRenderer.material.color = criticalColor; // setta // riga-ok

        // blocco: controlla se va
        if (containedStateObject != null) // se ok // riga-ok
            containedStateObject.SetActive(false); // chiama // riga-ok

        // Se non ancora contenuto, avvia tutte le perdite di gocce/scintille e audio 3D
        // blocco: controlla se va
        if (!contenuto) // se ok // riga-ok
        { // apre // riga-ok
            AvviaTutteLeParticelleGuasto(); // chiama // riga-ok
            AvviaAudioGuasto(); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    /// <summary>
    /// Trova automaticamente tutte le particelle di gocce d'acqua / scintille nell'oggetto, nei figli e nel genitore/root
    /// </summary>
    // blocco: funzione fa cose
    public void AutoTrovaParticelleGuastoSeVuoto() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (particelleGuasto == null || particelleGuasto.Length == 0) // se ok // riga-ok
        { // apre // riga-ok
            var trovate = new System.Collections.Generic.List<ParticleSystem>(); // setta // riga-ok

            // 1. Cerca nei figli
            trovate.AddRange(GetComponentsInChildren<ParticleSystem>(true)); // chiama // riga-ok

            // 2. Cerca nel genitore e fratelli (es. RootNode del chemical_tank)
            // blocco: controlla se va
            if (transform.parent != null) // se ok // riga-ok
            { // apre // riga-ok
                ParticleSystem[] fratelli = transform.parent.GetComponentsInChildren<ParticleSystem>(true); // setta // riga-ok
                // blocco: gira piu volte
                foreach (var ps in fratelli) // ciclo x // riga-ok
                { // apre // riga-ok
                    // blocco: controlla se va
                    if (ps != null && !trovate.Contains(ps)) // se ok // riga-ok
                        trovate.Add(ps); // chiama // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok

            // blocco: controlla se va
            if (trovate.Count > 0) // se ok // riga-ok
            { // apre // riga-ok
                particelleGuasto = trovate.ToArray(); // setta // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (potenziaVisibilitaParticelle && particelleGuasto != null) // se ok // riga-ok
        { // apre // riga-ok
            // blocco: gira piu volte
            foreach (var ps in particelleGuasto) // ciclo x // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (ps != null) // se ok // riga-ok
                    CalibraVisibilitaGocce(ps, moltiplicatoreParticelle); // chiama // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void AggiungiParticellaGuasto(ParticleSystem ps) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (ps == null) return; // se ok // riga-ok
        var lista = new System.Collections.Generic.List<ParticleSystem>(); // setta // riga-ok
        // blocco: controlla se va
        if (particelleGuasto != null) // se ok // riga-ok
            lista.AddRange(particelleGuasto); // chiama // riga-ok

        // blocco: controlla se va
        if (!lista.Contains(ps)) // se ok // riga-ok
        { // apre // riga-ok
            lista.Add(ps); // chiama // riga-ok
            particelleGuasto = lista.ToArray(); // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    /// <summary>
    /// Calibra le particelle differenziando esteticamente Gas (sfere espanse), Acqua (gocce allungate), e Scintille (strisce veloci).
    /// </summary>
    // blocco: funzione fa cose
    public static void CalibraVisibilitaGocce(ParticleSystem ps, float moltiplicatore = 1.0f) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (ps == null) return; // se ok // riga-ok

        var main = ps.main; // setta // riga-ok
        main.playOnAwake = true; // setta // riga-ok
        main.loop = true; // setta // riga-ok

        string n = ps.name.ToLower(); // setta // riga-ok
        // blocco: controlla se va
        if (n.Contains("gas") || n.Contains("steam") || n.Contains("fumo") || n.Contains("vapor") || n.Contains("smoke")) // se ok // riga-ok
        { // apre // riga-ok
            // 1. Colore Gas Tossico / Vapore Chimico ad Alta Pressione
            Color coloreGas = new Color(0.32f, 0.98f, 0.28f, 0.60f); // setta // riga-ok
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.35f, 1f, 0.3f, 0.65f), new Color(0.7f, 1f, 0.2f, 0.45f)); // setta // riga-ok

            // 2. Dimensione e Durata della colonna di gas
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 3.0f); // setta // riga-ok
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f * moltiplicatore, 0.65f * moltiplicatore); // setta // riga-ok

            // 3. Spinta di fuoriuscita e galleggiamento (leggera ascesa nell'aria)
            // blocco: controlla se va
            if (n.Contains("cloud") || n.Contains("nube") || n.Contains("haze")) // se ok // riga-ok
            { // apre // riga-ok
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.2f); // setta // riga-ok
                main.gravityModifier = -0.02f; // setta // riga-ok
            } // chiude // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
            { // apre // riga-ok
                main.startSpeed = new ParticleSystem.MinMaxCurve(2.0f, 3.8f); // setta // riga-ok
                main.gravityModifier = -0.04f; // setta // riga-ok
            } // chiude // riga-ok

            // 4. Forma ad espansione larga (crepa/rottura) invece che foro puntiforme
            var shape = ps.shape; // setta // riga-ok
            shape.enabled = true; // setta // riga-ok
            // blocco: controlla se va
            if (n.Contains("cloud") || n.Contains("nube")) // se ok // riga-ok
            { // apre // riga-ok
                shape.shapeType = ParticleSystemShapeType.Box; // setta // riga-ok
                shape.scale = new Vector3(1.5f * moltiplicatore, 0.4f * moltiplicatore, 1.5f * moltiplicatore); // setta // riga-ok
            } // chiude // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
            { // apre // riga-ok
                shape.shapeType = ParticleSystemShapeType.SingleSidedEdge; // setta // riga-ok
                shape.radius = 0.8f * moltiplicatore; // Ampiezza del foro // setta // riga-ok
            } // chiude // riga-ok

            // 5. Ritmo di emissione continuo e denso
            var emission = ps.emission; // setta // riga-ok
            emission.rateOverTime = 30f; // setta // riga-ok

            // 6. Espansione volumetrica nel tempo (Size over Lifetime)
            var sol = ps.sizeOverLifetime; // setta // riga-ok
            sol.enabled = true; // setta // riga-ok
            AnimationCurve curve = new AnimationCurve(); // setta // riga-ok
            curve.AddKey(0f, 0.3f); // chiama // riga-ok
            curve.AddKey(0.3f, 0.9f); // chiama // riga-ok
            curve.AddKey(1f, 2.2f); // chiama // riga-ok
            sol.size = new ParticleSystem.MinMaxCurve(1f, curve); // setta // riga-ok

            // 7. Dissolvenza graduale (Color over Lifetime con morbido Alpha Blend)
            var col = ps.colorOverLifetime; // setta // riga-ok
            col.enabled = true; // setta // riga-ok
            Gradient grad = new Gradient(); // setta // riga-ok
            grad.SetKeys( // ok qua // riga-ok
                new GradientColorKey[] { new GradientColorKey(coloreGas, 0f), new GradientColorKey(new Color(0.85f, 1f, 0.4f), 1f) }, // ok qua // riga-ok
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.65f, 0.15f), new GradientAlphaKey(0.45f, 0.7f), new GradientAlphaKey(0f, 1f) } // ok qua // riga-ok
            ); // chiama // riga-ok
            col.color = grad; // setta // riga-ok

            // 8. Colora anche il materiale del ParticleSystemRenderer
            ParticleSystemRenderer rend = ps.GetComponent<ParticleSystemRenderer>(); // setta // riga-ok
            // blocco: controlla se va
            if (rend != null && rend.material != null) // se ok // riga-ok
            { // apre // riga-ok
                Material mat = rend.material; // setta // riga-ok
                // blocco: controlla se va
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", coloreGas); // se ok // riga-ok
                // blocco: controlla se va
                if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", coloreGas); // se ok // riga-ok
                // blocco: controlla se va
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", coloreGas); // se ok // riga-ok
                // blocco: controlla se va
                if (mat.HasProperty("_EmissionColor")) // se ok // riga-ok
                { // apre // riga-ok
                    mat.EnableKeyword("_EMISSION"); // chiama // riga-ok
                    mat.SetColor("_EmissionColor", coloreGas * 1.8f); // chiama // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok

            // Calibra eventuali emettitori figli
            // blocco: gira piu volte
            foreach (var childPS in ps.GetComponentsInChildren<ParticleSystem>(true)) // ciclo x // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (childPS != ps) // se ok // riga-ok
                { // apre // riga-ok
                    var cMain = childPS.main; // setta // riga-ok
                    cMain.startColor = new ParticleSystem.MinMaxGradient(coloreGas); // setta // riga-ok
                    childPS.gameObject.SetActive(true); // chiama // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
        // blocco: controlla se va
        else if (n.Contains("water") || n.Contains("drip") || n.Contains("gocc") || n.Contains("leak") || n.Contains("chemical") || n.Contains("splatter")) // se ok // riga-ok
        { // apre // riga-ok
            // 1. Colore Verde Fluorescente Radioattivo / Tossico puro
            Color verdeFluo = new Color(0.15f, 1f, 0.08f, 1f); // setta // riga-ok
            main.startColor = new ParticleSystem.MinMaxGradient(verdeFluo); // setta // riga-ok

            // 2. Dimensione e Durata goccia
            main.startSize = 0.24f * moltiplicatore; // setta // riga-ok
            main.startLifetime = 3.5f; // setta // riga-ok

            // 3. Caduta realistica verso il basso (gravità), non uno spruzzo sparato a pressione
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.45f); // setta // riga-ok
            main.gravityModifier = 0.95f; // setta // riga-ok

            // 4. Forma stretta a gocciolamento puntiforme
            var shape = ps.shape; // setta // riga-ok
            shape.enabled = true; // setta // riga-ok
            shape.shapeType = ParticleSystemShapeType.Sphere; // setta // riga-ok
            shape.radius = 0.04f * moltiplicatore; // setta // riga-ok

            // 5. Ritmo di emissione: gocciolamento costante e nitido
            var emission = ps.emission; // setta // riga-ok
            emission.rateOverTime = 12f; // setta // riga-ok

            // Estetica: RenderMode Stretch per sembrare gocce liquide allungate dalla caduta
            ParticleSystemRenderer rend = ps.GetComponent<ParticleSystemRenderer>(); // setta // riga-ok
            // blocco: controlla se va
            if (rend != null) // se ok // riga-ok
            { // apre // riga-ok
                rend.renderMode = ParticleSystemRenderMode.Stretch; // setta // riga-ok
                rend.lengthScale = 1.8f;   // Allunga la goccia // setta // riga-ok
                rend.velocityScale = 0.1f; // Scala in base alla velocità // setta // riga-ok
                
                // blocco: controlla se va
                if (rend.material != null) // se ok // riga-ok
                { // apre // riga-ok
                    Material mat = rend.material; // setta // riga-ok
                    // blocco: controlla se va
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", verdeFluo); // se ok // riga-ok
                    // blocco: controlla se va
                    if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", verdeFluo); // se ok // riga-ok
                    // blocco: controlla se va
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", verdeFluo); // se ok // riga-ok
                    // blocco: controlla se va
                    if (mat.HasProperty("_EmissionColor")) // se ok // riga-ok
                    { // apre // riga-ok
                        mat.EnableKeyword("_EMISSION"); // chiama // riga-ok
                        mat.SetColor("_EmissionColor", verdeFluo * 2.5f); // chiama // riga-ok
                    } // chiude // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok

            // Calibra anche eventuali figli (es. Rain_Splatter / schizzi a terra)
            // blocco: gira piu volte
            foreach (var childPS in ps.GetComponentsInChildren<ParticleSystem>(true)) // ciclo x // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (childPS != ps) // se ok // riga-ok
                { // apre // riga-ok
                    var cMain = childPS.main; // setta // riga-ok
                    cMain.startColor = new ParticleSystem.MinMaxGradient(verdeFluo); // setta // riga-ok
                    childPS.gameObject.SetActive(true); // chiama // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
        // blocco: controlla se va
        else if (n.Contains("spark") || n.Contains("scintill")) // se ok // riga-ok
        { // apre // riga-ok
            // Scintille elettriche blu/ciano brillante
            Color coloreScintille = new Color(0.1f, 0.7f, 1f, 1f); // setta // riga-ok
            main.startColor = coloreScintille; // setta // riga-ok
            main.startSize = 0.08f * moltiplicatore; // setta // riga-ok
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4.5f); // setta // riga-ok
            main.gravityModifier = 0.65f; // setta // riga-ok
            
            // Ritmo e burst
            var emission = ps.emission; // setta // riga-ok
            emission.rateOverTime = 40f; // setta // riga-ok

            // Estetica: RenderMode Stretch estremo per sembrare veri e propri fulmini/scintille in movimento
            ParticleSystemRenderer rend = ps.GetComponent<ParticleSystemRenderer>(); // setta // riga-ok
            // blocco: controlla se va
            if (rend != null) // se ok // riga-ok
            { // apre // riga-ok
                rend.renderMode = ParticleSystemRenderMode.Stretch; // setta // riga-ok
                rend.lengthScale = 3.5f;   // Molto allungate // setta // riga-ok
                rend.velocityScale = 0.2f; // Reagiscono fortemente alla velocità // setta // riga-ok
                
                // blocco: controlla se va
                if (rend.material != null) // se ok // riga-ok
                { // apre // riga-ok
                    Material mat = rend.material; // setta // riga-ok
                    // blocco: controlla se va
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", coloreScintille); // se ok // riga-ok
                    // blocco: controlla se va
                    if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", coloreScintille); // se ok // riga-ok
                    // blocco: controlla se va
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", coloreScintille); // se ok // riga-ok
                    // blocco: controlla se va
                    if (mat.HasProperty("_EmissionColor")) // se ok // riga-ok
                    { // apre // riga-ok
                        mat.EnableKeyword("_EMISSION"); // chiama // riga-ok
                        mat.SetColor("_EmissionColor", coloreScintille * 3.0f); // chiama // riga-ok
                    } // chiude // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        ps.gameObject.SetActive(true); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void AvviaTutteLeParticelleGuasto() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (particelleGuasto != null) // se ok // riga-ok
        { // apre // riga-ok
            // blocco: gira piu volte
            foreach (var ps in particelleGuasto) // ciclo x // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (ps != null) // se ok // riga-ok
                { // apre // riga-ok
                    ps.gameObject.SetActive(true); // chiama // riga-ok
                    // blocco: controlla se va
                    if (!ps.isPlaying) // se ok // riga-ok
                        ps.Play(true); // chiama // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    public event System.Action OnFocolaioContenuto; // roba pub // riga-ok
    public bool Contenuto => contenuto; // roba pub // riga-ok
    public string RequiredCredentialId => requiredCredentialId; // roba pub // riga-ok
    public string HotspotId => hotspotId; // roba pub // riga-ok

    // blocco: funzione fa cose
    public void Interact() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (contenuto) // se ok // riga-ok
        { // apre // riga-ok
            Debug.Log($"<color=green>[FOCOLAIO]</color> <b>{hotspotId}</b> è già stato riparato e stabilizzato."); // logga // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (MissionManager.Instance == null) // se ok // riga-ok
        { // apre // riga-ok
            Debug.LogError($"[FOCOLAIO] MissionManager assente nella scena: impossibile contenere {hotspotId}.", this); // logga // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (!string.IsNullOrWhiteSpace(requiredCredentialId) && !MissionManager.Instance.PossiedeCredenziale(requiredCredentialId)) // se ok // riga-ok
        { // apre // riga-ok
            Debug.LogWarning($"<color=yellow>[FOCOLAIO]</color> Impossibile riparare <b>{hotspotId}</b>! Serve prima raccogliere la chiave/credenziale: <b>'{requiredCredentialId}'</b>.", this); // logga // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        bool successo = MissionManager.Instance.ContieniFocolaio(hotspotId, requiredCredentialId); // setta // riga-ok
        // blocco: controlla se va
        if (!successo) // se ok // riga-ok
        { // apre // riga-ok
            Debug.LogWarning($"[FOCOLAIO] Fallimento contenimento per {hotspotId}.", this); // logga // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        contenuto = true; // setta // riga-ok
        ApplicaRisoluzioneGrafica(); // chiama // riga-ok
        OnFocolaioContenuto?.Invoke(); // chiama // riga-ok
        Debug.Log($"<color=lime>[FOCOLAIO]</color> Guasto <b>{hotspotId}</b> RIPARATO con successo! Gocce d'acqua e scintille arrestate."); // logga // riga-ok
    } // chiude // riga-ok

    [ContextMenu("DEBUG: Risolvi Questo Focolaio")] // nota unity // riga-ok
    // blocco: funzione fa cose
    public void ForzaRisoluzioneDebug() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (contenuto) // se ok // riga-ok
            return; // torna val // riga-ok

        // blocco: controlla se va
        if (MissionManager.Instance != null) // se ok // riga-ok
        { // apre // riga-ok
            MissionManager.Instance.RegistraCredenziale(requiredCredentialId); // chiama // riga-ok
            MissionManager.Instance.ContieniFocolaio(hotspotId, requiredCredentialId); // chiama // riga-ok
        } // chiude // riga-ok

        contenuto = true; // setta // riga-ok
        ApplicaRisoluzioneGrafica(); // chiama // riga-ok
        OnFocolaioContenuto?.Invoke(); // chiama // riga-ok
        Debug.Log($"<color=lime>[FOCOLAIO]</color> Focolaio <b>{hotspotId}</b> risolto per debug (gocce/scintille spente)!"); // logga // riga-ok
    } // chiude // riga-ok

    [ContextMenu("Potenzia e Accendi Gocce d'Acqua / Scintille")] // nota unity // riga-ok
    // blocco: funzione fa cose
    public void ForzaAccensioneVisibile() // roba pub // riga-ok
    { // apre // riga-ok
        AutoTrovaParticelleGuastoSeVuoto(); // chiama // riga-ok
        AvviaTutteLeParticelleGuasto(); // chiama // riga-ok
        Debug.Log($"<color=cyan>[HOTSPOT]</color> Gocce e particelle guasto calibrate e avviate per <b>{name}</b>!"); // logga // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void InizializzaAudio() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (loopAudioSource == null) // se ok // riga-ok
            loopAudioSource = GetComponent<AudioSource>(); // setta // riga-ok

        // blocco: controlla se va
        if (loopAudioSource == null) // se ok // riga-ok
        { // apre // riga-ok
            loopAudioSource = gameObject.AddComponent<AudioSource>(); // setta // riga-ok
            loopAudioSource.playOnAwake = false; // setta // riga-ok
            loopAudioSource.spatialBlend = 1.0f; // 3D // setta // riga-ok
            loopAudioSource.loop = true; // setta // riga-ok
            loopAudioSource.rolloffMode = AudioRolloffMode.Logarithmic; // setta // riga-ok
            loopAudioSource.minDistance = 2.0f; // setta // riga-ok
            loopAudioSource.maxDistance = 16.0f; // setta // riga-ok
            loopAudioSource.dopplerLevel = 0f; // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void AvviaAudioGuasto() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (suonoLoopGuasto == null) return; // se ok // riga-ok
        InizializzaAudio(); // chiama // riga-ok
        // blocco: controlla se va
        if (loopAudioSource != null) // se ok // riga-ok
        { // apre // riga-ok
            loopAudioSource.clip = suonoLoopGuasto; // setta // riga-ok
            loopAudioSource.volume = volumeAudio; // setta // riga-ok
            loopAudioSource.loop = true; // setta // riga-ok
            // blocco: controlla se va
            if (!loopAudioSource.isPlaying) // se ok // riga-ok
                loopAudioSource.Play(); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void FermaAudioGuasto() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (loopAudioSource != null && loopAudioSource.isPlaying) // se ok // riga-ok
        { // apre // riga-ok
            loopAudioSource.Stop(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (suonoRiparazione != null) // se ok // riga-ok
        { // apre // riga-ok
            AudioSource.PlayClipAtPoint(suonoRiparazione, transform.position, volumeAudio); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void ApplicaRisoluzioneGrafica() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (targetRenderer != null) // se ok // riga-ok
            targetRenderer.material.color = containedColor; // setta // riga-ok

        // Spegni l'audio del guasto e riproduci il suono di successo
        FermaAudioGuasto(); // chiama // riga-ok

        // Spegni le gocce e le scintille del guasto
        // blocco: controlla se va
        if (particelleGuasto != null) // se ok // riga-ok
        { // apre // riga-ok
            // blocco: gira piu volte
            foreach (var ps in particelleGuasto) // ciclo x // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (ps != null) // se ok // riga-ok
                { // apre // riga-ok
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // chiama // riga-ok
                    ps.gameObject.SetActive(false); // chiama // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // Spegni automaticamente tutte le particelle figlie e sorelle del guasto
        // blocco: controlla se va
        if (autoDisattivaParticelleGuasto) // se ok // riga-ok
        { // apre // riga-ok
            Transform root = (transform.parent != null) ? transform.parent : transform; // setta // riga-ok
            ParticleSystem[] allPS = root.GetComponentsInChildren<ParticleSystem>(true); // setta // riga-ok
            // blocco: gira piu volte
            foreach (var ps in allPS) // ciclo x // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (ps != null && ps != containmentVfx) // se ok // riga-ok
                { // apre // riga-ok
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // chiama // riga-ok
                    ps.gameObject.SetActive(false); // chiama // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // Attiva effetto di successo se presente (containment VFX verde)
        // blocco: controlla se va
        if (containmentVfx != null) // se ok // riga-ok
        { // apre // riga-ok
            containmentVfx.gameObject.SetActive(true); // chiama // riga-ok
            containmentVfx.Play(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (containedStateObject != null) // se ok // riga-ok
            containedStateObject.SetActive(true); // chiama // riga-ok

        // Disattiva eventuali collider di pericolo elettrico / gas
        StructuralHazard hazard = GetComponent<StructuralHazard>() ?? GetComponentInChildren<StructuralHazard>(); // setta // riga-ok
        // blocco: controlla se va
        if (hazard != null) // se ok // riga-ok
            hazard.enabled = false; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void ApplicaTagUnity() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (applicaTagAutomatico) // se ok // riga-ok
            SectorContainmentTags.ApplyTag(gameObject, SectorContainmentTags.EmergencyHotspot); // chiama // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
