// ============================================================================
// Crisis Protocol / Sector Containment - Utility editor
// File: .\Assets\Editor\Settore2ServerSetup.cs
// Responsabilita': automatizza setup, popolamento scena, salvataggio, validazione o manutenzione direttamente dentro Unity Editor.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
#if UNITY_EDITOR // prep ok // riga-ok
using UnityEditor; // usa lib // riga-ok
using UnityEditor.SceneManagement; // usa lib // riga-ok
using UnityEngine; // usa lib // riga-ok
using System.Collections.Generic; // usa lib // riga-ok

/// <summary>
/// Editor tool per configurare il Settore 2: server rack sui muri, cavi modulari a terra e sul soffitto,
/// container in un angolo con hotspot di emergenza (scossa elettrica + perdita gas).
/// Aprire da: CrisisProtocol > Settore 2 > Configura Server, Cavi e Container
/// </summary>
// blocco: classe x roba grossa
public class Settore2ServerSetup : EditorWindow // classe qui // riga-ok
{ // apre // riga-ok
    // ─── GUID Assets ───────────────────────────────────────────────────────
    // wires.fbx (cavi modulari)
    private const string GUID_WIRES_FBX = "986fdbec5cb35764683041a3a0c91b5e"; // roba pub // riga-ok
    // rak_server.glb
    private const string GUID_RAK_SERVER = "8c0bd40c0f4c95e4ea1bb2ef5a37de1a"; // roba pub // riga-ok

    // ─── PARAMETRI SERVER ──────────────────────────────────────────────────
    private float serverHeight  = 2.0f; // roba pub // riga-ok
    private float serverWidth   = 0.6f; // roba pub // riga-ok
    private float serverDepth   = 0.6f; // roba pub // riga-ok
    private float serverSpacing = 0.70f; // roba pub // riga-ok
    private bool  autoFitServer = true; // roba pub // riga-ok
    private int   serverPerWall = 4; // roba pub // riga-ok

    // ─── PARAMETRI CAVI ────────────────────────────────────────────────────
    private int   cableFloor   = 5; // roba pub // riga-ok
    private int   cableCeiling = 3; // roba pub // riga-ok
    private float segLen       = 1.0f; // roba pub // riga-ok

    // ─── PARAMETRI CONTAINER ───────────────────────────────────────────────
    private Vector3 containerSize = new Vector3(2.4f, 2.0f, 4.0f); // roba pub // riga-ok

    // ─── UI ────────────────────────────────────────────────────────────────
    private bool   fServer    = true; // roba pub // riga-ok
    private bool   fWires     = true; // roba pub // riga-ok
    private bool   fContainer = true; // roba pub // riga-ok
    private Vector2 scroll; // roba pub // riga-ok

    [MenuItem("CrisisProtocol/Settore 2/Configura Server, Cavi e Container")] // nota unity // riga-ok
    [MenuItem("Tools/Settore 2 – Server, Cavi e Container")] // nota unity // riga-ok
    // blocco: funzione fa cose
    public static void ApriFinestra() // roba pub // riga-ok
    { // apre // riga-ok
        var win = GetWindow<Settore2ServerSetup>("Settore 2 Setup", true); // setta // riga-ok
        win.minSize = new Vector2(420, 560); // setta // riga-ok
        win.Show(); // chiama // riga-ok
    } // chiude // riga-ok

    // ══════════════════════════════════════════════════════════════════════
    // GUI
    // ══════════════════════════════════════════════════════════════════════
    // blocco: funzione fa cose
    private void OnGUI() // roba pub // riga-ok
    { // apre // riga-ok
        // Struttura semplice: niente try/catch attorno a BeginScrollView/EndScrollView.
        // ExitGUIException (lanciata da DisplayDialog) si propaga naturalmente a Unity
        // senza bisogno di wrapper — qualsiasi catch che la intercetta rompe lo stack GUI.
        scroll = EditorGUILayout.BeginScrollView(scroll); // setta // riga-ok
        DisegnaContenuto(); // chiama // riga-ok
        EditorGUILayout.EndScrollView(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void DisegnaContenuto() // roba pub // riga-ok
    { // apre // riga-ok
        EditorGUILayout.Space(8); // chiama // riga-ok
        EditorGUILayout.LabelField("Settore 2 – Layout Automatico", EditorStyles.boldLabel); // chiama // riga-ok
        EditorGUILayout.HelpBox( // ok qua // riga-ok
            "Posiziona i server rack sui 4 muri interni, i cavi modulari (wires.fbx) " + // ok qua // riga-ok
            "a terra e soffitto, e il container di emergenza in un angolo.\n" + // ok qua // riga-ok
            "Assicurati di avere la scena Settore 2 aperta e attiva.", // ok qua // riga-ok
            MessageType.Info); // chiama // riga-ok

        EditorGUILayout.Space(6); // chiama // riga-ok

        // ── SERVER ──────────────────────────────────────────────────────────
        fServer = EditorGUILayout.Foldout(fServer, "Server Rack (muri interni)", true, EditorStyles.foldoutHeader); // setta // riga-ok
        // blocco: controlla se va
        if (fServer) // se ok // riga-ok
        { // apre // riga-ok
            EditorGUI.indentLevel++; // ok qua // riga-ok
            serverHeight  = EditorGUILayout.Slider("Altezza Rack (m)",       serverHeight,  1.0f, 3.0f); // setta // riga-ok
            serverWidth   = EditorGUILayout.Slider("Larghezza Rack (m)",      serverWidth,   0.3f, 1.5f); // setta // riga-ok
            serverDepth   = EditorGUILayout.Slider("Profondità Rack (m)",     serverDepth,   0.3f, 1.0f); // setta // riga-ok
            serverSpacing = EditorGUILayout.Slider("Passo centro-centro (m)", serverSpacing, 0.4f, 2.0f); // setta // riga-ok
            autoFitServer = EditorGUILayout.Toggle("Auto-fit muro",           autoFitServer); // setta // riga-ok
            // blocco: controlla se va
            if (!autoFitServer) // se ok // riga-ok
                serverPerWall = EditorGUILayout.IntSlider("Server per muro",  serverPerWall, 1, 20); // setta // riga-ok
            EditorGUI.indentLevel--; // ok qua // riga-ok
        } // chiude // riga-ok

        EditorGUILayout.Space(4); // chiama // riga-ok

        // ── CAVI ────────────────────────────────────────────────────────────
        fWires = EditorGUILayout.Foldout(fWires, "Cavi Modulari (wires.fbx)", true, EditorStyles.foldoutHeader); // setta // riga-ok
        // blocco: controlla se va
        if (fWires) // se ok // riga-ok
        { // apre // riga-ok
            EditorGUI.indentLevel++; // ok qua // riga-ok
            cableFloor   = EditorGUILayout.IntSlider("Sezioni a terra",      cableFloor,   1, 20); // setta // riga-ok
            cableCeiling = EditorGUILayout.IntSlider("Sezioni soffitto",     cableCeiling, 1, 10); // setta // riga-ok
            segLen       = EditorGUILayout.Slider("Lunghezza segmento (m)",  segLen,       0.5f, 3.0f); // setta // riga-ok
            EditorGUI.indentLevel--; // ok qua // riga-ok
        } // chiude // riga-ok

        EditorGUILayout.Space(4); // chiama // riga-ok

        // ── CONTAINER ───────────────────────────────────────────────────────
        fContainer = EditorGUILayout.Foldout(fContainer, "Container Emergenza (angolo NW)", true, EditorStyles.foldoutHeader); // setta // riga-ok
        // blocco: controlla se va
        if (fContainer) // se ok // riga-ok
        { // apre // riga-ok
            EditorGUI.indentLevel++; // ok qua // riga-ok
            containerSize = EditorGUILayout.Vector3Field("Dimensioni (L,H,P)", containerSize); // setta // riga-ok
            EditorGUILayout.HelpBox("MeshCollider + EmergencyHotspot gas + scintille elettriche.", MessageType.None); // chiama // riga-ok
            EditorGUI.indentLevel--; // ok qua // riga-ok
        } // chiude // riga-ok

        EditorGUILayout.Space(10); // chiama // riga-ok

        // ── PULSANTI ────────────────────────────────────────────────────────
        // IMPORTANTE: tutte le azioni sono deferite con EditorApplication.delayCall
        // In questo modo il frame OnGUI (incluso EndScrollView) termina normalmente
        // PRIMA che qualsiasi operazione pesante venga eseguita.
        // Questo è l'unico pattern che evita in modo affidabile EndLayoutGroup errors.

        GUI.backgroundColor = new Color(0.25f, 0.82f, 0.42f); // setta // riga-ok
        // blocco: controlla se va
        if (GUILayout.Button("▶  ESEGUI SETUP COMPLETO", GUILayout.Height(40))) // se ok // riga-ok
            EditorApplication.delayCall += EseguiSetupCompleto; // setta // riga-ok
        GUI.backgroundColor = Color.white; // setta // riga-ok

        EditorGUILayout.Space(4); // chiama // riga-ok

        // blocco: controlla se va
        if (GUILayout.Button("Solo Server sui Muri", GUILayout.Height(26))) // se ok // riga-ok
            EditorApplication.delayCall += PosizionaServer; // setta // riga-ok

        // blocco: controlla se va
        if (GUILayout.Button("Solo Cavi (terra + soffitto)", GUILayout.Height(26))) // se ok // riga-ok
            EditorApplication.delayCall += PosizionaCavi; // setta // riga-ok

        // blocco: controlla se va
        if (GUILayout.Button("Solo Container con Hotspot", GUILayout.Height(26))) // se ok // riga-ok
            EditorApplication.delayCall += CreaContainer; // setta // riga-ok

        EditorGUILayout.Space(4); // chiama // riga-ok
        GUI.backgroundColor = new Color(1f, 0.45f, 0.18f); // setta // riga-ok
        // blocco: controlla se va
        if (GUILayout.Button("Pulizia: Rimuovi layout Settore 2", GUILayout.Height(26))) // se ok // riga-ok
            EditorApplication.delayCall += () => // setta // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (EditorUtility.DisplayDialog("Conferma", "Rimuovere tutti gli oggetti generati?", "Sì", "No")) // se ok // riga-ok
                    Pulisci(); // chiama // riga-ok
            }; // ok qua // riga-ok
        GUI.backgroundColor = Color.white; // setta // riga-ok
    } // chiude // riga-ok

    // ══════════════════════════════════════════════════════════════════════
    // SETUP COMPLETO
    // ══════════════════════════════════════════════════════════════════════
    // blocco: funzione fa cose
    private void EseguiSetupCompleto() // roba pub // riga-ok
    { // apre // riga-ok
        PosizionaServer(); // chiama // riga-ok
        PosizionaCavi(); // chiama // riga-ok
        CreaContainer(); // chiama // riga-ok

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); // chiama // riga-ok
        AutoSceneSaver.EseguiSalvataggioAutomatico(forzaAncheSeNonDirty: true); // chiama // riga-ok

        // delayCall: mostra il dialogo di riepilogo DOPO che OnGUI ha completato
        // il frame corrente (EndScrollView incluso). Senza questo, DisplayDialog
        // lancia ExitGUIException che sfugge prima di EndScrollView → crash.
        EditorApplication.delayCall += () => // setta // riga-ok
            EditorUtility.DisplayDialog( // ok qua // riga-ok
                "Settore 2 – Setup Completato!", // ok qua // riga-ok
                "✔ Server Rack posizionati sui 4 muri interni con MeshCollider\n" + // ok qua // riga-ok
                "✔ Cavi modulari (wires.fbx) a terra e soffitto\n" + // ok qua // riga-ok
                "✔ Container emergenza in angolo NW con EmergencyHotspot\n" + // ok qua // riga-ok
                "✔ Scena salvata\n\n" + // ok qua // riga-ok
                "Ispeziona gli oggetti in Hierarchy sotto:\n" + // ok qua // riga-ok
                "Settore2_ServerRacks / Settore2_Cavi / Settore2_Container", // ok qua // riga-ok
                "OK"); // chiama // riga-ok
    } // chiude // riga-ok

    // ══════════════════════════════════════════════════════════════════════
    // SERVER
    // ══════════════════════════════════════════════════════════════════════
    // blocco: funzione fa cose
    private void PosizionaServer() // roba pub // riga-ok
    { // apre // riga-ok
        Bounds b = CalcolaBounds(); // setta // riga-ok
        // blocco: controlla se va
        if (b.size == Vector3.zero) { MsgNessunEdificio(); return; } // se ok // riga-ok

        float floorY    = b.min.y; // setta // riga-ok
        float buildH    = b.max.y - floorY; // setta // riga-ok
        float rackH     = Mathf.Min(serverHeight, buildH * 0.92f); // setta // riga-ok
        float rackY     = floorY + rackH * 0.5f; // setta // riga-ok

        GameObject serverPrefab = CaricaAsset(GUID_RAK_SERVER); // setta // riga-ok
        GameObject root         = GetOrCreate("Settore2_ServerRacks"); // setta // riga-ok

        // 4 muri interni: il WallCenter usa floorY come Y di riferimento.
        // L'offset X/Z sposta il rack appena dentro il muro (spessore depth/2 + 1cm).
        // La Y finale viene sovrascritta da pos.y = rackY (centro del rack in altezza).
        float wallY = floorY; // riferimento quota pavimento // setta // riga-ok
        var walls = new[] // setta // riga-ok
        { // apre // riga-ok
            new WallData("Nord",  Vector3.right,   new Vector3(b.center.x, wallY, b.max.z), b.size.x, new Vector3( 0, 0,-(serverDepth*0.5f+0.02f)),   0f), // ok qua // riga-ok
            new WallData("Sud",   Vector3.right,   new Vector3(b.center.x, wallY, b.min.z), b.size.x, new Vector3( 0, 0,  serverDepth*0.5f+0.02f),  180f), // ok qua // riga-ok
            new WallData("Est",   Vector3.forward, new Vector3(b.max.x,    wallY, b.center.z), b.size.z, new Vector3(-(serverDepth*0.5f+0.02f), 0, 0),  90f), // ok qua // riga-ok
            new WallData("Ovest", Vector3.forward, new Vector3(b.min.x,    wallY, b.center.z), b.size.z, new Vector3(  serverDepth*0.5f+0.02f, 0, 0), -90f), // ok qua // riga-ok
        }; // ok qua // riga-ok

        int totale = 0; // setta // riga-ok
        // blocco: gira piu volte
        foreach (var w in walls) // ciclo x // riga-ok
        { // apre // riga-ok
            int count = autoFitServer // setta // riga-ok
                ? Mathf.Max(1, Mathf.FloorToInt((w.WallLen - serverWidth) / serverSpacing)) // chiama // riga-ok
                : serverPerWall; // ok qua // riga-ok
            count = Mathf.Min(count, 30); // setta // riga-ok

            float totalLen = (count - 1) * serverSpacing; // setta // riga-ok
            float startT   = -totalLen * 0.5f; // setta // riga-ok

            // blocco: gira piu volte
            for (int i = 0; i < count; i++) // ciclo x // riga-ok
            { // apre // riga-ok
                float   t   = startT + i * serverSpacing; // setta // riga-ok
                Vector3 pos = w.WallCenter + w.Axis * t + w.Offset; // setta // riga-ok
                pos.y = rackY; // setta // riga-ok

                GameObject server; // ok qua // riga-ok
                // blocco: controlla se va
                if (serverPrefab != null) // se ok // riga-ok
                { // apre // riga-ok
                    server = (GameObject)PrefabUtility.InstantiatePrefab(serverPrefab); // setta // riga-ok
                    server.transform.position   = pos; // setta // riga-ok
                    server.transform.rotation   = Quaternion.Euler(0, w.Rot, 0); // setta // riga-ok
                    server.transform.localScale = CalcolaScalaServer(server, rackH); // setta // riga-ok
                } // chiude // riga-ok
                // blocco: caso diverso
                else // se no // riga-ok
                { // apre // riga-ok
                    server = CuboServer(pos, w.Rot, rackH); // setta // riga-ok
                } // chiude // riga-ok

                server.name = $"Server_{w.Name}_{i + 1:D2}"; // setta // riga-ok
                server.transform.SetParent(root.transform, true); // chiama // riga-ok
                AggiornaMeshCollider(server); // chiama // riga-ok
                Undo.RegisterCreatedObjectUndo(server, "Crea Server"); // chiama // riga-ok
                totale++; // ok qua // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); // chiama // riga-ok
        Debug.Log($"<color=cyan>[Settore2Setup]</color> Creati <b>{totale}</b> server rack sui 4 muri."); // logga // riga-ok
    } // chiude // riga-ok

    // ══════════════════════════════════════════════════════════════════════
    // CAVI
    // ══════════════════════════════════════════════════════════════════════
    // blocco: funzione fa cose
    private void PosizionaCavi() // roba pub // riga-ok
    { // apre // riga-ok
        Bounds b = CalcolaBounds(); // setta // riga-ok
        // blocco: controlla se va
        if (b.size == Vector3.zero) { MsgNessunEdificio(); return; } // se ok // riga-ok

        float floorY = b.min.y + 0.02f; // setta // riga-ok
        float ceilY  = b.max.y - 0.08f; // setta // riga-ok

        GameObject wireAsset = CaricaAsset(GUID_WIRES_FBX); // setta // riga-ok
        GameObject root      = GetOrCreate("Settore2_Cavi"); // setta // riga-ok

        // ── A TERRA ──────────────────────────────────────────────────────
        GameObject rootTerra = new GameObject("Cavi_Terra"); // setta // riga-ok
        rootTerra.transform.SetParent(root.transform, false); // chiama // riga-ok

        // Linea X: da ovest verso centro
        FilaCavi(wireAsset, rootTerra, "Terra_LineaX", // ok qua // riga-ok
            new Vector3(b.min.x + 0.3f, floorY, b.center.z + 0.6f), // ok qua // riga-ok
            Vector3.right, cableFloor, segLen, Quaternion.identity); // chiama // riga-ok

        // Linea Z: da sud verso centro
        FilaCavi(wireAsset, rootTerra, "Terra_LineaZ", // ok qua // riga-ok
            new Vector3(b.center.x - 0.5f, floorY, b.min.z + 0.3f), // ok qua // riga-ok
            Vector3.forward, cableFloor, segLen, Quaternion.Euler(0, 90, 0)); // chiama // riga-ok

        // Gomito diagonale (area server Est)
        FilaCavi(wireAsset, rootTerra, "Terra_Gomito", // ok qua // riga-ok
            new Vector3(b.max.x - 1.2f, floorY, b.center.z - 0.8f), // ok qua // riga-ok
            Vector3.left, Mathf.Max(1, cableFloor / 2), segLen, Quaternion.Euler(0, 35, 0)); // chiama // riga-ok

        // ── SUL SOFFITTO ─────────────────────────────────────────────────
        GameObject rootSoffitto = new GameObject("Cavi_Soffitto"); // setta // riga-ok
        rootSoffitto.transform.SetParent(root.transform, false); // chiama // riga-ok

        // Cavi soffitto: niente rotazione capovolta — il modello FBX è già orientato
        // correttamente. Basta posizionarli a ceilY; usare Euler(180,x,x) capovolgeva
        // la mesh rendendola invisibile o invertita.
        FilaCavi(wireAsset, rootSoffitto, "Soffitto_LineaX", // ok qua // riga-ok
            new Vector3(b.min.x + 0.5f, ceilY, b.center.z), // ok qua // riga-ok
            Vector3.right,   cableCeiling, segLen, Quaternion.identity); // chiama // riga-ok

        FilaCavi(wireAsset, rootSoffitto, "Soffitto_LineaZ", // ok qua // riga-ok
            new Vector3(b.center.x + 0.3f, ceilY, b.min.z + 0.5f), // ok qua // riga-ok
            Vector3.forward, cableCeiling, segLen, Quaternion.Euler(0, 90, 0)); // chiama // riga-ok

        // ── HOTSPOT SCOSSA su cavo a terra ────────────────────────────────
        Transform grpX = rootTerra.transform.Find("Terra_LineaX"); // setta // riga-ok
        // blocco: controlla se va
        if (grpX != null && grpX.childCount > 0) // se ok // riga-ok
        { // apre // riga-ok
            int idx  = Mathf.Min(2, grpX.childCount - 1); // setta // riga-ok
            Transform cavo = grpX.GetChild(idx); // setta // riga-ok
            CreaHotspotScossaSuCavo(cavo.gameObject); // chiama // riga-ok
        } // chiude // riga-ok

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); // chiama // riga-ok
        Debug.Log("<color=cyan>[Settore2Setup]</color> Cavi modulari posizionati a terra e soffitto."); // logga // riga-ok
    } // chiude // riga-ok

    private void FilaCavi(GameObject wireAsset, GameObject parent, string groupName, // roba pub // riga-ok
        Vector3 start, Vector3 direction, int count, float sl, Quaternion rot) // chiama // riga-ok
    { // apre // riga-ok
        GameObject grp = new GameObject(groupName); // setta // riga-ok
        grp.transform.SetParent(parent.transform, false); // chiama // riga-ok

        // blocco: gira piu volte
        for (int i = 0; i < count; i++) // ciclo x // riga-ok
        { // apre // riga-ok
            Vector3 pos = start + direction * (i * sl); // setta // riga-ok
            GameObject seg; // ok qua // riga-ok

            // blocco: controlla se va
            if (wireAsset != null) // se ok // riga-ok
            { // apre // riga-ok
                seg = (GameObject)PrefabUtility.InstantiatePrefab(wireAsset); // setta // riga-ok
                seg.transform.position   = pos; // setta // riga-ok
                seg.transform.rotation   = rot; // setta // riga-ok
                Vector3 sz   = GetRendererBounds(seg); // setta // riga-ok
                float modelL = Mathf.Max(sz.z, sz.x, 0.01f); // setta // riga-ok
                float s      = sl / modelL; // setta // riga-ok
                seg.transform.localScale = new Vector3(s, s, s); // setta // riga-ok
            } // chiude // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
            { // apre // riga-ok
                seg = GameObject.CreatePrimitive(PrimitiveType.Cylinder); // setta // riga-ok
                seg.transform.position   = pos; // setta // riga-ok
                seg.transform.rotation   = rot * Quaternion.Euler(90, 0, 0); // setta // riga-ok
                seg.transform.localScale = new Vector3(0.04f, sl * 0.5f, 0.04f); // setta // riga-ok
                ColoreScuro(seg, new Color(0.1f, 0.1f, 0.1f)); // chiama // riga-ok
            } // chiude // riga-ok

            seg.name = $"{groupName}_Seg{i + 1:D2}"; // setta // riga-ok
            seg.transform.SetParent(grp.transform, true); // chiama // riga-ok
            Undo.RegisterCreatedObjectUndo(seg, "Crea Cavo"); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void CreaHotspotScossaSuCavo(GameObject cavo) // roba pub // riga-ok
    { // apre // riga-ok
        GameObject hGo = new GameObject("Cavo_Hotspot_ScossaElettr"); // setta // riga-ok
        hGo.transform.SetParent(cavo.transform, false); // chiama // riga-ok
        hGo.transform.localPosition = new Vector3(0, 0.05f, 0); // setta // riga-ok

        BoxCollider box  = hGo.AddComponent<BoxCollider>(); // setta // riga-ok
        box.isTrigger    = true; // setta // riga-ok
        box.size         = new Vector3(0.8f, 0.25f, segLen); // setta // riga-ok
        box.center       = Vector3.zero; // setta // riga-ok

        EmergencyHotspot hs = hGo.AddComponent<EmergencyHotspot>(); // setta // riga-ok
        // 0.20 = particelle ridotte — il cavo è un oggetto sottile a terra
        SetHotspot(hs, "CABLE_FAULT_S2", "KEYCARD_S2", EmergencyHotspot.HotspotVisualType.ElectricSparks, 0.20f); // chiama // riga-ok

        CreaSparks(hGo); // chiama // riga-ok
        Undo.RegisterCreatedObjectUndo(hGo, "Crea HotspotCavo"); // chiama // riga-ok
        Debug.Log("<color=yellow>[Settore2Setup]</color> Hotspot scossa su cavo creato."); // logga // riga-ok
    } // chiude // riga-ok

    // ══════════════════════════════════════════════════════════════════════
    // CONTAINER
    // ══════════════════════════════════════════════════════════════════════
    // blocco: funzione fa cose
    private void CreaContainer() // roba pub // riga-ok
    { // apre // riga-ok
        Bounds b = CalcolaBounds(); // setta // riga-ok
        // blocco: controlla se va
        if (b.size == Vector3.zero) { MsgNessunEdificio(); return; } // se ok // riga-ok

        float floorY  = b.min.y; // setta // riga-ok
        float margine = 0.05f; // setta // riga-ok

        Vector3 pos = new Vector3( // setta // riga-ok
            b.min.x + containerSize.x * 0.5f + margine, // ok qua // riga-ok
            floorY  + containerSize.y * 0.5f, // ok qua // riga-ok
            b.max.z - containerSize.z * 0.5f - margine // ok qua // riga-ok
        ); // chiama // riga-ok

        // Corpo container
        GameObject cont = GameObject.CreatePrimitive(PrimitiveType.Cube); // setta // riga-ok
        cont.name = "Container_Emergenza_S2"; // setta // riga-ok
        cont.transform.position   = pos; // setta // riga-ok
        cont.transform.localScale = containerSize; // setta // riga-ok
        ColoreScuro(cont, new Color(0.18f, 0.21f, 0.17f)); // chiama // riga-ok
        AggiornaMeshCollider(cont); // chiama // riga-ok

        // Hotspot figlio: localPosition in coordinate locali del container
        // (scale già applicata al parent). Valori in [-0.5, 0.5] per restare
        // dentro il cubo. Z=0.48 = quasi sulla faccia frontale (punto di perdita).
        GameObject hGo = new GameObject("Hotspot_Gas_Scossa"); // setta // riga-ok
        hGo.transform.SetParent(cont.transform, false); // chiama // riga-ok
        hGo.transform.localPosition = new Vector3(0f, 0.35f, 0.48f); // setta // riga-ok

        // BoxCollider: le dimensioni sono in spazio locale del hotspot
        // (il parent è scalato, quindi 1 unità locale = 1 unità world / containerScale).
        BoxCollider hBox = hGo.AddComponent<BoxCollider>(); // setta // riga-ok
        hBox.isTrigger   = true; // setta // riga-ok
        hBox.size        = new Vector3(0.9f, 0.7f, 0.15f); // larghezza/altezza/profondità in locale // setta // riga-ok
        hBox.center      = Vector3.zero; // setta // riga-ok

        EmergencyHotspot hs = hGo.AddComponent<EmergencyHotspot>(); // setta // riga-ok
        // 0.25 = particelle ridotte al 25% — il container è piccolo (2.4 x 2.0 x 4.0 m)
        SetHotspot(hs, "CONTAINER_FAULT_S2", "KEYCARD_S2", EmergencyHotspot.HotspotVisualType.ToxicGasLeak, 0.25f); // chiama // riga-ok
        CreaSparks(hGo); // chiama // riga-ok

        // Luce rossa allarme
        GameObject lGo = new GameObject("Luce_Allarme"); // setta // riga-ok
        lGo.transform.SetParent(cont.transform, false); // chiama // riga-ok
        lGo.transform.localPosition = new Vector3(0, 0.45f, 0); // setta // riga-ok
        Light lt    = lGo.AddComponent<Light>(); // setta // riga-ok
        lt.type     = LightType.Point; // setta // riga-ok
        lt.color    = new Color(1f, 0.08f, 0.04f); // setta // riga-ok
        lt.intensity = 3f; // setta // riga-ok
        lt.range    = 5f; // setta // riga-ok

        GameObject root = GetOrCreate("Settore2_Container"); // setta // riga-ok
        cont.transform.SetParent(root.transform, true); // chiama // riga-ok

        Undo.RegisterCreatedObjectUndo(cont, "Crea Container"); // chiama // riga-ok
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); // chiama // riga-ok
        Selection.activeGameObject = cont; // setta // riga-ok

        Debug.Log("<color=lime>[Settore2Setup]</color> Container emergenza creato nell'angolo NW."); // logga // riga-ok
    } // chiude // riga-ok

    // ══════════════════════════════════════════════════════════════════════
    // SCINTILLE ELETTRICHE
    // ══════════════════════════════════════════════════════════════════════
    // blocco: funzione fa cose
    private static void CreaSparks(GameObject parent) // roba pub // riga-ok
    { // apre // riga-ok
        GameObject go = new GameObject("VFX_Scintille"); // setta // riga-ok
        go.transform.SetParent(parent.transform, false); // chiama // riga-ok
        go.transform.localPosition = Vector3.zero; // setta // riga-ok

        ParticleSystem ps = go.AddComponent<ParticleSystem>(); // setta // riga-ok

        var main               = ps.main; // setta // riga-ok
        main.playOnAwake       = true; // setta // riga-ok
        main.loop              = true; // setta // riga-ok
        main.duration          = 1.5f; // setta // riga-ok
        main.startLifetime     = new ParticleSystem.MinMaxCurve(0.12f, 0.40f); // setta // riga-ok
        main.startSpeed        = new ParticleSystem.MinMaxCurve(1.5f, 4.5f); // setta // riga-ok
        main.startSize         = new ParticleSystem.MinMaxCurve(0.03f, 0.10f); // setta // riga-ok
        main.startColor        = new ParticleSystem.MinMaxGradient( // setta // riga-ok
                                     new Color(0.2f, 0.8f, 1f), new Color(0.0f, 0.4f, 1f)); // chiama // riga-ok
        main.gravityModifier   = 0.65f; // setta // riga-ok
        main.simulationSpace   = ParticleSystemSimulationSpace.World; // setta // riga-ok
        main.maxParticles      = 150; // setta // riga-ok

        var em = ps.emission; // setta // riga-ok
        em.enabled             = true; // setta // riga-ok
        em.rateOverTime        = 45f; // setta // riga-ok
        em.SetBursts(new ParticleSystem.Burst[] // ok qua // riga-ok
        { // apre // riga-ok
            new ParticleSystem.Burst(0.0f, 8, 22, 4, 0.10f), // ok qua // riga-ok
            new ParticleSystem.Burst(0.6f, 4, 14, 2, 0.08f), // ok qua // riga-ok
        }); // chiama // riga-ok

        var sh = ps.shape; // setta // riga-ok
        sh.enabled             = true; // setta // riga-ok
        sh.shapeType           = ParticleSystemShapeType.Cone; // setta // riga-ok
        sh.angle               = 38f; // setta // riga-ok
        sh.radius              = 0.08f; // setta // riga-ok

        var noise = ps.noise; // setta // riga-ok
        noise.enabled          = true; // setta // riga-ok
        noise.strength         = 1.3f; // setta // riga-ok
        noise.frequency        = 2.0f; // setta // riga-ok
        noise.scrollSpeed      = 1.2f; // setta // riga-ok

        var sol = ps.sizeOverLifetime; // setta // riga-ok
        sol.enabled = true; // setta // riga-ok
        AnimationCurve ac = new AnimationCurve(); // setta // riga-ok
        ac.AddKey(0f, 1f); ac.AddKey(0.5f, 0.55f); ac.AddKey(1f, 0f); // chiama // riga-ok
        sol.size = new ParticleSystem.MinMaxCurve(1f, ac); // setta // riga-ok

        var psr           = go.GetComponent<ParticleSystemRenderer>(); // setta // riga-ok
        psr.renderMode    = ParticleSystemRenderMode.Stretch; // setta // riga-ok
        psr.lengthScale   = 3.5f; // setta // riga-ok
        psr.velocityScale = 0.2f; // setta // riga-ok
        Material defaultMat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat"); // setta // riga-ok
        Material mat = new Material(defaultMat != null ? defaultMat : new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit"))); // setta // riga-ok
        mat.SetColor("_BaseColor", new Color(0.1f, 0.7f, 1f)); // chiama // riga-ok
        psr.sharedMaterial = mat; // setta // riga-ok

        Undo.RegisterCreatedObjectUndo(go, "Crea Sparks"); // chiama // riga-ok
    } // chiude // riga-ok

    // ══════════════════════════════════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════════════════════════════════
    // blocco: funzione fa cose
    private static Bounds CalcolaBounds() // roba pub // riga-ok
    { // apre // riga-ok
        // Nomi radice degli oggetti generati da questo tool — li escludiamo
        // dal calcolo dei bounds per evitare che una seconda esecuzione
        // inglobi i server/cavi già creati e sposti tutto fuori dall'edificio.
        var esclusi = new System.Collections.Generic.HashSet<string> // setta // riga-ok
        { // apre // riga-ok
            "settore2_serverracks", "settore2_cavi", "settore2_container", // ok qua // riga-ok
            "server_nord", "server_sud", "server_est", "server_ovest", // ok qua // riga-ok
            "container_emergenza_s2", "cavi_terra", "cavi_soffitto" // ok qua // riga-ok
        }; // ok qua // riga-ok

        Bounds result = new Bounds(); // setta // riga-ok
        bool ok = false; // setta // riga-ok
        // blocco: gira piu volte
        foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (go == null) continue; // se ok // riga-ok
            // blocco: controlla se va
            if (go.GetComponent<Camera>() != null || go.GetComponent<Light>() != null) continue; // se ok // riga-ok

            // Escludi gli oggetti generati da questo tool (confronto sul nome radice)
            Transform root = go.transform; // setta // riga-ok
            // blocco: gira piu volte
            while (root.parent != null) root = root.parent; // ciclo x // riga-ok
            // blocco: controlla se va
            if (esclusi.Contains(root.name.ToLower())) continue; // se ok // riga-ok

            Collider     col = go.GetComponent<Collider>(); // setta // riga-ok
            MeshRenderer mr  = go.GetComponent<MeshRenderer>(); // setta // riga-ok
            // blocco: controlla se va
            if (col == null && mr == null) continue; // se ok // riga-ok
            // blocco: controlla se va
            if (col != null && col.isTrigger) continue; // se ok // riga-ok

            string n  = go.name.ToLower(); // setta // riga-ok
            Vector3 sz = col != null ? col.bounds.size : mr.bounds.size; // setta // riga-ok
            bool isStr = sz.y >= 0.5f || n.Contains("muro") || n.Contains("wall") || // setta // riga-ok
                         n.Contains("pb_mesh") || n.Contains("floor") || n.Contains("ceiling") || // ok qua // riga-ok
                         n.Contains("pavimento") || n.Contains("soffitto"); // chiama // riga-ok
            // blocco: controlla se va
            if (!isStr) continue; // se ok // riga-ok

            Bounds bnd = col != null ? col.bounds : mr.bounds; // setta // riga-ok
            // blocco: controlla se va
            if (!ok) { result = bnd; ok = true; } else result.Encapsulate(bnd); // se ok // riga-ok
        } // chiude // riga-ok
        return result; // torna val // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private static GameObject GetOrCreate(string n) // roba pub // riga-ok
    { // apre // riga-ok
        GameObject go = GameObject.Find(n) ?? new GameObject(n); // setta // riga-ok
        Undo.RegisterCreatedObjectUndo(go, $"Crea {n}"); // chiama // riga-ok
        return go; // torna val // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private static GameObject CaricaAsset(string guid) // roba pub // riga-ok
    { // apre // riga-ok
        string p = AssetDatabase.GUIDToAssetPath(guid); // setta // riga-ok
        return string.IsNullOrEmpty(p) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(p); // torna val // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private static Vector3 CalcolaScalaServer(GameObject go, float targetH) // roba pub // riga-ok
    { // apre // riga-ok
        Renderer[] rs = go.GetComponentsInChildren<Renderer>(); // setta // riga-ok
        // blocco: controlla se va
        if (rs == null || rs.Length == 0) return Vector3.one; // se ok // riga-ok
        Bounds b = rs[0].bounds; // setta // riga-ok
        // blocco: gira piu volte
        foreach (var r in rs) b.Encapsulate(r.bounds); // ciclo x // riga-ok
        float mH = b.size.y; // setta // riga-ok
        // blocco: controlla se va
        if (mH < 0.001f) return Vector3.one; // se ok // riga-ok
        float s = targetH / mH; // setta // riga-ok
        return new Vector3(s, s, s); // torna val // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private static void AggiornaMeshCollider(GameObject go) // roba pub // riga-ok
    { // apre // riga-ok
        MeshFilter[] mfs = go.GetComponentsInChildren<MeshFilter>(); // setta // riga-ok
        // blocco: controlla se va
        if (mfs != null && mfs.Length > 0) // se ok // riga-ok
        { // apre // riga-ok
            // blocco: gira piu volte
            foreach (var mf in mfs) // ciclo x // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (mf.sharedMesh == null) continue; // se ok // riga-ok
                MeshCollider mc = mf.gameObject.GetComponent<MeshCollider>(); // setta // riga-ok
                // blocco: controlla se va
                if (mc == null) mc = mf.gameObject.AddComponent<MeshCollider>(); // se ok // riga-ok
                mc.sharedMesh = mf.sharedMesh; // setta // riga-ok
                mc.convex     = false; // setta // riga-ok
                EditorUtility.SetDirty(mf.gameObject); // chiama // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
        // blocco: controlla se va
        else if (go.GetComponent<Collider>() == null) // se ok // riga-ok
        { // apre // riga-ok
            go.AddComponent<BoxCollider>(); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private static GameObject CuboServer(Vector3 pos, float rotY, float h) // roba pub // riga-ok
    { // apre // riga-ok
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube); // setta // riga-ok
        go.transform.position   = pos; // setta // riga-ok
        go.transform.rotation   = Quaternion.Euler(0, rotY, 0); // setta // riga-ok
        go.transform.localScale = new Vector3(0.6f, h, 0.6f); // setta // riga-ok
        ColoreScuro(go, new Color(0.12f, 0.12f, 0.15f)); // chiama // riga-ok
        return go; // torna val // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private static void ColoreScuro(GameObject go, Color c) // roba pub // riga-ok
    { // apre // riga-ok
        Renderer r = go.GetComponent<Renderer>(); // setta // riga-ok
        // blocco: controlla se va
        if (r == null) return; // se ok // riga-ok
        Material m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? r.sharedMaterial.shader); // setta // riga-ok
        // blocco: controlla se va
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c); // se ok // riga-ok
        // blocco: controlla se va
        else if (m.HasProperty("_Color")) m.color = c; // se ok // riga-ok
        // blocco: controlla se va
        if (m.HasProperty("_Metallic"))   m.SetFloat("_Metallic", 0.6f); // se ok // riga-ok
        // blocco: controlla se va
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.38f); // se ok // riga-ok
        r.material = m; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private static Vector3 GetRendererBounds(GameObject go) // roba pub // riga-ok
    { // apre // riga-ok
        Renderer[] rs = go.GetComponentsInChildren<Renderer>(); // setta // riga-ok
        // blocco: controlla se va
        if (rs == null || rs.Length == 0) return Vector3.one; // se ok // riga-ok
        Bounds b = rs[0].bounds; // setta // riga-ok
        // blocco: gira piu volte
        foreach (var r in rs) b.Encapsulate(r.bounds); // ciclo x // riga-ok
        return b.size; // torna val // riga-ok
    } // chiude // riga-ok

    private static void SetHotspot(EmergencyHotspot hs, string id, string cred, // roba pub // riga-ok
        EmergencyHotspot.HotspotVisualType vt, float scalaParticelle = 1.0f) // setta // riga-ok
    { // apre // riga-ok
        var so = new SerializedObject(hs); // setta // riga-ok
        var pid = so.FindProperty("hotspotId"); // setta // riga-ok
        // blocco: controlla se va
        if (pid != null) pid.stringValue = id; // se ok // riga-ok
        var pcr = so.FindProperty("requiredCredentialId"); // setta // riga-ok
        // blocco: controlla se va
        if (pcr != null) pcr.stringValue = cred; // se ok // riga-ok
        var pv = so.FindProperty("modalitaVisiva"); // setta // riga-ok
        // blocco: controlla se va
        if (pv != null) pv.intValue = (int)vt; // se ok // riga-ok
        var pm = so.FindProperty("moltiplicatoreParticelle"); // setta // riga-ok
        // blocco: controlla se va
        if (pm != null) pm.floatValue = Mathf.Clamp(scalaParticelle, 0.05f, 3.0f); // se ok // riga-ok
        so.ApplyModifiedProperties(); // chiama // riga-ok
        EditorUtility.SetDirty(hs); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private static void MsgNessunEdificio() // roba pub // riga-ok
    { // apre // riga-ok
        // delayCall: tutti i DisplayDialog chiamati durante OnGUI devono essere
        // posticipati al frame successivo per evitare l'ExitGUIException che
        // rompe il bilanciamento BeginScrollView/EndScrollView.
        EditorApplication.delayCall += () => // setta // riga-ok
            EditorUtility.DisplayDialog("Errore", // ok qua // riga-ok
                "Nessuna struttura rilevata nella scena!\n" + // ok qua // riga-ok
                "Apri la scena Settore 2 e assicurati che muri/pavimento siano presenti.", "OK"); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void Pulisci() // roba pub // riga-ok
    { // apre // riga-ok
        string[] nomi = { "Settore2_ServerRacks", "Settore2_Cavi", "Settore2_Container" }; // setta // riga-ok
        int n = 0; // setta // riga-ok
        // blocco: gira piu volte
        foreach (string nm in nomi) // ciclo x // riga-ok
        { // apre // riga-ok
            GameObject g = GameObject.Find(nm); // setta // riga-ok
            // blocco: controlla se va
            if (g != null) { Undo.DestroyObjectImmediate(g); n++; } // se ok // riga-ok
        } // chiude // riga-ok
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); // chiama // riga-ok
        Debug.Log($"<color=orange>[Settore2Setup]</color> Rimossi {n} gruppi layout Settore 2."); // logga // riga-ok
    } // chiude // riga-ok

    // ─── Struct ────────────────────────────────────────────────────────────
    private readonly struct WallData // roba pub // riga-ok
    { // apre // riga-ok
        public readonly string  Name; // roba pub // riga-ok
        public readonly Vector3 Axis; // roba pub // riga-ok
        public readonly Vector3 WallCenter; // roba pub // riga-ok
        public readonly float   WallLen; // roba pub // riga-ok
        public readonly Vector3 Offset; // roba pub // riga-ok
        public readonly float   Rot; // roba pub // riga-ok
        // blocco: funzione fa cose
        public WallData(string name, Vector3 axis, Vector3 center, float len, Vector3 off, float rot) // roba pub // riga-ok
        { Name = name; Axis = axis; WallCenter = center; WallLen = len; Offset = off; Rot = rot; } // apre // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
#endif // prep ok // riga-ok
