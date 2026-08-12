using UnityEngine;

namespace AsyncronQuest.Anachronism
{
    [DisallowMultipleComponent]
    public sealed class TimeErrorObjectResolver : MonoBehaviour, IInteractable
    {
        private const string DefaultTimeErrorTag = "TIme_error";
        private const string AlternateTimeErrorTag = "Time_error";

        [SerializeField] private string anachronismId = string.Empty;
        [SerializeField] private string requiredTravelTag = string.Empty;
        [SerializeField] private bool requireCurrentTagMatch = false;
        [SerializeField] private bool deactivateAfterResolution = false;

        private bool isResolved;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AttachToTaggedTimeErrors()
        {
            AttachToTaggedObjects(DefaultTimeErrorTag);
            AttachToTaggedObjects(AlternateTimeErrorTag);
        }

        public void Interact()
        {
            if (isResolved)
                return;

            if (GameManager.Instance == null)
            {
                Debug.LogWarning("[TIME_ERROR] GameManager assente: impossibile risolvere l'anacronismo.");
                return;
            }

            if (requireCurrentTagMatch && !string.IsNullOrWhiteSpace(requiredTravelTag) && GameManager.Instance.currentTagID != requiredTravelTag)
            {
                Debug.Log($"[TIME_ERROR] Tag corrente non valido. Richiesto: {requiredTravelTag}, corrente: {GameManager.Instance.currentTagID}");
                return;
            }

            string resolvedId = GetResolutionId();
            if (!GameManager.Instance.ResolveAnachronism(resolvedId))
                return;

            isResolved = true;
            if (deactivateAfterResolution)
                gameObject.SetActive(false);
        }

        private string GetResolutionId()
        {
            if (!string.IsNullOrWhiteSpace(anachronismId))
                return anachronismId;

            if (!string.IsNullOrWhiteSpace(requiredTravelTag))
                return requiredTravelTag + "::" + gameObject.scene.name + "::" + gameObject.name;

            return gameObject.scene.name + "::" + gameObject.name;
        }

        private static void AttachToTaggedObjects(string tagName)
        {
            GameObject[] objects;
            try
            {
                objects = GameObject.FindGameObjectsWithTag(tagName);
            }
            catch (UnityException)
            {
                return;
            }

            foreach (GameObject target in objects)
            {
                if (!target.GetComponent<TimeErrorObjectResolver>())
                    target.AddComponent<TimeErrorObjectResolver>();
            }
        }
    }
}
