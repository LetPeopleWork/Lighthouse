# Acceptance SSOT — story-6131-chart-postgres-major-upgrade (slices 01-03)
# Executable via: helm-unittest (render assertions, @in-memory) in chart/tests/unit/, and the kind harness
# chart/tests/upgrade-path/run.sh (@real-io), which the upgrade-path job in ci_chart.yml runs in groups:
# `happy` and `refusals` for slices 01-02, `chain` and `chain-refusals` for slice 03. Every slice 01-02
# scenario runs. The slice 03 scenarios are @pending until DELIVER implements them; it removes @pending from a
# scenario in the same commit that turns its check green.
#
# State machine of the bundled database volume, as the upgrade step sees it on every pod start
# (M = the image's major, D = the major of the data the volume was first initialised with):
#   empty volume                        -> fresh install on M, nothing to do
#   D = M                               -> nothing to do
#   D = M-1, no upgraded copy           -> upgrade into a new copy, keep the old one
#   D = M-1, upgraded copy up to date   -> nothing to do, start on the upgraded copy
#   D = M-1, upgraded copy out of date  -> set it aside, upgrade again from the old data, then discard it
#   D = M-1, old control file missing   -> refuse, touch nothing (cannot tell whether the copy is current)
#   old copy removed, upgraded copy on M -> nothing to do, start on the upgraded copy
#   D <= M-2, D > M, or no copy on M    -> refuse, touch nothing, say why and how to get out
# Every refusal is checked before anything is written, so "touch nothing" is asserted byte for byte.
#
# Slice 03 (pending) reads D from the live copy instead of pgdata: the last copy in the chain pgdata ->
# pgdata-K -> pgdata-(K+1)..., each made by an upgrade from the one below it and still current. The rules above
# then hold for the live copy. After a successful upgrade from the live copy, every copy older than it is
# removed (placeholder first), so the volume keeps the source and the new copy, and one step of rollback.

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

  @US-01 @AC-1.8 @edge @real-io @env:reuse-values @slice-01 @contract-shape:bounded-change
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

  @US-01 @AC-1.6 @in-memory @env:external-db @slice-01 @contract-shape:pure-function
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

  @US-01 @AC-1.8 @in-memory @env:reuse-values @slice-01 @contract-shape:pure-function
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

  @US-02 @AC-2.3 @in-memory @slice-02 @contract-shape:pure-function
  Scenario: The Kubernetes docs say where the old copy is, what it costs and how to remove it
    Given the Kubernetes installation docs
    When the operator reads the section on upgrading the bundled Postgres
    Then it says where the previous major's data sits and how much room it takes
    And it says a rollback discards whatever was written since the upgrade
    And it gives one command that removes the old copy
    And it says that after that command a rollback to the previous chart is no longer possible

  @US-02 @AC-2.5 @edge @real-io @env:kind-after-rollback @slice-01 @contract-shape:bounded-change
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

  @US-02 @AC-2.4 @AC-2.3 @error @real-io @env:kind-newer-data-no-kept-copy @slice-02 @contract-shape:bounded-change
  Scenario: After the old copy is removed, a rollback refuses to start an empty database
    Given the operator has upgraded a Lighthouse with data from chart 0.1.17 to the new chart
    And has removed the old copy with the command from the Kubernetes docs, verbatim
    When the operator runs "helm rollback" to the chart 0.1.17 revision
    Then the bundled database does not become Ready
    And its log says the data folder is not empty
    And no empty Lighthouse database was created
    When the operator runs a plain "helm upgrade" to the new chart again
    And deletes the database pod still stuck on the refused start, as the Kubernetes docs say
    Then the bundled database becomes Ready on Postgres 18
    And every Lighthouse table has the same row count as before the upgrade
    And the marker row is present

  # ------------------------------------------------------------------------------------------------
  # slice-02 — US-03: an upgrade that cannot be done safely says why and touches nothing
  # ------------------------------------------------------------------------------------------------

  @US-03 @AC-3.1 @error @real-io @env:kind-size-limited-pv @slice-02 @contract-shape:unbounded-preservation
  Scenario: Too little room for a second copy stops the upgrade before it writes anything
    Given Lighthouse was installed from chart 0.1.17 on a volume with room for one copy of its data but not two
    And it holds Lighthouse data with a known marker row
    And a fingerprint of every file on the database volume has been taken
    When the operator runs a plain "helm upgrade" to the new chart with no other flags
    Then the bundled database does not become Ready, and keeps retrying by itself
    And its log carries one line naming the free space and the space needed
    And that line says to grow the volume's claim with "kubectl patch pvc", naming the claim, or to pin "postgresql.image" to the current major
    And describing the database pod shows the same line
    And every file on the database volume is exactly as it was before the attempt

  @US-03 @AC-3.5 @AC-3.1 @real-io @env:kind-size-limited-pv @slice-02 @contract-shape:bounded-change
  Scenario: Once the volume has grown, the refused upgrade goes ahead by itself
    Given an upgrade to the new chart was refused because the volume had too little room
    When the operator grows the volume enough for a second copy
    Then the bundled database becomes Ready on Postgres 18 with no other step from the operator
    And every Lighthouse table has the same row count as before the upgrade
    And the marker row is present

  @US-03 @AC-3.2 @error @real-io @env:kind-16-data @slice-02 @contract-shape:unbounded-preservation
  Scenario: Data two majors behind is refused, naming both majors and the manual path
    Given Lighthouse was installed from chart 0.1.17 with the bundled database image pinned to Postgres 16
    And it holds Lighthouse data with a known marker row
    And a fingerprint of every file on the database volume has been taken
    When the operator upgrades to the new chart with the database image moved to the chart's default
    Then the bundled database does not become Ready, and keeps retrying by itself
    And its log carries one line naming Postgres 16 as the data's major and Postgres 18 as the image's
    And that line points to the manual upgrade section of the Kubernetes docs
    And every file on the database volume is exactly as it was before the attempt

  @US-03 @AC-3.5 @AC-3.2 @real-io @env:kind-16-data @slice-02 @contract-shape:bounded-change
  Scenario: Pinning the image back to the data's major lets the database start with no other step
    Given an upgrade to the new chart was refused because the data was two majors behind
    When the operator pins the bundled database image to Postgres 16
    And deletes the database pod still stuck on the refused start, as the refusal line says
    Then the bundled database becomes Ready on Postgres 16 with no other step from the operator
    And the marker row is present

  @US-03 @AC-3.3 @error @real-io @env:kind-newer-data-no-kept-copy @slice-02 @contract-shape:unbounded-preservation
  Scenario: Data newer than the image, with no kept copy of the image's major, is refused
    Given the operator has upgraded a Lighthouse with data from chart 0.1.17 to the new chart
    And has removed the old copy with the command from the Kubernetes docs, verbatim
    And a fingerprint of every file on the database volume has been taken
    When the operator pins the bundled database image to Postgres 17 on the new chart
    Then the bundled database does not become Ready, and keeps retrying by itself
    And its log carries one line naming Postgres 18 as the data's major and Postgres 17 as the image's
    And every file on the database volume is exactly as it was before the attempt

  @US-03 @AC-3.4 @error @real-io @env:kind-interrupted-upgrade @slice-02 @contract-shape:bounded-change
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

  @US-03 @AC-3.4 @error @real-io @env:kind-newer-data-no-kept-copy @slice-02 @contract-shape:unbounded-preservation
  Scenario: An interrupted removal of the old copy never costs the upgraded copy
    Given the operator has upgraded a Lighthouse with data from chart 0.1.17 to the new chart
    And a row was written on Postgres 18 after the upgrade
    And the old copy's control file is gone, as a removal of the old copy cut off part-way leaves it
    And a fingerprint of every file on the database volume has been taken
    When the bundled database is restarted
    Then the bundled database does not become Ready, and keeps retrying by itself
    And its log carries one line naming the missing control file and pointing to the Kubernetes docs on finishing the removal
    And every file on the database volume is exactly as it was before the attempt
    When the operator finishes removing the old copy as the Kubernetes docs say
    Then the bundled database becomes Ready on Postgres 18
    And every Lighthouse table has the same row count as before the restart, the row written on Postgres 18 included

  @US-03 @D3 @error @real-io @env:kind-0.1.17-with-data @slice-02 @contract-shape:unbounded-preservation
  Scenario: An upgrade-source image whose programs cannot run beside the database image is refused
    Given Lighthouse was installed from chart 0.1.17 with the bundled Postgres 17
    And it holds Lighthouse data with a known marker row
    And a fingerprint of every file on the database volume has been taken
    When the operator upgrades to the new chart with the upgrade-source image set to one built on another operating system
    Then the bundled database does not become Ready, and keeps retrying by itself
    And its log carries one line saying the Postgres 17 programs cannot run beside the Postgres 18 image
    And every file on the database volume is exactly as it was before the attempt

  # ------------------------------------------------------------------------------------------------
  # slice-03 — US-04: a volume this chart already upgraded moves on to the next major the same way
  # No Postgres 19 image exists, so every scenario is proved one major lower: a Postgres 16 volume is
  # upgraded to 17 by the new chart pinned to 17 (upgrade source 16), then to 18 by the chart's defaults.
  # ------------------------------------------------------------------------------------------------

  @US-04 @AC-4.11 @real-io @env:kind-chain-16-17 @slice-03 @contract-shape:bounded-change
  Scenario: The first upgrade on a volume removes nothing
    Given Lighthouse was installed from chart 0.1.17 with the bundled database image pinned to Postgres 16
    And it holds Lighthouse data with a known marker row
    And the row count of every Lighthouse table has been recorded
    When the operator upgrades to the new chart pinned to Postgres 17, with Postgres 16 as the upgrade source
    Then the bundled database becomes Ready on Postgres 17
    And every Lighthouse table has the same row count as before the upgrade
    And the kept Postgres 16 copy still opens with Postgres 16 and holds the recorded row counts
    And the database log says nothing about removing a copy

  @US-04 @AC-4.1 @kpi:K2 @real-io @env:kind-chain-16-17 @slice-03 @contract-shape:bounded-change
  Scenario: A volume already upgraded once moves on to the next major with every row
    Given Lighthouse was upgraded once by the new chart from Postgres 16 to Postgres 17
    And a row was written on Postgres 17 after that upgrade
    And the row count of every Lighthouse table has been recorded
    When the operator runs a plain "helm upgrade" to the new chart with its default Postgres 18
    Then the bundled database becomes Ready on Postgres 18
    And Lighthouse reports itself ready
    And every Lighthouse table has the same row count as before this upgrade
    And the marker row is present
    And the row written on Postgres 17 is present
    And the database log names the Postgres 17 copy it upgraded from and the Postgres 18 copy it built, and says the Postgres 17 copy is kept

  @US-04 @AC-4.2 @real-io @env:kind-chain-16-17 @slice-03 @contract-shape:bounded-change
  Scenario: Once the new copy is in place, the copy before last is removed and one rollback step is kept
    Given Lighthouse was upgraded from Postgres 16 to 17, then by a plain "helm upgrade" to Postgres 18
    When the operator looks at the database volume and the database log
    Then the original folder holds only the placeholder saying the data was upgraded to Postgres 18
    And the volume holds exactly two copies of the database, the Postgres 17 copy and the Postgres 18 copy
    And the kept Postgres 17 copy still opens with Postgres 17 and holds the rows it had
    And the database log carries one line naming the removed Postgres 16 copy
    And that line says a rollback to the chart on Postgres 16 is no longer possible and a rollback to the chart on Postgres 17 still is

  @US-04 @AC-4.9 @kpi:K3 @edge @real-io @env:kind-chain-16-17 @slice-03 @contract-shape:bounded-change
  Scenario: Restarting the database after a second upgrade does not upgrade or remove anything
    Given Lighthouse was upgraded from Postgres 16 to 17, then by a plain "helm upgrade" to Postgres 18
    And the time a fresh install's database takes to start in the same cluster has been recorded
    When the bundled database is restarted
    Then the database log says there was nothing to upgrade and nothing to remove
    And the older major's programs were not handed over for this start
    And it starts on Postgres 18 less than 5 seconds later than the fresh install's database did
    And it becomes Ready on Postgres 18
    And every Lighthouse table has the same row count as before the restart

  @US-04 @AC-4.3 @real-io @env:kind-chain-16-17 @slice-03 @contract-shape:bounded-change
  Scenario: Rolling back one chart after a second upgrade starts the previous major and says what is missing
    Given Lighthouse was upgraded from Postgres 16 to 17, then by a plain "helm upgrade" to Postgres 18
    And a row was written on Postgres 18 after the second upgrade
    When the operator runs "helm rollback" to the revision on Postgres 17
    Then the bundled database becomes Ready on Postgres 17, on the kept Postgres 17 copy
    And the row written on Postgres 17 before the second upgrade is present
    And the row written on Postgres 18 is absent
    And its log carries one warning line naming the newer Postgres 18 copy and saying its writes are not in this database

  @US-04 @AC-4.4 @edge @real-io @env:kind-chain-16-17 @slice-03 @contract-shape:bounded-change
  Scenario: Upgrading again after a one-chart rollback starts afresh from the Postgres 17 copy
    Given the operator has rolled back one chart, from Postgres 18 to the revision on Postgres 17
    And a row was written on Postgres 17 after the rollback
    When the operator runs a plain "helm upgrade" to the new chart with its default Postgres 18
    Then the bundled database becomes Ready on Postgres 18
    And the row written on Postgres 17 after the rollback is present
    And the row written on Postgres 18 before the rollback is absent
    And the database log says nothing about removing a copy
    And the volume holds the placeholder, the Postgres 17 copy and the Postgres 18 copy, and nothing else

  @US-04 @AC-4.5 @edge @real-io @env:kind-chain-16-17 @slice-03 @contract-shape:bounded-change
  Scenario: Pinning the image back one major after a second upgrade starts on the kept copy with the warning
    Given Lighthouse was upgraded from Postgres 16 to 17, then by a plain "helm upgrade" to Postgres 18
    And a row was written on Postgres 18 after the second upgrade
    When the operator pins the bundled database image to Postgres 17 on the new chart
    And deletes the database pod, as the Kubernetes docs say
    Then the bundled database becomes Ready on Postgres 17, on the kept Postgres 17 copy
    And the row written on Postgres 18 is not there
    And its log carries one warning line naming the newer Postgres 18 copy

  @US-04 @AC-4.5 @AC-4.3 @AC-4.8 @error @real-io @env:kind-chain-16-17 @slice-03 @contract-shape:unbounded-preservation
  Scenario: Pinning the image back two majors after a second upgrade is refused, naming both majors
    Given Lighthouse was upgraded from Postgres 16 to 17, then by a plain "helm upgrade" to Postgres 18
    And a fingerprint of every file on the database volume has been taken
    When the operator pins the bundled database image to Postgres 16 on the new chart
    And deletes the database pod, as the Kubernetes docs say
    Then the bundled database does not become Ready, and keeps retrying by itself
    And its log carries one line saying the data is Postgres 18, in the Postgres 18 copy, which is newer than the image
    And that line says no Postgres 16 copy is left to start on, and to set the image back to Postgres 18 or remove the pin, then delete the pod
    And every file on the database volume is exactly as it was before the attempt

  @US-04 @AC-4.3 @error @real-io @env:kind-chain-16-17 @slice-03 @contract-shape:bounded-change
  Scenario: Rolling back two charts after a second upgrade fails loudly and never starts an empty database
    Given Lighthouse was upgraded from Postgres 16 to 17, then by a plain "helm upgrade" to Postgres 18
    When the operator runs "helm rollback" to the chart 0.1.17 revision
    Then the bundled database does not become Ready
    And its log says the data folder is not empty
    And no empty Lighthouse database was created
    And the Postgres 17 and Postgres 18 copies are as they were
    When the operator runs a plain "helm upgrade" to the new chart again
    And deletes the database pod still stuck on the refused start, as the Kubernetes docs say
    Then the bundled database becomes Ready on Postgres 18
    And every Lighthouse table has the same row count as before the rollback

  @US-04 @AC-4.6 @real-io @env:kind-chain-16-17 @slice-03 @contract-shape:bounded-change @pending
  Scenario: The documented cleanup removes every copy older than the live one, and can be run again
    Given Lighthouse was upgraded from Postgres 16 to 17, then by a plain "helm upgrade" to Postgres 18
    When the operator removes the old copies with the command from the Kubernetes docs, verbatim
    Then the volume holds only the placeholder and the Postgres 18 copy
    When the operator runs the same command again
    Then it succeeds and removes nothing
    And after a restart the bundled database becomes Ready on Postgres 18 with every row it had

  @US-04 @AC-4.6 @error @real-io @env:kind-chain-16-17 @slice-03 @contract-shape:bounded-change @pending
  Scenario: A cleanup cut off part-way is finished by running it again
    Given Lighthouse was upgraded from Postgres 16 to 17, then by a plain "helm upgrade" to Postgres 18
    And the documented cleanup was cut off after the placeholder was written and the Postgres 17 copy's version file was removed
    When the bundled database is restarted
    Then the bundled database becomes Ready on Postgres 18 with every row it had
    And Postgres is never started on the half-removed copy
    When the operator runs the cleanup command from the Kubernetes docs again
    Then the volume holds only the placeholder and the Postgres 18 copy

  @US-04 @AC-4.6 @AC-4.2 @error @real-io @env:kind-chain-16-17 @slice-03 @contract-shape:bounded-change
  Scenario: A removal of the copy before last cut off part-way is finished by the next start
    Given Lighthouse was upgraded once by the new chart from Postgres 16 to Postgres 17
    And the original Postgres 16 copy holds about 300 MB of Lighthouse data
    And the row count of every Lighthouse table has been recorded
    And the operator has run a plain "helm upgrade" to the new chart with its default Postgres 18
    When the database pod is deleted once the removal of the Postgres 16 copy has started
    Then the next start serves the Postgres 18 copy, and Postgres is never started on the half-removed copy
    And its log says it finished removing the Postgres 16 copy
    And the volume holds the placeholder, the Postgres 17 copy and the Postgres 18 copy, and nothing else
    And every Lighthouse table has the same row count as before the upgrade

  @US-04 @AC-4.2 @error @real-io @env:kind-chain-16-17 @slice-03 @contract-shape:bounded-change
  Scenario: A second upgrade interrupted during the copy removes nothing and is redone
    Given Lighthouse was upgraded once by the new chart from Postgres 16 to Postgres 17
    And it holds about 300 MB of Lighthouse data with a known marker row
    And the row count of every Lighthouse table has been recorded
    And the operator has run a plain "helm upgrade" to the new chart with its default Postgres 18
    When the database pod is deleted once its log says the copy has started
    Then the original Postgres 16 copy is still in place and still opens with Postgres 16
    And the next start begins the upgrade again from the Postgres 17 copy
    And the bundled database becomes Ready on Postgres 18
    And every Lighthouse table has the same row count as before the upgrade
    And only then is the Postgres 16 copy removed

  @US-04 @AC-4.2 @error @real-io @env:kind-chain-size-limited-pv @slice-03 @contract-shape:unbounded-preservation @pending
  Scenario: Too little room for the next copy refuses, naming the cleanup as a way out
    Given Lighthouse was upgraded once by the new chart from Postgres 16 to Postgres 17, on a volume with room for two copies of its data but not three
    And a fingerprint of every file on the database volume has been taken
    When the operator runs a plain "helm upgrade" to the new chart with its default Postgres 18
    Then the bundled database does not become Ready, and keeps retrying by itself
    And its log carries one line naming the free space and the space needed
    And that line points to the Kubernetes docs on removing old copies while the database is stopped, as one way to free the room
    And every file on the database volume is exactly as it was before the attempt
    When the operator removes the copies older than the Postgres 17 copy as that docs section says
    Then the next retry upgrades to Postgres 18 with no other step from the operator
    And every Lighthouse table has the same row count as before the upgrade

  @US-04 @AC-4.7 @real-io @env:kind-chain-cleaned @slice-03 @contract-shape:bounded-change @pending
  Scenario: A cleaned-up volume moves on to the next major and removes nothing
    Given Lighthouse was upgraded once by the new chart from Postgres 16 to Postgres 17
    And the operator removed the old copy with the command from the Kubernetes docs, verbatim
    And the row count of every Lighthouse table has been recorded
    When the operator runs a plain "helm upgrade" to the new chart with its default Postgres 18
    Then the bundled database becomes Ready on Postgres 18
    And every Lighthouse table has the same row count as before the upgrade
    And the database log says nothing about removing a copy
    And the volume holds the placeholder, the Postgres 17 copy and the Postgres 18 copy, and nothing else

  @US-04 @AC-4.8 @error @real-io @env:kind-chain-16-17 @slice-03 @contract-shape:unbounded-preservation
  Scenario: A newer copy made out of date by a rollback counts as the older major and is refused as a gap
    Given Lighthouse was upgraded once by the new chart from Postgres 16 to Postgres 17
    And the operator then rolled back to the chart 0.1.17 revision, so Postgres 16 ran again
    And a fingerprint of every file on the database volume has been taken
    When the operator runs a plain "helm upgrade" to the new chart with its default Postgres 18
    Then the bundled database does not become Ready, and keeps retrying by itself
    And its log carries one line naming the original copy as Postgres 16 data, two majors behind Postgres 18
    And that line says the Postgres 17 copy is out of date because Postgres 16 ran after it was made
    And that line does not offer the Postgres 17 copy as a way back
    And every file on the database volume is exactly as it was before the attempt

  @US-04 @AC-4.8 @AC-4.10 @error @real-io @env:kind-gap-from-copy @slice-03 @contract-shape:unbounded-preservation
  Scenario: A live copy two majors behind the image is refused, naming that copy and one major per release
    Given Lighthouse was installed from chart 0.1.17 with the bundled database image pinned to Postgres 15
    And it was upgraded once by the new chart from Postgres 15 to Postgres 16
    And a fingerprint of every file on the database volume has been taken
    When the operator runs a plain "helm upgrade" to the new chart with its default Postgres 18
    Then the bundled database does not become Ready, and keeps retrying by itself
    And its log carries one line naming the Postgres 16 copy as the data the database runs on and Postgres 18 as the image's major
    And that line says to upgrade through each chart release that moved the major, one at a time, or to follow the manual path
    And every file on the database volume is exactly as it was before the attempt

  # --- slice-03, added after the design review ---------------------------------------------------

  @US-04 @AC-4.5 @AC-4.6 @edge @real-io @env:kind-chain-16-17 @slice-03 @contract-shape:unbounded-preservation
  Scenario: Pinning back after a cut-off cleanup keeps the pinned major's data however often it restarts
    Given Lighthouse was upgraded once by the new chart from Postgres 16 to Postgres 17
    And the documented cleanup was cut off after the placeholder was written, while the original copy still says it is Postgres 16
    When the operator pins the bundled database image to Postgres 16 and deletes the database pod, as the Kubernetes docs say
    Then the bundled database becomes Ready on Postgres 16, on the original copy, with the warning naming the newer Postgres 17 copy
    When the operator writes a row on Postgres 16
    And the bundled database is restarted twice
    Then the original copy still opens with Postgres 16 and holds that row
    And the database log says nothing about removing a copy
    And the Postgres 17 copy is still on the volume

  @US-04 @AC-4.8 @error @real-io @env:kind-chain-16-17 @slice-03 @contract-shape:unbounded-preservation
  Scenario: A live copy whose control file cannot be read is refused before the next upgrade writes anything
    Given Lighthouse was upgraded once by the new chart from Postgres 16 to Postgres 17
    And the Postgres 17 copy's control file cannot be read
    And a fingerprint of every file on the database volume has been taken
    When the operator runs a plain "helm upgrade" to the new chart with its default Postgres 18
    Then the bundled database does not become Ready, and keeps retrying by itself
    And its log carries one line naming the Postgres 17 copy, saying Postgres 17 cannot open it without that file, and to put the file back from a backup
    And every file on the database volume is exactly as it was before the attempt

  @US-04 @AC-4.8 @error @real-io @env:kind-chain-16-17 @slice-03 @contract-shape:unbounded-preservation
  Scenario: A volume whose chain of copies cannot be followed is refused once, by the upgrade step alone
    Given Lighthouse was upgraded once by the new chart from Postgres 16 to Postgres 17
    And the original copy's control file cannot be read, with no sign that a removal had started there
    And a fingerprint of every file on the database volume has been taken
    When the operator runs a plain "helm upgrade" to the new chart with its default Postgres 18
    Then the bundled database does not become Ready, and keeps retrying by itself
    And the step that hands over the older major's programs finished without error and handed nothing over
    And the database log and the pod's description carry one refusal line, naming the original copy and saying to put its control file back from a backup
    And every file on the database volume is exactly as it was before the attempt

  @US-04 @AC-4.6 @error @real-io @env:kind-chain-16-17 @slice-03 @contract-shape:bounded-change @pending
  Scenario: The cleanup finishes a removal it can see had started, even past an unreadable original copy
    Given Lighthouse was upgraded once by the new chart from Postgres 16 to Postgres 17
    And a cleanup was cut off after the placeholder was written into the original copy
    And the original copy's control file cannot be read
    And the bundled database refuses to start, pointing to the Kubernetes docs on a cut-off removal
    When the operator runs the cleanup while the database is stopped, as the Kubernetes docs say
    Then the volume holds only the placeholder and the Postgres 17 copy
    And after the database is started again it becomes Ready on Postgres 17 with every row it had

  @US-04 @AC-4.6 @error @real-io @env:kind-chain-16-17 @slice-03 @contract-shape:unbounded-preservation @pending
  Scenario: The cleanup refuses to guess past an unreadable copy it has no sign of having started on
    Given Lighthouse was upgraded once by the new chart from Postgres 16 to Postgres 17
    And the original copy's control file cannot be read, with no sign that a removal had started there
    And a fingerprint of every file on the database volume has been taken
    When the operator runs the cleanup while the database is stopped, as the Kubernetes docs say
    Then the cleanup fails with one line naming the original copy's control file as unreadable and saying to put it back from a backup before removing anything
    And every file on the database volume is exactly as it was before the cleanup

  @US-04 @AC-4.2 @error @real-io @env:kind-chain-16-17 @slice-03 @contract-shape:bounded-change
  Scenario: A copy that cannot be fully removed never stops the database starting on the new copy
    Given Lighthouse was upgraded once by the new chart from Postgres 16 to Postgres 17
    And one file in the original Postgres 16 copy cannot be deleted
    And the row count of every Lighthouse table has been recorded
    When the operator runs a plain "helm upgrade" to the new chart with its default Postgres 18
    Then the bundled database becomes Ready on Postgres 18
    And every Lighthouse table has the same row count as before the upgrade
    And its log carries one warning line saying the Postgres 16 copy could not be fully removed
    When the bundled database is restarted
    Then its log shows the removal was tried again
    And the bundled database becomes Ready on Postgres 18
    When that file can be deleted again and the bundled database is restarted
    Then the original folder holds only the placeholder

  @US-04 @AC-4.10 @in-memory @slice-03 @contract-shape:pure-function @pending
  Scenario: The Kubernetes docs say what a second upgrade keeps, removes and costs
    Given the Kubernetes installation docs
    When the operator reads the section on upgrading the bundled Postgres
    Then it says that after a second upgrade only a rollback of one chart is possible, and what a rollback of two charts does
    And it says the copy before last is removed automatically on a second upgrade, quoting the log line
    And it gives the command that removes the one remaining older copy earlier
    And it says an upgrade leaves about twice the data, and briefly needs room for one more copy while it runs
    And it says the automatic upgrade moves one major per chart release, so a skipped release is upgraded to first

# Deliberately NOT acceptance-tested here, with the reason recorded in the feature delta (DISTILL / Upstream
# findings): the exact space threshold (needed = 1.1 x data + 64 MiB) is not pinned to the byte, because a
# tmpfs in a kind node cannot be sized that precisely; arm64 (CI runners are amd64 only); settings changed
# with ALTER SYSTEM (the chart never sets any); K1 (counted by hand from issues and Slack after release).
