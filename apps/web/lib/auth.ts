import { randomBytes, randomUUID, createHash } from "node:crypto";
import { hash, verify } from "@node-rs/argon2";
import { db, type Database, type Row } from "./db";
export const passwordHash = (password: string) =>
  hash(password, {
    algorithm: 2,
    memoryCost: 19456,
    timeCost: 2,
    parallelism: 1,
  });
export const checkPassword = (hash: string, password: string) =>
  verify(hash, password);
export const tokenHash = (value: string) =>
  createHash("sha256").update(value).digest("hex");
export const secret = () => randomBytes(24).toString("base64url");
export class HttpError extends Error {
  constructor(
    public status: number,
    message: string,
  ) {
    super(message);
  }
}
export const fail = (status: number, message: string): never => {
  throw new HttpError(status, message);
};
export function publicUser(u: Row) {
  return {
    id: u.id,
    username: u.username,
    displayName: u.display_name,
    role: u.role,
    enabled: u.enabled,
    mustChangePassword: u.must_change_password,
    createdAt: u.created_at,
  };
}
export async function issueSession(
  userId: string,
  kind: "web" | "access" | "refresh",
  family = randomUUID(),
  tx: Database = db(),
) {
  const token = secret(),
    seconds = kind === "access" ? 900 : kind === "refresh" ? 2592000 : 43200;
  await tx.query(
    "INSERT INTO sessions(id,user_id,token_hash,kind,family_id,expires_at) VALUES($1,$2,$3,$4,$5,$6)",
    [
      randomUUID(),
      userId,
      tokenHash(token),
      kind,
      family,
      new Date(Date.now() + seconds * 1000),
    ],
  );
  return { token, seconds, family };
}
export const cookie = (token: string, seconds: number) =>
  `ct_session=${token}; Path=/; HttpOnly; SameSite=Strict; Max-Age=${seconds}${process.env.NODE_ENV === "production" && process.env.CT_LOCAL_HTTP !== "1" ? "; Secure" : ""}`;
export async function authenticate(req: Request, allowPasswordChange = false) {
  const bearer = req.headers.get("authorization")?.match(/^Bearer (.+)$/)?.[1];
  const token =
    bearer ??
    req.headers.get("cookie")?.match(/(?:^|;\s*)ct_session=([^;]+)/)?.[1];
  if (!token) return fail(401, "Bitte anmelden.");
  const {
    rows: [user],
  } = await db().query(
    `SELECT u.*,s.id AS session_id,s.family_id,s.kind FROM sessions s JOIN users u ON u.id=s.user_id WHERE s.token_hash=$1 AND s.kind=$2 AND s.expires_at>now() AND s.revoked_at IS NULL AND u.enabled=true`,
    [tokenHash(token), bearer ? "access" : "web"],
  );
  if (!user) return fail(401, "Die Sitzung ist abgelaufen.");
  if (user.must_change_password && !allowPasswordChange)
    return fail(428, "Bitte zuerst das Startpasswort ändern.");
  return user;
}
export async function limit(key: string, max: number, seconds: number) {
  const {
    rows: [r],
  } = await db().query(
    `INSERT INTO rate_limits(key,hits,window_start) VALUES($1,1,now()) ON CONFLICT(key) DO UPDATE SET hits=CASE WHEN rate_limits.window_start < $2 THEN 1 ELSE rate_limits.hits+1 END,window_start=CASE WHEN rate_limits.window_start < $2 THEN now() ELSE rate_limits.window_start END RETURNING hits`,
    [tokenHash(key), new Date(Date.now() - seconds * 1000)],
  );
  if (r.hits > max)
    fail(429, "Zu viele Versuche. Bitte später erneut versuchen.");
}
export async function bootstrap(tx: Database = db()) {
  // Locking makes concurrent bootstrap attempts idempotent; never reset an existing account.
  await tx.query("LOCK TABLE users IN EXCLUSIVE MODE");
  const { rows } = await tx.query("SELECT id FROM users WHERE username='Fufi'");
  if (rows.length) return null;
  const password = secret();
  await tx.query(
    "INSERT INTO users(id,username,display_name,role,password_hash) VALUES($1,'Fufi','Fufi','ADMIN',$2)",
    [randomUUID(), await passwordHash(password)],
  );
  return password;
}
