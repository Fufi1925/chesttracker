import { randomUUID } from "node:crypto";
import { z } from "zod";
import { db, transaction, type Database, type Row } from "./db";
import {
  authenticate,
  checkPassword,
  cookie,
  fail,
  HttpError,
  issueSession,
  limit,
  passwordHash,
  publicUser,
  secret,
  tokenHash,
} from "./auth";

const uuid = z.string().uuid();
const date = z.iso.datetime({ offset: true }).transform((v) => new Date(v));
const username = z
  .string()
  .min(2)
  .max(32)
  .regex(/^[A-Za-z0-9_-]+$/);
const password = z.string().min(12).max(128);
const matchSchema = z.object({
  id: uuid,
  userId: uuid.optional(),
  startedAt: date,
  endedAt: date.nullable().optional(),
  status: z.enum(["ACTIVE", "COMPLETED", "INCOMPLETE"]),
});
const eventSchema = z.object({
  clientEventId: uuid,
  matchId: uuid,
  occurredAt: date,
  confidence: z.number().min(0.8).max(1),
  detectorVersion: z.string().min(1).max(80),
});
const json = (
  data: unknown,
  status = 200,
  headers: Record<string, string> = {},
) =>
  Response.json(data, {
    status,
    headers: { "Cache-Control": "no-store", ...headers },
  });
async function body(req: Request) {
  if (Number(req.headers.get("content-length") ?? 0) > 65536)
    fail(413, "Anfrage zu groß.");
  const raw = await req.text();
  if (raw.length > 65536) fail(413, "Anfrage zu groß.");
  try {
    return JSON.parse(raw);
  } catch {
    return fail(400, "Ungültiges JSON.");
  }
}
async function audit(
  tx: Database,
  actor: Row,
  action: string,
  type: string,
  id: string,
  before: unknown,
  after: unknown,
) {
  await tx.query(
    "INSERT INTO audit_log(id,admin_user_id,action,target_type,target_id,before_json,after_json) VALUES($1,$2,$3,$4,$5,$6,$7)",
    [
      randomUUID(),
      actor.id,
      action,
      type,
      id,
      JSON.stringify(before),
      JSON.stringify(after),
    ],
  );
}
async function matchRows(userId?: string) {
  return (
    await db().query(
      `SELECT m.*,u.display_name,COUNT(e.id)::int AS chest_count FROM matches m JOIN users u ON u.id=m.user_id LEFT JOIN chest_events e ON e.match_id=m.id ${userId ? "WHERE m.user_id=$1" : ""} GROUP BY m.id,u.display_name ORDER BY m.started_at DESC LIMIT 250`,
      userId ? [userId] : [],
    )
  ).rows;
}
export async function handle(req: Request): Promise<Response> {
  try {
    const url = new URL(req.url),
      path = url.pathname.replace(/^\/api\/?/, "").replace(/\/$/, ""),
      method = req.method;
    if (method !== "GET" && method !== "HEAD") {
      const origin = req.headers.get("origin");
      // Browser mutations always require an exact same-origin header. Desktop requests have no cookies.
      if ((req.headers.has("cookie") || origin) && origin !== url.origin)
        fail(403, "Ungültiger Anfrageursprung.");
      if (!req.headers.has("authorization") && !origin)
        fail(403, "Anfrageursprung erforderlich.");
      if (!req.headers.get("content-type")?.startsWith("application/json"))
        fail(415, "JSON erforderlich.");
    }
    if (path === "health") {
      await db().query("SELECT version FROM schema_migrations LIMIT 1");
      return json({ status: "ok", version: "1.0.1" });
    }
    if (path === "auth/login" && method === "POST") {
      const input = z
        .object({
          username: z.string().min(1).max(32),
          password: z.string().min(1).max(128),
          client: z.enum(["web", "desktop"]).default("web"),
        })
        .parse(await body(req));
      const ip =
        req.headers.get("x-vercel-forwarded-for") ??
        req.headers.get("x-forwarded-for") ??
        "local";
      await limit("login-ip:" + ip, 30, 900);
      await limit("login-user:" + input.username.toLowerCase(), 10, 900);
      const {
        rows: [user],
      } = await db().query(
        "SELECT * FROM users WHERE lower(username)=lower($1) AND enabled=true",
        [input.username],
      );
      if (!user || !(await checkPassword(user.password_hash, input.password)))
        return fail(401, "Benutzername oder Passwort ist falsch.");
      if (input.client === "desktop") {
        const access = await issueSession(user.id, "access"),
          refresh = await issueSession(user.id, "refresh", access.family);
        return json({
          user: publicUser(user),
          accessToken: access.token,
          refreshToken: refresh.token,
          expiresIn: access.seconds,
        });
      }
      const session = await issueSession(user.id, "web");
      return json({ user: publicUser(user) }, 200, {
        "Set-Cookie": cookie(session.token, session.seconds),
      });
    }
    if (path === "auth/refresh" && method === "POST") {
      const { refreshToken } = z
        .object({ refreshToken: z.string().min(20).max(128) })
        .parse(await body(req));
      const pair = await transaction(async (tx) => {
        const {
          rows: [s],
        } = await tx.query(
          `SELECT s.*,u.enabled FROM sessions s JOIN users u ON u.id=s.user_id WHERE token_hash=$1 AND kind='refresh' FOR UPDATE`,
          [tokenHash(refreshToken)],
        );
        if (!s || !s.enabled || new Date(s.expires_at).getTime() < Date.now())
          return null;
        if (s.revoked_at) {
          await tx.query(
            "UPDATE sessions SET revoked_at=now() WHERE family_id=$1",
            [s.family_id],
          );
          return null;
        }
        await tx.query("UPDATE sessions SET revoked_at=now() WHERE id=$1", [
          s.id,
        ]);
        const a = await issueSession(s.user_id, "access", s.family_id, tx),
          r = await issueSession(s.user_id, "refresh", s.family_id, tx);
        return {
          accessToken: a.token,
          refreshToken: r.token,
          expiresIn: a.seconds,
        };
      });
      if (!pair) fail(401, "Erneute Anmeldung erforderlich.");
      return json(pair);
    }
    const user = await authenticate(
      req,
      ["auth/me", "auth/password", "auth/logout"].includes(path),
    );
    if (path === "auth/me" && method === "GET")
      return json({ user: publicUser(user) });
    if (path === "auth/logout" && method === "POST") {
      await db().query(
        "UPDATE sessions SET revoked_at=now() WHERE family_id=$1",
        [user.family_id],
      );
      return json({ ok: true }, 200, { "Set-Cookie": cookie("", 0) });
    }
    if (path === "auth/password" && method === "POST") {
      const input = z
        .object({ currentPassword: z.string().max(128), newPassword: password })
        .parse(await body(req));
      await limit("password:" + user.id, 10, 900);
      if (!(await checkPassword(user.password_hash, input.currentPassword)))
        fail(401, "Das aktuelle Passwort ist falsch.");
      if (input.newPassword === input.currentPassword)
        fail(400, "Bitte ein anderes Passwort wählen.");
      const hash = await passwordHash(input.newPassword);
      await transaction(async (tx) => {
        await tx.query(
          "UPDATE users SET password_hash=$1,must_change_password=false WHERE id=$2",
          [hash, user.id],
        );
        await tx.query(
          "UPDATE sessions SET revoked_at=now() WHERE user_id=$1 AND family_id<>$2",
          [user.id, user.family_id],
        );
      });
      return json({ ok: true });
    }
    if (path === "dashboard" && method === "GET") {
      const { rows: users } = await db()
        .query(`SELECT u.id,u.username,u.display_name,u.role,u.created_at,
      CASE WHEN p.last_heartbeat_at>now()-interval '20 seconds' THEN p.state ELSE 'OFFLINE' END AS state,
      p.match_id,(SELECT COUNT(*)::int FROM chest_events e WHERE e.match_id=p.match_id) AS current_chests,
      (SELECT COUNT(*)::int FROM chest_events e WHERE e.user_id=u.id) AS total_chests,
      (SELECT COUNT(*)::int FROM matches m WHERE m.user_id=u.id AND m.status='COMPLETED') AS completed_matches,
      (SELECT COUNT(*)::int FROM chest_events e JOIN matches m ON m.id=e.match_id WHERE e.user_id=u.id AND m.status='COMPLETED') AS completed_chests,
      (SELECT MAX(n)::int FROM (SELECT COUNT(*) n FROM chest_events e JOIN matches m ON m.id=e.match_id WHERE m.user_id=u.id AND m.status='COMPLETED' GROUP BY m.id) counts) AS best_match
      FROM users u LEFT JOIN presence p ON p.user_id=u.id WHERE u.enabled=true ORDER BY u.created_at`);
      return json({
        user: publicUser(user),
        users: users.map((u) => ({
          ...u,
          average: u.completed_matches
            ? Number((u.completed_chests / u.completed_matches).toFixed(1))
            : 0,
          best_match: u.best_match ?? 0,
        })),
        matches: await matchRows(),
        serverTime: new Date().toISOString(),
      });
    }
    if (path === "matches" && method === "GET") {
      const id = url.searchParams.get("userId");
      if (id) uuid.parse(id);
      return json({ matches: await matchRows(id ?? undefined) });
    }
    const detail = path.match(/^matches\/([\w-]+)$/);
    if (detail && method === "GET") {
      uuid.parse(detail[1]);
      const {
        rows: [match],
      } = await db().query(
        "SELECT m.*,u.display_name FROM matches m JOIN users u ON u.id=m.user_id WHERE m.id=$1",
        [detail[1]],
      );
      if (!match) fail(404, "Match nicht gefunden.");
      const { rows: events } = await db().query(
        "SELECT id,occurred_at,confidence,detector_version FROM chest_events WHERE match_id=$1 ORDER BY occurred_at,id",
        [detail[1]],
      );
      return json({ match, events });
    }
    if (path.startsWith("client/")) {
      if (user.kind !== "access") fail(403, "Client-Anmeldung erforderlich.");
      await limit("client:" + user.id, 300, 60);
      if (path === "client/heartbeat" && method === "POST") {
        const input = z
          .object({
            state: z.enum([
              "OFFLINE",
              "UNKNOWN",
              "LOBBY",
              "LOADING",
              "IN_MATCH",
              "MATCH_END_PENDING",
            ]),
            matchId: uuid.nullable().default(null),
          })
          .parse(await body(req));
        if (input.state === "IN_MATCH" && !input.matchId)
          fail(400, "Match-ID erforderlich.");
        if (input.matchId) {
          const { rows } = await db().query(
            "SELECT id FROM matches WHERE id=$1 AND user_id=$2 AND status='ACTIVE'",
            [input.matchId, user.id],
          );
          if (!rows.length) fail(400, "Kein aktives eigenes Match.");
        }
        await db().query(
          "INSERT INTO presence(user_id,state,match_id) VALUES($1,$2,$3) ON CONFLICT(user_id) DO UPDATE SET state=$2,match_id=$3,last_heartbeat_at=now()",
          [user.id, input.state, input.matchId],
        );
        return json({ ok: true });
      }
      if (path === "client/matches" && method === "POST") {
        const input = z
          .object({ id: uuid, startedAt: date })
          .parse(await body(req));
        if (
          input.startedAt.getTime() > Date.now() + 60000 ||
          input.startedAt.getTime() < Date.now() - 7 * 86400000
        )
          fail(400, "Unplausibler Matchbeginn.");
        const { rows } = await db().query(
          "INSERT INTO matches(id,user_id,started_at,status) VALUES($1,$2,$3,'ACTIVE') ON CONFLICT(id) DO NOTHING RETURNING id",
          [input.id, user.id, input.startedAt],
        );
        if (!rows.length) {
          const {
            rows: [existing],
          } = await db().query("SELECT user_id FROM matches WHERE id=$1", [
            input.id,
          ]);
          if (existing?.user_id !== user.id)
            fail(409, "Match-ID bereits vergeben.");
        }
        return json({ id: input.id }, rows.length ? 201 : 200);
      }
      if (path === "client/matches/end" && method === "POST") {
        const input = z
          .object({
            id: uuid,
            endedAt: date,
            status: z.enum(["COMPLETED", "INCOMPLETE"]),
          })
          .parse(await body(req));
        const { rows } = await db().query(
          "UPDATE matches SET ended_at=$1,status=$2 WHERE id=$3 AND user_id=$4 AND started_at<=$1 AND $1<=now()+interval '1 minute' RETURNING id",
          [input.endedAt, input.status, input.id, user.id],
        );
        if (!rows.length) fail(400, "Ungültiges Matchende.");
        return json({ ok: true });
      }
      if (path === "client/events" && method === "POST") {
        const input = eventSchema.parse(await body(req));
        const result = await transaction(async (tx) => {
          const {
            rows: [m],
          } = await tx.query(
            "SELECT * FROM matches WHERE id=$1 AND user_id=$2 FOR UPDATE",
            [input.matchId, user.id],
          );
          if (!m) fail(404, "Eigenes Match nicht gefunden.");
          const {
            rows: [old],
          } = await tx.query(
            "SELECT * FROM chest_events WHERE user_id=$1 AND client_event_id=$2",
            [user.id, input.clientEventId],
          );
          if (old) {
            if (old.match_id !== input.matchId)
              fail(409, "Ereignis gehört zu einem anderen Match.");
            return { duplicate: true, id: old.id };
          }
          const t = input.occurredAt.getTime();
          if (
            t < new Date(m.started_at).getTime() ||
            t > Date.now() + 60000 ||
            (m.ended_at && t > new Date(m.ended_at).getTime())
          )
            fail(400, "Ereignis liegt außerhalb des Matches.");
          const {
            rows: [count],
          } = await tx.query(
            "SELECT COUNT(*)::int AS n FROM chest_events WHERE match_id=$1",
            [input.matchId],
          );
          if (count.n >= 200) fail(400, "Plausibilitätsgrenze erreicht.");
          const { rows: near } = await tx.query(
            "SELECT id FROM chest_events WHERE user_id=$1 AND occurred_at BETWEEN $2::timestamptz-interval '500 milliseconds' AND $2::timestamptz+interval '500 milliseconds'",
            [user.id, input.occurredAt],
          );
          if (near.length) fail(400, "Ereignisse liegen zu dicht zusammen.");
          const id = randomUUID();
          await tx.query(
            "INSERT INTO chest_events(id,match_id,user_id,client_event_id,occurred_at,confidence,detector_version) VALUES($1,$2,$3,$4,$5,$6,$7)",
            [
              id,
              input.matchId,
              user.id,
              input.clientEventId,
              input.occurredAt,
              input.confidence,
              input.detectorVersion,
            ],
          );
          return { duplicate: false, id };
        });
        return json(result, result.duplicate ? 200 : 201);
      }
      fail(404, "Client-Endpunkt nicht gefunden.");
    }
    if (path.startsWith("admin/")) {
      if (user.role !== "ADMIN") fail(403, "Administratorrechte erforderlich.");
      if (path === "admin/users" && method === "GET")
        return json({
          users: (
            await db().query("SELECT * FROM users ORDER BY created_at")
          ).rows.map(publicUser),
        });
      if (path === "admin/users" && method === "POST") {
        const input = z
          .object({ username, displayName: z.string().min(1).max(40) })
          .parse(await body(req));
        const initial = secret(),
          hash = await passwordHash(initial),
          id = randomUUID();
        await transaction(async (tx) => {
          await tx.query("LOCK TABLE users IN EXCLUSIVE MODE");
          const {
            rows: [c],
          } = await tx.query(
            "SELECT COUNT(*)::int n FROM users WHERE enabled=true",
          );
          if (c.n >= 4)
            fail(409, "Es sind höchstens vier aktive Konten erlaubt.");
          await tx.query(
            "INSERT INTO users(id,username,display_name,role,password_hash) VALUES($1,$2,$3,'USER',$4)",
            [id, input.username, input.displayName, hash],
          );
          await audit(tx, user, "CREATE", "USER", id, null, input);
        });
        // Only an authenticated admin receives this one-time credential; it is never logged or saved as plaintext.
        return json({ id, initialPassword: initial }, 201);
      }
      const userPath = path.match(/^admin\/users\/([\w-]+)$/);
      if (userPath && method === "DELETE") {
        const id = uuid.parse(userPath[1]);
        await transaction(async (tx) => {
          await tx.query("LOCK TABLE users IN EXCLUSIVE MODE");
          const {
            rows: [target],
          } = await tx.query("SELECT * FROM users WHERE id=$1 FOR UPDATE", [
            id,
          ]);
          if (!target) fail(404, "Konto nicht gefunden.");
          if (target.role === "ADMIN" || target.id === user.id)
            fail(400, "Das Administratorkonto kann nicht gelöscht werden.");
          const {
            rows: [counts],
          } = await tx.query(
            "SELECT (SELECT COUNT(*)::int FROM matches WHERE user_id=$1) AS matches, (SELECT COUNT(*)::int FROM chest_events WHERE user_id=$1) AS events",
            [id],
          );
          await tx.query("DELETE FROM presence WHERE user_id=$1", [id]);
          await tx.query("DELETE FROM sessions WHERE user_id=$1", [id]);
          await tx.query("DELETE FROM chest_events WHERE user_id=$1", [id]);
          await tx.query("DELETE FROM matches WHERE user_id=$1", [id]);
          await tx.query("DELETE FROM users WHERE id=$1", [id]);
          await audit(
            tx,
            user,
            "DELETE",
            "USER",
            id,
            {
              user: publicUser(target),
              deletedMatches: counts.matches,
              deletedEvents: counts.events,
            },
            null,
          );
        });
        return json({ ok: true });
      }
      if (userPath && method === "PATCH") {
        const id = uuid.parse(userPath[1]);
        const input = z
          .object({
            displayName: z.string().min(1).max(40).optional(),
            enabled: z.boolean().optional(),
            resetPassword: z.boolean().optional(),
          })
          .parse(await body(req));
        const initial = input.resetPassword ? secret() : null,
          hash = initial ? await passwordHash(initial) : null;
        await transaction(async (tx) => {
          await tx.query("LOCK TABLE users IN EXCLUSIVE MODE");
          const {
            rows: [before],
          } = await tx.query("SELECT * FROM users WHERE id=$1", [id]);
          if (!before) fail(404, "Konto nicht gefunden.");
          if (before.role === "ADMIN" && input.enabled === false)
            fail(400, "Das Administratorkonto kann nicht deaktiviert werden.");
          if (input.enabled === true && !before.enabled) {
            const {
              rows: [c],
            } = await tx.query(
              "SELECT COUNT(*)::int n FROM users WHERE enabled=true",
            );
            if (c.n >= 4) fail(409, "Höchstens vier aktive Konten.");
          }
          await tx.query(
            "UPDATE users SET display_name=COALESCE($1,display_name),enabled=COALESCE($2,enabled),password_hash=COALESCE($3,password_hash),must_change_password=CASE WHEN $3::text IS NULL THEN must_change_password ELSE true END WHERE id=$4",
            [input.displayName ?? null, input.enabled ?? null, hash, id],
          );
          if (hash || input.enabled === false)
            await tx.query(
              "UPDATE sessions SET revoked_at=now() WHERE user_id=$1",
              [id],
            );
          const {
            rows: [after],
          } = await tx.query("SELECT * FROM users WHERE id=$1", [id]);
          await audit(tx, user, "UPDATE", "USER", id, publicUser(before), {
            ...publicUser(after),
            passwordReset: !!hash,
          });
        });
        return json({
          ok: true,
          ...(initial ? { initialPassword: initial } : {}),
        });
      }
      if (path === "admin/audit" && method === "GET")
        return json({
          audit: (
            await db().query(
              "SELECT a.*,u.display_name AS admin_name FROM audit_log a JOIN users u ON u.id=a.admin_user_id ORDER BY created_at DESC LIMIT 200",
            )
          ).rows,
        });
      if (path === "admin/export" && method === "GET")
        return json({
          exportedAt: new Date().toISOString(),
          users: (await db().query("SELECT * FROM users")).rows.map(publicUser),
          matches: (await db().query("SELECT * FROM matches")).rows,
          events: (await db().query("SELECT * FROM chest_events")).rows,
          audit: (await db().query("SELECT * FROM audit_log")).rows,
        });
      if (path === "admin/matches" && method === "POST") {
        const input = matchSchema.parse(await body(req));
        if (!input.userId) fail(400, "Spieler erforderlich.");
        if (input.status === "COMPLETED" && !input.endedAt)
          fail(400, "Endzeit erforderlich.");
        await transaction(async (tx) => {
          await tx.query(
            "INSERT INTO matches(id,user_id,started_at,ended_at,status) VALUES($1,$2,$3,$4,$5)",
            [
              input.id,
              input.userId,
              input.startedAt,
              input.endedAt ?? null,
              input.status,
            ],
          );
          await audit(tx, user, "CREATE", "MATCH", input.id, null, input);
        });
        return json({ id: input.id }, 201);
      }
      const adminMatch = path.match(/^admin\/matches\/([\w-]+)$/);
      if (adminMatch && (method === "PATCH" || method === "DELETE")) {
        const id = uuid.parse(adminMatch[1]);
        const input =
          method === "PATCH"
            ? matchSchema
                .omit({ id: true, userId: true })
                .parse(await body(req))
            : null;
        if (input?.status === "COMPLETED" && !input.endedAt)
          fail(400, "Endzeit erforderlich.");
        await transaction(async (tx) => {
          const {
            rows: [before],
          } = await tx.query("SELECT * FROM matches WHERE id=$1 FOR UPDATE", [
            id,
          ]);
          if (!before) fail(404, "Match nicht gefunden.");
          const { rows: events } = await tx.query(
            "SELECT * FROM chest_events WHERE match_id=$1",
            [id],
          );
          if (input) {
            if (
              events.some(
                (e) =>
                  new Date(e.occurred_at) < input.startedAt ||
                  (input.endedAt && new Date(e.occurred_at) > input.endedAt),
              )
            )
              fail(400, "Matchzeiten müssen alle Ereignisse einschließen.");
            await tx.query(
              "UPDATE matches SET started_at=$1,ended_at=$2,status=$3 WHERE id=$4",
              [input.startedAt, input.endedAt ?? null, input.status, id],
            );
          } else await tx.query("DELETE FROM matches WHERE id=$1", [id]);
          await audit(
            tx,
            user,
            input ? "UPDATE" : "DELETE",
            "MATCH",
            id,
            { ...before, events },
            input,
          );
        });
        return json({ ok: true });
      }
      if (path === "admin/events" && method === "POST") {
        const input = eventSchema.parse(await body(req)),
          id = randomUUID();
        await transaction(async (tx) => {
          const {
            rows: [m],
          } = await tx.query("SELECT * FROM matches WHERE id=$1 FOR UPDATE", [
            input.matchId,
          ]);
          if (!m) fail(404, "Match nicht gefunden.");
          if (
            input.occurredAt < new Date(m.started_at) ||
            (m.ended_at && input.occurredAt > new Date(m.ended_at))
          )
            fail(400, "Zeitpunkt liegt außerhalb des Matches.");
          await tx.query(
            "INSERT INTO chest_events(id,match_id,user_id,client_event_id,occurred_at,confidence,detector_version) VALUES($1,$2,$3,$4,$5,$6,$7)",
            [
              id,
              input.matchId,
              m.user_id,
              input.clientEventId,
              input.occurredAt,
              input.confidence,
              input.detectorVersion,
            ],
          );
          await audit(tx, user, "CREATE", "EVENT", id, null, input);
        });
        return json({ id }, 201);
      }
      const adminEvent = path.match(/^admin\/events\/([\w-]+)$/);
      if (adminEvent && (method === "DELETE" || method === "PATCH")) {
        const id = uuid.parse(adminEvent[1]);
        const input =
          method === "PATCH"
            ? z.object({ occurredAt: date }).parse(await body(req))
            : null;
        await transaction(async (tx) => {
          const {
            rows: [before],
          } = await tx.query(
            "SELECT * FROM chest_events WHERE id=$1 FOR UPDATE",
            [id],
          );
          if (!before) fail(404, "Ereignis nicht gefunden.");
          if (input) {
            const {
              rows: [m],
            } = await tx.query("SELECT * FROM matches WHERE id=$1", [
              before.match_id,
            ]);
            if (
              input.occurredAt < new Date(m.started_at) ||
              (m.ended_at && input.occurredAt > new Date(m.ended_at))
            )
              fail(400, "Zeitpunkt liegt außerhalb des Matches.");
            await tx.query(
              "UPDATE chest_events SET occurred_at=$1 WHERE id=$2",
              [input.occurredAt, id],
            );
          } else await tx.query("DELETE FROM chest_events WHERE id=$1", [id]);
          await audit(
            tx,
            user,
            input ? "UPDATE" : "DELETE",
            "EVENT",
            id,
            before,
            input,
          );
        });
        return json({ ok: true });
      }
    }
    return json({ error: "Endpunkt nicht gefunden." }, 404);
  } catch (e) {
    if (e instanceof HttpError) return json({ error: e.message }, e.status);
    if (e instanceof z.ZodError)
      return json(
        {
          error: "Bitte Eingaben prüfen.",
          fields: e.issues.map((i) => ({
            path: i.path.join("."),
            message: i.message,
          })),
        },
        400,
      );
    const code = (e as { code?: string }).code;
    if (code === "23505")
      return json(
        {
          error:
            "Dieser Eintrag existiert bereits oder es läuft noch ein Match.",
        },
        409,
      );
    if (code === "23503" || code === "23514")
      return json({ error: "Ungültiger Bezug oder unplausible Werte." }, 400);
    console.error("API failure", {
      code: code ?? "INTERNAL",
      type: e instanceof Error ? e.name : "Unknown",
    });
    return json({ error: "Der Server ist zurzeit nicht verfügbar." }, 503);
  }
}
