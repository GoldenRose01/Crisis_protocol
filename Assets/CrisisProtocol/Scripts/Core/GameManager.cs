// ============================================================================
// Crisis Protocol / Sector Containment - Core runtime
// File: .\Assets\CrisisProtocol\Scripts\Core\GameManager.cs
// Responsabilita': coordina stato globale, salvataggi, avanzamento partita o servizi persistenti condivisi tra scene.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine; // usa lib // riga-ok
using System; // usa lib // riga-ok
using System.Collections.Generic; // usa lib // riga-ok
using System.IO; // usa lib // riga-ok
using UnityEngine.SceneManagement; // usa lib // riga-ok

/// <summary>
/// Struttura serializzabile scritta su disco in JSON.
/// Contiene sia dati di progressione generale (score, settore, missioni completate)
/// sia dati di sicurezza usati dallo scanner e dai sistemi di accesso.
/// </summary>
[System.Serializable] // nota unity // riga-ok
// blocco: classe x roba grossa
public class SaveDataWrapper // classe qui // riga-ok
{ // apre // riga-ok
    public int punteggioTotale; // roba pub // riga-ok
    public int missioniCompletate; // roba pub // riga-ok
    public List<string> missioniCompletateIds = new List<string>(); // roba pub // riga-ok

    public string savedCredentialId; // roba pub // riga-ok
    public List<string> securitySignaturesAcquired = new List<string>(); // roba pub // riga-ok
    public List<string> incidentsResolved = new List<string>(); // roba pub // riga-ok
    public List<string> unlockedSecurityHistory = new List<string>(); // roba pub // riga-ok

    /// <summary>Indice dell'ultimo settore raggiunto (0 = Settore 0).</summary>
    public int lastSectorIndex = 0; // roba pub // riga-ok
} // chiude // riga-ok

/// <summary>
/// Singleton persistente dell'intera partita.
/// Mantiene lo stato che deve sopravvivere ai cambi scena: progressione dei settori,
/// punteggio totale, credenziali/firme di sicurezza, incidenti risolti e salvataggio JSON.
/// </summary>
// blocco: classe x roba grossa
public class GameManager : MonoBehaviour // classe qui // riga-ok
{ // apre // riga-ok
    public static GameManager Instance { get; private set; } // roba pub // riga-ok

    [Header("Configurazione Scene")] // nota unity // riga-ok
    [Tooltip("Lista ordinata dei settori del gioco. Devono corrispondere esattamente ai nomi in Build Settings.")] // nota unity // riga-ok
    [SerializeField] private List<string> livelliInOrdine = new List<string> { "settore 0", "settore 1", "settore 2" }; // setta // riga-ok

    /// <summary>Indice del settore attualmente caricato (0-based).</summary>
    private int indiceSettoreCorrente = 0; // roba pub // riga-ok

    /// <summary>Nome della scena del menu principale.</summary>
    [SerializeField] private string scenaMainMenu = "MainMenu-Scene"; // setta // riga-ok

    private const string SaveFileName = "SectorContainment_Save.json"; // roba pub // riga-ok

    public static event Action<int> OnPunteggioAggiornato; // roba pub // riga-ok
    public static event Action<string> OnMissioneCompletata; // roba pub // riga-ok

    public static event Action<string, int> OnSecuritySignatureAcquired; // roba pub // riga-ok
    public static event Action<string, int> OnIncidentResolved; // roba pub // riga-ok

    private int punteggioTotale = 0; // roba pub // riga-ok
    private HashSet<string> missioniCompletate = new HashSet<string>(); // roba pub // riga-ok

    [Header("Stato Operatore")] // nota unity // riga-ok
    public string currentCredentialID = ""; // roba pub // riga-ok

    private readonly HashSet<string> volatileContainmentState = new HashSet<string>(); // roba pub // riga-ok
    private HashSet<string> securitySignaturesAcquired = new HashSet<string>(); // roba pub // riga-ok
    private HashSet<string> incidentsResolved = new HashSet<string>(); // roba pub // riga-ok
    private HashSet<string> unlockedSecurityHistory = new HashSet<string>(); // roba pub // riga-ok
    private HashSet<string> authorizedReturnChannels = new HashSet<string>(); // roba pub // riga-ok

    private string saveFilePath; // roba pub // riga-ok

    public string CurrentSecuritySignatureId => currentCredentialID; // roba pub // riga-ok
    public int AcquiredSecuritySignatureCount => securitySignaturesAcquired.Count; // roba pub // riga-ok
    public int ResolvedIncidentCount => incidentsResolved.Count; // roba pub // riga-ok

    public int PunteggioTotale => punteggioTotale; // roba pub // riga-ok
    public int MissioniCompletateCount => missioniCompletate.Count; // roba pub // riga-ok

    // blocco: funzione fa cose
    private void Awake() // roba pub // riga-ok
    { // apre // riga-ok
        // Enforce del singleton: deve esistere un solo GameManager persistente,
        // altrimenti eventi e salvataggi verrebbero duplicati tra scene.
        // blocco: controlla se va
        if (Instance != null && Instance != this) // se ok // riga-ok
        { // apre // riga-ok
            Destroy(gameObject); // elimina // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        Instance = this; // setta // riga-ok
        DontDestroyOnLoad(gameObject); // chiama // riga-ok

        saveFilePath = Path.Combine(Application.persistentDataPath, SaveFileName); // setta // riga-ok
        LoadGameState(); // chiama // riga-ok

        // Sincronizza l'indice con la scena attualmente aperta. Questo permette
        // di avviare Play Mode direttamente da un settore senza rompere la progressione.
        string nomeScenaAttuale = SceneManager.GetActiveScene().name; // setta // riga-ok
        // blocco: controlla se va
        if (livelliInOrdine != null && livelliInOrdine.Contains(nomeScenaAttuale)) // se ok // riga-ok
        { // apre // riga-ok
            indiceSettoreCorrente = livelliInOrdine.IndexOf(nomeScenaAttuale); // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void Update() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (Input.GetKeyDown(KeyCode.F5)) // se ok // riga-ok
        { // apre // riga-ok
            Debug.LogWarning("[DEBUG] F5 premuto. Reset completo dei dati di Sector Containment."); // logga // riga-ok
            ResetDatiDebug(); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void AggiornaPunteggio(int punti) // roba pub // riga-ok
    { // apre // riga-ok
        // Il punteggio globale viene salvato subito per non perdere progressi
        // quando una scena di settore viene completata o ricaricata.
        punteggioTotale += punti; // setta // riga-ok
        SaveGameState(); // chiama // riga-ok
        OnPunteggioAggiornato?.Invoke(punteggioTotale); // chiama // riga-ok
        Debug.Log($"[GAMEMANAGER] Punteggio aggiornato: +{punti} → totale {punteggioTotale}"); // logga // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public bool RegistraMissioneCompletata(string missioneId) // roba pub // riga-ok
    { // apre // riga-ok
        // HashSet evita doppi conteggi se un hotspot o una missione notificano
        // piu' volte lo stesso ID durante reload, debug o interazioni ripetute.
        // blocco: controlla se va
        if (string.IsNullOrWhiteSpace(missioneId)) return false; // se ok // riga-ok
        // blocco: controlla se va
        if (!missioniCompletate.Add(missioneId)) return false; // se ok // riga-ok

        SaveGameState(); // chiama // riga-ok
        OnMissioneCompletata?.Invoke(missioneId); // chiama // riga-ok
        Debug.Log($"[GAMEMANAGER] Missione completata registrata: {missioneId}"); // logga // riga-ok
        return true; // torna val // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public bool IsMissioneCompletata(string missioneId) => missioniCompletate.Contains(missioneId); // roba pub // riga-ok

    // blocco: funzione fa cose
    public bool IsHotspotResolved(string id) => volatileContainmentState.Contains(id); // roba pub // riga-ok

    // Ponte di compatibilita' per OstacoloCausale: lo script puo' continuare
    // a chiamare una semantica "causale", ma il dato viene trattato come stato
    // volatile di contenimento/risoluzione del settore.
    // blocco: funzione fa cose
    public bool GetCausalState(string id) => IsHotspotResolved(id); // roba pub // riga-ok
    // blocco: funzione fa cose
    public void SetCausalState(string id, bool state) => SetHotspotResolved(id, state); // roba pub // riga-ok

    // blocco: funzione fa cose
    public void SetHotspotResolved(string id, bool state) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (string.IsNullOrWhiteSpace(id)) return; // se ok // riga-ok
        // blocco: controlla se va
        if (state) volatileContainmentState.Add(id); else volatileContainmentState.Remove(id); // se ok // riga-ok
        SaveGameState(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void RegisterSecuritySignature(string signatureId) // roba pub // riga-ok
    { // apre // riga-ok
        // Una firma di sicurezza rappresenta keycard, frequenza, autorizzazione
        // o dato scanner acquisito. Viene registrata anche nel MissionManager
        // per sbloccare hotspot/porte collegati alla credenziale.
        // blocco: controlla se va
        if (string.IsNullOrWhiteSpace(signatureId)) return; // se ok // riga-ok

        currentCredentialID = signatureId; // setta // riga-ok
        bool isNewSignature = securitySignaturesAcquired.Add(signatureId); // setta // riga-ok

        // blocco: controlla se va
        if (unlockedSecurityHistory.Add(signatureId)) // se ok // riga-ok
            Debug.Log($"<color=lime>[SECURITY]</color> Firma <b>{signatureId}</b> archiviata nello storico autorizzazioni."); // logga // riga-ok

        // blocco: controlla se va
        if (MissionManager.Instance != null) // se ok // riga-ok
            MissionManager.Instance.RegistraCredenziale(signatureId); // chiama // riga-ok

        SaveGameState(); // chiama // riga-ok

        // blocco: controlla se va
        if (isNewSignature) // se ok // riga-ok
            OnSecuritySignatureAcquired?.Invoke(signatureId, securitySignaturesAcquired.Count); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public bool ResolveIncident(string incidentId) // roba pub // riga-ok
    { // apre // riga-ok
        // Gli incidenti risolti vengono persistiti per distinguere contenimenti
        // gia' completati da problemi ancora attivi dopo cambi scena o reload.
        // blocco: controlla se va
        if (string.IsNullOrWhiteSpace(incidentId)) return false; // se ok // riga-ok
        // blocco: controlla se va
        if (!incidentsResolved.Add(incidentId)) return false; // se ok // riga-ok

        SaveGameState(); // chiama // riga-ok
        OnIncidentResolved?.Invoke(incidentId, incidentsResolved.Count); // chiama // riga-ok
        Debug.Log($"[SECTOR] Incidente risolto: {incidentId}"); // logga // riga-ok
        return true; // torna val // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public bool IsSecuritySignatureUnlocked(string signatureId) => string.IsNullOrWhiteSpace(signatureId) || unlockedSecurityHistory.Contains(signatureId); // roba pub // riga-ok

    // blocco: funzione fa cose
    public void AuthorizeReturnChannel(string signatureId) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (string.IsNullOrWhiteSpace(signatureId)) return; // se ok // riga-ok
        // blocco: controlla se va
        if (authorizedReturnChannels.Add(signatureId)) // se ok // riga-ok
            Debug.Log($"<color=cyan>[SECURITY]</color> Canale operativo autorizzato per firma <b>{signatureId}</b>."); // logga // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public bool ConsumeReturnChannel(string signatureId) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (authorizedReturnChannels.Remove(signatureId)) // se ok // riga-ok
        { // apre // riga-ok
            Debug.Log($"<color=orange>[SECURITY]</color> Canale operativo consumato: <b>{signatureId}</b>."); // logga // riga-ok
            return true; // torna val // riga-ok
        } // chiude // riga-ok
        return false; // torna val // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void SaveGameState() // roba pub // riga-ok
    { // apre // riga-ok
        // JsonUtility richiede un oggetto wrapper concreto: copiamo gli HashSet
        // in liste serializzabili e lasciamo fuori solo lo stato volutamente volatile.
        SaveDataWrapper data = new SaveDataWrapper // setta // riga-ok
        { // apre // riga-ok
            punteggioTotale = this.punteggioTotale, // setta // riga-ok
            missioniCompletate = this.missioniCompletate.Count, // setta // riga-ok
            missioniCompletateIds = new List<string>(this.missioniCompletate), // setta // riga-ok
            savedCredentialId = currentCredentialID, // setta // riga-ok
            securitySignaturesAcquired = new List<string>(this.securitySignaturesAcquired), // setta // riga-ok
            incidentsResolved = new List<string>(this.incidentsResolved), // setta // riga-ok
            unlockedSecurityHistory = new List<string>(this.unlockedSecurityHistory), // setta // riga-ok
            lastSectorIndex = this.indiceSettoreCorrente // setta // riga-ok
        }; // ok qua // riga-ok

        File.WriteAllText(saveFilePath, JsonUtility.ToJson(data, true)); // chiama // riga-ok
        Debug.Log($"[SISTEMA] Stato salvato in: {saveFilePath}"); // logga // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void LoadGameState() // roba pub // riga-ok
    { // apre // riga-ok
        // Le collezioni volatili vengono azzerate a ogni boot: rappresentano
        // effetti di scena/sessione e non progressione permanente.
        volatileContainmentState.Clear(); // chiama // riga-ok
        authorizedReturnChannels = new HashSet<string>(); // setta // riga-ok

        // blocco: controlla se va
        if (File.Exists(saveFilePath)) // se ok // riga-ok
        { // apre // riga-ok
            SaveDataWrapper data = JsonUtility.FromJson<SaveDataWrapper>(File.ReadAllText(saveFilePath)); // setta // riga-ok
            currentCredentialID = string.IsNullOrEmpty(data.savedCredentialId) ? "KEYCARD_A01" : data.savedCredentialId; // setta // riga-ok
            securitySignaturesAcquired = new HashSet<string>(data.securitySignaturesAcquired); // setta // riga-ok
            incidentsResolved = new HashSet<string>(data.incidentsResolved); // setta // riga-ok
            unlockedSecurityHistory = new HashSet<string>(data.unlockedSecurityHistory); // setta // riga-ok
            punteggioTotale = data.punteggioTotale; // setta // riga-ok
            missioniCompletate = new HashSet<string>(data.missioniCompletateIds); // setta // riga-ok
            indiceSettoreCorrente = Mathf.Clamp(data.lastSectorIndex, 0, Mathf.Max(0, livelliInOrdine.Count - 1)); // setta // riga-ok
            Debug.Log($"[SISTEMA] Stato ripristinato. Ultimo settore: {indiceSettoreCorrente}."); // logga // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        securitySignaturesAcquired = new HashSet<string>(); // setta // riga-ok
        incidentsResolved = new HashSet<string>(); // setta // riga-ok
        unlockedSecurityHistory = new HashSet<string>(); // setta // riga-ok
        currentCredentialID = "KEYCARD_A01"; // setta // riga-ok
        punteggioTotale = 0; // setta // riga-ok
        indiceSettoreCorrente = 0; // setta // riga-ok
        missioniCompletate = new HashSet<string>(); // setta // riga-ok
        Debug.Log("[SISTEMA] Nessun salvataggio rilevato. Avvio nuova emergenza/partita."); // logga // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void ResumeSavedGame() // roba pub // riga-ok
    { // apre // riga-ok
        LoadGameState(); // chiama // riga-ok
        CaricaSettore(indiceSettoreCorrente); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void NuovaPartita() // roba pub // riga-ok
    { // apre // riga-ok
        // Reset completo della progressione persistente prima del briefing.
        // Il briefing decide poi quando caricare il primo settore giocabile.
        punteggioTotale = 0; // setta // riga-ok
        indiceSettoreCorrente = 0; // setta // riga-ok
        missioniCompletate.Clear(); // chiama // riga-ok
        securitySignaturesAcquired.Clear(); // chiama // riga-ok
        incidentsResolved.Clear(); // chiama // riga-ok
        unlockedSecurityHistory.Clear(); // chiama // riga-ok
        currentCredentialID = "KEYCARD_A01"; // setta // riga-ok
        SaveGameState(); // chiama // riga-ok
        
        // Avvia il briefing invece di caricare direttamente la scena
        StoryBriefingController.ShowBriefingAndLoadGame(); // chiama // riga-ok
    } // chiude // riga-ok

    /// <summary>
    /// Restituisce true se il settore corrente è l'ultimo della campagna (Settore 2).
    /// </summary>
    public bool IsUltimoSettore => indiceSettoreCorrente >= (livelliInOrdine != null && livelliInOrdine.Count > 0 ? livelliInOrdine.Count - 1 : 2); // roba pub // riga-ok

    /// <summary>
    /// Carica il settore per indice. Se l'indice supera la lista, mostra i titoli di coda e fine gioco.
    /// </summary>
    // blocco: funzione fa cose
    public void CaricaSettore(int indice) // roba pub // riga-ok
    { // apre // riga-ok
        // Centralizza il caricamento dei settori: tutte le uscite/vittorie passano
        // da qui, cosi' indice e salvataggio restano coerenti con la scena caricata.
        // blocco: controlla se va
        if (livelliInOrdine == null || livelliInOrdine.Count == 0) // se ok // riga-ok
        { // apre // riga-ok
            Debug.LogError("[GAMEMANAGER] Lista livelli vuota. Aggiungila nell'Inspector."); // logga // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (indice >= livelliInOrdine.Count) // se ok // riga-ok
        { // apre // riga-ok
            Debug.Log("[GAMEMANAGER] Tutti i settori completati! Visualizzazione schermata di fine gioco e crediti."); // logga // riga-ok
            EndGameCreditsController.ShowVictoryAndCredits(punteggioTotale); // chiama // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        indiceSettoreCorrente = Mathf.Clamp(indice, 0, livelliInOrdine.Count - 1); // setta // riga-ok
        string nomeScena = livelliInOrdine[indiceSettoreCorrente]; // setta // riga-ok
        SaveGameState(); // chiama // riga-ok
        Debug.Log($"[GAMEMANAGER] Carico settore {indiceSettoreCorrente}: '{nomeScena}'"); // logga // riga-ok
        SceneManager.LoadScene(nomeScena); // chiama // riga-ok
    } // chiude // riga-ok

    /// <summary>Avanza al settore successivo (chiamato da MissionManager su vittoria).</summary>
    // blocco: funzione fa cose
    public void CaricaProssimoSettore() // roba pub // riga-ok
    { // apre // riga-ok
        CaricaSettore(indiceSettoreCorrente + 1); // chiama // riga-ok
    } // chiude // riga-ok

    /// <summary>Ricarica il settore corrente (chiamato su sconfitta).</summary>
    // blocco: funzione fa cose
    public void CaricaSettoreCorrente() // roba pub // riga-ok
    { // apre // riga-ok
        CaricaSettore(indiceSettoreCorrente); // chiama // riga-ok
    } // chiude // riga-ok

    /// <summary>Indice del settore attualmente attivo (0-based).</summary>
    public int IndiceSettoreCorrente => indiceSettoreCorrente; // roba pub // riga-ok

    [ContextMenu("RESETTA DATI DEBUG (CANCELLA JSON)")] // nota unity // riga-ok
    // blocco: funzione fa cose
    public void ResetDatiDebug() // roba pub // riga-ok
    { // apre // riga-ok
        // Reset pensato per testing rapido in editor: pulisce runtime + salvataggio
        // e ricarica la scena corrente senza richiedere riavvio di Unity.
        volatileContainmentState.Clear(); // chiama // riga-ok
        securitySignaturesAcquired.Clear(); // chiama // riga-ok
        incidentsResolved.Clear(); // chiama // riga-ok
        unlockedSecurityHistory.Clear(); // chiama // riga-ok
        authorizedReturnChannels.Clear(); // chiama // riga-ok
        currentCredentialID = "KEYCARD_A01"; // setta // riga-ok

        // blocco: controlla se va
        if (File.Exists(saveFilePath)) File.Delete(saveFilePath); // se ok // riga-ok
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // chiama // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
