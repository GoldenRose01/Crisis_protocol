using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;

[Serializable]
public class SaveDataWrapper
{
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

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private const string DefaultGameplaySceneName = "locale";
    private const string SaveFileName = "SectorContainment_Save.json";
    private const string LegacySaveFileName = "GoldenCast_Save.json";

    [Header("Stato Operatore")]
    [Tooltip("Ultima firma di sicurezza acquisita dallo scanner. Nome legacy mantenuto per non rompere scene e prefab.")]
    public string currentTagID = "";

    public static event Action<string, int> OnSecuritySignatureAcquired;
    public static event Action<string, int> OnIncidentResolved;

    public static event Action<string, int> OnTemporalTagAcquired;
    public static event Action<string, int> OnAnachronismResolved;

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

    private void Awake()
    {
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
        SaveDataWrapper data = new SaveDataWrapper
        {
            savedCredentialId = currentTagID,
            savedTagID = currentTagID,
            securitySignaturesAcquired = new List<string>(securitySignaturesAcquired),
            incidentsResolved = new List<string>(incidentsResolved),
            unlockedSecurityHistory = new List<string>(unlockedSecurityHistory),
            tagTemporaliAcquisiti = new List<string>(securitySignaturesAcquired),
            anacronismiRisolti = new List<string>(incidentsResolved),
            tagSbloccatiStorico = new List<string>(unlockedSecurityHistory)
        };

        string jsonOutput = JsonUtility.ToJson(data, true);
        File.WriteAllText(saveFilePath, jsonOutput);

        Debug.Log($"[SISTEMA] Stato Sector Containment salvato in: {saveFilePath}");
    }

    public void LoadGameState()
    {
        volatileContainmentState.Clear();
        authorizedReturnChannels = new HashSet<string>();

        string pathToLoad = File.Exists(saveFilePath)
            ? saveFilePath
            : File.Exists(legacySaveFilePath) ? legacySaveFilePath : string.Empty;

        if (!string.IsNullOrEmpty(pathToLoad))
        {
            string jsonInput = File.ReadAllText(pathToLoad);
            SaveDataWrapper data = JsonUtility.FromJson<SaveDataWrapper>(jsonInput);

            currentTagID = FirstNonEmpty(data.savedCredentialId, data.savedTagID, "KEYCARD_A01");
            securitySignaturesAcquired = MergeLists(data.securitySignaturesAcquired, data.tagTemporaliAcquisiti);
            incidentsResolved = MergeLists(data.incidentsResolved, data.anacronismiRisolti);
            unlockedSecurityHistory = MergeLists(data.unlockedSecurityHistory, data.tagSbloccatiStorico);

            Debug.Log($"[SISTEMA] Stato Sector Containment ripristinato da: {pathToLoad}");
            return;
        }

        securitySignaturesAcquired = new HashSet<string>();
        incidentsResolved = new HashSet<string>();
        unlockedSecurityHistory = new HashSet<string>();
        currentTagID = "KEYCARD_A01";
        Debug.Log("[SISTEMA] Nessun salvataggio rilevato. Avvio nuova emergenza di settore.");
    }

    public void ResumeSavedGame()
    {
        LoadGameState();
        SceneManager.LoadScene(DefaultGameplaySceneName);
    }

    [ContextMenu("RESETTA DATI DEBUG (CANCELLA JSON)")]
    public void ResetDatiDebug()
    {
        volatileContainmentState.Clear();
        securitySignaturesAcquired.Clear();
        incidentsResolved.Clear();
        unlockedSecurityHistory.Clear();
        authorizedReturnChannels.Clear();
        currentTagID = "KEYCARD_A01";

        DeleteSaveFile(saveFilePath);
        DeleteSaveFile(legacySaveFilePath);

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
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"[DEBUG] File salvataggio eliminato: {path}");
        }
    }
}
