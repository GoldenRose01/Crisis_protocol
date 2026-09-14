// ============================================================================
// Crisis Protocol / Sector Containment - UI e feedback AsyncronQuest
// File: .\Assets\AsyncronQuest\SteampunkUI\Scripts\SteampunkUILocalization.cs
// Responsabilita': fornisce schermate, tooltip, transizioni, menu e feedback visivi integrati nel progetto Crisis Protocol.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using System; // usa lib // riga-ok
using System.Collections.Generic; // usa lib // riga-ok
using UnityEngine; // usa lib // riga-ok

namespace AsyncronQuest.SteampunkUI // zona cod // riga-ok
{ // apre // riga-ok
    // blocco: classe x roba grossa
    public sealed class SteampunkUILocalization // classe qui // riga-ok
    { // apre // riga-ok
        private const string DefaultLanguageCode = "It"; // roba pub // riga-ok
        private const string ResourceFolder = "Localization/"; // roba pub // riga-ok

        private static readonly Dictionary<string, SteampunkUILocalization> Cache = new Dictionary<string, SteampunkUILocalization>(); // roba pub // riga-ok

        private readonly Dictionary<string, string> entries = new Dictionary<string, string>(); // roba pub // riga-ok
        private readonly string languageCode; // roba pub // riga-ok

        // blocco: funzione fa cose
        private SteampunkUILocalization(string languageCode, LocalizationFile file) // roba pub // riga-ok
        { // apre // riga-ok
            this.languageCode = string.IsNullOrWhiteSpace(languageCode) ? DefaultLanguageCode : languageCode; // setta // riga-ok

            // blocco: controlla se va
            if (file?.entries == null) // se ok // riga-ok
                return; // torna val // riga-ok

            // blocco: gira piu volte
            foreach (LocalizationEntry entry in file.entries) // ciclo x // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (entry == null || string.IsNullOrWhiteSpace(entry.key)) // se ok // riga-ok
                    continue; // salta // riga-ok

                entries[entry.key] = entry.value ?? string.Empty; // setta // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public static SteampunkUILocalization Load(string languageCode) // roba pub // riga-ok
        { // apre // riga-ok
            languageCode = NormalizeLanguageCode(languageCode); // setta // riga-ok
            // blocco: controlla se va
            if (Cache.TryGetValue(languageCode, out SteampunkUILocalization localization)) // se ok // riga-ok
                return localization; // torna val // riga-ok

            localization = LoadFromResources(languageCode); // setta // riga-ok
            Cache[languageCode] = localization; // setta // riga-ok
            return localization; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public string Get(string key) // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (string.IsNullOrWhiteSpace(key)) // se ok // riga-ok
                return string.Empty; // torna val // riga-ok

            // blocco: controlla se va
            if (entries.TryGetValue(key, out string value)) // se ok // riga-ok
                return value; // torna val // riga-ok

            Debug.LogWarning($"Localization key '{key}' missing in {languageCode}.json"); // logga // riga-ok
            return key; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        public static void ClearCache() // roba pub // riga-ok
        { // apre // riga-ok
            Cache.Clear(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private static SteampunkUILocalization LoadFromResources(string languageCode) // roba pub // riga-ok
        { // apre // riga-ok
            TextAsset json = Resources.Load<TextAsset>(ResourceFolder + languageCode); // setta // riga-ok
            // blocco: controlla se va
            if (!json && languageCode != DefaultLanguageCode) // se ok // riga-ok
            { // apre // riga-ok
                Debug.LogWarning($"Localization file '{languageCode}.json' not found. Falling back to {DefaultLanguageCode}.json."); // logga // riga-ok
                json = Resources.Load<TextAsset>(ResourceFolder + DefaultLanguageCode); // setta // riga-ok
                languageCode = DefaultLanguageCode; // setta // riga-ok
            } // chiude // riga-ok

            // blocco: controlla se va
            if (!json) // se ok // riga-ok
            { // apre // riga-ok
                Debug.LogWarning($"Localization file '{DefaultLanguageCode}.json' not found in Resources/{ResourceFolder}."); // logga // riga-ok
                return new SteampunkUILocalization(languageCode, null); // torna val // riga-ok
            } // chiude // riga-ok

            LocalizationFile file = JsonUtility.FromJson<LocalizationFile>(json.text); // setta // riga-ok
            return new SteampunkUILocalization(languageCode, file); // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private static string NormalizeLanguageCode(string languageCode) // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (string.IsNullOrWhiteSpace(languageCode)) // se ok // riga-ok
                return DefaultLanguageCode; // torna val // riga-ok

            languageCode = languageCode.Trim(); // setta // riga-ok
            // blocco: controlla se va
            if (languageCode.Length == 1) // se ok // riga-ok
                return languageCode.ToUpperInvariant(); // torna val // riga-ok

            return char.ToUpperInvariant(languageCode[0]) + languageCode.Substring(1).ToLowerInvariant(); // torna val // riga-ok
        } // chiude // riga-ok

        [Serializable] // nota unity // riga-ok
        // blocco: classe x roba grossa
        private sealed class LocalizationFile // classe qui // riga-ok
        { // apre // riga-ok
            public LocalizationEntry[] entries = Array.Empty<LocalizationEntry>(); // roba pub // riga-ok
        } // chiude // riga-ok

        [Serializable] // nota unity // riga-ok
        // blocco: classe x roba grossa
        private sealed class LocalizationEntry // classe qui // riga-ok
        { // apre // riga-ok
            public string key = string.Empty; // roba pub // riga-ok
            public string value = string.Empty; // roba pub // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
