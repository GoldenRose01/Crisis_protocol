using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestisce le luci di emergenza e i cubi/lampade emettitori:
/// Lampeggiano in rosso finché l'emergenza è attiva, e si spengono automaticamente
/// quando il problema/focolaio viene risolto.
/// </summary>
public class LuceEmergenzaSettore : MonoBehaviour
{
    public enum ModalitaRilevamento
    {
        [Tooltip("L'allarme si spegne quando tutti i focolai del settore sono contenuti (estrazione sbloccata).")]
        TuttiIFocolaiSettore,

        [Tooltip("L'allarme si spegne quando un focolaio specifico collegato viene contenuto.")]
        FocolaioSpecifico,

        [Tooltip("Controllato manualmente tramite codice o eventi esterni (es. trigger o terminali).")]
        Manuale
    }

    public enum StileLampeggio
    {
        [Tooltip("Acceso / Spento a intermittenza netta.")]
        Blink,

        [Tooltip("Pulsazione fluida e graduale (effetto sirena/respiro).")]
        Pulsante
    }

    [Header("Componenti Luce & Cubo")]
    [Tooltip("La luce (o le luci) che illuminano la stanza/ambiente.")]
    [SerializeField] private Light[] luciAmbientali;

    [Tooltip("Il Cubo o la Mesh della lampada/sirena che emette la luce.")]
    [SerializeField] private Renderer[] oggettiEmettitori;

    [Header("Condizione di Emergenza")]
    [Tooltip("Come determinare quando l'emergenza è risolta.")]
    [SerializeField] private ModalitaRilevamento modalita = ModalitaRilevamento.TuttiIFocolaiSettore;

    [Tooltip("Se la modalità è 'FocolaioSpecifico', trascina qui il focolaio che controlla questa stanza.")]
    [SerializeField] private EmergencyHotspot focolaioCollegato;

    [Tooltip("Stato iniziale all'avvio della scena.")]
    [SerializeField] private bool emergenzaAttivaAllAvvio = true;

    [Header("Configurazione Lampeggio")]
    [SerializeField] private StileLampeggio stileLampeggio = StileLampeggio.Pulsante;

    [Tooltip("Colore durante l'allarme.")]
    [SerializeField] private Color coloreEmergenza = Color.red;

    [Tooltip("Velocità del lampeggio (cicli al secondo).")]
    [SerializeField] [Range(0.5f, 10f)] private float velocitaLampeggio = 2f;

    [Tooltip("Intensità massima delle luci ambientali quando accese.")]
    [SerializeField] private float intensitaMassimaLuce = 3f;

    [Tooltip("Intensità dell'effetto glow/emissione sul materiale del Cubo.")]
    [SerializeField] private float intensitaEmissioneCubo = 3f;

    [Header("Stato Risolto (Quando il problema è finito)")]
    [Tooltip("Se true, la luce e il cubo si spengono completamente. Se false, restano accesi fissi con il colore di standby.")]
    [SerializeField] private bool spegniCompletamenteSuRisoluzione = true;

    [Tooltip("Colore quando l'emergenza è terminata (usato solo se 'Spegni Completamente' è falso).")]
    [SerializeField] private Color coloreStandbyRisolto = Color.green;

    [Header("Audio Opzionale")]
    [Tooltip("Sorgente audio con la sirena/allarme (opzionale: suona durante l'emergenza e si ferma alla risoluzione).")]
    [SerializeField] private AudioSource sirenaAudio;

    // Stato interno
    private bool emergenzaAttiva = false;
    private float timerLampeggio = 0f;
    private List<float> intensitaOriginaliLuci = new List<float>();

    private void Awake()
    {
        // Se non sono state assegnate luci nell'Inspector, cerca automaticamente tra i figli
        if (luciAmbientali == null || luciAmbientali.Length == 0)
        {
            luciAmbientali = GetComponentsInChildren<Light>();
        }

        // Se non sono stati assegnati emettitori, cerca automaticamente tra i figli
        if (oggettiEmettitori == null || oggettiEmettitori.Length == 0)
        {
            oggettiEmettitori = GetComponentsInChildren<Renderer>();
        }

        // Memorizza le intensità di partenza
        foreach (Light l in luciAmbientali)
        {
            if (l != null)
                intensitaOriginaliLuci.Add(l.intensity > 0f ? l.intensity : intensitaMassimaLuce);
        }
    }

    private void OnEnable()
    {
        MissionManager.OnEstrazioneSbloccata += OnStatoEstrazioneCambiato;
        MissionManager.OnFocolaiCambiati += OnFocolaiAggiornati;

        if (focolaioCollegato != null)
            focolaioCollegato.OnFocolaioContenuto += OnFocolaioRisolto;
    }

    private void OnDisable()
    {
        MissionManager.OnEstrazioneSbloccata -= OnStatoEstrazioneCambiato;
        MissionManager.OnFocolaiCambiati -= OnFocolaiAggiornati;

        if (focolaioCollegato != null)
            focolaioCollegato.OnFocolaioContenuto -= OnFocolaioRisolto;
    }

    private void Start()
    {
        if (modalita == ModalitaRilevamento.TuttiIFocolaiSettore)
        {
            bool tuttiRisolti = MissionManager.Instance != null && MissionManager.Instance.EstrazioneSbloccata;
            SetStatoEmergenza(!tuttiRisolti);
        }
        else if (modalita == ModalitaRilevamento.FocolaioSpecifico && focolaioCollegato != null)
        {
            SetStatoEmergenza(!focolaioCollegato.Contenuto);
        }
        else
        {
            SetStatoEmergenza(emergenzaAttivaAllAvvio);
        }
    }

    private void OnFocolaioRisolto()
    {
        if (modalita == ModalitaRilevamento.FocolaioSpecifico)
        {
            SetStatoEmergenza(false);
        }
    }

    private void Update()
    {
        if (!emergenzaAttiva)
            return;

        EseguiLampeggio();
    }

    private void EseguiLampeggio()
    {
        timerLampeggio += Time.deltaTime * velocitaLampeggio;
        float fattoreLuminosita = 0f;

        if (stileLampeggio == StileLampeggio.Blink)
        {
            // Lampeggio netto 0 o 1
            fattoreLuminosita = (Mathf.Sin(timerLampeggio * Mathf.PI * 2f) > 0f) ? 1f : 0f;
        }
        else // Pulsante
        {
            // Curva fluida tra 0 e 1
            fattoreLuminosita = (Mathf.Sin(timerLampeggio * Mathf.PI * 2f) + 1f) * 0.5f;
            fattoreLuminosita = Mathf.SmoothStep(0f, 1f, fattoreLuminosita);
        }

        // Aggiorna le luci ambientali
        for (int i = 0; i < luciAmbientali.Length; i++)
        {
            Light l = luciAmbientali[i];
            if (l != null)
            {
                l.enabled = fattoreLuminosita > 0.01f;
                l.color = coloreEmergenza;
                float maxInt = (i < intensitaOriginaliLuci.Count) ? intensitaOriginaliLuci[i] : intensitaMassimaLuce;
                l.intensity = maxInt * fattoreLuminosita;
            }
        }

        // Aggiorna i cubi / oggetti emettitori
        foreach (Renderer rend in oggettiEmettitori)
        {
            if (rend != null)
            {
                Material mat = rend.material;
                if (mat != null)
                {
                    Color c = coloreEmergenza * fattoreLuminosita;
                    mat.color = coloreEmergenza;

                    if (mat.HasProperty("_BaseColor"))
                        mat.SetColor("_BaseColor", coloreEmergenza);

                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", c * intensitaEmissioneCubo);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Attiva o disattiva lo stato di emergenza.
    /// </summary>
    public void SetStatoEmergenza(bool attiva)
    {
        emergenzaAttiva = attiva;

        if (sirenaAudio != null)
        {
            if (emergenzaAttiva && !sirenaAudio.isPlaying)
                sirenaAudio.Play();
            else if (!emergenzaAttiva && sirenaAudio.isPlaying)
                sirenaAudio.Stop();
        }

        if (!emergenzaAttiva)
        {
            ApplicaStatoRisolto();
        }
    }

    /// <summary>
    /// Chiamato quando l'emergenza viene risolta: spegne o imposta a standby luci e cubi.
    /// </summary>
    private void ApplicaStatoRisolto()
    {
        foreach (Light l in luciAmbientali)
        {
            if (l != null)
            {
                if (spegniCompletamenteSuRisoluzione)
                {
                    l.enabled = false;
                    l.intensity = 0f;
                }
                else
                {
                    l.enabled = true;
                    l.color = coloreStandbyRisolto;
                    l.intensity = intensitaMassimaLuce * 0.5f;
                }
            }
        }

        foreach (Renderer rend in oggettiEmettitori)
        {
            if (rend != null)
            {
                Material mat = rend.material;
                if (mat != null)
                {
                    if (spegniCompletamenteSuRisoluzione)
                    {
                        if (mat.HasProperty("_EmissionColor"))
                            mat.SetColor("_EmissionColor", Color.black);
                    }
                    else
                    {
                        mat.color = coloreStandbyRisolto;
                        if (mat.HasProperty("_BaseColor"))
                            mat.SetColor("_BaseColor", coloreStandbyRisolto);

                        if (mat.HasProperty("_EmissionColor"))
                        {
                            mat.EnableKeyword("_EMISSION");
                            mat.SetColor("_EmissionColor", coloreStandbyRisolto * intensitaEmissioneCubo);
                        }
                    }
                }
            }
        }

        Debug.Log($"<color=cyan>[ALLARME]</color> Emergenza su '{name}' RISOLTA: luci e cubi spenti.");
    }

    private void OnStatoEstrazioneCambiato(bool estrazioneSbloccata)
    {
        if (modalita == ModalitaRilevamento.TuttiIFocolaiSettore)
        {
            SetStatoEmergenza(!estrazioneSbloccata);
        }
    }

    private void OnFocolaiAggiornati(int contenuti, int totali)
    {
        if (modalita == ModalitaRilevamento.TuttiIFocolaiSettore)
        {
            if (contenuti >= totali && totali > 0)
                SetStatoEmergenza(false);
        }
    }

    /// <summary>
    /// Metodo pubblico per risolvere manualmente l'emergenza da un trigger, bottone o UnityEvent.
    /// </summary>
    public void RisolviEmergenza()
    {
        SetStatoEmergenza(false);
    }

    /// <summary>
    /// Metodo pubblico per attivare manualmente l'allarme da un trigger, bottone o UnityEvent.
    /// </summary>
    public void InnescaEmergenza()
    {
        SetStatoEmergenza(true);
    }

    [ContextMenu("Test Risolvi Emergenza (Spegni)")]
    public void TestRisolvi()
    {
        SetStatoEmergenza(false);
    }

    [ContextMenu("Test Innesca Emergenza (Lampeggia)")]
    public void TestInnesca()
    {
        SetStatoEmergenza(true);
    }
}
