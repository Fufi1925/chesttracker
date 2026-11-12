# Installation und Betrieb

## Vercel + Neon

1. Privates Repository `Fufi1925/chesttracker`. Vercel-Projekt `chesttracker`, Team `fufi1925s-projects`, Framework Next.js, Root Directory `apps/web`, Node.js 24, Functions-Region Frankfurt.
2. Neon im Vercel Marketplace verbinden: Production und Development, automatische Datenbank-Branches für Deployments ausgeschaltet. `DATABASE_URL` wird von der Integration verwaltet. Preview erhält bewusst keinen Zugriff auf die Produktionsdatenbank.
3. Vor dem ersten Betrieb lokal `scripts/setup.ts` gegen die verbundene Datenbank ausführen. Der Bootstrap ist ausschließlich ein lokales Script, kein öffentlicher HTTP-Endpunkt. Er legt `Fufi` als Admin mit Argon2id-Hash an und schreibt das zufällige Startpasswort nur in `.secrets/fufi-startpasswort.txt`, Modus 0600, Verzeichnis 0700. Das Passwort nie in Hosting-Logs oder CI eingeben.
4. `npm run build`, `npm test` und Browser-Tests müssen erfolgreich sein. Git-Commit auf Vercel als Production deployen. `/api/health` prüft Datenbank und Migration. Keine Migration oder Passwortausgabe im Build.
5. Die Website hat eine eigene Anmeldung. Nach ausdrücklicher Nutzerfreigabe gilt Vercel Deployment Protection nur für Vorschau-Deployments. Die Produktionswebsite und die Windows-API benötigen keine zusätzliche Vercel-Anmeldung; der ChestTracker-Login bleibt verpflichtend.
6. Beim ersten Login das Startpasswort ändern. Danach bis zu drei Konten unter Admin anlegen und deren einmalig sichtbare Startpasswörter sicher weitergeben.

Bei bestehendem Projekt ohne Git-Verknüpfung erfolgen weitere Deployments explizit über Vercel. Für automatische Deployments in Vercel unter Settings → Git das private Repository verbinden. Der CI-Workflow prüft jeden Push und stellt das Windows-Paket als privates Artifact bereit.

## Lokaler Server

`node scripts/local-env.mjs` erzeugt ein zufälliges lokales DB-Passwort in `infra/.env`. `docker compose --env-file infra/.env -f infra/compose.yml up --build` startet Datenbank und die gemeinsame Website/API. PostgreSQL wird nicht auf einem Hostport veröffentlicht. Die Webanwendung bindet lokal Port 3000. Bootstrap-Secrets werden in `.secrets` auf dem Host gespeichert. Die Healthchecks überwachen beide Dienste. `docker compose down` erhält das Datenbankvolume. **Nicht `down -v` verwenden**, wenn die Statistiken erhalten bleiben sollen.

Ohne Docker eine PostgreSQL-Datenbank bereitstellen und `DATABASE_URL` setzen. `npm run setup` migriert und initialisiert. Die Next.js-Entwicklungsumgebung lädt auch `apps/web/.env.local`. Die eigenständigen Scripts laden `.env.local` nur bei Verwendung von `node --env-file=...`.

## Daten sichern

Der Admin-JSON-Export enthält Kontoprofile ohne Passwort-Hashes, Matches, Events und Audit-Logs. Er ist ein lesbarer Datenexport, kein automatischer vollständiger Restore. Für vollständige Backups Neon-Snapshots/Branches oder `pg_dump` verwenden und die Wiederherstellung in einer separaten Datenbank prüfen. Backups privat und verschlüsselt verwahren. Aufbewahrung und Löschung nach den Bedürfnissen der vier Nutzer festlegen.

## Windows-Build und Release

`scripts/build-windows.ps1` erstellt ein selbstenthaltendes x64-Paket mit EXE, Replay-Fixtures und Anleitung. SHA-256 steht in `artifacts/SHA256SUMS.txt`. GitHub Actions führt zusätzlich Windows-Pixeltests aus. Releases werden im privaten Repository veröffentlicht; ein Release macht ein privates Repository nicht öffentlich. Ein Release wird erst nach erfolgreichen Prüfungen erstellt. Versions-Tag und Release müssen auf den geprüften Commit zeigen.
