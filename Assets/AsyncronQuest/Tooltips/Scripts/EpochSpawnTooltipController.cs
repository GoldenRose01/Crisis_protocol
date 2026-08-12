using System;
using System.Collections;
using GoldenCast.Legacy;
using UnityEngine;

namespace AsyncronQuest.Tooltips
{
    [DisallowMultipleComponent]
    public sealed class EpochSpawnTooltipController : MonoBehaviour
    {
        [Header("Configurazione Visualizzazione")]
        [SerializeField] private bool showOnEpochSpawn = true;
        [SerializeField] private bool showOncePerEpochSession;
        [SerializeField, Min(0f)] private float initialDelay = 0.8f;
        [SerializeField, Min(0.1f)] private float duration = 6f;
        [SerializeField] private bool readAloud = true;

        [Header("Dati Strutturati Epoche")]
        [SerializeField] private EpochTooltip[] epochTooltips =
        {
            new EpochTooltip(0, "tooltip.epoch.middle_ages"),
            new EpochTooltip(1, "tooltip.epoch.1860"),
            new EpochTooltip(2, "tooltip.epoch.2000"),
            new EpochTooltip(3, "tooltip.epoch.future")
        };

        private readonly bool[] shownEpochs = new bool[16];
        private Coroutine pendingRoutine;
        private int lastRequestedEpoch = -1;
        private float lastRequestedAt = -999f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntimeController()
        {
            if (FindFirstObjectByType<EpochSpawnTooltipController>())
                return;

            GameObject root = new GameObject("GoldenCast Epoch Spawn Tooltips");
            DontDestroyOnLoad(root);
            root.AddComponent<EpochSpawnTooltipController>();
        }

        private void OnEnable()
        {
            GlobalEnvironmentManager.OnCambioEpoca += HandleEpochChanged;
        }

       private void Start()
{
    // Utilizziamo il Singleton 'Instance' poiché nel codice di questo commit il membro non è statico
    if (showOnEpochSpawn && GlobalEnvironmentManager.Instance != null)
    {
        HandleEpochChanged(GlobalEnvironmentManager.Instance.EpocaCorrente);
    }
}

        private void OnDisable()
        {
            GlobalEnvironmentManager.OnCambioEpoca -= HandleEpochChanged;
        }

        private void HandleEpochChanged(int epochIndex)
        {
            if (!showOnEpochSpawn)
                return;

            if (showOncePerEpochSession && IsEpochAlreadyShown(epochIndex))
                return;

            // Controllo anti-rimbalzo temporale per evitare trigger multipli nello stesso frame
            if (lastRequestedEpoch == epochIndex && Time.unscaledTime - lastRequestedAt < 0.5f)
                return;

            string tooltipKey = GetTooltipKey(epochIndex);
            if (string.IsNullOrWhiteSpace(tooltipKey))
                return;

            lastRequestedEpoch = epochIndex;
            lastRequestedAt = Time.unscaledTime;

            if (pendingRoutine != null)
                StopCoroutine(pendingRoutine);

            pendingRoutine = StartCoroutine(ShowRoutine(epochIndex, tooltipKey));
        }

        private IEnumerator ShowRoutine(int epochIndex, string tooltipKey)
        {
            if (initialDelay > 0f)
                yield return new WaitForSecondsRealtime(initialDelay);

            MarkEpochShown(epochIndex);
            TooltipManager.Show(tooltipKey, duration, readAloud);
            pendingRoutine = null;
        }

        private string GetTooltipKey(int epochIndex)
        {
            foreach (EpochTooltip tooltip in epochTooltips)
            {
                if (tooltip.epochIndex == epochIndex)
                    return tooltip.tooltipKey;
            }

            return string.Empty;
        }

        private bool IsEpochAlreadyShown(int epochIndex)
        {
            return epochIndex >= 0 && epochIndex < shownEpochs.Length && shownEpochs[epochIndex];
        }

        private void MarkEpochShown(int epochIndex)
        {
            if (epochIndex >= 0 && epochIndex < shownEpochs.Length)
                shownEpochs[epochIndex] = true;
        }

        [Serializable]
        private sealed class EpochTooltip
        {
            public int epochIndex;
            public string tooltipKey;

            public EpochTooltip(int epochIndex, string tooltipKey)
            {
                this.epochIndex = epochIndex;
                this.tooltipKey = tooltipKey;
            }
        }
    }
}