using UnityEngine;
using UnityEngine.InputSystem;
using GoldenCast.UI;

public class GestoreArmi : MonoBehaviour
{
    [Header("Configurazione Visuale (Zoom e Distanza)")]
    public Camera telecameraMain;
    public move_camara scriptCamera; // Il riferimento al motore della telecamera
    
    public float fovStandard = 60f;
    public float fovMira = 35f;
    
    [Tooltip("La distanza fisica della telecamera quando cammini in terza persona")]
    public float distanzaStandard = 1f;
    [Tooltip("La distanza fisica quando miri (0 = prima persona dentro il volto)")]
    public float distanzaMira = 0f;
    
    public float velocitaZoom = 12f;

    [Header("Modalità")]
    public AttaccoPlayer scriptCorpoACorpo;
    public SparoPlayer scriptSparo;

    private bool inModalitaSparo = false;

    void Start()
    {
        if (telecameraMain == null) telecameraMain = Camera.main;
        
        // Cerca automaticamente lo script della telecamera per evitare errori di riferimento
        if (scriptCamera == null && telecameraMain != null) 
        {
            scriptCamera = telecameraMain.GetComponent<move_camara>();
        }
    }

    void Update()
    {
        if (ModalUIState.IsModalOpen)
            return;

        bool staMirando = Mouse.current != null && Mouse.current.rightButton.isPressed;

        if (staMirando)
        {
            AttivaModalitaSparo();
        }
        else
        {
            AttivaModalitaMischia();
        }

        // Interpolazione lineare dell'ottica (FOV)
        if (telecameraMain != null)
        {
            float targetFov = inModalitaSparo ? fovMira : fovStandard;
            telecameraMain.fieldOfView = Mathf.Lerp(telecameraMain.fieldOfView, targetFov, Time.deltaTime * velocitaZoom);
        }

        // Interpolazione lineare della distanza FISICA (Avanzamento / Arretramento)
        if (scriptCamera != null)
        {
            float targetDist = inModalitaSparo ? distanzaMira : distanzaStandard;
            scriptCamera.distanza = Mathf.Lerp(scriptCamera.distanza, targetDist, Time.deltaTime * velocitaZoom);
        }
    }

    private void AttivaModalitaSparo()
    {
        inModalitaSparo = true;
        if (scriptCorpoACorpo != null) scriptCorpoACorpo.enabled = false;
        if (scriptSparo != null) scriptSparo.enabled = true;
    }

    private void AttivaModalitaMischia()
    {
        inModalitaSparo = false;
        if (scriptCorpoACorpo != null) scriptCorpoACorpo.enabled = true;
        if (scriptSparo != null) scriptSparo.enabled = false;
    }
}
