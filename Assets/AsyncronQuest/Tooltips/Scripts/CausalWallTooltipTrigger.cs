// ============================================================================
// Crisis Protocol / Sector Containment - UI e feedback AsyncronQuest
// File: .\Assets\AsyncronQuest\Tooltips\Scripts\CausalWallTooltipTrigger.cs
// Responsabilita': fornisce schermate, tooltip, transizioni, menu e feedback visivi integrati nel progetto Crisis Protocol.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine; // usa lib // riga-ok

namespace AsyncronQuest.Tooltips // zona cod // riga-ok
{ // apre // riga-ok
    [DisallowMultipleComponent] // nota unity // riga-ok
    // blocco: classe x roba grossa
    public sealed class CausalWallTooltipTrigger : MonoBehaviour // classe qui // riga-ok
    { // apre // riga-ok
        [Header("Tooltip")] // nota unity // riga-ok
        [SerializeField] private string tooltipKey = "tooltip.causal_wall"; // setta // riga-ok
        [SerializeField, Min(0.1f)] private float duration = 6f; // setta // riga-ok
        [SerializeField] private bool readAloud = true; // setta // riga-ok
        [SerializeField] private bool showOnce = true; // setta // riga-ok
        [SerializeField, Min(0f)] private float cooldown = 1.5f; // setta // riga-ok

        [Header("Trigger")] // nota unity // riga-ok
        [SerializeField] private string playerTag = "Player"; // setta // riga-ok

        private bool hasShown; // roba pub // riga-ok
        private float lastShownAt = -999f; // roba pub // riga-ok

        // blocco: funzione fa cose
        private void Reset() // roba pub // riga-ok
        { // apre // riga-ok
            Collider trigger = GetComponent<Collider>(); // setta // riga-ok
            // blocco: controlla se va
            if (trigger) // se ok // riga-ok
                trigger.isTrigger = true; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void OnTriggerEnter(Collider other) // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (!other.CompareTag(playerTag)) // se ok // riga-ok
                return; // torna val // riga-ok

            Show(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public void ResetTooltipState() // roba pub // riga-ok
        { // apre // riga-ok
            hasShown = false; // setta // riga-ok
            lastShownAt = -999f; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void Show() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (showOnce && hasShown) // se ok // riga-ok
                return; // torna val // riga-ok

            // blocco: controlla se va
            if (Time.unscaledTime - lastShownAt < cooldown) // se ok // riga-ok
                return; // torna val // riga-ok

            hasShown = true; // setta // riga-ok
            lastShownAt = Time.unscaledTime; // setta // riga-ok
            TooltipManager.Show(tooltipKey, duration, readAloud); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
