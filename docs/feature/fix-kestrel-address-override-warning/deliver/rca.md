# RCA — Kestrel "Overriding address(es)" warning on startup

- **Work item**: Story 6044 (`Fix Kestrel Warning on Startup`)
- **Reported**: 2026-09-20, by the maintainer
- **Reported symptom**: `WARNING - Kestrel: Overriding address(es) 'http://localhost:5169'. Binding to endpoints defined via IConfiguration and/or UseKestrel() instead.` on every startup. It is a warning, so `RecentProblemsSink` picks it up and the Task Manager shows it to users as a problem.

## Root cause

ASP.NET Core resolves listening addresses from two independent mechanisms:

1. an **address source** — `ASPNETCORE_URLS`, `ASPNETCORE_HTTP_PORTS`/`ASPNETCORE_HTTPS_PORTS`, or a launch profile's `applicationUrl`; and
2. **`Kestrel:Endpoints`** configuration.

When both are present, `Kestrel:Endpoints` wins and the framework logs the discarded address source at Warning level. The warning is therefore not a malfunction — it is the framework correctly reporting that configuration was supplied and ignored. Removing it means removing the duplicate declaration, not silencing the logger.

Lighthouse declares both on two shipped surfaces.

### Docker — the user-facing one

The base image supplies an address source that the Dockerfile never clears:

```
$ docker run --rm --entrypoint printenv ghcr.io/letpeoplework/lighthouse:latest
ASPNETCORE_HTTP_PORTS=8080                    <- mcr.microsoft.com/dotnet/aspnet:10.0
Kestrel__Endpoints__Http__Url=http://+:80     <- Dockerfile
Kestrel__Endpoints__Https__Url=https://+:443  <- Dockerfile
```

Confirmed by running the published image:

| Run | Environment | Observed |
| --- | --- | --- |
| Baseline | image as shipped | `WARNING Kestrel: Overriding address(es) 'http://*:8080'`, then binds 80 / 443 |
| Fix candidate | `ASPNETCORE_HTTP_PORTS=` | no warning, binds 80 / 443 |
| Chart-shaped | `ASPNETCORE_HTTP_PORTS=` + chart's `Kestrel__Endpoints__*` | no warning, binds 8080 / 8443 |

The port the base image names has been discarded on every container start since the image moved to a .NET 8-or-later base; clearing it changes nothing but the log line. The Helm chart inherits the cleared variable, so it needs no change of its own.

### Development — the reported address

`Properties/launchSettings.json` sets `applicationUrl` on the `http` and `https` profiles, while `appsettings.Development.json` defines `Kestrel:Endpoints` for the same two ports. `Start-DevServer.ps1` runs `dotnet run --launch-profile http`, which is where the reported `http://localhost:5169` comes from.

### Why the standalone build never showed it

`appsettings.json` has no `Kestrel` section, and a standalone run has neither launch profile nor container environment. One address source, nothing to override, no warning — which is why the maintainer could not reproduce it on the Linux standalone build. This is corroborating evidence for the mechanism rather than a separate case.

## Fix

Keep `Kestrel:Endpoints` as the single mechanism — it is already what the Dockerfile, the Helm chart and the CI workflows use — and remove every competing address source:

- `Dockerfile`: `ENV ASPNETCORE_HTTP_PORTS=""` in the final stage, clearing the value inherited from the base image.
- `Properties/launchSettings.json`: drop `applicationUrl` from the `http` and `https` profiles.

Risk: **low**. Both runs above show the binding is unchanged; the discarded value was already being discarded.

## Regression guard

`Architecture/StartupAddressSourceCollisionTest.cs` asserts that no guarded surface declares an address source and `Kestrel:Endpoints` at the same time.

Known limit, stated so nobody mistakes the guard for more than it is: it reads the repository's own files. It catches a port variable being reintroduced; it cannot catch a future base image introducing an address variable under a name nobody has seen yet. The only thing that would is booting the image in CI and reading its log, and no workflow runs the Lighthouse image today — `ci_docker.yml` builds and pushes it, and `ci_verifypostgres` only starts a Postgres container. A dedicated job for one warning is not worth its runtime.

## Out of scope

`render.yaml` sets `ASPNETCORE_URLS=http://+:10000`, which the image's `Kestrel:Endpoints` override exactly as above — so that service binds 80/443 and never the port the file names. That is dead configuration rather than a log-noise problem, and whether the deployment still exists is unresolved, so it is deliberately left untouched here and excluded from the guard's surface list.
