// ============================================================================
// Crisis Protocol / Sector Containment - Missione e contenimento
// File: .\Assets\CrisisProtocol\Scripts\Mission\SectorEmergency\SectorScoreSettings.cs
// Responsabilita': modella credenziali, focolai, portelloni, hazard o parametri di bilanciamento del loop emergenza -> contenimento -> estrazione.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using System; // usa lib // riga-ok
using UnityEngine; // usa lib // riga-ok

[Serializable] // nota unity // riga-ok
// blocco: classe x roba grossa
public class SectorScoreSettings // classe qui // riga-ok
{ // apre // riga-ok
    [SerializeField] private int puntiPerFocolaioContenuto = 1000; // setta // riga-ok
    [SerializeField] private int puntiPerCredenziale = 150; // setta // riga-ok
    [SerializeField] private int bonusEstrazioneSicura = 750; // setta // riga-ok
    [SerializeField] private int bonusStealthPerfetto = 1000; // setta // riga-ok
    [SerializeField] private int puntiPerIntegritaResidua = 15; // setta // riga-ok
    [SerializeField] private int penalitaPerAllarme = 125; // setta // riga-ok
    [SerializeField] private int penalitaPerDannoSubito = 5; // setta // riga-ok
    [SerializeField] private int penalitaPerCredenzialeErrata = 100; // setta // riga-ok
    [SerializeField] private int bonusTempoMassimo = 1200; // setta // riga-ok
    [SerializeField] private float decadimentoBonusTempoAlSecondo = 8f; // setta // riga-ok

    public int PuntiPerFocolaioContenuto => puntiPerFocolaioContenuto; // roba pub // riga-ok
    public int PuntiPerCredenziale => puntiPerCredenziale; // roba pub // riga-ok
    public int PenalitaPerAllarme => penalitaPerAllarme; // roba pub // riga-ok
    public int PenalitaPerDannoSubito => penalitaPerDannoSubito; // roba pub // riga-ok
    public int PenalitaPerCredenzialeErrata => penalitaPerCredenzialeErrata; // roba pub // riga-ok

    // blocco: funzione fa cose
    public int CalcolaProvvisorio(int punteggioBase, float integritaResidua) // roba pub // riga-ok
    { // apre // riga-ok
        int bonusIntegrita = Mathf.RoundToInt(integritaResidua * puntiPerIntegritaResidua); // setta // riga-ok
        return Mathf.Max(0, punteggioBase + bonusIntegrita); // torna val // riga-ok
    } // chiude // riga-ok

    public int CalcolaFinale( // roba pub // riga-ok
        MissionManager.MissionOutcome outcome, // ok qua // riga-ok
        int punteggioProvvisorio, // ok qua // riga-ok
        int allarmiSubiti, // ok qua // riga-ok
        int credenzialiErrate, // ok qua // riga-ok
        int danniSubitiArrotondati, // ok qua // riga-ok
        float tempoMissione) // chiama // riga-ok
    { // apre // riga-ok
        int score = punteggioProvvisorio; // setta // riga-ok

        // blocco: controlla se va
        if (outcome == MissionManager.MissionOutcome.Victory) // se ok // riga-ok
        { // apre // riga-ok
            score += bonusEstrazioneSicura; // setta // riga-ok

            // blocco: controlla se va
            if (allarmiSubiti == 0) // se ok // riga-ok
                score += bonusStealthPerfetto; // setta // riga-ok

            score += Mathf.Max(0, Mathf.RoundToInt(bonusTempoMassimo - tempoMissione * decadimentoBonusTempoAlSecondo)); // setta // riga-ok
        } // chiude // riga-ok

        score -= credenzialiErrate * penalitaPerCredenzialeErrata; // setta // riga-ok
        score -= danniSubitiArrotondati * penalitaPerDannoSubito; // setta // riga-ok
        return Mathf.Max(0, score); // torna val // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
