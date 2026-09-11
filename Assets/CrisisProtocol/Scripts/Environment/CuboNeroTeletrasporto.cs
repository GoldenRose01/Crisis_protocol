using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Cubo Nero Triggered: quando il giocatore entra nel volume del cubo,
/// se la crisi è terminata (o in debug), teletrasporta il giocatore alla scena successiva.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CuboNeroTeletrasporto : MonoBehaviour
{
    [Header("Condizioni di Transizione")]
    [Tooltip("Se true, il teletrasporto si attiva solo dopo aver completato tutti i focolai e sbloccato l'estrazione.")]
    [SerializeField] private bool richiediFineCrisi = true;

    [Tooltip("Secondi di attesa prima del cambio scena (per consentire effetti visivi/audio).")]
    [SerializeField] private float ritardoTeletrasporto = 0.5f;

    [Tooltip("Opzionale: nome esatto della scena da caricare. Se vuoto, usa l'ordine sequenziale dei settori nel GameManager.")]
    [SerializeField] private string scenaPersonalizzata = "";

    [Header("Feedback Visivo e Sonoro")]
    [Tooltip("Se true, applica automaticamente un materiale nero profondo/sci-fi alla mesh del cubo.")]
    [SerializeField] private bool applicaMaterialeNero = true;

    [Tooltip("Colore del cubo quando è inattivo / in attesa.")]
    [SerializeField] private Color coloreInattivo = new Color(0.05f, 0.05f, 0.05f, 0.8f);

    [Tooltip("Colore del cubo quando la crisi è finita e il portale è attivo.")]
    [SerializeField] private Color coloreAttivo = Color.black;

    [Tooltip("Particelle opzionali che si attivano all'entrata nel cubo.")]
    [SerializeField] private ParticleSystem particelleTeletrasporto;

    [Tooltip("Suono opzionale riprodotto al momento del teletrasporto.")]
    [SerializeField] private AudioClip suonoTeletrasporto;

    [Header("Debug")]
    [Tooltip("Se true, il cubo nero è sempre attivo e teletrasporta subito senza aspettare la fine della crisi.")]
    [SerializeField] private bool ignoraCrisiPerDebug = false;

    private Collider col;
    private Renderer rend;
    private Material matIstanza;
    private bool inTransizione = false;

    private void Awake()
    {
        col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        rend = GetComponent<Renderer>();
        if (rend != null && applicaMaterialeNero)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader != null)
            {
                matIstanza = new Material(shader);
                matIstanza.name = "Mat_CuboNero_Teletrasporto";
                ImpostaColoreMateriale(coloreInattivo);
                rend.material = matIstanza;
            }
        }
    }

    private void OnEnable()
    {
        MissionManager.OnEstrazioneSbloccata += OnEstrazioneModificata;
    }

    private void OnDisable()
    {
        MissionManager.OnEstrazioneSbloccata -= OnEstrazioneModificata;
    }

    private void Start()
    {
        bool pronto = ignoraCrisiPerDebug || (MissionManager.Instance != null && MissionManager.Instance.EstrazioneSbloccata);
        AggiornaAspetto(pronto);
    }

    private void OnEstrazioneModificata(bool sbloccata)
    {
        AggiornaAspetto(sbloccata || ignoraCrisiPerDebug);
    }

    private void AggiornaAspetto(bool attivo)
    {
        if (matIstanza != null)
        {
            ImpostaColoreMateriale(attivo ? coloreAttivo : coloreInattivo);
        }
    }

    private void ImpostaColoreMateriale(Color c)
    {
        if (matIstanza == null) return;

        if (matIstanza.HasProperty("_BaseColor"))
            matIstanza.SetColor("_BaseColor", c);
        else if (matIstanza.HasProperty("_Color"))
            matIstanza.color = c;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (inTransizione) return;

        // Verifica se l'oggetto entrato è il Player
        if (!IsPlayer(other)) return;

        // Controllo completamento crisi
        if (richiediFineCrisi && !ignoraCrisiPerDebug)
        {
            if (MissionManager.Instance != null && !MissionManager.Instance.EstrazioneSbloccata)
            {
                Debug.LogWarning("<color=orange>[CUBO NERO]</color> Portale inattivo: devi prima risolvere la crisi contenendo tutti i focolai!");
                return;
            }
        }

        Debug.Log("<color=cyan>[CUBO NERO]</color> Player entrato nel portale! Avvio teletrasporto alla scena successiva...");
        StartCoroutine(EseguiTeletrasporto());
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player") ||
               other.GetComponent<SalutePlayer>() != null ||
               other.GetComponentInParent<SalutePlayer>() != null ||
               other.GetComponent<muve_pg>() != null ||
               other.GetComponentInParent<muve_pg>() != null ||
               other.GetComponent<PlayerInteract>() != null ||
               other.GetComponentInParent<PlayerInteract>() != null;
    }

    private IEnumerator EseguiTeletrasporto()
    {
        inTransizione = true;

        if (particelleTeletrasporto != null)
            particelleTeletrasporto.Play();

        if (suonoTeletrasporto != null)
            AudioSource.PlayClipAtPoint(suonoTeletrasporto, transform.position);

        if (ritardoTeletrasporto > 0f)
            yield return new WaitForSeconds(ritardoTeletrasporto);

        // Se è specificato un nome scena personalizzato
        if (!string.IsNullOrWhiteSpace(scenaPersonalizzata))
        {
            Debug.Log($"[CUBO NERO] Carico scena specifica: '{scenaPersonalizzata}'");
            SceneManager.LoadScene(scenaPersonalizzata);
            yield break;
        }

        // Altrimenti usa il flusso standard del GameManager
        if (GameManager.Instance != null)
        {
            Debug.Log("[CUBO NERO] Avanzo al prossimo settore tramite GameManager...");
            GameManager.Instance.CaricaProssimoSettore();
        }
        else if (MissionManager.Instance != null)
        {
            Debug.Log("[CUBO NERO] Concludo missione tramite MissionManager...");
            MissionManager.Instance.TentaEstrazione();
        }
        else
        {
            int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
            if (nextIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(nextIndex);
            }
            else
            {
                Debug.LogWarning("[CUBO NERO] Nessun settore successivo trovato. Ricarico scena attuale.");
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
    }

    [ContextMenu("DEBUG: Teletrasporta Ora (Forza Prossimo Livello)")]
    public void ForzaTeletrasportoDebug()
    {
        StartCoroutine(EseguiTeletrasporto());
    }
}
