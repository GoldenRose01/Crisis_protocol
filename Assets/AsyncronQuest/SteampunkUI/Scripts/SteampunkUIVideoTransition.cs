using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

namespace AsyncronQuest.SteampunkUI
{
    public static class SteampunkUIVideoTransition
    {
        public static IEnumerator Play(MonoBehaviour owner, VideoClip clip, int sortingOrder, Action onComplete)
        {
            const float prepareTimeoutSeconds = 5f;
            const float playStartTimeoutSeconds = 2f;

            if (!clip)
            {
                onComplete?.Invoke();
                yield break;
            }

            GameObject root = new GameObject("Steampunk UI Video Transition", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            UnityEngine.Object.DontDestroyOnLoad(root);

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RawImage image = new GameObject("Video_Image", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            image.transform.SetParent(root.transform, false);
            Stretch(image.rectTransform);
            image.color = Color.black;

            AudioSource audioSource = root.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.ignoreListenerPause = true;

            VideoPlayer player = root.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.skipOnDrop = true;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.audioOutputMode = VideoAudioOutputMode.AudioSource;
            player.SetTargetAudioSource(0, audioSource);
            player.clip = clip;
            player.isLooping = false;
            player.waitForFirstFrame = true;

            RenderTexture renderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
            renderTexture.Create();
            player.targetTexture = renderTexture;
            image.texture = renderTexture;

            bool finished = false;
            player.loopPointReached += _ => finished = true;
            player.Prepare();

            float prepareStartedAt = Time.realtimeSinceStartup;
            while (!player.isPrepared && !WasSkipPressed() && Time.realtimeSinceStartup - prepareStartedAt < prepareTimeoutSeconds)
                yield return null;

            if (player.isPrepared && !WasSkipPressed())
                player.Play();

            bool started = false;
            float playRequestedAt = Time.realtimeSinceStartup;
            while (!finished && player && !WasSkipPressed())
            {
                if (player.isPlaying)
                    started = true;

                if (started && !player.isPlaying)
                    break;

                if (!started && Time.realtimeSinceStartup - playRequestedAt > playStartTimeoutSeconds)
                    break;

                yield return null;
            }

            player.Stop();
            renderTexture.Release();
            UnityEngine.Object.Destroy(renderTexture);
            UnityEngine.Object.Destroy(root);

            onComplete?.Invoke();
        }

        private static bool WasSkipPressed()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
