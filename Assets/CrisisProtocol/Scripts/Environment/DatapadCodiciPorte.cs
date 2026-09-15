// ============================================================================
// Crisis Protocol / Sector Containment - Ambiente interattivo
// File: .\Assets\CrisisProtocol\Scripts\Environment\DatapadCodiciPorte.cs
// Responsabilita': controlla porte, datapad, teletrasporti, camera o oggetti di scena collegati alla progressione.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using System;
using System.Collections.Generic;
using UnityEngine;
using CrisisProtocol.UI;
/// <summary>
/// Struttura dati per una voce di porta/settore visualizzata sull'ologramma.
/// </summary>
[Serializable]
public class VoceCodicePorta
{
    [Tooltip("Nome identificativo della porta o del settore (es. 'SETTORE REATTORE A1').")]
    public string nomePorta = "SETTORE REATTORE";
    [Tooltip("Codice PIN numerico a 4 cifre per l'apertura.")]
    public string codicePin = "4281";
    [Tooltip("Stato o livello di sicurezza (es. 'LIVELLO SICUREZZA 3', 'BLOCCATA', 'ACCESSO RISERVATO').")]
    public string livelloSicurezza = "ACCESSO RISERVATO";
    [Tooltip("Nota informativa o descrizione sul settore.")]
    [TextArea(2, 3)]
    public string note = "Richiesto terminale di controllo o scansione biometrica.";
}
/// <summary>
/// Script da assegnare a qualsiasi oggetto nella scena (Datapad, tablet, console, terminale o proiettore olografico).
/// Quando il giocatore si avvicina e preme 'E' (o ci clicca sopra col mouse), apre un'interfaccia olografica
/// verde acqua / ciano in stile LED con l'elenco dei codici PIN delle porte del livello.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DatapadCodiciPorte : MonoBehaviour, IInteractable
{
    [Header("Intestazione Ologramma")]
    [Tooltip("Titolo visualizzato in cima alla schermata olografica.")]
    public string titoloDatapad = "DATAPAD DI SICUREZZA // REGISTRO CODICI ACCESSO";
    [Tooltip("Sottotitolo o nota d'archivio.")]
    public string autoreONota = "Ufficio Sicurezza Struttura - Memorandum Riservato";
    [Header("Rilevamento Automatico Porte")]
    [Tooltip("Se attivo, cerca automaticamente tutti i TerminalePorta presenti nella scena e ne ricava nome e codice PIN.")]
    public bool autoRilevaPorteScena = true;
    [Header("Codici Manuali / Aggiuntivi")]
    [Tooltip("Voci manuali personalizzate di porte e codici da mostrare nell'ologramma.")]
    public List<VoceCodicePorta> codiciPersonalizzati = new List<VoceCodicePorta>()
    {
        new VoceCodicePorta() { nomePorta = "PORTA SETTORE REATTORE", codicePin = "4281", livelloSicurezza = "LIVELLO 3", note = "Terminale di sicurezza adiacente al varco." },
        new VoceCodicePorta() { nomePorta = "LABORATORIO BIOLOGICO", codicePin = "7139", livelloSicurezza = "LIVELLO 2", note = "Codice di emergenza per quarantena." },
        new VoceCodicePorta() { nomePorta = "HANGAR DI CONTENIMENTO", codicePin = "9024", livelloSicurezza = "LIVELLO 4", note = "Accesso riservato al personale autorizzato." }
    };
    [Header("Feedback Visivo 3D Oggetto")]
    [Tooltip("Luce del proiettore/schermo (opzionale, verde acqua).")]
    public Light luceOlogramma;
    [Tooltip("Renderer della mesh dello schermo/datapad per il materiale emissivo.")]
    public Renderer meshSchermo;
    [Tooltip("Colore luce ed emissione dello schermo (Cyan/Verde Acqua).")]
    public Color coloreOlogramma = new Color(0.1f, 0.95f, 0.85f, 1f);
    [Tooltip("Se true, la luce del datapad pulsa dolcemente per attirare l'attenzione.")]
    public bool animaPulsazioneLuce = true;
    [Header("Evidenziazione Visuale Player")]
    [Tooltip("Se attivo, crea da solo un marker luminoso sopra il datapad nella visuale del player.")]
    [SerializeField] private bool evidenziaInVisualePlayer = true;
    [Tooltip("Altezza del marker luminoso sopra l'oggetto.")]
    [SerializeField] private float altezzaMarkerVisuale = 2.5f;
    [Tooltip("Grandezza del marker visivo automatico.")]
    [SerializeField] private float scalaMarkerVisuale = 0.35f;
    [Tooltip("Distanza della luce usata per far risaltare il datapad.")]
    [SerializeField] private float raggioLuceVisuale = 7.0f;
    [Tooltip("Intensita' base della luce automatica sopra il datapad.")]
    [SerializeField] private float intensitaLuceVisuale = 3.5f;
    [Header("Audio")]
    [Tooltip("Suono di accensione / battitura all'apertura del datapad.")]
    [SerializeField] private AudioClip suonoApertura;
    [Range(0f, 1f)] [SerializeField] private float volumeAudio = 0.9f;
    private float intensitaLuceBase = 2.0f;

    // Marker creato a runtime sopra il datapad. Non lo salviamo come prefab:
    // deve adattarsi a qualunque oggetto abbia questo script, anche asset importati.
    private GameObject markerVisuale;
    private Light luceMarkerVisuale;
    private Renderer rendererMarkerVisuale;
    private Material materialeMarkerVisuale;
    private Camera cameraPrincipale;

    // Lock world-space: alcuni datapad sono figli di porte o oggetti animati.
    // Registriamo la posa iniziale e la ripristiniamo in LateUpdate, così la UI
    // non scappa via quando si muove il parent.
    private Vector3    _posMondiale;
    private Quaternion _rotMondiale;
    private bool       _posizioneFissata = false;
    private Vector3    _lucePosWorld;
    private Quaternion _luceRotWorld;
    private bool       _lucePosRegistrata = false;
    void Start()
    {
        // Fallback da prototipo: se chi monta la scena dimentica il layer, il
        // datapad prova a mettersi da solo su Interactable.
        if (gameObject.layer == 0)
        {
            int interactableLayer = LayerMask.NameToLayer("Interactable");
            if (interactableLayer != -1)
                gameObject.layer = interactableLayer;
        }
        // La mesh e la luce usano lo stesso colore base, così il datapad legge
        // come oggetto unico anche quando luce e schermo sono GameObject separati.
        if (luceOlogramma != null)
        {
            luceOlogramma.color = coloreOlogramma;
            intensitaLuceBase = luceOlogramma.intensity > 0 ? luceOlogramma.intensity : 2.0f;
        }
        if (meshSchermo != null && meshSchermo.material != null)
        {
            Material mat = meshSchermo.material;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", coloreOlogramma);
            else if (mat.HasProperty("_Color"))
                mat.color = coloreOlogramma;
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", coloreOlogramma * 2.5f);
            }
        }
        ConfiguraEvidenzaVisuale();
        // Dopo la configurazione salviamo la posa buona. Da qui in poi il datapad
        // resta piantato lì, anche se il parent prova a trascinarlo.
        _posMondiale     = transform.position;
        _rotMondiale     = transform.rotation;
        _posizioneFissata = true;
    }
    void Update()
    {
        // Pulsazione bassa, non gameplay-critical: serve solo a dire "qui c'è
        // qualcosa da leggere", senza diventare un faro fisso.
        if (animaPulsazioneLuce && luceOlogramma != null)
        {
            float sin = Mathf.Sin(Time.time * 3f);
            luceOlogramma.intensity = intensitaLuceBase + (sin * 0.4f);
        }
        AggiornaEvidenzaVisuale();
    }
    // LateUpdate gira dopo animazioni e Update degli altri script: è il momento
    // giusto per annullare spostamenti ereditati da porte/parent mobili.
    void LateUpdate()
    {
        if (!_posizioneFissata) return;
        // Mantiene leggibile il datapad nel mondo: se una porta si apre, il
        // pannello dei codici non deve viaggiare insieme all'anta.
        transform.position = _posMondiale;
        transform.rotation = _rotMondiale;
        // Se la luce è esterna al datapad, la trattiamo come parte del pannello
        // e le diamo lo stesso lock world-space.
        if (luceOlogramma != null && luceOlogramma.transform.parent != transform)
        {
            // Registrazione lazy: la luce può essere posizionata da altri script
            // durante Start, quindi prendiamo la posa al primo LateUpdate utile.
            if (!_lucePosRegistrata)
            {
                _lucePosWorld = luceOlogramma.transform.position;
                _luceRotWorld = luceOlogramma.transform.rotation;
                _lucePosRegistrata = true;
            }
            luceOlogramma.transform.position = _lucePosWorld;
            luceOlogramma.transform.rotation = _luceRotWorld;
        }
    }
    // Marker olografico: rende il datapad leggibile nella visuale senza toccare mesh originali.
    private void ConfiguraEvidenzaVisuale()
    {
        if (!evidenziaInVisualePlayer) return;
        if (markerVisuale != null) return;
        cameraPrincipale = Camera.main;
        markerVisuale = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        markerVisuale.name = "Datapad_View_Highlight";
        markerVisuale.transform.SetParent(transform, false);
        markerVisuale.transform.localPosition = Vector3.up * altezzaMarkerVisuale;
        markerVisuale.transform.localScale = Vector3.one * scalaMarkerVisuale;
        Collider markerCollider = markerVisuale.GetComponent<Collider>();
        if (markerCollider != null) Destroy(markerCollider);
        rendererMarkerVisuale = markerVisuale.GetComponent<Renderer>();
        if (rendererMarkerVisuale != null)
        {
            materialeMarkerVisuale = CreaMaterialeEvidenza();
            rendererMarkerVisuale.material = materialeMarkerVisuale;
        }
        luceMarkerVisuale = markerVisuale.AddComponent<Light>();
        luceMarkerVisuale.type = LightType.Point;
        luceMarkerVisuale.color = coloreOlogramma;
        luceMarkerVisuale.range = raggioLuceVisuale;
        luceMarkerVisuale.intensity = intensitaLuceVisuale;
        // L'anello rende il marker riconoscibile anche se la sfera è piccola o
        // finisce davanti a superfici luminose.
        LineRenderer anello = markerVisuale.AddComponent<LineRenderer>();
        anello.useWorldSpace = false;
        anello.loop = true;
        anello.positionCount = 36;
        anello.startWidth = 0.08f;
        anello.endWidth = 0.08f;
        anello.material = materialeMarkerVisuale;
        anello.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        anello.receiveShadows = false;
        float raggioAnello = 1.6f;
        for (int i = 0; i < 36; i++)
        {
            float rad = Mathf.Deg2Rad * (i * 10f);
            anello.SetPosition(i, new Vector3(Mathf.Sin(rad) * raggioAnello, 0f, Mathf.Cos(rad) * raggioAnello));
        }
    }
    // Materiale runtime dedicato: glow pulito e niente modifiche ai materiali condivisi.
    private Material CreaMaterialeEvidenza()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
        Material mat = new Material(shader);
        Color colore = coloreOlogramma;
        colore.a = 0.85f;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", colore);
        else if (mat.HasProperty("_Color")) mat.color = colore;
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", coloreOlogramma * 3.0f);
        }
        return mat;
    }
    // Pulse leggero: attira l'occhio senza trasformare il datapad in un faro fastidioso.
    private void AggiornaEvidenzaVisuale()
    {
        if (!evidenziaInVisualePlayer || markerVisuale == null) return;
        float pulse = (Mathf.Sin(Time.time * 4.5f) + 1f) * 0.5f;
        float scala = scalaMarkerVisuale * Mathf.Lerp(0.85f, 1.25f, pulse);
        markerVisuale.transform.localScale = Vector3.one * scala;
        // Micro-fluttuazione: l'occhio lo nota, ma non sembra un obiettivo ostile.
        float floatOffset = Mathf.Sin(Time.time * 2.5f) * 0.35f;
        // FIX VISIBILITA': Se l'oggetto è un grande server rack (come nel Settore 2) e la luce 
        // è in basso nello scaffale, il marker finirebbe nascosto dentro il mobile.
        // Soluzione: troviamo il punto più alto del collider in World Space e piazziamo il marker lì.
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            // Per oggetti grandi usiamo il bounds del collider: evita marker
            // nascosti dentro server rack, scaffali o terminali alti.
            markerVisuale.transform.position = new Vector3(
                col.bounds.center.x, 
                col.bounds.max.y + 0.6f + floatOffset, 
                col.bounds.center.z
            );
        }
        else
        {
            // Fallback per asset senza collider: almeno resta sopra il pivot.
            markerVisuale.transform.position = transform.position + Vector3.up * (altezzaMarkerVisuale + floatOffset);
        }
        // FIX: non ruotare il marker verso la camera — era la causa del movimento apparente.
        // Facciamo ruotare l'oggetto sul suo asse Y locale così l'anello gira in modo figo.
        markerVisuale.transform.localRotation = Quaternion.Euler(0f, Time.time * 120f, 0f);
        if (luceMarkerVisuale != null)
        {
            luceMarkerVisuale.intensity = intensitaLuceVisuale * Mathf.Lerp(0.65f, 1.35f, pulse);
        }
    }
    // Pulizia esplicita dei materiali creati runtime: niente leak quando Unity distrugge l'oggetto.
    private void OnDestroy()
    {
        if (materialeMarkerVisuale != null) Destroy(materialeMarkerVisuale);
    }
    /// <summary>
    /// Chiamato da PlayerInteract quando il giocatore si trova vicino e preme 'E'.
    /// </summary>
    public void Interact()
    {
        ApriSchermataOlogramma();
    }
    /// <summary>
    /// Supporto per clic diretto con il mouse sull'oggetto 3D nella scena.
    /// </summary>
    private void OnMouseDown()
    {
        if (ModalUIState.IsModalOpen) return;
        ApriSchermataOlogramma();
    }
    public void ApriSchermataOlogramma()
    {
        if (suonoApertura != null)
        {
            AudioSource.PlayClipAtPoint(suonoApertura, transform.position, volumeAudio);
        }
        if (DatapadOlogrammaUI.Instance != null)
        {
            DatapadOlogrammaUI.Instance.ApriOlogramma(this);
        }
        else
        {
            // Crea l'interfaccia olografica dinamicamente se non ancora presente
            GameObject uiObj = new GameObject("DatapadOlogrammaUI");
            DatapadOlogrammaUI ui = uiObj.AddComponent<DatapadOlogrammaUI>();
            ui.ApriOlogramma(this);
        }
    }
    /// <summary>
    /// Restituisce la lista completa delle voci (rilevando automaticamente le porte o unendo le voci personalizzate).
    /// </summary>
    public List<VoceCodicePorta> OttieniTuttiICodici()
    {
        List<VoceCodicePorta> listaCompleta = new List<VoceCodicePorta>();
        if (autoRilevaPorteScena)
        {
            // Cerca tutti i TerminalePorta attivi o presenti nella scena
            TerminalePorta[] tuttiITerminali = UnityEngine.Object.FindObjectsByType<TerminalePorta>(FindObjectsSortMode.None);
            HashSet<string> nomiGiaAggiunti = new HashSet<string>();
            if (tuttiITerminali != null && tuttiITerminali.Length > 0)
            {
                foreach (TerminalePorta t in tuttiITerminali)
                {
                    if (t == null) continue;
                    string nome = string.IsNullOrEmpty(t.nomeTerminale) ? t.name : t.nomeTerminale;
                    if (nomiGiaAggiunti.Contains(nome)) continue;
                    nomiGiaAggiunti.Add(nome);
                    string statoStr = t.IsSbloccato ? "✓ SBLOCCATO" : "BLOCCATO [ATTIVO]";
                    string nota = t.porteCollegate != null && t.porteCollegate.Count > 0 ? $"Collegato alla porta: {t.porteCollegate[0].name}" : "Terminale di sicurezza del settore.";
                    listaCompleta.Add(new VoceCodicePorta()
                    {
                        nomePorta = nome.ToUpper(),
                        codicePin = t.codiceSegreto,
                        livelloSicurezza = statoStr,
                        note = nota
                    });
                }
            }
        }
        // Aggiungi le voci manuali personalizzate
        if (codiciPersonalizzati != null)
        {
            foreach (VoceCodicePorta v in codiciPersonalizzati)
            {
                if (v != null && !string.IsNullOrEmpty(v.nomePorta))
                {
                    listaCompleta.Add(v);
                }
            }
        }
        return listaCompleta;
    }
}
