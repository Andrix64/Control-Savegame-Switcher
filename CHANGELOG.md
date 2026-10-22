# Novità

Tutte le modifiche rilevanti, dalla più recente. Le versioni seguono
[Semantic Versioning](https://semver.org/lang/it/).

## 0.9.0 — 2026-10-22

Prima versione distribuita.

### Profili

- Più profili di salvataggio per Control, ognuno con la sua partita.
- Cambio profilo in un clic, senza conferme: i progressi non salvati del
  profilo attivo vengono salvati da soli e prima di ogni operazione si fa un
  backup.
- Creazione (vuoto o copia dei salvataggi attuali), rinomina ed eliminazione.
  Un profilo eliminato finisce tra i backup e si può ancora ripristinare.
- Se sul PC ci sono più account Steam con Control, all'avvio si sceglie quale
  usare.
- Le cartelle in `Profili` con un nome non valido vengono segnalate all'avvio
  invece di essere ignorate in silenzio.

### Savestate e backup

- Savestate: istantanee dei salvataggi attuali (per esempio prima di un boss)
  da ripristinare quando serve, senza cambiare profilo.
- Finestra Backup con l'elenco dei backup automatici, il ripristino e la
  pulizia dei più vecchi (si conservano gli ultimi 20, impostabile).

### Sicurezza dei salvataggi

- Ogni copia viene verificata byte per byte.
- Se un'operazione viene interrotta (per esempio da uno spegnimento), al
  successivo avvio le cartelle tornano da sole allo stato di prima.
- Sostituzione in due passaggi: i nuovi salvataggi vengono preparati e
  verificati in una cartella a parte prima dello scambio; in caso di errore si
  ripristina il backup.
- Operazioni bloccate mentre Control è aperto, anche nelle finestre Savestate
  e Backup; una sola istanza dell'app.
- Dati (profili, savestate, backup, registro) in
  `%LOCALAPPDATA%\ControlSaveManager`, che OneDrive non sincronizza. I dati
  delle versioni di prova in `Documenti\ControlSaves` vengono spostati da soli
  al primo avvio, senza mai perdere nulla.
- Registro delle operazioni in `log.txt`.

### Steam Cloud

- L'app legge se Steam Cloud è attivo per Control e lo mostra nella finestra.
  Con il cloud disattivato non avvisa più quando Steam è aperto.

### Interfaccia

- Tema scuro, finestre di messaggio comprese.
- Italiano e inglese: la lingua segue quella di Windows. Le date seguono il
  formato della lingua di Windows.
- Exe singolo per Windows a 64 bit, senza installazione.
