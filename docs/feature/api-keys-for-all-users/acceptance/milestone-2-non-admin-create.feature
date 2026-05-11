Feature: Any authenticated user can create, list, and delete their own API keys
  As an authenticated Lighthouse user
  I want to create, list, and delete API keys that I own
  So that I can grant my own CLI / MCP clients access without escalating to a System Admin
  And so that other users' keys remain invisible and undeletable from my session

  Background:
    Given Lighthouse is running with authentication enabled
    And RBAC enforcement is enabled

  @real-io @adapter-integration @milestone-2
  Scenario: Non-admin authenticated user creates, lists, and deletes a personal key
    Given Jordan is an authenticated user with no System Admin role
    When Jordan sends "POST /api/latest/apikeys" with body {"name": "jordan-cli", "description": "local CLI"}
    Then the response status is 201
    And the response body has a non-empty "plainTextKey" field
    When Jordan sends "GET /api/latest/apikeys"
    Then the response status is 200
    And the response body lists exactly one key with name "jordan-cli"
    And the listed key records Jordan as the creator
    When Jordan sends "DELETE /api/latest/apikeys/{id}" for the listed key
    Then the response status is 204
    When Jordan sends "GET /api/latest/apikeys" again
    Then the response body lists zero keys

  @real-io @adapter-integration @milestone-2 @error
  Scenario: A non-admin user cannot see another user's keys
    Given Jordan is an authenticated user with no System Admin role
    And Riley is a different authenticated user with no System Admin role
    And Riley has previously created an API key named "riley-cli"
    When Jordan sends "GET /api/latest/apikeys"
    Then the response status is 200
    And the response body does NOT list any key named "riley-cli"

  @real-io @adapter-integration @milestone-2 @error
  Scenario: A non-admin user cannot delete another user's key
    Given Jordan is an authenticated user with no System Admin role
    And Riley is a different authenticated user with no System Admin role
    And Riley has previously created an API key with id {rileyKeyId}
    When Jordan sends "DELETE /api/latest/apikeys/{rileyKeyId}"
    Then the response status is 404
    When Riley sends "GET /api/latest/apikeys"
    Then the response body still lists the key with id {rileyKeyId}

  @real-io @adapter-integration @milestone-2 @error
  Scenario: Unauthenticated request to create a key is rejected
    Given the caller is not authenticated
    When the caller sends "POST /api/latest/apikeys" with body {"name": "anon-cli"}
    Then the response status is 401

  @real-io @adapter-integration @milestone-2 @error
  Scenario: Empty key name is rejected with 400
    Given Jordan is an authenticated user with no System Admin role
    When Jordan sends "POST /api/latest/apikeys" with body {"name": "   "}
    Then the response status is 400
    And the response body indicates that the name is required
