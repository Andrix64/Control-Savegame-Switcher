# Sviluppo

Note tecniche per chi lavora sul codice. La guida per chi usa l'app è nel
[README](../README.md).

## Percorsi

| Cosa | Percorso |
|---|---|
| Salvataggi del gioco | `<Steam>\userdata\<IDAccount>\870780\` |
| Dati dell'app | `%LOCALAPPDATA%\ControlSaveManager\` (`AppData.Root`) |
| Partite | `<dati>\<IDAccount>\Profili\Profilo_<nome>\` |
| Nota e tempo di gioco | `<dati>\<IDAccount>\Info\Profilo_<nome>.ini` |
| Copie automatiche | `<dati>\<IDAccount>\Backup\<motivo>_<data>\` più `<motivo>_<data>.ini` accanto |
| Punti salvati | `<dati>\<IDAccount>\Savestate\Profilo_<nome>\<data>_<nome punto>\` |
| Stato | `<dati>\<IDAccount>\stato.ini` |
| Registro | `<dati>\log.txt` (oltre 1 MB diventa `log.old.txt`) |
| Ultimo account usato | `<dati>\account.txt` |
| Impostazione Steam Cloud | `<Steam>\userdata\<IDAccount>\7\remote\sharedconfig.vdf` (solo lettura) |

I dati stanno in `%LOCALAPPDATA%` e non in Documenti perché Documenti è spesso
sincronizzata con OneDrive, che blocca i file durante il caricamento, li lascia
solo nel cloud ("file su richiesta") e crea copie "in conflitto": tutte cose che
in una cartella di salvataggi fanno fallire le copie o mischiano le partite.
`Local` e non `Roaming` perché i backup possono essere grandi.

Le versioni di sviluppo precedenti usavano `Documenti\ControlSaves`. All'avvio
`AppData.MoveFromDocuments` sposta quella cartella: sullo stesso disco con un
solo `Directory.Move`, su dischi diversi copiando in `ControlSaveManager.spostamento`,
verificando ogni file e solo dopo eliminando l'originale. Se ci sono dati in
tutte e due le cartelle non tocca niente e usa la nuova; se lo spostamento
fallisce continua a usare la vecchia. L'esito va nel registro e viene mostrato
una volta all'utente.

La cartella di Steam viene letta dal registro (`HKCU\Software\Valve\Steam\SteamPath`),
con ripiego sui percorsi standard in `Program Files`.

Se più account Steam hanno i salvataggi di Control, all'avvio l'app chiede
quale usare (nome letto da `userdata\<IDAccount>\config\localconfig.vdf`) e
preseleziona l'ultimo scelto. Ogni account ha le sue partite, copie e punti
salvati.

Nel codice e sul disco restano i nomi storici: *profilo* è una partita,
*savestate* un punto salvato, *backup* una copia automatica.

Il file `remotecache.vdf` di Steam, al primo livello della cartella del gioco,
non viene mai copiato né cancellato. Tutto il resto viene copiato e verificato,
a qualsiasi profondità: non ci sono altre esclusioni (un file con lo stesso
nome più in basso è un salvataggio come gli altri).

## Sicurezza delle copie

- Ogni backup e ogni copia viene confrontata byte per byte con l'originale.
- Attivazione e "Salva i progressi" lavorano in due passaggi: la sorgente viene
  copiata in `<cartella>.nuovo`, verificata, e poi le cartelle vengono
  scambiate (la vecchia passa per `<cartella>.vecchio` ed è eliminata). Se la
  cartella è in uso e non si può spostare, si ripiega sulla copia diretta dalla
  copia già verificata. In caso di errore si ripristina il backup.
- `stato.ini` viene scritto in `stato.ini.tmp` e poi sostituito.
- Recupero all'avvio di un'operazione interrotta (per esempio da uno
  spegnimento): prima di cercare gli account, per ogni cartella del gioco, e
  all'apertura, per `Profili` e `Savestate`. Se manca la cartella ma c'è
  `<cartella>.vecchio`, questa torna al suo posto (è lo stato di prima
  dell'operazione); se la cartella c'è, gli avanzi `.nuovo` e `.vecchio` vengono
  eliminati. Una rinomina solo di maiuscole interrotta (`<profilo>.rinomina`)
  viene completata. Un `.nuovo` da solo non viene toccato. Gli interventi
  finiscono nel registro.
- Un account viene riconosciuto se ha la cartella `870780`, anche senza
  `remote` (che manca dopo aver attivato un profilo creato a mano).
- Una sola istanza dell'app alla volta (`Mutex`).
- Tutte le operazioni che toccano i salvataggi del gioco sono bloccate mentre
  Control è in esecuzione, sia nella logica (`IsGameRunning` in ogni operazione)
  sia nell'interfaccia: la finestra principale e la Cronologia lo controllano
  ogni 2 secondi e disattivano i pulsanti. Il launcher si chiama
  `Control.exe` come il Pannello di controllo: si scarta quello nella cartella
  di Windows, e se il percorso non è leggibile (processo avviato come
  amministratore) nel dubbio conta come gioco aperto.
- All'avvio la finestra principale segnala (e scrive nel registro) le cartelle
  in `Profili` ignorate perché il nome non è valido.

## Partite e sessioni di gioco

- **Gioca questa** (`MainForm.PlayAsync`): se il gioco ha progressi non salvati
  li salva nella partita di adesso, poi attiva quella scelta e avvia Control
  con `steam://rungameid/870780` (`SaveManager.LaunchGame`). Con
  `AvviaGioco=0` o "Carica senza avviare" non avvia e offre **Annulla**, che
  rifà lo stesso percorso verso la partita di prima.
- **Sessioni**: la finestra principale controlla il gioco ogni 2 secondi.
  All'avvio del gioco segna l'ora (quella del processo, se leggibile) e la
  partita; alla chiusura aggiunge il tempo con `AddPlayTime` e, con
  `SalvaAllaChiusura=1`, salva i progressi (`SaveProgress(onGameExit: true)`).
  Funziona solo con l'app aperta.
- **Opzioni condivise** (`OpzioniCondivise=1`): attivando una partita, il file
  `remote\preferences_data` del gioco viene copiato prima nella partita;
  tornando a un punto o a una copia viene rimesso dopo.
- **Proprietario delle copie**: ogni copia ha accanto un `.ini` con la partita
  a cui appartiene il contenuto, l'operazione (`BackupKind`) e un dettaglio. La
  Cronologia di una partita mostra solo le sue copie; quelle senza proprietario
  (versioni precedenti, o fatte senza partita nel gioco) stanno in una sezione a
  parte e chiedono conferma.
- **Partite eliminate**: la cartella diventa una copia `Eliminato_<partita>`
  con nota e tempo di gioco nel suo `.ini`; «Recupera» la rimette tra le
  partite (con un nome libero se quello è stato riusato). I punti salvati
  vengono eliminati.
- **Primo avvio**: se non ci sono partite ma il gioco ha salvataggi,
  `AdoptGameSaves` li salva come prima partita e la segna come nel gioco, senza
  toccare la cartella del gioco.

## Formati

### `stato.ini`

```ini
ProfiloAttivo=Profilo_Mario
AttivatoIl=22/10/2026 18:30:00
SalvatoIl=-
BackupDaConservare=20
AvviaGioco=1
SalvaAllaChiusura=1
OpzioniCondivise=0
```

`-` indica un valore vuoto. `BackupDaConservare=0` conserva tutte le copie.
Le date sono sempre scritte come `gg/MM/aaaa HH:mm:ss` (cultura invariante) e
mostrate nel formato della lingua di Windows.

### Nomi delle cartelle

I nomi delle cartelle sono dati su disco e restano in italiano qualunque sia la
lingua dell'app.

- Partite: `Profilo_<nome>`, con lettere (anche accentate), cifre, spazi, `_`
  e `-`, senza spazi all'inizio o alla fine, massimo 50 caratteri. Le cartelle
  in `Profili` che non rispettano la regola vengono ignorate e segnalate.
- Copie: `Prima_salvataggio_<partita>` contiene la partita prima del
  salvataggio, `Eliminato_<partita>` una partita eliminata; gli altri
  (`Prima_di_<partita>`, `Prima_creazione`, `Prima_ripristino`) i salvataggi del
  gioco. Il testo mostrato viene da `SaveManager.BackupDescription`, che usa il
  `.ini` accanto alla copia o, per le copie delle versioni precedenti, il nome.

### File `.ini` di una partita e di una copia

```ini
; Info\Profilo_Mario.ini
Nota=Missione Polaris, prima di Ahti
TempoDiGioco=45000
UltimaSessione=2026-10-22 22:11:00

; Backup\Prima_di_Profilo_Luigi_2026-10-22_22-11-03-120.ini
Partita=Profilo_Mario
Motivo=attivazione
Dettaglio=Profilo_Luigi
```

`TempoDiGioco` è in secondi. `Motivo` è uno di `attivazione`, `salvataggio`,
`chiusura`, `ripristino`, `creazione`, `eliminazione`.

### Partita esportata

Uno zip con `partita.ini` (`Formato=1`, `Nome`, `Nota`, `TempoDiGioco`,
`UltimaSessione`), `salvataggi\` (la cartella della partita) e, se ci sono,
`punti\` (i punti salvati). L'importazione rifiuta gli zip senza `salvataggi\`
e i percorsi che escono dalla cartella.

### Steam Cloud

Steam salva l'impostazione "Mantieni i salvataggi su Steam Cloud" del gioco in
`sharedconfig.vdf`, nel blocco `apps → 870780 → "cloudenabled"`. L'app legge
solo quel valore (`Vdf.cs` è un lettore minimo del formato VDF): `"0"` vuol dire
disattivato, l'assenza della chiave o del file attivo (è il default di Steam).
L'impostazione generale "Abilita Steam Cloud" di Steam non viene letta.

Con il cloud disattivato l'avviso "Steam è aperto" non viene mostrato. Il file
`remotecache.vdf` viene rigenerato da Steam anche con il cloud disattivato: è
normale.

## Lingue

I testi dell'interfaccia sono in `Strings.resx` (inglese, lingua di ripiego) e
`Strings.it.resx` (italiano). La lingua segue quella di Windows
(`CultureInfo.CurrentUICulture`). La classe `Strings` viene generata dalla
build (`StronglyTypedLanguage` nel `.csproj`), senza bisogno di Visual Studio.

- Testi con valori: `Loc.F(Strings.Chiave, valore1, valore2)`.
- Per aggiungere un testo: stessa chiave in entrambi i file. Il test
  `LocalizationTests` controlla che ogni chiave esista in tutte e due le lingue
  con gli stessi segnaposto `{0}`, `{1}`...
- Per aggiungere una lingua: `Strings.<codice>.resx` con le stesse chiavi.
- Il registro (`log.txt`) resta in italiano.

## Struttura del codice

| File | Contenuto |
|---|---|
| `SaveManager.cs` | Logica principale: ricerca di Steam e degli account, partite, attivazione, salvataggio, punti salvati, copie automatiche, Steam Cloud. |
| `SaveManager.Playthroughs.cs` | Nota e tempo di gioco, proprietario delle copie, partite eliminate, primo avvio, impostazioni, opzioni condivise, esportazione, importazione, avvio del gioco. |
| `IniFile.cs` | Lettura e scrittura sicura dei file `.ini`. |
| `FileOps.cs` | Copia, verifica e sostituzione di cartelle. |
| `AppState.cs` | Lettura e scrittura di `stato.ini`. |
| `AppData.cs` | Cartella dei dati dell'app e spostamento da Documenti. |
| `Vdf.cs` | Lettore dei file di configurazione di Steam. |
| `Log.cs` | Registro delle operazioni e degli errori imprevisti. |
| `Result.cs` | Esito delle operazioni (messaggio, backup creato, errore grave). |
| `Loc.cs`, `Strings.resx`, `Strings.it.resx` | Testi in inglese e italiano. |
| `MainForm.cs` | Finestra principale: schede delle partite, barre, avvisi, sessioni di gioco. |
| `PlaythroughControls.cs` | Barra informativa (`InfoBar`), pillola e scheda di una partita. |
| `HistoryForm.cs` | Cronologia della partita: punti salvati e copie automatiche. |
| `PromptForms.cs` | Finestre Nota, Impostazioni e Primo avvio. |
| `NewProfileForm.cs` | Finestra "Nuova partita", usata anche per rinominare. |
| `AccountForm.cs` | Scelta dell'account Steam all'avvio. |
| `SnapshotForms.cs` | Lista a tema, base delle finestre a elenco e finestra delle partite eliminate. |
| `MessageDialog.cs` | Finestre di messaggio a tema; `Confirm` usa verbi sui pulsanti. |
| `Theme.cs` | Colori, caratteri e icona del tema scuro. |
| `ThemedControls.cs` | Controlli disegnati a mano: pulsanti, schede, intestazione, menu. |
| `app.ico` | Icona dell'exe e delle finestre. |
| `Program.cs` | Avvio dell'applicazione e versione. |
| `Properties\PublishProfiles\win-x64.pubxml` | Profilo di pubblicazione dell'exe singolo. |
| `scripts\release.ps1` | Crea lo zip di rilascio (in locale e su GitHub Actions). |
| `.github\workflows\build.yml` | Build, test, firma con SignPath e bozza di rilascio su GitHub. |
| `CODE_SIGNING.md` | Politica di firma richiesta da SignPath. |
| `tests\ControlSaveManager.Tests\` | Test xUnit. |

La vecchia versione da console è nella cronologia git (commit `17cd279`).

## Compilazione

Richiede .NET 10 SDK.

```bash
dotnet build ControlSaveManager.csproj -c Release
```

Exe singolo per Windows a 64 bit, con il runtime .NET incluso (chi lo usa non
deve installare nulla, circa 50 MB), in `bin\publish\win-x64\`:

```bash
dotnet publish ControlSaveManager.csproj -p:PublishProfile=win-x64
```

Per il debug in VS Code ci sono `.vscode\launch.json` e `.vscode\tasks.json`.

## Test

Test xUnit in `tests/ControlSaveManager.Tests`: lavorano su cartelle finte in
`%TEMP%` (non toccano Steam né i dati veri dell'app) e coprono copie e
scambio delle cartelle, partite, punti salvati, copie automatiche e il loro
proprietario, nota e tempo di gioco, partite eliminate, esportazione e
importazione, opzioni condivise, primo avvio, `stato.ini`, Steam Cloud,
account, recupero delle operazioni interrotte, rilevamento del launcher,
spostamento dei dati da Documenti, testi tradotti e righe delle schede. Il
registro dei test va in `%TEMP%\csm_test_log.txt`.
I test girano in italiano qualunque sia la lingua di Windows.

```bash
dotnet test tests/ControlSaveManager.Tests
```

Le finestre non sono coperte dai test. Su GitHub i test girano a ogni push e
pull request (vedi sotto); in locale li esegue anche `scripts\release.ps1`.

## Build su GitHub

Il repository è su [GitHub](https://github.com/Andrix64/Control-Savegame-Switcher)
(serve per la firma gratuita, vedi sotto). Il workflow
`.github/workflows/build.yml` gira su Windows a ogni push su `main`, a ogni pull
request e a mano (*Run workflow*):

1. sui tag `v*` controlla che il tag corrisponda a `<Version>`;
2. esegue i test;
3. pubblica l'exe singolo e lo carica come artifact `exe-non-firmato`;
4. **solo sui tag**, se SignPath è configurato, manda l'exe a SignPath, aspetta
   la firma (da approvare a mano su SignPath) e controlla che sia valida;
5. crea lo zip con `scripts/release.ps1 -Exe <exe>` e lo carica come artifact
   `control-save-manager`, con i risultati dei test;
6. sui tag prepara una **bozza** di rilascio con zip e `.sha256`, e scrive se
   l'exe è firmato oppure no.

Senza SignPath la build funziona lo stesso: lo zip semplicemente non è firmato.

## Firma del codice

Smart App Control e SmartScreen (vedi sotto) richiedono una firma da
un'autorità riconosciuta da Microsoft: un certificato autofirmato non basta. La
firma è gratuita tramite [SignPath Foundation](https://signpath.org/), che firma
i progetti open source con licenza OSI (qui MIT) compilati da una build
automatica in un repository pubblico. La politica di firma del progetto è in
[CODE_SIGNING.md](../CODE_SIGNING.md).

Configurazione, una volta sola:

1. Pubblica almeno una versione (anche non firmata) e fai domanda su
   [signpath.org](https://signpath.org/), indicando il repository e la pagina
   [CODE_SIGNING.md](../CODE_SIGNING.md).
2. Su SignPath: crea il progetto collegato al repository GitHub, con una
   *artifact configuration* che firma il file `ControlSaveManager.exe`
   (Authenticode) e la *signing policy* `release-signing`; crea un utente CI e
   il suo token.
3. Su GitHub, in *Settings → Secrets and variables → Actions*:
   - secret `SIGNPATH_API_TOKEN`: il token dell'utente CI;
   - variable `SIGNPATH_ORGANIZATION_ID`: l'ID dell'organizzazione;
   - variable `SIGNPATH_PROJECT_SLUG`: lo slug del progetto;
   - variable `SIGNPATH_SIGNING_POLICY_SLUG`: facoltativa, di base
     `release-signing`.

I nomi esatti (slug, policy) li dà SignPath quando approva il progetto:
controllali e adatta le variabili. Anche il nome e la versione dell'azione
`signpath/github-action-submit-signing-request` vanno verificati sulla loro
documentazione al momento della configurazione.

## Rilascio

1. Aggiorna `<Version>` in `ControlSaveManager.csproj` e il
   [CHANGELOG](../CHANGELOG.md), poi fai il commit. La versione compare
   nell'intestazione della finestra, nelle proprietà dell'exe e nel registro.
2. Crea il tag della versione e mandalo su GitHub:

   ```bash
   git tag v0.9.0
   ```

   ```bash
   git push origin main v0.9.0
   ```

3. Su SignPath approva la richiesta di firma (arriva da GitHub Actions).
4. Su GitHub, in **Releases**, controlla la bozza (deve dire "Exe firmato da
   SignPath Foundation") e pubblicala.

Per Forgejo, o senza GitHub, lo zip si crea anche in locale (non firmato):

```bash
powershell -ExecutionPolicy Bypass -File scripts/release.ps1
```

Lo script rifiuta di partire con modifiche non committate, esegue i test,
pubblica l'exe e crea in `dist\` lo zip `ControlSaveManager-<versione>-win-x64.zip`
(exe, README, CHANGELOG e LICENSE) con il file `.sha256`. Con
`-Exe <percorso>` usa un exe già pronto invece di pubblicarlo.

## Smart App Control

Smart App Control (Windows 11) può bloccare l'exe compilato in proprio, che non
è firmato: l'app si chiude subito all'avvio e nel registro eventi compare "An
Application Control policy has blocked this file". È successo con una versione
che chiamava direttamente `dwmapi.dll` e `uxtheme.dll` (barra del titolo e
barre di scorrimento scure); senza quelle chiamate parte. Meglio evitare
`DllImport` nel progetto.

Il giudizio però è sul singolo file (hash), non solo sul codice: è capitato che
una build Debug venisse bloccata (`FileLoadException ... 0x800711C7`) mentre la
Release degli stessi sorgenti partiva. Una build deterministica produce sempre
lo stesso file, quindi ricompilare non serviva. Per questo in Debug la build
non è deterministica: se viene bloccata, si ricompila tutto (task VS Code
`rebuild`, oppure `dotnet build --no-incremental`) e di norma il nuovo file
passa. Per chi scarica l'exe la soluzione vera è la firma del codice (vedi
[TODO](TODO.md)). Il registro degli eventi è
`Microsoft-Windows-CodeIntegrity/Operational` (eventi 3033 e 3077).

## Altri documenti

- [Bug noti](BUG_NOTI.md)
- [Cose da fare](TODO.md)
- [Novità di ogni versione](../CHANGELOG.md)

Il piano originale per passare dalla console all'interfaccia grafica è stato
completato ed eliminato; è nella cronologia git (fino al commit `90c00da`).
