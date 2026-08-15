# Sector Containment: Emergency

[![Review Assignment Due Date](https://classroom.github.com/assets/deadline-readme-button-22041afd0340ce965d47ae6ef1cefeee28c7c493a6346c4f15d667ab976d596c.svg)](https://classroom.github.com/a/QblLKDUe)

Titolo alternativo: `Crisis Protocol: Red Line`.

Prototipo 3D realizzato in Unity per un action-stealth sci-fi con elementi puzzle-strategy. Il giocatore interpreta un operatore di emergenza in un'infrastruttura tecnologica ad alta sicurezza colpita da un guasto critico a catena.

L'obiettivo e' attraversare settori in quarantena, recuperare credenziali e frequenze di accesso, contenere focolai d'emergenza e raggiungere l'estrazione prima del collasso strutturale.

Il progetto combina esplorazione, scanner di emergenza, contenimento di focolai/sistemi compromessi, IA nemica su NavMesh, UI di missione e punteggio.

## Stato del progetto

- Motore: Unity `6000.0.74f1`
- Pipeline grafica: Universal Render Pipeline `17.0.4`
- Input: Unity Input System `1.14.0`
- Scena principale abilitata in build: `Assets/Scenes/locale.unity`
- Scene presenti nel progetto: `MainMenu-Scene`, `locale`, `Passato_1961`, `test`
- Verifica C# locale: `dotnet build Crisis_protocol.slnx` completato con `0` errori

I warning attuali arrivano principalmente da asset/plugin esterni, in particolare `Blockout`, che usa alcune API Unity obsolete. Non bloccano la compilazione del gameplay custom.

La documentazione completa e' organizzata in [`docs/README.md`](docs/README.md):

- [`docs/design/GDD.md`](docs/design/GDD.md): Game Design Document.
- [`docs/technical/ARCHITECTURE.md`](docs/technical/ARCHITECTURE.md): moduli e responsabilita' tecniche.
- [`docs/technical/DEPENDENCIES.md`](docs/technical/DEPENDENCIES.md): dipendenze Unity e Git LFS.
- [`docs/technical/MAINTENANCE.md`](docs/technical/MAINTENANCE.md): checklist di manutenzione.
- [`docs/technical/MIGRATION_FROM_GOLDENCAST.md`](docs/technical/MIGRATION_FROM_GOLDENCAST.md): trasformazione dal prototipo GoldenCast al nuovo GDD.
- [`docs/process/RELEASE_CHECKLIST.md`](docs/process/RELEASE_CHECKLIST.md): controlli prima di consegna o push.

## Gameplay

L'obiettivo e' sopravvivere alla crisi del settore, stabilizzare l'infrastruttura e completare l'estrazione.

Il loop principale e':

1. Ricevi il briefing e identifica i settori critici dell'impianto.
2. Entra nel settore isolato evitando droni, guardie e sistemi automatizzati compromessi.
3. Recupera credenziali, chiavi di sicurezza o frequenze.
4. Usa scanner e interazioni per analizzare focolai, terminali e anomalie.
5. Contieni tutti i focolai richiesti.
6. Raggiungi il portellone di quarantena ed estrai prima che il collasso arrivi al 100%.

Il punteggio tiene conto di focolai contenuti, credenziali raccolte, integrita' residua, tempo impiegato, danni subiti, allarmi e tentativi errati.

## Sistemi principali

### Missione e collasso

`MissionManager` governa lo stato della missione:

- inizializza gli obiettivi del settore;
- aumenta progressivamente il collasso strutturale;
- registra credenziali, focolai contenuti, allarmi e danni;
- sblocca l'estrazione quando gli obiettivi sono completati;
- determina vittoria o sconfitta;
- notifica la UI tramite eventi statici.

Classi di supporto:

- `SectorObjectiveSettings`: calcolo del numero di focolai da contenere.
- `StructuralCollapseSettings`: valori di collasso, penalita' e recupero.
- `SectorScoreSettings`: regole di scoring provvisorio e finale.

### Persistenza e stato globale

`GameManager` mantiene lo stato globale tra scene e sessioni:

- firme di sicurezza e credenziali acquisite;
- incidenti/focolai risolti;
- storico delle autorizzazioni sbloccate;
- canali operativi temporanei;
- stato volatile degli ostacoli legacy.

Il salvataggio viene scritto in:

```text
Application.persistentDataPath/SectorContainment_Save.json
```

Se esiste ancora un vecchio `GoldenCast_Save.json`, viene letto come salvataggio legacy e convertito in memoria ai nuovi campi. Nota importante: il registro degli ostacoli causali e' volutamente volatile. Gli ostacoli possono cambiare durante la sessione, ma vengono ripristinati al riavvio del gioco.

### Scanner di emergenza

`ScannerTemporale` mantiene il nome tecnico legacy per non rompere scene e prefab, ma ora funziona come scanner di emergenza. Emette un raycast dalla prospettiva della camera e riconosce credenziali, focolai, interagibili e ostacoli legacy trasformati in firme di sicurezza.

Comando predefinito:

```text
Q
```

Per funzionare correttamente richiede:

- una `Main Camera`;
- un layer scansionabile assegnato;
- oggetti bersaglio con `AccessCredentialPickup`, `EmergencyHotspot`, `IInteractable` o `OstacoloCausale`;
- un `GameManager` attivo nella scena o persistente.

### Emergenze di settore

Gli script in `Assets/script/SectorEmergency` definiscono il nucleo della missione di contenimento:

- `EmergencyHotspot`: focolaio contenibile tramite interazione.
- `AccessCredentialPickup`: raccolta di credenziali/autorizzazioni.
- `QuarantineGate`: punto di estrazione finale.
- `StructuralHazard`: sorgenti di stress o danno ambientale.
- `SectorContainmentTags`: gestione centralizzata dei tag Unity richiesti.

### IA e combattimento

`ViaggiatoreTemporale`, `GuardiaNpc`, `DroneRonda` e gli script collegati gestiscono nemici e minacce:

- pattugliamento su NavMesh;
- rilevamento del player tramite raggio visivo;
- inseguimento;
- combattimento a distanza;
- danno tramite interfaccia `IDamageable`;
- stati di morte e cleanup.

Per gli NPC basati su NavMesh e' necessario avere una NavMesh valida nella scena.

### UI e feedback

La cartella `Assets/AsyncronQuest` contiene sistemi di interfaccia e feedback:

- schermata di morte;
- tooltip temporali;
- UI steampunk;
- transizioni video;
- controller HUD e menu.

Gli eventi esposti da `MissionManager` e `GameManager` consentono alla UI di aggiornare progressi, punteggio, collasso, credenziali e stato dell'estrazione.

## Struttura del repository

```text
Assets/
  AsyncronQuest/              UI, tooltip, video, sistemi scanner legacy
  Audio/                      Effetti sonori e tracce ambientali
  Blockout/                   Asset e strumenti di blockout
  Materials/                  Materiali e texture
  Scenes/                     Scene principali del progetto
  script/                     Gameplay custom principale
    SectorEmergency/          Sistemi di missione e contenimento
  prefab/                     Prefab di personaggi, oggetti e props
docs/                         Documentazione modulare
  design/                     GDD e direzione creativa
  technical/                  architettura, dipendenze, manutenzione
  process/                    checklist di consegna e release
Editor/                       Utility editor
Packages/                     Dipendenze Unity
ProjectSettings/              Configurazione progetto Unity
tools/                        Strumenti ausiliari
build/                        Build locale generata, ignorata da Git
```

## Requisiti

- Unity `6000.0.74f1`, stessa versione indicata in `ProjectSettings/ProjectVersion.txt`.
- Moduli Unity standard per Windows build, URP, Input System e NavMesh.
- .NET SDK compatibile con la generazione dei progetti C# Unity, utile per controlli rapidi da terminale.

## Setup

1. Clona il repository.
2. Apri Unity Hub.
3. Seleziona `Add project from disk`.
4. Scegli la cartella root del repository.
5. Apri con Unity `6000.0.74f1`.
6. Attendi la rigenerazione di `Library/` e degli asset importati.
7. Apri la scena `Assets/Scenes/locale.unity`.
8. Premi Play.

Alla prima apertura Unity puo' impiegare diversi minuti per importare texture, modelli, package e cache.

## Comandi utili

Verifica C# fuori da Unity:

```powershell
dotnet build Crisis_protocol.slnx
```

Controllo dei file modificati:

```powershell
git status --short
```

Ricerca degli script custom:

```powershell
rg --files -g "*.cs" Assets/script Assets/AsyncronQuest Editor
```

## Controlli in editor

Prima di consegnare o creare una build, verificare:

- `MissionManager` presente nella scena gameplay.
- `GameManager` presente nella scena iniziale o caricato in modo persistente.
- Player con tag corretto definito in `SectorContainmentTags`.
- Oggetti interagibili con collider e componente `IInteractable`.
- Hotspot con `hotspotId` univoco.
- Credenziali con ID coerenti con i requisiti degli hotspot.
- `QuarantineGate` collegato alla logica di estrazione.
- NavMesh bake valido per guardie, droni e unita' ostili.
- `Main Camera` presente per scanner e raycast visivi.
- Scene necessarie abilitate in `File > Build Profiles` o `Build Settings`.

## Build

La cartella `build/` contiene una build locale Windows generata in precedenza, ma e' ignorata da Git. Per produrre una nuova build:

1. Apri Unity.
2. Vai su `File > Build Profiles`.
3. Seleziona la piattaforma desiderata.
4. Controlla che `Assets/Scenes/locale.unity` sia inclusa.
5. Avvia `Build` o `Build And Run`.

Per una consegna pulita e' preferibile generare una build nuova dopo aver aperto il progetto nella versione Unity corretta.

## Note di versionamento

Il repository usa una `.gitignore` impostata per Unity. Sono ignorate cartelle generate o pesanti come:

- `Library/`
- `Temp/`
- `Obj/`
- `Build/` e `build/`
- `Logs/`
- `UserSettings/`
- file `.csproj`, `.sln`, `.slnx` generati dall'editor

Gli asset Unity devono invece mantenere il proprio file `.meta` quando sono tracciati da Git, per non rompere riferimenti, prefab e scene.

## Documentazione di design

Sono presenti documenti di progetto in formato Word:

- `GoldenCast_Base_Info_Game_Design_Document.docx`
- `GoldenCast_Game_Design_Document_Task_Force_Temporale.docx`
- `Mini_Report_Struttura_GoldenCast.docx`
- `docs/design/GDD.md`
- `docs/technical/ARCHITECTURE.md`
- `docs/technical/DEPENDENCIES.md`
- `docs/technical/MAINTENANCE.md`
- `docs/technical/MIGRATION_FROM_GOLDENCAST.md`
- `docs/process/RELEASE_CHECKLIST.md`

Questi file descrivono concept, struttura, direzione di design e manutenzione tecnica del progetto. Il file `docs/design/GDD.md` contiene la versione Markdown pulita e leggibile del GDD allegato.

## Troubleshooting

Se Unity mostra riferimenti mancanti:

- lascia terminare l'importazione degli asset;
- chiudi e riapri Unity;
- controlla che i `.meta` siano presenti;
- verifica che la versione Unity coincida con `6000.0.74f1`.

Se gli NPC non si muovono:

- controlla che la scena abbia una NavMesh valida;
- verifica che il `NavMeshAgent` sia attivo;
- assicurati che l'NPC sia posizionato sulla NavMesh.

Se lo scanner non trova bersagli:

- controlla il layer assegnato a `layerScansionabile`;
- verifica che il bersaglio abbia `AccessCredentialPickup`, `EmergencyHotspot`, `IInteractable` o `OstacoloCausale`;
- controlla che la camera abbia il tag `MainCamera`;
- verifica distanza e direzione del raycast nel Gizmo.

Se l'estrazione non si sblocca:

- verifica il numero totale di `EmergencyHotspot`;
- controlla che ogni focolaio abbia un `hotspotId` univoco;
- verifica che la credenziale richiesta sia stata raccolta;
- controlla i log di `MissionManager`.

## Verifica eseguita

Ultima verifica locale:

```text
dotnet build Crisis_protocol.slnx
Risultato: 0 errori, 47 warning
```

I warning rilevati riguardano principalmente API obsolete in asset esterni `Blockout` e non impediscono la compilazione del progetto.
