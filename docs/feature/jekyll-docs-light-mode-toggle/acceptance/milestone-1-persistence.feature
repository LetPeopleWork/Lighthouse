Feature: Color-scheme choice persists across reload and navigation
  As a returning docs visitor
  I want the docs site to remember my color-scheme choice
  So that I do not have to re-pick light mode on every page

  Background:
    Given the Jekyll docs site is served locally via `bundle exec jekyll serve`
    And the visitor's browser supports `localStorage` for the docs origin

  @real-io @milestone-1 @US-persistence
  Scenario: Light-mode choice survives a full page reload
    Given the visitor previously selected light mode (the choice has been recorded for the docs origin)
    When the visitor reloads the page
    Then the page renders directly in the light color scheme without any visible flash of dark colors
    And the visitor's light-mode choice remains recorded after the reload

  @real-io @milestone-1 @US-persistence
  Scenario: Light-mode choice survives navigation to another docs page
    Given the visitor is on the docs landing page in light mode (the choice has been recorded for the docs origin)
    When the visitor clicks a sidebar link to another docs page (e.g. `Installation`)
    Then the destination page renders directly in the light color scheme without a flash of dark colors

  @real-io @milestone-1 @US-persistence @error
  Scenario: Clearing browser storage returns the visitor to the default scheme
    Given the visitor previously selected light mode for the docs origin
    And the visitor's OS reports `prefers-color-scheme: dark`
    When the visitor clears site data for the docs origin
    And reloads the docs landing page
    Then the page renders in the dark color scheme
    And no prior color-scheme choice is remembered for the docs origin

  # Verification note for human testers (devtools inspection, not part of the user-observable contract):
  # the recorded choice lives in `localStorage` under the key `lighthouse-docs-color-scheme`
  # with values `"light"` or `"dark"`; the DOM-level confirmation is `<html data-theme="light">`
  # (or `"dark"`). These are implementation aids, not user-visible behaviors.
