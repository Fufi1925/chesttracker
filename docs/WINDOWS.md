# Windows-Client v1.1.0

Voraussetzungen: Windows 10 ab Version 2004 (Build 19041) oder Windows 11, x64. Das ZIP vollständig entpacken; EXE, x64-Bibliotheken und tessdata zusammen lassen. .NET und OCR-Modelle sind enthalten.

1. Alte ChestTracker-Instanzen beenden. Neue ChestTracker.exe öffnen.
2. Es erscheint der dunkle Vollbild-Login mit dezenter Bewegung. Benutzername und dasselbe Passwort wie auf der Website verwenden. Serveradresse ist unter dem Login ausklappbar.
3. Nach erfolgreicher Anmeldung wird das Fenster automatisch 420 × 620 logische Pixel groß. Oben stehen Anzeigename und echte Kontostatistiken.
4. Fortnite starten öffnet den Epic Games Launcher, wenn kein sichtbares natives Fortnite-Fenster läuft. Sobald ein solches Fenster vorhanden ist, wird es automatisch erkannt. Ein laufendes Spiel muss nicht erneut gestartet werden.
5. Fortnite stoppen beendet ausschließlich ChestTracker-Erfassung und Bildaufnahme. Der Spielprozess wird nicht gewaltsam geschlossen.
6. Mit Ändern neben dem Overlay-Hotkey eine Taste von A–Z oder F1–F12 auswählen. Escape bricht ab. Buchstaben werden beim Tippen nicht verschluckt. Im aktiven ChestTracker-Fenster schaltet normales Tippen das Overlay nicht um.
7. Optional unter Einstellungen und Erkennung die lokale experimentelle Fensteranalyse aktivieren. Die Freigabe wird lokal gespeichert, startet aber niemals automatisch beim Öffnen der EXE: Die Erfassung beginnt erst nach Fortnite starten. Es werden nur Bilder des erkannten Fortnite-Fensters verarbeitet, nur solange dieses im Vordergrund ist. Bilddaten und erkannte Texte werden nicht hochgeladen oder als Dateien gespeichert.

## Was die automatische Analyse tatsächlich tut

Windows Graphics Capture liefert die Bilder des gewählten Fenster-Handles, mit der normalen Windows-Aufnahmeanzeige. Lokale Tesseract-OCR wertet begrenzte HUD-Regionen auf Deutsch/Englisch aus. Der Zustandsautomat bestätigt Match-/Lobby-Hinweise über mehrere Bilder. Eine Truhenöffnung benötigt eine explizite Öffnungsmeldung und einen passenden positiven EP-/XP-Hinweis; eine sichtbare Truhe allein zählt nie. Mehrfachbilder werden entprellt. Metadaten/Events laufen über die idempotente API.

Ein geöffnetes Fortnite-Fenster bedeutet allein noch nicht Lobby oder Match. Fehlen eindeutige Hinweise, steht Status unbekannt. Bei Ladebildschirm, Fensterwechsel oder Alt-Tab wird kein Matchausgang erfunden. Stoppen/Schließen markiert ein laufendes Match nur als unvollständig.

**Die Erkennung ist an echten Fortnite-Aufnahmen weiterhin unvalidiert.** Die implementierten Textmuster passen möglicherweise nicht zum aktuellen HUD, zur Sprache, UI-Skalierung oder zu realen Öffnungshinweisen. Es gibt keine nachgewiesene Live-Precision/Recall, keine Garantie, dass eigene und Team-Aktionen in jedem Fall unterscheidbar sind, und keine Zusage von Epic-Zulässigkeit oder Sperrensicherheit. Nicht bei Turnieren verwenden. Ohne eindeutige Treffer bleiben Zähler und Status konservativ.

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
