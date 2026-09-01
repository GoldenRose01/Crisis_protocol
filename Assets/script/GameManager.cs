using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;

[System.Serializable]
public class SaveDataWrapper
{
    // Gameplay (CrisisProtocol)
    public int punteggioTotale;
    public int missioniCompletate;
    public List<string> missioniCompletateIds = new List<string>();

    // Sector Containment state
    public string savedCredentialId;
    public List<string> securitySignaturesAcquired = new List<string>();
    public List<string> incidentsResolved = new List<string>();
    public List<string> unlockedSecurityHistory = new List<string>();

    // Legacy GoldenCast fields kept for migration from older local saves.
    public string savedTagID;
    public List<string> tagTemporaliAcquisiti = new List<string>();
    public List<string> anacronismiRisolti = new List<string>();
    public List<string> tagSbloccatiStorico = new List<string>();
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
    private const string SaveFileName = "SectorContainment_Save.json";
    private const string LegacySaveFileName = "GoldenCast_Save.json";
    private const string PreviousSaveFileName = "CrisisProtocol_Save.json";

    // --- EVENTI GLOBALI ---
    public static event Action<int> OnPunteggioAggiornato;
    public static event Action<string> OnMissioneCompletata;

    public static event Action<string, int> OnSecuritySignatureAcquired;
    public static event Action<string, int> OnIncidentResolved;

    public static event Action<string, int> OnTemporalTagAcquired;
    public static event Action<string, int> OnAnachronismResolved;

    // --- STATO A RUNTIME ---
    private int punteggioTotale = 0;
    private HashSet<string> missioniCompletate = new HashSet<string>();

    [Header("Stato Operatore")]
    [Tooltip("Ultima firma di sicurezza acquisita dallo scanner. Nome legacy mantenuto per non rompere scene e prefab.")]
    public string currentTagID = "";

    private readonly HashSet<string> volatileContainmentState = new HashSet<string>();
    private HashSet<string> securitySignaturesAcquired = new HashSet<string>();
    private HashSet<string> incidentsResolved = new HashSet<string>();
    private HashSet<string> unlockedSecurityHistory = new HashSet<string>();
    private HashSet<string> authorizedReturnChannels = new HashSet<string>();

    private string saveFilePath;
    private string legacySaveFilePath;

    public string CurrentSecuritySignatureId => currentTagID;
    public int AcquiredSecuritySignatureCount => securitySignaturesAcquired.Count;
    public int ResolvedIncidentCount => incidentsResolved.Count;

    public int AcquiredTemporalTagCount => AcquiredSecuritySignatureCount;
    public int ResolvedAnachronismCount => ResolvedIncidentCount;

    // --- PROPRIETA' PUBBLICHE ---
    public int PunteggioTotale => punteggioTotale;
    public int MissioniCompletateCount => missioniCompletate.Count;

    private void Awake()
    {
        // Singleton rigoroso: una sola istanza persiste tra tutte le scene
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        saveFilePath = Path.Combine(Application.persistentDataPath, SaveFileName);
        legacySaveFilePath = Path.Combine(Application.persistentDataPath, LegacySaveFileName);
        LoadGameState();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            Debug.LogWarning("[DEBUG] F5 premuto. Reset completo dei dati di Sector Containment.");
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
    /// Stato volatile per logica causale (Sector Containment).
    /// </summary>
    public bool GetCausalState(string id)
    {
        return volatileContainmentState.Contains(id);
    }

    public void SetCausalState(string id, bool state)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        if (state)
            volatileContainmentState.Add(id);
        else
            volatileContainmentState.Remove(id);

        SaveGameState();
    }

    public void RegisterSecuritySignature(string signatureId)
    {
        Debug.Log($"<color=yellow>[SCANNER]</color> Firma di sicurezza rilevata: '{signatureId}'");

        if (string.IsNullOrWhiteSpace(signatureId))
        {
            Debug.LogError("[SCANNER] Firma di sicurezza vuota o nulla. Controllare configurazione dell'oggetto scansionato.");
            return;
        }

        currentTagID = signatureId;
        bool isNewSignature = securitySignaturesAcquired.Add(signatureId);

        if (unlockedSecurityHistory.Add(signatureId))
            Debug.Log($"<color=lime>[SECURITY]</color> Firma <b>{signatureId}</b> archiviata nello storico autorizzazioni.");

        if (MissionManager.Instance != null)
            MissionManager.Instance.RegistraCredenziale(signatureId);

        SaveGameState();

        if (isNewSignature)
        {
            OnSecuritySignatureAcquired?.Invoke(signatureId, securitySignaturesAcquired.Count);
            OnTemporalTagAcquired?.Invoke(signatureId, securitySignaturesAcquired.Count);
        }
    }

    public bool ResolveIncident(string incidentId)
    {
        if (string.IsNullOrWhiteSpace(incidentId))
            return false;

        bool isNewResolution = incidentsResolved.Add(incidentId);
        if (!isNewResolution)
            return false;

        SaveGameState();
        OnIncidentResolved?.Invoke(incidentId, incidentsResolved.Count);
        OnAnachronismResolved?.Invoke(incidentId, incidentsResolved.Count);
        Debug.Log($"[SECTOR] Incidente risolto: {incidentId}");
        return true;
    }

    public bool IsSecuritySignatureUnlocked(string signatureId)
    {
        return string.IsNullOrWhiteSpace(signatureId) || unlockedSecurityHistory.Contains(signatureId);
    }

    public void AuthorizeReturnChannel(string signatureId)
    {
        if (string.IsNullOrWhiteSpace(signatureId))
            return;

        if (authorizedReturnChannels.Add(signatureId))
            Debug.Log($"<color=cyan>[SECURITY]</color> Canale operativo autorizzato per firma <b>{signatureId}</b>.");
    }

    public bool ConsumeReturnChannel(string signatureId)
    {
        if (authorizedReturnChannels.Remove(signatureId))
        {
            Debug.Log($"<color=orange>[SECURITY]</color> Canale operativo consumato: <b>{signatureId}</b>.");
            return true;
        }

        return false;
    }

    public void ExtractTag(string newTagID) => RegisterSecuritySignature(newTagID);
    public bool ResolveAnachronism(string anachronismId) => ResolveIncident(anachronismId);
    public bool IsTagUnlocked(string tagID) => IsSecuritySignatureUnlocked(tagID);
    public void ApriVarcoRitorno(string tagID) => AuthorizeReturnChannel(tagID);
    public bool ConsumaVarcoRitorno(string tagID) => ConsumeReturnChannel(tagID);

    public void SaveGameState()
    {
        // Persist both CrisisProtocol gameplay fields and Sector Containment fields
        SaveDataWrapper data = new SaveDataWrapper
        {
            // Gameplay
            punteggioTotale = this.punteggioTotale,
            missioniCompletate = this.missioniCompletate.Count,
            missioniCompletateIds = new List<string>(this.missioniCompletate),

            // Sector containment
            savedCredentialId = currentTagID,
            savedTagID = currentTagID,
            securitySignaturesAcquired = new List<string>(this.securitySignaturesAcquired),
            incidentsResolved = new List<string>(this.incidentsResolved),
            unlockedSecurityHistory = new List<string>(this.unlockedSecurityHistory),

            // legacy lists for migration compatibility
            tagTemporaliAcquisiti = new List<string>(this.securitySignaturesAcquired),
            anacronismiRisolti = new List<string>(this.incidentsResolved),
            tagSbloccatiStorico = new List<string>(this.unlockedSecurityHistory)
        };

        string jsonOutput = JsonUtility.ToJson(data, true);
        File.WriteAllText(saveFilePath, jsonOutput);

        Debug.Log($"[SISTEMA] Stato salvato in: {saveFilePath}");
    }

    public void LoadGameState()
    {
        // Initialize/clear volatile collections
        volatileContainmentState.Clear();
        authorizedReturnChannels = new HashSet<string>();

        string pathToLoad = File.Exists(saveFilePath)
            ? saveFilePath
            : File.Exists(legacySaveFilePath) ? legacySaveFilePath : (File.Exists(Path.Combine(Application.persistentDataPath, PreviousSaveFileName)) ? Path.Combine(Application.persistentDataPath, PreviousSaveFileName) : string.Empty);

        if (!string.IsNullOrEmpty(pathToLoad))
        {
            string jsonInput = File.ReadAllText(pathToLoad);
            SaveDataWrapper data = JsonUtility.FromJson<SaveDataWrapper>(jsonInput);

            // Sector containment fields (with legacy fallbacks)
            currentTagID = FirstNonEmpty(data.savedCredentialId, data.savedTagID, "KEYCARD_A01");
            securitySignaturesAcquired = MergeLists(data.securitySignaturesAcquired, data.tagTemporaliAcquisiti);
            incidentsResolved = MergeLists(data.incidentsResolved, data.anacronismiRisolti);
            unlockedSecurityHistory = MergeLists(data.unlockedSecurityHistory, data.tagSbloccatiStorico);

            // Gameplay fields
            punteggioTotale = data.punteggioTotale;
            missioniCompletate = data.missioniCompletateIds != null ? new HashSet<string>(data.missioniCompletateIds) : new HashSet<string>();

            Debug.Log($"[SISTEMA] Stato ripristinato da: {pathToLoad}");
            return;
        }

        // No save found: initialize defaults for both systems
        securitySignaturesAcquired = new HashSet<string>();
        incidentsResolved = new HashSet<string>();
        unlockedSecurityHistory = new HashSet<string>();
        currentTagID = "KEYCARD_A01";

        punteggioTotale = 0;
        missioniCompletate = new HashSet<string>();

        Debug.Log("[SISTEMA] Nessun salvataggio rilevato. Avvio nuova emergenza/partita.");
    }
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

    [ContextMenu("RESETTA DATI DEBUG (CANCELLA JSON)")]
    public void ResetDatiDebug()
    {
        // Clear both gameplay and sector-containment runtime state
        volatileContainmentState.Clear();
        securitySignaturesAcquired.Clear();
        incidentsResolved.Clear();
        unlockedSecurityHistory.Clear();
        authorizedReturnChannels.Clear();
        currentTagID = "KEYCARD_A01";

        // Remove save files
        DeleteSaveFile(saveFilePath);
        DeleteSaveFile(legacySaveFilePath);
        string previousPath = Path.Combine(Application.persistentDataPath, PreviousSaveFileName);
        DeleteSaveFile(previousPath);

        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name);
    }

    private static HashSet<string> MergeLists(List<string> primary, List<string> legacy)
    {
        HashSet<string> result = new HashSet<string>();
        AddRange(result, primary);
        AddRange(result, legacy);
        return result;
    }

    private static void AddRange(HashSet<string> target, List<string> source)
    {
        if (source == null)
            return;

        foreach (string item in source)
        {
            if (!string.IsNullOrWhiteSpace(item))
                target.Add(item);
        }
    }

    private static string FirstNonEmpty(params string[] values)
    {
        foreach (string value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return string.Empty;
    }

    private static void DeleteSaveFile(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;

        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"[DEBUG] File salvataggio eliminato: {path}");
        }
    }

    private void AzzeraStato()
    {
        punteggioTotale = 0;
        missioniCompletate = new HashSet<string>();
    }
}
