// ============================================================================
// Crisis Protocol / Sector Containment - Missione
// File: .\Assets\CrisisProtocol\Scripts\Mission\GestoreFlusso.cs
// Responsabilita': gestisce scansione, flusso missione, anomalie operative e collegamento tra interazioni di scena e stato globale.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine; // usa lib // riga-ok

// blocco: classe x roba grossa
public class GestoreFlusso : MonoBehaviour // classe qui // riga-ok
{ // apre // riga-ok
    [Header("Interfaccia e Tempi")] // nota unity // riga-ok
    [Tooltip("Trascina qui il pannello UI del Game Over (disattivato di default)")] // nota unity // riga-ok
    public GameObject pannelloGameOver; // roba pub // riga-ok

    [Tooltip("Secondi di attesa prima di ricaricare la scena")] // nota unity // riga-ok
    public float ritardoRiavvio = 2f; // roba pub // riga-ok

    private bool gameOverInnescato; // roba pub // riga-ok

    // blocco: funzione fa cose
    private void OnEnable() // roba pub // riga-ok
    { // apre // riga-ok
        // Ci mettiamo in ascolto della morte del giocatore
        SalutePlayer.OnPlayerMorto += InnescaGameOver; // setta // riga-ok
        MissionManager.OnMissioneTerminata += GestisciFineMissione; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnDisable() // roba pub // riga-ok
    { // apre // riga-ok
        SalutePlayer.OnPlayerMorto -= InnescaGameOver; // setta // riga-ok
        MissionManager.OnMissioneTerminata -= GestisciFineMissione; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void Start() // roba pub // riga-ok
    { // apre // riga-ok
        // Assicuriamoci che il Game Over non sia visibile all'avvio
        // blocco: controlla se va
        if (pannelloGameOver != null) // se ok // riga-ok
        { // apre // riga-ok
            pannelloGameOver.SetActive(false); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void InnescaGameOver() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (gameOverInnescato) // se ok // riga-ok
            return; // torna val // riga-ok

        gameOverInnescato = true; // setta // riga-ok

        // blocco: controlla se va
        if (pannelloGameOver != null) // se ok // riga-ok
        { // apre // riga-ok
            pannelloGameOver.SetActive(true); // chiama // riga-ok
        } // chiude // riga-ok
        DeathScreenController.ShowAndReloadCurrentScene(ritardoRiavvio); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void GestisciFineMissione(MissionManager.MissionOutcome outcome, int score, string reason) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (outcome != MissionManager.MissionOutcome.Defeat) // se ok // riga-ok
            return; // torna val // riga-ok

        InnescaGameOver(); // chiama // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
