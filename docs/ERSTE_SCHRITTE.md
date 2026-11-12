# Vom ZIP zum angemeldeten Client

1. Im Windows-Downloadordner mit der rechten Maustaste auf `ChestTracker-win-x64.zip` klicken und **Alle extrahieren** wählen. Danach den entpackten Ordner öffnen. Die EXE aus diesem Ordner starten, nicht direkt aus der ZIP-Vorschau.
2. Die [Website](https://chesttracker-fufi1925s-projects.vercel.app) öffnen. Benutzername **Fufi**, Passwort: das selbst gesetzte ChestTracker-Passwort. Ein bereits ersetztes Startpasswort funktioniert nicht mehr. Für Freunde erstellt Fufi eigene Konten unter **Admin → Spieler hinzufügen**.
3. Im entpackten Ordner `ChestTracker.exe` doppelt anklicken. Bei **API-Adresse** muss `https://chesttracker-fufi1925s-projects.vercel.app` stehen. Diese Adresse ist in v1.0.0 voreingestellt.
4. **Verbindung prüfen** anklicken. Danach Benutzername **Fufi** und dasselbe Passwort wie auf der Website eintragen und **Anmelden** anklicken. Website und EXE verwenden dasselbe Konto.
5. Zum lokalen Funktionstest **Beobachtungs-Replay** anklicken und im entpackten Ordner `replays/synthetic-two-openings.json` öffnen. Das Overlay zeigt Lobby, dann zwei Öffnungen im Match und anschließend wieder Lobby. **O** blendet es aus oder ein. Dafür ist kein laufendes Fortnite nötig.
6. Nur wenn dieser Test bewusst auch auf der Website erscheinen soll: vor dem Replay **Replay ausdrücklich an den Server senden (Testdaten)** aktivieren und die Rückfrage bestätigen. Dadurch entstehen echte Testeinträge in der Statistik. Fufi kann sie später unter **Matches → Match öffnen → Match löschen** entfernen; die Löschung wird protokolliert.

Der zusätzliche Vercel-Anmeldeschutz wurde nach Freigabe des Nutzers für Produktion deaktiviert. Der ChestTracker-Login bleibt verpflichtend; Vorschau-Deployments behalten Vercel-Schutz.

**Die EXE zählt derzeit keine echten Fortnite-Truhen automatisch.** Live-Aufnahme und echte Spielerkennung sind nicht implementiert/freigegeben. Funktionsfähig sind die Anmeldung, das private Dashboard und der Offline-Replay-Test. Für eine reale Erkennung fehlen geprüfte Spielaufnahmen und die Validierung der Bildhinweise. Ein bloßes Aktivieren einer Einstellung kann diese fehlende Funktion nicht ersetzen.
