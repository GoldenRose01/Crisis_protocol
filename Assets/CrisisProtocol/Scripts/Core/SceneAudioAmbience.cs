// ============================================================================
// Crisis Protocol / Sector Containment - Core runtime
// File: .\Assets\CrisisProtocol\Scripts\Core\SceneAudioAmbience.cs
// Responsabilita': coordina stato globale, salvataggi, avanzamento partita o servizi persistenti condivisi tra scene.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine; // usa lib // riga-ok

/// <summary>
/// Gestore centrale dell'audio ambientale per la scena (Settore 2).
/// Gestisce la musica/atmosfera di sottofondo globale 2D e i punti di emissione 3D per le varie zone:
/// - Sottofondo Globale di Tensione (2D)
/// - Sala Generatore (Zona Rossa - Ronzio Elettrico e Compressore Industriale 3D)
/// - Laboratorio Chimico (Zona Verde - Ribollio Serbatoi e Liquidi 3D)
/// - Corridoi di Ventilazione (Fruscio Aria Condizionata 3D)
/// </summary>
// blocco: classe x roba grossa
public class SceneAudioAmbience : MonoBehaviour // classe qui // riga-ok
{ // apre // riga-ok
    public static SceneAudioAmbience Instance { get; private set; } // roba pub // riga-ok

    [Header("1. Sottofondo Globale Tensione (2D)")] // nota unity // riga-ok
    [Tooltip("Loop sonoro di atmosfera/tensione a basso volume su tutta la scena.")] // nota unity // riga-ok
    public AudioClip musicaAtmosferaGlobale; // roba pub // riga-ok
    [Range(0f, 1f)] public float volumeGlobale = 0.22f; // setta // riga-ok

    [Header("2. Zona Rossa - Sala Generatore (3D)")] // nota unity // riga-ok
    [Tooltip("Ronzio motore/compressore industriale continuo.")] // nota unity // riga-ok
    public AudioClip suonoGeneratoreIndustriale; // roba pub // riga-ok
    [Tooltip("Ronzio lampade fluorescenti / scarica elettrica.")] // nota unity // riga-ok
    public AudioClip suonoRonzioElettrico; // roba pub // riga-ok
    public Transform posizioneSalaGeneratore; // roba pub // riga-ok
    [Range(0f, 1f)] public float volumeSalaGeneratore = 0.55f; // setta // riga-ok

    [Header("3. Zona Verde - Laboratorio Chimico (3D)")] // nota unity // riga-ok
    [Tooltip("Ribollio continuo delle vasche e serbatoi chimici.")] // nota unity // riga-ok
    public AudioClip suonoRibollioChimico; // roba pub // riga-ok
    [Tooltip("Gorgoglio o scorrimento liquidi.")] // nota unity // riga-ok
    public AudioClip suonoLiquidiLaboratorio; // roba pub // riga-ok
    public Transform posizioneLaboratorioChimico; // roba pub // riga-ok
    [Range(0f, 1f)] public float volumeLaboratorioChimico = 0.5f; // setta // riga-ok

    [Header("4. Corridoi e Struttura (3D)")] // nota unity // riga-ok
    [Tooltip("Flusso d'aria nelle condotte di ventilazione dei corridoi.")] // nota unity // riga-ok
    public AudioClip suonoVentilazioneCorridoi; // roba pub // riga-ok
    public Transform posizioneCorridoi; // roba pub // riga-ok
    [Range(0f, 1f)] public float volumeCorridoi = 0.45f; // setta // riga-ok

    private AudioSource audioSourceGlobale; // roba pub // riga-ok
    private AudioSource audioSourceGeneratore; // roba pub // riga-ok
    private AudioSource audioSourceGeneratoreLuce; // roba pub // riga-ok
    private AudioSource audioSourceChimica; // roba pub // riga-ok
    private AudioSource audioSourceCorridoi; // roba pub // riga-ok

    // blocco: funzione fa cose
    private void Awake() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (Instance == null) // se ok // riga-ok
        { // apre // riga-ok
            Instance = this; // setta // riga-ok
        } // chiude // riga-ok
        // blocco: controlla se va
        else if (Instance != this) // se ok // riga-ok
        { // apre // riga-ok
            Destroy(gameObject); // elimina // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        InizializzaTuttiGliAudioSource(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void Start() // roba pub // riga-ok
    { // apre // riga-ok
        AvviaTuttiISuoniAmbientali(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void InizializzaTuttiGliAudioSource() // roba pub // riga-ok
    { // apre // riga-ok
        // 1. Audio Source Globale 2D
        // blocco: controlla se va
        if (audioSourceGlobale == null) // se ok // riga-ok
        { // apre // riga-ok
            Transform tGlob = transform.Find("Ambience_Global_2D"); // setta // riga-ok
            // blocco: controlla se va
            if (tGlob == null) // se ok // riga-ok
            { // apre // riga-ok
                GameObject go = new GameObject("Ambience_Global_2D"); // setta // riga-ok
                go.transform.SetParent(transform, false); // chiama // riga-ok
                audioSourceGlobale = go.AddComponent<AudioSource>(); // setta // riga-ok
            } // chiude // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
            { // apre // riga-ok
                audioSourceGlobale = tGlob.GetComponent<AudioSource>(); // setta // riga-ok
            } // chiude // riga-ok

            audioSourceGlobale.spatialBlend = 0f; // 2D Puro // setta // riga-ok
            audioSourceGlobale.loop = true; // setta // riga-ok
            audioSourceGlobale.playOnAwake = false; // setta // riga-ok
            audioSourceGlobale.volume = volumeGlobale; // setta // riga-ok
        } // chiude // riga-ok

        // 2. Audio Source Sala Generatore 3D
        Vector3 posGen = (posizioneSalaGeneratore != null) ? posizioneSalaGeneratore.position : new Vector3(-12f, 1.5f, 18f); // setta // riga-ok
        // blocco: controlla se va
        if (audioSourceGeneratore == null) // se ok // riga-ok
        { // apre // riga-ok
            audioSourceGeneratore = CreaEmettitore3D("Ambience_SalaGeneratore_Hum", posGen, 2.5f, 22f, volumeSalaGeneratore); // setta // riga-ok
        } // chiude // riga-ok
        // blocco: controlla se va
        if (audioSourceGeneratoreLuce == null) // se ok // riga-ok
        { // apre // riga-ok
            audioSourceGeneratoreLuce = CreaEmettitore3D("Ambience_SalaGeneratore_Buzz", posGen + Vector3.up * 1.5f, 2.0f, 18f, volumeSalaGeneratore * 0.7f); // setta // riga-ok
        } // chiude // riga-ok

        // 3. Audio Source Laboratorio Chimico 3D
        Vector3 posChem = (posizioneLaboratorioChimico != null) ? posizioneLaboratorioChimico.position : new Vector3(14f, 1.5f, -12f); // setta // riga-ok
        // blocco: controlla se va
        if (audioSourceChimica == null) // se ok // riga-ok
        { // apre // riga-ok
            audioSourceChimica = CreaEmettitore3D("Ambience_LabChimico_Bubbles", posChem, 2.5f, 20f, volumeLaboratorioChimico); // setta // riga-ok
        } // chiude // riga-ok

        // 4. Audio Source Corridoi 3D
        Vector3 posCorr = (posizioneCorridoi != null) ? posizioneCorridoi.position : new Vector3(0f, 2.0f, 0f); // setta // riga-ok
        // blocco: controlla se va
        if (audioSourceCorridoi == null) // se ok // riga-ok
        { // apre // riga-ok
            audioSourceCorridoi = CreaEmettitore3D("Ambience_Corridoi_Ventilation", posCorr, 3.0f, 25f, volumeCorridoi); // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private AudioSource CreaEmettitore3D(string nome, Vector3 posizione, float minDistance, float maxDistance, float volume) // roba pub // riga-ok
    { // apre // riga-ok
        Transform esistente = transform.Find(nome); // setta // riga-ok
        GameObject go; // ok qua // riga-ok
        AudioSource src; // ok qua // riga-ok

        // blocco: controlla se va
        if (esistente == null) // se ok // riga-ok
        { // apre // riga-ok
            go = new GameObject(nome); // setta // riga-ok
            go.transform.SetParent(transform, true); // chiama // riga-ok
            go.transform.position = posizione; // setta // riga-ok
            src = go.AddComponent<AudioSource>(); // setta // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            go = esistente.gameObject; // setta // riga-ok
            go.transform.position = posizione; // setta // riga-ok
            src = go.GetComponent<AudioSource>() ?? go.AddComponent<AudioSource>(); // setta // riga-ok
        } // chiude // riga-ok

        src.spatialBlend = 1.0f; // 3D // setta // riga-ok
        src.rolloffMode = AudioRolloffMode.Logarithmic; // setta // riga-ok
        src.minDistance = minDistance; // setta // riga-ok
        src.maxDistance = maxDistance; // setta // riga-ok
        src.loop = true; // setta // riga-ok
        src.playOnAwake = false; // setta // riga-ok
        src.volume = volume; // setta // riga-ok
        src.dopplerLevel = 0f; // setta // riga-ok

        return src; // torna val // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void AvviaTuttiISuoniAmbientali() // roba pub // riga-ok
    { // apre // riga-ok
        InizializzaTuttiGliAudioSource(); // chiama // riga-ok

        // Globale 2D
        // blocco: controlla se va
        if (musicaAtmosferaGlobale != null && audioSourceGlobale != null) // se ok // riga-ok
        { // apre // riga-ok
            audioSourceGlobale.clip = musicaAtmosferaGlobale; // setta // riga-ok
            audioSourceGlobale.volume = volumeGlobale; // setta // riga-ok
            // blocco: controlla se va
            if (!audioSourceGlobale.isPlaying) audioSourceGlobale.Play(); // se ok // riga-ok
        } // chiude // riga-ok

        // Generatore
        // blocco: controlla se va
        if (suonoGeneratoreIndustriale != null && audioSourceGeneratore != null) // se ok // riga-ok
        { // apre // riga-ok
            audioSourceGeneratore.clip = suonoGeneratoreIndustriale; // setta // riga-ok
            audioSourceGeneratore.volume = volumeSalaGeneratore; // setta // riga-ok
            // blocco: controlla se va
            if (!audioSourceGeneratore.isPlaying) audioSourceGeneratore.Play(); // se ok // riga-ok
        } // chiude // riga-ok
        // blocco: controlla se va
        if (suonoRonzioElettrico != null && audioSourceGeneratoreLuce != null) // se ok // riga-ok
        { // apre // riga-ok
            audioSourceGeneratoreLuce.clip = suonoRonzioElettrico; // setta // riga-ok
            audioSourceGeneratoreLuce.volume = volumeSalaGeneratore * 0.7f; // setta // riga-ok
            // blocco: controlla se va
            if (!audioSourceGeneratoreLuce.isPlaying) audioSourceGeneratoreLuce.Play(); // se ok // riga-ok
        } // chiude // riga-ok

        // Chimica
        // blocco: controlla se va
        if (suonoRibollioChimico != null && audioSourceChimica != null) // se ok // riga-ok
        { // apre // riga-ok
            audioSourceChimica.clip = suonoRibollioChimico; // setta // riga-ok
            audioSourceChimica.volume = volumeLaboratorioChimico; // setta // riga-ok
            // blocco: controlla se va
            if (!audioSourceChimica.isPlaying) audioSourceChimica.Play(); // se ok // riga-ok
        } // chiude // riga-ok

        // Corridoi
        // blocco: controlla se va
        if (suonoVentilazioneCorridoi != null && audioSourceCorridoi != null) // se ok // riga-ok
        { // apre // riga-ok
            audioSourceCorridoi.clip = suonoVentilazioneCorridoi; // setta // riga-ok
            audioSourceCorridoi.volume = volumeCorridoi; // setta // riga-ok
            // blocco: controlla se va
            if (!audioSourceCorridoi.isPlaying) audioSourceCorridoi.Play(); // se ok // riga-ok
        } // chiude // riga-ok

        Debug.Log("<color=cyan>[AUDIO AMBIENTALE]</color> Tutti gli emettitori sonori 3D e il sottofondo globale sono attivi!"); // logga // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
