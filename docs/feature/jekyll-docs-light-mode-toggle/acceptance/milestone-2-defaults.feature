Feature: First-time visitor defaults respect OS preference, preserving today's dark default
  As a first-time visitor with no stored color-scheme choice
  I want the docs to follow my OS preference when it is set
  And to fall back to the existing dark default otherwise
  So that the docs feel native to my environment without surprising existing users

  Background:
    Given the Jekyll docs site is served locally via `bundle exec jekyll serve`
    And the visitor has no prior color-scheme choice recorded for the docs origin

  @real-io @milestone-2 @US-defaults
  Scenario: First-time visitor on a light-preferring OS lands in light mode
    Given the visitor's OS reports `prefers-color-scheme: light`
    When the visitor opens the docs landing page at `/`
    Then the page renders in the light color scheme
    And no color-scheme choice is recorded for the docs origin (no implicit write on first paint)

  @real-io @milestone-2 @US-defaults
  Scenario: First-time visitor on a dark-preferring OS lands in dark mode
    Given the visitor's OS reports `prefers-color-scheme: dark`
    When the visitor opens the docs landing page at `/`
    Then the page renders in the dark color scheme
    And no color-scheme choice is recorded for the docs origin (no implicit write on first paint)

  @real-io @milestone-2 @US-defaults
  Scenario: First-time visitor with no OS preference lands in dark mode (preserves today's default)
    Given the visitor's browser reports `prefers-color-scheme: no-preference`
    When the visitor opens the docs landing page at `/`
    Then the page renders in the dark color scheme

  # Verification note for human testers: in Chromium devtools, OS preference can be emulated via
  # Rendering > Emulate CSS media feature `prefers-color-scheme`. The "no choice recorded" check
  # is a `localStorage` inspection: the key `lighthouse-docs-color-scheme` must be absent.
