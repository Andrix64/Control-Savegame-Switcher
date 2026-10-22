# Piano: interfaccia grafica

Obiettivo: sostituire il menu da console con una finestra semplice, mantenendo
lo stesso comportamento sui file e la stessa cartella `ControlSaves`.

## Scelta della tecnologia: WinForms

| Opzione | Pro | Contro |
|---|---|---|
| **WinForms** ✅ | Incluso in .NET 10, nessun pacchetto, semplice, exe singolo | Aspetto un po' datato |
| WPF | Più moderno e personalizzabile | XAML e binding: troppo per 4 azioni |
| WinUI 3 | Aspetto nativo di Windows 11 | Configurazione e distribuzione complesse |
| Avalonia | Multipiattaforma | Inutile: Steam/registro sono solo Windows |

L'app dipende già da Windows (registro, Esplora file), quindi WinForms è la
scelta più leggera.

## Fase 0 — Preparazione

- [x] Inizializzare un repository git (con `.gitignore` per `bin/` e `obj/`).
- [x] Commit dello stato attuale (versione console).

## Fase 1 — Separare la logica dalla console ✅

Completata. File creati: `SaveManager.cs`, `AppState.cs`, `FileOps.cs`,
`Result.cs`. `Program.cs` ora è solo il menu da console sopra `SaveManager`.

Differenze rispetto allo schema qui sotto:

- `SaveProgress(bool allowEmptySaves = false)`: rifiuta di svuotare il profilo
  se la cartella del gioco è vuota, a meno che l'interfaccia non abbia chiesto
  conferma.
- `IsSteamRunning()`: l'interfaccia lo usa per avvisare prima di attivare o
  salvare.
- `GameSavesAreEmpty()`, `HasActiveProfile`, `ProfileExists()`,
  `ProfileFolderName()`, `ReloadState()`.
- `Result.Critical` (creato con `Result.Fatal`): ripristino fallito, il gioco
  non va avviato.
- `CopyFolder` e `MirrorFolder` lanciano eccezioni invece di restituire `bool`.

Oggi ogni funzione mescola operazioni sui file e `Console.*`. Va estratta una
classe senza I/O di console.

**`SaveManager.cs`**

```csharp
public sealed class SaveManager
{
    public string SavePath { get; }
    public string ProfilesPath { get; }
    public string BackupPath { get; }
    public AppState State { get; }

    public static Result<SaveManager> Initialize();

    public IReadOnlyList<string> GetProfiles();
    public Result CreateProfile(string name, bool copyCurrentSaves);
    public Result ActivateProfile(string profileName);
    public Result SaveProgress();

    public static bool IsGameRunning();
    public static bool IsValidName(string name);
}
```

**`Result`**: esito più messaggio d'errore (ed eventuale percorso del backup),
così l'interfaccia decide come mostrarlo.

**`AppState`**: `ActiveProfile`, `ActiveSince`, `LastSaved`, più `Load()` e
`Save()` per `stato.ini`. Il formato del file resta invariato.

**Utility file** (`FileOps.cs`): `CopyFolder`, `MirrorFolder`, `IsExcluded`.

Nella stessa fase si correggono i bug di [BUG_NOTI.md](BUG_NOTI.md), almeno
quelli con priorità alta.

## Fase 2 — Progetto WinForms ✅

Modifiche al `.csproj`:

```xml
<OutputType>WinExe</OutputType>
<TargetFramework>net10.0-windows</TargetFramework>
<UseWindowsForms>true</UseWindowsForms>
```

Questo elimina anche i warning `CA1416` sul registro.

## Fase 3 — Finestre ✅

Completata: `MainForm.cs` e `NewProfileForm.cs`, scritte in codice (senza
file `.Designer.cs`) con `TableLayoutPanel`/`FlowLayoutPanel`, così il layout
si adatta al ridimensionamento e ai DPI. Altri dettagli:

- I profili sono mostrati senza il prefisso `Profilo_`.
- Durante un'operazione la finestra non si può chiudere.
- Gli errori con backup chiedono se aprire la cartella del backup.

### Finestra principale (`MainForm`)

```
┌─ Control Save Manager ─────────────────────┐
│ Profilo attivo: Mario                      │
│ Attivato: 22/10/2026 18:30                 │
│ Ultimo salvataggio: Mai                    │
├────────────────────────────────────────────┤
│ ┌ Profili ─────────────┐  [ Attiva       ] │
│ │ Mario  ✔             │  [ Salva prog.  ] │
│ │ Partita_NG+          │  [ Nuovo...     ] │
│ │ Speedrun             │  [ Apri cartella] │
│ └──────────────────────┘                   │
├────────────────────────────────────────────┤
│ ⚠ Control è in esecuzione                  │  ← barra di stato
└────────────────────────────────────────────┘
```

| Controllo | Comportamento |
|---|---|
| Etichette in alto | Profilo attivo, data di attivazione, ultimo salvataggio. |
| `ListBox` profili | Il profilo attivo è contrassegnato. Doppio clic = Attiva. |
| **Attiva** | Abilitato solo con un profilo selezionato diverso da quello attivo. Chiede conferma. |
| **Salva progressi** | Abilitato solo con un profilo attivo valido. Chiede conferma. |
| **Nuovo...** | Apre `NewProfileForm`. |
| **Apri cartella** | Apre `Profili` in Esplora file. |
| Barra di stato | Stato del gioco o esito dell'ultima operazione. |

**Rilevamento del gioco**: un `Timer` ogni ~2 secondi chiama `IsGameRunning()`.
Se il gioco è aperto, i pulsanti Attiva/Salva/Nuovo vengono disabilitati e la
barra di stato lo segnala. Sostituisce il ciclo "chiudi il gioco e premi INVIO".

**Operazioni lunghe**: le copie girano con `Task.Run` e `await`; durante
l'operazione i pulsanti sono disabilitati e il cursore è in attesa.

### Finestra nuovo profilo (`NewProfileForm`)

```
┌─ Nuovo profilo ─────────────────┐
│ Nome: [______________]          │
│ ( ) Copia i salvataggi attuali  │
│ ( ) Profilo vuoto               │
│ [x] Attiva subito               │
│            [ Annulla ] [ Crea ] │
└─────────────────────────────────┘
```

- **Crea** è abilitato solo se il nome è valido e non esiste già.
- Sotto il campo nome compare un messaggio se il nome non è valido.

### Messaggi

| Situazione | Dialogo |
|---|---|
| Conferma attivazione | "Attivare il profilo X? I salvataggi attuali verranno salvati in un backup." |
| Conferma salvataggio | "Sovrascrivere il profilo X con i salvataggi attuali?" |
| Errore con backup | Messaggio più percorso del backup, con pulsante per aprire la cartella. |
| Ripristino fallito | Errore grave: "Non avviare il gioco" più percorso del backup. |
| Steam / salvataggi non trovati all'avvio | Errore e chiusura dell'app. |

### Aspetto (tema scuro)

Aggiunto dopo la Fase 3. Tema scuro con accento rosso ispirato al gioco,
definito in `Theme.cs`; controlli disegnati a mano in `ThemedControls.cs`.

- Intestazione "CONTROL / SAVE MANAGER" con barra rossa.
- Scheda del profilo attivo con data di attivazione e ultimo salvataggio.
- Lista disegnata a mano: nome, data dell'ultima modifica, etichetta "ATTIVO".
  Messaggio dedicato quando non ci sono profili.
- Pulsanti piatti arrotondati: primario rosso, secondari grigi, "Apri cartella"
  discreto.
- Barra di stato con pallino colorato (pronto, in corso, riuscito, avviso, errore).
- Icona `app.ico`, incorporata anche come risorsa per le finestre.

Niente chiamate native a Windows (`DllImport`): una versione con barra del
titolo e barre di scorrimento scure forzate veniva bloccata da Smart App Control.

## Fase 4 — Pulizia

- [x] Rimuovere il vecchio `Program.cs` da console (resta nella cronologia git).
- [x] Aggiornare il [README](../README.md).
- [ ] Prova manuale completa (vedi sotto).

## Prova manuale

1. Avvio senza profili: lista vuota, Attiva e Salva disabilitati.
2. Creare un profilo copiando i salvataggi, con "Attiva subito".
3. Creare un profilo vuoto e attivarlo: nel gioco nessun salvataggio.
4. Tornare al primo profilo: salvataggi ripristinati.
5. Giocare, chiudere, **Salva progressi**: il profilo contiene i nuovi file.
6. Aprire Control con l'app aperta: pulsanti disabilitati entro ~2 s.
7. Controllare che in `Backup` ci sia una cartella per ogni operazione.

## Idee per dopo (fuori dallo scopo iniziale)

- Rinominare ed eliminare profili dall'interfaccia.
- Ripristinare un backup dalla finestra.
- Pulizia automatica dei backup più vecchi.
- Scelta dell'account Steam se ce n'è più di uno.
- Avviso per Steam aperto / Steam Cloud attivo.
