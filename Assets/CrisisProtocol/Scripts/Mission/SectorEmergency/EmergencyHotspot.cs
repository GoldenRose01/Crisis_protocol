using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EmergencyHotspot : MonoBehaviour, IInteractable
{
    [Header("Dati Focolaio")]
    [SerializeField] private string hotspotId = "REACTOR_FAULT_001";
    [SerializeField] private string requiredCredentialId = "KEYCARD_A01";
    [SerializeField] private bool applicaTagAutomatico = true;

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

    [SerializeField] private GameObject containedStateObject;

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

        AutoTrovaParticelleGuastoSeVuoto();
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

        // Se non ancora contenuto, avvia tutte le perdite di gocce/scintille
        if (!contenuto)
        {
            AvviaTutteLeParticelleGuasto();
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
                    CalibraVisibilitaGocce(ps);
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
    /// Calibra le particelle come vera perdita chimica/radioattiva: gocce verde fluorescente neon che colano verso il basso.
    /// </summary>
    public static void CalibraVisibilitaGocce(ParticleSystem ps)
    {
        if (ps == null) return;

        var main = ps.main;
        main.playOnAwake = true;
        main.loop = true;

        string n = ps.name.ToLower();
        if (n.Contains("water") || n.Contains("drip") || n.Contains("gocc") || n.Contains("leak") || n.Contains("chemical") || n.Contains("splatter"))
        {
            // 1. Colore Verde Fluorescente Radioattivo / Tossico puro
            Color verdeFluo = new Color(0.15f, 1f, 0.08f, 1f);
            main.startColor = new ParticleSystem.MinMaxGradient(verdeFluo);

            // 2. Dimensione e Durata goccia
            main.startSize = 0.24f;
            main.startLifetime = 3.5f;

            // 3. Caduta realistica verso il basso (gravità), non uno spruzzo sparato a pressione
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
            main.gravityModifier = 0.95f;

            // 4. Forma stretta a gocciolamento puntiforme da crepa (non cono aperto come spruzzo)
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.04f;

            // 5. Ritmo di emissione: gocciolamento costante e nitido
            var emission = ps.emission;
            emission.rateOverTime = 12f;

            // 6. Colora anche il materiale del ParticleSystemRenderer in Verde Neon Fluorescente
            ParticleSystemRenderer rend = ps.GetComponent<ParticleSystemRenderer>();
            if (rend != null && rend.material != null)
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
            // Scintille elettriche arancio/giallo brillante
            Color coloreScintille = new Color(1f, 0.75f, 0.1f, 1f);
            main.startColor = coloreScintille;
            main.startSize = 0.08f;
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

    private void ApplicaRisoluzioneGrafica()
    {
        if (targetRenderer != null)
            targetRenderer.material.color = containedColor;

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
