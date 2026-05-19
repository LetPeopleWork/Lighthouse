Feature: Toggle is accessible and degrades gracefully under error conditions
  As a visitor relying on keyboard, screen reader, or a restricted browser
  I want the color-scheme toggle to remain usable and the docs to remain readable
  So that light-mode support does not regress accessibility or break offline/private-window usage

  Background:
    Given the Jekyll docs site is served locally via `bundle exec jekyll serve`

  @real-io @milestone-3 @US-a11y @accessibility
  Scenario: Toggle is reachable by Tab key with a visible focus indicator
    Given the visitor is on the docs landing page with focus in the browser address bar
    When the visitor presses Tab until focus reaches the color-scheme toggle
    Then the toggle has a visible focus indicator matching the existing site's focus-ring standard

  @real-io @milestone-3 @US-a11y @accessibility
  Scenario: Toggle activates with the Enter key
    Given the color-scheme toggle has keyboard focus and the page is in dark mode
    When the visitor presses Enter
    Then the page repaints in the light color scheme

  @real-io @milestone-3 @US-a11y @accessibility
  Scenario: Toggle activates with the Space key
    Given the color-scheme toggle has keyboard focus and the page is in dark mode
    When the visitor presses Space
    Then the page repaints in the light color scheme

  @real-io @milestone-3 @US-a11y @accessibility
  Scenario: Toggle exposes an accessible name and button role to assistive tech (dark mode)
    Given the visitor is on the docs landing page in dark mode
    When the visitor inspects the toggle control in the browser accessibility tree
    Then the toggle is announced as a button (role=button), not a generic element
    And the toggle has a non-empty accessible name describing its action (e.g. "Switch to light mode")

  @real-io @milestone-3 @US-a11y @accessibility
  Scenario: Toggle's accessible name updates to reflect the new action after activation
    Given the visitor is on the docs landing page in dark mode and the toggle's accessible name offers switching to light mode
    When the visitor activates the toggle so the page is now in light mode
    Then the toggle's accessible name updates to describe the opposite action (e.g. "Switch to dark mode")

  @real-io @milestone-3 @US-a11y @error
  Scenario: Toggle still works within the session when localStorage write throws
    Given the visitor is browsing the docs in a private window where `localStorage.setItem` throws a QuotaExceededError
    And the visitor's OS reports `prefers-color-scheme: dark`
    When the visitor opens the docs landing page at `/`
    And activates the toggle
    Then the page repaints in the light color scheme within this tab
    And no uncaught exception is reported in the browser console
    And navigating to another docs page in the same tab renders the destination in dark mode (no persistence available -- expected)

  @real-io @milestone-3 @US-a11y @error @graceful-degradation
  Scenario: Page renders in dark mode when JavaScript is disabled
    Given the visitor has disabled JavaScript for the docs origin
    When the visitor opens the docs landing page at `/`
    Then the page renders in the dark color scheme (the existing default)
    And the page content is fully readable
    And the toggle either renders inert (no-op without JS) or is not present -- no broken click handler is exposed

  @real-io @milestone-3 @US-a11y @error
  Scenario: No flash of opposite scheme on full reload of a stored-light visitor
    Given the visitor previously selected light mode for the docs origin
    When the visitor reloads the docs landing page
    Then no dark-mode background colour is painted at any frame between navigation start and first contentful paint
    And the page is in the light color scheme by the time the first contentful paint completes

  # Verification notes for human testers:
  # - "previously selected light mode" = `localStorage.lighthouse-docs-color-scheme = "light"`
  # - QuotaExceededError simulation: open Chromium devtools > Application > Local Storage and
  #   overwrite the store, OR test in a private window with storage quota exhausted.
  # - FOUC check: use devtools Performance recorder to capture paints; alternatively scrub a
  #   slow-motion screen recording. No frames between Navigation Start and FCP should show
  #   the opposite scheme's background colour.
