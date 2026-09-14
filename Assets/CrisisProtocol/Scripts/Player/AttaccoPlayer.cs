// ============================================================================
// Crisis Protocol / Sector Containment - Player
// File: .\Assets\CrisisProtocol\Scripts\Player\AttaccoPlayer.cs
// Responsabilita': gestisce input, movimento, combattimento, interazione, salute o strumenti controllati dal giocatore.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine;
using UnityEngine.InputSystem;
using CrisisProtocol.UI;

public class AttaccoPlayer : MonoBehaviour
{
    [Header("Configurazione Attacco")]
    public float dannoAttaccoFrontale = 25f;
    public float raggioAttacco = 1.8f;
    public float cadenzaAttacco = 0.75f;
    [Tooltip("Secondi di attesa dall'avvio dell'animazione per applicare il danno al termine dello swing/colpo.")]
    public float ritardoDannoFineAnimazione = 0.45f;
    private float timerProssimoAttacco = 0f;
    private Coroutine coroutineAttacco;

    [Header("Rilevamento Bersagli")]
    public LayerMask layerNemici;
    [Tooltip("Punto di origine del colpo (es. pugno o spada). Se vuoto, usa l'area frontale al personaggio.")]
    public Transform puntoAttaccoMelee;

    [Header("Integrazione Animatore")]
    public Animator animatorePersonaggio;
    public string triggerAttacco = "Attack";

    [Header("Audio")]
    [Tooltip("Suono di fendente / swoosh durante l'attacco melee.")]
    [SerializeField] private AudioClip suonoAttacco;
    [Tooltip("Suono di impatto quando si colpisce un bersaglio.")]
    [SerializeField] private AudioClip suonoColpoASegno;
    [Range(0f, 1f)] [SerializeField] private float volumeAudio = 0.9f;

    private AudioSource audioSource;

    private void Awake()
    {
        if (animatorePersonaggio == null)
            animatorePersonaggio = GetComponentInChildren<Animator>();
    }

    private void OnDisable()
    {
        if (coroutineAttacco != null)
        {
            StopCoroutine(coroutineAttacco);
            coroutineAttacco = null;
        }
    }

    private void InizializzaAudioSource()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f; // 3D
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.minDistance = 1.5f;
            audioSource.maxDistance = 18.0f;
            audioSource.dopplerLevel = 0f;
        }
    }

    public void RiproduciSuono(AudioClip clip, float volumeMoltiplicatore = 1.0f)
    {
        if (clip == null) return;
        InizializzaAudioSource();
        if (audioSource != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(clip, volumeAudio * volumeMoltiplicatore);
        }
    }

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
        RiproduciSuono(suonoAttacco);

        // Attiva il trigger d'attacco nell'animatore
        if (animatorePersonaggio != null)
        {
            animatorePersonaggio.ResetTrigger(triggerAttacco);
            animatorePersonaggio.SetTrigger(triggerAttacco);
        }

        Debug.Log("<color=cyan>[ATTACCO PLAYER] Animazione avviata! Danno programmato a fine colpo.</color>");

        if (coroutineAttacco != null)
            StopCoroutine(coroutineAttacco);

        coroutineAttacco = StartCoroutine(EseguiDannoAlTermineAnimazione(ritardoDannoFineAnimazione));
    }

    private System.Collections.IEnumerator EseguiDannoAlTermineAnimazione(float ritardo)
    {
        yield return new WaitForSeconds(ritardo);

        // Calcola l'origine dell'attacco (frontale rispetto al giocatore se non è assegnato un punto preciso)
        Vector3 origineAttacco = puntoAttaccoMelee != null 
            ? puntoAttaccoMelee.position 
            : transform.position + transform.forward * 1.1f + Vector3.up * 1.0f;

        // Rileva tutti i collider entro la sfera d'attacco che appartengono al layer dei nemici
        Collider[] colpiti = Physics.OverlapSphere(origineAttacco, raggioAttacco, layerNemici);

        // FALLBACK DI SICUREZZA: Se non viene rilevato alcun nemico, scansione globale
        if (colpiti.Length == 0)
        {
            colpiti = Physics.OverlapSphere(origineAttacco, raggioAttacco);
        }

        bool haColpitoBersaglio = false;

        foreach (Collider col in colpiti)
        {
            if (col.transform.root == transform.root) continue;

            IDamageable bersaglio = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>() ?? col.GetComponentInChildren<IDamageable>();

            if (bersaglio != null)
            {
                bersaglio.SubisciDanno(dannoAttaccoFrontale);
                haColpitoBersaglio = true;
                Debug.Log($"<b>[COMBAT]</b> Colpito con successo: {col.gameObject.name}! Inflitti {dannoAttaccoFrontale} HP di danno.");
            }
        }

        if (haColpitoBersaglio)
        {
            RiproduciSuono(suonoColpoASegno);
        }

        coroutineAttacco = null;
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
