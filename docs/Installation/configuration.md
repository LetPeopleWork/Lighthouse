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
docker run -v lighthouse-data:/app/data -e "Database__ConnectionString=Data Source=/app/data/LighthouseAppContext.db" ghcr.io/letpeoplework/lighthouse:latest
```

{: .note}
The environment variable `-e "Database__Provider=sqlite"` is omitted because it's the default value.

This creates a Docker volume named *lighthouse-data* (`-v lighthouse-data:/app/data`) and then overwrites the configuration via environment variable to point at it (`-e "Database__ConnectionString=Data Source=/app/data/LighthouseAppContext.db"`), so *LighthouseAppContext.db* lives in that volume and survives the container being replaced. It also holds the key that encrypts your stored credentials, in `/app/data/keys`. Use a volume rather than a folder from your machine: the image runs as a non-root user and cannot write a directory owned by whoever ran the command. Check out the [docker sqlite example on GitHub](https://github.com/LetPeopleWork/Lighthouse/blob/main/examples/sqlite).

In a similar way, you can adjust the provider and connection string postgres. If you want to host your postgres database as well in a docker container, we provide a [docker-compose.yml](https://github.com/LetPeopleWork/Lighthouse/blob/main/examples/postgres/docker-compose.yml) that you can use as inspiration. It sets up two containers, one for postgres and one for Lighthouse itself, configured to store the data in postgres. Both keep their data in Docker volumes, and the Lighthouse container is given `Encryption__KeyStorePath` so it has a durable place for the key that encrypts your credentials — without one it refuses to start rather than fall back to the key published with the product.

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

**Upgrading re-encrypts nothing.** What you stored before it stays exactly where it was, under the key that is published with every copy of Lighthouse and can be read out of the public source — so it stays readable by anyone holding one, until you run a rotation. **Settings → Encryption** says how many credentials that applies to on this instance and moves them across without asking you to re-enter a single one; the procedure is [below](#changing-the-key-without-re-entering-a-single-credential).

### Supplying more than one key

`Encryption__Key` also accepts several keys at once, written as `id:key` and separated by commas:

```bash
Lighthouse.exe --Encryption:Key="current:<base64 key>,previous:<base64 key>"
```

The **first entry is the one new secrets are written under**; every later entry is only ever used to read what was stored earlier — which is what lets you replace a key without re-entering a single credential. A name may use lowercase letters, digits and hyphens and be at most 32 characters long; if you leave the `id:` off, Lighthouse derives a name from the key itself. A single key is simply a ring of one, which is why the same setting takes both.

This is the same grammar the encryption settings screen quotes back at you, so you can follow either. See [Secret Encryption Key](../settings/encryption.html) for what that screen does with it.

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

### The key that ships with Lighthouse is never accepted as your key

Every Lighthouse before this version encrypted with a key that is written into the product, and that key can be read out of the public source by anyone. It is never used to write anything new. What happens when Lighthouse finds it depends on where it came from.

**Left behind in `appsettings.json`** — Lighthouse shipped that value under `EncryptionSettings__EncryptionKey`, and an update from inside Lighthouse keeps your settings file on purpose so it does not throw away settings you changed. An upgraded instance therefore usually still has it, and nobody chose to put it there. Lighthouse **ignores it**, creates and keeps a key of its own, and says so on startup. Delete the `EncryptionSettings` block when you see that message — nothing depends on it any more. Do not copy its value to `Encryption__Key`.

**Typed into a setting by hand** — `Encryption__Key`, `Encryption__Keys`, or a mounted key file. That is a choice, so Lighthouse **stops startup** rather than writing your tokens under a key anybody already has. The message names the setting the value arrived in.

Nothing is changed and nothing is lost either way: Lighthouse always keeps that key for reading, so everything you already stored is still readable. Where startup stops, there are two ways on:

- set `Encryption__Key` to a key of your own, or
- remove the setting entirely and let Lighthouse create and keep one for you.

The same value is still welcome *behind* a key of your own — second or later in the list described above. That is exactly how an instance keeps reading what it stored before the upgrade; what it may never be is the key new secrets are written under.

### When Lighthouse has nowhere to keep a key

Some deployments leave Lighthouse nowhere to put a key it would still find tomorrow — a database that is not a local file (Postgres, for example), or a container whose database file is written inside the container rather than onto a mounted volume. Lighthouse says so on startup and names the two ways out:

- set `Encryption__Key` to a key of your own, or
- set `Encryption__KeyStorePath` to a directory on a volume that outlives the container.

**Override Options:**
- Command Line: `--Encryption:KeyStorePath`
- Environment Variable: `Encryption__KeyStorePath`

If nothing sensitive is stored yet, Lighthouse **stops instead of starting**: a key it cannot keep would make every token entered afterwards unreadable at the next restart. If something is already stored, it starts and keeps working, and repeats the warning on every start until you do one of the two things above.

What that instance is running on while you have not is the key that ships with the product — and not only for what was stored before. **Every credential entered afterwards is written under it too**, so this is a warning about now, not about the past. The encryption settings screen offers that instance no way out from inside Lighthouse either: moving stored secrets needs somewhere to move them to, and there is no other key. The two settings above are the whole remedy.

**A key store belongs to one instance.** Unless you supplied the key yourself — through `Encryption__Key`, `Encryption__Keys` or `Encryption__KeysFile` — do not point two Lighthouse instances at the same key store. An instance with no key of its own makes one and writes it there, and two instances doing that at the same moment both succeed: the last one to write wins the file, and the other carries on encrypting under a key the file no longer names. Everything it writes in the meantime becomes unreadable the moment it restarts, and nothing warns you while it is happening.

This is not a shape Lighthouse supports, and it takes deliberate effort to reach — two containers sharing one bind mount or NFS share, or a Compose file scaled past one replica. If you want to run more than one instance, supply the key to all of them; then no instance is making a key and there is nothing to collide. The Kubernetes chart already works this way and refuses to install without a key you supplied.

### What happens if the key changes

A key that Lighthouse cannot use — not base64, not 32 bytes, or an entry it cannot read — **stops startup**, and the log names the entry at fault. Lighthouse will not quietly start on some other key.

A key that is valid but isn't the one a stored secret was written under is a different matter, and what happens depends on how much of what you have stored it can still read.

If Lighthouse can read some of your stored secrets but not others, it starts, and it tells you which connection holds a value it can no longer read and which field that value sits in. You retype that one value rather than working out for yourself why a work tracking system stopped updating.

If it can read **none** of them, that is not a handful of stale values — it is the wrong key, and starting would leave every connection you have broken with no explanation. Lighthouse **stops startup** and says so. The message names the key it started on and the key your stored credentials say they were written under, and it offers the remedies in the order they are most likely to be the one you need:

1. **If you have just started supplying an encryption key** to an instance that was managing its own — remove that setting and start Lighthouse again. This is the commonest cause by a distance: the key you supplied displaced the key Lighthouse made for itself, and that key is still sitting in the key store, untouched.
2. Otherwise set `Encryption__Key` to the key those credentials were written under, or `Encryption__KeyStorePath` to the key store that belongs to this database.
3. If that key is genuinely gone, see below.

Nothing about the failed start changes anything: every stored value is exactly as it was.

### When the key is genuinely gone

If the key store was destroyed — a container recreated with the key on its writable layer, a volume deleted — no setting will bring those credentials back, and the credentials are not the only thing at stake: your teams, portfolios, forecasts and every hour of history live in the same database, and none of that is encrypted.

**Override Options:**
- Command Line: `--Encryption:StartEvenIfNothingStoredCanBeRead`
- Environment Variable: `Encryption__StartEvenIfNothingStoredCanBeRead`

Set it to `true` and Lighthouse starts. Nothing is deleted and nothing is re-encrypted — the credentials that could not be read still cannot be read, and you enter them again by hand.

The sequence back to a working instance:

1. **First give the instance somewhere durable to keep a key**, if it does not have one. This is the step that is easy to skip and the one that decides whether the rest of the sequence ends. In a container, removing the setting in step 4 means recreating the container — and if the key store is on the container's writable layer, that destroys the key again, so the credentials you just re-entered are unreadable on the very next start. Set `Encryption__KeyStorePath` to a directory on a volume that outlives the container, or supply the key with `Encryption__Key`, *before* re-entering anything.
2. Set `Encryption__StartEvenIfNothingStoredCanBeRead=true` and start Lighthouse.
3. Open **Settings → Encryption** and press **Check secrets**. Every credential that cannot be read is listed with the Connection and the field that holds it.
4. Edit each of those Connections and enter the credential again. It is stored under the key this instance is using now.
5. Check again. When nothing is listed as unreadable, remove the setting and restart.

{: .important}
While the setting is in force it is printed on every start and shown on the encryption settings page. That is deliberate: it is a way back in, not a configuration you leave behind. Remove it once the credentials have been re-entered.

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
To provide the custom certificate to your instance running in docker, mount the certificate file itself read-only and point at it. In the following example, we assume that *MyCustomCertificate.pfx* is in the local folder:
```bash
docker run -v "./MyCustomCertificate.pfx:/app/certs/MyCustomCertificate.pfx:ro" -e "Certificate__Path=/app/certs/MyCustomCertificate.pfx" -e "Certificate__Password=Password" ghcr.io/letpeoplework/lighthouse:latest
```

{: .note}
Reading a file from your machine is fine; writing to a folder on it is not, because the image runs as a non-root user. That is why the certificate is mounted read-only and on its own path, rather than through the data directory.

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
