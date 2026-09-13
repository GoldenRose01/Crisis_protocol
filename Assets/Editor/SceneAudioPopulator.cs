#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class SceneAudioPopulator : EditorWindow
{
    private const string SOUNDS_ROOT = "Assets/Sounds";

    [MenuItem("CrisisProtocol/Applica Tutti i Suoni a Oggetti e Scena")]
    [MenuItem("Tools/Applica Tutti i Suoni alla Scena (Audio Populator)")]
    public static void ApplicaTuttiISuoni()
    {
        Debug.Log("<color=cyan>[AUDIO POPULATOR]</color> Inizio catalogazione suoni da Assets/Sounds e assegnazione a tutti gli oggetti della scena...");

        // 1. Carica la libreria dei suoni
        AudioClip clipComplete = CaricaClip("notification-process-complete-slava-pogorelsky-1-00-03.mp3");
        AudioClip clipRobotMove = CaricaClip("tunetank.com_robot-move.wav");
        AudioClip clipRobotFast = CaricaClip("tunetank.com_fast-robot-walking.wav");
        AudioClip clipRobotScan = CaricaClip("tunetank.com_scroll-robotic-hover.wav");

        // Ambient
        AudioClip clipAirConditioner = CaricaClip("Ambient/Air conditioner_Ambient.wav");
        AudioClip clipLightBuzz = CaricaClip("Ambient/Fluorescent light bulb buzz_2.wav");
        AudioClip clipRadioStatic = CaricaClip("Ambient/Radio_static.wav");
        AudioClip clipVentAirflow = CaricaClip("Ambient/Vent_airflow.wav");
        AudioClip clipDroneDoom = CaricaClip("Ambient/Drone_doom.wav");
        AudioClip clipSpookyAmbience = CaricaClip("Stingers and Spooky Triggers/Spooky Ambience.wav") ?? CaricaClip("Ambient/Creepy_ambience_3.wav");
        AudioClip clipDistantYell = CaricaClip("Ambient/Distant Yell_Echo and Reverb_2.wav");

        // Character
        AudioClip clipFootstepsWalk = CaricaClip("Character/Footsteps_walking.wav");
        AudioClip clipFootstepsRun = CaricaClip("Character/Footsteps_ running.wav");
        AudioClip clipKeysPickup = CaricaClip("Character/Keys_pick up.wav");
        AudioClip clipGasp = CaricaClip("Character/Gasp.wav");
        AudioClip clipWoosh = CaricaClip("Character/woosh.wav");
        AudioClip clipWoosh5 = CaricaClip("Character/woosh_5.wav");
        AudioClip clipSwoosh3 = CaricaClip("Character/Swoosh_3.wav");

        // House & Office
        AudioClip clipGarageDoor = CaricaClip("House & Office/Garage door.wav");
        AudioClip clipDoorLocked = CaricaClip("House & Office/Door_handle_jiggle_checking if locked.wav");
        AudioClip clipDoorSqueeky = CaricaClip("House & Office/Door_squeeky_2.wav");
        AudioClip clipSwitch = CaricaClip("House & Office/switch.wav");
        AudioClip clipTyping = CaricaClip("House & Office/Typing.wav");
        AudioClip clipDrawerOpen = CaricaClip("House & Office/Drawer_open.wav");

        // Liquids
        AudioClip clipBubbles = CaricaClip("Liquids/Bubbles.wav");
        AudioClip clipBubbleArt = CaricaClip("Liquids/Bubble_artificial.wav");
        AudioClip clipLiquidSlosh = CaricaClip("Liquids/Liquid_slosh.wav");
        AudioClip clipSpray = CaricaClip("Liquids/Spray.wav");

        // Monsters & Ghosts
        AudioClip clipRoboticBass = CaricaClip("Monsters & Ghosts/Robotic_bass.wav");
        AudioClip clipRoboticScream = CaricaClip("Monsters & Ghosts/Scream_Robotic.wav");
        AudioClip clipRoboticHiss = CaricaClip("Monsters & Ghosts/robotic_hiss.wav");
        AudioClip clipInjured = CaricaClip("Monsters & Ghosts/Injured.wav");
        AudioClip clipMonsterGrunt = CaricaClip("Monsters & Ghosts/Monster_grunt x2 (ghmmm).wav");

        // Stingers
        AudioClip clipHarmonizedTone = CaricaClip("Stingers and Spooky Triggers/Harmonized Tone_Pleasant but Spooky.wav");
        AudioClip clipMetalTwang = CaricaClip("Stingers and Spooky Triggers/Metal_twang.wav");

        int porteConfigurate = 0;
        int terminaliConfigurati = 0;
        int focolaiConfigurati = 0;
        int keycardsConfigurate = 0;
        int guardieConfigurate = 0;
        int botConfigurati = 0;
        int droniConfigurati = 0;
        int playerConfigurati = 0;
        int datapadConfigurati = 0;
        int portaliConfigurati = 0;
        int cancelliQuarantenaConfigurati = 0;

        // 2. Porte (PortaSettore)
        PortaSettore[] tutteLePorte = Object.FindObjectsByType<PortaSettore>(FindObjectsSortMode.None);
        foreach (PortaSettore p in tutteLePorte)
        {
            if (p == null) continue;
            SerializedObject so = new SerializedObject(p);
            ImpostaSerializedProperty(so, "suonoApertura", clipGarageDoor ?? clipDoorSqueeky);
            ImpostaSerializedProperty(so, "suonoChiusura", clipGarageDoor ?? clipDoorSqueeky);
            ImpostaSerializedProperty(so, "suonoBloccata", clipDoorLocked);
            ImpostaSerializedProperty(so, "suonoSblocco", clipSwitch ?? clipComplete);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(p);
            porteConfigurate++;
        }

        // 3. Terminali di Sicurezza (TerminalePorta)
        TerminalePorta[] tuttiITerminali = Object.FindObjectsByType<TerminalePorta>(FindObjectsSortMode.None);
        foreach (TerminalePorta t in tuttiITerminali)
        {
            if (t == null) continue;
            SerializedObject so = new SerializedObject(t);
            ImpostaSerializedProperty(so, "suonoInterazione", clipTyping ?? clipSwitch);
            ImpostaSerializedProperty(so, "suonoAccessoGarantito", clipComplete ?? clipHarmonizedTone);
            ImpostaSerializedProperty(so, "suonoAccessoNegato", clipDoorLocked);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(t);
            terminaliConfigurati++;
        }

        // 4. Focolai di Emergenza (EmergencyHotspot)
        EmergencyHotspot[] tuttiIFocolai = Object.FindObjectsByType<EmergencyHotspot>(FindObjectsSortMode.None);
        foreach (EmergencyHotspot h in tuttiIFocolai)
        {
            if (h == null) continue;
            SerializedObject so = new SerializedObject(h);
            string n = h.name.ToLower();
            AudioClip loopClip = clipSpray ?? clipLiquidSlosh;
            if (n.Contains("elettr") || n.Contains("spark") || n.Contains("generat") || n.Contains("volt"))
            {
                loopClip = clipLightBuzz ?? clipSpray;
            }
            ImpostaSerializedProperty(so, "suonoLoopGuasto", loopClip);
            ImpostaSerializedProperty(so, "suonoRiparazione", clipComplete ?? clipHarmonizedTone);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(h);
            focolaiConfigurati++;
        }

        // 5. Credenziali / Keycard (AccessCredentialPickup)
        AccessCredentialPickup[] tutteLeKeycard = Object.FindObjectsByType<AccessCredentialPickup>(FindObjectsSortMode.None);
        foreach (AccessCredentialPickup k in tutteLeKeycard)
        {
            if (k == null) continue;
            SerializedObject so = new SerializedObject(k);
            ImpostaSerializedProperty(so, "suonoRaccolta", clipKeysPickup ?? clipComplete);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(k);
            keycardsConfigurate++;
        }

        // 6. Guardie Nemici (GuardiaNpc)
        GuardiaNpc[] tutteLeGuardie = Object.FindObjectsByType<GuardiaNpc>(FindObjectsSortMode.None);
        foreach (GuardiaNpc g in tutteLeGuardie)
        {
            if (g == null) continue;
            SerializedObject so = new SerializedObject(g);
            ImpostaSerializedProperty(so, "suonoPassi", clipFootstepsWalk);
            ImpostaSerializedProperty(so, "suonoCorsa", clipFootstepsRun);
            ImpostaSerializedProperty(so, "suonoAllarme", clipRadioStatic ?? clipGasp);
            ImpostaSerializedProperty(so, "suonoAttacco", clipWoosh ?? clipSwoosh3);
            ImpostaSerializedProperty(so, "suonoDanno", clipInjured ?? clipGasp);
            ImpostaSerializedProperty(so, "suonoMorte", clipMonsterGrunt ?? clipInjured);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(g);
            guardieConfigurate++;
        }

        // 7. Bot di Manutenzione (ManutenzioneBot)
        ManutenzioneBot[] tuttiIBot = Object.FindObjectsByType<ManutenzioneBot>(FindObjectsSortMode.None);
        foreach (ManutenzioneBot b in tuttiIBot)
        {
            if (b == null) continue;
            SerializedObject so = new SerializedObject(b);
            ImpostaSerializedProperty(so, "suonoMovimentoLoop", clipRobotMove ?? clipRobotFast);
            ImpostaSerializedProperty(so, "suonoAvvistamento", clipRobotScan);
            ImpostaSerializedProperty(so, "suonoSparo", clipRoboticBass ?? clipMetalTwang);
            ImpostaSerializedProperty(so, "suonoDanno", clipRoboticHiss ?? clipInjured);
            ImpostaSerializedProperty(so, "suonoMorte", clipRoboticScream);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(b);
            botConfigurati++;
        }

        // 8. Droni di Ronda (DroneRonda)
        DroneRonda[] tuttiIDroni = Object.FindObjectsByType<DroneRonda>(FindObjectsSortMode.None);
        foreach (DroneRonda d in tuttiIDroni)
        {
            if (d == null) continue;
            SerializedObject so = new SerializedObject(d);
            ImpostaSerializedProperty(so, "suonoHoverLoop", clipDroneDoom);
            ImpostaSerializedProperty(so, "suonoAllarme", clipRobotScan);
            ImpostaSerializedProperty(so, "suonoSparo", clipRoboticBass ?? clipMetalTwang);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(d);
            droniConfigurati++;
        }

        // 9. Giocatore (muve_pg, AttaccoPlayer, SparoPlayer, SalutePlayer)
        muve_pg[] tuttiIMovimenti = Object.FindObjectsByType<muve_pg>(FindObjectsSortMode.None);
        foreach (muve_pg m in tuttiIMovimenti)
        {
            if (m == null) continue;
            SerializedObject so = new SerializedObject(m);
            ImpostaSerializedProperty(so, "suonoPassi", clipFootstepsWalk);
            ImpostaSerializedProperty(so, "suonoCorsa", clipFootstepsRun);
            ImpostaSerializedProperty(so, "suonoPrimoSalto", clipWoosh5 ?? clipWoosh);
            ImpostaSerializedProperty(so, "suonoSecondoSalto", clipWoosh);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(m);
            playerConfigurati++;
        }

        AttaccoPlayer[] tuttiGliAttacchi = Object.FindObjectsByType<AttaccoPlayer>(FindObjectsSortMode.None);
        foreach (AttaccoPlayer a in tuttiGliAttacchi)
        {
            if (a == null) continue;
            SerializedObject so = new SerializedObject(a);
            ImpostaSerializedProperty(so, "suonoAttacco", clipSwoosh3 ?? clipWoosh);
            ImpostaSerializedProperty(so, "suonoColpoASegno", clipRoboticBass ?? clipWoosh5);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(a);
        }

        SparoPlayer[] tuttiGliSpari = Object.FindObjectsByType<SparoPlayer>(FindObjectsSortMode.None);
        foreach (SparoPlayer sp in tuttiGliSpari)
        {
            if (sp == null) continue;
            SerializedObject so = new SerializedObject(sp);
            ImpostaSerializedProperty(so, "suonoSparo", clipMetalTwang ?? clipRoboticBass);
            ImpostaSerializedProperty(so, "suonoVuoto", clipSwitch);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(sp);
        }

        SalutePlayer[] tutteLeSaluti = Object.FindObjectsByType<SalutePlayer>(FindObjectsSortMode.None);
        foreach (SalutePlayer s in tutteLeSaluti)
        {
            if (s == null) continue;
            SerializedObject so = new SerializedObject(s);
            ImpostaSerializedProperty(so, "suonoDanno", clipGasp ?? clipInjured);
            ImpostaSerializedProperty(so, "suonoMorte", clipDistantYell ?? clipMonsterGrunt);
            ImpostaSerializedProperty(so, "suonoCura", clipComplete);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(s);
        }

        // 10. Datapad Olografici (DatapadCodiciPorte)
        DatapadCodiciPorte[] tuttiIDatapad = Object.FindObjectsByType<DatapadCodiciPorte>(FindObjectsSortMode.None);
        foreach (DatapadCodiciPorte dp in tuttiIDatapad)
        {
            if (dp == null) continue;
            SerializedObject so = new SerializedObject(dp);
            ImpostaSerializedProperty(so, "suonoApertura", clipTyping ?? clipDrawerOpen);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(dp);
            datapadConfigurati++;
        }

        // 11. Portali / Cubo Nero (CuboNeroTeletrasporto)
        CuboNeroTeletrasporto[] tuttiIPortali = Object.FindObjectsByType<CuboNeroTeletrasporto>(FindObjectsSortMode.None);
        foreach (CuboNeroTeletrasporto cub in tuttiIPortali)
        {
            if (cub == null) continue;
            SerializedObject so = new SerializedObject(cub);
            ImpostaSerializedProperty(so, "suonoTeletrasporto", clipHarmonizedTone ?? clipComplete);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(cub);
            portaliConfigurati++;
        }

        // 12. Cancelli Quarantena (QuarantineGate)
        QuarantineGate[] tuttiICancelli = Object.FindObjectsByType<QuarantineGate>(FindObjectsSortMode.None);
        foreach (QuarantineGate qg in tuttiICancelli)
        {
            if (qg == null) continue;
            SerializedObject so = new SerializedObject(qg);
            ImpostaSerializedProperty(so, "suonoSblocco", clipComplete ?? clipHarmonizedTone);
            ImpostaSerializedProperty(so, "suonoInterazione", clipGarageDoor ?? clipDoorSqueeky);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(qg);
            cancelliQuarantenaConfigurati++;
        }

        // 13. Generazione e Configurazione Sistema Audio Ambientale della Scena
        SceneAudioAmbience ambience = Object.FindAnyObjectByType<SceneAudioAmbience>();
        if (ambience == null)
        {
            GameObject ambGO = GameObject.Find("[AUDIO_AMBIENTALE_SCENA]");
            if (ambGO == null)
            {
                ambGO = new GameObject("[AUDIO_AMBIENTALE_SCENA]");
                Undo.RegisterCreatedObjectUndo(ambGO, "Crea [AUDIO_AMBIENTALE_SCENA]");
            }
            ambience = ambGO.GetComponent<SceneAudioAmbience>() ?? ambGO.AddComponent<SceneAudioAmbience>();
        }

        // Trova i riferimenti delle zone nella scena per posizionare gli emettitori 3D
        GameObject zonaRossa = GameObject.Find("[ZONA_ROSSA_SALA_GENERATORE]");
        GameObject zonaVerde = GameObject.Find("[ZONA_VERDE_LABORATORIO_CHIMICO]");
        GameObject corridoi = GameObject.Find("[CORRIDOI_E_STRUTTURA_ESTERNA]");

        ambience.musicaAtmosferaGlobale = clipSpookyAmbience;
        ambience.suonoGeneratoreIndustriale = clipAirConditioner;
        ambience.suonoRonzioElettrico = clipLightBuzz;
        ambience.posizioneSalaGeneratore = (zonaRossa != null) ? zonaRossa.transform : null;

        ambience.suonoRibollioChimico = clipBubbles ?? clipBubbleArt;
        ambience.suonoLiquidiLaboratorio = clipLiquidSlosh;
        ambience.posizioneLaboratorioChimico = (zonaVerde != null) ? zonaVerde.transform : null;

        ambience.suonoVentilazioneCorridoi = clipVentAirflow;
        ambience.posizioneCorridoi = (corridoi != null) ? corridoi.transform : null;

        ambience.InizializzaTuttiGliAudioSource();
        EditorUtility.SetDirty(ambience.gameObject);

        // Salva la scena
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("<color=green><b>[AUDIO POPULATOR] CONFIGURAZIONE AUDIO COMPLETATA CON SUCCESSO!</b></color>");

        EditorUtility.DisplayDialog(
            "Crisis Protocol - Suoni Applicati",
            $"Tutti i suoni sono stati collegati con successo agli oggetti e alla scena!\n\n" +
            $"- Porte e Vetrati (Open, Close, Lock, Unlock): {porteConfigurate}\n" +
            $"- Terminali di Sicurezza (Typing, Success, Denied): {terminaliConfigurati}\n" +
            $"- Focolai Emergenza (3D Chemical Leak / Spark Loop + Chime): {focolaiConfigurati}\n" +
            $"- Schede Keycard (Pickup sound): {keycardsConfigurate}\n" +
            $"- Guardie NPC (Passi, Allarme radio, Fendenti, Morte): {guardieConfigurate}\n" +
            $"- Bot Manutenzione (Servomotore 3D, Allarme, Sparo plasma, Morte): {botConfigurati}\n" +
            $"- Droni di Ronda (Hover loop, Laser shot): {droniConfigurati}\n" +
            $"- Giocatore (Passi camminata/corsa, Salti, Colpi melee, Spari, Danni/Morte): {playerConfigurati}\n" +
            $"- Datapad e Terminali Olografici: {datapadConfigurati}\n" +
            $"- Portale Cubo Nero & Uscite Quarantena: {portaliConfigurati + cancelliQuarantenaConfigurati}\n" +
            $"- Sistema Audio Ambientale Scena (2D Tense Background + 3D Zona Rossa Generatore, 3D Zona Verde Chimica, 3D Corridoi)\n\n" +
            $"Tutto è pronto e calibrato in 3D!",
            "OK"
        );
    }

    private static AudioClip CaricaClip(string percorsoRelativo)
    {
        string fullPath = $"{SOUNDS_ROOT}/{percorsoRelativo}";
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(fullPath);
        if (clip == null)
        {
            // Cerca in tutto il database degli asset con il nome file
            string fileName = System.IO.Path.GetFileNameWithoutExtension(percorsoRelativo);
            string[] guids = AssetDatabase.FindAssets($"{fileName} t:AudioClip");
            if (guids != null && guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
        }
        return clip;
    }

    private static void ImpostaSerializedProperty(SerializedObject so, string propertyName, AudioClip clip)
    {
        if (clip == null) return;
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop != null)
        {
            prop.objectReferenceValue = clip;
        }
    }
}
#endif
