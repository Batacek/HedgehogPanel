/* Generated from JSX via Babel (preset: react); this file is the source of record. */
/* App chrome — sidebar + topbar.
   - Sidebar collapse button lives in top-right corner of the sidebar
   - Admin section is hidden entirely for non-admin viewers
   - Search/Bell carry "pending backend" tooltips */

const NAV_WORKSPACE = [{
  key: "home",
  icon: "home",
  tKey: "nav.home"
}, {
  key: "servers",
  icon: "server",
  tKey: "nav.servers"
}, {
  key: "services",
  icon: "service",
  tKey: "nav.services"
}, {
  key: "nodes",
  icon: "node",
  tKey: "nav.nodes"
}];
const NAV_ADMIN = [{
  key: "admin.users",
  icon: "users",
  tKey: "nav.users"
}, {
  key: "admin.groups",
  icon: "shield",
  tKey: "nav.groups"
}, {
  key: "admin.perms",
  icon: "key",
  tKey: "nav.perms"
}, {
  key: "admin.audit",
  icon: "info",
  tKey: "nav.audit"
}, {
  key: "admin.plugins",
  icon: "rocket",
  tKey: "nav.plugins"
}, {
  key: "admin.settings",
  icon: "settings",
  tKey: "nav.settings"
}];
const Sidebar = ({
  active,
  onNav,
  sidebarMode,
  onToggleSidebar,
  isAdmin
}) => {
  const t = useT();
  const counts = {
    servers: SERVERS.length,
    services: SERVICES.length,
    nodes: NODES.length,
    "admin.users": USERS.length,
    "admin.groups": GROUPS.length
  };
  return /*#__PURE__*/React.createElement("aside", {
    className: "sidebar"
  }, /*#__PURE__*/React.createElement("div", {
    className: "sb-brand"
  }, /*#__PURE__*/React.createElement("div", {
    className: "brand-mark"
  }, "H."), sidebarMode !== "icons" && /*#__PURE__*/React.createElement("div", {
    className: "brand-name"
  }, "Hedgehog", /*#__PURE__*/React.createElement("small", null, "Panel \xB7 v1.2")), /*#__PURE__*/React.createElement("button", {
    className: "sb-toggle",
    onClick: onToggleSidebar,
    title: sidebarMode === "icons" ? t("side.expand") : t("side.collapse"),
    "aria-label": sidebarMode === "icons" ? t("side.expand") : t("side.collapse")
  }, /*#__PURE__*/React.createElement(Icon, {
    name: sidebarMode === "icons" ? "chevron_right" : "chevron_left",
    size: 13
  }))), /*#__PURE__*/React.createElement("nav", {
    className: "sb-nav"
  }, /*#__PURE__*/React.createElement("div", {
    className: "sb-group"
  }, /*#__PURE__*/React.createElement("div", {
    className: "sb-heading"
  }, t("nav.workspace")), NAV_WORKSPACE.map(item => /*#__PURE__*/React.createElement(SbItem, {
    key: item.key,
    item: item,
    t: t,
    active: active === item.key || active === "server-detail" && item.key === "servers",
    badge: counts[item.key],
    onNav: onNav
  }))), isAdmin && /*#__PURE__*/React.createElement("div", {
    className: "sb-group"
  }, /*#__PURE__*/React.createElement("div", {
    className: "sb-heading"
  }, t("nav.admin")), NAV_ADMIN.map(item => /*#__PURE__*/React.createElement(SbItem, {
    key: item.key,
    item: item,
    t: t,
    active: active === item.key,
    badge: counts[item.key],
    onNav: onNav
  })))), /*#__PURE__*/React.createElement("div", {
    className: "sb-foot"
  }, /*#__PURE__*/React.createElement("div", {
    className: "sb-foot-row"
  }, /*#__PURE__*/React.createElement("span", {
    className: "sb-status-dot"
  }), /*#__PURE__*/React.createElement("span", {
    className: "sb-foot-text"
  }, t("nav.allOk")))));
};
const SbItem = ({
  item,
  t,
  active,
  badge,
  onNav
}) => /*#__PURE__*/React.createElement("button", {
  className: "sb-item " + (active ? "active" : ""),
  onClick: () => onNav(item.key),
  title: t(item.tKey)
}, /*#__PURE__*/React.createElement(Icon, {
  name: item.icon
}), /*#__PURE__*/React.createElement("span", null, t(item.tKey)), badge != null && /*#__PURE__*/React.createElement("span", {
  className: "badge"
}, badge));
const Topbar = ({
  crumbs = [],
  onNav,
  user
}) => {
  const t = useT();
  const [open, setOpen] = React.useState(false);
  return /*#__PURE__*/React.createElement("header", {
    className: "topbar"
  }, /*#__PURE__*/React.createElement("div", {
    className: "topbar-title"
  }, crumbs.map((c, i) => /*#__PURE__*/React.createElement(React.Fragment, {
    key: i
  }, i > 0 && /*#__PURE__*/React.createElement("span", {
    className: "sep"
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "chevron_right",
    size: 12
  })), /*#__PURE__*/React.createElement("span", {
    className: i === crumbs.length - 1 ? "" : "crumb"
  }, c)))), /*#__PURE__*/React.createElement("div", {
    className: "topbar-spacer"
  }), /*#__PURE__*/React.createElement("div", {
    className: "search",
    role: "search",
    title: t("top.search_tooltip")
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "search",
    size: 14
  }), /*#__PURE__*/React.createElement("input", {
    placeholder: t("top.search"),
    disabled: true
  }), /*#__PURE__*/React.createElement("kbd", null, "\u2318K")), /*#__PURE__*/React.createElement("button", {
    className: "icon-btn",
    title: t("top.notif_tooltip"),
    disabled: true,
    style: {
      opacity: 0.8
    }
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "bell",
    size: 16
  }), /*#__PURE__*/React.createElement("span", {
    className: "dot"
  })), /*#__PURE__*/React.createElement("div", {
    style: {
      position: "relative"
    }
  }, /*#__PURE__*/React.createElement("button", {
    className: "user-chip",
    onClick: () => setOpen(o => !o),
    "aria-expanded": open
  }, /*#__PURE__*/React.createElement("div", {
    className: "avatar"
  }, (user?.firstname?.[0] || "U") + (user?.lastname?.[0] || "")), /*#__PURE__*/React.createElement("div", {
    style: {
      display: "flex",
      flexDirection: "column",
      lineHeight: 1.1,
      alignItems: "flex-start"
    }
  }, /*#__PURE__*/React.createElement("span", {
    className: "name"
  }, user?.firstname, " ", user?.lastname), /*#__PURE__*/React.createElement("span", {
    className: "role"
  }, user?.isAdmin ? t("top.role.admin") : t("top.role.user"))), /*#__PURE__*/React.createElement(Icon, {
    name: "chevron_down",
    size: 14
  })), open && /*#__PURE__*/React.createElement("div", {
    style: {
      position: "absolute",
      top: "calc(100% + 8px)",
      right: 0,
      minWidth: 220,
      padding: 6,
      background: "var(--surface)",
      border: "1px solid var(--line-strong)",
      borderRadius: 10,
      boxShadow: "var(--shadow-pop)",
      zIndex: 20
    },
    onMouseLeave: () => setOpen(false)
  }, /*#__PURE__*/React.createElement(MenuItem, {
    icon: "settings",
    label: t("top.account"),
    onClick: () => {
      setOpen(false);
      onNav?.("account");
    }
  }), /*#__PURE__*/React.createElement("div", {
    style: {
      height: 1,
      background: "var(--line)",
      margin: "4px 0"
    }
  }), /*#__PURE__*/React.createElement(MenuItem, {
    icon: "log_out",
    label: t("top.signout"),
    tone: "danger",
    onClick: () => onNav?.("login")
  }))));
};
const MenuItem = ({
  icon,
  label,
  tone,
  onClick
}) => /*#__PURE__*/React.createElement("button", {
  onClick: onClick,
  style: {
    display: "flex",
    alignItems: "center",
    gap: 10,
    width: "100%",
    padding: "8px 10px",
    background: "transparent",
    border: 0,
    borderRadius: 6,
    color: tone === "danger" ? "var(--danger-2)" : "var(--text)",
    fontSize: 13,
    textAlign: "left"
  },
  onMouseEnter: e => e.currentTarget.style.background = tone === "danger" ? "var(--danger-bg)" : "var(--surface-2)",
  onMouseLeave: e => e.currentTarget.style.background = "transparent"
}, /*#__PURE__*/React.createElement(Icon, {
  name: icon,
  size: 15
}), label);
Object.assign(window, {
  Sidebar,
  Topbar
});