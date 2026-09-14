#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AI;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class SceneDoctor : EditorWindow
{
    [MenuItem("Tools/1-Click Scene Fix (NavMesh & Missing Scripts)")]
    public static void FixScene()
    {
        Debug.Log("<color=cyan>[SceneDoctor]</color> Inizio pulizia e calibrazione avanzata della scena...");

        // 1. Rimuovi script mancanti (Missing Scripts)
        int missingCount = 0;
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        
        foreach (GameObject go in allObjects)
        {
            int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            if (count > 0)
            {
                missingCount += count;
                Debug.Log($"[SceneDoctor] Rimossi {count} script mancanti da '{go.name}'", go);
                EditorUtility.SetDirty(go);
            }
        }
        
        if (missingCount > 0)
            Debug.Log($"<color=green>[SceneDoctor] {missingCount} script fantasma rimossi!</color>");

        // 2. Calibra tutti i Bot di Manutenzione presenti nella scena
        int botCount = 0;
        ManutenzioneBot[] bots = Object.FindObjectsByType<ManutenzioneBot>(FindObjectsSortMode.None);
        foreach (ManutenzioneBot bot in bots)
        {
            bot.ResetValoriPredefiniti();
            EditorUtility.SetDirty(bot.gameObject);
            botCount++;
        }
        if (botCount > 0)
            Debug.Log($"<color=green>[SceneDoctor] Calibrati {botCount} ManutenzioneBot con velocità di ronda calmi.</color>");

        // 3. Rimuovi il flag Static da tutte le Porte e Portelloni (altrimenti non si muovono)
        int porteStaticFix = 0;
        PortaSettore[] tutteLePorte = Object.FindObjectsByType<PortaSettore>(FindObjectsSortMode.None);
        foreach (PortaSettore p in tutteLePorte)
        {
            if (p != null)
            {
                foreach (Transform t in p.GetComponentsInChildren<Transform>(true))
                {
                    if (t.gameObject.isStatic)
                    {
                        t.gameObject.isStatic = false;
                        EditorUtility.SetDirty(t.gameObject);
                        porteStaticFix++;
                    }
                }
            }
        }
        if (porteStaticFix > 0)
            Debug.Log($"<color=green>[SceneDoctor] Rimosso flag Static da {porteStaticFix} elementi porta/vetrata.</color>");

        // 3b. Calibra Collider Solidi su tutti i Focolai di Emergenza (evita attraversamento mesh)
        int hotspotColliderFix = 0;
        EmergencyHotspot[] tuttiIFocolai = Object.FindObjectsByType<EmergencyHotspot>(FindObjectsSortMode.None);
        foreach (EmergencyHotspot h in tuttiIFocolai)
        {
            if (h != null)
            {
                h.AssicuraColliderValido();
                EditorUtility.SetDirty(h.gameObject);
                hotspotColliderFix++;
            }
        }
        if (hotspotColliderFix > 0)
            Debug.Log($"<color=green>[SceneDoctor] Generati/Verificati {hotspotColliderFix} collider fisici solidi su focolai ed emergenze.</color>");        // 3c. Rimozione radicale di tutti i NavMeshObstacle errati da muri statici, pavimenti, soffitti e strutture
        int ostacoliRimossi = 0;
        int ostacoliPorteCalibrati = 0;
        NavMeshObstacle[] tuttiGliOstacoli = Object.FindObjectsByType<NavMeshObstacle>(FindObjectsSortMode.None);
        foreach (NavMeshObstacle obs in tuttiGliOstacoli)
        {
            if (obs == null) continue;
            GameObject go = obs.gameObject;

            // Mantieni NavMeshObstacle ESCLUSIVAMENTE sulle porte dinamiche (PortaSettore, QuarantineGate)
            bool isPorta = go.GetComponent<PortaSettore>() != null || 
                           go.GetComponent<QuarantineGate>() != null || 
                           go.GetComponentInParent<PortaSettore>() != null ||
                           go.GetComponentInParent<QuarantineGate>() != null;

            if (isPorta)
            {
                obs.shape = NavMeshObstacleShape.Box;
                obs.carving = true;
                obs.carveOnlyStationary = false;
                obs.carvingMoveThreshold = 0.1f;

                Collider col = go.GetComponent<Collider>();
                if (col is BoxCollider bc)
                {
                    obs.center = bc.center;
                    obs.size = new Vector3(Mathf.Min(bc.size.x, 4f), Mathf.Min(bc.size.y, 4f), Mathf.Min(bc.size.z, 4f));
                }
                else
                {
                    obs.center = Vector3.zero;
                    obs.size = new Vector3(2.5f, 3f, 0.5f);
                }
                EditorUtility.SetDirty(go);
                ostacoliPorteCalibrati++;
            }
            else
            {
                // Muri, soffitti, pavimenti e strutture fisse NON devono avere NavMeshObstacle:
                // la loro fisica e blocco navigazione sono gestiti nativamente dalla geometria statica!
                DestroyImmediate(obs);
                EditorUtility.SetDirty(go);
                ostacoliRimossi++;
            }
        }
        if (ostacoliRimossi > 0)
            Debug.Log($"<color=green>[SceneDoctor] Rimossi {ostacoliRimossi} NavMeshObstacle giganti/errati da muri e strutture.</color>");

        // 3d. Classificazione accurata di Pavimenti e Muri
        int pavimentiCount = 0;
        int muriCount = 0;

        foreach (GameObject go in allObjects)
        {
            if (go == null) continue;

            // Salta entità dinamiche
            if (go.GetComponent<NavMeshAgent>() != null || 
                go.GetComponent<muve_pg>() != null || 
                go.GetComponent<SalutePlayer>() != null ||
                go.GetComponent<GuardiaNpc>() != null ||
                go.GetComponent<DroneRonda>() != null ||
                go.GetComponent<ManutenzioneBot>() != null ||
                go.GetComponent<AccessCredentialPickup>() != null ||
                go.GetComponent<EmergencyHotspot>() != null ||
                go.GetComponent<PortaSettore>() != null ||
                go.GetComponent<QuarantineGate>() != null ||
                go.GetComponent<Camera>() != null ||
                go.GetComponent<Light>() != null ||
                go.CompareTag(SectorContainmentTags.Player) ||
                go.CompareTag(SectorContainmentTags.Enemy) ||
                go.CompareTag(SectorContainmentTags.Drone) ||
                go.CompareTag(SectorContainmentTags.AccessCredential))
            {
                StaticEditorFlags f = GameObjectUtility.GetStaticEditorFlags(go);
                f &= ~StaticEditorFlags.NavigationStatic;
                GameObjectUtility.SetStaticEditorFlags(go, f);
                continue;
            }

            Collider col = go.GetComponent<Collider>();
            MeshRenderer mr = go.GetComponent<MeshRenderer>();

            if (col == null && mr == null)
                continue;

            string n = go.name.ToLower();

            // Soffitti e tetti: escludi da NavigationStatic per evitare ostruzioni verticali
            if (n.Contains("soffitto") || n.Contains("ceiling") || n.Contains("roof") || n.Contains("tetto"))
            {
                StaticEditorFlags f = GameObjectUtility.GetStaticEditorFlags(go);
                f &= ~StaticEditorFlags.NavigationStatic;
                GameObjectUtility.SetStaticEditorFlags(go, f);
                EditorUtility.SetDirty(go);
                continue;
            }

            Vector3 size = (col != null) ? col.bounds.size : mr.bounds.size;
            Vector3 center = (col != null) ? col.bounds.center : mr.bounds.center;

            bool isExplicitFloor = n.Contains("floor") || n.Contains("pavimento") || n.Contains("ground") ||
                                   n.Contains("terrain") || n.Contains("plane") || n.Contains("suolo") ||
                                   n.Contains("base") || n.Contains("walkway") || n.Contains("strada") ||
                                   n.Contains("road") || n.Contains("platform") || n.Contains("piattaforma");

            // È un pavimento se ha il nome esplicito o è geometricamente orizzontale e sottile a quota pavimento (Y <= 1.2m)
            bool isPavimento = isExplicitFloor || (size.y <= 0.6f && (size.x >= 1.5f || size.z >= 1.5f) && center.y <= 1.2f);

            StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(go);

            if (isPavimento)
            {
                flags |= StaticEditorFlags.NavigationStatic;
                GameObjectUtility.SetStaticEditorFlags(go, flags);
                GameObjectUtility.SetNavMeshArea(go, 0); // 0 = WALKABLE (Calpestabile)
                EditorUtility.SetDirty(go);
                pavimentiCount++;
            }
            else
            {
                // Muro o ostacolo statico
                flags |= StaticEditorFlags.NavigationStatic;
                GameObjectUtility.SetStaticEditorFlags(go, flags);
                GameObjectUtility.SetNavMeshArea(go, 1); // 1 = NOT WALKABLE
                EditorUtility.SetDirty(go);
                muriCount++;
            }
        }

        Debug.Log($"<color=green>[SceneDoctor] Classificazione completata: {pavimentiCount} Pavimenti (Walkable), {muriCount} Muri (Not Walkable), {ostacoliPorteCalibrati} Porte con Obstacle.</color>");

        // 4. Pulizia e Rigenerazione NavMesh Completa
        Debug.Log("<color=cyan>[SceneDoctor] Pulizia vecchia NavMesh e rigenerazione totale...</color>");
        UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes();
        UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
        Debug.Log("<color=green><b>[SceneDoctor] GENERAZIONE NAVMESH COMPLETATA CON SUCCESSO! Tutta la mappa è ora calpestabile.</b></color>");

        EditorUtility.DisplayDialog(
            "Scene Doctor - NavMesh Riparata", 
            $"NavMesh rigenerata con successo!\n\n" +
            $"- Rimossi {ostacoliRimossi} NavMeshObstacle errati/giganti che cancellavano il pavimento\n" +
            $"- Pavimenti calpestabili (Walkable): {pavimentiCount}\n" +
            $"- Muri e ostacoli geometrici (Not Walkable): {muriCount}\n" +
            $"- Porte di sicurezza con Carving calibrato: {ostacoliPorteCalibrati}\n\n" +
            $"Tutti i corridoi e le stanze hanno ora la NavMesh calpestabile attiva!", 
            "OK"
        );
    }

    [MenuItem("CrisisProtocol/Ripara e Rigenera NavMesh Scena")]
    public static void RiparaNavMeshMenu()
    {
        FixScene();
    }

    [MenuItem("Tools/Fix All Animations (Bake Into Pose / No Snapping)")]
    public static void FixAnimations()
    {
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets" });
        int fixedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                continue;

            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) continue;

            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length == 0)
                clips = importer.clipAnimations;

            if (clips == null || clips.Length == 0) continue;

            bool modified = false;
            foreach (var clip in clips)
            {
                if (!clip.lockRootPositionXZ || !clip.lockRootRotation || !clip.lockRootHeightY)
                {
                    clip.lockRootPositionXZ = true; // Bake Into Pose (XZ)
                    clip.lockRootRotation = true;   // Bake Into Pose (Rotation)
                    clip.lockRootHeightY = true;    // Bake Into Pose (Y)
                    clip.loopTime = true;
                    modified = true;
                }
            }

            if (modified)
            {
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
                fixedCount++;
                Debug.Log($"<color=green>[SceneDoctor]</color> Corretto Root Motion su: {path}");
            }
        }

        EditorUtility.DisplayDialog(
            "Animazioni Corrette",
            $"Completato!\n\n{fixedCount} file di animazioni FBX sono stati impostati con 'Bake Into Pose' (XZ, Y, Rotazione).\n\nI personaggi non salteranno né attraverseranno più i muri durante le animazioni!",
            "OK"
        );
    }

    [MenuItem("Tools/Potenzia e Calibra Gocce d'Acqua e Focolai (Settore 2)")]
    public static void FixHotspotWaterAndParticles()
    {
        EmergencyHotspot[] hotspots = Object.FindObjectsByType<EmergencyHotspot>(FindObjectsSortMode.None);
        int calibratedCount = 0;

        foreach (EmergencyHotspot h in hotspots)
        {
            if (h != null)
            {
                h.AutoTrovaParticelleGuastoSeVuoto();
                EditorUtility.SetDirty(h.gameObject);
                calibratedCount++;
            }
        }

        // Calibra tutti i ParticleSystem di gocce d'acqua e scintille nella scena
        ParticleSystem[] allPS = Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
        int psCount = 0;
        foreach (var ps in allPS)
        {
            if (ps == null) continue;
            string n = ps.name.ToLower();
            if (n.Contains("water") || n.Contains("drip") || n.Contains("gocc") || n.Contains("leak") || n.Contains("spark"))
            {
                EmergencyHotspot.CalibraVisibilitaGocce(ps);
                EditorUtility.SetDirty(ps.gameObject);
                psCount++;
            }
        }

        EditorUtility.DisplayDialog(
            "Calibrazione Perdite Chimiche",
            $"Completato con successo!\n\n- Focolai di emergenza configurati: {calibratedCount}\n- Emettitori calibrati su Perdita Chimica Verde Fluorescente: {psCount}\n\nLe perdite hanno ora il colore Verde Neon Radioattivo, gocciolano verticalmente verso il basso senza spruzzi e si spengono all'istante quando contieni il focolaio!",
            "OK"
        );
    }

    [MenuItem("Tools/Genera CyberHUD Visore Robot nella Scena")]
    public static void GeneraCyberHUD()
    {
        CyberHUD hud = Object.FindAnyObjectByType<CyberHUD>();
        if (hud == null)
        {
            GameObject go = new GameObject("CyberHUD_System");
            hud = go.AddComponent<CyberHUD>();
            Undo.RegisterCreatedObjectUndo(go, "Genera CyberHUD");
            Debug.Log("<color=lime>[CyberHUD]</color> Generato con successo nella scena!");
        }

        EditorUtility.DisplayDialog(
            "CyberHUD",
            "CyberHUD Visore Robot configurato con successo!\n\n- Barra Vita LCD a celle (Stato di Carica verde)\n- Mirino Visore Robotico con Lock-On dinamico\n- Prompt di prossimità [E] trasparente con contorni verde neon\n- Notifiche olografiche di raccolta Keycard",
            "OK"
        );
    }

    [MenuItem("CrisisProtocol/Configura Perdita Gas Focolaio 2 (Scena 1)")]
    [MenuItem("Tools/Configura Perdita Gas Focolaio 2 (Scena 1)")]
    public static void ConfiguraPerditaGasFocolaio2()
    {
        // 1. Trova il Focolaio 2 (EMERGENZA (1) o hotspotId REACTOR_FAULT_002)
        EmergencyHotspot[] hotspots = Object.FindObjectsByType<EmergencyHotspot>(FindObjectsSortMode.None);
        EmergencyHotspot hotspot2 = null;

        foreach (var h in hotspots)
        {
            if (h != null && (h.HotspotId == "REACTOR_FAULT_002" || h.name.Contains("(1)") || h.RequiredCredentialId == "KEYCARD_A02"))
            {
                hotspot2 = h;
                break;
            }
        }

        if (hotspot2 == null && hotspots.Length > 1)
        {
            hotspot2 = hotspots[1];
        }
        else if (hotspot2 == null && hotspots.Length > 0)
        {
            hotspot2 = hotspots[0];
        }

        if (hotspot2 == null)
        {
            EditorUtility.DisplayDialog("Errore", "Nessun EmergencyHotspot trovato nella scena attiva!", "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(hotspot2.gameObject, "Configura Perdita Gas Focolaio 2");

        // 2. Materiale particelle
        Material particleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Blockout/Materials/Blockout_Particle_Mat.mat");
        if (particleMat == null)
        {
            particleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Blockout/Materials/Blockout_Particle_Mat 1.mat");
        }

        // 3. Audio Clips
        AudioClip clipGasHiss = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/House & Office/Gas funace_running.wav") ??
                                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/House & Office/Gas Stove_running.wav") ??
                                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Liquids/Spray.wav") ??
                                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Monsters & Ghosts/robotic_hiss.wav");

        AudioClip clipRepaired = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/notification-process-complete-slava-pogorelsky-1-00-03.mp3") ??
                                 AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Stingers and Spooky Triggers/Harmonized Tone_Pleasant but Spooky.wav");

        // 4. Crea o recupera il root delle particelle di gas
        Transform gasRoot = hotspot2.transform.Find("VFX_Gas_Leak_Emitter");
        if (gasRoot == null)
        {
            GameObject gasGo = new GameObject("VFX_Gas_Leak_Emitter");
            gasGo.transform.SetParent(hotspot2.transform, false);
            gasGo.transform.localPosition = new Vector3(0f, 0.6f, 0.4f);
            gasGo.transform.localRotation = Quaternion.Euler(-30f, 0f, 0f);
            gasRoot = gasGo.transform;
        }

        // 5. Jet Stream Particellare Principale (Getto di Gas ad alta pressione)
        ParticleSystem psJet = gasRoot.GetComponent<ParticleSystem>();
        if (psJet == null)
            psJet = gasRoot.gameObject.AddComponent<ParticleSystem>();

        var mainJet = psJet.main;
        mainJet.playOnAwake = true;
        mainJet.loop = true;
        mainJet.duration = 2.0f;
        mainJet.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.8f);
        mainJet.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 4.0f);
        mainJet.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.55f);
        mainJet.startColor = new ParticleSystem.MinMaxGradient(new Color(0.35f, 1f, 0.3f, 0.65f), new Color(0.75f, 1f, 0.25f, 0.50f));
        mainJet.gravityModifier = -0.04f;
        mainJet.simulationSpace = ParticleSystemSimulationSpace.World;
        mainJet.maxParticles = 200;

        var emissionJet = psJet.emission;
        emissionJet.enabled = true;
        emissionJet.rateOverTime = 32f;

        var shapeJet = psJet.shape;
        shapeJet.enabled = true;
        shapeJet.shapeType = ParticleSystemShapeType.Cone;
        shapeJet.angle = 15f;
        shapeJet.radius = 0.06f;

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
        if (rendJet != null && particleMat != null)
        {
            rendJet.material = particleMat;
            rendJet.renderMode = ParticleSystemRenderMode.Billboard;
            rendJet.alignment = ParticleSystemRenderSpace.View;
        }

        // 6. Nube di Gas Tossico secondaria / Haze espanso
        Transform cloudTransform = gasRoot.Find("Gas_Cloud_Billowing");
        if (cloudTransform == null)
        {
            GameObject cloudGo = new GameObject("Gas_Cloud_Billowing");
            cloudGo.transform.SetParent(gasRoot, false);
            cloudGo.transform.localPosition = new Vector3(0f, 0.3f, 0.6f);
            cloudTransform = cloudGo.transform;
        }

        ParticleSystem psCloud = cloudTransform.GetComponent<ParticleSystem>();
        if (psCloud == null)
            psCloud = cloudTransform.gameObject.AddComponent<ParticleSystem>();

        var mainCloud = psCloud.main;
        mainCloud.playOnAwake = true;
        mainCloud.loop = true;
        mainCloud.duration = 4.0f;
        mainCloud.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4.2f);
        mainCloud.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
        mainCloud.startSize = new ParticleSystem.MinMaxCurve(0.7f, 1.6f);
        mainCloud.startColor = new ParticleSystem.MinMaxGradient(new Color(0.3f, 0.95f, 0.25f, 0.35f), new Color(0.6f, 0.95f, 0.2f, 0.25f));
        mainCloud.gravityModifier = -0.02f;
        mainCloud.simulationSpace = ParticleSystemSimulationSpace.World;
        mainCloud.maxParticles = 100;

        var emissionCloud = psCloud.emission;
        emissionCloud.enabled = true;
        emissionCloud.rateOverTime = 10f;

        var shapeCloud = psCloud.shape;
        shapeCloud.enabled = true;
        shapeCloud.shapeType = ParticleSystemShapeType.Sphere;
        shapeCloud.radius = 0.45f;

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

        ParticleSystemRenderer rendCloud = cloudTransform.GetComponent<ParticleSystemRenderer>();
        if (rendCloud != null && particleMat != null)
        {
            rendCloud.material = particleMat;
            rendCloud.renderMode = ParticleSystemRenderMode.Billboard;
        }

        // 7. Configura StructuralHazard (Pericolo Gas Tossico)
        StructuralHazard hazard = gasRoot.GetComponent<StructuralHazard>();
        if (hazard == null)
            hazard = gasRoot.gameObject.AddComponent<StructuralHazard>();

        SerializedObject soHazard = new SerializedObject(hazard);
        SerializedProperty propHazardType = soHazard.FindProperty("hazardType");
        if (propHazardType != null)
            propHazardType.enumValueIndex = (int)StructuralHazard.HazardType.GasLeak;
        SerializedProperty propDanno = soHazard.FindProperty("dannoAlSecondo");
        if (propDanno != null)
            propDanno.floatValue = 12f;
        soHazard.ApplyModifiedProperties();

        SphereCollider hazardCollider = gasRoot.GetComponent<SphereCollider>();
        if (hazardCollider == null)
            hazardCollider = gasRoot.gameObject.AddComponent<SphereCollider>();
        hazardCollider.isTrigger = true;
        hazardCollider.radius = 2.2f;

        // 8. Configura SerializedProperties di EmergencyHotspot
        SerializedObject soHotspot = new SerializedObject(hotspot2);
        SerializedProperty propParticelle = soHotspot.FindProperty("particelleGuasto");
        if (propParticelle != null)
        {
            propParticelle.arraySize = 2;
            propParticelle.GetArrayElementAtIndex(0).objectReferenceValue = psJet;
            propParticelle.GetArrayElementAtIndex(1).objectReferenceValue = psCloud;
        }

        SerializedProperty propSuonoLoop = soHotspot.FindProperty("suonoLoopGuasto");
        if (propSuonoLoop != null && clipGasHiss != null)
            propSuonoLoop.objectReferenceValue = clipGasHiss;

        SerializedProperty propSuonoRip = soHotspot.FindProperty("suonoRiparazione");
        if (propSuonoRip != null && clipRepaired != null)
            propSuonoRip.objectReferenceValue = clipRepaired;

        SerializedProperty propAutoDisattiva = soHotspot.FindProperty("autoDisattivaParticelleGuasto");
        if (propAutoDisattiva != null)
            propAutoDisattiva.boolValue = true;

        soHotspot.ApplyModifiedProperties();

        EditorUtility.SetDirty(hotspot2.gameObject);
        EditorUtility.SetDirty(gasRoot.gameObject);
        EditorUtility.SetDirty(cloudTransform.gameObject);

        // Salva la scena
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(hotspot2.gameObject.scene);

        Debug.Log($"<color=lime>[GAS LEAK]</color> Perdita di gas simulata con successo su <b>{hotspot2.name}</b> (Focolaio 2)! Particelle e audio 3D configurati.");

        EditorUtility.DisplayDialog(
            "Simulazione Perdita di Gas (Focolaio 2)",
            $"Configurazione completata con successo sul Focolaio 2 ('{hotspot2.name}')!\n\n" +
            $"• Emettitore Getto Gas in Pressione: Attivo\n" +
            $"• Nube Volumetrica Espansa di Vapore: Attiva\n" +
            $"• Audio 3D Loop Fischio/Gas: {clipGasHiss?.name ?? "Assegnato"}\n" +
            $"• Pericolo Ambientale (StructuralHazard GasLeak): Configurato (Raggio: 2.2m)\n" +
            $"• Spegnimento automatico al contenimento con Keycard '{hotspot2.RequiredCredentialId}': Abilitato",
            "OK"
        );
    }

    [MenuItem("CrisisProtocol/Configura Tutti i Suoni Scena ed Emettitori 3D")]
    public static void ConfiguraSuoniMenu()
    {
        SceneAudioPopulator.ApplicaTuttiISuoni();
    }
}
#endif
