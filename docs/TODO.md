# Cose da fare

Quello che manca, in ordine di priorità. I bug sono in
[BUG_NOTI.md](BUG_NOTI.md); quello che è già stato fatto è nel
[CHANGELOG](../CHANGELOG.md).

## Da fare

- [ ] **Firma del codice.** Senza firma SmartScreen avvisa chi scarica l'exe e
  Smart App Control può bloccarlo. Opzione più economica: Azure Trusted
  Signing. Un certificato autofirmato non basta.

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

- **Preferenze condivise tra i profili.** Le opzioni del gioco stanno in
  `remote\preferences_data`, insieme ai salvataggi: cambiano con il profilo, e
  un profilo vuoto parte con le opzioni predefinite. Si potrebbe offrire di
  tenerle uguali per tutti i profili.
- **Più lingue.** Basta un `Strings.<codice>.resx` con le stesse chiavi (vedi
  [SVILUPPO.md](SVILUPPO.md#lingue)).
