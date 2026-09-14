// ============================================================================
// Crisis Protocol / Sector Containment - Missione e contenimento
// File: .\Assets\CrisisProtocol\Scripts\Mission\SectorEmergency\AccessCredentialPickup.cs
// Responsabilita': modella credenziali, focolai, portelloni, hazard o parametri di bilanciamento del loop emergenza -> contenimento -> estrazione.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine; // usa lib // riga-ok

[RequireComponent(typeof(Collider))] // nota unity // riga-ok
// blocco: classe x roba grossa
public class AccessCredentialPickup : MonoBehaviour, IInteractable // classe qui // riga-ok
{ // apre // riga-ok
    [Header("Dati Credenziale")] // nota unity // riga-ok
    [SerializeField] private string credentialId = "KEYCARD_A01"; // setta // riga-ok
    [SerializeField] private string displayName = "Scheda di accesso"; // setta // riga-ok
    [SerializeField] private bool applicaTagAutomatico = true; // setta // riga-ok

    [Header("Contorno Verde Neon")] // nota unity // riga-ok
    [Tooltip("Colore del contorno verde neon attorno alla keycard.")] // nota unity // riga-ok
    [SerializeField] private Color contourColor = new Color(0.15f, 1f, 0.25f, 1f); // setta // riga-ok
    [Tooltip("Se true, mostra un elegante contorno/luce verde attorno alla carta senza colorare la carta stessa.")] // nota unity // riga-ok
    [SerializeField] private bool attivaContornoVerde = true; // setta // riga-ok

    [Header("Feedback")] // nota unity // riga-ok
    [SerializeField] private bool disattivaDopoRaccolta = true; // setta // riga-ok

    [Header("Audio")] // nota unity // riga-ok
    [Tooltip("Suono di raccolta chiavi / credenziale.")] // nota unity // riga-ok
    [SerializeField] private AudioClip suonoRaccolta; // ok qua // riga-ok
    [Range(0f, 1f)] [SerializeField] private float volumeAudio = 1.0f; // setta // riga-ok

    private Light contourLight; // roba pub // riga-ok
    private bool raccolta; // roba pub // riga-ok

    public string DisplayName => !string.IsNullOrEmpty(displayName) ? displayName : credentialId; // roba pub // riga-ok
    public string CredentialId => credentialId; // roba pub // riga-ok

    // blocco: funzione fa cose
    private void Awake() // roba pub // riga-ok
    { // apre // riga-ok
        ApplicaTagUnity(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void Start() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (attivaContornoVerde) // se ok // riga-ok
        { // apre // riga-ok
            GeneraContornoVerde(); // chiama // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private Bounds CalcolaBoundsVisivi() // roba pub // riga-ok
    { // apre // riga-ok
        Renderer[] allRenderers = GetComponentsInChildren<Renderer>(); // setta // riga-ok
        bool boundsInizializzati = false; // setta // riga-ok
        Bounds totalBounds = new Bounds(transform.position, Vector3.one * 0.1f); // setta // riga-ok

        // blocco: gira piu volte
        foreach (Renderer r in allRenderers) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (r == null || r is LineRenderer || r is TrailRenderer) continue; // se ok // riga-ok
            // blocco: controlla se va
            if (!boundsInizializzati) // se ok // riga-ok
            { // apre // riga-ok
                totalBounds = r.bounds; // setta // riga-ok
                boundsInizializzati = true; // setta // riga-ok
            } // chiude // riga-ok
            // blocco: caso diverso
            else // se no // riga-ok
            { // apre // riga-ok
                totalBounds.Encapsulate(r.bounds); // chiama // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (!boundsInizializzati) // se ok // riga-ok
        { // apre // riga-ok
            Collider col = GetComponent<Collider>(); // setta // riga-ok
            // blocco: controlla se va
            if (col != null) // se ok // riga-ok
            { // apre // riga-ok
                return col.bounds; // torna val // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok

        return totalBounds; // torna val // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void GeneraContornoVerde() // roba pub // riga-ok
    { // apre // riga-ok
        Bounds visualBounds = CalcolaBoundsVisivi(); // setta // riga-ok
        Vector3 centerPos = visualBounds.center; // setta // riga-ok
        float baseY = visualBounds.min.y; // setta // riga-ok

        // 1. Luce soffusa verde per l'aura di contorno posizionata al centro esatto della card
        Transform lightTransform = transform.Find("Keycard_ContourLight"); // setta // riga-ok
        // blocco: controlla se va
        if (lightTransform == null) // se ok // riga-ok
        { // apre // riga-ok
            GameObject lightGO = new GameObject("Keycard_ContourLight"); // setta // riga-ok
            lightGO.transform.SetParent(transform, true); // chiama // riga-ok
            lightTransform = lightGO.transform; // setta // riga-ok
            contourLight = lightGO.AddComponent<Light>(); // setta // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            contourLight = lightTransform.GetComponent<Light>(); // setta // riga-ok
        } // chiude // riga-ok

        lightTransform.position = centerPos + Vector3.up * 0.04f; // setta // riga-ok
        // blocco: controlla se va
        if (contourLight != null) // se ok // riga-ok
        { // apre // riga-ok
            contourLight.type = LightType.Point; // setta // riga-ok
            contourLight.color = contourColor; // setta // riga-ok
            contourLight.range = 0.85f; // raggio compatto e focalizzato attorno alla keycard // setta // riga-ok
            contourLight.intensity = 2.0f; // setta // riga-ok
            contourLight.shadows = LightShadows.None; // setta // riga-ok
        } // chiude // riga-ok

        // 2. Crea un piccolo anello / contorno di proiezione verde attorno alla base reale della card
        Transform ringTransform = transform.Find("Keycard_OutlineRing"); // setta // riga-ok
        LineRenderer lr; // ok qua // riga-ok
        // blocco: controlla se va
        if (ringTransform == null) // se ok // riga-ok
        { // apre // riga-ok
            GameObject ringGO = new GameObject("Keycard_OutlineRing"); // setta // riga-ok
            ringGO.transform.SetParent(transform, true); // chiama // riga-ok
            ringTransform = ringGO.transform; // setta // riga-ok
            lr = ringGO.AddComponent<LineRenderer>(); // setta // riga-ok
        } // chiude // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
        { // apre // riga-ok
            lr = ringTransform.GetComponent<LineRenderer>(); // setta // riga-ok
        } // chiude // riga-ok

        ringTransform.position = new Vector3(centerPos.x, baseY + 0.005f, centerPos.z); // setta // riga-ok
        ringTransform.rotation = Quaternion.identity; // setta // riga-ok

        // blocco: controlla se va
        if (lr != null) // se ok // riga-ok
        { // apre // riga-ok
            lr.useWorldSpace = false; // setta // riga-ok
            lr.loop = true; // setta // riga-ok
            lr.positionCount = 24; // setta // riga-ok
            lr.startWidth = 0.008f; // setta // riga-ok
            lr.endWidth = 0.008f; // setta // riga-ok

            // Materiale unlit verde per il contorno
            Shader unlitShader = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default") ?? Shader.Find("Unlit/Color"); // setta // riga-ok
            // blocco: controlla se va
            if (unlitShader != null) // se ok // riga-ok
            { // apre // riga-ok
                Material lineMat = new Material(unlitShader); // setta // riga-ok
                lineMat.color = contourColor; // setta // riga-ok
                lr.material = lineMat; // setta // riga-ok
            } // chiude // riga-ok

            float rX = Mathf.Max(visualBounds.extents.x * 1.35f, 0.12f); // setta // riga-ok
            float rZ = Mathf.Max(visualBounds.extents.z * 1.35f, 0.10f); // setta // riga-ok
            // blocco: gira piu volte
            for (int i = 0; i < 24; i++) // ciclo x // riga-ok
            { // apre // riga-ok
                float angle = (i / 24f) * Mathf.PI * 2f; // setta // riga-ok
                lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * rX, 0f, Mathf.Sin(angle) * rZ)); // chiama // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnValidate() // roba pub // riga-ok
    { // apre // riga-ok
        ApplicaTagUnity(); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    public void Interact() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (raccolta) // se ok // riga-ok
            return; // torna val // riga-ok

        // blocco: controlla se va
        if (MissionManager.Instance == null) // se ok // riga-ok
        { // apre // riga-ok
            Debug.LogError($"[CREDENZIALE] MissionManager assente: impossibile registrare {credentialId}.", this); // logga // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        raccolta = MissionManager.Instance.RegistraCredenziale(credentialId); // setta // riga-ok
        // blocco: controlla se va
        if (!raccolta) // se ok // riga-ok
            return; // torna val // riga-ok

        // blocco: controlla se va
        if (GameManager.Instance != null) // se ok // riga-ok
        { // apre // riga-ok
            GameManager.Instance.RegisterSecuritySignature(credentialId); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (suonoRaccolta != null) // se ok // riga-ok
        { // apre // riga-ok
            AudioSource.PlayClipAtPoint(suonoRaccolta, transform.position, volumeAudio); // chiama // riga-ok
        } // chiude // riga-ok

        Debug.Log($"<color=cyan>[CREDENZIALE]</color> {DisplayName} acquisita: {credentialId}"); // logga // riga-ok

        // Notifica Olografica a Schermo con CyberHUD
        // blocco: controlla se va
        if (CyberHUD.Instance != null) // se ok // riga-ok
        { // apre // riga-ok
            CyberHUD.Instance.MostraNotificaAcquisizione( // ok qua // riga-ok
                $"AUTORIZZAZIONE ACQUISITA", // ok qua // riga-ok
                $"{DisplayName.ToUpper()} [{credentialId}] // ACCESSO REGISTRATO" // ok qua // riga-ok
            ); // chiama // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (disattivaDopoRaccolta) // se ok // riga-ok
            gameObject.SetActive(false); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void ApplicaTagUnity() // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (applicaTagAutomatico) // se ok // riga-ok
            SectorContainmentTags.ApplyTag(gameObject, SectorContainmentTags.AccessCredential); // chiama // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
