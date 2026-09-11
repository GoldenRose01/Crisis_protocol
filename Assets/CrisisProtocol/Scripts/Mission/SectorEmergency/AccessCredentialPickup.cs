using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AccessCredentialPickup : MonoBehaviour, IInteractable
{
    [Header("Dati Credenziale")]
    [SerializeField] private string credentialId = "KEYCARD_A01";
    [SerializeField] private string displayName = "Scheda di accesso";
    [SerializeField] private bool applicaTagAutomatico = true;

    [Header("Contorno Verde Neon")]
    [Tooltip("Colore del contorno verde neon attorno alla keycard.")]
    [SerializeField] private Color contourColor = new Color(0.15f, 1f, 0.25f, 1f);
    [Tooltip("Se true, mostra un elegante contorno/luce verde attorno alla carta senza colorare la carta stessa.")]
    [SerializeField] private bool attivaContornoVerde = true;

    [Header("Feedback")]
    [SerializeField] private bool disattivaDopoRaccolta = true;

    private Light contourLight;
    private bool raccolta;

    public string DisplayName => !string.IsNullOrEmpty(displayName) ? displayName : credentialId;
    public string CredentialId => credentialId;

    private void Awake()
    {
        ApplicaTagUnity();
    }

    private void Start()
    {
        if (attivaContornoVerde)
        {
            GeneraContornoVerde();
        }
    }

    private Bounds CalcolaBoundsVisivi()
    {
        Renderer[] allRenderers = GetComponentsInChildren<Renderer>();
        bool boundsInizializzati = false;
        Bounds totalBounds = new Bounds(transform.position, Vector3.one * 0.1f);

        foreach (Renderer r in allRenderers)
        {
            if (r == null || r is LineRenderer || r is TrailRenderer) continue;
            if (!boundsInizializzati)
            {
                totalBounds = r.bounds;
                boundsInizializzati = true;
            }
            else
            {
                totalBounds.Encapsulate(r.bounds);
            }
        }

        if (!boundsInizializzati)
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                return col.bounds;
            }
        }

        return totalBounds;
    }

    private void GeneraContornoVerde()
    {
        Bounds visualBounds = CalcolaBoundsVisivi();
        Vector3 centerPos = visualBounds.center;
        float baseY = visualBounds.min.y;

        // 1. Luce soffusa verde per l'aura di contorno posizionata al centro esatto della card
        Transform lightTransform = transform.Find("Keycard_ContourLight");
        if (lightTransform == null)
        {
            GameObject lightGO = new GameObject("Keycard_ContourLight");
            lightGO.transform.SetParent(transform, true);
            lightTransform = lightGO.transform;
            contourLight = lightGO.AddComponent<Light>();
        }
        else
        {
            contourLight = lightTransform.GetComponent<Light>();
        }

        lightTransform.position = centerPos + Vector3.up * 0.04f;
        if (contourLight != null)
        {
            contourLight.type = LightType.Point;
            contourLight.color = contourColor;
            contourLight.range = 0.85f; // raggio compatto e focalizzato attorno alla keycard
            contourLight.intensity = 2.0f;
            contourLight.shadows = LightShadows.None;
        }

        // 2. Crea un piccolo anello / contorno di proiezione verde attorno alla base reale della card
        Transform ringTransform = transform.Find("Keycard_OutlineRing");
        LineRenderer lr;
        if (ringTransform == null)
        {
            GameObject ringGO = new GameObject("Keycard_OutlineRing");
            ringGO.transform.SetParent(transform, true);
            ringTransform = ringGO.transform;
            lr = ringGO.AddComponent<LineRenderer>();
        }
        else
        {
            lr = ringTransform.GetComponent<LineRenderer>();
        }

        ringTransform.position = new Vector3(centerPos.x, baseY + 0.005f, centerPos.z);
        ringTransform.rotation = Quaternion.identity;

        if (lr != null)
        {
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = 24;
            lr.startWidth = 0.008f;
            lr.endWidth = 0.008f;

            // Materiale unlit verde per il contorno
            Shader unlitShader = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default") ?? Shader.Find("Unlit/Color");
            if (unlitShader != null)
            {
                Material lineMat = new Material(unlitShader);
                lineMat.color = contourColor;
                lr.material = lineMat;
            }

            float rX = Mathf.Max(visualBounds.extents.x * 1.35f, 0.12f);
            float rZ = Mathf.Max(visualBounds.extents.z * 1.35f, 0.10f);
            for (int i = 0; i < 24; i++)
            {
                float angle = (i / 24f) * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * rX, 0f, Mathf.Sin(angle) * rZ));
            }
        }
    }

    private void OnValidate()
    {
        ApplicaTagUnity();
    }

    public void Interact()
    {
        if (raccolta)
            return;

        if (MissionManager.Instance == null)
        {
            Debug.LogError($"[CREDENZIALE] MissionManager assente: impossibile registrare {credentialId}.", this);
            return;
        }

        raccolta = MissionManager.Instance.RegistraCredenziale(credentialId);
        if (!raccolta)
            return;

        Debug.Log($"<color=cyan>[CREDENZIALE]</color> {DisplayName} acquisita: {credentialId}");

        // Notifica Olografica a Schermo con CyberHUD
        if (CyberHUD.Instance != null)
        {
            CyberHUD.Instance.MostraNotificaAcquisizione(
                $"AUTORIZZAZIONE ACQUISITA",
                $"{DisplayName.ToUpper()} [{credentialId}] // ACCESSO REGISTRATO"
            );
        }

        if (disattivaDopoRaccolta)
            gameObject.SetActive(false);
    }

    private void ApplicaTagUnity()
    {
        if (applicaTagAutomatico)
            SectorContainmentTags.ApplyTag(gameObject, SectorContainmentTags.AccessCredential);
    }
}
