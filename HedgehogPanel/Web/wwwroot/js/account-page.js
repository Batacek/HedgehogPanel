/* Generated from JSX via Babel (preset: react); this file is the source of record. */
/* Account settings — first/last name, password, language. */

const AccountPage = ({
  user,
  onUpdateUser
}) => {
  const t = useT();
  const [lang, setLang] = useLang();
  const [first, setFirst] = React.useState(user.firstname || "");
  const [middle, setMiddle] = React.useState(user.middlename || "");
  const [last, setLast] = React.useState(user.lastname || "");
  const [profileSaved, setProfileSaved] = React.useState(false);
  const [curPw, setCurPw] = React.useState("");
  const [newPw, setNewPw] = React.useState("");
  const [confPw, setConfPw] = React.useState("");
  const [pwMsg, setPwMsg] = React.useState(null);
  const saveProfile = e => {
    e.preventDefault();
    onUpdateUser?.({
      firstname: first,
      middlename: middle,
      lastname: last
    });
    setProfileSaved(true);
    setTimeout(() => setProfileSaved(false), 2500);
  };
  const changePw = e => {
    e.preventDefault();
    if (newPw !== confPw) {
      setPwMsg({
        tone: "danger",
        text: t("acct.pw_mismatch")
      });
      return;
    }
    setPwMsg({
      tone: "success",
      text: t("acct.pw_changed")
    });
    setCurPw("");
    setNewPw("");
    setConfPw("");
    setTimeout(() => setPwMsg(null), 3000);
  };
  return /*#__PURE__*/React.createElement("div", {
    className: "page",
    style: {
      maxWidth: 880
    }
  }, /*#__PURE__*/React.createElement("div", {
    className: "page-head"
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("h1", null, t("acct.title")), /*#__PURE__*/React.createElement("p", {
    className: "lede"
  }, t("acct.lede")))), /*#__PURE__*/React.createElement("div", {
    className: "card",
    style: {
      marginBottom: 16
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: "flex",
      gap: 18,
      marginBottom: 18
    }
  }, /*#__PURE__*/React.createElement(Avatar, {
    name: `${first} ${last}`,
    size: 56
  }), /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("h3", {
    style: {
      margin: 0,
      fontSize: 16
    }
  }, t("acct.section.profile")), /*#__PURE__*/React.createElement("div", {
    style: {
      fontSize: 12.5,
      color: "var(--text-mute)",
      marginTop: 4
    }
  }, t("acct.profile.hint")))), /*#__PURE__*/React.createElement("form", {
    onSubmit: saveProfile
  }, /*#__PURE__*/React.createElement("div", {
    className: "form-grid"
  }, /*#__PURE__*/React.createElement("div", {
    className: "field"
  }, /*#__PURE__*/React.createElement("label", null, t("acct.username")), /*#__PURE__*/React.createElement("input", {
    className: "input",
    value: user.username,
    disabled: true,
    style: {
      fontFamily: "var(--font-mono)",
      opacity: 0.7
    }
  })), /*#__PURE__*/React.createElement("div", {
    className: "field"
  }, /*#__PURE__*/React.createElement("label", null, t("acct.email")), /*#__PURE__*/React.createElement("input", {
    className: "input",
    value: user.email,
    disabled: true,
    style: {
      opacity: 0.7
    }
  }))), /*#__PURE__*/React.createElement("div", {
    className: "form-grid cols-3",
    style: {
      marginTop: 14
    }
  }, /*#__PURE__*/React.createElement("div", {
    className: "field"
  }, /*#__PURE__*/React.createElement("label", null, t("acct.firstname")), /*#__PURE__*/React.createElement("input", {
    className: "input",
    value: first,
    onChange: e => setFirst(e.target.value)
  })), /*#__PURE__*/React.createElement("div", {
    className: "field"
  }, /*#__PURE__*/React.createElement("label", null, t("acct.middlename")), /*#__PURE__*/React.createElement("input", {
    className: "input",
    value: middle,
    onChange: e => setMiddle(e.target.value)
  })), /*#__PURE__*/React.createElement("div", {
    className: "field"
  }, /*#__PURE__*/React.createElement("label", null, t("acct.lastname")), /*#__PURE__*/React.createElement("input", {
    className: "input",
    value: last,
    onChange: e => setLast(e.target.value)
  }))), /*#__PURE__*/React.createElement("div", {
    style: {
      display: "flex",
      gap: 10,
      alignItems: "center",
      marginTop: 16,
      justifyContent: "flex-end"
    }
  }, profileSaved && /*#__PURE__*/React.createElement("span", {
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
  }), t("acct.saved")), /*#__PURE__*/React.createElement("button", {
    className: "btn primary",
    type: "submit"
  }, t("btn.save"))))), /*#__PURE__*/React.createElement("div", {
    className: "card",
    style: {
      marginBottom: 16
    }
  }, /*#__PURE__*/React.createElement("h3", {
    style: {
      marginTop: 0,
      fontSize: 16
    }
  }, t("acct.section.password")), /*#__PURE__*/React.createElement("p", {
    style: {
      fontSize: 12.5,
      color: "var(--text-mute)",
      marginTop: -2
    }
  }, t("acct.password.hint")), /*#__PURE__*/React.createElement("form", {
    onSubmit: changePw,
    style: {
      marginTop: 12
    }
  }, pwMsg && /*#__PURE__*/React.createElement("div", {
    className: "note " + pwMsg.tone,
    style: {
      marginBottom: 12
    }
  }, pwMsg.text), /*#__PURE__*/React.createElement("div", {
    className: "form-grid cols-3"
  }, /*#__PURE__*/React.createElement("div", {
    className: "field"
  }, /*#__PURE__*/React.createElement("label", null, t("acct.cur_pw")), /*#__PURE__*/React.createElement("input", {
    className: "input",
    type: "password",
    value: curPw,
    onChange: e => setCurPw(e.target.value),
    autoComplete: "current-password"
  })), /*#__PURE__*/React.createElement("div", {
    className: "field"
  }, /*#__PURE__*/React.createElement("label", null, t("acct.new_pw")), /*#__PURE__*/React.createElement("input", {
    className: "input",
    type: "password",
    value: newPw,
    onChange: e => setNewPw(e.target.value),
    autoComplete: "new-password"
  })), /*#__PURE__*/React.createElement("div", {
    className: "field"
  }, /*#__PURE__*/React.createElement("label", null, t("acct.confirm_pw")), /*#__PURE__*/React.createElement("input", {
    className: "input",
    type: "password",
    value: confPw,
    onChange: e => setConfPw(e.target.value),
    autoComplete: "new-password"
  }))), /*#__PURE__*/React.createElement("div", {
    style: {
      display: "flex",
      justifyContent: "flex-end",
      marginTop: 14
    }
  }, /*#__PURE__*/React.createElement("button", {
    className: "btn primary",
    type: "submit",
    disabled: !curPw || !newPw || !confPw
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "key",
    size: 13
  }), t("acct.section.password"))))), /*#__PURE__*/React.createElement("div", {
    className: "card"
  }, /*#__PURE__*/React.createElement("h3", {
    style: {
      marginTop: 0,
      fontSize: 16
    }
  }, t("acct.section.language")), /*#__PURE__*/React.createElement("p", {
    style: {
      fontSize: 12.5,
      color: "var(--text-mute)",
      marginTop: -2
    }
  }, t("acct.lang.hint")), /*#__PURE__*/React.createElement("div", {
    style: {
      display: "flex",
      gap: 14,
      marginTop: 14,
      alignItems: "center"
    }
  }, /*#__PURE__*/React.createElement("label", {
    style: {
      fontSize: 13,
      fontWeight: 500
    }
  }, t("acct.lang.label")), /*#__PURE__*/React.createElement("div", {
    className: "lang-seg"
  }, /*#__PURE__*/React.createElement("button", {
    className: lang === "cs" ? "on" : "",
    onClick: () => setLang("cs")
  }, "\u010Ce\u0161tina"), /*#__PURE__*/React.createElement("button", {
    className: lang === "en" ? "on" : "",
    onClick: () => setLang("en")
  }, "English")))));
};
window.AccountPage = AccountPage;