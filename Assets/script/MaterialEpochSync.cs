using UnityEngine;
using System; 

namespace GoldenCast.Legacy
{
    public class MaterialEpochSync : MonoBehaviour
    {
        private MeshRenderer meshRenderer;

        [Header("Configurazione Epoche")]
        [Tooltip("Indice 0 = Passato, 1 = Anni 60, 2 = Anni 2000, 3 = Futuro")]
        public Material[] materialiEpoca;

        void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }

        void OnEnable()
        {
            GlobalEnvironmentManager.OnCambioEpoca += CambiaMateriale;
        }

        void OnDisable()
        {
            GlobalEnvironmentManager.OnCambioEpoca -= CambiaMateriale;
        }

        // Allineamento forzato all'avvio della scena (Post-Respawn)
        void Start()
        {
            // VERIFICA DI SICUREZZA: Controlliamo se l'istanza del manager esiste
            if (GlobalEnvironmentManager.Instance != null)
            {
                // CORREZIONE ERRORE CS0120: Accesso tramite .Instance
                int epocaAttualeAlAvvio = GlobalEnvironmentManager.Instance.EpocaCorrente;
                
                // Forziamo l'aggiornamento del materiale senza attendere un nuovo salto temporale
                CambiaMateriale(epocaAttualeAlAvvio);
            }
            else
            {
                Debug.LogWarning($"[SINC MAT] {gameObject.name} non ha trovato GlobalEnvironmentManager.Instance allo Start. Il materiale verrà allineato al primo evento utile.");
            }
        }

        public void CambiaMateriale(int indiceEpoca)
        {
            // Controllo di sicurezza: l'indice esiste e il renderer c'è?
            if (meshRenderer != null && indiceEpoca >= 0 && indiceEpoca < materialiEpoca.Length)
            {
                if (materialiEpoca[indiceEpoca] != null)
                {
                    meshRenderer.material = materialiEpoca[indiceEpoca];
                    Debug.Log($"[SINC MAT] {gameObject.name} aggiornato al materiale: {materialiEpoca[indiceEpoca].name}");
                }
                else
                {
                    Debug.LogError($"[SINC MAT] Attenzione! Hai lasciato vuoto lo slot {indiceEpoca} sull'oggetto {gameObject.name}");
                }
            }
        }
    }
}