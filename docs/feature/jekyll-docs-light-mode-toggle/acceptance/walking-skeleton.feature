Feature: Walking skeleton -- visitor toggles to light mode on the docs site
  As a docs visitor who prefers light interfaces
  I want a clearly visible toggle on every page of the Lighthouse docs site
  So that I can switch the docs from dark to light without leaving the page

  Background:
    Given the Jekyll docs site is built by `bundle exec jekyll serve` from the `docs/` directory
    And the visitor is using a modern browser with JavaScript enabled
    And the visitor has no prior `lighthouse-docs-color-scheme` value in localStorage
    And the visitor's OS reports `prefers-color-scheme: dark`

  @walking_skeleton @real-io @driving_adapter @US-toggle
  Scenario: Visitor opens the docs landing page and sees a color-scheme toggle in dark mode
    When the visitor opens the docs landing page at `/`
    Then the page is rendered in the dark color scheme
    And a color-scheme toggle control is visible in the site header
    And the toggle's accessible name offers switching to light mode

  @real-io @driving_adapter @US-toggle
  Scenario: Visitor activates the toggle and the page repaints in light mode
    Given the visitor is on the docs landing page rendered in the dark color scheme
    When the visitor activates the color-scheme toggle
    Then the page repaints in the light color scheme without a full page reload
    And the toggle's accessible name now offers switching back to dark mode

  # Verification note for human testers (devtools inspection, not part of the user-observable contract):
  # the `<html>` element's `data-theme` attribute should flip between `"dark"` and `"light"`
  # as the toggle is activated; the `just-the-docs` `jtd.setTheme()` JS API is responsible for setting it.
