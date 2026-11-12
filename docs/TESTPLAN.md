# Testplan und gemessene Grenzen

## Automatisch

- 22 API-/Datenbanktests: idempotenter Bootstrap und Argon2id, private Daten, Erstlogin, Passwortwechsel, Origin/CSRF, Freundeskonto, serverseitige Admin-Rollen, Vier-Konten-Limit, gemeinsame Desktop-Identität, Match-Start, Ereignis-Deduplikation, ungültige Payloads/Zeiten, Präsenz/Timeout, Abschluss und Durchschnitt aus echten Daten, Fremdmatchschutz, Admin-Korrektur und Audit, Refresh-Replay, Deaktivierung, Ablauf/Logout und persistente Login-Limits. Zusätzlich Kontolöschung mit allen abhängigen Daten, sofortigem Sitzungsverlust und Schutz des Administrators.
- 24 plattformunabhängige Detektor-/Tastentests: vorbeigelaufen, eigener Öffnungshinweis, Dauermeldung, Teamkamerad, schnell getrennte Meldungen, unsichtbarer Hinweis, Lobby-Debounce, Ladeunterbrechung, Matchwechsel, Offline, unsicherer Zustand, wiederholter Frame, unvollständiges Ende. Zusätzlich Tasteneingaben im Loginfenster, gehaltene Tasten und Fokuswechsel.
- Acht Windows-Pixel-/Login-/GUI-Tests: exaktes Template mit Negativfall, ungültige ROI, unverändertes O/o im Desktop-Login, verständliche HTML-Fehler und unveränderte Serverfehlermeldungen. Zusätzlich Vollbild-/Kompaktansicht mit echten WPF-Screenshots, native OCR mit Sprachmodellen und parallele Token-Erneuerung. Diese Tests laufen ausschließlich auf Windows.
- Drei Browser-Tests gegen eine isolierte PostgreSQL-kompatible PGlite-Datenbank: Login/Dashboard/Admin/Logout, Mobilnavigation ohne Dokumentüberlauf, anonyme API-Sperre. Keine Produktionssecrets und keine produktiven Demokonten.

## Manuell am Windows-Zielsystem

- Oben rechts, 220 × 36 logische Pixel; 100/125/150-%-Skalierung und mehrere Monitore testen.
- Overlay klickdurchlässig; keine Fokuswechsel. O/F8/F9 zweimal schalten und prüfen, dass der Zähler bei ausgeblendeter Anzeige weiterläuft.
- Lobby grün ohne Zähler; Match blau mit laufendem Zähler; Server offline und unsicherer Spielzustand müssen unterscheidbar sein.
- Während synchronisiertem Replay Verbindung unterbrechen, wiederherstellen, Match und Events mit dem Dashboard vergleichen. Wiederholungen dürfen die Summe nicht verändern. Beim Wiederanlauf ein unterbrochenes Replay nur als unvollständig markieren.
- Jeder der vier Nutzer kann freigegebene Profile lesen; normale Konten können keine Statistikwerte korrigieren.

## Messungen

Es wurden keine echten Fortnite-Clips bereitgestellt. Daher **keine gemessene Live-Precision/Recall**, keine nachgewiesene Truhenöffnungs-Erkennung und keine Anti-Cheat-Zusage. Erfolgreiche synthetische Tests belegen die implementierte Logik, nicht Erkennung im Spiel. Frame-Auswertung startet beim Replay mit ungefähr 12 FPS entsprechend den Fixture-Zeiten. Die Template-Vorverarbeitung läuft offline. Der Live-OCR-Adapter wertet höchstens etwa drei Bilder pro Sekunde aus, ohne parallele Analyseaufträge anzustauen; tatsächlich erreichbare Rate und CPU-Last müssen am Ziel-PC gemessen werden.

Vor Live-Freigabe je Auflösung/Sprache/Skalierung annotierte positive und negative Clips auswerten. TP/FP/FN pro Öffnung und Spieler zählen, Precision = TP/(TP+FP), Recall = TP/(TP+FN). Zusätzlich CPU/RAM/GPU, Frames/s, Benachrichtigungsdauer und Erkennungsverzögerung messen. Fälle: vorbei, angesehen, geöffnet, Dauermeldung, zwei schnelle Öffnungen, Teamkamerad, fehlende Meldung, Spielerwechsel, Ladebildschirm, Lobby, Verbindungsabbruch und Spielende. Kein Wert wird ohne Messgrundlage angegeben.
