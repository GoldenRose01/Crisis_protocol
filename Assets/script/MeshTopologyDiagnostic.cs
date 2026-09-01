using UnityEngine;

public class MeshTopologyDiagnostic : MonoBehaviour
{
    private void Start()
    {
        int problemi = 0;
        foreach (MeshCollider col in FindObjectsByType<MeshCollider>(FindObjectsSortMode.None))
        {
            if (col.sharedMesh == null) continue;
            problemi += AnalizzaMesh(col.sharedMesh, col.gameObject, "MeshCollider");
        }
        foreach (MeshFilter mf in FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
        {
            if (mf.sharedMesh == null) continue;
            problemi += AnalizzaMesh(mf.sharedMesh, mf.gameObject, "MeshRenderer");
        }
        foreach (SkinnedMeshRenderer smr in FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None))
        {
            if (smr.sharedMesh == null) continue;
            problemi += AnalizzaMesh(smr.sharedMesh, smr.gameObject, "SkinnedMeshRenderer");
        }
        if (problemi == 0)
            Debug.Log("[DIAGNOSTIC] Nessuna mesh problematica trovata. Rimuovi questo componente.");
        else
            Debug.LogWarning("[DIAGNOSTIC] " + problemi + " sub-mesh con topologia non-triangolo. Clicca i warning qui sopra per selezionare gli oggetti.");
    }

    private static int AnalizzaMesh(Mesh mesh, GameObject owner, string tipo)
    {
        int trovati = 0;
        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            MeshTopology topo = mesh.GetSubMesh(i).topology;
            if (topo == MeshTopology.Lines || topo == MeshTopology.LineStrip || topo == MeshTopology.Points)
            {
                Debug.LogWarning("[DIAGNOSTIC] " + owner.name + " | " + tipo + " | mesh '" + mesh.name + "' subMesh[" + i + "] = " + topo + " | " + GetPath(owner.transform), owner);
                trovati++;
            }
        }
        return trovati;
    }

    private static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
        return path;
    }
}
