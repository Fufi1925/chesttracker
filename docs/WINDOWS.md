# Windows-Client v1.3.0

Voraussetzungen: Windows 10 ab Version 2004 (Build 19041) oder Windows 11, x64. Das ZIP vollständig entpacken; EXE, x64-Bibliotheken und tessdata zusammen lassen. .NET und OCR-Modelle sind enthalten.

1. Alte ChestTracker-Instanzen beenden. Neue ChestTracker.exe öffnen.
2. Es erscheint der dunkle Vollbild-Login mit dezenter Bewegung. Benutzername und dasselbe Passwort wie auf der Website verwenden. Serveradresse ist unter dem Login ausklappbar.
3. Nach erfolgreicher Anmeldung wird das Fenster automatisch 420 × 620 logische Pixel groß. Oben stehen Anzeigename und echte Kontostatistiken.
4. Fortnite starten öffnet den Epic Games Launcher, wenn kein sichtbares natives Fortnite-Fenster läuft. Sobald ein solches Fenster vorhanden ist, wird es automatisch erkannt. Ein laufendes Spiel muss nicht erneut gestartet werden.
5. Fortnite stoppen beendet ausschließlich ChestTracker-Erfassung und Bildaufnahme. Der Spielprozess wird nicht gewaltsam geschlossen.
6. Mit Ändern neben dem Overlay-Hotkey eine Taste von A–Z oder F1–F12 auswählen. Escape bricht ab. Buchstaben werden beim Tippen nicht verschluckt. Im aktiven ChestTracker-Fenster schaltet normales Tippen das Overlay nicht um.
7. Direkt über Fortnite starten **Bildanalyse und Truhenzählung aktivieren** einschalten. Die Freigabe wird lokal gespeichert, startet aber niemals automatisch beim Öffnen der EXE: Die Erfassung beginnt erst nach Fortnite starten. Es werden nur Bilder des erkannten Fortnite-Fensters verarbeitet, nur solange dieses im Vordergrund ist. Bilddaten und erkannte Texte werden nicht hochgeladen oder als Dateien gespeichert.

## Was die automatische Analyse tatsächlich tut

Windows Graphics Capture liefert die Bilder des gewählten Fortnite-Fensters. Die Bildauswertung sucht den Match-HUD sowie eigene Truhen-EP-Meldungen. Goldene und violette Truhenmeldungen werden mit separatem Pluszeichen und EP-Zeile über mehrere Bilder geprüft. Beim Abbau werden ausgewählte Spitzhacke, Ziel-Lebensleiste, visuelles Truhen-Sound-Symbol und neue eigene Behälter-Meldung beziehungsweise frischer Loot zeitlich kombiniert. Lootgebende Holzkisten zählen ebenfalls; Ressourcen ohne Item-Loot zählen nicht. Vorbeilaufen, ein Sound-Symbol oder vorhandener Loot allein sollen keine Truhe zählen. OCR für Lobby, Ergebnis und Texte läuft daneben im Hintergrund; Ereignisse werden entprellt und über die bestehende idempotente API synchronisiert.

Im Referenzclip werden fünf markierte Aktionen ohne zusätzliche Zählung sowie Ergebnis und Lobby erkannt. Das ist keine allgemeine Trefferquote. Details und bekannte Grenzen, bei verdeckten Meldungen, schnellen Aktionen und anderen HUD-Skalierungen: [DETECTION_REFERENCE.md](DETECTION_REFERENCE.md).

Unter deiner eigenen Zeile zeigt das Overlay bis zu fünf weitere aktive Konten mit geöffnetem Fortnite, einschließlich Name, Spielzustand und Truhenzähler im Match. Beide PCs sollten v1.3.0 verwenden, angemeldet sein und Fortnite geöffnet haben. Aktualisierung etwa alle fünf Sekunden; ohne Heartbeat verschwindet ein Spieler nach rund 20 Sekunden. Fortnite offen ist unabhängig von der Bildanalyse; ohne Freigabe bleibt der Matchzustand unbekannt.

Das Overlay zeigt im Match Truhen, in der Lobby Lobby und nach dem Ergebnis die Rundensumme. **Bildanalyse aus**: Freigabe einschalten. **Analyse pausiert**: Fortnite in den Vordergrund holen. **Keine Spielbilder**: Fortnite im Fenster-Vollbild testen und Hinweise im Accountfenster beachten. **HUD unbekannt**: Bilder kommen an, aber der Spielzustand ist noch nicht eindeutig. Verbunden bedeutet allein, dass der Server erreichbar ist.

Stoppen, Schließen oder Verlust des Fensters markiert ein noch aktives Match als unvollständig. Ein erkannter Ergebnisbildschirm oder eine stabil erkannte Lobby schließt es ab. Die tatsächlich erreichbare Bildrate und das Overlay im Spiel müssen am Ziel-PC geprüft werden. Die EXE ist nicht codesigniert.

## Offline-Test

Unter Einstellungen und Erkennung → Offline-Replay testen kann replays/synthetic-two-openings.json geladen werden. Dieses Replay läuft nur lokal und verändert keine zentrale Statistik. Bild-Replay- und Template-Komponenten sind weiterhin als Quellcode vorhanden; der Hauptablauf bleibt bewusst auf Login, Start/Stopp und Hotkey konzentriert.

## Lokale Daten

Sitzungen, Aufnahmefreigabe, Hotkey und die dauerhafte Warteschlange liegen mit Windows-DPAPI geschützt unter %LOCALAPPDATA%\\ChestTracker. Das neue Fenster zeigt die Anzahl noch nicht synchronisierter Ereignisse. Keine Klartextpasswörter in Konfigurationsdateien. Bei Offline-Verbindung werden Events später mit denselben IDs wiederholt.

## Technische Quellen

- Windows-Fensteraufnahme: https://learn.microsoft.com/en-us/windows/apps/develop/media-authoring-processing/screen-capture
- CreateForWindow: https://learn.microsoft.com/en-us/windows/win32/api/windows.graphics.capture.interop/nf-windows-graphics-capture-interop-igraphicscaptureiteminterop-createforwindow
- Epic zu Drittsoftware: https://www.epicgames.com/help/c-34254770/c-40491939/me-suspenderan-si-uso-software-de-terceros-en-fortnite-a15028164
- Wettbewerbsregeln: https://www.fortnite.com/competitive/rules-guidelines/rules-library/fortnite-competitive-master-rules
- OCR-Komponenten und Lizenzen: THIRD_PARTY.md
