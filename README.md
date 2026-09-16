# Sector Containment: Emergency

Titolo alternativo: `Crisis Protocol: Red Line`.

Prototipo 3D realizzato in Unity per PC Windows. Il gioco e' un action-stealth sci-fi con componenti puzzle e gestione emergenze: il giocatore interpreta un operatore tecnico mandato in una struttura ad alta sicurezza durante una crisi a catena.

Il loop attuale e' centrato su esplorazione di settori, recupero credenziali, lettura di datapad, sblocco terminali, contenimento di focolai tecnici, evitamento di droni/guardie e apertura del portellone di estrazione a fine settore.

## Stato attuale

- Motore: Unity `6000.0.74f1`
- Render pipeline: Universal Render Pipeline `17.0.4`
- Input: Unity Input System `1.14.0`
- Target: PC Windows
- Build scenes attive:
  - `Assets/Scenes/MainMenu-Scene.unity`
  - `Assets/Scenes/settore 0.unity`
  - `Assets/Scenes/settore 1.unity`
  - `Assets/Scenes/settore 2.unity`
- Scene legacy disattivate in build:
  - `Assets/_Legacy/Scenes/locale.unity`
  - `Assets/_Legacy/Scenes/Passato_1961.unity`

Nota: la verifica completa va fatta da Unity generando una nuova build. Il controllo C# da terminale puo' non essere disponibile se sulla macchina e' installato solo il runtime .NET e non l'SDK.

## Cosa c'e' nel gioco

### Menu e flusso

- Menu principale neon con sfondo video/fallback visivo.
- Selezione livello per i settori disponibili.
- Avvio da `settore 0`.
- Progressione fino a `settore 2`.
- Schermata finale con crediti e pulsanti per nuova partita o menu principale.
- Schermata morte con pulsanti di retry/menu.

### Settori

- `settore 0`: introduzione e primo settore giocabile.
- `settore 1`: area con portellone di uscita, gas tossico, drone di ronda e focolai tecnici.
- `settore 2`: area server/reattore con porte tecniche, sala chimica, reattore, datapad e pericoli ambientali.

Ogni settore segue la stessa logica base:

1. entra nel settore;
2. esplora e identifica focolai, porte e terminali;
3. recupera credenziali o codici;
4. usa scanner/datapad/terminali;
5. contiene tutti i focolai richiesti;
6. apri il portellone finale solo quando la missione lo consente.

### HUD e controlli

- HUD cyber/neon con stato missione.
- Tutorial testuale apribile da tastiera con `F1`.
- Scanner ambientale per evidenziare/intercettare oggetti importanti.
- Mappa tattica dal menu pausa.
- Pause menu con `ESC`.
- Terminali PIN e bypass interattivi.
- Datapad olografico per codici porte.
- Cursor lock gestito tramite `ModalUIState` quando si aprono GUI.

Comandi principali:

```text
WASD / movimento configurato nel player
Mouse / visuale e attacco
E / interazione
Q / scanner
F1 / tutorial HUD
ESC / pausa e mappa
```

## Sistemi principali

### Missione

`Assets/CrisisProtocol/Scripts/Core/MissionManager.cs`

Gestisce:

- stato del settore;
- credenziali raccolte;
- focolai contenuti;
- collasso strutturale;
- sblocco estrazione;
- eventi verso HUD/UI;
- vittoria, sconfitta e fine gioco.

Supporti:

- `SectorObjectiveSettings`
- `StructuralCollapseSettings`
- `SectorScoreSettings`
- `SectorContainmentTags`

### Stato globale

`Assets/CrisisProtocol/Scripts/Core/GameManager.cs`

Mantiene:

- dati tra scene;
- salvataggio JSON;
- credenziali/firme acquisite;
- focolai e stati risolti;
- reset nuova partita;
- compatibilita' con alcuni wrapper legacy.

Salvataggio:

```text
Application.persistentDataPath/SectorContainment_Save.json
```

### Interazione player

Script principali:

- `Assets/CrisisProtocol/Scripts/Player/PlayerInteract.cs`
- `Assets/CrisisProtocol/Scripts/Mission/EmergencyScanner.cs`
- `Assets/CrisisProtocol/Scripts/Player/SalutePlayer.cs`
- `Assets/CrisisProtocol/Scripts/Player/GestoreArmi.cs`
- `Assets/CrisisProtocol/Scripts/Player/SparoPlayer.cs`
- `Assets/CrisisProtocol/Scripts/Player/AttaccoPlayer.cs`

Il player interagisce con oggetti che implementano `IInteractable`, tra cui porte, terminali, datapad, credenziali e focolai.

### Porte, terminali e datapad

Script principali:

- `Assets/CrisisProtocol/Scripts/Environment/PortaSettore.cs`
- `Assets/CrisisProtocol/Scripts/Environment/TerminalePorta.cs`
- `Assets/CrisisProtocol/Scripts/UI/TerminalePortaUI.cs`
- `Assets/CrisisProtocol/Scripts/Environment/DatapadCodiciPorte.cs`
- `Assets/CrisisProtocol/Scripts/UI/DatapadOlogrammaUI.cs`

Funzioni attuali:

- porte con stato rosso/verde;
- portelloni finali apribili solo a fine livello;
- terminali con PIN e minigioco bypass;
- datapad evidenziati nello spazio di gioco;
- chiusura GUI e gestione mouse in build;
- fallback runtime per luci/materiali di stato.

### Emergenze ambientali

Script principali:

- `Assets/CrisisProtocol/Scripts/Mission/SectorEmergency/EmergencyHotspot.cs`
- `Assets/CrisisProtocol/Scripts/Mission/SectorEmergency/StructuralHazard.cs`
- `Assets/script/LuceEmergenzaSettore.cs`

Focolai supportati:

- scintille elettriche;
- perdite chimiche;
- gas tossico;
- luci di emergenza;
- hazard che aumentano stress/collasso o danneggiano il player.

Il gas del settore 1 viene generato a runtime se non e' salvato in scena. Ha un materiale particellare runtime per evitare che in build sparisca per shader/materiali non inclusi.

### Nemici

Script principali:

- `Assets/CrisisProtocol/Scripts/Enemy/DroneRonda.cs`
- `Assets/CrisisProtocol/Scripts/Enemy/GuardiaNpc.cs`
- `Assets/CrisisProtocol/Scripts/Enemy/ManutenzioneBot.cs`
- `Assets/CrisisProtocol/Scripts/Enemy/npc.cs`

Funzioni attuali:

- pattugliamento waypoint/NavMesh;
- cono visivo con luce;
- stato ronda/allarme;
- sparo e danno al player;
- modalita' pacifica a emergenza completata;
- correzione anti-soffitto per droni troppo alti.

### UI e mappa tattica

Script principali:

- `Assets/CrisisProtocol/Scripts/UI/CyberHUD.cs`
- `Assets/CrisisProtocol/Scripts/UI/HUDManager.cs`
- `Assets/CrisisProtocol/Scripts/UI/CrisisProtocolUIController.cs`
- `Assets/CrisisProtocol/Scripts/UI/EndGameCreditsController.cs`
- `Assets/AsyncronQuest/SteampunkUI/Scripts/AsyncronQuestSteampunkUI.cs`
- `Assets/AsyncronQuest/SteampunkUI/Scripts/CrisisProtocolPauseMenu.cs`
- `Assets/AsyncronQuest/SteampunkUI/Scripts/SceneTopDownMapUI.cs`
- `Assets/AsyncronQuest/Death/Scripts/DeathScreenController.cs`

La mappa usa lo shader `UI/TacticalNeonMap`, incluso tra gli shader sempre presenti in build. Se lo shader non viene trovato, il codice applica un fallback verde/scuro.

## Struttura repository

```text
Assets/
  AsyncronQuest/
    Death/                       schermata morte
    SteampunkUI/                 menu principale, pausa, mappa, shader UI
    Tooltips/                    tooltip e feedback testuali
  CrisisProtocol/
    Animations/                  animazioni e controller
    Materials/                   materiali progetto
    Models/                      modelli 3D organizzati
    Prefabs/                     prefab gameplay
    Scripts/
      Core/                      GameManager, MissionManager, audio scena
      Data/                      dati configurabili
      Enemy/                     droni, guardie, bot
      Environment/               porte, terminali, datapad, camera, props
      Interfaces/                IInteractable, IDamageable
      Mission/                   scanner, ostacoli, sector emergency
      Player/                    movimento, salute, armi, interazione
      UI/                        HUD, terminali, datapad, credits, modal
    Terrain/                     terrain data
  Scenes/                        MainMenu, settore 0, settore 1, settore 2
docs/
  design/                        GDD
  technical/                     struttura progetto e dipendenze
Packages/                        pacchetti Unity
ProjectSettings/                 configurazione Unity
```

## Setup

1. Clona il repository.
2. Apri Unity Hub.
3. Aggiungi il progetto dalla cartella root.
4. Usa Unity `6000.0.74f1`.
5. Lascia completare importazione asset e rigenerazione `Library/`.
6. Apri `Assets/Scenes/MainMenu-Scene.unity`.
7. Premi Play.

## Build

Per generare una build aggiornata:

1. Apri Unity.
2. Vai in `File > Build Profiles` o `Build Settings`.
3. Verifica che siano attive:
   - `MainMenu-Scene`
   - `settore 0`
   - `settore 1`
   - `settore 2`
4. Genera una nuova build Windows.
5. Testa in build reale:
   - click mouse su menu, terminali, datapad, credits;
   - mappa pausa con sfondo verde/neon;
   - gas settore 1;
   - drone settore 1 non incastrato nel soffitto;
   - portellone finale che si apre solo a obiettivo completato;
   - luci stato porte rosso/verde.

## Troubleshooting

### Il mouse non clicca una GUI in build

Controllare che la GUI crei o riattivi `EventSystem` con `InputSystemUIInputModule`. Le UI principali sono gia' state aggiornate: menu, pausa, HUD tutorial, terminale porta, datapad, death screen e credits.

### La mappa perde il verde in build

Controllare:

- shader `UI/TacticalNeonMap` in `ProjectSettings/GraphicsSettings.asset`;
- `SceneTopDownMapUI`;
- `CrisisProtocolPauseMenu` per fallback cornice/sfondo runtime.

### Il gas non si vede

Controllare:

- hotspot con `modalitaVisiva = ToxicGasLeak`;
- `EmergencyHotspot.GeneraPerditaGasSeAssente()`;
- renderer particellari e materiale runtime `RuntimeGasMaterial`;
- se il focolaio e' gia' stato contenuto, le particelle vengono spente.

### Il drone e' troppo alto

Controllare:

- posizione root del drone nella scena;
- offset locale del prefab `drone_nemico 1`;
- waypoint assegnati;
- opzioni anti-soffitto in `DroneRonda`.

### L'estrazione non si apre

Controllare:

- tutti gli `EmergencyHotspot` richiesti contenuti;
- credenziali raccolte;
- `MissionManager.EstrazioneSbloccata`;
- `PortaSettore.apriAlTermineCrisi`;
- portellone finale non aperto manualmente prima della fine livello.

## Documentazione

- [`docs/design/GDD.md`](docs/design/GDD.md): Game Design Document.
- [`docs/technical/PROJECT_STRUCTURE.md`](docs/technical/PROJECT_STRUCTURE.md): struttura cartelle.
- [`docs/technical/DEPENDENCIES.md`](docs/technical/DEPENDENCIES.md): dipendenze.

## Note Git

Il progetto usa `.gitignore` Unity. Non versionare:

- `Library/`
- `Temp/`
- `Obj/`
- `Build/` e `build/`
- `Logs/`
- `UserSettings/`
- file `.csproj`, `.sln`, `.slnx` generati.

Conservare sempre i file `.meta` degli asset tracciati: scene e prefab Unity dipendono da quei GUID.
