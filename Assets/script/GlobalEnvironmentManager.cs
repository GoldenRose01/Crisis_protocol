using UnityEngine;
using System;

namespace GoldenCast.Legacy
{
    public class GlobalEnvironmentManager : MonoBehaviour
    {
        public static GlobalEnvironmentManager Instance { get; private set; }

        [Header("Gestione Macro-Epoche")]
        public GameObject[] contenitoriEpoche;

        [SerializeField] private int epocaInizialeDelLivello = 0;

        [Header("Configurazione Debug")]
        [Tooltip("Abilita il cambio epoca rapido tramite i tasti numerici (1, 2, 3, 4...).")]
        [SerializeField] private bool abilitaDebugInput = false;

        public static Action<int> OnCambioEpoca;

        private int epocaCorrente = 0; 
        public int EpocaCorrente => epocaCorrente;

        void Awake()
        {
            Instance = this;
            epocaCorrente = epocaInizialeDelLivello;

            try
            {
                AllineaGeometria(epocaCorrente);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ERRORE CRITICO SISTEMA GLOBALE] Impossibile allineare la geometria nell'Awake: {ex.Message}");
            }
        }

        void Start()
        {
            OnCambioEpoca?.Invoke(epocaCorrente);
        }

        void Update()
        {
            // Esegue il polling dell'input di debug solo se la feature è esplicitamente abilitata nell'Inspector
            if (abilitaDebugInput)
            {
                GestisciDebugInputEpoche();
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                OnCambioEpoca = null; 
            }
        }

        /// <summary>
        /// Intercetta la pressione dei tasti numerici da 1 a 9 per forzare il salto temporale.
        /// </summary>
        private void GestisciDebugInputEpoche()
        {
            // Iteriamo sulle potenziali epoche indicizzabili (limitandoci alla tastiera standard da 1 a 9)
            int limiteEpocheMappabili = Mathf.Min(contenitoriEpoche.Length, 9);

            for (int i = 0; i < limiteEpocheMappabili; i++)
            {
                // KeyCode.Alpha1 corrisponde al tasto '1', KeyCode.Alpha2 al '2', ecc.
                KeyCode tastoNumerico = KeyCode.Alpha1 + i;

                if (Input.GetKeyDown(tastoNumerico))
                {
                    Debug.Log($"<color=orange>[DEBUG INPUT]</color> Forzatura salto temporale all'epoca indice: {i}");
                    EseguiSaltoTemporale(i);
                    break; // Interrompe il ciclo per il frame corrente una volta rilevato l'input
                }
            }
        }

        public void EseguiSaltoTemporale(int indiceNuovaEpoca)
        {
            if (indiceNuovaEpoca < 0 || indiceNuovaEpoca >= contenitoriEpoche.Length) return;
            if (indiceNuovaEpoca == epocaCorrente) return;

            epocaCorrente = indiceNuovaEpoca;
            AllineaGeometria(epocaCorrente);
            OnCambioEpoca?.Invoke(epocaCorrente);
        }

        private void AllineaGeometria(int indiceEpoca)
        {
            if (contenitoriEpoche == null || contenitoriEpoche.Length == 0)
            {
                Debug.LogWarning("[SISTEMA GLOBALE] L'array contenitoriEpoche è vuoto o non assegnato!");
                return;
            }

            for (int i = 0; i < contenitoriEpoche.Length; i++)
            {
                if (contenitoriEpoche[i] != null)
                {
                    contenitoriEpoche[i].SetActive(i == indiceEpoca);
                }
                else
                {
                    Debug.LogWarning($"[SISTEMA GLOBALE] Il contenitore all'indice {i} è perso (Missing Reference). Riassegnalo nell'Editor.");
                }
            }
        }
    }
}