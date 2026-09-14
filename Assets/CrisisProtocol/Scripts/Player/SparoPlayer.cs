// ============================================================================
// Crisis Protocol / Sector Containment - Player
// File: .\Assets\CrisisProtocol\Scripts\Player\SparoPlayer.cs
// Responsabilita': gestisce input, movimento, combattimento, interazione, salute o strumenti controllati dal giocatore.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine; // usa lib // riga-ok
using UnityEngine.InputSystem; // usa lib // riga-ok
using System; // usa lib // riga-ok
using CrisisProtocol.UI; // usa lib // riga-ok
// blocco: classe x roba grossa
public class SparoPlayer : MonoBehaviour // classe qui // riga-ok
{ // apre // riga-ok
    [Header("Balistica e Danni")] // nota unity // riga-ok
    public float dannoCorpo = 50f; // roba pub // riga-ok
    public float dannoTesta = 100f; // roba pub // riga-ok
    public float gittataSparo = 150f; // roba pub // riga-ok

    [Header("Munizioni")] // nota unity // riga-ok
    public int proiettiliMassimi = 5; // roba pub // riga-ok
    private int proiettiliAttuali; // roba pub // riga-ok

    [Header("Acustica e Stealth")] // nota unity // riga-ok
    public float raggioRumore = 25f; // roba pub // riga-ok
    public LayerMask layerNemici; // roba pub // riga-ok

    [Header("Riferimenti")] // nota unity // riga-ok
    [Tooltip("Trascina qui il componente Camera principale")] // nota unity // riga-ok
    public Camera telecameraPrincipale; // roba pub // riga-ok
    public ParticleSystem particellareSparo; // roba pub // riga-ok
    public static event Action<int, int> OnMunizioniCambiate; // roba pub // riga-ok

    [Header("Audio")] // nota unity // riga-ok
    [Tooltip("Suono di sparo dell'arma.")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoSparo; // ok qua // riga-ok
    [Tooltip("Suono di caricatore vuoto (clic a vuoto).")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoVuoto; // ok qua // riga-ok
    [Range(0f, 1f)] [SerializeField] private float volumeAudio = 1.0f; // setta // riga-ok

    private AudioSource audioSource; // roba pub // riga-ok

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
            audioSource.minDistance = 2.0f; // setta // riga-ok
            audioSource.maxDistance = 25.0f; // setta // riga-ok
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
            audioSource.pitch = UnityEngine.Random.Range(0.96f, 1.04f); // setta // riga-ok
            audioSource.PlayOneShot(clip, volumeAudio * volumeMoltiplicatore); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok
    
    void Update() // chiama // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (ModalUIState.IsModalOpen) // se ok // riga-ok
            return; // torna val // riga-ok

        // Sparo con il Clic Sinistro
        // blocco: controlla se va
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) // se ok // riga-ok
        { // apre // riga-ok
            TentaSparo(); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    void Start() // chiama // riga-ok
    { // apre // riga-ok
        InizializzaAudioSource(); // chiama // riga-ok
        proiettiliAttuali = proiettiliMassimi; // setta // riga-ok
        
        // blocco: controlla se va
        if (telecameraPrincipale == null)  // se ok // riga-ok
        { // apre // riga-ok
            telecameraPrincipale = Camera.main; // setta // riga-ok
        } // chiude // riga-ok
        
        // Inizializza l'HUD all'avvio
        OnMunizioniCambiate?.Invoke(proiettiliAttuali, proiettiliMassimi); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void TentaSparo() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (proiettiliAttuali > 0) // se ok // riga-ok
        { // apre // riga-ok
            proiettiliAttuali--; // ok qua // riga-ok
            RiproduciSuono(suonoSparo); // chiama // riga-ok
            Debug.Log($"<color=cyan>[ARMA] Colpo esploso! Munizioni rimanenti: {proiettiliAttuali}/{proiettiliMassimi}</color>"); // logga // riga-ok
            
            // Aggiorna l'HUD
            OnMunizioniCambiate?.Invoke(proiettiliAttuali, proiettiliMassimi); // chiama // riga-ok
            
            // blocco: controlla se va
            if (particellareSparo != null) particellareSparo.Play(); // se ok // riga-ok

            CalcolaTraiettoria(); // chiama // riga-ok
            GeneraRumoreSparo(); // chiama // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            RiproduciSuono(suonoVuoto); // chiama // riga-ok
            Debug.Log("<color=red>[ARMA] Clic! Caricatore vuoto. Ricarica necessaria.</color>"); // logga // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void CalcolaTraiettoria() // roba pub // riga-ok
    { // apre // riga-ok
        // IL SEGRETO DELLA PRECISIONE: Spara un raggio dal centro esatto dello schermo (0.5 larghezza, 0.5 altezza)
        Ray raggioDiMira = telecameraPrincipale.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)); // setta // riga-ok
        
        // Raccoglie tutto ciò che viene attraversato dal raggio
        RaycastHit[] tuttiIColpi = Physics.RaycastAll(raggioDiMira, gittataSparo); // setta // riga-ok
        
        // Ordina gli impatti dal più vicino al più lontano
        System.Array.Sort(tuttiIColpi, (a, b) => a.distance.CompareTo(b.distance)); // setta // riga-ok

        // blocco: gira piu volte
        foreach (RaycastHit hit in tuttiIColpi) // ciclo x // riga-ok
        { // apre // riga-ok
            // Ignora te stesso se il raggio parte da "dentro" il tuo corpo
            // blocco: controlla se va
            if (hit.collider.transform.root == transform.root) continue; // se ok // riga-ok

            // Identifica il bersaglio colpito
            bool colpoInTesta = hit.collider.CompareTag("Testa"); // setta // riga-ok
            float dannoInflitto = colpoInTesta ? dannoTesta : dannoCorpo; // setta // riga-ok

            IDamageable bersaglio = hit.collider.GetComponentInParent<IDamageable>(); // setta // riga-ok
            
            // blocco: controlla se va
            if (bersaglio != null) // se ok // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (colpoInTesta)  // se ok // riga-ok
                    Debug.Log("<color=red><b>[HEADSHOT!]</b> Danno critico inflitto!</color>"); // logga // riga-ok
                
                bersaglio.SubisciDanno(dannoInflitto); // chiama // riga-ok
            } // chiude // riga-ok
            // blocco: caso diverso
            else  // se no // riga-ok
            { // apre // riga-ok
                Debug.Log($"<color=grey>[BALISTICA] Colpito ostacolo: {hit.collider.name}</color>"); // logga // riga-ok
            } // chiude // riga-ok

            // Ferma il proiettile al primo ostacolo (non trapassa i muri)
            break;  // stop // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void GeneraRumoreSparo() // roba pub // riga-ok
    { // apre // riga-ok
        Collider[] nemiciAllertati = Physics.OverlapSphere(transform.position, raggioRumore, layerNemici); // setta // riga-ok
        // blocco: gira piu volte
        foreach (Collider col in nemiciAllertati) // ciclo x // riga-ok
        { // apre // riga-ok
            GuardiaNpc guardia = col.GetComponentInParent<GuardiaNpc>(); // setta // riga-ok
            // blocco: controlla se va
            if (guardia != null) guardia.RiceviAllarmeRinforzi(transform); // se ok // riga-ok

            ManutenzioneBot bot = col.GetComponentInParent<ManutenzioneBot>(); // setta // riga-ok
            // blocco: controlla se va
            if (bot != null && bot.statoAttuale == ManutenzioneBot.StatoIA.RicercaAttiva) // se ok // riga-ok
            { // apre // riga-ok
                bot.statoAttuale = ManutenzioneBot.StatoIA.Inseguimento; // setta // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void RipristinaMunizioniFuturo() // roba pub // riga-ok
{ // apre // riga-ok
    proiettiliAttuali = proiettiliMassimi; // setta // riga-ok
    // Aggiorna l'HUD dopo il ricaricamento
    OnMunizioniCambiate?.Invoke(proiettiliAttuali, proiettiliMassimi); // chiama // riga-ok
} // chiude // riga-ok
} // chiude // riga-ok
