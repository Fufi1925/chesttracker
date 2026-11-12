export const migration = `
CREATE TABLE IF NOT EXISTS schema_migrations (version integer PRIMARY KEY, applied_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS users (
 id uuid PRIMARY KEY, username text UNIQUE NOT NULL, display_name text NOT NULL,
 role text NOT NULL CHECK(role IN ('ADMIN','USER')), password_hash text NOT NULL,
 enabled boolean NOT NULL DEFAULT true, must_change_password boolean NOT NULL DEFAULT true,
 created_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS sessions (
 id uuid PRIMARY KEY, user_id uuid NOT NULL REFERENCES users(id), token_hash text UNIQUE NOT NULL,
 kind text NOT NULL CHECK(kind IN ('web','access','refresh')), family_id uuid NOT NULL,
 expires_at timestamptz NOT NULL, revoked_at timestamptz, created_at timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX IF NOT EXISTS users_username_case_insensitive ON users(lower(username));
CREATE INDEX IF NOT EXISTS sessions_user_idx ON sessions(user_id);
CREATE TABLE IF NOT EXISTS matches (
 id uuid PRIMARY KEY, user_id uuid NOT NULL REFERENCES users(id), started_at timestamptz NOT NULL,
 ended_at timestamptz, status text NOT NULL CHECK(status IN ('ACTIVE','COMPLETED','INCOMPLETE')),
 CHECK(ended_at IS NULL OR ended_at >= started_at)
);
CREATE UNIQUE INDEX IF NOT EXISTS one_active_match ON matches(user_id) WHERE status='ACTIVE';
CREATE INDEX IF NOT EXISTS matches_user_time ON matches(user_id, started_at DESC);
CREATE TABLE IF NOT EXISTS chest_events (
 id uuid PRIMARY KEY, match_id uuid NOT NULL REFERENCES matches(id) ON DELETE CASCADE,
 user_id uuid NOT NULL REFERENCES users(id), client_event_id uuid NOT NULL,
 occurred_at timestamptz NOT NULL, received_at timestamptz NOT NULL DEFAULT now(),
 confidence real NOT NULL CHECK(confidence BETWEEN 0 AND 1), detector_version text NOT NULL,
 UNIQUE(user_id,client_event_id)
);
CREATE INDEX IF NOT EXISTS events_match_time ON chest_events(match_id,occurred_at);
CREATE TABLE IF NOT EXISTS presence (
 user_id uuid PRIMARY KEY REFERENCES users(id), state text NOT NULL,
 last_heartbeat_at timestamptz NOT NULL DEFAULT now(), match_id uuid REFERENCES matches(id) ON DELETE SET NULL
);
CREATE TABLE IF NOT EXISTS audit_log (
 id uuid PRIMARY KEY, admin_user_id uuid NOT NULL REFERENCES users(id), action text NOT NULL,
 target_type text NOT NULL, target_id text NOT NULL, before_json jsonb, after_json jsonb,
 created_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS rate_limits (
 key text PRIMARY KEY, hits integer NOT NULL, window_start timestamptz NOT NULL
);
INSERT INTO schema_migrations(version) VALUES(1) ON CONFLICT DO NOTHING;
`;
