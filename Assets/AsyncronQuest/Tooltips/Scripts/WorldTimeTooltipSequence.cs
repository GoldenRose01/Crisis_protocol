using System;
using System.Collections;
using UnityEngine;

namespace AsyncronQuest.Tooltips
{
    public sealed class WorldTimeTooltipSequence : MonoBehaviour
    {
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private bool playOnlyOnce = true;
        [SerializeField] private string sequenceId = "default_worldtime";
        [SerializeField] private TooltipStep[] steps =
        {
            new TooltipStep("tooltip.movement", 0.8f, 5f, true),
            new TooltipStep("tooltip.pause_menu", 6.2f, 5f, true),
            new TooltipStep("tooltip.interaction", 11.6f, 5f, true)
        };

        private Coroutine routine;

        private void Start()
        {
            if (playOnStart)
                Play();
        }

        public void Play()
        {
            if (playOnlyOnce && PlayerPrefs.GetInt(GetPlayerPrefsKey(), 0) == 1)
                return;

            if (routine != null)
                StopCoroutine(routine);

            routine = StartCoroutine(PlayRoutine());
        }

        public void ResetSequenceSave()
        {
            PlayerPrefs.DeleteKey(GetPlayerPrefsKey());
        }

        private IEnumerator PlayRoutine()
        {
            float elapsed = 0f;

            foreach (TooltipStep step in steps)
            {
                float wait = Mathf.Max(0f, step.delayFromSequenceStart - elapsed);
                if (wait > 0f)
                {
                    yield return new WaitForSecondsRealtime(wait);
                    elapsed += wait;
                }

                TooltipManager.Show(step.tooltipKey, step.duration, step.readAloud);
            }

            if (playOnlyOnce)
            {
                PlayerPrefs.SetInt(GetPlayerPrefsKey(), 1);
                PlayerPrefs.Save();
            }

            routine = null;
        }

        private string GetPlayerPrefsKey()
        {
            return "GoldenCast.TooltipSequence." + sequenceId;
        }

        [Serializable]
        private sealed class TooltipStep
        {
            public string tooltipKey;
            public float delayFromSequenceStart;
            public float duration;
            public bool readAloud;

            public TooltipStep(string tooltipKey, float delayFromSequenceStart, float duration, bool readAloud)
            {
                this.tooltipKey = tooltipKey;
                this.delayFromSequenceStart = delayFromSequenceStart;
                this.duration = duration;
                this.readAloud = readAloud;
            }
        }
    }
}
