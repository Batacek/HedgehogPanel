/* Generated from JSX via Babel (preset: react); this file is the source of record. */
/* Servers — list / grid with functional "New server" modal. */

const ServersPage = ({
  onOpenServer,
  onRefresh,
  refreshing
}) => {
  const t = useT();
  const [view, setView] = React.useState("list");
  const [query, setQuery] = React.useState("");
  const [statusF, setStatusF] = React.useState("all");
  const [createOpen, setCreateOpen] = React.useState(false);
  const filtered = SERVERS.filter(s => {
    if (statusF !== "all" && s.status !== statusF) return false;
    if (!query) return true;
    const q = query.toLowerCase();
    return s.name.toLowerCase().includes(q) || s.ip.toLowerCase().includes(q) || (s.description || "").toLowerCase().includes(q);
  });
  return /*#__PURE__*/React.createElement("div", {
    className: "page"
  }, /*#__PURE__*/React.createElement("div", {
    className: "page-head"
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("h1", null, t("page.servers.title")), /*#__PURE__*/React.createElement("p", {
    className: "lede"
  }, t("page.servers.lede"))), /*#__PURE__*/React.createElement("div", {
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
  }), t("btn.sync")), /*#__PURE__*/React.createElement("button", {
    className: "btn primary",
    onClick: () => setCreateOpen(true)
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "plus",
    size: 14
  }), t("btn.new_server")))), /*#__PURE__*/React.createElement("div", {
    className: "card flush"
  }, /*#__PURE__*/React.createElement("div", {
    className: "toolbar"
  }, /*#__PURE__*/React.createElement("div", {
    className: "seg",
    role: "tablist"
  }, [{
    k: "all",
    l: `${t("common.all")} · ${SERVERS.length}`
  }, {
    k: "running",
    l: t("st.running")
  }, {
    k: "stopped",
    l: t("st.stopped")
  }, {
    k: "warning",
    l: t("st.warning")
  }].map(opt => /*#__PURE__*/React.createElement("button", {
    key: opt.k,
    className: statusF === opt.k ? "on" : "",
    onClick: () => setStatusF(opt.k)
  }, opt.l))), /*#__PURE__*/React.createElement("div", {
    className: "spacer"
  }), /*#__PURE__*/React.createElement("div", {
    className: "field",
    style: {
      flexDirection: "row"
    }
  }, /*#__PURE__*/React.createElement("input", {
    className: "input",
    placeholder: t("page.servers.search"),
    value: query,
    onChange: e => setQuery(e.target.value)
  })), /*#__PURE__*/React.createElement("div", {
    className: "seg",
    role: "tablist"
  }, /*#__PURE__*/React.createElement("button", {
    className: view === "list" ? "on" : "",
    onClick: () => setView("list"),
    title: t("common.view.table")
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "list",
    size: 14
  })), /*#__PURE__*/React.createElement("button", {
    className: view === "grid" ? "on" : "",
    onClick: () => setView("grid"),
    title: t("common.view.cards")
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "grid",
    size: 14
  })))), filtered.length === 0 && /*#__PURE__*/React.createElement(EmptyState, {
    title: t("common.empty.servers"),
    body: t("common.empty.try"),
    icon: "server"
  }), filtered.length > 0 && view === "list" && /*#__PURE__*/React.createElement("table", {
    className: "table"
  }, /*#__PURE__*/React.createElement("thead", null, /*#__PURE__*/React.createElement("tr", null, /*#__PURE__*/React.createElement("th", null, "Server"), /*#__PURE__*/React.createElement("th", null, t("mod.server.kind")), /*#__PURE__*/React.createElement("th", null, t("common.status")), /*#__PURE__*/React.createElement("th", null, "IP : port"), /*#__PURE__*/React.createElement("th", {
    style: {
      width: 110
    }
  }, "CPU"), /*#__PURE__*/React.createElement("th", {
    style: {
      width: 110
    }
  }, "RAM"), /*#__PURE__*/React.createElement("th", {
    style: {
      width: 110
    }
  }, "Disk"), /*#__PURE__*/React.createElement("th", null, t("common.uptime")), /*#__PURE__*/React.createElement("th", null))), /*#__PURE__*/React.createElement("tbody", null, filtered.map(s => {
    const parent = s.parent && SERVERS.find(x => x.uuid === s.parent);
    return /*#__PURE__*/React.createElement("tr", {
      key: s.uuid,
      style: {
        cursor: "pointer"
      },
      onClick: () => onOpenServer?.(s.uuid)
    }, /*#__PURE__*/React.createElement("td", null, /*#__PURE__*/React.createElement("div", {
      className: "stack"
    }, /*#__PURE__*/React.createElement("strong", {
      style: {
        fontWeight: 600
      }
    }, s.name), parent ? /*#__PURE__*/React.createElement("span", {
      className: "srv-hier"
    }, /*#__PURE__*/React.createElement(Icon, {
      name: "chevron_right",
      size: 10,
      className: "arrow"
    }), t("page.servers.vm_on").replace("{{name}}", parent.name)) : /*#__PURE__*/React.createElement("span", {
      className: "sub",
      style: {
        fontSize: 11.5,
        color: "var(--text-mute)"
      }
    }, s.description))), /*#__PURE__*/React.createElement("td", null, /*#__PURE__*/React.createElement(KindPill, {
      kind: s.kind
    })), /*#__PURE__*/React.createElement("td", null, /*#__PURE__*/React.createElement(StatusPill, {
      status: s.status
    })), /*#__PURE__*/React.createElement("td", {
      className: "mono"
    }, s.ip !== "—" ? `${s.ip}:${s.daemon_port}` : "—"), /*#__PURE__*/React.createElement("td", null, /*#__PURE__*/React.createElement(MeterCell, {
      value: s.cpu
    })), /*#__PURE__*/React.createElement("td", null, /*#__PURE__*/React.createElement(MeterCell, {
      value: s.ram
    })), /*#__PURE__*/React.createElement("td", null, /*#__PURE__*/React.createElement(MeterCell, {
      value: s.disk
    })), /*#__PURE__*/React.createElement("td", {
      className: "mono"
    }, s.uptime), /*#__PURE__*/React.createElement("td", {
      className: "actions"
    }, /*#__PURE__*/React.createElement("button", {
      className: "btn ghost sm",
      onClick: e => e.stopPropagation()
    }, /*#__PURE__*/React.createElement(Icon, {
      name: "more",
      size: 16
    }))));
  }))), filtered.length > 0 && view === "grid" && /*#__PURE__*/React.createElement("div", {
    className: "grid cols-3",
    style: {
      padding: 16
    }
  }, filtered.map(s => /*#__PURE__*/React.createElement("button", {
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
  }), /*#__PURE__*/React.createElement(RolePill, {
    role: s.role
  })))))), /*#__PURE__*/React.createElement(NewServerModal, {
    open: createOpen,
    onClose: () => setCreateOpen(false)
  }));
};
const MeterCell = ({
  value
}) => {
  const tone = value > 85 ? "danger" : value > 70 ? "warn" : null;
  return /*#__PURE__*/React.createElement("div", {
    style: {
      display: "flex",
      alignItems: "center",
      gap: 8
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      flex: 1
    }
  }, /*#__PURE__*/React.createElement(Meter, {
    value: value,
    tone: tone
  })), /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: "var(--font-mono)",
      fontSize: 11.5,
      color: "var(--text-dim)",
      width: 32,
      textAlign: "right"
    }
  }, value, "%"));
};
const StatLine = ({
  icon,
  label,
  value
}) => {
  const tone = value > 85 ? "danger" : value > 70 ? "warn" : null;
  return /*#__PURE__*/React.createElement("div", {
    className: "stat-line"
  }, /*#__PURE__*/React.createElement("span", null, /*#__PURE__*/React.createElement(Icon, {
    name: icon,
    size: 12,
    style: {
      verticalAlign: "-2px",
      marginRight: 5
    }
  }), label), /*#__PURE__*/React.createElement(Meter, {
    value: value,
    tone: tone
  }), /*#__PURE__*/React.createElement("span", {
    className: "val"
  }, value, "%"));
};

/*  New Server Modal  */

const NewServerModal = ({
  open,
  onClose
}) => {
  const t = useT();
  const [ip, setIp] = React.useState("");
  const [name, setName] = React.useState("");
  const [port, setPort] = React.useState("50051");
  const [kind, setKind] = React.useState("dedicated");
  const [parent, setParent] = React.useState("");
  const [desc, setDesc] = React.useState("");
  const [done, setDone] = React.useState(false);
  React.useEffect(() => {
    if (open) {
      setIp("");
      setName("");
      setPort("50051");
      setKind("dedicated");
      setParent("");
      setDesc("");
      setDone(false);
    }
  }, [open]);
  const dedicatedServers = SERVERS.filter(s => s.kind === "dedicated");
  const submit = e => {
    e.preventDefault();
    setDone(true);
    setTimeout(onClose, 1100);
  };
  return /*#__PURE__*/React.createElement(Modal, {
    open: open,
    onClose: onClose,
    title: t("mod.server.title"),
    lede: t("mod.server.lede"),
    footer: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("button", {
      className: "btn",
      onClick: onClose
    }, t("btn.cancel")), /*#__PURE__*/React.createElement("button", {
      className: "btn primary",
      disabled: !ip || done,
      onClick: submit
    }, done ? /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Icon, {
      name: "check",
      size: 13
    }), " Ulo\u017Eeno") : /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Icon, {
      name: "plus",
      size: 13
    }), " ", t("btn.create"))))
  }, /*#__PURE__*/React.createElement("form", {
    onSubmit: submit,
    style: {
      display: "contents"
    }
  }, /*#__PURE__*/React.createElement("div", {
    className: "field"
  }, /*#__PURE__*/React.createElement("label", null, t("mod.server.ip"), " *"), /*#__PURE__*/React.createElement("input", {
    className: "input",
    value: ip,
    onChange: e => setIp(e.target.value),
    placeholder: "10.20.30.10",
    required: true,
    style: {
      fontFamily: "var(--font-mono)"
    },
    autoFocus: true
  })), /*#__PURE__*/React.createElement("div", {
    className: "form-grid"
  }, /*#__PURE__*/React.createElement("div", {
    className: "field"
  }, /*#__PURE__*/React.createElement("label", null, t("mod.server.name")), /*#__PURE__*/React.createElement("input", {
    className: "input",
    value: name,
    onChange: e => setName(e.target.value),
    placeholder: "Web Server"
  })), /*#__PURE__*/React.createElement("div", {
    className: "field"
  }, /*#__PURE__*/React.createElement("label", null, t("mod.server.port")), /*#__PURE__*/React.createElement("input", {
    className: "input",
    value: port,
    onChange: e => setPort(e.target.value),
    style: {
      fontFamily: "var(--font-mono)"
    }
  }))), /*#__PURE__*/React.createElement("div", {
    className: "field"
  }, /*#__PURE__*/React.createElement("label", null, t("mod.server.kind")), /*#__PURE__*/React.createElement("div", {
    className: "seg",
    style: {
      alignSelf: "flex-start"
    }
  }, /*#__PURE__*/React.createElement("button", {
    type: "button",
    className: kind === "dedicated" ? "on" : "",
    onClick: () => setKind("dedicated")
  }, t("kind.dedicated")), /*#__PURE__*/React.createElement("button", {
    type: "button",
    className: kind === "virtual" ? "on" : "",
    onClick: () => setKind("virtual")
  }, t("kind.virtual")))), kind === "virtual" && /*#__PURE__*/React.createElement("div", {
    className: "field"
  }, /*#__PURE__*/React.createElement("label", null, t("mod.server.parent")), /*#__PURE__*/React.createElement("select", {
    className: "select",
    value: parent,
    onChange: e => setParent(e.target.value)
  }, /*#__PURE__*/React.createElement("option", {
    value: ""
  }, "\u2014"), dedicatedServers.map(s => /*#__PURE__*/React.createElement("option", {
    key: s.uuid,
    value: s.uuid
  }, s.name))), /*#__PURE__*/React.createElement("div", {
    className: "help"
  }, t("mod.server.parent.h"))), /*#__PURE__*/React.createElement("div", {
    className: "field"
  }, /*#__PURE__*/React.createElement("label", null, t("mod.server.desc")), /*#__PURE__*/React.createElement("textarea", {
    className: "input",
    rows: "2",
    value: desc,
    onChange: e => setDesc(e.target.value),
    placeholder: "\u2014"
  }))));
};
window.ServersPage = ServersPage;
window.MeterCell = MeterCell;
window.StatLine = StatLine;
window.NewServerModal = NewServerModal;