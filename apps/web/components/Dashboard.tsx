"use client";
import { useCallback, useEffect, useState, type FormEvent } from "react";
import {
  Box,
  LayoutDashboard,
  Gamepad2,
  Trophy,
  UserRound,
  Settings,
  LogOut,
  ArrowUpRight,
  ChevronRight,
  ArrowLeft,
  Download,
  Radio,
  ShieldCheck,
  Plus,
  X,
  KeyRound,
  Trash2,
  RefreshCw,
  BarChart3,
  Clock3,
  Pencil,
  Check,
  UsersRound,
} from "lucide-react";
import { Chart } from "./Chart";
type User = {
  id: string;
  username: string;
  displayName: string;
  role: string;
  mustChangePassword: boolean;
  enabled: boolean;
};
type Player = {
  id: string;
  username: string;
  display_name: string;
  state: string;
  total_chests: number;
  completed_matches: number;
  average: number;
  best_match: number;
  current_chests: number;
  created_at: string;
};
type Match = {
  id: string;
  user_id: string;
  display_name: string;
  started_at: string;
  ended_at: string | null;
  status: string;
  chest_count: number;
};
type Data = {
  user: User;
  users: Player[];
  matches: Match[];
  serverTime: string;
};
type Detail = {
  match: Match;
  events: {
    id: string;
    occurred_at: string;
    confidence: number;
    detector_version: string;
  }[];
};
const fmt = new Intl.NumberFormat("de-DE");
const time = (d: string) =>
  new Date(d).toLocaleString("de-DE", {
    day: "2-digit",
    month: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  });
const duration = (m: Match) =>
  m.ended_at
    ? `${Math.max(0, Math.round((+new Date(m.ended_at) - +new Date(m.started_at)) / 60000))} Min.`
    : "Läuft noch";
function state(p: Player) {
  return p.state === "IN_MATCH"
    ? "Im Match"
    : p.state === "LOBBY"
      ? "In der Lobby"
      : p.state === "OFFLINE"
        ? "Offline"
        : p.state === "LOADING"
          ? "Lädt"
          : p.state === "MATCH_END_PENDING"
            ? "Matchende wird geprüft"
            : "Spielstatus unbekannt";
}
async function api(path: string, method = "GET", data?: unknown) {
  const r = await fetch("/api/" + path, {
    method,
    headers: method === "GET" ? {} : { "Content-Type": "application/json" },
    body: data === undefined ? undefined : JSON.stringify(data),
    cache: "no-store",
  });
  const body = await r.json();
  if (!r.ok)
    throw Object.assign(new Error(body.error ?? "Verbindung fehlgeschlagen."), {
      status: r.status,
    });
  return body;
}
const nav = [
  { id: "overview", label: "Übersicht", icon: LayoutDashboard },
  { id: "matches", label: "Matches", icon: Gamepad2 },
  { id: "ranking", label: "Rangliste", icon: Trophy },
  { id: "profile", label: "Profil", icon: UserRound },
];
export default function Dashboard() {
  const [session, setSession] = useState<User | null>(null),
    [ready, setReady] = useState(false),
    [data, setData] = useState<Data | null>(null),
    [page, setPage] = useState("overview"),
    [profile, setProfile] = useState<string | null>(null),
    [detail, setDetail] = useState<Detail | null>(null),
    [error, setError] = useState(""),
    [connection, setConnection] = useState(true),
    [working, setWorking] = useState(false),
    [modal, setModal] = useState<string | null>(null),
    [notice, setNotice] = useState(""),
    [adminUsers, setAdminUsers] = useState<User[]>([]),
    [audit, setAudit] = useState<any[]>([]),
    [filter, setFilter] = useState("all");
  useEffect(() => {
    api("auth/me")
      .then((r) => setSession(r.user))
      .catch(() => {})
      .finally(() => setReady(true));
  }, []);
  useEffect(() => {
    if (!modal) return;
    const previous = document.activeElement as HTMLElement | null;
    const dialog = document.querySelector<HTMLElement>('[role="dialog"]');
    const focusable = () =>
      Array.from(
        dialog?.querySelectorAll<HTMLElement>(
          'button:not(:disabled),a[href],input,select,[tabindex="0"]',
        ) ?? [],
      );
    focusable()[0]?.focus();
    const key = (e: KeyboardEvent) => {
      if (e.key === "Escape") {
        setModal(null);
        setError("");
      }
      if (e.key === "Tab") {
        const items = focusable(),
          first = items[0],
          last = items.at(-1);
        if (e.shiftKey && document.activeElement === first) {
          e.preventDefault();
          last?.focus();
        } else if (!e.shiftKey && document.activeElement === last) {
          e.preventDefault();
          first?.focus();
        }
      }
    };
    document.addEventListener("keydown", key);
    return () => {
      document.removeEventListener("keydown", key);
      previous?.focus();
    };
  }, [modal]);
  const load = useCallback(async () => {
    try {
      const d = await api("dashboard");
      setData(d);
      setConnection(true);
    } catch (e: any) {
      setConnection(false);
      if (e.status === 401) {
        setSession(null);
        setData(null);
      } else if (e.status === 428)
        setSession((u) => (u ? { ...u, mustChangePassword: true } : u));
    }
  }, []);
  useEffect(() => {
    if (!session || session.mustChangePassword) return;
    void load();
    const timer = setInterval(() => {
      if (!document.hidden) void load();
    }, 1000);
    return () => clearInterval(timer);
  }, [session, load]);
  const loadAdmin = useCallback(async () => {
    const [a, b] = await Promise.all([api("admin/users"), api("admin/audit")]);
    setAdminUsers(a.users);
    setAudit(b.audit);
  }, []);
  useEffect(() => {
    if (page === "admin" && session?.role === "ADMIN")
      void loadAdmin().catch((e) => setError(e.message));
  }, [page, session, loadAdmin]);
  const perform = async (fn: () => Promise<void>) => {
    setWorking(true);
    setError("");
    try {
      await fn();
    } catch (e: any) {
      setError(e.message);
    } finally {
      setWorking(false);
    }
  };
  async function login(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const f = new FormData(e.currentTarget);
    await perform(async () => {
      const r = await api("auth/login", "POST", {
        username: f.get("username"),
        password: f.get("password"),
      });
      setSession(r.user);
    });
  }
  async function changePassword(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const f = new FormData(e.currentTarget);
    if (f.get("newPassword") !== f.get("confirmPassword")) {
      setError("Die neuen Passwörter stimmen nicht überein.");
      return;
    }
    await perform(async () => {
      await api("auth/password", "POST", {
        currentPassword: f.get("currentPassword"),
        newPassword: f.get("newPassword"),
      });
      setSession((s) => (s ? { ...s, mustChangePassword: false } : s));
      setModal(null);
      setNotice("Passwort geändert.");
    });
  }
  async function openMatch(id: string) {
    await perform(async () => {
      setDetail(await api("matches/" + id));
    });
  }
  function navigate(id: string) {
    setPage(id);
    setDetail(null);
    setError("");
    if (id === "profile") setProfile(session?.id ?? null);
  }
  if (!ready)
    return (
      <main className="loading">
        <Box size={30} />
        <p>ChestTracker wird geladen …</p>
      </main>
    );
  const errorBox = error && (
    <div role="alert" className="error">
      {error}
      <button aria-label="Fehlermeldung schließen" onClick={() => setError("")}>
        <X size={15} />
      </button>
    </div>
  );
  const passwordForm = (
    <form onSubmit={changePassword} className="form">
      <label>
        Aktuelles Passwort
        <input
          name="currentPassword"
          type="password"
          autoComplete="current-password"
          required
        />
      </label>
      <label>
        Neues Passwort
        <input
          name="newPassword"
          type="password"
          minLength={12}
          maxLength={128}
          autoComplete="new-password"
          required
        />
        <small>Mindestens 12 Zeichen.</small>
      </label>
      <label>
        Neues Passwort wiederholen
        <input
          name="confirmPassword"
          type="password"
          minLength={12}
          autoComplete="new-password"
          required
        />
      </label>
      <button className="primary" disabled={working}>
        {working ? "Wird gespeichert …" : "Passwort ändern"}
        <ArrowUpRight size={16} />
      </button>
    </form>
  );
  if (!session)
    return (
      <main className="auth-layout">
        <section className="auth-story">
          <a className="brand" href="/">
            <span className="logo">
              <Box size={23} />
            </span>
            ChestTracker<span className="private-tag">PRIVAT</span>
          </a>
          <div>
            <p className="eyebrow">VIER SPIELER. EINE ÜBERSICHT.</p>
            <h1>
              Jedes Match.
              <br />
              Jede Truhe.
              <br />
              <span>Eure Statistik.</span>
            </h1>
            <p className="intro">
              Ein ruhiger Platz für eure Fortschritte.
              <br />
              Verfolge Matches, vergleiche Zahlen und
              <br />
              behalte eure Runde im Blick.
            </p>
            <div className="story-features">
              <span>
                <Radio size={17} />
                Gemeinsamer Live-Status
              </span>
              <span>
                <ShieldCheck size={17} />
                Nur für euren Kreis
              </span>
              <span>
                <BarChart3 size={17} />
                Statistiken aus echten Events
              </span>
            </div>
          </div>
          <small>ChestTracker · Private Anwendung · Keine Epic-Anmeldung</small>
        </section>
        <section className="auth-form">
          <div className="login-card">
            <span className="auth-icon">
              <KeyRound size={23} />
            </span>
            <p className="eyebrow">WILLKOMMEN ZURÜCK</p>
            <h2>Deine Runde wartet.</h2>
            <p>Melde dich mit deinem ChestTracker-Konto an.</p>
            {errorBox}
            <form className="form" onSubmit={login}>
              <label>
                Benutzername
                <input
                  name="username"
                  placeholder="Dein Benutzername"
                  autoComplete="username"
                  maxLength={32}
                  required
                />
              </label>
              <label>
                Passwort
                <input
                  name="password"
                  placeholder="Dein Passwort"
                  type="password"
                  autoComplete="current-password"
                  maxLength={128}
                  required
                />
              </label>
              <button className="primary" disabled={working}>
                {working ? "Anmeldung läuft …" : "Anmelden"}
                <ArrowUpRight size={17} />
              </button>
            </form>
            <div className="invite-note">
              <ShieldCheck size={15} />
              <span>Privater Zugang. Dein Konto erhältst du von Fufi.</span>
            </div>
          </div>
          <p className="auth-footer">
            Deine Daten bleiben in eurem privaten Kreis.
          </p>
        </section>
      </main>
    );
  if (session.mustChangePassword)
    return (
      <main className="password-screen">
        <div className="login-card">
          <span className="auth-icon">
            <ShieldCheck />
          </span>
          <p className="eyebrow">ERSTE ANMELDUNG</p>
          <h2>Mach es zu deinem Konto.</h2>
          <p>Bitte ersetze dein Startpasswort, bevor du fortfährst.</p>
          {errorBox}
          {passwordForm}
        </div>
      </main>
    );
  const me = data?.users.find((u) => u.id === session.id),
    selected = data?.users.find((u) => u.id === (profile ?? session.id)) ?? me,
    sorted = [...(data?.users ?? [])].sort(
      (a, b) => b.total_chests - a.total_chests,
    ),
    mine = data?.matches.filter((m) => m.user_id === session.id) ?? [],
    profileMatches =
      data?.matches.filter((m) => m.user_id === selected?.id) ?? [];
  const stats = (p: Player | undefined) => (
    <div className="stats-grid">
      {[
        {
          icon: Box,
          label: "Gesamte Truhen",
          value: p?.total_chests ?? 0,
          foot: "Alle bestätigten Öffnungen",
        },
        {
          icon: BarChart3,
          label: "Durchschnitt pro Match",
          value: p?.average ?? 0,
          foot: "Nur abgeschlossene Matches",
        },
        {
          icon: Gamepad2,
          label: "Erfasste Matches",
          value: p?.completed_matches ?? 0,
          foot: "Vollständig abgeschlossen",
        },
        {
          icon: Trophy,
          label: "Bestes Match",
          value: p?.best_match ?? 0,
          foot: "Truhen in einer Runde",
        },
      ].map((s) => (
        <article className="stat-card" key={s.label}>
          <div className="stat-top">
            <span>{s.label}</span>
            <s.icon size={19} />
          </div>
          <strong>{fmt.format(s.value)}</strong>
          <small>{s.foot}</small>
        </article>
      ))}
    </div>
  );
  const matchTable = (matches: Match[]) => (
    <div className="table-wrap">
      <table>
        <thead>
          <tr>
            <th>Match / Spieler</th>
            <th>Beginn</th>
            <th>Dauer</th>
            <th>Truhen</th>
            <th>Status</th>
            <th>
              <span className="sr-only">Details</span>
            </th>
          </tr>
        </thead>
        <tbody>
          {matches.map((m) => (
            <tr key={m.id}>
              <td>
                <button
                  className="text-button"
                  onClick={() => void openMatch(m.id)}
                >
                  Match {m.id.slice(0, 6)}
                  <small>{m.display_name}</small>
                </button>
              </td>
              <td>{time(m.started_at)}</td>
              <td>{duration(m)}</td>
              <td>
                <span className="count">
                  <Box size={14} />
                  {m.chest_count}
                </span>
              </td>
              <td>
                <span
                  className={"badge " + (m.status === "ACTIVE" ? "active" : "")}
                >
                  {m.status === "COMPLETED"
                    ? "Abgeschlossen"
                    : m.status === "ACTIVE"
                      ? "Im Match"
                      : "Unvollständig"}
                </span>
              </td>
              <td>
                <button
                  className="icon-button"
                  aria-label={"Details für Match " + m.id.slice(0, 6)}
                  onClick={() => void openMatch(m.id)}
                >
                  <ChevronRight size={17} />
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {!matches.length && (
        <Empty
          title="Hier beginnt deine Match-Historie."
          text="Sobald der Windows-Client ein Match erfasst, findest du hier seinen Verlauf."
          icon={Gamepad2}
        />
      )}
    </div>
  );
  const ranking = (
    <div className="ranking-list">
      {sorted.map((p, i) => (
        <button
          key={p.id}
          className={"rank-row " + (p.id === session.id ? "self" : "")}
          onClick={() => {
            setProfile(p.id);
            setPage("profile");
          }}
        >
          <span className="rank-position">
            {String(i + 1).padStart(2, "0")}
          </span>
          <span className="avatar small">
            {p.display_name.slice(0, 1).toUpperCase()}
          </span>
          <span className="rank-name">
            {p.display_name}
            {p.id === session.id && <small>Du</small>}
          </span>
          <span className="rank-score">
            {fmt.format(p.total_chests)}
            <small>Truhen</small>
          </span>
          <ChevronRight size={15} />
        </button>
      ))}
      {!sorted.length && <p className="muted">Spieler werden geladen …</p>}
    </div>
  );
  return (
    <div className="app-shell">
      <aside className="sidebar">
        <a className="brand" href="/">
          <span className="logo">
            <Box size={21} />
          </span>
          ChestTracker
        </a>
        <div className="workspace-label">
          DEIN PRIVATER KREIS <span>{data?.users.length ?? 1}/4</span>
        </div>
        <nav>
          {nav.map((n) => (
            <button
              key={n.id}
              className={page === n.id ? "selected" : ""}
              onClick={() => navigate(n.id)}
            >
              <n.icon size={19} />
              {n.label}
              {page === n.id && <span className="nav-mark" />}
            </button>
          ))}
          {session.role === "ADMIN" && (
            <>
              <div className="nav-divider" />
              <button
                className={page === "admin" ? "selected" : ""}
                onClick={() => navigate("admin")}
              >
                <Settings size={19} />
                Admin<span className="admin-tag">A</span>
              </button>
            </>
          )}
        </nav>
        <div className="sidebar-bottom">
          <div className="private-note">
            <ShieldCheck size={17} />
            <span>
              Ein privater Kreis.<small>Nur eure Daten.</small>
            </span>
          </div>
          <button className="account" onClick={() => navigate("profile")}>
            <span className="avatar">{session.displayName[0]}</span>
            <span>
              {session.displayName}
              <small>
                {session.role === "ADMIN" ? "Administrator" : "Spieler"}
              </small>
            </span>
            <ChevronRight size={15} />
          </button>
          <button
            className="logout"
            onClick={() =>
              void perform(async () => {
                await api("auth/logout", "POST", {});
                setSession(null);
                setData(null);
              })
            }
          >
            <LogOut size={17} />
            Abmelden
          </button>
        </div>
      </aside>
      <div className="app-body">
        <header className="topbar">
          <span className="breadcrumb">
            Dein Workspace <ChevronRight size={13} />
            <b>
              {detail
                ? "Match-Details"
                : page === "admin"
                  ? "Administration"
                  : nav.find((n) => n.id === page)?.label}
            </b>
          </span>
          <div className="topbar-actions">
            <button
              className="mobile-logout icon-button"
              aria-label="Abmelden"
              onClick={() =>
                void perform(async () => {
                  await api("auth/logout", "POST", {});
                  setSession(null);
                  setData(null);
                })
              }
            >
              <LogOut size={16} />
            </button>
            <span
              className={"connection " + (!connection ? "disconnected" : "")}
            >
              <i />
              {connection ? "Verbunden" : "Verbindung unterbrochen"}
            </span>
          </div>
        </header>
        <main className="content">
          {errorBox}
          {notice && (
            <div className="notice" role="status">
              <Check size={16} />
              {notice}
              <button
                aria-label="Hinweis schließen"
                onClick={() => setNotice("")}
              >
                <X size={15} />
              </button>
            </div>
          )}
          {!data && (
            <div className="panel">
              <Empty
                title="Verbindung wird hergestellt"
                text={
                  connection
                    ? "Deine Statistiken werden geladen."
                    : "Der Server ist noch nicht erreichbar. Wir versuchen es automatisch erneut."
                }
                icon={RefreshCw}
              />
            </div>
          )}
          {data && detail ? (
            <>
              <button className="back" onClick={() => setDetail(null)}>
                <ArrowLeft size={16} />
                Zurück zu den Matches
              </button>
              <PageTitle
                eyebrow="DEIN MATCH IM DETAIL"
                title={"Match " + detail.match.id.slice(0, 6)}
                description={
                  detail.match.display_name +
                  " · " +
                  time(detail.match.started_at)
                }
              />
              <div className="match-summary panel">
                <span>
                  <Box size={20} />
                  <strong>{detail.events.length}</strong> Truhen
                </span>
                <span>
                  <Clock3 size={20} />
                  {duration(detail.match)}
                </span>
                <span className="badge">
                  {detail.match.status === "COMPLETED"
                    ? "Abgeschlossen"
                    : detail.match.status === "ACTIVE"
                      ? "Im Match"
                      : "Unvollständig"}
                </span>
              </div>
              <section className="panel">
                <PanelHeader
                  title="Truhenverlauf"
                  description="Bestätigte Öffnungen über die Zeit · kumuliert"
                />
                <Chart
                  cumulative
                  points={[
                    { label: "00:00", value: 0 },
                    ...detail.events.map((e, i) => ({
                      label: `${Math.floor((+new Date(e.occurred_at) - +new Date(detail.match.started_at)) / 60000)}:${String(Math.floor((+new Date(e.occurred_at) - +new Date(detail.match.started_at)) / 1000) % 60).padStart(2, "0")}`,
                      value: i + 1,
                    })),
                  ]}
                />
              </section>
              {session.role === "ADMIN" && (
                <section className="panel">
                  <PanelHeader
                    title="Datensatz verwalten"
                    description="Jede Änderung wird im Audit-Log protokolliert."
                  />
                  <div className="button-row">
                    <button onClick={() => setModal("editMatch")}>
                      <Pencil size={16} />
                      Match bearbeiten
                    </button>
                    <button onClick={() => setModal("event")}>
                      <Plus size={16} />
                      Ereignis anlegen
                    </button>
                    <button
                      className="danger"
                      onClick={() => setModal("deleteMatch")}
                    >
                      <Trash2 size={16} />
                      Match löschen
                    </button>
                  </div>
                  <div className="event-list">
                    {detail.events.map((e) => (
                      <div key={e.id}>
                        <span>{time(e.occurred_at)}</span>
                        <small>{e.detector_version}</small>
                        <button
                          aria-label="Ereigniszeit bearbeiten"
                          onClick={() => {
                            setModal("editEvent:" + e.id);
                          }}
                        >
                          <Pencil size={14} />
                        </button>
                        <button
                          className="danger"
                          aria-label="Ereignis löschen"
                          onClick={() => setModal("deleteEvent:" + e.id)}
                        >
                          <Trash2 size={14} />
                        </button>
                      </div>
                    ))}
                  </div>
                </section>
              )}
            </>
          ) : data && page === "overview" ? (
            <>
              <PageTitle
                eyebrow="DEINE RUNDE IM BLICK"
                title={"Guten Tag, " + session.displayName + "."}
                description="Ein Überblick über eure Matches und deine Fortschritte."
                action={
                  <button onClick={() => setModal("download")}>
                    <Download size={16} />
                    Windows-App
                  </button>
                }
              />
              <div className="section-label">
                <span>
                  SPIELERSTATUS <b>{data.users.length} SPIELER</b>
                </span>
                <span>
                  <i className="tiny-dot" />
                  LIVE
                </span>
              </div>
              <div className="player-grid">
                {data.users.map((p) => (
                  <button
                    className="player-card"
                    key={p.id}
                    onClick={() => {
                      setProfile(p.id);
                      setPage("profile");
                    }}
                  >
                    <span className={"presence-icon " + p.state.toLowerCase()}>
                      <UserRound size={21} />
                      <i />
                    </span>
                    <span className="player-name">
                      {p.display_name}
                      {p.id === session.id && <small>DU</small>}
                      <span className="player-state">
                        {state(p)}
                        {p.state === "IN_MATCH" && (
                          <>
                            {" "}
                            · <Box size={12} />
                            {p.current_chests}
                          </>
                        )}
                      </span>
                    </span>
                    <ChevronRight size={15} />
                  </button>
                ))}
              </div>
              <div className="section-label">
                <span>DEINE STATISTIK</span>
                <span>GESAMTER ZEITRAUM</span>
              </div>
              {stats(me)}
              <div className="overview-grid">
                <section className="panel">
                  <PanelHeader
                    title="Truhen pro Match"
                    description="Deine letzten 10 abgeschlossenen Matches"
                    right={<BarChart3 size={17} />}
                  />
                  <Chart
                    points={mine
                      .filter((m) => m.status === "COMPLETED")
                      .slice(0, 10)
                      .reverse()
                      .map((m, i) => ({
                        label: "M" + (i + 1),
                        value: m.chest_count,
                      }))}
                  />
                </section>
                <section className="panel">
                  <PanelHeader
                    title="Eure Rangliste"
                    description="Gemeinsam besser werden"
                    right={<Trophy size={17} />}
                  />
                  {ranking}
                  <button
                    className="panel-link"
                    onClick={() => navigate("ranking")}
                  >
                    Rangliste ansehen
                    <ArrowUpRight size={15} />
                  </button>
                </section>
              </div>
              <section className="panel">
                <PanelHeader
                  title="Letzte Matches"
                  description="Deine zuletzt erfassten Runden"
                  right={
                    <button
                      className="text-button subtle"
                      onClick={() => navigate("matches")}
                    >
                      Alle Matches
                      <ArrowUpRight size={14} />
                    </button>
                  }
                />
                {matchTable(mine.slice(0, 5))}
              </section>
              <div className="client-note">
                <span>
                  <Radio size={18} />
                  <span>
                    Bereit für dein nächstes Match?
                    <small>
                      Der Windows-Client verbindet deine erfassten Runden mit
                      dieser Übersicht.
                    </small>
                  </span>
                </span>
                <button
                  className="text-button"
                  onClick={() => setModal("download")}
                >
                  Client einrichten
                  <ArrowUpRight size={16} />
                </button>
              </div>
            </>
          ) : data && page === "matches" ? (
            <>
              <PageTitle
                eyebrow="RUNDE FÜR RUNDE"
                title="Eure Matches"
                description="Alle erfassten Runden. Jeder bestätigte Moment."
              />
              <section className="panel">
                <div className="filter-bar">
                  <span>{data.matches.length} Matches</span>
                  <label>
                    Spieler
                    <select
                      value={filter}
                      onChange={(e) => setFilter(e.target.value)}
                    >
                      <option value="all">Alle Spieler</option>
                      {data.users.map((p) => (
                        <option key={p.id} value={p.id}>
                          {p.display_name}
                        </option>
                      ))}
                    </select>
                  </label>
                </div>
                {matchTable(
                  data.matches.filter(
                    (m) => filter === "all" || m.user_id === filter,
                  ),
                )}
              </section>
            </>
          ) : data && page === "ranking" ? (
            <>
              <PageTitle
                eyebrow="EUER VERGLEICH"
                title="Die Rangliste"
                description="Alle bestätigten Truhen. Euer gemeinsamer Fortschritt."
              />
              <section className="panel ranking-full">
                <PanelHeader
                  title="Euer privater Kreis"
                  description="Sortiert nach allen erfassten Truhen"
                />
                {sorted.map((p, i) => (
                  <button
                    className="leader-row"
                    key={p.id}
                    onClick={() => {
                      setProfile(p.id);
                      setPage("profile");
                    }}
                  >
                    <span className="rank-position">
                      {String(i + 1).padStart(2, "0")}
                    </span>
                    <span className="avatar">{p.display_name[0]}</span>
                    <span className="leader-name">
                      {p.display_name}
                      <small>{state(p)}</small>
                    </span>
                    <span>
                      <strong>{fmt.format(p.total_chests)}</strong>
                      <small>Truhen</small>
                    </span>
                    <span>
                      <strong>{fmt.format(p.average)}</strong>
                      <small>Ø / Match</small>
                    </span>
                    <span>
                      <strong>{p.completed_matches}</strong>
                      <small>Matches</small>
                    </span>
                    <ChevronRight size={18} />
                  </button>
                ))}
              </section>
            </>
          ) : data && page === "profile" ? (
            <>
              <PageTitle
                eyebrow="SPIELERPROFIL"
                title={selected?.display_name ?? "Profil"}
                description={
                  selected
                    ? "Dabei seit " +
                      new Date(selected.created_at).toLocaleDateString("de-DE")
                    : "Dein Konto"
                }
                action={
                  selected?.id === session.id ? (
                    <button onClick={() => setModal("password")}>
                      <KeyRound size={16} />
                      Passwort ändern
                    </button>
                  ) : undefined
                }
              />
              <div className="profile-intro panel">
                <span className="avatar large">
                  {selected?.display_name[0]}
                </span>
                <div>
                  <h2>{selected?.display_name}</h2>
                  <p>
                    <i
                      className={"status-dot " + selected?.state.toLowerCase()}
                    />
                    {selected && state(selected)}
                  </p>
                </div>
                <span className="badge">
                  {selected?.id === session.id
                    ? "Dein Profil"
                    : "Euer privater Kreis"}
                </span>
              </div>
              {stats(selected)}
              <section className="panel">
                <PanelHeader
                  title="Truhen pro Match"
                  description="Letzte 10 abgeschlossene Runden"
                />
                <Chart
                  points={profileMatches
                    .filter((m) => m.status === "COMPLETED")
                    .slice(0, 10)
                    .reverse()
                    .map((m, i) => ({
                      label: "M" + (i + 1),
                      value: m.chest_count,
                    }))}
                />
              </section>
              <section className="panel">
                <PanelHeader
                  title="Match-Verlauf"
                  description="Die letzten erfassten Runden"
                />
                {matchTable(profileMatches)}
              </section>
            </>
          ) : data && page === "admin" && session.role === "ADMIN" ? (
            <>
              <PageTitle
                eyebrow="ADMINISTRATION"
                title="Euer Kreis. Deine Verwaltung."
                description="Konten, Datensätze und nachvollziehbare Änderungen."
                action={
                  <button
                    onClick={() => setModal("user")}
                    disabled={adminUsers.filter((u) => u.enabled).length >= 4}
                  >
                    <Plus size={16} />
                    Spieler hinzufügen
                  </button>
                }
              />
              <div className="admin-summary">
                <ShieldCheck size={18} />
                Du verwaltest {adminUsers.filter((u) => u.enabled).length} von
                maximal 4 aktiven Konten.
              </div>
              <section className="panel">
                <PanelHeader
                  title="Spieler verwalten"
                  description="Startpasswörter müssen bei der ersten Anmeldung geändert werden."
                />
                {adminUsers.map((u) => (
                  <div className="admin-user" key={u.id}>
                    <span className="avatar">{u.displayName[0]}</span>
                    <div>
                      <b>{u.displayName}</b>
                      <small>
                        {u.username} ·{" "}
                        {u.role === "ADMIN"
                          ? "Admin"
                          : u.enabled
                            ? "Aktiv"
                            : "Deaktiviert"}
                      </small>
                    </div>
                    <button
                      aria-label={
                        "Anzeigename von " + u.displayName + " ändern"
                      }
                      onClick={() => setModal("rename:" + u.id)}
                    >
                      <Pencil size={15} />
                    </button>
                    <button
                      disabled={u.id === session.id}
                      onClick={() => setModal("reset:" + u.id)}
                    >
                      <KeyRound size={15} />
                      <span>Zurücksetzen</span>
                    </button>
                    {u.role !== "ADMIN" && (
                      <button
                        onClick={() =>
                          void perform(async () => {
                            await api("admin/users/" + u.id, "PATCH", {
                              enabled: !u.enabled,
                            });
                            await loadAdmin();
                            await load();
                          })
                        }
                      >
                        {u.enabled ? "Deaktivieren" : "Aktivieren"}
                      </button>
                    )}
                  </div>
                ))}
              </section>
              <section className="panel">
                <PanelHeader
                  title="Statistiken verwalten"
                  description="Matches und Ereignisse lassen sich in den Match-Details bearbeiten."
                />
                <div className="button-row">
                  <button onClick={() => setModal("match")}>
                    <Plus size={16} />
                    Match anlegen
                  </button>
                  <button onClick={() => navigate("matches")}>
                    <Gamepad2 size={16} />
                    Matches öffnen
                  </button>
                  <button
                    onClick={() =>
                      void perform(async () => {
                        const output = await api("admin/export");
                        const link = document.createElement("a");
                        link.href = URL.createObjectURL(
                          new Blob([JSON.stringify(output, null, 2)], {
                            type: "application/json",
                          }),
                        );
                        link.download = "chesttracker-export.json";
                        link.click();
                        URL.revokeObjectURL(link.href);
                      })
                    }
                  >
                    <Download size={16} />
                    JSON exportieren
                  </button>
                </div>
              </section>
              <section className="panel">
                <PanelHeader
                  title="Änderungsprotokoll"
                  description="Wer hat was und wann geändert?"
                  right={
                    <button
                      aria-label="Protokoll aktualisieren"
                      onClick={() => void loadAdmin()}
                    >
                      <RefreshCw size={15} />
                    </button>
                  }
                />
                {!audit.length ? (
                  <Empty
                    title="Noch keine Änderungen"
                    text="Administrative Änderungen erscheinen hier automatisch."
                    icon={ShieldCheck}
                  />
                ) : (
                  <div className="audit-list">
                    {audit.map((a) => (
                      <details key={a.id}>
                        <summary>
                          <span>
                            {a.admin_name} · {a.action} {a.target_type}
                          </span>
                          <small>{time(a.created_at)}</small>
                        </summary>
                        <pre>
                          {JSON.stringify(
                            { vorher: a.before_json, nachher: a.after_json },
                            null,
                            2,
                          )}
                        </pre>
                      </details>
                    ))}
                  </div>
                )}
              </section>
            </>
          ) : null}
          <footer className="page-footer">
            <span>
              <Box size={13} />
              ChestTracker <span className="footer-divider">/</span> Privater
              Workspace
            </span>
            <span>Keine Bildaufnahmen auf dem Server.</span>
          </footer>
        </main>
      </div>
      {modal && (
        <div
          className="modal-backdrop"
          onClick={() => {
            setModal(null);
            setError("");
          }}
        >
          <section
            className="modal"
            role="dialog"
            aria-modal="true"
            aria-label="ChestTracker Dialog"
            onClick={(e) => e.stopPropagation()}
          >
            <button
              className="modal-close icon-button"
              aria-label="Dialog schließen"
              onClick={() => {
                setModal(null);
                setError("");
              }}
            >
              <X size={20} />
            </button>
            {errorBox}
            {modal === "password" ? (
              <>
                <h2>Passwort ändern</h2>
                {passwordForm}
              </>
            ) : modal === "download" ? (
              <>
                <span className="auth-icon">
                  <Download />
                </span>
                <h2>Dein Client für Windows.</h2>
                <p>
                  Öffne das private GitHub-Release und lade{" "}
                  <b>ChestTracker-win-x64.zip</b> herunter. Entpacke es und
                  starte <b>ChestTracker.exe</b>.
                </p>
                <ol className="setup-steps">
                  <li>
                    API-Adresse auf{" "}
                    <code>
                      {typeof window !== "undefined"
                        ? window.location.origin
                        : ""}
                    </code>{" "}
                    setzen.
                  </li>
                  <li>Mit deinem ChestTracker-Konto anmelden.</li>
                  <li>
                    Offline-Replay wählen. Mit <kbd>O</kbd> das Overlay ein- und
                    ausblenden.
                  </li>
                </ol>
                <div className="info-box">
                  Live-Erkennung ist unvalidiert und bleibt deaktiviert. Für
                  einen echten Einsatz fehlen geprüfte Spielaufnahmen und eine
                  Kompatibilitätsprüfung.
                </div>
                <a
                  className="primary"
                  href="https://github.com/Fufi1925/chesttracker/releases"
                  target="_blank"
                  rel="noreferrer"
                >
                  Privates Release öffnen
                  <ArrowUpRight size={16} />
                </a>
              </>
            ) : modal === "user" ? (
              <>
                <h2>Spieler hinzufügen</h2>
                <p>Ein Startpasswort wird automatisch erstellt.</p>
                <form
                  className="form"
                  onSubmit={(e) => {
                    e.preventDefault();
                    const f = new FormData(e.currentTarget);
                    void perform(async () => {
                      const r = await api("admin/users", "POST", {
                        username: f.get("username"),
                        displayName: f.get("displayName"),
                      });
                      setNotice("Konto angelegt.");
                      setModal("credential:" + r.initialPassword);
                      await loadAdmin();
                      await load();
                    });
                  }}
                >
                  <label>
                    Benutzername
                    <input
                      name="username"
                      pattern="[A-Za-z0-9_-]{2,32}"
                      required
                    />
                  </label>
                  <label>
                    Anzeigename
                    <input name="displayName" maxLength={40} required />
                  </label>
                  <button className="primary" disabled={working}>
                    Konto erstellen
                    <Plus size={16} />
                  </button>
                </form>
              </>
            ) : modal.startsWith("credential:") ? (
              <>
                <h2>Einmaliges Startpasswort</h2>
                <p>
                  Gib dieses Passwort sicher an den Spieler weiter. Es wird nach
                  dem Schließen nicht erneut angezeigt.
                </p>
                <code className="credential">{modal.slice(11)}</code>
                <button className="primary" onClick={() => setModal(null)}>
                  Sicher übernommen
                  <Check size={16} />
                </button>
              </>
            ) : modal.startsWith("reset:") ? (
              <>
                <h2>Passwort zurücksetzen?</h2>
                <p>
                  Alle Sitzungen des Kontos werden beendet. Ein neues
                  Startpasswort wird erzeugt.
                </p>
                <button
                  className="primary"
                  disabled={working}
                  onClick={() =>
                    void perform(async () => {
                      const r = await api(
                        "admin/users/" + modal.slice(6),
                        "PATCH",
                        { resetPassword: true },
                      );
                      if (modal.slice(6) === session.id) {
                        setSession(null);
                        setNotice("");
                      }
                      setModal("credential:" + r.initialPassword);
                      await loadAdmin();
                    })
                  }
                >
                  Jetzt zurücksetzen
                  <KeyRound size={16} />
                </button>
              </>
            ) : modal.startsWith("rename:") ? (
              <>
                <h2>Anzeigename ändern</h2>
                <form
                  className="form"
                  onSubmit={(e) => {
                    e.preventDefault();
                    const f = new FormData(e.currentTarget);
                    void perform(async () => {
                      await api("admin/users/" + modal.slice(7), "PATCH", {
                        displayName: f.get("displayName"),
                      });
                      setModal(null);
                      await loadAdmin();
                      await load();
                    });
                  }}
                >
                  <label>
                    Anzeigename
                    <input
                      name="displayName"
                      defaultValue={
                        adminUsers.find((u) => u.id === modal.slice(7))
                          ?.displayName
                      }
                      maxLength={40}
                      required
                    />
                  </label>
                  <button className="primary" disabled={working}>
                    Speichern
                  </button>
                </form>
              </>
            ) : modal === "deleteMatch" || modal.startsWith("deleteEvent:") ? (
              <>
                <h2>
                  {modal === "deleteMatch"
                    ? "Match mit allen Ereignissen löschen?"
                    : "Ereignis löschen?"}
                </h2>
                <p>
                  Diese Änderung wird protokolliert. Die Statistik wird sofort
                  aus den verbleibenden Ereignissen neu berechnet.
                </p>
                <button
                  className="primary danger-solid"
                  disabled={working}
                  onClick={() =>
                    void perform(async () => {
                      await api(
                        modal === "deleteMatch"
                          ? "admin/matches/" + detail?.match.id
                          : "admin/events/" + modal.slice(12),
                        "DELETE",
                        {},
                      );
                      setModal(null);
                      if (modal === "deleteMatch") setDetail(null);
                      else if (detail)
                        setDetail(await api("matches/" + detail.match.id));
                      await load();
                    })
                  }
                >
                  Endgültig löschen
                  <Trash2 size={16} />
                </button>
              </>
            ) : modal === "match" || modal === "editMatch" ? (
              <>
                <h2>
                  {modal === "match" ? "Match anlegen" : "Match bearbeiten"}
                </h2>
                <form
                  className="form"
                  onSubmit={(e) => {
                    e.preventDefault();
                    const f = new FormData(e.currentTarget);
                    void perform(async () => {
                      const input = {
                        id: crypto.randomUUID(),
                        userId: f.get("userId"),
                        startedAt: new Date(
                          f.get("startedAt") as string,
                        ).toISOString(),
                        endedAt: f.get("endedAt")
                          ? new Date(f.get("endedAt") as string).toISOString()
                          : null,
                        status: f.get("status"),
                      };
                      await api(
                        modal === "match"
                          ? "admin/matches"
                          : "admin/matches/" + detail?.match.id,
                        modal === "match" ? "POST" : "PATCH",
                        input,
                      );
                      setModal(null);
                      await load();
                      if (detail)
                        setDetail(await api("matches/" + detail.match.id));
                    });
                  }}
                >
                  {modal === "match" && (
                    <label>
                      Spieler
                      <select name="userId">
                        {data?.users.map((u) => (
                          <option key={u.id} value={u.id}>
                            {u.display_name}
                          </option>
                        ))}
                      </select>
                    </label>
                  )}
                  <label>
                    Beginn
                    <input
                      name="startedAt"
                      type="datetime-local"
                      defaultValue={
                        modal === "editMatch"
                          ? localDate(detail?.match.started_at)
                          : localDate(new Date().toISOString())
                      }
                      required
                    />
                  </label>
                  <label>
                    Ende
                    <input
                      name="endedAt"
                      type="datetime-local"
                      defaultValue={
                        modal === "editMatch"
                          ? localDate(detail?.match.ended_at)
                          : ""
                      }
                    />
                  </label>
                  <label>
                    Status
                    <select
                      name="status"
                      defaultValue={
                        modal === "editMatch"
                          ? detail?.match.status
                          : "INCOMPLETE"
                      }
                    >
                      <option value="INCOMPLETE">Unvollständig</option>
                      <option value="COMPLETED">Abgeschlossen</option>
                      <option value="ACTIVE">Aktiv</option>
                    </select>
                  </label>
                  <button className="primary" disabled={working}>
                    Match speichern
                    <Check size={16} />
                  </button>
                </form>
              </>
            ) : modal === "event" || modal.startsWith("editEvent:") ? (
              <>
                <h2>
                  {modal === "event"
                    ? "Ereignis anlegen"
                    : "Ereigniszeit ändern"}
                </h2>
                <form
                  className="form"
                  onSubmit={(e) => {
                    e.preventDefault();
                    const f = new FormData(e.currentTarget);
                    void perform(async () => {
                      const occurredAt = new Date(
                        f.get("occurredAt") as string,
                      ).toISOString();
                      await api(
                        modal === "event"
                          ? "admin/events"
                          : "admin/events/" + modal.slice(10),
                        modal === "event" ? "POST" : "PATCH",
                        modal === "event"
                          ? {
                              clientEventId: crypto.randomUUID(),
                              matchId: detail?.match.id,
                              occurredAt,
                              confidence: 1,
                              detectorVersion: "admin-correction",
                            }
                          : { occurredAt },
                      );
                      setModal(null);
                      if (detail)
                        setDetail(await api("matches/" + detail.match.id));
                      await load();
                    });
                  }}
                >
                  <label>
                    Zeitpunkt
                    <input
                      name="occurredAt"
                      type="datetime-local"
                      step="1"
                      defaultValue={localDate(
                        modal === "event"
                          ? detail?.match.started_at
                          : detail?.events.find((e) => e.id === modal.slice(10))
                              ?.occurred_at,
                      )}
                      required
                    />
                  </label>
                  <button className="primary" disabled={working}>
                    Speichern
                    <Check size={16} />
                  </button>
                </form>
              </>
            ) : null}
          </section>
        </div>
      )}
    </div>
  );
}
function localDate(d?: string | null) {
  if (!d) return "";
  const t = new Date(d);
  return new Date(t.getTime() - t.getTimezoneOffset() * 60000)
    .toISOString()
    .slice(0, 19);
}
function PageTitle({
  eyebrow,
  title,
  description,
  action,
}: {
  eyebrow: string;
  title: string;
  description: string;
  action?: React.ReactNode;
}) {
  return (
    <div className="page-title">
      <div>
        <p className="eyebrow">{eyebrow}</p>
        <h1>{title}</h1>
        <p>{description}</p>
      </div>
      {action}
    </div>
  );
}
function PanelHeader({
  title,
  description,
  right,
}: {
  title: string;
  description: string;
  right?: React.ReactNode;
}) {
  return (
    <div className="panel-header">
      <div>
        <h2>{title}</h2>
        <p>{description}</p>
      </div>
      {right}
    </div>
  );
}
function Empty({
  title,
  text,
  icon: Icon,
}: {
  title: string;
  text: string;
  icon: typeof Box;
}) {
  return (
    <div className="empty">
      <span>
        <Icon size={24} />
      </span>
      <h3>{title}</h3>
      <p>{text}</p>
    </div>
  );
}
