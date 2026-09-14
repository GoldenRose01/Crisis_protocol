// ============================================================================
// Crisis Protocol / Sector Containment - Interfaccia utente
// File: .\Assets\CrisisProtocol\Scripts\UI\HUDManager.cs
// Responsabilita': aggiorna HUD, menu, overlay, gauge, notifiche o schermate di supporto in base agli eventi gameplay.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine; // usa lib // riga-ok
using TMPro; // usa lib // riga-ok

// blocco: classe x roba grossa
public class HUDManager : MonoBehaviour // classe qui // riga-ok
{ // apre // riga-ok
    [Header("Riferimenti Testuali (TextMeshPro)")] // nota unity // riga-ok
    public TextMeshProUGUI testoSalute; // roba pub // riga-ok
    public TextMeshProUGUI testoMunizioni; // roba pub // riga-ok
    public TextMeshProUGUI testoMissione; // roba pub // riga-ok
    public TextMeshProUGUI testoDestabilizzazione; // roba pub // riga-ok
    public TextMeshProUGUI testoFrequenze; // roba pub // riga-ok
    public TextMeshProUGUI testoPunteggio; // roba pub // riga-ok
    public TextMeshProUGUI testoEsitoMissione; // roba pub // riga-ok

    // blocco: funzione fa cose
    private void OnEnable() // roba pub // riga-ok
    { // apre // riga-ok
        // Iscrizione agli eventi (Observer)
        SalutePlayer.OnSaluteCambiata += AggiornaSalute; // setta // riga-ok
        SparoPlayer.OnMunizioniCambiate += AggiornaMunizioni; // setta // riga-ok
        MissionManager.OnCollassoStrutturaleCambiato += AggiornaDestabilizzazione; // setta // riga-ok
        MissionManager.OnFocolaiCambiati += AggiornaFocolai; // setta // riga-ok
        MissionManager.OnCredenzialiCambiate += AggiornaFrequenze; // setta // riga-ok
        MissionManager.OnPunteggioCambiato += AggiornaPunteggio; // setta // riga-ok
        MissionManager.OnEstrazioneSbloccata += AggiornaEstrazione; // setta // riga-ok
        MissionManager.OnMissioneTerminata += AggiornaEsitoMissione; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnDisable() // roba pub // riga-ok
    { // apre // riga-ok
        // Disiscrizione per prevenire memory leak al cambio scena
        SalutePlayer.OnSaluteCambiata -= AggiornaSalute; // setta // riga-ok
        SparoPlayer.OnMunizioniCambiate -= AggiornaMunizioni; // setta // riga-ok
        MissionManager.OnCollassoStrutturaleCambiato -= AggiornaDestabilizzazione; // setta // riga-ok
        MissionManager.OnFocolaiCambiati -= AggiornaFocolai; // setta // riga-ok
        MissionManager.OnCredenzialiCambiate -= AggiornaFrequenze; // setta // riga-ok
        MissionManager.OnPunteggioCambiato -= AggiornaPunteggio; // setta // riga-ok
        MissionManager.OnEstrazioneSbloccata -= AggiornaEstrazione; // setta // riga-ok
        MissionManager.OnMissioneTerminata -= AggiornaEsitoMissione; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void AggiornaSalute(float corrente, float massima) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (testoSalute != null) // se ok // riga-ok
        { // apre // riga-ok
            testoSalute.text = $"HP: {Mathf.CeilToInt(corrente)} / {Mathf.CeilToInt(massima)}"; // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void AggiornaMunizioni(int attuali, int massime) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (testoMunizioni != null) // se ok // riga-ok
        { // apre // riga-ok
            testoMunizioni.text = $"AMMO: {attuali} / {massime}"; // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // Interfaccia pubblica chiamata dal tuo futuro GestoreMissioni
    // blocco: funzione fa cose
    public void ImpostaTestoMissione(string nuovoTesto) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (testoMissione != null) // se ok // riga-ok
        { // apre // riga-ok
            testoMissione.text = nuovoTesto; // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void AggiornaDestabilizzazione(float corrente, float massima) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (testoDestabilizzazione != null) // se ok // riga-ok
            testoDestabilizzazione.text = $"COLLASSO STRUTTURA: {Mathf.CeilToInt(corrente)}%"; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void AggiornaFocolai(int stabilizzati, int totale) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (testoMissione != null) // se ok // riga-ok
            testoMissione.text = totale > 0 // setta // riga-ok
                ? $"FOCOLAI CONTENUTI: {stabilizzati} / {totale}" // ok qua // riga-ok
                : $"FOCOLAI CONTENUTI: {stabilizzati}"; // ok qua // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void AggiornaFrequenze(int count) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (testoFrequenze != null) // se ok // riga-ok
            testoFrequenze.text = $"CREDENZIALI: {count}"; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void AggiornaPunteggio(int score) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (testoPunteggio != null) // se ok // riga-ok
            testoPunteggio.text = $"SCORE: {score}"; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void AggiornaEstrazione(bool sbloccata) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (testoMissione != null && sbloccata) // se ok // riga-ok
            testoMissione.text = "PORTELLONE SBLOCCATO: RAGGIUNGI L'USCITA"; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void AggiornaEsitoMissione(MissionManager.MissionOutcome outcome, int score, string reason) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (testoEsitoMissione == null) // se ok // riga-ok
            return; // torna val // riga-ok

        string label = outcome == MissionManager.MissionOutcome.Victory ? "MISSIONE COMPLETATA" : "MISSIONE FALLITA"; // setta // riga-ok
        testoEsitoMissione.text = $"{label}\n{reason}\nSCORE: {score}"; // setta // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
