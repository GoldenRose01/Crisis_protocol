// ============================================================================
// Crisis Protocol / Sector Containment - Core runtime
// File: .\Assets\CrisisProtocol\Scripts\Core\SceneAudioAmbience.cs
// Responsabilita': coordina stato globale, salvataggi, avanzamento partita o servizi persistenti condivisi tra scene.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine;

/// <summary>
/// Gestore centrale dell'audio ambientale per la scena (Settore 2).
/// Gestisce la musica/atmosfera di sottofondo globale 2D e i punti di emissione 3D per le varie zone:
/// - Sottofondo Globale di Tensione (2D)
/// - Sala Generatore (Zona Rossa - Ronzio Elettrico e Compressore Industriale 3D)
/// - Laboratorio Chimico (Zona Verde - Ribollio Serbatoi e Liquidi 3D)
/// - Corridoi di Ventilazione (Fruscio Aria Condizionata 3D)
/// </summary>
public class SceneAudioAmbience : MonoBehaviour
{
    public static SceneAudioAmbience Instance { get; private set; }

    [Header("1. Sottofondo Globale Tensione (2D)")]
    [Tooltip("Loop sonoro di atmosfera/tensione a basso volume su tutta la scena.")]
    public AudioClip musicaAtmosferaGlobale;
    [Range(0f, 1f)] public float volumeGlobale = 0.22f;

    [Header("2. Zona Rossa - Sala Generatore (3D)")]
    [Tooltip("Ronzio motore/compressore industriale continuo.")]
    public AudioClip suonoGeneratoreIndustriale;
    [Tooltip("Ronzio lampade fluorescenti / scarica elettrica.")]
    public AudioClip suonoRonzioElettrico;
    public Transform posizioneSalaGeneratore;
    [Range(0f, 1f)] public float volumeSalaGeneratore = 0.55f;

    [Header("3. Zona Verde - Laboratorio Chimico (3D)")]
    [Tooltip("Ribollio continuo delle vasche e serbatoi chimici.")]
    public AudioClip suonoRibollioChimico;
    [Tooltip("Gorgoglio o scorrimento liquidi.")]
    public AudioClip suonoLiquidiLaboratorio;
    public Transform posizioneLaboratorioChimico;
    [Range(0f, 1f)] public float volumeLaboratorioChimico = 0.5f;

    [Header("4. Corridoi e Struttura (3D)")]
    [Tooltip("Flusso d'aria nelle condotte di ventilazione dei corridoi.")]
    public AudioClip suonoVentilazioneCorridoi;
    public Transform posizioneCorridoi;
    [Range(0f, 1f)] public float volumeCorridoi = 0.45f;

    private AudioSource audioSourceGlobale;
    private AudioSource audioSourceGeneratore;
    private AudioSource audioSourceGeneratoreLuce;
    private AudioSource audioSourceChimica;
    private AudioSource audioSourceCorridoi;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        InizializzaTuttiGliAudioSource();
    }

    private void Start()
    {
        AvviaTuttiISuoniAmbientali();
    }

    public void InizializzaTuttiGliAudioSource()
    {
        // 1. Audio Source Globale 2D
        if (audioSourceGlobale == null)
        {
            Transform tGlob = transform.Find("Ambience_Global_2D");
            if (tGlob == null)
            {
                GameObject go = new GameObject("Ambience_Global_2D");
                go.transform.SetParent(transform, false);
                audioSourceGlobale = go.AddComponent<AudioSource>();
            }
            else
            {
                audioSourceGlobale = tGlob.GetComponent<AudioSource>();
            }

            audioSourceGlobale.spatialBlend = 0f; // 2D Puro
            audioSourceGlobale.loop = true;
            audioSourceGlobale.playOnAwake = false;
            audioSourceGlobale.volume = volumeGlobale;
        }

        // 2. Audio Source Sala Generatore 3D
        Vector3 posGen = (posizioneSalaGeneratore != null) ? posizioneSalaGeneratore.position : new Vector3(-12f, 1.5f, 18f);
        if (audioSourceGeneratore == null)
        {
            audioSourceGeneratore = CreaEmettitore3D("Ambience_SalaGeneratore_Hum", posGen, 2.5f, 22f, volumeSalaGeneratore);
        }
        if (audioSourceGeneratoreLuce == null)
        {
            audioSourceGeneratoreLuce = CreaEmettitore3D("Ambience_SalaGeneratore_Buzz", posGen + Vector3.up * 1.5f, 2.0f, 18f, volumeSalaGeneratore * 0.7f);
        }

        // 3. Audio Source Laboratorio Chimico 3D
        Vector3 posChem = (posizioneLaboratorioChimico != null) ? posizioneLaboratorioChimico.position : new Vector3(14f, 1.5f, -12f);
        if (audioSourceChimica == null)
        {
            audioSourceChimica = CreaEmettitore3D("Ambience_LabChimico_Bubbles", posChem, 2.5f, 20f, volumeLaboratorioChimico);
        }

        // 4. Audio Source Corridoi 3D
        Vector3 posCorr = (posizioneCorridoi != null) ? posizioneCorridoi.position : new Vector3(0f, 2.0f, 0f);
        if (audioSourceCorridoi == null)
        {
            audioSourceCorridoi = CreaEmettitore3D("Ambience_Corridoi_Ventilation", posCorr, 3.0f, 25f, volumeCorridoi);
        }
    }

    private AudioSource CreaEmettitore3D(string nome, Vector3 posizione, float minDistance, float maxDistance, float volume)
    {
        Transform esistente = transform.Find(nome);
        GameObject go;
        AudioSource src;

        if (esistente == null)
        {
            go = new GameObject(nome);
            go.transform.SetParent(transform, true);
            go.transform.position = posizione;
            src = go.AddComponent<AudioSource>();
        }
        else
        {
            go = esistente.gameObject;
            go.transform.position = posizione;
            src = go.GetComponent<AudioSource>() ?? go.AddComponent<AudioSource>();
        }

        src.spatialBlend = 1.0f; // 3D
        src.rolloffMode = AudioRolloffMode.Logarithmic;
        src.minDistance = minDistance;
        src.maxDistance = maxDistance;
        src.loop = true;
        src.playOnAwake = false;
        src.volume = volume;
        src.dopplerLevel = 0f;

        return src;
    }

    public void AvviaTuttiISuoniAmbientali()
    {
        InizializzaTuttiGliAudioSource();

        // Globale 2D
        if (musicaAtmosferaGlobale != null && audioSourceGlobale != null)
        {
            audioSourceGlobale.clip = musicaAtmosferaGlobale;
            audioSourceGlobale.volume = volumeGlobale;
            if (!audioSourceGlobale.isPlaying) audioSourceGlobale.Play();
        }

        // Generatore
        if (suonoGeneratoreIndustriale != null && audioSourceGeneratore != null)
        {
            audioSourceGeneratore.clip = suonoGeneratoreIndustriale;
            audioSourceGeneratore.volume = volumeSalaGeneratore;
            if (!audioSourceGeneratore.isPlaying) audioSourceGeneratore.Play();
        }
        if (suonoRonzioElettrico != null && audioSourceGeneratoreLuce != null)
        {
            audioSourceGeneratoreLuce.clip = suonoRonzioElettrico;
            audioSourceGeneratoreLuce.volume = volumeSalaGeneratore * 0.7f;
            if (!audioSourceGeneratoreLuce.isPlaying) audioSourceGeneratoreLuce.Play();
        }

        // Chimica
        if (suonoRibollioChimico != null && audioSourceChimica != null)
        {
            audioSourceChimica.clip = suonoRibollioChimico;
            audioSourceChimica.volume = volumeLaboratorioChimico;
            if (!audioSourceChimica.isPlaying) audioSourceChimica.Play();
        }

        // Corridoi
        if (suonoVentilazioneCorridoi != null && audioSourceCorridoi != null)
        {
            audioSourceCorridoi.clip = suonoVentilazioneCorridoi;
            audioSourceCorridoi.volume = volumeCorridoi;
            if (!audioSourceCorridoi.isPlaying) audioSourceCorridoi.Play();
        }

        Debug.Log("<color=cyan>[AUDIO AMBIENTALE]</color> Tutti gli emettitori sonori 3D e il sottofondo globale sono attivi!");
    }
}
