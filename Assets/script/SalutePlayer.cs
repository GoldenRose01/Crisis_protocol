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

    private bool isMorto;
    private muve_pg scriptMovimento;
    private Rigidbody rb;

    public float SaluteAttuale => puntiVitaCorrenti;
    public static event Action<float, float> OnSaluteCambiata;
    public static event Action OnPlayerMorto;

    private void Start()
    {
        ApplicaTagUnity();
        puntiVitaCorrenti = puntiVitaMassimi;
        scriptMovimento = GetComponent<muve_pg>();
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

        if (animatorePersonaggio != null && !string.IsNullOrWhiteSpace(triggerDanno))
            animatorePersonaggio.SetTrigger(triggerDanno);
    }

    public void Cura(float quantitaCura)
    {
        if (isMorto)
            return;

        puntiVitaCorrenti = Mathf.Clamp(puntiVitaCorrenti + quantitaCura, 0f, puntiVitaMassimi);
        OnSaluteCambiata?.Invoke(puntiVitaCorrenti, puntiVitaMassimi);

        Debug.Log($"[SISTEMA] Curato di {quantitaCura} HP. Salute: {puntiVitaCorrenti}/{puntiVitaMassimi}");
    }

    private void EseguiMorte()
    {
        isMorto = true;
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
