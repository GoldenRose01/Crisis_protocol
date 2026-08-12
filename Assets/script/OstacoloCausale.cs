using UnityEngine;
using GoldenCast.Legacy; // Necessario per leggere il GlobalEnvironmentManager

public class OstacoloCausale : MonoBehaviour, IDamageable
{
    [Header("Dati Strutturali")]
    public string idCausale;
    public float puntiVita = 100f;
    
    [Header("Configurazione Epoche")]
    [Tooltip("L'indice dell'epoca in cui questo muro è vulnerabile (Es: 1 = Anni 1960, dove è di legno)")]
    [SerializeField] private int epocaDanneggiabile = 1;

    [Header("Effetti Visivi")]
    public GameObject prefabMacerie; // Il modello 3D rotto da istanziare

    void Start()
    {
        // Al caricamento, controlla se l'oggetto era già stato distrutto in un loop precedente
        if (GameManager.Instance != null && GameManager.Instance.GetCausalState(idCausale))
        {
            DistruggiFisicamente(false); // Niente macerie se era già rotto prima del caricamento
        }
    }

    public void SubisciDanno(float quantitaDanno)
    {
        // VERIFICA RIGOROSA DELL'ISTANZA DEL MANAGER
        if (GlobalEnvironmentManager.Instance == null)
        {
            Debug.LogError($"[ERRORE CAUSALE] GlobalEnvironmentManager.Instance è NULL! Impossibile calcolare la coerenza temporale per l'oggetto {gameObject.name}.");
            return;
        }

        // 1. CONTROLLO DI COERENZA TEMPORALE (Risoluzione Errore CS0120)
        // Accediamo alla proprietà d'istanza tramite il Singleton .Instance
        int epocaAttuale = GlobalEnvironmentManager.Instance.EpocaCorrente;

        // Se l'epoca attuale non è quella in cui il muro è vulnerabile, blocchiamo il danno
        if (epocaAttuale != epocaDanneggiabile)
        {
            string materialeCorrente = DeterminaMaterialeDebug(epocaAttuale);
            Debug.LogWarning($"[LOGICA TEMPORALE] Il muro con ID '{idCausale}' è immune! Attualmente è fatto di [{materialeCorrente}] (Epoca {epocaAttuale}).");
            return; // Interrompe il metodo: il muro non subisce alcun danno
        }

        // 2. LOGICA DI DANNO APPLICATA
        puntiVita -= quantitaDanno;
        Debug.Log($"[LOGICA TEMPORALE] Muro colpito nell'epoca vulnerabile ({epocaDanneggiabile})! Vita rimanente: {puntiVita}");
        
        if (puntiVita <= 0)
        {
            // Registra la distruzione nel registro causale globale del GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetCausalState(idCausale, true);
            }
            DistruggiFisicamente(true); // Genera il prefabMacerie
        }
    }

    private void DistruggiFisicamente(bool mostraEffetti)
    {
        if (mostraEffetti && prefabMacerie != null)
        {
            Instantiate(prefabMacerie, transform.position, transform.rotation);
        }
        
        // Disabilita l'oggetto intero della scena
        gameObject.SetActive(false);
    }

    private string DeterminaMaterialeDebug(int epoca)
    {
        return epoca switch
        {
            0 => "Medioevo (Pietra)",
            1 => "Anni 1960 (Legno)",
            2 => "Anni 2000 (Mattoni)",
            3 => "Futuro (Metallo)",
            _ => "Sconosciuto"
        };
    }
}