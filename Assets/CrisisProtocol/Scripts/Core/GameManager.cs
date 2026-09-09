using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;

[System.Serializable]
public class SaveDataWrapper
{
    public int punteggioTotale;
    public int missioniCompletate;
    public List<string> missioniCompletateIds = new List<string>();

    public string savedCredentialId;
    public List<string> securitySignaturesAcquired = new List<string>();
    public List<string> incidentsResolved = new List<string>();
    public List<string> unlockedSecurityHistory = new List<string>();

    /// <summary>Indice dell'ultimo settore raggiunto (0 = Settore 0).</summary>
    public int lastSectorIndex = 0;
}

/// <summary>
/// GameManager: gestisce lo stato globale persistente tra scene e sessioni.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Configurazione Scene")]
    [Tooltip("Lista ordinata dei settori del gioco. Devono corrispondere esattamente ai nomi in Build Settings.")]
    [SerializeField] private List<string> livelliInOrdine = new List<string> { "settore 0", "settore 1", "settore 2" };

    /// <summary>Indice del settore attualmente caricato (0-based).</summary>
    private int indiceSettoreCorrente = 0;

    /// <summary>Nome della scena del menu principale.</summary>
    [SerializeField] private string scenaMainMenu = "MainMenu-Scene";

    private const string SaveFileName = "SectorContainment_Save.json";

    public static event Action<int> OnPunteggioAggiornato;
    public static event Action<string> OnMissioneCompletata;

    public static event Action<string, int> OnSecuritySignatureAcquired;
    public static event Action<string, int> OnIncidentResolved;

    private int punteggioTotale = 0;
    private HashSet<string> missioniCompletate = new HashSet<string>();

    [Header("Stato Operatore")]
    public string currentCredentialID = "";

    private readonly HashSet<string> volatileContainmentState = new HashSet<string>();
    private HashSet<string> securitySignaturesAcquired = new HashSet<string>();
    private HashSet<string> incidentsResolved = new HashSet<string>();
    private HashSet<string> unlockedSecurityHistory = new HashSet<string>();
    private HashSet<string> authorizedReturnChannels = new HashSet<string>();

    private string saveFilePath;

    public string CurrentSecuritySignatureId => currentCredentialID;
    public int AcquiredSecuritySignatureCount => securitySignaturesAcquired.Count;
    public int ResolvedIncidentCount => incidentsResolved.Count;

    public int PunteggioTotale => punteggioTotale;
    public int MissioniCompletateCount => missioniCompletate.Count;

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

    public void AggiornaPunteggio(int punti)
    {
        punteggioTotale += punti;
        SaveGameState();
        OnPunteggioAggiornato?.Invoke(punteggioTotale);
        Debug.Log($"[GAMEMANAGER] Punteggio aggiornato: +{punti} → totale {punteggioTotale}");
    }

    public bool RegistraMissioneCompletata(string missioneId)
    {
        if (string.IsNullOrWhiteSpace(missioneId)) return false;
        if (!missioniCompletate.Add(missioneId)) return false;

        SaveGameState();
        OnMissioneCompletata?.Invoke(missioneId);
        Debug.Log($"[GAMEMANAGER] Missione completata registrata: {missioneId}");
        return true;
    }

    public bool IsMissioneCompletata(string missioneId) => missioniCompletate.Contains(missioneId);

    public bool IsHotspotResolved(string id) => volatileContainmentState.Contains(id);

    // Legacy wrappers for OstacoloCausale compatibility
    public bool GetCausalState(string id) => IsHotspotResolved(id);
    public void SetCausalState(string id, bool state) => SetHotspotResolved(id, state);

    public void SetHotspotResolved(string id, bool state)
    {
        if (string.IsNullOrWhiteSpace(id)) return;
        if (state) volatileContainmentState.Add(id); else volatileContainmentState.Remove(id);
        SaveGameState();
    }

    public void RegisterSecuritySignature(string signatureId)
    {
        if (string.IsNullOrWhiteSpace(signatureId)) return;

        currentCredentialID = signatureId;
        bool isNewSignature = securitySignaturesAcquired.Add(signatureId);

        if (unlockedSecurityHistory.Add(signatureId))
            Debug.Log($"<color=lime>[SECURITY]</color> Firma <b>{signatureId}</b> archiviata nello storico autorizzazioni.");

        if (MissionManager.Instance != null)
            MissionManager.Instance.RegistraCredenziale(signatureId);

        SaveGameState();

        if (isNewSignature)
            OnSecuritySignatureAcquired?.Invoke(signatureId, securitySignaturesAcquired.Count);
    }

    public bool ResolveIncident(string incidentId)
    {
        if (string.IsNullOrWhiteSpace(incidentId)) return false;
        if (!incidentsResolved.Add(incidentId)) return false;

        SaveGameState();
        OnIncidentResolved?.Invoke(incidentId, incidentsResolved.Count);
        Debug.Log($"[SECTOR] Incidente risolto: {incidentId}");
        return true;
    }

    public bool IsSecuritySignatureUnlocked(string signatureId) => string.IsNullOrWhiteSpace(signatureId) || unlockedSecurityHistory.Contains(signatureId);

    public void AuthorizeReturnChannel(string signatureId)
    {
        if (string.IsNullOrWhiteSpace(signatureId)) return;
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

    public void SaveGameState()
    {
        SaveDataWrapper data = new SaveDataWrapper
        {
            punteggioTotale = this.punteggioTotale,
            missioniCompletate = this.missioniCompletate.Count,
            missioniCompletateIds = new List<string>(this.missioniCompletate),
            savedCredentialId = currentCredentialID,
            securitySignaturesAcquired = new List<string>(this.securitySignaturesAcquired),
            incidentsResolved = new List<string>(this.incidentsResolved),
            unlockedSecurityHistory = new List<string>(this.unlockedSecurityHistory),
            lastSectorIndex = this.indiceSettoreCorrente
        };

        File.WriteAllText(saveFilePath, JsonUtility.ToJson(data, true));
        Debug.Log($"[SISTEMA] Stato salvato in: {saveFilePath}");
    }

    public void LoadGameState()
    {
        volatileContainmentState.Clear();
        authorizedReturnChannels = new HashSet<string>();

        if (File.Exists(saveFilePath))
        {
            SaveDataWrapper data = JsonUtility.FromJson<SaveDataWrapper>(File.ReadAllText(saveFilePath));
            currentCredentialID = string.IsNullOrEmpty(data.savedCredentialId) ? "KEYCARD_A01" : data.savedCredentialId;
            securitySignaturesAcquired = new HashSet<string>(data.securitySignaturesAcquired);
            incidentsResolved = new HashSet<string>(data.incidentsResolved);
            unlockedSecurityHistory = new HashSet<string>(data.unlockedSecurityHistory);
            punteggioTotale = data.punteggioTotale;
            missioniCompletate = new HashSet<string>(data.missioniCompletateIds);
            indiceSettoreCorrente = Mathf.Clamp(data.lastSectorIndex, 0, Mathf.Max(0, livelliInOrdine.Count - 1));
            Debug.Log($"[SISTEMA] Stato ripristinato. Ultimo settore: {indiceSettoreCorrente}.");
            return;
        }

        securitySignaturesAcquired = new HashSet<string>();
        incidentsResolved = new HashSet<string>();
        unlockedSecurityHistory = new HashSet<string>();
        currentCredentialID = "KEYCARD_A01";
        punteggioTotale = 0;
        indiceSettoreCorrente = 0;
        missioniCompletate = new HashSet<string>();
        Debug.Log("[SISTEMA] Nessun salvataggio rilevato. Avvio nuova emergenza/partita.");
    }

    public void ResumeSavedGame()
    {
        LoadGameState();
        CaricaSettore(indiceSettoreCorrente);
    }

    public void NuovaPartita()
    {
        punteggioTotale = 0;
        indiceSettoreCorrente = 0;
        missioniCompletate.Clear();
        securitySignaturesAcquired.Clear();
        incidentsResolved.Clear();
        unlockedSecurityHistory.Clear();
        currentCredentialID = "KEYCARD_A01";
        SaveGameState();
        CaricaSettore(0);
    }

    /// <summary>
    /// Carica il settore per indice. Se l'indice supera la lista, torna al MainMenu (fine gioco).
    /// </summary>
    public void CaricaSettore(int indice)
    {
        if (livelliInOrdine == null || livelliInOrdine.Count == 0)
        {
            Debug.LogError("[GAMEMANAGER] Lista livelli vuota. Aggiungila nell'Inspector.");
            return;
        }

        if (indice >= livelliInOrdine.Count)
        {
            Debug.Log("[GAMEMANAGER] Tutti i settori completati. Ritorno al MainMenu.");
            SceneManager.LoadScene(scenaMainMenu);
            return;
        }

        indiceSettoreCorrente = Mathf.Clamp(indice, 0, livelliInOrdine.Count - 1);
        string nomeScena = livelliInOrdine[indiceSettoreCorrente];
        SaveGameState();
        Debug.Log($"[GAMEMANAGER] Carico settore {indiceSettoreCorrente}: '{nomeScena}'");
        SceneManager.LoadScene(nomeScena);
    }

    /// <summary>Avanza al settore successivo (chiamato da MissionManager su vittoria).</summary>
    public void CaricaProssimoSettore()
    {
        CaricaSettore(indiceSettoreCorrente + 1);
    }

    /// <summary>Ricarica il settore corrente (chiamato su sconfitta).</summary>
    public void CaricaSettoreCorrente()
    {
        CaricaSettore(indiceSettoreCorrente);
    }

    /// <summary>Indice del settore attualmente attivo (0-based).</summary>
    public int IndiceSettoreCorrente => indiceSettoreCorrente;

    [ContextMenu("RESETTA DATI DEBUG (CANCELLA JSON)")]
    public void ResetDatiDebug()
    {
        volatileContainmentState.Clear();
        securitySignaturesAcquired.Clear();
        incidentsResolved.Clear();
        unlockedSecurityHistory.Clear();
        authorizedReturnChannels.Clear();
        currentCredentialID = "KEYCARD_A01";

        if (File.Exists(saveFilePath)) File.Delete(saveFilePath);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
