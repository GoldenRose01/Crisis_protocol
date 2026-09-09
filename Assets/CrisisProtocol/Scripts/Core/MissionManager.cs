using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MissionManager : MonoBehaviour
{
    public enum MissionOutcome { Victory, Defeat }

    public static MissionManager Instance { get; private set; }

    [Header("Obiettivi Missione")]
    [SerializeField] private SectorObjectiveSettings obiettivi = new SectorObjectiveSettings();

    [Header("Collasso Strutturale")]
    [SerializeField] private StructuralCollapseSettings collassoStrutturale = new StructuralCollapseSettings();

    [Header("Punteggio")]
    [SerializeField] private SectorScoreSettings scoreSettings = new SectorScoreSettings();

    private readonly HashSet<string> credenzialiRaccolte = new HashSet<string>();
    private readonly HashSet<string> focolaiContenuti = new HashSet<string>();

    private float collassoCorrente;
    private float tempoMissione;
    private int allarmiSubiti;
    private int credenzialiErrate;
    private int danniSubitiArrotondati;
    private int punteggioBase;
    private bool estrazioneSbloccata;
    private bool missioneTerminata;

    public static event Action<float, float> OnDestabilizzazioneCambiata;
    public static event Action<float, float> OnCollassoStrutturaleCambiato;
    public static event Action<int, int> OnFocolaiCambiati;
    public static event Action<int> OnFrequenzeCambiate;
    public static event Action<int> OnCredenzialiCambiate;
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
        obiettivi.InitializeFromScene();

        collassoCorrente = collassoStrutturale.Clamp(collassoStrutturale.CollassoIniziale);
        NotificaStatoMissione();
    }

    private void Update()
    {
        if (missioneTerminata)
            return;

        tempoMissione += Time.deltaTime;
        AggiungiCollasso(collassoStrutturale.IncrementoPerSecondo * Time.deltaTime);
    }

    public bool RegistraCredenziale(string credentialId)
    {
        if (missioneTerminata || string.IsNullOrWhiteSpace(credentialId))
            return false;

        bool nuovaCredenziale = credenzialiRaccolte.Add(credentialId);
        if (!nuovaCredenziale)
            return false;

        punteggioBase += scoreSettings.PuntiPerCredenziale;
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
        return string.IsNullOrWhiteSpace(credentialId) || credenzialiRaccolte.Contains(credentialId);
    }

    public bool PossiedeFrequenza(string frequencyId)
    {
        return PossiedeCredenziale(frequencyId);
    }

    public bool ContieniFocolaio(string hotspotId, string requiredCredentialId)
    {
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
        if (missioneTerminata)
            return;

        if (!estrazioneSbloccata)
        {
            Debug.LogWarning("[QUARANTENA] Portellone bloccato: contenere tutti i focolai d'emergenza.");
            return;
        }

        TerminaMissione(MissionOutcome.Victory, "Estrazione in sicurezza completata prima del collasso della struttura.");
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
        bool nuovoStato = obiettivi.IsQuarantineGateUnlocked(focolaiContenuti.Count);
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

    private void TerminaMissione(MissionOutcome outcome, string reason)
    {
        if (missioneTerminata)
            return;

        missioneTerminata = true;
        int punteggioFinale = CalcolaPunteggioFinale(outcome);
        OnPunteggioCambiato?.Invoke(punteggioFinale);
        OnMissioneTerminata?.Invoke(outcome, punteggioFinale, reason);
        Debug.Log($"<color=gold>[MISSIONE TERMINATA]</color> Esito: {outcome}. Score: {punteggioFinale}. Motivo: {reason}");

        if (outcome == MissionOutcome.Victory && GameManager.Instance != null)
            StartCoroutine(DelayCaricaProssimoSettore(2f));
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
