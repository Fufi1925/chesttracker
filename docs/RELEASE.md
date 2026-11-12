ChestTracker v1.1.0 – Neuer Windows-Client.

- Dunkler Vollbild-Login mit mittiger Anmeldung und dezenter Hintergrundbewegung. Nach Login automatisch kompaktes 420 × 620-Fenster.
- Anzeigename und echte Kontostatistiken oben. Fortnite starten/stoppen und frei wählbarer Hotkey darunter.
- Start über den Epic-Launcher und automatische Erkennung eines bereits laufenden sichtbaren nativen Fortnite-Fensters. Stoppen beendet die Erfassung, ohne das Spiel gewaltsam zu schließen.
- Optional aktivierbare lokale Windows-Fensteraufnahme mit Tesseract-OCR für deutsche/englische HUD-Hinweise. Keine Bilder oder OCR-Texte werden hochgeladen.
- Konservativer Match-/Lobby-Zustandsautomat, Entprellung von Öffnungshinweisen, lokaler Puffer und idempotente Synchronisierung.
- Vollständiges Paket mit OCR-Sprachmodellen, nativen Bibliotheken, Anleitung und Prüfsumme.

Prüfungen: 24 Core-/Detektor-/Hotkey-Tests, 8 Windows-Tests einschließlich WPF-Vollbild-/Kompaktansicht, nativer OCR und Login; außerdem 22 API-Tests und 3 Browserabläufe. Der Build prüft zusätzlich, dass die veröffentlichte EXE ihre mitgelieferte OCR laden kann. Das Release wird erst aus einem erfolgreichen CI-Lauf veröffentlicht.

Wichtig: Echte Fortnite-Truhenerkennung und Regelverträglichkeit sind weiterhin unvalidiert. Es liegen keine echten Testclips vor; aktuelle HUDs können von den implementierten Textmustern abweichen. Kein Versprechen zuverlässiger Zählung oder Sperrensicherheit. Nicht für Turniere verwenden. Unklare Bilder liefern Status unbekannt statt erfundener Werte. Die EXE ist nicht codesigniert.

ZIP vollständig entpacken, alle beigefügten Dateien zusammenlassen und ChestTracker.exe starten. Website und EXE verwenden dieselben Zugangsdaten. Der öffentliche Download wurde vom Eigentümer ausdrücklich freigegeben; Website-Statistiken bleiben hinter dem App-Login.

Website: https://chesttracker-fufi1925s-projects.vercel.app
