// ============================================================================
// Crisis Protocol / Sector Containment - UI e feedback AsyncronQuest
// File: .\Assets\AsyncronQuest\Tooltips\Scripts\WorldTimeTooltipSequence.cs
// Responsabilita': fornisce schermate, tooltip, transizioni, menu e feedback visivi integrati nel progetto Crisis Protocol.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using System; // usa lib // riga-ok
using System.Collections; // usa lib // riga-ok
using UnityEngine; // usa lib // riga-ok

namespace AsyncronQuest.Tooltips // zona cod // riga-ok
{ // apre // riga-ok
    // blocco: classe x roba grossa
    public sealed class WorldTimeTooltipSequence : MonoBehaviour // classe qui // riga-ok
    { // apre // riga-ok
        [SerializeField] private bool playOnStart = true; // setta // riga-ok
        [SerializeField] private bool playOnlyOnce = true; // setta // riga-ok
        [SerializeField] private string sequenceId = "default_worldtime"; // setta // riga-ok
        [SerializeField] private TooltipStep[] steps = // setta // riga-ok
        { // apre // riga-ok
            new TooltipStep("tooltip.movement", 0.8f, 5f, true), // ok qua // riga-ok
            new TooltipStep("tooltip.pause_menu", 6.2f, 5f, true), // ok qua // riga-ok
            new TooltipStep("tooltip.interaction", 11.6f, 5f, true) // chiama // riga-ok
        }; // ok qua // riga-ok

        private Coroutine routine; // roba pub // riga-ok

        // blocco: funzione fa cose
        private void Start() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (playOnStart) // se ok // riga-ok
                Play(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public void Play() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (playOnlyOnce && PlayerPrefs.GetInt(GetPlayerPrefsKey(), 0) == 1) // se ok // riga-ok
                return; // torna val // riga-ok

            // blocco: controlla se va
            if (routine != null) // se ok // riga-ok
                StopCoroutine(routine); // corutina // riga-ok

            routine = StartCoroutine(PlayRoutine()); // setta // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public void ResetSequenceSave() // roba pub // riga-ok
        { // apre // riga-ok
            PlayerPrefs.DeleteKey(GetPlayerPrefsKey()); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private IEnumerator PlayRoutine() // roba pub // riga-ok
        { // apre // riga-ok
            float elapsed = 0f; // setta // riga-ok

            // blocco: gira piu volte
            foreach (TooltipStep step in steps) // ciclo x // riga-ok
            { // apre // riga-ok
                float wait = Mathf.Max(0f, step.delayFromSequenceStart - elapsed); // setta // riga-ok
                // blocco: controlla se va
                if (wait > 0f) // se ok // riga-ok
                { // apre // riga-ok
                    yield return new WaitForSecondsRealtime(wait); // aspetta // riga-ok
                    elapsed += wait; // setta // riga-ok
                } // chiude // riga-ok

                TooltipManager.Show(step.tooltipKey, step.duration, step.readAloud); // chiama // riga-ok
            } // chiude // riga-ok

            // blocco: controlla se va
            if (playOnlyOnce) // se ok // riga-ok
            { // apre // riga-ok
                PlayerPrefs.SetInt(GetPlayerPrefsKey(), 1); // chiama // riga-ok
                PlayerPrefs.Save(); // chiama // riga-ok
            } // chiude // riga-ok

            routine = null; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private string GetPlayerPrefsKey() // roba pub // riga-ok
        { // apre // riga-ok
            return "progetto-precedente.TooltipSequence." + sequenceId; // torna val // riga-ok
        } // chiude // riga-ok

        [Serializable] // nota unity // riga-ok
        // blocco: classe x roba grossa
        private sealed class TooltipStep // classe qui // riga-ok
        { // apre // riga-ok
            public string tooltipKey; // roba pub // riga-ok
            public float delayFromSequenceStart; // roba pub // riga-ok
            public float duration; // roba pub // riga-ok
            public bool readAloud; // roba pub // riga-ok

            // blocco: funzione fa cose
            public TooltipStep(string tooltipKey, float delayFromSequenceStart, float duration, bool readAloud) // roba pub // riga-ok
            { // apre // riga-ok
                this.tooltipKey = tooltipKey; // setta // riga-ok
                this.delayFromSequenceStart = delayFromSequenceStart; // setta // riga-ok
                this.duration = duration; // setta // riga-ok
                this.readAloud = readAloud; // setta // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
