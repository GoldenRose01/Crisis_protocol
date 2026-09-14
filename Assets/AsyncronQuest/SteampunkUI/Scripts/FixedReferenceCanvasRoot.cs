// ============================================================================
// Crisis Protocol / Sector Containment - UI e feedback AsyncronQuest
// File: .\Assets\AsyncronQuest\SteampunkUI\Scripts\FixedReferenceCanvasRoot.cs
// Responsabilita': fornisce schermate, tooltip, transizioni, menu e feedback visivi integrati nel progetto Crisis Protocol.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine; // usa lib // riga-ok

namespace AsyncronQuest.SteampunkUI // zona cod // riga-ok
{ // apre // riga-ok
    [ExecuteAlways] // nota unity // riga-ok
    [DisallowMultipleComponent] // nota unity // riga-ok
    // blocco: classe x roba grossa
    public sealed class FixedReferenceCanvasRoot : MonoBehaviour // classe qui // riga-ok
    { // apre // riga-ok
        [SerializeField] private RectTransform targetRoot; // ok qua // riga-ok
        [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f); // setta // riga-ok
        [SerializeField] private Vector4 safePadding; // ok qua // riga-ok
        [SerializeField, Min(0.01f)] private float minimumScale = 0.01f; // setta // riga-ok
        [SerializeField] private bool allowUpscale = true; // setta // riga-ok

        private RectTransform selfRect; // roba pub // riga-ok
        private Vector2 lastParentSize; // roba pub // riga-ok
        private Vector2 lastReferenceResolution; // roba pub // riga-ok
        private Vector4 lastSafePadding; // roba pub // riga-ok
        private bool lastAllowUpscale; // roba pub // riga-ok

        // blocco: funzione fa cose
        public void Configure(RectTransform target, Vector2 referenceSize, Vector4 padding, bool canUpscale) // roba pub // riga-ok
        { // apre // riga-ok
            targetRoot = target; // setta // riga-ok
            referenceResolution = SanitizeReferenceSize(referenceSize); // setta // riga-ok
            safePadding = padding; // setta // riga-ok
            allowUpscale = canUpscale; // setta // riga-ok
            ApplyLayout(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void Awake() // roba pub // riga-ok
        { // apre // riga-ok
            CacheRect(); // chiama // riga-ok
            ApplyLayout(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void OnEnable() // roba pub // riga-ok
        { // apre // riga-ok
            CacheRect(); // chiama // riga-ok
            ApplyLayout(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void OnValidate() // roba pub // riga-ok
        { // apre // riga-ok
            referenceResolution = SanitizeReferenceSize(referenceResolution); // setta // riga-ok
            minimumScale = Mathf.Max(0.01f, minimumScale); // setta // riga-ok
            ApplyLayout(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void LateUpdate() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (NeedsRefresh()) // se ok // riga-ok
                ApplyLayout(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void OnRectTransformDimensionsChange() // roba pub // riga-ok
        { // apre // riga-ok
            ApplyLayout(); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private bool NeedsRefresh() // roba pub // riga-ok
        { // apre // riga-ok
            CacheRect(); // chiama // riga-ok
            // blocco: controlla se va
            if (!selfRect) // se ok // riga-ok
                return false; // torna val // riga-ok

            Vector2 parentSize = selfRect.rect.size; // setta // riga-ok
            return parentSize != lastParentSize // torna val // riga-ok
                || referenceResolution != lastReferenceResolution // setta // riga-ok
                || safePadding != lastSafePadding // setta // riga-ok
                || allowUpscale != lastAllowUpscale; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void ApplyLayout() // roba pub // riga-ok
        { // apre // riga-ok
            CacheRect(); // chiama // riga-ok
            // blocco: controlla se va
            if (!selfRect) // se ok // riga-ok
                return; // torna val // riga-ok

            RectTransform target = targetRoot ? targetRoot : selfRect; // setta // riga-ok
            Vector2 parentSize = selfRect.rect.size; // setta // riga-ok
            Vector2 availableSize = new Vector2( // setta // riga-ok
                Mathf.Max(1f, parentSize.x - safePadding.x - safePadding.z), // ok qua // riga-ok
                Mathf.Max(1f, parentSize.y - safePadding.y - safePadding.w)); // chiama // riga-ok

            float scale = Mathf.Min(availableSize.x / referenceResolution.x, availableSize.y / referenceResolution.y); // setta // riga-ok
            // blocco: controlla se va
            if (!allowUpscale) // se ok // riga-ok
                scale = Mathf.Min(1f, scale); // setta // riga-ok
            scale = Mathf.Max(minimumScale, scale); // setta // riga-ok

            target.anchorMin = new Vector2(0.5f, 0.5f); // setta // riga-ok
            target.anchorMax = new Vector2(0.5f, 0.5f); // setta // riga-ok
            target.pivot = new Vector2(0.5f, 0.5f); // setta // riga-ok
            target.sizeDelta = referenceResolution; // setta // riga-ok
            target.anchoredPosition = new Vector2( // setta // riga-ok
                (safePadding.x - safePadding.z) * 0.5f, // ok qua // riga-ok
                (safePadding.w - safePadding.y) * 0.5f); // chiama // riga-ok
            target.localScale = new Vector3(scale, scale, 1f); // setta // riga-ok

            lastParentSize = parentSize; // setta // riga-ok
            lastReferenceResolution = referenceResolution; // setta // riga-ok
            lastSafePadding = safePadding; // setta // riga-ok
            lastAllowUpscale = allowUpscale; // setta // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private void CacheRect() // roba pub // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (!selfRect) // se ok // riga-ok
                selfRect = GetComponent<RectTransform>(); // setta // riga-ok
        } // chiude // riga-ok

        // blocco: funzione fa cose
        private static Vector2 SanitizeReferenceSize(Vector2 size) // roba pub // riga-ok
        { // apre // riga-ok
            return new Vector2(Mathf.Max(1f, size.x), Mathf.Max(1f, size.y)); // torna val // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
