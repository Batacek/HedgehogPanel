/* Generated from JSX via Babel (preset: react); this file is the source of record. */
/* Services — list + functional "New service" modal. */

const ServicesPage = ({
  onOpenServer,
  onRefresh,
  refreshing
}) => {
  const t = useT();
  const [query, setQuery] = React.useState("");
  const [typeF, setTypeF] = React.useState("all");
  const [statusF, setStatusF] = React.useState("all");
  const [createOpen, setCreateOpen] = React.useState(false);
  const counts = {};
  SERVICES.forEach(s => {
    counts[s.type] = (counts[s.type] || 0) + 1;
  });
  const filtered = SERVICES.filter(s => {
    if (typeF !== "all" && s.type !== parseInt(typeF, 10)) return false;
    if (statusF !== "all" && s.status !== statusF) return false;
    if (!query) return true;
    const q = query.toLowerCase();
    return s.name.toLowerCase().includes(q) || s.description.toLowerCase().includes(q);
  });
  return /*#__PURE__*/React.createElement("div", {
    className: "page"
  }, /*#__PURE__*/React.createElement("div", {
    className: "page-head"
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("h1", null, t("page.services.title")), /*#__PURE__*/React.createElement("p", {
    className: "lede"
  }, t("page.services.lede"))), /*#__PURE__*/React.createElement("div", {
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
  }), t("btn.new_service")))), /*#__PURE__*/React.createElement("div", {
    className: "grid cols-4",
    style: {
      marginBottom: 14
    }
  }, Object.entries(SERVICE_TYPES).map(([id, ty]) => /*#__PURE__*/React.createElement("div", {
    className: "kpi-card",
    key: id
  }, /*#__PURE__*/React.createElement("div", {
    className: "label",
    style: {
      display: "flex",
      alignItems: "center",
      gap: 8
    }
  }, /*#__PURE__*/React.createElement(Icon, {
    name: ty.icon,
    size: 13
  }), " ", ty.label), /*#__PURE__*/React.createElement("div", {
    className: "value"
  }, counts[id] || 0), /*#__PURE__*/React.createElement("div", {
    className: "delta"
  }, t("page.services.across").replace("{{n}}", new Set(SERVICES.filter(s => s.type === parseInt(id, 10)).map(s => s.server)).size))))), /*#__PURE__*/React.createElement("div", {
    className: "card flush"
  }, /*#__PURE__*/React.createElement("div", {
    className: "toolbar"
  }, /*#__PURE__*/React.createElement("div", {
    className: "seg"
  }, [{
    k: "all",
    l: `${t("common.all")} · ${SERVICES.length}`
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
  }, opt.l))), /*#__PURE__*/React.createElement("select", {
    className: "select",
    value: typeF,
    onChange: e => setTypeF(e.target.value),
    style: {
      width: "auto",
      maxWidth: 180
    }
  }, /*#__PURE__*/React.createElement("option", {
    value: "all"
  }, t("page.services.all_types")), Object.entries(SERVICE_TYPES).map(([id, ty]) => /*#__PURE__*/React.createElement("option", {
    key: id,
    value: id
  }, ty.label))), /*#__PURE__*/React.createElement("div", {
    className: "spacer"
  }), /*#__PURE__*/React.createElement("div", {
    className: "field",
    style: {
      maxWidth: 320,
      flex: 1
    }
  }, /*#__PURE__*/React.createElement("input", {
    className: "input",
    placeholder: t("page.services.search"),
    value: query,
    onChange: e => setQuery(e.target.value)
  }))), filtered.length === 0 ? /*#__PURE__*/React.createElement(EmptyState, {
    title: t("common.empty"),
    icon: "service"
  }) : /*#__PURE__*/React.createElement("table", {
    className: "table"
  }, /*#__PURE__*/React.createElement("thead", null, /*#__PURE__*/React.createElement("tr", null, /*#__PURE__*/React.createElement("th", null, t("common.name")), /*#__PURE__*/React.createElement("th", null, t("common.type")), /*#__PURE__*/React.createElement("th", null, "Server"), /*#__PURE__*/React.createElement("th", null, "Port"), /*#__PURE__*/React.createElement("th", null, t("common.status")), /*#__PURE__*/React.createElement("th", null))), /*#__PURE__*/React.createElement("tbody", null, filtered.map(sv => {
    const type = SERVICE_TYPES[sv.type] || SERVICE_TYPES[4];
    const server = SERVERS.find(s => s.uuid === sv.server);
    return /*#__PURE__*/React.createElement("tr", {
      key: sv.uuid
    }, /*#__PURE__*/React.createElement("td", null, /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        gap: 12,
        alignItems: "center"
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        width: 30,
        height: 30,
        borderRadius: 8,
        background: "var(--surface-2)",
        color: "var(--text-dim)",
        display: "grid",
        placeItems: "center"
      }
    }, /*#__PURE__*/React.createElement(Icon, {
      name: type.icon,
      size: 14
    })), /*#__PURE__*/React.createElement("div", {
      className: "stack"
    }, /*#__PURE__*/React.createElement("strong", {
      style: {
        fontWeight: 600
      }
    }, sv.name), /*#__PURE__*/React.createElement("span", {
      className: "sub"
    }, sv.description)))), /*#__PURE__*/React.createElement("td", null, /*#__PURE__*/React.createElement(Pill, null, type.label)), /*#__PURE__*/React.createElement("td", null, /*#__PURE__*/React.createElement("button", {
      style: {
        background: "none",
        border: 0,
        color: "var(--text)",
        textAlign: "left",
        padding: 0,
        cursor: "pointer"
      },
      onClick: () => onOpenServer?.(sv.server)
    }, /*#__PURE__*/React.createElement("div", {
      className: "stack"
    }, /*#__PURE__*/React.createElement("strong", {
      style: {
        fontWeight: 500
      }
    }, server?.name), /*#__PURE__*/React.createElement("span", {
      className: "sub mono"
    }, server?.ip, ":", server?.daemon_port)))), /*#__PURE__*/React.createElement("td", {
      className: "mono"
    }, sv.port || "—"), /*#__PURE__*/React.createElement("td", null, /*#__PURE__*/React.createElement(StatusPill, {
      status: sv.status
    })), /*#__PURE__*/React.createElement("td", {
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
  })))), /*#__PURE__*/React.createElement(NewServiceModal, {
    open: createOpen,
    onClose: () => setCreateOpen(false)
  }));
};

/*  New Service Modal  */

const NewServiceModal = ({
  open,
  onClose,
  presetServer = null
}) => {
  const t = useT();
  const [server, setServer] = React.useState(presetServer || "");
  const [type, setType] = React.useState("3");
  const [name, setName] = React.useState("");
  const [port, setPort] = React.useState("");
  const [desc, setDesc] = React.useState("");
  const [done, setDone] = React.useState(false);
  React.useEffect(() => {
    if (open) {
      setServer(presetServer || "");
      setType("3");
      setName("");
      setPort("");
      setDesc("");
      setDone(false);
    }
  }, [open, presetServer]);
  const submit = e => {
    e.preventDefault();
    setDone(true);
    setTimeout(onClose, 1100);
  };
  return /*#__PURE__*/React.createElement(Modal, {
    open: open,
    onClose: onClose,
    title: t("mod.svc.title"),
    lede: t("mod.svc.lede"),
    footer: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("button", {
      className: "btn",
      onClick: onClose
    }, t("btn.cancel")), /*#__PURE__*/React.createElement("button", {
      className: "btn primary",
      disabled: !server || !name || done,
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
  }, /*#__PURE__*/React.createElement("label", null, t("mod.svc.server"), " *"), /*#__PURE__*/React.createElement("select", {
    className: "select",
    value: server,
    onChange: e => setServer(e.target.value),
    required: true
  }, /*#__PURE__*/React.createElement("option", {
    value: ""
  }, "\u2014 vyber server \u2014"), SERVERS.map(s => /*#__PURE__*/React.createElement("option", {
    key: s.uuid,
    value: s.uuid
  }, s.name)))), /*#__PURE__*/React.createElement("div", {
    className: "form-grid"
  }, /*#__PURE__*/React.createElement("div", {
    className: "field"
  }, /*#__PURE__*/React.createElement("label", null, t("mod.svc.type"), " *"), /*#__PURE__*/React.createElement("select", {
    className: "select",
    value: type,
    onChange: e => setType(e.target.value),
    required: true
  }, Object.entries(SERVICE_TYPES).map(([id, ty]) => /*#__PURE__*/React.createElement("option", {
    key: id,
    value: id
  }, ty.label)))), /*#__PURE__*/React.createElement("div", {
    className: "field"
  }, /*#__PURE__*/React.createElement("label", null, t("mod.svc.port")), /*#__PURE__*/React.createElement("input", {
    className: "input",
    value: port,
    onChange: e => setPort(e.target.value),
    placeholder: "\u2014",
    style: {
      fontFamily: "var(--font-mono)"
    }
  }))), /*#__PURE__*/React.createElement("div", {
    className: "field"
  }, /*#__PURE__*/React.createElement("label", null, t("mod.svc.name"), " *"), /*#__PURE__*/React.createElement("input", {
    className: "input",
    value: name,
    onChange: e => setName(e.target.value),
    placeholder: "mc-survival",
    required: true
  })), /*#__PURE__*/React.createElement("div", {
    className: "field"
  }, /*#__PURE__*/React.createElement("label", null, t("mod.svc.desc")), /*#__PURE__*/React.createElement("textarea", {
    className: "input",
    rows: "2",
    value: desc,
    onChange: e => setDesc(e.target.value),
    placeholder: "\u2014"
  }))));
};
window.ServicesPage = ServicesPage;
window.NewServiceModal = NewServiceModal;