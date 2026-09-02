using UnityEngine;
using UnityEngine.InputSystem;
using GoldenCast.UI;

public class move_camara : MonoBehaviour
{
    [Header("Bersaglio")]
    public Transform target;
    public Vector3 offsetTesta = new Vector3(0, 1.5f, 0);

    [Header("Impostazioni Prima Persona")]
    [Tooltip("Distanza bloccata a 0 per la visuale in prima persona.")]
    public float distanza = 0f;

    [Header("Rotazione Continua Libera")]
    [Tooltip("ATTENZIONE: Avendo rimosso il deltaTime, imposta la sensibilità molto bassa (es. 0.1 o 0.5)")]
    public float sensibilitaMouse = 0.2f;
    private float rotazioneX = 0f;
    private float rotazioneY = 0f;

    [Header("Transizione Prima Persona")]
    public Renderer[] renderersPersonaggio;
    public float sogliaSparizione = 1.2f;

    void Start()
    {
        // Il cursore viene bloccato e nascosto per garantire un input rotazionale continuo
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        rotazioneX = transform.eulerAngles.y;
        rotazioneY = transform.eulerAngles.x;
    }

    void LateUpdate()
    {
        if (ModalUIState.IsModalOpen)
            return;

        if (target == null || Mouse.current == null) return;

        // --- ZOOM DISABILITATO PER PRIMA PERSONA ---
        distanza = 0f;

        // --- RISOLUZIONE VISIBILITÀ MESH ---
        bool deveEssereVisibile = distanza > sogliaSparizione;

        foreach (Renderer r in renderersPersonaggio)
        {
            if (r != null && r.enabled != deveEssereVisibile)
            {
                r.enabled = deveEssereVisibile;
            }
        }

        // --- ROTAZIONE DELLA TELECAMERA INDIPENDENTE ---
        // Rimossa l'istruzione if sul tasto destro e rimosso il moltiplicatore Time.deltaTime.
        // I vettori X e Y del mouse vengono letti direttamente in modo crudo per la massima reattività.
        // Il moltiplicatore 0.02f funge da "ammortizzatore" per domare l'alta risoluzione del mouse
        float deltaX = Mouse.current.delta.ReadValue().x * sensibilitaMouse * 0.02f;
        float deltaY = Mouse.current.delta.ReadValue().y * sensibilitaMouse * 0.02f;

        rotazioneX += deltaX;
        rotazioneY -= deltaY;

        // Il Clamp previene il "Gimbal Lock" (espanso per la prima persona)
        rotazioneY = Mathf.Clamp(rotazioneY, -85f, 85f);

        // --- CALCOLO POSIZIONALE MEDIANTE QUATERNIONI ---
        Quaternion rotazioneCorrente = Quaternion.Euler(rotazioneY, rotazioneX, 0);
        Vector3 posizioneCorrente = target.position + offsetTesta - (rotazioneCorrente * Vector3.forward * distanza);

        transform.rotation = rotazioneCorrente;
        transform.position = posizioneCorrente;
    }
}
