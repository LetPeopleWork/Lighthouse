---
title: Configuration
layout: home
parent: Installation and Configuration
nav_order: 3
has_children: true
---

There are various options to configure Lighthouse, from the Database to the used ports. This section will show you **what** is configurable and how you configure it.

After you're done with the configuration, check out the [Concepts](../concepts/concepts.html) to see how Lighthouse works.

- TOC
{:toc}

# Overriding Configuration Options
Lighthouse is using the file `appsettings.json` for it's configuration. You can either override/provide your own file, or use command line parameters or environment variables to override individual values.

## Environment Variables
You can use environment variables to override the default configuration options specified in the `appsettings.json` file. For this, simply set a variable with the value you'd like to have. As an example, if you want to adjust the _Https Endpoint Url_ to be listening on port **1886**, you can do so by setting the variable `Kestrel__Endpoints__Https__Url` to `https://*:1886`

Environment variables are the preferred way to provide configuration if you run Lighthouse via docker.

## Commandline
Instead of environment variables, you can also specify parameters on startup. Simply pass your overrides on the commandline after a `--`.
```bash
Lighthouse.exe --Kestrel:Endpoints:Https:Url="https://*:1886"
```

## App Settings
The file `appsettings.json` can be opened in any text editor, it's a simple text based file in the json format. You can adjust the settings as you like.  

{: .note}
Please only use this option if you know what you're doing. If you don't provide a valid json file, Lighthouse will not start. Using commandline parameters or environment variables is recommended over adjusting directly in the appsettings.json.

# Configuration Options

## Instance Time Zone

{: .important}
**If your team is not on UTC, this setting needs your attention.** Lighthouse computes every calendar day — "today", the start and end of a metrics window, the day a snapshot is filed under — in a configurable **instance time zone**. The shipped default deliberately changes nothing: if you set nothing, your instance keeps behaving exactly as it did before. A Docker instance stays on UTC. To get calendar days that match your team's working day, you must **set `Lighthouse__TimeZone` yourself**.

A stored timestamp is an instant and has no time zone. A calendar day is *defined* by one. Lighthouse stores instants in UTC and always has; what this setting controls is the zone it uses when it reduces one of those instants to a day — and which day it considers "today".

**Default Value:** unset. Lighthouse ships no `Lighthouse:TimeZone` key in `appsettings.json`, on purpose: writing a concrete default there would move every existing instance onto a different calendar day on upgrade, unannounced.

**Override Options:**
- Command Line: `--Lighthouse:TimeZone`
- Environment Variable: `Lighthouse__TimeZone`

**Accepted values:** an [IANA time zone id](https://en.wikipedia.org/wiki/List_of_tz_database_time_zones) such as `Europe/Zurich`, `America/Los_Angeles` or `Asia/Tokyo`. On Windows, .NET 10 accepts both the IANA id and the Windows id (`W. Europe Standard Time`) — prefer the IANA id so the same configuration works on every platform. Daylight saving is handled for you; do not encode a fixed offset.

**Example:**

```bash
docker run -p 80:80 -e "Lighthouse__TimeZone=Europe/Zurich" ghcr.io/letpeoplework/lighthouse:latest
```

The container image ships the `tzdata` database, so any IANA id resolves inside Docker without extra packages.

### How the time zone is resolved

In order:

1. **`Lighthouse:TimeZone` is set** → that zone is used. If the id cannot be resolved on the host, **Lighthouse does not start** and logs which id it could not resolve. A silent fallback would hide exactly the kind of defect this setting exists to fix, so a wrong value fails loudly.
2. **The key is absent or blank** → the host time zone (`TZ` on Linux, the OS setting on Windows/macOS) is used. This is the shipped path. In the official Docker image the host zone is **UTC**, so a containerised instance keeps its previous behaviour; a standalone install picks up the machine's zone, which is usually what you already expected it to do.
3. **No host time zone can be determined** → UTC.

The resolved zone is written to the log on startup (`Instance calendar day is anchored on time zone …`), so you can confirm what the instance actually picked up rather than what you intended.

{: .note}
An absent key is a supported configuration, not a missing one — it means "no opinion". Only a value that is *present and wrong* stops startup.

### What changes when you set it

Setting a non-UTC zone moves every surface that reasons about a calendar day:

- **Forecasts** — the day a "when" forecast is projected forward from, and the dates the percentiles resolve to.
- **Metrics windows** — the days a `30 / 60 / 90 day` window covers, and which day a closed, started or blocked item is counted on.
- **Recorded snapshots** — the day key that Percentiles Over Time, PBC Over Time, Blocked Over Time and the delivery metric snapshots file each daily reading under.
- **Work item age and cycle time** — both ends of the span are calendar days, so they move together; the arithmetic is unchanged.
- **Licence expiry** — a licence is valid through the end of *your* last day, not UTC's.
- **Delivery target date validation** — "in the future" is judged against your calendar day.

Instants are deliberately **not** affected: audit stamps, token expiry, sync bookkeeping, blocked-transition timestamps and log entries stay UTC, because an instant is a point in time and has no day to move.

{: .note}
Changing the zone on a running instance shifts the day the next readings are filed under. Existing history is not rewritten and nothing is lost — a chart may show one day's reading landing on the neighbouring day at the moment of the change. See the release notes for the details.

## Http & Https URL
By default, Lighthouse will listen on Ports 5000 (http) and 5001 (https). On macOS, the HTTP port defaults to 5002 instead of 5000 to avoid conflicts with the AirPlay Receiver service that commonly uses port 5000. You might want to override this, for example if you want to expose Lighthouse on the default ports (80/443), or need to adjust it to whatever makes sense in your environment.

**Default Values:**
- Http URL: `http://*:5000` (Windows/Linux) or `http://*:5002` (macOS)
- Https URL: `https://*:5001`

**Override Options:**
- Command Line: 
  - `--Kestrel:Endpoints:Http:Url`
  - `--Kestrel:Endpoints:Https:Url`
- Environment Variables:
  - `Kestrel__Endpoints__Http__Url`
  - `Kestrel__Endpoints__Https__Url`

### Docker
When running in Docker, Lighthouse internally uses port 443 (HTTPS) and port 80 (HTTP). You can map these ports to any external port you want:
```bash
docker run -p 443:443 -p 80:80 ghcr.io/letpeoplework/lighthouse:latest
```

This mapping means: `-p [host-port]:[container-port]`

For example, to map container port 443 to host port 8081:
```bash
docker run -p 8081:443 ghcr.io/letpeoplework/lighthouse:latest
```

{: .note}
When running Lighthouse outside of Docker, it will use the default ports 5000/5001 (Windows/Linux) or 5002/5001 (macOS) unless overridden.

## Database
Lighthouse can work with different databases. Currently it supports [SQLite](https://www.sqlite.org/) and [postgresql](https://www.postgresql.org/) databases to store the data.

{: .recommendation}
More providers could be added upon request. Please reach out to us in such a case and we can discuss options.

As part of the configuration, you can specify the provider and the connection string.

**Default Values:** *sqlite* and *Data Source=LighthouseAppContext.db*

**Override Options:**
- Command Line: `--Database:Provider` and `--Database:ConnectionString`
- Environment Variable: `Database__Provider` and `Database__ConnectionString`

{: .recommendation}
If you just get started, and run Lighthouse on your local machine, we recommend to use **sqlite**. You don't need to install anything else or worry too much about setting up Database connections.  
We recommend using **Postgres** if you host Lighthouse for many users and the data stored for Lighthouse is increasing. Many cloud providers like AWS and Azure support hosting Postgres in their environments, so you can reuse their services and let Lighthouse store the data in a database hosted by those providers.

### SQLite
SQLite is the default provider. If used, the database is stored in a single file.

By default, that file is next to the executable and called *LighthouseAppContext.db*. If you want to store it in a subfolder called *data* and name it *MyDatabase.db*, you can provide the following value for the *Connection String*: `Data Source=data/MyDatabase.db`. You can also specify an absolute path: `Data Source=C:/data/MyDatabase.db`

{: .note}
The folder you specify must exist - if it's not existing, the startup will fail.

{: .important}
Wherever you put this file, Lighthouse keeps its [encryption key](#encryption-key) in a folder beside it. The two belong together: back them up together, and if you move the database onto a mounted volume, that is what keeps the key across a container being recreated. A database file written inside a container rather than onto a volume takes the key with it when the container goes.

### Postgres
Postgres is an open-source, relational database. It's widely used and very powerful.  

To configure Lighthouse to use postgres, set the *Database Provider* to *postgres*, an adjust the *Database ConnectionString* to a valid connection string for a postgres connection.

{: .note}
If you want to run Lighthouse with a connection to a Postgres database, you'll have to set up the database yourself. Please refer to the Postgres documentation on how to do this.


### Docker
Independent of which *provider* you are using, there are a few things to be aware of when running Lighthouse in Docker.

When using *sqlite*, you probably want to map the database to a file on your system, as otherwise you'll lose the data if your container gets removed. You can do so by specifying a volume and adjust the connection string to point to this volume:

```bash
docker run -v ".:/app/Data" -e "Database__ConnectionString=Data Source=/app/Data/LighthouseAppContext.db" ghcr.io/letpeoplework/lighthouse:latest
```

{: .note}
The environment variable `-e "Database__Provider=sqlite"` is omitted because it's the default value.

This will create a volume in the local folder (`-v ".:/app/Data`) and then overwrites the configuration via environment variable to point to this volume (`-e "Database__ConnectionString=Data Source=/app/Data/LighthouseAppContext.db"`). This will result in the file *LighthouseAppContext.db* to be created in the local folder where you run the container from. Check out the [docker sqlite example on GitHub](https://github.com/LetPeopleWork/Lighthouse/blob/main/examples/sqlite).

In a similar way, you can adjust the provider and connection string postgres. If you want to host your postgres database as well in a docker container, we provide a [docker-compose.yml](https://github.com/LetPeopleWork/Lighthouse/blob/main/examples/postgres/docker-compose.yml) that you can use as inspiration. It sets up two containers, one for postgres (which a mapping to your local filesystem to store the data) and one for the Lighthouse itself, configured to store the data in postgres.

## Encryption Key
In order to connect to Jira, Azure DevOps, etc., sensitive information (tokens) are needed. While we need to store them (as otherwise the continuous updating will not work), we don't want to keep those values in clear text.

**Default Value:** none required. On its first start, Lighthouse creates an encryption key for itself and keeps it in a folder next to the database, so the same key is still there after a restart and after an upgrade.

**Override Options:**
- Command Line: `--Encryption:Key`
- Environment Variable: `Encryption__Key`

Setting `Encryption__Key` overrides the key Lighthouse would create for itself. From that point on the key is yours to manage: Lighthouse will never create one of its own while the setting is present, and whatever holds that value is what stands between someone else and your stored tokens. If you don't set it, back up the key folder together with the database — losing one without the other leaves the stored tokens unreadable.

You have to specify a base64 encoded key that is 32 bytes long. You can generate a new random key via [https://generate.plus/en/base64](https://generate.plus/en/base64).  

{: .important}
Set the length to 32 as otherwise it will not work.

{: .note}
Instances created before this version encrypted with a key that ships inside the product. After an upgrade, Lighthouse keeps that key for reading only, so everything already stored keeps working, and writes anything new under a key of its own.

### Supplying more than one key

`Encryption__Key` also accepts several keys at once, written as `id:key` and separated by commas:

```bash
Lighthouse.exe --Encryption:Key="current:<base64 key>,previous:<base64 key>"
```

The **first entry is the one new secrets are written under**; every later entry is only ever used to read what was stored earlier. A name may use lowercase letters, digits and hyphens and be at most 32 characters long; if you leave the `id:` off, Lighthouse derives a name from the key itself. A single key is simply a ring of one, which is why the same setting takes both.

There is also a plural spelling that holds exactly the same thing:

**Override Options:**
- Command Line: `--Encryption:Keys`
- Environment Variable: `Encryption__Keys`

Use whichever reads better to you. If both are set, the plural one wins — so set one or the other rather than leaving an old value behind in the one you stopped using.

### Providing the key from a file

If a secret store already owns your key and mounts it into the container as a file, point Lighthouse at that file instead of putting the value into a setting.

**Override Options:**
- Command Line: `--Encryption:KeysFile`
- Environment Variable: `Encryption__KeysFile`

The file holds exactly what the setting would hold — one key, or several in the form above. If the setting names a file that isn't there, Lighthouse does not start: it won't fall back to a key of its own, because the mounted file would win again as soon as it reappeared and everything written in the meantime would be unreadable.

### When Lighthouse has nowhere to keep a key

Some deployments leave Lighthouse nowhere to put a key it would still find tomorrow — a database that is not a local file (Postgres, for example), or a container whose database file is written inside the container rather than onto a mounted volume. Lighthouse says so on startup and names the two ways out:

- set `Encryption__Key` to a key of your own, or
- set `Encryption__KeyStorePath` to a directory on a volume that outlives the container.

**Override Options:**
- Command Line: `--Encryption:KeyStorePath`
- Environment Variable: `Encryption__KeyStorePath`

If nothing sensitive is stored yet, Lighthouse **stops instead of starting**: a key it cannot keep would make every token entered afterwards unreadable at the next restart. If something is already stored, it starts and keeps working, and repeats the warning on every start until you do one of the two things above.

### What happens if the key changes

A key that Lighthouse cannot use — not base64, not 32 bytes, or an entry it cannot read — **stops startup**, and the log names the entry at fault. Lighthouse will not quietly start on some other key.

A key that is valid but isn't the one a stored secret was written under is a different matter, and what happens depends on how much of what you have stored it can still read.

If Lighthouse can read some of your stored secrets but not others, it starts, and it tells you which connection holds a value it can no longer read and which field that value sits in. You retype that one value rather than working out for yourself why a work tracking system stopped updating.

If it can read **none** of them, that is not a handful of stale values — it is the wrong key, and starting would leave every connection you have broken with no explanation. Lighthouse **stops startup** and says so, naming the two ways back: set `Encryption__Key` to the key the instance was using before, or set `Encryption__KeyStorePath` to the key store that belongs to this database. Nothing is changed and nothing is lost; your secrets are still there, encrypted under the key they were written with.

### Changing the key without re-entering a single credential

If a key may have been exposed, you don't have to reconfigure every work tracking system. **Settings → Encryption**, visible to System Administrators only, moves every stored secret onto a new key from inside Lighthouse. Nothing is asked for, nothing is re-entered, and every connection keeps working straight afterwards.

The screen shows where the key in force came from, which keys the instance currently holds, and where they are kept. What it offers depends on who owns the key.

**Where Lighthouse made the key itself** — a standalone install, or Docker with a data volume — the screen offers **Rotate key**. One action makes a new key, proves it can be read back, makes it the key new secrets are written under, moves every readable stored secret onto it, and keeps the previous key so nothing already stored becomes unreadable.

**Where you supplied the key** — through `Encryption__Key`, `Encryption__Keys` or `Encryption__KeysFile` — Lighthouse will not make one, and says so if you ask it to anyway. A key it minted would go to its own key store, and on the next start the key you supplied would win again, leaving everything moved onto the minted key unreadable. Replacing the key is four steps and you do the first two:

1. Put the new key **ahead of** the old one, so the ring reads `new:<base64 key>,old:<base64 key>`. On Kubernetes this is an edit to the Secret you already keep.
2. Restart Lighthouse — or roll the pod — so it picks the new ring up. New secrets are now written under the new key; everything already stored is still read with the old one.
3. Open **Settings → Encryption** and choose **Move stored secrets onto the active key**.
4. Once the result reports nothing left unreadable, remove the old key from your Secret and restart again.

Do not remove the old key before step 3. Nothing is destroyed if you do — the stored secrets are still there — but Lighthouse can no longer read them, and it will name each connection and field it cannot read rather than guessing.

### What the result tells you

Either action reports how many secrets were moved and how many could not be read, per connection, and names each unreadable one by the connection and the field holding it.

**A secret that cannot be read is never overwritten.** Something nobody can decrypt is something nobody can re-encrypt, and writing over it would destroy the only copy. It is left exactly as it is and named in the report so you know which single credential to re-enter — rather than reissuing every token to find out which one it was.

The pass is safe to interrupt and safe to repeat. A rotation only ever **adds** to the keys the instance can read with — the key that was in force is retired, never removed — so a rotation stopped half-way leaves every connection working, and running it again finishes the remainder and reports the same totals once there is nothing left to do. It needs no maintenance window either: a background refresh that stores a newly obtained token while the pass is running keeps that token, because the pass only ever writes over the exact value it read.

Only one rotation or move runs at a time. Starting a second one while the first is still going waits for it rather than running alongside it, and two started at the same moment do not interfere.

## Certificate
In order to run Lighthouse via secure https connection, we need to specify a certificate.

**Default Values:**
- Certificate File: `certs/LighthouseCert.pfx`
- Certificate Password: none

**Override Options:**
- Command Line:
  - `--Certificate:Path`
  - `--Certificate:Password`
- Environment Variables:
  - `Certificate__Path`
  - `Certificate__Password`

There is a default certificate delivered with the app, however, this is not tailored for your environment and you must trust it first.

{: .important}
If your certificate has no password, don't pass an empty string, but omit the whole password. On certain operating systems, an empty password is treated differently from an omitted one, potentially causing errors during startup.

### Creating a new Certificate
If you want to create a new certificate, you can do so via [OpenSSL](https://www.openssl.org/).
You can provide your own certificate and the respective password (if any), so that the Lighthouse can be trusted when exposed to your users. Assuming you have openssl installed, you can run the following commands which will guide you through the creation of a new "MyCustomCertificate.pfx" (during this process, openssl will ask you to provide certain information about - just follow along).

```bash
openssl req -newkey rsa:2048 -nodes -keyout MyCustomCertificate.key -out request.csr
openssl x509 -req -days 365 -in request.csr -signkey MyCustomCertificate.key -out MyCustomCertificate.crt
openssl pkcs12 -export -out MyCustomCertificate.pfx -inkey MyCustomCertificate.key -in MyCustomCertificate.crt
```

{: .note}
In step 3, you will be asked to provide a password. If you do, you have to specify it as well to Lighthouse.

### Using a custom Certificate

You know have a new file `MyCustomCertificate.pfx` that you could use instead of the default:
```bash
Lighthouse.exe --Certificate:Path="MyCustomCertificate.pfx" --Certificate:Password="Password"
```

If you then navigate to the Lighthouse URL, your browser might ask you to trust the certificate first. You can also inspect it and it should show the data you provided during the creation process.

### Docker
To provide the custom certificate to you instance running in docker, you can map a volume and specify the path through that volume. In the following example, we assume that *MyCustomCertificate.pfx* is in the local folder: 
```bash
docker run -v ".:/app/Data" -e "Certificate__Path=/app/Data/MyCustomCertificate.pfx" -e "Certificate__Password=Password" ghcr.io/letpeoplework/lighthouse:latest
```

## Authentication

{: .note}
Authentication is a **Premium** feature. You need a valid Premium license to enable it.

Lighthouse supports OpenID Connect (OIDC) authentication to protect your instance. When enabled, users must sign in through your configured identity provider before accessing Lighthouse.

**Default Values:** Authentication is **disabled** by default. All settings below apply only when `Enabled` is `true`.

**Override Options (environment variables):**
- `Authentication__Enabled`
- `Authentication__Authority`
- `Authentication__ClientId`
- `Authentication__ClientSecret`
- `Authentication__Scopes__0`, `Authentication__Scopes__1`, ...
- `Authentication__AllowedOrigins` — single origin (`https://app.example`) or a comma- or semicolon-separated list (`https://app.example,https://admin.example` or `https://app.example;https://admin.example`). Whitespace around delimiters is trimmed; empty fragments are dropped.
- `Authentication__AllowedOrigins__0`, `Authentication__AllowedOrigins__1`, ... — alternative indexed form for the same list. Use either form; do not combine them on the same instance.
- `Authentication__SessionLifetimeMinutes`
- `Authentication__RequireHttpsMetadata`

**Example configuration block** (in `appsettings.json`):

```json
{
  "Authentication": {
    "Enabled": true,
    "Authority": "https://your-idp.example.com",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "Scopes": ["openid", "profile", "email"],
    "AllowedOrigins": ["https://lighthouse.example.com"],
    "SessionLifetimeMinutes": 480,
    "RequireHttpsMetadata": true
  }
}
```

{: .important}
If `Enabled` is `true` but `Authority` is missing or misconfigured, Lighthouse will **not** start and will show an error. Authentication is fail-closed by design.

For full provider-specific setup guides (Keycloak, Microsoft Entra ID, Google, Auth0), see the [Authentication](./authentication.html) page.

## OAuth Callback Base URL

`Lighthouse:BaseUrl` is the public origin Lighthouse uses to compute the OAuth callback URL displayed in the connection form (and sent as `redirect_uri` to Jira / Azure DevOps at runtime). You need to set it when Lighthouse runs **behind a reverse proxy** — the host ASP.NET Core observes internally (e.g. `http://lighthouse:8080` inside the container) is not the public host the user's browser sees (e.g. `https://lighthouse.example.com`). Without `BaseUrl`, the callback URL displayed in the OAuth form is derived from `Request.Host`, which is usually wrong behind a proxy and will cause the IdP to reject the redirect.

**Default Value:** unset. When unset, Lighthouse falls back to `{Request.Scheme}://{Request.Host}` and shows a yellow warning on the OAuth form ("Your callback URL may be incorrect…"). For local development without a proxy, the fallback is usually fine; for any reverse-proxied or production deployment, set `BaseUrl` explicitly.

**Override Options:**
- Command Line: `--Lighthouse:BaseUrl`
- Environment Variable: `Lighthouse__BaseUrl`

**Example:**

```bash
docker run -e "Lighthouse__BaseUrl=https://lighthouse.example.com" ghcr.io/letpeoplework/lighthouse:latest
```

The value MUST be an absolute URL (scheme + host, optional port). Omit any trailing slash — Lighthouse appends `/api/oauth/callback` itself. The full callback URL the admin pastes into the Atlassian Developer Console or the Entra ID app registration is therefore `{BaseUrl}/api/oauth/callback`.

For the end-to-end Jira and Azure DevOps OAuth setup flows that consume this setting, see the OAuth section under [Jira](../concepts/worktrackingsystems/jira.html#jira-cloud-oauth) or [Azure DevOps](../concepts/worktrackingsystems/azuredevops.html#azure-devops-oauth).

## Telemetry (Metrics & Structured Logs)

Lighthouse can expose operational telemetry for a monitoring stack (Prometheus / Grafana / Loki). It is **off by default** — the single-container self-hosted deployment behaves exactly as before and pays no overhead unless you turn it on. Lighthouse never phones home; all telemetry is scraped or read by your own stack.

**`Telemetry:Enabled`** — when `true`, Lighthouse registers OpenTelemetry metrics and exposes a Prometheus scrape endpoint at `GET /metrics` (HTTP request count, error rate and latency histograms). When `false` (the default), no metrics are collected and `/metrics` is not mapped.

**`Telemetry:Logging:Format`** — set to `json` to emit logs as structured JSON to stdout (one object per line, ready for Loki). Any other value (the default `text`) keeps the human-readable console format. The file log is unaffected.

{: .important}
`/metrics` is unauthenticated and can reveal request paths. Only enable it on a deployment where the endpoint is reachable solely from your trusted monitoring network (e.g. a Kubernetes scrape network policy). Do not expose `/metrics` publicly.

**Default Values:**
- `Telemetry:Enabled`: `false`
- `Telemetry:Logging:Format`: `text`

**Override Options:**
- Command Line: `--Telemetry:Enabled` / `--Telemetry:Logging:Format`
- Environment Variables: `Telemetry__Enabled` / `Telemetry__Logging__Format`

**Example:**

```bash
docker run -p 80:80 \
  -e "Telemetry__Enabled=true" \
  -e "Telemetry__Logging__Format=json" \
  ghcr.io/letpeoplework/lighthouse:latest
```
