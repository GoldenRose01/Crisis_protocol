// ============================================================================
// Crisis Protocol / Sector Containment - Interfaccia utente
// File: .\Assets\CrisisProtocol\Scripts\UI\ModalUIState.cs
// Responsabilita': aggiorna HUD, menu, overlay, gauge, notifiche o schermate di supporto in base agli eventi gameplay.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using System; // usa lib // riga-ok
using UnityEngine; // usa lib // riga-ok

namespace CrisisProtocol.UI // zona cod // riga-ok
{ // apre // riga-ok
    public static class ModalUIState // roba pub // riga-ok
    { // apre // riga-ok
        private static string activeOwner; // roba pub // riga-ok
        private static float previousTimeScale = 1f; // roba pub // riga-ok
        private static CursorLockMode previousLockState = CursorLockMode.None; // roba pub // riga-ok
        private static bool previousCursorVisible = true; // roba pub // riga-ok

        public static bool IsModalOpen => !string.IsNullOrEmpty(activeOwner); // roba pub // riga-ok
        public static string ActiveOwner => activeOwner; // roba pub // riga-ok

        public static event Action<string> ModalOpened; // roba pub // riga-ok
        public static event Action<string> ModalClosed; // roba pub // riga-ok

        // blocco: funzione fa cose
        public static bool TryOpen(string owner, bool pauseGameplay = true, bool unlockCursor = true) // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (string.IsNullOrWhiteSpace(owner)) // se ok // riga-ok
                return false; // torna val // riga-ok

            // blocco: controlla se va
            if (IsModalOpen && activeOwner != owner) // se ok // riga-ok
                return false; // torna val // riga-ok

            // blocco: controlla se va
            if (activeOwner == owner) // se ok // riga-ok
                return true; // torna val // riga-ok

            activeOwner = owner; // setta // riga-ok
            previousTimeScale = Mathf.Approximately(Time.timeScale, 0f) ? 1f : Time.timeScale; // setta // riga-ok
            previousLockState = Cursor.lockState; // setta // riga-ok
            previousCursorVisible = Cursor.visible; // setta // riga-ok

            // blocco: controlla se va
            if (pauseGameplay) // se ok // riga-ok
                Time.timeScale = 0f; // setta // riga-ok

            // blocco: controlla se va
            if (unlockCursor) // se ok // riga-ok
            { // apre // riga-ok
                Cursor.lockState = CursorLockMode.None; // setta // riga-ok
                Cursor.visible = true; // setta // riga-ok
            } // chiude // riga-ok

            ModalOpened?.Invoke(owner); // chiama // riga-ok
            return true; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public static bool IsOwner(string owner) // roba pub // riga-ok
        { // apre // riga-ok
            return activeOwner == owner; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public static void Close(string owner) // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (!IsOwner(owner)) // se ok // riga-ok
                return; // torna val // riga-ok

            string closedOwner = activeOwner; // setta // riga-ok
            activeOwner = null; // setta // riga-ok
            Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale; // setta // riga-ok
            Cursor.lockState = previousLockState; // setta // riga-ok
            Cursor.visible = previousCursorVisible; // setta // riga-ok
            ModalClosed?.Invoke(closedOwner); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public static void ForceCloseAll() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (!IsModalOpen) // se ok // riga-ok
                return; // torna val // riga-ok

            string closedOwner = activeOwner; // setta // riga-ok
            activeOwner = null; // setta // riga-ok
            Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale; // setta // riga-ok
            Cursor.lockState = previousLockState; // setta // riga-ok
            Cursor.visible = previousCursorVisible; // setta // riga-ok
            ModalClosed?.Invoke(closedOwner); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
