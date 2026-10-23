# Cose da fare

Quello che manca, in ordine di priorità. I bug sono in
[BUG_NOTI.md](BUG_NOTI.md); quello che è già stato fatto è nel
[CHANGELOG](../CHANGELOG.md).

## Da fare

- [ ] **Firma del codice. Urgente.** Il 23/10/2026, sul PC di sviluppo con
  Smart App Control attivo, ogni nuova build dell'app veniva bloccata: l'exe,
  la dll e tre exe singoli rigenerati (il 22/10 l'exe singolo partiva). Chi ha
  Smart App Control attivo potrebbe non riuscire ad avviare la 0.9.0. Un
  certificato autofirmato non basta. Strada scelta: firma gratuita di SignPath
  Foundation con la build su GitHub Actions, già pronta (vedi
  [SVILUPPO.md](SVILUPPO.md#firma-del-codice)). Mancano:
  - [x] controllare che la prima build su GitHub vada a buon fine;
  - [x] pubblicare la prima versione (0.9.0, non firmata);
  - [ ] fare domanda su [signpath.org](https://signpath.org/);
  - [ ] configurare progetto e token su SignPath e i secret/variabili su
    GitHub;
  - [ ] fare un rilascio con tag e verificare che l'exe firmato parta con
    Smart App Control attivo.

## Prove con il gioco vero

Prove fatte il 23/10/2026 sui dati veri, dopo una copia di sicurezza
verificata (`H:\Backup Control Save Manager\2026-10-23_08-55`). L'app è stata
avviata da un eseguibile di prova con lo stesso codice, perché Smart App Control
bloccava quello vero (vedi sopra).

Riuscite:

- [x] **Spostamento da Documenti** con i dati veri (22/10, 728 file, esito
  "Moved" nel registro).
- [x] **Spostamento tra due dischi veri** (H: → C:) con dati finti: 60 file su
  60 identici, vecchia cartella eliminata, nessun avanzo.
- [x] **Avvio di Control** con `SaveManager.LaunchGame`
  (`steam://rungameid/870780`): con Steam chiuso, il gioco è partito in 21 s.
- [x] **Sessione di gioco**: alla chiusura del gioco l'app ha registrato 103 s
  nella partita giusta. Il gioco, rimasto al menu, non ha cambiato i
  salvataggi e l'app non ha fatto salvataggi inutili.
- [x] **Dati intatti**: dopo le prove, i dati dell'app erano identici alla
  copia di sicurezza (a parte registro e tempo di gioco).

Da fare a mano (servono azioni dentro il gioco o una scelta su Steam):

- [ ] **Salvataggio alla chiusura con progressi veri.** Giocare fino a un
  salvataggio, chiudere e controllare che la partita sia aggiornata (oggi
  provato solo senza progressi e con i test automatici).
- [ ] **Opzioni condivise.** `preferences_data` è un binario di 764 byte
  (contiene `default` e un identificativo) e le due partite ne hanno versioni
  diverse. Cambiare un'opzione nel gioco (per esempio i sottotitoli), cambiare
  partita con l'impostazione attiva e verificare che resti.
- [ ] **Esporta e importa tra due PC.** Sui dati veri non è stato possibile
  provarlo per i blocchi di Smart App Control; il percorso è coperto dai test
  automatici.
- [ ] **Steam Cloud attivo.** Non provato apposta: riattivarlo potrebbe far
  riscaricare salvataggi vecchi. Oggi l'app avvisa e consiglia di disattivarlo.
- [ ] **Giro completo.** Due partite, punto salvato prima di un boss, ritorno
  al punto, cambio di partita e ritorno: ogni partita deve ritrovare i suoi
  punti di controllo.

## Idee

- **Collegamento sul desktop "Gioca <partita>".** Un clic carica la partita e
  avvia il gioco senza aprire l'app. Va controllato con Smart App Control.
- **Più lingue.** Basta un `Strings.<codice>.resx` con le stesse chiavi (vedi
  [SVILUPPO.md](SVILUPPO.md#lingue)).
