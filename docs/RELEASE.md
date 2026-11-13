ChestTracker v1.3.0 – Mitspieler im Overlay und erweiterte Bilderkennung.

- Dein eigener Name bleibt oben. Darunter erscheinen bis zu fünf weitere angemeldete Nutzer mit geöffnetem Fortnite, Spielzustand und aktuellem Truhenzähler. Aktualisierung ungefähr alle fünf Sekunden; veraltete Präsenz läuft nach 20 Sekunden ab.
- Fensterstatus wird unabhängig vom erkannten Match-HUD synchronisiert. Bei unbekanntem fremdem HUD steht Fortnite offen.
- Goldene und violette eigene Truhen-EP-Meldungen werden mit separatem Plus-/EP-Muster und zeitlicher Bestätigung ausgewertet, einschließlich zusammengefasster EP-Zeilen. Sperren gegen stehenbleibende und doppelte Meldungen.
- Nach Nutzerwunsch zählen auch lootgebende Holzkisten. Eigener Abbau, Spitzhacke, Ziel-Lebensleiste, Truhensymbol und neue eigene Behälter-Meldung beziehungsweise frischer Loot werden kombiniert. Bereits herumliegender Loot und Ressourcen allein zählen nicht.
- Minimap/Uhr als ergänzende Matchhinweise; Epischer Sieg beendet die Runde.

Prüfungen: 57 Core-/Detektortests, 10 Windows-Tests einschließlich WPF-Mehrspieleransicht und nativer OCR, 24 API-Tests und 3 Browserabläufe. Der Paket-Build prüft zusätzlich das Laden der mitgelieferten OCR. Veröffentlichung erfolgt ausschließlich aus einem erfolgreichen Windows-/Web-CI-Lauf.

Zwei private Aufnahmen wurden lokal zur Kalibrierung verwendet. Im ersten Clip werden fünf markierte Aktionen sowie Ergebnis und Lobby erkannt. Die zweite Aufnahme erweitert die Erkennung, enthält aber weiterhin verpasste Fälle. 95 % allgemeine Genauigkeit sind nicht nachgewiesen. Details: docs/DETECTION_REFERENCE.md und DETECTION_REFERENCE.md im ZIP. Ein echter Live-Test an zwei Fortnite-PCs steht noch aus.

Installation: Alte EXE schließen, neues ZIP in einen neuen Ordner vollständig entpacken und ChestTracker.exe starten. Alle Teilnehmer verwenden die neue Version und ihre bisherigen eigenen Zugangsdaten. Bildanalyse und Truhenzählung aktivieren einschalten und Fortnite starten drücken. Fortnite während der Analyse im Vordergrund halten. Bei Keine Spielbilder Fenster-Vollbild testen. Bibliotheken und tessdata zusammenlassen.

Die Videos, Einzelbilder, OCR-Texte und abgeleiteten Referenzdaten bleiben lokal. Live-Bilder werden nicht gespeichert oder hochgeladen. Der öffentliche Download ist vom Eigentümer freigegeben; Statistiken bleiben durch den App-Login geschützt. Die EXE ist nicht codesigniert.

Website: https://chesttracker-fufi1925s-projects.vercel.app
