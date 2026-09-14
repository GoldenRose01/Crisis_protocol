// ============================================================================
// Crisis Protocol / Sector Containment - Utility editor
// File: .\Assets\Editor\AutoSceneSaver.cs
// Responsabilita': automatizza setup, popolamento scena, salvataggio, validazione o manutenzione direttamente dentro Unity Editor.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
#if UNITY_EDITOR // prep ok // riga-ok
using UnityEditor; // usa lib // riga-ok
using UnityEditor.SceneManagement; // usa lib // riga-ok
using UnityEngine; // usa lib // riga-ok
using UnityEngine.SceneManagement; // usa lib // riga-ok
using System; // usa lib // riga-ok

[InitializeOnLoad] // nota unity // riga-ok
// blocco: classe x roba grossa
public class AutoSceneSaver : EditorWindow // classe qui // riga-ok
{ // apre // riga-ok
    private const string PREF_ENABLED = "CrisisProtocol_AutoSave_Enabled"; // roba pub // riga-ok
    private const string PREF_INTERVAL = "CrisisProtocol_AutoSave_IntervalMin"; // roba pub // riga-ok
    private const string PREF_SAVE_ASSETS = "CrisisProtocol_AutoSave_SaveAssets"; // roba pub // riga-ok
    private const string PREF_LOG = "CrisisProtocol_AutoSave_LogConsole"; // roba pub // riga-ok

    // Impostazioni predefinite: 3 minuti
    private const float DEFAULT_INTERVAL_MINUTES = 3.0f; // roba pub // riga-ok

    private static double nextSaveTime; // roba pub // riga-ok
    private static bool isInitialized = false; // roba pub // riga-ok

    // Proprietà caricate
    public static bool Enabled // roba pub // riga-ok
    { // apre // riga-ok
        get => EditorPrefs.GetBool(PREF_ENABLED, true); // setta // riga-ok
        set // ok qua // riga-ok
        { // apre // riga-ok
            EditorPrefs.SetBool(PREF_ENABLED, value); // chiama // riga-ok
            ResetTimer(); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    public static float IntervalMinutes // roba pub // riga-ok
    { // apre // riga-ok
        get => EditorPrefs.GetFloat(PREF_INTERVAL, DEFAULT_INTERVAL_MINUTES); // setta // riga-ok
        set // ok qua // riga-ok
        { // apre // riga-ok
            EditorPrefs.SetFloat(PREF_INTERVAL, Mathf.Max(0.5f, value)); // chiama // riga-ok
            ResetTimer(); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    public static bool SaveAssets // roba pub // riga-ok
    { // apre // riga-ok
        get => EditorPrefs.GetBool(PREF_SAVE_ASSETS, true); // setta // riga-ok
        set => EditorPrefs.SetBool(PREF_SAVE_ASSETS, value); // setta // riga-ok
    } // chiude // riga-ok

    public static bool LogToConsole // roba pub // riga-ok
    { // apre // riga-ok
        get => EditorPrefs.GetBool(PREF_LOG, true); // setta // riga-ok
        set => EditorPrefs.SetBool(PREF_LOG, value); // setta // riga-ok
    } // chiude // riga-ok

    static AutoSceneSaver() // roba pub // riga-ok
    { // apre // riga-ok
        EditorApplication.update -= OnEditorUpdate; // setta // riga-ok
        EditorApplication.update += OnEditorUpdate; // setta // riga-ok
        ResetTimer(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private static void OnEditorUpdate() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (!Enabled) // se ok // riga-ok
            return; // torna val // riga-ok

        // Non salvare durante la modalità Play o durante la compilazione degli script
        // blocco: controlla se va
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) // se ok // riga-ok
        { // apre // riga-ok
            ResetTimer(); // chiama // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (!isInitialized) // se ok // riga-ok
        { // apre // riga-ok
            ResetTimer(); // chiama // riga-ok
            isInitialized = true; // setta // riga-ok
        } // chiude // riga-ok

        double currentTime = EditorApplication.timeSinceStartup; // setta // riga-ok
        // blocco: controlla se va
        if (currentTime >= nextSaveTime) // se ok // riga-ok
        { // apre // riga-ok
            EseguiSalvataggioAutomatico(); // chiama // riga-ok
            ResetTimer(); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public static void ResetTimer() // roba pub // riga-ok
    { // apre // riga-ok
        nextSaveTime = EditorApplication.timeSinceStartup + (IntervalMinutes * 60.0); // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public static double SecondiRimanenti() // roba pub // riga-ok
    { // apre // riga-ok
        double diff = nextSaveTime - EditorApplication.timeSinceStartup; // setta // riga-ok
        return Math.Max(0.0, diff); // torna val // riga-ok
    } // chiude // riga-ok

    [MenuItem("CrisisProtocol/Auto Save/Salva Scena e Asset Ora (Forza)", false, 1)] // nota unity // riga-ok
    [MenuItem("Tools/Auto Save/Salva Scena e Asset Ora (Forza)", false, 1)] // nota unity // riga-ok
    // blocco: funzione fa cose
    public static void SalvaOraMenu() // roba pub // riga-ok
    { // apre // riga-ok
        EseguiSalvataggioAutomatico(forzaAncheSeNonDirty: true); // chiama // riga-ok
        ResetTimer(); // chiama // riga-ok
    } // chiude // riga-ok

    [MenuItem("CrisisProtocol/Auto Save/Attiva o Disattiva Auto Save (3 Min)", false, 2)] // nota unity // riga-ok
    [MenuItem("Tools/Auto Save/Attiva o Disattiva Auto Save (3 Min)", false, 2)] // nota unity // riga-ok
    // blocco: funzione fa cose
    public static void ToggleAutoSave() // roba pub // riga-ok
    { // apre // riga-ok
        Enabled = !Enabled; // setta // riga-ok
        string stato = Enabled ? "<color=lime>ATTIVATO (ogni " + IntervalMinutes + " minuti)</color>" : "<color=red>DISATTIVATO</color>"; // setta // riga-ok
        Debug.Log($"<color=cyan>[AUTO SAVE]</color> Stato salvataggio automatico: {stato}"); // logga // riga-ok
        ResetTimer(); // chiama // riga-ok
    } // chiude // riga-ok

    [MenuItem("CrisisProtocol/Auto Save/Attiva o Disattiva Auto Save (3 Min)", true)] // nota unity // riga-ok
    [MenuItem("Tools/Auto Save/Attiva o Disattiva Auto Save (3 Min)", true)] // nota unity // riga-ok
    // blocco: funzione fa cose
    public static bool ToggleAutoSaveValidate() // roba pub // riga-ok
    { // apre // riga-ok
        Menu.SetChecked("CrisisProtocol/Auto Save/Attiva o Disattiva Auto Save (3 Min)", Enabled); // chiama // riga-ok
        Menu.SetChecked("Tools/Auto Save/Attiva o Disattiva Auto Save (3 Min)", Enabled); // chiama // riga-ok
        return true; // torna val // riga-ok
    } // chiude // riga-ok

    [MenuItem("CrisisProtocol/Auto Save/Pannello Impostazioni Auto Save", false, 3)] // nota unity // riga-ok
    [MenuItem("Tools/Auto Save/Pannello Impostazioni Auto Save", false, 3)] // nota unity // riga-ok
    // blocco: funzione fa cose
    public static void ApriFinestraImpostazioni() // roba pub // riga-ok
    { // apre // riga-ok
        var win = GetWindow<AutoSceneSaver>("Auto Save", true); // setta // riga-ok
        win.minSize = new Vector2(380, 280); // setta // riga-ok
        win.Show(); // chiama // riga-ok
    } // chiude // riga-ok

    /// <summary>
    /// Esegue il salvataggio automatico di tutte le scene aperte e dei relativi asset.
    /// </summary>
    // blocco: funzione fa cose
    public static void EseguiSalvataggioAutomatico(bool forzaAncheSeNonDirty = false) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (EditorApplication.isPlaying || EditorApplication.isCompiling) // se ok // riga-ok
            return; // torna val // riga-ok

        int sceneSalvate = 0; // setta // riga-ok
        bool ceraSceneDirty = false; // setta // riga-ok

        // blocco: gira piu volte
        for (int i = 0; i < SceneManager.sceneCount; i++) // ciclo x // riga-ok
        { // apre // riga-ok
            Scene s = SceneManager.GetSceneAt(i); // setta // riga-ok
            // blocco: controlla se va
            if (!s.isLoaded) continue; // se ok // riga-ok

            // blocco: controlla se va
            if (s.isDirty) // se ok // riga-ok
                ceraSceneDirty = true; // setta // riga-ok

            // Se la scena ha un percorso valido ed è dirty (o salvataggio forzato)
            // blocco: controlla se va
            if (!string.IsNullOrEmpty(s.path) && (s.isDirty || forzaAncheSeNonDirty)) // se ok // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (EditorSceneManager.SaveScene(s)) // se ok // riga-ok
                    sceneSalvate++; // ok qua // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (SaveAssets && (ceraSceneDirty || forzaAncheSeNonDirty)) // se ok // riga-ok
        { // apre // riga-ok
            AssetDatabase.SaveAssets(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (LogToConsole && (sceneSalvate > 0 || forzaAncheSeNonDirty)) // se ok // riga-ok
        { // apre // riga-ok
            string orario = DateTime.Now.ToString("HH:mm:ss"); // setta // riga-ok
            Debug.Log($"<color=lime>[AUTO SAVE]</color> Scena e Asset salvati con successo alle <b>{orario}</b> ({sceneSalvate} scene salvate). Prossimo salvataggio tra {IntervalMinutes:F1} min."); // logga // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnGUI() // roba pub // riga-ok
    { // apre // riga-ok
        GUILayout.Space(10); // chiama // riga-ok
        EditorGUILayout.LabelField("Configurazione Salvataggio Automatico", EditorStyles.boldLabel); // chiama // riga-ok
        EditorGUILayout.HelpBox($"Il sistema salva automaticamente le scene aperte e il database asset ogni {IntervalMinutes:F1} minuti in background.", MessageType.Info); // chiama // riga-ok
        GUILayout.Space(10); // chiama // riga-ok

        bool enabled = EditorGUILayout.Toggle("Auto Save Attivo", Enabled); // setta // riga-ok
        // blocco: controlla se va
        if (enabled != Enabled) // se ok // riga-ok
            Enabled = enabled; // setta // riga-ok

        float interval = EditorGUILayout.Slider("Intervallo (Minuti)", IntervalMinutes, 0.5f, 30f); // setta // riga-ok
        // blocco: controlla se va
        if (Math.Abs(interval - IntervalMinutes) > 0.05f) // se ok // riga-ok
            IntervalMinutes = Mathf.Round(interval * 2f) / 2f; // setta // riga-ok

        bool saveAssets = EditorGUILayout.Toggle("Salva anche AssetDatabase", SaveAssets); // setta // riga-ok
        // blocco: controlla se va
        if (saveAssets != SaveAssets) // se ok // riga-ok
            SaveAssets = saveAssets; // setta // riga-ok

        bool logConsole = EditorGUILayout.Toggle("Mostra notifica in Console", LogToConsole); // setta // riga-ok
        // blocco: controlla se va
        if (logConsole != LogToConsole) // se ok // riga-ok
            LogToConsole = logConsole; // setta // riga-ok

        GUILayout.Space(15); // chiama // riga-ok
        EditorGUILayout.LabelField("Stato Attuale", EditorStyles.boldLabel); // chiama // riga-ok
        
        // blocco: controlla se va
        if (Enabled) // se ok // riga-ok
        { // apre // riga-ok
            double sec = SecondiRimanenti(); // setta // riga-ok
            int minPart = (int)(sec / 60); // setta // riga-ok
            int secPart = (int)(sec % 60); // setta // riga-ok
            EditorGUILayout.LabelField("Prossimo salvataggio tra:", $"{minPart:D2}:{secPart:D2} min", EditorStyles.boldLabel); // chiama // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            EditorGUILayout.LabelField("Stato:", "DISATTIVATO", EditorStyles.boldLabel); // chiama // riga-ok
        } // chiude // riga-ok

        GUILayout.Space(15); // chiama // riga-ok
        // blocco: controlla se va
        if (GUILayout.Button("Salva Subito (Forza Salvataggio)", GUILayout.Height(32))) // se ok // riga-ok
        { // apre // riga-ok
            SalvaOraMenu(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (GUILayout.Button("Ripristina 3 Minuti Predefiniti", GUILayout.Height(24))) // se ok // riga-ok
        { // apre // riga-ok
            IntervalMinutes = 3.0f; // setta // riga-ok
            Enabled = true; // setta // riga-ok
            SaveAssets = true; // setta // riga-ok
            LogToConsole = true; // setta // riga-ok
            ResetTimer(); // chiama // riga-ok
        } // chiude // riga-ok

        // Ridisegna la finestra ogni secondo per aggiornare il countdown
        // blocco: controlla se va
        if (Enabled) // se ok // riga-ok
            Repaint(); // chiama // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
#endif // prep ok // riga-ok
