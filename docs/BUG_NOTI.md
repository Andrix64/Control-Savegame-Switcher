# Bug noti

Bug ancora aperti, trovati rileggendo il codice della 0.9.0 e verificati con
prove su cartelle finte dove indicato. I bug già risolti sono stati tolti: la
loro storia è nella cronologia git.

Sono tutti a priorità **bassa**: casi rari o solo di comodità. Nessun bug
aperto fa perdere salvataggi (prima di ogni operazione c'è un backup
verificato) o impedisce di usare l'app.

| # | Bug | Priorità | Verificato |
|---|---|---|---|
| 1 | Profili ignorati in silenzio | Bassa | Sì |
| 2 | Le finestre Savestate e Backup non sanno se il gioco è aperto | Bassa | No |
| 3 | "Crea profilo" non controlla che il gioco sia chiuso | Bassa | No |
| 4 | Launcher del gioco non rilevato se avviato come amministratore | Bassa | No |
| 5 | "Apri cartella" può creare una cartella di backup vuota | Bassa | No |
| 6 | Esclusioni delle cartelle applicate a ogni livello | Bassa | Sì |

## Bassa

### 1. Profili ignorati in silenzio

Le cartelle in `Profili` con un nome non valido (es. `Profilo_Mio Nome`, con
lo spazio, o con lettere accentate) non compaiono nell'elenco e non viene
detto perché.

**Correzione proposta**: una riga nella barra di stato ("1 cartella ignorata:
nome non valido") o nel registro.

### 2. Le finestre Savestate e Backup non sanno se il gioco è aperto

La finestra principale disattiva i pulsanti mentre Control è in esecuzione; le
finestre Savestate e Backup no. Se si avvia il gioco con una delle due aperte,
"Ripristina" e "Crea savestate" restano attivi: l'operazione viene comunque
rifiutata con "Chiudi Control prima di continuare", quindi non succede nulla di
grave.

**Correzione proposta**: passare alle due finestre lo stato del gioco della
finestra principale e disattivare i pulsanti.

### 3. "Crea profilo" non controlla che il gioco sia chiuso

`SaveManager.CreateProfile` con la copia dei salvataggi non controlla
`IsGameRunning`, a differenza delle altre operazioni. Oggi lo impedisce solo
l'interfaccia (il pulsante è disattivato col gioco aperto). Copiando mentre il
gioco scrive si potrebbe ottenere un profilo incoerente.

**Correzione proposta**: lo stesso controllo delle altre operazioni.

### 4. Launcher del gioco non rilevato se avviato come amministratore

Per riconoscere il launcher `Control.exe` (e non confonderlo con il Pannello di
controllo) l'app legge il percorso dell'eseguibile. Se il processo è avviato
come amministratore e l'app no, il percorso non è leggibile e il processo viene
ignorato. Il gioco vero (`Control_DX11` / `Control_DX12`) viene comunque
rilevato per nome, quindi il rischio è solo nella finestra di avvio del
launcher.

### 5. "Apri cartella" può creare una cartella di backup vuota

`ListDialog.OpenFolder` crea la cartella se non esiste. Se il backup
selezionato è stato cancellato da fuori (in Esplora file) mentre la finestra
Backup è aperta, "Apri cartella" lo ricrea vuoto e al prossimo aggiornamento
compare come backup vuoto.

**Correzione proposta**: creare la cartella solo per la radice (Backup o
Savestate del profilo), non per un singolo backup.

### 6. Esclusioni delle cartelle applicate a ogni livello

`FileOps` salta le cartelle `Backup`, `Profili` e `Profilo_*` e il file
`remotecache.vdf` a qualsiasi profondità, anche dentro `remote`, e la verifica
dopo la copia li ignora allo stesso modo. Le esclusioni delle cartelle vengono
dalla prima versione e oggi non servono: i dati dell'app non sono mai dentro la
cartella del gioco.

Verificato sui salvataggi reali: `870780` contiene solo `remotecache.vdf` e
`remote\` con `preferences_data` e `savegame-slot-00_*`, quindi oggi non
cambia nulla. Se una versione futura di Control creasse una sottocartella con
uno di quei nomi, non verrebbe copiata e la verifica non se ne accorgerebbe.

**Correzione proposta**: togliere le esclusioni delle cartelle e limitare
quella di `remotecache.vdf` al primo livello.
