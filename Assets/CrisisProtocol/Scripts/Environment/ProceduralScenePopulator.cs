using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Sistema di Popolamento e Arredo Ambientale Logico per Settore 2:
/// - ZONA SALA GENERATORE (Riquadro Rosso - Nord Centro, Z ~ 398..422, X ~ 438..478):
///   Trasformatori alta tensione, console elettriche a muro, quadri industriali, cavi a pavimento, carrello saldatura.
/// - ZONA LABORATORIO CHIMICO (Riquadro Verde - Sud Centro, Z ~ 348..372, X ~ 438..478):
///   Barili biohazard e radioattivi, capsule chimiche/criogeniche, tablet diagnostici, drone di contenimento, fusti di reagenti.
/// - CORRIDOI & STRUTTURA (Perimetro esterno e corridoio centrale):
///   Estintori di sicurezza a parete, tubature alte aeree/soffitto (Y >= 3.8m), scale mobili perimetrali, casse di rifornimento e cassette attrezzi negli angoli.
///   Tutti i corridoi e i varchi pedonali rimangono liberi e sgombri per il passaggio del player e dei droni.
/// </summary>
[ExecuteAlways]
public class ProceduralScenePopulator : MonoBehaviour
{
    public const string NOME_RADICE_OGGETTI = "--- OGGETTI_SCENA_PROCEDURALI ---";

    [Header("Impostazioni Arredo")]
    [Tooltip("Quota Y base del pavimento (default 0.1m).")]
    public float floorY = 0.1f;

    [Tooltip("Genera luci ambientali a tema (Verde Biohazard, Ciano Generatore, Spie).")]
    public bool generaLuciSceniche = true;

    [Tooltip("Aggiungi collider fisici convessi per collisioni realistiche.")]
    public bool generaCollider = true;

    private void Awake()
    {
        if (Application.isPlaying)
        {
            GameObject radiceEsistente = GameObject.Find(NOME_RADICE_OGGETTI);
            if (radiceEsistente == null || radiceEsistente.transform.childCount == 0)
            {
                PopolaScenaCompleta();
            }
        }
    }

    [ContextMenu("Popola Scena con Oggetti Scenici")]
    public void PopolaScenaCompleta()
    {
        // 1. Pulizia eventuale radice precedente
        GameObject root = GameObject.Find(NOME_RADICE_OGGETTI);
        if (root != null)
        {
            if (Application.isPlaying) Destroy(root);
            else DestroyImmediate(root);
        }

        root = new GameObject(NOME_RADICE_OGGETTI);
        root.transform.position = Vector3.zero;

        // Calcola quota pavimento effettiva se presente Pavimento_Settore
        GameObject pav = GameObject.Find("Pavimento_Settore");
        if (pav != null)
        {
            Renderer r = pav.GetComponent<Renderer>();
            if (r != null) floorY = r.bounds.max.y;
        }

        Debug.Log($"<color=cyan>[ProceduralScenePopulator]</color> Avvio posizionamento logico arredi. Quota pavimento Y={floorY:F2}");

        // Sottogruppi organizzati
        Transform grpGeneratore = new GameObject("[ZONA_ROSSA_SALA_GENERATORE]").transform;
        grpGeneratore.SetParent(root.transform, false);

        Transform grpChimico = new GameObject("[ZONA_VERDE_LABORATORIO_CHIMICO]").transform;
        grpChimico.SetParent(root.transform, false);

        Transform grpCorridoi = new GameObject("[CORRIDOI_E_STRUTTURA_ESTERNA]").transform;
        grpCorridoi.SetParent(root.transform, false);

        // 2. Popola Sala Generatore (Zona Rossa)
        ArredaStanzaGeneratore(grpGeneratore);

        // 3. Popola Laboratorio Chimico (Zona Verde)
        ArredaStanzaChimica(grpChimico);

        // 4. Popola Corridoi e Aree Perimetrali
        ArredaCorridoiEPerimetro(grpCorridoi);

        Debug.Log($"<color=green>[ProceduralScenePopulator]</color> Arredo completato con successo! Struttura pulita, logica e percorribile.");
    }

    [ContextMenu("Rimuovi Oggetti Scenici")]
    public void RimuoviOggettiScenici()
    {
        GameObject root = GameObject.Find(NOME_RADICE_OGGETTI);
        if (root != null)
        {
            if (Application.isPlaying) Destroy(root);
            else DestroyImmediate(root);
            Debug.Log("[ProceduralScenePopulator] Oggetti scenici rimossi.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 1. SALA GENERATORE (ZONA ROSSA: X ~ 438..478, Z ~ 398..422)
    // ─────────────────────────────────────────────────────────────────────────

    private void ArredaStanzaGeneratore(Transform parent)
    {
        // 1. Trasformatori ad Alta Tensione (Lungo la parete Ovest interna)
        SpawnProp("electric_transformer", new Vector3(443.0f, floorY, 416.0f), Quaternion.Euler(0, 90f, 0), Vector3.one * 1.3f, 2.7f, parent, "Trasformatore_Principale_01", true);
        SpawnProp("electric_transformer", new Vector3(443.0f, floorY, 404.0f), Quaternion.Euler(0, 90f, 0), Vector3.one * 1.3f, 2.7f, parent, "Trasformatore_Ausiliario_02", true);

        // 2. Pannelli di Controllo Elettrico (Montati a filo parete Ovest)
        SpawnProp("electrical_control_panel_sci-fi", new Vector3(439.5f, floorY, 410.0f), Quaternion.Euler(0, 90f, 0), Vector3.one * 1.15f, 1.85f, parent, "Pannello_Elettrico_Parete_Ovest", true);
        SpawnProp("wires_right_side", new Vector3(439.4f, floorY + 1.4f, 407.5f), Quaternion.Euler(0, 90f, 0), Vector3.one * 1.4f, 1.3f, parent, "Canalina_Cavi_Muro_Ovest", false);

        // 3. Console di Comando Alimentazione (Parete Nord)
        SpawnProp("control_panel", new Vector3(449.0f, floorY, 419.8f), Quaternion.Euler(0, 180f, 0), Vector3.one * 1.1f, 1.75f, parent, "Console_Comando_Generatore", true);

        // 4. Quadri di Sezionamento Industriali (Parete Est e Nord)
        SpawnProp("industrial_control_panel_box", new Vector3(476.8f, floorY, 406.0f), Quaternion.Euler(0, -90f, 0), Vector3.one, 1.5f, parent, "Quadro_Sezionamento_Est", true);
        SpawnProp("industrial_control_panel_box", new Vector3(468.0f, floorY, 421.0f), Quaternion.Euler(0, 180f, 0), Vector3.one, 1.5f, parent, "Quadro_Sezionamento_Nord", true);

        // 5. Cavi ad Alto Voltaggio (Aderenti alla base del muro Ovest e Nord, lontani dal corridoio centrale)
        SpawnProp("cables", new Vector3(441.5f, floorY, 410.0f), Quaternion.Euler(0, 0, 0), Vector3.one * 1.2f, 0.35f, parent, "Fascio_Cavi_Ovest", true);
        SpawnProp("cables", new Vector3(450.0f, floorY, 420.2f), Quaternion.Euler(0, 90f, 0), Vector3.one * 1.1f, 0.35f, parent, "Fascio_Cavi_Nord", true);

        // 6. Postazione Manutenzione e Saldatura (Angolo Nord-Est)
        SpawnProp("portable_welding_cart", new Vector3(472.5f, floorY, 417.0f), Quaternion.Euler(0, -45f, 0), Vector3.one, 1.3f, parent, "Carrello_Saldatura_Manutenzione", true);
        SpawnProp("industrial_toolbox", new Vector3(474.0f, floorY, 415.2f), Quaternion.Euler(0, 25f, 0), Vector3.one, 0.45f, parent, "Cassetta_Attrezzi_Generatore", true);

        // 7. Estintore CO2 a parete (Accanto alla porta Sud)
        SpawnProp("tf2_hd_fire_extinguisher", new Vector3(455.5f, floorY + 1.2f, 399.0f), Quaternion.Euler(0, 0, 0), Vector3.one, 0.75f, parent, "Estintore_CO2_Porta_Sud", false);

        // 8. Luce Scenica Ciano Generatore
        if (generaLuciSceniche)
        {
            CreaLuceScenica(new Vector3(444.0f, floorY + 2.8f, 410.0f), new Color(0.2f, 0.85f, 1f), 2.8f, 6.5f, parent);
            CreaLuceScenica(new Vector3(472.0f, floorY + 2.5f, 416.0f), new Color(1f, 0.75f, 0.3f), 1.8f, 4.5f, parent); // Luce ambra calda saldatura
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. LABORATORIO CHIMICO (ZONA VERDE: X ~ 438..478, Z ~ 348..372)
    // ─────────────────────────────────────────────────────────────────────────

    private void ArredaStanzaChimica(Transform parent)
    {
        // 1. Stazione Barili Radioattivi / Biohazard (Angolo Nord-Est del laboratorio)
        SpawnProp("radioactive_biohazard_unity_ue4", new Vector3(473.0f, floorY, 367.0f), Quaternion.Euler(0, -35f, 0), Vector3.one, 1.25f, parent, "Barili_Biohazard_Quarantena_01", true);

        // 2. Seconda postazione fusti di sicurezza (Angolo Sud-Ovest del laboratorio)
        SpawnProp("radioactive_biohazard_unity_ue4", new Vector3(443.0f, floorY, 352.0f), Quaternion.Euler(0, 145f, 0), Vector3.one * 0.95f, 1.15f, parent, "Barili_Biohazard_Quarantena_02", true);

        // 3. Capsule di Stoccaggio Criogenico / Chimico (Lungo la parete Ovest)
        SpawnProp("sci-fi_container", new Vector3(440.0f, floorY, 362.5f), Quaternion.Euler(0, 90f, 0), Vector3.one * 1.2f, 1.85f, parent, "Capsula_Chimica_A", true);
        SpawnProp("sci-fi_container", new Vector3(440.0f, floorY, 357.5f), Quaternion.Euler(0, 90f, 0), Vector3.one * 1.2f, 1.85f, parent, "Capsula_Chimica_B", true);

        // 4. Fusti Reagenti Industriali (Lungo la parete Est)
        SpawnProp("barrel_pack_-_low_poly_props", new Vector3(476.2f, floorY, 355.0f), Quaternion.Euler(0, -90f, 0), Vector3.one, 1.2f, parent, "Fusti_Reagenti_Chimici", true);

        // 5. Drone di Sicurezza/Contenimento Disattivato (Supporto parete Sud-Est)
        SpawnProp("fnsd-500_-_security_drone", new Vector3(469.0f, floorY, 352.0f), Quaternion.Euler(0, -60f, 0), Vector3.one, 0.9f, parent, "Drone_Ispezione_Lab", true);

        // 6. Tablet di Ricerca Chimica
        SpawnProp("rugged_sci-fi_tactical_military_tablet", new Vector3(448.5f, floorY + 0.85f, 368.0f), Quaternion.Euler(0, 25f, 0), Vector3.one * 0.9f, 0.28f, parent, "Tablet_Diagnostica_Lab", true);

        // 7. Cassetta attrezzi decontaminazione
        SpawnProp("industrial_toolbox", new Vector3(442.0f, floorY, 354.0f), Quaternion.Euler(0, 15f, 0), Vector3.one, 0.45f, parent, "Toolbox_Decontaminazione", true);

        // 8. Estintore Schiuma Chimica a parete (Accanto all'uscita Nord del Lab)
        SpawnProp("tf2_hd_fire_extinguisher", new Vector3(455.5f, floorY + 1.2f, 371.0f), Quaternion.Euler(0, 180f, 0), Vector3.one, 0.75f, parent, "Estintore_Schiuma_Lab_Nord", false);

        // 9. Luce Scenica Fluorescente Verde Biohazard
        if (generaLuciSceniche)
        {
            CreaLuceScenica(new Vector3(472.5f, floorY + 2.0f, 366.5f), new Color(0.1f, 1f, 0.25f), 2.5f, 5.5f, parent);
            CreaLuceScenica(new Vector3(443.0f, floorY + 2.0f, 353.0f), new Color(0.15f, 0.95f, 0.35f), 2.0f, 4.5f, parent);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. CORRIDOI & STRUTTURA ESTERNA (Corsie e Pareti Perimetrali)
    // ─────────────────────────────────────────────────────────────────────────

    private void ArredaCorridoiEPerimetro(Transform parent)
    {
        // ── CORRIDOIO OVEST (X ~ 420..428, Z ~ 335..440) ──────────────────────
        // Tubature montate ESCLUSIVAMENTE a filo soffitto/muro alto (Y = 3.8m)
        SpawnProp("large_modular_pipes_metal", new Vector3(421.0f, floorY + 3.8f, 385.0f), Quaternion.Euler(0, 0, 0), Vector3.one * 1.35f, 2.5f, parent, "Condotti_Alti_Soffitto_Ovest", false);

        // Scala mobile industriale aderente al muro esterno Ovest
        SpawnProp("rolling_ladder", new Vector3(421.5f, floorY, 395.0f), Quaternion.Euler(0, 90f, 0), Vector3.one, 2.6f, parent, "Scala_Manutenzione_Muro_Ovest", true);
        SpawnProp("industrial_toolbox", new Vector3(421.8f, floorY, 392.5f), Quaternion.Euler(0, 10f, 0), Vector3.one, 0.45f, parent, "Toolbox_Muro_Ovest", true);

        // Estintore su colonna / parete Ovest
        SpawnProp("tf2_hd_fire_extinguisher", new Vector3(421.0f, floorY + 1.2f, 370.0f), Quaternion.Euler(0, 90f, 0), Vector3.one, 0.75f, parent, "Estintore_Parete_Ovest", false);

        // Casse di rifornimento nell'angolo Sud-Ovest
        SpawnProp("lowpoly_scifi_containers_pack", new Vector3(422.5f, floorY, 338.0f), Quaternion.Euler(0, 30f, 0), Vector3.one * 1.15f, 1.4f, parent, "Casse_Stoccaggio_Angolo_SudOvest", true);


        // ── CORRIDOIO EST (X ~ 488..496, Z ~ 335..440) ───────────────────────
        // Tubature montate in alto a soffitto (Y = 3.8m)
        SpawnProp("industrial_pipes_pack", new Vector3(494.8f, floorY + 3.8f, 385.0f), Quaternion.Euler(0, 180f, 0), Vector3.one * 1.35f, 2.2f, parent, "Tubature_Aeree_Soffitto_Est", false);

        // Scala mobile industriale aderente al muro esterno Est
        SpawnProp("rolling_ladder", new Vector3(494.5f, floorY, 365.0f), Quaternion.Euler(0, -90f, 0), Vector3.one, 2.6f, parent, "Scala_Manutenzione_Muro_Est", true);

        // Casse cargo nell'angolo Sud-Est (Zona vicino allo spawn del giocatore)
        SpawnProp("lowpoly_scifi_containers_pack", new Vector3(494.0f, floorY, 338.0f), Quaternion.Euler(0, -25f, 0), Vector3.one * 1.15f, 1.4f, parent, "Casse_Cargo_SudEst", true);

        // Estintore a parete vicino allo spawn del giocatore
        SpawnProp("tf2_hd_fire_extinguisher", new Vector3(495.2f, floorY + 1.2f, 345.0f), Quaternion.Euler(0, -90f, 0), Vector3.one, 0.75f, parent, "Estintore_Sicurezza_SpawnPlayer", false);

        // Parti meccaniche di ricambio a bordo muro Est
        SpawnProp("factory_parts", new Vector3(494.5f, floorY, 415.0f), Quaternion.Euler(0, 45f, 0), Vector3.one, 1.2f, parent, "Componenti_Meccanici_Muro_Est", true);


        // ── CORRIDOIO CENTRALE DI COLLEGAMENTO (Tra le due stanze, Z ~ 380..390) ──
        // Paratia di sicurezza aderente alla rientranza laterale
        SpawnProp("modular_wall", new Vector3(439.0f, floorY, 385.0f), Quaternion.Euler(0, 90f, 0), Vector3.one * 1.15f, 2.5f, parent, "Paratia_Separazione_Snodo", true);
        SpawnProp("tf2_hd_fire_extinguisher", new Vector3(476.8f, floorY + 1.2f, 385.0f), Quaternion.Euler(0, -90f, 0), Vector3.one, 0.75f, parent, "Estintore_Snodo_Centrale", false);
        SpawnProp("barrel_pack_-_low_poly_props", new Vector3(468.0f, floorY, 373.2f), Quaternion.Euler(0, 0, 0), Vector3.one * 0.9f, 1.1f, parent, "Fusti_Transito_Centrale", true);


        // ── CORRIDOIO NORD / USCITA (Verso Gate Out, Z ~ 428..445) ────────────
        // Tubature hangar posizionate in alto sopra il varco di uscita (Y = 4.2m)
        SpawnProp("old_rusted_hangar_pipes", new Vector3(458.0f, floorY + 4.2f, 436.0f), Quaternion.Euler(0, 90f, 0), Vector3.one * 1.4f, 2.4f, parent, "Condotti_Scarico_Soffitto_Uscita", false);

        // Casse di sicurezza nell'angolo Nord-Est
        SpawnProp("lowpoly_scifi_containers_pack", new Vector3(493.5f, floorY, 438.0f), Quaternion.Euler(0, -40f, 0), Vector3.one * 1.15f, 1.4f, parent, "Casse_Sicurezza_NordEst", true);

        // Stazione estintore vicino a Gate Out
        SpawnProp("tf2_hd_fire_extinguisher", new Vector3(464.0f, floorY + 1.2f, 442.0f), Quaternion.Euler(0, 180f, 0), Vector3.one, 0.75f, parent, "Estintore_Uscita_GateOut", false);
        SpawnProp("industrial_toolbox", new Vector3(452.0f, floorY, 441.5f), Quaternion.Euler(0, -15f, 0), Vector3.one, 0.45f, parent, "Toolbox_Emergenza_GateOut", true);


        // ── CORRIDOIO SUD / INGRESSO (Z ~ 332..345) ───────────────────────────
        SpawnProp("lowpoly_scifi_containers_pack", new Vector3(423.0f, floorY, 334.0f), Quaternion.Euler(0, 20f, 0), Vector3.one * 1.1f, 1.35f, parent, "Casse_Stoccaggio_SudOvest", true);
        SpawnProp("factory_parts", new Vector3(468.0f, floorY, 333.0f), Quaternion.Euler(0, 0, 0), Vector3.one, 1.2f, parent, "Pallet_Parti_Meccaniche_Sud", true);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPER DI SPAWN, NORMALIZZAZIONE SCALA, ALLINEAMENTO E COLLIDER
    // ─────────────────────────────────────────────────────────────────────────

    private GameObject SpawnProp(string assetNamePartial, Vector3 worldPos, Quaternion rotation, Vector3 scaleMultiplier, float targetHeight, Transform parent, string customName, bool adagiaSulPavimento)
    {
        GameObject prefab = CaricaModello(assetNamePartial);
        if (prefab == null)
        {
            Debug.LogWarning($"[ProceduralScenePopulator] Impossibile trovare il modello per '{assetNamePartial}'");
            return null;
        }

        GameObject instance = Instantiate(prefab, worldPos, rotation, parent);
        instance.name = string.IsNullOrEmpty(customName) ? prefab.name : customName;

        // Normalizza la scala dell'oggetto basandosi sull'altezza target reale
        NormalizzaScala(instance, targetHeight, scaleMultiplier);

        // Se è un oggetto a pavimento, poggialo esattamente a floorY
        if (adagiaSulPavimento)
        {
            AllineaAlSuolo(instance, worldPos.y);
        }
        else
        {
            // Per oggetti a parete o soffitto (es. estintori, tubi alti), mantieni l'altezza worldPos.y specificata
            instance.transform.position = worldPos;
        }

        // Aggiungi Collider solidi se richiesto
        if (generaCollider)
        {
            ApplicaCollider(instance);
        }

        return instance;
    }

    private GameObject CaricaModello(string nomeParziale)
    {
#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets($"{nomeParziale} t:GameObject", new string[] { "Assets/OGGETI DI SCENA" });
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
#endif
        return Resources.Load<GameObject>(nomeParziale);
    }

    private void NormalizzaScala(GameObject go, float targetHeight, Vector3 multiplier)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            b.Encapsulate(renderers[i].bounds);
        }

        float currentHeight = b.size.y;
        if (currentHeight > 0.001f && targetHeight > 0.001f)
        {
            float ratio = targetHeight / currentHeight;
            Vector3 currentScale = go.transform.localScale;
            go.transform.localScale = new Vector3(
                currentScale.x * ratio * multiplier.x,
                currentScale.y * ratio * multiplier.y,
                currentScale.z * ratio * multiplier.z
            );
        }
    }

    private void AllineaAlSuolo(GameObject go, float targetFloorY)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            b.Encapsulate(renderers[i].bounds);
        }

        float deltaY = targetFloorY - b.min.y;
        go.transform.position += new Vector3(0f, deltaY, 0f);
    }

    private void ApplicaCollider(GameObject go)
    {
        Collider[] existing = go.GetComponentsInChildren<Collider>();
        if (existing.Length > 0) return;

        Renderer[] renderers = go.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer mr in renderers)
        {
            MeshFilter mf = mr.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                MeshCollider mc = mr.gameObject.AddComponent<MeshCollider>();
                mc.convex = true;
            }
            else
            {
                BoxCollider bc = mr.gameObject.AddComponent<BoxCollider>();
            }
        }
    }

    private void CreaLuceScenica(Vector3 pos, Color col, float intensity, float range, Transform parent)
    {
        GameObject lightGO = new GameObject("LuceScenica_Point");
        lightGO.transform.position = pos;
        lightGO.transform.SetParent(parent, false);

        Light l = lightGO.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = col;
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.None;
    }
}
