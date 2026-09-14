// ============================================================================
// Crisis Protocol / Sector Containment - Player
// File: .\Assets\CrisisProtocol\Scripts\Player\SparoPlayer.cs
// Responsabilita': gestisce input, movimento, combattimento, interazione, salute o strumenti controllati dal giocatore.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using CrisisProtocol.UI;
public class SparoPlayer : MonoBehaviour
{
    [Header("Balistica e Danni")]
    public float dannoCorpo = 50f;
    public float dannoTesta = 100f;
    public float gittataSparo = 150f;

    [Header("Munizioni")]
    public int proiettiliMassimi = 5;
    private int proiettiliAttuali;

    [Header("Acustica e Stealth")]
    public float raggioRumore = 25f;
    public LayerMask layerNemici;

    [Header("Riferimenti")]
    [Tooltip("Trascina qui il componente Camera principale")]
    public Camera telecameraPrincipale;
    public ParticleSystem particellareSparo;
    public static event Action<int, int> OnMunizioniCambiate;

    [Header("Audio")]
    [Tooltip("Suono di sparo dell'arma.")]
    [SerializeField] private AudioClip suonoSparo;
    [Tooltip("Suono di caricatore vuoto (clic a vuoto).")]
    [SerializeField] private AudioClip suonoVuoto;
    [Range(0f, 1f)] [SerializeField] private float volumeAudio = 1.0f;

    private AudioSource audioSource;

    private void InizializzaAudioSource()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f; // 3D
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.minDistance = 2.0f;
            audioSource.maxDistance = 25.0f;
            audioSource.dopplerLevel = 0f;
        }
    }

    public void RiproduciSuono(AudioClip clip, float volumeMoltiplicatore = 1.0f)
    {
        if (clip == null) return;
        InizializzaAudioSource();
        if (audioSource != null)
        {
            audioSource.pitch = UnityEngine.Random.Range(0.96f, 1.04f);
            audioSource.PlayOneShot(clip, volumeAudio * volumeMoltiplicatore);
        }
    }
    
    void Update()
    {
        if (ModalUIState.IsModalOpen)
            return;

        // Sparo con il Clic Sinistro
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TentaSparo();
        }
    }

    void Start()
    {
        InizializzaAudioSource();
        proiettiliAttuali = proiettiliMassimi;
        
        if (telecameraPrincipale == null) 
        {
            telecameraPrincipale = Camera.main;
        }
        
        // Inizializza l'HUD all'avvio
        OnMunizioniCambiate?.Invoke(proiettiliAttuali, proiettiliMassimi);
    }

    private void TentaSparo()
    {
        if (proiettiliAttuali > 0)
        {
            proiettiliAttuali--;
            RiproduciSuono(suonoSparo);
            Debug.Log($"<color=cyan>[ARMA] Colpo esploso! Munizioni rimanenti: {proiettiliAttuali}/{proiettiliMassimi}</color>");
            
            // Aggiorna l'HUD
            OnMunizioniCambiate?.Invoke(proiettiliAttuali, proiettiliMassimi);
            
            if (particellareSparo != null) particellareSparo.Play();

            CalcolaTraiettoria();
            GeneraRumoreSparo();
        }
        else
        {
            RiproduciSuono(suonoVuoto);
            Debug.Log("<color=red>[ARMA] Clic! Caricatore vuoto. Ricarica necessaria.</color>");
        }
    }

    private void CalcolaTraiettoria()
    {
        // IL SEGRETO DELLA PRECISIONE: Spara un raggio dal centro esatto dello schermo (0.5 larghezza, 0.5 altezza)
        Ray raggioDiMira = telecameraPrincipale.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        
        // Raccoglie tutto ciò che viene attraversato dal raggio
        RaycastHit[] tuttiIColpi = Physics.RaycastAll(raggioDiMira, gittataSparo);
        
        // Ordina gli impatti dal più vicino al più lontano
        System.Array.Sort(tuttiIColpi, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in tuttiIColpi)
        {
            // Ignora te stesso se il raggio parte da "dentro" il tuo corpo
            if (hit.collider.transform.root == transform.root) continue;

            // Identifica il bersaglio colpito
            bool colpoInTesta = hit.collider.CompareTag("Testa");
            float dannoInflitto = colpoInTesta ? dannoTesta : dannoCorpo;

            IDamageable bersaglio = hit.collider.GetComponentInParent<IDamageable>();
            
            if (bersaglio != null)
            {
                if (colpoInTesta) 
                    Debug.Log("<color=red><b>[HEADSHOT!]</b> Danno critico inflitto!</color>");
                
                bersaglio.SubisciDanno(dannoInflitto);
            }
            else 
            {
                Debug.Log($"<color=grey>[BALISTICA] Colpito ostacolo: {hit.collider.name}</color>");
            }

            // Ferma il proiettile al primo ostacolo (non trapassa i muri)
            break; 
        }
    }

    private void GeneraRumoreSparo()
    {
        Collider[] nemiciAllertati = Physics.OverlapSphere(transform.position, raggioRumore, layerNemici);
        foreach (Collider col in nemiciAllertati)
        {
            GuardiaNpc guardia = col.GetComponentInParent<GuardiaNpc>();
            if (guardia != null) guardia.RiceviAllarmeRinforzi(transform);

            ManutenzioneBot bot = col.GetComponentInParent<ManutenzioneBot>();
            if (bot != null && bot.statoAttuale == ManutenzioneBot.StatoIA.RicercaAttiva)
            {
                bot.statoAttuale = ManutenzioneBot.StatoIA.Inseguimento;
            }
        }
    }

    public void RipristinaMunizioniFuturo()
{
    proiettiliAttuali = proiettiliMassimi;
    // Aggiorna l'HUD dopo il ricaricamento
    OnMunizioniCambiate?.Invoke(proiettiliAttuali, proiettiliMassimi);
}
}
