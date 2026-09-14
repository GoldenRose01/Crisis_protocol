// ============================================================================
// Crisis Protocol / Sector Containment - Utility editor
// File: .\Assets\Editor\LevelLayoutHelper.cs
// Responsabilita': automatizza setup, popolamento scena, salvataggio, validazione o manutenzione direttamente dentro Unity Editor.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
#if UNITY_EDITOR // prep ok // riga-ok
using UnityEditor; // usa lib // riga-ok
using UnityEditor.SceneManagement; // usa lib // riga-ok
using UnityEngine; // usa lib // riga-ok
using UnityEngine.AI; // usa lib // riga-ok
using System.Collections.Generic; // usa lib // riga-ok

// blocco: classe x roba grossa
public class LevelLayoutHelper : EditorWindow // classe qui // riga-ok
{ // apre // riga-ok
    [MenuItem("Tools/Adatta Pavimento alla Struttura & Configura Flag Droni")] // nota unity // riga-ok
    // blocco: funzione fa cose
    public static void ConfiguraLivelloEPattuglia() // roba pub // riga-ok
    { // apre // riga-ok
        Debug.Log("<color=cyan>[LevelLayoutHelper]</color> Avvio configurazione pavimento su misura e circuito droni..."); // logga // riga-ok

        // 1. Calcola l'ingombro (Bounding Box) esatto della struttura edilizia
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None); // setta // riga-ok
        Bounds boundsStruttura = new Bounds(); // setta // riga-ok
        bool boundsInizializzato = false; // setta // riga-ok
        int muriRilevati = 0; // setta // riga-ok

        // blocco: gira piu volte
        foreach (GameObject go in allObjects) // ciclo x // riga-ok
        { // apre // riga-ok
            // Escludi Terrain, Player, Drone, Bot, Luci, Telecamere
            // blocco: controlla se va
            if (go.GetComponent<Terrain>() != null || // se ok // riga-ok
                go.GetComponent<NavMeshAgent>() != null || // setta // riga-ok
                go.GetComponent<muve_pg>() != null || // setta // riga-ok
                go.GetComponent<Camera>() != null || // setta // riga-ok
                go.GetComponent<Light>() != null) // setta // riga-ok
            { // apre // riga-ok
                continue; // salta // riga-ok
            } // chiude // riga-ok

            Collider col = go.GetComponent<Collider>(); // setta // riga-ok
            MeshRenderer mr = go.GetComponent<MeshRenderer>(); // setta // riga-ok

            // blocco: controlla se va
            if (col == null && mr == null) continue; // se ok // riga-ok
            // blocco: controlla se va
            if (col != null && col.isTrigger) continue; // se ok // riga-ok

            string n = go.name.ToLower(); // setta // riga-ok
            Vector3 size = col != null ? col.bounds.size : mr.bounds.size; // setta // riga-ok
            Vector3 center = col != null ? col.bounds.center : mr.bounds.center; // setta // riga-ok

            // Riconosci muri, pilastri, strutture edificate o cubi alti
            bool isStruttura = size.y >= 0.8f || // setta // riga-ok
                              n.Contains("muro") || n.Contains("muri") || n.Contains("wall") || // ok qua // riga-ok
                              n.Contains("pillar") || n.Contains("colonna") || n.Contains("building") || // ok qua // riga-ok
                              n.Contains("structure") || n.Contains("pb_mesh"); // chiama // riga-ok

            // blocco: controlla se va
            if (isStruttura) // se ok // riga-ok
            { // apre // riga-ok
                Bounds currentBounds = col != null ? col.bounds : mr.bounds; // setta // riga-ok
                // blocco: controlla se va
                if (!boundsInizializzato) // se ok // riga-ok
                { // apre // riga-ok
                    boundsStruttura = currentBounds; // setta // riga-ok
                    boundsInizializzato = true; // setta // riga-ok
                } // chiude // riga-ok
                // blocco: caso diverso
                else // se no // riga-ok
                { // apre // riga-ok
                    boundsStruttura.Encapsulate(currentBounds); // chiama // riga-ok
                } // chiude // riga-ok
                muriRilevati++; // ok qua // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (!boundsInizializzato) // se ok // riga-ok
        { // apre // riga-ok
            EditorUtility.DisplayDialog("Attenzione", "Nessuna struttura o muro rilevato nella scena per calcolare le dimensioni!", "OK"); // chiama // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        float floorY = boundsStruttura.min.y; // setta // riga-ok
        Debug.Log($"<color=green>[LevelLayoutHelper]</color> Struttura rilevata ({muriRilevati} elementi): " + // logga // riga-ok
                  $"Larghezza X={boundsStruttura.size.x:F1}m, Lunghezza Z={boundsStruttura.size.z:F1}m, Quota Base Y={floorY:F2}m"); // setta // riga-ok

        // 2. Rimuovi o Disattiva il vecchio Terrain gigante
        Terrain[] terrains = Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None); // setta // riga-ok
        // blocco: gira piu volte
        foreach (Terrain t in terrains) // ciclo x // riga-ok
        { // apre // riga-ok
            Undo.RegisterCompleteObjectUndo(t.gameObject, "Disattiva Terrain"); // chiama // riga-ok
            t.gameObject.SetActive(false); // chiama // riga-ok
            Debug.Log($"[LevelLayoutHelper] Disattivato Terrain gigante: '{t.gameObject.name}'"); // logga // riga-ok
        } // chiude // riga-ok

        // 3. Crea o Aggiorna il Pavimento Plane tagliato esattamente a misura
        GameObject pavimento = GameObject.Find("Pavimento_Settore"); // setta // riga-ok
        // blocco: controlla se va
        if (pavimento == null) // se ok // riga-ok
        { // apre // riga-ok
            pavimento = GameObject.CreatePrimitive(PrimitiveType.Plane); // setta // riga-ok
            pavimento.name = "Pavimento_Settore"; // setta // riga-ok
            Undo.RegisterCreatedObjectUndo(pavimento, "Crea Pavimento Su Misura"); // chiama // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            Undo.RecordObject(pavimento.transform, "Adatta Pavimento"); // chiama // riga-ok
        } // chiude // riga-ok

        // Il Plane predefinito di Unity è 10x10 metri
        pavimento.transform.position = new Vector3(boundsStruttura.center.x, floorY, boundsStruttura.center.z); // setta // riga-ok
        float scaleX = (boundsStruttura.size.x / 10f) * 1.02f; // margine minimo 2% per chiudere i bordi // setta // riga-ok
        float scaleZ = (boundsStruttura.size.z / 10f) * 1.02f; // setta // riga-ok
        pavimento.transform.localScale = new Vector3(scaleX, 1f, scaleZ); // setta // riga-ok

        // Imposta Navigation Static Walkable per il pavimento
        StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(pavimento); // setta // riga-ok
        flags |= StaticEditorFlags.NavigationStatic; // setta // riga-ok
        GameObjectUtility.SetStaticEditorFlags(pavimento, flags); // chiama // riga-ok
        GameObjectUtility.SetNavMeshArea(pavimento, 0); // Walkable // ok qua // riga-ok

        // Cerca e applica un materiale pavimento idoneo se presente
        MeshRenderer mrPavimento = pavimento.GetComponent<MeshRenderer>(); // setta // riga-ok
        // blocco: controlla se va
        if (mrPavimento != null && (mrPavimento.sharedMaterial == null || mrPavimento.sharedMaterial.name.Contains("Default"))) // se ok // riga-ok
        { // apre // riga-ok
            string[] matGuids = AssetDatabase.FindAssets("metallo opaco t:Material"); // setta // riga-ok
            // blocco: controlla se va
            if (matGuids.Length == 0) matGuids = AssetDatabase.FindAssets("Concrete t:Material"); // se ok // riga-ok
            // blocco: controlla se va
            if (matGuids.Length > 0) // se ok // riga-ok
            { // apre // riga-ok
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(matGuids[0])); // setta // riga-ok
                // blocco: controlla se va
                if (mat != null) mrPavimento.sharedMaterial = mat; // se ok // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // 4. Configura le Flag del Drone nelle corsie laterali a forma di anello perimetrale
        List<GameObject> existingFlags = new List<GameObject>(); // setta // riga-ok
        // blocco: gira piu volte
        foreach (GameObject go in allObjects) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (go != null && (go.name.StartsWith("FLAG", System.StringComparison.OrdinalIgnoreCase) ||  // se ok // riga-ok
                               go.name.StartsWith("Flag", System.StringComparison.OrdinalIgnoreCase))) // chiama // riga-ok
            { // apre // riga-ok
                existingFlags.Add(go); // chiama // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // Se non ci sono flag sufficienti, creane 4
        GameObject flagContainer = GameObject.Find("Waypoint_Drone_Circuito"); // setta // riga-ok
        // blocco: controlla se va
        if (flagContainer == null) // se ok // riga-ok
        { // apre // riga-ok
            flagContainer = new GameObject("Waypoint_Drone_Circuito"); // setta // riga-ok
            Undo.RegisterCreatedObjectUndo(flagContainer, "Crea Contenitore Waypoint Drone"); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: gira piu volte
        while (existingFlags.Count < 4) // ciclo x // riga-ok
        { // apre // riga-ok
            GameObject newFlag = new GameObject($"FLAG ({existingFlags.Count + 1})"); // setta // riga-ok
            newFlag.transform.parent = flagContainer.transform; // setta // riga-ok
            Undo.RegisterCreatedObjectUndo(newFlag, "Crea Flag Drone"); // chiama // riga-ok
            existingFlags.Add(newFlag); // chiama // riga-ok
        } // chiude // riga-ok

        float droneFlyHeight = floorY + 2.5f; // Quota di volo sicura // setta // riga-ok
        float marginX = Mathf.Clamp(boundsStruttura.size.x * 0.12f, 1.5f, 4.0f); // setta // riga-ok
        float marginZ = Mathf.Clamp(boundsStruttura.size.z * 0.12f, 1.5f, 4.0f); // setta // riga-ok

        float minX = boundsStruttura.min.x + marginX; // setta // riga-ok
        float maxX = boundsStruttura.max.x - marginX; // setta // riga-ok
        float minZ = boundsStruttura.min.z + marginZ; // setta // riga-ok
        float maxZ = boundsStruttura.max.z - marginZ; // setta // riga-ok

        // Distribuzione a circuito orario: Nord-Ovest -> Nord-Est -> Sud-Est -> Sud-Ovest
        Vector3[] loopPositions = new Vector3[] // setta // riga-ok
        { // apre // riga-ok
            new Vector3(minX, droneFlyHeight, maxZ), // 1. Nord-Ovest // ok qua // riga-ok
            new Vector3(maxX, droneFlyHeight, maxZ), // 2. Nord-Est // ok qua // riga-ok
            new Vector3(maxX, droneFlyHeight, minZ), // 3. Sud-Est // ok qua // riga-ok
            new Vector3(minX, droneFlyHeight, minZ)  // 4. Sud-Ovest // ok qua // riga-ok
        }; // ok qua // riga-ok

        Transform[] waypointsArray = new Transform[existingFlags.Count]; // setta // riga-ok

        // blocco: gira piu volte
        for (int i = 0; i < existingFlags.Count; i++) // ciclo x // riga-ok
        { // apre // riga-ok
            Undo.RecordObject(existingFlags[i].transform, "Posiziona Flag"); // chiama // riga-ok
            // blocco: controlla se va
            if (i < 4) // se ok // riga-ok
            { // apre // riga-ok
                existingFlags[i].transform.position = loopPositions[i]; // setta // riga-ok
            } // chiude // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
            { // apre // riga-ok
                // Se ci sono più di 4 flag, distribuiscili nei punti intermedi dei corridoi
                float t = (float)(i - 3) / (existingFlags.Count - 3 + 1); // setta // riga-ok
                existingFlags[i].transform.position = Vector3.Lerp(loopPositions[3], loopPositions[0], t); // setta // riga-ok
            } // chiude // riga-ok
            waypointsArray[i] = existingFlags[i].transform; // setta // riga-ok
        } // chiude // riga-ok

        // 5. Collega automaticamente i waypoints a tutti i DroneRonda e GuardiaNpc nella scena
        DroneRonda[] droni = Object.FindObjectsByType<DroneRonda>(FindObjectsSortMode.None); // setta // riga-ok
        int droniConfigurati = 0; // setta // riga-ok
        // blocco: gira piu volte
        foreach (DroneRonda drone in droni) // ciclo x // riga-ok
        { // apre // riga-ok
            Undo.RecordObject(drone, "Assegna Waypoint Drone"); // chiama // riga-ok
            drone.waypoints = waypointsArray; // setta // riga-ok
            drone.velocita = 3.5f; // setta // riga-ok
            EditorUtility.SetDirty(drone); // chiama // riga-ok
            droniConfigurati++; // ok qua // riga-ok
        } // chiude // riga-ok

        GuardiaNpc[] guardie = Object.FindObjectsByType<GuardiaNpc>(FindObjectsSortMode.None); // setta // riga-ok
        int guardieConfigurate = 0; // setta // riga-ok
        // blocco: gira piu volte
        foreach (GuardiaNpc guardia in guardie) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (guardia.waypointRonda == null || guardia.waypointRonda.Length == 0) // se ok // riga-ok
            { // apre // riga-ok
                Undo.RecordObject(guardia, "Assegna Waypoint Guardia"); // chiama // riga-ok
                guardia.waypointRonda = waypointsArray; // setta // riga-ok
                guardia.eStatica = false; // setta // riga-ok
                EditorUtility.SetDirty(guardia); // chiama // riga-ok
                guardieConfigurate++; // ok qua // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); // chiama // riga-ok

        Debug.Log("<color=green>[LevelLayoutHelper] Configurazione completata con successo!</color>"); // logga // riga-ok

        EditorUtility.DisplayDialog( // ok qua // riga-ok
            "Configurazione Completata!", // ok qua // riga-ok
            $"Il layout è stato impostato con successo:\n\n" + // ok qua // riga-ok
            $"• Pavimento generato su misura: {boundsStruttura.size.x:F1}m x {boundsStruttura.size.z:F1}m (Base Y: {floorY:F2})\n" + // ok qua // riga-ok
            $"• Terrain disattivato\n" + // ok qua // riga-ok
            $"• {existingFlags.Count} Flag del Drone posizionate nelle corsie perimetrali ad anello\n" + // ok qua // riga-ok
            $"• {droniConfigurati} DroneRonda collegati al circuito di ronda\n\n" + // ok qua // riga-ok
            $"Premi 'Ctrl + S' per salvare la scena e 'Play' per testare il drone!", // ok qua // riga-ok
            "OK" // ok qua // riga-ok
        ); // chiama // riga-ok
    } // chiude // riga-ok

    [MenuItem("Tools/Crea Terminale con Codice e Bypass per Porta Selezionata")] // nota unity // riga-ok
    // blocco: funzione fa cose
    public static void CreaTerminalePerPorta() // roba pub // riga-ok
    { // apre // riga-ok
        PortaSettore porta = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<PortaSettore>() : null; // setta // riga-ok
        // blocco: controlla se va
        if (porta == null) // se ok // riga-ok
            porta = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInChildren<PortaSettore>() : null; // setta // riga-ok

        // blocco: controlla se va
        if (porta == null) // se ok // riga-ok
            porta = Object.FindFirstObjectByType<PortaSettore>(); // setta // riga-ok

        // blocco: controlla se va
        if (porta == null) // se ok // riga-ok
        { // apre // riga-ok
            EditorUtility.DisplayDialog("Attenzione", "Seleziona prima una Porta (con componente PortaSettore) nella gerarchia per posizionare il terminale accanto ad essa!", "OK"); // chiama // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        // Calcola posizione a fianco della porta
        Vector3 posTerminale = porta.transform.position + porta.transform.right * 1.5f; // setta // riga-ok
        posTerminale.y = porta.transform.position.y; // setta // riga-ok

        // Crea il GameObject del Terminale
        GameObject terminaleObj = new GameObject("Terminale_Sicurezza_" + porta.gameObject.name); // setta // riga-ok
        terminaleObj.transform.position = posTerminale; // setta // riga-ok
        terminaleObj.transform.rotation = porta.transform.rotation; // setta // riga-ok

        int layerInteractable = LayerMask.NameToLayer("Interactable"); // setta // riga-ok
        // blocco: controlla se va
        if (layerInteractable != -1) terminaleObj.layer = layerInteractable; // se ok // riga-ok

        // Piedistallo / Colonnina 3D
        GameObject pilastro = GameObject.CreatePrimitive(PrimitiveType.Cube); // setta // riga-ok
        pilastro.name = "Supporto_Colonnina"; // setta // riga-ok
        pilastro.transform.SetParent(terminaleObj.transform, false); // chiama // riga-ok
        pilastro.transform.localPosition = new Vector3(0, 0.6f, 0); // setta // riga-ok
        pilastro.transform.localScale = new Vector3(0.25f, 1.2f, 0.25f); // setta // riga-ok

        // Monitor / Schermo 3D
        GameObject monitor = GameObject.CreatePrimitive(PrimitiveType.Cube); // setta // riga-ok
        monitor.name = "Schermo_Monitor"; // setta // riga-ok
        monitor.transform.SetParent(terminaleObj.transform, false); // chiama // riga-ok
        monitor.transform.localPosition = new Vector3(0, 1.25f, 0.1f); // setta // riga-ok
        monitor.transform.localScale = new Vector3(0.6f, 0.45f, 0.15f); // setta // riga-ok

        // Luce dello Schermo
        GameObject luceObj = new GameObject("Luce_Schermo"); // setta // riga-ok
        luceObj.transform.SetParent(terminaleObj.transform, false); // chiama // riga-ok
        luceObj.transform.localPosition = new Vector3(0, 1.25f, 0.35f); // setta // riga-ok
        Light luceMonitor = luceObj.AddComponent<Light>(); // setta // riga-ok
        luceMonitor.type = LightType.Point; // setta // riga-ok
        luceMonitor.range = 2.5f; // setta // riga-ok
        luceMonitor.intensity = 1.5f; // setta // riga-ok
        luceMonitor.color = Color.red; // setta // riga-ok

        // Collider di Interazione
        BoxCollider boxCol = terminaleObj.AddComponent<BoxCollider>(); // setta // riga-ok
        boxCol.center = new Vector3(0, 0.9f, 0); // setta // riga-ok
        boxCol.size = new Vector3(0.8f, 1.8f, 0.8f); // setta // riga-ok

        // Componente TerminalePorta
        TerminalePorta terminaleScript = terminaleObj.AddComponent<TerminalePorta>(); // setta // riga-ok
        terminaleScript.porteCollegate.Add(porta); // chiama // riga-ok
        terminaleScript.nomeTerminale = "PANNELLO DI ACCESSO // " + porta.gameObject.name.ToUpper(); // setta // riga-ok
        terminaleScript.codiceSegreto = "4281"; // setta // riga-ok
        terminaleScript.consentiCodicePin = true; // setta // riga-ok
        terminaleScript.consentiBypassElettronico = true; // setta // riga-ok
        terminaleScript.monitorRenderer = monitor.GetComponent<MeshRenderer>(); // setta // riga-ok
        terminaleScript.luceMonitor = luceMonitor; // setta // riga-ok

        porta.terminaleSicurezza = terminaleScript; // setta // riga-ok
        porta.AggiornaFeedbackVisivo(); // chiama // riga-ok
        EditorUtility.SetDirty(porta.gameObject); // chiama // riga-ok

        Undo.RegisterCreatedObjectUndo(terminaleObj, "Crea Terminale Porta"); // chiama // riga-ok
        Selection.activeGameObject = terminaleObj; // setta // riga-ok
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); // chiama // riga-ok

        Debug.Log($"<color=green>[LevelLayoutHelper]</color> Creato con successo Terminale per '{porta.gameObject.name}'!"); // logga // riga-ok

        EditorUtility.DisplayDialog( // ok qua // riga-ok
            "Terminale Creato!", // ok qua // riga-ok
            $"Il Terminale di Sicurezza è stato generato e collegato a '{porta.gameObject.name}':\n\n" + // ok qua // riga-ok
            $"• Codice PIN di sblocco: 4281 (modificabile nell'Inspector)\n" + // ok qua // riga-ok
            $"• Minigioco di Bypass Circuiti attivo\n" + // ok qua // riga-ok
            $"• Interagisci premendo 'E' davanti al monitor durante il gioco!\n\n" + // ok qua // riga-ok
            $"Premi 'Ctrl + S' per salvare.", // ok qua // riga-ok
            "OK" // ok qua // riga-ok
        ); // chiama // riga-ok
    } // chiude // riga-ok

    [MenuItem("Tools/Crea Datapad Olografico con Codici Porte (Verde Acqua LED)")] // nota unity // riga-ok
    // blocco: funzione fa cose
    public static void CreaDatapadOlograficoCodici() // roba pub // riga-ok
    { // apre // riga-ok
        Vector3 spawnPos = Vector3.zero; // setta // riga-ok

        // Cerca posizione davanti alla camera della SceneView o vicino al Player
        // blocco: controlla se va
        if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null) // se ok // riga-ok
        { // apre // riga-ok
            Transform camT = SceneView.lastActiveSceneView.camera.transform; // setta // riga-ok
            spawnPos = camT.position + camT.forward * 3f; // setta // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            GameObject player = GameObject.FindWithTag("Player"); // setta // riga-ok
            // blocco: controlla se va
            if (player != null) // se ok // riga-ok
                spawnPos = player.transform.position + player.transform.forward * 2f + Vector3.up * 0.8f; // setta // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
                spawnPos = new Vector3(0, 1f, 0); // setta // riga-ok
        } // chiude // riga-ok

        // Crea GameObject Root del Datapad
        GameObject datapadObj = new GameObject("Datapad_Ologramma_CodiciPorte"); // setta // riga-ok
        datapadObj.transform.position = spawnPos; // setta // riga-ok
        datapadObj.transform.rotation = Quaternion.Euler(20f, 0f, 0f); // setta // riga-ok

        int layerInteractable = LayerMask.NameToLayer("Interactable"); // setta // riga-ok
        // blocco: controlla se va
        if (layerInteractable != -1) datapadObj.layer = layerInteractable; // se ok // riga-ok

        // Scocca Tablet / Datapad 3D
        GameObject scocca = GameObject.CreatePrimitive(PrimitiveType.Cube); // setta // riga-ok
        scocca.name = "Scocca_Tablet"; // setta // riga-ok
        scocca.transform.SetParent(datapadObj.transform, false); // chiama // riga-ok
        scocca.transform.localPosition = Vector3.zero; // setta // riga-ok
        scocca.transform.localScale = new Vector3(0.45f, 0.04f, 0.30f); // setta // riga-ok

        // Schermo Olografico Emissivo Verde Acqua
        GameObject schermo = GameObject.CreatePrimitive(PrimitiveType.Cube); // setta // riga-ok
        schermo.name = "Schermo_Ologramma"; // setta // riga-ok
        schermo.transform.SetParent(datapadObj.transform, false); // chiama // riga-ok
        schermo.transform.localPosition = new Vector3(0, 0.025f, 0); // setta // riga-ok
        schermo.transform.localScale = new Vector3(0.40f, 0.02f, 0.25f); // setta // riga-ok

        MeshRenderer mrSchermo = schermo.GetComponent<MeshRenderer>(); // setta // riga-ok
        Color aquaColor = new Color(0.0f, 0.95f, 0.85f); // setta // riga-ok
        // blocco: controlla se va
        if (mrSchermo != null && mrSchermo.sharedMaterial != null) // se ok // riga-ok
        { // apre // riga-ok
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? mrSchermo.sharedMaterial.shader); // setta // riga-ok
            mat.name = "Mat_OlogrammaAquaLED"; // setta // riga-ok
            // blocco: controlla se va
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", aquaColor); // se ok // riga-ok
            // blocco: controlla se va
            else if (mat.HasProperty("_Color")) mat.color = aquaColor; // se ok // riga-ok
            // blocco: controlla se va
            if (mat.HasProperty("_EmissionColor")) // se ok // riga-ok
            { // apre // riga-ok
                mat.EnableKeyword("_EMISSION"); // chiama // riga-ok
                mat.SetColor("_EmissionColor", aquaColor * 3f); // chiama // riga-ok
            } // chiude // riga-ok
            mrSchermo.material = mat; // setta // riga-ok
        } // chiude // riga-ok

        // Luce Olografica Point Light
        GameObject luceObj = new GameObject("Luce_Ologramma"); // setta // riga-ok
        luceObj.transform.SetParent(datapadObj.transform, false); // chiama // riga-ok
        luceObj.transform.localPosition = new Vector3(0, 0.15f, 0); // setta // riga-ok
        Light luce = luceObj.AddComponent<Light>(); // setta // riga-ok
        luce.type = LightType.Point; // setta // riga-ok
        luce.color = aquaColor; // setta // riga-ok
        luce.intensity = 2.0f; // setta // riga-ok
        luce.range = 3.0f; // setta // riga-ok

        // Collider di Interazione
        BoxCollider col = datapadObj.AddComponent<BoxCollider>(); // setta // riga-ok
        col.center = Vector3.zero; // setta // riga-ok
        col.size = new Vector3(0.6f, 0.4f, 0.5f); // setta // riga-ok

        // Script DatapadCodiciPorte
        DatapadCodiciPorte script = datapadObj.AddComponent<DatapadCodiciPorte>(); // setta // riga-ok
        script.titoloDatapad = "DATAPAD DI SICUREZZA // REGISTRO CODICI ACCESSO"; // setta // riga-ok
        script.autoreONota = "Ufficio Sicurezza Settore Alpha - Codici di emergenza porte"; // setta // riga-ok
        script.autoRilevaPorteScena = true; // setta // riga-ok
        script.coloreOlogramma = aquaColor; // setta // riga-ok
        script.luceOlogramma = luce; // setta // riga-ok
        script.meshSchermo = mrSchermo; // setta // riga-ok
        script.animaPulsazioneLuce = true; // setta // riga-ok

        Undo.RegisterCreatedObjectUndo(datapadObj, "Crea Datapad Olografico"); // chiama // riga-ok
        Selection.activeGameObject = datapadObj; // setta // riga-ok
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); // chiama // riga-ok

        Debug.Log("<color=cyan>[LevelLayoutHelper]</color> Creato Datapad Ologramma Codici Porte nella scena!"); // logga // riga-ok
        EditorUtility.DisplayDialog( // ok qua // riga-ok
            "Datapad Olografico Creato!", // ok qua // riga-ok
            "Il Datapad Olografico con i PIN delle porte è stato creato nella scena:\n\n" + // ok qua // riga-ok
            "• Rileva in automatico tutti i Terminali e i PIN delle porte presenti!\n" + // ok qua // riga-ok
            "• Schermata LED verde acqua / ciano in stile ologramma sci-fi\n" + // ok qua // riga-ok
            "• Interagisci avvicinandoti e premendo 'E' oppure cliccandoci sopra col mouse.\n\n" + // ok qua // riga-ok
            "Puoi posizionarlo su un tavolo, scrivania o per terra e personalizzare i testi nell'Inspector.", // ok qua // riga-ok
            "OK" // ok qua // riga-ok
        ); // chiama // riga-ok
    } // chiude // riga-ok

    [MenuItem("Tools/Debug/Trova e Seleziona Portellone Uscita (Quarantine Gate)")] // nota unity // riga-ok
    // blocco: funzione fa cose
    public static void TrovaESelezionaPortelloneUscita() // roba pub // riga-ok
    { // apre // riga-ok
        QuarantineGate gate = Object.FindAnyObjectByType<QuarantineGate>(); // setta // riga-ok
        // blocco: controlla se va
        if (gate != null) // se ok // riga-ok
        { // apre // riga-ok
            Selection.activeGameObject = gate.gameObject; // setta // riga-ok
            EditorGUIUtility.PingObject(gate.gameObject); // chiama // riga-ok
            // blocco: controlla se va
            if (SceneView.lastActiveSceneView != null) // se ok // riga-ok
            { // apre // riga-ok
                SceneView.lastActiveSceneView.FrameSelected(); // chiama // riga-ok
            } // chiude // riga-ok

            // blocco: controlla se va
            if (Application.isPlaying) // se ok // riga-ok
            { // apre // riga-ok
                gate.ForzaSbloccoEditor(); // chiama // riga-ok
                Debug.Log($"<color=lime>[DEBUG]</color> Portellone di uscita <b>'{gate.name}'</b> selezionato e SBLOCCATO in Play Mode!"); // logga // riga-ok
            } // chiude // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
            { // apre // riga-ok
                Debug.Log($"<color=cyan>[DEBUG]</color> Portellone di uscita <b>'{gate.name}'</b> selezionato e inquadrato nella scena."); // logga // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            bool crea = EditorUtility.DisplayDialog( // setta // riga-ok
                "Portellone Non Trovato", // ok qua // riga-ok
                "Nessun componente 'QuarantineGate' trovato nella scena corrente.\n\nVuoi creare automaticamente un Portellone di Uscita adesso?", // ok qua // riga-ok
                "Sì, Crea Portellone", // ok qua // riga-ok
                "Annulla" // ok qua // riga-ok
            ); // chiama // riga-ok

            // blocco: controlla se va
            if (crea) // se ok // riga-ok
            { // apre // riga-ok
                CreaPortelloneUscitaQuarantena(); // chiama // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    [MenuItem("Tools/Debug/Crea Portellone Uscita Quarantena nel Settore")] // nota unity // riga-ok
    // blocco: funzione fa cose
    public static void CreaPortelloneUscitaQuarantena() // roba pub // riga-ok
    { // apre // riga-ok
        Vector3 spawnPos = Vector3.zero; // setta // riga-ok
        // blocco: controlla se va
        if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null) // se ok // riga-ok
        { // apre // riga-ok
            Transform camT = SceneView.lastActiveSceneView.camera.transform; // setta // riga-ok
            spawnPos = camT.position + camT.forward * 4f; // setta // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            GameObject player = GameObject.FindWithTag("Player"); // setta // riga-ok
            // blocco: controlla se va
            if (player != null) // se ok // riga-ok
                spawnPos = player.transform.position + player.transform.forward * 3f + Vector3.up * 1f; // setta // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
                spawnPos = new Vector3(0, 1.5f, 0); // setta // riga-ok
        } // chiude // riga-ok

        // Root del Portellone
        GameObject gateObj = new GameObject("Portellone_Quarantena_Uscita"); // setta // riga-ok
        gateObj.transform.position = spawnPos; // setta // riga-ok

        int layerInteractable = LayerMask.NameToLayer("Interactable"); // setta // riga-ok
        // blocco: controlla se va
        if (layerInteractable != -1) gateObj.layer = layerInteractable; // se ok // riga-ok

        // Struttura Telaio / Portale
        GameObject telaio = GameObject.CreatePrimitive(PrimitiveType.Cube); // setta // riga-ok
        telaio.name = "Telaio_Portale"; // setta // riga-ok
        telaio.transform.SetParent(gateObj.transform, false); // chiama // riga-ok
        telaio.transform.localPosition = new Vector3(0, 0, 0); // setta // riga-ok
        telaio.transform.localScale = new Vector3(2.6f, 3.2f, 0.4f); // setta // riga-ok

        // Pannello Porta interna
        GameObject anta = GameObject.CreatePrimitive(PrimitiveType.Cube); // setta // riga-ok
        anta.name = "Pannello_Portellone"; // setta // riga-ok
        anta.transform.SetParent(gateObj.transform, false); // chiama // riga-ok
        anta.transform.localPosition = new Vector3(0, -0.1f, 0); // setta // riga-ok
        anta.transform.localScale = new Vector3(2.2f, 2.8f, 0.2f); // setta // riga-ok

        // Luce di Stato Quarantena
        GameObject luceObj = new GameObject("Luce_Stato_Quarantena"); // setta // riga-ok
        luceObj.transform.SetParent(gateObj.transform, false); // chiama // riga-ok
        luceObj.transform.localPosition = new Vector3(0, 1.4f, -0.3f); // setta // riga-ok
        Light lightComp = luceObj.AddComponent<Light>(); // setta // riga-ok
        lightComp.type = LightType.Point; // setta // riga-ok
        lightComp.color = Color.red; // setta // riga-ok
        lightComp.intensity = 3.5f; // setta // riga-ok
        lightComp.range = 5.0f; // setta // riga-ok

        // Indicatore Visivo Bloccato (Luce/Bordo Rosso)
        GameObject lockedVis = GameObject.CreatePrimitive(PrimitiveType.Sphere); // setta // riga-ok
        lockedVis.name = "Indicatore_Bloccato_Rosso"; // setta // riga-ok
        lockedVis.transform.SetParent(gateObj.transform, false); // chiama // riga-ok
        lockedVis.transform.localPosition = new Vector3(0, 1.4f, -0.25f); // setta // riga-ok
        lockedVis.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f); // setta // riga-ok
        Renderer rLocked = lockedVis.GetComponent<Renderer>(); // setta // riga-ok
        // blocco: controlla se va
        if (rLocked != null) // se ok // riga-ok
        { // apre // riga-ok
            Material m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? rLocked.sharedMaterial.shader); // setta // riga-ok
            m.color = Color.red; // setta // riga-ok
            // blocco: controlla se va
            if (m.HasProperty("_EmissionColor")) // se ok // riga-ok
            { // apre // riga-ok
                m.EnableKeyword("_EMISSION"); // chiama // riga-ok
                m.SetColor("_EmissionColor", Color.red * 2f); // chiama // riga-ok
            } // chiude // riga-ok
            rLocked.material = m; // setta // riga-ok
        } // chiude // riga-ok

        // Indicatore Visivo Sbloccato (Luce/Bordo Verde)
        GameObject unlockedVis = GameObject.CreatePrimitive(PrimitiveType.Sphere); // setta // riga-ok
        unlockedVis.name = "Indicatore_Sbloccato_Verde"; // setta // riga-ok
        unlockedVis.transform.SetParent(gateObj.transform, false); // chiama // riga-ok
        unlockedVis.transform.localPosition = new Vector3(0, 1.4f, -0.25f); // setta // riga-ok
        unlockedVis.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f); // setta // riga-ok
        unlockedVis.SetActive(false); // chiama // riga-ok
        Renderer rUnlocked = unlockedVis.GetComponent<Renderer>(); // setta // riga-ok
        // blocco: controlla se va
        if (rUnlocked != null) // se ok // riga-ok
        { // apre // riga-ok
            Material m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? rUnlocked.sharedMaterial.shader); // setta // riga-ok
            m.color = Color.green; // setta // riga-ok
            // blocco: controlla se va
            if (m.HasProperty("_EmissionColor")) // se ok // riga-ok
            { // apre // riga-ok
                m.EnableKeyword("_EMISSION"); // chiama // riga-ok
                m.SetColor("_EmissionColor", Color.green * 2f); // chiama // riga-ok
            } // chiude // riga-ok
            rUnlocked.material = m; // setta // riga-ok
        } // chiude // riga-ok

        // Collider di interazione fisico
        BoxCollider boxCol = gateObj.AddComponent<BoxCollider>(); // setta // riga-ok
        boxCol.size = new Vector3(2.8f, 3.4f, 1.5f); // setta // riga-ok
        boxCol.center = Vector3.zero; // setta // riga-ok

        // Script QuarantineGate
        QuarantineGate qGate = gateObj.AddComponent<QuarantineGate>(); // setta // riga-ok
        var statusLightField = typeof(QuarantineGate).GetField("statusLight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance); // setta // riga-ok
        // blocco: controlla se va
        if (statusLightField != null) statusLightField.SetValue(qGate, lightComp); // se ok // riga-ok

        var lockedVisualField = typeof(QuarantineGate).GetField("lockedVisual", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance); // setta // riga-ok
        // blocco: controlla se va
        if (lockedVisualField != null) lockedVisualField.SetValue(qGate, lockedVis); // se ok // riga-ok

        var unlockedVisualField = typeof(QuarantineGate).GetField("unlockedVisual", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance); // setta // riga-ok
        // blocco: controlla se va
        if (unlockedVisualField != null) unlockedVisualField.SetValue(qGate, unlockedVis); // se ok // riga-ok

        Undo.RegisterCreatedObjectUndo(gateObj, "Crea Portellone Quarantena"); // chiama // riga-ok
        Selection.activeGameObject = gateObj; // setta // riga-ok
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); // chiama // riga-ok

        Debug.Log("<color=green>[LevelLayoutHelper]</color> Portellone di Quarantena creato con successo!"); // logga // riga-ok
        EditorUtility.DisplayDialog( // ok qua // riga-ok
            "Portellone Quarantena Creato!", // ok qua // riga-ok
            "Il Portellone di Uscita/Quarantena è stato posizionato nella scena:\n\n" + // ok qua // riga-ok
            "• Layer impostato su 'Interactable'\n" + // ok qua // riga-ok
            "• Script QuarantineGate con gestione luci (Rosso=Bloccato, Verde=Sbloccato)\n" + // setta // riga-ok
            "• Per testarlo subito in Play Mode puoi premere 'F6' (sblocca) o 'F7' (completa).\n\n" + // ok qua // riga-ok
            "Posizionalo dove preferisci e salva la scena (Ctrl + S).", // ok qua // riga-ok
            "OK" // ok qua // riga-ok
        ); // chiama // riga-ok
    } // chiude // riga-ok

    [MenuItem("Tools/Debug/Forza Sblocco Estrazione (In Play Mode)")] // nota unity // riga-ok
    // blocco: funzione fa cose
    public static void ForzaSbloccoInPlayMode() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (!Application.isPlaying) // se ok // riga-ok
        { // apre // riga-ok
            EditorUtility.DisplayDialog("Avviso", "Questa opzione funziona solo mentre sei in PLAY MODE!", "OK"); // chiama // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        QuarantineGate gate = Object.FindAnyObjectByType<QuarantineGate>(); // setta // riga-ok
        // blocco: controlla se va
        if (gate != null) // se ok // riga-ok
        { // apre // riga-ok
            gate.ForzaSbloccoEditor(); // chiama // riga-ok
        } // chiude // riga-ok
        // blocco: controlla se va
        else if (MissionManager.Instance != null) // se ok // riga-ok
        { // apre // riga-ok
            MissionManager.Instance.ForzaSbloccoEstrazioneDebug(); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    [MenuItem("Tools/Debug/Risolvi Stato Emergenza e Completa Tutti i Task (In Play Mode)")] // nota unity // riga-ok
    // blocco: funzione fa cose
    public static void RisolviEmergenzaETaskInPlayMode() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (!Application.isPlaying) // se ok // riga-ok
        { // apre // riga-ok
            EditorUtility.DisplayDialog("Avviso", "Questa opzione funziona solo mentre sei in PLAY MODE!", "OK"); // chiama // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (MissionManager.Instance != null) // se ok // riga-ok
        { // apre // riga-ok
            MissionManager.Instance.RisolviStatoEmergenzaETuttiTaskDebug(); // chiama // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            EditorUtility.DisplayDialog("Avviso", "MissionManager non trovato nella scena attiva!", "OK"); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    [MenuItem("Tools/Debug/Crea Cubo Nero Teletrasporto (Trigger Uscita Scena)")] // nota unity // riga-ok
    // blocco: funzione fa cose
    public static void CreaCuboNeroTeletrasporto() // roba pub // riga-ok
    { // apre // riga-ok
        Vector3 spawnPos = Vector3.zero; // setta // riga-ok
        // blocco: controlla se va
        if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null) // se ok // riga-ok
        { // apre // riga-ok
            Transform camT = SceneView.lastActiveSceneView.camera.transform; // setta // riga-ok
            spawnPos = camT.position + camT.forward * 3f; // setta // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            GameObject player = GameObject.FindWithTag("Player"); // setta // riga-ok
            // blocco: controlla se va
            if (player != null) // se ok // riga-ok
                spawnPos = player.transform.position + player.transform.forward * 3f + Vector3.up * 1f; // setta // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
                spawnPos = new Vector3(0, 1.5f, 0); // setta // riga-ok
        } // chiude // riga-ok

        GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube); // setta // riga-ok
        cubo.name = "Cubo_Nero_Teletrasporto"; // setta // riga-ok
        cubo.transform.position = spawnPos; // setta // riga-ok
        cubo.transform.localScale = new Vector3(2.5f, 3.0f, 2.5f); // setta // riga-ok

        // Imposta il BoxCollider come Trigger
        BoxCollider col = cubo.GetComponent<BoxCollider>(); // setta // riga-ok
        // blocco: controlla se va
        if (col != null) // se ok // riga-ok
        { // apre // riga-ok
            col.isTrigger = true; // setta // riga-ok
        } // chiude // riga-ok

        // Materiale Nero Lucido / Sci-Fi
        MeshRenderer mr = cubo.GetComponent<MeshRenderer>(); // setta // riga-ok
        // blocco: controlla se va
        if (mr != null) // se ok // riga-ok
        { // apre // riga-ok
            Material matNero = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")); // setta // riga-ok
            matNero.name = "Mat_CuboNero_Portale"; // setta // riga-ok
            Color blackColor = new Color(0.02f, 0.02f, 0.02f, 0.95f); // setta // riga-ok
            // blocco: controlla se va
            if (matNero.HasProperty("_BaseColor")) matNero.SetColor("_BaseColor", blackColor); // se ok // riga-ok
            // blocco: controlla se va
            else if (matNero.HasProperty("_Color")) matNero.color = blackColor; // se ok // riga-ok
            // blocco: controlla se va
            if (matNero.HasProperty("_Smoothness")) matNero.SetFloat("_Smoothness", 0.9f); // se ok // riga-ok
            // blocco: controlla se va
            if (matNero.HasProperty("_Metallic")) matNero.SetFloat("_Metallic", 0.5f); // se ok // riga-ok
            mr.material = matNero; // setta // riga-ok
        } // chiude // riga-ok

        // Aggiunge lo script di transizione
        CuboNeroTeletrasporto script = cubo.AddComponent<CuboNeroTeletrasporto>(); // setta // riga-ok

        Undo.RegisterCreatedObjectUndo(cubo, "Crea Cubo Nero Teletrasporto"); // chiama // riga-ok
        Selection.activeGameObject = cubo; // setta // riga-ok
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); // chiama // riga-ok

        Debug.Log("<color=black><color=white>[LevelLayoutHelper]</color></color> Creato 'Cubo_Nero_Teletrasporto' con trigger attivo!"); // logga // riga-ok
        EditorUtility.DisplayDialog( // ok qua // riga-ok
            "Cubo Nero Teletrasporto Creato!", // ok qua // riga-ok
            "Il Cubo Nero Triggered è stato creato nella scena:\n\n" + // ok qua // riga-ok
            "• BoxCollider con 'Is Trigger' attivo\n" + // ok qua // riga-ok
            "• Script CuboNeroTeletrasporto collegato\n" + // ok qua // riga-ok
            "• Quando il Player ci cammina dentro a fine crisi (o con debug attivo), viene teletrasportato alla scena successiva!\n\n" + // ok qua // riga-ok
            "Puoi posizionarlo dietro alle porte di uscita e salvare la scena (Ctrl + S).", // ok qua // riga-ok
            "OK" // ok qua // riga-ok
        ); // chiama // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
#endif // prep ok // riga-ok
