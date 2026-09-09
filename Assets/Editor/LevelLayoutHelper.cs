#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class LevelLayoutHelper : EditorWindow
{
    [MenuItem("Tools/Adatta Pavimento alla Struttura & Configura Flag Droni")]
    public static void ConfiguraLivelloEPattuglia()
    {
        Debug.Log("<color=cyan>[LevelLayoutHelper]</color> Avvio configurazione pavimento su misura e circuito droni...");

        // 1. Calcola l'ingombro (Bounding Box) esatto della struttura edilizia
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        Bounds boundsStruttura = new Bounds();
        bool boundsInizializzato = false;
        int muriRilevati = 0;

        foreach (GameObject go in allObjects)
        {
            // Escludi Terrain, Player, Drone, Bot, Luci, Telecamere
            if (go.GetComponent<Terrain>() != null ||
                go.GetComponent<NavMeshAgent>() != null ||
                go.GetComponent<muve_pg>() != null ||
                go.GetComponent<Camera>() != null ||
                go.GetComponent<Light>() != null)
            {
                continue;
            }

            Collider col = go.GetComponent<Collider>();
            MeshRenderer mr = go.GetComponent<MeshRenderer>();

            if (col == null && mr == null) continue;
            if (col != null && col.isTrigger) continue;

            string n = go.name.ToLower();
            Vector3 size = col != null ? col.bounds.size : mr.bounds.size;
            Vector3 center = col != null ? col.bounds.center : mr.bounds.center;

            // Riconosci muri, pilastri, strutture edificate o cubi alti
            bool isStruttura = size.y >= 0.8f ||
                              n.Contains("muro") || n.Contains("muri") || n.Contains("wall") ||
                              n.Contains("pillar") || n.Contains("colonna") || n.Contains("building") ||
                              n.Contains("structure") || n.Contains("pb_mesh");

            if (isStruttura)
            {
                Bounds currentBounds = col != null ? col.bounds : mr.bounds;
                if (!boundsInizializzato)
                {
                    boundsStruttura = currentBounds;
                    boundsInizializzato = true;
                }
                else
                {
                    boundsStruttura.Encapsulate(currentBounds);
                }
                muriRilevati++;
            }
        }

        if (!boundsInizializzato)
        {
            EditorUtility.DisplayDialog("Attenzione", "Nessuna struttura o muro rilevato nella scena per calcolare le dimensioni!", "OK");
            return;
        }

        float floorY = boundsStruttura.min.y;
        Debug.Log($"<color=green>[LevelLayoutHelper]</color> Struttura rilevata ({muriRilevati} elementi): " +
                  $"Larghezza X={boundsStruttura.size.x:F1}m, Lunghezza Z={boundsStruttura.size.z:F1}m, Quota Base Y={floorY:F2}m");

        // 2. Rimuovi o Disattiva il vecchio Terrain gigante
        Terrain[] terrains = Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None);
        foreach (Terrain t in terrains)
        {
            Undo.RegisterCompleteObjectUndo(t.gameObject, "Disattiva Terrain");
            t.gameObject.SetActive(false);
            Debug.Log($"[LevelLayoutHelper] Disattivato Terrain gigante: '{t.gameObject.name}'");
        }

        // 3. Crea o Aggiorna il Pavimento Plane tagliato esattamente a misura
        GameObject pavimento = GameObject.Find("Pavimento_Settore");
        if (pavimento == null)
        {
            pavimento = GameObject.CreatePrimitive(PrimitiveType.Plane);
            pavimento.name = "Pavimento_Settore";
            Undo.RegisterCreatedObjectUndo(pavimento, "Crea Pavimento Su Misura");
        }
        else
        {
            Undo.RecordObject(pavimento.transform, "Adatta Pavimento");
        }

        // Il Plane predefinito di Unity è 10x10 metri
        pavimento.transform.position = new Vector3(boundsStruttura.center.x, floorY, boundsStruttura.center.z);
        float scaleX = (boundsStruttura.size.x / 10f) * 1.02f; // margine minimo 2% per chiudere i bordi
        float scaleZ = (boundsStruttura.size.z / 10f) * 1.02f;
        pavimento.transform.localScale = new Vector3(scaleX, 1f, scaleZ);

        // Imposta Navigation Static Walkable per il pavimento
        StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(pavimento);
        flags |= StaticEditorFlags.NavigationStatic;
        GameObjectUtility.SetStaticEditorFlags(pavimento, flags);
        GameObjectUtility.SetNavMeshArea(pavimento, 0); // Walkable

        // Cerca e applica un materiale pavimento idoneo se presente
        MeshRenderer mrPavimento = pavimento.GetComponent<MeshRenderer>();
        if (mrPavimento != null && (mrPavimento.sharedMaterial == null || mrPavimento.sharedMaterial.name.Contains("Default")))
        {
            string[] matGuids = AssetDatabase.FindAssets("metallo opaco t:Material");
            if (matGuids.Length == 0) matGuids = AssetDatabase.FindAssets("Concrete t:Material");
            if (matGuids.Length > 0)
            {
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(matGuids[0]));
                if (mat != null) mrPavimento.sharedMaterial = mat;
            }
        }

        // 4. Configura le Flag del Drone nelle corsie laterali a forma di anello perimetrale
        List<GameObject> existingFlags = new List<GameObject>();
        foreach (GameObject go in allObjects)
        {
            if (go != null && (go.name.StartsWith("FLAG", System.StringComparison.OrdinalIgnoreCase) || 
                               go.name.StartsWith("Flag", System.StringComparison.OrdinalIgnoreCase)))
            {
                existingFlags.Add(go);
            }
        }

        // Se non ci sono flag sufficienti, creane 4
        GameObject flagContainer = GameObject.Find("Waypoint_Drone_Circuito");
        if (flagContainer == null)
        {
            flagContainer = new GameObject("Waypoint_Drone_Circuito");
            Undo.RegisterCreatedObjectUndo(flagContainer, "Crea Contenitore Waypoint Drone");
        }

        while (existingFlags.Count < 4)
        {
            GameObject newFlag = new GameObject($"FLAG ({existingFlags.Count + 1})");
            newFlag.transform.parent = flagContainer.transform;
            Undo.RegisterCreatedObjectUndo(newFlag, "Crea Flag Drone");
            existingFlags.Add(newFlag);
        }

        float droneFlyHeight = floorY + 2.5f; // Quota di volo sicura
        float marginX = Mathf.Clamp(boundsStruttura.size.x * 0.12f, 1.5f, 4.0f);
        float marginZ = Mathf.Clamp(boundsStruttura.size.z * 0.12f, 1.5f, 4.0f);

        float minX = boundsStruttura.min.x + marginX;
        float maxX = boundsStruttura.max.x - marginX;
        float minZ = boundsStruttura.min.z + marginZ;
        float maxZ = boundsStruttura.max.z - marginZ;

        // Distribuzione a circuito orario: Nord-Ovest -> Nord-Est -> Sud-Est -> Sud-Ovest
        Vector3[] loopPositions = new Vector3[]
        {
            new Vector3(minX, droneFlyHeight, maxZ), // 1. Nord-Ovest
            new Vector3(maxX, droneFlyHeight, maxZ), // 2. Nord-Est
            new Vector3(maxX, droneFlyHeight, minZ), // 3. Sud-Est
            new Vector3(minX, droneFlyHeight, minZ)  // 4. Sud-Ovest
        };

        Transform[] waypointsArray = new Transform[existingFlags.Count];

        for (int i = 0; i < existingFlags.Count; i++)
        {
            Undo.RecordObject(existingFlags[i].transform, "Posiziona Flag");
            if (i < 4)
            {
                existingFlags[i].transform.position = loopPositions[i];
            }
            else
            {
                // Se ci sono più di 4 flag, distribuiscili nei punti intermedi dei corridoi
                float t = (float)(i - 3) / (existingFlags.Count - 3 + 1);
                existingFlags[i].transform.position = Vector3.Lerp(loopPositions[3], loopPositions[0], t);
            }
            waypointsArray[i] = existingFlags[i].transform;
        }

        // 5. Collega automaticamente i waypoints a tutti i DroneRonda e GuardiaNpc nella scena
        DroneRonda[] droni = Object.FindObjectsByType<DroneRonda>(FindObjectsSortMode.None);
        int droniConfigurati = 0;
        foreach (DroneRonda drone in droni)
        {
            Undo.RecordObject(drone, "Assegna Waypoint Drone");
            drone.waypoints = waypointsArray;
            drone.velocita = 3.5f;
            EditorUtility.SetDirty(drone);
            droniConfigurati++;
        }

        GuardiaNpc[] guardie = Object.FindObjectsByType<GuardiaNpc>(FindObjectsSortMode.None);
        int guardieConfigurate = 0;
        foreach (GuardiaNpc guardia in guardie)
        {
            if (guardia.waypointRonda == null || guardia.waypointRonda.Length == 0)
            {
                Undo.RecordObject(guardia, "Assegna Waypoint Guardia");
                guardia.waypointRonda = waypointsArray;
                guardia.eStatica = false;
                EditorUtility.SetDirty(guardia);
                guardieConfigurate++;
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("<color=green>[LevelLayoutHelper] Configurazione completata con successo!</color>");

        EditorUtility.DisplayDialog(
            "Configurazione Completata!",
            $"Il layout è stato impostato con successo:\n\n" +
            $"• Pavimento generato su misura: {boundsStruttura.size.x:F1}m x {boundsStruttura.size.z:F1}m (Base Y: {floorY:F2})\n" +
            $"• Terrain disattivato\n" +
            $"• {existingFlags.Count} Flag del Drone posizionate nelle corsie perimetrali ad anello\n" +
            $"• {droniConfigurati} DroneRonda collegati al circuito di ronda\n\n" +
            $"Premi 'Ctrl + S' per salvare la scena e 'Play' per testare il drone!",
            "OK"
        );
    }

    [MenuItem("Tools/Crea Terminale con Codice e Bypass per Porta Selezionata")]
    public static void CreaTerminalePerPorta()
    {
        PortaSettore porta = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<PortaSettore>() : null;
        if (porta == null)
            porta = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInChildren<PortaSettore>() : null;

        if (porta == null)
            porta = Object.FindFirstObjectByType<PortaSettore>();

        if (porta == null)
        {
            EditorUtility.DisplayDialog("Attenzione", "Seleziona prima una Porta (con componente PortaSettore) nella gerarchia per posizionare il terminale accanto ad essa!", "OK");
            return;
        }

        // Calcola posizione a fianco della porta
        Vector3 posTerminale = porta.transform.position + porta.transform.right * 1.5f;
        posTerminale.y = porta.transform.position.y;

        // Crea il GameObject del Terminale
        GameObject terminaleObj = new GameObject("Terminale_Sicurezza_" + porta.gameObject.name);
        terminaleObj.transform.position = posTerminale;
        terminaleObj.transform.rotation = porta.transform.rotation;

        int layerInteractable = LayerMask.NameToLayer("Interactable");
        if (layerInteractable != -1) terminaleObj.layer = layerInteractable;

        // Piedistallo / Colonnina 3D
        GameObject pilastro = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pilastro.name = "Supporto_Colonnina";
        pilastro.transform.SetParent(terminaleObj.transform, false);
        pilastro.transform.localPosition = new Vector3(0, 0.6f, 0);
        pilastro.transform.localScale = new Vector3(0.25f, 1.2f, 0.25f);

        // Monitor / Schermo 3D
        GameObject monitor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        monitor.name = "Schermo_Monitor";
        monitor.transform.SetParent(terminaleObj.transform, false);
        monitor.transform.localPosition = new Vector3(0, 1.25f, 0.1f);
        monitor.transform.localScale = new Vector3(0.6f, 0.45f, 0.15f);

        // Luce dello Schermo
        GameObject luceObj = new GameObject("Luce_Schermo");
        luceObj.transform.SetParent(terminaleObj.transform, false);
        luceObj.transform.localPosition = new Vector3(0, 1.25f, 0.35f);
        Light luceMonitor = luceObj.AddComponent<Light>();
        luceMonitor.type = LightType.Point;
        luceMonitor.range = 2.5f;
        luceMonitor.intensity = 1.5f;
        luceMonitor.color = Color.red;

        // Collider di Interazione
        BoxCollider boxCol = terminaleObj.AddComponent<BoxCollider>();
        boxCol.center = new Vector3(0, 0.9f, 0);
        boxCol.size = new Vector3(0.8f, 1.8f, 0.8f);

        // Componente TerminalePorta
        TerminalePorta terminaleScript = terminaleObj.AddComponent<TerminalePorta>();
        terminaleScript.portaCollegata = porta;
        terminaleScript.nomeTerminale = "PANNELLO DI ACCESSO // " + porta.gameObject.name.ToUpper();
        terminaleScript.codiceSegreto = "4281";
        terminaleScript.consentiCodicePin = true;
        terminaleScript.consentiBypassElettronico = true;
        terminaleScript.monitorRenderer = monitor.GetComponent<MeshRenderer>();
        terminaleScript.luceMonitor = luceMonitor;

        porta.terminaleSicurezza = terminaleScript;
        porta.AggiornaFeedbackVisivo();
        EditorUtility.SetDirty(porta.gameObject);

        Undo.RegisterCreatedObjectUndo(terminaleObj, "Crea Terminale Porta");
        Selection.activeGameObject = terminaleObj;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log($"<color=green>[LevelLayoutHelper]</color> Creato con successo Terminale per '{porta.gameObject.name}'!");

        EditorUtility.DisplayDialog(
            "Terminale Creato!",
            $"Il Terminale di Sicurezza è stato generato e collegato a '{porta.gameObject.name}':\n\n" +
            $"• Codice PIN di sblocco: 4281 (modificabile nell'Inspector)\n" +
            $"• Minigioco di Bypass Circuiti attivo\n" +
            $"• Interagisci premendo 'E' davanti al monitor durante il gioco!\n\n" +
            $"Premi 'Ctrl + S' per salvare.",
            "OK"
        );
    }

    [MenuItem("Tools/Crea Datapad Olografico con Codici Porte (Verde Acqua LED)")]
    public static void CreaDatapadOlograficoCodici()
    {
        Vector3 spawnPos = Vector3.zero;

        // Cerca posizione davanti alla camera della SceneView o vicino al Player
        if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null)
        {
            Transform camT = SceneView.lastActiveSceneView.camera.transform;
            spawnPos = camT.position + camT.forward * 3f;
        }
        else
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                spawnPos = player.transform.position + player.transform.forward * 2f + Vector3.up * 0.8f;
            else
                spawnPos = new Vector3(0, 1f, 0);
        }

        // Crea GameObject Root del Datapad
        GameObject datapadObj = new GameObject("Datapad_Ologramma_CodiciPorte");
        datapadObj.transform.position = spawnPos;
        datapadObj.transform.rotation = Quaternion.Euler(20f, 0f, 0f);

        int layerInteractable = LayerMask.NameToLayer("Interactable");
        if (layerInteractable != -1) datapadObj.layer = layerInteractable;

        // Scocca Tablet / Datapad 3D
        GameObject scocca = GameObject.CreatePrimitive(PrimitiveType.Cube);
        scocca.name = "Scocca_Tablet";
        scocca.transform.SetParent(datapadObj.transform, false);
        scocca.transform.localPosition = Vector3.zero;
        scocca.transform.localScale = new Vector3(0.45f, 0.04f, 0.30f);

        // Schermo Olografico Emissivo Verde Acqua
        GameObject schermo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        schermo.name = "Schermo_Ologramma";
        schermo.transform.SetParent(datapadObj.transform, false);
        schermo.transform.localPosition = new Vector3(0, 0.025f, 0);
        schermo.transform.localScale = new Vector3(0.40f, 0.02f, 0.25f);

        MeshRenderer mrSchermo = schermo.GetComponent<MeshRenderer>();
        Color aquaColor = new Color(0.0f, 0.95f, 0.85f);
        if (mrSchermo != null && mrSchermo.sharedMaterial != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? mrSchermo.sharedMaterial.shader);
            mat.name = "Mat_OlogrammaAquaLED";
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", aquaColor);
            else if (mat.HasProperty("_Color")) mat.color = aquaColor;
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", aquaColor * 3f);
            }
            mrSchermo.material = mat;
        }

        // Luce Olografica Point Light
        GameObject luceObj = new GameObject("Luce_Ologramma");
        luceObj.transform.SetParent(datapadObj.transform, false);
        luceObj.transform.localPosition = new Vector3(0, 0.15f, 0);
        Light luce = luceObj.AddComponent<Light>();
        luce.type = LightType.Point;
        luce.color = aquaColor;
        luce.intensity = 2.0f;
        luce.range = 3.0f;

        // Collider di Interazione
        BoxCollider col = datapadObj.AddComponent<BoxCollider>();
        col.center = Vector3.zero;
        col.size = new Vector3(0.6f, 0.4f, 0.5f);

        // Script DatapadCodiciPorte
        DatapadCodiciPorte script = datapadObj.AddComponent<DatapadCodiciPorte>();
        script.titoloDatapad = "DATAPAD DI SICUREZZA // REGISTRO CODICI ACCESSO";
        script.autoreONota = "Ufficio Sicurezza Settore Alpha - Codici di emergenza porte";
        script.autoRilevaPorteScena = true;
        script.coloreOlogramma = aquaColor;
        script.luceOlogramma = luce;
        script.meshSchermo = mrSchermo;
        script.animaPulsazioneLuce = true;

        Undo.RegisterCreatedObjectUndo(datapadObj, "Crea Datapad Olografico");
        Selection.activeGameObject = datapadObj;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("<color=cyan>[LevelLayoutHelper]</color> Creato Datapad Ologramma Codici Porte nella scena!");
        EditorUtility.DisplayDialog(
            "Datapad Olografico Creato!",
            "Il Datapad Olografico con i PIN delle porte è stato creato nella scena:\n\n" +
            "• Rileva in automatico tutti i Terminali e i PIN delle porte presenti!\n" +
            "• Schermata LED verde acqua / ciano in stile ologramma sci-fi\n" +
            "• Interagisci avvicinandoti e premendo 'E' oppure cliccandoci sopra col mouse.\n\n" +
            "Puoi posizionarlo su un tavolo, scrivania o per terra e personalizzare i testi nell'Inspector.",
            "OK"
        );
    }
}
#endif
