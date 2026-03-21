---
title: Installation and Configuration
layout: home
nav_order: 2
has_children: true
---

# Installation

Lighthouse is available in two editions: **Standalone** and **Server**.

## Which Version Should I Use?

**Choose Standalone if:**
- You want to try Lighthouse for the first time
- You are the only person using Lighthouse on this machine
- You want a simple, native desktop experience with built-in automatic updates
- You do not need to share your instance with other users or access it from another machine
- You are on macOS (the Server edition is not available for macOS)

**Choose Server if:**
- Multiple people need access to the same Lighthouse instance simultaneously
- You want to host Lighthouse centrally (on a server, VM, or container)
- You need features like PostgreSQL, custom certificates, or running as a background service
- You want to run Lighthouse via Docker

---

## Standalone Edition

The Standalone edition is a native desktop application for **Windows, macOS, and Linux**. It installs and runs like any other app — no network configuration or server setup required. Updates are handled automatically in the background.

{: .note}
**Constraints:** The Standalone edition uses a local SQLite database stored on your machine. Only one Lighthouse database can exist per system, and the instance is not accessible from other machines or by other users.

See [Standalone Installation](./standalone.html) for platform-specific downloads and instructions.

---

## Server Edition

The Server edition runs as a web server accessible from any browser. It supports multiple simultaneous users, PostgreSQL, Docker, and advanced configuration options. It is available for **Windows**, **Linux**, and **Docker**.

See [Server Installation](./server.html) for all options including Docker setup and running as a background service.

---

# Configuration
Once you have successfully installed Lighthouse, check out the [Configuration Options](./configuration.html) to see all configuration options (like using custom certificates).