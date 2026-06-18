const NOW = Date.now();
const min = (m) => new Date(NOW - m * 60_000);
const hr  = (h) => new Date(NOW - h * 60 * 60_000);
const day = (d) => new Date(NOW - d * 86_400_000);

const SERVICE_TYPES = {
  0: { label: "Minecraft", icon: "rocket"   },
  1: { label: "Web",       icon: "globe"    },
  2: { label: "Database",  icon: "database" },
  3: { label: "Worker",    icon: "service"  },
  4: { label: "Custom",    icon: "terminal" },
};

const EVENT_DECORATION = {
  login_success:        { ico: "log_in",   tone: "ok"    },
  login_failed:         { ico: "lock",     tone: "danger"},
  logout:               { ico: "log_out",  tone: "ok"    },
  service_start:        { ico: "play",     tone: "ok"    },
  service_stop:         { ico: "stop",     tone: "warn"  },
  server_warning:       { ico: "alert",    tone: "warn"  },
  node_offline:         { ico: "power",    tone: "danger"},
  user_created:         { ico: "user",     tone: "ok"    },
  password_change:      { ico: "key",      tone: "ok"    },
  "User.Login.Success": { ico: "log_in",   tone: "ok"    },
  "User.Login.Failed":  { ico: "lock",     tone: "danger"},
  "User.Login.Blocked": { ico: "lock",     tone: "danger"},
  "User.Logout":        { ico: "log_out",  tone: "ok"    },
  "User.Created":       { ico: "user",     tone: "ok"    },
  "User.Deleted":       { ico: "trash",    tone: "danger"},
  "User.Unlocked":      { ico: "check",    tone: "ok"    },
};

const EVENT_LABELS = {
  login_success:        "logged in",
  login_failed:         "failed login attempt",
  logout:               "logged out",
  service_start:        "started service",
  service_stop:         "stopped service",
  server_warning:       "server warning",
  node_offline:         "node offline",
  user_created:         "created user",
  password_change:      "changed password",
  "User.Login.Success": "logged in",
  "User.Login.Failed":  "failed login attempt",
  "User.Login.Blocked": "blocked login attempt",
  "User.Logout":        "logged out",
  "User.Created":       "created user",
  "User.Deleted":       "deleted user",
  "User.Unlocked":      "unlocked account",
};

const fmtAgo = (date) => {
  const now = Date.now();
  let d;
  if (typeof date === "number")      d = date;
  else if (typeof date === "string") d = new Date(date).getTime();
  else if (date instanceof Date)     d = date.getTime();
  else                               d = now;
  const s = Math.max(1, Math.floor((now - d) / 1000));
  if (s < 60)    return s + "s";
  if (s < 3600)  return Math.floor(s / 60) + "m";
  if (s < 86400) return Math.floor(s / 3600) + "h";
  return Math.floor(s / 86400) + "d";
};

const fmtAbs = (date) => {
  const d = (date instanceof Date) ? date : new Date(date);
  return d.toLocaleString("cs-CZ", { dateStyle: "medium", timeStyle: "short" });
};

/* Map backend ServerStatus enum values to frontend status strings. */
const SERVER_STATUS_MAP = {
  online:      "running",
  offline:     "stopped",
  maintenance: "warning",
  unknown:     "stopped",
};

/* Live globals — start empty, populated by __hh_load_data() before first render. */
let USERS             = [];
let GROUPS            = [];
let SERVERS           = [];
let NODES             = [];
let SERVICES          = [];
let EVENTS            = [];
let PERMISSIONS       = [];
let PERMISSION_GRANTS = {};
let CURRENT_USER      = null;

/* Fetch all panel data from the Hedgehog Panel backend API. */
window.__hh_load_data = async () => {
  const csrfToken = (document.cookie.split("; ")
    .find(r => r.startsWith("XSRF-TOKEN=")) || "=")
    .split("=").slice(1).join("=");

  const hdrs = { "X-CSRF-Token": csrfToken };
  const get  = (url) => fetch(url, { credentials: "same-origin", headers: hdrs });

  const [meR, srvR, nodeR, usrR] = await Promise.allSettled([
    get("/api/me"),
    get("/api/servers"),
    get("/api/nodes"),
    get("/api/admin/users"),
  ]);

  /* /api/me — current user identity */
  if (meR.status === "fulfilled" && meR.value.ok) {
    CURRENT_USER = await meR.value.json();
  }

  /* /api/admin/users (admin only) */
  if (usrR.status === "fulfilled" && usrR.value.ok) {
    const raw = await usrR.value.json();
    USERS = raw.map(u => ({
      uuid:       u.guid,
      username:   u.username,
      email:      u.email || "",
      firstname:  u.firstName || "",
      middlename: u.middleName || "",
      lastname:   u.lastName || "",
      isAdmin:    u.isAdmin,
      group:      u.highestPriorityGroup || "default",
      createdAt:  new Date(),
    }));
  } else if (CURRENT_USER) {
    /* Non-admin: synthesize a single entry from /api/me */
    const parts = (CURRENT_USER.displayName || CURRENT_USER.username).split(" ");
    USERS = [{
      uuid:       "current",
      username:   CURRENT_USER.username,
      email:      "",
      firstname:  parts[0] || CURRENT_USER.username,
      middlename: "",
      lastname:   parts.slice(1).join(" ") || "",
      isAdmin:    CURRENT_USER.isAdmin,
      group:      CURRENT_USER.isAdmin ? "admin" : "default",
      createdAt:  new Date(),
    }];
  }

  /* /api/servers */
  if (srvR.status === "fulfilled" && srvR.value.ok) {
    const raw = await srvR.value.json();
    SERVERS = raw.map(s => {
      const rawStatus = (s.status || "unknown").toLowerCase();
      const status    = SERVER_STATUS_MAP[rawStatus] || rawStatus;
      return {
        uuid:        s.id,
        name:        s.name,
        kind:        "dedicated",
        parent:      null,
        ip:          "—",
        daemon_port: 0,
        node_uuid:   null,
        role:        (s.role || "viewer").toLowerCase(),
        status,
        cpu: 0, ram: 0, disk: 0,
        uptime:      "—",
        description: s.description || "",
      };
    });
  }

  /* /api/nodes */
  if (nodeR.status === "fulfilled" && nodeR.value.ok) {
    const raw = await nodeR.value.json();
    NODES = raw.map(n => ({
      uuid:        n.id,
      name:        n.name,
      server_uuid: null,
      ip:          n.ipAddress || "—",
      port:        n.port || 0,
      status:      (n.status != null ? String(n.status) : "unknown").toLowerCase(),
      lastSeen:    n.lastSeen ? new Date(n.lastSeen).getTime() : Date.now(),
      description: n.description || "",
      version:     "—",
    }));
  }

  /* Publish all globals to the window so components can read them. */
  Object.assign(window, {
    USERS, GROUPS, NODES, SERVERS, SERVICES, EVENTS, SERVICE_TYPES,
    EVENT_DECORATION, EVENT_LABELS, PERMISSIONS, PERMISSION_GRANTS,
    CURRENT_USER, fmtAgo, fmtAbs, NOW, min, hr, day,
  });
};

/* Publish statics immediately; dynamic data follows after __hh_load_data(). */
Object.assign(window, {
  USERS, GROUPS, NODES, SERVERS, SERVICES, EVENTS, SERVICE_TYPES,
  EVENT_DECORATION, EVENT_LABELS, PERMISSIONS, PERMISSION_GRANTS,
  CURRENT_USER, fmtAgo, fmtAbs, NOW, min, hr, day,
});
