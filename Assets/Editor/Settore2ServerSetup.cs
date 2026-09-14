// ============================================================================
// Crisis Protocol / Sector Containment - Utility editor
// File: .\Assets\Editor\Settore2ServerSetup.cs
// Responsabilita': automatizza setup, popolamento scena, salvataggio, validazione o manutenzione direttamente dentro Unity Editor.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Editor tool per configurare il Settore 2: server rack sui muri, cavi modulari a terra e sul soffitto,
/// container in un angolo con hotspot di emergenza (scossa elettrica + perdita gas).
/// Aprire da: CrisisProtocol > Settore 2 > Configura Server, Cavi e Container
/// </summary>
public class Settore2ServerSetup : EditorWindow
{
    // ─── GUID Assets ───────────────────────────────────────────────────────
    // wires.fbx (cavi modulari)
    private const string GUID_WIRES_FBX = "986fdbec5cb35764683041a3a0c91b5e";
    // rak_server.glb
    private const string GUID_RAK_SERVER = "8c0bd40c0f4c95e4ea1bb2ef5a37de1a";

    // ─── PARAMETRI SERVER ──────────────────────────────────────────────────
    private float serverHeight  = 2.0f;
    private float serverWidth   = 0.6f;
    private float serverDepth   = 0.6f;
    private float serverSpacing = 0.70f;
    private bool  autoFitServer = true;
    private int   serverPerWall = 4;

    // ─── PARAMETRI CAVI ────────────────────────────────────────────────────
    private int   cableFloor   = 5;
    private int   cableCeiling = 3;
    private float segLen       = 1.0f;

    // ─── PARAMETRI CONTAINER ───────────────────────────────────────────────
    private Vector3 containerSize = new Vector3(2.4f, 2.0f, 4.0f);

    // ─── UI ────────────────────────────────────────────────────────────────
    private bool   fServer    = true;
    private bool   fWires     = true;
    private bool   fContainer = true;
    private Vector2 scroll;

    [MenuItem("CrisisProtocol/Settore 2/Configura Server, Cavi e Container")]
    [MenuItem("Tools/Settore 2 – Server, Cavi e Container")]
    public static void ApriFinestra()
    {
        var win = GetWindow<Settore2ServerSetup>("Settore 2 Setup", true);
        win.minSize = new Vector2(420, 560);
        win.Show();
    }

    // ══════════════════════════════════════════════════════════════════════
    // GUI
    // ══════════════════════════════════════════════════════════════════════
    private void OnGUI()
    {
        // Struttura semplice: niente try/catch attorno a BeginScrollView/EndScrollView.
        // ExitGUIException (lanciata da DisplayDialog) si propaga naturalmente a Unity
        // senza bisogno di wrapper — qualsiasi catch che la intercetta rompe lo stack GUI.
        scroll = EditorGUILayout.BeginScrollView(scroll);
        DisegnaContenuto();
        EditorGUILayout.EndScrollView();
    }

    private void DisegnaContenuto()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Settore 2 – Layout Automatico", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Posiziona i server rack sui 4 muri interni, i cavi modulari (wires.fbx) " +
            "a terra e soffitto, e il container di emergenza in un angolo.\n" +
            "Assicurati di avere la scena Settore 2 aperta e attiva.",
            MessageType.Info);

        EditorGUILayout.Space(6);

        // ── SERVER ──────────────────────────────────────────────────────────
        fServer = EditorGUILayout.Foldout(fServer, "Server Rack (muri interni)", true, EditorStyles.foldoutHeader);
        if (fServer)
        {
            EditorGUI.indentLevel++;
            serverHeight  = EditorGUILayout.Slider("Altezza Rack (m)",       serverHeight,  1.0f, 3.0f);
            serverWidth   = EditorGUILayout.Slider("Larghezza Rack (m)",      serverWidth,   0.3f, 1.5f);
            serverDepth   = EditorGUILayout.Slider("Profondità Rack (m)",     serverDepth,   0.3f, 1.0f);
            serverSpacing = EditorGUILayout.Slider("Passo centro-centro (m)", serverSpacing, 0.4f, 2.0f);
            autoFitServer = EditorGUILayout.Toggle("Auto-fit muro",           autoFitServer);
            if (!autoFitServer)
                serverPerWall = EditorGUILayout.IntSlider("Server per muro",  serverPerWall, 1, 20);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(4);

        // ── CAVI ────────────────────────────────────────────────────────────
        fWires = EditorGUILayout.Foldout(fWires, "Cavi Modulari (wires.fbx)", true, EditorStyles.foldoutHeader);
        if (fWires)
        {
            EditorGUI.indentLevel++;
            cableFloor   = EditorGUILayout.IntSlider("Sezioni a terra",      cableFloor,   1, 20);
            cableCeiling = EditorGUILayout.IntSlider("Sezioni soffitto",     cableCeiling, 1, 10);
            segLen       = EditorGUILayout.Slider("Lunghezza segmento (m)",  segLen,       0.5f, 3.0f);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(4);

        // ── CONTAINER ───────────────────────────────────────────────────────
        fContainer = EditorGUILayout.Foldout(fContainer, "Container Emergenza (angolo NW)", true, EditorStyles.foldoutHeader);
        if (fContainer)
        {
            EditorGUI.indentLevel++;
            containerSize = EditorGUILayout.Vector3Field("Dimensioni (L,H,P)", containerSize);
            EditorGUILayout.HelpBox("MeshCollider + EmergencyHotspot gas + scintille elettriche.", MessageType.None);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(10);

        // ── PULSANTI ────────────────────────────────────────────────────────
        // IMPORTANTE: tutte le azioni sono deferite con EditorApplication.delayCall
        // In questo modo il frame OnGUI (incluso EndScrollView) termina normalmente
        // PRIMA che qualsiasi operazione pesante venga eseguita.
        // Questo è l'unico pattern che evita in modo affidabile EndLayoutGroup errors.

        GUI.backgroundColor = new Color(0.25f, 0.82f, 0.42f);
        if (GUILayout.Button("▶  ESEGUI SETUP COMPLETO", GUILayout.Height(40)))
            EditorApplication.delayCall += EseguiSetupCompleto;
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(4);

        if (GUILayout.Button("Solo Server sui Muri", GUILayout.Height(26)))
            EditorApplication.delayCall += PosizionaServer;

        if (GUILayout.Button("Solo Cavi (terra + soffitto)", GUILayout.Height(26)))
            EditorApplication.delayCall += PosizionaCavi;

        if (GUILayout.Button("Solo Container con Hotspot", GUILayout.Height(26)))
            EditorApplication.delayCall += CreaContainer;

        EditorGUILayout.Space(4);
        GUI.backgroundColor = new Color(1f, 0.45f, 0.18f);
        if (GUILayout.Button("Pulizia: Rimuovi layout Settore 2", GUILayout.Height(26)))
            EditorApplication.delayCall += () =>
            {
                if (EditorUtility.DisplayDialog("Conferma", "Rimuovere tutti gli oggetti generati?", "Sì", "No"))
                    Pulisci();
            };
        GUI.backgroundColor = Color.white;
    }

    // ══════════════════════════════════════════════════════════════════════
    // SETUP COMPLETO
    // ══════════════════════════════════════════════════════════════════════
    private void EseguiSetupCompleto()
    {
        PosizionaServer();
        PosizionaCavi();
        CreaContainer();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AutoSceneSaver.EseguiSalvataggioAutomatico(forzaAncheSeNonDirty: true);

        // delayCall: mostra il dialogo di riepilogo DOPO che OnGUI ha completato
        // il frame corrente (EndScrollView incluso). Senza questo, DisplayDialog
        // lancia ExitGUIException che sfugge prima di EndScrollView → crash.
        EditorApplication.delayCall += () =>
            EditorUtility.DisplayDialog(
                "Settore 2 – Setup Completato!",
                "✔ Server Rack posizionati sui 4 muri interni con MeshCollider\n" +
                "✔ Cavi modulari (wires.fbx) a terra e soffitto\n" +
                "✔ Container emergenza in angolo NW con EmergencyHotspot\n" +
                "✔ Scena salvata\n\n" +
                "Ispeziona gli oggetti in Hierarchy sotto:\n" +
                "Settore2_ServerRacks / Settore2_Cavi / Settore2_Container",
                "OK");
    }

    // ══════════════════════════════════════════════════════════════════════
    // SERVER
    // ══════════════════════════════════════════════════════════════════════
    private void PosizionaServer()
    {
        Bounds b = CalcolaBounds();
        if (b.size == Vector3.zero) { MsgNessunEdificio(); return; }

        float floorY    = b.min.y;
        float buildH    = b.max.y - floorY;
        float rackH     = Mathf.Min(serverHeight, buildH * 0.92f);
        float rackY     = floorY + rackH * 0.5f;

        GameObject serverPrefab = CaricaAsset(GUID_RAK_SERVER);
        GameObject root         = GetOrCreate("Settore2_ServerRacks");

        // 4 muri interni: il WallCenter usa floorY come Y di riferimento.
        // L'offset X/Z sposta il rack appena dentro il muro (spessore depth/2 + 1cm).
        // La Y finale viene sovrascritta da pos.y = rackY (centro del rack in altezza).
        float wallY = floorY; // riferimento quota pavimento
        var walls = new[]
        {
            new WallData("Nord",  Vector3.right,   new Vector3(b.center.x, wallY, b.max.z), b.size.x, new Vector3( 0, 0,-(serverDepth*0.5f+0.02f)),   0f),
            new WallData("Sud",   Vector3.right,   new Vector3(b.center.x, wallY, b.min.z), b.size.x, new Vector3( 0, 0,  serverDepth*0.5f+0.02f),  180f),
            new WallData("Est",   Vector3.forward, new Vector3(b.max.x,    wallY, b.center.z), b.size.z, new Vector3(-(serverDepth*0.5f+0.02f), 0, 0),  90f),
            new WallData("Ovest", Vector3.forward, new Vector3(b.min.x,    wallY, b.center.z), b.size.z, new Vector3(  serverDepth*0.5f+0.02f, 0, 0), -90f),
        };

        int totale = 0;
        foreach (var w in walls)
        {
            int count = autoFitServer
                ? Mathf.Max(1, Mathf.FloorToInt((w.WallLen - serverWidth) / serverSpacing))
                : serverPerWall;
            count = Mathf.Min(count, 30);

            float totalLen = (count - 1) * serverSpacing;
            float startT   = -totalLen * 0.5f;

            for (int i = 0; i < count; i++)
            {
                float   t   = startT + i * serverSpacing;
                Vector3 pos = w.WallCenter + w.Axis * t + w.Offset;
                pos.y = rackY;

                GameObject server;
                if (serverPrefab != null)
                {
                    server = (GameObject)PrefabUtility.InstantiatePrefab(serverPrefab);
                    server.transform.position   = pos;
                    server.transform.rotation   = Quaternion.Euler(0, w.Rot, 0);
                    server.transform.localScale = CalcolaScalaServer(server, rackH);
                }
                else
                {
                    server = CuboServer(pos, w.Rot, rackH);
                }

                server.name = $"Server_{w.Name}_{i + 1:D2}";
                server.transform.SetParent(root.transform, true);
                AggiornaMeshCollider(server);
                Undo.RegisterCreatedObjectUndo(server, "Crea Server");
                totale++;
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"<color=cyan>[Settore2Setup]</color> Creati <b>{totale}</b> server rack sui 4 muri.");
    }

    // ══════════════════════════════════════════════════════════════════════
    // CAVI
    // ══════════════════════════════════════════════════════════════════════
    private void PosizionaCavi()
    {
        Bounds b = CalcolaBounds();
        if (b.size == Vector3.zero) { MsgNessunEdificio(); return; }

        float floorY = b.min.y + 0.02f;
        float ceilY  = b.max.y - 0.08f;

        GameObject wireAsset = CaricaAsset(GUID_WIRES_FBX);
        GameObject root      = GetOrCreate("Settore2_Cavi");

        // ── A TERRA ──────────────────────────────────────────────────────
        GameObject rootTerra = new GameObject("Cavi_Terra");
        rootTerra.transform.SetParent(root.transform, false);

        // Linea X: da ovest verso centro
        FilaCavi(wireAsset, rootTerra, "Terra_LineaX",
            new Vector3(b.min.x + 0.3f, floorY, b.center.z + 0.6f),
            Vector3.right, cableFloor, segLen, Quaternion.identity);

        // Linea Z: da sud verso centro
        FilaCavi(wireAsset, rootTerra, "Terra_LineaZ",
            new Vector3(b.center.x - 0.5f, floorY, b.min.z + 0.3f),
            Vector3.forward, cableFloor, segLen, Quaternion.Euler(0, 90, 0));

        // Gomito diagonale (area server Est)
        FilaCavi(wireAsset, rootTerra, "Terra_Gomito",
            new Vector3(b.max.x - 1.2f, floorY, b.center.z - 0.8f),
            Vector3.left, Mathf.Max(1, cableFloor / 2), segLen, Quaternion.Euler(0, 35, 0));

        // ── SUL SOFFITTO ─────────────────────────────────────────────────
        GameObject rootSoffitto = new GameObject("Cavi_Soffitto");
        rootSoffitto.transform.SetParent(root.transform, false);

        // Cavi soffitto: niente rotazione capovolta — il modello FBX è già orientato
        // correttamente. Basta posizionarli a ceilY; usare Euler(180,x,x) capovolgeva
        // la mesh rendendola invisibile o invertita.
        FilaCavi(wireAsset, rootSoffitto, "Soffitto_LineaX",
            new Vector3(b.min.x + 0.5f, ceilY, b.center.z),
            Vector3.right,   cableCeiling, segLen, Quaternion.identity);

        FilaCavi(wireAsset, rootSoffitto, "Soffitto_LineaZ",
            new Vector3(b.center.x + 0.3f, ceilY, b.min.z + 0.5f),
            Vector3.forward, cableCeiling, segLen, Quaternion.Euler(0, 90, 0));

        // ── HOTSPOT SCOSSA su cavo a terra ────────────────────────────────
        Transform grpX = rootTerra.transform.Find("Terra_LineaX");
        if (grpX != null && grpX.childCount > 0)
        {
            int idx  = Mathf.Min(2, grpX.childCount - 1);
            Transform cavo = grpX.GetChild(idx);
            CreaHotspotScossaSuCavo(cavo.gameObject);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("<color=cyan>[Settore2Setup]</color> Cavi modulari posizionati a terra e soffitto.");
    }

    private void FilaCavi(GameObject wireAsset, GameObject parent, string groupName,
        Vector3 start, Vector3 direction, int count, float sl, Quaternion rot)
    {
        GameObject grp = new GameObject(groupName);
        grp.transform.SetParent(parent.transform, false);

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = start + direction * (i * sl);
            GameObject seg;

            if (wireAsset != null)
            {
                seg = (GameObject)PrefabUtility.InstantiatePrefab(wireAsset);
                seg.transform.position   = pos;
                seg.transform.rotation   = rot;
                Vector3 sz   = GetRendererBounds(seg);
                float modelL = Mathf.Max(sz.z, sz.x, 0.01f);
                float s      = sl / modelL;
                seg.transform.localScale = new Vector3(s, s, s);
            }
            else
            {
                seg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                seg.transform.position   = pos;
                seg.transform.rotation   = rot * Quaternion.Euler(90, 0, 0);
                seg.transform.localScale = new Vector3(0.04f, sl * 0.5f, 0.04f);
                ColoreScuro(seg, new Color(0.1f, 0.1f, 0.1f));
            }

            seg.name = $"{groupName}_Seg{i + 1:D2}";
            seg.transform.SetParent(grp.transform, true);
            Undo.RegisterCreatedObjectUndo(seg, "Crea Cavo");
        }
    }

    private void CreaHotspotScossaSuCavo(GameObject cavo)
    {
        GameObject hGo = new GameObject("Cavo_Hotspot_ScossaElettr");
        hGo.transform.SetParent(cavo.transform, false);
        hGo.transform.localPosition = new Vector3(0, 0.05f, 0);

        BoxCollider box  = hGo.AddComponent<BoxCollider>();
        box.isTrigger    = true;
        box.size         = new Vector3(0.8f, 0.25f, segLen);
        box.center       = Vector3.zero;

        EmergencyHotspot hs = hGo.AddComponent<EmergencyHotspot>();
        // 0.20 = particelle ridotte — il cavo è un oggetto sottile a terra
        SetHotspot(hs, "CABLE_FAULT_S2", "KEYCARD_S2", EmergencyHotspot.HotspotVisualType.ElectricSparks, 0.20f);

        CreaSparks(hGo);
        Undo.RegisterCreatedObjectUndo(hGo, "Crea HotspotCavo");
        Debug.Log("<color=yellow>[Settore2Setup]</color> Hotspot scossa su cavo creato.");
    }

    // ══════════════════════════════════════════════════════════════════════
    // CONTAINER
    // ══════════════════════════════════════════════════════════════════════
    private void CreaContainer()
    {
        Bounds b = CalcolaBounds();
        if (b.size == Vector3.zero) { MsgNessunEdificio(); return; }

        float floorY  = b.min.y;
        float margine = 0.05f;

        Vector3 pos = new Vector3(
            b.min.x + containerSize.x * 0.5f + margine,
            floorY  + containerSize.y * 0.5f,
            b.max.z - containerSize.z * 0.5f - margine
        );

        // Corpo container
        GameObject cont = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cont.name = "Container_Emergenza_S2";
        cont.transform.position   = pos;
        cont.transform.localScale = containerSize;
        ColoreScuro(cont, new Color(0.18f, 0.21f, 0.17f));
        AggiornaMeshCollider(cont);

        // Hotspot figlio: localPosition in coordinate locali del container
        // (scale già applicata al parent). Valori in [-0.5, 0.5] per restare
        // dentro il cubo. Z=0.48 = quasi sulla faccia frontale (punto di perdita).
        GameObject hGo = new GameObject("Hotspot_Gas_Scossa");
        hGo.transform.SetParent(cont.transform, false);
        hGo.transform.localPosition = new Vector3(0f, 0.35f, 0.48f);

        // BoxCollider: le dimensioni sono in spazio locale del hotspot
        // (il parent è scalato, quindi 1 unità locale = 1 unità world / containerScale).
        BoxCollider hBox = hGo.AddComponent<BoxCollider>();
        hBox.isTrigger   = true;
        hBox.size        = new Vector3(0.9f, 0.7f, 0.15f); // larghezza/altezza/profondità in locale
        hBox.center      = Vector3.zero;

        EmergencyHotspot hs = hGo.AddComponent<EmergencyHotspot>();
        // 0.25 = particelle ridotte al 25% — il container è piccolo (2.4 x 2.0 x 4.0 m)
        SetHotspot(hs, "CONTAINER_FAULT_S2", "KEYCARD_S2", EmergencyHotspot.HotspotVisualType.ToxicGasLeak, 0.25f);
        CreaSparks(hGo);

        // Luce rossa allarme
        GameObject lGo = new GameObject("Luce_Allarme");
        lGo.transform.SetParent(cont.transform, false);
        lGo.transform.localPosition = new Vector3(0, 0.45f, 0);
        Light lt    = lGo.AddComponent<Light>();
        lt.type     = LightType.Point;
        lt.color    = new Color(1f, 0.08f, 0.04f);
        lt.intensity = 3f;
        lt.range    = 5f;

        GameObject root = GetOrCreate("Settore2_Container");
        cont.transform.SetParent(root.transform, true);

        Undo.RegisterCreatedObjectUndo(cont, "Crea Container");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = cont;

        Debug.Log("<color=lime>[Settore2Setup]</color> Container emergenza creato nell'angolo NW.");
    }

    // ══════════════════════════════════════════════════════════════════════
    // SCINTILLE ELETTRICHE
    // ══════════════════════════════════════════════════════════════════════
    private static void CreaSparks(GameObject parent)
    {
        GameObject go = new GameObject("VFX_Scintille");
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = Vector3.zero;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        var main               = ps.main;
        main.playOnAwake       = true;
        main.loop              = true;
        main.duration          = 1.5f;
        main.startLifetime     = new ParticleSystem.MinMaxCurve(0.12f, 0.40f);
        main.startSpeed        = new ParticleSystem.MinMaxCurve(1.5f, 4.5f);
        main.startSize         = new ParticleSystem.MinMaxCurve(0.03f, 0.10f);
        main.startColor        = new ParticleSystem.MinMaxGradient(
                                     new Color(0.2f, 0.8f, 1f), new Color(0.0f, 0.4f, 1f));
        main.gravityModifier   = 0.65f;
        main.simulationSpace   = ParticleSystemSimulationSpace.World;
        main.maxParticles      = 150;

        var em = ps.emission;
        em.enabled             = true;
        em.rateOverTime        = 45f;
        em.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0.0f, 8, 22, 4, 0.10f),
            new ParticleSystem.Burst(0.6f, 4, 14, 2, 0.08f),
        });

        var sh = ps.shape;
        sh.enabled             = true;
        sh.shapeType           = ParticleSystemShapeType.Cone;
        sh.angle               = 38f;
        sh.radius              = 0.08f;

        var noise = ps.noise;
        noise.enabled          = true;
        noise.strength         = 1.3f;
        noise.frequency        = 2.0f;
        noise.scrollSpeed      = 1.2f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve ac = new AnimationCurve();
        ac.AddKey(0f, 1f); ac.AddKey(0.5f, 0.55f); ac.AddKey(1f, 0f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, ac);

        var psr           = go.GetComponent<ParticleSystemRenderer>();
        psr.renderMode    = ParticleSystemRenderMode.Stretch;
        psr.lengthScale   = 3.5f;
        psr.velocityScale = 0.2f;
        Material defaultMat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");
        Material mat = new Material(defaultMat != null ? defaultMat : new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit")));
        mat.SetColor("_BaseColor", new Color(0.1f, 0.7f, 1f));
        psr.sharedMaterial = mat;

        Undo.RegisterCreatedObjectUndo(go, "Crea Sparks");
    }

    // ══════════════════════════════════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════════════════════════════════
    private static Bounds CalcolaBounds()
    {
        // Nomi radice degli oggetti generati da questo tool — li escludiamo
        // dal calcolo dei bounds per evitare che una seconda esecuzione
        // inglobi i server/cavi già creati e sposti tutto fuori dall'edificio.
        var esclusi = new System.Collections.Generic.HashSet<string>
        {
            "settore2_serverracks", "settore2_cavi", "settore2_container",
            "server_nord", "server_sud", "server_est", "server_ovest",
            "container_emergenza_s2", "cavi_terra", "cavi_soffitto"
        };

        Bounds result = new Bounds();
        bool ok = false;
        foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go == null) continue;
            if (go.GetComponent<Camera>() != null || go.GetComponent<Light>() != null) continue;

            // Escludi gli oggetti generati da questo tool (confronto sul nome radice)
            Transform root = go.transform;
            while (root.parent != null) root = root.parent;
            if (esclusi.Contains(root.name.ToLower())) continue;

            Collider     col = go.GetComponent<Collider>();
            MeshRenderer mr  = go.GetComponent<MeshRenderer>();
            if (col == null && mr == null) continue;
            if (col != null && col.isTrigger) continue;

            string n  = go.name.ToLower();
            Vector3 sz = col != null ? col.bounds.size : mr.bounds.size;
            bool isStr = sz.y >= 0.5f || n.Contains("muro") || n.Contains("wall") ||
                         n.Contains("pb_mesh") || n.Contains("floor") || n.Contains("ceiling") ||
                         n.Contains("pavimento") || n.Contains("soffitto");
            if (!isStr) continue;

            Bounds bnd = col != null ? col.bounds : mr.bounds;
            if (!ok) { result = bnd; ok = true; } else result.Encapsulate(bnd);
        }
        return result;
    }

    private static GameObject GetOrCreate(string n)
    {
        GameObject go = GameObject.Find(n) ?? new GameObject(n);
        Undo.RegisterCreatedObjectUndo(go, $"Crea {n}");
        return go;
    }

    private static GameObject CaricaAsset(string guid)
    {
        string p = AssetDatabase.GUIDToAssetPath(guid);
        return string.IsNullOrEmpty(p) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(p);
    }

    private static Vector3 CalcolaScalaServer(GameObject go, float targetH)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        if (rs == null || rs.Length == 0) return Vector3.one;
        Bounds b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        float mH = b.size.y;
        if (mH < 0.001f) return Vector3.one;
        float s = targetH / mH;
        return new Vector3(s, s, s);
    }

    private static void AggiornaMeshCollider(GameObject go)
    {
        MeshFilter[] mfs = go.GetComponentsInChildren<MeshFilter>();
        if (mfs != null && mfs.Length > 0)
        {
            foreach (var mf in mfs)
            {
                if (mf.sharedMesh == null) continue;
                MeshCollider mc = mf.gameObject.GetComponent<MeshCollider>();
                if (mc == null) mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                mc.convex     = false;
                EditorUtility.SetDirty(mf.gameObject);
            }
        }
        else if (go.GetComponent<Collider>() == null)
        {
            go.AddComponent<BoxCollider>();
        }
    }

    private static GameObject CuboServer(Vector3 pos, float rotY, float h)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.position   = pos;
        go.transform.rotation   = Quaternion.Euler(0, rotY, 0);
        go.transform.localScale = new Vector3(0.6f, h, 0.6f);
        ColoreScuro(go, new Color(0.12f, 0.12f, 0.15f));
        return go;
    }

    private static void ColoreScuro(GameObject go, Color c)
    {
        Renderer r = go.GetComponent<Renderer>();
        if (r == null) return;
        Material m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? r.sharedMaterial.shader);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        else if (m.HasProperty("_Color")) m.color = c;
        if (m.HasProperty("_Metallic"))   m.SetFloat("_Metallic", 0.6f);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.38f);
        r.material = m;
    }

    private static Vector3 GetRendererBounds(GameObject go)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        if (rs == null || rs.Length == 0) return Vector3.one;
        Bounds b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b.size;
    }

    private static void SetHotspot(EmergencyHotspot hs, string id, string cred,
        EmergencyHotspot.HotspotVisualType vt, float scalaParticelle = 1.0f)
    {
        var so = new SerializedObject(hs);
        var pid = so.FindProperty("hotspotId");
        if (pid != null) pid.stringValue = id;
        var pcr = so.FindProperty("requiredCredentialId");
        if (pcr != null) pcr.stringValue = cred;
        var pv = so.FindProperty("modalitaVisiva");
        if (pv != null) pv.intValue = (int)vt;
        var pm = so.FindProperty("moltiplicatoreParticelle");
        if (pm != null) pm.floatValue = Mathf.Clamp(scalaParticelle, 0.05f, 3.0f);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(hs);
    }

    private static void MsgNessunEdificio()
    {
        // delayCall: tutti i DisplayDialog chiamati durante OnGUI devono essere
        // posticipati al frame successivo per evitare l'ExitGUIException che
        // rompe il bilanciamento BeginScrollView/EndScrollView.
        EditorApplication.delayCall += () =>
            EditorUtility.DisplayDialog("Errore",
                "Nessuna struttura rilevata nella scena!\n" +
                "Apri la scena Settore 2 e assicurati che muri/pavimento siano presenti.", "OK");
    }

    private void Pulisci()
    {
        string[] nomi = { "Settore2_ServerRacks", "Settore2_Cavi", "Settore2_Container" };
        int n = 0;
        foreach (string nm in nomi)
        {
            GameObject g = GameObject.Find(nm);
            if (g != null) { Undo.DestroyObjectImmediate(g); n++; }
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"<color=orange>[Settore2Setup]</color> Rimossi {n} gruppi layout Settore 2.");
    }

    // ─── Struct ────────────────────────────────────────────────────────────
    private readonly struct WallData
    {
        public readonly string  Name;
        public readonly Vector3 Axis;
        public readonly Vector3 WallCenter;
        public readonly float   WallLen;
        public readonly Vector3 Offset;
        public readonly float   Rot;
        public WallData(string name, Vector3 axis, Vector3 center, float len, Vector3 off, float rot)
        { Name = name; Axis = axis; WallCenter = center; WallLen = len; Offset = off; Rot = rot; }
    }
}
#endif
