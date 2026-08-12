using UnityEngine;

namespace GoldenCast.Legacy
{
public class MultiEpochMaskManager : MonoBehaviour
{
    [Tooltip("La telecamera principale del giocatore")]
    public Camera playerCamera;
    
    [Header("Configurazione Layer")]
    [Tooltip("Il nome esatto del layer della geometria immutabile")]
    public string nomeLayerBase = "Geometria_Base";
    
    [Tooltip("Inserisci i NOMI dei layer nell'ordine corretto (0=Presente, 1=Passato1, ecc.)")]
    public string[] nomiLayerEpoche;

    public void AttivaMascheraEpoca(int indiceEpoca)
    {
        if (indiceEpoca < 0 || indiceEpoca >= nomiLayerEpoche.Length)
        {
            Debug.LogError("[MASK MANAGER] Indice epoca fuori limite!");
            return;
        }

        // Calcolo bitwise per attivare il layer base
        int maskBase = 1 << LayerMask.NameToLayer(nomeLayerBase);
        
        // Calcolo bitwise per attivare SOLO il layer dell'epoca richiesta
        int maskEpocaAttiva = 1 << LayerMask.NameToLayer(nomiLayerEpoche[indiceEpoca]);
        
        // La telecamera vedrà solo Base + Epoca Attiva
        playerCamera.cullingMask = maskBase | maskEpocaAttiva;
        
        Debug.Log($"[SISTEMA VISIVO] Transizione completata: Maschera {nomiLayerEpoche[indiceEpoca]} attiva.");
    }
}
}
