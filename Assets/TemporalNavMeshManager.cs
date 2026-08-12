using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation; // Necessario per NavMeshSurface
using UnityEngine.AI;      // Necessario per NavMeshData

[RequireComponent(typeof(NavMeshSurface))]
public class TemporalNavMeshManager : MonoBehaviour
{
    [System.Serializable]
    public struct EpocaNavMeshConfig
    {
        [Tooltip("Identificativo stringa o enum dell'epoca (deve corrispondere a quelli nel GameManager).")]
        public string epocheID;
        
        [Tooltip("Il file di dati NavMesh pre-calcolato (Bake) specifico per questa epoca.")]
        public NavMeshData dataDellaNavMesh; // CORRETTO: Cambiato da 'NavMesh' a 'NavMeshData'
    }

    [Header("Configurazione Epoche")]
    [SerializeField] private List<EpocaNavMeshConfig> mappaturaEpoche = new List<EpocaNavMeshConfig>();
    
    private NavMeshSurface navMeshSurface;

    private void Awake()
    {
        navMeshSurface = GetComponent<NavMeshSurface>();
    }

    /// <summary>
    /// Sostituisce i dati di navigazione attivi con quelli dell'epoca selezionata.
    /// </summary>
  public void CambiaEpocaNavMesh(string nuovaEpocaID)
{
    if (navMeshSurface == null) return;

    // Ricerca della configurazione corrispondente
    EpocaNavMeshConfig configSelezionata = mappaturaEpoche.Find(config => config.epocheID == nuovaEpocaID);

    if (configSelezionata.dataDellaNavMesh != null)
    {
        // 1. Rimuove esplicitamente i dati correnti dalla memoria globale di Unity
        navMeshSurface.RemoveData();
        navMeshSurface.enabled = false;

        // 2. Iniezione dell'asset NavMeshData corretto
        navMeshSurface.navMeshData = configSelezionata.dataDellaNavMesh;

        // 3. Riabilita e forza il build/add dei nuovi dati inseriti
        navMeshSurface.enabled = true;
        navMeshSurface.AddData(); // Forza l'aggiornamento immediato nell'Inspector

        Debug.Log($"<color=cyan>[NAV_MESH]</color> Mappa di navigazione aggiornata con successo per l'epoca: <b>{nuovaEpocaID}</b>.");
    }
    else
    {
        Debug.LogError($"[NAV_MESH] Impossibile trovare dati NavMeshData validi assegnati per l'epoca: {nuovaEpocaID}");
    }
}
}