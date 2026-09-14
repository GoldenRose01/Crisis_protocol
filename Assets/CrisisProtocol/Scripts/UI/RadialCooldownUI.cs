// ============================================================================
// Crisis Protocol / Sector Containment - Interfaccia utente
// File: .\Assets\CrisisProtocol\Scripts\UI\RadialCooldownUI.cs
// Responsabilita': aggiorna HUD, menu, overlay, gauge, notifiche o schermate di supporto in base agli eventi gameplay.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine;
using UnityEngine.UI;

namespace CrisisProtocol.UI
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
