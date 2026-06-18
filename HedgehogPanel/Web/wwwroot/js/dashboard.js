/* Dashboard / Home — server status grid and activity feed. */

const DashboardPage = ({ user, onNav, onOpenServer, onRefresh, refreshing }) => {
  const t = useT();

  return (
    <div className="page">
      <div className="page-head">
        <div>
          <h1>{t("dash.greet")}, {user?.firstname || "uživateli"} 👋</h1>
          <p className="lede">{t("dash.subtitle")} · {fmtAbs(new Date())}</p>
        </div>
        <div className="page-actions">
          <button className="btn" onClick={() => onRefresh?.()} disabled={refreshing}>
            <Icon name="refresh" size={14} style={refreshing ? { animation: "spin 0.8s linear infinite" } : undefined} />
            {t("btn.refresh")}
          </button>
          <button className="btn primary" onClick={() => onNav?.("servers")}>
            <Icon name="plus" size={14} />{t("btn.new_server")}
          </button>
        </div>
      </div>

      <div className="grid dash">
        <div className="card flush">
          <div className="card-head">
            <div>
              <h3>{t("dash.servers.title")}</h3>
              <div className="sub">{t("dash.servers.sub")}</div>
            </div>
            <button className="btn ghost sm" onClick={() => onNav?.("servers")}>
              {t("dash.servers.all")} <Icon name="chevron_right" size={12} />
            </button>
          </div>

          <div className="grid cols-2" style={{ padding: 16 }}>
            {SERVERS.slice(0, 4).map((s) => (
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
                  <span><Icon name="clock" size={11} style={{ verticalAlign: "-2px" }} /> {s.uptime}</span>
                </div>
              </button>
            ))}
            {SERVERS.length === 0 && (
              <div style={{ gridColumn: "1/-1" }}>
                <EmptyState title={t("common.empty.servers")} icon="server" />
              </div>
            )}
          </div>
        </div>

        <div className="card flush">
          <div className="card-head">
            <div>
              <h3>{t("dash.activity.title")}</h3>
              <div className="sub">{t("dash.activity.sub")}</div>
            </div>
            <button className="btn ghost sm" onClick={() => onNav?.("admin.audit")}>
              {t("dash.activity.all")} <Icon name="chevron_right" size={12} />
            </button>
          </div>
          <div className="activity" style={{ padding: "4px 16px 8px" }}>
            {EVENTS.length === 0 ? (
              <EmptyState title={t("admin.audit.empty")} icon="info" />
            ) : EVENTS.slice(0, 7).map((ev) => {
              const dec = EVENT_DECORATION[ev.type] || { ico: "info", tone: "default" };
              const label = t("ev." + ev.type, ev.type);
              return (
                <div className="activity-item" key={ev.id}>
                  <div className={"dot " + dec.tone}><Icon name={dec.ico} size={12} /></div>
                  <div className="body">
                    <div><span className="who">{ev.user}</span> {label}</div>
                    <div className="meta">{ev.ip} · {ev.meta}</div>
                  </div>
                  <div className="time">{fmtAgo(ev.at)}</div>
                </div>
              );
            })}
          </div>
        </div>
      </div>

      <div className="card flush" style={{ marginTop: 16 }}>
        <div className="card-head">
          <div>
            <h3>{t("dash.nodes.title")}</h3>
            <div className="sub">{t("dash.nodes.sub")}</div>
          </div>
          <button className="btn ghost sm" onClick={() => onNav?.("nodes")}>
            {t("page.nodes.title")} <Icon name="chevron_right" size={12} />
          </button>
        </div>
        {NODES.length === 0 ? (
          <EmptyState title={t("common.empty.servers")} icon="node" />
        ) : (
          <table className="table">
            <thead>
              <tr>
                <th>Node</th><th>Server</th><th>{t("common.address")}</th>
                <th>{t("common.status")}</th><th>{t("common.last_seen")}</th><th></th>
              </tr>
            </thead>
            <tbody>
              {NODES.slice(0, 5).map((n) => {
                const server = SERVERS.find(s => s.uuid === n.server_uuid);
                return (
                  <tr key={n.uuid}>
                    <td>
                      <div className="stack">
                        <strong style={{ fontWeight: 600 }}>{n.name}</strong>
                        <span className="sub">{n.description}</span>
                      </div>
                    </td>
                    <td>{server?.name || "—"}</td>
                    <td className="mono">{n.ip}:{n.port}</td>
                    <td><StatusPill status={n.status} /></td>
                    <td className="mono">{fmtAgo(n.lastSeen)} ago</td>
                    <td className="actions">
                      <button className="btn ghost sm"><Icon name="more" size={16} /></button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
};

window.DashboardPage = DashboardPage;
