/* Generated from JSX via Babel (preset: react); this file is the source of record. */
/* Small UI primitives. */

const Pill = ({
  children,
  tone = "default",
  dot = false,
  mono = false,
  className = ""
}) => /*#__PURE__*/React.createElement("span", {
  className: `pill ${tone} ${mono ? "mono" : ""} ${className}`
}, dot && /*#__PURE__*/React.createElement("i", {
  className: "ldot"
}), children);
const StatusPill = ({
  status
}) => {
  const map = {
    running: {
      tone: "ok",
      label: "Running"
    },
    online: {
      tone: "ok",
      label: "Online"
    },
    stopped: {
      tone: "default",
      label: "Stopped"
    },
    offline: {
      tone: "danger",
      label: "Offline"
    },
    warning: {
      tone: "warn",
      label: "Warning"
    },
    degraded: {
      tone: "warn",
      label: "Degraded"
    }
  };
  const m = map[status] || {
    tone: "default",
    label: status
  };
  return /*#__PURE__*/React.createElement(Pill, {
    tone: m.tone,
    dot: true
  }, m.label);
};
const RolePill = ({
  role
}) => {
  const tone = role === "owner" ? "accent" : "default";
  return /*#__PURE__*/React.createElement(Pill, {
    tone: tone
  }, role);
};
const KindPill = ({
  kind
}) => {
  if (!kind) return null;
  return /*#__PURE__*/React.createElement(Pill, {
    tone: kind === "dedicated" ? "accent" : "default"
  }, kind === "dedicated" ? "Dedicated" : "Virtual");
};
const KpiCard = ({
  label,
  value,
  unit,
  delta,
  deltaTone = "up",
  spark
}) => /*#__PURE__*/React.createElement("div", {
  className: "kpi-card"
}, /*#__PURE__*/React.createElement("div", {
  className: "label"
}, label), /*#__PURE__*/React.createElement("div", {
  className: "value"
}, value, unit && /*#__PURE__*/React.createElement("span", {
  className: "unit"
}, unit)), delta && /*#__PURE__*/React.createElement("div", {
  className: "delta " + deltaTone
}, /*#__PURE__*/React.createElement(Icon, {
  name: deltaTone === "up" ? "arrow_up" : "arrow_down",
  size: 12
}), delta), spark && /*#__PURE__*/React.createElement("div", {
  className: "spark",
  style: {
    marginTop: 8
  }
}, spark));
const Sparkline = ({
  seed = "x",
  height = 36,
  tone
}) => {
  const N = 28;
  const pts = [];
  let h = 0;
  for (let i = 0; i < seed.length; i++) h = h * 31 + seed.charCodeAt(i) >>> 0;
  let x = h;
  for (let i = 0; i < N; i++) {
    x = x * 1103515245 + 12345 >>> 0;
    pts.push(x % 1000 / 1000);
  }
  const trend = i => i / (N - 1) * 0.5 + 0.25;
  const vals = pts.map((p, i) => p * 0.5 + trend(i));
  const min = Math.min(...vals),
    max = Math.max(...vals);
  const norm = vals.map(v => (v - min) / Math.max(0.001, max - min));
  const W = 100,
    H = height;
  const step = W / (N - 1);
  const path = norm.map((v, i) => `${i === 0 ? "M" : "L"} ${(i * step).toFixed(2)} ${(H - 4 - v * (H - 8)).toFixed(2)}`).join(" ");
  const area = `${path} L ${W} ${H} L 0 ${H} Z`;
  const color = tone === "danger" ? "var(--danger)" : tone === "warn" ? "var(--warn)" : "var(--accent)";
  return /*#__PURE__*/React.createElement("svg", {
    viewBox: `0 0 ${W} ${H}`,
    preserveAspectRatio: "none"
  }, /*#__PURE__*/React.createElement("path", {
    d: area,
    fill: color,
    opacity: "0.18"
  }), /*#__PURE__*/React.createElement("path", {
    d: path,
    fill: "none",
    stroke: color,
    strokeWidth: "1.4",
    strokeLinejoin: "round",
    strokeLinecap: "round"
  }));
};
const Meter = ({
  value,
  tone
}) => {
  const t = tone || (value > 85 ? "danger" : value > 70 ? "warn" : "");
  return /*#__PURE__*/React.createElement("div", {
    className: "meter " + (t || "")
  }, /*#__PURE__*/React.createElement("i", {
    style: {
      width: `${Math.min(100, Math.max(0, value))}%`
    }
  }));
};
const Avatar = ({
  name,
  size = 26
}) => {
  const initials = (name || "?").split(/\s+/).map(s => s[0]).slice(0, 2).join("").toUpperCase();
  return /*#__PURE__*/React.createElement("div", {
    style: {
      width: size,
      height: size,
      borderRadius: "50%",
      background: "linear-gradient(160deg, var(--accent-2), var(--accent))",
      color: "oklch(0.12 0.01 80)",
      display: "grid",
      placeItems: "center",
      fontWeight: 700,
      fontSize: Math.round(size * 0.42),
      flexShrink: 0
    }
  }, initials);
};
const EmptyState = ({
  icon = "info",
  title,
  body,
  action
}) => /*#__PURE__*/React.createElement("div", {
  style: {
    padding: "40px 20px",
    textAlign: "center",
    color: "var(--text-mute)",
    display: "flex",
    flexDirection: "column",
    alignItems: "center",
    gap: 8
  }
}, /*#__PURE__*/React.createElement("div", {
  style: {
    width: 38,
    height: 38,
    borderRadius: 10,
    background: "var(--surface-2)",
    display: "grid",
    placeItems: "center",
    color: "var(--text-dim)"
  }
}, /*#__PURE__*/React.createElement(Icon, {
  name: icon,
  size: 18
})), /*#__PURE__*/React.createElement("div", {
  style: {
    fontWeight: 600,
    color: "var(--text)",
    fontSize: 14
  }
}, title), body && /*#__PURE__*/React.createElement("div", {
  style: {
    maxWidth: 380,
    fontSize: 12.5,
    lineHeight: 1.5
  }
}, body), action);

/* ── Modal ────────────────────────────────────────────────────────── */

const Modal = ({
  open,
  onClose,
  title,
  lede,
  children,
  footer,
  size = "md"
}) => {
  React.useEffect(() => {
    if (!open) return;
    const prev = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    const onKey = e => {
      if (e.key === "Escape") onClose?.();
    };
    window.addEventListener("keydown", onKey);
    return () => {
      document.body.style.overflow = prev;
      window.removeEventListener("keydown", onKey);
    };
  }, [open, onClose]);
  if (!open) return null;
  return ReactDOM.createPortal(/*#__PURE__*/React.createElement("div", {
    className: "modal-overlay",
    onMouseDown: onClose
  }, /*#__PURE__*/React.createElement("div", {
    className: `modal modal-${size}`,
    onMouseDown: e => e.stopPropagation(),
    role: "dialog",
    "aria-modal": "true"
  }, /*#__PURE__*/React.createElement("div", {
    className: "modal-head"
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("h3", null, title), lede && /*#__PURE__*/React.createElement("div", {
    className: "modal-lede"
  }, lede)), /*#__PURE__*/React.createElement("button", {
    className: "icon-btn",
    onClick: onClose,
    "aria-label": "Close"
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "x",
    size: 16
  }))), /*#__PURE__*/React.createElement("div", {
    className: "modal-body"
  }, children), footer && /*#__PURE__*/React.createElement("div", {
    className: "modal-foot"
  }, footer))), document.body);
};
Object.assign(window, {
  Pill,
  StatusPill,
  RolePill,
  KindPill,
  KpiCard,
  Sparkline,
  Meter,
  Avatar,
  EmptyState,
  Modal
});