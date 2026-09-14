#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

[InitializeOnLoad]
public class AutoSceneSaver : EditorWindow
{
    private const string PREF_ENABLED = "CrisisProtocol_AutoSave_Enabled";
    private const string PREF_INTERVAL = "CrisisProtocol_AutoSave_IntervalMin";
    private const string PREF_SAVE_ASSETS = "CrisisProtocol_AutoSave_SaveAssets";
    private const string PREF_LOG = "CrisisProtocol_AutoSave_LogConsole";

    // Impostazioni predefinite: 3 minuti
    private const float DEFAULT_INTERVAL_MINUTES = 3.0f;

    private static double nextSaveTime;
    private static bool isInitialized = false;

    // Proprietà caricate
    public static bool Enabled
    {
        get => EditorPrefs.GetBool(PREF_ENABLED, true);
        set
        {
            EditorPrefs.SetBool(PREF_ENABLED, value);
            ResetTimer();
        }
    }

    public static float IntervalMinutes
    {
        get => EditorPrefs.GetFloat(PREF_INTERVAL, DEFAULT_INTERVAL_MINUTES);
        set
        {
            EditorPrefs.SetFloat(PREF_INTERVAL, Mathf.Max(0.5f, value));
            ResetTimer();
        }
    }

    public static bool SaveAssets
    {
        get => EditorPrefs.GetBool(PREF_SAVE_ASSETS, true);
        set => EditorPrefs.SetBool(PREF_SAVE_ASSETS, value);
    }

    public static bool LogToConsole
    {
        get => EditorPrefs.GetBool(PREF_LOG, true);
        set => EditorPrefs.SetBool(PREF_LOG, value);
    }

    static AutoSceneSaver()
    {
        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.update += OnEditorUpdate;
        ResetTimer();
    }

    private static void OnEditorUpdate()
    {
        if (!Enabled)
            return;

        // Non salvare durante la modalità Play o durante la compilazione degli script
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            ResetTimer();
            return;
        }

        if (!isInitialized)
        {
            ResetTimer();
            isInitialized = true;
        }

        double currentTime = EditorApplication.timeSinceStartup;
        if (currentTime >= nextSaveTime)
        {
            EseguiSalvataggioAutomatico();
            ResetTimer();
        }
    }

    public static void ResetTimer()
    {
        nextSaveTime = EditorApplication.timeSinceStartup + (IntervalMinutes * 60.0);
    }

    public static double SecondiRimanenti()
    {
        double diff = nextSaveTime - EditorApplication.timeSinceStartup;
        return Math.Max(0.0, diff);
    }

    [MenuItem("CrisisProtocol/Auto Save/Salva Scena e Asset Ora (Forza)", false, 1)]
    [MenuItem("Tools/Auto Save/Salva Scena e Asset Ora (Forza)", false, 1)]
    public static void SalvaOraMenu()
    {
        EseguiSalvataggioAutomatico(forzaAncheSeNonDirty: true);
        ResetTimer();
    }

    [MenuItem("CrisisProtocol/Auto Save/Attiva o Disattiva Auto Save (3 Min)", false, 2)]
    [MenuItem("Tools/Auto Save/Attiva o Disattiva Auto Save (3 Min)", false, 2)]
    public static void ToggleAutoSave()
    {
        Enabled = !Enabled;
        string stato = Enabled ? "<color=lime>ATTIVATO (ogni " + IntervalMinutes + " minuti)</color>" : "<color=red>DISATTIVATO</color>";
        Debug.Log($"<color=cyan>[AUTO SAVE]</color> Stato salvataggio automatico: {stato}");
        ResetTimer();
    }

    [MenuItem("CrisisProtocol/Auto Save/Attiva o Disattiva Auto Save (3 Min)", true)]
    [MenuItem("Tools/Auto Save/Attiva o Disattiva Auto Save (3 Min)", true)]
    public static bool ToggleAutoSaveValidate()
    {
        Menu.SetChecked("CrisisProtocol/Auto Save/Attiva o Disattiva Auto Save (3 Min)", Enabled);
        Menu.SetChecked("Tools/Auto Save/Attiva o Disattiva Auto Save (3 Min)", Enabled);
        return true;
    }

    [MenuItem("CrisisProtocol/Auto Save/Pannello Impostazioni Auto Save", false, 3)]
    [MenuItem("Tools/Auto Save/Pannello Impostazioni Auto Save", false, 3)]
    public static void ApriFinestraImpostazioni()
    {
        var win = GetWindow<AutoSceneSaver>("Auto Save", true);
        win.minSize = new Vector2(380, 280);
        win.Show();
    }

    /// <summary>
    /// Esegue il salvataggio automatico di tutte le scene aperte e dei relativi asset.
    /// </summary>
    public static void EseguiSalvataggioAutomatico(bool forzaAncheSeNonDirty = false)
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling)
            return;

        int sceneSalvate = 0;
        bool ceraSceneDirty = false;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (!s.isLoaded) continue;

            if (s.isDirty)
                ceraSceneDirty = true;

            // Se la scena ha un percorso valido ed è dirty (o salvataggio forzato)
            if (!string.IsNullOrEmpty(s.path) && (s.isDirty || forzaAncheSeNonDirty))
            {
                if (EditorSceneManager.SaveScene(s))
                    sceneSalvate++;
            }
        }

        if (SaveAssets && (ceraSceneDirty || forzaAncheSeNonDirty))
        {
            AssetDatabase.SaveAssets();
        }

        if (LogToConsole && (sceneSalvate > 0 || forzaAncheSeNonDirty))
        {
            string orario = DateTime.Now.ToString("HH:mm:ss");
            Debug.Log($"<color=lime>[AUTO SAVE]</color> Scena e Asset salvati con successo alle <b>{orario}</b> ({sceneSalvate} scene salvate). Prossimo salvataggio tra {IntervalMinutes:F1} min.");
        }
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Configurazione Salvataggio Automatico", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox($"Il sistema salva automaticamente le scene aperte e il database asset ogni {IntervalMinutes:F1} minuti in background.", MessageType.Info);
        GUILayout.Space(10);

        bool enabled = EditorGUILayout.Toggle("Auto Save Attivo", Enabled);
        if (enabled != Enabled)
            Enabled = enabled;

        float interval = EditorGUILayout.Slider("Intervallo (Minuti)", IntervalMinutes, 0.5f, 30f);
        if (Math.Abs(interval - IntervalMinutes) > 0.05f)
            IntervalMinutes = Mathf.Round(interval * 2f) / 2f;

        bool saveAssets = EditorGUILayout.Toggle("Salva anche AssetDatabase", SaveAssets);
        if (saveAssets != SaveAssets)
            SaveAssets = saveAssets;

        bool logConsole = EditorGUILayout.Toggle("Mostra notifica in Console", LogToConsole);
        if (logConsole != LogToConsole)
            LogToConsole = logConsole;

        GUILayout.Space(15);
        EditorGUILayout.LabelField("Stato Attuale", EditorStyles.boldLabel);
        
        if (Enabled)
        {
            double sec = SecondiRimanenti();
            int minPart = (int)(sec / 60);
            int secPart = (int)(sec % 60);
            EditorGUILayout.LabelField("Prossimo salvataggio tra:", $"{minPart:D2}:{secPart:D2} min", EditorStyles.boldLabel);
        }
        else
        {
            EditorGUILayout.LabelField("Stato:", "DISATTIVATO", EditorStyles.boldLabel);
        }

        GUILayout.Space(15);
        if (GUILayout.Button("Salva Subito (Forza Salvataggio)", GUILayout.Height(32)))
        {
            SalvaOraMenu();
        }

        if (GUILayout.Button("Ripristina 3 Minuti Predefiniti", GUILayout.Height(24)))
        {
            IntervalMinutes = 3.0f;
            Enabled = true;
            SaveAssets = true;
            LogToConsole = true;
            ResetTimer();
        }

        // Ridisegna la finestra ogni secondo per aggiornare il countdown
        if (Enabled)
            Repaint();
    }
}
#endif
