/* Generated from JSX via Babel (preset: react); this file is the source of record. */
/* Server detail — overview / services / console / settings tabs. */

const ServerDetailPage = ({
  serverId,
  onBack
}) => {
  const t = useT();
  const server = SERVERS.find(s => s.uuid === serverId) || SERVERS[0];
  const node = NODES.find(n => n.uuid === server.node_uuid);
  const parent = server.parent && SERVERS.find(x => x.uuid === server.parent);
  const services = SERVICES.filter(sv => sv.server === server.uuid);
  const ownerUser = server && USERS.find(u => u.username === server.owner);
  const ownerName = ownerUser ? (ownerUser.firstname + " " + ownerUser.lastname).trim() : "";
  const ownerLabel = server && server.owner ? ownerName ? ownerName + " (" + server.owner + ")" : server.owner : "—";
  const [tab, setTab] = React.useState("overview");
  const [newSvcOpen, setNewSvcOpen] = React.useState(false);
  return /*#__PURE__*/React.createElement("div", {
    className: "page"
  }, /*#__PURE__*/React.createElement("button", {
    className: "btn ghost sm",
    onClick: onBack,
    style: {
      marginBottom: 14
    }
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "chevron_left",
    size: 14
  }), " ", t("btn.back_servers")), /*#__PURE__*/React.createElement("div", {
    className: "detail-head"
  }, /*#__PURE__*/React.createElement("div", {
    className: "id"
  }, server.name.split(" ").map(w => w[0]).slice(0, 2).join("")), /*#__PURE__*/React.createElement("div", {
    style: {
      flex: 1,
      minWidth: 0
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: "flex",
      alignItems: "center",
      gap: 10,
      flexWrap: "wrap"
    }
  }, /*#__PURE__*/React.createElement("h2", null, server.name), /*#__PURE__*/React.createElement(StatusPill, {
    status: server.status
  }), /*#__PURE__*/React.createElement(KindPill, {
    kind: server.kind
  }), /*#__PURE__*/React.createElement(RolePill, {
    role: server.role
  })), /*#__PURE__*/React.createElement("div", {
    className: "meta"
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: "var(--font-mono)"
    }
  }, server.ip, ":", server.daemon_port), /*#__PURE__*/React.createElement("span", null, "Node ", /*#__PURE__*/React.createElement("strong", {
    style: {
      color: "var(--text-dim)"
    }
  }, node?.name || "—")), parent && /*#__PURE__*/React.createElement("span", null, t("common.host"), " ", /*#__PURE__*/React.createElement("strong", {
    style: {
      color: "var(--text-dim)"
    }
  }, parent.name)), /*#__PURE__*/React.createElement("span", null, t("common.uptime"), " ", server.uptime)), /*#__PURE__*/React.createElement("p", {
    style: {
      margin: "8px 0 0",
      color: "var(--text-dim)",
      fontSize: 13
    }
  }, server.description)), /*#__PURE__*/React.createElement("div", {
    className: "actions"
  }, server.status === "running" ? /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("button", {
    className: "btn"
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "restart",
    size: 14
  }), "Restart"), /*#__PURE__*/React.createElement("button", {
    className: "btn danger"
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "stop",
    size: 12
  }), "Stop")) : /*#__PURE__*/React.createElement("button", {
    className: "btn primary"
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "play",
    size: 12
  }), "Start"), /*#__PURE__*/React.createElement("button", {
    className: "btn ghost"
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "more",
    size: 16
  })))), /*#__PURE__*/React.createElement("div", {
    className: "tabs",
    role: "tablist"
  }, /*#__PURE__*/React.createElement("button", {
    className: "tab " + (tab === "overview" ? "on" : ""),
    onClick: () => setTab("overview")
  }, t("tab.overview")), /*#__PURE__*/React.createElement("button", {
    className: "tab " + (tab === "services" ? "on" : ""),
    onClick: () => setTab("services")
  }, t("tab.services"), " ", /*#__PURE__*/React.createElement("span", {
    style: {
      color: "var(--text-mute)"
    }
  }, services.length)), /*#__PURE__*/React.createElement("button", {
    className: "tab " + (tab === "console" ? "on" : ""),
    onClick: () => setTab("console")
  }, t("tab.console")), /*#__PURE__*/React.createElement("button", {
    className: "tab " + (tab === "logs" ? "on" : ""),
    onClick: () => setTab("logs")
  }, t("tab.logs")), /*#__PURE__*/React.createElement("button", {
    className: "tab " + (tab === "settings" ? "on" : ""),
    onClick: () => setTab("settings")
  }, t("tab.settings"))), tab === "overview" && /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("div", {
    className: "grid kpi",
    style: {
      marginBottom: 14
    }
  }, /*#__PURE__*/React.createElement(KpiCard, {
    label: "CPU",
    value: `${server.cpu}`,
    unit: "%",
    spark: /*#__PURE__*/React.createElement(Sparkline, {
      seed: server.uuid + "c",
      tone: server.cpu > 85 ? "danger" : null
    })
  }), /*#__PURE__*/React.createElement(KpiCard, {
    label: "RAM",
    value: `${server.ram}`,
    unit: "%",
    spark: /*#__PURE__*/React.createElement(Sparkline, {
      seed: server.uuid + "r",
      tone: server.ram > 85 ? "danger" : null
    })
  }), /*#__PURE__*/React.createElement(KpiCard, {
    label: "Disk",
    value: `${server.disk}`,
    unit: "%",
    spark: /*#__PURE__*/React.createElement(Sparkline, {
      seed: server.uuid + "d"
    })
  }), /*#__PURE__*/React.createElement(KpiCard, {
    label: "Network",
    value: "0",
    unit: "MB/s",
    spark: /*#__PURE__*/React.createElement(Sparkline, {
      seed: server.uuid + "n"
    })
  })), /*#__PURE__*/React.createElement("div", {
    className: "grid dash"
  }, /*#__PURE__*/React.createElement("div", {
    className: "card flush"
  }, /*#__PURE__*/React.createElement("div", {
    className: "card-head"
  }, /*#__PURE__*/React.createElement("h3", null, t("detail.props"))), /*#__PURE__*/React.createElement("div", {
    style: {
      padding: "4px 16px 16px"
    }
  }, /*#__PURE__*/React.createElement(KV, {
    k: t("detail.uuid"),
    v: server.uuid + "-7c33-a401-8e22",
    mono: true
  }), /*#__PURE__*/React.createElement(KV, {
    k: t("detail.ip"),
    v: `${server.ip}:${server.daemon_port}`,
    mono: true
  }), /*#__PURE__*/React.createElement(KV, {
    k: t("detail.node"),
    v: `${node?.name || "—"} · ${node?.ip}:${node?.port}`,
    mono: true
  }), /*#__PURE__*/React.createElement(KV, {
    k: t("detail.created"),
    v: fmtAbs(day(120))
  }), /*#__PURE__*/React.createElement(KV, {
    k: t("detail.owner"),
    v: ownerLabel
  }), /*#__PURE__*/React.createElement(KV, {
    k: t("common.role"),
    v: /*#__PURE__*/React.createElement(RolePill, {
      role: server.role
    })
  }))), /*#__PURE__*/React.createElement("div", {
    className: "card flush"
  }, /*#__PURE__*/React.createElement("div", {
    className: "card-head"
  }, /*#__PURE__*/React.createElement("h3", null, t("detail.events"))), /*#__PURE__*/React.createElement("div", {
    className: "activity",
    style: {
      padding: "4px 16px 12px"
    }
  }, EVENTS.slice(0, 5).map(ev => {
    const dec = EVENT_DECORATION[ev.type] || {
      ico: "info",
      tone: "default"
    };
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
    }, ev.user), " ", t("ev." + ev.type)), /*#__PURE__*/React.createElement("div", {
      className: "meta"
    }, ev.meta)), /*#__PURE__*/React.createElement("div", {
      className: "time"
    }, fmtAgo(ev.at)));
  }))))), tab === "services" && /*#__PURE__*/React.createElement("div", {
    className: "card flush"
  }, /*#__PURE__*/React.createElement("div", {
    className: "card-head"
  }, /*#__PURE__*/React.createElement("h3", null, t("page.services.on_server")), /*#__PURE__*/React.createElement("button", {
    className: "btn sm",
    onClick: () => setNewSvcOpen(true)
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "plus",
    size: 12
  }), t("btn.new_service"))), services.length === 0 ? /*#__PURE__*/React.createElement(EmptyState, {
    title: t("common.empty.services"),
    body: t("page.services.empty"),
    icon: "service"
  }) : services.map(sv => {
    const type = SERVICE_TYPES[sv.type] || SERVICE_TYPES[4];
    return /*#__PURE__*/React.createElement("div", {
      className: "svc-row",
      key: sv.uuid
    }, /*#__PURE__*/React.createElement("div", {
      className: "ico"
    }, /*#__PURE__*/React.createElement(Icon, {
      name: type.icon,
      size: 14
    })), /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("div", {
      className: "name"
    }, sv.name), /*#__PURE__*/React.createElement("div", {
      className: "sub"
    }, sv.description, " \xB7 ", type.label, sv.port ? ` · :${sv.port}` : "")), /*#__PURE__*/React.createElement(StatusPill, {
      status: sv.status
    }), /*#__PURE__*/React.createElement("span", {
      className: "mono",
      style: {
        fontSize: 11.5,
        color: "var(--text-mute)"
      }
    }, type.label), /*#__PURE__*/React.createElement("div", {
      className: "actions"
    }, sv.status === "running" ? /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("button", {
      className: "btn ghost sm"
    }, /*#__PURE__*/React.createElement(Icon, {
      name: "restart",
      size: 12
    })), /*#__PURE__*/React.createElement("button", {
      className: "btn ghost sm danger"
    }, /*#__PURE__*/React.createElement(Icon, {
      name: "stop",
      size: 12
    }))) : /*#__PURE__*/React.createElement("button", {
      className: "btn ghost sm"
    }, /*#__PURE__*/React.createElement(Icon, {
      name: "play",
      size: 12
    })), /*#__PURE__*/React.createElement("button", {
      className: "btn ghost sm"
    }, /*#__PURE__*/React.createElement(Icon, {
      name: "more",
      size: 14
    }))));
  })), tab === "console" && /*#__PURE__*/React.createElement(ConsolePane, {
    server: server
  }), tab === "logs" && /*#__PURE__*/React.createElement("div", {
    className: "card"
  }, /*#__PURE__*/React.createElement("h3", {
    style: {
      marginTop: 0
    }
  }, t("logs.title")), /*#__PURE__*/React.createElement("p", {
    style: {
      color: "var(--text-mute)",
      fontSize: 13
    }
  }, t("logs.lede")), /*#__PURE__*/React.createElement(LogPane, null)), tab === "settings" && /*#__PURE__*/React.createElement("div", {
    className: "card"
  }, /*#__PURE__*/React.createElement("h3", {
    style: {
      marginTop: 0
    }
  }, t("detail.settings")), /*#__PURE__*/React.createElement("div", {
    className: "form-grid",
    style: {
      marginTop: 10
    }
  }, /*#__PURE__*/React.createElement("div", {
    className: "field"
  }, /*#__PURE__*/React.createElement("label", null, t("common.name")), /*#__PURE__*/React.createElement("input", {
    className: "input",
    defaultValue: server.name
  })), /*#__PURE__*/React.createElement("div", {
    className: "field"
  }, /*#__PURE__*/React.createElement("label", null, t("detail.ip")), /*#__PURE__*/React.createElement("input", {
    className: "input",
    defaultValue: server.ip,
    style: {
      fontFamily: "var(--font-mono)"
    }
  })), /*#__PURE__*/React.createElement("div", {
    className: "field full"
  }, /*#__PURE__*/React.createElement("label", null, t("common.description")), /*#__PURE__*/React.createElement("textarea", {
    className: "input",
    rows: "3",
    defaultValue: server.description
  })), /*#__PURE__*/React.createElement("div", {
    className: "field"
  }, /*#__PURE__*/React.createElement("label", null, t("detail.node"), " (", t("common.daemon").toLowerCase(), ")"), /*#__PURE__*/React.createElement("select", {
    className: "select",
    defaultValue: server.node_uuid
  }, NODES.map(n => /*#__PURE__*/React.createElement("option", {
    key: n.uuid,
    value: n.uuid
  }, n.name)))), /*#__PURE__*/React.createElement("div", {
    className: "field"
  }, /*#__PURE__*/React.createElement("label", null, t("detail.daemon_port")), /*#__PURE__*/React.createElement("input", {
    className: "input",
    defaultValue: server.daemon_port
  }))), /*#__PURE__*/React.createElement("div", {
    style: {
      display: "flex",
      gap: 8,
      marginTop: 16,
      justifyContent: "space-between"
    }
  }, /*#__PURE__*/React.createElement("button", {
    className: "btn danger"
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "trash",
    size: 14
  }), t("detail.delete")), /*#__PURE__*/React.createElement("div", {
    style: {
      display: "flex",
      gap: 8
    }
  }, /*#__PURE__*/React.createElement("button", {
    className: "btn"
  }, t("btn.cancel")), /*#__PURE__*/React.createElement("button", {
    className: "btn primary"
  }, t("btn.save"))))), /*#__PURE__*/React.createElement(NewServiceModal, {
    open: newSvcOpen,
    onClose: () => setNewSvcOpen(false),
    presetServer: server.uuid
  }));
};
const KV = ({
  k,
  v,
  mono
}) => /*#__PURE__*/React.createElement("div", {
  style: {
    display: "grid",
    gridTemplateColumns: "130px 1fr",
    padding: "8px 0",
    borderBottom: "1px dashed var(--line)",
    alignItems: "center"
  }
}, /*#__PURE__*/React.createElement("span", {
  style: {
    color: "var(--text-mute)",
    fontSize: 12
  }
}, k), /*#__PURE__*/React.createElement("span", {
  style: {
    fontFamily: mono ? "var(--font-mono)" : "inherit",
    fontSize: mono ? 12 : 13,
    color: "var(--text)"
  }
}, v));
const ConsolePane = ({
  server
}) => {
  const t = useT();
  const [cmd, setCmd] = React.useState("");
  const [lines, setLines] = React.useState([{
    who: "system",
    text: `Connected to ${server.ip}:${server.daemon_port} via gRPC`
  }, {
    who: "system",
    text: `Daemon version 0.4.1 · rustc 1.78 · linux/amd64`
  }, {
    who: "system",
    text: `Type 'help' for available commands.`
  }, {
    who: "out",
    text: `[14:31:02] svc.start  mc-survival   ok`
  }, {
    who: "out",
    text: `[14:31:02] svc.start  panel-api     ok`
  }, {
    who: "out",
    text: `[14:31:03] svc.start  postgres-main ok`
  }]);
  const exec = e => {
    e.preventDefault();
    if (!cmd.trim()) return;
    const next = [...lines, {
      who: "in",
      text: `$ ${cmd}`
    }];
    if (cmd === "help") {
      next.push({
        who: "out",
        text: "Commands: ps, status, start <svc>, stop <svc>, restart <svc>, logs <svc>, clear"
      });
    } else if (cmd === "ps") {
      next.push({
        who: "out",
        text: "NAME            STATUS    PORT"
      });
      SERVICES.filter(s => s.server === server.uuid).forEach(s => {
        next.push({
          who: "out",
          text: `${s.name.padEnd(16)}${s.status.padEnd(10)}${s.port || "-"}`
        });
      });
    } else if (cmd === "clear") {
      setLines([]);
      setCmd("");
      return;
    } else {
      next.push({
        who: "out",
        text: `bash: ${cmd}: command not found`
      });
    }
    setLines(next);
    setCmd("");
  };
  return /*#__PURE__*/React.createElement("div", {
    className: "card flush"
  }, /*#__PURE__*/React.createElement("div", {
    className: "card-head"
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("h3", null, t("console.title")), /*#__PURE__*/React.createElement("div", {
    className: "sub"
  }, t("console.sub"))), /*#__PURE__*/React.createElement("div", {
    style: {
      display: "flex",
      gap: 6
    }
  }, /*#__PURE__*/React.createElement("button", {
    className: "btn ghost sm"
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "copy",
    size: 12
  }), t("console.copy")), /*#__PURE__*/React.createElement("button", {
    className: "btn ghost sm"
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "trash",
    size: 12
  }), t("console.clear")))), /*#__PURE__*/React.createElement("div", {
    style: {
      background: "oklch(0.12 0.005 80)",
      color: "var(--text-dim)",
      fontFamily: "var(--font-mono)",
      fontSize: 12.5,
      padding: 16,
      minHeight: 280,
      whiteSpace: "pre-wrap",
      lineHeight: 1.55
    }
  }, lines.map((l, i) => /*#__PURE__*/React.createElement("div", {
    key: i,
    style: {
      color: l.who === "system" ? "var(--text-mute)" : l.who === "in" ? "var(--accent-2)" : "var(--text-dim)"
    }
  }, l.text)), /*#__PURE__*/React.createElement("form", {
    onSubmit: exec,
    style: {
      display: "flex",
      alignItems: "center",
      gap: 6,
      marginTop: 8
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      color: "var(--accent-2)"
    }
  }, "$"), /*#__PURE__*/React.createElement("input", {
    value: cmd,
    onChange: e => setCmd(e.target.value),
    style: {
      flex: 1,
      background: "transparent",
      border: 0,
      outline: "none",
      color: "var(--text)",
      fontFamily: "inherit",
      fontSize: "inherit"
    },
    autoFocus: true,
    placeholder: t("console.placeholder")
  }))));
};
const LogPane = () => {
  const LOG = [{
    t: "14:31:55",
    lvl: "INFO",
    src: "panel-api",
    msg: "GET /api/servers 200 12ms"
  }, {
    t: "14:31:54",
    lvl: "INFO",
    src: "nginx-edge",
    msg: "85.207.10.4 - GET /panel 304"
  }, {
    t: "14:31:50",
    lvl: "WARN",
    src: "postgres-main",
    msg: "checkpoint complete: wrote 4582 buffers (3.7%)"
  }, {
    t: "14:31:46",
    lvl: "INFO",
    src: "panel-api",
    msg: "User admin authenticated from 85.207.10.4"
  }, {
    t: "14:31:31",
    lvl: "ERROR",
    src: "pg-replica",
    msg: "replication lag 8.2s — investigate"
  }, {
    t: "14:31:20",
    lvl: "INFO",
    src: "panel-api",
    msg: "Service status check: 8 running, 2 stopped"
  }, {
    t: "14:30:58",
    lvl: "INFO",
    src: "backup-worker",
    msg: "Snapshot db.main → S3 (482 MB) ok"
  }];
  const lvlColor = l => l === "ERROR" ? "var(--danger-2)" : l === "WARN" ? "var(--warn-2)" : "var(--ok-2)";
  return /*#__PURE__*/React.createElement("div", {
    style: {
      background: "oklch(0.12 0.005 80)",
      borderRadius: 8,
      fontFamily: "var(--font-mono)",
      fontSize: 12,
      padding: 14,
      color: "var(--text-dim)",
      lineHeight: 1.6
    }
  }, LOG.map((l, i) => /*#__PURE__*/React.createElement("div", {
    key: i,
    style: {
      display: "grid",
      gridTemplateColumns: "60px 60px 110px 1fr",
      gap: 10
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      color: "var(--text-mute)"
    }
  }, l.t), /*#__PURE__*/React.createElement("span", {
    style: {
      color: lvlColor(l.lvl),
      fontWeight: 600
    }
  }, l.lvl), /*#__PURE__*/React.createElement("span", {
    style: {
      color: "var(--accent-2)"
    }
  }, l.src), /*#__PURE__*/React.createElement("span", null, l.msg))));
};
window.ServerDetailPage = ServerDetailPage;