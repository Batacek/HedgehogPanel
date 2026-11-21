/* Generated from JSX via Babel (preset: react); this file is the source of record. */
/* Nodes — dedicated page (daemon instances): list, add, pair, ping and the load of the host. */

const el = React.createElement;
const isAdminUser = () => !!(window.CURRENT_USER && window.CURRENT_USER.isAdmin);

/* One call to a node endpoint, reduced to what the page needs. Never throws. */
const callNode = async (path, method, body) => {
  try {
    const res = await window.__hh_api("/api/nodes" + path, {
      method,
      body: body === undefined ? undefined : JSON.stringify(body)
    });
    const data = await res.json().catch(() => ({}));
    return {
      ok: res.ok,
      status: res.status,
      data,
      failure: data.failure || null,
      error: data.error || null
    };
  } catch (e) {
    return {
      ok: false,
      status: 0,
      data: {},
      failure: null,
      error: null
    };
  }
};

/* What to tell the user about a failed call: one text per daemon failure kind. */
const failureMessage = (t, result) => {
  if (result.failure) return t("node.failure." + result.failure, t("node.failure.generic"));
  if (result.status === 409) return t("node.failure.duplicate");
  return result.error || t("node.failure.generic");
};

/* A daemon that rejects the panel's token, or lets it do a handshake only, has to be paired again. */
const needsRepair = failure => failure === "Unauthenticated" || failure === "PermissionDenied";

/* ABCD-EFGH: capitals only, the dash is put in, so lower case and a missing dash both work. */
const formatCode = raw => {
  const s = String(raw).toUpperCase().replace(/[^A-Z0-9]/g, "").slice(0, 12);
  return s.length > 4 ? s.slice(0, 4) + "-" + s.slice(4) : s;
};
const fmtLastSeen = ms => ms == null ? "—" : fmtAgo(ms) + " ago";
const fmtBytes = n => {
  const gb = n / 1073741824;
  return gb >= 1 ? gb.toFixed(1) + " GB" : Math.round(n / 1048576) + " MB";
};
const fmtUptime = seconds => {
  const d = Math.floor(seconds / 86400);
  const hrs = Math.floor(seconds % 86400 / 3600);
  const m = Math.floor(seconds % 3600 / 60);
  if (d > 0) return d + "d " + hrs + "h";
  if (hrs > 0) return hrs + "h " + m + "m";
  return m + "m";
};

/* Full needs no mention; the others say how much of the protocol the panel can use. */
const COMPAT_TONE = {
  Limited: "default",
  LimitedPreRelease: "warn",
  Incompatible: "danger"
};
const CompatPill = ({
  compat
}) => {
  const t = useT();
  if (!compat || compat === "Full") return null;
  return el(Pill, {
    tone: COMPAT_TONE[compat] || "default"
  }, t("node.compat." + compat, compat));
};

/* The longer note for a daemon that is not a full match. */
const CompatNote = ({
  compat
}) => {
  const t = useT();
  if (!compat || compat === "Full") return null;
  const tone = compat === "Limited" ? "" : compat === "LimitedPreRelease" ? " warn" : " danger";
  return el("div", {
    className: "note" + tone,
    style: {
      display: "flex",
      gap: 10,
      alignItems: "flex-start"
    }
  }, el(Icon, {
    name: compat === "Limited" ? "info" : "alert",
    size: 16
  }), el("div", null, el("strong", {
    style: {
      display: "block",
      marginBottom: 2
    }
  }, t("node.compat." + compat)), t("node.compat." + compat + ".b")));
};
const RepairNote = ({
  failure
}) => {
  const t = useT();
  const restricted = failure === "PermissionDenied";
  return el("div", {
    className: "note warn",
    style: {
      display: "flex",
      gap: 10,
      alignItems: "flex-start"
    }
  }, el(Icon, {
    name: "lock",
    size: 16
  }), el("div", null, el("strong", {
    style: {
      display: "block",
      marginBottom: 2
    }
  }, t(restricted ? "node.restricted.title" : "node.rejected.title")), t(restricted ? "node.restricted.b" : "node.rejected.b")));
};

/* ───────────────────────────── page ───────────────────────────── */

const NodesPage = ({
  onRefresh,
  refreshing
}) => {
  const t = useT();
  const admin = isAdminUser();
  const [addOpen, setAddOpen] = React.useState(false);
  const [pairTarget, setPairTarget] = React.useState(null);
  const [detailId, setDetailId] = React.useState(null);
  const [pinging, setPinging] = React.useState(null);
  const [note, setNote] = React.useState(null);
  const flash = (tone, text) => {
    setNote({
      tone,
      text
    });
    setTimeout(() => setNote(null), 5000);
  };
  const ping = async node => {
    if (pinging) return;
    setPinging(node.uuid);
    const res = await callNode("/" + encodeURIComponent(node.uuid) + "/ping", "POST");
    setPinging(null);
    if (res.ok) flash("success", node.name + ": " + t("node.ping.ok"));else flash("danger", node.name + ": " + failureMessage(t, res));
    onRefresh?.();
  };
  const openPair = node => setPairTarget({
    id: node.uuid,
    name: node.name
  });
  const actionsFor = n => {
    if (!admin) return null;
    return el(React.Fragment, null, n.paired ? el("button", {
      className: "btn sm",
      onClick: () => ping(n),
      disabled: pinging != null
    }, el(Icon, {
      name: "refresh",
      size: 13,
      style: pinging === n.uuid ? {
        animation: "spin 0.8s linear infinite"
      } : undefined
    }), t("node.action.ping")) : el("button", {
      className: "btn sm primary",
      onClick: () => openPair(n)
    }, el(Icon, {
      name: "key",
      size: 13
    }), t("node.action.pair")), " ");
  };
  return el("div", {
    className: "page"
  }, el("div", {
    className: "page-head"
  }, el("div", null, el("h1", null, t("page.nodes.title")), el("p", {
    className: "lede"
  }, t("page.nodes.lede"))), el("div", {
    className: "page-actions"
  }, el("button", {
    className: "btn",
    onClick: () => onRefresh?.(),
    disabled: refreshing
  }, el(Icon, {
    name: "refresh",
    size: 14,
    style: refreshing ? {
      animation: "spin 0.8s linear infinite"
    } : undefined
  }), t("btn.refresh")), admin && el("button", {
    className: "btn primary",
    onClick: () => setAddOpen(true)
  }, el(Icon, {
    name: "plus",
    size: 14
  }), t("btn.new_node")))), note && el("div", {
    className: "note " + note.tone,
    style: {
      marginBottom: 12
    }
  }, note.text), el("div", {
    className: "card flush"
  }, NODES.length === 0 ? el(EmptyState, {
    title: t("node.empty"),
    body: admin ? t("node.empty.b") : undefined,
    icon: "node"
  }) : el("table", {
    className: "table"
  }, el("thead", null, el("tr", null, el("th", null, t("node.col.node")), el("th", null, t("node.col.server")), el("th", null, t("common.address")), el("th", null, t("common.status")), el("th", null, t("common.daemon")), el("th", null, t("common.last_seen")), el("th", null))), el("tbody", null, NODES.map(n => {
    const server = SERVERS.find(s => s.node_uuid === n.uuid);
    return el("tr", {
      key: n.uuid
    }, el("td", null, el("div", {
      className: "stack"
    }, el("strong", {
      style: {
        fontWeight: 600
      }
    }, n.name), el("span", {
      className: "sub"
    }, n.description))), el("td", null, server ? el("div", {
      className: "stack"
    }, el("span", {
      style: {
        fontWeight: 500
      }
    }, server.name), el("span", {
      className: "sub"
    }, el(KindPill, {
      kind: server.kind
    }))) : el("span", {
      style: {
        color: "var(--text-mute)"
      }
    }, "—")), el("td", {
      className: "mono"
    }, n.ip, ":", n.port), el("td", null, el("div", {
      className: "stack"
    }, el("span", {
      style: {
        display: "flex",
        gap: 6,
        flexWrap: "wrap"
      }
    }, el(StatusPill, {
      status: n.status
    }), el(CompatPill, {
      compat: n.compatibility
    })), n.lastError && n.status !== "online" && el("span", {
      className: "sub",
      title: n.lastError,
      style: {
        maxWidth: 240,
        overflow: "hidden",
        textOverflow: "ellipsis",
        whiteSpace: "nowrap"
      }
    }, n.lastError))), el("td", {
      className: "mono"
    }, n.version ? el("div", {
      className: "stack"
    }, el("span", null, "v", n.version), n.protocolVersion && el("span", {
      className: "sub"
    }, "proto ", n.protocolVersion)) : "—"), el("td", {
      className: "mono"
    }, fmtLastSeen(n.lastSeen)), el("td", {
      className: "actions"
    }, actionsFor(n), el("button", {
      className: "btn ghost sm",
      onClick: () => setDetailId(n.uuid),
      title: t("node.action.details"),
      "aria-label": t("node.action.details")
    }, el(Icon, {
      name: "more",
      size: 16
    }))));
  })))), el(NewNodeModal, {
    open: addOpen,
    onClose: () => setAddOpen(false),
    onCreated: (id, name) => {
      setAddOpen(false);
      flash("success", t("node.add.created"));
      onRefresh?.();
      setPairTarget({
        id,
        name
      });
    }
  }), el(PairNodeModal, {
    target: pairTarget,
    onClose: () => setPairTarget(null),
    onPaired: async (id, name) => {
      setPairTarget(null);
      flash("success", name + ": " + t("node.pair.success"));
      /* Pairing only learns the protocol version; a handshake fills in the daemon version. */
      await callNode("/" + encodeURIComponent(id) + "/ping", "POST");
      onRefresh?.();
    }
  }), el(NodeDetailModal, {
    id: detailId,
    admin: admin,
    onClose: () => setDetailId(null),
    onPair: node => {
      setDetailId(null);
      openPair(node);
    },
    onPing: ping,
    pinging: pinging
  }));
};

/* ──────────────────────────── add node ──────────────────────────── */

const NewNodeModal = ({
  open,
  onClose,
  onCreated
}) => {
  const t = useT();
  const empty = {
    name: "",
    address: "",
    port: "50051",
    description: ""
  };
  const [form, setForm] = React.useState(empty);
  const [busy, setBusy] = React.useState(false);
  const [error, setError] = React.useState(null);
  React.useEffect(() => {
    if (open) {
      setForm(empty);
      setError(null);
    }
  }, [open]);
  const set = key => e => setForm({
    ...form,
    [key]: e.target.value
  });
  const submit = async e => {
    e.preventDefault();
    if (busy) return;
    const name = form.name.trim();
    const address = form.address.trim();
    const port = Number(form.port);
    if (!name || !address || !Number.isInteger(port) || port < 1 || port > 65535) {
      setError(t("node.add.invalid"));
      return;
    }
    setBusy(true);
    setError(null);
    const res = await callNode("", "POST", {
      name,
      ipAddress: address,
      port,
      description: form.description.trim() || null
    });
    setBusy(false);
    if (res.ok && res.data.id) onCreated(res.data.id, name);else setError(res.error || t("node.failure.generic"));
  };
  const field = (key, label, props) => el("div", {
    className: "field" + (props && props.full ? " full" : "")
  }, el("label", null, label), el("input", {
    className: "input",
    value: form[key],
    onChange: set(key),
    ...(props || {}).input
  }));
  return el(Modal, {
    open: open,
    onClose: onClose,
    title: t("mod.node.title"),
    lede: t("node.add.lede"),
    size: "md",
    footer: el(React.Fragment, null, el("button", {
      className: "btn",
      type: "button",
      onClick: onClose
    }, t("btn.cancel")), el("button", {
      className: "btn primary",
      type: "submit",
      form: "hh-node-form",
      disabled: busy
    }, t("node.add.submit")))
  }, el("form", {
    id: "hh-node-form",
    onSubmit: submit,
    className: "form-grid"
  }, field("name", t("node.field.name"), {
    input: {
      autoFocus: true,
      required: true,
      placeholder: "node-1"
    }
  }), field("address", t("node.field.address"), {
    input: {
      required: true,
      placeholder: "10.0.0.5"
    }
  }), field("port", t("node.field.port"), {
    input: {
      required: true,
      inputMode: "numeric"
    }
  }), field("description", t("node.field.description"), {
    input: {}
  }), error && el("div", {
    className: "note danger full"
  }, error)));
};

/* ───────────────────────────── pairing ───────────────────────────── */

const PairNodeModal = ({
  target,
  onClose,
  onPaired
}) => {
  const t = useT();
  const open = target != null;
  const [code, setCode] = React.useState("");
  const [busy, setBusy] = React.useState(false);
  const [error, setError] = React.useState(null);
  const [failure, setFailure] = React.useState(null);
  React.useEffect(() => {
    if (open) {
      setCode("");
      setError(null);
      setFailure(null);
    }
  }, [target && target.id]);
  const submit = async e => {
    e.preventDefault();
    if (busy || !target) return;
    if (!code.replace("-", "")) {
      setError(t("node.pair.code_required"));
      return;
    }
    setBusy(true);
    setError(null);
    setFailure(null);
    const res = await callNode("/" + encodeURIComponent(target.id) + "/register", "POST", {
      code
    });
    setBusy(false);
    if (res.ok) {
      onPaired(target.id, target.name);
      return;
    }
    setFailure(res.failure);
    setError(failureMessage(t, res));
  };
  return el(Modal, {
    open: open,
    onClose: onClose,
    title: t("node.pair.title") + (target ? ": " + target.name : ""),
    lede: t("node.pair.lede"),
    size: "md",
    footer: el(React.Fragment, null, el("button", {
      className: "btn",
      type: "button",
      onClick: onClose
    }, t("btn.cancel")), el("button", {
      className: "btn primary",
      type: "submit",
      form: "hh-pair-form",
      disabled: busy
    }, t("node.pair.submit")))
  }, el("form", {
    id: "hh-pair-form",
    onSubmit: submit,
    style: {
      display: "flex",
      flexDirection: "column",
      gap: 12
    }
  }, el("div", {
    className: "field"
  }, el("label", null, t("node.pair.code")), el("input", {
    className: "input mono",
    value: code,
    onChange: e => setCode(formatCode(e.target.value)),
    placeholder: "ABCD-EFGH",
    autoFocus: true,
    autoComplete: "off",
    spellCheck: false,
    style: {
      letterSpacing: "0.12em"
    }
  }), el("span", {
    className: "help"
  }, t("node.pair.hint"))), error && el("div", {
    className: "note danger"
  }, error), needsRepair(failure) && el(RepairNote, {
    failure: failure
  })));
};

/* ───────────────────────────── details ───────────────────────────── */

const DetailRow = ({
  label,
  children
}) => el("div", {
  style: {
    display: "grid",
    gridTemplateColumns: "150px 1fr",
    gap: 12,
    padding: "7px 0",
    borderBottom: "1px solid var(--line)",
    fontSize: 13
  }
}, el("span", {
  style: {
    color: "var(--text-mute)"
  }
}, label), el("span", {
  style: {
    wordBreak: "break-word"
  }
}, children));
const HealthStat = ({
  label,
  value
}) => el("div", {
  style: {
    flex: "1 1 120px",
    background: "var(--surface-2)",
    border: "1px solid var(--line)",
    borderRadius: 8,
    padding: "10px 12px"
  }
}, el("div", {
  style: {
    fontSize: 11,
    color: "var(--text-mute)",
    textTransform: "uppercase",
    letterSpacing: "0.06em"
  }
}, label), el("div", {
  className: "mono",
  style: {
    fontSize: 15,
    marginTop: 4
  }
}, value));

/* The load of the host, read from the daemon when the dialog opens and on request. Admins only. */
const NodeHealth = ({
  node,
  onFailure
}) => {
  const t = useT();
  const [state, setState] = React.useState({
    loading: false,
    health: null,
    error: null,
    failure: null
  });
  const load = React.useCallback(async () => {
    setState(s => ({
      ...s,
      loading: true,
      error: null
    }));
    const res = await callNode("/" + encodeURIComponent(node.uuid) + "/health", "GET");
    if (res.ok) {
      setState({
        loading: false,
        health: res.data,
        error: null
      });
      onFailure(null);
    } else {
      setState({
        loading: false,
        health: null,
        error: failureMessage(t, res),
        failure: res.failure
      });
      onFailure(res.failure);
    }
  }, [node.uuid, t]);
  React.useEffect(() => {
    load();
  }, [node.uuid]);
  const {
    loading,
    health,
    error,
    failure
  } = state;
  const ramPct = health && health.ramTotalBytes > 0 ? Math.round(health.ramUsedBytes / health.ramTotalBytes * 100) : 0;
  return el("div", {
    style: {
      marginTop: 18
    }
  }, el("div", {
    style: {
      display: "flex",
      alignItems: "center",
      justifyContent: "space-between",
      marginBottom: 8
    }
  }, el("strong", {
    style: {
      fontSize: 13
    }
  }, t("node.health.title")), el("button", {
    className: "btn sm",
    onClick: load,
    disabled: loading
  }, el(Icon, {
    name: "refresh",
    size: 13,
    style: loading ? {
      animation: "spin 0.8s linear infinite"
    } : undefined
  }), t("node.health.load"))), loading && !health && el("div", {
    style: {
      color: "var(--text-mute)",
      fontSize: 13
    }
  }, t("node.health.loading")), error && !needsRepair(failure) && el("div", {
    className: "note danger"
  }, error), health && el("div", {
    style: {
      display: "flex",
      gap: 10,
      flexWrap: "wrap",
      opacity: loading ? 0.6 : 1
    }
  }, el(HealthStat, {
    label: t("node.health.cpu"),
    value: health.cpuUsagePercent.toFixed(1) + " %"
  }), el(HealthStat, {
    label: t("node.health.ram"),
    value: fmtBytes(health.ramUsedBytes) + " / " + fmtBytes(health.ramTotalBytes) + " (" + ramPct + " %)"
  }), el(HealthStat, {
    label: t("node.health.processes"),
    value: health.runningProcesses
  }), el(HealthStat, {
    label: t("node.health.uptime"),
    value: fmtUptime(health.uptimeSeconds)
  })));
};
const NodeDetailModal = ({
  id,
  admin,
  onClose,
  onPair,
  onPing,
  pinging
}) => {
  const t = useT();
  const node = id ? NODES.find(n => n.uuid === id) : null;
  const [healthFailure, setHealthFailure] = React.useState(null);
  React.useEffect(() => setHealthFailure(null), [id]);
  if (!node) return el(Modal, {
    open: false,
    onClose: onClose
  });
  const server = SERVERS.find(s => s.node_uuid === node.uuid);
  const blocked = node.compatibility === "Incompatible";
  const dash = "—";
  const notes = [el(CompatNote, {
    key: "compat",
    compat: node.compatibility
  }), needsRepair(healthFailure) && el(RepairNote, {
    key: "repair",
    failure: healthFailure
  }), !node.paired && el("div", {
    key: "unpaired",
    className: "note"
  }, t("node.detail.unpaired"))].filter(Boolean);
  const hasNotes = notes.length > 0 && !(notes.length === 1 && notes[0].key === "compat" && (!node.compatibility || node.compatibility === "Full"));
  return el(Modal, {
    open: true,
    onClose: onClose,
    title: node.name,
    lede: node.description || undefined,
    size: "lg",
    footer: el(React.Fragment, null, admin && node.paired && el("button", {
      className: "btn",
      onClick: () => onPing(node),
      disabled: pinging != null
    }, el(Icon, {
      name: "refresh",
      size: 14
    }), t("node.action.ping")), admin && el("button", {
      className: "btn" + (node.paired ? "" : " primary"),
      onClick: () => onPair(node),
      disabled: blocked
    }, el(Icon, {
      name: "key",
      size: 14
    }), t(node.paired ? "node.action.repair" : "node.action.pair")), el("button", {
      className: "btn",
      onClick: onClose
    }, t("btn.close", "Close")))
  }, hasNotes && el("div", {
    style: {
      display: "flex",
      flexDirection: "column",
      gap: 10,
      marginBottom: 12
    }
  }, notes), el(DetailRow, {
    label: t("node.detail.status")
  }, el(StatusPill, {
    status: node.status
  })), el(DetailRow, {
    label: t("node.detail.address")
  }, el("span", {
    className: "mono"
  }, node.ip, ":", node.port)), el(DetailRow, {
    label: t("node.detail.server")
  }, server ? server.name : dash), el(DetailRow, {
    label: t("node.detail.daemon_version")
  }, node.version ? el("span", {
    className: "mono"
  }, "v", node.version) : dash), el(DetailRow, {
    label: t("node.detail.protocol")
  }, node.protocolVersion ? el("span", {
    className: "mono"
  }, node.protocolVersion) : dash), el(DetailRow, {
    label: t("node.detail.compat")
  }, node.compatibility ? t("node.compat." + node.compatibility, node.compatibility) : dash), el(DetailRow, {
    label: t("node.detail.daemon_uuid")
  }, node.daemonUuid ? el("span", {
    className: "mono"
  }, node.daemonUuid) : dash), el(DetailRow, {
    label: t("node.detail.last_seen")
  }, node.lastSeen == null ? t("node.detail.never") : fmtAgo(node.lastSeen) + " ago (" + fmtAbs(node.lastSeen) + ")"), el(DetailRow, {
    label: t("node.detail.last_error")
  }, node.lastError || dash), admin && node.paired && !blocked && el(NodeHealth, {
    node: node,
    onFailure: setHealthFailure
  }), admin && node.paired && blocked && el("div", {
    className: "note",
    style: {
      marginTop: 18
    }
  }, t("node.health.blocked")));
};
window.NodesPage = NodesPage;
window.NewNodeModal = NewNodeModal;
