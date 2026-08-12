using UnityEngine;
using UnityEngine.InputSystem;

namespace AsyncronQuest.Anachronism
{
    [DisallowMultipleComponent]
    public sealed class AnachronismDebugLinks : MonoBehaviour
    {
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private Color anachronismLineColor = new Color(0f, 0.95f, 1f, 1f);
        [SerializeField] private Color leylineLineColor = new Color(1f, 0.82f, 0.12f, 1f);
        [SerializeField] private float lineWidth = 0.055f;
        [SerializeField] private float verticalOffset = 1.1f;

        private Transform player;
        private LineRenderer anachronismLine;
        private LineRenderer leylineLine;
        private bool visible;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntimeDebugLinks()
        {
            if (FindFirstObjectByType<AnachronismDebugLinks>())
                return;

            GameObject root = new GameObject("GoldenCast Anachronism Debug Links");
            DontDestroyOnLoad(root);
            root.AddComponent<AnachronismDebugLinks>();
        }

        private void Awake()
        {
            anachronismLine = CreateLine("Debug_Player_To_Anachronism", anachronismLineColor);
            leylineLine = CreateLine("Debug_Player_To_Leyline", leylineLineColor);
            SetVisible(false);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.lKey.wasPressedThisFrame)
                SetVisible(!visible);

            if (visible)
                UpdateLines();
        }

        private void SetVisible(bool value)
        {
            visible = value;
            if (anachronismLine)
                anachronismLine.enabled = value;
            if (leylineLine)
                leylineLine.enabled = value;
        }

        private void UpdateLines()
        {
            if (!player)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
                player = playerObject ? playerObject.transform : null;
            }

            if (!player)
                return;

            Transform anachronism = FindNearest<testAnacronismo>(player.position);
            Transform leyline = FindNearest<LeylineTrigger>(player.position);
            UpdateLine(anachronismLine, player, anachronism);
            UpdateLine(leylineLine, player, leyline);
        }

        private Transform FindNearest<T>(Vector3 origin) where T : Component
        {
            T[] targets = FindObjectsByType<T>(FindObjectsSortMode.None);
            Transform best = null;
            float bestDistance = float.PositiveInfinity;

            foreach (T target in targets)
            {
                float distance = (target.transform.position - origin).sqrMagnitude;
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                best = target.transform;
            }

            return best;
        }

        private void UpdateLine(LineRenderer line, Transform start, Transform end)
        {
            if (!line)
                return;

            bool canDraw = start && end;
            line.enabled = visible && canDraw;
            if (!canDraw)
                return;

            line.SetPosition(0, start.position + Vector3.up * verticalOffset);
            line.SetPosition(1, end.position + Vector3.up * verticalOffset);
        }

        private LineRenderer CreateLine(string objectName, Color color)
        {
            GameObject lineObject = new GameObject(objectName);
            lineObject.transform.SetParent(transform, false);

            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = color;
            line.endColor = color;
            return line;
        }
    }
}
