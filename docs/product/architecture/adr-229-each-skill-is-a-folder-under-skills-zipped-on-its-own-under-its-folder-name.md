# ADR-229: Each skill is a folder under `skills/`, zipped on its own under its folder's name

- **Status**: Proposed (2026-10-08, DESIGN wave for ADO User Stories #6245, #6217, #6246). Interaction mode =
  **propose**.
- **Feature**: `story-6217-lighthouse-skills` — repo `lighthouse-clients` (`skills/`, `scripts/`, `ci.yml`)

## Context

`lighthouse-clients` ships one agent skill: the folder `skill/` (`SKILL.md` + `references/`), zipped by the CI
`release` job into `lighthouse-skill.zip` and attached to every GitHub Release. Lighthouse's
`docs/aiintegration.md` links `releases/latest/download/lighthouse-skill.zip`. Two more skills now ship with it
(Refinement, Daily Flow Review). Skill importers (Claude Desktop, Claude Code) take one `SKILL.md` per skill, at
the root of what they import. Each new skill also carries evaluation cases and fixtures that users never need.

Quality attributes: compatibility (the existing download link), installability (one skill per import),
maintainability (one packing rule for every skill), and catching a packing fault before an approved release.

## Decision

1. Skills live in `skills/<name>/`, one folder per skill: `skills/lighthouse/` (today's `skill/`, moved),
   `skills/lighthouse-refinement/`, `skills/lighthouse-daily-flow-review/`. A folder holds `SKILL.md`, optional
   `references/`, and `evals/`. Files directly in `skills/` (the drift test, a README) are not skills.
2. `scripts/pack-skills.sh <out-dir>` zips each folder's contents into `<out-dir>/<name>-skill.zip`, with
   `SKILL.md` at the zip root and `evals/` left out. It fails when a folder has no `SKILL.md` or the frontmatter
   `name` is not the folder's name. The general skill therefore stays `lighthouse-skill.zip`.
3. The `release` job runs the script and attaches `release-assets/*-skill.zip`, with `fail_on_unmatched_files`
   on. The `verify` job runs the same script into a temporary directory and lists every zip, so a packing fault
   fails the pull request rather than the approved release.

## Alternatives considered

- **Keep `skill/` and add sibling folders** (`skill/`, `skill-refinement/`, …). No move, but the naming has no
  rule and the CI step stays one hand-written line per skill. Rejected: a fourth skill would repeat the edit, and
  the name of the general folder would differ from its zip's rule.
- **One zip holding all three skills.** One asset and one link. Rejected: importers take one `SKILL.md` per
  import; a nested zip does not import as three skills, and a user who wants only the Refinement skill would get
  three.
- **Ship `evals/` inside each zip.** Simpler packing. Rejected: an installed skill would carry prompts with
  their expected answers, which an assistant may read as guidance, and users gain nothing from them.

## Consequences

- Positive: every `lighthouse-skill.zip` link keeps working; each skill installs alone; a new skill needs a
  folder and nothing in CI.
- Positive: packing faults (wrong root, wrong name, evals shipped) fail in `verify`.
- Negative: the move of `skill/` to `skills/lighthouse/` touches every path in `ARCHITECTURE.md` §11 and any
  open branch that edits the skill. It lands in one slice (the first with a second skill to ship).
- Negative: a release made while a skill folder is half-written would publish it. Mitigation: a skill folder
  is added in the slice that makes it usable, and the Lighthouse docs link a zip only after the release that
  carries it.
