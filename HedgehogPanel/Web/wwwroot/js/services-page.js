/* Services — list + functional "New service" modal. */

const ServicesPage = ({ onOpenServer, onRefresh, refreshing }) => {
  const t = useT();
  const [query, setQuery]     = React.useState("");
  const [typeF, setTypeF]     = React.useState("all");
  const [statusF, setStatusF] = React.useState("all");
  const [createOpen, setCreateOpen] = React.useState(false);

  const counts = {};
  SERVICES.forEach(s => { counts[s.type] = (counts[s.type] || 0) + 1; });

  const filtered = SERVICES.filter((s) => {
    if (typeF !== "all" && s.type !== parseInt(typeF, 10)) return false;
    if (statusF !== "all" && s.status !== statusF) return false;
    if (!query) return true;
    const q = query.toLowerCase();
    return s.name.toLowerCase().includes(q) || s.description.toLowerCase().includes(q);
  });

  return (
    <div className="page">
      <div className="page-head">
        <div>
          <h1>{t("page.services.title")}</h1>
          <p className="lede">{t("page.services.lede")}</p>
        </div>
        <div className="page-actions">
          <button className="btn" onClick={() => onRefresh?.()} disabled={refreshing}>
            <Icon name="refresh" size={14} style={refreshing ? { animation: "spin 0.8s linear infinite" } : undefined} />
            {t("btn.sync")}
          </button>
          <button className="btn primary" onClick={() => setCreateOpen(true)}>
            <Icon name="plus" size={14} />{t("btn.new_service")}
          </button>
        </div>
      </div>

      <div className="grid cols-4" style={{ marginBottom: 14 }}>
        {Object.entries(SERVICE_TYPES).map(([id, ty]) => (
          <div className="kpi-card" key={id}>
            <div className="label" style={{ display: "flex", alignItems: "center", gap: 8 }}>
              <Icon name={ty.icon} size={13} /> {ty.label}
            </div>
            <div className="value">{counts[id] || 0}</div>
            <div className="delta">
              {t("page.services.across").replace("{{n}}", new Set(SERVICES.filter(s => s.type === parseInt(id, 10)).map(s => s.server)).size)}
            </div>
          </div>
        ))}
      </div>

      <div className="card flush">
        <div className="toolbar">
          <div className="seg">
            {[
              { k: "all",     l: `${t("common.all")} · ${SERVICES.length}` },
              { k: "running", l: t("st.running") },
              { k: "stopped", l: t("st.stopped") },
              { k: "warning", l: t("st.warning") },
            ].map(opt => (
              <button key={opt.k} className={statusF === opt.k ? "on" : ""} onClick={() => setStatusF(opt.k)}>{opt.l}</button>
            ))}
          </div>

          <select className="select" value={typeF} onChange={(e) => setTypeF(e.target.value)} style={{ width: "auto", maxWidth: 180 }}>
            <option value="all">{t("page.services.all_types")}</option>
            {Object.entries(SERVICE_TYPES).map(([id, ty]) => (
              <option key={id} value={id}>{ty.label}</option>
            ))}
          </select>

          <div className="spacer" />

          <div className="field" style={{ maxWidth: 320, flex: 1 }}>
            <input className="input" placeholder={t("page.services.search")} value={query} onChange={(e) => setQuery(e.target.value)} />
          </div>
        </div>

        {filtered.length === 0 ? (
          <EmptyState title={t("common.empty")} icon="service" />
        ) : (
          <table className="table">
            <thead>
              <tr>
                <th>{t("common.name")}</th>
                <th>{t("common.type")}</th>
                <th>Server</th>
                <th>Port</th>
                <th>{t("common.status")}</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((sv) => {
                const type = SERVICE_TYPES[sv.type] || SERVICE_TYPES[4];
                const server = SERVERS.find(s => s.uuid === sv.server);
                return (
                  <tr key={sv.uuid}>
                    <td>
                      <div style={{ display: "flex", gap: 12, alignItems: "center" }}>
                        <div style={{
                          width: 30, height: 30, borderRadius: 8,
                          background: "var(--surface-2)", color: "var(--text-dim)",
                          display: "grid", placeItems: "center",
                        }}><Icon name={type.icon} size={14} /></div>
                        <div className="stack">
                          <strong style={{ fontWeight: 600 }}>{sv.name}</strong>
                          <span className="sub">{sv.description}</span>
                        </div>
                      </div>
                    </td>
                    <td><Pill>{type.label}</Pill></td>
                    <td>
                      <button
                        style={{ background: "none", border: 0, color: "var(--text)", textAlign: "left", padding: 0, cursor: "pointer" }}
                        onClick={() => onOpenServer?.(sv.server)}
                      >
                        <div className="stack">
                          <strong style={{ fontWeight: 500 }}>{server?.name}</strong>
                          <span className="sub mono">{server?.ip}:{server?.daemon_port}</span>
                        </div>
                      </button>
                    </td>
                    <td className="mono">{sv.port || "—"}</td>
                    <td><StatusPill status={sv.status} /></td>
                    <td className="actions">
                      {sv.status === "running" ? (
                        <>
                          <button className="btn ghost sm"><Icon name="restart" size={12} /></button>
                          <button className="btn ghost sm danger"><Icon name="stop" size={12} /></button>
                        </>
                      ) : (
                        <button className="btn ghost sm"><Icon name="play" size={12} /></button>
                      )}
                      <button className="btn ghost sm"><Icon name="more" size={14} /></button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>

      <NewServiceModal open={createOpen} onClose={() => setCreateOpen(false)} />
    </div>
  );
};

/*  New Service Modal  */

const NewServiceModal = ({ open, onClose, presetServer = null }) => {
  const t = useT();
  const [server, setServer] = React.useState(presetServer || "");
  const [type, setType]     = React.useState("3");
  const [name, setName]     = React.useState("");
  const [port, setPort]     = React.useState("");
  const [desc, setDesc]     = React.useState("");
  const [done, setDone]     = React.useState(false);

  React.useEffect(() => {
    if (open) {
      setServer(presetServer || "");
      setType("3"); setName(""); setPort(""); setDesc(""); setDone(false);
    }
  }, [open, presetServer]);

  const submit = (e) => {
    e.preventDefault();
    setDone(true);
    setTimeout(onClose, 1100);
  };

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={t("mod.svc.title")}
      lede={t("mod.svc.lede")}
      footer={
        <>
          <button className="btn" onClick={onClose}>{t("btn.cancel")}</button>
          <button className="btn primary" disabled={!server || !name || done} onClick={submit}>
            {done ? <><Icon name="check" size={13}/> Uloženo</> : <><Icon name="plus" size={13}/> {t("btn.create")}</>}
          </button>
        </>
      }
    >
      <form onSubmit={submit} style={{ display: "contents" }}>
        <div className="field">
          <label>{t("mod.svc.server")} *</label>
          <select className="select" value={server} onChange={(e) => setServer(e.target.value)} required>
            <option value="">— vyber server —</option>
            {SERVERS.map(s => (
              <option key={s.uuid} value={s.uuid}>{s.name}</option>
            ))}
          </select>
        </div>

        <div className="form-grid">
          <div className="field">
            <label>{t("mod.svc.type")} *</label>
            <select className="select" value={type} onChange={(e) => setType(e.target.value)} required>
              {Object.entries(SERVICE_TYPES).map(([id, ty]) => (
                <option key={id} value={id}>{ty.label}</option>
              ))}
            </select>
          </div>
          <div className="field">
            <label>{t("mod.svc.port")}</label>
            <input className="input" value={port} onChange={(e) => setPort(e.target.value)} placeholder="—" style={{ fontFamily: "var(--font-mono)" }} />
          </div>
        </div>

        <div className="field">
          <label>{t("mod.svc.name")} *</label>
          <input className="input" value={name} onChange={(e) => setName(e.target.value)} placeholder="mc-survival" required />
        </div>

        <div className="field">
          <label>{t("mod.svc.desc")}</label>
          <textarea className="input" rows="2" value={desc} onChange={(e) => setDesc(e.target.value)} placeholder="—" />
        </div>
      </form>
    </Modal>
  );
};

window.ServicesPage = ServicesPage;
window.NewServiceModal = NewServiceModal;
