using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Porta o vetrata del settore. Si integra con PlayerInteract tramite IInteractable.
/// Il player preme E vicino alla porta per aprirla/chiuderla.
///
/// SETUP IN UNITY EDITOR:
///   1. Aggiungi PortaSettore al GameObject della porta/vetrata
///   2. Assegna il layer "Interactable" al GameObject
///   3. NON mettere il Collider come Trigger (serve come fisico)
///   4. Aggiungi NavMeshObstacle (opzionale) se vuoi bloccare i nemici
/// </summary>
[RequireComponent(typeof(Collider))]
public class PortaSettore : MonoBehaviour, IInteractable
{
    public enum TipoApertura { Slide, Rotazione }

    [Header("Configurazione Porta")]
    [Tooltip("ID univoco: usato per salvare lo stato nel GameManager.")]
    [SerializeField] private string portaId = "DOOR_S0_001";

    [Tooltip("Se compilato, richiede questa credenziale raccolta prima di aprire.")]
    [SerializeField] private string credenzialeRichiesta = "";

    [Tooltip("Come si apre: Slide = scivola, Rotazione = ruota.")]
    [SerializeField] private TipoApertura tipoApertura = TipoApertura.Slide;

    [Header("Slide - solo se Tipo = Slide")]
    [Tooltip("Direzione locale: Vector3.up = si solleva, Vector3.right = laterale, Vector3.forward = scorre in profondita'.")]
    [SerializeField] private Vector3 direzioneScivolamento = Vector3.up;
    [Tooltip("Distanza di scivolamento in unita' Unity.")]
    [SerializeField] private float offsetApertura = 3f;

    [Header("Rotazione - solo se Tipo = Rotazione")]
    [Tooltip("Gradi di rotazione sull'asse Y quando si apre (90 = anta, 180 = doppia anta).")]
    [SerializeField] private float angoloApertura = 90f;

    [Header("Animazione")]
    [Tooltip("Durata dell'animazione apertura/chiusura in secondi.")]
    [SerializeField] private float durataAnimazione = 0.5f;

    [Header("Feedback Visivo")]
    [Tooltip("Luce di stato opzionale: rossa = chiusa/bloccata, verde = aperta.")]
    [SerializeField] private Light luceDiStato;
    [SerializeField] private Color coloreBlocco = Color.red;
    [SerializeField] private Color coloreAperta = Color.green;

    [Header("Stato Iniziale")]
    [Tooltip("Se true la porta parte gia' aperta all'avvio della scena.")]
    [SerializeField] private bool apertaAllInizio = false;

    // Stato interno
    private bool aperta = false;
    private bool inAnimazione = false;

    private Vector3 posizioneChiusa;
    private Vector3 posizioneAperta;
    private Quaternion rotazioneChiusa;
    private Quaternion rotazioneAperta;

    private Collider colliderFisico;
    private NavMeshObstacle ostacolo; // opzionale

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        colliderFisico = GetComponent<Collider>();
        ostacolo = GetComponent<NavMeshObstacle>(); // null se non presente

        // Memorizza le posizioni di riferimento basandosi sulla posizione attuale nell'Editor
        posizioneChiusa = transform.localPosition;
        rotazioneChiusa = transform.localRotation;

        posizioneAperta = posizioneChiusa + direzioneScivolamento.normalized * offsetApertura;
        rotazioneAperta  = rotazioneChiusa * Quaternion.Euler(0f, angoloApertura, 0f);
    }

    private void Start()
    {
        // Ripristina lo stato se la porta era gia' stata aperta in precedenza (tra sessioni)
        if (GameManager.Instance != null &&
            !string.IsNullOrEmpty(portaId) &&
            GameManager.Instance.GetCausalState(portaId))
        {
            ApplicaStatoIstantaneo(true);
            return;
        }

        ApplicaStatoIstantaneo(apertaAllInizio);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // IInteractable: chiamato da PlayerInteract quando il player preme E
    // ─────────────────────────────────────────────────────────────────────────

    public void Interact()
    {
        if (inAnimazione) return;

        // Toggle: se aperta, richiudi
        if (aperta)
        {
            StartCoroutine(AnimaPorta(false));
            if (GameManager.Instance != null && !string.IsNullOrEmpty(portaId))
                GameManager.Instance.SetCausalState(portaId, false);
            return;
        }

        // Controlla credenziale richiesta
        if (!string.IsNullOrEmpty(credenzialeRichiesta))
        {
            bool haCred = MissionManager.Instance != null &&
                          MissionManager.Instance.PossiedeCredenziale(credenzialeRichiesta);
            if (!haCred)
            {
                Debug.LogWarning("[PORTA] " + portaId + ": credenziale '" + credenzialeRichiesta + "' non posseduta. Porta bloccata.");
                StartCoroutine(FlashCoroutine()); // luce lampeggia in bianco
                return;
            }
        }

        // Apri
        StartCoroutine(AnimaPorta(true));
        if (GameManager.Instance != null && !string.IsNullOrEmpty(portaId))
            GameManager.Instance.SetCausalState(portaId, true);
    }

    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator AnimaPorta(bool versoAperta)
    {
        inAnimazione = true;

        Vector3 posizioneStart    = transform.localPosition;
        Vector3 posizioneFine     = versoAperta ? posizioneAperta : posizioneChiusa;
        Quaternion rotazioneStart = transform.localRotation;
        Quaternion rotazioneFine  = versoAperta ? rotazioneAperta : rotazioneChiusa;

        float tempo = 0f;
        while (tempo < durataAnimazione)
        {
            tempo += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, tempo / durataAnimazione); // curva fluida

            if (tipoApertura == TipoApertura.Slide)
                transform.localPosition = Vector3.Lerp(posizioneStart, posizioneFine, t);
            else
                transform.localRotation = Quaternion.Slerp(rotazioneStart, rotazioneFine, t);

            yield return null;
        }

        // Snap finale preciso
        if (tipoApertura == TipoApertura.Slide)
            transform.localPosition = posizioneFine;
        else
            transform.localRotation = rotazioneFine;

        aperta = versoAperta;
        inAnimazione = false;
        AggiornaStato();

        Debug.Log("[PORTA] " + portaId + ": " + (aperta ? "APERTA" : "CHIUSA"));
    }

    private void ApplicaStatoIstantaneo(bool statoAperta)
    {
        if (tipoApertura == TipoApertura.Slide)
            transform.localPosition = statoAperta ? posizioneAperta : posizioneChiusa;
        else
            transform.localRotation = statoAperta ? rotazioneAperta : rotazioneChiusa;

        aperta = statoAperta;
        AggiornaStato();
    }

    private void AggiornaStato()
    {
        // Collider: disabilitato quando aperta (player e nemici possono passare)
        if (colliderFisico != null)
            colliderFisico.enabled = !aperta;

        // NavMeshObstacle: disabilitato quando aperta (NavMesh non bloccato)
        if (ostacolo != null)
            ostacolo.enabled = !aperta;

        // Luce di stato
        if (luceDiStato != null)
            luceDiStato.color = aperta ? coloreAperta : coloreBlocco;
    }

    // Fa lampeggiare la luce 3 volte in bianco quando la credenziale manca
    private IEnumerator FlashCoroutine()
    {
        for (int i = 0; i < 3; i++)
        {
            if (luceDiStato != null) luceDiStato.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            if (luceDiStato != null) luceDiStato.color = coloreBlocco;
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void OnDrawGizmos()
    {
        // Mostra nell'Editor la direzione e la distanza di apertura
        if (tipoApertura == TipoApertura.Slide)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
            Gizmos.DrawLine(
                transform.position,
                transform.position + transform.TransformDirection(direzioneScivolamento.normalized * offsetApertura));
        }
    }
}
