# Acceptance SSOT — epic-5306-k8s-productization, publish + enterprise docs (slices 04-05)
# Executable via: release-stage guard scripts + helm repo add/install from the published index
# (@real-io) and the helm-docs drift gate (@in-memory). RED until the publish step + docs exist.

@feature:epic-5306-k8s-productization
Feature: Publish the chart and self-serve from the enterprise docs
  As a maintainer (publish) and a self-hoster/prospect (consume)
  I want a versioned public chart and docs good enough to self-host and pitch
  So that any external user installs and evaluates without source or a sales call

  # --- slice-04: package + publish ---

  @US-01 @real-io @env:ci-kind-clean
  Scenario: External user installs from the published Helm repo with no source
    Given the chart version is published to the docs/charts Helm index
    When a user with no source runs helm repo add then helm install from letpeoplework/lighthouse
    Then the stack comes up Ready

  @error @US-01 @kpi:OUT-chart-publish-consistency
  Scenario: Publish refuses to silently overwrite an existing version
    Given the Chart.yaml version already exists in the published index
    When the maintainer runs the publish step
    Then the publish fails naming the un-bumped version and does not overwrite

  @US-01 @kpi:OUT-chart-publish-consistency
  Scenario: Chart version is consistent across all surfaces
    Given a packaged chart
    When the version is compared across Chart.yaml, the index, NOTES.txt and the README snippet
    Then all four agree
    And appVersion equals values.image.tag

  # --- slice-05: enterprise docs ---

  @US-02 @real-io @env:ci-kind-clean
  Scenario: A reader reaches a running instance from the quick-start verbatim
    Given the published quick-start and a conformant cluster
    When a reader follows the quick-start verbatim
    Then they reach a responding Lighthouse instance using the published chart

  @US-02 @in-memory @kpi:OUT-enterprise-docs-self-serve
  Scenario: Config reference has zero phantom keys
    Given the generated config reference and values.yaml
    When each documented option is checked against the chart
    Then every option corresponds to a real values key

  @error @US-02 @in-memory @kpi:OUT-enterprise-docs-self-serve
  Scenario: Config-reference drift is caught at finalization
    Given a values key has been renamed without regenerating the reference
    When the helm-docs drift gate runs
    Then it fails flagging the stale reference before publish

  @US-02 @real-io @requires_external @env:enterprise-values
  Scenario: The demo walkthrough runs end to end
    Given a reader runs the demo walkthrough against the real image
    When they execute install, auth, MCP and scaling stages in order
    Then each stage produces its documented observable output

  @US-02 @in-memory
  Scenario: The architecture diagram is present and shows the topology
    Given the published docs
    When a reader views the architecture section
    Then a rendered diagram shows Ingress to oauth2-proxy to API plus MCP plus Postgres
