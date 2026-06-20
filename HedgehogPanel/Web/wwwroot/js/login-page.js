/* Generated from JSX via Babel (preset: react); this file is the source of record. */
/* Login page — memorable brand-first design.
   Big wordmark, evocative tagline. Localized via i18n. */

const LoginPage = ({
  onSignIn
}) => {
  const t = useT();
  const [lang, setLang] = useLang();
  const [user, setUser] = React.useState("admin");
  const [pw, setPw] = React.useState("admin123");
  const [err, setErr] = React.useState("");
  const [busy, setBusy] = React.useState(false);
  const submit = e => {
    e.preventDefault();
    setErr("");
    if (!user || !pw) {
      setErr(t("login.err_empty"));
      return;
    }
    setBusy(true);
    setTimeout(() => {
      setBusy(false);
      if (user === "admin" || user === "default_user") onSignIn?.(user);else setErr(t("login.err_invalid"));
    }, 450);
  };
  return /*#__PURE__*/React.createElement("div", {
    className: "login-stage"
  }, /*#__PURE__*/React.createElement("div", {
    className: "login-poster"
  }, /*#__PURE__*/React.createElement("div", {
    className: "poster-head",
    style: {
      justifyContent: "space-between"
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: "flex",
      alignItems: "center",
      gap: 10
    }
  }, /*#__PURE__*/React.createElement("div", {
    className: "brand-mark"
  }, "H."), /*#__PURE__*/React.createElement("h1", null, "Hedgehog Panel")), /*#__PURE__*/React.createElement("div", {
    className: "lang-seg",
    role: "tablist",
    "aria-label": "Language"
  }, /*#__PURE__*/React.createElement("button", {
    className: lang === "cs" ? "on" : "",
    onClick: () => setLang("cs")
  }, "CS"), /*#__PURE__*/React.createElement("button", {
    className: lang === "en" ? "on" : "",
    onClick: () => setLang("en")
  }, "EN"))), /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("h2", {
    className: "login-wordmark"
  }, "hedgehog", /*#__PURE__*/React.createElement("span", {
    className: "dot"
  })), /*#__PURE__*/React.createElement("p", {
    className: "login-tagline"
  }, t("login.tagline")), /*#__PURE__*/React.createElement("p", {
    className: "login-subline"
  }, t("login.subline")), /*#__PURE__*/React.createElement("div", {
    className: "login-spikes",
    "aria-hidden": "true"
  }, /*#__PURE__*/React.createElement("i", null), /*#__PURE__*/React.createElement("i", null), /*#__PURE__*/React.createElement("i", null), /*#__PURE__*/React.createElement("i", null), /*#__PURE__*/React.createElement("i", null), /*#__PURE__*/React.createElement("i", null), /*#__PURE__*/React.createElement("i", null), /*#__PURE__*/React.createElement("i", null), /*#__PURE__*/React.createElement("i", null))), /*#__PURE__*/React.createElement("div", {
    className: "poster-foot"
  }, /*#__PURE__*/React.createElement("span", null, "v1.2.2"), /*#__PURE__*/React.createElement("span", null, "\xB7"), /*#__PURE__*/React.createElement("span", null, "Apache-2.0"), /*#__PURE__*/React.createElement("span", null, "\xB7"), /*#__PURE__*/React.createElement("span", null, "batacek.eu"))), /*#__PURE__*/React.createElement("div", {
    className: "login-form-wrap"
  }, /*#__PURE__*/React.createElement("div", {
    className: "login-card"
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("h3", null, t("login.heading")), /*#__PURE__*/React.createElement("p", {
    className: "sub",
    style: {
      marginTop: 4
    }
  }, t("login.sub"))), /*#__PURE__*/React.createElement("form", {
    onSubmit: submit
  }, /*#__PURE__*/React.createElement("div", {
    className: "field"
  }, /*#__PURE__*/React.createElement("label", {
    htmlFor: "lf-user"
  }, t("login.username")), /*#__PURE__*/React.createElement("input", {
    className: "input",
    id: "lf-user",
    autoComplete: "username",
    value: user,
    onChange: e => setUser(e.target.value)
  })), /*#__PURE__*/React.createElement("div", {
    className: "field"
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: "flex",
      justifyContent: "space-between"
    }
  }, /*#__PURE__*/React.createElement("label", {
    htmlFor: "lf-pw"
  }, t("login.password")), /*#__PURE__*/React.createElement("a", {
    style: {
      fontSize: 12,
      color: "var(--text-mute)"
    },
    href: "#"
  }, t("login.forgot"))), /*#__PURE__*/React.createElement("input", {
    className: "input",
    id: "lf-pw",
    type: "password",
    autoComplete: "current-password",
    value: pw,
    onChange: e => setPw(e.target.value)
  })), err && /*#__PURE__*/React.createElement("div", {
    className: "note danger"
  }, err), /*#__PURE__*/React.createElement("button", {
    className: "btn primary",
    type: "submit",
    disabled: busy,
    style: {
      marginTop: 4
    }
  }, busy ? t("login.signing") : /*#__PURE__*/React.createElement(React.Fragment, null, t("login.submit"), " ", /*#__PURE__*/React.createElement(Icon, {
    name: "chevron_right",
    size: 14
  })))), /*#__PURE__*/React.createElement("div", {
    className: "ext-help"
  }, t("login.demo"), ":\xA0 ", /*#__PURE__*/React.createElement("code", null, "admin"), " / ", /*#__PURE__*/React.createElement("code", null, "admin123"), " \xA0\xB7\xA0", /*#__PURE__*/React.createElement("code", null, "default_user"), " / ", /*#__PURE__*/React.createElement("code", null, "user123")))));
};
window.LoginPage = LoginPage;