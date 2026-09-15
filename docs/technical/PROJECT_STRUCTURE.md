# Struttura progetto

Questo progetto e' organizzato per tenere separati codice, asset di gioco, materiali, modelli e documentazione. L'idea e' semplice: se un file non e' codice, non deve stare dentro `Scripts`.

## Cartelle principali

- `Assets/CrisisProtocol/Scripts`
  Codice gameplay del progetto. Qui devono restare solo file `.cs` e relativi `.meta`.

- `Assets/CrisisProtocol/Models`
  Modelli 3D proprietari del progetto, divisi per contesto:
  - `Environment`: porte, terminali, tavoli e oggetti ambientali.
  - `MissionEmergency`: generatori, serbatoi, ascensori e asset legati alle emergenze.
  - `Props`: oggetti interattivi o kit specifici, tipo keypad e proiettori.

- `Assets/CrisisProtocol/Materials`
  Materiali usati dal progetto. I materiali legati a emergenze o prototipi missione stanno in `MissionEmergency`.

- `Assets/CrisisProtocol/Prefabs`
  Prefab riusabili nelle scene.

- `Assets/CrisisProtocol/Animations`
  Controller e animazioni del progetto.

- `Assets/CrisisProtocol/Terrain`
  TerrainData e asset di terreno, tolti dalla root di `Assets` per non impastare tutto.

- `Assets/Scenes`
  Scene Unity principali.

- `docs/design`
  GDD e documenti di design.

- `docs/reports`
  Report e documenti di consegna.

- `docs/archive`
  File legacy o temporanei che non devono stare nella root del repo.

## Regola pratica

Quando importi roba nuova in Unity:

1. Modello 3D: mettilo in `Assets/CrisisProtocol/Models/...`.
2. Materiale: mettilo in `Assets/CrisisProtocol/Materials/...`.
3. Script: mettilo in `Assets/CrisisProtocol/Scripts/...`.
4. Documento: mettilo in `docs/design`, `docs/reports` o `docs/archive`.
5. Non lasciare asset sciolti nella root di `Assets`, pk dopo due giorni diventa impossibile capire cosa serve davvero.

## Commenti nel codice

I commenti devono spiegare perche' esiste un blocco o quale problema risolve. Evitare commenti tipo `setta`, `chiama`, `fa cose`, `riga-ok`: sono rumore e non aiutano chi legge.
