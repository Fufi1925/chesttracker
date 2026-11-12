# API v1

Basis: dieselbe Origin wie die Website, Präfix `/api`. JSON, Zeitpunkte ISO 8601/UTC, IDs UUID. Alle Statistikrouten sind authentifiziert. Fehler: `{ "error": "Deutscher Fehlertext" }`, optional validierte Feldfehler. `401` Sitzung fehlt/abgelaufen, `403` Origin/Rolle, `428` Pflicht-Passwortwechsel, `429` Ratenlimit, `400` Validierung, `409` Konflikt.

| Methode / Pfad | Inhalt |
| --- | --- |
| GET health | Datenbank-/Migrationsstatus, keine Statistiken |
| POST auth/login | `{username,password,client:"web" oder "desktop"}`; Web-Cookie oder `{user,accessToken,refreshToken,expiresIn}` |
| GET auth/me | `{user:{id,username,displayName,role,mustChangePassword,...}}` |
| POST auth/password | `{currentPassword,newPassword}`; Pflichtwechsel serverseitig |
| POST auth/refresh | `{refreshToken}`; neues Access-/Refresh-Paar, altes Refresh widerrufen |
| POST auth/logout | Token-Familie widerrufen |
| GET dashboard | `{user,users,matches,serverTime}`; Status/Summen nur aus DB |
| GET matches?userId=UUID | letzte 250 Matches, optional Spielerfilter |
| GET matches/:id | `{match,events}` mit echten Eventzeitpunkten |
| POST client/heartbeat | `{state,matchId?}`; Desktop-Access-Token erforderlich |
| POST client/matches | `{id,startedAt}`; idempotenter Start eines eigenen Matches |
| POST client/events | `{clientEventId,matchId,occurredAt,confidence,detectorVersion}`; idempotent nach Nutzer/Event-ID |
| POST client/matches/end | `{id,endedAt,status:"COMPLETED" oder "INCOMPLETE"}` |
| GET/POST admin/users | Liste / `{username,displayName}`; initialPassword einmalig nur für Admin |
| PATCH admin/users/:id | `{displayName?,enabled?,resetPassword?}` |
| GET admin/audit | letzte 200 Änderungen mit Vorher/Nachher, ohne Passwort-Hashes |
| POST admin/matches | `{id,userId,startedAt,endedAt,status}` |
| PATCH/DELETE admin/matches/:id | Zeiten/Status ändern oder Match samt Events entfernen |
| POST admin/events | Event anlegen; gleiche Form wie Client-Event |
| PATCH/DELETE admin/events/:id | `{occurredAt}` ändern oder Event löschen |
| GET admin/export | Profile, Matches, Events, Audit als JSON; keine Passwörter/Sitzungen |

Browsermutationen senden `Origin` exakt wie die Website und `Content-Type: application/json`. Desktop-Anfragen senden `Authorization: Bearer <accessToken>`; beim Login/Refresh dient ein nicht authentifizierender Bearer-Marker dazu, reine Desktop-Anfragen ohne Browser-Cookie/Origin zu unterscheiden. Dieser Marker gewährt keine Rechte; Login braucht weiterhin Benutzername/Passwort, Refresh einen gültigen Refresh-Token.

Zustände: `OFFLINE / UNKNOWN / LOBBY / LOADING / IN_MATCH / MATCH_END_PENDING`. IN_MATCH benötigt ein aktives eigenes Match. Heartbeat alle fünf Sekunden; offline nach 20 Sekunden.
