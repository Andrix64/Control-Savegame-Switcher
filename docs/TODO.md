# Cose da fare

Quello che manca, in ordine di priorità. I bug sono in
[BUG_NOTI.md](BUG_NOTI.md); quello che è già stato fatto è nel
[CHANGELOG](../CHANGELOG.md).

## Da fare

- [ ] **Firma del codice.** Senza firma SmartScreen avvisa chi scarica l'exe e
  Smart App Control può bloccarlo. Opzione più economica: Azure Trusted
  Signing. Un certificato autofirmato non basta.

## Nuova interfaccia

Bozza approvata (sei schermate): https://claude.ai/artifact/3NzXmpka5GFsAeMHX4WhRB.
Segue le euristiche di Nielsen Norman Group e le linee guida Microsoft per
finestre di dialogo e testi.

- [ ] **Parole nuove.** Profilo → Partita, Attiva profilo → Gioca questa,
  savestate → Punto salvato, backup → Copie automatiche nella Cronologia.
- [ ] **Conferme con verbi.** "Elimina partita" / "Annulla" invece di Sì/No;
  errori che dicono cosa è successo, che non si è perso nulla e cosa fare.
- [ ] **Finestra principale a schede.** Ogni partita con le sue azioni visibili
  ("Gioca questa", menu "⋯" con Carica senza avviare, Rinomina, Modifica
  nota, Esporta, Elimina) e la scritta "Nel gioco adesso" su quella caricata.
- [ ] **Barre di stato in alto** per Control aperto e Steam Cloud attivo, con
  la spiegazione e cosa fare.
- [ ] **"Annulla" dopo un cambio** al posto della conferma prima (il backup
  esiste già).
- [ ] **Cronologia per partita.** Punti salvati e copie automatiche in un solo
  elenco con "Torna qui". Richiede di sapere a quale partita appartiene ogni
  copia automatica: per alcuni motivi il nome lo dice già, per altri no.
- [ ] **Primo avvio.** Due righe su cosa fa l'app, nome per la partita già nel
  gioco, controlli di account e Steam Cloud.
- [ ] **Finestra Impostazioni** (vedi le funzioni sotto).

## Nuove funzioni

- [ ] **"Gioca questa" avvia Control.** Dopo il cambio apre
  `steam://rungameid/870780` (Steam si apre da solo se è chiuso). Si può
  spegnere nelle impostazioni; nel menu "⋯" resta "Carica senza avviare".
  Con Steam Cloud attivo, avviso prima di avviare.
- [ ] **Salvataggio alla chiusura del gioco.** Quando l'app vede che Control
  si è chiuso, salva i progressi nella partita in uso, con la sua copia
  automatica. Attivo di base, si può spegnere.
- [ ] **Tempo di gioco per partita.** Somma delle sessioni misurate tra
  l'avvio e la chiusura del gioco, più la data dell'ultima sessione. Va
  salvato per partita (per esempio in un file accanto al profilo).
- [ ] **Nota per partita.** Testo libero ("Missione Polaris, prima di Ahti"),
  mostrato sulla scheda e modificabile dal menu.
- [ ] **Stesse opzioni del gioco per tutte le partite.** Le opzioni stanno in
  `remote\preferences_data`, insieme ai salvataggi: oggi cambiano con la
  partita e una partita nuova riparte da quelle predefinite. Con l'opzione
  attiva, il file resta quello della partita di prima. Spenta di base.
- [ ] **Esporta e importa una partita** in un file zip (salvataggi, punti
  salvati, nota), per spostarla su un altro PC o tenerne una copia.

## Da provare con il gioco vero

Le prove automatiche usano cartelle finte; queste vanno fatte a mano.

- [ ] **Steam Cloud attivo.** Cambiare profilo con il cloud attivo e vedere
  cosa fa Steam all'avvio del gioco (finestra di conflitto, file
  riscaricati). Oggi l'app avvisa e consiglia di disattivarlo.
- [ ] **Spostamento dei dati tra dischi diversi.** Con Documenti su un disco e
  `%LOCALAPPDATA%` su un altro lo spostamento usa la copia verificata: è
  provato solo con le cartelle di prova, non con due dischi veri.
- [ ] **Giro completo.** Due profili, savestate prima di un boss, ripristino,
  cambio profilo e ritorno: ogni profilo deve ritrovare i suoi punti di
  controllo.

## Idee

- **Collegamento sul desktop "Gioca <partita>".** Un clic carica la partita e
  avvia il gioco senza aprire l'app. Va controllato con Smart App Control.
- **Più lingue.** Basta un `Strings.<codice>.resx` con le stesse chiavi (vedi
  [SVILUPPO.md](SVILUPPO.md#lingue)).
