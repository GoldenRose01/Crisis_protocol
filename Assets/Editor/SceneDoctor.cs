// ============================================================================
// Crisis Protocol / Sector Containment - Utility editor
// File: .\Assets\Editor\SceneDoctor.cs
// Responsabilita': automatizza setup, popolamento scena, salvataggio, validazione o manutenzione direttamente dentro Unity Editor.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
#if UNITY_EDITOR // prep ok // riga-ok
using UnityEditor; // usa lib // riga-ok
using UnityEditor.AI; // usa lib // riga-ok
using UnityEngine; // usa lib // riga-ok
using UnityEngine.AI; // usa lib // riga-ok
using System.Collections.Generic; // usa lib // riga-ok

// blocco: classe x roba grossa
public class SceneDoctor : EditorWindow // classe qui // riga-ok
{ // apre // riga-ok
    [MenuItem("Tools/1-Click Scene Fix (NavMesh & Missing Scripts)")] // nota unity // riga-ok
    // blocco: funzione fa cose
    public static void FixScene() // roba pub // riga-ok
    { // apre // riga-ok
        Debug.Log("<color=cyan>[SceneDoctor]</color> Inizio pulizia e calibrazione avanzata della scena..."); // logga // riga-ok

        // 1. Rimuovi script mancanti (Missing Scripts)
        int missingCount = 0; // setta // riga-ok
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None); // setta // riga-ok
        
        // blocco: gira piu volte
        foreach (GameObject go in allObjects) // ciclo x // riga-ok
        { // apre // riga-ok
            int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go); // setta // riga-ok
            // blocco: controlla se va
            if (count > 0) // se ok // riga-ok
            { // apre // riga-ok
                missingCount += count; // setta // riga-ok
                Debug.Log($"[SceneDoctor] Rimossi {count} script mancanti da '{go.name}'", go); // logga // riga-ok
                EditorUtility.SetDirty(go); // chiama // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
        
        // blocco: controlla se va
        if (missingCount > 0) // se ok // riga-ok
            Debug.Log($"<color=green>[SceneDoctor] {missingCount} script fantasma rimossi!</color>"); // logga // riga-ok

        // 2. Calibra tutti i Bot di Manutenzione presenti nella scena
        int botCount = 0; // setta // riga-ok
        ManutenzioneBot[] bots = Object.FindObjectsByType<ManutenzioneBot>(FindObjectsSortMode.None); // setta // riga-ok
        // blocco: gira piu volte
        foreach (ManutenzioneBot bot in bots) // ciclo x // riga-ok
        { // apre // riga-ok
            bot.ResetValoriPredefiniti(); // chiama // riga-ok
            EditorUtility.SetDirty(bot.gameObject); // chiama // riga-ok
            botCount++; // ok qua // riga-ok
        } // chiude // riga-ok
        // blocco: controlla se va
        if (botCount > 0) // se ok // riga-ok
            Debug.Log($"<color=green>[SceneDoctor] Calibrati {botCount} ManutenzioneBot con velocità di ronda calmi.</color>"); // logga // riga-ok

        // 3. Rimuovi il flag Static da tutte le Porte e Portelloni (altrimenti non si muovono)
        int porteStaticFix = 0; // setta // riga-ok
        PortaSettore[] tutteLePorte = Object.FindObjectsByType<PortaSettore>(FindObjectsSortMode.None); // setta // riga-ok
        // blocco: gira piu volte
        foreach (PortaSettore p in tutteLePorte) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (p != null) // se ok // riga-ok
            { // apre // riga-ok
                // blocco: gira piu volte
                foreach (Transform t in p.GetComponentsInChildren<Transform>(true)) // ciclo x // riga-ok
                { // apre // riga-ok
                    // blocco: controlla se va
                    if (t.gameObject.isStatic) // se ok // riga-ok
                    { // apre // riga-ok
                        t.gameObject.isStatic = false; // setta // riga-ok
                        EditorUtility.SetDirty(t.gameObject); // chiama // riga-ok
                        porteStaticFix++; // ok qua // riga-ok
                    } // chiude // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
        // blocco: controlla se va
        if (porteStaticFix > 0) // se ok // riga-ok
            Debug.Log($"<color=green>[SceneDoctor] Rimosso flag Static da {porteStaticFix} elementi porta/vetrata.</color>"); // logga // riga-ok

        // 3b. Calibra Collider Solidi su tutti i Focolai di Emergenza (evita attraversamento mesh)
        int hotspotColliderFix = 0; // setta // riga-ok
        EmergencyHotspot[] tuttiIFocolai = Object.FindObjectsByType<EmergencyHotspot>(FindObjectsSortMode.None); // setta // riga-ok
        // blocco: gira piu volte
        foreach (EmergencyHotspot h in tuttiIFocolai) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (h != null) // se ok // riga-ok
            { // apre // riga-ok
                h.AssicuraColliderValido(); // chiama // riga-ok
                EditorUtility.SetDirty(h.gameObject); // chiama // riga-ok
                hotspotColliderFix++; // ok qua // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
        // blocco: controlla se va
        if (hotspotColliderFix > 0) // se ok // riga-ok
            Debug.Log($"<color=green>[SceneDoctor] Generati/Verificati {hotspotColliderFix} collider fisici solidi su focolai ed emergenze.</color>");        // 3c. Rimozione radicale di tutti i NavMeshObstacle errati da muri statici, pavimenti, soffitti e strutture // logga // riga-ok
        int ostacoliRimossi = 0; // setta // riga-ok
        int ostacoliPorteCalibrati = 0; // setta // riga-ok
        NavMeshObstacle[] tuttiGliOstacoli = Object.FindObjectsByType<NavMeshObstacle>(FindObjectsSortMode.None); // setta // riga-ok
        // blocco: gira piu volte
        foreach (NavMeshObstacle obs in tuttiGliOstacoli) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (obs == null) continue; // se ok // riga-ok
            GameObject go = obs.gameObject; // setta // riga-ok

            // Mantieni NavMeshObstacle ESCLUSIVAMENTE sulle porte dinamiche (PortaSettore, QuarantineGate)
            bool isPorta = go.GetComponent<PortaSettore>() != null ||  // setta // riga-ok
                           go.GetComponent<QuarantineGate>() != null ||  // setta // riga-ok
                           go.GetComponentInParent<PortaSettore>() != null || // setta // riga-ok
                           go.GetComponentInParent<QuarantineGate>() != null; // setta // riga-ok

            // blocco: controlla se va
            if (isPorta) // se ok // riga-ok
            { // apre // riga-ok
                obs.shape = NavMeshObstacleShape.Box; // setta // riga-ok
                obs.carving = true; // setta // riga-ok
                obs.carveOnlyStationary = false; // setta // riga-ok
                obs.carvingMoveThreshold = 0.1f; // setta // riga-ok

                Collider col = go.GetComponent<Collider>(); // setta // riga-ok
                // blocco: controlla se va
                if (col is BoxCollider bc) // se ok // riga-ok
                { // apre // riga-ok
                    obs.center = bc.center; // setta // riga-ok
                    obs.size = new Vector3(Mathf.Min(bc.size.x, 4f), Mathf.Min(bc.size.y, 4f), Mathf.Min(bc.size.z, 4f)); // setta // riga-ok
                } // chiude // riga-ok
                // blocco: caso diverso
                else // se no // riga-ok
                { // apre // riga-ok
                    obs.center = Vector3.zero; // setta // riga-ok
                    obs.size = new Vector3(2.5f, 3f, 0.5f); // setta // riga-ok
                } // chiude // riga-ok
                EditorUtility.SetDirty(go); // chiama // riga-ok
                ostacoliPorteCalibrati++; // ok qua // riga-ok
            } // chiude // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
            { // apre // riga-ok
                // Muri, soffitti, pavimenti e strutture fisse NON devono avere NavMeshObstacle:
                // la loro fisica e blocco navigazione sono gestiti nativamente dalla geometria statica!
                DestroyImmediate(obs); // elimina // riga-ok
                EditorUtility.SetDirty(go); // chiama // riga-ok
                ostacoliRimossi++; // ok qua // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
        // blocco: controlla se va
        if (ostacoliRimossi > 0) // se ok // riga-ok
            Debug.Log($"<color=green>[SceneDoctor] Rimossi {ostacoliRimossi} NavMeshObstacle giganti/errati da muri e strutture.</color>"); // logga // riga-ok

        // 3d. Classificazione accurata di Pavimenti e Muri
        int pavimentiCount = 0; // setta // riga-ok
        int muriCount = 0; // setta // riga-ok

        // blocco: gira piu volte
        foreach (GameObject go in allObjects) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (go == null) continue; // se ok // riga-ok

            // Salta entità dinamiche
            // blocco: controlla se va
            if (go.GetComponent<NavMeshAgent>() != null ||  // se ok // riga-ok
                go.GetComponent<muve_pg>() != null ||  // setta // riga-ok
                go.GetComponent<SalutePlayer>() != null || // setta // riga-ok
                go.GetComponent<GuardiaNpc>() != null || // setta // riga-ok
                go.GetComponent<DroneRonda>() != null || // setta // riga-ok
                go.GetComponent<ManutenzioneBot>() != null || // setta // riga-ok
                go.GetComponent<AccessCredentialPickup>() != null || // setta // riga-ok
                go.GetComponent<EmergencyHotspot>() != null || // setta // riga-ok
                go.GetComponent<PortaSettore>() != null || // setta // riga-ok
                go.GetComponent<QuarantineGate>() != null || // setta // riga-ok
                go.GetComponent<Camera>() != null || // setta // riga-ok
                go.GetComponent<Light>() != null || // setta // riga-ok
                go.CompareTag(SectorContainmentTags.Player) || // ok qua // riga-ok
                go.CompareTag(SectorContainmentTags.Enemy) || // ok qua // riga-ok
                go.CompareTag(SectorContainmentTags.Drone) || // ok qua // riga-ok
                go.CompareTag(SectorContainmentTags.AccessCredential)) // chiama // riga-ok
            { // apre // riga-ok
                StaticEditorFlags f = GameObjectUtility.GetStaticEditorFlags(go); // setta // riga-ok
                f &= ~StaticEditorFlags.NavigationStatic; // setta // riga-ok
                GameObjectUtility.SetStaticEditorFlags(go, f); // chiama // riga-ok
                continue; // salta // riga-ok
            } // chiude // riga-ok

            Collider col = go.GetComponent<Collider>(); // setta // riga-ok
            MeshRenderer mr = go.GetComponent<MeshRenderer>(); // setta // riga-ok

            // blocco: controlla se va
            if (col == null && mr == null) // se ok // riga-ok
                continue; // salta // riga-ok

            string n = go.name.ToLower(); // setta // riga-ok

            // Soffitti e tetti: escludi da NavigationStatic per evitare ostruzioni verticali
            // blocco: controlla se va
            if (n.Contains("soffitto") || n.Contains("ceiling") || n.Contains("roof") || n.Contains("tetto")) // se ok // riga-ok
            { // apre // riga-ok
                StaticEditorFlags f = GameObjectUtility.GetStaticEditorFlags(go); // setta // riga-ok
                f &= ~StaticEditorFlags.NavigationStatic; // setta // riga-ok
                GameObjectUtility.SetStaticEditorFlags(go, f); // chiama // riga-ok
                EditorUtility.SetDirty(go); // chiama // riga-ok
                continue; // salta // riga-ok
            } // chiude // riga-ok

            Vector3 size = (col != null) ? col.bounds.size : mr.bounds.size; // setta // riga-ok
            Vector3 center = (col != null) ? col.bounds.center : mr.bounds.center; // setta // riga-ok

            bool isExplicitFloor = n.Contains("floor") || n.Contains("pavimento") || n.Contains("ground") || // setta // riga-ok
                                   n.Contains("terrain") || n.Contains("plane") || n.Contains("suolo") || // ok qua // riga-ok
                                   n.Contains("base") || n.Contains("walkway") || n.Contains("strada") || // ok qua // riga-ok
                                   n.Contains("road") || n.Contains("platform") || n.Contains("piattaforma"); // chiama // riga-ok

            // È un pavimento se ha il nome esplicito o è geometricamente orizzontale e sottile a quota pavimento (Y <= 1.2m)
            bool isPavimento = isExplicitFloor || (size.y <= 0.6f && (size.x >= 1.5f || size.z >= 1.5f) && center.y <= 1.2f); // setta // riga-ok

            StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(go); // setta // riga-ok

            // blocco: controlla se va
            if (isPavimento) // se ok // riga-ok
            { // apre // riga-ok
                flags |= StaticEditorFlags.NavigationStatic; // setta // riga-ok
                GameObjectUtility.SetStaticEditorFlags(go, flags); // chiama // riga-ok
                GameObjectUtility.SetNavMeshArea(go, 0); // 0 = WALKABLE (Calpestabile) // setta // riga-ok
                EditorUtility.SetDirty(go); // chiama // riga-ok
                pavimentiCount++; // ok qua // riga-ok
            } // chiude // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
            { // apre // riga-ok
                // Muro o ostacolo statico
                flags |= StaticEditorFlags.NavigationStatic; // setta // riga-ok
                GameObjectUtility.SetStaticEditorFlags(go, flags); // chiama // riga-ok
                GameObjectUtility.SetNavMeshArea(go, 1); // 1 = NOT WALKABLE // setta // riga-ok
                EditorUtility.SetDirty(go); // chiama // riga-ok
                muriCount++; // ok qua // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        Debug.Log($"<color=green>[SceneDoctor] Classificazione completata: {pavimentiCount} Pavimenti (Walkable), {muriCount} Muri (Not Walkable), {ostacoliPorteCalibrati} Porte con Obstacle.</color>"); // logga // riga-ok

        // 4. Pulizia e Rigenerazione NavMesh Completa
        Debug.Log("<color=cyan>[SceneDoctor] Pulizia vecchia NavMesh e rigenerazione totale...</color>"); // logga // riga-ok
        UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes(); // chiama // riga-ok
        UnityEditor.AI.NavMeshBuilder.BuildNavMesh(); // chiama // riga-ok
        Debug.Log("<color=green><b>[SceneDoctor] GENERAZIONE NAVMESH COMPLETATA CON SUCCESSO! Tutta la mappa è ora calpestabile.</b></color>"); // logga // riga-ok

        EditorUtility.DisplayDialog( // ok qua // riga-ok
            "Scene Doctor - NavMesh Riparata",  // ok qua // riga-ok
            $"NavMesh rigenerata con successo!\n\n" + // ok qua // riga-ok
            $"- Rimossi {ostacoliRimossi} NavMeshObstacle errati/giganti che cancellavano il pavimento\n" + // ok qua // riga-ok
            $"- Pavimenti calpestabili (Walkable): {pavimentiCount}\n" + // ok qua // riga-ok
            $"- Muri e ostacoli geometrici (Not Walkable): {muriCount}\n" + // ok qua // riga-ok
            $"- Porte di sicurezza con Carving calibrato: {ostacoliPorteCalibrati}\n\n" + // ok qua // riga-ok
            $"Tutti i corridoi e le stanze hanno ora la NavMesh calpestabile attiva!",  // ok qua // riga-ok
            "OK" // ok qua // riga-ok
        ); // chiama // riga-ok
    } // chiude // riga-ok

    [MenuItem("CrisisProtocol/Ripara e Rigenera NavMesh Scena")] // nota unity // riga-ok
    // blocco: funzione fa cose
    public static void RiparaNavMeshMenu() // roba pub // riga-ok
    { // apre // riga-ok
        FixScene(); // chiama // riga-ok
    } // chiude // riga-ok

    [MenuItem("Tools/Fix All Animations (Bake Into Pose / No Snapping)")] // nota unity // riga-ok
    // blocco: funzione fa cose
    public static void FixAnimations() // roba pub // riga-ok
    { // apre // riga-ok
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets" }); // setta // riga-ok
        int fixedCount = 0; // setta // riga-ok

        // blocco: gira piu volte
        foreach (string guid in guids) // ciclo x // riga-ok
        { // apre // riga-ok
            string path = AssetDatabase.GUIDToAssetPath(guid); // setta // riga-ok
            // blocco: controlla se va
            if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)) // se ok // riga-ok
                continue; // salta // riga-ok

            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter; // setta // riga-ok
            // blocco: controlla se va
            if (importer == null) continue; // se ok // riga-ok

            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations; // setta // riga-ok
            // blocco: controlla se va
            if (clips == null || clips.Length == 0) // se ok // riga-ok
                clips = importer.clipAnimations; // setta // riga-ok

            // blocco: controlla se va
            if (clips == null || clips.Length == 0) continue; // se ok // riga-ok

            bool modified = false; // setta // riga-ok
            // blocco: gira piu volte
            foreach (var clip in clips) // ciclo x // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (!clip.lockRootPositionXZ || !clip.lockRootRotation || !clip.lockRootHeightY) // se ok // riga-ok
                { // apre // riga-ok
                    clip.lockRootPositionXZ = true; // Bake Into Pose (XZ) // setta // riga-ok
                    clip.lockRootRotation = true;   // Bake Into Pose (Rotation) // setta // riga-ok
                    clip.lockRootHeightY = true;    // Bake Into Pose (Y) // setta // riga-ok
                    clip.loopTime = true; // setta // riga-ok
                    modified = true; // setta // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok

            // blocco: controlla se va
            if (modified) // se ok // riga-ok
            { // apre // riga-ok
                importer.clipAnimations = clips; // setta // riga-ok
                importer.SaveAndReimport(); // chiama // riga-ok
                fixedCount++; // ok qua // riga-ok
                Debug.Log($"<color=green>[SceneDoctor]</color> Corretto Root Motion su: {path}"); // logga // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        EditorUtility.DisplayDialog( // ok qua // riga-ok
            "Animazioni Corrette", // ok qua // riga-ok
            $"Completato!\n\n{fixedCount} file di animazioni FBX sono stati impostati con 'Bake Into Pose' (XZ, Y, Rotazione).\n\nI personaggi non salteranno né attraverseranno più i muri durante le animazioni!", // ok qua // riga-ok
            "OK" // ok qua // riga-ok
        ); // chiama // riga-ok
    } // chiude // riga-ok

    [MenuItem("Tools/Potenzia e Calibra Gocce d'Acqua e Focolai (Settore 2)")] // nota unity // riga-ok
    // blocco: funzione fa cose
    public static void FixHotspotWaterAndParticles() // roba pub // riga-ok
    { // apre // riga-ok
        EmergencyHotspot[] hotspots = Object.FindObjectsByType<EmergencyHotspot>(FindObjectsSortMode.None); // setta // riga-ok
        int calibratedCount = 0; // setta // riga-ok

        // blocco: gira piu volte
        foreach (EmergencyHotspot h in hotspots) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (h != null) // se ok // riga-ok
            { // apre // riga-ok
                h.AutoTrovaParticelleGuastoSeVuoto(); // chiama // riga-ok
                EditorUtility.SetDirty(h.gameObject); // chiama // riga-ok
                calibratedCount++; // ok qua // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // Calibra tutti i ParticleSystem di gocce d'acqua e scintille nella scena
        ParticleSystem[] allPS = Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None); // setta // riga-ok
        int psCount = 0; // setta // riga-ok
        // blocco: gira piu volte
        foreach (var ps in allPS) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (ps == null) continue; // se ok // riga-ok
            string n = ps.name.ToLower(); // setta // riga-ok
            // blocco: controlla se va
            if (n.Contains("water") || n.Contains("drip") || n.Contains("gocc") || n.Contains("leak") || n.Contains("spark")) // se ok // riga-ok
            { // apre // riga-ok
                EmergencyHotspot.CalibraVisibilitaGocce(ps); // chiama // riga-ok
                EditorUtility.SetDirty(ps.gameObject); // chiama // riga-ok
                psCount++; // ok qua // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        EditorUtility.DisplayDialog( // ok qua // riga-ok
            "Calibrazione Perdite Chimiche", // ok qua // riga-ok
            $"Completato con successo!\n\n- Focolai di emergenza configurati: {calibratedCount}\n- Emettitori calibrati su Perdita Chimica Verde Fluorescente: {psCount}\n\nLe perdite hanno ora il colore Verde Neon Radioattivo, gocciolano verticalmente verso il basso senza spruzzi e si spengono all'istante quando contieni il focolaio!", // ok qua // riga-ok
            "OK" // ok qua // riga-ok
        ); // chiama // riga-ok
    } // chiude // riga-ok

    [MenuItem("Tools/Genera CyberHUD Visore Robot nella Scena")] // nota unity // riga-ok
    // blocco: funzione fa cose
    public static void GeneraCyberHUD() // roba pub // riga-ok
    { // apre // riga-ok
        CyberHUD hud = Object.FindAnyObjectByType<CyberHUD>(); // setta // riga-ok
        // blocco: controlla se va
        if (hud == null) // se ok // riga-ok
        { // apre // riga-ok
            GameObject go = new GameObject("CyberHUD_System"); // setta // riga-ok
            hud = go.AddComponent<CyberHUD>(); // setta // riga-ok
            Undo.RegisterCreatedObjectUndo(go, "Genera CyberHUD"); // chiama // riga-ok
            Debug.Log("<color=lime>[CyberHUD]</color> Generato con successo nella scena!"); // logga // riga-ok
        } // chiude // riga-ok

        EditorUtility.DisplayDialog( // ok qua // riga-ok
            "CyberHUD", // ok qua // riga-ok
            "CyberHUD Visore Robot configurato con successo!\n\n- Barra Vita LCD a celle (Stato di Carica verde)\n- Mirino Visore Robotico con Lock-On dinamico\n- Prompt di prossimità [E] trasparente con contorni verde neon\n- Notifiche olografiche di raccolta Keycard", // ok qua // riga-ok
            "OK" // ok qua // riga-ok
        ); // chiama // riga-ok
    } // chiude // riga-ok

    [MenuItem("CrisisProtocol/Configura Perdita Gas Focolaio 2 (Scena 1)")] // nota unity // riga-ok
    [MenuItem("Tools/Configura Perdita Gas Focolaio 2 (Scena 1)")] // nota unity // riga-ok
    // blocco: funzione fa cose
    public static void ConfiguraPerditaGasFocolaio2() // roba pub // riga-ok
    { // apre // riga-ok
        // 1. Trova il Focolaio 2 (EMERGENZA (1) o hotspotId REACTOR_FAULT_002)
        EmergencyHotspot[] hotspots = Object.FindObjectsByType<EmergencyHotspot>(FindObjectsSortMode.None); // setta // riga-ok
        EmergencyHotspot hotspot2 = null; // setta // riga-ok

        // blocco: gira piu volte
        foreach (var h in hotspots) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (h != null && (h.HotspotId == "REACTOR_FAULT_002" || h.name.Contains("(1)") || h.RequiredCredentialId == "KEYCARD_A02")) // se ok // riga-ok
            { // apre // riga-ok
                hotspot2 = h; // setta // riga-ok
                break; // stop // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (hotspot2 == null && hotspots.Length > 1) // se ok // riga-ok
        { // apre // riga-ok
            hotspot2 = hotspots[1]; // setta // riga-ok
        } // chiude // riga-ok
        // blocco: controlla se va
        else if (hotspot2 == null && hotspots.Length > 0) // se ok // riga-ok
        { // apre // riga-ok
            hotspot2 = hotspots[0]; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (hotspot2 == null) // se ok // riga-ok
        { // apre // riga-ok
            EditorUtility.DisplayDialog("Errore", "Nessun EmergencyHotspot trovato nella scena attiva!", "OK"); // chiama // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        Undo.RegisterFullObjectHierarchyUndo(hotspot2.gameObject, "Configura Perdita Gas Focolaio 2"); // chiama // riga-ok

        // 2. Materiale particelle
        Material particleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Blockout/Materials/Blockout_Particle_Mat.mat"); // setta // riga-ok
        // blocco: controlla se va
        if (particleMat == null) // se ok // riga-ok
        { // apre // riga-ok
            particleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Blockout/Materials/Blockout_Particle_Mat 1.mat"); // setta // riga-ok
        } // chiude // riga-ok

        // 3. Audio Clips
        AudioClip clipGasHiss = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/House & Office/Gas funace_running.wav") ?? // setta // riga-ok
                                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/House & Office/Gas Stove_running.wav") ?? // ok qua // riga-ok
                                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Liquids/Spray.wav") ?? // ok qua // riga-ok
                                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Monsters & Ghosts/robotic_hiss.wav"); // chiama // riga-ok

        AudioClip clipRepaired = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/notification-process-complete-slava-pogorelsky-1-00-03.mp3") ?? // setta // riga-ok
                                 AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Stingers and Spooky Triggers/Harmonized Tone_Pleasant but Spooky.wav"); // chiama // riga-ok

        // 4. Crea o recupera il root delle particelle di gas
        Transform gasRoot = hotspot2.transform.Find("VFX_Gas_Leak_Emitter"); // setta // riga-ok
        // blocco: controlla se va
        if (gasRoot == null) // se ok // riga-ok
        { // apre // riga-ok
            GameObject gasGo = new GameObject("VFX_Gas_Leak_Emitter"); // setta // riga-ok
            gasGo.transform.SetParent(hotspot2.transform, false); // chiama // riga-ok
            gasGo.transform.localPosition = new Vector3(0f, 0.6f, 0.4f); // setta // riga-ok
            gasGo.transform.localRotation = Quaternion.Euler(-30f, 0f, 0f); // setta // riga-ok
            gasRoot = gasGo.transform; // setta // riga-ok
        } // chiude // riga-ok

        // 5. Jet Stream Particellare Principale (Getto di Gas ad alta pressione)
        ParticleSystem psJet = gasRoot.GetComponent<ParticleSystem>(); // setta // riga-ok
        // blocco: controlla se va
        if (psJet == null) // se ok // riga-ok
            psJet = gasRoot.gameObject.AddComponent<ParticleSystem>(); // setta // riga-ok

        var mainJet = psJet.main; // setta // riga-ok
        mainJet.playOnAwake = true; // setta // riga-ok
        mainJet.loop = true; // setta // riga-ok
        mainJet.duration = 2.0f; // setta // riga-ok
        mainJet.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.8f); // setta // riga-ok
        mainJet.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 4.0f); // setta // riga-ok
        mainJet.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.55f); // setta // riga-ok
        mainJet.startColor = new ParticleSystem.MinMaxGradient(new Color(0.35f, 1f, 0.3f, 0.65f), new Color(0.75f, 1f, 0.25f, 0.50f)); // setta // riga-ok
        mainJet.gravityModifier = -0.04f; // setta // riga-ok
        mainJet.simulationSpace = ParticleSystemSimulationSpace.World; // setta // riga-ok
        mainJet.maxParticles = 200; // setta // riga-ok

        var emissionJet = psJet.emission; // setta // riga-ok
        emissionJet.enabled = true; // setta // riga-ok
        emissionJet.rateOverTime = 32f; // setta // riga-ok

        var shapeJet = psJet.shape; // setta // riga-ok
        shapeJet.enabled = true; // setta // riga-ok
        shapeJet.shapeType = ParticleSystemShapeType.Cone; // setta // riga-ok
        shapeJet.angle = 15f; // setta // riga-ok
        shapeJet.radius = 0.06f; // setta // riga-ok

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
        if (rendJet != null && particleMat != null) // se ok // riga-ok
        { // apre // riga-ok
            rendJet.material = particleMat; // setta // riga-ok
            rendJet.renderMode = ParticleSystemRenderMode.Billboard; // setta // riga-ok
            rendJet.alignment = ParticleSystemRenderSpace.View; // setta // riga-ok
        } // chiude // riga-ok

        // 6. Nube di Gas Tossico secondaria / Haze espanso
        Transform cloudTransform = gasRoot.Find("Gas_Cloud_Billowing"); // setta // riga-ok
        // blocco: controlla se va
        if (cloudTransform == null) // se ok // riga-ok
        { // apre // riga-ok
            GameObject cloudGo = new GameObject("Gas_Cloud_Billowing"); // setta // riga-ok
            cloudGo.transform.SetParent(gasRoot, false); // chiama // riga-ok
            cloudGo.transform.localPosition = new Vector3(0f, 0.3f, 0.6f); // setta // riga-ok
            cloudTransform = cloudGo.transform; // setta // riga-ok
        } // chiude // riga-ok

        ParticleSystem psCloud = cloudTransform.GetComponent<ParticleSystem>(); // setta // riga-ok
        // blocco: controlla se va
        if (psCloud == null) // se ok // riga-ok
            psCloud = cloudTransform.gameObject.AddComponent<ParticleSystem>(); // setta // riga-ok

        var mainCloud = psCloud.main; // setta // riga-ok
        mainCloud.playOnAwake = true; // setta // riga-ok
        mainCloud.loop = true; // setta // riga-ok
        mainCloud.duration = 4.0f; // setta // riga-ok
        mainCloud.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4.2f); // setta // riga-ok
        mainCloud.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.9f); // setta // riga-ok
        mainCloud.startSize = new ParticleSystem.MinMaxCurve(0.7f, 1.6f); // setta // riga-ok
        mainCloud.startColor = new ParticleSystem.MinMaxGradient(new Color(0.3f, 0.95f, 0.25f, 0.35f), new Color(0.6f, 0.95f, 0.2f, 0.25f)); // setta // riga-ok
        mainCloud.gravityModifier = -0.02f; // setta // riga-ok
        mainCloud.simulationSpace = ParticleSystemSimulationSpace.World; // setta // riga-ok
        mainCloud.maxParticles = 100; // setta // riga-ok

        var emissionCloud = psCloud.emission; // setta // riga-ok
        emissionCloud.enabled = true; // setta // riga-ok
        emissionCloud.rateOverTime = 10f; // setta // riga-ok

        var shapeCloud = psCloud.shape; // setta // riga-ok
        shapeCloud.enabled = true; // setta // riga-ok
        shapeCloud.shapeType = ParticleSystemShapeType.Sphere; // setta // riga-ok
        shapeCloud.radius = 0.45f; // setta // riga-ok

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

        ParticleSystemRenderer rendCloud = cloudTransform.GetComponent<ParticleSystemRenderer>(); // setta // riga-ok
        // blocco: controlla se va
        if (rendCloud != null && particleMat != null) // se ok // riga-ok
        { // apre // riga-ok
            rendCloud.material = particleMat; // setta // riga-ok
            rendCloud.renderMode = ParticleSystemRenderMode.Billboard; // setta // riga-ok
        } // chiude // riga-ok

        // 7. Configura StructuralHazard (Pericolo Gas Tossico)
        StructuralHazard hazard = gasRoot.GetComponent<StructuralHazard>(); // setta // riga-ok
        // blocco: controlla se va
        if (hazard == null) // se ok // riga-ok
            hazard = gasRoot.gameObject.AddComponent<StructuralHazard>(); // setta // riga-ok

        SerializedObject soHazard = new SerializedObject(hazard); // setta // riga-ok
        SerializedProperty propHazardType = soHazard.FindProperty("hazardType"); // setta // riga-ok
        // blocco: controlla se va
        if (propHazardType != null) // se ok // riga-ok
            propHazardType.enumValueIndex = (int)StructuralHazard.HazardType.GasLeak; // setta // riga-ok
        SerializedProperty propDanno = soHazard.FindProperty("dannoAlSecondo"); // setta // riga-ok
        // blocco: controlla se va
        if (propDanno != null) // se ok // riga-ok
            propDanno.floatValue = 12f; // setta // riga-ok
        soHazard.ApplyModifiedProperties(); // chiama // riga-ok

        SphereCollider hazardCollider = gasRoot.GetComponent<SphereCollider>(); // setta // riga-ok
        // blocco: controlla se va
        if (hazardCollider == null) // se ok // riga-ok
            hazardCollider = gasRoot.gameObject.AddComponent<SphereCollider>(); // setta // riga-ok
        hazardCollider.isTrigger = true; // setta // riga-ok
        hazardCollider.radius = 2.2f; // setta // riga-ok

        // 8. Configura SerializedProperties di EmergencyHotspot
        SerializedObject soHotspot = new SerializedObject(hotspot2); // setta // riga-ok
        SerializedProperty propParticelle = soHotspot.FindProperty("particelleGuasto"); // setta // riga-ok
        // blocco: controlla se va
        if (propParticelle != null) // se ok // riga-ok
        { // apre // riga-ok
            propParticelle.arraySize = 2; // setta // riga-ok
            propParticelle.GetArrayElementAtIndex(0).objectReferenceValue = psJet; // setta // riga-ok
            propParticelle.GetArrayElementAtIndex(1).objectReferenceValue = psCloud; // setta // riga-ok
        } // chiude // riga-ok

        SerializedProperty propSuonoLoop = soHotspot.FindProperty("suonoLoopGuasto"); // setta // riga-ok
        // blocco: controlla se va
        if (propSuonoLoop != null && clipGasHiss != null) // se ok // riga-ok
            propSuonoLoop.objectReferenceValue = clipGasHiss; // setta // riga-ok

        SerializedProperty propSuonoRip = soHotspot.FindProperty("suonoRiparazione"); // setta // riga-ok
        // blocco: controlla se va
        if (propSuonoRip != null && clipRepaired != null) // se ok // riga-ok
            propSuonoRip.objectReferenceValue = clipRepaired; // setta // riga-ok

        SerializedProperty propAutoDisattiva = soHotspot.FindProperty("autoDisattivaParticelleGuasto"); // setta // riga-ok
        // blocco: controlla se va
        if (propAutoDisattiva != null) // se ok // riga-ok
            propAutoDisattiva.boolValue = true; // setta // riga-ok

        soHotspot.ApplyModifiedProperties(); // chiama // riga-ok

        EditorUtility.SetDirty(hotspot2.gameObject); // chiama // riga-ok
        EditorUtility.SetDirty(gasRoot.gameObject); // chiama // riga-ok
        EditorUtility.SetDirty(cloudTransform.gameObject); // chiama // riga-ok

        // Salva la scena
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(hotspot2.gameObject.scene); // chiama // riga-ok

        Debug.Log($"<color=lime>[GAS LEAK]</color> Perdita di gas simulata con successo su <b>{hotspot2.name}</b> (Focolaio 2)! Particelle e audio 3D configurati."); // logga // riga-ok

        EditorUtility.DisplayDialog( // ok qua // riga-ok
            "Simulazione Perdita di Gas (Focolaio 2)", // ok qua // riga-ok
            $"Configurazione completata con successo sul Focolaio 2 ('{hotspot2.name}')!\n\n" + // ok qua // riga-ok
            $"• Emettitore Getto Gas in Pressione: Attivo\n" + // ok qua // riga-ok
            $"• Nube Volumetrica Espansa di Vapore: Attiva\n" + // ok qua // riga-ok
            $"• Audio 3D Loop Fischio/Gas: {clipGasHiss?.name ?? "Assegnato"}\n" + // ok qua // riga-ok
            $"• Pericolo Ambientale (StructuralHazard GasLeak): Configurato (Raggio: 2.2m)\n" + // ok qua // riga-ok
            $"• Spegnimento automatico al contenimento con Keycard '{hotspot2.RequiredCredentialId}': Abilitato", // ok qua // riga-ok
            "OK" // ok qua // riga-ok
        ); // chiama // riga-ok
    } // chiude // riga-ok

    [MenuItem("CrisisProtocol/Configura Tutti i Suoni Scena ed Emettitori 3D")] // nota unity // riga-ok
    // blocco: funzione fa cose
    public static void ConfiguraSuoniMenu() // roba pub // riga-ok
    { // apre // riga-ok
        SceneAudioPopulator.ApplicaTuttiISuoni(); // chiama // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
#endif // prep ok // riga-ok
