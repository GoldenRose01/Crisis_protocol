using System;
using System.Collections.Generic;
using UnityEngine;
using GoldenCast.UI;

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

    [Tooltip("Colore del LED/Ologramma dell'oggetto nel mondo 3D.")]
    public Color coloreOlogramma = new Color(0.0f, 0.95f, 0.85f); // Verde Acqua / Ciano brillante

    [Tooltip("Attiva una lieve pulsazione luminosa sul proiettore olografico.")]
    public bool animaPulsazioneLuce = true;

    private float intensitaLuceBase = 2.0f;

    void Start()
    {
        // Se non è stato impostato il layer Interactable, applicalo per abilitare la pressione di E con PlayerInteract
        if (gameObject.layer == 0)
        {
            int interactableLayer = LayerMask.NameToLayer("Interactable");
            if (interactableLayer != -1)
                gameObject.layer = interactableLayer;
        }

        // Configura il colore iniziale della luce e dell'emissione
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
    }

    void Update()
    {
        if (animaPulsazioneLuce && luceOlogramma != null)
        {
            float sin = Mathf.Sin(Time.time * 3f);
            luceOlogramma.intensity = intensitaLuceBase + (sin * 0.4f);
        }
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
                    string nota = t.portaCollegata != null ? $"Collegato alla porta: {t.portaCollegata.name}" : "Terminale di sicurezza del settore.";

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
