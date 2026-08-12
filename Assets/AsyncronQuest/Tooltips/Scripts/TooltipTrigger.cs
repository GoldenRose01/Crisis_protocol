using UnityEngine;

namespace AsyncronQuest.Tooltips
{
    [DisallowMultipleComponent]
    public sealed class TooltipTrigger : MonoBehaviour, IInteractable
    {
        public enum TriggerMode
        {
            Proximity,
            Interaction,
            ProximityAndInteraction
        }

        [Header("Tooltip")]
        [SerializeField] private string tooltipKey = "tooltip.interact";
        [SerializeField] private TriggerMode triggerMode = TriggerMode.Proximity;
        [SerializeField, Min(0.1f)] private float duration = 5f;
        [SerializeField] private bool readAloud = true;
        [SerializeField] private bool showOnce = true;
        [SerializeField, Min(0f)] private float cooldown = 1.5f;

        [Header("Trigger")]
        [SerializeField] private string playerTag = "Player";

        private bool hasShown;
        private float lastShownAt = -999f;

        private void OnTriggerEnter(Collider other)
        {
            if (!CanShowFromProximity(other))
                return;

            Show();
        }

        public void Interact()
        {
            if (triggerMode == TriggerMode.Proximity)
                return;

            Show();
        }

        public void ResetTooltipState()
        {
            hasShown = false;
            lastShownAt = -999f;
        }

        private bool CanShowFromProximity(Collider other)
        {
            if (triggerMode == TriggerMode.Interaction)
                return false;

            if (!other.CompareTag(playerTag))
                return false;

            return true;
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
