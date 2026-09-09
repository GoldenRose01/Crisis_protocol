using UnityEngine;
using UnityEngine.UI;

namespace GoldenCast.UI
{
    public class RadialCooldownUI : MonoBehaviour
    {
        [SerializeField] private Image radialFill;
        [SerializeField] private float cooldownSeconds = 12.4f;
        [SerializeField] private bool loop = true;

        private float timer;

        private void OnEnable()
        {
            timer = cooldownSeconds;
        }

        private void Update()
        {
            if (!radialFill || cooldownSeconds <= 0f) return;

            timer -= Time.unscaledDeltaTime;
            if (timer <= 0f)
                timer = loop ? cooldownSeconds : 0f;

            radialFill.fillAmount = Mathf.Clamp01(timer / cooldownSeconds);
        }

        public void Restart(float seconds)
        {
            cooldownSeconds = Mathf.Max(0.01f, seconds);
            timer = cooldownSeconds;
        }
    }
}
