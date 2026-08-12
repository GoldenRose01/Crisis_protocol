# Dependencies

## Unity

Versione progetto:

```text
Unity 6000.0.74f1
```

File sorgente:

```text
ProjectSettings/ProjectVersion.txt
```

## Unity packages diretti

Pacchetti principali dichiarati in `Packages/manifest.json`:

- `com.unity.render-pipelines.universal` `17.0.4`
- `com.unity.inputsystem` `1.14.0`
- `com.unity.ai.navigation` `2.0.13`
- `com.unity.probuilder` `6.0.9`
- `com.unity.textmeshpro` `5.0.0`
- `com.unity.timeline` `1.8.12`
- `com.unity.ugui` `2.0.0`
- `com.unity.test-framework` `1.6.0`
- `com.unity.cloud.gltfast` `6.19.0`

Il lock file `Packages/packages-lock.json` deve restare versionato insieme al manifest.

## Git LFS

Il progetto contiene asset binari grandi: modelli `.fbx/.glb`, texture `.png/.jpg`, audio/video, PDF e documenti Word. Questi file sono configurati in `.gitattributes` per Git LFS.

Installazione richiesta:

```powershell
git lfs install
```

Controllo file LFS:

```powershell
git lfs track
git lfs status
```

## File generati da Unity

Non sono dipendenze da versionare:

- `Library/`
- `Temp/`
- `Obj/`
- `Logs/`
- `UserSettings/`
- `build/`
- file `.csproj`, `.sln`, `.slnx` rigenerabili.

## Verifica rapida

```powershell
dotnet build Crisis_protocol.slnx
```

La compilazione C# puo' mostrare warning da asset esterni, ma non deve mostrare errori.
