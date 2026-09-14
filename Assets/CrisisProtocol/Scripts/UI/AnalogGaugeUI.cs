// ============================================================================
// Crisis Protocol / Sector Containment - Interfaccia utente
// File: .\Assets\CrisisProtocol\Scripts\UI\AnalogGaugeUI.cs
// Responsabilita': aggiorna HUD, menu, overlay, gauge, notifiche o schermate di supporto in base agli eventi gameplay.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine;

namespace CrisisProtocol.UI
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
