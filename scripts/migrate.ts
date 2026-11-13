import { db } from "../apps/web/lib/db";
import { migration } from "../apps/web/lib/schema";
await db().query(migration);
console.log("Migrationen bis Version 2 erfolgreich.");
process.exit(0);
