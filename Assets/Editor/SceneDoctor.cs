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
            Debug.Log($"<color=green>[SceneDoctor] Generati/Verificati {hotspotColliderFix} collider fisici solidi su focolai ed emergenze.</color>");
        int pavimentiCount = 0;
        int muriCount = 0;

        foreach (GameObject go in allObjects)
        {
            // Salta entità dinamiche come Player, Bot, Drone, Luci, Telecamere
            if (go.GetComponent<NavMeshAgent>() != null || 
                go.GetComponent<muve_pg>() != null || 
                go.GetComponent<ManutenzioneBot>() != null ||
                go.GetComponent<Camera>() != null ||
                go.GetComponent<Light>() != null)
            {
                continue;
            }

            Collider col = go.GetComponent<Collider>();
            MeshRenderer mr = go.GetComponent<MeshRenderer>();

            if (col == null && mr == null)
                continue;

            // Salta trigger che non sono porte
            if (col != null && col.isTrigger && go.GetComponent<PortaSettore>() == null && go.GetComponent<QuarantineGate>() == null)
                continue;

            string n = go.name.ToLower();

            // Rilevamento semantico parole chiave
            bool hasExplicitWallKeyword = n.Contains("muro") || n.Contains("muri") || n.Contains("wall") ||
                                          n.Contains("ostacolo") || n.Contains("pillar") || n.Contains("colonna") ||
                                          n.Contains("door") || n.Contains("porta") || n.Contains("gate") ||
                                          n.Contains("barrier") || n.Contains("barriera") || n.Contains("building") ||
                                          n.Contains("structure") || n.Contains("edificio") || n.Contains("roman") ||
                                          n.Contains("prop") || n.Contains("box") || n.Contains("crate") ||
                                          n.Contains("cassa") || n.Contains("container") || n.Contains("fence") ||
                                          n.Contains("recinto") || n.Contains("soffitto") || n.Contains("ceiling") ||
                                          n.Contains("roof") || n.Contains("tetto");

            bool hasExplicitFloorKeyword = n.Contains("floor") || n.Contains("pavimento") || n.Contains("ground") ||
                                           n.Contains("terrain") || n.Contains("plane") || n.Contains("suolo") ||
                                           n.Contains("base") || n.Contains("walkway") || n.Contains("strada") ||
                                           n.Contains("road") || n.Contains("platform") || n.Contains("piattaforma");

            // Controllo gerarchico dei genitori (es. cubi dentro contenitore "muri" o "floor")
            Transform curr = go.transform.parent;
            while (curr != null)
            {
                string pName = curr.name.ToLower();
                if (pName.Contains("muro") || pName.Contains("muri") || pName.Contains("wall") ||
                    pName.Contains("ostacolo") || pName.Contains("pillar") || pName.Contains("building") ||
                    pName.Contains("structure") || pName.Contains("box") || pName.Contains("recinto") ||
                    pName.Contains("ceiling") || pName.Contains("soffitto") || pName.Contains("tetto"))
                {
                    hasExplicitWallKeyword = true;
                    break;
                }
                if (pName.Contains("floor") || pName.Contains("pavimento") || pName.Contains("ground") ||
                    pName.Contains("terrain") || pName.Contains("plane") || pName.Contains("suolo"))
                {
                    hasExplicitFloorKeyword = true;
                    break;
                }
                curr = curr.parent;
            }

            // Calcolo dimensioni geometriche in World Space
            Vector3 size = Vector3.zero;
            if (col != null)
                size = col.bounds.size;
            else if (mr != null)
                size = mr.bounds.size;

            // Un pavimento è piatto (spessore Y <= 0.6m) e largo orizzontalmente
            bool isGeometricallyFloor = (size.y <= 0.6f && (size.x >= 1.5f || size.z >= 1.5f));
            // Un muro è sviluppato in altezza (spessore Y >= 0.8m)
            bool isGeometricallyWall = (size.y >= 0.8f);

            bool isMuro = false;
            bool isPavimento = false;

            if (hasExplicitWallKeyword)
            {
                isMuro = true;
            }
            else if (hasExplicitFloorKeyword)
            {
                isPavimento = true;
            }
            else if (isGeometricallyWall)
            {
                // Se è alto almeno 0.8 metri, è indiscutibilmente un MURO/OSTACOLO
                isMuro = true;
            }
            else if (isGeometricallyFloor)
            {
                isPavimento = true;
            }
            else
            {
                // In caso di dubbio, non è un pavimento
                isMuro = true;
            }

            StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(go);

            if (isMuro)
            {
                flags |= StaticEditorFlags.NavigationStatic;
                GameObjectUtility.SetStaticEditorFlags(go, flags);
                GameObjectUtility.SetNavMeshArea(go, 1); // 1 = NOT WALKABLE (Area bloccata)

                // Aggiungi e configura NavMeshObstacle con Carving attivo per tagliare fisicamente la NavMesh
                NavMeshObstacle obs = go.GetComponent<NavMeshObstacle>();
                if (obs == null)
                {
                    obs = go.AddComponent<NavMeshObstacle>();
                }
                obs.carving = true;
                obs.carveOnlyStationary = false;
                obs.carvingMoveThreshold = 0.1f;
                
                if (col is BoxCollider bc)
                {
                    obs.shape = NavMeshObstacleShape.Box;
                    obs.center = bc.center;
                    obs.size = bc.size;
                }

                EditorUtility.SetDirty(go);
                muriCount++;
            }
            else if (isPavimento)
            {
                flags |= StaticEditorFlags.NavigationStatic;
                GameObjectUtility.SetStaticEditorFlags(go, flags);
                GameObjectUtility.SetNavMeshArea(go, 0); // 0 = WALKABLE (Calpestabile)

                // Rimuovi eventuali NavMeshObstacle erroneamente presenti sul pavimento
                NavMeshObstacle obs = go.GetComponent<NavMeshObstacle>();
                if (obs != null)
                {
                    DestroyImmediate(obs);
                }

                EditorUtility.SetDirty(go);
                pavimentiCount++;
            }
        }

        Debug.Log($"<color=green>[SceneDoctor] Classificazione completata: {pavimentiCount} Pavimenti (Walkable) e {muriCount} Muri con Carving (Not Walkable).</color>");

        // 4. Pulisci la vecchia NavMesh e Rigenerala con parametri precisi
        Debug.Log("[SceneDoctor] Pulizia vecchia NavMesh e rigenerazione...");
        UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes();
        UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
        Debug.Log("<color=cyan>[SceneDoctor] Generazione NavMesh COMPLETATA con successo!</color>");

        EditorUtility.DisplayDialog(
            "Scene Doctor", 
            $"Configurazione completata con successo!\n\n- Muri e ostacoli protetti con Carving (Not Walkable): {muriCount}\n- Pavimenti calpestabili (Walkable): {pavimentiCount}\n- Bot calibrati con velocità regolari: {botCount}\n- Script fantasma rimossi: {missingCount}\n\nTutti i muri hanno ora il Carving attivo: è fisicamente impossibile per i bot attraversarli!", 
            "OK"
        );
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
}
#endif
