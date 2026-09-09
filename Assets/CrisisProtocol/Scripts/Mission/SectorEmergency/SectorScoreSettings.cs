using System;
using UnityEngine;

[Serializable]
public class SectorScoreSettings
{
    [SerializeField] private int puntiPerFocolaioContenuto = 1000;
    [SerializeField] private int puntiPerCredenziale = 150;
    [SerializeField] private int bonusEstrazioneSicura = 750;
    [SerializeField] private int bonusStealthPerfetto = 1000;
    [SerializeField] private int puntiPerIntegritaResidua = 15;
    [SerializeField] private int penalitaPerAllarme = 125;
    [SerializeField] private int penalitaPerDannoSubito = 5;
    [SerializeField] private int penalitaPerCredenzialeErrata = 100;
    [SerializeField] private int bonusTempoMassimo = 1200;
    [SerializeField] private float decadimentoBonusTempoAlSecondo = 8f;

    public int PuntiPerFocolaioContenuto => puntiPerFocolaioContenuto;
    public int PuntiPerCredenziale => puntiPerCredenziale;
    public int PenalitaPerAllarme => penalitaPerAllarme;
    public int PenalitaPerDannoSubito => penalitaPerDannoSubito;
    public int PenalitaPerCredenzialeErrata => penalitaPerCredenzialeErrata;

    public int CalcolaProvvisorio(int punteggioBase, float integritaResidua)
    {
        int bonusIntegrita = Mathf.RoundToInt(integritaResidua * puntiPerIntegritaResidua);
        return Mathf.Max(0, punteggioBase + bonusIntegrita);
    }

    public int CalcolaFinale(
        MissionManager.MissionOutcome outcome,
        int punteggioProvvisorio,
        int allarmiSubiti,
        int credenzialiErrate,
        int danniSubitiArrotondati,
        float tempoMissione)
    {
        int score = punteggioProvvisorio;

        if (outcome == MissionManager.MissionOutcome.Victory)
        {
            score += bonusEstrazioneSicura;

            if (allarmiSubiti == 0)
                score += bonusStealthPerfetto;

            score += Mathf.Max(0, Mathf.RoundToInt(bonusTempoMassimo - tempoMissione * decadimentoBonusTempoAlSecondo));
        }

        score -= credenzialiErrate * penalitaPerCredenzialeErrata;
        score -= danniSubitiArrotondati * penalitaPerDannoSubito;
        return Mathf.Max(0, score);
    }
}
