# Documentary Gherkin — bug 5023 regression
#
# This .feature file documents the regression scenarios in business language.
# It is NOT executed (Lighthouse backend uses NUnit, no pytest-bdd / SpecFlow).
# The executable contract lives in:
#   Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/OAuth/Bug5023JiraRefreshRotationTest.cs

@regression @bug-5023 @oauth @jira
Feature: Jira OAuth refresh chain survives rotated refresh tokens

  Background: a connector-admin has a working Jira OAuth connection
    Given a Jira OAuth connection exists with credential status "Valid"
    And the credential's stored refresh token is the one Atlassian last issued
    And Atlassian rotates the refresh token on every refresh response

  @real-io
  Scenario: Five consecutive token refreshes all succeed
    When the application requests a fresh access token five times in a row
    And each request happens after the access token has entered its 5-minute refresh window
    Then every refresh call to Atlassian uses the refresh token from Atlassian's previous response
    And the credential's persisted refresh token equals the most recent Atlassian-issued one
    And the credential status remains "Valid" throughout
    And no "oauth.token.refresh_failed" event is emitted

  @real-io
  Scenario: A stale refresh token is never sent to Atlassian
    Given the application has performed at least one successful refresh
    When the application performs the next refresh
    Then the refresh token sent to Atlassian is the one Atlassian issued in the most recent response
    And the refresh token sent to Atlassian is NOT the one that was used in any earlier refresh

  @real-io
  Scenario: The credential does not silently flip to "RefreshFailed" across a rotation chain
    When the application performs five consecutive refreshes that all succeed at Atlassian
    Then the credential status reads "Valid" after each refresh
    And the credential status never transitioned through "RefreshFailed"
