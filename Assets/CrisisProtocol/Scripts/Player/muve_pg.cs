// ============================================================================
// Crisis Protocol / Sector Containment - Player
// File: .\Assets\CrisisProtocol\Scripts\Player\muve_pg.cs
// Responsabilita': gestisce input, movimento, combattimento, interazione, salute o strumenti controllati dal giocatore.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine;
using UnityEngine.InputSystem;
using CrisisProtocol.UI;
public class muve_pg : MonoBehaviour
{
    [Header("Parametri di Movimento")]
    [Header("Velocità")]
    public float velocitaCamminata = 4f;
    public float velocitaCorsa = 8f;
    private float velocitaCorrente;
    public float forzaPrimoSalto = 5f;
    public float forzaSecondoSalto = 3.5f;
    // FONDAMENTALE: Trascina la tua Main Camera qui dall'Inspector
    public Transform cameraTransform;
    [Header("Configurazione Animazione")]
    public Animator animatorePersonaggio;
    [Header("Audio")]
    [Tooltip("Suono dei passi durante la camminata standard.")]
    [SerializeField] private AudioClip suonoPassi;
    [Tooltip("Suono dei passi durante la corsa (Shift).")]
    [SerializeField] private AudioClip suonoCorsa;
    [Tooltip("Suono di stacco del primo salto.")]
    [SerializeField] private AudioClip suonoPrimoSalto;
    [Tooltip("Suono di stacco del secondo salto / double jump.")]
    [SerializeField] private AudioClip suonoSecondoSalto;
    [Range(0f, 1f)] [SerializeField] private float volumeAudio = 0.85f;
    [SerializeField] private float intervalloPassiCamminata = 0.48f;
    [SerializeField] private float intervalloPassiCorsa = 0.30f;
    private int countJump = 0;
    private bool richiediSalto = false;
    private bool isGrounded = true;
    private Rigidbody rb;
    private BoxCollider bx;
    private AudioSource audioSource;
    private AudioSource audioSourcePassi;
    private float timerPassi = 0f;
    private bool AnimatorePronto =>
        animatorePersonaggio != null &&
        animatorePersonaggio.isActiveAndEnabled &&
        animatorePersonaggio.runtimeAnimatorController != null;
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
            audioSource.maxDistance = 20.0f;
            audioSource.dopplerLevel = 0f;
        }
        if (audioSourcePassi == null)
        {
            Transform childPassi = transform.Find("AudioPassiSource");
            if (childPassi != null)
            {
                audioSourcePassi = childPassi.GetComponent<AudioSource>();
            }
            if (audioSourcePassi == null)
            {
                GameObject goPassi = new GameObject("AudioPassiSource");
                goPassi.transform.SetParent(transform, false);
                audioSourcePassi = goPassi.AddComponent<AudioSource>();
            }
            audioSourcePassi.playOnAwake = false;
            audioSourcePassi.spatialBlend = 1.0f;
            audioSourcePassi.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSourcePassi.minDistance = 1.5f;
            audioSourcePassi.maxDistance = 20.0f;
            audioSourcePassi.dopplerLevel = 0f;
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
    void Start()
    {
        InizializzaAudioSource();
        bx = GetComponent<BoxCollider>();
        rb = GetComponent<Rigidbody>();
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        // Inizializza con la velocità base
        velocitaCorrente = velocitaCamminata;
    }
    private void OnDisable()
    {
        FermaAudioPassi();
    }
    public void FermaAudioPassi()
    {
        if (audioSourcePassi != null && audioSourcePassi.isPlaying)
        {
            audioSourcePassi.Stop();
        }
        timerPassi = 0f;
    }
    void Update()
    {
        if (ModalUIState.IsModalOpen)
        {
            FermaAudioPassi();
            return;
        }
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            richiediSalto = true;
        }
    }
    void FixedUpdate()
    {
        if (ModalUIState.IsModalOpen)
        {
            FermaAudioPassi();
            if (rb != null)
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            if (AnimatorePronto)
                animatorePersonaggio.SetFloat("Speed", 0f);
            return;
        }
        float inputOrizzontale = 0f;
        float inputVerticale = 0f;
        bool staCorrendo = false;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) inputOrizzontale += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) inputOrizzontale -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) inputVerticale += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) inputVerticale -= 1f;
            // RILEVAZIONE DELLA CORSA (Left Shift)
            staCorrendo = Keyboard.current.leftShiftKey.isPressed;
        }
        // Calcolo della velocità corrente
        velocitaCorrente = staCorrendo ? velocitaCorsa : velocitaCamminata;
        Vector2 direzioneInput = new Vector2(inputOrizzontale, inputVerticale).normalized;
        float magnitudineMovimento = direzioneInput.magnitude;
        // NOTA: rb.linearVelocity è corretto su Unity 6. Se usi Unity 2023 o inferiori, usa rb.velocity
        float velocitaY = rb.linearVelocity.y;
        if (richiediSalto)
        {
            if (countJump == 0 && isGrounded)
            {
                velocitaY = forzaPrimoSalto;
                countJump++;
                isGrounded = false;
                FermaAudioPassi();
                RiproduciSuono(suonoPrimoSalto);
                Debug.Log("Primo Salto eseguito.");
            }
            else if (countJump == 1)
            {
                velocitaY = forzaSecondoSalto;
                countJump++;
                FermaAudioPassi();
                RiproduciSuono(suonoSecondoSalto ?? suonoPrimoSalto);
                if (AnimatorePronto)
                {
                    animatorePersonaggio.SetTrigger("DoubleJump");
                }
                Debug.Log("Secondo Salto (Risalto) eseguito.");
            }
            richiediSalto = false;
        }
        // GESTIONE SUONO PASSI
        GestisciAudioPassi(magnitudineMovimento, staCorrendo);
        Vector3 movimentoFinale = Vector3.zero;
        // Calcolo della direzione relativa alla telecamera
        if (cameraTransform != null)
        {
            Vector3 forwardCamera = cameraTransform.forward;
            Vector3 rightCamera = cameraTransform.right;
            forwardCamera.y = 0f;
            rightCamera.y = 0f;
            forwardCamera.Normalize();
            rightCamera.Normalize();
            movimentoFinale = (forwardCamera * direzioneInput.y + rightCamera * direzioneInput.x) * velocitaCorrente;
        }
        else
        {
            movimentoFinale = new Vector3(direzioneInput.x * velocitaCorrente, 0f, direzioneInput.y * velocitaCorrente);
        }
        // Applicazione della velocità lineare
        rb.linearVelocity = new Vector3(movimentoFinale.x, velocitaY, movimentoFinale.z);
        // --- ROTAZIONE DEL PERSONAGGIO ---
        if (movimentoFinale.x != 0 || movimentoFinale.z != 0)
        {
            Vector3 direzioneSguardo = new Vector3(movimentoFinale.x, 0f, movimentoFinale.z).normalized;
            Quaternion rotazioneTarget = Quaternion.LookRotation(direzioneSguardo);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, rotazioneTarget, 10f * Time.fixedDeltaTime));
        }
        // INVIO DATI COERENTI ALL'ANIMATORE
        if (AnimatorePronto)
        {
            // Moltiplichiamo per 2 se corre così lo blend tree dell'Animator distingue camminata (1) da corsa (2)
            float speedParametro = magnitudineMovimento * (staCorrendo ? 2f : 1f);
            animatorePersonaggio.SetFloat("Speed", speedParametro);
            animatorePersonaggio.SetBool("IsGrounded", isGrounded);
            animatorePersonaggio.SetFloat("VerticalVelocity", rb.linearVelocity.y);
        }
    }
    private void GestisciAudioPassi(float magnitudineMovimento, bool staCorrendo)
    {
        InizializzaAudioSource();
        bool staMuovendo = isGrounded && magnitudineMovimento > 0.1f;
        if (!staMuovendo)
        {
            FermaAudioPassi();
            return;
        }
        AudioClip clipPasso = staCorrendo ? (suonoCorsa ?? suonoPassi) : suonoPassi;
        if (clipPasso == null || audioSourcePassi == null)
        {
            FermaAudioPassi();
            return;
        }
        float volumeTarget = volumeAudio * (staCorrendo ? 0.9f : 0.7f);
        // Se la traccia audio è una registrazione continua/multi-passo (es. durata > 0.8s come Footsteps_ running.wav o Footsteps_walking.wav)
        if (clipPasso.length > 0.8f)
        {
            audioSourcePassi.loop = true;
            audioSourcePassi.volume = volumeTarget;
            audioSourcePassi.pitch = staCorrendo ? 1.05f : 1.0f;
            if (audioSourcePassi.clip != clipPasso)
            {
                audioSourcePassi.clip = clipPasso;
                audioSourcePassi.Play();
            }
            else if (!audioSourcePassi.isPlaying)
            {
                audioSourcePassi.Play();
            }
        }
        else
        {
            // Se la clip è un singolo impatto di passo (singolo step < 0.8s)
            audioSourcePassi.loop = false;
            timerPassi -= Time.fixedDeltaTime;
            if (timerPassi <= 0f)
            {
                audioSourcePassi.pitch = Random.Range(0.95f, 1.05f);
                audioSourcePassi.PlayOneShot(clipPasso, volumeTarget);
                timerPassi = staCorrendo ? intervalloPassiCorsa : intervalloPassiCamminata;
            }
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Terrain"))
        {
            countJump = 0;
            isGrounded = true;
            Debug.Log("TERRENO RILEVATO! Reset cinematiche.");
        }
    }
    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Terrain"))
        {
            if (rb.linearVelocity.y < -0.1f)
            {
                isGrounded = false;
                FermaAudioPassi();
            }
        }
    }
}