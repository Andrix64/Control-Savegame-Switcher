# Sviluppo

Note tecniche per chi lavora sul codice. La guida per chi usa l'app è nel
[README](../README.md).

## Percorsi

| Cosa | Percorso |
|---|---|
| Salvataggi del gioco | `<Steam>\userdata\<IDAccount>\870780\` |
| Dati dell'app | `%LOCALAPPDATA%\ControlSaveManager\` (`AppData.Root`) |
| Profili | `<dati>\<IDAccount>\Profili\Profilo_<nome>\` |
| Backup | `<dati>\<IDAccount>\Backup\<motivo>_<data>\` |
| Savestate | `<dati>\<IDAccount>\Savestate\Profilo_<nome>\<data>_<nome savestate>\` |
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
preseleziona l'ultimo scelto. Ogni account ha profili, backup e savestate
separati.

Il file `remotecache.vdf` di Steam non viene mai copiato né cancellato.

## Sicurezza delle copie

- Ogni backup e ogni copia viene confrontata byte per byte con l'originale.
- Attivazione e "Salva i progressi" lavorano in due passaggi: la sorgente viene
  copiata in `<cartella>.nuovo`, verificata, e poi le cartelle vengono
  scambiate (la vecchia passa per `<cartella>.vecchio` ed è eliminata). Se la
  cartella è in uso e non si può spostare, si ripiega sulla copia diretta dalla
  copia già verificata. In caso di errore si ripristina il backup.
- `stato.ini` viene scritto in `stato.ini.tmp` e poi sostituito.
- Una sola istanza dell'app alla volta (`Mutex`).
- Tutte le operazioni sono bloccate mentre Control è in esecuzione: la finestra
  lo controlla ogni 2 secondi.

## Formati

### `stato.ini`

```ini
ProfiloAttivo=Profilo_Mario
AttivatoIl=22/10/2026 18:30:00
SalvatoIl=-
BackupDaConservare=20
```

`-` indica un valore vuoto. `BackupDaConservare=0` conserva tutti i backup.
Le date sono sempre scritte come `gg/MM/aaaa HH:mm:ss` (cultura invariante) e
mostrate nel formato della lingua di Windows.

### Nomi delle cartelle

I nomi delle cartelle sono dati su disco e restano in italiano qualunque sia la
lingua dell'app.

- Profili: `Profilo_<nome>`, con lettere ASCII, cifre, `_` e `-`, massimo 50
  caratteri. Le cartelle in `Profili` che non rispettano la regola vengono
  ignorate.
- Backup: `Prima_salvataggio_<profilo>` contiene il profilo prima del
  salvataggio, `Eliminato_<profilo>` un profilo eliminato; gli altri
  (`Prima_di_<profilo>`, `Prima_creazione`, `Prima_ripristino`) i salvataggi del
  gioco. Il testo mostrato viene da `SaveManager.BackupReasonText`.

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
| `SaveManager.cs` | Tutta la logica: ricerca di Steam e degli account, profili, attivazione, salvataggio, savestate, backup, Steam Cloud. |
| `FileOps.cs` | Copia, verifica e sostituzione di cartelle. |
| `AppState.cs` | Lettura e scrittura di `stato.ini`. |
| `AppData.cs` | Cartella dei dati dell'app e spostamento da Documenti. |
| `Vdf.cs` | Lettore dei file di configurazione di Steam. |
| `Log.cs` | Registro delle operazioni e degli errori imprevisti. |
| `Result.cs` | Esito delle operazioni (messaggio, backup creato, errore grave). |
| `Loc.cs`, `Strings.resx`, `Strings.it.resx` | Testi in inglese e italiano. |
| `MainForm.cs` | Finestra principale (WinForms). |
| `NewProfileForm.cs` | Finestra "Nuovo profilo", usata anche per rinominare. |
| `AccountForm.cs` | Scelta dell'account Steam all'avvio. |
| `SnapshotForms.cs` | Finestre "Savestate" e "Backup" e la loro lista. |
| `MessageDialog.cs` | Finestre di messaggio a tema, al posto di `MessageBox`. |
| `Theme.cs` | Colori, caratteri e icona del tema scuro. |
| `ThemedControls.cs` | Controlli disegnati a mano: pulsanti, schede, intestazione, barra di stato, menu. |
| `app.ico` | Icona dell'exe e delle finestre. |
| `Program.cs` | Avvio dell'applicazione e versione. |
| `Properties\PublishProfiles\win-x64.pubxml` | Profilo di pubblicazione dell'exe singolo. |
| `scripts\release.ps1` | Crea lo zip di rilascio. |
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
scambio delle cartelle, profili, savestate, backup, `stato.ini`, Steam Cloud,
account e testi tradotti. Il registro dei test va in `%TEMP%\csm_test_log.txt`.
I test girano in italiano qualunque sia la lingua di Windows.

```bash
dotnet test tests/ControlSaveManager.Tests
```

Le finestre non sono coperte dai test. Non c'è CI (il repository è su Forgejo
senza runner): i test si eseguono a mano e comunque da `scripts\release.ps1`
prima di ogni rilascio.

## Rilascio

1. Aggiorna `<Version>` in `ControlSaveManager.csproj` e il
   [CHANGELOG](../CHANGELOG.md), poi fai il commit. La versione compare
   nell'intestazione della finestra, nelle proprietà dell'exe e nel registro.
2. Crea lo zip:

   ```bash
   powershell -ExecutionPolicy Bypass -File scripts/release.ps1
   ```

   Lo script rifiuta di partire con modifiche non committate, esegue i test,
   pubblica l'exe e crea in `dist\` lo zip `ControlSaveManager-<versione>-win-x64.zip`
   (exe, README, CHANGELOG e LICENSE) con il file `.sha256`.
3. Crea il tag della versione e mandalo su Forgejo:

   ```bash
   git tag v0.9.0
   ```

   ```bash
   git push origin main v0.9.0
   ```

4. Su Forgejo, in **Release → Nuova release**, scegli il tag, allega lo zip
   e il file `.sha256` da `dist\` e pubblica.

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

- [Piano per l'interfaccia grafica](PIANO_GUI.md)
- [Bug noti](BUG_NOTI.md)
- [Cose da fare](TODO.md)
