# Control Save Manager

Piccolo strumento per Windows che gestisce **più profili di salvataggio** per
*Control* (Steam, AppID `870780`). Permette di passare da una partita all'altra
sostituendo i file di salvataggio del gioco, con un backup automatico prima di
ogni operazione.

## Funzioni

| Funzione | Cosa fa |
|---|---|
| **Cambia profilo** | Senza conferme: se ci sono progressi non salvati li salva nel profilo attivo, fa il backup dei salvataggi attuali, poi copia il profilo scelto nella cartella del gioco. |
| **Salva i progressi** | Fa il backup del profilo attivo, poi copia i salvataggi del gioco nel profilo. Chiede conferma solo se la cartella del gioco è vuota. |
| **Savestate** | Istantanea dei salvataggi attuali del gioco (es. prima di un boss), con nome facoltativo, senza cambiare né sovrascrivere il profilo attivo. Si ripristina (con backup prima) o si elimina dalla finestra Savestate. |
| **Backup** | Elenco dei backup automatici con data e motivo; "Ripristina" li rimette nel gioco (con backup prima). Si conservano gli ultimi N (predefinito 20, 0 = tutti). |
| **Crea profilo** | Nuovo profilo vuoto oppure copia dei salvataggi attuali; può essere attivato subito. |
| **Apri cartella profili** | Apre la cartella dei profili in Esplora file. |

Tutte le operazioni sono bloccate mentre Control è in esecuzione: la finestra lo
controlla ogni 2 secondi e disattiva i pulsanti.

## Percorsi

| Cosa | Percorso |
|---|---|
| Salvataggi del gioco | `<Steam>\userdata\<IDAccount>\870780\` |
| Profili | `Documenti\ControlSaves\<IDAccount>\Profili\Profilo_<nome>\` |
| Backup | `Documenti\ControlSaves\<IDAccount>\Backup\<motivo>_<data>\` |
| Savestate | `Documenti\ControlSaves\<IDAccount>\Savestate\Profilo_<nome>\<data>_<nome savestate>\` |
| Stato | `Documenti\ControlSaves\<IDAccount>\stato.ini` |
| Registro | `Documenti\ControlSaves\log.txt` (oltre 1 MB diventa `log.old.txt`) |

La cartella di Steam viene letta dal registro (`HKCU\Software\Valve\Steam\SteamPath`),
con ripiego sui percorsi standard in `Program Files`.

Il file `remotecache.vdf` di Steam non viene mai copiato né cancellato.

### Sicurezza delle copie

- Ogni backup e ogni copia viene confrontata byte per byte con l'originale.
- Attivazione e "Salva i progressi" lavorano in due passaggi: la sorgente viene
  copiata in `<cartella>.nuovo`, verificata, e poi le cartelle vengono
  scambiate (la vecchia passa per `<cartella>.vecchio` ed è eliminata). Se la
  cartella è in uso e non si può spostare, si ripiega sulla copia diretta dalla
  copia già verificata. In caso di errore si ripristina il backup.
- `stato.ini` viene scritto in `stato.ini.tmp` e poi sostituito.
- Una sola istanza dell'app alla volta.

### Formato di `stato.ini`

```ini
ProfiloAttivo=Profilo_Mario
AttivatoIl=22/10/2026 18:30:00
SalvatoIl=-
BackupDaConservare=20
```

`-` indica un valore vuoto. `BackupDaConservare=0` conserva tutti i backup.

I backup `Prima_salvataggio_<profilo>` contengono il profilo prima del
salvataggio; gli altri (`Prima_di_<profilo>`, `Prima_creazione`,
`Prima_ripristino`) i salvataggi del gioco.

## Nomi dei profili

Lettere ASCII, cifre, `_` e `-`, massimo 50 caratteri. Le cartelle in `Profili`
che non rispettano la regola vengono ignorate.

## Struttura del codice

| File | Contenuto |
|---|---|
| `SaveManager.cs` | Tutta la logica: ricerca di Steam, profili, attivazione, salvataggio. Nessun I/O di console. |
| `FileOps.cs` | Copia e sincronizzazione di cartelle. |
| `AppState.cs` | Lettura e scrittura di `stato.ini`. |
| `Log.cs` | Registro delle operazioni e degli errori imprevisti. |
| `Result.cs` | Esito delle operazioni (messaggio, backup creato, errore grave). |
| `MainForm.cs` | Finestra principale (WinForms). |
| `NewProfileForm.cs` | Finestra "Nuovo profilo". |
| `SnapshotForms.cs` | Finestre "Savestate" e "Backup" e la loro lista. |
| `Theme.cs` | Colori, caratteri e icona del tema scuro. |
| `ThemedControls.cs` | Controlli disegnati a mano: pulsanti, schede, intestazione, barra di stato. |
| `app.ico` | Icona dell'exe e delle finestre. |
| `Program.cs` | Avvio dell'applicazione. |

La vecchia versione da console è nella cronologia git (commit `17cd279`).

## Compilazione

Richiede .NET 10 SDK.

```bash
dotnet build -c Release
```

```bash
dotnet publish -c Release
```

## Avvertenze

- **Steam Cloud**: se attivo per Control, Steam può risincronizzare i file dopo
  lo scambio. Si consiglia di disattivarlo per il gioco o di chiudere Steam
  durante l'uso.
- **Smart App Control** (Windows 11) può bloccare l'exe compilato in proprio,
  che non è firmato: l'app si chiude subito all'avvio e nel registro eventi
  compare "An Application Control policy has blocked this file". È successo con
  una versione che chiamava direttamente `dwmapi.dll` e `uxtheme.dll` (barra
  del titolo e barre di scorrimento scure); senza quelle chiamate parte. Meglio
  evitare `DllImport` nel progetto.
- L'esito delle operazioni riuscite compare solo nella barra di stato; le
  finestre di messaggio restano per gli errori e per l'avviso su Steam aperto,
  mostrato una sola volta per sessione.

## Documentazione

- [Piano per l'interfaccia grafica](docs/PIANO_GUI.md)
- [Bug noti](docs/BUG_NOTI.md)
- [Cose da fare](docs/TODO.md)
