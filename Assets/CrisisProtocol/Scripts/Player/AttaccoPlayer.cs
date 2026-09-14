// ============================================================================
// Crisis Protocol / Sector Containment - Player
// File: .\Assets\CrisisProtocol\Scripts\Player\AttaccoPlayer.cs
// Responsabilita': gestisce input, movimento, combattimento, interazione, salute o strumenti controllati dal giocatore.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine; // usa lib // riga-ok
using UnityEngine.InputSystem; // usa lib // riga-ok
using CrisisProtocol.UI; // usa lib // riga-ok

// blocco: classe x roba grossa
public class AttaccoPlayer : MonoBehaviour // classe qui // riga-ok
{ // apre // riga-ok
    [Header("Configurazione Attacco")] // nota unity // riga-ok
    public float dannoAttaccoFrontale = 25f; // roba pub // riga-ok
    public float raggioAttacco = 1.8f; // roba pub // riga-ok
    public float cadenzaAttacco = 0.75f; // roba pub // riga-ok
    [Tooltip("Secondi di attesa dall'avvio dell'animazione per applicare il danno al termine dello swing/colpo.")] // nota unity // riga-ok
    public float ritardoDannoFineAnimazione = 0.45f; // roba pub // riga-ok
    private float timerProssimoAttacco = 0f; // roba pub // riga-ok
    private Coroutine coroutineAttacco; // roba pub // riga-ok

    [Header("Rilevamento Bersagli")] // nota unity // riga-ok
    public LayerMask layerNemici; // roba pub // riga-ok
    [Tooltip("Punto di origine del colpo (es. pugno o spada). Se vuoto, usa l'area frontale al personaggio.")] // nota unity // riga-ok
    public Transform puntoAttaccoMelee; // roba pub // riga-ok

    [Header("Integrazione Animatore")] // nota unity // riga-ok
    public Animator animatorePersonaggio; // roba pub // riga-ok
    public string triggerAttacco = "Attack"; // roba pub // riga-ok

    [Header("Audio")] // nota unity // riga-ok
    [Tooltip("Suono di fendente / swoosh durante l'attacco melee.")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoAttacco; // ok qua // riga-ok
    [Tooltip("Suono di impatto quando si colpisce un bersaglio.")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoColpoASegno; // ok qua // riga-ok
    [Range(0f, 1f)] [SerializeField] private float volumeAudio = 0.9f; // setta // riga-ok

    private AudioSource audioSource; // roba pub // riga-ok

    // blocco: funzione fa cose
    private void Awake() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (animatorePersonaggio == null) // se ok // riga-ok
            animatorePersonaggio = GetComponentInChildren<Animator>(); // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnDisable() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (coroutineAttacco != null) // se ok // riga-ok
        { // apre // riga-ok
            StopCoroutine(coroutineAttacco); // corutina // riga-ok
            coroutineAttacco = null; // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void InizializzaAudioSource() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (audioSource == null) // se ok // riga-ok
            audioSource = GetComponent<AudioSource>(); // setta // riga-ok

        // blocco: controlla se va
        if (audioSource == null) // se ok // riga-ok
        { // apre // riga-ok
            audioSource = gameObject.AddComponent<AudioSource>(); // setta // riga-ok
            audioSource.playOnAwake = false; // setta // riga-ok
            audioSource.spatialBlend = 1.0f; // 3D // setta // riga-ok
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic; // setta // riga-ok
            audioSource.minDistance = 1.5f; // setta // riga-ok
            audioSource.maxDistance = 18.0f; // setta // riga-ok
            audioSource.dopplerLevel = 0f; // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void RiproduciSuono(AudioClip clip, float volumeMoltiplicatore = 1.0f) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (clip == null) return; // se ok // riga-ok
        InizializzaAudioSource(); // chiama // riga-ok
        // blocco: controlla se va
        if (audioSource != null) // se ok // riga-ok
        { // apre // riga-ok
            audioSource.pitch = Random.Range(0.95f, 1.05f); // setta // riga-ok
            audioSource.PlayOneShot(clip, volumeAudio * volumeMoltiplicatore); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    void Update() // chiama // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (ModalUIState.IsModalOpen) // se ok // riga-ok
            return; // torna val // riga-ok

        // blocco: controlla se va
        if (timerProssimoAttacco > 0) // se ok // riga-ok
        { // apre // riga-ok
            timerProssimoAttacco -= Time.deltaTime; // setta // riga-ok
        } // chiude // riga-ok

        // Rilevamento Input d'attacco: click sinistro del mouse o tasto F sulla tastiera
        bool richiedeAttacco = false; // setta // riga-ok
        // blocco: controlla se va
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) richiedeAttacco = true; // se ok // riga-ok
        // blocco: controlla se va
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) richiedeAttacco = true; // se ok // riga-ok

        // blocco: controlla se va
        if (richiedeAttacco && timerProssimoAttacco <= 0) // se ok // riga-ok
        { // apre // riga-ok
            EseguiColpoMischia(); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void EseguiColpoMischia() // roba pub // riga-ok
    { // apre // riga-ok
        timerProssimoAttacco = cadenzaAttacco; // setta // riga-ok
        RiproduciSuono(suonoAttacco); // chiama // riga-ok

        // Attiva il trigger d'attacco nell'animatore
        // blocco: controlla se va
        if (animatorePersonaggio != null) // se ok // riga-ok
        { // apre // riga-ok
            animatorePersonaggio.ResetTrigger(triggerAttacco); // chiama // riga-ok
            animatorePersonaggio.SetTrigger(triggerAttacco); // chiama // riga-ok
        } // chiude // riga-ok

        Debug.Log("<color=cyan>[ATTACCO PLAYER] Animazione avviata! Danno programmato a fine colpo.</color>"); // logga // riga-ok

        // blocco: controlla se va
        if (coroutineAttacco != null) // se ok // riga-ok
            StopCoroutine(coroutineAttacco); // corutina // riga-ok

        coroutineAttacco = StartCoroutine(EseguiDannoAlTermineAnimazione(ritardoDannoFineAnimazione)); // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private System.Collections.IEnumerator EseguiDannoAlTermineAnimazione(float ritardo) // roba pub // riga-ok
    { // apre // riga-ok
        yield return new WaitForSeconds(ritardo); // aspetta // riga-ok

        // Calcola l'origine dell'attacco (frontale rispetto al giocatore se non è assegnato un punto preciso)
        Vector3 origineAttacco = puntoAttaccoMelee != null  // setta // riga-ok
            ? puntoAttaccoMelee.position  // ok qua // riga-ok
            : transform.position + transform.forward * 1.1f + Vector3.up * 1.0f; // ok qua // riga-ok

        // Rileva tutti i collider entro la sfera d'attacco che appartengono al layer dei nemici
        Collider[] colpiti = Physics.OverlapSphere(origineAttacco, raggioAttacco, layerNemici); // setta // riga-ok

        // FALLBACK DI SICUREZZA: Se non viene rilevato alcun nemico, scansione globale
        // blocco: controlla se va
        if (colpiti.Length == 0) // se ok // riga-ok
        { // apre // riga-ok
            colpiti = Physics.OverlapSphere(origineAttacco, raggioAttacco); // setta // riga-ok
        } // chiude // riga-ok

        bool haColpitoBersaglio = false; // setta // riga-ok

        // blocco: gira piu volte
        foreach (Collider col in colpiti) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (col.transform.root == transform.root) continue; // se ok // riga-ok

            IDamageable bersaglio = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>() ?? col.GetComponentInChildren<IDamageable>(); // setta // riga-ok

            // blocco: controlla se va
            if (bersaglio != null) // se ok // riga-ok
            { // apre // riga-ok
                bersaglio.SubisciDanno(dannoAttaccoFrontale); // chiama // riga-ok
                haColpitoBersaglio = true; // setta // riga-ok
                Debug.Log($"<b>[COMBAT]</b> Colpito con successo: {col.gameObject.name}! Inflitti {dannoAttaccoFrontale} HP di danno."); // logga // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (haColpitoBersaglio) // se ok // riga-ok
        { // apre // riga-ok
            RiproduciSuono(suonoColpoASegno); // chiama // riga-ok
        } // chiude // riga-ok

        coroutineAttacco = null; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnDrawGizmosSelected() // roba pub // riga-ok
    { // apre // riga-ok
        // Visualizza il raggio d'azione dell'attacco nell'editor di Unity
        Gizmos.color = Color.red; // setta // riga-ok
        Vector3 origineAttacco = puntoAttaccoMelee != null  // setta // riga-ok
            ? puntoAttaccoMelee.position  // ok qua // riga-ok
            : transform.position + transform.forward * 1.0f + Vector3.up * 1.0f; // ok qua // riga-ok
        Gizmos.DrawWireSphere(origineAttacco, raggioAttacco); // chiama // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
