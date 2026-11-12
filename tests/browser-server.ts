import { createServer } from "node:http";
import next from "next";
import { PGlite } from "@electric-sql/pglite";
import { migration } from "../apps/web/lib/schema";
import { setTestDatabase, db } from "../apps/web/lib/db";
import { passwordHash } from "../apps/web/lib/auth";
import { handle } from "../apps/web/lib/api";
import { randomUUID } from "node:crypto";
const pg = new PGlite();
setTestDatabase({
  query: async (t, p) => {
    const r = await pg.query(t, p);
    return { rows: r.rows as any[], rowCount: r.affectedRows };
  },
});
await pg.exec(migration);
await db().query(
  "INSERT INTO users(id,username,display_name,role,password_hash,must_change_password) VALUES($1,'Fufi','Fufi','ADMIN',$2,false)",
  [randomUUID(), await passwordHash("browser-test-password-only")],
);
const app = next({ dev: false, dir: "apps/web" });
await app.prepare();
const handler = app.getRequestHandler();
createServer(async (req, res) => {
  if (req.url?.startsWith("/api/")) {
    const chunks = [];
    for await (const c of req) chunks.push(c);
    const headers = new Headers();
    for (const [k, v] of Object.entries(req.headers))
      if (v) headers.set(k, Array.isArray(v) ? v.join(",") : v);
    const r = await handle(
      new Request("http://127.0.0.1:3100" + req.url, {
        method: req.method,
        headers,
        body: ["GET", "HEAD"].includes(req.method!)
          ? undefined
          : Buffer.concat(chunks),
      }),
    );
    res.writeHead(r.status, Object.fromEntries(r.headers));
    res.end(await r.text());
  } else await handler(req, res);
}).listen(3100, "127.0.0.1", () =>
  console.log("Isolated browser test server ready"),
);
