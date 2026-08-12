using UnityEngine;
using UnityEngine.UI;

namespace AsyncronQuest.SteampunkUI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RawImage))]
    public sealed class SceneTopDownMapUI : MonoBehaviour
    {
        [SerializeField] private Transform followTarget;
        [SerializeField] private string playerTag = "Player";
        [SerializeField, Min(10f)] private float cameraHeight = 120f;
        [SerializeField, Min(5f)] private float orthographicSize = 48f;
        [SerializeField, Min(64)] private int textureSize = 512;
        [SerializeField] private LayerMask cullingMask = ~0;
        [SerializeField] private Color backgroundColor = new Color(0.015f, 0.045f, 0.025f, 1f);

        private RawImage rawImage;
        private Camera mapCamera;
        private RenderTexture renderTexture;

        private void Awake()
        {
            rawImage = GetComponent<RawImage>();
            CreateCamera();
            ResolveFollowTarget();
        }

        private void OnEnable()
        {
            if (mapCamera)
                mapCamera.enabled = true;
        }

        private void OnDisable()
        {
            if (mapCamera)
                mapCamera.enabled = false;
        }

        private void OnDestroy()
        {
            if (mapCamera)
                Destroy(mapCamera.gameObject);

            if (renderTexture)
                renderTexture.Release();
        }

        private void LateUpdate()
        {
            if (!followTarget)
                ResolveFollowTarget();

            if (!mapCamera)
                return;

            Vector3 center = followTarget ? followTarget.position : Vector3.zero;
            mapCamera.transform.position = new Vector3(center.x, center.y + cameraHeight, center.z);
            mapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            mapCamera.orthographicSize = orthographicSize;
            mapCamera.cullingMask = cullingMask;
        }

        public void Configure(Transform target, float height, float size, int renderTextureSize)
        {
            followTarget = target;
            cameraHeight = Mathf.Max(10f, height);
            orthographicSize = Mathf.Max(5f, size);
            textureSize = Mathf.Max(64, renderTextureSize);

            if (!rawImage)
                rawImage = GetComponent<RawImage>();

            CreateCamera();
        }

        private void CreateCamera()
        {
            if (renderTexture && renderTexture.width == textureSize && renderTexture.height == textureSize)
                return;

            if (renderTexture)
                renderTexture.Release();

            renderTexture = new RenderTexture(textureSize, textureSize, 16, RenderTextureFormat.ARGB32)
            {
                name = "Scene Top Down Map Texture"
            };
            renderTexture.Create();

            if (!mapCamera)
            {
                GameObject cameraObject = new GameObject("Scene Top Down Map Camera");
                cameraObject.hideFlags = HideFlags.DontSave;
                mapCamera = cameraObject.AddComponent<Camera>();
                mapCamera.transform.SetParent(transform, false);
            }

            mapCamera.clearFlags = CameraClearFlags.SolidColor;
            mapCamera.backgroundColor = backgroundColor;
            mapCamera.orthographic = true;
            mapCamera.nearClipPlane = 0.1f;
            mapCamera.farClipPlane = cameraHeight + 300f;
            mapCamera.depth = -100f;
            mapCamera.targetTexture = renderTexture;
            mapCamera.enabled = isActiveAndEnabled;

            rawImage.texture = renderTexture;
        }

        private void ResolveFollowTarget()
        {
            GameObject player = null;
            try
            {
                player = GameObject.FindGameObjectWithTag(playerTag);
            }
            catch (UnityException)
            {
                player = null;
            }

            if (player)
            {
                followTarget = player.transform;
                return;
            }

            if (Camera.main)
                followTarget = Camera.main.transform;
        }
    }
}
