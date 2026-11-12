import { mkdir, writeFile, chmod } from "node:fs/promises";
import { bootstrap } from "../apps/web/lib/auth";
import { transaction, db } from "../apps/web/lib/db";
import { migration } from "../apps/web/lib/schema";
await db().query(migration);
const password = await transaction(bootstrap);
if (password) {
  await mkdir(".secrets", { recursive: true, mode: 0o700 });
  await chmod(".secrets", 0o700);
  await writeFile(
    ".secrets/fufi-startpasswort.txt",
    `Benutzername: Fufi\nStartpasswort: ${password}\nBeim ersten Login ändern. Diese Datei danach sicher löschen.\n`,
    { mode: 0o600, flag: "wx" },
  );
  console.log(
    "Fufi erstellt. Startpasswort steht ausschließlich in .secrets/fufi-startpasswort.txt (0600).",
  );
} else console.log("Fufi existiert bereits. Passwort unverändert.");
process.exit(0);
