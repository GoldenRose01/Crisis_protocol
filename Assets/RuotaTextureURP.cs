// ============================================================================
// Crisis Protocol / Sector Containment - Script gameplay custom
// File: .\Assets\RuotaTextureURP.cs
// Responsabilita': contiene logica specifica del prototipo e viene eseguito da Unity tramite componenti, eventi o utility editor.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine; // usa lib // riga-ok

[RequireComponent(typeof(MeshRenderer))] // nota unity // riga-ok
// blocco: classe x roba grossa
public class RuotaTextureURP : MonoBehaviour // classe qui // riga-ok
{ // apre // riga-ok
    [Header("Configurazione Rotazione UV")] // nota unity // riga-ok
    [Tooltip("Gradi di rotazione della texture (es. 90 o -90)")] // nota unity // riga-ok
    [SerializeField] private float gradiRotazione = 90f; // setta // riga-ok

    void Start() // chiama // riga-ok
    { // apre // riga-ok
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>(); // setta // riga-ok
        
        // blocco: controlla se va
        if (meshRenderer != null) // se ok // riga-ok
        { // apre // riga-ok
            // Crea un'istanza locale unica del materiale per isolare la parete
            Material mat = meshRenderer.material; // setta // riga-ok

            // Conversione dell'angolo in radianti
            float rad = gradiRotazione * Mathf.Deg2Rad; // setta // riga-ok
            float sin = Mathf.Sin(rad); // setta // riga-ok
            float cos = Mathf.Cos(rad); // setta // riga-ok

            // Costruzione della matrice di rotazione 2D affine per coordinate UV
            // Spostiamo il pivot al centro (0.5, 0.5) per evitare traslazioni d'angolo
            Matrix4x4 matriceRotazione = Matrix4x4.identity; // setta // riga-ok
            
            matriceRotazione.m00 = cos;   matriceRotazione.m01 = -sin;  matriceRotazione.m03 = 0.5f * (1f - cos + sin); // setta // riga-ok
            matriceRotazione.m10 = sin;   matriceRotazione.m11 = cos;   matriceRotazione.m13 = 0.5f * (1f - sin - cos); // setta // riga-ok

            // Assegnazione della matrice di trasformazione allo shader standard URP
            // Questa proprietà agisce direttamente sulla pipeline di campionamento delle texture
            mat.SetMatrix("_MainTex_Transform", matriceRotazione);  // chiama // riga-ok
            
            // Fallback per Shader Graph custom se implementano parametri esposti
            // blocco: controlla se va
            if (mat.HasProperty("_Rotation")) // se ok // riga-ok
            { // apre // riga-ok
                mat.SetFloat("_Rotation", gradiRotazione); // chiama // riga-ok
            } // chiude // riga-ok

            Debug.Log($"<color=lime>[UV ROTATE]</color> Trasformazione geometrica applicata a {gameObject.name} ({gradiRotazione}°)."); // logga // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
