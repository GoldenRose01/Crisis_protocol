// ============================================================================
// Crisis Protocol / Sector Containment - Feedback ambientale
// File: .\Assets\script\LuceEmergenzaSettore.cs
// Responsabilita': controlla luci e segnali di emergenza collegati allo stato della missione.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using System.Collections; // usa lib // riga-ok
using System.Collections.Generic; // usa lib // riga-ok
using UnityEngine; // usa lib // riga-ok

/// <summary>
/// Gestisce le luci di emergenza e i cubi/lampade emettitori:
/// Lampeggiano in rosso finché l'emergenza è attiva, e si spengono automaticamente
/// quando il problema/focolaio viene risolto.
/// </summary>
// blocco: classe x roba grossa
public class LuceEmergenzaSettore : MonoBehaviour // classe qui // riga-ok
{ // apre // riga-ok
    // blocco: scelte rapide
    public enum ModalitaRilevamento // enum val // riga-ok
    { // apre // riga-ok
        [Tooltip("L'allarme si spegne quando tutti i focolai del settore sono contenuti (estrazione sbloccata).")] // nota unity // riga-ok
        TuttiIFocolaiSettore, // ok qua // riga-ok

        [Tooltip("L'allarme si spegne quando un focolaio specifico collegato viene contenuto.")] // nota unity // riga-ok
        FocolaioSpecifico, // ok qua // riga-ok

        [Tooltip("Controllato manualmente tramite codice o eventi esterni (es. trigger o terminali).")] // nota unity // riga-ok
        Manuale // ok qua // riga-ok
    } // chiude // riga-ok

    // blocco: scelte rapide
    public enum StileLampeggio // enum val // riga-ok
    { // apre // riga-ok
        [Tooltip("Acceso / Spento a intermittenza netta.")] // nota unity // riga-ok
        Blink, // ok qua // riga-ok

        [Tooltip("Pulsazione fluida e graduale (effetto sirena/respiro).")] // nota unity // riga-ok
        Pulsante // ok qua // riga-ok
    } // chiude // riga-ok

    [Header("Componenti Luce & Cubo")] // nota unity // riga-ok
    [Tooltip("La luce (o le luci) che illuminano la stanza/ambiente.")] // nota unity // riga-ok
    [SerializeField] private Light[] luciAmbientali; // ok qua // riga-ok

    [Tooltip("Il Cubo o la Mesh della lampada/sirena che emette la luce.")] // nota unity // riga-ok
    [SerializeField] private Renderer[] oggettiEmettitori; // ok qua // riga-ok

    [Header("Condizione di Emergenza")] // nota unity // riga-ok
    [Tooltip("Come determinare quando l'emergenza è risolta.")] // nota unity // riga-ok
    [SerializeField] private ModalitaRilevamento modalita = ModalitaRilevamento.TuttiIFocolaiSettore; // setta // riga-ok

    [Tooltip("Se la modalità è 'FocolaioSpecifico', trascina qui il focolaio che controlla questa stanza.")] // nota unity // riga-ok
    [SerializeField] private EmergencyHotspot focolaioCollegato; // ok qua // riga-ok

    [Tooltip("Stato iniziale all'avvio della scena.")] // nota unity // riga-ok
    [SerializeField] private bool emergenzaAttivaAllAvvio = true; // setta // riga-ok

    [Header("Configurazione Lampeggio")] // nota unity // riga-ok
    [SerializeField] private StileLampeggio stileLampeggio = StileLampeggio.Pulsante; // setta // riga-ok

    [Tooltip("Colore durante l'allarme.")] // nota unity // riga-ok
    [SerializeField] private Color coloreEmergenza = Color.red; // setta // riga-ok

    [Tooltip("Velocità del lampeggio (cicli al secondo).")] // nota unity // riga-ok
    [SerializeField] [Range(0.5f, 10f)] private float velocitaLampeggio = 2f; // setta // riga-ok

    [Tooltip("Intensità massima delle luci ambientali quando accese.")] // nota unity // riga-ok
    [SerializeField] private float intensitaMassimaLuce = 3f; // setta // riga-ok

    [Tooltip("Intensità dell'effetto glow/emissione sul materiale del Cubo.")] // nota unity // riga-ok
    [SerializeField] private float intensitaEmissioneCubo = 3f; // setta // riga-ok

    [Header("Stato Risolto (Quando il problema è finito)")] // nota unity // riga-ok
    [Tooltip("Se true, la luce e il cubo si spengono completamente. Se false, restano accesi fissi con il colore di standby.")] // nota unity // riga-ok
    [SerializeField] private bool spegniCompletamenteSuRisoluzione = true; // setta // riga-ok

    [Tooltip("Colore quando l'emergenza è terminata (usato solo se 'Spegni Completamente' è falso).")] // nota unity // riga-ok
    [SerializeField] private Color coloreStandbyRisolto = Color.green; // setta // riga-ok

    [Header("Audio Opzionale")] // nota unity // riga-ok
    [Tooltip("Sorgente audio con la sirena/allarme (opzionale: suona durante l'emergenza e si ferma alla risoluzione).")] // nota unity // riga-ok
    [SerializeField] private AudioSource sirenaAudio; // ok qua // riga-ok

    // Stato interno
    private bool emergenzaAttiva = false; // roba pub // riga-ok
    private float timerLampeggio = 0f; // roba pub // riga-ok
    private List<float> intensitaOriginaliLuci = new List<float>(); // roba pub // riga-ok

    // blocco: funzione fa cose
    private void Awake() // roba pub // riga-ok
    { // apre // riga-ok
        // Se non sono state assegnate luci nell'Inspector, cerca automaticamente tra i figli
        // blocco: controlla se va
        if (luciAmbientali == null || luciAmbientali.Length == 0) // se ok // riga-ok
        { // apre // riga-ok
            luciAmbientali = GetComponentsInChildren<Light>(); // setta // riga-ok
        } // chiude // riga-ok

        // Se non sono stati assegnati emettitori, cerca automaticamente tra i figli
        // blocco: controlla se va
        if (oggettiEmettitori == null || oggettiEmettitori.Length == 0) // se ok // riga-ok
        { // apre // riga-ok
            oggettiEmettitori = GetComponentsInChildren<Renderer>(); // setta // riga-ok
        } // chiude // riga-ok

        // Memorizza le intensità di partenza
        // blocco: gira piu volte
        foreach (Light l in luciAmbientali) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (l != null) // se ok // riga-ok
                intensitaOriginaliLuci.Add(l.intensity > 0f ? l.intensity : intensitaMassimaLuce); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnEnable() // roba pub // riga-ok
    { // apre // riga-ok
        MissionManager.OnEstrazioneSbloccata += OnStatoEstrazioneCambiato; // setta // riga-ok
        MissionManager.OnFocolaiCambiati += OnFocolaiAggiornati; // setta // riga-ok

        // blocco: controlla se va
        if (focolaioCollegato != null) // se ok // riga-ok
            focolaioCollegato.OnFocolaioContenuto += OnFocolaioRisolto; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnDisable() // roba pub // riga-ok
    { // apre // riga-ok
        MissionManager.OnEstrazioneSbloccata -= OnStatoEstrazioneCambiato; // setta // riga-ok
        MissionManager.OnFocolaiCambiati -= OnFocolaiAggiornati; // setta // riga-ok

        // blocco: controlla se va
        if (focolaioCollegato != null) // se ok // riga-ok
            focolaioCollegato.OnFocolaioContenuto -= OnFocolaioRisolto; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void Start() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (modalita == ModalitaRilevamento.TuttiIFocolaiSettore) // se ok // riga-ok
        { // apre // riga-ok
            bool tuttiRisolti = MissionManager.Instance != null && MissionManager.Instance.EstrazioneSbloccata; // setta // riga-ok
            SetStatoEmergenza(!tuttiRisolti); // chiama // riga-ok
        } // chiude // riga-ok
        // blocco: controlla se va
        else if (modalita == ModalitaRilevamento.FocolaioSpecifico && focolaioCollegato != null) // se ok // riga-ok
        { // apre // riga-ok
            SetStatoEmergenza(!focolaioCollegato.Contenuto); // chiama // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            SetStatoEmergenza(emergenzaAttivaAllAvvio); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnFocolaioRisolto() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (modalita == ModalitaRilevamento.FocolaioSpecifico) // se ok // riga-ok
        { // apre // riga-ok
            SetStatoEmergenza(false); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void Update() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (!emergenzaAttiva) // se ok // riga-ok
            return; // torna val // riga-ok

        EseguiLampeggio(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void EseguiLampeggio() // roba pub // riga-ok
    { // apre // riga-ok
        timerLampeggio += Time.deltaTime * velocitaLampeggio; // setta // riga-ok
        float fattoreLuminosita = 0f; // setta // riga-ok

        // blocco: controlla se va
        if (stileLampeggio == StileLampeggio.Blink) // se ok // riga-ok
        { // apre // riga-ok
            // Lampeggio netto 0 o 1
            fattoreLuminosita = (Mathf.Sin(timerLampeggio * Mathf.PI * 2f) > 0f) ? 1f : 0f; // setta // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // Pulsante // se no // riga-ok
        { // apre // riga-ok
            // Curva fluida tra 0 e 1
            fattoreLuminosita = (Mathf.Sin(timerLampeggio * Mathf.PI * 2f) + 1f) * 0.5f; // setta // riga-ok
            fattoreLuminosita = Mathf.SmoothStep(0f, 1f, fattoreLuminosita); // setta // riga-ok
        } // chiude // riga-ok

        // Aggiorna le luci ambientali
        // blocco: gira piu volte
        for (int i = 0; i < luciAmbientali.Length; i++) // ciclo x // riga-ok
        { // apre // riga-ok
            Light l = luciAmbientali[i]; // setta // riga-ok
            // blocco: controlla se va
            if (l != null) // se ok // riga-ok
            { // apre // riga-ok
                l.enabled = fattoreLuminosita > 0.01f; // setta // riga-ok
                l.color = coloreEmergenza; // setta // riga-ok
                float maxInt = (i < intensitaOriginaliLuci.Count) ? intensitaOriginaliLuci[i] : intensitaMassimaLuce; // setta // riga-ok
                l.intensity = maxInt * fattoreLuminosita; // setta // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // Aggiorna i cubi / oggetti emettitori
        // blocco: gira piu volte
        foreach (Renderer rend in oggettiEmettitori) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (rend != null) // se ok // riga-ok
            { // apre // riga-ok
                Material mat = rend.material; // setta // riga-ok
                // blocco: controlla se va
                if (mat != null) // se ok // riga-ok
                { // apre // riga-ok
                    Color c = coloreEmergenza * fattoreLuminosita; // setta // riga-ok
                    mat.color = coloreEmergenza; // setta // riga-ok

                    // blocco: controlla se va
                    if (mat.HasProperty("_BaseColor")) // se ok // riga-ok
                        mat.SetColor("_BaseColor", coloreEmergenza); // chiama // riga-ok

                    // blocco: controlla se va
                    if (mat.HasProperty("_EmissionColor")) // se ok // riga-ok
                    { // apre // riga-ok
                        mat.EnableKeyword("_EMISSION"); // chiama // riga-ok
                        mat.SetColor("_EmissionColor", c * intensitaEmissioneCubo); // chiama // riga-ok
                    } // chiude // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    /// <summary>
    /// Attiva o disattiva lo stato di emergenza.
    /// </summary>
    // blocco: funzione fa cose
    public void SetStatoEmergenza(bool attiva) // roba pub // riga-ok
    { // apre // riga-ok
        emergenzaAttiva = attiva; // setta // riga-ok

        // blocco: controlla se va
        if (sirenaAudio != null) // se ok // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (emergenzaAttiva && !sirenaAudio.isPlaying) // se ok // riga-ok
                sirenaAudio.Play(); // chiama // riga-ok
            // blocco: controlla se va
            else if (!emergenzaAttiva && sirenaAudio.isPlaying) // se ok // riga-ok
                sirenaAudio.Stop(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (!emergenzaAttiva) // se ok // riga-ok
        { // apre // riga-ok
            ApplicaStatoRisolto(); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    /// <summary>
    /// Chiamato quando l'emergenza viene risolta: spegne o imposta a standby luci e cubi.
    /// </summary>
    // blocco: funzione fa cose
    private void ApplicaStatoRisolto() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: gira piu volte
        foreach (Light l in luciAmbientali) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (l != null) // se ok // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (spegniCompletamenteSuRisoluzione) // se ok // riga-ok
                { // apre // riga-ok
                    l.enabled = false; // setta // riga-ok
                    l.intensity = 0f; // setta // riga-ok
                } // chiude // riga-ok
                // blocco: caso diverso
                else // se no // riga-ok
                { // apre // riga-ok
                    l.enabled = true; // setta // riga-ok
                    l.color = coloreStandbyRisolto; // setta // riga-ok
                    l.intensity = intensitaMassimaLuce * 0.5f; // setta // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // blocco: gira piu volte
        foreach (Renderer rend in oggettiEmettitori) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (rend != null) // se ok // riga-ok
            { // apre // riga-ok
                Material mat = rend.material; // setta // riga-ok
                // blocco: controlla se va
                if (mat != null) // se ok // riga-ok
                { // apre // riga-ok
                    // blocco: controlla se va
                    if (spegniCompletamenteSuRisoluzione) // se ok // riga-ok
                    { // apre // riga-ok
                        // blocco: controlla se va
                        if (mat.HasProperty("_EmissionColor")) // se ok // riga-ok
                            mat.SetColor("_EmissionColor", Color.black); // chiama // riga-ok
                    } // chiude // riga-ok
                    // blocco: caso diverso
                    else // se no // riga-ok
                    { // apre // riga-ok
                        mat.color = coloreStandbyRisolto; // setta // riga-ok
                        // blocco: controlla se va
                        if (mat.HasProperty("_BaseColor")) // se ok // riga-ok
                            mat.SetColor("_BaseColor", coloreStandbyRisolto); // chiama // riga-ok

                        // blocco: controlla se va
                        if (mat.HasProperty("_EmissionColor")) // se ok // riga-ok
                        { // apre // riga-ok
                            mat.EnableKeyword("_EMISSION"); // chiama // riga-ok
                            mat.SetColor("_EmissionColor", coloreStandbyRisolto * intensitaEmissioneCubo); // chiama // riga-ok
                        } // chiude // riga-ok
                    } // chiude // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        Debug.Log($"<color=cyan>[ALLARME]</color> Emergenza su '{name}' RISOLTA: luci e cubi spenti."); // logga // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnStatoEstrazioneCambiato(bool estrazioneSbloccata) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (modalita == ModalitaRilevamento.TuttiIFocolaiSettore) // se ok // riga-ok
        { // apre // riga-ok
            SetStatoEmergenza(!estrazioneSbloccata); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnFocolaiAggiornati(int contenuti, int totali) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (modalita == ModalitaRilevamento.TuttiIFocolaiSettore) // se ok // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (contenuti >= totali && totali > 0) // se ok // riga-ok
                SetStatoEmergenza(false); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    /// <summary>
    /// Metodo pubblico per risolvere manualmente l'emergenza da un trigger, bottone o UnityEvent.
    /// </summary>
    // blocco: funzione fa cose
    public void RisolviEmergenza() // roba pub // riga-ok
    { // apre // riga-ok
        SetStatoEmergenza(false); // chiama // riga-ok
    } // chiude // riga-ok

    /// <summary>
    /// Metodo pubblico per attivare manualmente l'allarme da un trigger, bottone o UnityEvent.
    /// </summary>
    // blocco: funzione fa cose
    public void InnescaEmergenza() // roba pub // riga-ok
    { // apre // riga-ok
        SetStatoEmergenza(true); // chiama // riga-ok
    } // chiude // riga-ok

    [ContextMenu("Test Risolvi Emergenza (Spegni)")] // nota unity // riga-ok
    // blocco: funzione fa cose
    public void TestRisolvi() // roba pub // riga-ok
    { // apre // riga-ok
        SetStatoEmergenza(false); // chiama // riga-ok
    } // chiude // riga-ok

    [ContextMenu("Test Innesca Emergenza (Lampeggia)")] // nota unity // riga-ok
    // blocco: funzione fa cose
    public void TestInnesca() // roba pub // riga-ok
    { // apre // riga-ok
        SetStatoEmergenza(true); // chiama // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
