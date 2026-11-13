# Testplan und gemessene Grenzen

## Automatisch

- 24 API-/Datenbanktests: idempotenter Bootstrap und Argon2id, private Daten, Erstlogin, Passwortwechsel, Origin/CSRF, Freundeskonto, serverseitige Admin-Rollen, Sechs-Konten-Limit, gemeinsame Desktop-Identität, Match-Start, Ereignis-Deduplikation, ungültige Payloads/Zeiten, Präsenz/Timeout, Abschluss und Durchschnitt aus echten Daten, Fremdmatchschutz, Admin-Korrektur und Audit, Refresh-Replay, Deaktivierung, Ablauf/Logout und persistente Login-Limits. Zusätzlich Kontolöschung mit allen abhängigen Daten, sofortigem Sitzungsverlust und Schutz des Administrators.
- 57 plattformunabhängige Detektor-/Tastentests: vorbeigelaufen, eigener Öffnungshinweis, Dauermeldung, Teamkamerad, schnell getrennte Meldungen, unsichtbarer Hinweis, Lobby-Debounce, Ladeunterbrechung, Matchwechsel, Offline, unsicherer Zustand, wiederholter Frame, unvollständiges Ende. Zusätzlich Tasteneingaben im Loginfenster, gehaltene Tasten und Fokuswechsel.
- Zehn Windows-Pixel-/Login-/GUI-Tests: exaktes Template mit Negativfall, ungültige ROI, unverändertes O/o im Desktop-Login, verständliche HTML-Fehler und unveränderte Serverfehlermeldungen. Zusätzlich Vollbild-/Kompaktansicht mit echten WPF-Screenshots, native OCR mit Sprachmodellen und parallele Token-Erneuerung. Diese Tests laufen ausschließlich auf Windows.
- Drei Browser-Tests gegen eine isolierte PostgreSQL-kompatible PGlite-Datenbank: Login/Dashboard/Admin/Logout, Mobilnavigation ohne Dokumentüberlauf, anonyme API-Sperre. Keine Produktionssecrets und keine produktiven Demokonten.

## Manuell am Windows-Zielsystem

- Oben rechts, 260 Pixel breit, variable Höhe bis 200 logische Pixel; 100/125/150-%-Skalierung und mehrere Monitore testen.
- Overlay klickdurchlässig; keine Fokuswechsel. O/F8/F9 zweimal schalten und prüfen, dass der Zähler bei ausgeblendeter Anzeige weiterläuft.
- Lobby grün ohne Zähler; Match blau mit laufendem Zähler; Server offline und unsicherer Spielzustand müssen unterscheidbar sein.
- Während synchronisiertem Replay Verbindung unterbrechen, wiederherstellen, Match und Events mit dem Dashboard vergleichen. Wiederholungen dürfen die Summe nicht verändern. Beim Wiederanlauf ein unterbrochenes Replay nur als unvollständig markieren.
- Zwei oder mehr PCs: jeweils eigenes Konto, neuer Client, Fortnite offen; eigene Zeile zuerst, andere darunter. Stoppen, Fenster schließen, Logout, Netzverlust und Heartbeat-Ablauf prüfen. Fremde unbekannte HUDs müssen als Fortnite offen erscheinen, statt einen Matchzähler zu erfinden.
- Jeder der sechs Nutzer kann freigegebene Profile lesen; normale Konten können keine Statistikwerte korrigieren.

## Messungen

Ein privater Kalibrierungsclip wurde lokal ausgewertet: fünf markierte Truhenaktionen, fünf Ereignisse, keine zusätzlichen Ereignisse in den enthaltenen Negativfällen. Ergebnis und Lobby werden erkannt. Kalibrierung und Grenzen: [DETECTION_REFERENCE.md](DETECTION_REFERENCE.md). Dies ergibt keine unabhängige oder allgemeine Live-Precision/Recall. Die Referenzdaten bleiben lokal und sind nicht Teil des öffentlichen Repositories. Windows-CI prüft zusätzlich, dass die gemeinsame OCR-Vorverarbeitung native PGM-Bilder laden und die gelbe Spielen-Schaltfläche und den eigenen Behälter-Hinweis lesen kann. Die WPF-Prüfung rendert zusätzlich eigene Zeile und mehrere Mitspieler. Die zweite Aufnahme dient ebenfalls der Kalibrierung; 95 % sind nicht nachgewiesen.

Live: maximal acht Bildanalysen pro Sekunde, ohne Warteschlange alter Bilder; OCR höchstens ein paralleler Auftrag, frühestens alle 750 ms. Tatsächliche Bildrate und CPU-Last müssen am Ziel-PC gemessen werden.

Vor Live-Freigabe je Auflösung/Sprache/Skalierung annotierte positive und negative Clips auswerten. TP/FP/FN pro Öffnung und Spieler zählen, Precision = TP/(TP+FP), Recall = TP/(TP+FN). Zusätzlich CPU/RAM/GPU, Frames/s, Benachrichtigungsdauer und Erkennungsverzögerung messen. Fälle: vorbei, angesehen, geöffnet, Dauermeldung, zwei schnelle Öffnungen, Teamkamerad, fehlende Meldung, Spielerwechsel, Ladebildschirm, Lobby, Verbindungsabbruch und Spielende. Kein Wert wird ohne Messgrundlage angegeben.
