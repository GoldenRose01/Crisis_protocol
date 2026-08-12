using UnityEngine;

public class OggettoTemporaleVincolato : MonoBehaviour
{
    public enum TipoEpoca { Passato = 0, Futuro = 3 }

    [Header("Identificazione")]
    [Tooltip("Imposta su Passato per permettere la raccolta. Il Futuro viene ignorato dall'input.")]
    [SerializeField] private TipoEpoca epocaAppartenenza;

    [Header("Interazione")]
    [SerializeField] private string tagGiocatore = SectorContainmentTags.Player;
    private bool giocatoreInZona = false;

    private void Update()
    {
        // Se è un oggetto del passato, il giocatore è nel trigger e preme E
        if (epocaAppartenenza == TipoEpoca.Passato && giocatoreInZona && Input.GetKeyDown(KeyCode.E))
        {
            EseguiRaccolta();
        }
    }

    private void EseguiRaccolta()
    {
        // 1. Segnala l'acquisizione al manager centrale
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.RegistraRaccolta();
        }

        // 2. Disattiva il flag del trigger per sicurezza
        giocatoreInZona = false;

        // 3. Rimuove fisicamente l'intero GameObject dalla scena
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(tagGiocatore))
        {
            giocatoreInZona = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(tagGiocatore))
        {
            giocatoreInZona = false;
        }
    }
}
