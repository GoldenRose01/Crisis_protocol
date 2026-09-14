// ============================================================================
// Crisis Protocol / Sector Containment - UI e feedback AsyncronQuest
// File: .\Assets\AsyncronQuest\Tooltips\Scripts\TooltipSpeechSynthesizer.cs
// Responsabilita': fornisce schermate, tooltip, transizioni, menu e feedback visivi integrati nel progetto Crisis Protocol.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using System; // usa lib // riga-ok
using System.Reflection; // usa lib // riga-ok
using UnityEngine; // usa lib // riga-ok

namespace AsyncronQuest.Tooltips // zona cod // riga-ok
{ // apre // riga-ok
    public static class TooltipSpeechSynthesizer // roba pub // riga-ok
    { // apre // riga-ok
        private const int SpeechAsyncAndPurge = 3; // roba pub // riga-ok
        private static object voice; // roba pub // riga-ok
        private static Type voiceType; // roba pub // riga-ok
        private static string configuredVoiceNameContains = string.Empty; // roba pub // riga-ok
        private static int configuredVolume = 100; // roba pub // riga-ok
        private static int configuredRate; // roba pub // riga-ok

        public static bool IsAvailable // roba pub // riga-ok
        { // apre // riga-ok
            get // ok qua // riga-ok
            { // apre // riga-ok
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN // prep ok // riga-ok
                return EnsureVoice(); // torna val // riga-ok
#else // prep ok // riga-ok
                return false; // torna val // riga-ok
#endif // prep ok // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public static void Speak(string text) // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (string.IsNullOrWhiteSpace(text)) // se ok // riga-ok
                return; // torna val // riga-ok

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN // prep ok // riga-ok
            // blocco: controlla se va
            if (!EnsureVoice()) // se ok // riga-ok
                return; // torna val // riga-ok

            // blocco: prova safe
            try // prova // riga-ok
            { // apre // riga-ok
                voiceType.InvokeMember("Speak", BindingFlags.InvokeMethod, null, voice, new object[] { text, SpeechAsyncAndPurge }); // chiama // riga-ok
            } // chiude // riga-ok
            // blocco: becca errore
            catch (Exception exception) // err qui // riga-ok
            { // apre // riga-ok
                Debug.LogWarning("Tooltip speech synthesis failed: " + exception.Message); // logga // riga-ok
            } // chiude // riga-ok
#endif // prep ok // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public static void Configure(string voiceNameContains, int volume, int rate) // roba pub // riga-ok
        { // apre // riga-ok
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN // prep ok // riga-ok
            configuredVoiceNameContains = voiceNameContains ?? string.Empty; // setta // riga-ok
            configuredVolume = Mathf.Clamp(volume, 0, 100); // setta // riga-ok
            configuredRate = Mathf.Clamp(rate, -10, 10); // setta // riga-ok

            // blocco: controlla se va
            if (!EnsureVoice()) // se ok // riga-ok
                return; // torna val // riga-ok

            // blocco: prova safe
            try // prova // riga-ok
            { // apre // riga-ok
                voiceType.InvokeMember("Volume", BindingFlags.SetProperty, null, voice, new object[] { configuredVolume }); // chiama // riga-ok
                voiceType.InvokeMember("Rate", BindingFlags.SetProperty, null, voice, new object[] { configuredRate }); // chiama // riga-ok
                TrySelectVoice(configuredVoiceNameContains); // chiama // riga-ok
            } // chiude // riga-ok
            // blocco: becca errore
            catch (Exception exception) // err qui // riga-ok
            { // apre // riga-ok
                Debug.LogWarning("Tooltip speech configuration failed: " + exception.Message); // logga // riga-ok
            } // chiude // riga-ok
#endif // prep ok // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public static void Stop() // roba pub // riga-ok
        { // apre // riga-ok
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN // prep ok // riga-ok
            // blocco: controlla se va
            if (!EnsureVoice()) // se ok // riga-ok
                return; // torna val // riga-ok

            // blocco: prova safe
            try // prova // riga-ok
            { // apre // riga-ok
                voiceType.InvokeMember("Speak", BindingFlags.InvokeMethod, null, voice, new object[] { string.Empty, SpeechAsyncAndPurge }); // chiama // riga-ok
            } // chiude // riga-ok
            // blocco: becca errore
            catch (Exception exception) // err qui // riga-ok
            { // apre // riga-ok
                Debug.LogWarning("Tooltip speech stop failed: " + exception.Message); // logga // riga-ok
            } // chiude // riga-ok
#endif // prep ok // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private static bool EnsureVoice() // roba pub // riga-ok
        { // apre // riga-ok
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN // prep ok // riga-ok
            // blocco: controlla se va
            if (voice != null && voiceType != null) // se ok // riga-ok
                return true; // torna val // riga-ok

            // blocco: prova safe
            try // prova // riga-ok
            { // apre // riga-ok
                voiceType = Type.GetTypeFromProgID("SAPI.SpVoice"); // setta // riga-ok
                // blocco: controlla se va
                if (voiceType == null) // se ok // riga-ok
                    return false; // torna val // riga-ok

                voice = Activator.CreateInstance(voiceType); // setta // riga-ok
                Configure(configuredVoiceNameContains, configuredVolume, configuredRate); // chiama // riga-ok
                return voice != null; // torna val // riga-ok
            } // chiude // riga-ok
            // blocco: becca errore
            catch (Exception exception) // err qui // riga-ok
            { // apre // riga-ok
                Debug.LogWarning("Windows speech synthesizer not available: " + exception.Message); // logga // riga-ok
                voice = null; // setta // riga-ok
                voiceType = null; // setta // riga-ok
                return false; // torna val // riga-ok
            } // chiude // riga-ok
#else // prep ok // riga-ok
            return false; // torna val // riga-ok
#endif // prep ok // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private static void TrySelectVoice(string voiceNameContains) // roba pub // riga-ok
        { // apre // riga-ok
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN // prep ok // riga-ok
            // blocco: controlla se va
            if (string.IsNullOrWhiteSpace(voiceNameContains)) // se ok // riga-ok
                return; // torna val // riga-ok

            object voices = voiceType.InvokeMember("GetVoices", BindingFlags.InvokeMethod, null, voice, null); // setta // riga-ok
            // blocco: controlla se va
            if (voices == null) // se ok // riga-ok
                return; // torna val // riga-ok

            Type voicesType = voices.GetType(); // setta // riga-ok
            int count = Convert.ToInt32(voicesType.InvokeMember("Count", BindingFlags.GetProperty, null, voices, null)); // setta // riga-ok

            // blocco: gira piu volte
            for (int i = 0; i < count; i++) // ciclo x // riga-ok
            { // apre // riga-ok
                object token = voicesType.InvokeMember("Item", BindingFlags.InvokeMethod, null, voices, new object[] { i }); // setta // riga-ok
                // blocco: controlla se va
                if (token == null) // se ok // riga-ok
                    continue; // salta // riga-ok

                string description = Convert.ToString(token.GetType().InvokeMember("GetDescription", BindingFlags.InvokeMethod, null, token, new object[] { 0 })); // setta // riga-ok
                // blocco: controlla se va
                if (description.IndexOf(voiceNameContains, StringComparison.OrdinalIgnoreCase) < 0) // se ok // riga-ok
                    continue; // salta // riga-ok

                voiceType.InvokeMember("Voice", BindingFlags.SetProperty, null, voice, new[] { token }); // chiama // riga-ok
                return; // torna val // riga-ok
            } // chiude // riga-ok

            Debug.LogWarning("Tooltip speech voice containing '" + voiceNameContains + "' was not found."); // logga // riga-ok
#endif // prep ok // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
