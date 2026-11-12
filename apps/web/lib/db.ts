import { Pool, type PoolClient } from "pg";
import { Pool as NeonPool, neonConfig } from "@neondatabase/serverless";
import WebSocket from "ws";
import { HttpsProxyAgent } from "https-proxy-agent";
export type Row = Record<string, any>;
export interface Database {
  query<T extends Row = Row>(
    text: string,
    values?: any[],
  ): Promise<{ rows: T[]; rowCount?: number | null }>;
}
let pool: Pool | NeonPool | undefined;
let override: Database | undefined;
export function setTestDatabase(db: Database) {
  if (process.env.NODE_ENV === "production")
    throw Error("Test adapter disabled");
  override = db;
}
export function db(): Database {
  if (override) return override;
  if (!process.env.DATABASE_URL) throw new Error("DATABASE_NOT_CONFIGURED");
  if (!pool) {
    const connectionString = process.env.DATABASE_URL;
    if (new URL(connectionString).hostname.endsWith(".neon.tech")) {
      const proxy = process.env.HTTPS_PROXY ?? process.env.https_proxy;
      neonConfig.webSocketConstructor = proxy
        ? class extends WebSocket {
            constructor(address: string, protocols?: string | string[]) {
              super(address, protocols, { agent: new HttpsProxyAgent(proxy!) });
            }
          }
        : WebSocket;
      pool = new NeonPool({
        connectionString,
        max: 3,
        idleTimeoutMillis: 10000,
        connectionTimeoutMillis: 10000,
      });
    } else
      pool = new Pool({
        connectionString,
        max: 3,
        idleTimeoutMillis: 10000,
        connectionTimeoutMillis: 10000,
      });
  }
  return pool as Database;
}
export async function transaction<T>(
  work: (tx: Database) => Promise<T>,
): Promise<T> {
  const connection: Database & { release?: () => void } =
    override ?? ((await (db() as Pool).connect()) as PoolClient);
  await connection.query("BEGIN");
  try {
    const result = await work(connection);
    await connection.query("COMMIT");
    return result;
  } catch (e) {
    await connection.query("ROLLBACK");
    throw e;
  } finally {
    connection.release?.();
  }
}
