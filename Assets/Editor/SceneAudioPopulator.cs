// ============================================================================
// Crisis Protocol / Sector Containment - Utility editor
// File: .\Assets\Editor\SceneAudioPopulator.cs
// Responsabilita': automatizza setup, popolamento scena, salvataggio, validazione o manutenzione direttamente dentro Unity Editor.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
#if UNITY_EDITOR // prep ok // riga-ok
using UnityEditor; // usa lib // riga-ok
using UnityEngine; // usa lib // riga-ok
using System.Collections.Generic; // usa lib // riga-ok

// blocco: classe x roba grossa
public class SceneAudioPopulator : EditorWindow // classe qui // riga-ok
{ // apre // riga-ok
    private const string SOUNDS_ROOT = "Assets/Sounds"; // roba pub // riga-ok

    [MenuItem("CrisisProtocol/Applica Tutti i Suoni a Oggetti e Scena")] // nota unity // riga-ok
    [MenuItem("Tools/Applica Tutti i Suoni alla Scena (Audio Populator)")] // nota unity // riga-ok
    // blocco: funzione fa cose
    public static void ApplicaTuttiISuoni() // roba pub // riga-ok
    { // apre // riga-ok
        Debug.Log("<color=cyan>[AUDIO POPULATOR]</color> Inizio catalogazione suoni da Assets/Sounds e assegnazione a tutti gli oggetti della scena..."); // logga // riga-ok

        // 1. Carica la libreria dei suoni
        AudioClip clipComplete = CaricaClip("notification-process-complete-slava-pogorelsky-1-00-03.mp3"); // setta // riga-ok
        AudioClip clipRobotMove = CaricaClip("tunetank.com_robot-move.wav"); // setta // riga-ok
        AudioClip clipRobotFast = CaricaClip("tunetank.com_fast-robot-walking.wav"); // setta // riga-ok
        AudioClip clipRobotScan = CaricaClip("tunetank.com_scroll-robotic-hover.wav"); // setta // riga-ok

        // Ambient
        AudioClip clipAirConditioner = CaricaClip("Ambient/Air conditioner_Ambient.wav"); // setta // riga-ok
        AudioClip clipLightBuzz = CaricaClip("Ambient/Fluorescent light bulb buzz_2.wav"); // setta // riga-ok
        AudioClip clipRadioStatic = CaricaClip("Ambient/Radio_static.wav"); // setta // riga-ok
        AudioClip clipVentAirflow = CaricaClip("Ambient/Vent_airflow.wav"); // setta // riga-ok
        AudioClip clipDroneDoom = CaricaClip("Ambient/Drone_doom.wav"); // setta // riga-ok
        AudioClip clipSpookyAmbience = CaricaClip("Stingers and Spooky Triggers/Spooky Ambience.wav") ?? CaricaClip("Ambient/Creepy_ambience_3.wav"); // setta // riga-ok
        AudioClip clipDistantYell = CaricaClip("Ambient/Distant Yell_Echo and Reverb_2.wav"); // setta // riga-ok

        // Character
        AudioClip clipFootstepsWalk = CaricaClip("Character/Footsteps_walking.wav"); // setta // riga-ok
        AudioClip clipFootstepsRun = CaricaClip("Character/Footsteps_ running.wav"); // setta // riga-ok
        AudioClip clipKeysPickup = CaricaClip("Character/Keys_pick up.wav"); // setta // riga-ok
        AudioClip clipGasp = CaricaClip("Character/Gasp.wav"); // setta // riga-ok
        AudioClip clipWoosh = CaricaClip("Character/woosh.wav"); // setta // riga-ok
        AudioClip clipWoosh5 = CaricaClip("Character/woosh_5.wav"); // setta // riga-ok
        AudioClip clipSwoosh3 = CaricaClip("Character/Swoosh_3.wav"); // setta // riga-ok

        // House & Office
        AudioClip clipGarageDoor = CaricaClip("House & Office/Garage door.wav"); // setta // riga-ok
        AudioClip clipDoorLocked = CaricaClip("House & Office/Door_handle_jiggle_checking if locked.wav"); // setta // riga-ok
        AudioClip clipDoorSqueeky = CaricaClip("House & Office/Door_squeeky_2.wav"); // setta // riga-ok
        AudioClip clipSwitch = CaricaClip("House & Office/switch.wav"); // setta // riga-ok
        AudioClip clipTyping = CaricaClip("House & Office/Typing.wav"); // setta // riga-ok
        AudioClip clipDrawerOpen = CaricaClip("House & Office/Drawer_open.wav"); // setta // riga-ok

        // Liquids
        AudioClip clipBubbles = CaricaClip("Liquids/Bubbles.wav"); // setta // riga-ok
        AudioClip clipBubbleArt = CaricaClip("Liquids/Bubble_artificial.wav"); // setta // riga-ok
        AudioClip clipLiquidSlosh = CaricaClip("Liquids/Liquid_slosh.wav"); // setta // riga-ok
        AudioClip clipSpray = CaricaClip("Liquids/Spray.wav"); // setta // riga-ok

        // Monsters & Ghosts
        AudioClip clipRoboticBass = CaricaClip("Monsters & Ghosts/Robotic_bass.wav"); // setta // riga-ok
        AudioClip clipRoboticScream = CaricaClip("Monsters & Ghosts/Scream_Robotic.wav"); // setta // riga-ok
        AudioClip clipRoboticHiss = CaricaClip("Monsters & Ghosts/robotic_hiss.wav"); // setta // riga-ok
        AudioClip clipInjured = CaricaClip("Monsters & Ghosts/Injured.wav"); // setta // riga-ok
        AudioClip clipMonsterGrunt = CaricaClip("Monsters & Ghosts/Monster_grunt x2 (ghmmm).wav"); // setta // riga-ok

        // Stingers
        AudioClip clipHarmonizedTone = CaricaClip("Stingers and Spooky Triggers/Harmonized Tone_Pleasant but Spooky.wav"); // setta // riga-ok
        AudioClip clipMetalTwang = CaricaClip("Stingers and Spooky Triggers/Metal_twang.wav"); // setta // riga-ok

        int porteConfigurate = 0; // setta // riga-ok
        int terminaliConfigurati = 0; // setta // riga-ok
        int focolaiConfigurati = 0; // setta // riga-ok
        int keycardsConfigurate = 0; // setta // riga-ok
        int guardieConfigurate = 0; // setta // riga-ok
        int botConfigurati = 0; // setta // riga-ok
        int droniConfigurati = 0; // setta // riga-ok
        int playerConfigurati = 0; // setta // riga-ok
        int datapadConfigurati = 0; // setta // riga-ok
        int portaliConfigurati = 0; // setta // riga-ok
        int cancelliQuarantenaConfigurati = 0; // setta // riga-ok

        // 2. Porte (PortaSettore)
        PortaSettore[] tutteLePorte = Object.FindObjectsByType<PortaSettore>(FindObjectsSortMode.None); // setta // riga-ok
        // blocco: gira piu volte
        foreach (PortaSettore p in tutteLePorte) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (p == null) continue; // se ok // riga-ok
            SerializedObject so = new SerializedObject(p); // setta // riga-ok
            ImpostaSerializedProperty(so, "suonoApertura", clipGarageDoor ?? clipDoorSqueeky); // chiama // riga-ok
            ImpostaSerializedProperty(so, "suonoChiusura", clipGarageDoor ?? clipDoorSqueeky); // chiama // riga-ok
            ImpostaSerializedProperty(so, "suonoBloccata", clipDoorLocked); // chiama // riga-ok
            ImpostaSerializedProperty(so, "suonoSblocco", clipSwitch ?? clipComplete); // chiama // riga-ok
            so.ApplyModifiedProperties(); // chiama // riga-ok
            EditorUtility.SetDirty(p); // chiama // riga-ok
            porteConfigurate++; // ok qua // riga-ok
        } // chiude // riga-ok

        // 3. Terminali di Sicurezza (TerminalePorta)
        TerminalePorta[] tuttiITerminali = Object.FindObjectsByType<TerminalePorta>(FindObjectsSortMode.None); // setta // riga-ok
        // blocco: gira piu volte
        foreach (TerminalePorta t in tuttiITerminali) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (t == null) continue; // se ok // riga-ok
            SerializedObject so = new SerializedObject(t); // setta // riga-ok
            ImpostaSerializedProperty(so, "suonoInterazione", clipTyping ?? clipSwitch); // chiama // riga-ok
            ImpostaSerializedProperty(so, "suonoAccessoGarantito", clipComplete ?? clipHarmonizedTone); // chiama // riga-ok
            ImpostaSerializedProperty(so, "suonoAccessoNegato", clipDoorLocked); // chiama // riga-ok
            so.ApplyModifiedProperties(); // chiama // riga-ok
            EditorUtility.SetDirty(t); // chiama // riga-ok
            terminaliConfigurati++; // ok qua // riga-ok
        } // chiude // riga-ok

        // 4. Focolai di Emergenza (EmergencyHotspot)
        AudioClip clipGasFurnace = CaricaClip("House & Office/Gas funace_running.wav") ?? CaricaClip("House & Office/Gas Stove_running.wav") ?? clipSpray; // setta // riga-ok
        EmergencyHotspot[] tuttiIFocolai = Object.FindObjectsByType<EmergencyHotspot>(FindObjectsSortMode.None); // setta // riga-ok
        // blocco: gira piu volte
        foreach (EmergencyHotspot h in tuttiIFocolai) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (h == null) continue; // se ok // riga-ok
            SerializedObject so = new SerializedObject(h); // setta // riga-ok
            string n = h.name.ToLower(); // setta // riga-ok
            AudioClip loopClip = clipSpray ?? clipLiquidSlosh; // setta // riga-ok
            // blocco: controlla se va
            if (n.Contains("gas") || n.Contains("steam") || n.Contains("fumo") || h.HotspotId == "REACTOR_FAULT_002" || n.Contains("(1)")) // se ok // riga-ok
            { // apre // riga-ok
                loopClip = clipGasFurnace ?? clipSpray; // setta // riga-ok
            } // chiude // riga-ok
            // blocco: controlla se va
            else if (n.Contains("elettr") || n.Contains("spark") || n.Contains("generat") || n.Contains("volt")) // se ok // riga-ok
            { // apre // riga-ok
                loopClip = clipLightBuzz ?? clipSpray; // setta // riga-ok
            } // chiude // riga-ok
            ImpostaSerializedProperty(so, "suonoLoopGuasto", loopClip); // chiama // riga-ok
            ImpostaSerializedProperty(so, "suonoRiparazione", clipComplete ?? clipHarmonizedTone); // chiama // riga-ok
            so.ApplyModifiedProperties(); // chiama // riga-ok
            EditorUtility.SetDirty(h); // chiama // riga-ok
            focolaiConfigurati++; // ok qua // riga-ok
        } // chiude // riga-ok

        // 5. Credenziali / Keycard (AccessCredentialPickup)
        AccessCredentialPickup[] tutteLeKeycard = Object.FindObjectsByType<AccessCredentialPickup>(FindObjectsSortMode.None); // setta // riga-ok
        // blocco: gira piu volte
        foreach (AccessCredentialPickup k in tutteLeKeycard) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (k == null) continue; // se ok // riga-ok
            SerializedObject so = new SerializedObject(k); // setta // riga-ok
            ImpostaSerializedProperty(so, "suonoRaccolta", clipKeysPickup ?? clipComplete); // chiama // riga-ok
            so.ApplyModifiedProperties(); // chiama // riga-ok
            EditorUtility.SetDirty(k); // chiama // riga-ok
            keycardsConfigurate++; // ok qua // riga-ok
        } // chiude // riga-ok

        // 6. Guardie Nemici (GuardiaNpc)
        GuardiaNpc[] tutteLeGuardie = Object.FindObjectsByType<GuardiaNpc>(FindObjectsSortMode.None); // setta // riga-ok
        // blocco: gira piu volte
        foreach (GuardiaNpc g in tutteLeGuardie) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (g == null) continue; // se ok // riga-ok
            SerializedObject so = new SerializedObject(g); // setta // riga-ok
            ImpostaSerializedProperty(so, "suonoPassi", clipFootstepsWalk); // chiama // riga-ok
            ImpostaSerializedProperty(so, "suonoCorsa", clipFootstepsRun); // chiama // riga-ok
            ImpostaSerializedProperty(so, "suonoAllarme", clipRadioStatic ?? clipGasp); // chiama // riga-ok
            ImpostaSerializedProperty(so, "suonoAttacco", clipWoosh ?? clipSwoosh3); // chiama // riga-ok
            ImpostaSerializedProperty(so, "suonoDanno", clipInjured ?? clipGasp); // chiama // riga-ok
            ImpostaSerializedProperty(so, "suonoMorte", clipMonsterGrunt ?? clipInjured); // chiama // riga-ok
            so.ApplyModifiedProperties(); // chiama // riga-ok
            EditorUtility.SetDirty(g); // chiama // riga-ok
            guardieConfigurate++; // ok qua // riga-ok
        } // chiude // riga-ok

        // 7. Bot di Manutenzione (ManutenzioneBot)
        ManutenzioneBot[] tuttiIBot = Object.FindObjectsByType<ManutenzioneBot>(FindObjectsSortMode.None); // setta // riga-ok
        // blocco: gira piu volte
        foreach (ManutenzioneBot b in tuttiIBot) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (b == null) continue; // se ok // riga-ok
            SerializedObject so = new SerializedObject(b); // setta // riga-ok
            ImpostaSerializedProperty(so, "suonoMovimentoLoop", clipRobotMove ?? clipRobotFast); // chiama // riga-ok
            ImpostaSerializedProperty(so, "suonoAvvistamento", clipRobotScan); // chiama // riga-ok
            ImpostaSerializedProperty(so, "suonoSparo", clipRoboticBass ?? clipMetalTwang); // chiama // riga-ok
            ImpostaSerializedProperty(so, "suonoDanno", clipRoboticHiss ?? clipInjured); // chiama // riga-ok
            ImpostaSerializedProperty(so, "suonoMorte", clipRoboticScream); // chiama // riga-ok
            so.ApplyModifiedProperties(); // chiama // riga-ok
            EditorUtility.SetDirty(b); // chiama // riga-ok
            botConfigurati++; // ok qua // riga-ok
        } // chiude // riga-ok

        // 8. Droni di Ronda (DroneRonda)
        DroneRonda[] tuttiIDroni = Object.FindObjectsByType<DroneRonda>(FindObjectsSortMode.None); // setta // riga-ok
        // blocco: gira piu volte
        foreach (DroneRonda d in tuttiIDroni) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (d == null) continue; // se ok // riga-ok
            SerializedObject so = new SerializedObject(d); // setta // riga-ok
            ImpostaSerializedProperty(so, "suonoHoverLoop", clipDroneDoom); // chiama // riga-ok
            ImpostaSerializedProperty(so, "suonoAllarme", clipRobotScan); // chiama // riga-ok
            ImpostaSerializedProperty(so, "suonoSparo", clipRoboticBass ?? clipMetalTwang); // chiama // riga-ok
            so.ApplyModifiedProperties(); // chiama // riga-ok
            EditorUtility.SetDirty(d); // chiama // riga-ok
            droniConfigurati++; // ok qua // riga-ok
        } // chiude // riga-ok

        // 9. Giocatore (muve_pg, AttaccoPlayer, SparoPlayer, SalutePlayer)
        muve_pg[] tuttiIMovimenti = Object.FindObjectsByType<muve_pg>(FindObjectsSortMode.None); // setta // riga-ok
        // blocco: gira piu volte
        foreach (muve_pg m in tuttiIMovimenti) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (m == null) continue; // se ok // riga-ok
            SerializedObject so = new SerializedObject(m); // setta // riga-ok
            ImpostaSerializedProperty(so, "suonoPassi", clipFootstepsWalk); // chiama // riga-ok
            ImpostaSerializedProperty(so, "suonoCorsa", clipFootstepsRun); // chiama // riga-ok
            ImpostaSerializedProperty(so, "suonoPrimoSalto", clipWoosh5 ?? clipWoosh); // chiama // riga-ok
            ImpostaSerializedProperty(so, "suonoSecondoSalto", clipWoosh); // chiama // riga-ok
            so.ApplyModifiedProperties(); // chiama // riga-ok
            EditorUtility.SetDirty(m); // chiama // riga-ok
            playerConfigurati++; // ok qua // riga-ok
        } // chiude // riga-ok

        AttaccoPlayer[] tuttiGliAttacchi = Object.FindObjectsByType<AttaccoPlayer>(FindObjectsSortMode.None); // setta // riga-ok
        // blocco: gira piu volte
        foreach (AttaccoPlayer a in tuttiGliAttacchi) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (a == null) continue; // se ok // riga-ok
            SerializedObject so = new SerializedObject(a); // setta // riga-ok
            ImpostaSerializedProperty(so, "suonoAttacco", clipSwoosh3 ?? clipWoosh); // chiama // riga-ok
            ImpostaSerializedProperty(so, "suonoColpoASegno", clipRoboticBass ?? clipWoosh5); // chiama // riga-ok
            so.ApplyModifiedProperties(); // chiama // riga-ok
            EditorUtility.SetDirty(a); // chiama // riga-ok
        } // chiude // riga-ok

        SparoPlayer[] tuttiGliSpari = Object.FindObjectsByType<SparoPlayer>(FindObjectsSortMode.None); // setta // riga-ok
        // blocco: gira piu volte
        foreach (SparoPlayer sp in tuttiGliSpari) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (sp == null) continue; // se ok // riga-ok
            SerializedObject so = new SerializedObject(sp); // setta // riga-ok
            ImpostaSerializedProperty(so, "suonoSparo", clipMetalTwang ?? clipRoboticBass); // chiama // riga-ok
            ImpostaSerializedProperty(so, "suonoVuoto", clipSwitch); // chiama // riga-ok
            so.ApplyModifiedProperties(); // chiama // riga-ok
            EditorUtility.SetDirty(sp); // chiama // riga-ok
        } // chiude // riga-ok

        SalutePlayer[] tutteLeSaluti = Object.FindObjectsByType<SalutePlayer>(FindObjectsSortMode.None); // setta // riga-ok
        // blocco: gira piu volte
        foreach (SalutePlayer s in tutteLeSaluti) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (s == null) continue; // se ok // riga-ok
            SerializedObject so = new SerializedObject(s); // setta // riga-ok
            ImpostaSerializedProperty(so, "suonoDanno", clipGasp ?? clipInjured); // chiama // riga-ok
            ImpostaSerializedProperty(so, "suonoMorte", clipDistantYell ?? clipMonsterGrunt); // chiama // riga-ok
            ImpostaSerializedProperty(so, "suonoCura", clipComplete); // chiama // riga-ok
            so.ApplyModifiedProperties(); // chiama // riga-ok
            EditorUtility.SetDirty(s); // chiama // riga-ok
        } // chiude // riga-ok

        // 10. Datapad Olografici (DatapadCodiciPorte)
        DatapadCodiciPorte[] tuttiIDatapad = Object.FindObjectsByType<DatapadCodiciPorte>(FindObjectsSortMode.None); // setta // riga-ok
        // blocco: gira piu volte
        foreach (DatapadCodiciPorte dp in tuttiIDatapad) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (dp == null) continue; // se ok // riga-ok
            SerializedObject so = new SerializedObject(dp); // setta // riga-ok
            ImpostaSerializedProperty(so, "suonoApertura", clipTyping ?? clipDrawerOpen); // chiama // riga-ok
            so.ApplyModifiedProperties(); // chiama // riga-ok
            EditorUtility.SetDirty(dp); // chiama // riga-ok
            datapadConfigurati++; // ok qua // riga-ok
        } // chiude // riga-ok

        // 11. Portali / Cubo Nero (CuboNeroTeletrasporto)
        CuboNeroTeletrasporto[] tuttiIPortali = Object.FindObjectsByType<CuboNeroTeletrasporto>(FindObjectsSortMode.None); // setta // riga-ok
        // blocco: gira piu volte
        foreach (CuboNeroTeletrasporto cub in tuttiIPortali) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (cub == null) continue; // se ok // riga-ok
            SerializedObject so = new SerializedObject(cub); // setta // riga-ok
            ImpostaSerializedProperty(so, "suonoTeletrasporto", clipHarmonizedTone ?? clipComplete); // chiama // riga-ok
            so.ApplyModifiedProperties(); // chiama // riga-ok
            EditorUtility.SetDirty(cub); // chiama // riga-ok
            portaliConfigurati++; // ok qua // riga-ok
        } // chiude // riga-ok

        // 12. Cancelli Quarantena (QuarantineGate)
        QuarantineGate[] tuttiICancelli = Object.FindObjectsByType<QuarantineGate>(FindObjectsSortMode.None); // setta // riga-ok
        // blocco: gira piu volte
        foreach (QuarantineGate qg in tuttiICancelli) // ciclo x // riga-ok
        { // apre // riga-ok
            // blocco: controlla se va
            if (qg == null) continue; // se ok // riga-ok
            SerializedObject so = new SerializedObject(qg); // setta // riga-ok
            ImpostaSerializedProperty(so, "suonoSblocco", clipComplete ?? clipHarmonizedTone); // chiama // riga-ok
            ImpostaSerializedProperty(so, "suonoInterazione", clipGarageDoor ?? clipDoorSqueeky); // chiama // riga-ok
            so.ApplyModifiedProperties(); // chiama // riga-ok
            EditorUtility.SetDirty(qg); // chiama // riga-ok
            cancelliQuarantenaConfigurati++; // ok qua // riga-ok
        } // chiude // riga-ok

        // 13. Generazione e Configurazione Sistema Audio Ambientale della Scena
        SceneAudioAmbience ambience = Object.FindAnyObjectByType<SceneAudioAmbience>(); // setta // riga-ok
        // blocco: controlla se va
        if (ambience == null) // se ok // riga-ok
        { // apre // riga-ok
            GameObject ambGO = GameObject.Find("[AUDIO_AMBIENTALE_SCENA]"); // setta // riga-ok
            // blocco: controlla se va
            if (ambGO == null) // se ok // riga-ok
            { // apre // riga-ok
                ambGO = new GameObject("[AUDIO_AMBIENTALE_SCENA]"); // setta // riga-ok
                Undo.RegisterCreatedObjectUndo(ambGO, "Crea [AUDIO_AMBIENTALE_SCENA]"); // chiama // riga-ok
            } // chiude // riga-ok
            ambience = ambGO.GetComponent<SceneAudioAmbience>() ?? ambGO.AddComponent<SceneAudioAmbience>(); // setta // riga-ok
        } // chiude // riga-ok

        // Trova i riferimenti delle zone nella scena per posizionare gli emettitori 3D
        GameObject zonaRossa = GameObject.Find("[ZONA_ROSSA_SALA_GENERATORE]"); // setta // riga-ok
        GameObject zonaVerde = GameObject.Find("[ZONA_VERDE_LABORATORIO_CHIMICO]"); // setta // riga-ok
        GameObject corridoi = GameObject.Find("[CORRIDOI_E_STRUTTURA_ESTERNA]"); // setta // riga-ok

        ambience.musicaAtmosferaGlobale = clipSpookyAmbience; // setta // riga-ok
        ambience.suonoGeneratoreIndustriale = clipAirConditioner; // setta // riga-ok
        ambience.suonoRonzioElettrico = clipLightBuzz; // setta // riga-ok
        ambience.posizioneSalaGeneratore = (zonaRossa != null) ? zonaRossa.transform : null; // setta // riga-ok

        ambience.suonoRibollioChimico = clipBubbles ?? clipBubbleArt; // setta // riga-ok
        ambience.suonoLiquidiLaboratorio = clipLiquidSlosh; // setta // riga-ok
        ambience.posizioneLaboratorioChimico = (zonaVerde != null) ? zonaVerde.transform : null; // setta // riga-ok

        ambience.suonoVentilazioneCorridoi = clipVentAirflow; // setta // riga-ok
        ambience.posizioneCorridoi = (corridoi != null) ? corridoi.transform : null; // setta // riga-ok

        ambience.InizializzaTuttiGliAudioSource(); // chiama // riga-ok
        EditorUtility.SetDirty(ambience.gameObject); // chiama // riga-ok

        // Salva la scena
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene()); // chiama // riga-ok

        Debug.Log("<color=green><b>[AUDIO POPULATOR] CONFIGURAZIONE AUDIO COMPLETATA CON SUCCESSO!</b></color>"); // logga // riga-ok

        EditorUtility.DisplayDialog( // ok qua // riga-ok
            "Crisis Protocol - Suoni Applicati", // ok qua // riga-ok
            $"Tutti i suoni sono stati collegati con successo agli oggetti e alla scena!\n\n" + // ok qua // riga-ok
            $"- Porte e Vetrati (Open, Close, Lock, Unlock): {porteConfigurate}\n" + // ok qua // riga-ok
            $"- Terminali di Sicurezza (Typing, Success, Denied): {terminaliConfigurati}\n" + // ok qua // riga-ok
            $"- Focolai Emergenza (3D Chemical Leak / Spark Loop + Chime): {focolaiConfigurati}\n" + // ok qua // riga-ok
            $"- Schede Keycard (Pickup sound): {keycardsConfigurate}\n" + // ok qua // riga-ok
            $"- Guardie NPC (Passi, Allarme radio, Fendenti, Morte): {guardieConfigurate}\n" + // ok qua // riga-ok
            $"- Bot Manutenzione (Servomotore 3D, Allarme, Sparo plasma, Morte): {botConfigurati}\n" + // ok qua // riga-ok
            $"- Droni di Ronda (Hover loop, Laser shot): {droniConfigurati}\n" + // ok qua // riga-ok
            $"- Giocatore (Passi camminata/corsa, Salti, Colpi melee, Spari, Danni/Morte): {playerConfigurati}\n" + // ok qua // riga-ok
            $"- Datapad e Terminali Olografici: {datapadConfigurati}\n" + // ok qua // riga-ok
            $"- Portale Cubo Nero & Uscite Quarantena: {portaliConfigurati + cancelliQuarantenaConfigurati}\n" + // ok qua // riga-ok
            $"- Sistema Audio Ambientale Scena (2D Tense Background + 3D Zona Rossa Generatore, 3D Zona Verde Chimica, 3D Corridoi)\n\n" + // ok qua // riga-ok
            $"Tutto è pronto e calibrato in 3D!", // ok qua // riga-ok
            "OK" // ok qua // riga-ok
        ); // chiama // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private static AudioClip CaricaClip(string percorsoRelativo) // roba pub // riga-ok
    { // apre // riga-ok
        string fullPath = $"{SOUNDS_ROOT}/{percorsoRelativo}"; // setta // riga-ok
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(fullPath); // setta // riga-ok
        // blocco: controlla se va
        if (clip == null) // se ok // riga-ok
        { // apre // riga-ok
            // Cerca in tutto il database degli asset con il nome file
            string fileName = System.IO.Path.GetFileNameWithoutExtension(percorsoRelativo); // setta // riga-ok
            string[] guids = AssetDatabase.FindAssets($"{fileName} t:AudioClip"); // setta // riga-ok
            // blocco: controlla se va
            if (guids != null && guids.Length > 0) // se ok // riga-ok
            { // apre // riga-ok
                string path = AssetDatabase.GUIDToAssetPath(guids[0]); // setta // riga-ok
                clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path); // setta // riga-ok
            } // chiude // riga-ok
        } // chiude // riga-ok
        return clip; // torna val // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private static void ImpostaSerializedProperty(SerializedObject so, string propertyName, AudioClip clip) // roba pub // riga-ok
    { // apre // riga-ok
        // blocco: controlla se va
        if (clip == null) return; // se ok // riga-ok
        SerializedProperty prop = so.FindProperty(propertyName); // setta // riga-ok
        // blocco: controlla se va
        if (prop != null) // se ok // riga-ok
        { // apre // riga-ok
            prop.objectReferenceValue = clip; // setta // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
#endif // prep ok // riga-ok
