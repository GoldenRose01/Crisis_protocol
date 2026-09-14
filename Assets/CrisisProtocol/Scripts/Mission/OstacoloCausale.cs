// ============================================================================
// Crisis Protocol / Sector Containment - Missione
// File: .\Assets\CrisisProtocol\Scripts\Mission\OstacoloCausale.cs
// Responsabilita': gestisce scansione, flusso missione, anomalie operative e collegamento tra interazioni di scena e stato globale.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine; // usa lib // riga-ok

/// <summary>
/// Ostacolo/anomalia legacy convertito al modello Sector Containment.
/// </summary>
// blocco: classe x roba grossa
public class OstacoloCausale : MonoBehaviour, IDamageable // classe qui // riga-ok
{ // apre // riga-ok
    [Header("Dati Strutturali")] // nota unity // riga-ok
    [Tooltip("Identificativo univoco: usato come firma di sicurezza e come chiave nel registro causale.")] // nota unity // riga-ok
    public string idCausale; // roba pub // riga-ok

    [Tooltip("Punti vita dell'ostacolo. A 0 viene distrutto e la firma registrata.")] // nota unity // riga-ok
    public float puntiVita = 100f; // roba pub // riga-ok

    [Header("Configurazione (legacy)")] // nota unity // riga-ok
    [Tooltip("Campo legacy del vecchio sistema temporale. Mantenuto per compatibilita' con la scena; non piu' usato per gating del danno.")] // nota unity // riga-ok
    public int epocaDanneggiabile = 0; // roba pub // riga-ok

    [Header("Effetti Visivi")] // nota unity // riga-ok
    [Tooltip("Prefab di macerie da istanziare alla distruzione (opzionale).")] // nota unity // riga-ok
    public GameObject prefabMacerie; // roba pub // riga-ok

    // blocco: funzione fa cose
    private void Start() // roba pub // riga-ok
    { // apre // riga-ok
        // Se l'ostacolo era gia' stato distrutto in precedenza, ripristina lo stato "rotto".
        // blocco: controlla se va
        if (GameManager.Instance != null && !string.IsNullOrEmpty(idCausale) && // se ok // riga-ok
            GameManager.Instance.GetCausalState(idCausale)) // chiama // riga-ok
        { // apre // riga-ok
            DistruggiFisicamente(false); // niente effetti: era gia' rotto prima del caricamento // ok qua // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    /// <summary>
    /// Applica danno all'ostacolo. Alla morte registra la firma e distrugge fisicamente.
    /// </summary>
    // blocco: funzione fa cose
    public void SubisciDanno(float quantitaDanno) // roba pub // riga-ok
    { // apre // riga-ok
        puntiVita -= quantitaDanno; // setta // riga-ok
        Debug.Log($"[OSTACOLO] '{idCausale}' colpito (epoca legacy {epocaDanneggiabile}). Vita rimanente: {puntiVita}."); // logga // riga-ok

        // blocco: controlla se va
        if (puntiVita <= 0f) // se ok // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (GameManager.Instance != null && !string.IsNullOrEmpty(idCausale)) // se ok // riga-ok
            { // apre // riga-ok
                GameManager.Instance.SetCausalState(idCausale, true); // chiama // riga-ok
                GameManager.Instance.RegisterSecuritySignature(idCausale); // chiama // riga-ok
            } // chiude // riga-ok

            DistruggiFisicamente(true); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void DistruggiFisicamente(bool mostraEffetti) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (mostraEffetti && prefabMacerie != null) // se ok // riga-ok
        { // apre // riga-ok
            Instantiate(prefabMacerie, transform.position, transform.rotation); // chiama // riga-ok
        } // chiude // riga-ok

        gameObject.SetActive(false); // chiama // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
