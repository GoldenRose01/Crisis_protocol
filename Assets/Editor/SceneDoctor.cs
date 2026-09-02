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
        Debug.Log("[SceneDoctor] Inizio pulizia della scena...");

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
            Debug.Log($"<color=green>[SceneDoctor] Pulizia completata: {missingCount} script fantasma rimossi!</color>");
        else
            Debug.Log("[SceneDoctor] Nessuno script mancante trovato.");

        // 2. Assicurati che i pavimenti/muri siano Navigation Static
        int staticCount = 0;
        foreach (GameObject go in allObjects)
        {
            // Seleziona oggetti che dovrebbero essere ostacoli o pavimenti
            string n = go.name.ToLower();
            if (n.Contains("muro") || n.Contains("wall") || n.Contains("floor") || 
                n.Contains("pavimento") || n.Contains("riccinto") || n.Contains("ostacolo") ||
                n.Contains("cube") || n.Contains("plane"))
            {
                // Se ha un collider ed e' attivo
                Collider col = go.GetComponent<Collider>();
                if (col != null && !col.isTrigger)
                {
                    StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(go);
#pragma warning disable 0618
                    if ((flags & StaticEditorFlags.NavigationStatic) == 0)
                    {
                        flags |= StaticEditorFlags.NavigationStatic;
                        GameObjectUtility.SetStaticEditorFlags(go, flags);
                        staticCount++;
                    }
#pragma warning restore 0618
                }
            }
        }

        if (staticCount > 0)
            Debug.Log($"<color=green>[SceneDoctor] {staticCount} oggetti impostati automaticamente come Navigation Static.</color>");

        // 3. Genera la NavMesh
        Debug.Log("[SceneDoctor] Avvio generazione automatica NavMesh...");
        UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
        Debug.Log("<color=cyan>[SceneDoctor] Generazione NavMesh COMPLETATA! Gli NPC ora possono muoversi.</color>");

        EditorUtility.DisplayDialog(
            "Scene Doctor", 
            $"Pulizia completata!\n\n- Script mancanti rimossi: {missingCount}\n- Oggetti resi statici: {staticCount}\n- NavMesh rigenerata con successo.\n\nPremi Play per testare il gioco.", 
            "OK"
        );
    }
}
#endif
