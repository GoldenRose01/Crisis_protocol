# Report 1:1 delle modifiche effettuate

## Scope

Questo documento elenca una per una le modifiche applicate per trasformare il progetto precedente `ep2526-tt-goldencast` / GoldenCast nel progetto conforme al GDD `Sector Containment: Emergency`.

Commit principali:

- `b0f234b Migrate core gameplay to sector containment GDD`
- `99122b6 Add detailed change report`

Confronto di riferimento:

```text
5fd95db..99122b6
```

Totale file coinvolti:

```text
10 file modificati/aggiunti
588 insertions
237 deletions
```

## 1. `Assets/script/GameManager.cs`

### 1.1 `SaveDataWrapper`

Aggiunto:

- `public string savedCredentialId;`
- `public List<string> securitySignaturesAcquired = new List<string>();`
- `public List<string> incidentsResolved = new List<string>();`
- `public List<string> unlockedSecurityHistory = new List<string>();`

Mantenuto per compatibilita' GoldenCast:

- `public string savedTagID;`
- `public List<string> tagTemporaliAcquisiti = new List<string>();`
- `public List<string> anacronismiRisolti = new List<string>();`
- `public List<string> tagSbloccatiStorico = new List<string>();`

Sostituito:

- `[System.Serializable]` con `[Serializable]`, usando `using System;` gia' presente.

Rimosso:

- commento descrittivo originale sulla serializzazione JSON;
- commento su `oggettiDistrutti`, non piu' presente nel wrapper.

### 1.2 Costanti di salvataggio

Aggiunto:

- `private const string SaveFileName = "SectorContainment_Save.json";`
- `private const string LegacySaveFileName = "GoldenCast_Save.json";`

Sostituito:

- percorso hardcoded `GoldenCast_Save.json` con costante `SaveFileName`;
- aggiunto percorso legacy separato per leggere vecchi salvataggi.

### 1.3 Stato pubblico corrente

Sostituito:

- header inspector `Stato del Viaggiatore` con `Stato Operatore`;
- tooltip `L'ultimo tag estratto dallo scanner temporale.` con tooltip su firma di sicurezza.

Mantenuto:

- `public string currentTagID = "";`

Motivo:

- il campo resta con nome legacy per non rompere riferimenti serializzati da Unity.

### 1.4 Eventi

Aggiunto:

- `public static event Action<string, int> OnSecuritySignatureAcquired;`
- `public static event Action<string, int> OnIncidentResolved;`

Mantenuto:

- `public static event Action<string, int> OnTemporalTagAcquired;`
- `public static event Action<string, int> OnAnachronismResolved;`

Motivo:

- i nuovi eventi servono al dominio Sector Containment;
- gli eventi legacy restano per UI/script che ascoltano ancora il vecchio lessico.

### 1.5 Collezioni runtime

Sostituito:

- `registroCausale` con `volatileContainmentState`;
- `tagTemporaliAcquisiti` con `securitySignaturesAcquired`;
- `anacronismiRisolti` con `incidentsResolved`;
- `storicoTagSbloccati` con `unlockedSecurityHistory`;
- `varchiApertiPerRitorno` con `authorizedReturnChannels`.

Aggiunto:

- `private string legacySaveFilePath;`

### 1.6 Proprieta' pubbliche

Aggiunto:

- `public string CurrentSecuritySignatureId => currentTagID;`
- `public int AcquiredSecuritySignatureCount => securitySignaturesAcquired.Count;`
- `public int ResolvedIncidentCount => incidentsResolved.Count;`

Sostituito internamente:

- `AcquiredTemporalTagCount` ora ritorna `AcquiredSecuritySignatureCount`;
- `ResolvedAnachronismCount` ora ritorna `ResolvedIncidentCount`.

Motivo:

- supportare nuovo lessico senza rompere codice legacy.

### 1.7 `Awake()`

Sostituito:

- `Destroy(this.gameObject);` con `Destroy(gameObject);`
- `DontDestroyOnLoad(this.gameObject);` con `DontDestroyOnLoad(gameObject);`
- `Path.Combine(Application.persistentDataPath, "GoldenCast_Save.json")` con `Path.Combine(Application.persistentDataPath, SaveFileName)`.

Aggiunto:

- inizializzazione di `legacySaveFilePath`.

Rimosso:

- commenti ridondanti sul singleton e sulla deserializzazione.

### 1.8 `Update()`

Sostituito:

- log debug F5 da messaggio GoldenCast/generico a `Reset completo dei dati di Sector Containment`.

Mantenuto:

- scorciatoia `F5`;
- chiamata a `ResetDatiDebug()`.

### 1.9 `GetCausalState(string id)`

Sostituito:

- lettura da `registroCausale` con lettura da `volatileContainmentState`.

Comportamento:

- resta compatibile con gli ostacoli causali legacy;
- lo stato continua a essere volatile.

### 1.10 `SetCausalState(string id, bool state)`

Sostituito:

- parametro `stato` con `state`;
- scrittura su `registroCausale` con scrittura su `volatileContainmentState`.

Aggiunto:

- guard clause `if (string.IsNullOrWhiteSpace(id)) return;`.

Mantenuto:

- `SaveGameState()` dopo la modifica dello stato.

### 1.11 Nuovo metodo `RegisterSecuritySignature(string signatureId)`

Aggiunto come sostituto di `ExtractTag`.

Fa queste operazioni:

- logga la firma rilevata;
- valida `signatureId`;
- assegna `currentTagID = signatureId`;
- aggiunge la firma a `securitySignaturesAcquired`;
- aggiunge la firma a `unlockedSecurityHistory`;
- se esiste `MissionManager.Instance`, chiama `MissionManager.Instance.RegistraCredenziale(signatureId)`;
- chiama `SaveGameState()`;
- se la firma e' nuova, invoca `OnSecuritySignatureAcquired`;
- se la firma e' nuova, invoca anche `OnTemporalTagAcquired` per compatibilita'.

Sostituisce il vecchio comportamento di `ExtractTag`, che prima:

- registrava un tag temporale;
- aggiornava `tagTemporaliAcquisiti`;
- aggiornava `storicoTagSbloccati`;
- emetteva solo `OnTemporalTagAcquired`.

### 1.12 Nuovo metodo `ResolveIncident(string incidentId)`

Aggiunto come sostituto di `ResolveAnachronism`.

Fa queste operazioni:

- rifiuta ID vuoti;
- aggiunge `incidentId` a `incidentsResolved`;
- se gia' presente, ritorna `false`;
- salva lo stato;
- invoca `OnIncidentResolved`;
- invoca anche `OnAnachronismResolved` per compatibilita';
- logga `Incidente risolto`.

### 1.13 Nuovo metodo `IsSecuritySignatureUnlocked(string signatureId)`

Aggiunto come sostituto di `IsTagUnlocked`.

Comportamento:

- ritorna `true` se `signatureId` e' vuoto;
- ritorna `true` se `signatureId` e' in `unlockedSecurityHistory`;
- altrimenti ritorna `false`.

Differenza rispetto al vecchio metodo:

- il vecchio `IsTagUnlocked` controllava solo `storicoTagSbloccati.Contains(tagID)`.

### 1.14 Nuovo metodo `AuthorizeReturnChannel(string signatureId)`

Aggiunto come sostituto di `ApriVarcoRitorno`.

Fa queste operazioni:

- ignora ID vuoti;
- aggiunge `signatureId` a `authorizedReturnChannels`;
- logga l'autorizzazione del canale operativo.

### 1.15 Nuovo metodo `ConsumeReturnChannel(string signatureId)`

Aggiunto come sostituto di `ConsumaVarcoRitorno`.

Fa queste operazioni:

- rimuove `signatureId` da `authorizedReturnChannels`;
- se rimosso, logga il canale consumato e ritorna `true`;
- se non esiste, ritorna `false`.

### 1.16 Wrapper legacy aggiunti

Aggiunto blocco wrapper:

```csharp
public void ExtractTag(string newTagID) => RegisterSecuritySignature(newTagID);
public bool ResolveAnachronism(string anachronismId) => ResolveIncident(anachronismId);
public bool IsTagUnlocked(string tagID) => IsSecuritySignatureUnlocked(tagID);
public void ApriVarcoRitorno(string tagID) => AuthorizeReturnChannel(tagID);
public bool ConsumaVarcoRitorno(string tagID) => ConsumeReturnChannel(tagID);
```

Motivo:

- mantenere compatibile il codice che chiama ancora le API GoldenCast.

### 1.17 `SaveGameState()`

Sostituito:

- costruzione manuale del wrapper con object initializer;
- salvataggio di soli campi GoldenCast con salvataggio sia dei campi nuovi sia dei campi legacy.

Ora scrive:

- `savedCredentialId = currentTagID`;
- `savedTagID = currentTagID`;
- `securitySignaturesAcquired`;
- `incidentsResolved`;
- `unlockedSecurityHistory`;
- `tagTemporaliAcquisiti` come copia di `securitySignaturesAcquired`;
- `anacronismiRisolti` come copia di `incidentsResolved`;
- `tagSbloccatiStorico` come copia di `unlockedSecurityHistory`.

Sostituito log:

- da `Dati serializzati e salvati` a `Stato Sector Containment salvato`.

### 1.18 `LoadGameState()`

Sostituito:

- reset di `registroCausale` con `volatileContainmentState.Clear()`;
- reset di `varchiApertiPerRitorno` con nuova istanza di `authorizedReturnChannels`;
- lettura esclusiva da `saveFilePath` con selezione tra nuovo salvataggio e salvataggio legacy.

Aggiunto:

- `pathToLoad`, che sceglie:
  - `SectorContainment_Save.json` se esiste;
  - `GoldenCast_Save.json` se il nuovo non esiste;
  - stringa vuota se non esistono salvataggi.

Aggiunto ripristino compatibile:

- `currentTagID = FirstNonEmpty(data.savedCredentialId, data.savedTagID, "KEYCARD_A01");`
- `securitySignaturesAcquired = MergeLists(data.securitySignaturesAcquired, data.tagTemporaliAcquisiti);`
- `incidentsResolved = MergeLists(data.incidentsResolved, data.anacronismiRisolti);`
- `unlockedSecurityHistory = MergeLists(data.unlockedSecurityHistory, data.tagSbloccatiStorico);`

Sostituito fallback iniziale:

- da `TAG_001` a `KEYCARD_A01`.

Sostituito log:

- da `Avvio di una nuova linea temporale` a `Avvio nuova emergenza di settore`.

### 1.19 `ResetDatiDebug()`

Sostituito reset di:

- `registroCausale` con `volatileContainmentState`;
- `tagTemporaliAcquisiti` con `securitySignaturesAcquired`;
- `anacronismiRisolti` con `incidentsResolved`;
- `storicoTagSbloccati` con `unlockedSecurityHistory`;
- `varchiApertiPerRitorno` con `authorizedReturnChannels`;
- `currentTagID = "TAG_001"` con `currentTagID = "KEYCARD_A01"`.

Aggiunto:

- cancellazione di `SectorContainment_Save.json`;
- cancellazione di `GoldenCast_Save.json`;
- uso helper `DeleteSaveFile`.

Mantenuto:

- ricaricamento della scena attiva.

### 1.20 Helper privati aggiunti

Aggiunto:

- `MergeLists(List<string> primary, List<string> legacy)`;
- `AddRange(HashSet<string> target, List<string> source)`;
- `FirstNonEmpty(params string[] values)`;
- `DeleteSaveFile(string path)`.

Scopo:

- migrare campi vecchi e nuovi senza duplicati;
- ignorare stringhe vuote;
- scegliere fallback robusto;
- cancellare salvataggi in modo riusabile.

## 2. `Assets/script/ScannerTemporale.cs`

### 2.1 Inspector

Sostituito:

- header `Configurazione Scanner` con `Scanner di Emergenza`;
- tooltip origine scan da riferimento al visore/Gabriel a descrizione neutra;
- tooltip layer da `oggetti che contengono Tag temporali` a `credenziali, terminali, focolai o anomalie legacy`.

### 2.2 `Start()`

Mantenuto:

- `telecameraPrincipale = Camera.main;`

Rimosso:

- commento ridondante sulla ricerca della Main Camera.

### 2.3 `EseguiScansione()`

Sostituito:

- condizione annidata `if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)` con guard clause:
  - `if (Keyboard.current == null || !Keyboard.current.qKey.wasPressedThisFrame) return;`

Aggiunto:

- fallback se `telecameraPrincipale` e' nulla;
- direzione scan da camera se presente, altrimenti `transform.forward`;
- log `Analisi emergenza emessa`;
- delega dell'analisi a `AnalizzaBersaglio(hitInfo.collider)`.

Sostituito comportamento raycast:

- prima gestiva solo `OstacoloCausale`;
- ora passa il collider a una funzione che riconosce piu' tipi.

Sostituito log vuoto:

- da `Nessun bersaglio intercettato nella traiettoria visiva` a `Nessun bersaglio operativo intercettato`.

### 2.4 Nuovo metodo `AnalizzaBersaglio(Collider target)`

Aggiunto controllo null:

- se `target == null`, ritorna.

Aggiunto riconoscimento `AccessCredentialPickup`:

- cerca sul collider;
- cerca nel parent;
- logga credenziale fisica rilevata;
- indica di avvicinarsi e premere `E`;
- ritorna.

Aggiunto riconoscimento `EmergencyHotspot`:

- cerca sul collider;
- cerca nel parent;
- logga focolaio d'emergenza identificato;
- indica che richiede procedura di contenimento;
- ritorna.

Aggiunto riconoscimento `OstacoloCausale` legacy:

- cerca sul collider;
- cerca nel parent;
- se `GameManager.Instance` esiste, chiama `RegisterSecuritySignature(obstacle.idCausale)`;
- logga firma di sicurezza acquisita;
- ritorna.

Aggiunto riconoscimento `IInteractable`:

- cerca sul collider;
- cerca nel parent;
- logga oggetto operativo analizzabile;
- ritorna.

Aggiunto fallback:

- se nessun componente e' riconosciuto, logga warning `nessun protocollo di emergenza riconosciuto`.

### 2.5 `OnDrawGizmos()`

Mantenuto:

- gizmo cyan;
- origine da `puntoDiOrigine` oppure corpo player;
- ray lungo `portataScanner`.

Rimosso:

- commento ridondante sul disegno della linea.

## 3. `Assets/script/TemporalTagData.cs`

### 3.1 Menu asset

Sostituito:

```csharp
[CreateAssetMenu(fileName = "NuovoTagTemporale", menuName = "Sistema Temporale/Tag Temporale")]
```

con:

```csharp
[CreateAssetMenu(fileName = "NuovaFirmaSicurezza", menuName = "Sector Containment/Firma Scanner")]
```

### 3.2 Campo `idTag`

Mantenuto:

- `public string idTag = "TAG_000";`

Aggiunto:

- tooltip che lo descrive come ID di firma, credenziale o criticita';
- nota che il campo resta legacy per compatibilita'.

### 3.3 Campo `epochName`

Mantenuto:

- `public string epochName;`

Sostituito tooltip:

- da epoca/luogo tipo Berlino Est 1961;
- a nome mostrato nello scanner: settore, terminale o anomalia.

### 3.4 Campo `description`

Mantenuto:

- `public string description;`
- `[TextArea(3, 5)]`.

Sostituito tooltip:

- da descrizione dell'anacronismo;
- a descrizione operativa della criticita'.

### 3.5 Campo `anachronismVideo`

Mantenuto:

- `public VideoClip anachronismVideo;`

Sostituito tooltip:

- da `quando il protocollo scanner completa tutti e tre i passaggi`;
- a `quando il protocollo scanner completa tutti i passaggi`.

### 3.6 Alias aggiunti

Aggiunto:

```csharp
public string SecuritySignatureId => idTag;
public string DisplayName => epochName;
public string IncidentDescription => description;
```

Motivo:

- usare lessico Sector Containment senza rompere dati serializzati.

## 4. `Assets/script/TestAnacronismo.cs`

### 4.1 Inspector

Sostituito:

- header `Dati Temporali Obbligatori` con `Dati Scansione Obbligatori`;
- tooltip `metadati dell'epoca associata` con `metadati della firma/criticita' scansionata`;
- header `Feedback Visivo (Stato)` con `Feedback Visivo`;
- tooltip `avvenuta sincronizzazione` con `avvenuta acquisizione`.

### 4.2 Campi booleani

Sostituito:

- `private bool isSynchronized = false;` con `private bool isSynchronized;`
- `private bool isScannerOpen = false;` con `private bool isScannerOpen;`

Rimosso:

- commento inline sulla variabile di stato UI.

### 4.3 `Interact()`

Sostituito:

- `if (isSynchronized || isScannerOpen) return;`
- con guard clause su due righe.

Sostituito log errore `tagData == null`:

- da `[ANACRONISMO] Dati 'tagData'...`;
- a `[SCANNER] Dati scansione mancanti...`.

Sostituito log errore `GameManager.Instance == null`:

- da impossibile aprire scanner per anacronismo;
- a `GameManager assente: impossibile aprire analisi`.

Mantenuto:

- apertura `AnachronismScannerUI.Open(tagData, CompleteScannerInteraction, CancelScannerInteraction);`

Motivo:

- UI legacy ancora collegata al progetto.

### 4.4 `ExecuteSynchronization()`

Sostituito:

- `if (GameManager.Instance == null) return;` con guard clause su due righe.

Sostituito chiamata:

- da `GameManager.Instance.ExtractTag(tagData.idTag);`
- a `GameManager.Instance.RegisterSecuritySignature(tagData.SecuritySignatureId);`

Mantenuto:

- cambio colore materiale se `targetRenderer != null`;
- `isSynchronized = true;`

Sostituito log finale:

- da `[SINCRONIZZAZIONE COMPLETATA] Estratto Tag ID... Epoca...`;
- a `[SCANSIONE COMPLETATA] Firma acquisita... DisplayName...`.

Rimosso:

- commenti XML e commenti numerati ormai non coerenti.

## 5. `README.md`

### 5.1 Descrizione progetto

Sostituito:

- `scanner temporale`;
- `gestione di anacronismi/sistemi compromessi`;
- `UI steampunk`.

Con:

- `scanner di emergenza`;
- `contenimento di focolai/sistemi compromessi`;
- `UI di missione`.

### 5.2 Indice documentazione

Aggiunto link:

- `docs/technical/MIGRATION_FROM_GOLDENCAST.md`.

### 5.3 Sezione persistenza

Sostituito titolo:

- `Persistenza e linea temporale`;
- con `Persistenza e stato globale`.

Sostituiti bullet:

- `tag temporali acquisiti` -> `firme di sicurezza e credenziali acquisite`;
- `anacronismi risolti` -> `incidenti/focolai risolti`;
- `storico dei tag sbloccati` -> `storico delle autorizzazioni sbloccate`;
- `varchi temporali aperti per il ritorno` -> `canali operativi temporanei`;
- `stato volatile degli ostacoli causali` -> `stato volatile degli ostacoli legacy`.

Sostituito path salvataggio:

- `Application.persistentDataPath/GoldenCast_Save.json`;
- con `Application.persistentDataPath/SectorContainment_Save.json`.

Aggiunta nota:

- se esiste `GoldenCast_Save.json`, viene letto come salvataggio legacy.

### 5.4 Sezione scanner

Sostituito titolo:

- `Scanner temporale`;
- con `Scanner di emergenza`.

Sostituita descrizione:

- da raycast per identificare ostacoli causali e acquisire tag;
- a scanner che riconosce credenziali, focolai, interagibili e ostacoli legacy trasformati in firme.

Sostituiti requisiti bersaglio:

- da solo `OstacoloCausale`;
- a `AccessCredentialPickup`, `EmergencyHotspot`, `IInteractable` o `OstacoloCausale`.

### 5.5 Struttura repository

Sostituito:

- `AsyncronQuest/ UI, tooltip, video, sistemi anacronismo`;
- con `AsyncronQuest/ UI, tooltip, video, sistemi scanner legacy`.

### 5.6 Documentazione di design

Aggiunto:

- `docs/technical/MIGRATION_FROM_GOLDENCAST.md`.

### 5.7 Troubleshooting scanner

Sostituito controllo bersaglio:

- da `verifica che il bersaglio abbia OstacoloCausale`;
- a verifica dei componenti `AccessCredentialPickup`, `EmergencyHotspot`, `IInteractable` o `OstacoloCausale`.

## 6. `docs/README.md`

### 6.1 Sezione Technical

Aggiunto:

- link a `technical/MIGRATION_FROM_GOLDENCAST.md`;
- descrizione: differenze tra GoldenCast e versione allineata al nuovo GDD.

### 6.2 Sezione Process

Aggiunto:

- link a `process/CHANGE_REPORT.md`;
- descrizione: report dettagliato delle modifiche effettuate.

## 7. `docs/design/GDD.md`

### 7.1 Collegamento prototipo Unity

Sostituito bullet `GameManager`:

- da `persistenza, stato globale e anacronismi risolti`;
- a `persistenza, stato globale, firme di sicurezza acquisite e incidenti risolti`;
- aggiunta nota sui wrapper legacy GoldenCast.

Sostituito bullet `ScannerTemporale`:

- da `lettura/interazione con oggetti e tag temporali`;
- a `scanner di emergenza per credenziali, focolai, terminali/interagibili e firme di sicurezza`;
- aggiunta nota sul nome classe legacy.

## 8. `docs/technical/ARCHITECTURE.md`

### 8.1 Sezione `Global State`

Sostituiti bullet responsabilita':

- `tag temporali acquisiti` -> `firme di sicurezza e credenziali acquisite`;
- `anacronismi risolti` -> `incidenti/focolai risolti`;
- `varchi temporali temporanei` -> `canali operativi temporanei`;
- aggiunto `migrazione dai salvataggi legacy GoldenCast`.

### 8.2 Sezione `Player Interaction`

Sostituito:

- `scanner temporale`;
- con `scanner di emergenza`.

### 8.3 Sezione `UI`

Sostituito:

- `feedback scanner/anomalie`;
- con `feedback scanner/anomalie legacy`.

### 8.4 Nuova sezione `Compatibilita' GoldenCast`

Aggiunto testo che specifica:

- la migrazione e' stata applicata prima al comportamento dei sistemi core;
- alcuni nomi restano legacy perche' Unity serializza riferimenti a script, prefab e scene;
- rinominare subito `ScannerTemporale`, `TemporalTagData`, `testAnacronismo` o UI puo' rompere riferimenti;
- i nuovi sviluppi devono usare lessico Sector Containment;
- i wrapper legacy restano come ponte tecnico.

Wrapper elencati:

- `ExtractTag`;
- `ResolveAnachronism`;
- `IsTagUnlocked`;
- `ApriVarcoRitorno`;
- `ConsumaVarcoRitorno`.

## 9. `docs/technical/MIGRATION_FROM_GOLDENCAST.md`

File nuovo.

### 9.1 Sezione `Scopo`

Aggiunto:

- spiegazione del passaggio da `ep2526-tt-goldencast` / GoldenCast a `Sector Containment: Emergency`.

### 9.2 Sezione `Direzione di design`

Aggiunto elenco del nuovo modello:

- operatore di emergenza;
- struttura sci-fi ad alta sicurezza;
- settori in quarantena;
- credenziali, chiavi e frequenze;
- focolai d'emergenza e terminali compromessi;
- droni, guardie e sistemi automatizzati;
- collasso strutturale;
- estrazione dal portellone di quarantena.

### 9.3 Sezione `Codice trasformato`

Aggiunto dettaglio per:

- `GameManager.cs`;
- `ScannerTemporale.cs`;
- `TestAnacronismo.cs`;
- `TemporalTagData.cs`.

### 9.4 Sezione `Sistemi gia' allineati al nuovo GDD`

Aggiunto elenco:

- `AccessCredentialPickup`;
- `EmergencyHotspot`;
- `QuarantineGate`;
- `StructuralHazard`;
- impostazioni di obiettivi, collasso e punteggio.

### 9.5 Sezione `Cosa resta legacy`

Aggiunto elenco:

- classi e file gia' referenziati da scene/prefab;
- namespace UI come `GoldenCast.UI`;
- UI AsyncronQuest con naming `Anachronism`;
- scena `Passato_1961`;
- documenti Word storici GoldenCast.

### 9.6 Sezione `Verifica`

Aggiunto:

- comando `dotnet build Crisis_protocol.slnx`;
- risultato `0` errori;
- nota sui warning Blockout nel primo giro di build.

## 10. `docs/process/CHANGE_REPORT.md`

File nuovo, poi riscritto in questa versione piu' puntuale.

### 10.1 Prima versione

Era un report descrittivo con:

- riepilogo;
- commit;
- file modificati;
- motivazione;
- verifica;
- prossimi interventi.

### 10.2 Versione attuale

Modificata per rispondere alla richiesta:

```text
riporta 1 a 1 tutte le modifiche
```

Ora contiene:

- scope e commit di riferimento;
- totale file/modifiche;
- elenco 1:1 per file;
- campi aggiunti;
- metodi aggiunti;
- wrapper mantenuti;
- log cambiati;
- path salvataggio cambiati;
- sezioni documentali cambiate;
- motivazioni tecniche quando servono.

## 11. Verifiche effettuate

### 11.1 Build dopo migrazione core

Comando:

```powershell
dotnet build Crisis_protocol.slnx
```

Risultato:

```text
0 errori
47 warning
```

Nota:

- i warning erano concentrati negli asset esterni `Blockout`, per API Unity obsolete.

### 11.2 Build finale dopo assestamento

Comando:

```powershell
dotnet build Crisis_protocol.slnx
```

Risultato:

```text
0 errori
0 warning
```

## 12. Modifiche non fatte intenzionalmente

Non sono stati rinominati fisicamente:

- `ScannerTemporale.cs`;
- `TemporalTagData.cs`;
- `TestAnacronismo.cs`;
- classe `testAnacronismo`;
- namespace `GoldenCast.UI`;
- UI `AnachronismScannerUI`;
- scene legacy come `Passato_1961`.

Motivo:

- Unity potrebbe perdere riferimenti serializzati in scene, prefab e ScriptableObject.

Non sono stati spostati asset dentro `Assets/`.

Motivo:

- spostamenti asset Unity richiedono controllo dei `.meta` e verifica immediata in editor.

Non sono stati modificati asset binari.

Motivo:

- la migrazione richiesta era su codice/documentazione e non su prefab o scene editate in editor.

## 13. Stato Git finale

Branch:

```text
main
```

Stato:

```text
main...origin/main
```

Commit pushati:

- `b0f234b Migrate core gameplay to sector containment GDD`;
- `99122b6 Add detailed change report`.
