// ============================================================================
// Crisis Protocol / Sector Containment - Missione e contenimento
// File: .\Assets\CrisisProtocol\Scripts\Mission\SectorEmergency\EmergencyHotspot.cs
// Responsabilita': modella credenziali, focolai, portelloni, hazard o parametri di bilanciamento del loop emergenza -> contenimento -> estrazione.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EmergencyHotspot : MonoBehaviour, IInteractable
{
    public enum HotspotVisualType { AutoDetect, WaterChemicalLeak, ElectricSparks, ToxicGasLeak }

    [Header("Dati Focolaio")]
    [SerializeField] private string hotspotId = "REACTOR_FAULT_001";
    [SerializeField] private string requiredCredentialId = "KEYCARD_A01";
    [SerializeField] private bool applicaTagAutomatico = true;
    [SerializeField] private HotspotVisualType modalitaVisiva = HotspotVisualType.AutoDetect;

    [Header("Feedback Visivo e Particelle")]
    [SerializeField] private Color criticalColor = new Color(1f, 0.12f, 0.05f);
    [SerializeField] private Color containedColor = Color.green;
    
    [Tooltip("Particelle di gocce d'acqua, perdite, scintille, fumo o fiamme ATTIVE durante il guasto (si spengono quando risolvi il problema).")]
    [SerializeField] private ParticleSystem[] particelleGuasto;

    [Tooltip("Effetto di contenimento/risoluzione (verde o stabilizzazione) che si accende SOLO quando ripari il problema.")]
    [SerializeField] private ParticleSystem containmentVfx;

    [Tooltip("Se true, trova e spegne automaticamente tutte le particelle di perdite/scintille presenti su questo oggetto, figli o genitori del serbatoio.")]
    [SerializeField] private bool autoDisattivaParticelleGuasto = true;
    
    [Tooltip("Se true, aumenta automaticamente la luminosità e dimensione delle gocce/scintille per renderle chiaramente visibili nel buio.")]
    [SerializeField] private bool potenziaVisibilitaParticelle = true;

    [Tooltip("Scala globale applicata a tutte le particelle generate (getto gas, nube, scintille). " +
             "Valore 1 = dimensioni standard. Usa 0.2-0.4 per hotspot piccoli come container o cavi.")]
    [Range(0.05f, 3.0f)]
    [SerializeField] private float moltiplicatoreParticelle = 1.0f;

    [Tooltip("Se assegnato, le particelle generate automaticamente partiranno da questo punto esatto (usa un GameObject vuoto). Se vuoto, partono dal centro del collider.")]
    [SerializeField] private Transform puntoEmissioneCustom;

    [SerializeField] private GameObject containedStateObject;

    [Header("Audio 3D")]
    [Tooltip("Suono continuo del guasto attivo (perdita chimica, fischio gas, ronzio elettrico).")]
    [SerializeField] private AudioClip suonoLoopGuasto;

    [Tooltip("Suono di avvenuto contenimento / riparazione del focolaio.")]
    [SerializeField] private AudioClip suonoRiparazione;

    [Range(0f, 1f)] [SerializeField] private float volumeAudio = 0.8f;

    private AudioSource loopAudioSource;
    private Renderer targetRenderer;
    private bool contenuto;

    private void Awake()
    {
        AssicuraColliderValido();
        ApplicaTagUnity();
        targetRenderer = GetComponentInChildren<Renderer>();

        // Auto-fix: se Water_Drip o Sparks sono stati erroneamente trascinati dentro Containment VFX, spostali in Particelle Guasto
        if (containmentVfx != null)
        {
            string vfxName = containmentVfx.name.ToLower();
            if (vfxName.Contains("water") || vfxName.Contains("drip") || vfxName.Contains("spark") || vfxName.Contains("leak") || vfxName.Contains("gocc"))
            {
                Debug.Log($"<color=cyan>[HOTSPOT]</color> Spostato '{containmentVfx.name}' da 'Containment VFX' a 'Particelle Guasto Attivo' per avviarlo correttamente all'inizio!");
                AggiungiParticellaGuasto(containmentVfx);
                containmentVfx = null;
            }
        }

        // Se configurato come perdita di gas o focolaio 2, assicura la presenza del sistema di particelle di gas
        if (modalitaVisiva == HotspotVisualType.ToxicGasLeak || hotspotId == "REACTOR_FAULT_002" || name.Contains("(1)"))
        {
            GeneraPerditaGasSeAssente();
        }

        AutoTrovaParticelleGuastoSeVuoto();
    }

    /// <summary>
    /// Genera a runtime o in editor il sistema particellare duale di getto in pressione + nube volumetrica di gas tossico.
    /// </summary>
    public void GeneraPerditaGasSeAssente()
    {
        Transform existing = transform.Find("VFX_Gas_Leak_Emitter");
        if (existing != null && existing.GetComponent<ParticleSystem>() != null)
            return;

        float sc = moltiplicatoreParticelle; // alias breve per leggibilità

        // 1. Root Emettitore Getto Gas — usa il punto custom se definito, altrimenti posiziona calcolata
        GameObject gasRoot = new GameObject("VFX_Gas_Leak_Emitter");
        if (puntoEmissioneCustom != null)
        {
            gasRoot.transform.SetParent(puntoEmissioneCustom, false);
            gasRoot.transform.localPosition = Vector3.zero;
            gasRoot.transform.localRotation = Quaternion.identity;
        }
        else
        {
            gasRoot.transform.SetParent(transform, false);
            gasRoot.transform.localPosition = new Vector3(0f, 0.6f * sc, 0.4f * sc);
            gasRoot.transform.localRotation = Quaternion.Euler(-30f, 0f, 0f);
        }

        // 2. Jet Stream (Getto Gas in Pressione)
        ParticleSystem psJet = gasRoot.AddComponent<ParticleSystem>();
        var mainJet = psJet.main;
        mainJet.playOnAwake = true;
        mainJet.loop = true;
        mainJet.duration = 2.0f;
        mainJet.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.8f);
        mainJet.startSpeed    = new ParticleSystem.MinMaxCurve(2.2f * sc, 4.0f * sc);
        mainJet.startSize     = new ParticleSystem.MinMaxCurve(0.2f * sc, 0.55f * sc);
        mainJet.startColor    = new ParticleSystem.MinMaxGradient(new Color(0.35f, 1f, 0.3f, 0.65f), new Color(0.75f, 1f, 0.25f, 0.50f));
        mainJet.gravityModifier = -0.04f;
        mainJet.simulationSpace = ParticleSystemSimulationSpace.World;
        mainJet.maxParticles    = Mathf.Max(20, Mathf.RoundToInt(200 * sc));

        var emissionJet = psJet.emission;
        emissionJet.enabled = true;
        emissionJet.rateOverTime = 32f;

        var shapeJet = psJet.shape;
        shapeJet.enabled = true;
        shapeJet.shapeType = ParticleSystemShapeType.SingleSidedEdge;
        shapeJet.radius = 0.8f * sc;

        var solJet = psJet.sizeOverLifetime;
        solJet.enabled = true;
        AnimationCurve curveJet = new AnimationCurve();
        curveJet.AddKey(0f, 0.3f);
        curveJet.AddKey(0.3f, 0.95f);
        curveJet.AddKey(1f, 2.2f);
        solJet.size = new ParticleSystem.MinMaxCurve(1f, curveJet);

        var colJet = psJet.colorOverLifetime;
        colJet.enabled = true;
        Gradient gradJet = new Gradient();
        gradJet.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(0.35f, 1f, 0.3f), 0f), new GradientColorKey(new Color(0.85f, 1f, 0.35f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.65f, 0.12f), new GradientAlphaKey(0.45f, 0.65f), new GradientAlphaKey(0f, 1f) }
        );
        colJet.color = gradJet;

        var noiseJet = psJet.noise;
        noiseJet.enabled = true;
        noiseJet.strength = 0.25f;
        noiseJet.frequency = 0.45f;
        noiseJet.scrollSpeed = 0.35f;

        ParticleSystemRenderer rendJet = gasRoot.GetComponent<ParticleSystemRenderer>();
        if (rendJet != null)
        {
            rendJet.renderMode = ParticleSystemRenderMode.Billboard;
            rendJet.alignment = ParticleSystemRenderSpace.View;
        }

        // 3. Secondary Billowing Cloud (Nube di Gas Espansa)
        GameObject cloudGo = new GameObject("Gas_Cloud_Billowing");
        cloudGo.transform.SetParent(gasRoot.transform, false);
        cloudGo.transform.localPosition = new Vector3(0f, 0.3f * sc, 0.6f * sc);

        ParticleSystem psCloud = cloudGo.AddComponent<ParticleSystem>();
        var mainCloud = psCloud.main;
        mainCloud.playOnAwake    = true;
        mainCloud.loop           = true;
        mainCloud.duration       = 4.0f;
        mainCloud.startLifetime  = new ParticleSystem.MinMaxCurve(2.5f, 4.2f);
        mainCloud.startSpeed     = new ParticleSystem.MinMaxCurve(0.3f * sc, 0.9f * sc);
        mainCloud.startSize      = new ParticleSystem.MinMaxCurve(0.7f * sc, 1.6f * sc);
        mainCloud.startColor     = new ParticleSystem.MinMaxGradient(new Color(0.3f, 0.95f, 0.25f, 0.35f), new Color(0.6f, 0.95f, 0.2f, 0.25f));
        mainCloud.gravityModifier = -0.02f;
        mainCloud.simulationSpace = ParticleSystemSimulationSpace.World;
        mainCloud.maxParticles    = Mathf.Max(10, Mathf.RoundToInt(100 * sc));

        var emissionCloud = psCloud.emission;
        emissionCloud.enabled = true;
        emissionCloud.rateOverTime = 10f;

        var shapeCloud = psCloud.shape;
        shapeCloud.enabled   = true;
        shapeCloud.shapeType = ParticleSystemShapeType.Box;
        shapeCloud.scale     = new Vector3(1.5f * sc, 0.4f * sc, 1.5f * sc);

        var solCloud = psCloud.sizeOverLifetime;
        solCloud.enabled = true;
        AnimationCurve curveCloud = new AnimationCurve();
        curveCloud.AddKey(0f, 0.5f);
        curveCloud.AddKey(0.4f, 1.4f);
        curveCloud.AddKey(1f, 2.6f);
        solCloud.size = new ParticleSystem.MinMaxCurve(1f, curveCloud);

        var colCloud = psCloud.colorOverLifetime;
        colCloud.enabled = true;
        Gradient gradCloud = new Gradient();
        gradCloud.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(0.3f, 0.95f, 0.25f), 0f), new GradientColorKey(new Color(0.7f, 1f, 0.3f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.35f, 0.25f), new GradientAlphaKey(0.25f, 0.75f), new GradientAlphaKey(0f, 1f) }
        );
        colCloud.color = gradCloud;

        ParticleSystemRenderer rendCloud = cloudGo.GetComponent<ParticleSystemRenderer>();
        if (rendCloud != null)
        {
            rendCloud.renderMode = ParticleSystemRenderMode.Billboard;
        }

        // 4. Pericolo Ambientale (StructuralHazard)
        StructuralHazard hazard = gasRoot.AddComponent<StructuralHazard>();
        SphereCollider hazardCollider = gasRoot.AddComponent<SphereCollider>();
        hazardCollider.isTrigger = true;
        hazardCollider.radius    = Mathf.Max(0.5f, 2.2f * sc); // zona pericolo proporzionata

        // 5. Aggiungi a particelle guasto
        AggiungiParticellaGuasto(psJet);
        AggiungiParticellaGuasto(psCloud);

        Debug.Log($"<color=lime>[GAS LEAK]</color> Generato sistema particellare perdita di gas su <b>{name}</b>!");
    }

    private void OnValidate()
    {
        ApplicaTagUnity();
    }

    /// <summary>
    /// Controlla se il collider è vuoto (es. MeshCollider con Mesh: None) e genera automaticamente un solido CapsuleCollider fisico basato sulla grandezza del serbatoio.
    /// </summary>
    public void AssicuraColliderValido()
    {
        Collider col = GetComponent<Collider>();
        MeshCollider mc = col as MeshCollider;

        // Se non c'è collider o c'è un MeshCollider senza Mesh assegnata
        if (col == null || (mc != null && mc.sharedMesh == null))
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers != null && renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    b.Encapsulate(renderers[i].bounds);
                }

                if (mc != null)
                {
                    if (Application.isPlaying)
                        Destroy(mc);
                    else
                        DestroyImmediate(mc);
                }

                CapsuleCollider capsule = GetComponent<CapsuleCollider>();
                if (capsule == null)
                    capsule = gameObject.AddComponent<CapsuleCollider>();

                capsule.center = transform.InverseTransformPoint(b.center);

                Vector3 lossy = transform.lossyScale;
                float sx = Mathf.Abs(lossy.x) > 0.001f ? Mathf.Abs(lossy.x) : 1f;
                float sy = Mathf.Abs(lossy.y) > 0.001f ? Mathf.Abs(lossy.y) : 1f;
                float sz = Mathf.Abs(lossy.z) > 0.001f ? Mathf.Abs(lossy.z) : 1f;

                capsule.radius = Mathf.Max(b.extents.x / sx, b.extents.z / sz);
                capsule.height = Mathf.Max(b.size.y / sy, capsule.radius * 2f);
                capsule.direction = 1; // Asse Y

                Debug.Log($"<color=lime>[COLLIDER]</color> Generato CapsuleCollider solido su <b>{name}</b>! (Raggio: {capsule.radius:F2}, Altezza: {capsule.height:F2})");
            }
        }
    }

    private void Start()
    {
        if (targetRenderer != null)
            targetRenderer.material.color = criticalColor;

        if (containedStateObject != null)
            containedStateObject.SetActive(false);

        // Se non ancora contenuto, avvia tutte le perdite di gocce/scintille e audio 3D
        if (!contenuto)
        {
            AvviaTutteLeParticelleGuasto();
            AvviaAudioGuasto();
        }
    }

    /// <summary>
    /// Trova automaticamente tutte le particelle di gocce d'acqua / scintille nell'oggetto, nei figli e nel genitore/root
    /// </summary>
    public void AutoTrovaParticelleGuastoSeVuoto()
    {
        if (particelleGuasto == null || particelleGuasto.Length == 0)
        {
            var trovate = new System.Collections.Generic.List<ParticleSystem>();

            // 1. Cerca nei figli
            trovate.AddRange(GetComponentsInChildren<ParticleSystem>(true));

            // 2. Cerca nel genitore e fratelli (es. RootNode del chemical_tank)
            if (transform.parent != null)
            {
                ParticleSystem[] fratelli = transform.parent.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var ps in fratelli)
                {
                    if (ps != null && !trovate.Contains(ps))
                        trovate.Add(ps);
                }
            }

            if (trovate.Count > 0)
            {
                particelleGuasto = trovate.ToArray();
            }
        }

        if (potenziaVisibilitaParticelle && particelleGuasto != null)
        {
            foreach (var ps in particelleGuasto)
            {
                if (ps != null)
                    CalibraVisibilitaGocce(ps, moltiplicatoreParticelle);
            }
        }
    }

    private void AggiungiParticellaGuasto(ParticleSystem ps)
    {
        if (ps == null) return;
        var lista = new System.Collections.Generic.List<ParticleSystem>();
        if (particelleGuasto != null)
            lista.AddRange(particelleGuasto);

        if (!lista.Contains(ps))
        {
            lista.Add(ps);
            particelleGuasto = lista.ToArray();
        }
    }

    /// <summary>
    /// Calibra le particelle differenziando esteticamente Gas (sfere espanse), Acqua (gocce allungate), e Scintille (strisce veloci).
    /// </summary>
    public static void CalibraVisibilitaGocce(ParticleSystem ps, float moltiplicatore = 1.0f)
    {
        if (ps == null) return;

        var main = ps.main;
        main.playOnAwake = true;
        main.loop = true;

        string n = ps.name.ToLower();
        if (n.Contains("gas") || n.Contains("steam") || n.Contains("fumo") || n.Contains("vapor") || n.Contains("smoke"))
        {
            // 1. Colore Gas Tossico / Vapore Chimico ad Alta Pressione
            Color coloreGas = new Color(0.32f, 0.98f, 0.28f, 0.60f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.35f, 1f, 0.3f, 0.65f), new Color(0.7f, 1f, 0.2f, 0.45f));

            // 2. Dimensione e Durata della colonna di gas
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 3.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f * moltiplicatore, 0.65f * moltiplicatore);

            // 3. Spinta di fuoriuscita e galleggiamento (leggera ascesa nell'aria)
            if (n.Contains("cloud") || n.Contains("nube") || n.Contains("haze"))
            {
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
                main.gravityModifier = -0.02f;
            }
            else
            {
                main.startSpeed = new ParticleSystem.MinMaxCurve(2.0f, 3.8f);
                main.gravityModifier = -0.04f;
            }

            // 4. Forma ad espansione larga (crepa/rottura) invece che foro puntiforme
            var shape = ps.shape;
            shape.enabled = true;
            if (n.Contains("cloud") || n.Contains("nube"))
            {
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(1.5f * moltiplicatore, 0.4f * moltiplicatore, 1.5f * moltiplicatore);
            }
            else
            {
                shape.shapeType = ParticleSystemShapeType.SingleSidedEdge;
                shape.radius = 0.8f * moltiplicatore; // Ampiezza del foro
            }

            // 5. Ritmo di emissione continuo e denso
            var emission = ps.emission;
            emission.rateOverTime = 30f;

            // 6. Espansione volumetrica nel tempo (Size over Lifetime)
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 0.3f);
            curve.AddKey(0.3f, 0.9f);
            curve.AddKey(1f, 2.2f);
            sol.size = new ParticleSystem.MinMaxCurve(1f, curve);

            // 7. Dissolvenza graduale (Color over Lifetime con morbido Alpha Blend)
            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(coloreGas, 0f), new GradientColorKey(new Color(0.85f, 1f, 0.4f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.65f, 0.15f), new GradientAlphaKey(0.45f, 0.7f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = grad;

            // 8. Colora anche il materiale del ParticleSystemRenderer
            ParticleSystemRenderer rend = ps.GetComponent<ParticleSystemRenderer>();
            if (rend != null && rend.material != null)
            {
                Material mat = rend.material;
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", coloreGas);
                if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", coloreGas);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", coloreGas);
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", coloreGas * 1.8f);
                }
            }

            // Calibra eventuali emettitori figli
            foreach (var childPS in ps.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (childPS != ps)
                {
                    var cMain = childPS.main;
                    cMain.startColor = new ParticleSystem.MinMaxGradient(coloreGas);
                    childPS.gameObject.SetActive(true);
                }
            }
        }
        else if (n.Contains("water") || n.Contains("drip") || n.Contains("gocc") || n.Contains("leak") || n.Contains("chemical") || n.Contains("splatter"))
        {
            // 1. Colore Verde Fluorescente Radioattivo / Tossico puro
            Color verdeFluo = new Color(0.15f, 1f, 0.08f, 1f);
            main.startColor = new ParticleSystem.MinMaxGradient(verdeFluo);

            // 2. Dimensione e Durata goccia
            main.startSize = 0.24f * moltiplicatore;
            main.startLifetime = 3.5f;

            // 3. Caduta realistica verso il basso (gravità), non uno spruzzo sparato a pressione
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
            main.gravityModifier = 0.95f;

            // 4. Forma stretta a gocciolamento puntiforme
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.04f * moltiplicatore;

            // 5. Ritmo di emissione: gocciolamento costante e nitido
            var emission = ps.emission;
            emission.rateOverTime = 12f;

            // Estetica: RenderMode Stretch per sembrare gocce liquide allungate dalla caduta
            ParticleSystemRenderer rend = ps.GetComponent<ParticleSystemRenderer>();
            if (rend != null)
            {
                rend.renderMode = ParticleSystemRenderMode.Stretch;
                rend.lengthScale = 1.8f;   // Allunga la goccia
                rend.velocityScale = 0.1f; // Scala in base alla velocità
                
                if (rend.material != null)
                {
                    Material mat = rend.material;
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", verdeFluo);
                    if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", verdeFluo);
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", verdeFluo);
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", verdeFluo * 2.5f);
                    }
                }
            }

            // Calibra anche eventuali figli (es. Rain_Splatter / schizzi a terra)
            foreach (var childPS in ps.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (childPS != ps)
                {
                    var cMain = childPS.main;
                    cMain.startColor = new ParticleSystem.MinMaxGradient(verdeFluo);
                    childPS.gameObject.SetActive(true);
                }
            }
        }
        else if (n.Contains("spark") || n.Contains("scintill"))
        {
            // Scintille elettriche blu/ciano brillante
            Color coloreScintille = new Color(0.1f, 0.7f, 1f, 1f);
            main.startColor = coloreScintille;
            main.startSize = 0.08f * moltiplicatore;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4.5f);
            main.gravityModifier = 0.65f;
            
            // Ritmo e burst
            var emission = ps.emission;
            emission.rateOverTime = 40f;

            // Estetica: RenderMode Stretch estremo per sembrare veri e propri fulmini/scintille in movimento
            ParticleSystemRenderer rend = ps.GetComponent<ParticleSystemRenderer>();
            if (rend != null)
            {
                rend.renderMode = ParticleSystemRenderMode.Stretch;
                rend.lengthScale = 3.5f;   // Molto allungate
                rend.velocityScale = 0.2f; // Reagiscono fortemente alla velocità
                
                if (rend.material != null)
                {
                    Material mat = rend.material;
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", coloreScintille);
                    if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", coloreScintille);
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", coloreScintille);
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", coloreScintille * 3.0f);
                    }
                }
            }
        }

        ps.gameObject.SetActive(true);
    }

    public void AvviaTutteLeParticelleGuasto()
    {
        if (particelleGuasto != null)
        {
            foreach (var ps in particelleGuasto)
            {
                if (ps != null)
                {
                    ps.gameObject.SetActive(true);
                    if (!ps.isPlaying)
                        ps.Play(true);
                }
            }
        }
    }

    public event System.Action OnFocolaioContenuto;
    public bool Contenuto => contenuto;
    public string RequiredCredentialId => requiredCredentialId;
    public string HotspotId => hotspotId;

    public void Interact()
    {
        if (contenuto)
        {
            Debug.Log($"<color=green>[FOCOLAIO]</color> <b>{hotspotId}</b> è già stato riparato e stabilizzato.");
            return;
        }

        if (MissionManager.Instance == null)
        {
            Debug.LogError($"[FOCOLAIO] MissionManager assente nella scena: impossibile contenere {hotspotId}.", this);
            return;
        }

        if (!string.IsNullOrWhiteSpace(requiredCredentialId) && !MissionManager.Instance.PossiedeCredenziale(requiredCredentialId))
        {
            Debug.LogWarning($"<color=yellow>[FOCOLAIO]</color> Impossibile riparare <b>{hotspotId}</b>! Serve prima raccogliere la chiave/credenziale: <b>'{requiredCredentialId}'</b>.", this);
            return;
        }

        bool successo = MissionManager.Instance.ContieniFocolaio(hotspotId, requiredCredentialId);
        if (!successo)
        {
            Debug.LogWarning($"[FOCOLAIO] Fallimento contenimento per {hotspotId}.", this);
            return;
        }

        contenuto = true;
        ApplicaRisoluzioneGrafica();
        OnFocolaioContenuto?.Invoke();
        Debug.Log($"<color=lime>[FOCOLAIO]</color> Guasto <b>{hotspotId}</b> RIPARATO con successo! Gocce d'acqua e scintille arrestate.");
    }

    [ContextMenu("DEBUG: Risolvi Questo Focolaio")]
    public void ForzaRisoluzioneDebug()
    {
        if (contenuto)
            return;

        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.RegistraCredenziale(requiredCredentialId);
            MissionManager.Instance.ContieniFocolaio(hotspotId, requiredCredentialId);
        }

        contenuto = true;
        ApplicaRisoluzioneGrafica();
        OnFocolaioContenuto?.Invoke();
        Debug.Log($"<color=lime>[FOCOLAIO]</color> Focolaio <b>{hotspotId}</b> risolto per debug (gocce/scintille spente)!");
    }

    [ContextMenu("Potenzia e Accendi Gocce d'Acqua / Scintille")]
    public void ForzaAccensioneVisibile()
    {
        AutoTrovaParticelleGuastoSeVuoto();
        AvviaTutteLeParticelleGuasto();
        Debug.Log($"<color=cyan>[HOTSPOT]</color> Gocce e particelle guasto calibrate e avviate per <b>{name}</b>!");
    }

    private void InizializzaAudio()
    {
        if (loopAudioSource == null)
            loopAudioSource = GetComponent<AudioSource>();

        if (loopAudioSource == null)
        {
            loopAudioSource = gameObject.AddComponent<AudioSource>();
            loopAudioSource.playOnAwake = false;
            loopAudioSource.spatialBlend = 1.0f; // 3D
            loopAudioSource.loop = true;
            loopAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            loopAudioSource.minDistance = 2.0f;
            loopAudioSource.maxDistance = 16.0f;
            loopAudioSource.dopplerLevel = 0f;
        }
    }

    public void AvviaAudioGuasto()
    {
        if (suonoLoopGuasto == null) return;
        InizializzaAudio();
        if (loopAudioSource != null)
        {
            loopAudioSource.clip = suonoLoopGuasto;
            loopAudioSource.volume = volumeAudio;
            loopAudioSource.loop = true;
            if (!loopAudioSource.isPlaying)
                loopAudioSource.Play();
        }
    }

    public void FermaAudioGuasto()
    {
        if (loopAudioSource != null && loopAudioSource.isPlaying)
        {
            loopAudioSource.Stop();
        }

        if (suonoRiparazione != null)
        {
            AudioSource.PlayClipAtPoint(suonoRiparazione, transform.position, volumeAudio);
        }
    }

    private void ApplicaRisoluzioneGrafica()
    {
        if (targetRenderer != null)
            targetRenderer.material.color = containedColor;

        // Spegni l'audio del guasto e riproduci il suono di successo
        FermaAudioGuasto();

        // Spegni le gocce e le scintille del guasto
        if (particelleGuasto != null)
        {
            foreach (var ps in particelleGuasto)
            {
                if (ps != null)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.gameObject.SetActive(false);
                }
            }
        }

        // Spegni automaticamente tutte le particelle figlie e sorelle del guasto
        if (autoDisattivaParticelleGuasto)
        {
            Transform root = (transform.parent != null) ? transform.parent : transform;
            ParticleSystem[] allPS = root.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in allPS)
            {
                if (ps != null && ps != containmentVfx)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.gameObject.SetActive(false);
                }
            }
        }

        // Attiva effetto di successo se presente (containment VFX verde)
        if (containmentVfx != null)
        {
            containmentVfx.gameObject.SetActive(true);
            containmentVfx.Play();
        }

        if (containedStateObject != null)
            containedStateObject.SetActive(true);

        // Disattiva eventuali collider di pericolo elettrico / gas
        StructuralHazard hazard = GetComponent<StructuralHazard>() ?? GetComponentInChildren<StructuralHazard>();
        if (hazard != null)
            hazard.enabled = false;
    }

    private void ApplicaTagUnity()
    {
        if (applicaTagAutomatico)
            SectorContainmentTags.ApplyTag(gameObject, SectorContainmentTags.EmergencyHotspot);
    }
}
