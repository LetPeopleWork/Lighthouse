# ADR-212: Renovate is the one dependency bot across LetPeopleWork repos, and a green update merges itself

**Status**: Accepted (2026-09-29, by the maintainer at the end of the DESIGN wave, interaction mode PROPOSE)

**Feature**: `story-6095-switch-to-renovate` (ADO Story #6095), `docs/feature/story-6095-switch-to-renovate/feature-delta.md`

**Decider**: Benjamin (maintainer)

**Relationship to prior work**: ADR-097 chose the hosted Mend Renovate app for lighthouse-platform and scoped
what auto-merges there. Story #6088 brought Renovate to lighthouse-clients with "patch and minor only". This
ADR moves Lighthouse onto the same bot and settles one auto-merge rule for Lighthouse and clients.
lighthouse-platform keeps ADR-097's rules.

---

## Context

The Lighthouse repo got its updates from Dependabot. That meant a 5-day cooldown, and a PAT-driven workflow
that turned on auto-merge for every Dependabot PR. The other two repos use Renovate. The maintainer was
reasoning about two tools with two rule sets, and the auto-merge rules differed between repos for no stated
reason.

## Decision

1. **The hosted Renovate app is the only dependency bot.** In Lighthouse, `dependabot.yml`, the auto-merge
   workflow and its PAT are removed in the same change that adds `renovate.json`. Dependabot *alerts* stay on,
   because Renovate reads them. Dependabot *security updates* go off.
2. **The bot covers everything it detects**, not only the ecosystems the previous bot covered. In Lighthouse
   that is npm (including the pnpm override floors), NuGet, Docker, GitHub Actions (including the versions
   written into workflow inputs), nvm, Cargo, Bundler and the chart's helm values.
3. **Once the repository's required checks are green, every update merges itself:** patch, minor, major,
   digest and security. The merge happens through GitHub-native auto-merge, as a merge commit. The required
   checks of the default-branch ruleset are the only gate, and no bot-specific check is added. Packages
   released together as a monorepo move in one PR. Every other dependency gets its own PR.
4. **There are three exceptions, and they wait for a human.**
   - The toolchain every other build follows, which in Lighthouse means Node and pnpm, grouped into one PR.
   - A new major of the .NET runtime image, which only works once the projects target that .NET version.
   - A change to the Helm chart's values, which also needs its generated README and chart version updated.
5. **A version is proposed only after it has been published for 7 days.** Security fixes are exempt.
6. **Every Action is pinned to a commit SHA, with the release named in a trailing comment**, so Renovate can
   compare and update it. Pins the bot can't read (bare SHAs) and unpinned tag or branch refs are converted
   once. A branch ref follows its latest release when one sits at the branch head, and otherwise stays
   labelled with the branch name.

## Alternatives Considered

- **Keep Dependabot in Lighthouse.** Rejected, because it keeps two tools. Dependabot can't group the
  toolchain pins into one hand-merged PR without a separate workflow.
- **Auto-merge patch and minor only, everywhere** (the #6088 rule). Rejected by the maintainer on 2026-09-29.
  The all-types rule has run in Lighthouse without incident, and the Release gate still puts a human between a
  merged major and a shipped release.
- **Self-hosted Renovate.** Rejected: it's extra infrastructure, and the hosted app already serves both other
  repos.

## Consequences

- One config language, and one dashboard per repo.
- The actor on main after a dependency merge becomes `renovate[bot]`. Workflows must not filter on the bot's
  name, or main-only jobs stop running after dependency merges.
- A vulnerable *transitive* package gets no bot PR, because Renovate has no transitive remediation. It is
  detected by the `pnpm audit` step on main and fixed by hand with a pnpm override.
- Required checks that miss a class of break (main-only packaging, visual regressions) now let a green major
  through where a human used to look in clients. The Release gate is where that is caught.
- A security fix for a toolchain pin (pnpm, Node) auto-merges on green like every other security fix. The
  maintainer accepted this rather than holding the toolchain back from security fixes too.
- Workflows filtered by path are not required checks, so they don't gate an update. That covers the Helm
  Chart workflow, among others. An update that only such a workflow exercises, like a chart default image or
  the Helm version CI uses, can merge green and turn that workflow red on main.
