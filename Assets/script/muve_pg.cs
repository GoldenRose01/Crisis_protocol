using UnityEngine;
using UnityEngine.InputSystem;
using GoldenCast.UI;

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

    private int countJump = 0;
    private bool richiediSalto = false;
    private bool isGrounded = true;

    private Rigidbody rb;
    private BoxCollider bx;

    private bool AnimatorePronto =>
        animatorePersonaggio != null &&
        animatorePersonaggio.isActiveAndEnabled &&
        animatorePersonaggio.runtimeAnimatorController != null;

    void Start()
    {
        bx = GetComponent<BoxCollider>();
        rb = GetComponent<Rigidbody>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        // Inizializza con la velocità base
        velocitaCorrente = velocitaCamminata;
    }

    void Update()
    {
        if (ModalUIState.IsModalOpen)
            return;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            richiediSalto = true;
        }
    }

    void FixedUpdate()
    {
        if (ModalUIState.IsModalOpen)
        {
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
                Debug.Log("Primo Salto eseguito.");
            }
            else if (countJump == 1)
            {
                velocitaY = forzaSecondoSalto;
                countJump++;

                if (AnimatorePronto)
                {
                    animatorePersonaggio.SetTrigger("DoubleJump");
                }
                Debug.Log("Secondo Salto (Risalto) eseguito.");
            }
            richiediSalto = false;
        }

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
            }
        }
    }
}
