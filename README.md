# 🦔 Hedgehog Panel

[![License: Apache](https://img.shields.io/badge/License-Apache%202.0-yellow.svg)](https://opensource.org/licenses/Apache-2-0)
![C#](https://img.shields.io/badge/C%23-239120?logo=c-sharp&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-8-blue)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?logo=postgresql&logoColor=white)
 ![HTML5](https://img.shields.io/badge/HTML5-E34F26?logo=html5&logoColor=white) ![CSS3](https://img.shields.io/badge/CSS3-1572B6?logo=css3&logoColor=white) ![JavaScript](https://img.shields.io/badge/JavaScript-F7DF1E?logo=javascript&logoColor=black)

Hedgehog Panel is a personal project that is being built to gain experience and to create a C# alternative to existing server management panels like Pterodactyl or Portainer.

---
## Overview
Hedgehog Panel consists of two main components:

- **Backend (C#)** — Handles API requests, user management, and service configuration.
- **Daemon (Rust)** — A lightweight and secure background service responsible for directly controlling server processes and services. It lives in its own repository, [HedgehogDaemon](https://github.com/Batacek/HedgehogDaemon), and runs on every machine the panel manages. One machine with a daemon is a **node** in the panel.

## Technologies

| Component | Language / Framework |
|-----------|----------------------|
| Backend   | C# (.NET 8)          |
| Daemon    | Rust                 |
| Frontend  | HTML/CSS/JS          |
| Database  | PostgreSQL           |

---
## Connecting a daemon to the panel

A node is the panel's record of one machine running a daemon. Adding a node and pairing it takes four steps, all on the **Nodes** page, which only admins can change.

1. **Start the daemon.** A daemon that has no panel paired yet prints a one-time code at startup (`Pairing code: XXXX-XXXX`) and writes it to `./data/pairing-code`. To get a new code, run `hedgehog-daemon pair` in the daemon's directory; any earlier code stops working. A code is valid for 15 minutes and can be used once.
2. **Add the node.** Choose *Add node* and enter a name, the address of the machine and the daemon's port (50051 by default).
3. **Pair it.** The pairing dialog opens right after, or use *Pair* on the node. Enter the code; case and the dash do not matter. The panel checks the daemon's protocol version first, and a daemon it cannot work with is refused before the code is spent.
4. **Check on it.** *Ping* asks the daemon for a handshake, and the node's details show its versions, when it was last seen and the load of its host (CPU, memory, processes, uptime).

Paired nodes are also checked in the background, every 30 seconds by default, so their status stays current.

### What a status means

| Status | Meaning |
| --- | --- |
| Unpaired | The node has no daemon token yet. |
| Online | The daemon answered the last check and its protocol works with this panel. |
| Offline | The daemon could not be reached, did not answer in time, or the node's address is unusable. The error is shown. |
| Incompatible | The daemon answered, but its protocol version cannot work with this panel. Pairing and reading the load are blocked until the daemon is updated. |
| Unknown | The daemon failed in some other way, for example it rejected the panel's token. The error is shown. |

A daemon on a different minor protocol version works with limited features, and one on a pre-release protocol gets a warning. Both are marked on the node.

### Configuration

The `Daemon` section of `appsettings.json`, or environment variables written as `Daemon__PollIntervalSeconds`:

| Setting | Default | Meaning |
| --- | --- | --- |
| `RequestTimeoutSeconds` | `10` | Deadline of every call to a daemon |
| `RetryCount` | `3` | Retries of a call that failed as unavailable or timed out; pairing is never retried |
| `RetryDelayMilliseconds` | `200` | Wait before the first retry; each further retry waits longer |
| `AllowInsecureConnections` | `false` | Skip certificate validation. Only relevant once daemons offer TLS |
| `PollingEnabled` | `true` | Switches the background checks off |
| `PollIntervalSeconds` | `30` | Pause between rounds of checks |
| `PollMaxParallelism` | `8` | How many daemons are checked at once |

### The panel's identity

A daemon trusts a panel by the token it issued and the panel's UUID, which the panel keeps in `data/panel-identity` next to itself, not in the database. A fresh install pointed at the same database therefore has a new identity: the daemons it was paired with accept only a handshake from it, so *Ping* still works but the load cannot be read. The node details say so. To get out of it, pair the node again with a new code.

Keep `data/panel-identity` with the panel (in Docker, on a volume), so that an update does not change the identity.

### Known limitations

- **No TLS.** The connection between panel and daemon is plain HTTP/2, so use it only on a network you trust, such as a private network or a VPN.
- **The daemon token is stored unencrypted** in the `nodes.daemon_token` column of the database. It is never sent to the browser. Encrypting it is planned.
- Nodes cannot be edited or deleted from the page yet, only through the API.
