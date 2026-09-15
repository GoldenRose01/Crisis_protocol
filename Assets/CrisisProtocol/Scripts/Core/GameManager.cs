// ============================================================================
// Crisis Protocol / Sector Containment - Core runtime
// File: .\Assets\CrisisProtocol\Scripts\Core\GameManager.cs
// Responsabilita': coordina stato globale, salvataggi, avanzamento partita o servizi persistenti condivisi tra scene.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;
/// <summary>
/// Struttura serializzabile scritta su disco in JSON.
/// Contiene sia dati di progressione generale (score, settore, missioni completate)
/// sia dati di sicurezza usati dallo scanner e dai sistemi di accesso.
/// </summary>
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
/// Singleton persistente dell'intera partita.
/// Mantiene lo stato che deve sopravvivere ai cambi scena: progressione dei settori,
/// punteggio totale, credenziali/firme di sicurezza, incidenti risolti e salvataggio JSON.
/// </summary>
public class GameManager : MonoBehaviour
{
    // Persistente tra scene: tiene campagna, save e autorizzazioni globali.
    // Non deve gestire dettagli minuto-per-minuto del settore: quelli sono del MissionManager.
    public static GameManager Instance { get; private set; }
    [Header("Configurazione Scene")]
    [Tooltip("Lista ordinata dei settori del gioco. Devono corrispondere esattamente ai nomi in Build Settings.")]
    [SerializeField] private List<string> livelliInOrdine = new List<string> { "settore 0", "settore 1", "settore 2" };
    /// <summary>Indice del settore attualmente caricato (0-based).</summary>
    private int indiceSettoreCorrente = 0;
    /// <summary>Nome della scena del menu principale.</summary>
    [SerializeField] private string scenaMainMenu = "MainMenu-Scene";
    private const string SaveFileName = "SectorContainment_Save.json";

    // Eventi globali, stile centralina: UI e altri sistemi ascoltano qui invece
    // di cercare GameObject specifici in scena.
    public static event Action<int> OnPunteggioAggiornato;
    public static event Action<string> OnMissioneCompletata;
    public static event Action<string, int> OnSecuritySignatureAcquired;
    public static event Action<string, int> OnIncidentResolved;
    private int punteggioTotale = 0;
    private HashSet<string> missioniCompletate = new HashSet<string>();
    [Header("Stato Operatore")]
    public string currentCredentialID = "";

    // Stato di run/settore: utile per ostacoli e porte nella sessione corrente,
    // ma volutamente pulito al caricamento per non inquinare una nuova partita.
    private readonly HashSet<string> volatileContainmentState = new HashSet<string>();

    // Stato persistente: HashSet in runtime per evitare doppioni, List nel wrapper
    // JSON perché JsonUtility non serializza bene gli HashSet.
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
        // Enforce del singleton: deve esistere un solo GameManager persistente,
        // altrimenti eventi e salvataggi verrebbero duplicati tra scene.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        saveFilePath = Path.Combine(Application.persistentDataPath, SaveFileName);
        LoadGameState();
        // Sincronizza l'indice con la scena attualmente aperta. Questo permette
        // di avviare Play Mode direttamente da un settore senza rompere la progressione.
        string nomeScenaAttuale = SceneManager.GetActiveScene().name;
        if (livelliInOrdine != null && livelliInOrdine.Contains(nomeScenaAttuale))
        {
            indiceSettoreCorrente = livelliInOrdine.IndexOf(nomeScenaAttuale);
        }
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
        // Il punteggio globale viene salvato subito per non perdere progressi
        // quando una scena di settore viene completata o ricaricata.
        punteggioTotale += punti;
        SaveGameState();
        OnPunteggioAggiornato?.Invoke(punteggioTotale);
        Debug.Log($"[GAMEMANAGER] Punteggio aggiornato: +{punti} → totale {punteggioTotale}");
    }
    public bool RegistraMissioneCompletata(string missioneId)
    {
        // HashSet evita doppi conteggi se un hotspot o una missione notificano
        // piu' volte lo stesso ID durante reload, debug o interazioni ripetute.
        if (string.IsNullOrWhiteSpace(missioneId)) return false;
        if (!missioniCompletate.Add(missioneId)) return false;
        SaveGameState();
        OnMissioneCompletata?.Invoke(missioneId);
        Debug.Log($"[GAMEMANAGER] Missione completata registrata: {missioneId}");
        return true;
    }
    public bool IsMissioneCompletata(string missioneId) => missioniCompletate.Contains(missioneId);
    public bool IsHotspotResolved(string id) => volatileContainmentState.Contains(id);
    // Ponte di compatibilita' per OstacoloCausale: lo script puo' continuare
    // a chiamare una semantica "causale", ma il dato viene trattato come stato
    // volatile di contenimento/risoluzione del settore.
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
        // Una firma di sicurezza rappresenta keycard, frequenza, autorizzazione
        // o dato scanner acquisito. Viene registrata anche nel MissionManager
        // per sbloccare hotspot/porte collegati alla credenziale.
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
        // Gli incidenti risolti vengono persistiti per distinguere contenimenti
        // gia' completati da problemi ancora attivi dopo cambi scena o reload.
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
        // Permesso temporaneo one-shot: utile per rientri controllati senza
        // trasformare quella firma in una chiave permanente.
        if (string.IsNullOrWhiteSpace(signatureId)) return;
        if (authorizedReturnChannels.Add(signatureId))
            Debug.Log($"<color=cyan>[SECURITY]</color> Canale operativo autorizzato per firma <b>{signatureId}</b>.");
    }
    public bool ConsumeReturnChannel(string signatureId)
    {
        // Consume = usa e cancella. Se restasse attivo sarebbe un bypass infinito,
        // quindi lo trattiamo come un ticket consumabile.
        if (authorizedReturnChannels.Remove(signatureId))
        {
            Debug.Log($"<color=orange>[SECURITY]</color> Canale operativo consumato: <b>{signatureId}</b>.");
            return true;
        }
        return false;
    }
    public void SaveGameState()
    {
        // JsonUtility richiede un oggetto wrapper concreto: copiamo gli HashSet
        // in liste serializzabili e lasciamo fuori solo lo stato volutamente volatile.
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
        // Le collezioni volatili vengono azzerate a ogni boot: rappresentano
        // effetti di scena/sessione e non progressione permanente.
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
        // Rilegge il JSON prima del load: se il save è cambiato mentre eri nel
        // menu, riparti sempre dall'ultimo stato davvero scritto su disco.
        LoadGameState();
        CaricaSettore(indiceSettoreCorrente);
    }
    public void NuovaPartita()
    {
        // Reset completo della progressione persistente prima del briefing.
        // Il briefing decide poi quando caricare il primo settore giocabile.
        punteggioTotale = 0;
        indiceSettoreCorrente = 0;
        missioniCompletate.Clear();
        securitySignaturesAcquired.Clear();
        incidentsResolved.Clear();
        unlockedSecurityHistory.Clear();
        currentCredentialID = "KEYCARD_A01";
        SaveGameState();
        // Avvia il briefing invece di caricare direttamente la scena
        StoryBriefingController.ShowBriefingAndLoadGame();
    }
    /// <summary>
    /// Restituisce true se il settore corrente è l'ultimo della campagna (Settore 2).
    /// </summary>
    public bool IsUltimoSettore => indiceSettoreCorrente >= (livelliInOrdine != null && livelliInOrdine.Count > 0 ? livelliInOrdine.Count - 1 : 2);
    /// <summary>
    /// Carica il settore per indice. Se l'indice supera la lista, mostra i titoli di coda e fine gioco.
    /// </summary>
    public void CaricaSettore(int indice)
    {
        // Centralizza il caricamento dei settori: tutte le uscite/vittorie passano
        // da qui, cosi' indice e salvataggio restano coerenti con la scena caricata.
        if (livelliInOrdine == null || livelliInOrdine.Count == 0)
        {
            Debug.LogError("[GAMEMANAGER] Lista livelli vuota. Aggiungila nell'Inspector.");
            return;
        }
        if (indice >= livelliInOrdine.Count)
        {
            Debug.Log("[GAMEMANAGER] Tutti i settori completati! Visualizzazione schermata di fine gioco e crediti.");
            EndGameCreditsController.ShowVictoryAndCredits(punteggioTotale);
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
        // Avanzamento lineare campagna: tutte le vittorie usano questa strada,
        // così indice settore e salvataggio non si sfasano.
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
        // Reset pensato per testing rapido in editor: pulisce runtime + salvataggio
        // e ricarica la scena corrente senza richiedere riavvio di Unity.
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
