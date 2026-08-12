using UnityEngine;

namespace AsyncronQuest.Tooltips
{
    [DisallowMultipleComponent]
    public sealed class CausalWallTooltipTrigger : MonoBehaviour
    {
        [Header("Tooltip")]
        [SerializeField] private string tooltipKey = "tooltip.causal_wall";
        [SerializeField, Min(0.1f)] private float duration = 6f;
        [SerializeField] private bool readAloud = true;
        [SerializeField] private bool showOnce = true;
        [SerializeField, Min(0f)] private float cooldown = 1.5f;

        [Header("Trigger")]
        [SerializeField] private string playerTag = "Player";

        private bool hasShown;
        private float lastShownAt = -999f;

        private void Reset()
        {
            Collider trigger = GetComponent<Collider>();
            if (trigger)
                trigger.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag))
                return;

            Show();
        }

        public void ResetTooltipState()
        {
            hasShown = false;
            lastShownAt = -999f;
        }

        private void Show()
        {
            if (showOnce && hasShown)
                return;

            if (Time.unscaledTime - lastShownAt < cooldown)
                return;

            hasShown = true;
            lastShownAt = Time.unscaledTime;
            TooltipManager.Show(tooltipKey, duration, readAloud);
        }
    }
}
