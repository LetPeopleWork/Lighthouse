# Acceptance SSOT — story-6131-chart-postgres-major-upgrade (slices 01-02)
# Executable via: helm-unittest (render assertions, @in-memory) in chart/tests/unit/, and the kind harness
# chart/tests/upgrade-path/run.sh (@real-io), which the upgrade-path job in ci_chart.yml runs in two groups:
# `happy` and `refusals`. Every scenario but one render guard is @pending until DELIVER implements its slice;
# DELIVER removes @pending from a scenario in the same commit that turns its check green.
#
# State machine of the bundled database volume, as the upgrade step sees it on every pod start
# (M = the image's major, D = the major of the data the volume was first initialised with):
#   empty volume                        -> fresh install on M, nothing to do
#   D = M                               -> nothing to do
#   D = M-1, no upgraded copy           -> upgrade into a new copy, keep the old one
#   D = M-1, upgraded copy up to date   -> nothing to do, start on the upgraded copy
#   D = M-1, upgraded copy out of date  -> discard it, upgrade again from the old data
#   old copy removed, upgraded copy on M -> nothing to do, start on the upgraded copy
#   D <= M-2, D > M, or no copy on M    -> refuse, touch nothing, say why and how to get out
# Every refusal is checked before anything is written, so "touch nothing" is asserted byte for byte.

@feature:story-6131-chart-postgres-major-upgrade
Feature: An upgrade that moves the bundled Postgres to a new major carries the data across
  As a self-hoster running the bundled Postgres (platform-operator)
  I want the chart release that moves the database to a new major to bring my data with it
  So that Lighthouse comes back after an upgrade with all its history, and a rollback still works

  # ------------------------------------------------------------------------------------------------
  # slice-01 — US-01: an upgrade keeps every piece of data
  # ------------------------------------------------------------------------------------------------

  @walking_skeleton @driving_port @US-01 @AC-1.1 @AC-1.2 @kpi:K2 @real-io @env:kind-0.1.17-with-data @slice-01 @contract-shape:bounded-change
  Scenario: Operator upgrades a Lighthouse with data and finds every row on Postgres 18
    Given Lighthouse was installed from chart 0.1.17 with the bundled Postgres 17
    And it holds Lighthouse data with a known marker row
    And the row count of every Lighthouse table has been recorded
    When the operator runs a plain "helm upgrade" to the new chart with no other flags
    Then the bundled database becomes Ready on Postgres 18
    And Lighthouse reports itself ready
    And every Lighthouse table has the same row count as before the upgrade
    And the marker row is present
    And Lighthouse reaches its database with the credentials it had before, with no value added or changed

  @US-01 @AC-1.1 @kpi:K2 @real-io @slice-01 @contract-shape:bounded-change
  Scenario Outline: The other ways of applying the new chart carry the data across the same way
    Given Lighthouse was installed from chart 0.1.17 with the bundled Postgres 17, applied by <apply method>
    And it holds Lighthouse data with a known marker row
    And the row count of every Lighthouse table has been recorded
    When the operator applies the new chart by <apply method>
    Then the bundled database becomes Ready on Postgres 18
    And Lighthouse reports itself ready
    And every Lighthouse table has the same row count as before the upgrade
    And the marker row is present

    @env:kind-0.1.17-with-data
    Examples: The Helm CLI, taking the new chart's defaults
      | apply method                                  |
      | "helm upgrade --reset-then-reuse-values"      |

    @AC-1.7 @env:gitops-rendered
    Examples: A GitOps tool that renders the chart and applies the manifests
      | apply method                                  |
      | rendering the chart and applying the result   |

  @US-01 @AC-1.3 @real-io @env:kind-0.1.17-with-data @slice-01 @contract-shape:unbounded-preservation
  Scenario: The previous major's data stays on the volume with exactly the pre-upgrade rows
    Given the operator has upgraded a Lighthouse with data from chart 0.1.17 to the new chart
    When the kept Postgres 17 copy is opened with Postgres 17
    Then it opens without error
    And every Lighthouse table in it has the row count recorded before the upgrade
    And the marker row is present in it

  @US-01 @AC-1.1 @edge @real-io @env:kind-0.1.17-sigkilled @slice-01 @contract-shape:bounded-change
  Scenario: An old database that was stopped abruptly is still carried across
    Given Lighthouse was installed from chart 0.1.17 with the bundled Postgres 17
    And it holds Lighthouse data with a known marker row
    And the row count of every Lighthouse table has been recorded
    And the Postgres 17 server is stopped without a clean shutdown when the upgrade begins
    When the operator runs a plain "helm upgrade" to the new chart with no other flags
    Then the bundled database becomes Ready on Postgres 18
    And every Lighthouse table has the same row count as before the upgrade
    And the marker row is present

  @US-01 @AC-1.8 @edge @real-io @env:reuse-values @slice-01 @contract-shape:bounded-change @pending
  Scenario: Reusing the previous values keeps the database on 17 and says how to move it
    Given Lighthouse was installed from chart 0.1.17 with the bundled Postgres 17
    And it holds Lighthouse data with a known marker row
    When the operator runs "helm upgrade --reuse-values" to the new chart
    Then the bundled database is Ready on Postgres 17
    And Lighthouse reports itself ready
    And the marker row is present
    And the upgrade output carries one line saying the bundled Postgres is behind the chart's default
    And that line names how to move it to the chart's default

  @US-01 @AC-1.4 @real-io @env:kind-clean-fresh @slice-01 @contract-shape:bounded-change
  Scenario: A fresh install starts an empty Postgres 18 database with no upgrade step
    Given an empty cluster with no earlier Lighthouse install
    When the operator installs the new chart with the bundled Postgres
    Then the bundled database becomes Ready on Postgres 18
    And Lighthouse reports itself ready
    And the database log says there was nothing to upgrade
    And the volume holds a single copy of the database

  @US-01 @AC-1.5 @kpi:K3 @edge @real-io @env:kind-0.1.17-with-data @slice-01 @contract-shape:bounded-change
  Scenario: Restarting the database after an upgrade does not upgrade again
    Given the operator has upgraded a Lighthouse with data from chart 0.1.17 to the new chart
    And the time a fresh install's database takes to start in the same cluster has been recorded
    When the bundled database is restarted
    Then the database log says there was nothing to upgrade
    And it starts on Postgres 18 less than 5 seconds later than the fresh install's database did
    And it becomes Ready on Postgres 18
    And every Lighthouse table has the same row count as before the upgrade

  @US-01 @AC-1.6 @in-memory @env:external-db @slice-01 @contract-shape:pure-function
  Scenario Outline: The upgrade steps exist only where the chart runs the database itself
    Given the chart is rendered with the bundled database <bundled>
    When the manifests are produced
    Then the database upgrade steps are <present>
    And the scripts they run are <present>

    Examples:
      | bundled  | present       |
      | enabled  | rendered      |
      | disabled | not rendered  |

  @US-01 @AC-1.6 @in-memory @env:external-db @slice-01 @contract-shape:pure-function @pending
  Scenario: An install with an external database renders exactly as before this story
    Given the chart is rendered with an external database
    When the manifests are produced
    Then they are identical to the render of the same values before this story

  @US-01 @AC-1.8 @edge @in-memory @env:reuse-values @slice-01 @contract-shape:pure-function
  Scenario: Values from an older chart still render a working upgrade step
    Given the chart is rendered with values that predate the upgrade-source image setting
    When the manifests are produced
    Then the upgrade source falls back to the chart's own Postgres 17 image
    And the upgrade step itself runs the database image the operator configured

  @US-01 @AC-1.8 @in-memory @env:reuse-values @slice-01 @contract-shape:pure-function @pending
  Scenario Outline: The install notes warn only when the database image is behind the chart's default
    Given the chart is rendered with the bundled database image set to <image>
    When the install notes are produced
    Then the line saying the bundled Postgres is behind the chart's default is <shown>

    Examples:
      | image                                   | shown      |
      | postgres:17                             | shown      |
      | postgres:17-trixie                      | shown      |
      | postgres:18-trixie                      | not shown  |
      | postgres@sha256:<a digest with no tag>  | not shown  |

  # Green today on purpose: it guards what a rollback to chart 0.1.17 relies on while the upgrade is built.
  @US-01 @AC-1.6 @AC-2.1 @in-memory @env:kind-0.1.17-with-data @slice-01 @contract-shape:pure-function
  Scenario: The database volume, where it is mounted and the health checks stay as they were
    Given the chart is rendered with the bundled database enabled
    When the manifests are produced
    Then the database keeps the same volume claim, size and mount path as chart 0.1.17
    And its readiness and liveness checks are those of chart 0.1.17

  # ------------------------------------------------------------------------------------------------
  # slice-01 — US-02: rolling back brings the old database back
  # ------------------------------------------------------------------------------------------------

  @US-02 @AC-2.1 @kpi:K2 @real-io @env:kind-after-rollback @slice-01 @contract-shape:bounded-change
  Scenario: Rolling back after an upgrade starts Postgres 17 on the pre-upgrade data
    Given the operator has upgraded a Lighthouse with data from chart 0.1.17 to the new chart
    And a row was written on Postgres 18 after the upgrade
    When the operator runs "helm rollback" to the chart 0.1.17 revision
    Then the bundled database becomes Ready on Postgres 17
    And Lighthouse reports itself ready
    And the marker row is present
    And the row written on Postgres 18 is absent

  @US-02 @AC-2.2 @kpi:K2 @edge @real-io @env:kind-after-rollback @slice-01 @contract-shape:bounded-change
  Scenario: Upgrading again after a rollback starts afresh from the Postgres 17 data
    Given the operator has rolled back from the new chart to chart 0.1.17
    And a row was written on Postgres 17 after the rollback
    When the operator runs a plain "helm upgrade" to the new chart with no other flags
    Then the bundled database becomes Ready on Postgres 18
    And the row written on Postgres 17 after the rollback is present
    And the row written on Postgres 18 before the rollback is absent

  @US-02 @rollback-contract @edge @real-io @env:kind-0.1.17-with-data @slice-01 @contract-shape:bounded-change
  Scenario: Rolling back between two releases of the new chart leaves the database as it is
    Given the operator has upgraded a Lighthouse with data from chart 0.1.17 to the new chart
    And has upgraded it once more with an unrelated setting changed
    And a row was written on Postgres 18 after that
    When the operator runs "helm rollback" to the first revision on the new chart
    Then the bundled database becomes Ready on Postgres 18
    And the database log says there was nothing to upgrade
    And the row written on Postgres 18 is present

  # ------------------------------------------------------------------------------------------------
  # slice-02 — US-02: removing the old copy, and what the docs promise about it
  # ------------------------------------------------------------------------------------------------

  @US-02 @AC-2.3 @in-memory @slice-02 @contract-shape:pure-function @pending
  Scenario: The Kubernetes docs say where the old copy is, what it costs and how to remove it
    Given the Kubernetes installation docs
    When the operator reads the section on upgrading the bundled Postgres
    Then it says where the previous major's data sits and how much room it takes
    And it says a rollback discards whatever was written since the upgrade
    And it gives one command that removes the old copy
    And it says that after that command a rollback to the previous chart is no longer possible

  @US-02 @AC-2.5 @edge @real-io @env:kind-after-rollback @slice-01 @contract-shape:bounded-change @pending
  Scenario: Pinning the image back after an upgrade starts on the kept copy and says what is missing
    Given Lighthouse was upgraded from chart 0.1.17 to the new chart and runs on Postgres 18
    And a row was written on Postgres 18 after the upgrade
    When the operator pins the bundled database image to Postgres 17 on the new chart
    Then the bundled database becomes Ready on Postgres 17 with the pre-upgrade rows
    And the row written on Postgres 18 is not there
    And its log carries one warning line saying a newer Postgres 18 copy exists, its writes are not in this database, and removing the pin redoes the upgrade from this copy
    When the operator writes a row on Postgres 17 and removes the pin
    Then the bundled database becomes Ready on Postgres 18 with the row written on Postgres 17
    And the row written on Postgres 18 before the pin is still not there

  @US-02 @AC-2.4 @AC-2.3 @error @real-io @env:kind-newer-data-no-kept-copy @slice-02 @contract-shape:bounded-change @pending
  Scenario: After the old copy is removed, a rollback refuses to start an empty database
    Given the operator has upgraded a Lighthouse with data from chart 0.1.17 to the new chart
    And has removed the old copy with the command from the Kubernetes docs, verbatim
    When the operator runs "helm rollback" to the chart 0.1.17 revision
    Then the bundled database does not become Ready
    And its log says the data folder is not empty
    And no empty Lighthouse database was created
    When the operator runs a plain "helm upgrade" to the new chart again
    Then the bundled database becomes Ready on Postgres 18
    And every Lighthouse table has the same row count as before the upgrade
    And the marker row is present

  # ------------------------------------------------------------------------------------------------
  # slice-02 — US-03: an upgrade that cannot be done safely says why and touches nothing
  # ------------------------------------------------------------------------------------------------

  @US-03 @AC-3.1 @error @real-io @env:kind-size-limited-pv @slice-02 @contract-shape:unbounded-preservation @pending
  Scenario: Too little room for a second copy stops the upgrade before it writes anything
    Given Lighthouse was installed from chart 0.1.17 on a volume with room for one copy of its data but not two
    And it holds Lighthouse data with a known marker row
    And a fingerprint of every file on the database volume has been taken
    When the operator runs a plain "helm upgrade" to the new chart with no other flags
    Then the bundled database does not become Ready, and keeps retrying by itself
    And its log carries one line naming the free space and the space needed
    And that line says to grow the volume with "postgresql.persistence.size" or to pin "postgresql.image" to the current major
    And describing the database pod shows the same line
    And every file on the database volume is exactly as it was before the attempt

  @US-03 @AC-3.5 @AC-3.1 @real-io @env:kind-size-limited-pv @slice-02 @contract-shape:bounded-change @pending
  Scenario: Once the volume has grown, the refused upgrade goes ahead by itself
    Given an upgrade to the new chart was refused because the volume had too little room
    When the operator grows the volume enough for a second copy
    Then the bundled database becomes Ready on Postgres 18 with no other step from the operator
    And every Lighthouse table has the same row count as before the upgrade
    And the marker row is present

  @US-03 @AC-3.2 @error @real-io @env:kind-16-data @slice-02 @contract-shape:unbounded-preservation @pending
  Scenario: Data two majors behind is refused, naming both majors and the manual path
    Given Lighthouse was installed from chart 0.1.17 with the bundled database image pinned to Postgres 16
    And it holds Lighthouse data with a known marker row
    And a fingerprint of every file on the database volume has been taken
    When the operator upgrades to the new chart with the database image moved to the chart's default
    Then the bundled database does not become Ready, and keeps retrying by itself
    And its log carries one line naming Postgres 16 as the data's major and Postgres 18 as the image's
    And that line points to the manual upgrade section of the Kubernetes docs
    And every file on the database volume is exactly as it was before the attempt

  @US-03 @AC-3.5 @AC-3.2 @real-io @env:kind-16-data @slice-02 @contract-shape:bounded-change @pending
  Scenario: Pinning the image back to the data's major lets the database start with no other step
    Given an upgrade to the new chart was refused because the data was two majors behind
    When the operator pins the bundled database image to Postgres 16
    Then the bundled database becomes Ready on Postgres 16 with no other step from the operator
    And the marker row is present

  @US-03 @AC-3.3 @error @real-io @env:kind-newer-data-no-kept-copy @slice-02 @contract-shape:unbounded-preservation @pending
  Scenario: Data newer than the image, with no kept copy of the image's major, is refused
    Given the operator has upgraded a Lighthouse with data from chart 0.1.17 to the new chart
    And has removed the old copy with the command from the Kubernetes docs, verbatim
    And a fingerprint of every file on the database volume has been taken
    When the operator pins the bundled database image to Postgres 17 on the new chart
    Then the bundled database does not become Ready, and keeps retrying by itself
    And its log carries one line naming Postgres 18 as the data's major and Postgres 17 as the image's
    And every file on the database volume is exactly as it was before the attempt

  @US-03 @AC-3.4 @error @real-io @env:kind-interrupted-upgrade @slice-02 @contract-shape:bounded-change @pending
  Scenario: An upgrade interrupted part-way is redone from the start and never serves a partial copy
    Given Lighthouse was installed from chart 0.1.17 with the bundled Postgres 17
    And it holds about 300 MB of Lighthouse data with a known marker row
    And the row count of every Lighthouse table has been recorded
    And the operator has run a plain "helm upgrade" to the new chart
    When the database pod is deleted once its log says the copy has started
    Then the next start begins the upgrade again from the beginning
    And Postgres is never started on a partly copied database
    And the bundled database becomes Ready on Postgres 18
    And every Lighthouse table has the same row count as before the upgrade
    And the kept Postgres 17 copy still opens with Postgres 17 and holds the recorded row counts

  @US-03 @D3 @error @real-io @env:kind-0.1.17-with-data @slice-02 @contract-shape:unbounded-preservation @pending
  Scenario: An upgrade-source image whose programs cannot run beside the database image is refused
    Given Lighthouse was installed from chart 0.1.17 with the bundled Postgres 17
    And it holds Lighthouse data with a known marker row
    And a fingerprint of every file on the database volume has been taken
    When the operator upgrades to the new chart with the upgrade-source image set to one built on another operating system
    Then the bundled database does not become Ready, and keeps retrying by itself
    And its log carries one line saying the Postgres 17 programs cannot run beside the Postgres 18 image
    And every file on the database volume is exactly as it was before the attempt

# Deliberately NOT acceptance-tested here, with the reason recorded in the feature delta (DISTILL / Upstream
# findings): the exact space threshold (needed = 1.1 x data + 64 MiB) is not pinned to the byte, because a
# tmpfs in a kind node cannot be sized that precisely; arm64 (CI runners are amd64 only); settings changed
# with ALTER SYSTEM (the chart never sets any); K1 (counted by hand from issues and Slack after release).
