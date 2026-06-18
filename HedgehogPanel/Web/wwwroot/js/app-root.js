/* Generated from JSX via Babel (preset: react); this file is the source of record. */
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
/* App root — routing, live data loading, real auth. */

const PALETTE_SWATCHES = {
  forest: ["#83b06a", "#3b5c2a", "#1a1d18"],
  cobalt: ["#7fa9f5", "#2849a3", "#161a22"],
  ember: ["#e89763", "#a14a1f", "#1f1a16"],
  violet: ["#c79bf2", "#6b3aa3", "#1c1820"]
};
const PALETTE_KEYS = Object.keys(PALETTE_SWATCHES);
const PALETTE_OPTIONS = PALETTE_KEYS.map(k => PALETTE_SWATCHES[k]);

/*  Hash routing */
const VALID_PAGES = new Set(["home", "servers", "services", "nodes", "account", "admin.users", "admin.groups", "admin.perms", "admin.audit", "admin.plugins", "admin.settings"]);
const parseHash = () => {
  let raw = (window.location.hash || "").replace(/^#/, "");
  try {
    raw = decodeURIComponent(raw);
  } catch (e) {/* keep raw on malformed % */}
  if (raw.startsWith("server/")) {
    const id = raw.slice("server/".length);
    return id ? {
      page: "server-detail",
      serverId: id
    } : {
      page: "servers",
      serverId: null
    };
  }
  if (raw && raw !== "home" && VALID_PAGES.has(raw)) return {
    page: raw,
    serverId: null
  };
  return {
    page: "home",
    serverId: null
  };
};
const pageToHash = (page, serverId) => {
  if (page === "server-detail" && serverId) return "#server/" + encodeURIComponent(serverId);
  if (page === "home" || !VALID_PAGES.has(page)) return "";
  return "#" + page;
};
const App = () => {
  const [tw, setTweak] = useTweaks({
    "palette": "forest",
    "density": "comfortable"
  });
  const t = useT();
  const [lang, setLang] = useLang();
  const [dataLoaded, setDataLoaded] = React.useState(false);
  const [dataVersion, setDataVersion] = React.useState(0);
  const [refreshing, setRefreshing] = React.useState(false);
  const [page, setPage] = React.useState(() => parseHash().page);
  const [serverId, setServerId] = React.useState(() => parseHash().serverId);
  const [sidebarMode, setSidebarMode] = React.useState("expanded");
  const [profileEdits, setProfileEdits] = React.useState({});

  /* Load real data from backend on mount. */
  React.useEffect(() => {
    window.__hh_load_data().then(() => setDataLoaded(true)).catch(() => setDataLoaded(true)); // render with whatever loaded
  }, []);
  React.useEffect(() => {
    document.documentElement.dataset.palette = tw.palette;
    document.documentElement.dataset.density = tw.density;
  }, [tw.palette, tw.density]);

  /* Set default language on first load. */
  React.useEffect(() => {
    if (dataLoaded && !window.__hh_user_lang_set) {
      setLang("en");
      window.__hh_user_lang_set = true;
    }
  }, [dataLoaded]);

  /* Derive user: prefer full record from admin list, fall back to /api/me. */
  const currentUsername = window.CURRENT_USER?.username;
  const meFromList = USERS.find(u => u.username === currentUsername);
  const baseUser = meFromList || (window.CURRENT_USER ? {
    uuid: "current",
    username: window.CURRENT_USER.username,
    email: "",
    firstname: (window.CURRENT_USER.displayName || window.CURRENT_USER.username).split(" ")[0] || "",
    middlename: "",
    lastname: (window.CURRENT_USER.displayName || "").split(" ").slice(1).join(" ") || "",
    isAdmin: window.CURRENT_USER.isAdmin,
    group: window.CURRENT_USER.isAdmin ? "admin" : "default"
  } : {
    uuid: "unknown",
    username: "user",
    firstname: "User",
    middlename: "",
    lastname: "",
    isAdmin: false
  });
  const user = {
    ...baseUser,
    ...profileEdits
  };

  /* If non-admin lands on admin route, redirect to home. Wait for data so a
     deep-linked admin isn't bounced before CURRENT_USER is known. */
  React.useEffect(() => {
    if (dataLoaded && user && !user.isAdmin && page.startsWith("admin.")) setPage("home");
  }, [dataLoaded, user.isAdmin, page]);
  const firstHashSync = React.useRef(true);
  React.useEffect(() => {
    const target = pageToHash(page, serverId);
    if ((window.location.hash || "") === target) {
      firstHashSync.current = false;
      return;
    }
    const url = target || window.location.pathname + window.location.search;
    if (firstHashSync.current) window.history.replaceState(null, "", url);else window.history.pushState(null, "", url);
    firstHashSync.current = false;
  }, [page, serverId]);

  /* Back / forward buttons and manual hash edits drive the page state. */
  React.useEffect(() => {
    const sync = () => {
      const r = parseHash();
      setPage(r.page);
      setServerId(r.serverId);
    };
    window.addEventListener("popstate", sync);
    window.addEventListener("hashchange", sync);
    return () => {
      window.removeEventListener("popstate", sync);
      window.removeEventListener("hashchange", sync);
    };
  }, []);

  /* Refresh: reload all data from API and bump version to trigger re-renders. */
  const handleRefresh = React.useCallback(() => {
    if (refreshing) return;
    setRefreshing(true);
    window.__hh_load_data().then(() => setDataVersion(v => v + 1)).catch(() => setDataVersion(v => v + 1)).finally(() => setRefreshing(false));
  }, [refreshing]);
  const nav = key => {
    if (key === "login") {
      /* Real logout: POST /api/logout then redirect to login page. */
      const csrfToken = (document.cookie.split("; ").find(r => r.startsWith("XSRF-TOKEN=")) || "=").split("=").slice(1).join("=");
      fetch("/api/logout", {
        method: "POST",
        credentials: "same-origin",
        headers: {
          "X-CSRF-Token": csrfToken
        }
      }).finally(() => {
        window.location.href = "/html/login.html";
      });
      return;
    }
    setPage(key);
    setServerId(null);
  };
  const openServer = id => {
    setServerId(id);
    setPage("server-detail");
  };
  const toggleSidebar = () => setSidebarMode(m => m === "icons" ? "expanded" : "icons");

  /* Loading screen — shown until API data arrives. */
  if (!dataLoaded) {
    return /*#__PURE__*/React.createElement("div", {
      style: {
        minHeight: "100vh",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        background: "var(--bg)",
        flexDirection: "column",
        gap: 16
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        width: 40,
        height: 40,
        borderRadius: "50%",
        border: "3px solid var(--surface-2)",
        borderTopColor: "var(--accent)",
        animation: "spin 0.8s linear infinite"
      }
    }), /*#__PURE__*/React.createElement("div", {
      style: {
        color: "var(--text-mute)",
        fontSize: 13
      }
    }, "Loading\u2026"), /*#__PURE__*/React.createElement("style", null, "@keyframes spin { to { transform: rotate(360deg); } }"));
  }

  /* Build breadcrumbs based on active page. */
  const crumbs = (() => {
    const root = "Hedgehog Panel";
    if (page === "home") return [root, t("nav.home")];
    if (page === "servers") return [root, t("nav.servers")];
    if (page === "server-detail") return [root, t("nav.servers"), SERVERS.find(s => s.uuid === serverId)?.name || ""];
    if (page === "services") return [root, t("nav.services")];
    if (page === "nodes") return [root, t("nav.nodes")];
    if (page === "account") return [root, t("acct.title")];
    if (page === "admin.users") return [root, t("nav.admin"), t("nav.users")];
    if (page === "admin.groups") return [root, t("nav.admin"), t("nav.groups")];
    if (page === "admin.perms") return [root, t("nav.admin"), t("nav.perms")];
    if (page === "admin.audit") return [root, t("nav.admin"), t("nav.audit")];
    if (page === "admin.plugins") return [root, t("nav.admin"), t("nav.plugins")];
    if (page === "admin.settings") return [root, t("nav.admin"), t("nav.settings")];
    return [root];
  })();
  const refreshProps = {
    onRefresh: handleRefresh,
    refreshing,
    dataVersion
  };
  return /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("div", {
    className: "app",
    "data-sidebar": sidebarMode
  }, /*#__PURE__*/React.createElement(Sidebar, {
    active: page,
    onNav: nav,
    sidebarMode: sidebarMode,
    onToggleSidebar: toggleSidebar,
    isAdmin: !!user.isAdmin
  }), /*#__PURE__*/React.createElement("div", {
    style: {
      minWidth: 0
    }
  }, /*#__PURE__*/React.createElement(Topbar, {
    crumbs: crumbs,
    user: user,
    onNav: nav
  }), page === "home" && /*#__PURE__*/React.createElement(DashboardPage, _extends({
    user: user,
    onNav: nav,
    onOpenServer: openServer
  }, refreshProps)), page === "servers" && /*#__PURE__*/React.createElement(ServersPage, _extends({
    onOpenServer: openServer
  }, refreshProps)), page === "server-detail" && /*#__PURE__*/React.createElement(ServerDetailPage, {
    serverId: serverId,
    onBack: () => nav("servers")
  }), page === "services" && /*#__PURE__*/React.createElement(ServicesPage, _extends({
    onOpenServer: openServer
  }, refreshProps)), page === "nodes" && /*#__PURE__*/React.createElement(NodesPage, refreshProps), page === "account" && /*#__PURE__*/React.createElement(AccountPage, {
    user: user,
    onUpdateUser: p => setProfileEdits(prev => ({
      ...prev,
      ...p
    }))
  }), page === "admin.users" && user.isAdmin && /*#__PURE__*/React.createElement(AdminUsersPage, null), page === "admin.groups" && user.isAdmin && /*#__PURE__*/React.createElement(AdminGroupsPage, null), page === "admin.perms" && user.isAdmin && /*#__PURE__*/React.createElement(AdminPermissionsPage, null), page === "admin.audit" && user.isAdmin && /*#__PURE__*/React.createElement(AdminAuditPage, null), page === "admin.plugins" && user.isAdmin && /*#__PURE__*/React.createElement(AdminPluginsPage, null), page === "admin.settings" && user.isAdmin && /*#__PURE__*/React.createElement(AdminSettingsPage, {
    defaults: {
      palette: tw.palette,
      lang
    },
    onChangeDefaults: d => {
      if (d.palette) setTweak("palette", d.palette);
      if (d.lang) setLang(d.lang);
    }
  }))), /*#__PURE__*/React.createElement(TweaksUI, {
    tw: tw,
    setTweak: setTweak
  }));
};
const TweaksUI = ({
  tw,
  setTweak
}) => {
  const currentPaletteValue = PALETTE_SWATCHES[tw.palette] || PALETTE_OPTIONS[0];
  return /*#__PURE__*/React.createElement(TweaksPanel, {
    title: "Hedgehog \xB7 Tweaks"
  }, /*#__PURE__*/React.createElement(TweakSection, {
    label: "Barevn\xFD sm\u011Br"
  }, /*#__PURE__*/React.createElement(TweakColor, {
    label: "Akcent",
    value: currentPaletteValue,
    options: PALETTE_OPTIONS,
    onChange: v => {
      const idx = PALETTE_OPTIONS.findIndex(p => p[0] === v[0]);
      setTweak("palette", PALETTE_KEYS[idx] || "forest");
    }
  }), /*#__PURE__*/React.createElement("div", {
    style: {
      fontSize: 11.5,
      color: "#999",
      padding: "2px 12px 6px",
      textTransform: "capitalize"
    }
  }, tw.palette)), /*#__PURE__*/React.createElement(TweakSection, {
    label: "Hustota"
  }, /*#__PURE__*/React.createElement(TweakRadio, {
    label: "Layout density",
    value: tw.density,
    options: ["comfortable", "compact"],
    onChange: v => setTweak("density", v)
  })));
};
ReactDOM.createRoot(document.getElementById("root")).render(/*#__PURE__*/React.createElement(App, null));