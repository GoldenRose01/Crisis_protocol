# Struttura progetto

Il progetto e' organizzato per separare codice gameplay, asset, scene e documentazione. La regola base e': il codice sta in `Scripts`, gli asset stanno in cartelle asset dedicate.

## Cartelle principali

- `Assets/CrisisProtocol/Scripts`
  Codice gameplay diviso in `Core`, `Mission`, `Player`, `Enemy`, `Environment`, `UI`, `Interfaces` e `Data`.

- `Assets/CrisisProtocol/Models`
  Modelli 3D del progetto, divisi per ambiente, props ed emergenze.

- `Assets/CrisisProtocol/Materials`
  Materiali del progetto.

- `Assets/CrisisProtocol/Prefabs`
  Prefab riusabili nelle scene.

- `Assets/CrisisProtocol/Animations`
  Animazioni e controller.

- `Assets/CrisisProtocol/Terrain`
  TerrainData e asset legati al terreno.

- `Assets/AsyncronQuest`
  Menu principale, pause menu, mappa tattica, death screen, tooltip, shader UI e asset visivi di interfaccia.

- `Assets/Scenes`
  Scene giocabili: `MainMenu-Scene`, `settore 0`, `settore 1`, `settore 2`.

- `Assets/_Legacy`
  Scene e asset storici non attivi in build.

- `docs/design`
  GDD e documenti di design.

- `docs/technical`
  Architettura, dipendenze, struttura e manutenzione.

- `docs/process`
  Checklist e report di processo.

## Regola pratica

Quando importi o crei nuovi file:

1. Script: `Assets/CrisisProtocol/Scripts/...`
2. Modello 3D: `Assets/CrisisProtocol/Models/...`
3. Materiale: `Assets/CrisisProtocol/Materials/...`
4. Prefab: `Assets/CrisisProtocol/Prefabs/...`
5. Documento: `docs/design`, `docs/technical` o `docs/process`

Spostare asset Unity sempre conservando il relativo `.meta`, altrimenti scene e prefab possono perdere riferimenti.

## Commenti nel codice

I commenti devono spiegare il perche' di un blocco o il problema che risolve. Evitare commenti vuoti tipo `setta`, `chiama`, `fa cose`, `riga-ok`: aggiungono rumore e non aiutano chi legge.
