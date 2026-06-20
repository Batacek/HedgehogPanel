/* Generated from JSX via Babel (preset: react); this file is the source of record. */
/* Dashboard / Home — server status grid and activity feed. */

const DashboardPage = ({
  user,
  onNav,
  onOpenServer,
  onRefresh,
  refreshing
}) => {
  const t = useT();
  return /*#__PURE__*/React.createElement("div", {
    className: "page"
  }, /*#__PURE__*/React.createElement("div", {
    className: "page-head"
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("h1", null, t("dash.greet"), ", ", user?.firstname || "uživateli", " \uD83D\uDC4B"), /*#__PURE__*/React.createElement("p", {
    className: "lede"
  }, t("dash.subtitle"), " \xB7 ", fmtAbs(new Date()))), /*#__PURE__*/React.createElement("div", {
    className: "page-actions"
  }, /*#__PURE__*/React.createElement("button", {
    className: "btn",
    onClick: () => onRefresh?.(),
    disabled: refreshing
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "refresh",
    size: 14,
    style: refreshing ? {
      animation: "spin 0.8s linear infinite"
    } : undefined
  }), t("btn.refresh")), /*#__PURE__*/React.createElement("button", {
    className: "btn primary",
    onClick: () => onNav?.("servers")
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "plus",
    size: 14
  }), t("btn.new_server")))), /*#__PURE__*/React.createElement("div", {
    className: "grid dash"
  }, /*#__PURE__*/React.createElement("div", {
    className: "card flush"
  }, /*#__PURE__*/React.createElement("div", {
    className: "card-head"
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("h3", null, t("dash.servers.title")), /*#__PURE__*/React.createElement("div", {
    className: "sub"
  }, t("dash.servers.sub"))), /*#__PURE__*/React.createElement("button", {
    className: "btn ghost sm",
    onClick: () => onNav?.("servers")
  }, t("dash.servers.all"), " ", /*#__PURE__*/React.createElement(Icon, {
    name: "chevron_right",
    size: 12
  }))), /*#__PURE__*/React.createElement("div", {
    className: "grid cols-2",
    style: {
      padding: 16
    }
  }, SERVERS.slice(0, 4).map(s => /*#__PURE__*/React.createElement("button", {
    key: s.uuid,
    className: "server-card",
    onClick: () => onOpenServer?.(s.uuid)
  }, /*#__PURE__*/React.createElement("div", {
    className: "row head"
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("div", {
    className: "name"
  }, s.name), /*#__PURE__*/React.createElement("div", {
    className: "host"
  }, s.ip !== "—" ? `${s.ip}:${s.daemon_port}` : s.description || s.name)), /*#__PURE__*/React.createElement(StatusPill, {
    status: s.status
  })), /*#__PURE__*/React.createElement(StatLine, {
    icon: "cpu",
    label: "CPU",
    value: s.cpu
  }), /*#__PURE__*/React.createElement(StatLine, {
    icon: "ram",
    label: "RAM",
    value: s.ram
  }), /*#__PURE__*/React.createElement(StatLine, {
    icon: "disk",
    label: "Disk",
    value: s.disk
  }), /*#__PURE__*/React.createElement("div", {
    style: {
      display: "flex",
      justifyContent: "space-between",
      fontSize: 11.5,
      color: "var(--text-mute)"
    }
  }, /*#__PURE__*/React.createElement(KindPill, {
    kind: s.kind
  }), /*#__PURE__*/React.createElement("span", null, /*#__PURE__*/React.createElement(Icon, {
    name: "clock",
    size: 11,
    style: {
      verticalAlign: "-2px"
    }
  }), " ", s.uptime)))), SERVERS.length === 0 && /*#__PURE__*/React.createElement("div", {
    style: {
      gridColumn: "1/-1"
    }
  }, /*#__PURE__*/React.createElement(EmptyState, {
    title: t("common.empty.servers"),
    icon: "server"
  })))), /*#__PURE__*/React.createElement("div", {
    className: "card flush"
  }, /*#__PURE__*/React.createElement("div", {
    className: "card-head"
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("h3", null, t("dash.activity.title")), /*#__PURE__*/React.createElement("div", {
    className: "sub"
  }, t("dash.activity.sub"))), /*#__PURE__*/React.createElement("button", {
    className: "btn ghost sm",
    onClick: () => onNav?.("admin.audit")
  }, t("dash.activity.all"), " ", /*#__PURE__*/React.createElement(Icon, {
    name: "chevron_right",
    size: 12
  }))), /*#__PURE__*/React.createElement("div", {
    className: "activity",
    style: {
      padding: "4px 16px 8px"
    }
  }, EVENTS.length === 0 ? /*#__PURE__*/React.createElement(EmptyState, {
    title: t("admin.audit.empty"),
    icon: "info"
  }) : EVENTS.slice(0, 7).map(ev => {
    const dec = EVENT_DECORATION[ev.type] || {
      ico: "info",
      tone: "default"
    };
    const label = t("ev." + ev.type, ev.type);
    return /*#__PURE__*/React.createElement("div", {
      className: "activity-item",
      key: ev.id
    }, /*#__PURE__*/React.createElement("div", {
      className: "dot " + dec.tone
    }, /*#__PURE__*/React.createElement(Icon, {
      name: dec.ico,
      size: 12
    })), /*#__PURE__*/React.createElement("div", {
      className: "body"
    }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("span", {
      className: "who"
    }, ev.user), " ", label), /*#__PURE__*/React.createElement("div", {
      className: "meta"
    }, ev.ip, " \xB7 ", ev.meta)), /*#__PURE__*/React.createElement("div", {
      className: "time"
    }, fmtAgo(ev.at)));
  })))), /*#__PURE__*/React.createElement("div", {
    className: "card flush",
    style: {
      marginTop: 16
    }
  }, /*#__PURE__*/React.createElement("div", {
    className: "card-head"
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("h3", null, t("dash.nodes.title")), /*#__PURE__*/React.createElement("div", {
    className: "sub"
  }, t("dash.nodes.sub"))), /*#__PURE__*/React.createElement("button", {
    className: "btn ghost sm",
    onClick: () => onNav?.("nodes")
  }, t("page.nodes.title"), " ", /*#__PURE__*/React.createElement(Icon, {
    name: "chevron_right",
    size: 12
  }))), NODES.length === 0 ? /*#__PURE__*/React.createElement(EmptyState, {
    title: t("common.empty.servers"),
    icon: "node"
  }) : /*#__PURE__*/React.createElement("table", {
    className: "table"
  }, /*#__PURE__*/React.createElement("thead", null, /*#__PURE__*/React.createElement("tr", null, /*#__PURE__*/React.createElement("th", null, "Node"), /*#__PURE__*/React.createElement("th", null, "Server"), /*#__PURE__*/React.createElement("th", null, t("common.address")), /*#__PURE__*/React.createElement("th", null, t("common.status")), /*#__PURE__*/React.createElement("th", null, t("common.last_seen")), /*#__PURE__*/React.createElement("th", null))), /*#__PURE__*/React.createElement("tbody", null, NODES.slice(0, 5).map(n => {
    const server = SERVERS.find(s => s.uuid === n.server_uuid);
    return /*#__PURE__*/React.createElement("tr", {
      key: n.uuid
    }, /*#__PURE__*/React.createElement("td", null, /*#__PURE__*/React.createElement("div", {
      className: "stack"
    }, /*#__PURE__*/React.createElement("strong", {
      style: {
        fontWeight: 600
      }
    }, n.name), /*#__PURE__*/React.createElement("span", {
      className: "sub"
    }, n.description))), /*#__PURE__*/React.createElement("td", null, server?.name || "—"), /*#__PURE__*/React.createElement("td", {
      className: "mono"
    }, n.ip, ":", n.port), /*#__PURE__*/React.createElement("td", null, /*#__PURE__*/React.createElement(StatusPill, {
      status: n.status
    })), /*#__PURE__*/React.createElement("td", {
      className: "mono"
    }, fmtAgo(n.lastSeen), " ago"), /*#__PURE__*/React.createElement("td", {
      className: "actions"
    }, /*#__PURE__*/React.createElement("button", {
      className: "btn ghost sm"
    }, /*#__PURE__*/React.createElement(Icon, {
      name: "more",
      size: 16
    }))));
  })))));
};
window.DashboardPage = DashboardPage;