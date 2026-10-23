# Fix — a Jira Cloud URL copied from the browser breaks connection setup (Bug #6401)

**Shipped** 2026-10-23 · commits `c4caeda10` … `6cce18961` on `main` · ADO Bug #6401 (tagged Release Notes) ·
design: the approved root-cause analysis in `docs/feature/fix-jira-cloud-url-path/rca.md`.

## Summary

The Jira Cloud address bar now shows the site under a `/jira/` path, and users paste that into the
connection URL. The connector used the URL as typed. OAuth broke outright; the API-token flow only worked
by accident. Lighthouse now keeps a single site address for every Jira connection. Data Center context
paths work for the first time. A failed save in the create wizard, and a refused connection list on the
Overview, now say so instead of looking like nothing happened.

## The problem

- **OAuth:** the cloud-id lookup appended `/_edge/tenant_info` to the URL as typed. At
  `…atlassian.net/jira/_edge/tenant_info`, Atlassian answers with the Jira web page (200, HTML). Parsing it
  as JSON failed, and the user got a generic "unexpected error".
- **API token:** `new Uri(base, "rest/…")` replaced the base's last path segment. So `…/jira` happened to
  resolve to the site root, and validation passed. The user still landed back at onboarding after Create,
  and nothing on screen said why. The wizard's save had `try/finally` and no `catch`. The Overview turned a
  403 on the connection list into "no connections" for anyone the UI treats as a system admin.
- **Data Center (latent):** that same Uri merge dropped a real context path (`https://host/jira` →
  `https://host/rest/…`), so DC instances served under a path never worked.

## Root causes

1. No canonical Jira site address existed. The raw option was read in six places, each with its own
   `TrimEnd('/')`.
2. The gateway route concatenated strings while the direct route merged Uris, so the same input behaved
   two ways.
3. The cloud-id lookup built its own `HttpClient`, so no test could reach it.
4. The create wizard had no failure state for Create.
5. The Overview mapped a refused list to an empty one under a permissive RBAC fallback.

## The fix

- `JiraSiteAddress.Of` reduces `*.atlassian.net` URLs to scheme + host (a trailing-dot host is
  recognised too). Any other URL keeps its path and loses one trailing `/`. Null or non-absolute input is
  returned unchanged, so the existing `invalid_url` verdict still fires.
- The connector reads the URL only through `SiteAddressOf`, and the client base address always ends in
  `/`.
- The cloud-id lookup uses the injectable handler. A non-JSON answer becomes an `invalid_url` verdict:
  "Use your site address, e.g. https://yourcompany.atlassian.net".
- Create, Validate and Update store a Jira connection's URL as its site address
  (`JiraSiteAddress.ForOption`). Already-saved rows are covered at read time, so there is no migration.
- Wizard step 3 shows the save failure through the step-2 error alert, now shared as `renderErrorAlert`.
- Overview: a system admin whose connection list returns 403, once roles have loaded, sees "You don't
  have access to the connections list. Ask a Lighthouse administrator to check your roles." instead of
  the onboarding stepper.

## Behaviour change to call out

Data Center connections whose URL includes a context path now send requests under that path. These were
failing before, so this repairs them, but it is a visible change and belongs in the release notes.

## Quality gates

- Regression tests failed on the old code for the bug's reason, then passed. RED evidence is in each
  step's commit.
- Adversarial review: four findings, all fixed with tests (null URL → 500, trailing-dot host, refused
  alert flashing while roles load, wording kept as approved).
- Mutation: the frontend reached 83.02 % after one kill round. On the backend, every mutant on changed
  lines was killed and `JiraSiteAddress` scored 94.4 %. See `docs/feature/fix-jira-cloud-url-path/mutation/results.md`.
- DES integrity: all 4 steps have complete traces.

## Open

- The API-token reporter's actual failure is still unproven; the URL was not its cause. The fix makes it
  visible. A HAR or request log from the reporter would settle it.
- Left as maintainer choices: the Overview's "Create a Connection before adding a Team" tooltip still
  shows when the list is refused. The new alert says "connections list" rather than the configurable
  Work Tracking Systems term.
