// ============================================================================
// Crisis Protocol / Sector Containment - Script gameplay custom
// File: .\Assets\RuotaTextureURP.cs
// Responsabilita': contiene logica specifica del prototipo e viene eseguito da Unity tramite componenti, eventi o utility editor.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine;
[RequireComponent(typeof(MeshRenderer))]
public class RuotaTextureURP : MonoBehaviour
{
    [Header("Configurazione Rotazione UV")]
    [Tooltip("Gradi di rotazione della texture (es. 90 o -90)")]
    [SerializeField] private float gradiRotazione = 90f;
    void Start()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            // Crea un'istanza locale unica del materiale per isolare la parete
            Material mat = meshRenderer.material;
            // Conversione dell'angolo in radianti
            float rad = gradiRotazione * Mathf.Deg2Rad;
            float sin = Mathf.Sin(rad);
            float cos = Mathf.Cos(rad);
            // Costruzione della matrice di rotazione 2D affine per coordinate UV
            // Spostiamo il pivot al centro (0.5, 0.5) per evitare traslazioni d'angolo
            Matrix4x4 matriceRotazione = Matrix4x4.identity;
            matriceRotazione.m00 = cos;   matriceRotazione.m01 = -sin;  matriceRotazione.m03 = 0.5f * (1f - cos + sin);
            matriceRotazione.m10 = sin;   matriceRotazione.m11 = cos;   matriceRotazione.m13 = 0.5f * (1f - sin - cos);
            // Assegnazione della matrice di trasformazione allo shader standard URP
            // Questa proprietà agisce direttamente sulla pipeline di campionamento delle texture
            mat.SetMatrix("_MainTex_Transform", matriceRotazione);
            // Fallback per Shader Graph custom se implementano parametri esposti
            if (mat.HasProperty("_Rotation"))
            {
                mat.SetFloat("_Rotation", gradiRotazione);
            }
            Debug.Log($"<color=lime>[UV ROTATE]</color> Trasformazione geometrica applicata a {gameObject.name} ({gradiRotazione}°).");
        }
    }
}