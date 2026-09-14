// ============================================================================
// Crisis Protocol / Sector Containment - UI e feedback AsyncronQuest
// File: .\Assets\AsyncronQuest\SteampunkUI\Scripts\SteampunkUIVideoTransition.cs
// Responsabilita': fornisce schermate, tooltip, transizioni, menu e feedback visivi integrati nel progetto Crisis Protocol.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using System; // usa lib // riga-ok
using System.Collections; // usa lib // riga-ok
using UnityEngine; // usa lib // riga-ok
using UnityEngine.InputSystem; // usa lib // riga-ok
using UnityEngine.UI; // usa lib // riga-ok
using UnityEngine.Video; // usa lib // riga-ok

namespace AsyncronQuest.SteampunkUI // zona cod // riga-ok
{ // apre // riga-ok
    public static class SteampunkUIVideoTransition // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: funzione fa cose
        public static IEnumerator Play(MonoBehaviour owner, VideoClip clip, int sortingOrder, Action onComplete) // roba pub // riga-ok
        { // apre // riga-ok
            const float prepareTimeoutSeconds = 5f; // setta // riga-ok
            const float playStartTimeoutSeconds = 2f; // setta // riga-ok

            // blocco: controlla se va
            if (!clip) // se ok // riga-ok
            { // apre // riga-ok
                onComplete?.Invoke(); // chiama // riga-ok
                yield break; // aspetta // riga-ok
            } // chiude // riga-ok

            GameObject root = new GameObject("Steampunk UI Video Transition", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // setta // riga-ok
            UnityEngine.Object.DontDestroyOnLoad(root); // chiama // riga-ok

            Canvas canvas = root.GetComponent<Canvas>(); // setta // riga-ok
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // setta // riga-ok
            canvas.sortingOrder = sortingOrder; // setta // riga-ok

            CanvasScaler scaler = root.GetComponent<CanvasScaler>(); // setta // riga-ok
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // setta // riga-ok
            scaler.referenceResolution = new Vector2(1920f, 1080f); // setta // riga-ok
            scaler.matchWidthOrHeight = 0.5f; // setta // riga-ok

            RawImage image = new GameObject("Video_Image", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>(); // setta // riga-ok
            image.transform.SetParent(root.transform, false); // chiama // riga-ok
            Stretch(image.rectTransform); // chiama // riga-ok
            image.color = Color.black; // setta // riga-ok

            AudioSource audioSource = root.AddComponent<AudioSource>(); // setta // riga-ok
            audioSource.playOnAwake = false; // setta // riga-ok
            audioSource.spatialBlend = 0f; // setta // riga-ok
            audioSource.ignoreListenerPause = true; // setta // riga-ok

            VideoPlayer player = root.AddComponent<VideoPlayer>(); // setta // riga-ok
            player.playOnAwake = false; // setta // riga-ok
            player.skipOnDrop = true; // setta // riga-ok
            player.renderMode = VideoRenderMode.RenderTexture; // setta // riga-ok
            player.audioOutputMode = VideoAudioOutputMode.AudioSource; // setta // riga-ok
            player.SetTargetAudioSource(0, audioSource); // chiama // riga-ok
            player.clip = clip; // setta // riga-ok
            player.isLooping = false; // setta // riga-ok
            player.waitForFirstFrame = true; // setta // riga-ok

            RenderTexture renderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32); // setta // riga-ok
            renderTexture.Create(); // chiama // riga-ok
            player.targetTexture = renderTexture; // setta // riga-ok
            image.texture = renderTexture; // setta // riga-ok

            bool finished = false; // setta // riga-ok
            player.loopPointReached += _ => finished = true; // setta // riga-ok
            player.Prepare(); // chiama // riga-ok

            float prepareStartedAt = Time.realtimeSinceStartup; // setta // riga-ok
            // blocco: gira piu volte
            while (!player.isPrepared && !WasSkipPressed() && Time.realtimeSinceStartup - prepareStartedAt < prepareTimeoutSeconds) // ciclo x // riga-ok
                yield return null; // aspetta // riga-ok

            // blocco: controlla se va
            if (player.isPrepared && !WasSkipPressed()) // se ok // riga-ok
                player.Play(); // chiama // riga-ok

            bool started = false; // setta // riga-ok
            float playRequestedAt = Time.realtimeSinceStartup; // setta // riga-ok
            // blocco: gira piu volte
            while (!finished && player && !WasSkipPressed()) // ciclo x // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (player.isPlaying) // se ok // riga-ok
                    started = true; // setta // riga-ok

                // blocco: controlla se va
                if (started && !player.isPlaying) // se ok // riga-ok
                    break; // stop // riga-ok

                // blocco: controlla se va
                if (!started && Time.realtimeSinceStartup - playRequestedAt > playStartTimeoutSeconds) // se ok // riga-ok
                    break; // stop // riga-ok

                yield return null; // aspetta // riga-ok
            } // chiude // riga-ok

            player.Stop(); // chiama // riga-ok
            renderTexture.Release(); // chiama // riga-ok
            UnityEngine.Object.Destroy(renderTexture); // chiama // riga-ok
            UnityEngine.Object.Destroy(root); // chiama // riga-ok

            onComplete?.Invoke(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private static bool WasSkipPressed() // roba pub // riga-ok
        { // apre // riga-ok
            Keyboard keyboard = Keyboard.current; // setta // riga-ok
            return keyboard != null && keyboard.escapeKey.wasPressedThisFrame; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private static void Stretch(RectTransform rect) // roba pub // riga-ok
        { // apre // riga-ok
            rect.anchorMin = Vector2.zero; // setta // riga-ok
            rect.anchorMax = Vector2.one; // setta // riga-ok
            rect.pivot = new Vector2(0.5f, 0.5f); // setta // riga-ok
            rect.offsetMin = Vector2.zero; // setta // riga-ok
            rect.offsetMax = Vector2.zero; // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
