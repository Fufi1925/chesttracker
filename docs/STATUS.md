# Lieferstatus

## Implementiert

Website/API, PostgreSQL-Migration und Bootstrap, private Konten/Statistiken, Rollenprüfung, Desktop-Sitzungen, Präsenz-Polling, Profile/Rangliste/Diagramme, Admin-Änderungen mit Audit und JSON-Export. WPF-Client mit Overlay, globalem Hotkey/Tray, DPAPI, dauerhafter Warteschlange, Offline-Replay und lokalem Bild-Template-Adapter. Linux-Crossbuild erzeugt eine echte selbstenthaltende Windows-EXE. CI führt zusätzlich Windows-Build und Windows-Pixeltests aus.

## Unvalidiert / bewusst gesperrt

Echte Fortnite-Erkennung, geeignete eigene Öffnungshinweise, Genauigkeit, Ressourcenbedarf am Ziel-PC und Regel-/Anti-Cheat-Kompatibilität. Keine Live-Capture-Funktion. Keine geografischen Routen. Windows-GUI, Hotkeys und Overlay im Spiel erfordern einen manuellen Windows-Test. Die EXE ist nicht signiert.

## Infrastruktur

Privates GitHub-Repository und Vercel-Projekt verwenden die vom Benutzer verbundenen Konten. PostgreSQL wird durch die Neon-Integration bereitgestellt. Railway-Projekterstellung war wegen erschöpfter Ressourcen nicht möglich; Railway wird für dieses Projekt nicht verwendet.

Nach ausdrücklicher Zustimmung des Nutzers ist Vercel Deployment Protection auf Vorschau-Deployments beschränkt. Die Produktionswebsite und die Desktop-API sind direkt erreichbar. Der ChestTracker-Login und sämtliche serverseitigen Rechteprüfungen bleiben aktiv. Das frühere Startpasswort von Fufi wurde bereits ersetzt; der Nutzer verwendet sein selbst gesetztes Passwort.
