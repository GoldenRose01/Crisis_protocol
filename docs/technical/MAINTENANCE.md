# Project Maintenance

Questa checklist serve a mantenere il progetto Unity stabile, riproducibile e facile da consegnare.

## Versione Unity

- Versione richiesta: `6000.0.74f1`
- File di riferimento: `ProjectSettings/ProjectVersion.txt`
- Impostazioni gia' corrette:
  - asset serialization: `Force Text`
  - version control mode: `Visible Meta Files`

## Dipendenze

Le dipendenze Unity sono dichiarate in:

```text
Packages/manifest.json
```

Il lock file e' tracciato in:

```text
Packages/packages-lock.json
```

Controllo rapido di coerenza tra manifest e lock:

```powershell
$manifest = Get-Content Packages\manifest.json -Raw | ConvertFrom-Json
$lock = Get-Content Packages\packages-lock.json -Raw | ConvertFrom-Json
foreach ($p in $manifest.dependencies.PSObject.Properties) {
  $locked = $lock.dependencies.PSObject.Properties[$p.Name]
  if ($null -eq $locked) { "Missing in lock: $($p.Name)" }
  elseif ($locked.Value.version -ne $p.Value) { "Mismatch: $($p.Name)" }
}
```

Se il comando non stampa nulla, le dipendenze dirette sono allineate.

## Verifiche prima di consegnare

Eseguire dalla root del repository:

```powershell
git status --short
dotnet build Crisis_protocol.slnx
```

Controllare in Unity:

- nessun errore nella Console;
- `Assets/Scenes/locale.unity` avviabile in Play Mode;
- `MissionManager` e `GameManager` presenti nella scena o caricati correttamente;
- `NavMeshAgent` dei nemici posizionato su una NavMesh valida;
- credenziali e hotspot con ID coerenti;
- scene necessarie abilitate nei Build Profiles.

## File da non versionare

La `.gitignore` esclude le principali cartelle generate da Unity:

- `Library/`
- `Temp/`
- `Obj/`
- `Build/` e `build/`
- `Logs/`
- `UserSettings/`

Se compaiono file generati non previsti, controllare con:

```powershell
git status --short
git ls-files -ci --exclude-standard
```

Il secondo comando deve restituire vuoto: significa che non ci sono file gia' tracciati che oggi sarebbero ignorati.

## Line endings e asset

`.gitattributes` normalizza i file testuali e usa Git LFS per asset binari pesanti o non mergiabili. Questo riduce diff rumorosi, problemi tra Windows/macOS/Linux, merge accidentali su immagini/modelli/audio e push troppo grandi verso GitHub.

`.editorconfig` mantiene indentazione, newline finale e UTF-8 coerenti tra editor diversi.

## Note operative Unity

- Non eliminare o rigenerare manualmente file `.meta` tracciati.
- Quando si sposta un asset, farlo da Unity o assicurarsi che il `.meta` si muova insieme.
- Evitare di committare build locali, cache o file generati dall'IDE.
- Dopo aggiornamenti di package Unity, aprire il progetto in Unity e verificare che `manifest.json` e `packages-lock.json` siano entrambi aggiornati.
