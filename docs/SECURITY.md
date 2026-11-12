# Sicherheit und Datenschutz

- Keine öffentliche Registrierung, maximal vier aktive Konten. Fufi ist der einzige initiale Admin.
- Argon2id: 19 MiB, zwei Iterationen, Parallelität eins; zufällige Startpasswörter mit 192 Bit Entropie. Bootstrap überschreibt nie ein bestehendes Passwort.
- Passwort-Hashes und Sitzungstokens erscheinen nicht in Profilen, Exporten oder Audit-Logs. Nur Admins erhalten neu erzeugte Freundes-Startpasswörter einmalig in ihrer authentifizierten Verwaltungsansicht.
- Web-Cookies: HttpOnly, SameSite Strict, Secure bei Produktion. JSON-Mutationen werden per Origin geprüft. HTTPS für jede externe Desktop-Verbindung; HTTP ausschließlich an Loopback für Entwicklung.
- Sessions sind widerrufbar; deaktivierte Nutzer verlieren sofort Zugang. Pflicht-Passwortwechsel wird von der API erzwungen. Refresh-Token-Wiederverwendung widerruft die Familie.
- SQL-Parameter statt SQL-Interpolation mit Nutzereingaben. Validierung mit Zod. Ratenlimits liegen zentral in PostgreSQL. Keine fremden Match-IDs bei Client-Schreibvorgängen. Plausibilitätsgrenzen: maximal 200 Events pro Match, mindestens 500 ms Abstand, ausreichende Confidence und Ereigniszeiten innerhalb des Matches.
- API-Fehler enthalten keine internen Datenbankmeldungen oder Secrets. Produktionsdaten werden nicht in Browser-Speichern gecacht. API-Responses sind `no-store`, private Seiten nicht für Suchmaschinen bestimmt.
- Referenzbilder befinden sich ausschließlich im privaten Quellcode. Die Website veröffentlicht keine Fortnite-Bildausschnitte. Der Client wertet geladene Replay-Bilder oder nach bewusster Aktivierung das sichtbare native Fortnite-Fenster im Vordergrund lokal aus. Keine Spielbilder oder OCR-Texte werden hochgeladen.
- Ein privater Client ist manipulierbar. Die Metadaten sind kein fälschungssicherer Nachweis einer echten Öffnung. Serverprüfungen reduzieren einfache Fehler, garantieren aber keine Anti-Tamper-Sicherheit.

## Vor einem späteren Live-Einsatz

Aktuelle Epic-/Fortnite-Regeln und Windows-Capture-Beschränkungen prüfen. Nur dokumentierte Aufnahme-APIs mit bewusster Freigabe und ausgewähltem sichtbaren Fenster erwägen. Keine Anti-Cheat-Umgehung, keine Spiel-Hooks, keine Eingaben an das Spiel. Ohne Prüfung und echte Testclips keine Zusage von Zulässigkeit, Sperrensicherheit oder zuverlässiger Erkennung.

## Betrieb

Neon- und Vercel-Zugang beschränken, MFA verwenden, Backups prüfen. Produktions-DATABASE_URL niemals an Preview-Builds fremder PRs weiterreichen. GitHub CI benötigt für Tests keine echten Zugangsdaten. Die lokale `.secrets`-Datei nach sicherer Übernahme und Passwortwechsel löschen. Anwendungs- und Vercel-Zugang sind zwei getrennte Schutzschichten; der aktuelle Deployment-Schutz wird nicht stillschweigend entfernt.
