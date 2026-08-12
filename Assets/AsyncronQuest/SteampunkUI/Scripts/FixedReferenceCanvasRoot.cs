using UnityEngine;

namespace AsyncronQuest.SteampunkUI
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class FixedReferenceCanvasRoot : MonoBehaviour
    {
        [SerializeField] private RectTransform targetRoot;
        [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
        [SerializeField] private Vector4 safePadding;
        [SerializeField, Min(0.01f)] private float minimumScale = 0.01f;
        [SerializeField] private bool allowUpscale = true;

        private RectTransform selfRect;
        private Vector2 lastParentSize;
        private Vector2 lastReferenceResolution;
        private Vector4 lastSafePadding;
        private bool lastAllowUpscale;

        public void Configure(RectTransform target, Vector2 referenceSize, Vector4 padding, bool canUpscale)
        {
            targetRoot = target;
            referenceResolution = SanitizeReferenceSize(referenceSize);
            safePadding = padding;
            allowUpscale = canUpscale;
            ApplyLayout();
        }

        private void Awake()
        {
            CacheRect();
            ApplyLayout();
        }

        private void OnEnable()
        {
            CacheRect();
            ApplyLayout();
        }

        private void OnValidate()
        {
            referenceResolution = SanitizeReferenceSize(referenceResolution);
            minimumScale = Mathf.Max(0.01f, minimumScale);
            ApplyLayout();
        }

        private void LateUpdate()
        {
            if (NeedsRefresh())
                ApplyLayout();
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplyLayout();
        }

        private bool NeedsRefresh()
        {
            CacheRect();
            if (!selfRect)
                return false;

            Vector2 parentSize = selfRect.rect.size;
            return parentSize != lastParentSize
                || referenceResolution != lastReferenceResolution
                || safePadding != lastSafePadding
                || allowUpscale != lastAllowUpscale;
        }

        private void ApplyLayout()
        {
            CacheRect();
            if (!selfRect)
                return;

            RectTransform target = targetRoot ? targetRoot : selfRect;
            Vector2 parentSize = selfRect.rect.size;
            Vector2 availableSize = new Vector2(
                Mathf.Max(1f, parentSize.x - safePadding.x - safePadding.z),
                Mathf.Max(1f, parentSize.y - safePadding.y - safePadding.w));

            float scale = Mathf.Min(availableSize.x / referenceResolution.x, availableSize.y / referenceResolution.y);
            if (!allowUpscale)
                scale = Mathf.Min(1f, scale);
            scale = Mathf.Max(minimumScale, scale);

            target.anchorMin = new Vector2(0.5f, 0.5f);
            target.anchorMax = new Vector2(0.5f, 0.5f);
            target.pivot = new Vector2(0.5f, 0.5f);
            target.sizeDelta = referenceResolution;
            target.anchoredPosition = new Vector2(
                (safePadding.x - safePadding.z) * 0.5f,
                (safePadding.w - safePadding.y) * 0.5f);
            target.localScale = new Vector3(scale, scale, 1f);

            lastParentSize = parentSize;
            lastReferenceResolution = referenceResolution;
            lastSafePadding = safePadding;
            lastAllowUpscale = allowUpscale;
        }

        private void CacheRect()
        {
            if (!selfRect)
                selfRect = GetComponent<RectTransform>();
        }

        private static Vector2 SanitizeReferenceSize(Vector2 size)
        {
            return new Vector2(Mathf.Max(1f, size.x), Mathf.Max(1f, size.y));
        }
    }
}
