using UnityEngine;
using UnityEngine.InputSystem;
using GoldenCast.UI;

public class AttaccoPlayer : MonoBehaviour
{
    [Header("Configurazione Attacco")]
    public float dannoAttaccoFrontale = 25f;
    public float raggioAttacco = 1.8f;
    public float cadenzaAttacco = 0.6f;
    private float timerProssimoAttacco = 0f;

    [Header("Rilevamento Bersagli")]
    public LayerMask layerNemici;
    [Tooltip("Punto di origine del colpo (es. pugno o spada). Se vuoto, usa l'area frontale al personaggio.")]
    public Transform puntoAttaccoMelee;

    [Header("Integrazione Animatore")]
    public Animator animatorePersonaggio;
    public string triggerAttacco = "Attack";

    void Update()
    {
        if (ModalUIState.IsModalOpen)
            return;

        if (timerProssimoAttacco > 0)
        {
            timerProssimoAttacco -= Time.deltaTime;
        }

        // Rilevamento Input d'attacco: click sinistro del mouse o tasto F sulla tastiera
        bool richiedeAttacco = false;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) richiedeAttacco = true;
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) richiedeAttacco = true;

        if (richiedeAttacco && timerProssimoAttacco <= 0)
        {
            EseguiColpoMischia();
        }
    }

    private void EseguiColpoMischia()
    {
        timerProssimoAttacco = cadenzaAttacco;

        // Attiva il trigger d'attacco nell'animatore
        if (animatorePersonaggio != null)
        {
            animatorePersonaggio.SetTrigger(triggerAttacco);
        }

        // Calcola l'origine dell'attacco (frontale rispetto al giocatore se non è assegnato un punto preciso)
        Vector3 origineAttacco = puntoAttaccoMelee != null 
            ? puntoAttaccoMelee.position 
            : transform.position + transform.forward * 1.0f + Vector3.up * 1.0f;

        Debug.Log("<color=cyan>[ATTACCO] Sferrato colpo in mischia!</color>");

        // Rileva tutti i collider entro la sfera d'attacco che appartengono al layer dei nemici
        Collider[] colpiti = Physics.OverlapSphere(origineAttacco, raggioAttacco, layerNemici);

        // FALLBACK DI SICUREZZA: Se non viene rilevato alcun nemico, eseguiamo una scansione globale senza filtro layer.
        // Questo evita problemi se l'utente si è dimenticato di configurare correttamente i Layer nell'Inspector di Unity.
        if (colpiti.Length == 0)
        {
            colpiti = Physics.OverlapSphere(origineAttacco, raggioAttacco);
        }

        foreach (Collider col in colpiti)
        {
            // Evita di colpire se stessi o qualsiasi parte (ossa, figli, mesh) del proprio personaggio
            if (col.transform.root == transform.root) continue;

            // Cerca se l'entità colpita o i suoi genitori implementano IDamageable (come la Guardia o un ostacolo)
            IDamageable bersaglio = col.GetComponent<IDamageable>();
            if (bersaglio == null)
            {
                bersaglio = col.GetComponentInParent<IDamageable>();
            }

            if (bersaglio != null)
            {
                // Se colpiamo una Guardia alle spalle, il Takedown furtivo avverrà automaticamente nel metodo SubisciDanno della guardia!
                bersaglio.SubisciDanno(dannoAttaccoFrontale);
                Debug.Log($"<b>[COMBAT]</b> Colpito con successo: {col.gameObject.name}! Inflitti {dannoAttaccoFrontale} HP di danno.");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visualizza il raggio d'azione dell'attacco nell'editor di Unity
        Gizmos.color = Color.red;
        Vector3 origineAttacco = puntoAttaccoMelee != null 
            ? puntoAttaccoMelee.position 
            : transform.position + transform.forward * 1.0f + Vector3.up * 1.0f;
        Gizmos.DrawWireSphere(origineAttacco, raggioAttacco);
    }
}
