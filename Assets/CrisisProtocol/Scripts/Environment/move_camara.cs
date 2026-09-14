// ============================================================================
// Crisis Protocol / Sector Containment - Ambiente interattivo
// File: .\Assets\CrisisProtocol\Scripts\Environment\move_camara.cs
// Responsabilita': controlla porte, datapad, teletrasporti, camera o oggetti di scena collegati alla progressione.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine; // usa lib // riga-ok
using UnityEngine.InputSystem; // usa lib // riga-ok
using CrisisProtocol.UI; // usa lib // riga-ok

// blocco: classe x roba grossa
public class move_camara : MonoBehaviour // classe qui // riga-ok
{ // apre // riga-ok
    [Header("Bersaglio")] // nota unity // riga-ok
    public Transform target; // roba pub // riga-ok
    public Vector3 offsetTesta = new Vector3(0, 1.5f, 0); // roba pub // riga-ok

    [Header("Impostazioni Prima Persona")] // nota unity // riga-ok
    [Tooltip("Distanza bloccata a 0 per la visuale in prima persona.")] // nota unity // riga-ok
    public float distanza = 0f; // roba pub // riga-ok

    [Header("Rotazione Continua Libera")] // nota unity // riga-ok
    [Tooltip("ATTENZIONE: Avendo rimosso il deltaTime, imposta la sensibilità molto bassa (es. 0.1 o 0.5)")] // nota unity // riga-ok
    public float sensibilitaMouse = 0.2f; // roba pub // riga-ok
    private float rotazioneX = 0f; // roba pub // riga-ok
    private float rotazioneY = 0f; // roba pub // riga-ok

    [Header("Transizione Prima Persona")] // nota unity // riga-ok
    public Renderer[] renderersPersonaggio; // roba pub // riga-ok
    public float sogliaSparizione = 1.2f; // roba pub // riga-ok

    void Start() // chiama // riga-ok
    { // apre // riga-ok
        // Il cursore viene bloccato e nascosto per garantire un input rotazionale continuo
        Cursor.lockState = CursorLockMode.Locked; // setta // riga-ok
        Cursor.visible = false; // setta // riga-ok

        rotazioneX = transform.eulerAngles.y; // setta // riga-ok
        rotazioneY = transform.eulerAngles.x; // setta // riga-ok
    } // chiude // riga-ok

    void LateUpdate() // chiama // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (ModalUIState.IsModalOpen) // se ok // riga-ok
            return; // torna val // riga-ok

        // blocco: controlla se va
        if (target == null || Mouse.current == null) return; // se ok // riga-ok

        // --- ZOOM DISABILITATO PER PRIMA PERSONA ---
        distanza = 0f; // setta // riga-ok

        // --- RISOLUZIONE VISIBILITÀ MESH ---
        bool deveEssereVisibile = distanza > sogliaSparizione; // setta // riga-ok

        // blocco: gira piu volte
        foreach (Renderer r in renderersPersonaggio) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (r != null && r.enabled != deveEssereVisibile) // se ok // riga-ok
            { // apre // riga-ok
                r.enabled = deveEssereVisibile; // setta // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // --- ROTAZIONE DELLA TELECAMERA INDIPENDENTE ---
        // Rimossa l'istruzione if sul tasto destro e rimosso il moltiplicatore Time.deltaTime.
        // I vettori X e Y del mouse vengono letti direttamente in modo crudo per la massima reattività.
        // Il moltiplicatore 0.02f funge da "ammortizzatore" per domare l'alta risoluzione del mouse
        float deltaX = Mouse.current.delta.ReadValue().x * sensibilitaMouse * 0.02f; // setta // riga-ok
        float deltaY = Mouse.current.delta.ReadValue().y * sensibilitaMouse * 0.02f; // setta // riga-ok

        rotazioneX += deltaX; // setta // riga-ok
        rotazioneY -= deltaY; // setta // riga-ok

        // Il Clamp previene il "Gimbal Lock" (espanso per la prima persona)
        rotazioneY = Mathf.Clamp(rotazioneY, -85f, 85f); // setta // riga-ok

        // --- CALCOLO POSIZIONALE MEDIANTE QUATERNIONI ---
        Quaternion rotazioneCorrente = Quaternion.Euler(rotazioneY, rotazioneX, 0); // setta // riga-ok
        Vector3 posizioneCorrente = target.position + offsetTesta - (rotazioneCorrente * Vector3.forward * distanza); // setta // riga-ok

        transform.rotation = rotazioneCorrente; // setta // riga-ok
        transform.position = posizioneCorrente; // setta // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
