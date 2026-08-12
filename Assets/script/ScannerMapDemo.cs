using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GoldenCast.UI
{
    public class ScannerMapDemo : MonoBehaviour
    {
        [SerializeField] private RectTransform playerIcon;
        [SerializeField] private RectTransform destinationIcon;
        [SerializeField] private TMP_Text distanceLabel;
        [SerializeField] private Image phosphorPulse;
        [SerializeField] private float simulatedDistanceMeters = 500f;
        [SerializeField] private float pulseSpeed = 2f;

        private void Update()
        {
            if (distanceLabel) distanceLabel.text = Mathf.RoundToInt(simulatedDistanceMeters) + "m";

            if (playerIcon)
                playerIcon.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime) * 18f);

            if (phosphorPulse)
            {
                var c = phosphorPulse.color;
                c.a = 0.35f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * 0.15f;
                phosphorPulse.color = c;
            }
        }

        public void SetDistance(float meters)
        {
            simulatedDistanceMeters = Mathf.Max(0f, meters);
        }
    }
}
