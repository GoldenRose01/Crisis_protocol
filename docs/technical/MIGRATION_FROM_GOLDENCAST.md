# Migrazione da GoldenCast a Sector Containment

## Scopo

Questo documento traccia cosa e' stato cambiato rispetto al prototipo precedente `ep2526-tt-goldencast` / GoldenCast per allineare il progetto al nuovo GDD `Sector Containment: Emergency` (`Crisis Protocol: Red Line`).

## Direzione di design

Il progetto non e' piu' centrato su viaggi temporali, tag temporali e anacronismi come fantasia principale. Il nuovo modello e':

- operatore di emergenza in una struttura sci-fi ad alta sicurezza;
- settori in quarantena;
- credenziali, chiavi e frequenze di accesso;
- focolai d'emergenza e terminali compromessi da contenere;
- droni, guardie e sistemi automatizzati come minacce;
- collasso strutturale come timer/pressione sistemica;
- estrazione dal portellone di quarantena.

## Codice trasformato

### `Assets/script/GameManager.cs`

Il `GameManager` e' stato convertito da registro temporale GoldenCast a stato globale Sector Containment:

- nuovo salvataggio `SectorContainment_Save.json`;
- migrazione automatica dai vecchi campi di `GoldenCast_Save.json`;
- nuove collezioni persistenti per firme di sicurezza, incidenti risolti e storico autorizzazioni;
- nuovi eventi `OnSecuritySignatureAcquired` e `OnIncidentResolved`;
- nuove API `RegisterSecuritySignature`, `ResolveIncident`, `IsSecuritySignatureUnlocked`, `AuthorizeReturnChannel` e `ConsumeReturnChannel`;
- integrazione con `MissionManager.RegistraCredenziale` quando una firma/credenziale viene acquisita;
- reset debug aggiornato per cancellare sia il salvataggio nuovo sia quello legacy.

Restano wrapper legacy (`ExtractTag`, `ResolveAnachronism`, `IsTagUnlocked`, `ApriVarcoRitorno`, `ConsumaVarcoRitorno`) per mantenere compatibili scene, UI e script non ancora rinominati.

### `Assets/script/ScannerTemporale.cs`

Il vecchio scanner temporale e' ora uno scanner di emergenza:

- riconosce `AccessCredentialPickup`;
- riconosce `EmergencyHotspot`;
- riconosce interagibili generici `IInteractable`;
- converte gli `OstacoloCausale` legacy in firme di sicurezza;
- usa ancora il tasto `Q` e raycast dalla camera per mantenere il controllo gia' presente.

Il nome classe/file resta `ScannerTemporale` per evitare perdita di riferimenti serializzati in Unity.

### `Assets/script/TestAnacronismo.cs`

L'interazione di scansione e' stata risemantizzata:

- i testi/log parlano di scansione e firma acquisita;
- al completamento registra una firma di sicurezza nel `GameManager`;
- continua a usare la UI `AnachronismScannerUI` come ponte visuale legacy.

### `Assets/script/TemporalTagData.cs`

Lo ScriptableObject resta compatibile con i campi serializzati esistenti, ma ora espone alias Sector Containment:

- `SecuritySignatureId`;
- `DisplayName`;
- `IncidentDescription`.

Il menu di creazione asset e' stato spostato a `Sector Containment/Firma Scanner`.

## Sistemi gia' allineati al nuovo GDD

La cartella `Assets/script/SectorEmergency` contiene gia' i sistemi centrali del nuovo loop:

- `AccessCredentialPickup`;
- `EmergencyHotspot`;
- `QuarantineGate`;
- `StructuralHazard`;
- impostazioni di obiettivi, collasso e punteggio.

Questi sistemi sono ora collegati meglio al `GameManager` tramite registrazione credenziali/firme e documentazione aggiornata.

## Cosa resta legacy

Restano nomi e asset GoldenCast in alcune aree:

- classi e file Unity gia' referenziati da scene/prefab;
- namespace UI come `GoldenCast.UI`;
- UI AsyncronQuest con naming `Anachronism`;
- scene legacy come `Passato_1961`;
- documenti Word storici GoldenCast.

Questi elementi non sono stati rinominati in massa per evitare rotture di riferimenti Unity. La rinomina fisica va fatta in una fase dedicata, da Unity Editor, verificando subito scene, prefab e `.meta`.

## Verifica

La migrazione core e' stata verificata con:

```powershell
dotnet build Crisis_protocol.slnx
```

Risultato: `0` errori. I warning rimasti arrivano da asset esterni `Blockout` che usano API Unity obsolete.
