using System;
using System.Reflection;
using UnityEngine;

namespace AsyncronQuest.Tooltips
{
    public static class TooltipSpeechSynthesizer
    {
        private const int SpeechAsyncAndPurge = 3;
        private static object voice;
        private static Type voiceType;
        private static string configuredVoiceNameContains = string.Empty;
        private static int configuredVolume = 100;
        private static int configuredRate;

        public static bool IsAvailable
        {
            get
            {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                return EnsureVoice();
#else
                return false;
#endif
            }
        }

        public static void Speak(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (!EnsureVoice())
                return;

            try
            {
                voiceType.InvokeMember("Speak", BindingFlags.InvokeMethod, null, voice, new object[] { text, SpeechAsyncAndPurge });
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Tooltip speech synthesis failed: " + exception.Message);
            }
#endif
        }

        public static void Configure(string voiceNameContains, int volume, int rate)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            configuredVoiceNameContains = voiceNameContains ?? string.Empty;
            configuredVolume = Mathf.Clamp(volume, 0, 100);
            configuredRate = Mathf.Clamp(rate, -10, 10);

            if (!EnsureVoice())
                return;

            try
            {
                voiceType.InvokeMember("Volume", BindingFlags.SetProperty, null, voice, new object[] { configuredVolume });
                voiceType.InvokeMember("Rate", BindingFlags.SetProperty, null, voice, new object[] { configuredRate });
                TrySelectVoice(configuredVoiceNameContains);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Tooltip speech configuration failed: " + exception.Message);
            }
#endif
        }

        public static void Stop()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (!EnsureVoice())
                return;

            try
            {
                voiceType.InvokeMember("Speak", BindingFlags.InvokeMethod, null, voice, new object[] { string.Empty, SpeechAsyncAndPurge });
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Tooltip speech stop failed: " + exception.Message);
            }
#endif
        }

        private static bool EnsureVoice()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (voice != null && voiceType != null)
                return true;

            try
            {
                voiceType = Type.GetTypeFromProgID("SAPI.SpVoice");
                if (voiceType == null)
                    return false;

                voice = Activator.CreateInstance(voiceType);
                Configure(configuredVoiceNameContains, configuredVolume, configuredRate);
                return voice != null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Windows speech synthesizer not available: " + exception.Message);
                voice = null;
                voiceType = null;
                return false;
            }
#else
            return false;
#endif
        }

        private static void TrySelectVoice(string voiceNameContains)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (string.IsNullOrWhiteSpace(voiceNameContains))
                return;

            object voices = voiceType.InvokeMember("GetVoices", BindingFlags.InvokeMethod, null, voice, null);
            if (voices == null)
                return;

            Type voicesType = voices.GetType();
            int count = Convert.ToInt32(voicesType.InvokeMember("Count", BindingFlags.GetProperty, null, voices, null));

            for (int i = 0; i < count; i++)
            {
                object token = voicesType.InvokeMember("Item", BindingFlags.InvokeMethod, null, voices, new object[] { i });
                if (token == null)
                    continue;

                string description = Convert.ToString(token.GetType().InvokeMember("GetDescription", BindingFlags.InvokeMethod, null, token, new object[] { 0 }));
                if (description.IndexOf(voiceNameContains, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                voiceType.InvokeMember("Voice", BindingFlags.SetProperty, null, voice, new[] { token });
                return;
            }

            Debug.LogWarning("Tooltip speech voice containing '" + voiceNameContains + "' was not found.");
#endif
        }
    }
}
