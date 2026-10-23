# Novità

Tutte le modifiche rilevanti, dalla più recente. Le versioni seguono
[Semantic Versioning](https://semver.org/lang/it/).

## 0.9.0 — non ancora rilasciata

Prima versione distribuita.

### Partite

- Più partite di Control sullo stesso account, una alla volta nel gioco.
- **Gioca questa**: salva i progressi della partita di adesso, carica quella
  scelta e avvia Control tramite Steam. Si può anche solo caricare, con
  **Annulla** subito dopo.
- Nota e tempo di gioco per ogni partita; il tempo si misura da solo mentre
  l'app è aperta.
- Alla chiusura del gioco i progressi vengono salvati da soli (si può
  spegnere).
- Opzione per tenere le stesse opzioni del gioco in tutte le partite.
- Nuova partita (vuota o copia dei salvataggi attuali), rinomina, esportazione
  e importazione in un file zip.
- Una partita eliminata si può recuperare da «Partite eliminate».
- Al primo avvio la partita già nel gioco viene salvata con un nome a scelta.
- Se sul PC ci sono più account Steam con Control, all'avvio si sceglie quale
  usare.
- Nomi delle partite con spazi e lettere accentate. Le cartelle in `Profili`
  con un nome non valido vengono segnalate all'avvio.

### Cronologia

- Punti salvati (per esempio prima di un boss) e copie automatiche fatte prima
  di ogni operazione, per partita, con **Torna qui**.
- Si conservano le ultime 20 copie automatiche (impostabile).

### Sicurezza dei salvataggi

- Ogni copia viene verificata byte per byte.
- Sostituzione in due passaggi: i nuovi salvataggi vengono preparati e
  verificati in una cartella a parte prima dello scambio; in caso di errore si
  ripristina la copia.
- Se un'operazione viene interrotta (per esempio da uno spegnimento), al
  successivo avvio le cartelle tornano da sole allo stato di prima.
- Operazioni bloccate mentre Control è aperto; una sola istanza dell'app.
- Dati in `%LOCALAPPDATA%\ControlSaveManager`, che OneDrive non sincronizza. I
  dati delle versioni di prova in `Documenti\ControlSaves` vengono spostati da
  soli al primo avvio, senza mai perdere nulla.
- Registro delle operazioni in `log.txt`.

### Steam Cloud

- L'app legge se Steam Cloud è attivo per Control e lo segnala con una barra,
  con le istruzioni per disattivarlo. Con il cloud disattivato non avvisa più.

### Interfaccia

- Una scheda per partita con le sue azioni visibili, barre per gli stati
  (gioco aperto, Steam Cloud) e conferme con verbi ("Elimina partita",
  "Annulla") solo per ciò che non si può annullare. Progettata seguendo le
  euristiche di Nielsen Norman Group e le linee guida Microsoft per Windows.
- Tema scuro, finestre di messaggio comprese.
- Italiano e inglese: la lingua e il formato delle date seguono Windows.
- Exe singolo per Windows a 64 bit, senza installazione.
