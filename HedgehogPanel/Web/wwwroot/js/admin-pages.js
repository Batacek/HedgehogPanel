/* Generated from JSX via Babel (preset: react); this file is the source of record. */
/*  Shared "not yet available" banner  */

const NotAvailableBanner = ({
  title,
  body
}) => /*#__PURE__*/React.createElement("div", {
  style: {
    display: "flex",
    gap: 14,
    alignItems: "flex-start",
    padding: "14px 18px",
    borderRadius: 10,
    background: "var(--surface)",
    border: "1px solid var(--line)",
    marginBottom: 16
  }
}, /*#__PURE__*/React.createElement("div", {
  style: {
    width: 32,
    height: 32,
    borderRadius: 8,
    flexShrink: 0,
    background: "var(--surface-2)",
    color: "var(--text-mute)",
    display: "grid",
    placeItems: "center"
  }
}, /*#__PURE__*/React.createElement(Icon, {
  name: "info",
  size: 15
})), /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("div", {
  style: {
    fontWeight: 600,
    fontSize: 13.5,
    marginBottom: 3
  }
}, title), /*#__PURE__*/React.createElement("div", {
  style: {
    fontSize: 12.5,
    color: "var(--text-mute)",
    lineHeight: 1.5
  }
}, body)));

/*  Users  */

const AdminUsersPage = () => {
  const t = useT();
  const [note, setNote] = React.useState(null);
  const submit = e => {
    e.preventDefault();
    setNote({
      tone: "success",
      text: t("admin.users.created")
    });
    setTimeout(() => setNote(null), 3500);
    e.target.reset();
  };
  return /*#__PURE__*/React.createElement("div", {
    className: "page"
  }, /*#__PURE__*/React.createElement("div", {
    className: "page-head"
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("h1", null, t("admin.users.title")), /*#__PURE__*/React.createElement("p", {
    className: "lede"
  }, t("admin.users.lede")))), /*#__PURE__*/React.createElement("div", {
    className: "grid dash"
  }, /*#__PURE__*/React.createElement("div", {
    className: "card flush"
  }, /*#__PURE__*/React.createElement("div", {
    className: "card-head"
  }, /*#__PURE__*/React.createElement("h3", null, t("admin.users.accounts")), /*#__PURE__*/React.createElement("button", {
    className: "btn sm"
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "filter",
    size: 12
  }), t("common.filter"))), /*#__PURE__*/React.createElement("table", {
    className: "table"
  }, /*#__PURE__*/React.createElement("thead", null, /*#__PURE__*/React.createElement("tr", null, /*#__PURE__*/React.createElement("th", null, t("nav.users")), /*#__PURE__*/React.createElement("th", null, t("common.email")), /*#__PURE__*/React.createElement("th", null, t("common.group")), /*#__PURE__*/React.createElement("th", null, t("common.created")), /*#__PURE__*/React.createElement("th", null))), /*#__PURE__*/React.createElement("tbody", null, USERS.map(u => /*#__PURE__*/React.createElement("tr", {
    key: u.uuid
  }, /*#__PURE__*/React.createElement("td", null, /*#__PURE__*/React.createElement("div", {
    style: {
      display: "flex",
      gap: 10,
      alignItems: "center"
    }
  }, /*#__PURE__*/React.createElement(Avatar, {
    name: `${u.firstname} ${u.lastname}`
  }), /*#__PURE__*/React.createElement("div", {
    className: "stack"
  }, /*#__PURE__*/React.createElement("strong", {
    style: {
      fontWeight: 600
    }
  }, u.firstname, " ", u.lastname), /*#__PURE__*/React.createElement("span", {
    className: "sub mono"
  }, "@", u.username)))), /*#__PURE__*/React.createElement("td", null, u.email), /*#__PURE__*/React.createElement("td", null, u.isAdmin ? /*#__PURE__*/React.createElement(Pill, {
    tone: "accent"
  }, "admin") : /*#__PURE__*/React.createElement(Pill, null, u.group)), /*#__PURE__*/React.createElement("td", {
    className: "mono"
  }, fmtAbs(u.createdAt)), /*#__PURE__*/React.createElement("td", {
    className: "actions"
  }, /*#__PURE__*/React.createElement("button", {
    className: "btn ghost sm"
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "key",
    size: 12
  })), /*#__PURE__*/React.createElement("button", {
    className: "btn ghost sm"
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "more",
    size: 14
  })), u.username !== "admin" && /*#__PURE__*/React.createElement("button", {
    className: "btn ghost sm danger"
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "trash",
    size: 12
  })))))))), /*#__PURE__*/React.createElement("div", {
    className: "card"
  }, /*#__PURE__*/React.createElement("h3", {
    style: {
      marginTop: 0,
      fontSize: 14
    }
  }, t("admin.users.create")), /*#__PURE__*/React.createElement("p", {
    style: {
      color: "var(--text-mute)",
      fontSize: 12.5,
      marginTop: -6
    }
  }, t("admin.users.create.h")), /*#__PURE__*/React.createElement("form", {
    onSubmit: submit,
    style: {
      display: "flex",
      flexDirection: "column",
      gap: 12,
      marginTop: 12
    }
  }, note && /*#__PURE__*/React.createElement("div", {
    className: "note " + note.tone
  }, note.text), /*#__PURE__*/React.createElement("div", {
    className: "form-grid"
  }, /*#__PURE__*/React.createElement("div", {
    className: "field"
  }, /*#__PURE__*/React.createElement("label", null, t("common.username")), /*#__PURE__*/React.createElement("input", {
    className: "input",
    name: "username",
    required: true,
    placeholder: "jnovak"
  })), /*#__PURE__*/React.createElement("div", {
    className: "field"
  }, /*#__PURE__*/React.createElement("label", null, t("common.email")), /*#__PURE__*/React.createElement("input", {
    className: "input",
    name: "email",
    type: "email",
    required: true,
    placeholder: "j@example.com"
  }))), /*#__PURE__*/React.createElement("div", {
    className: "field"
  }, /*#__PURE__*/React.createElement("label", null, t("common.password")), /*#__PURE__*/React.createElement("input", {
    className: "input",
    name: "password",
    type: "password",
    required: true
  })), /*#__PURE__*/React.createElement("div", {
    className: "form-grid cols-3"
  }, /*#__PURE__*/React.createElement("div", {
    className: "field"
  }, /*#__PURE__*/React.createElement("label", null, t("common.first")), /*#__PURE__*/React.createElement("input", {
    className: "input",
    name: "firstName"
  })), /*#__PURE__*/React.createElement("div", {
    className: "field"
  }, /*#__PURE__*/React.createElement("label", null, t("common.middle")), /*#__PURE__*/React.createElement("input", {
    className: "input",
    name: "middleName"
  })), /*#__PURE__*/React.createElement("div", {
    className: "field"
  }, /*#__PURE__*/React.createElement("label", null, t("common.last")), /*#__PURE__*/React.createElement("input", {
    className: "input",
    name: "lastName"
  }))), /*#__PURE__*/React.createElement("div", {
    className: "field"
  }, /*#__PURE__*/React.createElement("label", null, t("common.group")), /*#__PURE__*/React.createElement("select", {
    className: "select",
    name: "group"
  }, GROUPS.map(g => /*#__PURE__*/React.createElement("option", {
    key: g.uuid
  }, g.name)))), /*#__PURE__*/React.createElement("button", {
    className: "btn primary",
    type: "submit"
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "plus",
    size: 14
  }), t("btn.create"))))));
};

/*  Groups  */

const AdminGroupsPage = () => {
  const t = useT();
  const h = React.createElement;
  const [groups, setGroups] = React.useState(() => Array.isArray(window.GROUPS) ? window.GROUPS : []);
  const [note, setNote] = React.useState(null);
  const [busy, setBusy] = React.useState(false);

  const flash = (tone, text) => {
    setNote({
      tone,
      text
    });
    setTimeout(() => setNote(null), 3500);
  };

  /* Reload groups (+ members) from the backend and publish them globally. */
  const reload = React.useCallback(async () => {
    if (typeof window.__hh_loadGroups !== "function") return;
    const fresh = await window.__hh_loadGroups();
    window.GROUPS = fresh;
    setGroups(fresh);
  }, []);
  React.useEffect(() => {
    reload();
  }, [reload]);

  /* Runs an API mutation, then refreshes and flashes a success/error note. */
  const mutate = async (request, okText) => {
    if (busy) return;
    setBusy(true);
    try {
      const res = await request();
      if (res && res.ok) {
        await reload();
        flash("success", okText);
        return true;
      }
      const data = res ? await res.json().catch(() => ({})) : {};
      flash("danger", data.error || t("admin.groups.action_failed", "The action failed."));
      return false;
    } catch (err) {
      flash("danger", t("admin.groups.action_failed", "The action failed."));
      return false;
    } finally {
      setBusy(false);
    }
  };

  const createGroup = async e => {
    e.preventDefault();
    const form = e.target;
    const fd = new FormData(form);
    const name = (fd.get("name") || "").trim();
    const description = (fd.get("description") || "").trim();
    if (!name) return;
    const ok = await mutate(() => window.__hh_api("/api/admin/groups", {
      method: "POST",
      body: JSON.stringify({
        name,
        description
      })
    }), t("admin.groups.created", "Group created."));
    if (ok) form.reset();
  };

  const deleteGroup = name => {
    if (!window.confirm(t("admin.groups.delete_confirm", "Delete this group?"))) return;
    mutate(() => window.__hh_api(`/api/admin/groups/${encodeURIComponent(name)}`, {
      method: "DELETE"
    }), t("admin.groups.deleted", "Group deleted."));
  };

  const addMember = async (e, groupName) => {
    e.preventDefault();
    const form = e.target;
    const fd = new FormData(form);
    const username = (fd.get("username") || "").trim();
    const priority = parseInt(fd.get("priority"), 10) || 0;
    if (!username) return;
    const ok = await mutate(() => window.__hh_api(`/api/admin/groups/${encodeURIComponent(groupName)}/members`, {
      method: "POST",
      body: JSON.stringify({
        username,
        priority
      })
    }), t("admin.groups.member_added", "Member added."));
    if (ok) form.reset();
  };

  const removeMember = (groupName, username) => {
    mutate(() => window.__hh_api(`/api/admin/groups/${encodeURIComponent(groupName)}/members/${encodeURIComponent(username)}`, {
      method: "DELETE"
    }), t("admin.groups.member_removed", "Member removed."));
  };

  const sectionLabel = text => h("div", {
    style: {
      fontSize: 11.5,
      color: "var(--text-mute)",
      textTransform: "uppercase",
      letterSpacing: "0.06em",
      marginBottom: 8,
      fontWeight: 600
    }
  }, text);

  const groupCard = g => h("div", {
    className: "card",
    key: g.uuid || g.name
  }, h("div", {
    style: {
      display: "flex",
      justifyContent: "space-between",
      alignItems: "flex-start",
      marginBottom: 12
    }
  }, h("div", null, h("div", {
    style: {
      display: "flex",
      gap: 8,
      alignItems: "center",
      marginBottom: 4
    }
  }, h(Pill, {
    tone: g.name === "admin" ? "accent" : undefined
  }, g.name)), g.description ? h("div", {
    style: {
      color: "var(--text-dim)",
      fontSize: 12.5
    }
  }, g.description) : null), g.name !== "admin" ? h("button", {
    className: "btn ghost sm danger",
    disabled: busy,
    title: t("admin.groups.delete", "Delete group"),
    onClick: () => deleteGroup(g.name)
  }, h(Icon, {
    name: "trash",
    size: 12
  })) : null), h("div", {
    style: {
      borderTop: "1px solid var(--line)",
      paddingTop: 12,
      marginTop: 12
    }
  }, sectionLabel([t("common.members"), " \xB7 ", g.members.length]), g.members.length > 0 ? h("div", {
    style: {
      display: "flex",
      flexWrap: "wrap",
      gap: 6
    }
  }, g.members.map(m => h("div", {
    key: m.userId || m.username,
    style: {
      display: "flex",
      gap: 6,
      alignItems: "center",
      padding: "4px 4px 4px 10px",
      background: "var(--surface-2)",
      borderRadius: 999,
      fontSize: 12
    }
  }, h("span", null, m.username), h("span", {
    style: {
      color: "var(--text-mute)",
      fontSize: 11
    }
  }, t("common.priority").toLowerCase(), " ", m.priority), h("button", {
    className: "btn ghost sm danger",
    disabled: busy,
    title: t("admin.groups.remove_member", "Remove member"),
    onClick: () => removeMember(g.name, m.username),
    style: {
      padding: "2px 6px"
    }
  }, h(Icon, {
    name: "x",
    size: 11
  }))))) : h("span", {
    style: {
      fontSize: 12,
      color: "var(--text-mute)"
    }
  }, t("admin.groups.no_members"))), h("form", {
    onSubmit: e => addMember(e, g.name),
    style: {
      display: "flex",
      gap: 6,
      marginTop: 12,
      alignItems: "flex-end",
      flexWrap: "wrap"
    }
  }, h("div", {
    className: "field",
    style: {
      flex: 1,
      minWidth: 120
    }
  }, h("label", null, t("common.username")), h("input", {
    className: "input",
    name: "username",
    required: true,
    placeholder: "jnovak"
  })), h("div", {
    className: "field",
    style: {
      width: 88
    }
  }, h("label", null, t("common.priority")), h("input", {
    className: "input",
    name: "priority",
    type: "number",
    min: "0",
    max: "255",
    defaultValue: "0"
  })), h("button", {
    className: "btn sm",
    type: "submit",
    disabled: busy
  }, h(Icon, {
    name: "plus",
    size: 12
  }), t("admin.groups.add_member", "Add"))));

  return h("div", {
    className: "page"
  }, h("div", {
    className: "page-head"
  }, h("div", null, h("h1", null, t("admin.groups.title")), h("p", {
    className: "lede"
  }, t("admin.groups.lede")))), note ? h("div", {
    className: "note " + note.tone,
    style: {
      marginBottom: 14
    }
  }, note.text) : null, h("div", {
    className: "card",
    style: {
      marginBottom: 16
    }
  }, h("h3", {
    style: {
      marginTop: 0,
      fontSize: 14
    }
  }, t("admin.groups.new")), h("form", {
    onSubmit: createGroup,
    style: {
      display: "flex",
      gap: 10,
      marginTop: 12,
      alignItems: "flex-end",
      flexWrap: "wrap"
    }
  }, h("div", {
    className: "field",
    style: {
      flex: 1,
      minWidth: 160
    }
  }, h("label", null, t("common.name")), h("input", {
    className: "input",
    name: "name",
    required: true,
    placeholder: "developers"
  })), h("div", {
    className: "field",
    style: {
      flex: 2,
      minWidth: 200
    }
  }, h("label", null, t("common.description")), h("input", {
    className: "input",
    name: "description"
  })), h("button", {
    className: "btn primary",
    type: "submit",
    disabled: busy
  }, h(Icon, {
    name: "plus",
    size: 14
  }), t("admin.groups.new")))), groups.length === 0 ? h(EmptyState, {
    title: t("admin.groups.empty", "No groups yet"),
    icon: "shield"
  }) : h("div", {
    className: "grid cols-3"
  }, groups.map(groupCard)));
};

/*  Permissions (tri-state, role-by-role)  */

const AdminPermissionsPage = () => {
  const t = useT();
  const rolesDesc = [...GROUPS].sort((a, b) => b.priority - a.priority);
  const [activeRole, setActiveRole] = React.useState(rolesDesc[0]?.uuid);
  const [grants, setGrants] = React.useState(() => JSON.parse(JSON.stringify(PERMISSION_GRANTS)));
  const setCell = (groupId, permKey, value) => {
    setGrants(prev => {
      const next = {
        ...prev,
        [groupId]: {
          ...(prev[groupId] || {})
        }
      };
      if (value == null) delete next[groupId][permKey];else next[groupId][permKey] = value;
      return next;
    });
  };
  const resolveEffective = (groupId, permKey) => {
    const start = GROUPS.find(g => g.uuid === groupId);
    if (!start) return {
      state: "deny",
      from: null
    };
    const chain = [...GROUPS].sort((a, b) => b.priority - a.priority).filter(g => g.priority <= start.priority);
    for (const g of chain) {
      const v = grants[g.uuid]?.[permKey];
      if (v) return {
        state: v,
        from: g.uuid === groupId ? null : g
      };
    }
    return {
      state: "deny",
      from: {
        name: t("perm.role.everyone")
      }
    };
  };
  return /*#__PURE__*/React.createElement("div", {
    className: "page"
  }, /*#__PURE__*/React.createElement("div", {
    className: "page-head"
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("h1", null, t("admin.perms.title")), /*#__PURE__*/React.createElement("p", {
    className: "lede"
  }, t("perm.role.lede")))), PERMISSIONS.length === 0 ? /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(NotAvailableBanner, {
    title: "Permissions management not yet available",
    body: "The backend API for permission grants hasn't been implemented yet. This section will let you configure per-group allow/deny rules for every panel action."
  }), /*#__PURE__*/React.createElement("div", {
    className: "coming-soon"
  }, /*#__PURE__*/React.createElement("div", {
    className: "ico"
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "lock",
    size: 16
  })), /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("strong", null, "Coming soon"), /*#__PURE__*/React.createElement("p", null, "Fine-grained permissions with tri-state allow / inherit / deny per group will be available once the backend support is added.")))) : /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("div", {
    className: "role-tabs",
    role: "tablist"
  }, rolesDesc.map(g => /*#__PURE__*/React.createElement("button", {
    key: g.uuid,
    className: activeRole === g.uuid ? "on" : "",
    onClick: () => setActiveRole(g.uuid)
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "shield",
    size: 13
  }), g.name, /*#__PURE__*/React.createElement("span", {
    className: "pri"
  }, "pri ", g.priority)))), /*#__PURE__*/React.createElement("div", {
    className: "card flush"
  }, /*#__PURE__*/React.createElement("table", {
    className: "perm-matrix"
  }, /*#__PURE__*/React.createElement("thead", null, /*#__PURE__*/React.createElement("tr", null, /*#__PURE__*/React.createElement("th", null, t("perm.col.perm")), /*#__PURE__*/React.createElement("th", {
    style: {
      width: 100
    }
  }, t("perm.col.scope")), /*#__PURE__*/React.createElement("th", {
    style: {
      width: 80
    }
  }, t("perm.col.risk")), /*#__PURE__*/React.createElement("th", {
    style: {
      width: 280
    }
  }, t("perm.col.state")), /*#__PURE__*/React.createElement("th", {
    style: {
      width: 220
    }
  }, t("perm.col.eff")))), /*#__PURE__*/React.createElement("tbody", null, PERMISSIONS.map(p => {
    const explicit = grants[activeRole]?.[p.key];
    const eff = resolveEffective(activeRole, p.key);
    return /*#__PURE__*/React.createElement("tr", {
      key: p.key
    }, /*#__PURE__*/React.createElement("td", null, /*#__PURE__*/React.createElement("div", {
      className: "perm-name"
    }, t("perm.label." + p.key)), /*#__PURE__*/React.createElement("div", {
      className: "perm-key"
    }, p.key)), /*#__PURE__*/React.createElement("td", null, /*#__PURE__*/React.createElement(Pill, null, p.scope)), /*#__PURE__*/React.createElement("td", null, /*#__PURE__*/React.createElement("span", {
      className: "perm-risk-" + p.risk
    }, p.risk)), /*#__PURE__*/React.createElement("td", null, /*#__PURE__*/React.createElement("div", {
      className: "tri-seg",
      role: "radiogroup"
    }, /*#__PURE__*/React.createElement("button", {
      className: explicit === "allow" ? "on allow" : "",
      onClick: () => setCell(activeRole, p.key, "allow")
    }, /*#__PURE__*/React.createElement(Icon, {
      name: "check",
      size: 11
    }), t("perm.state.allow")), /*#__PURE__*/React.createElement("button", {
      className: explicit == null ? "on inherit" : "",
      onClick: () => setCell(activeRole, p.key, null)
    }, t("perm.state.inherit")), /*#__PURE__*/React.createElement("button", {
      className: explicit === "deny" ? "on deny" : "",
      onClick: () => setCell(activeRole, p.key, "deny")
    }, /*#__PURE__*/React.createElement(Icon, {
      name: "x",
      size: 11
    }), t("perm.state.deny")))), /*#__PURE__*/React.createElement("td", null, /*#__PURE__*/React.createElement("div", {
      className: "tri-eff"
    }, /*#__PURE__*/React.createElement("span", {
      className: "pill-eff " + eff.state
    }, t("perm.state." + eff.state)), eff.from && /*#__PURE__*/React.createElement("span", {
      style: {
        marginLeft: 6
      }
    }, t("perm.inherit_from"), " ", /*#__PURE__*/React.createElement("strong", {
      style: {
        color: "var(--text-dim)"
      }
    }, eff.from.name)))));
  })))), /*#__PURE__*/React.createElement("div", {
    className: "note",
    style: {
      marginTop: 14
    }
  }, t("perm.explain"))));
};

/*  Audit log  */

const AdminAuditPage = () => {
  const t = useT();
  const [q, setQ] = React.useState("");
  const [cat, setCat] = React.useState("all");
  const matches = ev => {
    if (cat === "auth" && !["login_success", "login_failed", "logout", "User.Login.Success", "User.Login.Failed", "User.Login.Blocked", "User.Logout"].includes(ev.type)) return false;
    if (cat === "svc" && !["service_start", "service_stop", "user_created", "password_change", "User.Created", "User.Deleted", "User.Unlocked"].includes(ev.type)) return false;
    if (cat === "sys" && !["server_warning", "node_offline"].includes(ev.type)) return false;
    if (!q) return true;
    const s = q.toLowerCase();
    return ev.user.toLowerCase().includes(s) || ev.ip.toLowerCase().includes(s) || ev.type.toLowerCase().includes(s) || (ev.meta || "").toLowerCase().includes(s);
  };
  const filtered = EVENTS.filter(matches);
  return /*#__PURE__*/React.createElement("div", {
    className: "page"
  }, /*#__PURE__*/React.createElement("div", {
    className: "page-head"
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("h1", null, t("admin.audit.title")), /*#__PURE__*/React.createElement("p", {
    className: "lede"
  }, t("admin.audit.lede"))), /*#__PURE__*/React.createElement("div", {
    className: "page-actions"
  }, /*#__PURE__*/React.createElement("button", {
    className: "btn",
    disabled: EVENTS.length === 0
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "external",
    size: 14
  }), t("common.export")))), /*#__PURE__*/React.createElement(NotAvailableBanner, {
    title: "Audit log API not yet available",
    body: "The backend endpoint for audit events hasn't been implemented yet. Events will appear here automatically once the API is ready."
  }), /*#__PURE__*/React.createElement("div", {
    className: "card flush"
  }, /*#__PURE__*/React.createElement("div", {
    className: "toolbar"
  }, /*#__PURE__*/React.createElement("div", {
    className: "seg"
  }, [{
    k: "all",
    l: t("admin.audit.filter.all") + " · " + EVENTS.length
  }, {
    k: "auth",
    l: t("admin.audit.filter.auth")
  }, {
    k: "svc",
    l: t("admin.audit.filter.svc")
  }, {
    k: "sys",
    l: t("admin.audit.filter.sys")
  }].map(o => /*#__PURE__*/React.createElement("button", {
    key: o.k,
    className: cat === o.k ? "on" : "",
    onClick: () => setCat(o.k)
  }, o.l))), /*#__PURE__*/React.createElement("div", {
    className: "spacer"
  }), /*#__PURE__*/React.createElement("div", {
    className: "field",
    style: {
      maxWidth: 360,
      flex: 1
    }
  }, /*#__PURE__*/React.createElement("div", {
    className: "search",
    style: {
      minWidth: 0
    }
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "search",
    size: 14
  }), /*#__PURE__*/React.createElement("input", {
    style: {
      flex: 1,
      background: "transparent",
      border: 0,
      outline: 0,
      color: "var(--text)",
      fontSize: 13
    },
    placeholder: t("admin.audit.search"),
    value: q,
    onChange: e => setQ(e.target.value)
  })))), filtered.length === 0 ? /*#__PURE__*/React.createElement(EmptyState, {
    title: t("admin.audit.empty"),
    icon: "info"
  }) : /*#__PURE__*/React.createElement("div", {
    className: "activity",
    style: {
      padding: "8px 18px"
    }
  }, filtered.map(ev => {
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
    }, ev.user), " ", t("ev." + ev.type, ev.type)), /*#__PURE__*/React.createElement("div", {
      className: "meta"
    }, ev.ip, " \xB7 ", ev.meta)), /*#__PURE__*/React.createElement("div", {
      className: "time"
    }, fmtAgo(ev.at)));
  }))));
};

/*  Plugins  */

const AdminPluginsPage = () => {
  const t = useT();
  return /*#__PURE__*/React.createElement("div", {
    className: "page"
  }, /*#__PURE__*/React.createElement("div", {
    className: "page-head"
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("h1", null, t("admin.plugins.h1")), /*#__PURE__*/React.createElement("p", {
    className: "lede"
  }, t("admin.plugins.lede")))), /*#__PURE__*/React.createElement("div", {
    className: "coming-soon"
  }, /*#__PURE__*/React.createElement("div", {
    className: "ico"
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "rocket",
    size: 16
  })), /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("strong", null, t("admin.plugins.soon.t")), /*#__PURE__*/React.createElement("p", null, t("admin.plugins.soon.b")))), /*#__PURE__*/React.createElement("div", {
    className: "note",
    style: {
      marginTop: 14
    }
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "info",
    size: 13,
    style: {
      verticalAlign: "-2px",
      marginRight: 6,
      color: "var(--text-mute)"
    }
  }), t("admin.plugins.note")));
};

/*  Panel settings  */

const AdminSettingsPage = ({
  defaults,
  onChangeDefaults
}) => {
  const t = useT();
  const [palette, setPalette] = React.useState(defaults.palette);
  const [defaultLang, setDefaultLang] = React.useState(defaults.lang);
  const [saved, setSaved] = React.useState(false);
  const save = e => {
    e.preventDefault();
    onChangeDefaults?.({
      palette,
      lang: defaultLang
    });
    setSaved(true);
    setTimeout(() => setSaved(false), 2500);
  };
  return /*#__PURE__*/React.createElement("div", {
    className: "page",
    style: {
      maxWidth: 880
    }
  }, /*#__PURE__*/React.createElement("div", {
    className: "page-head"
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("h1", null, t("admin.settings.title")), /*#__PURE__*/React.createElement("p", {
    className: "lede"
  }, t("admin.settings.lede")))), /*#__PURE__*/React.createElement("div", {
    className: "card",
    style: {
      marginBottom: 16
    }
  }, /*#__PURE__*/React.createElement("h3", {
    style: {
      marginTop: 0,
      fontSize: 15
    }
  }, t("admin.settings.lang")), /*#__PURE__*/React.createElement("p", {
    style: {
      color: "var(--text-mute)",
      fontSize: 12.5,
      marginTop: -4
    }
  }, t("admin.settings.lang.h")), /*#__PURE__*/React.createElement("div", {
    style: {
      display: "flex",
      gap: 12,
      marginTop: 12,
      alignItems: "center"
    }
  }, /*#__PURE__*/React.createElement("div", {
    className: "lang-seg"
  }, /*#__PURE__*/React.createElement("button", {
    className: defaultLang === "cs" ? "on" : "",
    onClick: () => setDefaultLang("cs")
  }, "\u010Ce\u0161tina"), /*#__PURE__*/React.createElement("button", {
    className: defaultLang === "en" ? "on" : "",
    onClick: () => setDefaultLang("en")
  }, "English")))), /*#__PURE__*/React.createElement("div", {
    className: "card",
    style: {
      marginBottom: 16
    }
  }, /*#__PURE__*/React.createElement("h3", {
    style: {
      marginTop: 0,
      fontSize: 15
    }
  }, t("admin.settings.theme")), /*#__PURE__*/React.createElement("p", {
    style: {
      color: "var(--text-mute)",
      fontSize: 12.5,
      marginTop: -4
    }
  }, t("admin.settings.theme.h")), /*#__PURE__*/React.createElement("div", {
    style: {
      display: "flex",
      gap: 10,
      marginTop: 14,
      flexWrap: "wrap"
    }
  }, [{
    k: "forest",
    l: "Forest",
    colors: ["#83b06a", "#3b5c2a"]
  }, {
    k: "cobalt",
    l: "Cobalt",
    colors: ["#7fa9f5", "#2849a3"]
  }, {
    k: "ember",
    l: "Ember",
    colors: ["#e89763", "#a14a1f"]
  }, {
    k: "violet",
    l: "Violet",
    colors: ["#c79bf2", "#6b3aa3"]
  }].map(p => /*#__PURE__*/React.createElement("button", {
    key: p.k,
    onClick: () => setPalette(p.k),
    className: "btn",
    style: {
      padding: "10px 14px",
      background: palette === p.k ? "var(--surface-2)" : "var(--surface)",
      borderColor: palette === p.k ? "var(--accent)" : "var(--line)",
      gap: 10
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      display: "flex",
      gap: 2
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      width: 14,
      height: 14,
      borderRadius: "50%",
      background: p.colors[0]
    }
  }), /*#__PURE__*/React.createElement("span", {
    style: {
      width: 14,
      height: 14,
      borderRadius: "50%",
      background: p.colors[1],
      marginLeft: -6,
      border: "1px solid var(--bg)"
    }
  })), p.l)))), /*#__PURE__*/React.createElement("div", {
    className: "card",
    style: {
      marginBottom: 16
    }
  }, /*#__PURE__*/React.createElement("h3", {
    style: {
      marginTop: 0,
      fontSize: 15
    }
  }, t("admin.settings.lockout")), /*#__PURE__*/React.createElement("p", {
    style: {
      color: "var(--text-mute)",
      fontSize: 12.5,
      marginTop: -4
    }
  }, t("admin.settings.lockout.h")), /*#__PURE__*/React.createElement("div", {
    className: "form-grid",
    style: {
      marginTop: 12
    }
  }, /*#__PURE__*/React.createElement("div", {
    className: "field"
  }, /*#__PURE__*/React.createElement("label", null, t("admin.settings.max_fails")), /*#__PURE__*/React.createElement("input", {
    className: "input",
    defaultValue: "5"
  })), /*#__PURE__*/React.createElement("div", {
    className: "field"
  }, /*#__PURE__*/React.createElement("label", null, t("admin.settings.lock_minutes")), /*#__PURE__*/React.createElement("input", {
    className: "input",
    defaultValue: "15"
  })))), /*#__PURE__*/React.createElement("div", {
    style: {
      display: "flex",
      justifyContent: "flex-end",
      gap: 10,
      alignItems: "center"
    }
  }, saved && /*#__PURE__*/React.createElement("span", {
    style: {
      fontSize: 12.5,
      color: "var(--ok-2)"
    }
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "check",
    size: 12,
    style: {
      verticalAlign: "-2px",
      marginRight: 4
    }
  }), t("admin.settings.saved")), /*#__PURE__*/React.createElement("button", {
    className: "btn primary",
    onClick: save
  }, t("btn.save"))));
};
Object.assign(window, {
  AdminUsersPage,
  AdminGroupsPage,
  AdminPermissionsPage,
  AdminAuditPage,
  AdminPluginsPage,
  AdminSettingsPage
});