# Cose da fare

Quello che manca, in ordine di priorità. I bug sono in
[BUG_NOTI.md](BUG_NOTI.md); quello che è già stato fatto è nel
[CHANGELOG](../CHANGELOG.md).

## Da fare

- [ ] **Firma del codice.** Senza firma SmartScreen avvisa chi scarica l'exe e
  Smart App Control può bloccarlo. Opzione più economica: Azure Trusted
  Signing. Un certificato autofirmato non basta.

## Da provare con il gioco vero

Le prove automatiche usano cartelle finte e le finestre sono controllate solo
a vista con dati inventati; queste vanno fatte a mano.

- [ ] **Gioca questa.** Che `steam://rungameid/870780` avvii Control, anche
  con Steam chiuso, e che la barra "Stai giocando" compaia.
- [ ] **Chiusura del gioco.** Che alla chiusura i progressi vengano salvati
  nella partita giusta e il tempo di gioco aumenti.
- [ ] **Opzioni condivise.** Che `remote\preferences_data` contenga davvero le
  opzioni (comandi, audio, sottotitoli) e che con l'impostazione attiva
  restino uguali cambiando partita.
- [ ] **Esporta e importa** tra due PC.
- [ ] **Spostamento da Documenti** al primo avvio, con i dati veri.
- [ ] **Steam Cloud attivo.** Cambiare partita con il cloud attivo e vedere
  cosa fa Steam all'avvio del gioco (finestra di conflitto, file
  riscaricati). Oggi l'app avvisa e consiglia di disattivarlo.
- [ ] **Spostamento dei dati tra dischi diversi.** Con Documenti su un disco e
  `%LOCALAPPDATA%` su un altro lo spostamento usa la copia verificata: è
  provato solo con le cartelle di prova, non con due dischi veri.
- [ ] **Giro completo.** Due partite, punto salvato prima di un boss, ritorno
  al punto, cambio di partita e ritorno: ogni partita deve ritrovare i suoi
  punti di controllo.

## Idee

- **Collegamento sul desktop "Gioca <partita>".** Un clic carica la partita e
  avvia il gioco senza aprire l'app. Va controllato con Smart App Control.
- **Più lingue.** Basta un `Strings.<codice>.resx` con le stesse chiavi (vedi
  [SVILUPPO.md](SVILUPPO.md#lingue)).
