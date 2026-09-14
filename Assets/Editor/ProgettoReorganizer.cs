// ============================================================================
// Crisis Protocol / Sector Containment - Utility editor
// File: .\Assets\Editor\ProgettoReorganizer.cs
// Responsabilita': automatizza setup, popolamento scena, salvataggio, validazione o manutenzione direttamente dentro Unity Editor.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
#if UNITY_EDITOR // prep ok // riga-ok
using UnityEngine; // usa lib // riga-ok
using UnityEditor; // usa lib // riga-ok
using System.Collections.Generic; // usa lib // riga-ok
using System.IO; // usa lib // riga-ok

/// <summary>
/// Riorganizza il progetto Crisis Protocol in una struttura pulita e logica.
/// Accessibile da: Unity menu > Crisis Protocol > Organizza Progetto
///
/// NON cancella nulla — sposta tutto tramite AssetDatabase.MoveAsset().
/// I GUID e i riferimenti nelle scene rimangono intatti.
/// </summary>
// blocco: classe x roba grossa
public class ProgettoReorganizer : EditorWindow // classe qui // riga-ok
{ // apre // riga-ok
    private Vector2 scrollPos; // roba pub // riga-ok
    private List<string> log = new List<string>(); // roba pub // riga-ok
    private bool haEseguito = false; // roba pub // riga-ok

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
    //  ├── _Legacy/               tutto ciò che viene da progetto-precedente
    //  │   ├── Scenes/
    //  │   ├── Prefabs/
    //  │   ├── Models/
    //  │   └── Animations/
    //  └── ThirdParty/            (AsyncronQuest, Blockout, TextMeshPro rimangono dove sono)

    [MenuItem("Crisis Protocol/Organizza Progetto")] // nota unity // riga-ok
    // blocco: funzione fa cose
    public static void Apri() // roba pub // riga-ok
    { // apre // riga-ok
        var w = GetWindow<ProgettoReorganizer>("Organizza Progetto"); // setta // riga-ok
        w.minSize = new Vector2(600, 500); // setta // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void OnGUI() // roba pub // riga-ok
    { // apre // riga-ok
        GUILayout.Label("Crisis Protocol — Riorganizzazione Progetto", EditorStyles.boldLabel); // chiama // riga-ok
        EditorGUILayout.HelpBox( // ok qua // riga-ok
            "Questo strumento sposta file e cartelle usando AssetDatabase.MoveAsset().\n" + // ok qua // riga-ok
            "I GUID e i riferimenti nelle scene rimangono intatti.\n" + // ok qua // riga-ok
            "NON viene eliminato nulla.", MessageType.Info); // chiama // riga-ok

        EditorGUILayout.Space(8); // chiama // riga-ok

        using (new EditorGUILayout.HorizontalScope()) // usa lib // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (GUILayout.Button("📋  Anteprima (solo mostra cosa farebbe)", GUILayout.Height(35))) // se ok // riga-ok
            { // apre // riga-ok
                log.Clear(); // chiama // riga-ok
                haEseguito = false; // setta // riga-ok
                EseguiOrganizzazione(dryRun: true); // chiama // riga-ok
            } // chiude // riga-ok

            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f); // setta // riga-ok
            // blocco: controlla se va
            if (GUILayout.Button("✅  ESEGUI Riorganizzazione", GUILayout.Height(35))) // se ok // riga-ok
            { // apre // riga-ok
                // blocco: controlla se va
                if (EditorUtility.DisplayDialog( // se ok // riga-ok
                    "Conferma Riorganizzazione", // ok qua // riga-ok
                    "Verranno spostati tutti i file nelle nuove cartelle.\nI riferimenti nelle scene rimarranno validi.\n\nContinuare?", // ok qua // riga-ok
                    "Sì, esegui", "Annulla")) // chiama // riga-ok
                { // apre // riga-ok
                    log.Clear(); // chiama // riga-ok
                    haEseguito = true; // setta // riga-ok
                    EseguiOrganizzazione(dryRun: false); // chiama // riga-ok
                    AssetDatabase.Refresh(); // chiama // riga-ok
                    EditorUtility.DisplayDialog("Completato", // ok qua // riga-ok
                        $"Riorganizzazione completata.\nOperazioni: {log.Count}\nControlla il log per i dettagli.", // ok qua // riga-ok
                        "OK"); // chiama // riga-ok
                } // chiude // riga-ok
            } // chiude // riga-ok
            GUI.backgroundColor = Color.white; // setta // riga-ok
        } // chiude // riga-ok

        EditorGUILayout.Space(8); // chiama // riga-ok
        GUILayout.Label(haEseguito ? "Log operazioni eseguite:" : "Anteprima operazioni:", EditorStyles.boldLabel); // chiama // riga-ok

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true)); // setta // riga-ok
        // blocco: gira piu volte
        foreach (var line in log) // ciclo x // riga-ok
        { // apre // riga-ok
            Color prev = GUI.color; // setta // riga-ok
            // blocco: controlla se va
            if (line.StartsWith("❌")) GUI.color = Color.red; // se ok // riga-ok
            // blocco: controlla se va
            else if (line.StartsWith("✅")) GUI.color = Color.green; // se ok // riga-ok
            // blocco: controlla se va
            else if (line.StartsWith("📁")) GUI.color = Color.cyan; // se ok // riga-ok
            // blocco: controlla se va
            else if (line.StartsWith("⚠️")) GUI.color = Color.yellow; // se ok // riga-ok
            GUILayout.Label(line, EditorStyles.wordWrappedLabel); // chiama // riga-ok
            GUI.color = prev; // setta // riga-ok
        } // chiude // riga-ok
        EditorGUILayout.EndScrollView(); // chiama // riga-ok
    } // chiude // riga-ok

    // ─────────────────────────────────────────────────────────────────────────

    // blocco: funzione fa cose
    private void EseguiOrganizzazione(bool dryRun) // roba pub // riga-ok
    { // apre // riga-ok
        // 1. Crea la struttura di cartelle
        CreaCartellaSeNonEsiste("Assets/CrisisProtocol", dryRun); // chiama // riga-ok
        CreaCartellaSeNonEsiste("Assets/CrisisProtocol/Scripts", dryRun); // chiama // riga-ok
        CreaCartellaSeNonEsiste("Assets/CrisisProtocol/Scripts/Core", dryRun); // chiama // riga-ok
        CreaCartellaSeNonEsiste("Assets/CrisisProtocol/Scripts/Player", dryRun); // chiama // riga-ok
        CreaCartellaSeNonEsiste("Assets/CrisisProtocol/Scripts/Enemy", dryRun); // chiama // riga-ok
        CreaCartellaSeNonEsiste("Assets/CrisisProtocol/Scripts/Mission", dryRun); // chiama // riga-ok
        CreaCartellaSeNonEsiste("Assets/CrisisProtocol/Scripts/UI", dryRun); // chiama // riga-ok
        CreaCartellaSeNonEsiste("Assets/CrisisProtocol/Scripts/Interfaces", dryRun); // chiama // riga-ok
        CreaCartellaSeNonEsiste("Assets/CrisisProtocol/Scripts/Environment", dryRun); // chiama // riga-ok
        CreaCartellaSeNonEsiste("Assets/CrisisProtocol/Scripts/Data", dryRun); // chiama // riga-ok
        CreaCartellaSeNonEsiste("Assets/CrisisProtocol/Prefabs", dryRun); // chiama // riga-ok
        CreaCartellaSeNonEsiste("Assets/CrisisProtocol/Materials", dryRun); // chiama // riga-ok
        CreaCartellaSeNonEsiste("Assets/CrisisProtocol/Animations", dryRun); // chiama // riga-ok
        CreaCartellaSeNonEsiste("Assets/CrisisProtocol/Models", dryRun); // chiama // riga-ok
        CreaCartellaSeNonEsiste("Assets/_Legacy", dryRun); // chiama // riga-ok
        CreaCartellaSeNonEsiste("Assets/_Legacy/Scenes", dryRun); // chiama // riga-ok
        CreaCartellaSeNonEsiste("Assets/_Legacy/Prefabs", dryRun); // chiama // riga-ok
        CreaCartellaSeNonEsiste("Assets/_Legacy/Models", dryRun); // chiama // riga-ok
        CreaCartellaSeNonEsiste("Assets/_Legacy/Animations", dryRun); // chiama // riga-ok

        log.Add(""); // chiama // riga-ok
        log.Add("── SCRIPT CORE ──────────────────────────────────────────"); // chiama // riga-ok
        Sposta("Assets/MissionManager.cs",                       "Assets/CrisisProtocol/Scripts/Core/MissionManager.cs", dryRun); // chiama // riga-ok
        Sposta("Assets/script/GameManager.cs",                   "Assets/CrisisProtocol/Scripts/Core/GameManager.cs", dryRun); // chiama // riga-ok

        log.Add(""); // chiama // riga-ok
        log.Add("── SCRIPT PLAYER ────────────────────────────────────────"); // chiama // riga-ok
        Sposta("Assets/script/muve_pg.cs",                       "Assets/CrisisProtocol/Scripts/Player/muve_pg.cs", dryRun); // chiama // riga-ok
        Sposta("Assets/script/PlayerInteract.cs",                "Assets/CrisisProtocol/Scripts/Player/PlayerInteract.cs", dryRun); // chiama // riga-ok
        Sposta("Assets/script/SalutePlayer.cs",                  "Assets/CrisisProtocol/Scripts/Player/SalutePlayer.cs", dryRun); // chiama // riga-ok
        Sposta("Assets/script/AttaccoPlayer.cs",                 "Assets/CrisisProtocol/Scripts/Player/AttaccoPlayer.cs", dryRun); // chiama // riga-ok
        Sposta("Assets/script/SparoPlayer.cs",                   "Assets/CrisisProtocol/Scripts/Player/SparoPlayer.cs", dryRun); // chiama // riga-ok
        Sposta("Assets/script/GestoreArmi.cs",                   "Assets/CrisisProtocol/Scripts/Player/GestoreArmi.cs", dryRun); // chiama // riga-ok

        log.Add(""); // chiama // riga-ok
        log.Add("── SCRIPT ENEMY ─────────────────────────────────────────"); // chiama // riga-ok
        Sposta("Assets/script/GuardiaNpc.cs",                    "Assets/CrisisProtocol/Scripts/Enemy/GuardiaNpc.cs", dryRun); // chiama // riga-ok
        Sposta("Assets/script/DroneRonda.cs",                    "Assets/CrisisProtocol/Scripts/Enemy/DroneRonda.cs", dryRun); // chiama // riga-ok
        Sposta("Assets/script/ManutenzioneBot.cs",               "Assets/CrisisProtocol/Scripts/Enemy/ManutenzioneBot.cs", dryRun); // chiama // riga-ok
        Sposta("Assets/npc.cs",                                  "Assets/CrisisProtocol/Scripts/Enemy/npc.cs", dryRun); // chiama // riga-ok

        log.Add(""); // chiama // riga-ok
        log.Add("── SCRIPT MISSION / SECTOR EMERGENCY ────────────────────"); // chiama // riga-ok
        Sposta("Assets/script/EmergencyScanner.cs",              "Assets/CrisisProtocol/Scripts/Mission/EmergencyScanner.cs", dryRun); // chiama // riga-ok
        Sposta("Assets/script/OstacoloCausale.cs",               "Assets/CrisisProtocol/Scripts/Mission/OstacoloCausale.cs", dryRun); // chiama // riga-ok
        Sposta("Assets/script/GestoreFlusso.cs",                 "Assets/CrisisProtocol/Scripts/Mission/GestoreFlusso.cs", dryRun); // chiama // riga-ok
        SpostaCarto("Assets/script/SectorEmergency",             "Assets/CrisisProtocol/Scripts/Mission/SectorEmergency", dryRun); // chiama // riga-ok

        log.Add(""); // chiama // riga-ok
        log.Add("── SCRIPT UI ────────────────────────────────────────────"); // chiama // riga-ok
        Sposta("Assets/script/HUDManager.cs",                    "Assets/CrisisProtocol/Scripts/UI/HUDManager.cs", dryRun); // chiama // riga-ok
        Sposta("Assets/script/MainMenuManager.cs",               "Assets/CrisisProtocol/Scripts/UI/MainMenuManager.cs", dryRun); // chiama // riga-ok
        Sposta("Assets/script/CrisisProtocolUIController.cs",        "Assets/CrisisProtocol/Scripts/UI/CrisisProtocolUIController.cs", dryRun); // chiama // riga-ok
        Sposta("Assets/script/ModalUIState.cs",                  "Assets/CrisisProtocol/Scripts/UI/ModalUIState.cs", dryRun); // chiama // riga-ok
        Sposta("Assets/script/MenuAudioSilencer.cs",             "Assets/CrisisProtocol/Scripts/UI/MenuAudioSilencer.cs", dryRun); // chiama // riga-ok
        Sposta("Assets/script/AutoCommitMenu.cs",                "Assets/CrisisProtocol/Scripts/UI/AutoCommitMenu.cs", dryRun); // chiama // riga-ok
        Sposta("Assets/script/AnalogGaugeUI.cs",                 "Assets/CrisisProtocol/Scripts/UI/AnalogGaugeUI.cs", dryRun); // chiama // riga-ok
        Sposta("Assets/script/RadialCooldownUI.cs",              "Assets/CrisisProtocol/Scripts/UI/RadialCooldownUI.cs", dryRun); // chiama // riga-ok

        log.Add(""); // chiama // riga-ok
        log.Add("── SCRIPT INTERFACES ────────────────────────────────────"); // chiama // riga-ok
        Sposta("Assets/script/IInteractable.cs",                 "Assets/CrisisProtocol/Scripts/Interfaces/IInteractable.cs", dryRun); // chiama // riga-ok
        Sposta("Assets/script/IDamageable.cs",                   "Assets/CrisisProtocol/Scripts/Interfaces/IDamageable.cs", dryRun); // chiama // riga-ok

        log.Add(""); // chiama // riga-ok
        log.Add("── SCRIPT ENVIRONMENT ───────────────────────────────────"); // chiama // riga-ok
        Sposta("Assets/script/PortaSettore.cs",                  "Assets/CrisisProtocol/Scripts/Environment/PortaSettore.cs", dryRun); // chiama // riga-ok
        Sposta("Assets/script/RuotaTextureURP.cs",               "Assets/CrisisProtocol/Scripts/Environment/RuotaTextureURP.cs", dryRun); // chiama // riga-ok
        Sposta("Assets/script/move_camara.cs",                   "Assets/CrisisProtocol/Scripts/Environment/move_camara.cs", dryRun); // chiama // riga-ok

        log.Add(""); // chiama // riga-ok
        log.Add("── SCRIPT DATA ──────────────────────────────────────────"); // chiama // riga-ok
        Sposta("Assets/script/EmergencyKeyData.cs",              "Assets/CrisisProtocol/Scripts/Data/EmergencyKeyData.cs", dryRun); // chiama // riga-ok

        log.Add(""); // chiama // riga-ok
        log.Add("── PREFAB (progetto attivo) ─────────────────────────────"); // chiama // riga-ok
        Sposta("Assets/prefab/guardia 1.prefab",                 "Assets/CrisisProtocol/Prefabs/guardia 1.prefab", dryRun); // chiama // riga-ok
        Sposta("Assets/prefab/drone_nemico 1.prefab",            "Assets/CrisisProtocol/Prefabs/drone_nemico 1.prefab", dryRun); // chiama // riga-ok

        log.Add(""); // chiama // riga-ok
        log.Add("── ANIMAZIONI (progetto attivo) ─────────────────────────"); // chiama // riga-ok
        Sposta("Assets/guardia.controller",                      "Assets/CrisisProtocol/Animations/guardia.controller", dryRun); // chiama // riga-ok
        Sposta("Assets/pg.controller",                           "Assets/CrisisProtocol/Animations/pg.controller", dryRun); // chiama // riga-ok
        Sposta("Assets/Jump.fbx",                                "Assets/CrisisProtocol/Animations/Jump.fbx", dryRun); // chiama // riga-ok
        Sposta("Assets/Running.fbx",                             "Assets/CrisisProtocol/Animations/Running.fbx", dryRun); // chiama // riga-ok
        Sposta("Assets/Rifle Idle.fbx",                          "Assets/CrisisProtocol/Animations/Rifle Idle.fbx", dryRun); // chiama // riga-ok
        Sposta("Assets/Rifle Walk.fbx",                          "Assets/CrisisProtocol/Animations/Rifle Walk.fbx", dryRun); // chiama // riga-ok
        Sposta("Assets/Falling Idle.fbx",                        "Assets/CrisisProtocol/Animations/Falling Idle.fbx", dryRun); // chiama // riga-ok

        log.Add(""); // chiama // riga-ok
        log.Add("── MATERIALI (progetto attivo) ──────────────────────────"); // chiama // riga-ok
        Sposta("Assets/script/Mat_ToonFiltro.mat",               "Assets/CrisisProtocol/Materials/Mat_ToonFiltro.mat", dryRun); // chiama // riga-ok
        Sposta("Assets/case.mat",                                "Assets/CrisisProtocol/Materials/case.mat", dryRun); // chiama // riga-ok

        log.Add(""); // chiama // riga-ok
        log.Add("── LEGACY — Scene ───────────────────────────────────────"); // chiama // riga-ok
        Sposta("Assets/Scenes/locale.unity",                     "Assets/_Legacy/Scenes/locale.unity", dryRun); // chiama // riga-ok
        Sposta("Assets/Scenes/Passato_1961.unity",               "Assets/_Legacy/Scenes/Passato_1961.unity", dryRun); // chiama // riga-ok
        Sposta("Assets/Scenes/test.unity",                       "Assets/_Legacy/Scenes/test.unity", dryRun); // chiama // riga-ok
        Sposta("Assets/loc.unity",                               "Assets/_Legacy/Scenes/loc.unity", dryRun); // chiama // riga-ok

        log.Add(""); // chiama // riga-ok
        log.Add("── LEGACY — Prefab ──────────────────────────────────────"); // chiama // riga-ok
        Sposta("Assets/prefab/viaggiatore.prefab",               "Assets/_Legacy/Prefabs/viaggiatore.prefab", dryRun); // chiama // riga-ok
        Sposta("Assets/prefab/viaggiatore Variant.prefab",       "Assets/_Legacy/Prefabs/viaggiatore Variant.prefab", dryRun); // chiama // riga-ok
        Sposta("Assets/prefab/comparsa 1.prefab",                "Assets/_Legacy/Prefabs/comparsa 1.prefab", dryRun); // chiama // riga-ok
        Sposta("Assets/prefab/Riccinto dietro.prefab",           "Assets/_Legacy/Prefabs/Riccinto dietro.prefab", dryRun); // chiama // riga-ok

        log.Add(""); // chiama // riga-ok
        log.Add("── LEGACY — Modelli ─────────────────────────────────────"); // chiama // riga-ok
        Sposta("Assets/prefab/sedia-01.fbx",                     "Assets/_Legacy/Models/sedia-01.fbx", dryRun); // chiama // riga-ok
        Sposta("Assets/prefab/tavolo_da_pranzo.glb",             "Assets/_Legacy/Models/tavolo_da_pranzo.glb", dryRun); // chiama // riga-ok
        Sposta("Assets/prefab/fontana_pigna.glb",                "Assets/_Legacy/Models/fontana_pigna.glb", dryRun); // chiama // riga-ok
        Sposta("Assets/fbxFountain.fbx",                         "Assets/_Legacy/Models/fbxFountain.fbx", dryRun); // chiama // riga-ok
        Sposta("Assets/Senza Titolo.obj",                        "Assets/_Legacy/Models/Senza Titolo.obj", dryRun); // chiama // riga-ok
        SpostaCarto("Assets/woman 1",                            "Assets/_Legacy/Models/woman 1", dryRun); // chiama // riga-ok

        log.Add(""); // chiama // riga-ok
        log.Add("── LEGACY — Animazioni ──────────────────────────────────"); // chiama // riga-ok
        Sposta("Assets/FERMO.anim",                              "Assets/_Legacy/Animations/FERMO.anim", dryRun); // chiama // riga-ok
        Sposta("Assets/FERMO 1.anim",                            "Assets/_Legacy/Animations/FERMO 1.anim", dryRun); // chiama // riga-ok
        Sposta("Assets/Start Walking.fbx",                       "Assets/_Legacy/Animations/Start Walking.fbx", dryRun); // chiama // riga-ok
        Sposta("Assets/Pulling Lever.fbx",                       "Assets/_Legacy/Animations/Pulling Lever.fbx", dryRun); // chiama // riga-ok
        Sposta("Assets/Standing W_Briefcase Idle.fbx",           "Assets/_Legacy/Animations/Standing W_Briefcase Idle.fbx", dryRun); // chiama // riga-ok
        Sposta("Assets/viaggiatore nel tempo.controller",        "Assets/_Legacy/Animations/viaggiatore nel tempo.controller", dryRun); // chiama // riga-ok

        log.Add(""); // chiama // riga-ok
        log.Add($"── FINE — {log.Count} operazioni totali ─────────────────────────────"); // chiama // riga-ok
    } // chiude // riga-ok

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    // blocco: funzione fa cose
    private void Sposta(string da, string a, bool dryRun) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (!File.Exists(da)) // se ok // riga-ok
        { // apre // riga-ok
            log.Add($"⚠️  Non trovato (skip): {da}"); // chiama // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (dryRun) // se ok // riga-ok
        { // apre // riga-ok
            log.Add($"   {Path.GetFileName(da)}\n   {da}\n   → {a}"); // chiama // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        string errore = AssetDatabase.MoveAsset(da, a); // setta // riga-ok
        // blocco: controlla se va
        if (string.IsNullOrEmpty(errore)) // se ok // riga-ok
            log.Add($"✅  {Path.GetFileName(da)} → {a}"); // chiama // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
            log.Add($"❌  ERRORE {Path.GetFileName(da)}: {errore}"); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void SpostaCarto(string da, string a, bool dryRun) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (!AssetDatabase.IsValidFolder(da)) // se ok // riga-ok
        { // apre // riga-ok
            log.Add($"⚠️  Cartella non trovata (skip): {da}"); // chiama // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        // blocco: controlla se va
        if (dryRun) // se ok // riga-ok
        { // apre // riga-ok
            log.Add($"📁 Cartella: {da}\n   → {a}"); // chiama // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        string errore = AssetDatabase.MoveAsset(da, a); // setta // riga-ok
        // blocco: controlla se va
        if (string.IsNullOrEmpty(errore)) // se ok // riga-ok
            log.Add($"✅  Cartella spostata: {a}"); // chiama // riga-ok
        // blocco: caso diverso
        else // se no // riga-ok
            log.Add($"❌  ERRORE cartella {da}: {errore}"); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private void CreaCartellaSeNonEsiste(string path, bool dryRun) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (AssetDatabase.IsValidFolder(path)) return; // se ok // riga-ok

        string parent = Path.GetDirectoryName(path).Replace('\\', '/'); // setta // riga-ok
        string name   = Path.GetFileName(path); // setta // riga-ok

        // blocco: controlla se va
        if (dryRun) // se ok // riga-ok
        { // apre // riga-ok
            log.Add($"📁 Crea cartella: {path}"); // chiama // riga-ok
            return; // torna val // riga-ok
        } // chiude // riga-ok

        AssetDatabase.CreateFolder(parent, name); // chiama // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
#endif // prep ok // riga-ok
