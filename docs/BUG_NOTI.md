# Bug noti

Revisione della versione console (`Program.cs`). Build: 0 errori, 3 warning.

## Stato

| # | Bug | Stato |
|---|---|---|
| 1 | Backup incompleto non rilevato | ✅ Risolto (Fase 1) |
| 2 | `SaveProgress` può svuotare il profilo | ✅ Risolto (Fase 1) |
| 3 | Steam Cloud / Steam aperto | ✅ Avviso se Steam è aperto (Fase 1) |
| 4 | Profilo parziale lasciato su disco | ✅ Risolto (la cartella viene eliminata se la copia fallisce) |
| 5 | Falso positivo `Control` | ✅ Risolto (si scarta `Control.exe` nella cartella di Windows) |
| 6 | Cambio profilo senza avviso | ✅ Risolto (i progressi non salvati vengono salvati da soli nel profilo attivo prima del cambio) |
| 7 | `SaveState` ignora gli errori | ✅ Risolto (Fase 1, errore restituito nel `Result`) |
| 8 | Ciclo infinito con input chiuso | ✅ Sparito con la versione grafica (Fase 3) |
| 9 | Warning `CA1416` | ✅ Risolto (Fase 2) |
| 10, 11, 14 | Priorità bassa | Aperti |
| 12 | Backup senza limite | ✅ Risolto (si conservano gli ultimi N, predefinito 20) |
| 13 | Più account Steam | ✅ Risolto (scelta all'avvio, con l'ultimo account preselezionato) |

Priorità: **Alta** = rischio di perdere salvataggi, **Media** = comportamento
sbagliato, **Bassa** = rifinitura.

## Alta

### 1. Backup incompleto non rilevato

`CopyFolder` ignora il risultato della chiamata ricorsiva sulle sottocartelle:

```csharp
CopyFolder(dir, Path.Combine(destination, name)); // risultato ignorato
```

Se una sottocartella non viene copiata, la funzione restituisce comunque `true`
e l'operazione prosegue sovrascrivendo i salvataggi. Peggio: in caso di errore
`ChangeToProfile` ripristina con `MirrorFolder(backup, savePath)`, che
**cancella** dai salvataggi tutti i file assenti nel backup incompleto.

**Correzione**: `if (!CopyFolder(...)) return false;`

### 2. `SaveProgress` può svuotare il profilo

`MirrorFolder(savePath, profile)` copia la cartella del gioco così com'è. Se è
vuota o corrotta (per esempio dopo una sincronizzazione Steam Cloud sbagliata),
il contenuto del profilo viene cancellato. Il backup `Prima_salvataggio` copre il
caso, ma l'utente non viene avvisato.

**Correzione**: se la cartella `remote` del gioco non contiene file, chiedere
conferma prima di procedere.

### 3. Steam Cloud / Steam aperto

Non si controlla se Steam è in esecuzione. Il client può risincronizzare la
cartella `remote` dopo lo scambio e annullarlo o creare conflitti.

**Correzione**: almeno un avviso se il processo `steam` è attivo; idealmente
documentare di disattivare Steam Cloud per Control.

## Media

### 4. Profilo parziale lasciato su disco

In `CreateProfile`, se la copia dei salvataggi nel nuovo profilo fallisce, la
cartella `Profilo_<nome>` resta. Compare nella lista e impedisce di ricreare un
profilo con lo stesso nome.

**Correzione**: eliminare la cartella del profilo nel ramo di errore.

### 5. Falso positivo nel rilevamento del gioco

`IsGameRunning` cerca anche il processo `Control`, che coincide con
`control.exe` (Pannello di controllo di Windows). Se è aperto, l'app crede che
il gioco sia in esecuzione e si blocca.

**Correzione**: cercare solo `Control_DX12` e `Control_DX11`. Rilasciare gli
oggetti `Process` restituiti (`Dispose`).

**Correzione applicata**: `Control.exe` resta tra i processi cercati, perché è
il launcher del gioco, ma viene scartato se l'eseguibile si trova nella
cartella di Windows.

### 6. Cambio profilo senza avviso sui progressi

Cambiando profilo, i progressi non salvati del profilo attivo finiscono solo nel
backup `Prima_di_<profilo>`, senza che l'utente lo sappia.

**Correzione**: chiedere "Salvare prima i progressi nel profilo attivo?".

**Correzione applicata**: `SaveManager.HasUnsavedProgress()` confronta file e
contenuti della cartella del gioco con il profilo attivo (ignorando
`remotecache.vdf`). Se sono diversi, prima di attivare un altro profilo la
finestra chiede: Sì (salva e poi attiva), No (attiva senza salvare), Annulla.
Con i salvataggi del gioco vuoti non viene chiesto nulla.

### 7. `SaveState` ignora gli errori

Se la scrittura di `stato.ini` fallisce, l'errore viene solo stampato: il
profilo attivo mostrato può non corrispondere ai file reali.

**Correzione**: restituire l'esito e mostrarlo come errore.

### 8. Ciclo infinito con input chiuso

Se `Console.ReadLine()` restituisce `null` (stdin chiuso), il menu principale
stampa "Scelta non valida" all'infinito. Sparisce con l'interfaccia grafica.

## Bassa

### 9. Warning `CA1416` sul registro

`Registry.CurrentUser` è usato senza dichiarare il progetto come solo Windows.

**Correzione**: `<TargetFramework>net10.0-windows</TargetFramework>`.

### 10. Conflitto file/cartella in `MirrorFolder`

Se uno stesso nome è un file da una parte e una cartella dall'altra, la copia
lancia un'eccezione e la destinazione resta in stato parziale.

### 11. Profili ignorati in silenzio

Cartelle create a mano con nomi non validi (es. `Profilo_Mio Nome`) non
compaiono nella lista e non viene detto perché.

### 12. Backup senza limite

La cartella `Backup` cresce all'infinito. Valutare una pulizia dei più vecchi.

### 13. Più account Steam

`Initialize` prende il primo account con salvataggi di Control, senza chiedere
quale usare se ce n'è più di uno.

### 14. Formato data dipendente dalla lingua

`dd/MM/yyyy` usa il separatore della cultura corrente. Solo estetico.
