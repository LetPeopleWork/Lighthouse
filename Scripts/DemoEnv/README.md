# Demo and integration-test environments

One updater per work tracking system, each seeding and churning records in a real instance so the
demo environment and the screenshot runs have something live to read. They are run nightly by
`.github/workflows/updatedemoenv.yml`.

`ServiceNowPdiProvisioner.py` is the odd one out: it does not seed a demo, it rebuilds the
preconditions the ServiceNow **integration tests** depend on after a Personal Developer Instance has
been reclaimed. `.github/workflows/provision-servicenow-pdi.yml` runs it on demand.

## When the ServiceNow PDI dies

ServiceNow reclaims a PDI after about ten days idle. The signature is a **502 Bad Gateway** served
by `snow_adc` — DNS still resolves, but nothing is behind it:

```bash
curl -s -o /dev/null -w '%{http_code}\n' https://devNNNNNN.service-now.com/
# 502
```

A *hibernating* instance is a different thing and answers 200 with a "wake this instance up" page. A
502 from `snow_adc` means it is gone and a replacement is needed — waiting does not bring it back.

Everything under `Category=ServiceNowIntegration` then fails, and because
`.github/workflows/ci_changes.yml` selects that category whenever a diff touches the ServiceNow
connector, a shared connector path, or any `ci*.yml`, every PR in that area is blocked.

### Rebuild

Budget an hour. Two of these steps cannot be scripted, and the reasons are in *What bit us* below.

1. **Create a new PDI** at <https://developer.servicenow.com> → *Request an instance*. Note the
   instance URL and the admin password from the instance card.

2. **Grant `admin` the basic-auth role, in the browser.** Current releases block inbound basic
   authentication for any account without `snc_basic_auth_api_access`, and the block answers `401`
   *before* the password is checked.

   The Table API cannot do this for you — it is the thing being refused. Log in to the instance
   (browser login uses a session, not basic auth, so it still works) and add the role under
   **User Administration → Users → admin → Roles**, or create the record directly at
   `/sys_user_has_role.do?sys_id=-1`.

3. **Provision it.** From the repository root:

   ```bash
   pip install requests
   export ServiceNowLighthouseIntegrationTestToken='<new admin password>'
   python Scripts/DemoEnv/ServiceNowPdiProvisioner.py --instance https://devNNNNNN.service-now.com
   ```

   It creates the probe accounts and their roles, creates and shares the board the picker reads,
   runs the record seeder twice, and then measures every precondition and prints what each one
   holds up. It is idempotent — re-run it as often as you like.

4. **Set the probe passwords, in the browser.** The provisioner cannot, and prints the exact
   background script to paste when it detects accounts in this state. See *What bit us*.

5. **Run the provisioner again.** It should now report every precondition met.

6. **Update the two repository secrets:**

   ```bash
   gh secret set SERVICENOWLIGHTHOUSEINTEGRATIONTESTINSTANCE --body 'https://devNNNNNN.service-now.com'
   gh secret set SERVICENOWLIGHTHOUSEINTEGRATIONTESTTOKEN --body '<new admin password>'
   ```

   Both are already plumbed into `ci_backend.yml`, `ci_verifypostgres.yml`, `ci_verifysqlite.yml`,
   `updatedemoenv.yml` and `provision-servicenow-pdi.yml`, so no workflow needs editing.

7. **Update the three hard-coded fallbacks**, so a missing secret fails against a live host rather
   than a reclaimed one: `ServiceNowWorkTrackingConnectorIntegrationTest.cs` (`DefaultInstanceUrl`),
   `ServiceNowSystemUpdater.py` (`DEFAULT_INSTANCE_URL`), and `playwright.config.ts`
   (`SERVICENOWDEFAULTINSTANCE`).

8. **Re-run the failed CI jobs.**

To check an instance without changing anything, add `--verify-only`.

## What bit us

Recorded from the 2026-09-14 rebuild, which took far longer than it should have. Every one of these
cost real time, and none of them is discoverable from the error you get.

### Three different failures return the identical 401

An unknown user name, a wrong password, and a basic-auth request blocked by the restriction are all
answered with exactly this, byte for byte:

```json
{"error":{"message":"User is not authenticated","detail":"Required to provide Auth information"},"status":"failure"}
```

Comparing a real password against a deliberately wrong one tells you **nothing**, because both come
back the same. Do not try to diagnose a 401 from the response. Read the user record instead — the
`diff the accounts` approach below is what actually settled it.

A useful separator: logging in through the browser proves the password is correct, because UI login
and REST basic auth check the same credential. If the browser lets you in and REST does not, the
password is not the problem.

### `user_password` written over the Table API is stored verbatim, never hashed

**This is the one that cost the most.** A `PATCH` to `sys_user` with `user_password` succeeds,
returns 2xx, and leaves the account permanently unable to authenticate — the platform stores the
string exactly as sent instead of running it through the hashing the login path compares against.
As a bonus it leaves the password sitting in the field in clear text.

It is visible only by comparing records:

| account | `user_password` | authenticates |
| --- | --- | --- |
| `admin` (set by the platform) | 92 characters | yes |
| a probe (set over the API) | 12 characters — the password's own length | no |

There is no Table API call that sets a usable password, so the provisioner no longer tries. It
creates and roles the accounts, then prints a background script to paste at
**All → System Definition → Scripts - Background**, which sets them server side where
`setDisplayValue` performs the hashing.

Role grants through `sys_user_has_role`, by contrast, work perfectly over the API.

### The basic-auth restriction has a date on it, per instance

`glide.authenticate.basic_auth.restriction.*` in `sys_properties`. An older instance may show
`enforce=false` — tracking mode, blocking nothing yet but carrying an `enforcement_date`. Any
instance created after that date enforces from the moment it exists.

Measured: `dev191338` had enforcement scheduled for 2026-07-29, and `dev340014` reported
`enforce=true` from 2026-09-05. The SPIKE predicted this would break `updatedemoenv.yml` on its
first scheduled run after the date, and it did — independently of the reclaim.

The provisioner prints this property as a `[note]` on every run, so the state is never a guess.

### `sys_user_list.do` hides `admin` behind a default filter

Looking for the administrator in the user list and not finding it means the list is filtered, not
that the account is missing. Query it explicitly:
`/sys_user_list.do?sysparm_query=user_nameSTARTSWITHadmin`.

This one sent the diagnosis down a blind alley for a while — the account existed the whole time.

### A stock instance ships a board that breaks the picker measurement

The picker offers every board carrying a table and a filter, in **no particular order**, and the
fixture reads whichever comes first. A stock instance ships upgrade-tracking boards — on
`upgrade_history_task`, filtered to one upgrade — whose filter matches their whole table. When one
of those is served first, the fixture measures "selects 61 of 61" and fails, while a perfectly good
probe board sits behind it.

Two things made this hard to see. An administrator is shown every board regardless of who it is
shared with, so removing a membership fixes nothing. And the order is not stable — reading the same
query twice can put a different board first, so it is entirely possible to pass this locally and
fail in CI.

The provisioner now deactivates any other offered board whose filter does not narrow, leaving the
picker with boards that can actually demonstrate what a board is for. Its verification checks
**every** offered board rather than the first one, which is the mistake the earlier version made.

### Seeded record counts collide with the fixture's paging thresholds

The fixture needs open incidents **and** open changes each to fit inside one page of 100, while the
two together spill past it. A stock instance already carries about 90 open changes, so there is only
room for a handful of seeded ones — the first run pushed it to 101 and failed the check.
`ServiceNowSystemUpdater.py` therefore seeds only 8 changes, and the comment there says why.

Lowering the target does not remove records that already exist: the seeder tops up towards a
ceiling and never deletes. Surplus seeded records carry `correlation_id=LIGHTHOUSE_DEMO` and can be
deleted from a filtered list.

### Transition history needs two seeder passes and cannot be backfilled

A state span is recorded only when a record moves, and only once the metric definition already
exists. The first pass creates the definition and the records; the second walks them. Hence
`--seed-passes`, defaulting to 2. Stock ships a definition on `incident` and `problem` but **none**
on `change_request`; the seeder creates the missing one.

## What the provisioner cannot fix

Reported by the verification pass rather than repaired, because the repair is a judgement call:

- **`incident_task` has gained records.** The fixture needs a genuine kind of work the instance
  holds *none* of. Point it at another empty `task` descendant rather than weakening the assertion.
- **Open incidents or open changes exceed a single page.** Close records rather than raising the
  threshold — the proof depends on landing either side of 100.
- **An inherited role cannot be removed.** `lh_probe_snc_read` must not read problems. A directly
  assigned `sn_problem_read` is stripped automatically; one inherited through a containing role is
  reported instead, since removing it means unpicking the role that brought it in.

## The accounts it creates

All share the admin password, because the fixture authenticates every one of them with the single
`ServiceNowLighthouseIntegrationTestToken` secret. Every one also holds
`snc_basic_auth_api_access`, including the account whose whole purpose is holding no other role —
that role grants the right to present a password over REST, not access to any data.

| Account | Roles | Why it exists |
| --- | --- | --- |
| `lh_probe_none` | none | Authenticates, and every read returns 200 with zero rows — indistinguishable from an empty table unless the connector looks harder. |
| `lh_probe_snc_read` | `sn_incident_read`, `sn_change_read`, `sn_request_read` | Reads incidents but deliberately **not** problems. The asymmetry is the only thing that separates two otherwise identical responses. |
| `lh_probe_itil` | `itil` | Full service-desk grade, used by the exploratory probes rather than the standing suite. |
