// ============================================================================
// Crisis Protocol / Sector Containment - Player
// File: .\Assets\CrisisProtocol\Scripts\Player\PlayerInteract.cs
// Responsabilita': gestisce input, movimento, combattimento, interazione, salute o strumenti controllati dal giocatore.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine;
using UnityEngine.InputSystem;
using CrisisProtocol.UI;
public class PlayerInteract : MonoBehaviour
{
    [Header("Configurazione Prossimità")]
    [Tooltip("Raggio della sfera di rilevamento per l'interazione di prossimità.")]
    [SerializeField] private float interactionRadius = 3f;
    [Tooltip("Il layer contenente gli oggetti interagibili.")]
    [SerializeField] private LayerMask interactableLayer;
    [Header("Riferimenti UI / Feedback")]
    [Tooltip("Il pannello o bottone UI generico di interazione che deve apparire (es. 'Premi E per interagire').")]
    [SerializeField] private GameObject interactionPromptUI;
    [Tooltip("La scheda informativa di dettaglio specifica dell'oggetto.")]
    [SerializeField] private GameObject interactionDetailCard;
    // Target attualmente agganciato dal raggio di prossimità. Li teniamo separati
    // perché il collider può stare su un figlio, mentre IInteractable spesso sta sul parent.
    private IInteractable currentInteractable;
    private Collider currentCollider;
    private bool isCardOpen = false;

    private void Update()
    {
        // Se c'e' una modale aperta (datapad, terminale, tutorial), il player non
        // deve rubare input con E o cambiare target sotto la finestra.
        if (ModalUIState.IsModalOpen)
            return;

        CheckProximity();
        HandleInteractionInput();
    }
    /// <summary>
    /// Esegue lo screening volumetrico tramite OverlapSphere per identificare l'interagibile più vicino.
    /// </summary>
    private void CheckProximity()
    {
        // La scheda dettaglio "congela" il target: mentre la leggi, lo scanner
        // non deve saltare a un altro oggetto solo perché ti sei mosso di poco.
        if (isCardOpen) return;

        // Se il layer non e' configurato in Inspector, fallback su Everything.
        // È meno elegante, ma salva le scene prototipo dove il layer manca.
        int mask = interactableLayer.value;
        if (mask == 0)
        {
            mask = ~0; // Everything
        }
        // OverlapSphere è più robusto di un raycast singolo: funziona anche con
        // oggetti bassi, collider grandi o datapad messi di lato.
        Collider[] colliders = Physics.OverlapSphere(transform.position, Mathf.Max(interactionRadius, 3.5f), mask);
        if (colliders.Length > 0)
        {
            // Scegliamo il collider più vicino alla superficie, non al pivot:
            // tanti asset importati hanno pivot buttati a caso.
            Collider closestCollider = GetClosestCollider(colliders);
            if (closestCollider != null)
            {
                IInteractable interactable = closestCollider.GetComponent<IInteractable>()
                                          ?? closestCollider.GetComponentInParent<IInteractable>()
                                          ?? closestCollider.GetComponentInChildren<IInteractable>();
                if (interactable != null)
                {
                    if (closestCollider != currentCollider || interactable != currentInteractable)
                    {
                        currentCollider = closestCollider;
                        currentInteractable = interactable;
                        TogglePromptUI(true);
                        Debug.Log($"<color=cyan>[PROSSIMITÀ]</color> Target interagibile agganciato: <b>{closestCollider.name}</b>");
                    }
                    return;
                }
            }
        }
        // Nessun target valido nel volume: spegni prompt e lock HUD.
        ResetTargetState();
    }
    /// <summary>
    /// Gestisce la ricezione dell'input e l'esecuzione sequenziale dell'interazione o dell'apertura dell'interfaccia.
    /// </summary>
    private void HandleInteractionInput()
    {
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            // Tap su E mentre una scheda è aperta = chiusura rapida, tipo "ok ho letto".
            if (isCardOpen)
            {
                CloseDetailCard();
                return;
            }
            // Se esiste una scheda UI usiamo quella; se manca, chiamiamo direttamente
            // Interact. Questo tiene giocabili anche scene prototipo incomplete.
            if (currentInteractable != null)
            {
                TogglePromptUI(false);
                if (interactionDetailCard != null)
                {
                    OpenDetailCard();
                }
                else
                {
                    ExecuteDirectInteraction();
                }
            }
        }
    }
    private Collider GetClosestCollider(Collider[] colliders)
    {
        // Piccola utility "da gameplay": tra tutti i collider trovati, prende
        // quello che il player sta davvero toccando/guardando più da vicino.
        Collider bestTarget = null;
        float closestDistanceSqr = Mathf.Infinity;
        Vector3 playerPos = transform.position;
        foreach (Collider potentialTarget in colliders)
        {
            if (potentialTarget == null) continue;
            // Evita auto-lock su collider del player, arma o figli del rig.
            if (potentialTarget.transform.root == transform.root) continue;

            // Cerca l'interfaccia in parent/figli perché i prefab importati non
            // hanno sempre script e collider sullo stesso GameObject.
            IInteractable candidate = potentialTarget.GetComponent<IInteractable>()
                                   ?? potentialTarget.GetComponentInParent<IInteractable>()
                                   ?? potentialTarget.GetComponentInChildren<IInteractable>();
            if (candidate == null) continue;
            // ClosestPoint rende il confronto sensato con porte larghe e terminali
            // con pivot lontani dalla parte effettivamente interagibile.
            Vector3 puntoSuperficie = potentialTarget.ClosestPoint(playerPos);
            float dSqr = (puntoSuperficie - playerPos).sqrMagnitude;
            if (dSqr < closestDistanceSqr)
            {
                closestDistanceSqr = dSqr;
                bestTarget = potentialTarget;
            }
        }
        return bestTarget;
    }
    private void OpenDetailCard()
    {
        // La scheda è opzionale: quando c'e', fa da preview/descrizione prima
        // dell'azione logica. Alcuni interagibili aprono subito una modale loro.
        interactionDetailCard.SetActive(true);
        isCardOpen = true;
        Debug.Log($"<color=green>[UI]</color> Apertura scheda dettagliata per l'oggetto: <b>{currentCollider.name}</b>.");
        // Esegue comunque l'interazione logica se richiesto dall'architettura del componente
        currentInteractable.Interact();
        if (ModalUIState.IsModalOpen)
        {
            interactionDetailCard.SetActive(false);
            isCardOpen = false;
        }
    }
    private void CloseDetailCard()
    {
        if (interactionDetailCard != null) interactionDetailCard.SetActive(false);
        isCardOpen = false;
        Debug.Log("[UI] Chiusura scheda dettagliata. Ripristino scansione di prossimità.");
        ResetTargetState();
    }
    private void ExecuteDirectInteraction()
    {
        currentInteractable.Interact();
        Debug.Log($"<color=orange>[INTERAZIONE DIRECTA]</color> Acquisizione completata per: <b>{currentCollider.name}</b>.");
    }
    private void TogglePromptUI(bool state)
    {
        // Mantiene allineati prompt vecchio e CyberHUD nuovo. Finché entrambi
        // possono esistere in scena, aggiornarli insieme evita doppie UI incoerenti.
        if (interactionPromptUI != null)
        {
            interactionPromptUI.SetActive(state);
        }
        if (CyberHUD.Instance != null)
        {
            if (state && currentInteractable != null)
            {
                OttieniDescrizioneTarget(currentInteractable, currentCollider, out string titolo, out string azione);
                CyberHUD.Instance.MostraPrompt(titolo, azione);
            }
            else
            {
                CyberHUD.Instance.NascondiPrompt();
            }
        }
    }
    private void OttieniDescrizioneTarget(IInteractable interactable, Collider col, out string titolo, out string azione)
    {
        // Mappa tipi gameplay -> testo HUD. È volutamente qui, vicino al sistema
        // di lock, così non spargiamo label UI dentro ogni singolo prefab.
        if (interactable is AccessCredentialPickup keycard)
        {
            titolo = $"AUTORIZZAZIONE: {keycard.DisplayName.ToUpper()}";
            azione = "Premi [E] per Raccogliere Scheda di Accesso";
            return;
        }
        if (interactable is EmergencyHotspot hotspot)
        {
            string nome = hotspot.name.ToLower();
            if (nome.Contains("tank") || nome.Contains("chemic"))
            {
                titolo = "SERBATOIO CHIMICO // REATTORE 002";
                azione = "Premi [E] per Sigillare Falla e Fermare Perdita Tossica";
            }
            else if (nome.Contains("generator") || nome.Contains("basic"))
            {
                titolo = "GENERATORE AUSILIARIO // SOVRACCARICO 001";
                azione = "Premi [E] per Stabilizzare Sovraccarico Energetico";
            }
            else
            {
                titolo = $"FOCOLAIO DI EMERGENZA // {hotspot.name.ToUpper()}";
                azione = "Premi [E] per Sigillare e Contenere Emergenza";
            }
            return;
        }
        if (interactable is PortaSettore porta)
        {
            titolo = "PORTA BLINDATA DI SETTORE";
            azione = porta.PuoEssereAperta() ? "Premi [E] per Aprire / Chiudere" : "PORTA BLOCCATA: Richiede Autorizzazione o Bypass";
            return;
        }
        if (interactable is TerminalePorta terminale)
        {
            titolo = "TERMINALE DI SICUREZZA";
            azione = "Premi [E] per Inserire Codice o Avviare Bypass Minigioco";
            return;
        }
        if (interactable is DatapadCodiciPorte datapad)
        {
            titolo = "DATAPAD SCIENTIFICO DI SETTORE";
            azione = "Premi [E] per Leggere Informazioni e Codici Tattici";
            return;
        }
        string rawName = col != null ? col.name.ToUpper() : "OGGETTO INTERATTIVO";
        titolo = $"TARGET: {rawName}";
        azione = "Premi [E] per Interagire";
    }
    private void ResetTargetState()
    {
        // Reset unico dello stato target: evita casi strani dove il prompt resta
        // acceso anche dopo essere usciti dal raggio o aver raccolto l'oggetto.
        if (currentCollider != null || currentInteractable != null)
        {
            TogglePromptUI(false);
            currentCollider = null;
            currentInteractable = null;
            Debug.Log("[PROSSIMITÀ] Il Player è uscito dal raggio d'azione o il target è stato rimosso.");
        }
    }
    private void OnDrawGizmos()
    {
        // Visualizzazione volumetrica del raggio di prossimità all'interno dell'Editor di Unity
        Gizmos.color = currentInteractable != null ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
