# Dipendenze

## Unity

Versione progetto:

```text
Unity 6000.0.74f1
```

File di riferimento:

```text
ProjectSettings/ProjectVersion.txt
```

## Package Unity

Package principali dichiarati in `Packages/manifest.json`:

- `com.unity.render-pipelines.universal` `17.0.4`
- `com.unity.inputsystem` `1.14.0`
- `com.unity.ai.navigation` `2.0.13`
- `com.unity.probuilder` `6.0.9`
- `com.unity.textmeshpro` `5.0.0`
- `com.unity.timeline` `1.8.12`
- `com.unity.ugui` `2.0.0`
- `com.unity.test-framework` `1.6.0`
- `com.unity.cloud.gltfast` `6.19.0`

`Packages/packages-lock.json` deve restare versionato insieme al manifest.

## Scene di build

Le scene abilitate in `ProjectSettings/EditorBuildSettings.asset` sono:

- `Assets/Scenes/MainMenu-Scene.unity`
- `Assets/Scenes/settore 0.unity`
- `Assets/Scenes/settore 1.unity`
- `Assets/Scenes/settore 2.unity`

Le scene in `_Legacy` restano disattivate.

## Git LFS

Il progetto contiene asset binari grandi: modelli, texture, audio, video e documenti. Usare Git LFS quando configurato dalla repo:

```powershell
git lfs install
git lfs status
```

## File generati da Unity

Non versionare:

- `Library/`
- `Temp/`
- `Obj/`
- `Logs/`
- `UserSettings/`
- `Build/`
- `build/`
- file `.csproj`, `.sln`, `.slnx` generati dall'editor

## Verifica

La verifica piu' affidabile e' da Unity:

1. aprire il progetto con Unity `6000.0.74f1`;
2. controllare la Console;
3. avviare `MainMenu-Scene` in Play Mode;
4. generare una build Windows e testare i settori.

Il comando `dotnet build` e' opzionale: funziona solo se sulla macchina e' installato un .NET SDK compatibile, non solo il runtime.
