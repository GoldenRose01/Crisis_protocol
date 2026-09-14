// ============================================================================
// Crisis Protocol / Sector Containment - Interfaccia utente
// File: .\Assets\CrisisProtocol\Scripts\UI\RadialCooldownUI.cs
// Responsabilita': aggiorna HUD, menu, overlay, gauge, notifiche o schermate di supporto in base agli eventi gameplay.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine; // usa lib // riga-ok
using UnityEngine.UI; // usa lib // riga-ok

namespace CrisisProtocol.UI // zona cod // riga-ok
{ // apre // riga-ok
    // blocco: classe x roba grossa
    public class RadialCooldownUI : MonoBehaviour // classe qui // riga-ok
    { // apre // riga-ok
        [SerializeField] private Image radialFill; // ok qua // riga-ok
        [SerializeField] private float cooldownSeconds = 12.4f; // setta // riga-ok
        [SerializeField] private bool loop = true; // setta // riga-ok

        private float timer; // roba pub // riga-ok

        // blocco: funzione fa cose
        private void OnEnable() // roba pub // riga-ok
        { // apre // riga-ok
            timer = cooldownSeconds; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void Update() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (!radialFill || cooldownSeconds <= 0f) return; // se ok // riga-ok

            timer -= Time.unscaledDeltaTime; // setta // riga-ok
            // blocco: controlla se va
            if (timer <= 0f) // se ok // riga-ok
                timer = loop ? cooldownSeconds : 0f; // setta // riga-ok

            radialFill.fillAmount = Mathf.Clamp01(timer / cooldownSeconds); // setta // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public void Restart(float seconds) // roba pub // riga-ok
        { // apre // riga-ok
            cooldownSeconds = Mathf.Max(0.01f, seconds); // setta // riga-ok
            timer = cooldownSeconds; // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
