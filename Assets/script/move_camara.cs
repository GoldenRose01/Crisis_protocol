using UnityEngine;
using UnityEngine.InputSystem;
using GoldenCast.UI;

public class move_camara : MonoBehaviour
{
    [Header("Bersaglio")]
    public Transform target;
    public Vector3 offsetTesta = new Vector3(0, 1.5f, 0);

    [Header("Zoom (Rotellina)")]
    public float distanza = 2f;
    public float minDistanza = 0f;
    public float maxDistanza = 15f;
    public float velocitaZoom = 0f;

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

        // --- GESTIONE DELLO ZOOM MATEMATICO ---
        float scrollY = Mouse.current.scroll.ReadValue().y;
        if (scrollY != 0)
        {
            distanza -= Mathf.Sign(scrollY) * velocitaZoom;
            distanza = Mathf.Clamp(distanza, minDistanza, maxDistanza);
        }

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

        // Il Clamp previene il "Gimbal Lock" impedendo alla telecamera di ribaltarsi sottosopra
        rotazioneY = Mathf.Clamp(rotazioneY, -45f, 80f);

        // --- CALCOLO POSIZIONALE MEDIANTE QUATERNIONI ---
        Quaternion rotazioneCorrente = Quaternion.Euler(rotazioneY, rotazioneX, 0);
        Vector3 posizioneCorrente = target.position + offsetTesta - (rotazioneCorrente * Vector3.forward * distanza);

        transform.rotation = rotazioneCorrente;
        transform.position = posizioneCorrente;
    }
}
