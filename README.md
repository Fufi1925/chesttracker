# ChestTracker

Private Truhenstatistik für Fufi und bis zu fünf Freunde. Deutsche, responsive Website, gemeinsame API/PostgreSQL-Datenbank und selbstenthaltender Windows-Client mit kompaktem Overlay.

## Was funktioniert

- Geschützte Übersicht, Spielerstatus, Matches, zeitlicher Eventverlauf, Rangliste, Profile und Admin-Verwaltung. Keine öffentliche Registrierung und keine Demo-Zahlen in der Produktionsdatenbank.
- Argon2id, serverseitige Rollenprüfung, Pflicht-Passwortwechsel, Web-Cookies mit HttpOnly/SameSite/HTTPS, Desktop-Access-/Refresh-Sitzungen mit Widerruf und Rotation, persistente Ratenlimits.
- Maximal sechs aktive Konten. Änderungen an Konten, Matches und Ereignissen werden protokolliert. Summen werden aus gespeicherten Ereignissen abgeleitet.
- Windows-Overlay oben rechts, globaler Hotkey O/F8/F9, Tray, DPAPI-geschützte Sitzungen und dauerhafte Ereigniswarteschlange, idempotente Synchronisierung.
- Konservativer Replay-Detektor mit Hysterese, lokalem Template-Abgleich und synthetischen Testfällen.

**Windows v1.3.0 zeigt andere angemeldete Spieler mit geöffnetem Fortnite unter deiner eigenen Overlay-Zeile.** Die lokale Erkennung wurde mit einer zweiten Aufnahme für goldene/violette EP-Meldungen und lootgebende Holzkisten erweitert. 95 % allgemeine Genauigkeit sind noch nicht nachgewiesen. Die Bildanalyse muss im Accountfenster aktiviert werden. [Referenz, Messungen und Grenzen](docs/DETECTION_REFERENCE.md).

## Windows herunterladen

Im [Release](https://github.com/Fufi1925/chesttracker/releases) `ChestTracker-win-x64.zip` herunterladen, entpacken und `ChestTracker.exe` starten. Windows 10/11 x64; keine separate .NET-Installation erforderlich. Die EXE ist nicht codesigniert.

Die API-Basisadresse entspricht der bereitgestellten Website. Mit dem vom Admin angelegten ChestTracker-Konto anmelden. **Keine Epic-Zugangsdaten verwenden.** Einzelheiten: [Windows-Anleitung](docs/WINDOWS.md).

## Website und API lokal starten

Voraussetzungen: Node.js 24, npm und eine PostgreSQL-Datenbank (lokal Docker Compose). Für Windows-Builds zusätzlich .NET SDK 10.

```bash
npm ci
node scripts/local-env.mjs
docker compose --env-file infra/.env -f infra/compose.yml up --build
```

Website/API: `http://localhost:3000`. Compose führt Migrationen und den idempotenten Bootstrap aus. Das erste Passwort liegt im lokalen Verzeichnis `.secrets/fufi-startpasswort.txt`. Bei wiederholtem Start bleibt es unverändert. Das Datenbankvolume bleibt erhalten.

Ohne Docker mit vorhandener Datenbank:

```bash
# .env.local mit DATABASE_URL anlegen (nicht committen).
npm ci
node --env-file=.env.local --import tsx scripts/setup.ts
# DATABASE_URL in der Shell setzen oder apps/web/.env.local anlegen.
npm run dev
```

## Tests und Builds

```bash
npm test
npm run build
npx playwright install chromium
npm run test:browser
dotnet test apps/windows-client/ChestTracker.Tests/ChestTracker.Tests.csproj
# Auf Windows zusätzlich:
dotnet test apps/windows-client/Windows.Tests/ChestTracker.Windows.Tests.csproj
powershell -ExecutionPolicy Bypass -File scripts/build-windows.ps1
```

Buildausgabe: `artifacts/windows/ChestTracker.exe`; Paket: `artifacts/ChestTracker-win-x64.zip`. CI baut auf Windows und prüft API/Web sowie Browserabläufe. Die Pixeltests benötigen Windows. Das vom Eigentümer öffentlich freigegebene Release enthält das Paket und seine SHA-256-Prüfsumme.

## Struktur

| Pfad | Inhalt |
| --- | --- |
| `apps/web` | Next.js-Website und zentrale Route-Handler-API |
| `apps/web/lib/schema.ts` | Versionierte, idempotente PostgreSQL-Migration |
| `apps/windows-client` | .NET-10-WPF-Client, Bild-Replay, Overlay und Sync |
| `apps/windows-client/Core` | Plattformunabhängiger Detektor-Zustandsautomat |
| `tests` | API-, Browser- und synthetische Replay-Tests |
| `shared/contracts` | Dokumentierter API-Vertrag |
| `infra` | Lokale persistente PostgreSQL-/Web-Umgebung |
| `assets` | Ausschließlich interne Designreferenzen; keine Produktgrafiken |

[Installation und Deployment](docs/INSTALLATION.md) · [Architektur](docs/ARCHITEKTUR.md) · [Sicherheit](docs/SECURITY.md) · [Testplan](docs/TESTPLAN.md) · [Lieferstatus](docs/STATUS.md)
