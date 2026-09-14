// ============================================================================
// Crisis Protocol / Sector Containment - Player
// File: .\Assets\CrisisProtocol\Scripts\Player\PlayerInteract.cs
// Responsabilita': gestisce input, movimento, combattimento, interazione, salute o strumenti controllati dal giocatore.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine; // usa lib // riga-ok
using UnityEngine.InputSystem; // usa lib // riga-ok
using CrisisProtocol.UI; // usa lib // riga-ok

// blocco: classe x roba grossa
public class PlayerInteract : MonoBehaviour // classe qui // riga-ok
{ // apre // riga-ok
    [Header("Configurazione Prossimità")] // nota unity // riga-ok
    [Tooltip("Raggio della sfera di rilevamento per l'interazione di prossimità.")] // nota unity // riga-ok
    [SerializeField] private float interactionRadius = 3f; // setta // riga-ok

    [Tooltip("Il layer contenente gli oggetti interagibili.")] // nota unity // riga-ok
    [SerializeField] private LayerMask interactableLayer; // ok qua // riga-ok

    [Header("Riferimenti UI / Feedback")] // nota unity // riga-ok
    [Tooltip("Il pannello o bottone UI generico di interazione che deve apparire (es. 'Premi E per interagire').")] // nota unity // riga-ok
    [SerializeField] private GameObject interactionPromptUI; // ok qua // riga-ok

    [Tooltip("La scheda informativa di dettaglio specifica dell'oggetto.")] // nota unity // riga-ok
    [SerializeField] private GameObject interactionDetailCard; // ok qua // riga-ok

    // Campi di stato interni
    private IInteractable currentInteractable; // roba pub // riga-ok
    private Collider currentCollider; // roba pub // riga-ok
    private bool isCardOpen = false; // roba pub // riga-ok

    // blocco: funzione fa cose
    private void Update() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (ModalUIState.IsModalOpen) // se ok // riga-ok
            return; // torna val // riga-ok

        CheckProximity(); // chiama // riga-ok
        HandleInteractionInput(); // chiama // riga-ok
    } // chiude // riga-ok

    /// <summary>
    /// Esegue lo screening volumetrico tramite OverlapSphere per identificare l'interagibile più vicino.
    /// </summary>
    // blocco: funzione fa cose
    private void CheckProximity() // roba pub // riga-ok
    { // apre // riga-ok
        // Se la scheda di dettaglio è aperta, blocchiamo il rilevamento per mantenere il focus sull'oggetto corrente
        // blocco: controlla se va
        if (isCardOpen) return; // se ok // riga-ok

        // Layer mask flessibile: se non impostata o non include Default/Interactable, includi tutto ciò che è rilevante
        int mask = interactableLayer.value; // setta // riga-ok
        // blocco: controlla se va
        if (mask == 0) // se ok // riga-ok
        { // apre // riga-ok
            mask = ~0; // Everything // setta // riga-ok
        } // chiude // riga-ok

        // Rilevamento dei collisori all'interno del raggio d'azione
        Collider[] colliders = Physics.OverlapSphere(transform.position, Mathf.Max(interactionRadius, 3.5f), mask); // setta // riga-ok
        
        // blocco: controlla se va
        if (colliders.Length > 0) // se ok // riga-ok
        { // apre // riga-ok
            // Identificazione del collisore geometricamente più vicino alla superficie del Player
            Collider closestCollider = GetClosestCollider(colliders); // setta // riga-ok

            // blocco: controlla se va
            if (closestCollider != null) // se ok // riga-ok
            { // apre // riga-ok
                IInteractable interactable = closestCollider.GetComponent<IInteractable>()  // setta // riga-ok
                                          ?? closestCollider.GetComponentInParent<IInteractable>() // chiama // riga-ok
                                          ?? closestCollider.GetComponentInChildren<IInteractable>(); // chiama // riga-ok

                // blocco: controlla se va
                if (interactable != null) // se ok // riga-ok
                { // apre // riga-ok
                    // blocco: controlla se va
                    if (closestCollider != currentCollider || interactable != currentInteractable) // se ok // riga-ok
                    { // apre // riga-ok
                        currentCollider = closestCollider; // setta // riga-ok
                        currentInteractable = interactable; // setta // riga-ok
                        TogglePromptUI(true); // chiama // riga-ok
                        Debug.Log($"<color=cyan>[PROSSIMITÀ]</color> Target interagibile agganciato: <b>{closestCollider.name}</b>"); // logga // riga-ok
                    } // chiude // riga-ok
                    return; // torna val // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // Reset dello stato in caso di assenza di collisori interagibili nel volume di scansione
        ResetTargetState(); // chiama // riga-ok
    } // chiude // riga-ok

    /// <summary>
    /// Gestisce la ricezione dell'input e l'esecuzione sequenziale dell'interazione o dell'apertura dell'interfaccia.
    /// </summary>
    // blocco: funzione fa cose
    private void HandleInteractionInput() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) // se ok // riga-ok
        { // apre // riga-ok
            // Caso 1: La scheda dettagli è già aperta -> La chiudiamo e resettiamo lo stato
            // blocco: controlla se va
            if (isCardOpen) // se ok // riga-ok
            { // apre // riga-ok
                CloseDetailCard(); // chiama // riga-ok
                return; // torna val // riga-ok
            } // chiude // riga-ok

            // Caso 2: Premuto E in prossimità di un oggetto valido
            // blocco: controlla se va
            if (currentInteractable != null) // se ok // riga-ok
            { // apre // riga-ok
                TogglePromptUI(false); // Nasconde il prompt di notifica iniziale // ok qua // riga-ok

                // blocco: controlla se va
                if (interactionDetailCard != null) // se ok // riga-ok
                { // apre // riga-ok
                    // Apertura della scheda di interazione strutturata
                    OpenDetailCard(); // chiama // riga-ok
                } // chiude // riga-ok
                // blocco: caso diverso
                else // se no // riga-ok
                { // apre // riga-ok
                    // FALLBACK DEBUG: Esecuzione diretta dell'acquisizione/interazione se manca la scheda UI
                    ExecuteDirectInteraction(); // chiama // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private Collider GetClosestCollider(Collider[] colliders) // roba pub // riga-ok
    { // apre // riga-ok
        Collider bestTarget = null; // setta // riga-ok
        float closestDistanceSqr = Mathf.Infinity; // setta // riga-ok
        Vector3 playerPos = transform.position; // setta // riga-ok

        // blocco: gira piu volte
        foreach (Collider potentialTarget in colliders) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (potentialTarget == null) continue; // se ok // riga-ok

            // Ignora il collider del Player stesso
            // blocco: controlla se va
            if (potentialTarget.transform.root == transform.root) continue; // se ok // riga-ok

            // Verifica che l'oggetto o i suoi parent/figli implementino IInteractable
            IInteractable candidate = potentialTarget.GetComponent<IInteractable>()  // setta // riga-ok
                                   ?? potentialTarget.GetComponentInParent<IInteractable>() // chiama // riga-ok
                                   ?? potentialTarget.GetComponentInChildren<IInteractable>(); // chiama // riga-ok
            // blocco: controlla se va
            if (candidate == null) continue; // se ok // riga-ok

            // Calcolo della distanza reale dalla superficie del collider (ClosestPoint) invece del pivot centrale
            Vector3 puntoSuperficie = potentialTarget.ClosestPoint(playerPos); // setta // riga-ok
            float dSqr = (puntoSuperficie - playerPos).sqrMagnitude; // setta // riga-ok
            
            // blocco: controlla se va
            if (dSqr < closestDistanceSqr) // se ok // riga-ok
            { // apre // riga-ok
                closestDistanceSqr = dSqr; // setta // riga-ok
                bestTarget = potentialTarget; // setta // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        return bestTarget; // torna val // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OpenDetailCard() // roba pub // riga-ok
    { // apre // riga-ok
        interactionDetailCard.SetActive(true); // chiama // riga-ok
        isCardOpen = true; // setta // riga-ok
        Debug.Log($"<color=green>[UI]</color> Apertura scheda dettagliata per l'oggetto: <b>{currentCollider.name}</b>."); // logga // riga-ok
        
        // Esegue comunque l'interazione logica se richiesto dall'architettura del componente
        currentInteractable.Interact(); // chiama // riga-ok

        // blocco: controlla se va
        if (ModalUIState.IsModalOpen) // se ok // riga-ok
        { // apre // riga-ok
            interactionDetailCard.SetActive(false); // chiama // riga-ok
            isCardOpen = false; // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void CloseDetailCard() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (interactionDetailCard != null) interactionDetailCard.SetActive(false); // se ok // riga-ok
        isCardOpen = false; // setta // riga-ok
        Debug.Log("[UI] Chiusura scheda dettagliata. Ripristino scansione di prossimità."); // logga // riga-ok
        ResetTargetState(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void ExecuteDirectInteraction() // roba pub // riga-ok
    { // apre // riga-ok
        currentInteractable.Interact(); // chiama // riga-ok
        Debug.Log($"<color=orange>[INTERAZIONE DIRECTA]</color> Acquisizione completata per: <b>{currentCollider.name}</b>."); // logga // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void TogglePromptUI(bool state) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (interactionPromptUI != null) // se ok // riga-ok
        { // apre // riga-ok
            interactionPromptUI.SetActive(state); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (CyberHUD.Instance != null) // se ok // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (state && currentInteractable != null) // se ok // riga-ok
            { // apre // riga-ok
                OttieniDescrizioneTarget(currentInteractable, currentCollider, out string titolo, out string azione); // chiama // riga-ok
                CyberHUD.Instance.MostraPrompt(titolo, azione); // chiama // riga-ok
            } // chiude // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
            { // apre // riga-ok
                CyberHUD.Instance.NascondiPrompt(); // chiama // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OttieniDescrizioneTarget(IInteractable interactable, Collider col, out string titolo, out string azione) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (interactable is AccessCredentialPickup keycard) // se ok // riga-ok
        { // apre // riga-ok
            titolo = $"AUTORIZZAZIONE: {keycard.DisplayName.ToUpper()}"; // setta // riga-ok
            azione = "Premi [E] per Raccogliere Scheda di Accesso"; // setta // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (interactable is EmergencyHotspot hotspot) // se ok // riga-ok
        { // apre // riga-ok
            string nome = hotspot.name.ToLower(); // setta // riga-ok
            // blocco: controlla se va
            if (nome.Contains("tank") || nome.Contains("chemic")) // se ok // riga-ok
            { // apre // riga-ok
                titolo = "SERBATOIO CHIMICO // REATTORE 002"; // setta // riga-ok
                azione = "Premi [E] per Sigillare Falla e Fermare Perdita Tossica"; // setta // riga-ok
            } // chiude // riga-ok
            // blocco: controlla se va
            else if (nome.Contains("generator") || nome.Contains("basic")) // se ok // riga-ok
            { // apre // riga-ok
                titolo = "GENERATORE AUSILIARIO // SOVRACCARICO 001"; // setta // riga-ok
                azione = "Premi [E] per Stabilizzare Sovraccarico Energetico"; // setta // riga-ok
            } // chiude // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
            { // apre // riga-ok
                titolo = $"FOCOLAIO DI EMERGENZA // {hotspot.name.ToUpper()}"; // setta // riga-ok
                azione = "Premi [E] per Sigillare e Contenere Emergenza"; // setta // riga-ok
            } // chiude // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (interactable is PortaSettore porta) // se ok // riga-ok
        { // apre // riga-ok
            titolo = "PORTA BLINDATA DI SETTORE"; // setta // riga-ok
            azione = porta.PuoEssereAperta() ? "Premi [E] per Aprire / Chiudere" : "PORTA BLOCCATA: Richiede Autorizzazione o Bypass"; // setta // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (interactable is TerminalePorta terminale) // se ok // riga-ok
        { // apre // riga-ok
            titolo = "TERMINALE DI SICUREZZA"; // setta // riga-ok
            azione = "Premi [E] per Inserire Codice o Avviare Bypass Minigioco"; // setta // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (interactable is DatapadCodiciPorte datapad) // se ok // riga-ok
        { // apre // riga-ok
            titolo = "DATAPAD SCIENTIFICO DI SETTORE"; // setta // riga-ok
            azione = "Premi [E] per Leggere Informazioni e Codici Tattici"; // setta // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        string rawName = col != null ? col.name.ToUpper() : "OGGETTO INTERATTIVO"; // setta // riga-ok
        titolo = $"TARGET: {rawName}"; // setta // riga-ok
        azione = "Premi [E] per Interagire"; // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void ResetTargetState() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (currentCollider != null || currentInteractable != null) // se ok // riga-ok
        { // apre // riga-ok
            TogglePromptUI(false); // chiama // riga-ok
            currentCollider = null; // setta // riga-ok
            currentInteractable = null; // setta // riga-ok
            Debug.Log("[PROSSIMITÀ] Il Player è uscito dal raggio d'azione o il target è stato rimosso."); // logga // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnDrawGizmos() // roba pub // riga-ok
    { // apre // riga-ok
        // Visualizzazione volumetrica del raggio di prossimità all'interno dell'Editor di Unity
        Gizmos.color = currentInteractable != null ? Color.green : Color.yellow; // setta // riga-ok
        Gizmos.DrawWireSphere(transform.position, interactionRadius); // chiama // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
