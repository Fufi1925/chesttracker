ChestTracker v1.0.1 – Konten löschen und Windows-Anmeldung.

Das Repository und der Download sind auf ausdrücklichen Wunsch des Eigentümers derzeit öffentlich. Die Website-Statistiken bleiben hinter dem ChestTracker-Login geschützt. Dieses Release enthält keine Zugangsdaten.

- Freundeskonten unter Admin mit Bestätigung dauerhaft löschen. Zugehörige Sitzungen, Präsenz, Matches und Events werden entfernt; das Audit protokolliert die Löschung ohne Passwortdaten. Das Administratorkonto ist geschützt.
- Windows: O wird nicht mehr als systemweit verschluckte Taste registriert. Der Buchstabe bleibt beim Tippen von Benutzernamen und Passwörtern erhalten. Im aktiven Clientfenster schaltet Tippen das Overlay nicht um; außerhalb funktioniert O weiterhin.
- Klarere Meldung, wenn eine falsche API-Adresse oder eine vorgeschaltete HTML-Anmeldeseite statt JSON antwortet.
- Getestet: 22 API-Tests, 16 Detektor-/Tastentests, 5 Windows-Pixel-/Login-Tests und 3 Browser-Tests. Das Release verwendet das Windows-Artefakt des erfolgreich geprüften CI-Commits.

- Website und zentrale API auf Vercel, PostgreSQL auf Neon.
- Fufi und bis zu drei Freunde; Pflicht-Passwortwechsel, Profile, Match-Verlauf, Rangliste, Live-Status und Admin-Audit.
- Selbstenthaltender Windows-x64-Client mit kleinem Overlay, O/F8/F9-Hotkey, DPAPI-Sitzungen und dauerhafter Ereigniswarteschlange.
- Offline-Beobachtungs- und Bild-Replay mit synthetischen Beispielen. Keine Live-Capture-Funktion.

Der Release-Workflow prüft vor der Veröffentlichung den erfolgreichen CI-Lauf und verwendet exakt dessen Commit und Windows-Artefakt.

Download: `ChestTracker-win-x64.zip` vollständig entpacken und `ChestTracker.exe` starten. Keine separate .NET-Installation erforderlich. `SHA256SUMS.txt` prüft das Paket. Dieses Release enthält keine Zugangsdaten.

Grenzen: Live-Erkennung, Fortnite-Regelverträglichkeit und Genauigkeit sind unvalidiert; Live-Aufnahme ist deaktiviert. GUI/Overlay/Hotkeys auf dem Ziel-PC manuell prüfen. Die EXE ist nicht codesigniert.

Aktualisierung zur Einrichtung: Nach ausdrücklicher Nutzerfreigabe ist der zusätzliche Vercel-Anmeldeschutz für Produktion deaktiviert. Website und EXE verwenden direkt den verpflichtenden ChestTracker-Login. Die ältere Schutzwarnung in der mitgelieferten README ist damit erledigt; die EXE muss dafür nicht neu heruntergeladen werden. Eine Schritt-für-Schritt-Anleitung steht unter `docs/ERSTE_SCHRITTE.md` im Repository.

Website: https://chesttracker-fufi1925s-projects.vercel.app

Anleitung: `docs/WINDOWS.md` und `docs/INSTALLATION.md` im Repository.
