using UnityEngine;
using UnityEngine.UI;
using GoldenCast.Legacy; // Serve per leggere il tuo GlobalEnvironmentManager

public class FiltroEpocaController : MonoBehaviour
{
    [Header("Riferimenti UI")]
    [SerializeField] private Image immagineFiltro;

    [System.Serializable]
    public struct ConfigurazioneFiltro
    {
        public string nomeEpocaDebug;
        public Color coloreFiltro;
        [Range(0f, 1f)] public float opacitaTrasparenza;
    }

    [Header("Configurazione Colori per Epoca")]
    [Tooltip("Indice 0 = Passato, 1 = Anni 60, 2 = Anni 2000, 3 = Futuro")]
    [SerializeField] private ConfigurazioneFiltro[] filtriEpoche;

    void Awake()
    {
        if (immagineFiltro == null)
        {
            immagineFiltro = GetComponentInChildren<Image>();
        }
    }

    void OnEnable()
    {
        // Ci iscriviamo all'evento globale del tuo manager
        GlobalEnvironmentManager.OnCambioEpoca += AggiornaFiltroCromatico;
    }

    void OnDisable()
    {
        // Ci disiscriviamo per evitare memory leak
        GlobalEnvironmentManager.OnCambioEpoca -= AggiornaFiltroCromatico;
    }

    private void AggiornaFiltroCromatico(int indiceEpoca)
    {
        if (immagineFiltro == null) return;

        // Controllo di sicurezza sui limiti dell'array
        if (indiceEpoca >= 0 && indiceEpoca < filtriEpoche.Length)
        {
            ConfigurazioneFiltro configurazioneTarget = filtriEpoche[indiceEpoca];

            // Estraiamo il colore e applichiamo l'alfa (trasparenza)
            Color coloreFinale = configurazioneTarget.coloreFiltro;
            coloreFinale.a = configurazioneTarget.opacitaTrasparenza;

            // Applichiamo il colore al materiale dello Shader Graph tramite il componente Image
            immagineFiltro.color = coloreFinale;

            Debug.Log($"<color=magenta>[POST-FX EPOCA]</color> Schermo virato su: {configurazioneTarget.nomeEpocaDebug} (Indice {indiceEpoca})");
        }
        else
        {
            Debug.LogWarning($"[POST-FX EPOCA] Nessun filtro colore configurato per l'indice epoca: {indiceEpoca}");
        }
    }
}