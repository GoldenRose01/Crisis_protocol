using UnityEngine;
using TMPro;

public class HUDManager : MonoBehaviour
{
    [Header("Riferimenti Testuali (TextMeshPro)")]
    public TextMeshProUGUI testoSalute;
    public TextMeshProUGUI testoMunizioni;
    public TextMeshProUGUI testoMissione;
    public TextMeshProUGUI testoDestabilizzazione;
    public TextMeshProUGUI testoFrequenze;
    public TextMeshProUGUI testoPunteggio;
    public TextMeshProUGUI testoEsitoMissione;

    private void OnEnable()
    {
        // Iscrizione agli eventi (Observer)
        SalutePlayer.OnSaluteCambiata += AggiornaSalute;
        SparoPlayer.OnMunizioniCambiate += AggiornaMunizioni;
        MissionManager.OnCollassoStrutturaleCambiato += AggiornaDestabilizzazione;
        MissionManager.OnFocolaiCambiati += AggiornaFocolai;
        MissionManager.OnCredenzialiCambiate += AggiornaFrequenze;
        MissionManager.OnPunteggioCambiato += AggiornaPunteggio;
        MissionManager.OnEstrazioneSbloccata += AggiornaEstrazione;
        MissionManager.OnMissioneTerminata += AggiornaEsitoMissione;
    }

    private void OnDisable()
    {
        // Disiscrizione per prevenire memory leak al cambio scena
        SalutePlayer.OnSaluteCambiata -= AggiornaSalute;
        SparoPlayer.OnMunizioniCambiate -= AggiornaMunizioni;
        MissionManager.OnCollassoStrutturaleCambiato -= AggiornaDestabilizzazione;
        MissionManager.OnFocolaiCambiati -= AggiornaFocolai;
        MissionManager.OnCredenzialiCambiate -= AggiornaFrequenze;
        MissionManager.OnPunteggioCambiato -= AggiornaPunteggio;
        MissionManager.OnEstrazioneSbloccata -= AggiornaEstrazione;
        MissionManager.OnMissioneTerminata -= AggiornaEsitoMissione;
    }

    private void AggiornaSalute(float corrente, float massima)
    {
        if (testoSalute != null)
        {
            testoSalute.text = $"HP: {Mathf.CeilToInt(corrente)} / {Mathf.CeilToInt(massima)}";
        }
    }

    private void AggiornaMunizioni(int attuali, int massime)
    {
        if (testoMunizioni != null)
        {
            testoMunizioni.text = $"AMMO: {attuali} / {massime}";
        }
    }

    // Interfaccia pubblica chiamata dal tuo futuro GestoreMissioni
    public void ImpostaTestoMissione(string nuovoTesto)
    {
        if (testoMissione != null)
        {
            testoMissione.text = nuovoTesto;
        }
    }

    private void AggiornaDestabilizzazione(float corrente, float massima)
    {
        if (testoDestabilizzazione != null)
            testoDestabilizzazione.text = $"COLLASSO STRUTTURA: {Mathf.CeilToInt(corrente)}%";
    }

    private void AggiornaFocolai(int stabilizzati, int totale)
    {
        if (testoMissione != null)
            testoMissione.text = totale > 0
                ? $"FOCOLAI CONTENUTI: {stabilizzati} / {totale}"
                : $"FOCOLAI CONTENUTI: {stabilizzati}";
    }

    private void AggiornaFrequenze(int count)
    {
        if (testoFrequenze != null)
            testoFrequenze.text = $"CREDENZIALI: {count}";
    }

    private void AggiornaPunteggio(int score)
    {
        if (testoPunteggio != null)
            testoPunteggio.text = $"SCORE: {score}";
    }

    private void AggiornaEstrazione(bool sbloccata)
    {
        if (testoMissione != null && sbloccata)
            testoMissione.text = "PORTELLONE SBLOCCATO: RAGGIUNGI L'USCITA";
    }

    private void AggiornaEsitoMissione(MissionManager.MissionOutcome outcome, int score, string reason)
    {
        if (testoEsitoMissione == null)
            return;

        string label = outcome == MissionManager.MissionOutcome.Victory ? "MISSIONE COMPLETATA" : "MISSIONE FALLITA";
        testoEsitoMissione.text = $"{label}\n{reason}\nSCORE: {score}";
    }
}
