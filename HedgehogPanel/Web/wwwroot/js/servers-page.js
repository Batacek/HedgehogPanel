/* Servers — list / grid with functional "New server" modal. */

const ServersPage = ({ onOpenServer, onRefresh, refreshing }) => {
  const t = useT();
  const [view, setView]       = React.useState("list");
  const [query, setQuery]     = React.useState("");
  const [statusF, setStatusF] = React.useState("all");
  const [createOpen, setCreateOpen] = React.useState(false);

  const filtered = SERVERS.filter((s) => {
    if (statusF !== "all" && s.status !== statusF) return false;
    if (!query) return true;
    const q = query.toLowerCase();
    return s.name.toLowerCase().includes(q) || s.ip.toLowerCase().includes(q) || (s.description || "").toLowerCase().includes(q);
  });

  return (
    <div className="page">
      <div className="page-head">
        <div>
          <h1>{t("page.servers.title")}</h1>
          <p className="lede">{t("page.servers.lede")}</p>
        </div>
        <div className="page-actions">
          <button className="btn" onClick={() => onRefresh?.()} disabled={refreshing}>
            <Icon name="refresh" size={14} style={refreshing ? { animation: "spin 0.8s linear infinite" } : undefined} />
            {t("btn.sync")}
          </button>
          <button className="btn primary" onClick={() => setCreateOpen(true)}>
            <Icon name="plus" size={14} />{t("btn.new_server")}
          </button>
        </div>
      </div>

      <div className="card flush">
        <div className="toolbar">
          <div className="seg" role="tablist">
            {[
              { k: "all",     l: `${t("common.all")} · ${SERVERS.length}` },
              { k: "running", l: t("st.running") },
              { k: "stopped", l: t("st.stopped") },
              { k: "warning", l: t("st.warning") },
            ].map((opt) => (
              <button key={opt.k} className={statusF === opt.k ? "on" : ""} onClick={() => setStatusF(opt.k)}>
                {opt.l}
              </button>
            ))}
          </div>

          <div className="spacer" />

          <div className="field" style={{ flexDirection: "row" }}>
            <input
              className="input"
              placeholder={t("page.servers.search")}
              value={query}
              onChange={(e) => setQuery(e.target.value)}
            />
          </div>

          <div className="seg" role="tablist">
            <button className={view === "list" ? "on" : ""} onClick={() => setView("list")} title={t("common.view.table")}><Icon name="list" size={14} /></button>
            <button className={view === "grid" ? "on" : ""} onClick={() => setView("grid")} title={t("common.view.cards")}><Icon name="grid" size={14} /></button>
          </div>
        </div>

        {filtered.length === 0 && (
          <EmptyState title={t("common.empty.servers")} body={t("common.empty.try")} icon="server" />
        )}

        {filtered.length > 0 && view === "list" && (
          <table className="table">
            <thead>
              <tr>
                <th>Server</th>
                <th>{t("mod.server.kind")}</th>
                <th>{t("common.status")}</th>
                <th>IP : port</th>
                <th style={{ width: 110 }}>CPU</th>
                <th style={{ width: 110 }}>RAM</th>
                <th style={{ width: 110 }}>Disk</th>
                <th>{t("common.uptime")}</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((s) => {
                const parent = s.parent && SERVERS.find(x => x.uuid === s.parent);
                return (
                  <tr key={s.uuid} style={{ cursor: "pointer" }} onClick={() => onOpenServer?.(s.uuid)}>
                    <td>
                      <div className="stack">
                        <strong style={{ fontWeight: 600 }}>{s.name}</strong>
                        {parent ? (
                          <span className="srv-hier">
                            <Icon name="chevron_right" size={10} className="arrow" />
                            {t("page.servers.vm_on").replace("{{name}}", parent.name)}
                          </span>
                        ) : (
                          <span className="sub" style={{ fontSize: 11.5, color: "var(--text-mute)" }}>{s.description}</span>
                        )}
                      </div>
                    </td>
                    <td><KindPill kind={s.kind} /></td>
                    <td><StatusPill status={s.status} /></td>
                    <td className="mono">{s.ip !== "—" ? `${s.ip}:${s.daemon_port}` : "—"}</td>
                    <td><MeterCell value={s.cpu} /></td>
                    <td><MeterCell value={s.ram} /></td>
                    <td><MeterCell value={s.disk} /></td>
                    <td className="mono">{s.uptime}</td>
                    <td className="actions">
                      <button className="btn ghost sm" onClick={(e) => e.stopPropagation()}><Icon name="more" size={16} /></button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}

        {filtered.length > 0 && view === "grid" && (
          <div className="grid cols-3" style={{ padding: 16 }}>
            {filtered.map((s) => (
              <button key={s.uuid} className="server-card" onClick={() => onOpenServer?.(s.uuid)}>
                <div className="row head">
                  <div>
                    <div className="name">{s.name}</div>
                    <div className="host">{s.ip !== "—" ? `${s.ip}:${s.daemon_port}` : s.description || s.name}</div>
                  </div>
                  <StatusPill status={s.status} />
                </div>
                <StatLine icon="cpu"  label="CPU"  value={s.cpu} />
                <StatLine icon="ram"  label="RAM"  value={s.ram} />
                <StatLine icon="disk" label="Disk" value={s.disk} />
                <div style={{ display: "flex", justifyContent: "space-between", fontSize: 11.5, color: "var(--text-mute)" }}>
                  <KindPill kind={s.kind} />
                  <RolePill role={s.role} />
                </div>
              </button>
            ))}
          </div>
        )}
      </div>

      <NewServerModal open={createOpen} onClose={() => setCreateOpen(false)} />
    </div>
  );
};

const MeterCell = ({ value }) => {
  const tone = value > 85 ? "danger" : value > 70 ? "warn" : null;
  return (
    <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
      <div style={{ flex: 1 }}><Meter value={value} tone={tone} /></div>
      <span style={{ fontFamily: "var(--font-mono)", fontSize: 11.5, color: "var(--text-dim)", width: 32, textAlign: "right" }}>{value}%</span>
    </div>
  );
};

const StatLine = ({ icon, label, value }) => {
  const tone = value > 85 ? "danger" : value > 70 ? "warn" : null;
  return (
    <div className="stat-line">
      <span><Icon name={icon} size={12} style={{ verticalAlign: "-2px", marginRight: 5 }} />{label}</span>
      <Meter value={value} tone={tone} />
      <span className="val">{value}%</span>
    </div>
  );
};

/*  New Server Modal  */

const NewServerModal = ({ open, onClose }) => {
  const t = useT();
  const [ip, setIp]     = React.useState("");
  const [name, setName] = React.useState("");
  const [port, setPort] = React.useState("50051");
  const [kind, setKind] = React.useState("dedicated");
  const [parent, setParent] = React.useState("");
  const [desc, setDesc] = React.useState("");
  const [done, setDone] = React.useState(false);

  React.useEffect(() => {
    if (open) { setIp(""); setName(""); setPort("50051"); setKind("dedicated"); setParent(""); setDesc(""); setDone(false); }
  }, [open]);

  const dedicatedServers = SERVERS.filter(s => s.kind === "dedicated");

  const submit = (e) => {
    e.preventDefault();
    setDone(true);
    setTimeout(onClose, 1100);
  };

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={t("mod.server.title")}
      lede={t("mod.server.lede")}
      footer={
        <>
          <button className="btn" onClick={onClose}>{t("btn.cancel")}</button>
          <button className="btn primary" disabled={!ip || done} onClick={submit}>
            {done ? <><Icon name="check" size={13}/> Uloženo</> : <><Icon name="plus" size={13}/> {t("btn.create")}</>}
          </button>
        </>
      }
    >
      <form onSubmit={submit} style={{ display: "contents" }}>
        <div className="field">
          <label>{t("mod.server.ip")} *</label>
          <input className="input" value={ip} onChange={(e) => setIp(e.target.value)} placeholder="10.20.30.10" required style={{ fontFamily: "var(--font-mono)" }} autoFocus />
        </div>

        <div className="form-grid">
          <div className="field">
            <label>{t("mod.server.name")}</label>
            <input className="input" value={name} onChange={(e) => setName(e.target.value)} placeholder="Web Server" />
          </div>
          <div className="field">
            <label>{t("mod.server.port")}</label>
            <input className="input" value={port} onChange={(e) => setPort(e.target.value)} style={{ fontFamily: "var(--font-mono)" }} />
          </div>
        </div>

        <div className="field">
          <label>{t("mod.server.kind")}</label>
          <div className="seg" style={{ alignSelf: "flex-start" }}>
            <button type="button" className={kind === "dedicated" ? "on" : ""} onClick={() => setKind("dedicated")}>{t("kind.dedicated")}</button>
            <button type="button" className={kind === "virtual" ? "on" : ""} onClick={() => setKind("virtual")}>{t("kind.virtual")}</button>
          </div>
        </div>

        {kind === "virtual" && (
          <div className="field">
            <label>{t("mod.server.parent")}</label>
            <select className="select" value={parent} onChange={(e) => setParent(e.target.value)}>
              <option value="">—</option>
              {dedicatedServers.map(s => <option key={s.uuid} value={s.uuid}>{s.name}</option>)}
            </select>
            <div className="help">{t("mod.server.parent.h")}</div>
          </div>
        )}

        <div className="field">
          <label>{t("mod.server.desc")}</label>
          <textarea className="input" rows="2" value={desc} onChange={(e) => setDesc(e.target.value)} placeholder="—" />
        </div>
      </form>
    </Modal>
  );
};

window.ServersPage = ServersPage;
window.MeterCell = MeterCell;
window.StatLine = StatLine;
window.NewServerModal = NewServerModal;
