// ============================================================================
// Crisis Protocol / Sector Containment - Missione e contenimento
// File: .\Assets\CrisisProtocol\Scripts\Mission\SectorEmergency\StructuralCollapseSettings.cs
// Responsabilita': modella credenziali, focolai, portelloni, hazard o parametri di bilanciamento del loop emergenza -> contenimento -> estrazione.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using System; // usa lib // riga-ok
using UnityEngine; // usa lib // riga-ok

[Serializable] // nota unity // riga-ok
// blocco: classe x roba grossa
public class StructuralCollapseSettings // classe qui // riga-ok
{ // apre // riga-ok
    [SerializeField, Range(0f, 100f)] private float collassoIniziale = 0f; // setta // riga-ok
    [SerializeField, Range(1f, 100f)] private float collassoMassimo = 100f; // setta // riga-ok
    [SerializeField] private float puntiCollassoAlSecondo = 0.45f; // setta // riga-ok
    [SerializeField] private float penalitaCredenzialeErrata = 6f; // setta // riga-ok
    [SerializeField] private float penalitaAllarme = 4f; // setta // riga-ok
    [SerializeField] private float penalitaDannoOperatore = 0.12f; // setta // riga-ok
    [SerializeField] private float recuperoPerFocolaioContenuto = 8f; // setta // riga-ok

    public float CollassoIniziale => collassoIniziale; // roba pub // riga-ok
    public float CollassoMassimo => collassoMassimo; // roba pub // riga-ok
    public float IncrementoPerSecondo => puntiCollassoAlSecondo; // roba pub // riga-ok
    public float PenalitaCredenzialeErrata => penalitaCredenzialeErrata; // roba pub // riga-ok
    public float PenalitaAllarme => penalitaAllarme; // roba pub // riga-ok
    public float RecuperoPerFocolaioContenuto => recuperoPerFocolaioContenuto; // roba pub // riga-ok

    // blocco: funzione fa cose
    public float IntegritaResidua(float collassoCorrente) // roba pub // riga-ok
    { // apre // riga-ok
        return Mathf.Max(0f, collassoMassimo - collassoCorrente); // torna val // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public float CalcolaPenalitaDanno(float quantitaDanno) // roba pub // riga-ok
    { // apre // riga-ok
        return Mathf.Max(0f, quantitaDanno) * penalitaDannoOperatore; // torna val // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public float Clamp(float value) // roba pub // riga-ok
    { // apre // riga-ok
        return Mathf.Clamp(value, 0f, collassoMassimo); // torna val // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
