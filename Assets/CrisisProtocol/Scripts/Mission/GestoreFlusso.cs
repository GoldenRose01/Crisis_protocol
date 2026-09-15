// ============================================================================
// Crisis Protocol / Sector Containment - Missione
// File: .\Assets\CrisisProtocol\Scripts\Mission\GestoreFlusso.cs
// Responsabilita': gestisce scansione, flusso missione, anomalie operative e collegamento tra interazioni di scena e stato globale.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine;
public class GestoreFlusso : MonoBehaviour
{
    [Header("Interfaccia e Tempi")]
    [Tooltip("Trascina qui il pannello UI del Game Over (disattivato di default)")]
    public GameObject pannelloGameOver;
    [Tooltip("Secondi di attesa prima di ricaricare la scena")]
    public float ritardoRiavvio = 2f;
    private bool gameOverInnescato;
    private void OnEnable()
    {
        // Ci mettiamo in ascolto della morte del giocatore
        SalutePlayer.OnPlayerMorto += InnescaGameOver;
        MissionManager.OnMissioneTerminata += GestisciFineMissione;
    }
    private void OnDisable()
    {
        SalutePlayer.OnPlayerMorto -= InnescaGameOver;
        MissionManager.OnMissioneTerminata -= GestisciFineMissione;
    }
    private void Start()
    {
        // Assicuriamoci che il Game Over non sia visibile all'avvio
        if (pannelloGameOver != null)
        {
            pannelloGameOver.SetActive(false);
        }
    }
    private void InnescaGameOver()
    {
        if (gameOverInnescato)
            return;
        gameOverInnescato = true;
        if (pannelloGameOver != null)
        {
            pannelloGameOver.SetActive(true);
        }
        DeathScreenController.ShowAndReloadCurrentScene(ritardoRiavvio);
    }
    private void GestisciFineMissione(MissionManager.MissionOutcome outcome, int score, string reason)
    {
        if (outcome != MissionManager.MissionOutcome.Defeat)
            return;
        InnescaGameOver();
    }
}