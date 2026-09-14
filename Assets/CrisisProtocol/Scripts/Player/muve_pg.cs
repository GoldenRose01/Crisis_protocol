// ============================================================================
// Crisis Protocol / Sector Containment - Player
// File: .\Assets\CrisisProtocol\Scripts\Player\muve_pg.cs
// Responsabilita': gestisce input, movimento, combattimento, interazione, salute o strumenti controllati dal giocatore.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine; // usa lib // riga-ok
using UnityEngine.InputSystem; // usa lib // riga-ok
using CrisisProtocol.UI; // usa lib // riga-ok

// blocco: classe x roba grossa
public class muve_pg : MonoBehaviour // classe qui // riga-ok
{ // apre // riga-ok
    [Header("Parametri di Movimento")] // nota unity // riga-ok

    [Header("Velocità")] // nota unity // riga-ok
    public float velocitaCamminata = 4f; // roba pub // riga-ok
    public float velocitaCorsa = 8f; // roba pub // riga-ok
    private float velocitaCorrente; // roba pub // riga-ok

    public float forzaPrimoSalto = 5f; // roba pub // riga-ok
    public float forzaSecondoSalto = 3.5f; // roba pub // riga-ok

    // FONDAMENTALE: Trascina la tua Main Camera qui dall'Inspector
    public Transform cameraTransform; // roba pub // riga-ok

    [Header("Configurazione Animazione")] // nota unity // riga-ok
    public Animator animatorePersonaggio; // roba pub // riga-ok

    [Header("Audio")] // nota unity // riga-ok
    [Tooltip("Suono dei passi durante la camminata standard.")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoPassi; // ok qua // riga-ok
    [Tooltip("Suono dei passi durante la corsa (Shift).")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoCorsa; // ok qua // riga-ok
    [Tooltip("Suono di stacco del primo salto.")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoPrimoSalto; // ok qua // riga-ok
    [Tooltip("Suono di stacco del secondo salto / double jump.")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoSecondoSalto; // ok qua // riga-ok
    [Range(0f, 1f)] [SerializeField] private float volumeAudio = 0.85f; // setta // riga-ok
    [SerializeField] private float intervalloPassiCamminata = 0.48f; // setta // riga-ok
    [SerializeField] private float intervalloPassiCorsa = 0.30f; // setta // riga-ok

    private int countJump = 0; // roba pub // riga-ok
    private bool richiediSalto = false; // roba pub // riga-ok
    private bool isGrounded = true; // roba pub // riga-ok

    private Rigidbody rb; // roba pub // riga-ok
    private BoxCollider bx; // roba pub // riga-ok
    private AudioSource audioSource; // roba pub // riga-ok
    private AudioSource audioSourcePassi; // roba pub // riga-ok
    private float timerPassi = 0f; // roba pub // riga-ok

    private bool AnimatorePronto => // roba pub // riga-ok
        animatorePersonaggio != null && // setta // riga-ok
        animatorePersonaggio.isActiveAndEnabled && // ok qua // riga-ok
        animatorePersonaggio.runtimeAnimatorController != null; // setta // riga-ok

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
            audioSource.maxDistance = 20.0f; // setta // riga-ok
            audioSource.dopplerLevel = 0f; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (audioSourcePassi == null) // se ok // riga-ok
        { // apre // riga-ok
            Transform childPassi = transform.Find("AudioPassiSource"); // setta // riga-ok
            // blocco: controlla se va
            if (childPassi != null) // se ok // riga-ok
            { // apre // riga-ok
                audioSourcePassi = childPassi.GetComponent<AudioSource>(); // setta // riga-ok
            } // chiude // riga-ok

            // blocco: controlla se va
            if (audioSourcePassi == null) // se ok // riga-ok
            { // apre // riga-ok
                GameObject goPassi = new GameObject("AudioPassiSource"); // setta // riga-ok
                goPassi.transform.SetParent(transform, false); // chiama // riga-ok
                audioSourcePassi = goPassi.AddComponent<AudioSource>(); // setta // riga-ok
            } // chiude // riga-ok

            audioSourcePassi.playOnAwake = false; // setta // riga-ok
            audioSourcePassi.spatialBlend = 1.0f; // setta // riga-ok
            audioSourcePassi.rolloffMode = AudioRolloffMode.Logarithmic; // setta // riga-ok
            audioSourcePassi.minDistance = 1.5f; // setta // riga-ok
            audioSourcePassi.maxDistance = 20.0f; // setta // riga-ok
            audioSourcePassi.dopplerLevel = 0f; // setta // riga-ok
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

    void Start() // chiama // riga-ok
    { // apre // riga-ok
        InizializzaAudioSource(); // chiama // riga-ok
        bx = GetComponent<BoxCollider>(); // setta // riga-ok
        rb = GetComponent<Rigidbody>(); // setta // riga-ok

        // blocco: controlla se va
        if (cameraTransform == null && Camera.main != null) // se ok // riga-ok
        { // apre // riga-ok
            cameraTransform = Camera.main.transform; // setta // riga-ok
        } // chiude // riga-ok

        // Inizializza con la velocità base
        velocitaCorrente = velocitaCamminata; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnDisable() // roba pub // riga-ok
    { // apre // riga-ok
        FermaAudioPassi(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void FermaAudioPassi() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (audioSourcePassi != null && audioSourcePassi.isPlaying) // se ok // riga-ok
        { // apre // riga-ok
            audioSourcePassi.Stop(); // chiama // riga-ok
        } // chiude // riga-ok
        timerPassi = 0f; // setta // riga-ok
    } // chiude // riga-ok

    void Update() // chiama // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (ModalUIState.IsModalOpen) // se ok // riga-ok
        { // apre // riga-ok
            FermaAudioPassi(); // chiama // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) // se ok // riga-ok
        { // apre // riga-ok
            richiediSalto = true; // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    void FixedUpdate() // chiama // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (ModalUIState.IsModalOpen) // se ok // riga-ok
        { // apre // riga-ok
            FermaAudioPassi(); // chiama // riga-ok

            // blocco: controlla se va
            if (rb != null) // se ok // riga-ok
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f); // setta // riga-ok

            // blocco: controlla se va
            if (AnimatorePronto) // se ok // riga-ok
                animatorePersonaggio.SetFloat("Speed", 0f); // chiama // riga-ok

            return; // torna val // riga-ok
        } // chiude // riga-ok

        float inputOrizzontale = 0f; // setta // riga-ok
        float inputVerticale = 0f; // setta // riga-ok

        bool staCorrendo = false; // setta // riga-ok

        // blocco: controlla se va
        if (Keyboard.current != null) // se ok // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) inputOrizzontale += 1f; // se ok // riga-ok
            // blocco: controlla se va
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) inputOrizzontale -= 1f; // se ok // riga-ok

            // blocco: controlla se va
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) inputVerticale += 1f; // se ok // riga-ok
            // blocco: controlla se va
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) inputVerticale -= 1f; // se ok // riga-ok

            // RILEVAZIONE DELLA CORSA (Left Shift)
            staCorrendo = Keyboard.current.leftShiftKey.isPressed; // setta // riga-ok
        } // chiude // riga-ok

        // Calcolo della velocità corrente
        velocitaCorrente = staCorrendo ? velocitaCorsa : velocitaCamminata; // setta // riga-ok

        Vector2 direzioneInput = new Vector2(inputOrizzontale, inputVerticale).normalized; // setta // riga-ok
        float magnitudineMovimento = direzioneInput.magnitude; // setta // riga-ok

        // NOTA: rb.linearVelocity è corretto su Unity 6. Se usi Unity 2023 o inferiori, usa rb.velocity
        float velocitaY = rb.linearVelocity.y; // setta // riga-ok

        // blocco: controlla se va
        if (richiediSalto) // se ok // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (countJump == 0 && isGrounded) // se ok // riga-ok
            { // apre // riga-ok
                velocitaY = forzaPrimoSalto; // setta // riga-ok
                countJump++; // ok qua // riga-ok
                isGrounded = false; // setta // riga-ok
                FermaAudioPassi(); // chiama // riga-ok
                RiproduciSuono(suonoPrimoSalto); // chiama // riga-ok
                Debug.Log("Primo Salto eseguito."); // logga // riga-ok
            } // chiude // riga-ok
            // blocco: controlla se va
            else if (countJump == 1) // se ok // riga-ok
            { // apre // riga-ok
                velocitaY = forzaSecondoSalto; // setta // riga-ok
                countJump++; // ok qua // riga-ok
                FermaAudioPassi(); // chiama // riga-ok
                RiproduciSuono(suonoSecondoSalto ?? suonoPrimoSalto); // chiama // riga-ok

                // blocco: controlla se va
                if (AnimatorePronto) // se ok // riga-ok
                { // apre // riga-ok
                    animatorePersonaggio.SetTrigger("DoubleJump"); // chiama // riga-ok
                } // chiude // riga-ok
                Debug.Log("Secondo Salto (Risalto) eseguito."); // logga // riga-ok
            } // chiude // riga-ok
            richiediSalto = false; // setta // riga-ok
        } // chiude // riga-ok

        // GESTIONE SUONO PASSI
        GestisciAudioPassi(magnitudineMovimento, staCorrendo); // chiama // riga-ok

        Vector3 movimentoFinale = Vector3.zero; // setta // riga-ok

        // Calcolo della direzione relativa alla telecamera
        // blocco: controlla se va
        if (cameraTransform != null) // se ok // riga-ok
        { // apre // riga-ok
            Vector3 forwardCamera = cameraTransform.forward; // setta // riga-ok
            Vector3 rightCamera = cameraTransform.right; // setta // riga-ok

            forwardCamera.y = 0f; // setta // riga-ok
            rightCamera.y = 0f; // setta // riga-ok

            forwardCamera.Normalize(); // chiama // riga-ok
            rightCamera.Normalize(); // chiama // riga-ok

            movimentoFinale = (forwardCamera * direzioneInput.y + rightCamera * direzioneInput.x) * velocitaCorrente; // setta // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            movimentoFinale = new Vector3(direzioneInput.x * velocitaCorrente, 0f, direzioneInput.y * velocitaCorrente); // setta // riga-ok
        } // chiude // riga-ok

        // Applicazione della velocità lineare
        rb.linearVelocity = new Vector3(movimentoFinale.x, velocitaY, movimentoFinale.z); // setta // riga-ok

        // --- ROTAZIONE DEL PERSONAGGIO ---
        // blocco: controlla se va
        if (movimentoFinale.x != 0 || movimentoFinale.z != 0) // se ok // riga-ok
        { // apre // riga-ok
            Vector3 direzioneSguardo = new Vector3(movimentoFinale.x, 0f, movimentoFinale.z).normalized; // setta // riga-ok
            Quaternion rotazioneTarget = Quaternion.LookRotation(direzioneSguardo); // setta // riga-ok
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, rotazioneTarget, 10f * Time.fixedDeltaTime)); // chiama // riga-ok
        } // chiude // riga-ok

        // INVIO DATI COERENTI ALL'ANIMATORE
        // blocco: controlla se va
        if (AnimatorePronto) // se ok // riga-ok
        { // apre // riga-ok
            // Moltiplichiamo per 2 se corre così lo blend tree dell'Animator distingue camminata (1) da corsa (2)
            float speedParametro = magnitudineMovimento * (staCorrendo ? 2f : 1f); // setta // riga-ok

            animatorePersonaggio.SetFloat("Speed", speedParametro); // chiama // riga-ok
            animatorePersonaggio.SetBool("IsGrounded", isGrounded); // chiama // riga-ok
            animatorePersonaggio.SetFloat("VerticalVelocity", rb.linearVelocity.y); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void GestisciAudioPassi(float magnitudineMovimento, bool staCorrendo) // roba pub // riga-ok
    { // apre // riga-ok
        InizializzaAudioSource(); // chiama // riga-ok

        bool staMuovendo = isGrounded && magnitudineMovimento > 0.1f; // setta // riga-ok
        // blocco: controlla se va
        if (!staMuovendo) // se ok // riga-ok
        { // apre // riga-ok
            FermaAudioPassi(); // chiama // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        AudioClip clipPasso = staCorrendo ? (suonoCorsa ?? suonoPassi) : suonoPassi; // setta // riga-ok
        // blocco: controlla se va
        if (clipPasso == null || audioSourcePassi == null) // se ok // riga-ok
        { // apre // riga-ok
            FermaAudioPassi(); // chiama // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        float volumeTarget = volumeAudio * (staCorrendo ? 0.9f : 0.7f); // setta // riga-ok

        // Se la traccia audio è una registrazione continua/multi-passo (es. durata > 0.8s come Footsteps_ running.wav o Footsteps_walking.wav)
        // blocco: controlla se va
        if (clipPasso.length > 0.8f) // se ok // riga-ok
        { // apre // riga-ok
            audioSourcePassi.loop = true; // setta // riga-ok
            audioSourcePassi.volume = volumeTarget; // setta // riga-ok
            audioSourcePassi.pitch = staCorrendo ? 1.05f : 1.0f; // setta // riga-ok

            // blocco: controlla se va
            if (audioSourcePassi.clip != clipPasso) // se ok // riga-ok
            { // apre // riga-ok
                audioSourcePassi.clip = clipPasso; // setta // riga-ok
                audioSourcePassi.Play(); // chiama // riga-ok
            } // chiude // riga-ok
            // blocco: controlla se va
            else if (!audioSourcePassi.isPlaying) // se ok // riga-ok
            { // apre // riga-ok
                audioSourcePassi.Play(); // chiama // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            // Se la clip è un singolo impatto di passo (singolo step < 0.8s)
            audioSourcePassi.loop = false; // setta // riga-ok
            timerPassi -= Time.fixedDeltaTime; // setta // riga-ok
            // blocco: controlla se va
            if (timerPassi <= 0f) // se ok // riga-ok
            { // apre // riga-ok
                audioSourcePassi.pitch = Random.Range(0.95f, 1.05f); // setta // riga-ok
                audioSourcePassi.PlayOneShot(clipPasso, volumeTarget); // chiama // riga-ok
                timerPassi = staCorrendo ? intervalloPassiCorsa : intervalloPassiCamminata; // setta // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnCollisionEnter(Collision collision) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (collision.gameObject.CompareTag("Terrain")) // se ok // riga-ok
        { // apre // riga-ok
            countJump = 0; // setta // riga-ok
            isGrounded = true; // setta // riga-ok
            Debug.Log("TERRENO RILEVATO! Reset cinematiche."); // logga // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnCollisionExit(Collision collision) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (collision.gameObject.CompareTag("Terrain")) // se ok // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (rb.linearVelocity.y < -0.1f) // se ok // riga-ok
            { // apre // riga-ok
                isGrounded = false; // setta // riga-ok
                FermaAudioPassi(); // chiama // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
