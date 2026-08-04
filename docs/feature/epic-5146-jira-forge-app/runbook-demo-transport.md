# Runbook — the demo transport (#5661)

Raising and lowering the public Lighthouse used for epic 5146's demos and for slice 02's browser
verdict. Short on purpose: this gets run in the minutes before a call.

**Public hostname**: `https://cachy-desktop.tail70a661.ts.net` — stable, derived from the node name
and the tailnet, so it does not change between sessions. This is the string written into the Forge
manifest's `permissions.external.frames`; changing it costs a MAJOR version bump and a re-consent,
which is why it is written down here rather than remembered.

## Raise

Pick the backend first — they are not interchangeable:

| Backend | Port | Authentication | Use it for |
|---|---|---|---|
| Dev instance | `5169` | **Disabled** | The canned demo. Real recorded history |
| docker-compose (`lighthouse-app`) | `48331` | **Enabled** | The embed session — those endpoints do not exist under auth-disabled (D31) |

```bash
# 1. The dev instance is not a service; start it if you are using it.
cd Lighthouse.Backend/Lighthouse.Backend
ASPNETCORE_URLS=http://0.0.0.0:5169 ASPNETCORE_ENVIRONMENT=Development dotnet run

# 2. Point the funnel at the backend you want.
tailscale funnel --bg 5169      # canned demo
tailscale funnel --bg 48331     # embed work

# 3. Confirm it is actually serving what you think it is.
curl -s https://cachy-desktop.tail70a661.ts.net/api/latest/auth/mode
```

`ASPNETCORE_URLS` must bind `0.0.0.0`, not `localhost`. The funnel proxies to `127.0.0.1`, so a
loopback-only bind happens to work today — but the default `dotnet run` profile listens on `:5000`,
not `:5169`, and that mismatch is a silent 502 rather than an error message.

Switching backends needs **no Forge redeploy**: the hostname is unchanged, so the declared origin in
the manifest still matches. Verified 2026-08-04 by swapping `5169` ↔ `48331` and watching
`auth/mode` flip from `Disabled` to `Enabled` on the same URL.

## Lower

```bash
tailscale funnel --https=443 off
tailscale funnel status          # expect: No serve config
```

**Funnel does not stop on its own.** The config is persistent — it survives closing the terminal and
survives a reboot. Lowering it is a deliberate command, and M2 requires it after every session.

## What is exposed while it is up

With the dev instance behind it, authentication is disabled, so RBAC is not enforced and **anyone
holding the URL is effectively a system administrator of that instance** — not a reader. The
maintainer assessed that database as holding nothing sensitive (2026-08-04, D41) and accepted this
for a funnel raised per session. The acceptance attaches to *that database*: point the funnel at
anything else and the judgement has to be made again.

The hostname is linked from nowhere — not the website, not the docs, not a public repository (M3).

## At epic close (M8, AC6)

```bash
tailscale funnel --https=443 off
sudo tailscale down
# uninstall tailscale; remove the node from the tailnet in the admin console
```

The transport is scaffolding for a feasibility question. Scaffolding that outlives its question
becomes a standing exposure nobody is deciding about any more.

## Setup notes, recorded so they are not rediscovered

- **Funnel must be enabled per tailnet**, once, through a node-scoped link the CLI prints
  (`https://login.tailscale.com/f/funnel?node=…`). Until then `tailscale funnel` hangs with no output
  when its stdout is piped.
- **Serve config needs root** unless the operator is set: `sudo tailscale set --operator=$USER`, once.
  Reversible with an empty value.
- **Tailscale's edge adds no framing headers.** Verified on the live funnel: no `X-Frame-Options` and
  no `Content-Security-Policy` in the response. R3 holds through the transport, which was not a given
  — an edge that injected either would have broken the epic silently.
- A `/etc/resolv.conf` health warning on this machine (pihole holds the file) affects MagicDNS name
  resolution locally and has no bearing on Funnel, which is served from Tailscale's edge.
