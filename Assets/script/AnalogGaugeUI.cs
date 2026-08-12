using UnityEngine;

namespace GoldenCast.UI
{
    public class AnalogGaugeUI : MonoBehaviour
    {
        [SerializeField] private RectTransform needle;
        [SerializeField, Range(0f, 1f)] private float normalizedValue = 0.75f;
        [SerializeField] private float minAngle = 130f;
        [SerializeField] private float maxAngle = -130f;
        [SerializeField] private float smoothing = 8f;

        private float currentValue;

        private void Update()
        {
            if (!needle) return;
            currentValue = Mathf.Lerp(currentValue, normalizedValue, Time.unscaledDeltaTime * smoothing);
            float z = Mathf.Lerp(minAngle, maxAngle, currentValue);
            needle.localRotation = Quaternion.Euler(0f, 0f, z);
        }

        public void SetValue01(float value)
        {
            normalizedValue = Mathf.Clamp01(value);
        }
    }
}
