#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Riorganizza il progetto Crisis Protocol in una struttura pulita e logica.
/// Accessibile da: Unity menu > Crisis Protocol > Organizza Progetto
///
/// NON cancella nulla — sposta tutto tramite AssetDatabase.MoveAsset().
/// I GUID e i riferimenti nelle scene rimangono intatti.
/// </summary>
public class ProgettoReorganizer : EditorWindow
{
    private Vector2 scrollPos;
    private List<string> log = new List<string>();
    private bool haEseguito = false;

    // ─────────────────────────────────────────────────────────────────────────
    // STRUTTURA DESTINAZIONE
    // ─────────────────────────────────────────────────────────────────────────
    //
    //  Assets/
    //  ├── CrisisProtocol/
    //  │   ├── Scripts/
    //  │   │   ├── Core/          GameManager, MissionManager
    //  │   │   ├── Player/        muve_pg, PlayerInteract, Salute, Attacco, Sparo, Armi
    //  │   │   ├── Enemy/         GuardiaNpc, DroneRonda, ManutenzioneBot, npc
    //  │   │   ├── Mission/       EmergencyScanner, SectorEmergency/, OstacoloCausale, GestoreFlusso
    //  │   │   ├── UI/            HUDManager, MainMenuManager, ModalUIState, ecc.
    //  │   │   ├── Interfaces/    IInteractable, IDamageable
    //  │   │   ├── Environment/   PortaSettore, RuotaTextureURP, move_camara
    //  │   │   └── Data/          EmergencyKeyData
    //  │   ├── Prefabs/           guardia, drone, futuro uso
    //  │   ├── Materials/         tutti i .mat del progetto
    //  │   ├── Animations/        controller + clip di animazione
    //  │   └── Models/            FBX/GLB/OBJ usati nel gioco
    //  ├── _Legacy/               tutto ciò che viene da GoldenCast
    //  │   ├── Scenes/
    //  │   ├── Prefabs/
    //  │   ├── Models/
    //  │   └── Animations/
    //  └── ThirdParty/            (AsyncronQuest, Blockout, TextMeshPro rimangono dove sono)

    [MenuItem("Crisis Protocol/Organizza Progetto")]
    public static void Apri()
    {
        var w = GetWindow<ProgettoReorganizer>("Organizza Progetto");
        w.minSize = new Vector2(600, 500);
    }

    private void OnGUI()
    {
        GUILayout.Label("Crisis Protocol — Riorganizzazione Progetto", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Questo strumento sposta file e cartelle usando AssetDatabase.MoveAsset().\n" +
            "I GUID e i riferimenti nelle scene rimangono intatti.\n" +
            "NON viene eliminato nulla.", MessageType.Info);

        EditorGUILayout.Space(8);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("📋  Anteprima (solo mostra cosa farebbe)", GUILayout.Height(35)))
            {
                log.Clear();
                haEseguito = false;
                EseguiOrganizzazione(dryRun: true);
            }

            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
            if (GUILayout.Button("✅  ESEGUI Riorganizzazione", GUILayout.Height(35)))
            {
                if (EditorUtility.DisplayDialog(
                    "Conferma Riorganizzazione",
                    "Verranno spostati tutti i file nelle nuove cartelle.\nI riferimenti nelle scene rimarranno validi.\n\nContinuare?",
                    "Sì, esegui", "Annulla"))
                {
                    log.Clear();
                    haEseguito = true;
                    EseguiOrganizzazione(dryRun: false);
                    AssetDatabase.Refresh();
                    EditorUtility.DisplayDialog("Completato",
                        $"Riorganizzazione completata.\nOperazioni: {log.Count}\nControlla il log per i dettagli.",
                        "OK");
                }
            }
            GUI.backgroundColor = Color.white;
        }

        EditorGUILayout.Space(8);
        GUILayout.Label(haEseguito ? "Log operazioni eseguite:" : "Anteprima operazioni:", EditorStyles.boldLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));
        foreach (var line in log)
        {
            Color prev = GUI.color;
            if (line.StartsWith("❌")) GUI.color = Color.red;
            else if (line.StartsWith("✅")) GUI.color = Color.green;
            else if (line.StartsWith("📁")) GUI.color = Color.cyan;
            else if (line.StartsWith("⚠️")) GUI.color = Color.yellow;
            GUILayout.Label(line, EditorStyles.wordWrappedLabel);
            GUI.color = prev;
        }
        EditorGUILayout.EndScrollView();
    }

    // ─────────────────────────────────────────────────────────────────────────

    private void EseguiOrganizzazione(bool dryRun)
    {
        // 1. Crea la struttura di cartelle
        CreaCartellaSeNonEsiste("Assets/CrisisProtocol", dryRun);
        CreaCartellaSeNonEsiste("Assets/CrisisProtocol/Scripts", dryRun);
        CreaCartellaSeNonEsiste("Assets/CrisisProtocol/Scripts/Core", dryRun);
        CreaCartellaSeNonEsiste("Assets/CrisisProtocol/Scripts/Player", dryRun);
        CreaCartellaSeNonEsiste("Assets/CrisisProtocol/Scripts/Enemy", dryRun);
        CreaCartellaSeNonEsiste("Assets/CrisisProtocol/Scripts/Mission", dryRun);
        CreaCartellaSeNonEsiste("Assets/CrisisProtocol/Scripts/UI", dryRun);
        CreaCartellaSeNonEsiste("Assets/CrisisProtocol/Scripts/Interfaces", dryRun);
        CreaCartellaSeNonEsiste("Assets/CrisisProtocol/Scripts/Environment", dryRun);
        CreaCartellaSeNonEsiste("Assets/CrisisProtocol/Scripts/Data", dryRun);
        CreaCartellaSeNonEsiste("Assets/CrisisProtocol/Prefabs", dryRun);
        CreaCartellaSeNonEsiste("Assets/CrisisProtocol/Materials", dryRun);
        CreaCartellaSeNonEsiste("Assets/CrisisProtocol/Animations", dryRun);
        CreaCartellaSeNonEsiste("Assets/CrisisProtocol/Models", dryRun);
        CreaCartellaSeNonEsiste("Assets/_Legacy", dryRun);
        CreaCartellaSeNonEsiste("Assets/_Legacy/Scenes", dryRun);
        CreaCartellaSeNonEsiste("Assets/_Legacy/Prefabs", dryRun);
        CreaCartellaSeNonEsiste("Assets/_Legacy/Models", dryRun);
        CreaCartellaSeNonEsiste("Assets/_Legacy/Animations", dryRun);

        log.Add("");
        log.Add("── SCRIPT CORE ──────────────────────────────────────────");
        Sposta("Assets/MissionManager.cs",                       "Assets/CrisisProtocol/Scripts/Core/MissionManager.cs", dryRun);
        Sposta("Assets/script/GameManager.cs",                   "Assets/CrisisProtocol/Scripts/Core/GameManager.cs", dryRun);

        log.Add("");
        log.Add("── SCRIPT PLAYER ────────────────────────────────────────");
        Sposta("Assets/script/muve_pg.cs",                       "Assets/CrisisProtocol/Scripts/Player/muve_pg.cs", dryRun);
        Sposta("Assets/script/PlayerInteract.cs",                "Assets/CrisisProtocol/Scripts/Player/PlayerInteract.cs", dryRun);
        Sposta("Assets/script/SalutePlayer.cs",                  "Assets/CrisisProtocol/Scripts/Player/SalutePlayer.cs", dryRun);
        Sposta("Assets/script/AttaccoPlayer.cs",                 "Assets/CrisisProtocol/Scripts/Player/AttaccoPlayer.cs", dryRun);
        Sposta("Assets/script/SparoPlayer.cs",                   "Assets/CrisisProtocol/Scripts/Player/SparoPlayer.cs", dryRun);
        Sposta("Assets/script/GestoreArmi.cs",                   "Assets/CrisisProtocol/Scripts/Player/GestoreArmi.cs", dryRun);

        log.Add("");
        log.Add("── SCRIPT ENEMY ─────────────────────────────────────────");
        Sposta("Assets/script/GuardiaNpc.cs",                    "Assets/CrisisProtocol/Scripts/Enemy/GuardiaNpc.cs", dryRun);
        Sposta("Assets/script/DroneRonda.cs",                    "Assets/CrisisProtocol/Scripts/Enemy/DroneRonda.cs", dryRun);
        Sposta("Assets/script/ManutenzioneBot.cs",               "Assets/CrisisProtocol/Scripts/Enemy/ManutenzioneBot.cs", dryRun);
        Sposta("Assets/npc.cs",                                  "Assets/CrisisProtocol/Scripts/Enemy/npc.cs", dryRun);

        log.Add("");
        log.Add("── SCRIPT MISSION / SECTOR EMERGENCY ────────────────────");
        Sposta("Assets/script/EmergencyScanner.cs",              "Assets/CrisisProtocol/Scripts/Mission/EmergencyScanner.cs", dryRun);
        Sposta("Assets/script/OstacoloCausale.cs",               "Assets/CrisisProtocol/Scripts/Mission/OstacoloCausale.cs", dryRun);
        Sposta("Assets/script/GestoreFlusso.cs",                 "Assets/CrisisProtocol/Scripts/Mission/GestoreFlusso.cs", dryRun);
        SpostaCarto("Assets/script/SectorEmergency",             "Assets/CrisisProtocol/Scripts/Mission/SectorEmergency", dryRun);

        log.Add("");
        log.Add("── SCRIPT UI ────────────────────────────────────────────");
        Sposta("Assets/script/HUDManager.cs",                    "Assets/CrisisProtocol/Scripts/UI/HUDManager.cs", dryRun);
        Sposta("Assets/script/MainMenuManager.cs",               "Assets/CrisisProtocol/Scripts/UI/MainMenuManager.cs", dryRun);
        Sposta("Assets/script/GoldenCastUIController.cs",        "Assets/CrisisProtocol/Scripts/UI/GoldenCastUIController.cs", dryRun);
        Sposta("Assets/script/ModalUIState.cs",                  "Assets/CrisisProtocol/Scripts/UI/ModalUIState.cs", dryRun);
        Sposta("Assets/script/MenuAudioSilencer.cs",             "Assets/CrisisProtocol/Scripts/UI/MenuAudioSilencer.cs", dryRun);
        Sposta("Assets/script/AutoCommitMenu.cs",                "Assets/CrisisProtocol/Scripts/UI/AutoCommitMenu.cs", dryRun);
        Sposta("Assets/script/AnalogGaugeUI.cs",                 "Assets/CrisisProtocol/Scripts/UI/AnalogGaugeUI.cs", dryRun);
        Sposta("Assets/script/RadialCooldownUI.cs",              "Assets/CrisisProtocol/Scripts/UI/RadialCooldownUI.cs", dryRun);

        log.Add("");
        log.Add("── SCRIPT INTERFACES ────────────────────────────────────");
        Sposta("Assets/script/IInteractable.cs",                 "Assets/CrisisProtocol/Scripts/Interfaces/IInteractable.cs", dryRun);
        Sposta("Assets/script/IDamageable.cs",                   "Assets/CrisisProtocol/Scripts/Interfaces/IDamageable.cs", dryRun);

        log.Add("");
        log.Add("── SCRIPT ENVIRONMENT ───────────────────────────────────");
        Sposta("Assets/script/PortaSettore.cs",                  "Assets/CrisisProtocol/Scripts/Environment/PortaSettore.cs", dryRun);
        Sposta("Assets/script/RuotaTextureURP.cs",               "Assets/CrisisProtocol/Scripts/Environment/RuotaTextureURP.cs", dryRun);
        Sposta("Assets/script/move_camara.cs",                   "Assets/CrisisProtocol/Scripts/Environment/move_camara.cs", dryRun);

        log.Add("");
        log.Add("── SCRIPT DATA ──────────────────────────────────────────");
        Sposta("Assets/script/EmergencyKeyData.cs",              "Assets/CrisisProtocol/Scripts/Data/EmergencyKeyData.cs", dryRun);

        log.Add("");
        log.Add("── PREFAB (progetto attivo) ─────────────────────────────");
        Sposta("Assets/prefab/guardia 1.prefab",                 "Assets/CrisisProtocol/Prefabs/guardia 1.prefab", dryRun);
        Sposta("Assets/prefab/drone_nemico 1.prefab",            "Assets/CrisisProtocol/Prefabs/drone_nemico 1.prefab", dryRun);

        log.Add("");
        log.Add("── ANIMAZIONI (progetto attivo) ─────────────────────────");
        Sposta("Assets/guardia.controller",                      "Assets/CrisisProtocol/Animations/guardia.controller", dryRun);
        Sposta("Assets/pg.controller",                           "Assets/CrisisProtocol/Animations/pg.controller", dryRun);
        Sposta("Assets/Jump.fbx",                                "Assets/CrisisProtocol/Animations/Jump.fbx", dryRun);
        Sposta("Assets/Running.fbx",                             "Assets/CrisisProtocol/Animations/Running.fbx", dryRun);
        Sposta("Assets/Rifle Idle.fbx",                          "Assets/CrisisProtocol/Animations/Rifle Idle.fbx", dryRun);
        Sposta("Assets/Rifle Walk.fbx",                          "Assets/CrisisProtocol/Animations/Rifle Walk.fbx", dryRun);
        Sposta("Assets/Falling Idle.fbx",                        "Assets/CrisisProtocol/Animations/Falling Idle.fbx", dryRun);

        log.Add("");
        log.Add("── MATERIALI (progetto attivo) ──────────────────────────");
        Sposta("Assets/script/Mat_ToonFiltro.mat",               "Assets/CrisisProtocol/Materials/Mat_ToonFiltro.mat", dryRun);
        Sposta("Assets/case.mat",                                "Assets/CrisisProtocol/Materials/case.mat", dryRun);

        log.Add("");
        log.Add("── LEGACY — Scene ───────────────────────────────────────");
        Sposta("Assets/Scenes/locale.unity",                     "Assets/_Legacy/Scenes/locale.unity", dryRun);
        Sposta("Assets/Scenes/Passato_1961.unity",               "Assets/_Legacy/Scenes/Passato_1961.unity", dryRun);
        Sposta("Assets/Scenes/test.unity",                       "Assets/_Legacy/Scenes/test.unity", dryRun);
        Sposta("Assets/loc.unity",                               "Assets/_Legacy/Scenes/loc.unity", dryRun);

        log.Add("");
        log.Add("── LEGACY — Prefab ──────────────────────────────────────");
        Sposta("Assets/prefab/viaggiatore.prefab",               "Assets/_Legacy/Prefabs/viaggiatore.prefab", dryRun);
        Sposta("Assets/prefab/viaggiatore Variant.prefab",       "Assets/_Legacy/Prefabs/viaggiatore Variant.prefab", dryRun);
        Sposta("Assets/prefab/comparsa 1.prefab",                "Assets/_Legacy/Prefabs/comparsa 1.prefab", dryRun);
        Sposta("Assets/prefab/Riccinto dietro.prefab",           "Assets/_Legacy/Prefabs/Riccinto dietro.prefab", dryRun);

        log.Add("");
        log.Add("── LEGACY — Modelli ─────────────────────────────────────");
        Sposta("Assets/prefab/sedia-01.fbx",                     "Assets/_Legacy/Models/sedia-01.fbx", dryRun);
        Sposta("Assets/prefab/tavolo_da_pranzo.glb",             "Assets/_Legacy/Models/tavolo_da_pranzo.glb", dryRun);
        Sposta("Assets/prefab/fontana_pigna.glb",                "Assets/_Legacy/Models/fontana_pigna.glb", dryRun);
        Sposta("Assets/fbxFountain.fbx",                         "Assets/_Legacy/Models/fbxFountain.fbx", dryRun);
        Sposta("Assets/Senza Titolo.obj",                        "Assets/_Legacy/Models/Senza Titolo.obj", dryRun);
        SpostaCarto("Assets/woman 1",                            "Assets/_Legacy/Models/woman 1", dryRun);

        log.Add("");
        log.Add("── LEGACY — Animazioni ──────────────────────────────────");
        Sposta("Assets/FERMO.anim",                              "Assets/_Legacy/Animations/FERMO.anim", dryRun);
        Sposta("Assets/FERMO 1.anim",                            "Assets/_Legacy/Animations/FERMO 1.anim", dryRun);
        Sposta("Assets/Start Walking.fbx",                       "Assets/_Legacy/Animations/Start Walking.fbx", dryRun);
        Sposta("Assets/Pulling Lever.fbx",                       "Assets/_Legacy/Animations/Pulling Lever.fbx", dryRun);
        Sposta("Assets/Standing W_Briefcase Idle.fbx",           "Assets/_Legacy/Animations/Standing W_Briefcase Idle.fbx", dryRun);
        Sposta("Assets/viaggiatore nel tempo.controller",        "Assets/_Legacy/Animations/viaggiatore nel tempo.controller", dryRun);

        log.Add("");
        log.Add($"── FINE — {log.Count} operazioni totali ─────────────────────────────");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private void Sposta(string da, string a, bool dryRun)
    {
        if (!File.Exists(da))
        {
            log.Add($"⚠️  Non trovato (skip): {da}");
            return;
        }

        if (dryRun)
        {
            log.Add($"   {Path.GetFileName(da)}\n   {da}\n   → {a}");
            return;
        }

        string errore = AssetDatabase.MoveAsset(da, a);
        if (string.IsNullOrEmpty(errore))
            log.Add($"✅  {Path.GetFileName(da)} → {a}");
        else
            log.Add($"❌  ERRORE {Path.GetFileName(da)}: {errore}");
    }

    private void SpostaCarto(string da, string a, bool dryRun)
    {
        if (!AssetDatabase.IsValidFolder(da))
        {
            log.Add($"⚠️  Cartella non trovata (skip): {da}");
            return;
        }

        if (dryRun)
        {
            log.Add($"📁 Cartella: {da}\n   → {a}");
            return;
        }

        string errore = AssetDatabase.MoveAsset(da, a);
        if (string.IsNullOrEmpty(errore))
            log.Add($"✅  Cartella spostata: {a}");
        else
            log.Add($"❌  ERRORE cartella {da}: {errore}");
    }

    private void CreaCartellaSeNonEsiste(string path, bool dryRun)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string name   = Path.GetFileName(path);

        if (dryRun)
        {
            log.Add($"📁 Crea cartella: {path}");
            return;
        }

        AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
