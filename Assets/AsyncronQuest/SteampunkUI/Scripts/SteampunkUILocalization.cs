using System;
using System.Collections.Generic;
using UnityEngine;

namespace AsyncronQuest.SteampunkUI
{
    public sealed class SteampunkUILocalization
    {
        private const string DefaultLanguageCode = "It";
        private const string ResourceFolder = "Localization/";

        private static readonly Dictionary<string, SteampunkUILocalization> Cache = new Dictionary<string, SteampunkUILocalization>();

        private readonly Dictionary<string, string> entries = new Dictionary<string, string>();
        private readonly string languageCode;

        private SteampunkUILocalization(string languageCode, LocalizationFile file)
        {
            this.languageCode = string.IsNullOrWhiteSpace(languageCode) ? DefaultLanguageCode : languageCode;

            if (file?.entries == null)
                return;

            foreach (LocalizationEntry entry in file.entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                    continue;

                entries[entry.key] = entry.value ?? string.Empty;
            }
        }

        public static SteampunkUILocalization Load(string languageCode)
        {
            languageCode = NormalizeLanguageCode(languageCode);
            if (Cache.TryGetValue(languageCode, out SteampunkUILocalization localization))
                return localization;

            localization = LoadFromResources(languageCode);
            Cache[languageCode] = localization;
            return localization;
        }

        public string Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;

            if (entries.TryGetValue(key, out string value))
                return value;

            Debug.LogWarning($"Localization key '{key}' missing in {languageCode}.json");
            return key;
        }

        public static void ClearCache()
        {
            Cache.Clear();
        }

        private static SteampunkUILocalization LoadFromResources(string languageCode)
        {
            TextAsset json = Resources.Load<TextAsset>(ResourceFolder + languageCode);
            if (!json && languageCode != DefaultLanguageCode)
            {
                Debug.LogWarning($"Localization file '{languageCode}.json' not found. Falling back to {DefaultLanguageCode}.json.");
                json = Resources.Load<TextAsset>(ResourceFolder + DefaultLanguageCode);
                languageCode = DefaultLanguageCode;
            }

            if (!json)
            {
                Debug.LogWarning($"Localization file '{DefaultLanguageCode}.json' not found in Resources/{ResourceFolder}.");
                return new SteampunkUILocalization(languageCode, null);
            }

            LocalizationFile file = JsonUtility.FromJson<LocalizationFile>(json.text);
            return new SteampunkUILocalization(languageCode, file);
        }

        private static string NormalizeLanguageCode(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
                return DefaultLanguageCode;

            languageCode = languageCode.Trim();
            if (languageCode.Length == 1)
                return languageCode.ToUpperInvariant();

            return char.ToUpperInvariant(languageCode[0]) + languageCode.Substring(1).ToLowerInvariant();
        }

        [Serializable]
        private sealed class LocalizationFile
        {
            public LocalizationEntry[] entries = Array.Empty<LocalizationEntry>();
        }

        [Serializable]
        private sealed class LocalizationEntry
        {
            public string key = string.Empty;
            public string value = string.Empty;
        }
    }
}
