// ============================================================================
// Crisis Protocol / Sector Containment - Ambiente interattivo
// File: .\Assets\CrisisProtocol\Scripts\Environment\CuboNeroTeletrasporto.cs
// Responsabilita': controlla porte, datapad, teletrasporti, camera o oggetti di scena collegati alla progressione.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using System.Collections; // usa lib // riga-ok
using UnityEngine; // usa lib // riga-ok
using UnityEngine.SceneManagement; // usa lib // riga-ok

/// <summary>
/// Cubo Nero Triggered: quando il giocatore entra nel volume del cubo,
/// se la crisi è terminata (o in debug), teletrasporta il giocatore alla scena successiva.
/// </summary>
[RequireComponent(typeof(Collider))] // nota unity // riga-ok
// blocco: classe x roba grossa
public class CuboNeroTeletrasporto : MonoBehaviour // classe qui // riga-ok
{ // apre // riga-ok
    [Header("Condizioni di Transizione")] // nota unity // riga-ok
    [Tooltip("Se true, il teletrasporto si attiva solo dopo aver completato tutti i focolai e sbloccato l'estrazione.")] // nota unity // riga-ok
    [SerializeField] private bool richiediFineCrisi = true; // setta // riga-ok

    [Tooltip("Secondi di attesa prima del cambio scena (per consentire effetti visivi/audio).")] // nota unity // riga-ok
    [SerializeField] private float ritardoTeletrasporto = 0.5f; // setta // riga-ok

    [Tooltip("Opzionale: nome esatto della scena da caricare. Se vuoto, usa l'ordine sequenziale dei settori nel GameManager.")] // nota unity // riga-ok
    [SerializeField] private string scenaPersonalizzata = ""; // setta // riga-ok

    [Header("Feedback Visivo e Sonoro")] // nota unity // riga-ok
    [Tooltip("Se true, applica automaticamente un materiale nero profondo/sci-fi alla mesh del cubo.")] // nota unity // riga-ok
    [SerializeField] private bool applicaMaterialeNero = true; // setta // riga-ok

    [Tooltip("Colore del cubo quando è inattivo / in attesa.")] // nota unity // riga-ok
    [SerializeField] private Color coloreInattivo = new Color(0.05f, 0.05f, 0.05f, 0.8f); // setta // riga-ok

    [Tooltip("Colore del cubo quando la crisi è finita e il portale è attivo.")] // nota unity // riga-ok
    [SerializeField] private Color coloreAttivo = Color.black; // setta // riga-ok

    [Tooltip("Particelle opzionali che si attivano all'entrata nel cubo.")] // nota unity // riga-ok
    [SerializeField] private ParticleSystem particelleTeletrasporto; // ok qua // riga-ok

    [Tooltip("Suono opzionale riprodotto al momento del teletrasporto.")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoTeletrasporto; // ok qua // riga-ok

    [Header("Debug")] // nota unity // riga-ok
    [Tooltip("Se true, il cubo nero è sempre attivo e teletrasporta subito senza aspettare la fine della crisi.")] // nota unity // riga-ok
    [SerializeField] private bool ignoraCrisiPerDebug = false; // setta // riga-ok

    private Collider col; // roba pub // riga-ok
    private Renderer rend; // roba pub // riga-ok
    private Material matIstanza; // roba pub // riga-ok
    private bool inTransizione = false; // roba pub // riga-ok

    // blocco: funzione fa cose
    private void Awake() // roba pub // riga-ok
    { // apre // riga-ok
        col = GetComponent<Collider>(); // setta // riga-ok
        // blocco: controlla se va
        if (col != null) // se ok // riga-ok
        { // apre // riga-ok
            col.isTrigger = true; // setta // riga-ok
        } // chiude // riga-ok

        rend = GetComponent<Renderer>(); // setta // riga-ok
        // blocco: controlla se va
        if (rend != null && applicaMaterialeNero) // se ok // riga-ok
        { // apre // riga-ok
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"); // setta // riga-ok
            // blocco: controlla se va
            if (shader != null) // se ok // riga-ok
            { // apre // riga-ok
                matIstanza = new Material(shader); // setta // riga-ok
                matIstanza.name = "Mat_CuboNero_Teletrasporto"; // setta // riga-ok
                ImpostaColoreMateriale(coloreInattivo); // chiama // riga-ok
                rend.material = matIstanza; // setta // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnEnable() // roba pub // riga-ok
    { // apre // riga-ok
        MissionManager.OnEstrazioneSbloccata += OnEstrazioneModificata; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnDisable() // roba pub // riga-ok
    { // apre // riga-ok
        MissionManager.OnEstrazioneSbloccata -= OnEstrazioneModificata; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void Start() // roba pub // riga-ok
    { // apre // riga-ok
        bool pronto = ignoraCrisiPerDebug || (MissionManager.Instance != null && MissionManager.Instance.EstrazioneSbloccata); // setta // riga-ok
        AggiornaAspetto(pronto); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnEstrazioneModificata(bool sbloccata) // roba pub // riga-ok
    { // apre // riga-ok
        AggiornaAspetto(sbloccata || ignoraCrisiPerDebug); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void AggiornaAspetto(bool attivo) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (matIstanza != null) // se ok // riga-ok
        { // apre // riga-ok
            ImpostaColoreMateriale(attivo ? coloreAttivo : coloreInattivo); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void ImpostaColoreMateriale(Color c) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (matIstanza == null) return; // se ok // riga-ok

        // blocco: controlla se va
        if (matIstanza.HasProperty("_BaseColor")) // se ok // riga-ok
            matIstanza.SetColor("_BaseColor", c); // chiama // riga-ok
        // blocco: controlla se va
        else if (matIstanza.HasProperty("_Color")) // se ok // riga-ok
            matIstanza.color = c; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnTriggerEnter(Collider other) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (inTransizione) return; // se ok // riga-ok

        // Verifica se l'oggetto entrato è il Player
        // blocco: controlla se va
        if (!IsPlayer(other)) return; // se ok // riga-ok

        // Controllo completamento crisi
        // blocco: controlla se va
        if (richiediFineCrisi && !ignoraCrisiPerDebug) // se ok // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (MissionManager.Instance != null && !MissionManager.Instance.EstrazioneSbloccata) // se ok // riga-ok
            { // apre // riga-ok
                Debug.LogWarning("<color=orange>[CUBO NERO]</color> Portale inattivo: devi prima risolvere la crisi contenendo tutti i focolai!"); // logga // riga-ok
                return; // torna val // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        Debug.Log("<color=cyan>[CUBO NERO]</color> Player entrato nel portale! Avvio teletrasporto alla scena successiva..."); // logga // riga-ok
        StartCoroutine(EseguiTeletrasporto()); // corutina // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private bool IsPlayer(Collider other) // roba pub // riga-ok
    { // apre // riga-ok
        return other.CompareTag("Player") || // torna val // riga-ok
               other.GetComponent<SalutePlayer>() != null || // setta // riga-ok
               other.GetComponentInParent<SalutePlayer>() != null || // setta // riga-ok
               other.GetComponent<muve_pg>() != null || // setta // riga-ok
               other.GetComponentInParent<muve_pg>() != null || // setta // riga-ok
               other.GetComponent<PlayerInteract>() != null || // setta // riga-ok
               other.GetComponentInParent<PlayerInteract>() != null; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private IEnumerator EseguiTeletrasporto() // roba pub // riga-ok
    { // apre // riga-ok
        inTransizione = true; // setta // riga-ok

        // blocco: controlla se va
        if (particelleTeletrasporto != null) // se ok // riga-ok
            particelleTeletrasporto.Play(); // chiama // riga-ok

        // blocco: controlla se va
        if (suonoTeletrasporto != null) // se ok // riga-ok
            AudioSource.PlayClipAtPoint(suonoTeletrasporto, transform.position); // chiama // riga-ok

        // blocco: controlla se va
        if (ritardoTeletrasporto > 0f) // se ok // riga-ok
            yield return new WaitForSeconds(ritardoTeletrasporto); // aspetta // riga-ok

        // Se è specificato un nome scena personalizzato
        // blocco: controlla se va
        if (!string.IsNullOrWhiteSpace(scenaPersonalizzata)) // se ok // riga-ok
        { // apre // riga-ok
            Debug.Log($"[CUBO NERO] Carico scena specifica: '{scenaPersonalizzata}'"); // logga // riga-ok
            SceneManager.LoadScene(scenaPersonalizzata); // chiama // riga-ok
            yield break; // aspetta // riga-ok
        } // chiude // riga-ok

        // Altrimenti usa il flusso standard del GameManager
        // blocco: controlla se va
        if (GameManager.Instance != null) // se ok // riga-ok
        { // apre // riga-ok
            Debug.Log("[CUBO NERO] Avanzo al prossimo settore tramite GameManager..."); // logga // riga-ok
            GameManager.Instance.CaricaProssimoSettore(); // chiama // riga-ok
        } // chiude // riga-ok
        // blocco: controlla se va
        else if (MissionManager.Instance != null) // se ok // riga-ok
        { // apre // riga-ok
            Debug.Log("[CUBO NERO] Concludo missione tramite MissionManager..."); // logga // riga-ok
            MissionManager.Instance.TentaEstrazione(); // chiama // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            int nextIndex = SceneManager.GetActiveScene().buildIndex + 1; // setta // riga-ok
            // blocco: controlla se va
            if (nextIndex < SceneManager.sceneCountInBuildSettings) // se ok // riga-ok
            { // apre // riga-ok
                SceneManager.LoadScene(nextIndex); // chiama // riga-ok
            } // chiude // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
            { // apre // riga-ok
                Debug.LogWarning("[CUBO NERO] Nessun settore successivo trovato. Ricarico scena attuale."); // logga // riga-ok
                SceneManager.LoadScene(SceneManager.GetActiveScene().name); // chiama // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    [ContextMenu("DEBUG: Teletrasporta Ora (Forza Prossimo Livello)")] // nota unity // riga-ok
    // blocco: funzione fa cose
    public void ForzaTeletrasportoDebug() // roba pub // riga-ok
    { // apre // riga-ok
        StartCoroutine(EseguiTeletrasporto()); // corutina // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
