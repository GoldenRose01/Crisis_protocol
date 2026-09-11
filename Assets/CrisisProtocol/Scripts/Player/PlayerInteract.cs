using UnityEngine;
using UnityEngine.InputSystem;
using GoldenCast.UI;

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

    // Campi di stato interni
    private IInteractable currentInteractable;
    private Collider currentCollider;
    private bool isCardOpen = false;

    private void Update()
    {
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
        // Se la scheda di dettaglio è aperta, blocchiamo il rilevamento per mantenere il focus sull'oggetto corrente
        if (isCardOpen) return;

        // Layer mask flessibile: se non impostata o non include Default/Interactable, includi tutto ciò che è rilevante
        int mask = interactableLayer.value;
        if (mask == 0)
        {
            mask = ~0; // Everything
        }

        // Rilevamento dei collisori all'interno del raggio d'azione
        Collider[] colliders = Physics.OverlapSphere(transform.position, Mathf.Max(interactionRadius, 3.5f), mask);
        
        if (colliders.Length > 0)
        {
            // Identificazione del collisore geometricamente più vicino alla superficie del Player
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

        // Reset dello stato in caso di assenza di collisori interagibili nel volume di scansione
        ResetTargetState();
    }

    /// <summary>
    /// Gestisce la ricezione dell'input e l'esecuzione sequenziale dell'interazione o dell'apertura dell'interfaccia.
    /// </summary>
    private void HandleInteractionInput()
    {
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            // Caso 1: La scheda dettagli è già aperta -> La chiudiamo e resettiamo lo stato
            if (isCardOpen)
            {
                CloseDetailCard();
                return;
            }

            // Caso 2: Premuto E in prossimità di un oggetto valido
            if (currentInteractable != null)
            {
                TogglePromptUI(false); // Nasconde il prompt di notifica iniziale

                if (interactionDetailCard != null)
                {
                    // Apertura della scheda di interazione strutturata
                    OpenDetailCard();
                }
                else
                {
                    // FALLBACK DEBUG: Esecuzione diretta dell'acquisizione/interazione se manca la scheda UI
                    ExecuteDirectInteraction();
                }
            }
        }
    }

    private Collider GetClosestCollider(Collider[] colliders)
    {
        Collider bestTarget = null;
        float closestDistanceSqr = Mathf.Infinity;
        Vector3 playerPos = transform.position;

        foreach (Collider potentialTarget in colliders)
        {
            if (potentialTarget == null) continue;

            // Ignora il collider del Player stesso
            if (potentialTarget.transform.root == transform.root) continue;

            // Verifica che l'oggetto o i suoi parent/figli implementino IInteractable
            IInteractable candidate = potentialTarget.GetComponent<IInteractable>() 
                                   ?? potentialTarget.GetComponentInParent<IInteractable>()
                                   ?? potentialTarget.GetComponentInChildren<IInteractable>();
            if (candidate == null) continue;

            // Calcolo della distanza reale dalla superficie del collider (ClosestPoint) invece del pivot centrale
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
