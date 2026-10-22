# RED classification — stories 6348, 6352, 6355

Revision round 1 (2026-10-22). Every pending scenario was un-skipped once (not committed) and run with
`pnpm vitest run <file> --reporter=verbose`. All 44 failed on the missing behaviour; none failed on setup,
import or types. The 7 active guards passed in the same run. The popover helpers (`openTheTip` finding the MUI
popover by role `presentation`, Enter to open, Tab to "Learn More", Escape to close) were proven once against
today's dashboard widget popover in a throwaway probe, so a slice 02 scenario that still fails after DELIVER is
failing on the form, not on the helper.

Paths are under `Lighthouse.Frontend/src/`.

## Slice 01, Story 6348 — `pages/Common/AdvancedInputs/AdvancedInputs.closedItemsCutoff.test.tsx`

| Scenario | Classification | Failure seen |
|---|---|---|
| Team: the line says what it limits, of its Work Items | MISSING_FUNCTIONALITY | `toHaveAccessibleDescription` received the old "Number of days to retain closed/done items…" |
| Portfolio: the same line speaks of its Features | MISSING_FUNCTIONALITY | same, old helper text received |
| Tickets on a Team, never Work Items | MISSING_FUNCTIONALITY | same, old helper text received |
| Bets on a Portfolio, never Features | MISSING_FUNCTIONALITY | same, old helper text received |
| Before the settings arrive, the line already explains | MISSING_FUNCTIONALITY | same, old helper text received |
| Names Reports; old wording gone | MISSING_FUNCTIONALITY | `Unable to find an element with the text: /how far back Reports can reach/` |

## Slice 02, Story 6352

### `pages/Common/AdvancedInputs/AdvancedInputs.parentOverrideTip.test.tsx`

All 11 fail with `Unable to find … role "button" and name "About Parent Override Field"`: MISSING_FUNCTIONALITY
(no info button yet).

| Scenario |
|---|
| Jira Team: is caused by, and what None does |
| Jira Portfolio: same example |
| Azure DevOps Team: the parent's id, no Jira phrase |
| Linear / Csv / ServiceNow / unknown tracker: neutral (4 cases) |
| Team speaks of a Ticket, never a Work Item |
| Portfolio speaks of a Bet, never a Feature |
| Team: Learn More opens the Team docs |
| Portfolio: Learn More opens the Portfolio docs |

### `components/Common/ProjectSettings/Advanced/DependenciesComponent.directionTip.test.tsx`

All 11 fail with `Unable to find … role "button" and name "About Dependency Field"`: MISSING_FUNCTIONALITY.

| Scenario |
|---|
| Jira: is blocked by, not blocks |
| Jira: None reads Jira's links, a field replaces them |
| Azure DevOps: Reverse end, Predecessor |
| Linear / Csv / ServiceNow / unknown tracker: neutral (4 cases) |
| Speaks of a Bet, never a Feature |
| Learn More opens the Dependency Field docs |
| Keyboard alone: Enter opens, Tab reaches Learn More |
| Escape closes; the button says it is closed |

### `components/Common/Connection/ModifyConnectionSettings.directionTips.test.tsx`

| Scenario | Classification | Failure seen |
|---|---|---|
| Only the Parent Override and Dependency rows carry an About button | MISSING_FUNCTIONALITY | `expected [] to deeply equal [ 'About Parent Override Field' ]` |
| Estimation, Size Estimate, Feature Owner carry no info icon | MISSING_FUNCTIONALITY | `expected [] to have a length of 1` (no icon on the Teams Parent Override row yet) |
| Jira: Teams' Parent Override tip gives Jira's example | MISSING_FUNCTIONALITY | no button "About Parent Override Field" |
| Jira: Dependency tip asks for is blocked by | MISSING_FUNCTIONALITY | no button "About Dependency Field" |
| Azure DevOps: Dependency names Predecessor | MISSING_FUNCTIONALITY | no button "About Dependency Field" |
| Tenant's words: Dependency tip speaks of a Bet | MISSING_FUNCTIONALITY | no button "About Dependency Field" in the "Programs" group (the group was found, so the renamed terms arrived) |
| Teams' Parent Override → Team docs | MISSING_FUNCTIONALITY | no button "About Parent Override Field" |
| Portfolios' Parent Override → Portfolio docs | MISSING_FUNCTIONALITY | no button "About Parent Override Field" |
| Dependency → Dependency Field docs | MISSING_FUNCTIONALITY | no button "About Dependency Field" |

### `components/Common/FormSelectField/FormSelectField.info.test.tsx`

All 4 fail with `Unable to find … role "button" and name "About Dependency Field"` (`info` is accepted and
ignored): MISSING_FUNCTIONALITY. Scenarios: the button is named after the field and closed until asked; opening it
shows the tip and Learn More; a tip with nowhere to lead has no Learn More; Escape closes it.

## Slice 03, Story 6355 — `pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySourceTab.whyAndSearch.test.tsx`

| Scenario | Classification | Failure seen |
|---|---|---|
| Add Delivery's Jira Release tab: placeholder "Type to search Jira Releases" | MISSING_FUNCTIONALITY | `toHaveAttribute("placeholder", …)`, the attribute is absent |
| A dateless Release reads "No date in Jira: set one there to follow it" | MISSING_FUNCTIONALITY | `toHaveTextContent`, the row reads "No Release Date Set" |
| Unknown source key: "Type to search Milestones", "No date in Tracker: …" | MISSING_FUNCTIONALITY | `toHaveAttribute("placeholder", "Type to search Milestones")`, the attribute is absent |
