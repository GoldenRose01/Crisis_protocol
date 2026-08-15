# Report modifiche effettuate

## Riepilogo

Questo documento descrive le modifiche applicate al progetto per trasformare il prototipo precedente `ep2526-tt-goldencast` / GoldenCast nel progetto conforme al nuovo GDD:

```text
Sector Containment: Emergency
```

Titolo alternativo:

```text
Crisis Protocol: Red Line
```

La modifica principale consiste nello spostare il focus di gameplay da viaggio temporale, tag temporali e anacronismi a un'esperienza sci-fi di emergenza: operatore tecnico, settori in quarantena, credenziali, focolai da contenere, collasso strutturale ed estrazione.

## Commit di riferimento

```text
b0f234b Migrate core gameplay to sector containment GDD
```

Branch:

```text
main
```

Repository remoto:

```text
https://github.com/GoldenRose01/Crisis_protocol.git
```

## File modificati

### `Assets/script/GameManager.cs`

Il `GameManager` e' stato trasformato nel gestore globale dello stato di `Sector Containment`.

Modifiche principali:

- sostituito il modello mentale `tag temporali/anacronismi` con `firme di sicurezza/incidenti`;
- aggiunto il nuovo file di salvataggio `SectorContainment_Save.json`;
- mantenuta la lettura del vecchio `GoldenCast_Save.json` per compatibilita' con salvataggi precedenti;
- aggiunte collezioni persistenti per firme acquisite, incidenti risolti e storico autorizzazioni;
- aggiunti eventi nuovi:
  - `OnSecuritySignatureAcquired`;
  - `OnIncidentResolved`;
- aggiunte API nuove:
  - `RegisterSecuritySignature`;
  - `ResolveIncident`;
  - `IsSecuritySignatureUnlocked`;
  - `AuthorizeReturnChannel`;
  - `ConsumeReturnChannel`;
- collegata l'acquisizione di una firma/credenziale a `MissionManager.RegistraCredenziale`;
- aggiornato il reset debug per cancellare sia il salvataggio nuovo sia quello legacy.

Compatibilita' mantenuta:

- `ExtractTag`;
- `ResolveAnachronism`;
- `IsTagUnlocked`;
- `ApriVarcoRitorno`;
- `ConsumaVarcoRitorno`.

Questi metodi restano come wrapper per evitare rotture in scene, prefab, UI o script che usano ancora nomi GoldenCast.

### `Assets/script/ScannerTemporale.cs`

Lo scanner e' stato convertito da scanner temporale a scanner di emergenza.

Modifiche principali:

- il tasto `Q` continua a emettere una scansione tramite raycast;
- lo scanner ora riconosce credenziali fisiche con `AccessCredentialPickup`;
- riconosce focolai d'emergenza con `EmergencyHotspot`;
- riconosce oggetti interagibili tramite `IInteractable`;
- riconosce ostacoli legacy `OstacoloCausale` e li registra come firme di sicurezza;
- aggiornata la messaggistica di debug verso lessico di emergenza, credenziali e focolai.

Il nome `ScannerTemporale` e' rimasto invariato perche' Unity salva i riferimenti agli script nei prefab e nelle scene.

### `Assets/script/TestAnacronismo.cs`

L'interazione di scansione legacy e' stata risemantizzata.

Modifiche principali:

- aggiornati header, tooltip e log verso `scansione`, `firma` e `criticita'`;
- al completamento della UI scanner viene chiamato `RegisterSecuritySignature`;
- mantenuta la UI `AnachronismScannerUI` come ponte visivo gia' presente nel progetto;
- mantenuto il nome classe/file legacy per compatibilita' Unity.

### `Assets/script/TemporalTagData.cs`

Lo ScriptableObject dati scanner e' stato adattato al nuovo GDD.

Modifiche principali:

- cambiato menu di creazione asset in `Sector Containment/Firma Scanner`;
- aggiornati tooltip e descrizioni;
- aggiunti alias leggibili per il nuovo dominio:
  - `SecuritySignatureId`;
  - `DisplayName`;
  - `IncidentDescription`.

I campi originali `idTag`, `epochName`, `description` e `anachronismVideo` sono rimasti per non perdere dati gia' serializzati negli asset Unity.

### `README.md`

Il README principale e' stato aggiornato per descrivere il progetto come `Sector Containment: Emergency`.

Modifiche principali:

- sostituiti riferimenti a scanner temporale, anacronismi e tag temporali nella descrizione operativa;
- documentato il salvataggio nuovo `SectorContainment_Save.json`;
- spiegata la compatibilita' con `GoldenCast_Save.json`;
- aggiornata la sezione scanner;
- aggiornato il troubleshooting;
- aggiunto link al documento di migrazione.

### `docs/design/GDD.md`

Il GDD e' stato collegato al prototipo reale.

Modifiche principali:

- aggiornato il ruolo di `GameManager`;
- aggiornato il ruolo di `ScannerTemporale`;
- chiarito che alcuni nomi restano legacy solo per compatibilita' tecnica.

### `docs/technical/ARCHITECTURE.md`

La documentazione tecnica e' stata riallineata alla nuova architettura.

Modifiche principali:

- `Global State` ora parla di firme, credenziali, incidenti e migrazione legacy;
- `Player Interaction` ora parla di scanner di emergenza;
- aggiunta nota sulla compatibilita' GoldenCast;
- chiarito che i wrapper legacy sono un ponte tecnico temporaneo.

### `docs/technical/MIGRATION_FROM_GOLDENCAST.md`

Creato un documento dedicato alla migrazione dal vecchio progetto.

Contenuti principali:

- differenze di direzione di design;
- sistemi core trasformati;
- sistemi gia' coerenti con il nuovo GDD;
- elementi che restano legacy;
- verifica tecnica eseguita.

### `docs/README.md`

Aggiornato l'indice documentale.

Modifiche principali:

- aggiunto link alla migrazione GoldenCast;
- aggiunto collegamento a questo report delle modifiche.

## Perche' alcuni nomi GoldenCast restano nel progetto

Unity serializza riferimenti a script, classi, prefab e scene. Rinominare di colpo file o classi come `ScannerTemporale`, `TemporalTagData`, `testAnacronismo` o namespace UI puo' generare riferimenti mancanti in editor.

Per questo la migrazione e' stata fatta in modo controllato:

- comportamento aggiornato al nuovo GDD;
- API nuove introdotte;
- wrapper vecchi mantenuti;
- documentazione aggiornata;
- rinomina fisica rimandata a una fase dedicata da fare dentro Unity Editor.

## Verifica eseguita

Comando:

```powershell
dotnet build Crisis_protocol.slnx
```

Risultato finale:

```text
0 errori
0 warning
```

## Stato finale

Il progetto ora ha:

- core gameplay orientato a Sector Containment;
- persistenza aggiornata;
- compatibilita' con salvataggi e script legacy;
- scanner coerente con emergenze, credenziali e focolai;
- documentazione tecnica e README allineati;
- report di migrazione e report modifiche separati.

## Prossimi interventi consigliati

1. Aprire il progetto in Unity `6000.0.74f1`.
2. Verificare scene e prefab in Play Mode.
3. Collegare visivamente UI e testi in editor al nuovo lessico.
4. Rinominare gradualmente classi e namespace legacy solo dopo verifica dei riferimenti.
5. Generare una build Windows pulita dopo il test in editor.
