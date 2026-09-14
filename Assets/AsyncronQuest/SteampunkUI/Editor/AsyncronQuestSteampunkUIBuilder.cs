// ============================================================================
// Crisis Protocol / Sector Containment - UI e feedback AsyncronQuest
// File: .\Assets\AsyncronQuest\SteampunkUI\Editor\AsyncronQuestSteampunkUIBuilder.cs
// Responsabilita': fornisce schermate, tooltip, transizioni, menu e feedback visivi integrati nel progetto Crisis Protocol.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using AsyncronQuest.SteampunkUI; // usa lib // riga-ok
using UnityEditor; // usa lib // riga-ok
using UnityEditor.SceneManagement; // usa lib // riga-ok
using UnityEngine; // usa lib // riga-ok

namespace AsyncronQuest.SteampunkUI.Editor // zona cod // riga-ok
{ // apre // riga-ok
    public static class AsyncronQuestSteampunkUIBuilder // roba pub // riga-ok
    { // apre // riga-ok
        private const string PrefabFolder = "Assets/AsyncronQuest/SteampunkUI/Prefabs"; // roba pub // riga-ok
        private const string PrefabPath = PrefabFolder + "/AsyncronQuestSteampunkUI.prefab"; // roba pub // riga-ok
        private const string PauseMenuPrefabPath = PrefabFolder + "/CrisisProtocolPauseMenu.prefab"; // roba pub // riga-ok
        private const string StartMenuBackgroundPath = "Assets/AsyncronQuest/SteampunkUI/UI_Style/Start_menu.png"; // roba pub // riga-ok
        private const string PauseMenuBackgroundPath = "Assets/AsyncronQuest/SteampunkUI/UI_Style/Option_menu.png"; // roba pub // riga-ok

        [MenuItem("Tools/Asyncron Quest/Create Steampunk UI Prefab")] // nota unity // riga-ok
        // blocco: funzione fa cose
        public static void CreatePrefab() // roba pub // riga-ok
        { // apre // riga-ok
            EnsureFolder("Assets/AsyncronQuest"); // chiama // riga-ok
            EnsureFolder("Assets/AsyncronQuest/SteampunkUI"); // chiama // riga-ok
            EnsureFolder(PrefabFolder); // chiama // riga-ok

            GameObject root = new GameObject("AsyncronQuestSteampunkUI"); // setta // riga-ok
            AsyncronQuestSteampunkUI ui = root.AddComponent<AsyncronQuestSteampunkUI>(); // setta // riga-ok
            AssignVideoClip(ui, "backgroundVideoClip", "Assets/AsyncronQuest/SteampunkUI/UI_Style/DEVE_ESSERE_SOLO_IL_NEON_NENTE.mp4"); // chiama // riga-ok
            ui.RebuildMainMenu(); // chiama // riga-ok
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath); // chiama // riga-ok
            Object.DestroyImmediate(root); // chiama // riga-ok

            AssetDatabase.Refresh(); // chiama // riga-ok
            EditorUtility.DisplayDialog("Asyncron Quest", "Created prefab:\n" + PrefabPath, "OK"); // chiama // riga-ok
        } // chiude // riga-ok

        [MenuItem("Tools/Asyncron Quest/Create Pause Menu Prefab")] // nota unity // riga-ok
        // blocco: funzione fa cose
        public static void CreatePauseMenuPrefab() // roba pub // riga-ok
        { // apre // riga-ok
            EnsureFolder("Assets/AsyncronQuest"); // chiama // riga-ok
            EnsureFolder("Assets/AsyncronQuest/SteampunkUI"); // chiama // riga-ok
            EnsureFolder(PrefabFolder); // chiama // riga-ok

            GameObject root = new GameObject("CrisisProtocolPauseMenu"); // setta // riga-ok
            root.layer = 5; // setta // riga-ok
            CrisisProtocolPauseMenu ui = root.AddComponent<CrisisProtocolPauseMenu>(); // setta // riga-ok
            AssignSprite(ui, "pauseMenuBackgroundSprite", PauseMenuBackgroundPath); // chiama // riga-ok
            ui.RebuildPauseMenu(); // chiama // riga-ok
            PrefabUtility.SaveAsPrefabAsset(root, PauseMenuPrefabPath); // chiama // riga-ok
            Object.DestroyImmediate(root); // chiama // riga-ok

            AssetDatabase.Refresh(); // chiama // riga-ok
            EditorUtility.DisplayDialog("Asyncron Quest", "Created prefab:\n" + PauseMenuPrefabPath, "OK"); // chiama // riga-ok
        } // chiude // riga-ok

        [MenuItem("Tools/Asyncron Quest/Add Steampunk UI To Current Scene")] // nota unity // riga-ok
        // blocco: funzione fa cose
        public static void AddToCurrentScene() // roba pub // riga-ok
        { // apre // riga-ok
            GameObject existing = GameObject.Find("AsyncronQuestSteampunkUI"); // setta // riga-ok
            // blocco: controlla se va
            if (existing != null) // se ok // riga-ok
            { // apre // riga-ok
                Selection.activeGameObject = existing; // setta // riga-ok
                EditorUtility.DisplayDialog("Asyncron Quest", "The scene already contains AsyncronQuestSteampunkUI.", "OK"); // chiama // riga-ok
                return; // torna val // riga-ok
            } // chiude // riga-ok

            GameObject root = new GameObject("AsyncronQuestSteampunkUI"); // setta // riga-ok
            AsyncronQuestSteampunkUI ui = root.AddComponent<AsyncronQuestSteampunkUI>(); // setta // riga-ok
            AssignVideoClip(ui, "backgroundVideoClip", "Assets/AsyncronQuest/SteampunkUI/UI_Style/DEVE_ESSERE_SOLO_IL_NEON_NENTE.mp4"); // chiama // riga-ok
            ui.RebuildMainMenu(); // chiama // riga-ok
            Undo.RegisterCreatedObjectUndo(root, "Add Asyncron Quest Steampunk UI"); // chiama // riga-ok
            Selection.activeGameObject = root; // setta // riga-ok
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); // chiama // riga-ok
        } // chiude // riga-ok

        [MenuItem("Tools/Asyncron Quest/Sync Current Scene Main Menu From Prefab")] // nota unity // riga-ok
        // blocco: funzione fa cose
        public static void SyncCurrentSceneMainMenuFromPrefab() // roba pub // riga-ok
        { // apre // riga-ok
            AsyncronQuestSteampunkUI source = AssetDatabase.LoadAssetAtPath<AsyncronQuestSteampunkUI>(PrefabPath); // setta // riga-ok
            AsyncronQuestSteampunkUI target = Object.FindFirstObjectByType<AsyncronQuestSteampunkUI>(); // setta // riga-ok

            // blocco: controlla se va
            if (!source || !target) // se ok // riga-ok
            { // apre // riga-ok
                EditorUtility.DisplayDialog("Asyncron Quest", "Prefab or scene AsyncronQuestSteampunkUI not found.", "OK"); // chiama // riga-ok
                return; // torna val // riga-ok
            } // chiude // riga-ok

            EditorUtility.CopySerialized(source, target); // chiama // riga-ok
            EditorUtility.SetDirty(target); // chiama // riga-ok
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); // chiama // riga-ok
            Selection.activeGameObject = target.gameObject; // setta // riga-ok
        } // chiude // riga-ok

        [MenuItem("Tools/Asyncron Quest/Add Pause Menu To Current Scene")] // nota unity // riga-ok
        // blocco: funzione fa cose
        public static void AddPauseMenuToCurrentScene() // roba pub // riga-ok
        { // apre // riga-ok
            CrisisProtocolPauseMenu existing = Object.FindFirstObjectByType<CrisisProtocolPauseMenu>(); // setta // riga-ok
            // blocco: controlla se va
            if (existing != null) // se ok // riga-ok
            { // apre // riga-ok
                Selection.activeGameObject = existing.gameObject; // setta // riga-ok
                EditorUtility.DisplayDialog("Asyncron Quest", "The scene already contains CrisisProtocolPauseMenu.", "OK"); // chiama // riga-ok
                return; // torna val // riga-ok
            } // chiude // riga-ok

            GameObject root = new GameObject("CrisisProtocolPauseMenu"); // setta // riga-ok
            root.layer = 5; // setta // riga-ok
            CrisisProtocolPauseMenu ui = root.AddComponent<CrisisProtocolPauseMenu>(); // setta // riga-ok
            AssignSprite(ui, "pauseMenuBackgroundSprite", PauseMenuBackgroundPath); // chiama // riga-ok
            ui.RebuildPauseMenu(); // chiama // riga-ok
            Undo.RegisterCreatedObjectUndo(root, "Add Crisis Protocol Pause Menu"); // chiama // riga-ok
            Selection.activeGameObject = root; // setta // riga-ok
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private static void EnsureFolder(string path) // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (AssetDatabase.IsValidFolder(path)) // se ok // riga-ok
                return; // torna val // riga-ok

            int slash = path.LastIndexOf('/'); // setta // riga-ok
            string parent = path.Substring(0, slash); // setta // riga-ok
            string leaf = path.Substring(slash + 1); // setta // riga-ok
            EnsureFolder(parent); // chiama // riga-ok
            AssetDatabase.CreateFolder(parent, leaf); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private static void AssignSprite(Component component, string propertyName, string path) // roba pub // riga-ok
        { // apre // riga-ok
            Sprite sprite = LoadUiSprite(path); // setta // riga-ok
            // blocco: controlla se va
            if (!sprite) // se ok // riga-ok
                return; // torna val // riga-ok

            SerializedObject serialized = new SerializedObject(component); // setta // riga-ok
            SerializedProperty property = serialized.FindProperty(propertyName); // setta // riga-ok
            // blocco: controlla se va
            if (property == null) // se ok // riga-ok
                return; // torna val // riga-ok

            property.objectReferenceValue = sprite; // setta // riga-ok
            serialized.ApplyModifiedPropertiesWithoutUndo(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private static Sprite LoadUiSprite(string path) // roba pub // riga-ok
        { // apre // riga-ok
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter; // setta // riga-ok
            // blocco: controlla se va
            if (importer != null && importer.textureType != TextureImporterType.Sprite) // se ok // riga-ok
            { // apre // riga-ok
                importer.textureType = TextureImporterType.Sprite; // setta // riga-ok
                importer.spriteImportMode = SpriteImportMode.Single; // setta // riga-ok
                importer.mipmapEnabled = false; // setta // riga-ok
                importer.alphaIsTransparency = true; // setta // riga-ok
                importer.SaveAndReimport(); // chiama // riga-ok
            } // chiude // riga-ok

            return AssetDatabase.LoadAssetAtPath<Sprite>(path); // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private static void AssignVideoClip(Component component, string propertyName, string path) // roba pub // riga-ok
        { // apre // riga-ok
            UnityEngine.Video.VideoClip clip = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>(path); // setta // riga-ok
            // blocco: controlla se va
            if (!clip) // se ok // riga-ok
                return; // torna val // riga-ok

            SerializedObject serialized = new SerializedObject(component); // setta // riga-ok
            SerializedProperty property = serialized.FindProperty(propertyName); // setta // riga-ok
            // blocco: controlla se va
            if (property == null) // se ok // riga-ok
                return; // torna val // riga-ok

            property.objectReferenceValue = clip; // setta // riga-ok
            serialized.ApplyModifiedPropertiesWithoutUndo(); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
