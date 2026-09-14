// ============================================================================
// Crisis Protocol / Sector Containment - Interfaccia utente
// File: .\Assets\CrisisProtocol\Scripts\UI\AnalogGaugeUI.cs
// Responsabilita': aggiorna HUD, menu, overlay, gauge, notifiche o schermate di supporto in base agli eventi gameplay.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine; // usa lib // riga-ok

namespace CrisisProtocol.UI // zona cod // riga-ok
{ // apre // riga-ok
    // blocco: classe x roba grossa
    public class AnalogGaugeUI : MonoBehaviour // classe qui // riga-ok
    { // apre // riga-ok
        [SerializeField] private RectTransform needle; // ok qua // riga-ok
        [SerializeField, Range(0f, 1f)] private float normalizedValue = 0.75f; // setta // riga-ok
        [SerializeField] private float minAngle = 130f; // setta // riga-ok
        [SerializeField] private float maxAngle = -130f; // setta // riga-ok
        [SerializeField] private float smoothing = 8f; // setta // riga-ok

        private float currentValue; // roba pub // riga-ok

        // blocco: funzione fa cose
        private void Update() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (!needle) return; // se ok // riga-ok
            currentValue = Mathf.Lerp(currentValue, normalizedValue, Time.unscaledDeltaTime * smoothing); // setta // riga-ok
            float z = Mathf.Lerp(minAngle, maxAngle, currentValue); // setta // riga-ok
            needle.localRotation = Quaternion.Euler(0f, 0f, z); // setta // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public void SetValue01(float value) // roba pub // riga-ok
        { // apre // riga-ok
            normalizedValue = Mathf.Clamp01(value); // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
