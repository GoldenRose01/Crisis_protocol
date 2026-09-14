// ============================================================================
// Crisis Protocol / Sector Containment - Player
// File: .\Assets\CrisisProtocol\Scripts\Player\SalutePlayer.cs
// Responsabilita': gestisce input, movimento, combattimento, interazione, salute o strumenti controllati dal giocatore.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using System; // usa lib // riga-ok
using UnityEngine; // usa lib // riga-ok

[RequireComponent(typeof(Rigidbody))] // nota unity // riga-ok
// blocco: classe x roba grossa
public class SalutePlayer : MonoBehaviour, IDamageable // classe qui // riga-ok
{ // apre // riga-ok
    [Header("Statistiche Salute")] // nota unity // riga-ok
    public float puntiVitaMassimi = 100f; // roba pub // riga-ok
    [SerializeField] private float puntiVitaCorrenti; // ok qua // riga-ok
    [SerializeField] private bool applicaTagPlayerAutomatico = true; // setta // riga-ok

    [Header("Configurazione Animazioni & Stato")] // nota unity // riga-ok
    public Animator animatorePersonaggio; // roba pub // riga-ok
    public string triggerMorte = "Morte"; // roba pub // riga-ok
    public string triggerDanno = "SubisciDanno"; // roba pub // riga-ok

    [Header("Audio")] // nota unity // riga-ok
    [Tooltip("Suono di gemito/gasp quando il player subisce danno.")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoDanno; // ok qua // riga-ok
    [Tooltip("Suono di morte / Game Over.")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoMorte; // ok qua // riga-ok
    [Tooltip("Suono di cura / ripristino salute.")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoCura; // ok qua // riga-ok
    [Range(0f, 1f)] [SerializeField] private float volumeAudio = 1.0f; // setta // riga-ok

    private bool isMorto; // roba pub // riga-ok
    private muve_pg scriptMovimento; // roba pub // riga-ok
    private Rigidbody rb; // roba pub // riga-ok
    private AudioSource audioSource; // roba pub // riga-ok

    public float SaluteAttuale => puntiVitaCorrenti; // roba pub // riga-ok
    public static event Action<float, float> OnSaluteCambiata; // roba pub // riga-ok
    public static event Action OnPlayerMorto; // roba pub // riga-ok

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

    // blocco: funzione fa cose
    private void Awake() // roba pub // riga-ok
    { // apre // riga-ok
        ApplicaTagUnity(); // chiama // riga-ok
        puntiVitaCorrenti = puntiVitaMassimi; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void Start() // roba pub // riga-ok
    { // apre // riga-ok
        InizializzaAudioSource(); // chiama // riga-ok
        ApplicaTagUnity(); // chiama // riga-ok
        // blocco: controlla se va
        if (puntiVitaCorrenti <= 0) // se ok // riga-ok
            puntiVitaCorrenti = puntiVitaMassimi; // setta // riga-ok
            
        scriptMovimento = GetComponent<muve_pg>() ?? GetComponentInParent<muve_pg>() ?? GetComponentInChildren<muve_pg>(); // setta // riga-ok
        rb = GetComponent<Rigidbody>(); // setta // riga-ok

        OnSaluteCambiata?.Invoke(puntiVitaCorrenti, puntiVitaMassimi); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnValidate() // roba pub // riga-ok
    { // apre // riga-ok
        ApplicaTagUnity(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void SubisciDanno(float quantitaDanno) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (isMorto) // se ok // riga-ok
            return; // torna val // riga-ok

        puntiVitaCorrenti -= quantitaDanno; // setta // riga-ok
        puntiVitaCorrenti = Mathf.Clamp(puntiVitaCorrenti, 0f, puntiVitaMassimi); // setta // riga-ok

        Debug.Log($"<b>[GIOCATORE]</b> Colpito! Subito {quantitaDanno} HP di danno. Vita rimanente: {puntiVitaCorrenti}/{puntiVitaMassimi}"); // logga // riga-ok
        OnSaluteCambiata?.Invoke(puntiVitaCorrenti, puntiVitaMassimi); // chiama // riga-ok

        // blocco: controlla se va
        if (MissionManager.Instance != null) // se ok // riga-ok
            MissionManager.Instance.RegistraDannoSubito(quantitaDanno); // chiama // riga-ok

        // blocco: controlla se va
        if (puntiVitaCorrenti <= 0f) // se ok // riga-ok
        { // apre // riga-ok
            EseguiMorte(); // chiama // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        RiproduciSuono(suonoDanno); // chiama // riga-ok

        // blocco: controlla se va
        if (animatorePersonaggio != null && !string.IsNullOrWhiteSpace(triggerDanno)) // se ok // riga-ok
            animatorePersonaggio.SetTrigger(triggerDanno); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void Cura(float quantitaCura) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (isMorto) // se ok // riga-ok
            return; // torna val // riga-ok

        puntiVitaCorrenti = Mathf.Clamp(puntiVitaCorrenti + quantitaCura, 0f, puntiVitaMassimi); // setta // riga-ok
        RiproduciSuono(suonoCura); // chiama // riga-ok
        OnSaluteCambiata?.Invoke(puntiVitaCorrenti, puntiVitaMassimi); // chiama // riga-ok

        Debug.Log($"[SISTEMA] Curato di {quantitaCura} HP. Salute: {puntiVitaCorrenti}/{puntiVitaMassimi}"); // logga // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void EseguiMorte() // roba pub // riga-ok
    { // apre // riga-ok
        isMorto = true; // setta // riga-ok
        RiproduciSuono(suonoMorte); // chiama // riga-ok
        Debug.Log("<color=red><b>[GAME OVER]</b> Il giocatore e' crollato a terra.</color>"); // logga // riga-ok

        // blocco: controlla se va
        if (animatorePersonaggio != null) // se ok // riga-ok
            animatorePersonaggio.SetTrigger(triggerMorte); // chiama // riga-ok

        // blocco: controlla se va
        if (scriptMovimento != null) // se ok // riga-ok
            scriptMovimento.enabled = false; // setta // riga-ok

        // blocco: controlla se va
        if (rb != null) // se ok // riga-ok
        { // apre // riga-ok
            rb.linearVelocity = Vector3.zero; // setta // riga-ok
            rb.isKinematic = true; // setta // riga-ok
        } // chiude // riga-ok

        OnPlayerMorto?.Invoke(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void ApplicaTagUnity() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (applicaTagPlayerAutomatico) // se ok // riga-ok
            SectorContainmentTags.ApplyTag(gameObject, SectorContainmentTags.Player); // chiama // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
