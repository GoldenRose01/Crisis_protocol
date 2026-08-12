using System;
using UnityEngine;

[Serializable]
public class StructuralCollapseSettings
{
    [SerializeField, Range(0f, 100f)] private float collassoIniziale = 0f;
    [SerializeField, Range(1f, 100f)] private float collassoMassimo = 100f;
    [SerializeField] private float puntiCollassoAlSecondo = 0.45f;
    [SerializeField] private float penalitaCredenzialeErrata = 6f;
    [SerializeField] private float penalitaAllarme = 4f;
    [SerializeField] private float penalitaDannoOperatore = 0.12f;
    [SerializeField] private float recuperoPerFocolaioContenuto = 8f;

    public float CollassoIniziale => collassoIniziale;
    public float CollassoMassimo => collassoMassimo;
    public float IncrementoPerSecondo => puntiCollassoAlSecondo;
    public float PenalitaCredenzialeErrata => penalitaCredenzialeErrata;
    public float PenalitaAllarme => penalitaAllarme;
    public float RecuperoPerFocolaioContenuto => recuperoPerFocolaioContenuto;

    public float IntegritaResidua(float collassoCorrente)
    {
        return Mathf.Max(0f, collassoMassimo - collassoCorrente);
    }

    public float CalcolaPenalitaDanno(float quantitaDanno)
    {
        return Mathf.Max(0f, quantitaDanno) * penalitaDannoOperatore;
    }

    public float Clamp(float value)
    {
        return Mathf.Clamp(value, 0f, collassoMassimo);
    }
}
