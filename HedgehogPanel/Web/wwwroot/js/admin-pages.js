/*  Shared "not yet available" banner  */

const NotAvailableBanner = ({ title, body }) => (
  <div style={{
    display: "flex", gap: 14, alignItems: "flex-start",
    padding: "14px 18px", borderRadius: 10,
    background: "var(--surface)", border: "1px solid var(--line)",
    marginBottom: 16,
  }}>
    <div style={{
      width: 32, height: 32, borderRadius: 8, flexShrink: 0,
      background: "var(--surface-2)", color: "var(--text-mute)",
      display: "grid", placeItems: "center",
    }}>
      <Icon name="info" size={15} />
    </div>
    <div>
      <div style={{ fontWeight: 600, fontSize: 13.5, marginBottom: 3 }}>{title}</div>
      <div style={{ fontSize: 12.5, color: "var(--text-mute)", lineHeight: 1.5 }}>{body}</div>
    </div>
  </div>
);

/*  Users  */

const AdminUsersPage = () => {
  const t = useT();
  const [note, setNote] = React.useState(null);
  const submit = (e) => {
    e.preventDefault();
    setNote({ tone: "success", text: t("admin.users.created") });
    setTimeout(() => setNote(null), 3500);
    e.target.reset();
  };
  return (
    <div className="page">
      <div className="page-head">
        <div>
          <h1>{t("admin.users.title")}</h1>
          <p className="lede">{t("admin.users.lede")}</p>
        </div>
      </div>

      <div className="grid dash">
        <div className="card flush">
          <div className="card-head">
            <h3>{t("admin.users.accounts")}</h3>
            <button className="btn sm"><Icon name="filter" size={12} />{t("common.filter")}</button>
          </div>
          <table className="table">
            <thead>
              <tr>
                <th>{t("nav.users")}</th>
                <th>{t("common.email")}</th>
                <th>{t("common.group")}</th>
                <th>{t("common.created")}</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {USERS.map((u) => (
                <tr key={u.uuid}>
                  <td>
                    <div style={{ display: "flex", gap: 10, alignItems: "center" }}>
                      <Avatar name={`${u.firstname} ${u.lastname}`} />
                      <div className="stack">
                        <strong style={{ fontWeight: 600 }}>{u.firstname} {u.lastname}</strong>
                        <span className="sub mono">@{u.username}</span>
                      </div>
                    </div>
                  </td>
                  <td>{u.email}</td>
                  <td>{u.isAdmin ? <Pill tone="accent">admin</Pill> : <Pill>{u.group}</Pill>}</td>
                  <td className="mono">{fmtAbs(u.createdAt)}</td>
                  <td className="actions">
                    <button className="btn ghost sm"><Icon name="key" size={12} /></button>
                    <button className="btn ghost sm"><Icon name="more" size={14} /></button>
                    {u.username !== "admin" && <button className="btn ghost sm danger"><Icon name="trash" size={12} /></button>}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <div className="card">
          <h3 style={{ marginTop: 0, fontSize: 14 }}>{t("admin.users.create")}</h3>
          <p style={{ color: "var(--text-mute)", fontSize: 12.5, marginTop: -6 }}>{t("admin.users.create.h")}</p>
          <form onSubmit={submit} style={{ display: "flex", flexDirection: "column", gap: 12, marginTop: 12 }}>
            {note && <div className={"note " + note.tone}>{note.text}</div>}
            <div className="form-grid">
              <div className="field"><label>{t("common.username")}</label><input className="input" name="username" required placeholder="jnovak" /></div>
              <div className="field"><label>{t("common.email")}</label><input className="input" name="email" type="email" required placeholder="j@example.com" /></div>
            </div>
            <div className="field"><label>{t("common.password")}</label><input className="input" name="password" type="password" required /></div>
            <div className="form-grid cols-3">
              <div className="field"><label>{t("common.first")}</label><input className="input" name="firstName" /></div>
              <div className="field"><label>{t("common.middle")}</label><input className="input" name="middleName" /></div>
              <div className="field"><label>{t("common.last")}</label><input className="input" name="lastName" /></div>
            </div>
            <div className="field">
              <label>{t("common.group")}</label>
              <select className="select" name="group">
                {GROUPS.map(g => <option key={g.uuid}>{g.name}</option>)}
              </select>
            </div>
            <button className="btn primary" type="submit"><Icon name="plus" size={14} />{t("btn.create")}</button>
          </form>
        </div>
      </div>
    </div>
  );
};

/*  Groups  */

const AdminGroupsPage = () => {
  const t = useT();
  return (
    <div className="page">
      <div className="page-head">
        <div>
          <h1>{t("admin.groups.title")}</h1>
          <p className="lede">{t("admin.groups.lede")}</p>
        </div>
        <div className="page-actions">
          <button className="btn primary" disabled><Icon name="plus" size={14} />{t("admin.groups.new")}</button>
        </div>
      </div>

      {GROUPS.length === 0 ? (
        <>
          <NotAvailableBanner
            title="Groups management not yet available"
            body="The backend API for groups and roles hasn't been implemented yet. This section will allow you to create custom permission groups, set priorities, and assign users."
          />
          <div className="coming-soon">
            <div className="ico"><Icon name="shield" size={16} /></div>
            <div>
              <strong>Coming soon</strong>
              <p>Group management, role hierarchies and member assignment will be available once the backend support is added.</p>
            </div>
          </div>
        </>
      ) : (
        <div className="grid cols-3">
          {GROUPS.map((g) => {
            const grants = PERMISSION_GRANTS[g.uuid] || {};
            const granted = Object.entries(grants).filter(([, v]) => v === "allow").map(([k]) => k);
            const members = USERS.filter(u => u.group === g.name);
            return (
              <div className="card" key={g.uuid}>
                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", marginBottom: 12 }}>
                  <div>
                    <div style={{ display: "flex", gap: 8, alignItems: "center", marginBottom: 4 }}>
                      <Pill tone={g.color}>{g.name}</Pill>
                      <span style={{ fontSize: 11.5, color: "var(--text-mute)" }}>{t("common.priority").toLowerCase()} {g.priority}</span>
                    </div>
                    <div style={{ color: "var(--text-dim)", fontSize: 12.5 }}>{g.description}</div>
                  </div>
                  <button className="btn ghost sm"><Icon name="more" size={14} /></button>
                </div>

                <div style={{ borderTop: "1px solid var(--line)", paddingTop: 12, marginTop: 12 }}>
                  <div style={{ fontSize: 11.5, color: "var(--text-mute)", textTransform: "uppercase", letterSpacing: "0.06em", marginBottom: 8, fontWeight: 600 }}>
                    {t("common.members")} · {members.length}
                  </div>
                  <div style={{ display: "flex", flexWrap: "wrap", gap: 6 }}>
                    {members.length > 0 ? members.map(u => (
                      <div key={u.uuid} style={{ display: "flex", gap: 6, alignItems: "center", padding: "4px 10px 4px 4px", background: "var(--surface-2)", borderRadius: 999, fontSize: 12 }}>
                        <Avatar name={`${u.firstname} ${u.lastname}`} size={20} />
                        <span>{u.firstname} {u.lastname}</span>
                      </div>
                    )) : <span style={{ fontSize: 12, color: "var(--text-mute)" }}>{t("admin.groups.no_members")}</span>}
                  </div>
                </div>

                <div style={{ borderTop: "1px solid var(--line)", paddingTop: 12, marginTop: 12 }}>
                  <div style={{ fontSize: 11.5, color: "var(--text-mute)", textTransform: "uppercase", letterSpacing: "0.06em", marginBottom: 8, fontWeight: 600 }}>
                    {t("nav.perms")} · {granted.length} / {PERMISSIONS.length}
                  </div>
                  <div style={{ fontSize: 12, color: "var(--text-dim)" }}>
                    {granted.length === 0 ? t("common.no_special") :
                     granted.length === PERMISSIONS.length ? t("common.all_perms") :
                     granted.slice(0, 3).join(", ") + (granted.length > 3 ? ` + ${granted.length - 3} ${t("common.more")}` : "")}
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};

/*  Permissions (tri-state, role-by-role)  */

const AdminPermissionsPage = () => {
  const t = useT();
  const rolesDesc = [...GROUPS].sort((a, b) => b.priority - a.priority);
  const [activeRole, setActiveRole] = React.useState(rolesDesc[0]?.uuid);
  const [grants, setGrants] = React.useState(() => JSON.parse(JSON.stringify(PERMISSION_GRANTS)));

  const setCell = (groupId, permKey, value) => {
    setGrants((prev) => {
      const next = { ...prev, [groupId]: { ...(prev[groupId] || {}) } };
      if (value == null) delete next[groupId][permKey];
      else next[groupId][permKey] = value;
      return next;
    });
  };

  const resolveEffective = (groupId, permKey) => {
    const start = GROUPS.find(g => g.uuid === groupId);
    if (!start) return { state: "deny", from: null };
    const chain = [...GROUPS].sort((a, b) => b.priority - a.priority).filter(g => g.priority <= start.priority);
    for (const g of chain) {
      const v = grants[g.uuid]?.[permKey];
      if (v) return { state: v, from: g.uuid === groupId ? null : g };
    }
    return { state: "deny", from: { name: t("perm.role.everyone") } };
  };

  return (
    <div className="page">
      <div className="page-head">
        <div>
          <h1>{t("admin.perms.title")}</h1>
          <p className="lede">{t("perm.role.lede")}</p>
        </div>
      </div>

      {PERMISSIONS.length === 0 ? (
        <>
          <NotAvailableBanner
            title="Permissions management not yet available"
            body="The backend API for permission grants hasn't been implemented yet. This section will let you configure per-group allow/deny rules for every panel action."
          />
          <div className="coming-soon">
            <div className="ico"><Icon name="lock" size={16} /></div>
            <div>
              <strong>Coming soon</strong>
              <p>Fine-grained permissions with tri-state allow / inherit / deny per group will be available once the backend support is added.</p>
            </div>
          </div>
        </>
      ) : (
        <>
          <div className="role-tabs" role="tablist">
            {rolesDesc.map((g) => (
              <button
                key={g.uuid}
                className={activeRole === g.uuid ? "on" : ""}
                onClick={() => setActiveRole(g.uuid)}
              >
                <Icon name="shield" size={13} />
                {g.name}
                <span className="pri">pri {g.priority}</span>
              </button>
            ))}
          </div>

          <div className="card flush">
            <table className="perm-matrix">
              <thead>
                <tr>
                  <th>{t("perm.col.perm")}</th>
                  <th style={{ width: 100 }}>{t("perm.col.scope")}</th>
                  <th style={{ width: 80 }}>{t("perm.col.risk")}</th>
                  <th style={{ width: 280 }}>{t("perm.col.state")}</th>
                  <th style={{ width: 220 }}>{t("perm.col.eff")}</th>
                </tr>
              </thead>
              <tbody>
                {PERMISSIONS.map((p) => {
                  const explicit = grants[activeRole]?.[p.key];
                  const eff = resolveEffective(activeRole, p.key);
                  return (
                    <tr key={p.key}>
                      <td>
                        <div className="perm-name">{t("perm.label." + p.key)}</div>
                        <div className="perm-key">{p.key}</div>
                      </td>
                      <td><Pill>{p.scope}</Pill></td>
                      <td><span className={"perm-risk-" + p.risk}>{p.risk}</span></td>
                      <td>
                        <div className="tri-seg" role="radiogroup">
                          <button className={(explicit === "allow" ? "on allow" : "")} onClick={() => setCell(activeRole, p.key, "allow")}>
                            <Icon name="check" size={11} />{t("perm.state.allow")}
                          </button>
                          <button className={(explicit == null ? "on inherit" : "")} onClick={() => setCell(activeRole, p.key, null)}>
                            {t("perm.state.inherit")}
                          </button>
                          <button className={(explicit === "deny" ? "on deny" : "")} onClick={() => setCell(activeRole, p.key, "deny")}>
                            <Icon name="x" size={11} />{t("perm.state.deny")}
                          </button>
                        </div>
                      </td>
                      <td>
                        <div className="tri-eff">
                          <span className={"pill-eff " + eff.state}>{t("perm.state." + eff.state)}</span>
                          {eff.from && (
                            <span style={{ marginLeft: 6 }}>
                              {t("perm.inherit_from")} <strong style={{ color: "var(--text-dim)" }}>{eff.from.name}</strong>
                            </span>
                          )}
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          <div className="note" style={{ marginTop: 14 }}>
            {t("perm.explain")}
          </div>
        </>
      )}
    </div>
  );
};

/*  Audit log  */

const AdminAuditPage = () => {
  const t = useT();
  const [q, setQ]     = React.useState("");
  const [cat, setCat] = React.useState("all");

  const matches = (ev) => {
    if (cat === "auth" && !["login_success","login_failed","logout","User.Login.Success","User.Login.Failed","User.Login.Blocked","User.Logout"].includes(ev.type)) return false;
    if (cat === "svc"  && !["service_start","service_stop","user_created","password_change","User.Created","User.Deleted","User.Unlocked"].includes(ev.type)) return false;
    if (cat === "sys"  && !["server_warning","node_offline"].includes(ev.type)) return false;
    if (!q) return true;
    const s = q.toLowerCase();
    return ev.user.toLowerCase().includes(s)
        || ev.ip.toLowerCase().includes(s)
        || ev.type.toLowerCase().includes(s)
        || (ev.meta || "").toLowerCase().includes(s);
  };
  const filtered = EVENTS.filter(matches);

  return (
    <div className="page">
      <div className="page-head">
        <div>
          <h1>{t("admin.audit.title")}</h1>
          <p className="lede">{t("admin.audit.lede")}</p>
        </div>
        <div className="page-actions">
          <button className="btn" disabled={EVENTS.length === 0}><Icon name="external" size={14} />{t("common.export")}</button>
        </div>
      </div>

      <NotAvailableBanner
        title="Audit log API not yet available"
        body="The backend endpoint for audit events hasn't been implemented yet. Events will appear here automatically once the API is ready."
      />

      <div className="card flush">
        <div className="toolbar">
          <div className="seg">
            {[
              { k: "all",  l: t("admin.audit.filter.all") + " · " + EVENTS.length },
              { k: "auth", l: t("admin.audit.filter.auth") },
              { k: "svc",  l: t("admin.audit.filter.svc") },
              { k: "sys",  l: t("admin.audit.filter.sys") },
            ].map(o => (
              <button key={o.k} className={cat === o.k ? "on" : ""} onClick={() => setCat(o.k)}>{o.l}</button>
            ))}
          </div>
          <div className="spacer" />
          <div className="field" style={{ maxWidth: 360, flex: 1 }}>
            <div className="search" style={{ minWidth: 0 }}>
              <Icon name="search" size={14} />
              <input
                style={{ flex: 1, background: "transparent", border: 0, outline: 0, color: "var(--text)", fontSize: 13 }}
                placeholder={t("admin.audit.search")}
                value={q}
                onChange={(e) => setQ(e.target.value)}
              />
            </div>
          </div>
        </div>
        {filtered.length === 0 ? (
          <EmptyState title={t("admin.audit.empty")} icon="info" />
        ) : (
          <div className="activity" style={{ padding: "8px 18px" }}>
            {filtered.map((ev) => {
              const dec = EVENT_DECORATION[ev.type] || { ico: "info", tone: "default" };
              return (
                <div className="activity-item" key={ev.id}>
                  <div className={"dot " + dec.tone}><Icon name={dec.ico} size={12} /></div>
                  <div className="body">
                    <div><span className="who">{ev.user}</span> {t("ev." + ev.type, ev.type)}</div>
                    <div className="meta">{ev.ip} · {ev.meta}</div>
                  </div>
                  <div className="time">{fmtAgo(ev.at)}</div>
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
};

/*  Plugins  */

const AdminPluginsPage = () => {
  const t = useT();
  return (
    <div className="page">
      <div className="page-head">
        <div>
          <h1>{t("admin.plugins.h1")}</h1>
          <p className="lede">{t("admin.plugins.lede")}</p>
        </div>
      </div>

      <div className="coming-soon">
        <div className="ico"><Icon name="rocket" size={16} /></div>
        <div>
          <strong>{t("admin.plugins.soon.t")}</strong>
          <p>{t("admin.plugins.soon.b")}</p>
        </div>
      </div>

      <div className="note" style={{ marginTop: 14 }}>
        <Icon name="info" size={13} style={{ verticalAlign: "-2px", marginRight: 6, color: "var(--text-mute)" }} />
        {t("admin.plugins.note")}
      </div>
    </div>
  );
};

/*  Panel settings  */

const AdminSettingsPage = ({ defaults, onChangeDefaults }) => {
  const t = useT();
  const [palette, setPalette] = React.useState(defaults.palette);
  const [defaultLang, setDefaultLang] = React.useState(defaults.lang);
  const [saved, setSaved] = React.useState(false);

  const save = (e) => {
    e.preventDefault();
    onChangeDefaults?.({ palette, lang: defaultLang });
    setSaved(true);
    setTimeout(() => setSaved(false), 2500);
  };

  return (
    <div className="page" style={{ maxWidth: 880 }}>
      <div className="page-head">
        <div>
          <h1>{t("admin.settings.title")}</h1>
          <p className="lede">{t("admin.settings.lede")}</p>
        </div>
      </div>

      <div className="card" style={{ marginBottom: 16 }}>
        <h3 style={{ marginTop: 0, fontSize: 15 }}>{t("admin.settings.lang")}</h3>
        <p style={{ color: "var(--text-mute)", fontSize: 12.5, marginTop: -4 }}>{t("admin.settings.lang.h")}</p>
        <div style={{ display: "flex", gap: 12, marginTop: 12, alignItems: "center" }}>
          <div className="lang-seg">
            <button className={defaultLang === "cs" ? "on" : ""} onClick={() => setDefaultLang("cs")}>Čeština</button>
            <button className={defaultLang === "en" ? "on" : ""} onClick={() => setDefaultLang("en")}>English</button>
          </div>
        </div>
      </div>

      <div className="card" style={{ marginBottom: 16 }}>
        <h3 style={{ marginTop: 0, fontSize: 15 }}>{t("admin.settings.theme")}</h3>
        <p style={{ color: "var(--text-mute)", fontSize: 12.5, marginTop: -4 }}>{t("admin.settings.theme.h")}</p>
        <div style={{ display: "flex", gap: 10, marginTop: 14, flexWrap: "wrap" }}>
          {[
            { k: "forest", l: "Forest", colors: ["#83b06a", "#3b5c2a"] },
            { k: "cobalt", l: "Cobalt", colors: ["#7fa9f5", "#2849a3"] },
            { k: "ember",  l: "Ember",  colors: ["#e89763", "#a14a1f"] },
            { k: "violet", l: "Violet", colors: ["#c79bf2", "#6b3aa3"] },
          ].map((p) => (
            <button
              key={p.k}
              onClick={() => setPalette(p.k)}
              className="btn"
              style={{
                padding: "10px 14px",
                background: palette === p.k ? "var(--surface-2)" : "var(--surface)",
                borderColor: palette === p.k ? "var(--accent)" : "var(--line)",
                gap: 10,
              }}
            >
              <span style={{ display: "flex", gap: 2 }}>
                <span style={{ width: 14, height: 14, borderRadius: "50%", background: p.colors[0] }} />
                <span style={{ width: 14, height: 14, borderRadius: "50%", background: p.colors[1], marginLeft: -6, border: "1px solid var(--bg)" }} />
              </span>
              {p.l}
            </button>
          ))}
        </div>
      </div>

      <div className="card" style={{ marginBottom: 16 }}>
        <h3 style={{ marginTop: 0, fontSize: 15 }}>{t("admin.settings.lockout")}</h3>
        <p style={{ color: "var(--text-mute)", fontSize: 12.5, marginTop: -4 }}>{t("admin.settings.lockout.h")}</p>
        <div className="form-grid" style={{ marginTop: 12 }}>
          <div className="field"><label>{t("admin.settings.max_fails")}</label><input className="input" defaultValue="5" /></div>
          <div className="field"><label>{t("admin.settings.lock_minutes")}</label><input className="input" defaultValue="15" /></div>
        </div>
      </div>

      <div style={{ display: "flex", justifyContent: "flex-end", gap: 10, alignItems: "center" }}>
        {saved && <span style={{ fontSize: 12.5, color: "var(--ok-2)" }}><Icon name="check" size={12} style={{ verticalAlign: "-2px", marginRight: 4 }} />{t("admin.settings.saved")}</span>}
        <button className="btn primary" onClick={save}>{t("btn.save")}</button>
      </div>
    </div>
  );
};

Object.assign(window, {
  AdminUsersPage, AdminGroupsPage, AdminPermissionsPage,
  AdminAuditPage, AdminPluginsPage, AdminSettingsPage,
});
