import {randomBytes} from 'node:crypto';
import {existsSync,mkdirSync,writeFileSync} from 'node:fs';
mkdirSync('infra',{recursive:true});mkdirSync('.secrets',{recursive:true,mode:0o700});
if(!existsSync('infra/.env'))writeFileSync('infra/.env',`POSTGRES_PASSWORD=${randomBytes(24).toString('hex')}\n`,{mode:0o600,flag:'wx'});
console.log('Lokale Konfiguration bereit. Start: docker compose --env-file infra/.env -f infra/compose.yml up --build');
