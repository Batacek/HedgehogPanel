/* Nodes — dedicated page (daemon instances). */

const NodesPage = ({ onRefresh, refreshing }) => {
  const t = useT();
  const [addOpen, setAddOpen] = React.useState(false);

  return (
    <div className="page">
      <div className="page-head">
        <div>
          <h1>{t("page.nodes.title")}</h1>
          <p className="lede">{t("page.nodes.lede")}</p>
        </div>
        <div className="page-actions">
          <button className="btn" onClick={() => onRefresh?.()} disabled={refreshing}>
            <Icon name="refresh" size={14} style={refreshing ? { animation: "spin 0.8s linear infinite" } : undefined} />
            {t("btn.refresh")}
          </button>
          <button className="btn primary" onClick={() => setAddOpen(true)}>
            <Icon name="plus" size={14} />{t("btn.new_node")}
          </button>
        </div>
      </div>

      <div className="card flush">
        {NODES.length === 0 ? (
          <EmptyState title={t("common.empty.servers")} icon="node" />
        ) : (
          <table className="table">
            <thead>
              <tr>
                <th>Node</th>
                <th>Server</th>
                <th>{t("common.address")}</th>
                <th>{t("common.status")}</th>
                <th>{t("common.daemon")}</th>
                <th>{t("common.last_seen")}</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {NODES.map((n) => {
                const server = SERVERS.find(s => s.uuid === n.server_uuid);
                return (
                  <tr key={n.uuid}>
                    <td>
                      <div className="stack">
                        <strong style={{ fontWeight: 600 }}>{n.name}</strong>
                        <span className="sub">{n.description}</span>
                      </div>
                    </td>
                    <td>
                      {server ? (
                        <div className="stack">
                          <span style={{ fontWeight: 500 }}>{server.name}</span>
                          <span className="sub"><KindPill kind={server.kind} /></span>
                        </div>
                      ) : <span style={{ color: "var(--text-mute)" }}>—</span>}
                    </td>
                    <td className="mono">{n.ip}:{n.port}</td>
                    <td><StatusPill status={n.status} /></td>
                    <td className="mono">v{n.version}</td>
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

      <NewNodeModal open={addOpen} onClose={() => setAddOpen(false)} />
    </div>
  );
};

const NewNodeModal = ({ open, onClose }) => {
  const t = useT();
  return (
    <Modal
      open={open}
      onClose={onClose}
      title={t("mod.node.title")}
      size="md"
      footer={<button className="btn primary" onClick={onClose}>{t("mod.node.gotit")}</button>}
    >
      <div className="coming-soon" style={{ margin: 0 }}>
        <div className="ico"><Icon name="info" size={16} /></div>
        <div>
          <strong>{t("mod.node.soon")}</strong>
          <p>{t("mod.node.soon.b")}</p>
        </div>
      </div>
    </Modal>
  );
};

window.NodesPage = NodesPage;
window.NewNodeModal = NewNodeModal;
