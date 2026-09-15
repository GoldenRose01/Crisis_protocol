// ============================================================================
// Crisis Protocol / Sector Containment - Core runtime
// File: .\Assets\CrisisProtocol\Scripts\Core\MissionManager.cs
// Responsabilita': coordina stato globale, salvataggi, avanzamento partita o servizi persistenti condivisi tra scene.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
/// <summary>
/// Controller runtime della missione corrente.
/// Tiene insieme obiettivi, credenziali, focolai, collasso strutturale,
/// scoring e condizioni di vittoria/sconfitta per il settore attivo.
/// </summary>
public class MissionManager : MonoBehaviour
{
    public enum MissionOutcome { Victory, Defeat }

    // Singleton di scena: a differenza del GameManager non resta tra i livelli.
    // Ogni settore ha la sua missione, i suoi focolai e il suo timer.
    public static MissionManager Instance { get; private set; }

    [Header("Obiettivi Missione")]
    [SerializeField] private SectorObjectiveSettings obiettivi = new SectorObjectiveSettings();
    [Header("Timer Missione & Sconfitta (Countdown)")]
    [Tooltip("Se abilitato, impone un tempo limite di evacuazione con countdown a schermo.")]
    [SerializeField] private bool usaTempoLimite = true;
    [Tooltip("Durata del countdown in MINUTI (es. 5 = 5:00, 2.5 = 2:30, 10 = 10:00).")]
    [SerializeField] [Range(0.5f, 60f)] private float tempoLimiteMinuti = 5.0f;
    [Tooltip("Se abilitato, allo scadere del tempo (00:00.0) scatta immediatamente il Game Over (Sconfitta).")]
    [SerializeField] private bool sconfittaAScadenzaTimer = true;
    [Header("Collasso Strutturale")]
    [SerializeField] private StructuralCollapseSettings collassoStrutturale = new StructuralCollapseSettings();
    [Header("Punteggio")]
    [SerializeField] private SectorScoreSettings scoreSettings = new SectorScoreSettings();

    // HashSet per evitare doppioni: se un pickup o un hotspot notifica due volte,
    // il sistema resta stabile e non regala punti extra.
    private readonly HashSet<string> credenzialiRaccolte = new HashSet<string>();
    private readonly HashSet<string> focolaiContenuti = new HashSet<string>();

    // Stato runtime del settore. Non va salvato qui: il GameManager gestisce solo
    // la progressione globale, mentre questo script gestisce la partita in corso.
    private float collassoCorrente;
    private float tempoMissione;
    private int allarmiSubiti;
    private int credenzialiErrate;
    private int danniSubitiArrotondati;
    private int punteggioBase;
    private bool estrazioneSbloccata;
    private bool bloccaSbloccoEstrazioneDebug;
    private bool missioneTerminata;

    // Eventi usati dalla UI e dagli oggetti di scena. Così HUD, porte e feedback
    // non devono cercarsi a vicenda: ascoltano MissionManager e reagiscono.
    public static event Action<float, float> OnDestabilizzazioneCambiata;
    public static event Action<float, float> OnCollassoStrutturaleCambiato;
    public static event Action<int, int> OnFocolaiCambiati;
    public static event Action<int> OnFrequenzeCambiate;
    public static event Action<int> OnCredenzialiCambiate;
    public static event Action<string, int> OnNuovaCredenzialeRaccolta;
    public static event Action<int> OnPunteggioCambiato;
    public static event Action<bool> OnEstrazioneSbloccata;
    public static event Action<MissionOutcome, int, string> OnMissioneTerminata;
    public float DestabilizzazioneCorrente => collassoCorrente;
    public float CollassoCorrente => collassoCorrente;
    public int FrequenzeRaccolte => credenzialiRaccolte.Count;
    public int CredenzialiRaccolte => credenzialiRaccolte.Count;
    public int FocolaiStabilizzati => focolaiContenuti.Count;
    public int FocolaiContenuti => focolaiContenuti.Count;
    public int TotaleFocolai => obiettivi.TotaleFocolaiDaContenere;
    public bool EstrazioneSbloccata => estrazioneSbloccata;
    public bool MissioneTerminata => missioneTerminata;
    public bool UsaTempoLimite => usaTempoLimite;
    public float TempoLimiteMinuti => tempoLimiteMinuti;
    public float TempoLimiteSecondi => tempoLimiteMinuti * 60f;
    public bool SconfittaAScadenzaTimer => sconfittaAScadenzaTimer;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    private void OnEnable()
    {
        SalutePlayer.OnPlayerMorto += GestisciMortePlayer;
    }
    private void OnDisable()
    {
        SalutePlayer.OnPlayerMorto -= GestisciMortePlayer;
    }
    private void Start()
    {
        // Calcola gli obiettivi partendo dagli hotspot effettivamente presenti
        // nella scena, cosi' il designer puo' modificare il livello senza toccare codice.
        obiettivi.InitializeFromScene();
        collassoCorrente = collassoStrutturale.Clamp(collassoStrutturale.CollassoIniziale);
        NotificaStatoMissione();
    }
    private void Update()
    {
        // F4 resta in codice ma spento: era comodo per testare, pero' apriva
        // indirettamente portelloni di settore. Meglio lasciarlo come tool
        // riattivabile da dev, non come shortcut attiva durante la consegna.
        if (false && Input.GetKeyDown(KeyCode.F4))
        {
            Debug.LogWarning("[DEBUG] Tasto F4 premuto: Risoluzione emergenza e completamento totale task.");
            RisolviStatoEmergenzaETuttiTaskDebug();
        }
        if (Input.GetKeyDown(KeyCode.F6))
        {
            Debug.LogWarning("[DEBUG] Tasto F6 premuto: Sblocco immediato estrazione/portellone.");
            ForzaSbloccoEstrazioneDebug();
        }
        if (Input.GetKeyDown(KeyCode.F7))
        {
            Debug.LogWarning("[DEBUG] Tasto F7 premuto: Vittoria forzata e passaggio al prossimo livello.");
            ForzaCompletamentoMissioneDebug();
        }
        // blocco: controlla se va
        if (missioneTerminata || estrazioneSbloccata) // se ok // riga-ok
            return; // torna val // riga-ok

        // Il collasso cresce continuamente finche' la missione e' attiva e non e' sbloccata l'estrazione.
        // Eventi, danni e allarmi possono aumentarlo; contenimenti riusciti possono ridurlo.
        tempoMissione += Time.deltaTime; // setta // riga-ok
        AggiungiCollasso(collassoStrutturale.IncrementoPerSecondo * Time.deltaTime); // chiama // riga-ok
    } // chiude // riga-ok
    public bool RegistraCredenziale(string credentialId)
    {
        // Le credenziali sono risorse di accesso: vengono contate per UI/punteggio
        // e abilitate per hotspot che richiedono una keycard specifica.
        if (missioneTerminata || string.IsNullOrWhiteSpace(credentialId))
            return false;
        bool nuovaCredenziale = credenzialiRaccolte.Add(credentialId);
        if (!nuovaCredenziale)
            return false;
        punteggioBase += scoreSettings.PuntiPerCredenziale;
        OnNuovaCredenzialeRaccolta?.Invoke(credentialId, credenzialiRaccolte.Count);
        OnCredenzialiCambiate?.Invoke(credenzialiRaccolte.Count);
        OnFrequenzeCambiate?.Invoke(credenzialiRaccolte.Count);
        OnPunteggioCambiato?.Invoke(CalcolaPunteggioProvvisorio());
        Debug.Log($"<color=cyan>[CREDENZIALE]</color> Autorizzazione acquisita: {credentialId}. Totale: {credenzialiRaccolte.Count}");
        return true;
    }
    public bool RegistraFrequenza(string frequencyId)
    {
        return RegistraCredenziale(frequencyId);
    }
    public bool PossiedeCredenziale(string credentialId)
    {
        // Stringa vuota = nessun requisito. Così le porte libere e gli hotspot
        // senza keycard non devono gestire casi speciali altrove.
        return string.IsNullOrWhiteSpace(credentialId) || credenzialiRaccolte.Contains(credentialId);
    }
    /// <summary>
    /// Restituisce true solo se la credenziale specifica è stata fisicamente raccolta nella sessione corrente di missione.
    /// </summary>
    public bool HaRaccoltoCredenzialeSpecifica(string credentialId)
    {
        if (string.IsNullOrWhiteSpace(credentialId)) return false;
        return credenzialiRaccolte.Contains(credentialId);
    }
    public bool PossiedeFrequenza(string frequencyId)
    {
        return PossiedeCredenziale(frequencyId);
    }
    public bool ContieniFocolaio(string hotspotId, string requiredCredentialId)
    {
        // Contenimento atomico del focolaio: verifica autorizzazione, assegna punti,
        // riduce collasso, notifica GameManager e aggiorna lo stato estrazione.
        if (missioneTerminata || string.IsNullOrWhiteSpace(hotspotId))
            return false;
        if (!PossiedeCredenziale(requiredCredentialId))
        {
            RegistraCredenzialeErrata($"Credenziale richiesta non posseduta: {requiredCredentialId}");
            return false;
        }
        bool nuovoFocolaio = focolaiContenuti.Add(hotspotId);
        if (!nuovoFocolaio)
            return false;
        punteggioBase += scoreSettings.PuntiPerFocolaioContenuto;
        AggiungiCollasso(-collassoStrutturale.RecuperoPerFocolaioContenuto);
        if (GameManager.Instance != null)
            GameManager.Instance.RegistraMissioneCompletata(hotspotId);
        AggiornaEstrazione();
        OnFocolaiCambiati?.Invoke(focolaiContenuti.Count, TotaleFocolai);
        OnPunteggioCambiato?.Invoke(CalcolaPunteggioProvvisorio());
        Debug.Log($"<color=lime>[CONTENIMENTO]</color> Focolaio contenuto: {hotspotId}. Progresso: {focolaiContenuti.Count}/{TotaleFocolai}");
        return true;
    }
    public bool StabilizzaFocolaio(string fractureId, string requiredFrequencyId)
    {
        return ContieniFocolaio(fractureId, requiredFrequencyId);
    }
    public void TentaEstrazione()
    {
        // L'estrazione e' valida solo dopo aver soddisfatto gli obiettivi del settore.
        // La porta puo' chiamare questo metodo senza conoscere i dettagli della missione.
        if (missioneTerminata)
            return;
        if (!estrazioneSbloccata)
        {
            Debug.LogWarning("[QUARANTENA] Portellone bloccato: contenere tutti i focolai d'emergenza.");
            return;
        }
        TerminaMissione(MissionOutcome.Victory, "Estrazione in sicurezza completata prima del collasso della struttura.");
    }
    [ContextMenu("DEBUG: Risolvi Stato Emergenza e Completa Tutti i Task (F4)")]
    public void RisolviStatoEmergenzaETuttiTaskDebug()
    {
        Debug.Log("<color=lime><b>[DEBUG] RISOLUZIONE TOTALE EMERGENZA AVVIATA...</b></color>");

        // Guard rail importante: risolvere i task via debug non deve simulare
        // anche l'apertura del portellone. Per aprirlo apposta esiste F6.
        bloccaSbloccoEstrazioneDebug = true;
        try
        {
            // Raccolta "di servizio": serve a far passare i controlli degli hotspot
            // quando li forziamo chiusi, senza far fallire i check sulle keycard.
            AccessCredentialPickup[] pickups = UnityEngine.Object.FindObjectsByType<AccessCredentialPickup>(FindObjectsSortMode.None);
            foreach (var p in pickups)
            {
                if (p != null)
                {
                    var idField = typeof(AccessCredentialPickup).GetField("credentialId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    string credId = idField != null ? (string)idField.GetValue(p) : "KEYCARD_A01";
                    RegistraCredenziale(credId);
                    if (GameManager.Instance != null)
                        GameManager.Instance.RegisterSecuritySignature(credId);
                }
            }
            RegistraCredenziale("KEYCARD_A01");
            RegistraCredenziale("KEYCARD_B02");
            RegistraCredenziale("KEYCARD_MASTER");
            // Chiude tutti i focolai trovati in scena. Non filtra per settore
            // perché MissionManager vive dentro la scena attiva.
            EmergencyHotspot[] focolai = UnityEngine.Object.FindObjectsByType<EmergencyHotspot>(FindObjectsSortMode.None);
            foreach (var f in focolai)
            {
                if (f != null && !f.Contenuto)
                {
                    f.ForzaRisoluzioneDebug();
                }
            }
            // Reset del rischio: se il debug risolve la crisi, il player non deve
            // morire subito dopo per un collasso gia' accumulato.
            collassoCorrente = 0f;
            OnDestabilizzazioneCambiata?.Invoke(0f, collassoStrutturale.CollassoMassimo);
            OnCollassoStrutturaleCambiato?.Invoke(0f, collassoStrutturale.CollassoMassimo);
            // Nota: non impostiamo estrazioneSbloccata = true. Se lo facessimo,
            // i QuarantineGate riceverebbero il segnale e aprirebbero l'uscita.
            if (estrazioneSbloccata)
                OnEstrazioneSbloccata?.Invoke(true);

            // Spegne feedback ambientali collegati alla crisi: utile per vedere
            // subito in scena che lo stato di emergenza e' stato neutralizzato.
            LuceEmergenzaSettore[] luciEmergenza = UnityEngine.Object.FindObjectsByType<LuceEmergenzaSettore>(FindObjectsSortMode.None);
            foreach (var l in luciEmergenza)
            {
                if (l != null) l.SetStatoEmergenza(false);
            }
            // Ultimo giro di eventi per riallineare HUD, timer, score e indicatori.
            NotificaStatoMissione();
        }
        finally
        {
            bloccaSbloccoEstrazioneDebug = false;
        }
        Debug.Log("<color=lime><b>[DEBUG] STATO DI EMERGENZA RIMOSSO! F4 NON APRE IL PORTELLONE: USA F6 SE VUOI SBLOCCARLO.</b></color>");
    }
    [ContextMenu("DEBUG: Forza Sblocco Estrazione (F6)")]
    public void ForzaSbloccoEstrazioneDebug()
    {
        estrazioneSbloccata = true;
        OnEstrazioneSbloccata?.Invoke(true);
        Debug.Log("<color=lime>[DEBUG]</color> Estrazione/Portellone sbloccato con successo!");
    }
    [ContextMenu("DEBUG: Completa Missione e Avanza (F7)")]
    public void ForzaCompletamentoMissioneDebug()
    {
        ForzaSbloccoEstrazioneDebug();
        TentaEstrazione();
    }
    public void RegistraRilevamento(string sourceName)
    {
        if (missioneTerminata)
            return;
        allarmiSubiti++;
        punteggioBase -= scoreSettings.PenalitaPerAllarme;
        AggiungiCollasso(collassoStrutturale.PenalitaAllarme);
        OnPunteggioCambiato?.Invoke(CalcolaPunteggioProvvisorio());
        Debug.Log($"<color=orange>[ALLARME]</color> Rilevamento subito da {sourceName}. Totale allarmi: {allarmiSubiti}");
    }
    public void RegistraDannoSubito(float quantitaDanno)
    {
        if (missioneTerminata || quantitaDanno <= 0f)
            return;
        int dannoArrotondato = Mathf.CeilToInt(quantitaDanno);
        danniSubitiArrotondati += dannoArrotondato;
        punteggioBase -= dannoArrotondato * scoreSettings.PenalitaPerDannoSubito;
        AggiungiCollasso(collassoStrutturale.CalcolaPenalitaDanno(quantitaDanno));
        OnPunteggioCambiato?.Invoke(CalcolaPunteggioProvvisorio());
    }
    public void RegistraCredenzialeErrata(string motivo = "")
    {
        if (missioneTerminata)
            return;
        credenzialiErrate++;
        punteggioBase -= scoreSettings.PenalitaPerCredenzialeErrata;
        AggiungiCollasso(collassoStrutturale.PenalitaCredenzialeErrata);
        OnPunteggioCambiato?.Invoke(CalcolaPunteggioProvvisorio());
        Debug.LogWarning($"[CREDENZIALE] Tentativo errato. {motivo}");
    }
    public void RegistraCodiceErrato(string motivo = "")
    {
        RegistraCredenzialeErrata(motivo);
    }
    public void RegistraStressStrutturale(float quantitaCollasso, string sourceName)
    {
        if (missioneTerminata || quantitaCollasso <= 0f)
            return;
        AggiungiCollasso(quantitaCollasso);
    }
    public void RicaricaScenaCorrente()
    {
        Time.timeScale = 1f;
        if (GameManager.Instance != null)
            GameManager.Instance.CaricaSettoreCorrente();
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void RegistraRaccolta()
    {
        RegistraCredenziale($"LEGACY_CREDENTIAL_{credenzialiRaccolte.Count + 1:000}");
    }
    private void AggiungiCollasso(float quantita)
    {
        // Tutte le variazioni di collasso passano da qui per mantenere clamp,
        // notifiche UI e game over strutturale in un unico punto.
        float valorePrecedente = collassoCorrente;
        collassoCorrente = collassoStrutturale.Clamp(collassoCorrente + quantita);
        if (!Mathf.Approximately(valorePrecedente, collassoCorrente))
        {
            OnDestabilizzazioneCambiata?.Invoke(collassoCorrente, collassoStrutturale.CollassoMassimo);
            OnCollassoStrutturaleCambiato?.Invoke(collassoCorrente, collassoStrutturale.CollassoMassimo);
        }
        if (collassoCorrente >= collassoStrutturale.CollassoMassimo)
            TerminaMissione(MissionOutcome.Defeat, "Collasso critico della struttura raggiunto al 100%.");
    }
    private void AggiornaEstrazione()
    {
        // L'obiettivo decide quando il portellone puo' aprirsi; MissionManager
        // si limita a tradurre il conteggio dei focolai in evento per UI e porta.
        bool nuovoStato = obiettivi.IsQuarantineGateUnlocked(focolaiContenuti.Count);

        // Durante F4 vogliamo completare i task senza aprire l'uscita. Questo
        // blocco evita il bug "premi F4 nel settore 0 e trovi gia' aperto il varco".
        if (bloccaSbloccoEstrazioneDebug && nuovoStato && !estrazioneSbloccata)
        {
            Debug.Log("<color=yellow>[DEBUG]</color> F4 ha completato i task, ma il portellone resta chiuso. Usa F6 x aprirlo.");
            return;
        }
        if (estrazioneSbloccata == nuovoStato)
            return;
        estrazioneSbloccata = nuovoStato;
        OnEstrazioneSbloccata?.Invoke(estrazioneSbloccata);
        Debug.Log("<color=gold>[QUARANTENA]</color> Portellone di settore sbloccato.");
    }
    private void GestisciMortePlayer()
    {
        TerminaMissione(MissionOutcome.Defeat, "Operatore neutralizzato.");
    }
    public void TerminaPerTempoScaduto()
    {
        if (missioneTerminata)
            return;
        Debug.Log("<color=red><b>[TEMPO SCADUTO]</b> Il countdown di emergenza è terminato a 00:00.0! Attivazione Game Over immediato.</color>");
        SalutePlayer player = UnityEngine.Object.FindAnyObjectByType<SalutePlayer>();
        if (player != null && player.SaluteAttuale > 0)
        {
            player.SubisciDanno(99999f);
        }
        TerminaMissione(MissionOutcome.Defeat, "Tempo limite scaduto! Evacuazione fallita.");
        DeathScreenController.ShowAndReloadCurrentScene(3.0f, 0f, "TEMPO SCADUTO // EVACUAZIONE FALLITA");
    }
    private void TerminaMissione(MissionOutcome outcome, string reason)
    {
        // Finalizzazione unica: calcola score, emette evento finale e decide
        // se avanzare settore, mostrare crediti o lasciare la gestione alla death screen.
        if (missioneTerminata)
            return;
        missioneTerminata = true;
        int punteggioFinale = CalcolaPunteggioFinale(outcome);
        OnPunteggioCambiato?.Invoke(punteggioFinale);
        OnMissioneTerminata?.Invoke(outcome, punteggioFinale, reason);
        Debug.Log($"<color=gold>[MISSIONE TERMINATA]</color> Esito: {outcome}. Score: {punteggioFinale}. Motivo: {reason}");
        if (outcome == MissionOutcome.Victory)
        {
            bool isUltimoLivello = (GameManager.Instance != null && GameManager.Instance.IsUltimoSettore) ||
                                   SceneManager.GetActiveScene().name.ToLower().Contains("settore 2");
            if (isUltimoLivello)
            {
                Debug.Log("<color=lime><b>[VITTORIA FINALE]</b> Settore 2 completato! Avvio schermata finale e titoli di coda...</color>");
                StartCoroutine(DelayMostraTitoliDiCoda(2f, punteggioFinale));
            }
            else if (GameManager.Instance != null)
            {
                StartCoroutine(DelayCaricaProssimoSettore(2f));
            }
        }
    }
    private IEnumerator DelayMostraTitoliDiCoda(float delay, int score)
    {
        yield return new WaitForSeconds(delay);
        EndGameCreditsController.ShowVictoryAndCredits(score);
    }
    /// <summary>
    /// Attende <paramref name="delay"/> secondi, poi carica il settore successivo.
    /// Il delay lascia il tempo alla UI di mostrare il risultato prima della transizione.
    /// </summary>
    private IEnumerator DelayCaricaProssimoSettore(float delay)
    {
        yield return new WaitForSeconds(delay);
        GameManager.Instance.CaricaProssimoSettore();
    }
    private int CalcolaPunteggioProvvisorio()
    {
        return scoreSettings.CalcolaProvvisorio(punteggioBase, collassoStrutturale.IntegritaResidua(collassoCorrente));
    }
    private int CalcolaPunteggioFinale(MissionOutcome outcome)
    {
        return scoreSettings.CalcolaFinale(
            outcome,
            CalcolaPunteggioProvvisorio(),
            allarmiSubiti,
            credenzialiErrate,
            danniSubitiArrotondati,
            tempoMissione);
    }
    private void NotificaStatoMissione()
    {
        OnDestabilizzazioneCambiata?.Invoke(collassoCorrente, collassoStrutturale.CollassoMassimo);
        OnCollassoStrutturaleCambiato?.Invoke(collassoCorrente, collassoStrutturale.CollassoMassimo);
        OnFocolaiCambiati?.Invoke(focolaiContenuti.Count, TotaleFocolai);
        OnCredenzialiCambiate?.Invoke(credenzialiRaccolte.Count);
        OnFrequenzeCambiate?.Invoke(credenzialiRaccolte.Count);
        OnPunteggioCambiato?.Invoke(CalcolaPunteggioProvvisorio());
        AggiornaEstrazione();
    }
}
