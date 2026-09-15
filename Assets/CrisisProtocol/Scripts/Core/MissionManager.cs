// ============================================================================
// Crisis Protocol / Sector Containment - Core runtime
// File: .\Assets\CrisisProtocol\Scripts\Core\MissionManager.cs
// Responsabilita': coordina stato globale, salvataggi, avanzamento partita o servizi persistenti condivisi tra scene.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using System; // usa lib // riga-ok
using System.Collections; // usa lib // riga-ok
using System.Collections.Generic; // usa lib // riga-ok
using UnityEngine; // usa lib // riga-ok
using UnityEngine.SceneManagement; // usa lib // riga-ok

/// <summary>
/// Controller runtime della missione corrente.
/// Tiene insieme obiettivi, credenziali, focolai, collasso strutturale,
/// scoring e condizioni di vittoria/sconfitta per il settore attivo.
/// </summary>
// blocco: classe x roba grossa
public class MissionManager : MonoBehaviour // classe qui // riga-ok
{ // apre // riga-ok
    // blocco: scelte rapide
    public enum MissionOutcome { Victory, Defeat } // enum val // riga-ok

    public static MissionManager Instance { get; private set; } // roba pub // riga-ok

    [Header("Obiettivi Missione")] // nota unity // riga-ok
    [SerializeField] private SectorObjectiveSettings obiettivi = new SectorObjectiveSettings(); // setta // riga-ok

    [Header("Timer Missione & Sconfitta (Countdown)")] // nota unity // riga-ok
    [Tooltip("Se abilitato, impone un tempo limite di evacuazione con countdown a schermo.")] // nota unity // riga-ok
    [SerializeField] private bool usaTempoLimite = true; // setta // riga-ok
    [Tooltip("Durata del countdown in MINUTI (es. 5 = 5:00, 2.5 = 2:30, 10 = 10:00).")] // nota unity // riga-ok
    [SerializeField] [Range(0.5f, 60f)] private float tempoLimiteMinuti = 5.0f; // setta // riga-ok
    [Tooltip("Se abilitato, allo scadere del tempo (00:00.0) scatta immediatamente il Game Over (Sconfitta).")] // nota unity // riga-ok
    [SerializeField] private bool sconfittaAScadenzaTimer = true; // setta // riga-ok

    [Header("Collasso Strutturale")] // nota unity // riga-ok
    [SerializeField] private StructuralCollapseSettings collassoStrutturale = new StructuralCollapseSettings(); // setta // riga-ok

    [Header("Punteggio")] // nota unity // riga-ok
    [SerializeField] private SectorScoreSettings scoreSettings = new SectorScoreSettings(); // setta // riga-ok

    private readonly HashSet<string> credenzialiRaccolte = new HashSet<string>(); // roba pub // riga-ok
    private readonly HashSet<string> focolaiContenuti = new HashSet<string>(); // roba pub // riga-ok

    private float collassoCorrente; // roba pub // riga-ok
    private float tempoMissione; // roba pub // riga-ok
    private int allarmiSubiti; // roba pub // riga-ok
    private int credenzialiErrate; // roba pub // riga-ok
    private int danniSubitiArrotondati; // roba pub // riga-ok
    private int punteggioBase; // roba pub // riga-ok
    private bool estrazioneSbloccata; // roba pub // riga-ok
    private bool bloccaSbloccoEstrazioneDebug; // blocca debug // riga-ok
    private bool missioneTerminata; // roba pub // riga-ok

    public static event Action<float, float> OnDestabilizzazioneCambiata; // roba pub // riga-ok
    public static event Action<float, float> OnCollassoStrutturaleCambiato; // roba pub // riga-ok
    public static event Action<int, int> OnFocolaiCambiati; // roba pub // riga-ok
    public static event Action<int> OnFrequenzeCambiate; // roba pub // riga-ok
    public static event Action<int> OnCredenzialiCambiate; // roba pub // riga-ok
    public static event Action<string, int> OnNuovaCredenzialeRaccolta; // roba pub // riga-ok
    public static event Action<int> OnPunteggioCambiato; // roba pub // riga-ok
    public static event Action<bool> OnEstrazioneSbloccata; // roba pub // riga-ok
    public static event Action<MissionOutcome, int, string> OnMissioneTerminata; // roba pub // riga-ok

    public float DestabilizzazioneCorrente => collassoCorrente; // roba pub // riga-ok
    public float CollassoCorrente => collassoCorrente; // roba pub // riga-ok
    public int FrequenzeRaccolte => credenzialiRaccolte.Count; // roba pub // riga-ok
    public int CredenzialiRaccolte => credenzialiRaccolte.Count; // roba pub // riga-ok
    public int FocolaiStabilizzati => focolaiContenuti.Count; // roba pub // riga-ok
    public int FocolaiContenuti => focolaiContenuti.Count; // roba pub // riga-ok
    public int TotaleFocolai => obiettivi.TotaleFocolaiDaContenere; // roba pub // riga-ok
    public bool EstrazioneSbloccata => estrazioneSbloccata; // roba pub // riga-ok
    public bool MissioneTerminata => missioneTerminata; // roba pub // riga-ok
    public bool UsaTempoLimite => usaTempoLimite; // roba pub // riga-ok
    public float TempoLimiteMinuti => tempoLimiteMinuti; // roba pub // riga-ok
    public float TempoLimiteSecondi => tempoLimiteMinuti * 60f; // roba pub // riga-ok
    public bool SconfittaAScadenzaTimer => sconfittaAScadenzaTimer; // roba pub // riga-ok

    // blocco: funzione fa cose
    private void Awake() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (Instance != null && Instance != this) // se ok // riga-ok
        { // apre // riga-ok
            Destroy(gameObject); // elimina // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        Instance = this; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnEnable() // roba pub // riga-ok
    { // apre // riga-ok
        SalutePlayer.OnPlayerMorto += GestisciMortePlayer; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnDisable() // roba pub // riga-ok
    { // apre // riga-ok
        SalutePlayer.OnPlayerMorto -= GestisciMortePlayer; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void Start() // roba pub // riga-ok
    { // apre // riga-ok
        // Calcola gli obiettivi partendo dagli hotspot effettivamente presenti
        // nella scena, cosi' il designer puo' modificare il livello senza toccare codice.
        obiettivi.InitializeFromScene(); // chiama // riga-ok

        collassoCorrente = collassoStrutturale.Clamp(collassoStrutturale.CollassoIniziale); // setta // riga-ok
        NotificaStatoMissione(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void Update() // roba pub // riga-ok
    { // apre // riga-ok
        // Hotkey di debug rapido (DISABILITATO)
        // blocco: controlla se va
        if (false && Input.GetKeyDown(KeyCode.F4)) // se ok // riga-ok
        { // apre // riga-ok
            Debug.LogWarning("[DEBUG] Tasto F4 premuto: Risoluzione emergenza e completamento totale task."); // logga // riga-ok
            RisolviStatoEmergenzaETuttiTaskDebug(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (Input.GetKeyDown(KeyCode.F6)) // se ok // riga-ok
        { // apre // riga-ok
            Debug.LogWarning("[DEBUG] Tasto F6 premuto: Sblocco immediato estrazione/portellone."); // logga // riga-ok
            ForzaSbloccoEstrazioneDebug(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (Input.GetKeyDown(KeyCode.F7)) // se ok // riga-ok
        { // apre // riga-ok
            Debug.LogWarning("[DEBUG] Tasto F7 premuto: Vittoria forzata e passaggio al prossimo livello."); // logga // riga-ok
            ForzaCompletamentoMissioneDebug(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (missioneTerminata) // se ok // riga-ok
            return; // torna val // riga-ok

        // Il collasso cresce continuamente finche' la missione e' attiva.
        // Eventi, danni e allarmi possono aumentarlo; contenimenti riusciti possono ridurlo.
        tempoMissione += Time.deltaTime; // setta // riga-ok
        AggiungiCollasso(collassoStrutturale.IncrementoPerSecondo * Time.deltaTime); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public bool RegistraCredenziale(string credentialId) // roba pub // riga-ok
    { // apre // riga-ok
        // Le credenziali sono risorse di accesso: vengono contate per UI/punteggio
        // e abilitate per hotspot che richiedono una keycard specifica.
        // blocco: controlla se va
        if (missioneTerminata || string.IsNullOrWhiteSpace(credentialId)) // se ok // riga-ok
            return false; // torna val // riga-ok

        bool nuovaCredenziale = credenzialiRaccolte.Add(credentialId); // setta // riga-ok
        // blocco: controlla se va
        if (!nuovaCredenziale) // se ok // riga-ok
            return false; // torna val // riga-ok

        punteggioBase += scoreSettings.PuntiPerCredenziale; // setta // riga-ok
        OnNuovaCredenzialeRaccolta?.Invoke(credentialId, credenzialiRaccolte.Count); // chiama // riga-ok
        OnCredenzialiCambiate?.Invoke(credenzialiRaccolte.Count); // chiama // riga-ok
        OnFrequenzeCambiate?.Invoke(credenzialiRaccolte.Count); // chiama // riga-ok
        OnPunteggioCambiato?.Invoke(CalcolaPunteggioProvvisorio()); // chiama // riga-ok
        Debug.Log($"<color=cyan>[CREDENZIALE]</color> Autorizzazione acquisita: {credentialId}. Totale: {credenzialiRaccolte.Count}"); // logga // riga-ok
        return true; // torna val // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public bool RegistraFrequenza(string frequencyId) // roba pub // riga-ok
    { // apre // riga-ok
        return RegistraCredenziale(frequencyId); // torna val // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public bool PossiedeCredenziale(string credentialId) // roba pub // riga-ok
    { // apre // riga-ok
        return string.IsNullOrWhiteSpace(credentialId) || credenzialiRaccolte.Contains(credentialId); // torna val // riga-ok
    } // chiude // riga-ok

    /// <summary>
    /// Restituisce true solo se la credenziale specifica è stata fisicamente raccolta nella sessione corrente di missione.
    /// </summary>
    // blocco: funzione fa cose
    public bool HaRaccoltoCredenzialeSpecifica(string credentialId) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (string.IsNullOrWhiteSpace(credentialId)) return false; // se ok // riga-ok
        return credenzialiRaccolte.Contains(credentialId); // torna val // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public bool PossiedeFrequenza(string frequencyId) // roba pub // riga-ok
    { // apre // riga-ok
        return PossiedeCredenziale(frequencyId); // torna val // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public bool ContieniFocolaio(string hotspotId, string requiredCredentialId) // roba pub // riga-ok
    { // apre // riga-ok
        // Contenimento atomico del focolaio: verifica autorizzazione, assegna punti,
        // riduce collasso, notifica GameManager e aggiorna lo stato estrazione.
        // blocco: controlla se va
        if (missioneTerminata || string.IsNullOrWhiteSpace(hotspotId)) // se ok // riga-ok
            return false; // torna val // riga-ok

        // blocco: controlla se va
        if (!PossiedeCredenziale(requiredCredentialId)) // se ok // riga-ok
        { // apre // riga-ok
            RegistraCredenzialeErrata($"Credenziale richiesta non posseduta: {requiredCredentialId}"); // chiama // riga-ok
            return false; // torna val // riga-ok
        } // chiude // riga-ok

        bool nuovoFocolaio = focolaiContenuti.Add(hotspotId); // setta // riga-ok
        // blocco: controlla se va
        if (!nuovoFocolaio) // se ok // riga-ok
            return false; // torna val // riga-ok

        punteggioBase += scoreSettings.PuntiPerFocolaioContenuto; // setta // riga-ok
        AggiungiCollasso(-collassoStrutturale.RecuperoPerFocolaioContenuto); // chiama // riga-ok

        // blocco: controlla se va
        if (GameManager.Instance != null) // se ok // riga-ok
            GameManager.Instance.RegistraMissioneCompletata(hotspotId); // chiama // riga-ok

        AggiornaEstrazione(); // chiama // riga-ok
        OnFocolaiCambiati?.Invoke(focolaiContenuti.Count, TotaleFocolai); // chiama // riga-ok
        OnPunteggioCambiato?.Invoke(CalcolaPunteggioProvvisorio()); // chiama // riga-ok
        Debug.Log($"<color=lime>[CONTENIMENTO]</color> Focolaio contenuto: {hotspotId}. Progresso: {focolaiContenuti.Count}/{TotaleFocolai}"); // logga // riga-ok
        return true; // torna val // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public bool StabilizzaFocolaio(string fractureId, string requiredFrequencyId) // roba pub // riga-ok
    { // apre // riga-ok
        return ContieniFocolaio(fractureId, requiredFrequencyId); // torna val // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void TentaEstrazione() // roba pub // riga-ok
    { // apre // riga-ok
        // L'estrazione e' valida solo dopo aver soddisfatto gli obiettivi del settore.
        // La porta puo' chiamare questo metodo senza conoscere i dettagli della missione.
        // blocco: controlla se va
        if (missioneTerminata) // se ok // riga-ok
            return; // torna val // riga-ok

        // blocco: controlla se va
        if (!estrazioneSbloccata) // se ok // riga-ok
        { // apre // riga-ok
            Debug.LogWarning("[QUARANTENA] Portellone bloccato: contenere tutti i focolai d'emergenza."); // logga // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        TerminaMissione(MissionOutcome.Victory, "Estrazione in sicurezza completata prima del collasso della struttura."); // chiama // riga-ok
    } // chiude // riga-ok

    [ContextMenu("DEBUG: Risolvi Stato Emergenza e Completa Tutti i Task (F4)")] // nota unity // riga-ok
    // blocco: funzione fa cose
    public void RisolviStatoEmergenzaETuttiTaskDebug() // roba pub // riga-ok
    { // apre // riga-ok
        Debug.Log("<color=lime><b>[DEBUG] RISOLUZIONE TOTALE EMERGENZA AVVIATA...</b></color>"); // logga // riga-ok
        bloccaSbloccoEstrazioneDebug = true; // blocca porta // riga-ok

        try // prova // riga-ok
        { // apre // riga-ok
            // 1. Raccogli tutte le credenziali/keycard presenti nella scena
            AccessCredentialPickup[] pickups = UnityEngine.Object.FindObjectsByType<AccessCredentialPickup>(FindObjectsSortMode.None); // setta // riga-ok
            // blocco: gira piu volte
            foreach (var p in pickups) // ciclo x // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (p != null) // se ok // riga-ok
                { // apre // riga-ok
                    var idField = typeof(AccessCredentialPickup).GetField("credentialId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance); // setta // riga-ok
                    string credId = idField != null ? (string)idField.GetValue(p) : "KEYCARD_A01"; // setta // riga-ok
                    RegistraCredenziale(credId); // chiama // riga-ok
                    // blocco: controlla se va
                    if (GameManager.Instance != null) // se ok // riga-ok
                        GameManager.Instance.RegisterSecuritySignature(credId); // chiama // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
            RegistraCredenziale("KEYCARD_A01"); // chiama // riga-ok
            RegistraCredenziale("KEYCARD_B02"); // chiama // riga-ok
            RegistraCredenziale("KEYCARD_MASTER"); // chiama // riga-ok

            // 2. Risolvi tutti i focolai nella scena
            EmergencyHotspot[] focolai = UnityEngine.Object.FindObjectsByType<EmergencyHotspot>(FindObjectsSortMode.None); // setta // riga-ok
            // blocco: gira piu volte
            foreach (var f in focolai) // ciclo x // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (f != null && !f.Contenuto) // se ok // riga-ok
                { // apre // riga-ok
                    f.ForzaRisoluzioneDebug(); // chiama // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok

            // 3. Azzera il collasso strutturale
            collassoCorrente = 0f; // setta // riga-ok
            OnDestabilizzazioneCambiata?.Invoke(0f, collassoStrutturale.CollassoMassimo); // chiama // riga-ok
            OnCollassoStrutturaleCambiato?.Invoke(0f, collassoStrutturale.CollassoMassimo); // chiama // riga-ok

            // 4. Lascia il portellone chiuso: F6 serve x sbloccarlo a mano
            if (estrazioneSbloccata) // se ok // riga-ok
                OnEstrazioneSbloccata?.Invoke(true); // chiama // riga-ok

            // 5. Spegni tutte le luci e sirene di emergenza del settore
            LuceEmergenzaSettore[] luciEmergenza = UnityEngine.Object.FindObjectsByType<LuceEmergenzaSettore>(FindObjectsSortMode.None); // setta // riga-ok
            // blocco: gira piu volte
            foreach (var l in luciEmergenza) // ciclo x // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (l != null) l.SetStatoEmergenza(false); // se ok // riga-ok
            } // chiude // riga-ok

            // 6. Notifica stato e aggiorna UI
            NotificaStatoMissione(); // chiama // riga-ok
        } // chiude // riga-ok
        finally // sempre // riga-ok
        { // apre // riga-ok
            bloccaSbloccoEstrazioneDebug = false; // sblocca flag // riga-ok
        } // chiude // riga-ok

        Debug.Log("<color=lime><b>[DEBUG] STATO DI EMERGENZA RIMOSSO! F4 NON APRE IL PORTELLONE: USA F6 SE VUOI SBLOCCARLO.</b></color>"); // logga // riga-ok
    } // chiude // riga-ok

    [ContextMenu("DEBUG: Forza Sblocco Estrazione (F6)")] // nota unity // riga-ok
    // blocco: funzione fa cose
    public void ForzaSbloccoEstrazioneDebug() // roba pub // riga-ok
    { // apre // riga-ok
        estrazioneSbloccata = true; // setta // riga-ok
        OnEstrazioneSbloccata?.Invoke(true); // chiama // riga-ok
        Debug.Log("<color=lime>[DEBUG]</color> Estrazione/Portellone sbloccato con successo!"); // logga // riga-ok
    } // chiude // riga-ok

    [ContextMenu("DEBUG: Completa Missione e Avanza (F7)")] // nota unity // riga-ok
    // blocco: funzione fa cose
    public void ForzaCompletamentoMissioneDebug() // roba pub // riga-ok
    { // apre // riga-ok
        ForzaSbloccoEstrazioneDebug(); // chiama // riga-ok
        TentaEstrazione(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void RegistraRilevamento(string sourceName) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (missioneTerminata) // se ok // riga-ok
            return; // torna val // riga-ok

        allarmiSubiti++; // ok qua // riga-ok
        punteggioBase -= scoreSettings.PenalitaPerAllarme; // setta // riga-ok
        AggiungiCollasso(collassoStrutturale.PenalitaAllarme); // chiama // riga-ok
        OnPunteggioCambiato?.Invoke(CalcolaPunteggioProvvisorio()); // chiama // riga-ok
        Debug.Log($"<color=orange>[ALLARME]</color> Rilevamento subito da {sourceName}. Totale allarmi: {allarmiSubiti}"); // logga // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void RegistraDannoSubito(float quantitaDanno) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (missioneTerminata || quantitaDanno <= 0f) // se ok // riga-ok
            return; // torna val // riga-ok

        int dannoArrotondato = Mathf.CeilToInt(quantitaDanno); // setta // riga-ok
        danniSubitiArrotondati += dannoArrotondato; // setta // riga-ok
        punteggioBase -= dannoArrotondato * scoreSettings.PenalitaPerDannoSubito; // setta // riga-ok
        AggiungiCollasso(collassoStrutturale.CalcolaPenalitaDanno(quantitaDanno)); // chiama // riga-ok
        OnPunteggioCambiato?.Invoke(CalcolaPunteggioProvvisorio()); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void RegistraCredenzialeErrata(string motivo = "") // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (missioneTerminata) // se ok // riga-ok
            return; // torna val // riga-ok

        credenzialiErrate++; // ok qua // riga-ok
        punteggioBase -= scoreSettings.PenalitaPerCredenzialeErrata; // setta // riga-ok
        AggiungiCollasso(collassoStrutturale.PenalitaCredenzialeErrata); // chiama // riga-ok
        OnPunteggioCambiato?.Invoke(CalcolaPunteggioProvvisorio()); // chiama // riga-ok
        Debug.LogWarning($"[CREDENZIALE] Tentativo errato. {motivo}"); // logga // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void RegistraCodiceErrato(string motivo = "") // roba pub // riga-ok
    { // apre // riga-ok
        RegistraCredenzialeErrata(motivo); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void RegistraStressStrutturale(float quantitaCollasso, string sourceName) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (missioneTerminata || quantitaCollasso <= 0f) // se ok // riga-ok
            return; // torna val // riga-ok

        AggiungiCollasso(quantitaCollasso); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void RicaricaScenaCorrente() // roba pub // riga-ok
    { // apre // riga-ok
        Time.timeScale = 1f; // setta // riga-ok
        // blocco: controlla se va
        if (GameManager.Instance != null) // se ok // riga-ok
            GameManager.Instance.CaricaSettoreCorrente(); // chiama // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
            SceneManager.LoadScene(SceneManager.GetActiveScene().name); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void RegistraRaccolta() // roba pub // riga-ok
    { // apre // riga-ok
        RegistraCredenziale($"LEGACY_CREDENTIAL_{credenzialiRaccolte.Count + 1:000}"); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void AggiungiCollasso(float quantita) // roba pub // riga-ok
    { // apre // riga-ok
        // Tutte le variazioni di collasso passano da qui per mantenere clamp,
        // notifiche UI e game over strutturale in un unico punto.
        float valorePrecedente = collassoCorrente; // setta // riga-ok
        collassoCorrente = collassoStrutturale.Clamp(collassoCorrente + quantita); // setta // riga-ok

        // blocco: controlla se va
        if (!Mathf.Approximately(valorePrecedente, collassoCorrente)) // se ok // riga-ok
        { // apre // riga-ok
            OnDestabilizzazioneCambiata?.Invoke(collassoCorrente, collassoStrutturale.CollassoMassimo); // chiama // riga-ok
            OnCollassoStrutturaleCambiato?.Invoke(collassoCorrente, collassoStrutturale.CollassoMassimo); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (collassoCorrente >= collassoStrutturale.CollassoMassimo) // se ok // riga-ok
            TerminaMissione(MissionOutcome.Defeat, "Collasso critico della struttura raggiunto al 100%."); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void AggiornaEstrazione() // roba pub // riga-ok
    { // apre // riga-ok
        // L'obiettivo decide quando il portellone puo' aprirsi; MissionManager
        // si limita a tradurre il conteggio dei focolai in evento per UI e porta.
        bool nuovoStato = obiettivi.IsQuarantineGateUnlocked(focolaiContenuti.Count); // setta // riga-ok
        // blocco: controlla se va
        if (bloccaSbloccoEstrazioneDebug && nuovoStato && !estrazioneSbloccata) // se ok // riga-ok
        { // apre // riga-ok
            Debug.Log("<color=yellow>[DEBUG]</color> F4 ha completato i task, ma il portellone resta chiuso. Usa F6 x aprirlo."); // logga // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok
        // blocco: controlla se va
        if (estrazioneSbloccata == nuovoStato) // se ok // riga-ok
            return; // torna val // riga-ok

        estrazioneSbloccata = nuovoStato; // setta // riga-ok
        OnEstrazioneSbloccata?.Invoke(estrazioneSbloccata); // chiama // riga-ok
        Debug.Log("<color=gold>[QUARANTENA]</color> Portellone di settore sbloccato."); // logga // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void GestisciMortePlayer() // roba pub // riga-ok
    { // apre // riga-ok
        TerminaMissione(MissionOutcome.Defeat, "Operatore neutralizzato."); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void TerminaPerTempoScaduto() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (missioneTerminata) // se ok // riga-ok
            return; // torna val // riga-ok

        Debug.Log("<color=red><b>[TEMPO SCADUTO]</b> Il countdown di emergenza è terminato a 00:00.0! Attivazione Game Over immediato.</color>"); // logga // riga-ok

        SalutePlayer player = UnityEngine.Object.FindAnyObjectByType<SalutePlayer>(); // setta // riga-ok
        // blocco: controlla se va
        if (player != null && player.SaluteAttuale > 0) // se ok // riga-ok
        { // apre // riga-ok
            player.SubisciDanno(99999f); // chiama // riga-ok
        } // chiude // riga-ok

        TerminaMissione(MissionOutcome.Defeat, "Tempo limite scaduto! Evacuazione fallita."); // chiama // riga-ok
        DeathScreenController.ShowAndReloadCurrentScene(3.0f, 0f, "TEMPO SCADUTO // EVACUAZIONE FALLITA"); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void TerminaMissione(MissionOutcome outcome, string reason) // roba pub // riga-ok
    { // apre // riga-ok
        // Finalizzazione unica: calcola score, emette evento finale e decide
        // se avanzare settore, mostrare crediti o lasciare la gestione alla death screen.
        // blocco: controlla se va
        if (missioneTerminata) // se ok // riga-ok
            return; // torna val // riga-ok

        missioneTerminata = true; // setta // riga-ok
        int punteggioFinale = CalcolaPunteggioFinale(outcome); // setta // riga-ok
        OnPunteggioCambiato?.Invoke(punteggioFinale); // chiama // riga-ok
        OnMissioneTerminata?.Invoke(outcome, punteggioFinale, reason); // chiama // riga-ok
        Debug.Log($"<color=gold>[MISSIONE TERMINATA]</color> Esito: {outcome}. Score: {punteggioFinale}. Motivo: {reason}"); // logga // riga-ok

        // blocco: controlla se va
        if (outcome == MissionOutcome.Victory) // se ok // riga-ok
        { // apre // riga-ok
            bool isUltimoLivello = (GameManager.Instance != null && GameManager.Instance.IsUltimoSettore) || // setta // riga-ok
                                   SceneManager.GetActiveScene().name.ToLower().Contains("settore 2"); // chiama // riga-ok

            // blocco: controlla se va
            if (isUltimoLivello) // se ok // riga-ok
            { // apre // riga-ok
                Debug.Log("<color=lime><b>[VITTORIA FINALE]</b> Settore 2 completato! Avvio schermata finale e titoli di coda...</color>"); // logga // riga-ok
                StartCoroutine(DelayMostraTitoliDiCoda(2f, punteggioFinale)); // corutina // riga-ok
            } // chiude // riga-ok
            // blocco: controlla se va
            else if (GameManager.Instance != null) // se ok // riga-ok
            { // apre // riga-ok
                StartCoroutine(DelayCaricaProssimoSettore(2f)); // corutina // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private IEnumerator DelayMostraTitoliDiCoda(float delay, int score) // roba pub // riga-ok
    { // apre // riga-ok
        yield return new WaitForSeconds(delay); // aspetta // riga-ok
        EndGameCreditsController.ShowVictoryAndCredits(score); // chiama // riga-ok
    } // chiude // riga-ok

    /// <summary>
    /// Attende <paramref name="delay"/> secondi, poi carica il settore successivo.
    /// Il delay lascia il tempo alla UI di mostrare il risultato prima della transizione.
    /// </summary>
    // blocco: funzione fa cose
    private IEnumerator DelayCaricaProssimoSettore(float delay) // roba pub // riga-ok
    { // apre // riga-ok
        yield return new WaitForSeconds(delay); // aspetta // riga-ok
        GameManager.Instance.CaricaProssimoSettore(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private int CalcolaPunteggioProvvisorio() // roba pub // riga-ok
    { // apre // riga-ok
        return scoreSettings.CalcolaProvvisorio(punteggioBase, collassoStrutturale.IntegritaResidua(collassoCorrente)); // torna val // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private int CalcolaPunteggioFinale(MissionOutcome outcome) // roba pub // riga-ok
    { // apre // riga-ok
        return scoreSettings.CalcolaFinale( // torna val // riga-ok
            outcome, // ok qua // riga-ok
            CalcolaPunteggioProvvisorio(), // ok qua // riga-ok
            allarmiSubiti, // ok qua // riga-ok
            credenzialiErrate, // ok qua // riga-ok
            danniSubitiArrotondati, // ok qua // riga-ok
            tempoMissione); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void NotificaStatoMissione() // roba pub // riga-ok
    { // apre // riga-ok
        OnDestabilizzazioneCambiata?.Invoke(collassoCorrente, collassoStrutturale.CollassoMassimo); // chiama // riga-ok
        OnCollassoStrutturaleCambiato?.Invoke(collassoCorrente, collassoStrutturale.CollassoMassimo); // chiama // riga-ok
        OnFocolaiCambiati?.Invoke(focolaiContenuti.Count, TotaleFocolai); // chiama // riga-ok
        OnCredenzialiCambiate?.Invoke(credenzialiRaccolte.Count); // chiama // riga-ok
        OnFrequenzeCambiate?.Invoke(credenzialiRaccolte.Count); // chiama // riga-ok
        OnPunteggioCambiato?.Invoke(CalcolaPunteggioProvvisorio()); // chiama // riga-ok
        AggiornaEstrazione(); // chiama // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
