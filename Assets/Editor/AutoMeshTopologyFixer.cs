#if UNITY_EDITOR
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

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class AutoMeshTopologyFixer
{
    private const string SessionKey = "AutoMeshTopologyFixer_v3";

    static AutoMeshTopologyFixer()
    {
        EditorApplication.delayCall += RunOnce;
    }

    private static void RunOnce()
    {
        if (SessionState.GetBool(SessionKey, false)) return;
        SessionState.SetBool(SessionKey, true);

        int count = FixAll(silent: true);
        if (count > 0)
            Debug.Log("[AutoFix] Corrette automaticamente " + count +
                      " sub-mesh con topologia non-triangolo. " +
                      "Premi Play: gli errori subMesh.topology non appariranno piu'.");
    }

    [MenuItem("Tools/Fix Mesh Topology Issues")]
    public static void FixManual()
    {
        int count = FixAll(silent: false);
        EditorUtility.DisplayDialog(
            "Fix Mesh Topology",
            count > 0
                ? "Corrette " + count + " sub-mesh non-triangolo.\nPremi Play per verificare."
                : "Nessuna mesh problematica trovata.",
            "OK");
    }

    private static int FixAll(bool silent)
    {
        int totalFixed = 0;
        var processed = new HashSet<string>();

        AssetDatabase.StartAssetEditing();
        try
        {
            string[] guids = AssetDatabase.FindAssets("t:Mesh");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!processed.Add(path)) continue;

                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                bool anyFixed = false;

                foreach (Object asset in assets)
                {
                    Mesh mesh = asset as Mesh;
                    if (mesh == null || !mesh.isReadable) continue;

                    for (int i = 0; i < mesh.subMeshCount; i++)
                    {
                        MeshTopology topo = mesh.GetSubMesh(i).topology;
                        if (topo != MeshTopology.Lines &&
                            topo != MeshTopology.LineStrip &&
                            topo != MeshTopology.Points) continue;

                        // Svuota la sub-mesh: 0 indici e la forza a Triangles per non mandare in crash Unity
                        mesh.SetIndices(new int[0], MeshTopology.Triangles, i, false);
                        EditorUtility.SetDirty(mesh);
                        totalFixed++;
                        anyFixed = true;

                        if (!silent)
                            Debug.Log("[AutoFix] sub-mesh[" + i + "] (" + topo + ")" +
                                      " svuotata in '" + mesh.name + "' @ " + path);
                    }
                }

                if (anyFixed)
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
        }

        // Protezione extra: MeshCollider con mesh problematica -> convex = true
        MeshCollider[] cols = Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None);
        foreach (MeshCollider col in cols)
        {
            if (col.sharedMesh == null || col.convex) continue;
            for (int i = 0; i < col.sharedMesh.subMeshCount; i++)
            {
                MeshTopology topo = col.sharedMesh.GetSubMesh(i).topology;
                if (topo != MeshTopology.Lines &&
                    topo != MeshTopology.LineStrip &&
                    topo != MeshTopology.Points) continue;

                col.convex = true;
                EditorUtility.SetDirty(col);
                if (!silent)
                    Debug.Log("[AutoFix] MeshCollider.convex=true su '" + col.gameObject.name + "'");
                break;
            }
        }

        return totalFixed;
    }
}

// Previene il problema su ogni futuro modello importato (FBX, GLB, OBJ, ...)
public class MeshTopologyPostprocessor : AssetPostprocessor
{
    private void OnPostprocessModel(GameObject root)
    {
        int count = 0;
        foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
            if (mf.sharedMesh != null) count += Strip(mf.sharedMesh);
        foreach (SkinnedMeshRenderer smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (smr.sharedMesh != null) count += Strip(smr.sharedMesh);
        if (count > 0)
            Debug.Log("[MeshPostprocessor] Rimosse " + count +
                      " sub-mesh non-triangolo da '" + assetPath + "'");
    }

    private static int Strip(Mesh mesh)
    {
        int n = 0;
        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            MeshTopology t = mesh.GetSubMesh(i).topology;
            if (t == MeshTopology.Lines ||
                t == MeshTopology.LineStrip ||
                t == MeshTopology.Points)
            {
                mesh.SetIndices(new int[0], MeshTopology.Triangles, i, false);
                n++;
            }
        }
        return n;
    }
}
#endif

