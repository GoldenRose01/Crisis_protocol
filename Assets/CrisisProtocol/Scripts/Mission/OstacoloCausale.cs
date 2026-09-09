using UnityEngine;

/// <summary>
/// Ostacolo/anomalia legacy convertito al modello Sector Containment.
/// </summary>
public class OstacoloCausale : MonoBehaviour, IDamageable
{
    [Header("Dati Strutturali")]
    [Tooltip("Identificativo univoco: usato come firma di sicurezza e come chiave nel registro causale.")]
    public string idCausale;

    [Tooltip("Punti vita dell'ostacolo. A 0 viene distrutto e la firma registrata.")]
    public float puntiVita = 100f;

    [Header("Configurazione (legacy)")]
    [Tooltip("Campo legacy del vecchio sistema temporale. Mantenuto per compatibilita' con la scena; non piu' usato per gating del danno.")]
    public int epocaDanneggiabile = 0;

    [Header("Effetti Visivi")]
    [Tooltip("Prefab di macerie da istanziare alla distruzione (opzionale).")]
    public GameObject prefabMacerie;

    private void Start()
    {
        // Se l'ostacolo era gia' stato distrutto in precedenza, ripristina lo stato "rotto".
        if (GameManager.Instance != null && !string.IsNullOrEmpty(idCausale) &&
            GameManager.Instance.GetCausalState(idCausale))
        {
            DistruggiFisicamente(false); // niente effetti: era gia' rotto prima del caricamento
        }
    }

    /// <summary>
    /// Applica danno all'ostacolo. Alla morte registra la firma e distrugge fisicamente.
    /// </summary>
    public void SubisciDanno(float quantitaDanno)
    {
        puntiVita -= quantitaDanno;
        Debug.Log($"[OSTACOLO] '{idCausale}' colpito (epoca legacy {epocaDanneggiabile}). Vita rimanente: {puntiVita}.");

        if (puntiVita <= 0f)
        {
            if (GameManager.Instance != null && !string.IsNullOrEmpty(idCausale))
            {
                GameManager.Instance.SetCausalState(idCausale, true);
                GameManager.Instance.RegisterSecuritySignature(idCausale);
            }

            DistruggiFisicamente(true);
        }
    }

    private void DistruggiFisicamente(bool mostraEffetti)
    {
        if (mostraEffetti && prefabMacerie != null)
        {
            Instantiate(prefabMacerie, transform.position, transform.rotation);
        }

        gameObject.SetActive(false);
    }
}
