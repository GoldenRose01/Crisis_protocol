using UnityEngine;
using GoldenCast.Legacy;

[RequireComponent(typeof(Collider))]
public class LeylineTrigger : MonoBehaviour
{
    [Header("Configurazione Portale")]
    [Tooltip("L'ID del Tag univoco richiesto per attivare questa Leyline.")]
    [SerializeField] private string requiredTagID = "TAG_001";
    
    [Tooltip("L'epoca verso cui questa Leyline ti porta (0 = Futuro, 1 = Passato, ecc.)")]
    [SerializeField] private int epocaDiDestinazione = 1;

    [Tooltip("Tasto di attivazione per il teletrasporto.")]
    [SerializeField] private KeyCode tastoAttivazione = KeyCode.E;

    // Controllo Anti-Rimbalzo globale statico per evitare loop infiniti
    private static float orarioUltimoSalto = -10f;
    private const float TEMPO_DI_COOLDOWN = 2.0f;

    // Stato locale
    private bool giocatoreInArea = false;
    private bool istanzaAutorizzataAlRitorno = false;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void Update()
    {
        // Viaggia solo se il giocatore preme attivamente il tasto di interazione (E)
        if (giocatoreInArea && Input.GetKeyDown(tastoAttivazione))
        {
            TentaTransizioneTemporale();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(SectorContainmentTags.Player))
        {
            giocatoreInArea = true;
            
            // Diagnostica contestuale della UI basata sull'epoca GLOBALE effettiva
            string epocaTarget = (GlobalEnvironmentManager.Instance != null && GlobalEnvironmentManager.Instance.EpocaCorrente == 0) 
                ? "PASSATO" : "FUTURO";
                
            Debug.Log($"<color=yellow>[LEYLINE]</color> Sei sopra la Leyline <b>{gameObject.name}</b>. Premi <b>{tastoAttivazione}</b> per viaggiare verso il <b>{epocaTarget}</b>.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(SectorContainmentTags.Player))
        {
            giocatoreInArea = false;
        }
    }

    private void TentaTransizioneTemporale()
    {
        // Controllo Cooldown anti-rimbalzo
        if (Time.time < orarioUltimoSalto + TEMPO_DI_COOLDOWN)
        {
            Debug.LogWarning("[LEYLINE] Cooldown attivo. Attendi un momento prima di saltare di nuovo.");
            return;
        }

        // Controllo centralizzato e validazione di sicurezza dei Singleton
        if (GlobalEnvironmentManager.Instance == null || GameManager.Instance == null)
        {
            Debug.LogError("[LEYLINE ERROR] Sistemi globali mancanti! Assicurati che GameManager e GlobalEnvironmentManager siano attivi.");
            return;
        }

        // Legge l'epoca in cui si trova il mondo in questo momento
        int epocaAttuale = GlobalEnvironmentManager.Instance.EpocaCorrente;

        if (epocaAttuale == 0)
        {
            // Se siamo nel Futuro (0), proviamo ad andare nel Passato
            GestisciSaltoVersoPassato();
        }
        else if (epocaAttuale == epocaDiDestinazione)
        {
            // Se siamo già nell'epoca passata, valutiamo se possiamo tornare indietro
            GestisciRitornoVersoFuturo();
        }
        else
        {
            Debug.LogWarning($"[LEYLINE] Questa Leyline non è configurata per funzionare nell'epoca corrente ({epocaAttuale}).");
        }
    }

    private void GestisciSaltoVersoPassato()
    {
        // Controllo sblocco (Accetta sia lo storico che l'ultimo tag estratto a runtime)
        if (GameManager.Instance.IsTagUnlocked(requiredTagID) || GameManager.Instance.currentTagID == requiredTagID)
        {
            GameManager.Instance.ApriVarcoRitorno(requiredTagID);
            Debug.Log($"<color=cyan>[LEYLINE]</color> ANDATA OK: Varco autorizzato nel GameManager per il canale <b>{requiredTagID}</b>. Viaggio verso l'epoca {epocaDiDestinazione}.");
            
            // Autorizza localmente questa specifica istanza fisica al viaggio di ritorno
            istanzaAutorizzataAlRitorno = true;
            
            EseguiTransizione(epocaDiDestinazione); 
        }
        else
        {
            Debug.LogWarning($"[LEYLINE NEGATA] Non possiedi il tag richiesto '{requiredTagID}' per andare nel passato.");
        }
    }

    private void GestisciRitornoVersoFuturo()
    {
        // 1. Controllo di proprietà: È lo stesso trigger dell'andata?
        if (istanzaAutorizzataAlRitorno)
        {
            // 2. Chiediamo alla memoria volatile del GameManager se il canale è valido ed esistente
            if (GameManager.Instance.ConsumaVarcoRitorno(requiredTagID))
            {
                Debug.Log($"<color=lime>[LEYLINE]</color> RITORNO OK: Canale <b>{requiredTagID}</b> verificato. Ritorno al futuro (Epoca 0).");
                
                istanzaAutorizzataAlRitorno = false;
                EseguiTransizione(0); // Forza il teletrasporto geometrico all'Epoca 0
            }
            else
            {
                istanzaAutorizzataAlRitorno = false;
                Debug.LogError($"[LEYLINE ERROR] Coerenza RAM compromessa per il canale '{requiredTagID}'.");
            }
        }
        else
        {
            Debug.LogWarning($"[LEYLINE NEGATA] Non hai usato questa specifica Leyline per viaggiare nel passato. Accesso di ritorno negato!");
        }
    }

    private void EseguiTransizione(int targetEpoca)
    {
        orarioUltimoSalto = Time.time;
        giocatoreInArea = false; 
        
        // Ordina al manager dell'ambiente di spegnere/accendere i macro-contenitori delle epoche
        GlobalEnvironmentManager.Instance.EseguiSaltoTemporale(targetEpoca);
    }
}
