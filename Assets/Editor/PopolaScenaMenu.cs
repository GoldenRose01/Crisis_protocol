#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PopolaScenaMenu
{
    [MenuItem("CrisisProtocol/Popola Scena con Oggetti Scenici")]
    public static void PopolaScenaEditor()
    {
        ProceduralScenePopulator populator = Object.FindAnyObjectByType<ProceduralScenePopulator>();
        if (populator == null)
        {
            GameObject go = new GameObject("ProceduralScenePopulator_Manager");
            populator = go.AddComponent<ProceduralScenePopulator>();
            Undo.RegisterCreatedObjectUndo(go, "Crea Gestore Oggetti Scenici");
        }

        populator.PopolaScenaCompleta();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("<color=green>[PopolaScenaMenu]</color> Scena popolata e contrassegnata come modificata!");
    }

    [MenuItem("CrisisProtocol/Rimuovi Oggetti Scenici")]
    public static void RimuoviOggettiSceniciEditor()
    {
        ProceduralScenePopulator populator = Object.FindAnyObjectByType<ProceduralScenePopulator>();
        if (populator != null)
        {
            populator.RimuoviOggettiScenici();
        }
        else
        {
            GameObject root = GameObject.Find("--- OGGETTI_SCENA_PROCEDURALI ---");
            if (root != null)
            {
                Undo.DestroyObjectImmediate(root);
            }
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("<color=yellow>[PopolaScenaMenu]</color> Oggetti scenici rimossi!");
    }
}
#endif
