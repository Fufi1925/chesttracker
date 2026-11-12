import { db } from "../apps/web/lib/db";
import { migration } from "../apps/web/lib/schema";
await db().query(migration);
console.log("Migration 1 erfolgreich.");
process.exit(0);
