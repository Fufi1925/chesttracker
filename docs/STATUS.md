# Lieferstatus

## Implementiert

Website/API, PostgreSQL-Migration und Bootstrap, private Konten/Statistiken, Rollenprüfung, Desktop-Sitzungen, Präsenz-Polling mit separatem Fortnite-Fensterstatus, Mitspieler unter eigener Overlay-Zeile, Profile/Rangliste/Diagramme, Admin-Änderungen mit Audit und JSON-Export. WPF-Client mit Overlay, globalem Hotkey/Tray, DPAPI, dauerhafter Warteschlange, Offline-Replay und lokalem Bild-Template-Adapter. Linux-Crossbuild erzeugt eine echte selbstenthaltende Windows-EXE. CI führt zusätzlich Windows-Build und Windows-Pixeltests aus.

## Unvalidiert / bewusst gesperrt

V1.3.0 erkennt fünf markierte Truhenaktionen, Ergebnis und Lobby im ersten Kalibrierungsclip. Eine zweite Aufnahme erweitert die Kalibrierung um goldene Meldungen und lootgebende Holzkisten; dort verbleiben verpasste Fälle. Das Ziel von 95 % ist nicht belegt. Allgemeine Genauigkeit, andere HUDs/Situationen, Ressourcenbedarf am Ziel-PC und Regel-/Anti-Cheat-Kompatibilität bleiben unvalidiert. Siehe [Referenzbericht](DETECTION_REFERENCE.md). Keine geografischen Routen. Vollbild-/Kompakt-GUI und OCR-Laden werden in Windows-CI geprüft; tatsächliche Aufnahme, Hotkeys und Overlay im Spiel erfordern weiterhin einen manuellen Test am Ziel-PC. Die EXE ist nicht signiert.

## Infrastruktur

GitHub-Repository und Vercel-Projekt verwenden die vom Benutzer verbundenen Konten. Nach ausdrücklicher neuer Freigabe bleibt das Repository derzeit öffentlich, damit Freunde das Windows-Paket herunterladen können. PostgreSQL wird durch die Neon-Integration bereitgestellt. Railway-Projekterstellung war wegen erschöpfter Ressourcen nicht möglich; Railway wird für dieses Projekt nicht verwendet.

Nach ausdrücklicher Zustimmung des Nutzers ist Vercel Deployment Protection auf Vorschau-Deployments beschränkt. Die Produktionswebsite und die Desktop-API sind direkt erreichbar. Der ChestTracker-Login und sämtliche serverseitigen Rechteprüfungen bleiben aktiv. Das frühere Startpasswort von Fufi wurde bereits ersetzt; der Nutzer verwendet sein selbst gesetztes Passwort.
