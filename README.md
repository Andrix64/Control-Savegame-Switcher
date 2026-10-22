# Control Save Manager

Piccolo strumento per Windows che gestisce **più profili di salvataggio** per
*Control* (Steam, AppID `870780`). Permette di passare da una partita all'altra
sostituendo i file di salvataggio del gioco, con un backup automatico prima di
ogni operazione.

## Funzioni

| Funzione | Cosa fa |
|---|---|
| **Cambia profilo** | Fa il backup dei salvataggi attuali, poi copia il profilo scelto nella cartella del gioco. |
| **Salva i progressi** | Fa il backup del profilo attivo, poi copia i salvataggi del gioco nel profilo. |
| **Crea profilo** | Nuovo profilo vuoto oppure copia dei salvataggi attuali; può essere attivato subito. |
| **Apri cartella profili** | Apre la cartella dei profili in Esplora file. |

Tutte le operazioni sono bloccate mentre Control è in esecuzione.

## Percorsi

| Cosa | Percorso |
|---|---|
| Salvataggi del gioco | `<Steam>\userdata\<IDAccount>\870780\` |
| Profili | `Documenti\ControlSaves\<IDAccount>\Profili\Profilo_<nome>\` |
| Backup | `Documenti\ControlSaves\<IDAccount>\Backup\<motivo>_<data>\` |
| Stato | `Documenti\ControlSaves\<IDAccount>\stato.ini` |

La cartella di Steam viene letta dal registro (`HKCU\Software\Valve\Steam\SteamPath`),
con ripiego sui percorsi standard in `Program Files`.

Il file `remotecache.vdf` di Steam non viene mai copiato né cancellato.

### Formato di `stato.ini`

```ini
ProfiloAttivo=Profilo_Mario
AttivatoIl=22/10/2026 18:30:00
SalvatoIl=-
```

`-` indica un valore vuoto.

## Nomi dei profili

Lettere ASCII, cifre, `_` e `-`, massimo 50 caratteri. Le cartelle in `Profili`
che non rispettano la regola vengono ignorate.

## Struttura del codice

| File | Contenuto |
|---|---|
| `SaveManager.cs` | Tutta la logica: ricerca di Steam, profili, attivazione, salvataggio. Nessun I/O di console. |
| `FileOps.cs` | Copia e sincronizzazione di cartelle. |
| `AppState.cs` | Lettura e scrittura di `stato.ini`. |
| `Result.cs` | Esito delle operazioni (messaggio, backup creato, errore grave). |
| `Program.cs` | Menu da console. |

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
- Cambiare profilo **senza aver salvato i progressi** li sposta solo nel backup
  `Prima_di_<profilo>`, non nel profilo attivo.

## Documentazione

- [Piano per l'interfaccia grafica](docs/PIANO_GUI.md)
- [Bug noti](docs/BUG_NOTI.md)
