# Cose da fare

Lista per arrivare a una versione "production ready", in ordine di priorità.
I bug noti sono in [BUG_NOTI.md](BUG_NOTI.md).

## Indispensabile

- [ ] **Firma del codice.** Senza firma Smart App Control e SmartScreen bloccano
  o avvisano chi scarica l'exe. Opzione più economica: Azure Trusted Signing.
- [x] **Attivazione in due passaggi.** Copiare il profilo in una cartella
  temporanea accanto a quella del gioco, verificarla e poi scambiare le
  cartelle, invece di cancellare e copiare direttamente. Stesso schema per
  "Salva progressi".
- [x] **Verifica dopo la copia.** Usare `FileOps.FoldersAreEqual` dopo ogni
  copia; se non coincide, ripristinare il backup.
- [x] **Scrittura sicura di `stato.ini`.** Scrivere un file temporaneo e poi
  sostituirlo, così un'interruzione non lo lascia vuoto.
- [x] **Una sola istanza.** `Mutex` all'avvio per impedire due finestre aperte
  che lavorano sugli stessi file.
- [x] **Errori imprevisti e registro.** Gestore globale delle eccezioni con
  messaggio comprensibile; file `ControlSaves\log.txt` con ogni operazione
  (data, profilo, backup, esito).

## Importante

- [x] **Savestate senza cambiare profilo.** Salvare un'istantanea dei
  salvataggi attuali del gioco (per esempio prima di un boss) senza
  attivare un altro profilo e senza sovrascrivere il profilo attivo.
  - Pulsante "Crea savestate" con un nome facoltativo (es. "Prima di Ahti").
  - I savestate appartengono al profilo attivo e stanno in
    `ControlSaves\<IDAccount>\Savestate\<profilo>\<data>_<nome>\`, accanto a
    `Profili` e `Backup`.
  - Elenco dei savestate del profilo con data e nome; "Ripristina" li
    rimette nella cartella del gioco (con backup prima, come le altre
    operazioni) ed "Elimina".
  - Si può riusare la logica di backup e ripristino del punto successivo.
- [x] **Pulizia dei backup.** Tenere per esempio gli ultimi 20 o quelli degli
  ultimi 30 giorni, impostabile (bug 12). Fatto con "ultimi N" (predefinito 20,
  0 = tutti), impostabile dalla finestra Backup.
- [x] **Ripristinare un backup dall'app.** Elenco dei backup con data e motivo,
  pulsante "Ripristina".
- [x] **Rinominare ed eliminare i profili** dalla finestra.
- [x] **Più account Steam.** Chiedere quale usare se ce n'è più di uno (bug 13).
- [x] **Test nel repository.** Progetto xUnit con i test su cartelle finte in
  `tests/ControlSaveManager.Tests`. Niente CI: il repository è su Forgejo
  senza runner, i test girano a mano e in `scripts\release.ps1`.
- [x] **Pubblicazione.** Exe singolo, versione, nome prodotto e copyright nel
  `.csproj`, zip di rilascio. Fatto: profilo `win-x64` (exe singolo con il
  runtime incluso) e `scripts\release.ps1`; lo zip si carica a mano nelle
  Release di Forgejo.

## Rifiniture

- [x] **Finestre di conferma a tema** al posto delle `MessageBox` chiare di Windows
  (`MessageDialog.cs`).
- [x] **Lingua.** Spostare i testi in file `.resx` per aggiungere l'inglese.
  Inglese in `Strings.resx`, italiano in `Strings.it.resx`; la lingua segue Windows.
- [x] **Steam Cloud.** Provarlo con il cloud attivo, documentarlo o rilevarlo.
  Rilevato da `sharedconfig.vdf` e mostrato nella finestra; documentato nel
  README. Non ancora provato un cambio di profilo con il cloud attivo.
- [x] **Licenza, changelog e README per l'utente finale** con le schermate.
  MIT, `CHANGELOG.md`, README per l'utente e note tecniche in `docs/SVILUPPO.md`.
