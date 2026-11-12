import { before, after, test } from "node:test";
import assert from "node:assert/strict";
import { randomUUID } from "node:crypto";
import { PGlite } from "@electric-sql/pglite";
import { setTestDatabase, db, transaction } from "../apps/web/lib/db";
import { migration } from "../apps/web/lib/schema";
import {
  bootstrap,
  checkPassword,
  passwordHash,
  issueSession,
} from "../apps/web/lib/auth";
import { handle } from "../apps/web/lib/api";
const pg = new PGlite();
let initial = "",
  adminCookie = "",
  friendCookie = "",
  friendId = "",
  access = "",
  refresh = "",
  matchId = randomUUID(),
  eventId = randomUUID();
const origin = "http://localhost:3000";
async function request(
  path: string,
  method = "GET",
  body?: unknown,
  auth = adminCookie,
  extra: Record<string, string> = {},
) {
  const res = await handle(
    new Request(origin + "/api/" + path, {
      method,
      headers: {
        ...(method === "GET"
          ? {}
          : { "content-type": "application/json", origin }),
        ...(auth
          ? auth.startsWith("Bearer ")
            ? { authorization: auth }
            : { cookie: auth }
          : {}),
        ...extra,
      },
      body: body === undefined ? undefined : JSON.stringify(body),
    }),
  );
  return {
    status: res.status,
    body: await res.json(),
    cookie: res.headers.get("set-cookie")?.split(";")[0] ?? "",
  };
}
before(async () => {
  setTestDatabase({
    query: async (text, params) => {
      const r = await pg.query(text, params);
      return { rows: r.rows as any[], rowCount: r.affectedRows };
    },
  });
  await pg.exec(migration);
  initial = (await transaction(bootstrap))!;
});
after(async () => {
  await pg.close();
});
test("bootstrap creates exactly one Fufi and stores Argon2id only", async () => {
  assert.ok(initial.length >= 24);
  const {
    rows: [u],
  } = await db().query("SELECT * FROM users");
  assert.equal(u.username, "Fufi");
  assert.equal(u.role, "ADMIN");
  assert.ok(u.password_hash.startsWith("$argon2id$"));
  assert.ok(await checkPassword(u.password_hash, initial));
  assert.equal(await transaction(bootstrap), null);
  assert.equal((await db().query("SELECT * FROM users")).rows.length, 1);
});
test("unauthenticated data is private", async () => {
  assert.equal((await request("dashboard", "GET", undefined, "")).status, 401);
  assert.equal(
    (await request("admin/users", "GET", undefined, "")).status,
    401,
  );
});
test("web login issues HttpOnly session without exposing password", async () => {
  const r = await request(
    "auth/login",
    "POST",
    { username: "Fufi", password: initial },
    "",
  );
  assert.equal(r.status, 200);
  adminCookie = r.cookie;
  assert.ok(adminCookie.startsWith("ct_session="));
  assert.equal(r.body.user.mustChangePassword, true);
  assert.ok(!JSON.stringify(r.body).includes(initial));
  assert.equal((await request("dashboard")).status, 428);
});
test("first login must change password, old password fails", async () => {
  let r = await request("auth/password", "POST", {
    currentPassword: initial,
    newPassword: "Fufi-new-password-2026!",
  });
  assert.equal(r.status, 200);
  assert.equal((await request("dashboard")).status, 200);
  r = await request(
    "auth/login",
    "POST",
    { username: "Fufi", password: initial },
    "",
  );
  assert.equal(r.status, 401);
});
test("cross-site and missing-origin cookie mutations rejected", async () => {
  assert.equal(
    (
      await request(
        "admin/users",
        "POST",
        { username: "bad", displayName: "bad" },
        adminCookie,
        { origin: "https://evil.example" },
      )
    ).status,
    403,
  );
  const r = await handle(
    new Request(origin + "/api/auth/logout", {
      method: "POST",
      headers: { cookie: adminCookie, "content-type": "application/json" },
      body: "{}",
    }),
  );
  assert.equal(r.status, 403);
});
test("admin creates a friend with unique initial password", async () => {
  const r = await request("admin/users", "POST", {
    username: "Luca",
    displayName: "Luca",
  });
  assert.equal(r.status, 201);
  friendId = r.body.id;
  const login = await request(
    "auth/login",
    "POST",
    { username: "Luca", password: r.body.initialPassword },
    "",
  );
  friendCookie = login.cookie;
  assert.equal(
    (
      await request(
        "auth/password",
        "POST",
        {
          currentPassword: r.body.initialPassword,
          newPassword: "Luca-password-2026!",
        },
        friendCookie,
      )
    ).status,
    200,
  );
});
test("friend cannot write admin statistics or users", async () => {
  assert.equal(
    (
      await request(
        "admin/users",
        "POST",
        { username: "hacker", displayName: "No" },
        friendCookie,
      )
    ).status,
    403,
  );
  assert.equal(
    (await request("admin/export", "GET", undefined, friendCookie)).status,
    403,
  );
  assert.equal(
    (await request("admin/matches", "POST", {}, friendCookie)).status,
    403,
  );
});
test("four active accounts maximum, case-insensitive login", async () => {
  for (const n of ["Tim", "Noah"])
    assert.equal(
      (await request("admin/users", "POST", { username: n, displayName: n }))
        .status,
      201,
    );
  assert.equal(
    (
      await request("admin/users", "POST", {
        username: "Fifth",
        displayName: "No",
      })
    ).status,
    409,
  );
  assert.equal(
    (
      await request(
        "auth/login",
        "POST",
        { username: "luca", password: "Luca-password-2026!" },
        "",
      )
    ).status,
    200,
  );
});
test("desktop gets short-lived access and rotating refresh tokens", async () => {
  const r = await request(
    "auth/login",
    "POST",
    { username: "Luca", password: "Luca-password-2026!", client: "desktop" },
    "",
  );
  assert.equal(r.status, 200);
  access = r.body.accessToken;
  refresh = r.body.refreshToken;
  assert.equal(r.body.expiresIn, 900);
  assert.equal(
    (
      await request(
        "client/heartbeat",
        "POST",
        { state: "LOBBY" },
        friendCookie,
      )
    ).status,
    403,
  );
});
test("client starts its own match and repeated start is idempotent", async () => {
  const body = {
    id: matchId,
    startedAt: new Date(Date.now() - 60000).toISOString(),
  };
  assert.equal(
    (await request("client/matches", "POST", body, "Bearer " + access)).status,
    201,
  );
  assert.equal(
    (await request("client/matches", "POST", body, "Bearer " + access)).status,
    200,
  );
});
test("chest event is counted once after network retry", async () => {
  const body = {
    clientEventId: eventId,
    matchId,
    occurredAt: new Date(Date.now() - 40000).toISOString(),
    confidence: 0.98,
    detectorVersion: "synthetic-test",
  };
  let r = await request("client/events", "POST", body, "Bearer " + access);
  assert.equal(r.status, 201);
  r = await request("client/events", "POST", body, "Bearer " + access);
  assert.equal(r.status, 200);
  assert.equal(r.body.duplicate, true);
  const d = (await request("dashboard")).body;
  assert.equal(d.users.find((u: any) => u.id === friendId).total_chests, 1);
});
test("bad confidence, timestamps and payloads fail validation", async () => {
  const body = {
    clientEventId: randomUUID(),
    matchId,
    occurredAt: new Date().toISOString(),
    confidence: 0.3,
    detectorVersion: "test",
  };
  assert.equal(
    (await request("client/events", "POST", body, "Bearer " + access)).status,
    400,
  );
  assert.equal(
    (
      await request(
        "client/events",
        "POST",
        {
          ...body,
          confidence: 1,
          occurredAt: new Date(Date.now() + 120000).toISOString(),
        },
        "Bearer " + access,
      )
    ).status,
    400,
  );
  assert.equal((await request("matches/not-a-uuid")).status, 400);
});
test("presence is blue in match, unknown stays unknown, timeout becomes offline", async () => {
  assert.equal(
    (
      await request(
        "client/heartbeat",
        "POST",
        { state: "IN_MATCH", matchId },
        "Bearer " + access,
      )
    ).status,
    200,
  );
  let d = (await request("dashboard")).body;
  assert.equal(d.users.find((u: any) => u.id === friendId).state, "IN_MATCH");
  assert.equal(d.users.find((u: any) => u.id === friendId).current_chests, 1);
  await request(
    "client/heartbeat",
    "POST",
    { state: "UNKNOWN", matchId },
    "Bearer " + access,
  );
  d = (await request("dashboard")).body;
  assert.equal(d.users.find((u: any) => u.id === friendId).state, "UNKNOWN");
  await db().query(
    "UPDATE presence SET last_heartbeat_at=now()-interval '30 seconds' WHERE user_id=$1",
    [friendId],
  );
  d = (await request("dashboard")).body;
  assert.equal(d.users.find((u: any) => u.id === friendId).state, "OFFLINE");
});
test("complete match, average and graph derive from stored events", async () => {
  assert.equal(
    (
      await request(
        "client/matches/end",
        "POST",
        { id: matchId, endedAt: new Date().toISOString(), status: "COMPLETED" },
        "Bearer " + access,
      )
    ).status,
    200,
  );
  const d = (await request("dashboard")).body,
    p = d.users.find((u: any) => u.id === friendId);
  assert.equal(p.completed_matches, 1);
  assert.equal(p.average, 1);
  const detail = (await request("matches/" + matchId)).body;
  assert.equal(detail.events.length, 1);
  assert.ok(detail.events[0].occurred_at);
});
test("other clients cannot submit events to foreign matches", async () => {
  const {
    rows: [admin],
  } = await db().query("SELECT id FROM users WHERE username='Fufi'");
  const a = await issueSession(admin.id, "access");
  assert.equal(
    (
      await request(
        "client/events",
        "POST",
        {
          clientEventId: randomUUID(),
          matchId,
          occurredAt: new Date().toISOString(),
          confidence: 1,
          detectorVersion: "test",
        },
        "Bearer " + a.token,
      )
    ).status,
    404,
  );
});
test("admin event correction recalculates aggregates and writes audit", async () => {
  const r = await request("admin/events", "POST", {
    clientEventId: randomUUID(),
    matchId,
    occurredAt: new Date(Date.now() - 20000).toISOString(),
    confidence: 1,
    detectorVersion: "admin-correction",
  });
  assert.equal(r.status, 201);
  assert.equal(
    (await request("dashboard")).body.users.find((u: any) => u.id === friendId)
      .total_chests,
    2,
  );
  assert.equal(
    (await request("admin/events/" + r.body.id, "DELETE", {})).status,
    200,
  );
  assert.equal(
    (await request("dashboard")).body.users.find((u: any) => u.id === friendId)
      .total_chests,
    1,
  );
  const logs = (await request("admin/audit")).body.audit;
  assert.ok(
    logs.some((l: any) => l.action === "DELETE" && l.target_type === "EVENT"),
  );
  assert.ok(!JSON.stringify(logs).includes("password_hash"));
});
test("refresh replay revokes the whole token family", async () => {
  const r = await request(
    "auth/refresh",
    "POST",
    { refreshToken: refresh },
    "Bearer " + access,
  );
  assert.equal(r.status, 200);
  assert.equal(
    (
      await request(
        "auth/refresh",
        "POST",
        { refreshToken: refresh },
        "Bearer " + access,
      )
    ).status,
    401,
  );
  assert.equal(
    (await request("auth/me", "GET", undefined, "Bearer " + r.body.accessToken))
      .status,
    401,
  );
});
test("disabled users lose access immediately, admin cannot disable itself", async () => {
  const {
    rows: [admin],
  } = await db().query("SELECT id FROM users WHERE username='Fufi'");
  assert.equal(
    (await request("admin/users/" + admin.id, "PATCH", { enabled: false }))
      .status,
    400,
  );
  assert.equal(
    (await request("admin/users/" + friendId, "PATCH", { enabled: false }))
      .status,
    200,
  );
  assert.equal(
    (await request("dashboard", "GET", undefined, friendCookie)).status,
    401,
  );
});
test("expired session is rejected, logout revokes", async () => {
  const {
    rows: [admin],
  } = await db().query("SELECT id FROM users WHERE username='Fufi'");
  const a = await issueSession(admin.id, "access");
  await db().query(
    "UPDATE sessions SET expires_at=now()-interval '1 second' WHERE kind='access'",
  );
  assert.equal(
    (await request("auth/me", "GET", undefined, "Bearer " + a.token)).status,
    401,
  );
  assert.equal((await request("auth/logout", "POST", {})).status, 200);
  assert.equal((await request("dashboard")).status, 401);
});
test("login rate limit is persistent in database", async () => {
  for (let i = 0; i < 11; i++)
    await request(
      "auth/login",
      "POST",
      { username: "Unknown", password: "wrong" },
      "",
    );
  assert.equal(
    (
      await request(
        "auth/login",
        "POST",
        { username: "Unknown", password: "wrong" },
        "",
      )
    ).status,
    429,
  );
});
