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
{ // apre // riga-ok
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

    [Header("Audio")] // nota unity // riga-ok
    [Tooltip("Suono di accensione / battitura all'apertura del datapad.")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoApertura; // ok qua // riga-ok
    [Range(0f, 1f)] [SerializeField] private float volumeAudio = 0.9f; // setta // riga-ok

    private float intensitaLuceBase = 2.0f; // roba pub // riga-ok

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
    } // chiude // riga-ok

    void Update() // chiama // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (animaPulsazioneLuce && luceOlogramma != null) // se ok // riga-ok
        { // apre // riga-ok
            float sin = Mathf.Sin(Time.time * 3f); // setta // riga-ok
            luceOlogramma.intensity = intensitaLuceBase + (sin * 0.4f); // setta // riga-ok
        } // chiude // riga-ok
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
