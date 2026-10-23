# Control Save Manager

Più partite di **Control** (Steam) sullo stesso account: scegli quale giocare
e il programma carica i suoi salvataggi e avvia il gioco. Con i punti salvati
da usare prima dei passaggi difficili e una copia automatica prima di ogni
operazione.

![Finestra principale](docs/img/main.png)

*[English below](#english)*

## Installazione

1. Scarica `ControlSaveManager-<versione>-win-x64.zip` dalla pagina dei
   rilasci ed estrai `ControlSaveManager.exe` dove vuoi.
2. Avvialo. Non serve installare nulla: il programma contiene già tutto quello
   che gli serve (per questo pesa circa 50 MB).

Requisiti: Windows 10 o 11 a 64 bit, Control su Steam avviato almeno una volta.

> **Windows potrebbe avvisarti o bloccarlo.** Il programma non è firmato
> digitalmente, quindi SmartScreen può mostrare "Windows ha protetto il PC"
> (scegli *Ulteriori informazioni → Esegui comunque*) e Smart App Control, se
> attivo, può impedirne l'avvio.

Al primo avvio il programma ti chiede come chiamare la partita che è già nel
gioco e la salva come prima partita.

### Prima di cominciare: disattiva Steam Cloud per Control

Con Steam Cloud attivo, all'avvio del gioco Steam può rimettere salvataggi di
un'altra partita. Disattivalo per Control: in Steam, **Libreria → tasto destro
su Control → Proprietà → Generale** e spegni *Mantieni i salvataggi su Steam
Cloud*. Finché è attivo, il programma lo segnala con una barra gialla in alto.

## Come si usa

### Le partite

Ogni partita ha i suoi salvataggi. Quella con il bordo rosso e la scritta
*NEL GIOCO ADESSO* è quella caricata nel gioco.

- **Gioca questa**: salva i progressi della partita di adesso, carica quella
  scelta e avvia Control tramite Steam. Non ci sono domande: prima di ogni
  cambio si fa una copia.
- **Gioca**, sulla partita già nel gioco: avvia solo Control.
- **⋯** su ogni partita: *Carica senza avviare*, *Rinomina*, *Modifica nota*,
  *Esporta in un file...* ed *Elimina partita...*. Se carichi senza avviare,
  in basso compare **Annulla** per tornare subito alla partita di prima.
- **Nuova partita**: con i salvataggi attuali del gioco oppure vuota, per
  ricominciare dall'inizio.

  ![Nuova partita](docs/img/newprofile.png)

- **Importa...**: aggiunge una partita esportata da un altro PC.

Ogni scheda mostra la nota, il tempo di gioco, quando è stata salvata e i
punti salvati.

### Mentre giochi

Finché Control è aperto il programma non tocca i salvataggi e lo dice in alto.
Quando chiudi il gioco, i progressi vengono salvati da soli nella partita che
stavi giocando e il tempo di gioco viene aggiornato (serve che il programma
sia aperto).

![Gioco aperto](docs/img/playing.png)

### Cronologia e punti salvati

**Salva un punto** (dalla partita nel gioco) salva i salvataggi di adesso con
un nome, per esempio prima di un boss: se va male, torni lì. La
**Cronologia** mostra i tuoi punti salvati e le copie automatiche fatte prima
di ogni operazione; ogni riga ha **Torna qui**. Prima di tornare indietro la
situazione attuale viene copiata, quindi puoi sempre cambiare idea.

![Cronologia](docs/img/history.png)

### Impostazioni

- *«Gioca questa» avvia anche Control*: se lo spegni, la partita viene solo
  caricata.
- *Salva i progressi quando chiudi Control*.
- *Stesse opzioni del gioco per tutte le partite*: comandi, audio e
  sottotitoli restano uguali cambiando partita.
- Quante copie automatiche tenere (20 di base, 0 = tutte).

![Impostazioni](docs/img/settings.png)

## Dove sono i file

Partite, punti salvati, copie automatiche e registro delle operazioni
(`log.txt`) sono in `%LOCALAPPDATA%\ControlSaveManager` (di solito
`C:\Users\<nome>\AppData\Local\ControlSaveManager`). È una cartella che OneDrive
non sincronizza: così i salvataggi non vengono bloccati, scaricati a metà o
duplicati come "copie in conflitto". Le Impostazioni hanno un pulsante per
aprirla; altrimenti premi Win+R e scrivi `%LOCALAPPDATA%\ControlSaveManager`.

Se più account Steam sul PC hanno Control, all'avvio il programma chiede quale
usare; ogni account ha le sue partite.

Se avevi usato una versione di prova che salvava in `Documenti\ControlSaves`,
al primo avvio i dati vengono spostati da soli nella nuova cartella e il
programma te lo dice.

Per disinstallare basta cancellare l'exe. Se vuoi, cancella anche
`%LOCALAPPDATA%\ControlSaveManager` (ma lì ci sono le tue partite).

## Domande

**Ho caricato la partita sbagliata.** Se il gioco non è partito, premi
**Annulla** nell'avviso in basso; altrimenti gioca quella di prima: i suoi
progressi erano stati salvati prima del cambio.

**Ho perso dei progressi.** Apri la **Cronologia**: c'è una copia prima di
ogni operazione, con data e descrizione.

**Ho eliminato una partita per sbaglio.** In fondo alla finestra principale
compare *Partite eliminate*: da lì la recuperi, finché la pulizia delle copie
automatiche non la toglie.

**I salvataggi si mescolano tra le partite.** Quasi sempre è Steam Cloud:
controlla che sia disattivato (vedi sopra).

**Lingua.** Il programma è in italiano o in inglese, in base alla lingua di
Windows.

## English

Several **Control** (Steam) playthroughs on the same account: pick the one to
play and the app loads its saves and starts the game. With saved points for
the hard parts and an automatic copy before every operation. The app is in
English or Italian, following the Windows language.

![Main window](docs/img/main_en.png)

- Download the zip from the releases page, extract `ControlSaveManager.exe`
  and run it. No installation needed (Windows 10/11, 64-bit). It is not
  digitally signed, so SmartScreen may warn you and Smart App Control may
  block it. On first start it saves the playthrough already in the game.
- **Turn off Steam Cloud for Control** (Library → right-click Control →
  Properties → General), otherwise Steam may mix up playthroughs. A yellow bar
  shows while it is on.
- **Play this** saves the current playthrough, loads the chosen one and starts
  Control through Steam. The **⋯** menu has *Load without starting* (with
  *Undo*), *Rename*, *Edit note*, *Export* and *Delete*. **Import...** adds an
  exported playthrough.
- When you close the game, progress is saved automatically and the play time
  is updated (while the app is open).
- **Save a point** and **History**: saved points and automatic copies, each
  with *Go back here*.
- Everything is in `%LOCALAPPDATA%\ControlSaveManager`, a folder OneDrive
  doesn't sync.

## Licenza

[MIT](LICENSE). Novità di ogni versione nel [CHANGELOG](CHANGELOG.md). Per chi
vuole lavorare sul codice: [docs/SVILUPPO.md](docs/SVILUPPO.md).

*Control è un marchio di Remedy Entertainment. Questo programma non è affiliato
né approvato da Remedy o da Valve.*
