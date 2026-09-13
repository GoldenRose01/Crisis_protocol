using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SalutePlayer : MonoBehaviour, IDamageable
{
    [Header("Statistiche Salute")]
    public float puntiVitaMassimi = 100f;
    [SerializeField] private float puntiVitaCorrenti;
    [SerializeField] private bool applicaTagPlayerAutomatico = true;

    [Header("Configurazione Animazioni & Stato")]
    public Animator animatorePersonaggio;
    public string triggerMorte = "Morte";
    public string triggerDanno = "SubisciDanno";

    [Header("Audio")]
    [Tooltip("Suono di gemito/gasp quando il player subisce danno.")]
    [SerializeField] private AudioClip suonoDanno;
    [Tooltip("Suono di morte / Game Over.")]
    [SerializeField] private AudioClip suonoMorte;
    [Tooltip("Suono di cura / ripristino salute.")]
    [SerializeField] private AudioClip suonoCura;
    [Range(0f, 1f)] [SerializeField] private float volumeAudio = 1.0f;

    private bool isMorto;
    private muve_pg scriptMovimento;
    private Rigidbody rb;
    private AudioSource audioSource;

    public float SaluteAttuale => puntiVitaCorrenti;
    public static event Action<float, float> OnSaluteCambiata;
    public static event Action OnPlayerMorto;

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
            audioSource.minDistance = 1.5f;
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

    private void Awake()
    {
        ApplicaTagUnity();
        puntiVitaCorrenti = puntiVitaMassimi;
    }

    private void Start()
    {
        InizializzaAudioSource();
        ApplicaTagUnity();
        if (puntiVitaCorrenti <= 0)
            puntiVitaCorrenti = puntiVitaMassimi;
            
        scriptMovimento = GetComponent<muve_pg>() ?? GetComponentInParent<muve_pg>() ?? GetComponentInChildren<muve_pg>();
        rb = GetComponent<Rigidbody>();

        OnSaluteCambiata?.Invoke(puntiVitaCorrenti, puntiVitaMassimi);
    }

    private void OnValidate()
    {
        ApplicaTagUnity();
    }

    public void SubisciDanno(float quantitaDanno)
    {
        if (isMorto)
            return;

        puntiVitaCorrenti -= quantitaDanno;
        puntiVitaCorrenti = Mathf.Clamp(puntiVitaCorrenti, 0f, puntiVitaMassimi);

        Debug.Log($"<b>[GIOCATORE]</b> Colpito! Subito {quantitaDanno} HP di danno. Vita rimanente: {puntiVitaCorrenti}/{puntiVitaMassimi}");
        OnSaluteCambiata?.Invoke(puntiVitaCorrenti, puntiVitaMassimi);

        if (MissionManager.Instance != null)
            MissionManager.Instance.RegistraDannoSubito(quantitaDanno);

        if (puntiVitaCorrenti <= 0f)
        {
            EseguiMorte();
            return;
        }

        RiproduciSuono(suonoDanno);

        if (animatorePersonaggio != null && !string.IsNullOrWhiteSpace(triggerDanno))
            animatorePersonaggio.SetTrigger(triggerDanno);
    }

    public void Cura(float quantitaCura)
    {
        if (isMorto)
            return;

        puntiVitaCorrenti = Mathf.Clamp(puntiVitaCorrenti + quantitaCura, 0f, puntiVitaMassimi);
        RiproduciSuono(suonoCura);
        OnSaluteCambiata?.Invoke(puntiVitaCorrenti, puntiVitaMassimi);

        Debug.Log($"[SISTEMA] Curato di {quantitaCura} HP. Salute: {puntiVitaCorrenti}/{puntiVitaMassimi}");
    }

    private void EseguiMorte()
    {
        isMorto = true;
        RiproduciSuono(suonoMorte);
        Debug.Log("<color=red><b>[GAME OVER]</b> Il giocatore e' crollato a terra.</color>");

        if (animatorePersonaggio != null)
            animatorePersonaggio.SetTrigger(triggerMorte);

        if (scriptMovimento != null)
            scriptMovimento.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        OnPlayerMorto?.Invoke();
    }

    private void ApplicaTagUnity()
    {
        if (applicaTagPlayerAutomatico)
            SectorContainmentTags.ApplyTag(gameObject, SectorContainmentTags.Player);
    }
}
