// ============================================================================
// Crisis Protocol / Sector Containment - Ambiente interattivo
// File: .\Assets\CrisisProtocol\Scripts\Environment\DatapadCodiciPorte.cs
// Responsabilita': controlla porte, datapad, teletrasporti, camera o oggetti di scena collegati alla progressione.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using System; // usa lib // riga-ok
using System.Collections.Generic; // usa lib // riga-ok
using UnityEngine; // usa lib // riga-ok
using CrisisProtocol.UI; // usa lib // riga-ok

/// <summary>
/// Struttura dati per una voce di porta/settore visualizzata sull'ologramma.
/// </summary>
[Serializable] // nota unity // riga-ok
// blocco: classe x roba grossa
public class VoceCodicePorta // classe qui // riga-ok
{// apre // riga-ok
    [Tooltip("Nome identificativo della porta o del settore (es. 'SETTORE REATTORE A1').")] // nota unity // riga-ok
    public string nomePorta = "SETTORE REATTORE"; // roba pub // riga-ok

    [Tooltip("Codice PIN numerico a 4 cifre per l'apertura.")] // nota unity // riga-ok
    public string codicePin = "4281"; // roba pub // riga-ok

    [Tooltip("Stato o livello di sicurezza (es. 'LIVELLO SICUREZZA 3', 'BLOCCATA', 'ACCESSO RISERVATO').")] // nota unity // riga-ok
    public string livelloSicurezza = "ACCESSO RISERVATO"; // roba pub // riga-ok

    [Tooltip("Nota informativa o descrizione sul settore.")] // nota unity // riga-ok
    [TextArea(2, 3)] // nota unity // riga-ok
    public string note = "Richiesto terminale di controllo o scansione biometrica."; // roba pub // riga-ok
} // chiude // riga-ok

/// <summary>
/// Script da assegnare a qualsiasi oggetto nella scena (Datapad, tablet, console, terminale o proiettore olografico).
/// Quando il giocatore si avvicina e preme 'E' (o ci clicca sopra col mouse), apre un'interfaccia olografica
/// verde acqua / ciano in stile LED con l'elenco dei codici PIN delle porte del livello.
/// </summary>
[RequireComponent(typeof(Collider))] // nota unity // riga-ok
// blocco: classe x roba grossa
public class DatapadCodiciPorte : MonoBehaviour, IInteractable // classe qui // riga-ok
{ // apre // riga-ok
    [Header("Intestazione Ologramma")] // nota unity // riga-ok
    [Tooltip("Titolo visualizzato in cima alla schermata olografica.")] // nota unity // riga-ok
    public string titoloDatapad = "DATAPAD DI SICUREZZA // REGISTRO CODICI ACCESSO"; // roba pub // riga-ok

    [Tooltip("Sottotitolo o nota d'archivio.")] // nota unity // riga-ok
    public string autoreONota = "Ufficio Sicurezza Struttura - Memorandum Riservato"; // roba pub // riga-ok

    [Header("Rilevamento Automatico Porte")] // nota unity // riga-ok
    [Tooltip("Se attivo, cerca automaticamente tutti i TerminalePorta presenti nella scena e ne ricava nome e codice PIN.")] // nota unity // riga-ok
    public bool autoRilevaPorteScena = true; // roba pub // riga-ok

    [Header("Codici Manuali / Aggiuntivi")] // nota unity // riga-ok
    [Tooltip("Voci manuali personalizzate di porte e codici da mostrare nell'ologramma.")] // nota unity // riga-ok
    // blocco: funzione fa cose
    public List<VoceCodicePorta> codiciPersonalizzati = new List<VoceCodicePorta>() // roba pub // riga-ok
    { // apre // riga-ok
        new VoceCodicePorta() { nomePorta = "PORTA SETTORE REATTORE", codicePin = "4281", livelloSicurezza = "LIVELLO 3", note = "Terminale di sicurezza adiacente al varco." }, // setta // riga-ok
        new VoceCodicePorta() { nomePorta = "LABORATORIO BIOLOGICO", codicePin = "7139", livelloSicurezza = "LIVELLO 2", note = "Codice di emergenza per quarantena." }, // setta // riga-ok
        new VoceCodicePorta() { nomePorta = "HANGAR DI CONTENIMENTO", codicePin = "9024", livelloSicurezza = "LIVELLO 4", note = "Accesso riservato al personale autorizzato." } // setta // riga-ok
    }; // ok qua // riga-ok

    [Header("Feedback Visivo 3D Oggetto")] // nota unity // riga-ok
    [Tooltip("Luce del proiettore/schermo (opzionale, verde acqua).")] // nota unity // riga-ok
    public Light luceOlogramma; // roba pub // riga-ok

    [Tooltip("Renderer della mesh dello schermo/datapad per il materiale emissivo.")] // nota unity // riga-ok
    public Renderer meshSchermo; // roba pub // riga-ok

    [Tooltip("Colore luce ed emissione dello schermo (Cyan/Verde Acqua).")] // nota unity // riga-ok
    public Color coloreOlogramma = new Color(0.1f, 0.95f, 0.85f, 1f); // roba pub // riga-ok

    [Tooltip("Se true, la luce del datapad pulsa dolcemente per attirare l'attenzione.")] // nota unity // riga-ok
    public bool animaPulsazioneLuce = true; // roba pub // riga-ok

    [Header("Evidenziazione Visuale Player")] // nota unity // riga-ok
    [Tooltip("Se attivo, crea da solo un marker luminoso sopra il datapad nella visuale del player.")] // nota unity // riga-ok
    [SerializeField] private bool evidenziaInVisualePlayer = true; // setta // riga-ok

    [Tooltip("Altezza del marker luminoso sopra l'oggetto.")] // nota unity // riga-ok
    [SerializeField] private float altezzaMarkerVisuale = 2.5f; // setta // riga-ok

    [Tooltip("Grandezza del marker visivo automatico.")] // nota unity // riga-ok
    [SerializeField] private float scalaMarkerVisuale = 0.35f; // setta // riga-ok

    [Tooltip("Distanza della luce usata per far risaltare il datapad.")] // nota unity // riga-ok
    [SerializeField] private float raggioLuceVisuale = 7.0f; // setta // riga-ok

    [Tooltip("Intensita' base della luce automatica sopra il datapad.")] // nota unity // riga-ok
    [SerializeField] private float intensitaLuceVisuale = 3.5f; // setta // riga-ok

    [Header("Audio")] // nota unity // riga-ok
    [Tooltip("Suono di accensione / battitura all'apertura del datapad.")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoApertura; // ok qua // riga-ok
    [Range(0f, 1f)] [SerializeField] private float volumeAudio = 0.9f; // setta // riga-ok

    private float intensitaLuceBase = 2.0f; // roba pub // riga-ok
    private GameObject markerVisuale; // cache obj // riga-ok
    private Light luceMarkerVisuale; // cache luce // riga-ok
    private Renderer rendererMarkerVisuale; // cache rend // riga-ok
    private Material materialeMarkerVisuale; // cache mat // riga-ok
    private Camera cameraPrincipale; // cache cam // riga-ok

    // FIX: blocca posizione nel mondo — registra pos/rot iniziali e le reimpone ogni LateUpdate
    private Vector3    _posMondiale;        // posizione world registrata // riga-ok
    private Quaternion _rotMondiale;        // rotazione world registrata // riga-ok
    private bool       _posizioneFissata = false; // flag attivo // riga-ok
    private Vector3    _lucePosWorld;       // pos world luce esterna // riga-ok
    private Quaternion _luceRotWorld;       // rot world luce esterna // riga-ok
    private bool       _lucePosRegistrata = false; // flag luce // riga-ok

    void Start() // chiama // riga-ok
    { // apre // riga-ok
        // Se non è stato impostato il layer Interactable, applicalo per abilitare la pressione di E con PlayerInteract
        // blocco: controlla se va
        if (gameObject.layer == 0) // se ok // riga-ok
        { // apre // riga-ok
            int interactableLayer = LayerMask.NameToLayer("Interactable"); // setta // riga-ok
            // blocco: controlla se va
            if (interactableLayer != -1) // se ok // riga-ok
                gameObject.layer = interactableLayer; // setta // riga-ok
        } // chiude // riga-ok

        // Configura il colore iniziale della luce e dell'emissione
        // blocco: controlla se va
        if (luceOlogramma != null) // se ok // riga-ok
        { // apre // riga-ok
            luceOlogramma.color = coloreOlogramma; // setta // riga-ok
            intensitaLuceBase = luceOlogramma.intensity > 0 ? luceOlogramma.intensity : 2.0f; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (meshSchermo != null && meshSchermo.material != null) // se ok // riga-ok
        { // apre // riga-ok
            Material mat = meshSchermo.material; // setta // riga-ok
            // blocco: controlla se va
            if (mat.HasProperty("_BaseColor")) // se ok // riga-ok
                mat.SetColor("_BaseColor", coloreOlogramma); // chiama // riga-ok
            // blocco: controlla se va
            else if (mat.HasProperty("_Color")) // se ok // riga-ok
                mat.color = coloreOlogramma; // setta // riga-ok

            // blocco: controlla se va
            if (mat.HasProperty("_EmissionColor")) // se ok // riga-ok
            { // apre // riga-ok
                mat.EnableKeyword("_EMISSION"); // chiama // riga-ok
                mat.SetColor("_EmissionColor", coloreOlogramma * 2.5f); // chiama // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        ConfiguraEvidenzaVisuale(); // chiama // riga-ok

        // FIX: registra la posizione e rotazione mondiali DOPO che tutto è posizionato
        _posMondiale     = transform.position; // setta // riga-ok
        _rotMondiale     = transform.rotation; // setta // riga-ok
        _posizioneFissata = true; // attiva il lock // setta // riga-ok
    } // chiude // riga-ok

    void Update() // chiama // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (animaPulsazioneLuce && luceOlogramma != null) // se ok // riga-ok
        { // apre // riga-ok
            float sin = Mathf.Sin(Time.time * 3f); // setta // riga-ok
            luceOlogramma.intensity = intensitaLuceBase + (sin * 0.4f); // setta // riga-ok
        } // chiude // riga-ok

        AggiornaEvidenzaVisuale(); // chiama // riga-ok
    } // chiude // riga-ok

    // FIX: eseguito DOPO tutti gli Update — forza il pannello a restare nella posizione iniziale
    // qualunque cosa lo stia spostando (parent mobile, physics, animazioni, ecc.)
    void LateUpdate() // chiama // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (!_posizioneFissata) return; // non ancora pronto // se ok // riga-ok

        // Blocca posizione e rotazione di questo oggetto
        transform.position = _posMondiale; // forza pos // setta // riga-ok
        transform.rotation = _rotMondiale; // forza rot // setta // riga-ok

        // Blocca anche la luce esterna se presente e separata
        // blocco: controlla se va
        if (luceOlogramma != null && luceOlogramma.transform.parent != transform) // se ok // riga-ok
        { // apre // riga-ok
            // La luce è su un oggetto separato non figlio di questo: la blocca al suo posto
            // (registra la sua posizione iniziale al primo frame)
            // blocco: controlla se va
            if (!_lucePosRegistrata) // se ok // riga-ok
            { // apre // riga-ok
                _lucePosWorld = luceOlogramma.transform.position; // setta // riga-ok
                _luceRotWorld = luceOlogramma.transform.rotation; // setta // riga-ok
                _lucePosRegistrata = true; // setta // riga-ok
            } // chiude // riga-ok
            luceOlogramma.transform.position = _lucePosWorld; // blocca luce // setta // riga-ok
            luceOlogramma.transform.rotation = _luceRotWorld; // blocca luce // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: crea marker vista
    private void ConfiguraEvidenzaVisuale() // roba priv // riga-ok
    { // apre // riga-ok
        if (!evidenziaInVisualePlayer) return; // se off // riga-ok
        if (markerVisuale != null) return; // gia fatto // riga-ok

        cameraPrincipale = Camera.main; // trova cam // riga-ok

        markerVisuale = GameObject.CreatePrimitive(PrimitiveType.Sphere); // crea sfera // riga-ok
        markerVisuale.name = "Datapad_View_Highlight"; // setta nome // riga-ok
        markerVisuale.transform.SetParent(transform, false); // aggancia // riga-ok
        markerVisuale.transform.localPosition = Vector3.up * altezzaMarkerVisuale; // alza // riga-ok
        markerVisuale.transform.localScale = Vector3.one * scalaMarkerVisuale; // scala // riga-ok

        Collider markerCollider = markerVisuale.GetComponent<Collider>(); // prende col // riga-ok
        if (markerCollider != null) Destroy(markerCollider); // elimina col // riga-ok

        rendererMarkerVisuale = markerVisuale.GetComponent<Renderer>(); // prende rend // riga-ok
        if (rendererMarkerVisuale != null) // se ok // riga-ok
        { // apre // riga-ok
            materialeMarkerVisuale = CreaMaterialeEvidenza(); // crea mat // riga-ok
            rendererMarkerVisuale.material = materialeMarkerVisuale; // assegna // riga-ok
        } // chiude // riga-ok

        luceMarkerVisuale = markerVisuale.AddComponent<Light>(); // crea luce // riga-ok
        luceMarkerVisuale.type = LightType.Point; // tipo luce // riga-ok
        luceMarkerVisuale.color = coloreOlogramma; // setta col // riga-ok
        luceMarkerVisuale.range = raggioLuceVisuale; // setta raggio // riga-ok
        luceMarkerVisuale.intensity = intensitaLuceVisuale; // setta forza // riga-ok

        // Crea anello olografico attorno alla sfera
        LineRenderer anello = markerVisuale.AddComponent<LineRenderer>(); // crea line // riga-ok
        anello.useWorldSpace = false; // spazio locale // riga-ok
        anello.loop = true; // chiudi cerchio // riga-ok
        anello.positionCount = 36; // 36 segmenti // riga-ok
        anello.startWidth = 0.08f; // spessore // riga-ok
        anello.endWidth = 0.08f; // spessore // riga-ok
        anello.material = materialeMarkerVisuale; // stesso materiale // riga-ok
        anello.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // no ombre // riga-ok
        anello.receiveShadows = false; // no ombre // riga-ok
        
        float raggioAnello = 1.6f; // Raggio locale dell'anello // riga-ok
        for (int i = 0; i < 36; i++) // ciclo x // riga-ok
        { // apre // riga-ok
            float rad = Mathf.Deg2Rad * (i * 10f); // calcola angolo // riga-ok
            anello.SetPosition(i, new Vector3(Mathf.Sin(rad) * raggioAnello, 0f, Mathf.Cos(rad) * raggioAnello)); // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: materiale marker
    private Material CreaMaterialeEvidenza() // roba priv // riga-ok
    { // apre // riga-ok
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default"); // trova shader // riga-ok
        Material mat = new Material(shader); // crea mat // riga-ok
        Color colore = coloreOlogramma; // copia col // riga-ok
        colore.a = 0.85f; // setta alfa // riga-ok

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", colore); // col base // riga-ok
        else if (mat.HasProperty("_Color")) mat.color = colore; // col base // riga-ok

        if (mat.HasProperty("_EmissionColor")) // se ok // riga-ok
        { // apre // riga-ok
            mat.EnableKeyword("_EMISSION"); // accendi // riga-ok
            mat.SetColor("_EmissionColor", coloreOlogramma * 3.0f); // emette // riga-ok
        } // chiude // riga-ok

        return mat; // torna mat // riga-ok
    } // chiude // riga-ok

    // blocco: anima marker
    private void AggiornaEvidenzaVisuale() // roba priv // riga-ok
    { // apre // riga-ok
        if (!evidenziaInVisualePlayer || markerVisuale == null) return; // se off // riga-ok

        float pulse = (Mathf.Sin(Time.time * 4.5f) + 1f) * 0.5f; // calcola // riga-ok
        float scala = scalaMarkerVisuale * Mathf.Lerp(0.85f, 1.25f, pulse); // scala ora // riga-ok
        markerVisuale.transform.localScale = Vector3.one * scala; // applica // riga-ok

        // Aggiunge un movimento di fluttuazione (su e giù) per renderlo inequivocabile
        float floatOffset = Mathf.Sin(Time.time * 2.5f) * 0.35f; // offset fluttuazione // riga-ok
        
        // FIX VISIBILITA': Se l'oggetto è un grande server rack (come nel Settore 2) e la luce 
        // è in basso nello scaffale, il marker finirebbe nascosto dentro il mobile.
        // Soluzione: troviamo il punto più alto del collider in World Space e piazziamo il marker lì.
        Collider col = GetComponent<Collider>(); // setta // riga-ok
        // blocco: controlla se va
        if (col != null) // se ok // riga-ok
        { // apre // riga-ok
            // Posiziona il marker al centro X/Z, ma sopra il tetto del collider
            markerVisuale.transform.position = new Vector3(
                col.bounds.center.x, 
                col.bounds.max.y + 0.6f + floatOffset, 
                col.bounds.center.z
            ); // pos world // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            // Fallback se non ha un collider
            markerVisuale.transform.position = transform.position + Vector3.up * (altezzaMarkerVisuale + floatOffset); // pos fallback // riga-ok
        } // chiude // riga-ok

        // FIX: non ruotare il marker verso la camera — era la causa del movimento apparente.
        // Facciamo ruotare l'oggetto sul suo asse Y locale così l'anello gira in modo figo.
        markerVisuale.transform.localRotation = Quaternion.Euler(0f, Time.time * 120f, 0f); // ruota anello // riga-ok

        if (luceMarkerVisuale != null) // se ok // riga-ok
        { // apre // riga-ok
            luceMarkerVisuale.intensity = intensitaLuceVisuale * Mathf.Lerp(0.65f, 1.35f, pulse); // pulsa // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: pulizia marker
    private void OnDestroy() // roba priv // riga-ok
    { // apre // riga-ok
        if (materialeMarkerVisuale != null) Destroy(materialeMarkerVisuale); // elimina mat // riga-ok
    } // chiude // riga-ok

    /// <summary>
    /// Chiamato da PlayerInteract quando il giocatore si trova vicino e preme 'E'.
    /// </summary>
    // blocco: funzione fa cose
    public void Interact() // roba pub // riga-ok
    { // apre // riga-ok
        ApriSchermataOlogramma(); // chiama // riga-ok
    } // chiude // riga-ok

    /// <summary>
    /// Supporto per clic diretto con il mouse sull'oggetto 3D nella scena.
    /// </summary>
    // blocco: funzione fa cose
    private void OnMouseDown() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (ModalUIState.IsModalOpen) return; // se ok // riga-ok
        ApriSchermataOlogramma(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void ApriSchermataOlogramma() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (suonoApertura != null) // se ok // riga-ok
        { // apre // riga-ok
            AudioSource.PlayClipAtPoint(suonoApertura, transform.position, volumeAudio); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (DatapadOlogrammaUI.Instance != null) // se ok // riga-ok
        { // apre // riga-ok
            DatapadOlogrammaUI.Instance.ApriOlogramma(this); // chiama // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            // Crea l'interfaccia olografica dinamicamente se non ancora presente
            GameObject uiObj = new GameObject("DatapadOlogrammaUI"); // setta // riga-ok
            DatapadOlogrammaUI ui = uiObj.AddComponent<DatapadOlogrammaUI>(); // setta // riga-ok
            ui.ApriOlogramma(this); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    /// <summary>
    /// Restituisce la lista completa delle voci (rilevando automaticamente le porte o unendo le voci personalizzate).
    /// </summary>
    // blocco: funzione fa cose
    public List<VoceCodicePorta> OttieniTuttiICodici() // roba pub // riga-ok
    { // apre // riga-ok
        List<VoceCodicePorta> listaCompleta = new List<VoceCodicePorta>(); // setta // riga-ok

        // blocco: controlla se va
        if (autoRilevaPorteScena) // se ok // riga-ok
        { // apre // riga-ok
            // Cerca tutti i TerminalePorta attivi o presenti nella scena
            TerminalePorta[] tuttiITerminali = UnityEngine.Object.FindObjectsByType<TerminalePorta>(FindObjectsSortMode.None); // setta // riga-ok
            HashSet<string> nomiGiaAggiunti = new HashSet<string>(); // setta // riga-ok

            // blocco: controlla se va
            if (tuttiITerminali != null && tuttiITerminali.Length > 0) // se ok // riga-ok
            { // apre // riga-ok
                // blocco: gira piu volte
                foreach (TerminalePorta t in tuttiITerminali) // ciclo x // riga-ok
                { // apre // riga-ok
                    // blocco: controlla se va
                    if (t == null) continue; // se ok // riga-ok
                    string nome = string.IsNullOrEmpty(t.nomeTerminale) ? t.name : t.nomeTerminale; // setta // riga-ok
                    // blocco: controlla se va
                    if (nomiGiaAggiunti.Contains(nome)) continue; // se ok // riga-ok
                    nomiGiaAggiunti.Add(nome); // chiama // riga-ok

                    string statoStr = t.IsSbloccato ? "✓ SBLOCCATO" : "BLOCCATO [ATTIVO]"; // setta // riga-ok
                    string nota = t.porteCollegate != null && t.porteCollegate.Count > 0 ? $"Collegato alla porta: {t.porteCollegate[0].name}" : "Terminale di sicurezza del settore."; // setta // riga-ok

                    listaCompleta.Add(new VoceCodicePorta() // chiama // riga-ok
                    { // apre // riga-ok
                        nomePorta = nome.ToUpper(), // setta // riga-ok
                        codicePin = t.codiceSegreto, // setta // riga-ok
                        livelloSicurezza = statoStr, // setta // riga-ok
                        note = nota // setta // riga-ok
                    }); // chiama // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // Aggiungi le voci manuali personalizzate
        // blocco: controlla se va
        if (codiciPersonalizzati != null) // se ok // riga-ok
        { // apre // riga-ok
            // blocco: gira piu volte
            foreach (VoceCodicePorta v in codiciPersonalizzati) // ciclo x // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (v != null && !string.IsNullOrEmpty(v.nomePorta)) // se ok // riga-ok
                { // apre // riga-ok
                    listaCompleta.Add(v); // chiama // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        return listaCompleta; // torna val // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
