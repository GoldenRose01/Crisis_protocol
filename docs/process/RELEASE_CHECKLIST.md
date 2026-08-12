# Release Checklist

Usare questa checklist prima di consegna, commit finale o push su GitHub.

## 1. Stato Git

```powershell
git status --short
git lfs status
```

Controllare che non compaiano:

- `Library/`
- `Temp/`
- `Logs/`
- `UserSettings/`
- `build/`
- file `.csproj` o `.slnx` generati.

## 2. Dipendenze

```powershell
git lfs install
```

Verificare:

- `Packages/manifest.json` presente;
- `Packages/packages-lock.json` presente;
- nessun package mancante nella finestra Package Manager di Unity.

## 3. Build C#

```powershell
dotnet build Crisis_protocol.slnx
```

Obiettivo:

- `0` errori;
- warning ammessi solo se noti e provenienti da asset esterni.

## 4. Unity Play Mode

Aprire `Assets/Scenes/locale.unity` e verificare:

- scena caricata senza errori Console;
- player controllabile;
- scanner funzionante;
- almeno una credenziale raccoglibile;
- almeno un focolaio contenibile;
- collasso e punteggio aggiornati in HUD;
- estrazione sbloccabile.

## 5. Commit

Separare i commit per argomento:

```text
docs: document game design and maintenance
config: normalize Unity repository settings
gameplay: update sector emergency loop
assets: add containment props
```

Per asset pesanti, controllare prima:

```powershell
git lfs status
```

## 6. Push

Se il push e' molto lento, controllare gli oggetti grandi:

```powershell
git lfs ls-files
git count-objects -vH
```

Non committare build locali per aggirare problemi di distribuzione: generare invece una release separata o usare artifact esterni.
