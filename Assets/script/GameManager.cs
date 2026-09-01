using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;

// Contenitore dati per la serializzazione JSON del salvataggio
[System.Serializable]
public class SaveDataWrapper
{
    public int punteggioTotale;
    public int missioniCompletate;
    public List<string> missioniCompletateIds = new List<string>();
}

/// <summary>
/// GameManager: gestisce lo stato globale persistente tra scene e sessioni.
/// Mantiene punteggio cumulativo, storico missioni completate e fornisce
/// accesso al caricamento/salvataggio della partita.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private const string DefaultGameplaySceneName = "locale";
    private const string SaveFileName = "CrisisProtocol_Save.json";

    // --- EVENTI GLOBALI ---
    public static event Action<int> OnPunteggioAggiornato;
    public static event Action<string> OnMissioneCompletata;

    // --- STATO A RUNTIME ---
    private int punteggioTotale = 0;
    private HashSet<string> missioniCompletate = new HashSet<string>();

    private string saveFilePath;

    // --- PROPRIETA' PUBBLICHE ---
    public int PunteggioTotale => punteggioTotale;
    public int MissioniCompletateCount => missioniCompletate.Count;

    private void Awake()
    {
        // Singleton rigoroso: una sola istanza persiste tra tutte le scene
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject);

        saveFilePath = Path.Combine(Application.persistentDataPath, SaveFileName);
        LoadGameState();
    }

    private void Update()
    {
        // Reset debug rapido con F5
        if (Input.GetKeyDown(KeyCode.F5))
        {
            Debug.LogWarning("[DEBUG] F5 premuto: reset completo dati di gioco.");
            ResetDatiDebug();
        }
    }

    // --- GESTIONE PUNTEGGIO ---

    /// <summary>
    /// Aggiunge punti al punteggio cumulativo globale e notifica la UI.
    /// </summary>
    public void AggiornaPunteggio(int punti)
    {
        punteggioTotale += punti;
        SaveGameState();
        OnPunteggioAggiornato?.Invoke(punteggioTotale);
        Debug.Log($"[GAMEMANAGER] Punteggio aggiornato: +{punti} → totale {punteggioTotale}");
    }

    // --- GESTIONE MISSIONI ---

    /// <summary>
    /// Registra una missione come completata (con ID univoco).
    /// Ritorna true se è una nuova missione, false se era già registrata.
    /// </summary>
    public bool RegistraMissioneCompletata(string missioneId)
    {
        if (string.IsNullOrWhiteSpace(missioneId))
            return false;

        bool isNuova = missioniCompletate.Add(missioneId);
        if (!isNuova)
            return false;

        SaveGameState();
        OnMissioneCompletata?.Invoke(missioneId);
        Debug.Log($"[GAMEMANAGER] Missione completata registrata: {missioneId}");
        return true;
    }

    /// <summary>
    /// Verifica se una missione specifica è già stata completata.
    /// </summary>
    public bool IsMissioneCompletata(string missioneId)
    {
        return missioniCompletate.Contains(missioneId);
    }

    // --- NAVIGAZIONE SCENE ---

    /// <summary>
    /// Carica lo stato salvato e avvia la scena di gioco principale.
    /// </summary>
    public void ResumeSavedGame()
    {
        LoadGameState();
        SceneManager.LoadScene(DefaultGameplaySceneName);
    }

    /// <summary>
    /// Avvia una nuova partita azzerando tutto e caricando la scena principale.
    /// </summary>
    public void NuovaPartita()
    {
        AzzeraStato();
        SaveGameState();
        SceneManager.LoadScene(DefaultGameplaySceneName);
    }

    // --- PERSISTENZA JSON ---

    public void SaveGameState()
    {
        SaveDataWrapper data = new SaveDataWrapper
        {
            punteggioTotale = this.punteggioTotale,
            missioniCompletate = this.missioniCompletate.Count,
            missioniCompletateIds = new List<string>(this.missioniCompletate)
        };

        string jsonOutput = JsonUtility.ToJson(data, true);
        File.WriteAllText(saveFilePath, jsonOutput);
        Debug.Log($"[GAMEMANAGER] Dati salvati in: {saveFilePath}");
    }

    public void LoadGameState()
    {
        if (File.Exists(saveFilePath))
        {
            string jsonInput = File.ReadAllText(saveFilePath);
            SaveDataWrapper data = JsonUtility.FromJson<SaveDataWrapper>(jsonInput);

            punteggioTotale = data.punteggioTotale;
            missioniCompletate = data.missioniCompletateIds != null
                ? new HashSet<string>(data.missioniCompletateIds)
                : new HashSet<string>();

            Debug.Log($"[GAMEMANAGER] Salvataggio caricato. Punteggio: {punteggioTotale} | Missioni: {missioniCompletate.Count}");
        }
        else
        {
            AzzeraStato();
            Debug.Log("[GAMEMANAGER] Nessun salvataggio trovato. Partita nuova.");
        }
    }

    // --- DEBUG ---

    [ContextMenu("RESETTA DATI DEBUG (CANCELLA SALVATAGGIO)")]
    public void ResetDatiDebug()
    {
        AzzeraStato();

        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
            Debug.Log("[DEBUG] File di salvataggio eliminato.");
        }
        else
        {
            Debug.LogWarning("[DEBUG] Nessun file di salvataggio presente.");
        }

        Scene scenaAttiva = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scenaAttiva.name);
    }

    private void AzzeraStato()
    {
        punteggioTotale = 0;
        missioniCompletate = new HashSet<string>();
    }
}