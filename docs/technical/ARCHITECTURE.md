# Technical Architecture

## Obiettivo

Questa pagina descrive i moduli logici del prototipo senza imporre spostamenti fisici rischiosi dentro `Assets/`. In Unity, il riordino degli asset deve essere fatto con attenzione per preservare `.meta`, scene e prefab.

## Moduli gameplay

### Mission Flow

Responsabilita':

- stato della missione;
- obiettivi;
- collasso strutturale;
- punteggio;
- vittoria e sconfitta;
- notifica eventi alla UI.

Script principali:

- `Assets/MissionManager.cs`
- `Assets/script/SectorEmergency/SectorObjectiveSettings.cs`
- `Assets/script/SectorEmergency/StructuralCollapseSettings.cs`
- `Assets/script/SectorEmergency/SectorScoreSettings.cs`

### Global State

Responsabilita':

- persistenza JSON;
- stato globale tra scene;
- tag temporali acquisiti;
- anacronismi risolti;
- varchi temporali temporanei.

Script principali:

- `Assets/script/GameManager.cs`
- `Assets/script/TemporalTagData.cs`
- `Assets/script/TestAnacronismo.cs`
- `Assets/script/OstacoloCausale.cs`

### Sector Emergency

Responsabilita':

- credenziali;
- focolai;
- portelloni di quarantena;
- hazard ambientali;
- tag Unity centralizzati.

Script principali:

- `Assets/script/SectorEmergency/AccessCredentialPickup.cs`
- `Assets/script/SectorEmergency/EmergencyHotspot.cs`
- `Assets/script/SectorEmergency/QuarantineGate.cs`
- `Assets/script/SectorEmergency/StructuralHazard.cs`
- `Assets/script/SectorEmergency/SectorContainmentTags.cs`

### Player Interaction

Responsabilita':

- input e interazioni;
- scanner temporale;
- danno e salute;
- armi.

Script principali:

- `Assets/script/PlayerInteract.cs`
- `Assets/script/ScannerTemporale.cs`
- `Assets/script/SalutePlayer.cs`
- `Assets/script/GestoreArmi.cs`
- `Assets/script/SparoPlayer.cs`
- `Assets/script/AttaccoPlayer.cs`

### Enemies

Responsabilita':

- pattugliamento;
- rilevamento;
- inseguimento;
- combattimento;
- morte/cleanup.

Script principali:

- `Assets/script/ViaggiatoreTemporale.cs`
- `Assets/script/GuardiaNpc.cs`
- `Assets/script/DroneRonda.cs`
- `Assets/npc.cs`

### UI

Responsabilita':

- HUD missione;
- pause/menu;
- schermata di morte;
- tooltip;
- feedback scanner/anomalie.

Script principali:

- `Assets/script/HUDManager.cs`
- `Assets/script/GoldenCastUIController.cs`
- `Assets/AsyncronQuest/Death/Scripts/DeathScreenController.cs`
- `Assets/AsyncronQuest/Tooltips/Scripts/*.cs`
- `Assets/AsyncronQuest/SteampunkUI/Scripts/*.cs`

## Moduli asset

### Core assets

Contengono asset direttamente usati dal prototipo:

- `Assets/Scenes`
- `Assets/script`
- `Assets/AsyncronQuest`
- `Assets/Audio`
- `Assets/prefab`
- `Assets/Materials`
- `Assets/Resources`
- `Assets/Settings`

### Third-party / support assets

Contengono pacchetti, demo o librerie utili ma potenzialmente pesanti:

- `Assets/Blockout`
- `Assets/Veridian`
- `Assets/TextMesh Pro`
- `Assets/ProBuilder Data`
- `Assets/AngeloMaN87`

## Regola di modularita'

Per nuovi script gameplay, preferire una struttura logica del tipo:

```text
Assets/script/
  Core/
  Player/
  Enemies/
  Mission/
  SectorEmergency/
  UI/
```

Gli spostamenti degli script esistenti vanno fatti da Unity o in una PR dedicata, verificando scene e prefab subito dopo.
