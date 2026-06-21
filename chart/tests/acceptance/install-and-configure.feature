# Acceptance SSOT — epic-5306-k8s-productization, configure + full stack (slices 02-03)
# Executable via: helm template (render assertions, @in-memory) + helm install into kind (@real-io).
# RED until chart/ templates + values.schema.json exist (DELIVER slices 02-03).

@feature:epic-5306-k8s-productization
Feature: Configure the install via values and bring up the full production stack
  As a self-hoster
  I want to shape the deployment by editing values, not templates
  So that Lighthouse fits my environment and runs production-ready

  # --- slice-02: configure via values ---

  @US-01 @in-memory @env:default-values
  Scenario: Image tag and ingress host come from values
    Given the chart is rendered with image.tag set to a published release and ingress.host set
    When the manifests are produced
    Then the API workload image equals that tag
    And the Ingress and NOTES.txt use that host

  @US-01 @real-io @env:multi-replica
  Scenario: Replica count with Redis yields a scaled API that syncs once
    Given values set replicaCount to 2 and ConnectionStrings:Redis to an operator Redis
    When the operator installs into the cluster
    Then 2 API pods are Ready
    And an external sync runs once across the fleet (epic-5305 backplane honoured)

  @US-01 @in-memory @env:default-values
  Scenario: Omitting all optional overrides still renders a working install
    Given the chart is rendered with no overrides beyond the required password
    When the manifests are produced
    Then defaults apply and rendering succeeds with no required-without-default knob in this slice

  # --- slice-03: full enterprise stack ---

  @US-01 @real-io @env:enterprise-values
  Scenario: Full stack comes up from values-enterprise.yaml
    Given values-enterprise.yaml with Postgres, OIDC and mcp.enabled true
    When the operator installs into the cluster
    Then API, Postgres and MCP workloads all reach Ready

  @US-01 @real-io @requires_external @env:enterprise-values
  Scenario: OIDC login over the configured host has no redirect loop
    Given the app is behind the ingress with OIDC configured and forwarded-headers trusted
    When a user logs in over the HTTPS host
    Then the OIDC callback uses https and the public host and the session persists

  @US-01 @in-memory @env:mcp-enabled
  Scenario Outline: MCP workload is rendered only when enabled
    Given the chart is rendered with mcp.enabled set to <enabled>
    When the manifests are produced
    Then an MCP workload is <present>
    Examples:
      | enabled | present     |
      | true    | created     |
      | false   | not created |

  @US-01 @in-memory @env:external-postgres-byo
  Scenario: Bring-your-own Postgres renders no bundled DB pod
    Given the chart is rendered with postgresql.enabled false and externalDatabase set
    When the manifests are produced
    Then no bundled Postgres workload is created
    And the connection wiring points at the external database

  @error @US-01 @in-memory @standalone_gate @env:default-values
  Scenario: frontend.mode split fails loud (not a silent no-op)
    Given the chart is rendered with frontend.mode set to split
    When the manifests are produced
    Then rendering fails with a message that split is not implemented in this chart version

  @error @US-01 @in-memory @env:missing-required-value
  Scenario: Missing required value fails fast naming the key
    Given the chart is rendered with the Postgres password omitted
    When the manifests are produced
    Then rendering fails with a message naming the missing password key
    And no partial release is created

  @error @US-01 @in-memory @env:default-values
  Scenario: Invalid frontend.mode value is rejected by the schema
    Given the chart is rendered with frontend.mode set to an unknown value
    When the manifests are produced
    Then schema validation fails naming the frontend.mode key with its allowed values

  @error @US-01 @in-memory @env:default-values
  Scenario: Non-positive replicaCount is rejected by the schema
    Given the chart is rendered with replicaCount set to zero
    When the manifests are produced
    Then schema validation fails naming the replicaCount key

  @error @US-01 @in-memory @env:default-values
  Scenario: TLS without a host fails fast
    Given the chart is rendered with ingress.tls enabled and ingress.host empty
    When the manifests are produced
    Then rendering fails naming the missing ingress.host key

  @error @US-01 @in-memory @env:external-postgres-byo
  Scenario: Ambiguous database config fails fast
    Given the chart is rendered with both postgresql.enabled true and externalDatabase set
    When the manifests are produced
    Then rendering fails naming the conflicting database keys

  @error @US-01 @in-memory @env:mcp-enabled
  Scenario: MCP enabled without an image fails fast
    Given the chart is rendered with mcp.enabled true and mcp.image unset
    When the manifests are produced
    Then rendering fails naming the missing mcp.image key

# Runtime failure modes deliberately NOT acceptance-tested at the chart-render layer
# (image-pull failure, Postgres connection timeout, OIDC issuer unreachable, Redis loss
# at replicaCount>1, TLS cert invalid): these exercise the cluster/runtime, not the chart.
# They are covered by epic-5305 runtime tests + the per-slice dogfood (@requires_external),
# not by helm template/install acceptance. See distill/review-verdicts.md (Sentinel finding).
