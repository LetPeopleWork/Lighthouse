# Story #6095: Switch Lighthouse from Dependabot to Renovate
#
# Two kinds of scenario live here, and only one kind runs by itself.
#
# @automated @workflow-scripts
#   Repository invariants that keep holding after this story. Each names the node:test file that
#   carries it. They run in the required "Verify Workflow Scripts / workflow-scripts" check
#   (node --test .github/scripts/*.test.mjs). All are pending until slice 01 is delivered.
#
# @automated @existing-suite
#   Covered by tests that already exist (Vitest, pnpm build, the E2E suite). No new test.
#
# @manual
#   NOT wired to any runner. These describe what the hosted Renovate app and GitHub do, which no
#   test in this repository can execute, or what a person checks by eye. The maintainer runs them
#   by hand at the named moment and records the result in the slice's close-out:
#     @live-renovate    observed on GitHub after the cutover. SWITCH=<cutover date, YYYY-MM-DD>.
#                       T0 = first Renovate run, T1 = first Renovate merge on main, T+7d = a week later.
#     @dev-instance     observed on the local dev instance on :5169 with real history.
#     @delivery-check   a one-off check made in the DELIVER commit itself.
#
# The walking skeleton is the first live Renovate merge (T1). It is manual and cannot be green
# before slice 01 ships.

Feature: One dependency bot, one set of rules, in every repository the maintainer keeps
  As the maintainer of Lighthouse and lighthouse-clients
  I want every dependency update proposed once it is a week old and merged without me once CI is green
  So that I only look at the updates that could not merge themselves

  # ---------------------------------------------------------------------------------------------
  # Walking skeleton
  # ---------------------------------------------------------------------------------------------

  @walking_skeleton @driving_port @manual @live-renovate @US-01 @slice-01
  @contract-shape:bounded-change
  Scenario: A week-old update merges itself and the release waits for the maintainer
    Given Lighthouse's dependency policy is Renovate's, and Dependabot proposes nothing any more
    And an update to "actions/checkout" from v6.0.2 to v7.0.1 has been published for at least 7 days
    When Renovate proposes it and all eight required checks pass
    Then the update is merged without anyone acting, as a merge commit whose subject starts with "deps:"
    And the action is still pinned to a commit, now labelled "v7.0.1"
    And the build on main runs docker, the three standalone packages, Windows signing, and the macOS and Windows checks
    And that build waits at the Release approval, as it does after a maintainer's push
    # T1. gh pr list -R LetPeopleWork/Lighthouse --author app/renovate --state merged --json number,title,mergedBy,mergeCommit --limit 5
    #     gh api repos/LetPeopleWork/Lighthouse/commits/<mergeCommit> --jq '.parents | length'   -> 2 (a merge commit)
    #     git show <mergeCommit> -- .github/workflows | grep 'actions/checkout@'                 -> @<40-hex> # v7.0.1
    #     gh run list -R LetPeopleWork/Lighthouse --branch main --event push --limit 1 --json databaseId,status  -> status "waiting"
    #     gh run view <databaseId> -R LetPeopleWork/Lighthouse --json jobs --jq '.jobs[] | "\(.name) \(.conclusion)"'
    #       compare with the same list for the last Dependabot merge run: no job skipped that ran then.
    #     Record the PR number. Rollback trigger: any main-only job skipped.

  # ---------------------------------------------------------------------------------------------
  # Slice 01: Lighthouse switches to Renovate (US-01, US-02)
  # ---------------------------------------------------------------------------------------------

  @manual @delivery-check @US-01 @slice-01
  @contract-shape:bounded-change
  Scenario: The Renovate policy is accepted by Renovate's own checker
    Given the Renovate policy in the repository root
    When the maintainer checks it with Renovate's configuration validator
    Then it is reported valid, with no warning
    # Before the cutover push: npx --yes --package renovate@44 renovate-config-validator --strict renovate.json
    #   -> "Config validated successfully", exit 0

  @automated @workflow-scripts @US-01 @slice-01
  @contract-shape:unbounded-preservation
  Scenario: Only one dependency bot is configured
    Given the repository after the switch
    When the workflow-scripts check reads it
    Then it finds a Renovate policy and no Dependabot configuration
    # dependency-bot.test.mjs: "the repository has one dependency bot, ..."

  @automated @workflow-scripts @error @US-01 @slice-01
  @contract-shape:pure-function
  Scenario Outline: A second dependency bot's configuration is rejected
    Given a repository with a Renovate policy
    And a Dependabot configuration in "<file>"
    When the workflow-scripts check reads it
    Then it reports a second bot configured in "<file>"
    # dependency-bot.test.mjs: "rejects a second bot configured in ..."
    Examples:
      | file                    |
      | .github/dependabot.yml  |
      | .github/dependabot.yaml |

  @automated @workflow-scripts @error @US-01 @slice-01
  @contract-shape:pure-function
  Scenario: A repository without a readable Renovate policy is rejected
    Given a repository whose Renovate policy is missing, or cannot be read
    When the workflow-scripts check reads it
    Then it reports that Renovate has no policy to follow
    # dependency-bot.test.mjs: "rejects a repository with no Renovate policy",
    #                          "rejects a Renovate policy that is not valid JSON"

  @automated @workflow-scripts @US-01 @slice-01
  @contract-shape:unbounded-preservation
  Scenario: The build runs the same jobs whoever merged the change
    Given the workflows and shared actions after the switch
    When the workflow-scripts check reads them
    Then no job or step decides whether to run by who pushed or opened the change
    And a sign-in that passes the triggering account as a user name is not counted as such a decision
    # dependency-bot.test.mjs: "the repository has one dependency bot, ...",
    #                          "a tree with one bot, no actor-dependent job ... passes"

  @automated @workflow-scripts @error @US-01 @slice-01
  @contract-shape:pure-function
  Scenario Outline: A job that runs or skips depending on who merged is rejected
    Given a main-only job whose condition is "<condition>"
    When the workflow-scripts check reads it
    Then it reports that job's condition, on its line
    # dependency-bot.test.mjs: "rejects a workflow that ...", "rejects an actor condition written across several lines",
    #                          "rejects an actor condition on a step inside a composite action"
    Examples:
      | condition                                                            |
      | only on main, and not when Dependabot merged                         |
      | only on main, and not when Renovate merged                           |
      | only when one named person triggered the run                         |
      | only for pull requests Dependabot opened                             |
      | only on main and not for Renovate, written across several lines      |

  @automated @workflow-scripts @US-01 @slice-01
  @contract-shape:unbounded-preservation
  Scenario: Every action the build uses stays one Renovate can keep current
    Given the workflows and shared actions after every action is pinned
    When the workflow-scripts check reads them
    Then every action is pinned to a full commit followed by the release or branch it came from
    # action-pins.test.mjs: "every action the repository runs is pinned to a commit that names its release"

  @automated @workflow-scripts @error @US-01 @slice-01
  @contract-shape:pure-function
  Scenario Outline: An action Renovate would skip or that could move under the build is rejected
    Given a workflow step that uses "<reference>"
    When the workflow-scripts check reads it
    Then it reports "<problem>" on that step's line
    # action-pins.test.mjs: "rejects ..."
    Examples:
      | reference                                          | problem                           |
      | actions/download-artifact@v8                       | follows a tag, not a commit       |
      | actions/upload-artifact@v7.0.1                     | follows a tag, not a commit       |
      | sonarsource/sonarqube-quality-gate-action@master   | follows a branch, not a commit    |
      | dtolnay/rust-toolchain@stable                      | follows a branch, not a commit    |
      | actions/checkout@3d3c42e # v6.0.2                  | a shortened commit                |
      | actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1          | a commit with no release named |
      | actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1 #        | a commit with no release named |

  @automated @workflow-scripts @edge @US-01 @slice-01
  @contract-shape:pure-function
  Scenario Outline: References the pin rule does not apply to, or accepts in an unusual form
    Given a workflow step that uses "<reference>"
    When the workflow-scripts check reads it
    Then nothing is reported
    # action-pins.test.mjs: "accepts ...", "does not read a commented-out step",
    #                       "does not read YAML outside the workflows and actions folders"
    Examples:
      | reference                                                                          | why it is accepted                                   |
      | vimtor/action-zip@<commit> # v1.3                                                  | a two-part release is still a release                |
      | dtolnay/rust-toolchain@<commit> # stable                                           | the action publishes no release on the branch it follows |
      | 'actions/checkout@<commit>' # v6.0.2                                               | quoting does not change the reference               |
      | octo-org/shared/.github/workflows/build.yml@<commit> # v2.1.0                      | a shared workflow is pinned the same way             |
      | ./.github/actions/build-frontend                                                   | it lives in this repository                          |
      | docker://alpine:3.20                                                               | a container image, which Renovate updates separately |

  @manual @live-renovate @US-01 @slice-01
  @contract-shape:bounded-change
  Scenario: The first run lists every kind of dependency the repository has
    Given Lighthouse was added to the Renovate app after the switch was pushed
    When Renovate runs for the first time
    Then the Dependency Dashboard issue lists updates from the frontend and end-to-end packages, the .NET packages, the container images, the actions and their tool versions, the Node version file, the desktop app's Rust crates, the docs site's gems and the chart's values
    And no onboarding pull request is opened
    # T0. gh issue list -R LetPeopleWork/Lighthouse --author app/renovate --search "Dependency Dashboard in:title" --json number
    #     gh issue view <number> -R LetPeopleWork/Lighthouse | grep -E 'npm|nuget|dockerfile|github-actions|nvm|cargo|bundler|helm-values'
    #       -> all eight managers present
    #     gh pr list -R LetPeopleWork/Lighthouse --author app/renovate --search "Configure Renovate in:title" --state all  -> empty

  @manual @live-renovate @edge @US-01 @slice-01
  @contract-shape:bounded-change
  Scenario: An update younger than a week waits on the dashboard
    Given a version was published fewer than 7 days ago
    When Renovate runs
    Then no pull request is opened for it
    And the Dependency Dashboard shows it as pending
    # T+7d, KPI-4 (target 0), a 10-PR sample:
    #   gh pr list -R LetPeopleWork/Lighthouse --author app/renovate --state all --search "created:>=$SWITCH -label:security" --json number,title,createdAt
    #   npm: npm view <pkg> time.<ver>;  actions: gh api repos/<owner>/<repo>/releases/tags/<tag> --jq .published_at
    #   Every published date is at least 7 days before createdAt.

  @manual @live-renovate @error @US-01 @slice-01
  @contract-shape:bounded-change
  Scenario: An update whose checks fail stays open for the maintainer
    Given Renovate proposes the mutation-testing tools' next major version
    When a required check fails on it
    Then the update is not merged and stays open
    And the maintainer decides to fix it, close it, or hold it with a reason
    # Expected at T0 for the stryker 9 -> 10 update. KPI-5 at T+7d:
    #   gh pr list -R LetPeopleWork/Lighthouse --author app/renovate --state open --json number,title,createdAt,statusCheckRollup --jq '[.[] | select((.createdAt | fromdate) < (now - 604800) and any(.statusCheckRollup[]; .conclusion == "FAILURE"))] | .[] | "\(.number) \(.title)"'
    #   Each hit has a recorded decision.

  @manual @live-renovate @US-01 @slice-01
  @contract-shape:bounded-change
  Scenario: Green updates do not linger
    Given the switch was a week ago
    When the maintainer lists Renovate's open updates that are set to merge themselves
    Then none has been open more than 2 days with every check green
    # T+7d, KPI-2 (target 0), for <repo> = Lighthouse, and for lighthouse-clients after slice 02:
    #   gh pr list -R LetPeopleWork/<repo> --author app/renovate --state open --json number,title,createdAt,autoMergeRequest,statusCheckRollup --jq '[.[] | select(.autoMergeRequest != null and (.createdAt | fromdate) < (now - 172800) and all(.statusCheckRollup[]; .conclusion == "SUCCESS" or .conclusion == "SKIPPED" or .conclusion == "NEUTRAL"))]'

  @manual @live-renovate @US-01 @slice-01
  @contract-shape:bounded-change
  Scenario: A security fix does not wait a week and merges itself
    Given Dependabot alerts are on and Dependabot security updates are off
    And a vulnerability alert is raised for a dependency Renovate manages
    When Renovate runs
    Then it opens a "security"-labelled update at once, without the 7-day wait
    And the update merges itself once the required checks pass
    # First occurrence, whenever an alert arrives:
    #   gh pr list -R LetPeopleWork/Lighthouse --author app/renovate --label security --state all --json number,title,createdAt,mergedAt,mergedBy
    # Settings, after cutover step 2:
    #   gh api repos/LetPeopleWork/Lighthouse/automated-security-fixes --jq .enabled    -> false
    #   gh api repos/LetPeopleWork/Lighthouse/vulnerability-alerts -i | head -1         -> 204

  @manual @live-renovate @error @US-01 @slice-01
  @contract-shape:unbounded-preservation
  Scenario: Dependabot proposes nothing after the switch
    Given the switch was a week ago
    When the maintainer lists Dependabot's pull requests since the switch
    Then there are none, neither version updates nor security fixes
    # T+7d, KPI-1 (target 0):
    #   gh pr list -R LetPeopleWork/Lighthouse --author app/dependabot --state all --search "created:>=$SWITCH" --json number,title

  @manual @live-renovate @US-01 @slice-01
  @contract-shape:bounded-change
  Scenario: Renovate moves a pinned action and keeps it pinned
    Given every action is pinned to a commit labelled with its release
    When Renovate proposes a newer release of one of them
    Then the proposal replaces the commit and the label together, and never falls back to a tag
    # T0: one of the ~7 stale-action updates, e.g. actions/checkout v6.0.2 -> v7.0.1.
    #   gh pr diff <number> -R LetPeopleWork/Lighthouse | grep '^+.*uses:'   -> +... @<40-hex> # v7.0.1
    # From then on the action-pins guard keeps every later edit in that form.

  @manual @live-renovate @edge @US-01 @slice-01
  @contract-shape:unbounded-preservation
  Scenario: The held-back chart and date-picker packages get no minor updates until slice 03 lifts them
    Given the MUI charts and date pickers are held at 9.0.x and the MUI icons at v7
    When Renovate runs
    Then no minor or patch update is proposed for the charts or date pickers
    And no major update is proposed for the icons
    # T0: the dashboard log line "Filtered out 3 disabled update(s)", and
    #   gh pr list -R LetPeopleWork/Lighthouse --author app/renovate --state all --search "mui in:title created:>=$SWITCH" --json title
    #   shows no x-charts / x-date-pickers minor or patch and no icons-material major.

  @manual @live-renovate @error @US-01 @slice-01
  @contract-shape:bounded-change
  Scenario: An update's lockfile is written by the pnpm the project pins
    Given the frontend pins pnpm 10.33.2
    When Renovate proposes a frontend or end-to-end package update
    Then the required frontend and end-to-end checks install from its lockfile without changing it
    # Enforced by the required checks on every PR. On the first frontend PR at T0:
    #   gh pr checks <number> -R LetPeopleWork/Lighthouse   -> Verify Frontend and Verify E2E pass
    # Rollback trigger: ERR_PNPM_LOCKFILE_CONFIG_MISMATCH on main.

  @manual @live-renovate @US-02 @slice-01
  @contract-shape:bounded-change
  Scenario: A Node or pnpm move arrives as one update that waits for the maintainer
    Given a new pnpm major has been published for at least 7 days
    When Renovate proposes it
    Then one update moves the pnpm version in both projects together
    And a Node move would change the Node version file and both projects' Node engine together
    And it is not merged even once every check is green
    # pnpm 12.x is newer than the pinned 10.33.2, so expect this at T0 on branch renovate/node-and-pnpm-toolchain.
    #   gh pr view <number> -R LetPeopleWork/Lighthouse --json files,autoMergeRequest --jq '{files: [.files[].path], auto: .autoMergeRequest}'
    #     -> files include both package.json; auto is null
    #   The workflow-scripts check (toolchain single source) is green on it.
    #   Before merging by hand: confirm which pnpm wrote the lockfile; regenerate with the new pnpm if needed.

  @manual @live-renovate @error @US-02 @slice-01
  @contract-shape:unbounded-preservation
  Scenario: None of the updates the maintainer merges by hand is merged by Renovate
    Given the switch was a week ago
    When the maintainer lists what Renovate merged since the switch
    Then no toolchain, .NET runtime image major or chart value update is among them, except a security fix
    # T+7d, KPI-3 (target 0). Rollback trigger if > 0.
    #   gh pr list -R LetPeopleWork/Lighthouse --author app/renovate --state merged --search "merged:>=$SWITCH" --json number,title,headRefName,mergedBy,files --jq '[.[] | select(.mergedBy.login == "app/renovate" and ((.headRefName | test("node-and-pnpm-toolchain|dotnet")) or any(.files[]; .path == "chart/values.yaml" or .path == ".nvmrc")) and (.title | test("SECURITY") | not))]'

  @manual @live-renovate @edge @US-02 @slice-01
  @contract-shape:bounded-change
  Scenario: A security fix to pnpm or Node merges itself only with the toolchain still consistent
    Given a vulnerability alert is raised for pnpm
    When Renovate proposes the fix
    Then it merges itself once the checks are green, outside the toolchain update
    And it only merges with the toolchain single-source check green
    # T+7d, KPI-7 (reported, no target):
    #   gh pr list -R LetPeopleWork/Lighthouse --author app/renovate --state merged --search "SECURITY in:title merged:>=$SWITCH" --json number,title,files --jq '.[] | select(any(.files[]; .path | test("package.json$|.nvmrc$"))) | .title'
    #   For each: the "Verify Workflow Scripts / workflow-scripts" job on that merge concluded success.

  @manual @live-renovate @error @US-01 @slice-01
  @contract-shape:bounded-change
  Scenario: A red build on main after a Renovate merge is explained
    Given Renovate's updates have been merging for a week
    When the maintainer lists failed main builds that followed a Renovate merge
    Then each has a known cause (stale Sonar report, audit outage, connector rate limit) or is treated as a real break
    # T1 and T+7d, KPI-6 (0 unexplained):
    #   gh run list -R LetPeopleWork/Lighthouse --branch main --event push --status failure --created ">=$SWITCH" --json workflowName,displayTitle,headSha,url --jq '.[] | select(.displayTitle | test("renovate/")) | "\(.workflowName) \(.url)"'

  @manual @delivery-check @US-01 @slice-01
  @contract-shape:bounded-change
  Scenario: The contributor notes describe Renovate and the gap it leaves
    Given the shared actions' README
    When the maintainer reads its dependency-updates paragraph
    Then it names Renovate, not Dependabot
    And it says a vulnerable indirect dependency gets no bot update and is caught by the audit on main
    # grep -n -i 'dependabot' .github/actions/README.md   -> no match
    # After the first green Renovate merge: gh secret list -R LetPeopleWork/Lighthouse --app dependabot  -> empty

  # ---------------------------------------------------------------------------------------------
  # Slice 01 and 03: every hold carries its reason (US-04, set up by US-01)
  # ---------------------------------------------------------------------------------------------

  @automated @workflow-scripts @US-04 @slice-01 @slice-03
  @contract-shape:unbounded-preservation
  Scenario: Every update the repository holds back says why
    Given the Renovate policy
    When the workflow-scripts check reads it
    Then every rule that disables, caps, holds for approval or keeps an update from merging itself carries a written reason
    # dependency-bot.test.mjs: "every update the repository holds back says why"

  @automated @workflow-scripts @error @US-04 @slice-01 @slice-03
  @contract-shape:pure-function
  Scenario Outline: A hold without its reason is rejected
    Given a Renovate rule that <hold>
    And the rule's reason is <reason>
    When the workflow-scripts check reads it
    Then it reports a hold without a reason, asking for a description
    # dependency-bot.test.mjs: "rejects ... with no reason", "rejects a dependency ignored outside any rule",
    #                          "reports each hold without a reason, not just the first"
    Examples:
      | hold                                           | reason                     |
      | disables an update                             | missing                    |
      | keeps an update from merging itself            | missing                    |
      | caps a package below a version                 | missing                    |
      | holds an update for approval on the dashboard  | missing                    |
      | disables an update                             | blank                      |
      | disables an update                             | an empty list              |
      | ignores a package outside any rule             | impossible to write there  |

  @automated @workflow-scripts @edge @US-04 @slice-01 @slice-03
  @contract-shape:pure-function
  Scenario Outline: A rule that holds nothing needs no reason
    Given a Renovate rule that <does>
    When the workflow-scripts check reads it
    Then nothing is reported
    # dependency-bot.test.mjs: "does not ask for a reason on ..."
    Examples:
      | does                                             |
      | only groups related packages into one update     |
      | lets updates merge themselves                    |
      | enables a kind of update                         |
      | disables an update, with its reason on two lines |

  # ---------------------------------------------------------------------------------------------
  # Slice 02: lighthouse-clients follows the same rule (US-03)
  # ---------------------------------------------------------------------------------------------

  @manual @delivery-check @US-03 @slice-02
  @contract-shape:bounded-change
  Scenario: The clients' policy lets every update merge itself and is still valid
    Given the lighthouse-clients Renovate policy after the change
    When the maintainer checks it with Renovate's configuration validator
    Then it is reported valid
    And it lets patch, minor and major updates and security fixes merge themselves once "verify" is green
    And the 7-day wait is unchanged
    # In lighthouse-clients: npx --yes --package renovate@44 renovate-config-validator --strict renovate.json  -> exit 0
    #   jq '.minimumReleaseAge, .vulnerabilityAlerts.automerge, [.packageRules[] | select(.automerge == true) | .matchPackageNames]' renovate.json
    #     -> "7 days", true, [["*"]]

  @manual @delivery-check @US-03 @slice-02
  @contract-shape:bounded-change
  Scenario: The clients' release model states the same rule
    Given the clients' release model document
    When the maintainer reads its "Dependency Updates" section
    Then it says every update, majors and security fixes included, merges itself once CI is green
    And it no longer says majors or security fixes wait for a person
    And update pull requests still carry no changeset, and releases still wait at the Release approval
    # grep -n -iE 'major|security|wait' docs/release-model.md  (in lighthouse-clients)

  @manual @live-renovate @US-03 @slice-02
  @contract-shape:bounded-change
  Scenario: A major update to a client package merges itself once verify is green
    Given the clients auto-merge every update type
    When Renovate proposes a major update and "verify" passes
    Then the update merges without anyone acting
    And the next release still waits at the Release approval
    # First major after slice 02 (e.g. @types/node or pnpm):
    #   gh pr list -R LetPeopleWork/lighthouse-clients --author app/renovate --state merged --search "major in:title" --json number,title,mergedBy,mergedAt
    #     -> mergedBy app/renovate
    #   gh run list -R LetPeopleWork/lighthouse-clients --workflow release --limit 1 --json status  -> waiting at the next release

  @manual @live-renovate @error @US-03 @slice-02
  @contract-shape:bounded-change
  Scenario: A client update that fails verify stays open
    Given the clients auto-merge every update type
    When a Renovate update fails "verify"
    Then it is not merged and stays open for the maintainer
    # KPI-5 applied to lighthouse-clients at T+7d:
    #   gh pr list -R LetPeopleWork/lighthouse-clients --author app/renovate --state open --json number,title,statusCheckRollup --jq '.[] | select(any(.statusCheckRollup[]; .conclusion == "FAILURE")) | .title'

  # ---------------------------------------------------------------------------------------------
  # Slice 03: the held-back MUI packages catch up, or say why they can't (US-04)
  # ---------------------------------------------------------------------------------------------

  @automated @existing-suite @US-04 @slice-03
  @contract-shape:bounded-change
  Scenario: The upgraded MUI packages pass every frontend gate
    Given the MUI charts, date pickers and icons are on their latest 9.x release
    When the frontend tests, the production build and the end-to-end suite run
    Then all pass, and the build shows no warning
    # pnpm test; pnpm build; the Playwright suite. Watch for ERR_UNSUPPORTED_DIR_IMPORT (fix: server.deps.inline).
    # An icon that was renamed or removed fails pnpm build (its import no longer resolves).

  @automated @existing-suite @error @US-04 @slice-03
  @contract-shape:bounded-change
  Scenario: The metrics date range survives the keystrokes that once crashed it
    Given the MUI date pickers are on their latest 9.x release
    When the keystroke sequence from the date-picker crash is typed into the metrics date range
    Then the date range stays usable and the page does not crash
    # DateRangeSelector.keyboard.test.tsx and DashboardHeader.popover.test.tsx (real picker, not mocked).

  @manual @dev-instance @error @US-04 @slice-03
  @contract-shape:unbounded-preservation
  Scenario: Charts still show their x-axis labels
    Given the MUI charts are on their latest 9.x release
    When the maintainer opens a team's metrics on the dev instance
    Then the bar, run and stacked-area charts show their x-axis labels as before the upgrade
    # Start-DevServer.ps1 with real history (Restore-DbBackup.ps1); http://localhost:5169, a team's Metrics tab.
    # Unit tests mock the chart library, so none can see this; the 2025-11 regression was never red in CI.

  @manual @dev-instance @error @US-04 @slice-03
  @contract-shape:unbounded-preservation
  Scenario: Forecast icons still show
    Given the MUI icons are on their latest 9.x release
    When the maintainer opens a forecast on the dev instance
    Then the confident, realistic and risky forecast levels each show their icon, and none is missing or replaced
    # http://localhost:5169, a Feature's forecast and a team's manual forecast.

  @manual @delivery-check @error @US-04 @slice-03
  @contract-shape:bounded-change
  Scenario: A package that cannot be lifted keeps its hold with the reason and a reproduction
    Given one of the three packages breaks on its latest 9.x release
    When the maintainer keeps its hold
    Then the hold's reason in the Renovate policy names the break and the smallest reproduction
    And a Bug is created in Azure DevOps, after the maintainer confirms it
    # The "every update the repository holds back says why" check rejects a hold without a reason.
    # For each lifted package: jq '.packageRules[] | select(.matchPackageNames | index("<pkg>"))' renovate.json  -> nothing

  @manual @delivery-check @US-04 @slice-03
  @contract-shape:bounded-change
  Scenario: Documentation images that show charts or date pickers are regenerated
    Given the upgrade changed how charts or date pickers look
    When the maintainer regenerates the documentation screenshots
    Then each image that shows a chart or a date picker matches the upgraded look
    # rm the affected PNGs first (a diff under 0.5% keeps the old one); needs the premium licence.
    # Before rm: compare old and new for the chart pages; a missing x-axis label shows here too.
