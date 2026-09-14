// ============================================================================
// Crisis Protocol / Sector Containment - Player
// File: .\Assets\CrisisProtocol\Scripts\Player\GestoreArmi.cs
// Responsabilita': gestisce input, movimento, combattimento, interazione, salute o strumenti controllati dal giocatore.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine; // usa lib // riga-ok
using UnityEngine.InputSystem; // usa lib // riga-ok
using CrisisProtocol.UI; // usa lib // riga-ok

// blocco: classe x roba grossa
public class GestoreArmi : MonoBehaviour // classe qui // riga-ok
{ // apre // riga-ok
    [Header("Configurazione Visuale (Zoom e Distanza)")] // nota unity // riga-ok
    public Camera telecameraMain; // roba pub // riga-ok
    public move_camara scriptCamera; // Il riferimento al motore della telecamera // roba pub // riga-ok
    
    public float fovStandard = 60f; // roba pub // riga-ok
    public float fovMira = 35f; // roba pub // riga-ok
    
    [Tooltip("La distanza fisica della telecamera quando cammini in terza persona")] // nota unity // riga-ok
    public float distanzaStandard = 1f; // roba pub // riga-ok
    [Tooltip("La distanza fisica quando miri (0 = prima persona dentro il volto)")] // nota unity // riga-ok
    public float distanzaMira = 0f; // roba pub // riga-ok
    
    public float velocitaZoom = 12f; // roba pub // riga-ok

    [Header("Modalità")] // nota unity // riga-ok
    public AttaccoPlayer scriptCorpoACorpo; // roba pub // riga-ok
    public SparoPlayer scriptSparo; // roba pub // riga-ok

    private bool inModalitaSparo = false; // roba pub // riga-ok

    void Start() // chiama // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (telecameraMain == null) telecameraMain = Camera.main; // se ok // riga-ok
        
        // Cerca automaticamente lo script della telecamera per evitare errori di riferimento
        // blocco: controlla se va
        if (scriptCamera == null && telecameraMain != null)  // se ok // riga-ok
        { // apre // riga-ok
            scriptCamera = telecameraMain.GetComponent<move_camara>(); // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    void Update() // chiama // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (ModalUIState.IsModalOpen) // se ok // riga-ok
            return; // torna val // riga-ok

        bool staMirando = Mouse.current != null && Mouse.current.rightButton.isPressed; // setta // riga-ok

        // blocco: controlla se va
        if (staMirando) // se ok // riga-ok
        { // apre // riga-ok
            AttivaModalitaSparo(); // chiama // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            AttivaModalitaMischia(); // chiama // riga-ok
        } // chiude // riga-ok

        // Interpolazione lineare dell'ottica (FOV)
        // blocco: controlla se va
        if (telecameraMain != null) // se ok // riga-ok
        { // apre // riga-ok
            float targetFov = inModalitaSparo ? fovMira : fovStandard; // setta // riga-ok
            telecameraMain.fieldOfView = Mathf.Lerp(telecameraMain.fieldOfView, targetFov, Time.deltaTime * velocitaZoom); // setta // riga-ok
        } // chiude // riga-ok

        // Interpolazione lineare della distanza FISICA (Avanzamento / Arretramento)
        // blocco: controlla se va
        if (scriptCamera != null) // se ok // riga-ok
        { // apre // riga-ok
            float targetDist = inModalitaSparo ? distanzaMira : distanzaStandard; // setta // riga-ok
            scriptCamera.distanza = Mathf.Lerp(scriptCamera.distanza, targetDist, Time.deltaTime * velocitaZoom); // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void AttivaModalitaSparo() // roba pub // riga-ok
    { // apre // riga-ok
        inModalitaSparo = true; // setta // riga-ok
        // blocco: controlla se va
        if (scriptCorpoACorpo != null) scriptCorpoACorpo.enabled = false; // se ok // riga-ok
        // blocco: controlla se va
        if (scriptSparo != null) scriptSparo.enabled = true; // se ok // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void AttivaModalitaMischia() // roba pub // riga-ok
    { // apre // riga-ok
        inModalitaSparo = false; // setta // riga-ok
        // blocco: controlla se va
        if (scriptCorpoACorpo != null) scriptCorpoACorpo.enabled = true; // se ok // riga-ok
        // blocco: controlla se va
        if (scriptSparo != null) scriptSparo.enabled = false; // se ok // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
