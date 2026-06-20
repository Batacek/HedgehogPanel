/* Generated from JSX via Babel (preset: react); this file is the source of record. */
/* Nodes — dedicated page (daemon instances). */

const NodesPage = ({
  onRefresh,
  refreshing
}) => {
  const t = useT();
  const [addOpen, setAddOpen] = React.useState(false);
  return /*#__PURE__*/React.createElement("div", {
    className: "page"
  }, /*#__PURE__*/React.createElement("div", {
    className: "page-head"
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("h1", null, t("page.nodes.title")), /*#__PURE__*/React.createElement("p", {
    className: "lede"
  }, t("page.nodes.lede"))), /*#__PURE__*/React.createElement("div", {
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
  }), t("btn.refresh")), /*#__PURE__*/React.createElement("button", {
    className: "btn primary",
    onClick: () => setAddOpen(true)
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "plus",
    size: 14
  }), t("btn.new_node")))), /*#__PURE__*/React.createElement("div", {
    className: "card flush"
  }, NODES.length === 0 ? /*#__PURE__*/React.createElement(EmptyState, {
    title: t("common.empty.servers"),
    icon: "node"
  }) : /*#__PURE__*/React.createElement("table", {
    className: "table"
  }, /*#__PURE__*/React.createElement("thead", null, /*#__PURE__*/React.createElement("tr", null, /*#__PURE__*/React.createElement("th", null, "Node"), /*#__PURE__*/React.createElement("th", null, "Server"), /*#__PURE__*/React.createElement("th", null, t("common.address")), /*#__PURE__*/React.createElement("th", null, t("common.status")), /*#__PURE__*/React.createElement("th", null, t("common.daemon")), /*#__PURE__*/React.createElement("th", null, t("common.last_seen")), /*#__PURE__*/React.createElement("th", null))), /*#__PURE__*/React.createElement("tbody", null, NODES.map(n => {
    const server = SERVERS.find(s => s.uuid === n.server_uuid);
    return /*#__PURE__*/React.createElement("tr", {
      key: n.uuid
    }, /*#__PURE__*/React.createElement("td", null, /*#__PURE__*/React.createElement("div", {
      className: "stack"
    }, /*#__PURE__*/React.createElement("strong", {
      style: {
        fontWeight: 600
      }
    }, n.name), /*#__PURE__*/React.createElement("span", {
      className: "sub"
    }, n.description))), /*#__PURE__*/React.createElement("td", null, server ? /*#__PURE__*/React.createElement("div", {
      className: "stack"
    }, /*#__PURE__*/React.createElement("span", {
      style: {
        fontWeight: 500
      }
    }, server.name), /*#__PURE__*/React.createElement("span", {
      className: "sub"
    }, /*#__PURE__*/React.createElement(KindPill, {
      kind: server.kind
    }))) : /*#__PURE__*/React.createElement("span", {
      style: {
        color: "var(--text-mute)"
      }
    }, "\u2014")), /*#__PURE__*/React.createElement("td", {
      className: "mono"
    }, n.ip, ":", n.port), /*#__PURE__*/React.createElement("td", null, /*#__PURE__*/React.createElement(StatusPill, {
      status: n.status
    })), /*#__PURE__*/React.createElement("td", {
      className: "mono"
    }, "v", n.version), /*#__PURE__*/React.createElement("td", {
      className: "mono"
    }, fmtAgo(n.lastSeen), " ago"), /*#__PURE__*/React.createElement("td", {
      className: "actions"
    }, /*#__PURE__*/React.createElement("button", {
      className: "btn ghost sm"
    }, /*#__PURE__*/React.createElement(Icon, {
      name: "more",
      size: 16
    }))));
  })))), /*#__PURE__*/React.createElement(NewNodeModal, {
    open: addOpen,
    onClose: () => setAddOpen(false)
  }));
};
const NewNodeModal = ({
  open,
  onClose
}) => {
  const t = useT();
  return /*#__PURE__*/React.createElement(Modal, {
    open: open,
    onClose: onClose,
    title: t("mod.node.title"),
    size: "md",
    footer: /*#__PURE__*/React.createElement("button", {
      className: "btn primary",
      onClick: onClose
    }, t("mod.node.gotit"))
  }, /*#__PURE__*/React.createElement("div", {
    className: "coming-soon",
    style: {
      margin: 0
    }
  }, /*#__PURE__*/React.createElement("div", {
    className: "ico"
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "info",
    size: 16
  })), /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("strong", null, t("mod.node.soon")), /*#__PURE__*/React.createElement("p", null, t("mod.node.soon.b")))));
};
window.NodesPage = NodesPage;
window.NewNodeModal = NewNodeModal;