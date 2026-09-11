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

    [MenuItem("Tools/Debug/Trova e Seleziona Portellone Uscita (Quarantine Gate)")]
    public static void TrovaESelezionaPortelloneUscita()
    {
        QuarantineGate gate = Object.FindAnyObjectByType<QuarantineGate>();
        if (gate != null)
        {
            Selection.activeGameObject = gate.gameObject;
            EditorGUIUtility.PingObject(gate.gameObject);
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }

            if (Application.isPlaying)
            {
                gate.ForzaSbloccoEditor();
                Debug.Log($"<color=lime>[DEBUG]</color> Portellone di uscita <b>'{gate.name}'</b> selezionato e SBLOCCATO in Play Mode!");
            }
            else
            {
                Debug.Log($"<color=cyan>[DEBUG]</color> Portellone di uscita <b>'{gate.name}'</b> selezionato e inquadrato nella scena.");
            }
        }
        else
        {
            bool crea = EditorUtility.DisplayDialog(
                "Portellone Non Trovato",
                "Nessun componente 'QuarantineGate' trovato nella scena corrente.\n\nVuoi creare automaticamente un Portellone di Uscita adesso?",
                "Sì, Crea Portellone",
                "Annulla"
            );

            if (crea)
            {
                CreaPortelloneUscitaQuarantena();
            }
        }
    }

    [MenuItem("Tools/Debug/Crea Portellone Uscita Quarantena nel Settore")]
    public static void CreaPortelloneUscitaQuarantena()
    {
        Vector3 spawnPos = Vector3.zero;
        if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null)
        {
            Transform camT = SceneView.lastActiveSceneView.camera.transform;
            spawnPos = camT.position + camT.forward * 4f;
        }
        else
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                spawnPos = player.transform.position + player.transform.forward * 3f + Vector3.up * 1f;
            else
                spawnPos = new Vector3(0, 1.5f, 0);
        }

        // Root del Portellone
        GameObject gateObj = new GameObject("Portellone_Quarantena_Uscita");
        gateObj.transform.position = spawnPos;

        int layerInteractable = LayerMask.NameToLayer("Interactable");
        if (layerInteractable != -1) gateObj.layer = layerInteractable;

        // Struttura Telaio / Portale
        GameObject telaio = GameObject.CreatePrimitive(PrimitiveType.Cube);
        telaio.name = "Telaio_Portale";
        telaio.transform.SetParent(gateObj.transform, false);
        telaio.transform.localPosition = new Vector3(0, 0, 0);
        telaio.transform.localScale = new Vector3(2.6f, 3.2f, 0.4f);

        // Pannello Porta interna
        GameObject anta = GameObject.CreatePrimitive(PrimitiveType.Cube);
        anta.name = "Pannello_Portellone";
        anta.transform.SetParent(gateObj.transform, false);
        anta.transform.localPosition = new Vector3(0, -0.1f, 0);
        anta.transform.localScale = new Vector3(2.2f, 2.8f, 0.2f);

        // Luce di Stato Quarantena
        GameObject luceObj = new GameObject("Luce_Stato_Quarantena");
        luceObj.transform.SetParent(gateObj.transform, false);
        luceObj.transform.localPosition = new Vector3(0, 1.4f, -0.3f);
        Light lightComp = luceObj.AddComponent<Light>();
        lightComp.type = LightType.Point;
        lightComp.color = Color.red;
        lightComp.intensity = 3.5f;
        lightComp.range = 5.0f;

        // Indicatore Visivo Bloccato (Luce/Bordo Rosso)
        GameObject lockedVis = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lockedVis.name = "Indicatore_Bloccato_Rosso";
        lockedVis.transform.SetParent(gateObj.transform, false);
        lockedVis.transform.localPosition = new Vector3(0, 1.4f, -0.25f);
        lockedVis.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        Renderer rLocked = lockedVis.GetComponent<Renderer>();
        if (rLocked != null)
        {
            Material m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? rLocked.sharedMaterial.shader);
            m.color = Color.red;
            if (m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Color.red * 2f);
            }
            rLocked.material = m;
        }

        // Indicatore Visivo Sbloccato (Luce/Bordo Verde)
        GameObject unlockedVis = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        unlockedVis.name = "Indicatore_Sbloccato_Verde";
        unlockedVis.transform.SetParent(gateObj.transform, false);
        unlockedVis.transform.localPosition = new Vector3(0, 1.4f, -0.25f);
        unlockedVis.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        unlockedVis.SetActive(false);
        Renderer rUnlocked = unlockedVis.GetComponent<Renderer>();
        if (rUnlocked != null)
        {
            Material m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? rUnlocked.sharedMaterial.shader);
            m.color = Color.green;
            if (m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Color.green * 2f);
            }
            rUnlocked.material = m;
        }

        // Collider di interazione fisico
        BoxCollider boxCol = gateObj.AddComponent<BoxCollider>();
        boxCol.size = new Vector3(2.8f, 3.4f, 1.5f);
        boxCol.center = Vector3.zero;

        // Script QuarantineGate
        QuarantineGate qGate = gateObj.AddComponent<QuarantineGate>();
        var statusLightField = typeof(QuarantineGate).GetField("statusLight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (statusLightField != null) statusLightField.SetValue(qGate, lightComp);

        var lockedVisualField = typeof(QuarantineGate).GetField("lockedVisual", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (lockedVisualField != null) lockedVisualField.SetValue(qGate, lockedVis);

        var unlockedVisualField = typeof(QuarantineGate).GetField("unlockedVisual", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (unlockedVisualField != null) unlockedVisualField.SetValue(qGate, unlockedVis);

        Undo.RegisterCreatedObjectUndo(gateObj, "Crea Portellone Quarantena");
        Selection.activeGameObject = gateObj;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("<color=green>[LevelLayoutHelper]</color> Portellone di Quarantena creato con successo!");
        EditorUtility.DisplayDialog(
            "Portellone Quarantena Creato!",
            "Il Portellone di Uscita/Quarantena è stato posizionato nella scena:\n\n" +
            "• Layer impostato su 'Interactable'\n" +
            "• Script QuarantineGate con gestione luci (Rosso=Bloccato, Verde=Sbloccato)\n" +
            "• Per testarlo subito in Play Mode puoi premere 'F6' (sblocca) o 'F7' (completa).\n\n" +
            "Posizionalo dove preferisci e salva la scena (Ctrl + S).",
            "OK"
        );
    }

    [MenuItem("Tools/Debug/Forza Sblocco Estrazione (In Play Mode)")]
    public static void ForzaSbloccoInPlayMode()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Avviso", "Questa opzione funziona solo mentre sei in PLAY MODE!", "OK");
            return;
        }

        QuarantineGate gate = Object.FindAnyObjectByType<QuarantineGate>();
        if (gate != null)
        {
            gate.ForzaSbloccoEditor();
        }
        else if (MissionManager.Instance != null)
        {
            MissionManager.Instance.ForzaSbloccoEstrazioneDebug();
        }
    }

    [MenuItem("Tools/Debug/Risolvi Stato Emergenza e Completa Tutti i Task (In Play Mode)")]
    public static void RisolviEmergenzaETaskInPlayMode()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Avviso", "Questa opzione funziona solo mentre sei in PLAY MODE!", "OK");
            return;
        }

        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.RisolviStatoEmergenzaETuttiTaskDebug();
        }
        else
        {
            EditorUtility.DisplayDialog("Avviso", "MissionManager non trovato nella scena attiva!", "OK");
        }
    }

    [MenuItem("Tools/Debug/Crea Cubo Nero Teletrasporto (Trigger Uscita Scena)")]
    public static void CreaCuboNeroTeletrasporto()
    {
        Vector3 spawnPos = Vector3.zero;
        if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null)
        {
            Transform camT = SceneView.lastActiveSceneView.camera.transform;
            spawnPos = camT.position + camT.forward * 3f;
        }
        else
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                spawnPos = player.transform.position + player.transform.forward * 3f + Vector3.up * 1f;
            else
                spawnPos = new Vector3(0, 1.5f, 0);
        }

        GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubo.name = "Cubo_Nero_Teletrasporto";
        cubo.transform.position = spawnPos;
        cubo.transform.localScale = new Vector3(2.5f, 3.0f, 2.5f);

        // Imposta il BoxCollider come Trigger
        BoxCollider col = cubo.GetComponent<BoxCollider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // Materiale Nero Lucido / Sci-Fi
        MeshRenderer mr = cubo.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            Material matNero = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            matNero.name = "Mat_CuboNero_Portale";
            Color blackColor = new Color(0.02f, 0.02f, 0.02f, 0.95f);
            if (matNero.HasProperty("_BaseColor")) matNero.SetColor("_BaseColor", blackColor);
            else if (matNero.HasProperty("_Color")) matNero.color = blackColor;
            if (matNero.HasProperty("_Smoothness")) matNero.SetFloat("_Smoothness", 0.9f);
            if (matNero.HasProperty("_Metallic")) matNero.SetFloat("_Metallic", 0.5f);
            mr.material = matNero;
        }

        // Aggiunge lo script di transizione
        CuboNeroTeletrasporto script = cubo.AddComponent<CuboNeroTeletrasporto>();

        Undo.RegisterCreatedObjectUndo(cubo, "Crea Cubo Nero Teletrasporto");
        Selection.activeGameObject = cubo;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("<color=black><color=white>[LevelLayoutHelper]</color></color> Creato 'Cubo_Nero_Teletrasporto' con trigger attivo!");
        EditorUtility.DisplayDialog(
            "Cubo Nero Teletrasporto Creato!",
            "Il Cubo Nero Triggered è stato creato nella scena:\n\n" +
            "• BoxCollider con 'Is Trigger' attivo\n" +
            "• Script CuboNeroTeletrasporto collegato\n" +
            "• Quando il Player ci cammina dentro a fine crisi (o con debug attivo), viene teletrasportato alla scena successiva!\n\n" +
            "Puoi posizionarlo dietro alle porte di uscita e salvare la scena (Ctrl + S).",
            "OK"
        );
    }
}
#endif
