ChestTracker v1.2.0 – HUD- und Truhenerkennung überarbeitet.

- Behebt dauerhaft unbekannten Matchstatus beim aktuellen HUD ohne ausgeschriebenes STURM neben der Uhr.
- Erkennt die eigene kurze EP-Meldung TRUHE und kombiniert bei Abbau Ziel-Lebensleiste, visuelles Truhen-Sound-Symbol und neu erscheinenden Loot über mehrere Bilder.
- Schnellere lokale Bildanalyse mit separater OCR im Hintergrund. Ergebnisbildschirm schließt die Runde ab; Zuschauer-HUD erzeugt kein neues Match. Spielen-Schaltfläche und Lobby-Panel werden separat ausgewertet.
- Aufnahmefreigabe direkt im Accountfenster. Overlay unterscheidet Bildanalyse aus, Analyse pausiert, Keine Spielbilder, Match und Lobby.
- Unveränderte gemeinsame Anmeldung, Statistiksynchronisierung und sechs aktive Konten.

Referenzdurchlauf: In einem privaten Kalibrierungsclip werden fünf markierte Truhenaktionen erkannt, ohne zusätzliche Ereignisse bei den enthaltenen Negativfällen. Anschließend werden Ergebnis und Lobby erkannt. Dies ist eine Kalibrierungsaufnahme und keine allgemeine Erkennungsquote. Direkte Zerstörung einer Truhe, andere HUDs und Teamaktionen benötigen weitere Aufnahmen und Tests. Details: docs/DETECTION_REFERENCE.md sowie die gleichnamige Datei im ZIP.

Prüfungen: 40 Core-/Detektortests, 9 Windows-Tests einschließlich WPF-Ansichten, nativer OCR und gemeinsamer Bildvorverarbeitung; außerdem 23 API-Tests und 3 Browserabläufe. Der Build prüft zusätzlich, dass die veröffentlichte EXE die mitgelieferte OCR laden kann. Veröffentlichung erfolgt aus einem erfolgreichen CI-Lauf. Ein echter Live-Test im Spiel am Ziel-PC steht noch aus.

Installation: Alte EXE schließen, dieses ZIP in einen neuen Ordner vollständig entpacken, ChestTracker.exe starten und mit den bisherigen Daten anmelden. Direkt über Fortnite starten die Option Bildanalyse und Truhenzählung aktivieren einschalten. Fortnite muss während der Analyse im Vordergrund sein. Alle mitgelieferten Bibliotheken und tessdata zusammenlassen; bei Keine Spielbilder das Spiel im Fenster-Vollbild testen.

Das Nutzer-Video, dessen Einzelbilder und daraus abgeleitete Testdaten werden nicht veröffentlicht. Live-Bilder und OCR-Texte bleiben im Arbeitsspeicher des PCs. Der öffentliche Download wurde vom Eigentümer freigegeben; Statistiken benötigen weiterhin den App-Login. Die EXE ist nicht codesigniert.

Website: https://chesttracker-fufi1925s-projects.vercel.app
