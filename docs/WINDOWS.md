# Windows-Client

1. `ChestTracker-win-x64.zip` aus dem privaten Release herunterladen und vollständig entpacken.
2. `ChestTracker.exe` unter Windows 10/11 x64 starten. Selbstenthaltend; keine separate .NET-Runtime nötig. Unsigned, daher kann Windows eine Herausgeberwarnung zeigen.
3. API-Basisadresse der Website eintragen, ohne `/api` oder andere Pfade. Verbindung prüfen. Wenn Vercel Deployment Protection aktiv ist, kann sie Desktop-Zugriff zusätzlich blockieren; sie muss vor Nutzung des Desktop-Logins ausdrücklich passend konfiguriert werden.
4. Mit dem zentralen ChestTracker-Benutzernamen anmelden. Beim ersten Login das Startpasswort ändern. Die App speichert keine Klartextpasswörter.
5. `replays/synthetic-two-openings.json` als Beobachtungs-Replay laden. Erwartung: Lobby → Match → genau zwei eigene Öffnungen; eine Teamkamerad-Meldung zählt nicht → Lobby ohne alte Truhenzahl.
6. Replay ist standardmäßig lokal. Die Option „Replay ausdrücklich an den Server senden“ schreibt nach einem Bestätigungsdialog **echte Testdatensätze** in die zentrale Statistik. Nur für bewusste Testläufe verwenden; anschließend kann der Admin sie nachvollziehbar löschen.

Das Overlay ist 220 × 36 logische Pixel, oben rechts innerhalb des sichtbaren Arbeitsbereichs, klickdurchlässig und ohne Fokuswechsel. O blendet nur die Anzeige aus. F8/F9 sind Alternativen bei Konflikten. Tray: Overlay, Pause, Einstellungen, Website und Beenden. Windows-Skalierung beeinflusst physische Pixel. Exklusives Vollbild kann die Anzeige verdecken; für spätere Live-Anwendung wäre randloser Fenstermodus nötig. Mehrmonitor-/DPI-Wechsel und Hotkey-Verhalten müssen auf dem Ziel-PC geprüft werden.

## Lokales Bild-Replay

Ein JSON-Manifest beschreibt Bilder und fünf präzise HUD-Regionen: Brust-/Öffnungsbenachrichtigung, unabhängiger eigener-Spieler-Hinweis, Match-, Lobby- und Ladeindikator. Templatebilder müssen aus rechtmäßig bereitgestellten eigenen Aufnahmen stammen. Alle Pfade liegen relativ innerhalb des Manifestordners. Region-Werte sind normalisierte Bruchteile des Vollbildes. Die Templates müssen die jeweilige Region eng abdecken; eine beliebige sichtbare Truhe ist kein Öffnungsnachweis.

```json
{
  "chest": {"template":"templates/chest.png","region":{"x":0.4,"y":0.5,"width":0.2,"height":0.05},"threshold":0.94},
  "ownPlayer": {"template":"templates/own.png","region":{"x":0.4,"y":0.55,"width":0.2,"height":0.05},"threshold":0.94},
  "match": {"template":"templates/match.png","region":{"x":0.8,"y":0.1,"width":0.1,"height":0.05},"threshold":0.94},
  "lobby": {"template":"templates/lobby.png","region":{"x":0.2,"y":0.1,"width":0.1,"height":0.05},"threshold":0.94},
  "loading": {"template":"templates/loading.png","region":{"x":0.5,"y":0.1,"width":0.1,"height":0.05},"threshold":0.94},
  "frames":[{"image":"frames/0001.png","seconds":0},{"image":"frames/0002.png","seconds":0.083}]
}
```

Das Beispiel beschreibt nur das Dateiformat; echte Fortnite-Templates sind nicht beigefügt. `replays/synthetic-images/manifest.json` ist ein ausführbarer künstlicher Pixeltest ohne Spielgrafiken. Der Detector ist bei echten Szenen **unvalidiert**. Unterschiedliche UI-Skalierung, Sprache und HUD-Version benötigen neue Kalibrierung.

## Daten und Wiederverbindung

Konfiguration, Sitzungen, Wiederanlauf-Metadaten und Warteschlange liegen DPAPI-verschlüsselt unter `%LOCALAPPDATA%\ChestTracker`. Wiederholte Events behalten dieselbe ID. Der Client entfernt sie erst nach bestätigter Serverantwort. Ein nicht wiederholbarer Validierungsfehler stoppt die Warteschlange und wird angezeigt; er wird nicht stillschweigend verworfen. Für die Bereinigung eines beschädigten oder nicht mehr gültigen Matches den Admin hinzuziehen.
