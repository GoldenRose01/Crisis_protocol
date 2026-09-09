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

        // Rilevamento dei collisori all'interno del raggio d'azione
        Collider[] colliders = Physics.OverlapSphere(transform.position, interactionRadius, interactableLayer);
        
        if (colliders.Length > 0)
        {
            // Identificazione del collisore geometricamente più vicino all'origine del Player
            Collider closestCollider = GetClosestCollider(colliders);

            // Se il target è cambiato rispetto al frame precedente, aggiorniamo i riferimenti
            if (closestCollider != currentCollider)
            {
                currentCollider = closestCollider;
                currentInteractable = closestCollider.GetComponent<IInteractable>() ?? closestCollider.GetComponentInParent<IInteractable>();

                if (currentInteractable != null)
                {
                    TogglePromptUI(true);
                    Debug.Log($"<color=cyan>[PROSSIMITÀ]</color> Target valido agganciato: <b>{currentCollider.name}</b>.");
                }
            }
        }
        else
        {
            // Reset dello stato in caso di assenza di collisori nel volume di scansione
            ResetTargetState();
        }
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
                    Debug.LogWarning("[DEBUG-FALLBACK] Scheda dettagli non assegnata. Esecuzione diretta del metodo Interact().");
                    ExecuteDirectInteraction();
                }
            }
        }
    }

    private Collider GetClosestCollider(Collider[] colliders)
    {
        Collider bestTarget = null;
        float closestDistanceSqr = Mathf.Infinity;
        Vector3 currentPosition = transform.position;

        foreach (Collider potentialTarget in colliders)
        {
            Vector3 directionToTarget = potentialTarget.transform.position - currentPosition;
            float dSqrToTarget = directionToTarget.sqrMagnitude; // Uso della distanza al quadrato per ottimizzare le performance (evita la radice quadrata)
            
            if (dSqrToTarget < closestDistanceSqr)
            {
                closestDistanceSqr = dSqrToTarget;
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
