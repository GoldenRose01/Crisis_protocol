// ============================================================================
// Crisis Protocol / Sector Containment - Utility editor
// File: .\Assets\Editor\AutoMeshTopologyFixer.cs
// Responsabilita': automatizza setup, popolamento scena, salvataggio, validazione o manutenzione direttamente dentro Unity Editor.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
#if UNITY_EDITOR // prep ok // riga-ok
// AutoMeshTopologyFixer.cs  —  deve stare in una cartella "Editor"
// Non viene incluso nelle build di gioco.
//
// Risolve l'errore:
//   "Assertion failed: subMesh.topology == kPrimitiveTriangleStrip || kPrimitiveQuads"
//   "Failed getting triangles. Submesh topology is lines or points."
//
// Cosa fa:
//   1. [InitializeOnLoad] scansiona tutti gli asset mesh al caricamento del progetto
//   2. Per ogni sub-mesh con topologia Lines/LineStrip/Points: svuota gli indici
//   3. MeshCollider con mesh problematica -> convex = true (protezione extra)
//   4. MeshTopologyPostprocessor: previene il problema sulle importazioni future

using System.Collections.Generic; // usa lib // riga-ok
using UnityEditor; // usa lib // riga-ok
using UnityEngine; // usa lib // riga-ok

[InitializeOnLoad] // nota unity // riga-ok
public static class AutoMeshTopologyFixer // roba pub // riga-ok
{ // apre // riga-ok
    private const string SessionKey = "AutoMeshTopologyFixer_v3"; // roba pub // riga-ok

    static AutoMeshTopologyFixer() // roba pub // riga-ok
    { // apre // riga-ok
        EditorApplication.delayCall += RunOnce; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private static void RunOnce() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (SessionState.GetBool(SessionKey, false)) return; // se ok // riga-ok
        SessionState.SetBool(SessionKey, true); // chiama // riga-ok

        int count = FixAll(silent: true); // setta // riga-ok
        // blocco: controlla se va
        if (count > 0) // se ok // riga-ok
            Debug.Log("[AutoFix] Corrette automaticamente " + count + // logga // riga-ok
                      " sub-mesh con topologia non-triangolo. " + // ok qua // riga-ok
                      "Premi Play: gli errori subMesh.topology non appariranno piu'."); // chiama // riga-ok
    } // chiude // riga-ok

    [MenuItem("Tools/Fix Mesh Topology Issues")] // nota unity // riga-ok
    // blocco: funzione fa cose
    public static void FixManual() // roba pub // riga-ok
    { // apre // riga-ok
        int count = FixAll(silent: false); // setta // riga-ok
        EditorUtility.DisplayDialog( // ok qua // riga-ok
            "Fix Mesh Topology", // ok qua // riga-ok
            count > 0 // ok qua // riga-ok
                ? "Corrette " + count + " sub-mesh non-triangolo.\nPremi Play per verificare." // ok qua // riga-ok
                : "Nessuna mesh problematica trovata.", // ok qua // riga-ok
            "OK"); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private static int FixAll(bool silent) // roba pub // riga-ok
    { // apre // riga-ok
        int totalFixed = 0; // setta // riga-ok
        var processed = new HashSet<string>(); // setta // riga-ok

        AssetDatabase.StartAssetEditing(); // chiama // riga-ok
        // blocco: prova safe
        try // prova // riga-ok
        { // apre // riga-ok
            string[] guids = AssetDatabase.FindAssets("t:Mesh"); // setta // riga-ok
            // blocco: gira piu volte
            foreach (string guid in guids) // ciclo x // riga-ok
            { // apre // riga-ok
                string path = AssetDatabase.GUIDToAssetPath(guid); // setta // riga-ok
                // blocco: controlla se va
                if (!processed.Add(path)) continue; // se ok // riga-ok

                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path); // setta // riga-ok
                bool anyFixed = false; // setta // riga-ok

                // blocco: gira piu volte
                foreach (Object asset in assets) // ciclo x // riga-ok
                { // apre // riga-ok
                    Mesh mesh = asset as Mesh; // setta // riga-ok
                    // blocco: controlla se va
                    if (mesh == null || !mesh.isReadable) continue; // se ok // riga-ok

                    // blocco: gira piu volte
                    for (int i = 0; i < mesh.subMeshCount; i++) // ciclo x // riga-ok
                    { // apre // riga-ok
                        MeshTopology topo = mesh.GetSubMesh(i).topology; // setta // riga-ok
                        // blocco: controlla se va
                        if (topo != MeshTopology.Lines && // se ok // riga-ok
                            topo != MeshTopology.LineStrip && // setta // riga-ok
                            topo != MeshTopology.Points) continue; // setta // riga-ok

                        // Svuota la sub-mesh: 0 indici e la forza a Triangles per non mandare in crash Unity
                        mesh.SetIndices(new int[0], MeshTopology.Triangles, i, false); // chiama // riga-ok
                        EditorUtility.SetDirty(mesh); // chiama // riga-ok
                        totalFixed++; // ok qua // riga-ok
                        anyFixed = true; // setta // riga-ok

                        // blocco: controlla se va
                        if (!silent) // se ok // riga-ok
                            Debug.Log("[AutoFix] sub-mesh[" + i + "] (" + topo + ")" + // logga // riga-ok
                                      " svuotata in '" + mesh.name + "' @ " + path); // chiama // riga-ok
                    } // chiude // riga-ok
                } // chiude // riga-ok

                // blocco: controlla se va
                if (anyFixed) // se ok // riga-ok
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); // chiama // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
        finally // ok qua // riga-ok
        { // apre // riga-ok
            AssetDatabase.StopAssetEditing(); // chiama // riga-ok
            AssetDatabase.SaveAssets(); // chiama // riga-ok
        } // chiude // riga-ok

        // Protezione extra: MeshCollider con mesh problematica -> convex = true
        MeshCollider[] cols = Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None); // setta // riga-ok
        // blocco: gira piu volte
        foreach (MeshCollider col in cols) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (col.sharedMesh == null || col.convex) continue; // se ok // riga-ok
            // blocco: gira piu volte
            for (int i = 0; i < col.sharedMesh.subMeshCount; i++) // ciclo x // riga-ok
            { // apre // riga-ok
                MeshTopology topo = col.sharedMesh.GetSubMesh(i).topology; // setta // riga-ok
                // blocco: controlla se va
                if (topo != MeshTopology.Lines && // se ok // riga-ok
                    topo != MeshTopology.LineStrip && // setta // riga-ok
                    topo != MeshTopology.Points) continue; // setta // riga-ok

                col.convex = true; // setta // riga-ok
                EditorUtility.SetDirty(col); // chiama // riga-ok
                // blocco: controlla se va
                if (!silent) // se ok // riga-ok
                    Debug.Log("[AutoFix] MeshCollider.convex=true su '" + col.gameObject.name + "'"); // logga // riga-ok
                break; // stop // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        return totalFixed; // torna val // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok

// Previene il problema su ogni futuro modello importato (FBX, GLB, OBJ, ...)
// blocco: classe x roba grossa
public class MeshTopologyPostprocessor : AssetPostprocessor // classe qui // riga-ok
{ // apre // riga-ok
    // blocco: funzione fa cose
    private void OnPostprocessModel(GameObject root) // roba pub // riga-ok
    { // apre // riga-ok
        int count = 0; // setta // riga-ok
        // blocco: gira piu volte
        foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true)) // ciclo x // riga-ok
            // blocco: controlla se va
            if (mf.sharedMesh != null) count += Strip(mf.sharedMesh); // se ok // riga-ok
        // blocco: gira piu volte
        foreach (SkinnedMeshRenderer smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true)) // ciclo x // riga-ok
            // blocco: controlla se va
            if (smr.sharedMesh != null) count += Strip(smr.sharedMesh); // se ok // riga-ok
        // blocco: controlla se va
        if (count > 0) // se ok // riga-ok
            Debug.Log("[MeshPostprocessor] Rimosse " + count + // logga // riga-ok
                      " sub-mesh non-triangolo da '" + assetPath + "'"); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private static int Strip(Mesh mesh) // roba pub // riga-ok
    { // apre // riga-ok
        int n = 0; // setta // riga-ok
        // blocco: gira piu volte
        for (int i = 0; i < mesh.subMeshCount; i++) // ciclo x // riga-ok
        { // apre // riga-ok
            MeshTopology t = mesh.GetSubMesh(i).topology; // setta // riga-ok
            // blocco: controlla se va
            if (t == MeshTopology.Lines || // se ok // riga-ok
                t == MeshTopology.LineStrip || // setta // riga-ok
                t == MeshTopology.Points) // setta // riga-ok
            { // apre // riga-ok
                mesh.SetIndices(new int[0], MeshTopology.Triangles, i, false); // chiama // riga-ok
                n++; // ok qua // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
        return n; // torna val // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
#endif // prep ok // riga-ok

