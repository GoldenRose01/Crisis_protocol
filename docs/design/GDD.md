# Game Design Document

## 1. Titolo

Titolo principale:

```text
Sector Containment: Emergency
```

Titolo alternativo:

```text
Crisis Protocol: Red Line
```

## 2. Obiettivo del gioco

Il giocatore interpreta un operatore di emergenza o tecnico specializzato all'interno di un'infrastruttura di rete e laboratorio ad alta sicurezza, colpita da un guasto critico a catena.

L'obiettivo e' spostarsi rapidamente tra settori isolati in quarantena, raccogliere credenziali, chiavi di sicurezza e frequenze, sbloccare zone sigillate e neutralizzare i focolai d'emergenza prima che la struttura subisca un collasso irreversibile.

Durante la missione il giocatore deve evitare droni di manutenzione, sistemi di difesa automatizzati e pericoli ambientali causati dal malfunzionamento dell'impianto.

## 3. Target audience

Target primario:

- appassionati di action-stealth sci-fi;
- giocatori interessati a survival puzzle strategici;
- utenti che apprezzano simulazioni di gestione delle emergenze sotto pressione.

Fascia consigliata:

```text
PEGI 12 / 12+
```

Profilo giocatore:

- ama ambientazioni tecnologiche e futuristiche;
- cerca esplorazione dinamica;
- apprezza meccaniche stealth;
- preferisce gestione di risorse, tempo e percorsi ottimali;
- accetta pressione costante tramite timer, allarmi e sistemi di contenimento.

## 4. Genere

```text
Sci-Fi Emergency Action-Stealth / Puzzle-Strategy
```

Visuale prevista:

- terza persona;
- oppure visuale isometrica/top-down.

## 5. Piattaforma

Piattaforma target:

```text
PC Windows
```

Distribuzione prevista:

```text
Steam / build desktop Windows
```

## 6. Gameplay di base

### Main loop

1. Rilevamento emergenza e briefing

   Dalla sala di controllo centrale, il giocatore individua i settori critici dell'impianto in cui sono comparsi focolai d'emergenza: falle nel sistema, cortocircuiti, guasti ai reattori o fughe di materiale.

2. Infiltrazione e raccolta credenziali

   Il giocatore raggiunge il settore isolato, esplora la zona ed evita le routine di pattugliamento dei droni di sicurezza compromessi e dei sistemi automatizzati andati in cortocircuito. Durante l'infiltrazione raccoglie autorizzazioni, schede d'accesso e chiavi crittografiche.

3. Contenimento del focolaio

   Le credenziali raccolte permettono di sbloccare terminali e aree sigillate. Il giocatore interagisce direttamente con anomalie, guasti critici e terminali compromessi per disattivare il pericolo e stabilizzare il settore.

4. Estrazione e spostamento tra settori

   Dopo la neutralizzazione del pericolo, il portellone di quarantena si sblocca. Il giocatore deve estrarre rapidamente e spostarsi verso il settore successivo prima che il guasto provochi un effetto domino.

### Win condition

Il giocatore vince se:

- contiene e disattiva tutti i focolai d'emergenza;
- completa l'estrazione in sicurezza;
- evita il collasso totale della struttura.

### Lose condition

Il giocatore perde se:

- i punti vita dell'operatore arrivano a zero;
- droni, torrette o trappole ambientali neutralizzano l'operatore;
- il contatore di collasso raggiunge il 100%;
- il tempo scade prima della neutralizzazione dei focolai.

### Sistema di punteggio

Il punteggio finale considera:

- tempo di intervento;
- percentuale di integrita' residua dell'infrastruttura;
- completamento stealth senza rilevamenti;
- danni subiti;
- allarmi attivati;
- tentativi errati su chiavi di sicurezza, frequenze o terminali.

## 7. Progressione della difficolta'

### Complessita' dei focolai e delle credenziali

Nei settori avanzati, i guasti richiedono sequenze di calibrazione e decrittazione piu' complesse. Le credenziali si trovano in aree piu' rischiose, obbligando il giocatore a pianificare deviazioni e priorita'.

### IA nemica e pattugliamento dinamico

Droni e sistemi di difesa diventano piu' aggressivi:

- percorsi di pattuglia piu' rapidi;
- coni di visione piu' ampi;
- possibile rilevamento tramite rumore o prossimita';
- risposta piu' severa dopo l'attivazione dei terminali.

### Margine di reazione ridotto

Il collasso strutturale avanza piu' velocemente nei livelli avanzati. Il giocatore deve ottimizzare percorsi, uso delle credenziali e ordine di contenimento.

### Anomalie e pericoli ambientali

La difficolta' aumenta anche tramite ostacoli ambientali:

- glitch visivi e distorsioni shader;
- barriere d'emergenza ad alta tensione;
- settori saturati da sostanze pericolose;
- fughe di gas o vapore;
- vie di fuga temporaneamente bloccate.

## 8. Riferimenti

### Alien: Isolation

Riferimento per tensione costante, ambienti chiusi, evasione dei sistemi ostili e interazione con terminali sotto pressione.

### Invisible, Inc.

Riferimento per infiltrazione stealth, raccolta di pass di sicurezza, bypass di porte bloccate e gestione dell'allarme crescente.

### Heat Signature

Riferimento per la gestione rapida dei pericoli in settori isolati di un'infrastruttura tecnologica.

### Dead Space

Riferimento per la resa scenica delle riparazioni d'emergenza, dei guasti strutturali e dei pericoli ambientali in un impianto isolato.

## 9. Asset previsti

### Modelli 3D e ambientazione

- corridoi modulari sci-fi;
- laboratori e infrastrutture tecnologiche;
- terminali di sicurezza;
- porte e portelli di quarantena sigillati;
- quadri elettrici;
- droni di pattugliamento;
- bot di manutenzione;
- sistemi di sicurezza automatizzati;
- operatore o tecnico d'emergenza.

### Shader e VFX

- shader glitch;
- interferenze visive;
- distorsioni cromatiche;
- VFX attorno ai focolai d'emergenza;
- effetti sui terminali compromessi;
- particelle per gas, vapore e cortocircuiti;
- allarmi luminosi;
- campi di forza e barriere di sicurezza.

### Scripting e meccaniche Unity

- character controller per movimento in terza persona o isometrico;
- IA nemica con pattugliamento waypoint/NavMesh;
- rilevamento tramite cono di visione, udito o prossimita';
- stati di inseguimento e combattimento;
- interazione con terminali;
- inserimento di credenziali, chiavi crittografiche e frequenze;
- sblocco di settori in quarantena;
- HUD per integrita' della struttura, timer, codici raccolti e livello d'allarme.

## 10. Collegamento con il prototipo Unity

Il prototipo corrente implementa gia' diversi pilastri del GDD:

- `MissionManager`: gestione missione, collasso, punteggio e win/lose condition.
- `GameManager`: persistenza, stato globale, firme di sicurezza acquisite e incidenti risolti. Mantiene wrapper legacy GoldenCast per non rompere prefab e UI esistenti.
- `EmergencyHotspot`: focolai d'emergenza da contenere.
- `AccessCredentialPickup`: raccolta credenziali.
- `QuarantineGate`: estrazione dopo completamento obiettivi.
- `ViaggiatoreTemporale`, `GuardiaNpc`, `DroneRonda`: minacce, pattuglie e combattimento.
- `ScannerTemporale`: scanner di emergenza per credenziali, focolai, terminali/interagibili e firme di sicurezza. Il nome classe e' legacy per compatibilita' Unity.
- `HUDManager` e UI AsyncronQuest: feedback di missione, morte, tooltip e interfacce.

## 11. Priorita' di sviluppo

1. Rendere stabile il loop completo credenziale -> focolaio -> estrazione.
2. Migliorare feedback HUD per collasso, credenziali, focolai e allarmi.
3. Raffinare pattuglie e rilevamento dei droni.
4. Differenziare i focolai con requisiti e VFX specifici.
5. Bilanciare punteggio, penalita' e velocita' del collasso.
6. Preparare una build Windows pulita per test e consegna.
