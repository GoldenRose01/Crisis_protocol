using AsyncronQuest.SteampunkUI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AsyncronQuest.SteampunkUI.Editor
{
    public static class AsyncronQuestSteampunkUIBuilder
    {
        private const string PrefabFolder = "Assets/AsyncronQuest/SteampunkUI/Prefabs";
        private const string PrefabPath = PrefabFolder + "/AsyncronQuestSteampunkUI.prefab";
        private const string PauseMenuPrefabPath = PrefabFolder + "/GoldenCastPauseMenu.prefab";
        private const string StartMenuBackgroundPath = "Assets/AsyncronQuest/SteampunkUI/UI_Style/Start_menu.png";
        private const string PauseMenuBackgroundPath = "Assets/AsyncronQuest/SteampunkUI/UI_Style/Option_menu.png";

        [MenuItem("Tools/Asyncron Quest/Create Steampunk UI Prefab")]
        public static void CreatePrefab()
        {
            EnsureFolder("Assets/AsyncronQuest");
            EnsureFolder("Assets/AsyncronQuest/SteampunkUI");
            EnsureFolder(PrefabFolder);

            GameObject root = new GameObject("AsyncronQuestSteampunkUI");
            AsyncronQuestSteampunkUI ui = root.AddComponent<AsyncronQuestSteampunkUI>();
            AssignSprite(ui, "mainMenuBackgroundSprite", StartMenuBackgroundPath);
            ui.RebuildMainMenu();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Asyncron Quest", "Created prefab:\n" + PrefabPath, "OK");
        }

        [MenuItem("Tools/Asyncron Quest/Create Pause Menu Prefab")]
        public static void CreatePauseMenuPrefab()
        {
            EnsureFolder("Assets/AsyncronQuest");
            EnsureFolder("Assets/AsyncronQuest/SteampunkUI");
            EnsureFolder(PrefabFolder);

            GameObject root = new GameObject("GoldenCastPauseMenu");
            root.layer = 5;
            GoldenCastPauseMenu ui = root.AddComponent<GoldenCastPauseMenu>();
            AssignSprite(ui, "pauseMenuBackgroundSprite", PauseMenuBackgroundPath);
            ui.RebuildPauseMenu();
            PrefabUtility.SaveAsPrefabAsset(root, PauseMenuPrefabPath);
            Object.DestroyImmediate(root);

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Asyncron Quest", "Created prefab:\n" + PauseMenuPrefabPath, "OK");
        }

        [MenuItem("Tools/Asyncron Quest/Add Steampunk UI To Current Scene")]
        public static void AddToCurrentScene()
        {
            GameObject existing = GameObject.Find("AsyncronQuestSteampunkUI");
            if (existing != null)
            {
                Selection.activeGameObject = existing;
                EditorUtility.DisplayDialog("Asyncron Quest", "The scene already contains AsyncronQuestSteampunkUI.", "OK");
                return;
            }

            GameObject root = new GameObject("AsyncronQuestSteampunkUI");
            AsyncronQuestSteampunkUI ui = root.AddComponent<AsyncronQuestSteampunkUI>();
            AssignSprite(ui, "mainMenuBackgroundSprite", StartMenuBackgroundPath);
            ui.RebuildMainMenu();
            Undo.RegisterCreatedObjectUndo(root, "Add Asyncron Quest Steampunk UI");
            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        [MenuItem("Tools/Asyncron Quest/Sync Current Scene Main Menu From Prefab")]
        public static void SyncCurrentSceneMainMenuFromPrefab()
        {
            AsyncronQuestSteampunkUI source = AssetDatabase.LoadAssetAtPath<AsyncronQuestSteampunkUI>(PrefabPath);
            AsyncronQuestSteampunkUI target = Object.FindFirstObjectByType<AsyncronQuestSteampunkUI>();

            if (!source || !target)
            {
                EditorUtility.DisplayDialog("Asyncron Quest", "Prefab or scene AsyncronQuestSteampunkUI not found.", "OK");
                return;
            }

            EditorUtility.CopySerialized(source, target);
            EditorUtility.SetDirty(target);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = target.gameObject;
        }

        [MenuItem("Tools/Asyncron Quest/Add Pause Menu To Current Scene")]
        public static void AddPauseMenuToCurrentScene()
        {
            GoldenCastPauseMenu existing = Object.FindFirstObjectByType<GoldenCastPauseMenu>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorUtility.DisplayDialog("Asyncron Quest", "The scene already contains GoldenCastPauseMenu.", "OK");
                return;
            }

            GameObject root = new GameObject("GoldenCastPauseMenu");
            root.layer = 5;
            GoldenCastPauseMenu ui = root.AddComponent<GoldenCastPauseMenu>();
            AssignSprite(ui, "pauseMenuBackgroundSprite", PauseMenuBackgroundPath);
            ui.RebuildPauseMenu();
            Undo.RegisterCreatedObjectUndo(root, "Add GoldenCast Pause Menu");
            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            string leaf = path.Substring(slash + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void AssignSprite(Component component, string propertyName, string path)
        {
            Sprite sprite = LoadUiSprite(path);
            if (!sprite)
                return;

            SerializedObject serialized = new SerializedObject(component);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
                return;

            property.objectReferenceValue = sprite;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Sprite LoadUiSprite(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
