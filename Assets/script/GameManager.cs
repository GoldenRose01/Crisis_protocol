using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;

// 1. Classe contenitore ottimizzata per la serializzazione JSON
[System.Serializable]
public class SaveDataWrapper
{
    public string savedTagID;
    public List<string> tagTemporaliAcquisiti = new List<string>();
    public List<string> anacronismiRisolti = new List<string>();
    
    // Registro dei tag storici estratti e sbloccati dal giocatore
    public List<string> tagSbloccatiStorico = new List<string>();

    // NOTA: 'oggettiDistrutti' è stato rimosso da qui poiché la distruzione 
    // degli ostacoli deve essere volatile (resettata al riavvio del gioco).
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private const string DefaultGameplaySceneName = "locale";

    [Header("Stato del Viaggiatore")]
    [Tooltip("L'ultimo tag estratto dallo scanner temporale.")]
    public string currentTagID = ""; 

    public static event Action<string, int> OnTemporalTagAcquired;
    public static event Action<string, int> OnAnachronismResolved;

    // --- STRUTTURE DATI A RUNTIME (RAM) ---
    // Questo registro è VOLATILE: tiene traccia dei muri rotti nella sessione corrente,
    // garantendo la coerenza durante i viaggi nel tempo, ma si azzera al riavvio.
    private HashSet<string> registroCausale = new HashSet<string>();
    
    // Strutture persistenti nel file JSON
    private HashSet<string> tagTemporaliAcquisiti = new HashSet<string>();
    private HashSet<string> anacronismiRisolti = new HashSet<string>();
    private HashSet<string> storicoTagSbloccati = new HashSet<string>();
    
    // Registro volatile per la gestione asimmetrica delle Leyline (Andata -> Ritorno)
    private HashSet<string> varchiApertiPerRitorno = new HashSet<string>();

    private string saveFilePath;

    private void Awake()
    {
        // Implementazione rigorosa del pattern Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject);

        // Definizione del percorso di persistenza conforme al file system del sistema operativo ospite
        saveFilePath = Path.Combine(Application.persistentDataPath, "GoldenCast_Save.json");

        // Deserializzazione e ripristino dello stato del mondo al boot dell'applicazione
        LoadGameState();
    }

    private void Update()
    {
        // Forza l'azzeramento della memoria tramite shortcut di debug
        if (Input.GetKeyDown(KeyCode.F5))
        {
            Debug.LogWarning("[DEBUG] Tasto F5 premuto. Avvio azzeramento completo dei dati...");
            ResetDatiDebug();
        }
    }

    // --- LOGICA DI ALTERAZIONE TEMPORALE (OSTACOLI) ---

    public bool GetCausalState(string id)
    {
        return registroCausale.Contains(id);
    }

    public void SetCausalState(string id, bool stato)
    {
        if (stato)
        {
            registroCausale.Add(id);
        }
        else
        {
            registroCausale.Remove(id);
        }

        // Salva lo stato degli altri elementi, ma NON i muri (che rimangono solo in RAM)
        SaveGameState();
    }

    // --- LOGICA DELLO SCANNER E REGISTRO TEMPORALE ---

    /// <summary>
    /// Registra l'estrazione di un neo tag, inserendolo sia come tag attivo che nello storico persistente.
    /// </summary>
    public void ExtractTag(string newTagID)
    {
        Debug.Log($"<color=yellow>[TEST ESTRAZIONE]</color> Ricevuta richiesta di salvataggio per il tag: '{newTagID}'");

        if (string.IsNullOrWhiteSpace(newTagID))
        {
            Debug.LogError("[ERRORE ESTRAZIONE] Il tag ricevuto è vuoto o nullo! L'oggetto anacronismo non sta inviando l'ID corretto.");
            return;
        }

        currentTagID = newTagID;
        bool isNewTag = tagTemporaliAcquisiti.Add(newTagID);

        // Inserimento all'interno del registro storico ad alta efficienza
        if (!storicoTagSbloccati.Contains(newTagID))
        {
            storicoTagSbloccati.Add(newTagID);
            Debug.Log($"<color=lime>[MANAGER]</color> Nuovo Tag <b>{newTagID}</b> archiviato permanentemente nello storico.");
        }

        Debug.Log($"[GAMEMANAGER] Tag registrato come attivo nel sistema: {currentTagID}");
        SaveGameState();

        if (isNewTag)
            OnTemporalTagAcquired?.Invoke(newTagID, tagTemporaliAcquisiti.Count);
    }

    public int AcquiredTemporalTagCount => tagTemporaliAcquisiti.Count;
    public int ResolvedAnachronismCount => anacronismiRisolti.Count;

    public bool ResolveAnachronism(string anachronismId)
    {
        if (string.IsNullOrWhiteSpace(anachronismId))
            return false;

        bool isNewResolution = anacronismiRisolti.Add(anachronismId);
        if (!isNewResolution)
            return false;

        SaveGameState();
        OnAnachronismResolved?.Invoke(anachronismId, anacronismiRisolti.Count);
        Debug.Log($"[GAMEMANAGER] Anacronismo risolto: {anachronismId}");
        return true;
    }

    /// <summary>
    /// Verifica se un determinato Tag ID è mai stato estratto.
    /// </summary>
    public bool IsTagUnlocked(string tagID)
    {
        return storicoTagSbloccati.Contains(tagID);
    }

    // --- LOGICA TRAIETTORIE E VARCHI TEMPORALI (LEYLINE) ---

    /// <summary>
    /// Registra l'apertura di un canale di ritorno dal passato verso il futuro.
    /// </summary>
    public void ApriVarcoRitorno(string tagID)
    {
        if (!varchiApertiPerRitorno.Contains(tagID))
        {
            varchiApertiPerRitorno.Add(tagID);
            Debug.Log($"<color=cyan>[MANAGER]</color> Canale di ritorno abilitato in RAM per la firma temporale: <b>{tagID}</b>.");
        }
    }

    /// <summary>
    /// Verifica se il canale di ritorno è attivo e, in caso positivo, lo consuma chiudendo il varco.
    /// </summary>
    public bool ConsumaVarcoRitorno(string tagID)
    {
        if (varchiApertiPerRitorno.Contains(tagID))
        {
            varchiApertiPerRitorno.Remove(tagID); // Consuma il varco per prevenire exploit di ritorno infinito
            Debug.Log($"<color=orange>[MANAGER]</color> Varco temporale consumato. Canale <b>{tagID}</b> chiuso.");
            return true;
        }
        return false;
    }

    // --- PERSISTENZA DEI DATI (JSON) ---

    public void SaveGameState()
    {
        SaveDataWrapper data = new SaveDataWrapper();
        data.savedTagID = currentTagID;
        data.tagTemporaliAcquisiti = new List<string>(tagTemporaliAcquisiti);
        data.anacronismiRisolti = new List<string>(anacronismiRisolti);
        data.tagSbloccatiStorico = new List<string>(storicoTagSbloccati);

        // NOTA: La scrittura di 'registroCausale' su JSON è stata intenzionalmente omessa 
        // per permettere il ripristino globale dei muri ad ogni avvio dell'applicazione.

        string jsonOutput = JsonUtility.ToJson(data, true);
        File.WriteAllText(saveFilePath, jsonOutput);

        Debug.Log($"[SISTEMA] Dati serializzati e salvati in: {saveFilePath}");
    }

    public void LoadGameState()
    {
        // 1. Inizializziamo SEMPRE il registro causale come vuoto ad ogni avvio (I muri riappaiono)
        registroCausale = new HashSet<string>();
        varchiApertiPerRitorno = new HashSet<string>();

        if (File.Exists(saveFilePath))
        {
            string jsonInput = File.ReadAllText(saveFilePath);
            SaveDataWrapper data = JsonUtility.FromJson<SaveDataWrapper>(jsonInput);

            currentTagID = data.savedTagID ?? "TAG_001"; // Fallback di sicurezza

            // Ripristino delle collezioni persistenti
            tagTemporaliAcquisiti = data.tagTemporaliAcquisiti != null 
                ? new HashSet<string>(data.tagTemporaliAcquisiti) 
                : new HashSet<string>();
                
            anacronismiRisolti = data.anacronismiRisolti != null 
                ? new HashSet<string>(data.anacronismiRisolti) 
                : new HashSet<string>();
                
            storicoTagSbloccati = data.tagSbloccatiStorico != null 
                ? new HashSet<string>(data.tagSbloccatiStorico) 
                : new HashSet<string>();

            Debug.Log("[SISTEMA] Stato del mondo ripristinato dal file JSON. Registro degli ostacoli pulito (tutti integri).");
        }
        else
        {
            // Inizializzazione pulita totale in caso di assenza di file precedenti
            tagTemporaliAcquisiti = new HashSet<string>();
            anacronismiRisolti = new HashSet<string>();
            storicoTagSbloccati = new HashSet<string>();
            Debug.Log("[SISTEMA] Nessun file di salvataggio rilevato. Avvio di una nuova linea temporale.");
        }
    }

    public void ResumeSavedGame()
    {
        LoadGameState();
        SceneManager.LoadScene(DefaultGameplaySceneName);
    }

    // --- STRUMENTI DI DEBUG E CALIBRAZIONE ---
    [ContextMenu("RESETTA DATI DEBUG (CANCELLA JSON)")]
    public void ResetDatiDebug()
    {
        // 1. Epurazione della memoria volatile a runtime (RAM)
        registroCausale.Clear();
        tagTemporaliAcquisiti.Clear();
        anacronismiRisolti.Clear();
        storicoTagSbloccati.Clear();
        varchiApertiPerRitorno.Clear();
        currentTagID = "TAG_001";

        // 2. Rimozione fisica del file JSON di salvataggio
        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
            Debug.Log("[DEBUG] File JSON eliminato dal disco.");
        }
        else
        {
            Debug.LogWarning("[DEBUG] Nessun file JSON presente sul disco.");
        }

        // 3. Ricaricamento della scena attiva per rigenerare i GameObject
        Scene scenaAttiva = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scenaAttiva.name);
    }
}