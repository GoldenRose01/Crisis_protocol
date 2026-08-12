using UnityEngine;

namespace GoldenCast.Legacy
{
public class Mutabile : MonoBehaviour
{
    [Header("Visuali delle Epoche")]
    [Tooltip("Inserisci i GameObject figli che contengono le Mesh (0=Presente, 1=Passato)")]
    public GameObject[] modelliEpoche;

    public void CambiaAspetto(int indiceEpoca)
    {
        // 1. Spegne preventivamente tutte le visuali
        foreach (GameObject modello in modelliEpoche)
        {
            if (modello != null)
            {
                modello.SetActive(false);
            }
        }

        // 2. Accende esclusivamente la geometria dell'epoca attiva
        if (indiceEpoca >= 0 && indiceEpoca < modelliEpoche.Length)
        {
            if (modelliEpoche[indiceEpoca] != null)
            {
                modelliEpoche[indiceEpoca].SetActive(true);
            }
        }
    }
}
}
