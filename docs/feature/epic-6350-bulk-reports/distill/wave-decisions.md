# Epic 6350 — DISTILL wave decisions (2026-10-27, revised after the final wave review gate)

| Id | Decision | Why |
|---|---|---|
| DS-1 | Mirror the Epic 6298 suites: backend scenarios on `ReportsAcceptanceTest` (WebApplicationFactory, SQLite, mocked licence, fake clock), pending via `[Ignore(PendingEpic6350.SliceNN)]`; Vitest `it.skip` with a pending string; Playwright `fixme`; walking skeletons pending too | One precedent for reports suites; DELIVER enables one scenario at a time |
| DS-2 | Changes that happen *during* a bulk run (Admin lost, owner deleted, licence lapses, owner deleted between lookup and save) are modelled by pass-through decorators on `IRbacAdministrationService` and `IReportOwnerLookup` that fire once at a named owner | No clock or second request can interleave inside one request; the decorators keep the production services in the path |
| DS-3 | Slice 01 pins only the first sentence of NMD-1's empty copy; from 02a the whole sentence, also on Community | "or here with Create reports" names a button that does not exist before 02a |
| DS-4 | Selection scenarios (NMD-3) are tagged slice 05 and live in the removal file; the 07 file repeats the ceiling for Export PDF | AC-1.9/1.10 make selection visible only once Remove or Export PDF ships |
| DS-5 | NMD-7's confirm and result copy supersedes the US-05 pitch wording | Locked later, in the sketch walk |
| DS-6 | NMD-8's "No PDF was made" dialog supersedes DESIGN's snackbar | Same |
| DS-7 | The Owner cell shows the name alone as the link, announced with its kind ("Zenith, Team"); tests find the owner by that accessible name; the E2E reads "Team Zenith, Team" on demo data (NMD-11) | Settles the first pass's UI-1 and UI-2 |
| DS-8 | "The reports couldn't be loaded." (page) and "It could not be confirmed which reports were removed." (removal) are taken from DESIGN's sketches / mirrored from NMD-7's creation sentence | No NMD words them; one constant each in the kit |
| DS-9 | Result reason lines are pinned word for word from NMD-12 (`notCreatedBecause`, `notRemovedBecause`) | Replaces the first pass's prefix-only pins |
| DS-10 | E2E is one walking skeleton (bulk create on Zenith and Gravity, demo scenario 12, both listed, one opens); its POM waits for each wizard step by the step's own words | DEVOPS DoD 6 and the thin-E2E rule |
| DS-11 | The Postgres FK race runs on SharedPostgresContainer with a fresh database per case; SQLite covers the same race on the base host; EF InMemory is not used | InMemory has no foreign keys |
| DS-12 | Clients: the last Lighthouse without the cross-owner list is a stand-in tag (`v26.10.25.1`), marked "RESOLVED AT DELIVER" in both files: DELIVER reads the real tag from the releases | The release carrying this Epic is not cut yet |
| DS-13 | Step-reuse ratio not measured (informational only; no Python AST tooling for C#/TS); scenario bodies drive their port through the shared step class or kit | Readability over ratio |
| DS-14 | The removal confirmation lists each report as "Name — owner name", the same name-only owner as the table (NMD-11) | NMD-7 says "Name — Owner"; the owner is written as the table writes it |
| DS-15 | Export PDF with nothing selected is pinned to "Select at least one report." | NMD-1 asks every disabled state to carry its reason but does not word this one; DELIVER shows it at the slice 07 sketch (UI-7) |
| DS-16 | The backend RBAC test double's readable-Team answer is scoped to the caller's grants in DISTILL, like its Portfolio twin | So "a Team she cannot read is not listed" passes after a correct build; full backend suite re-run green apart from the 2 known worktree Licensing tests |
| DS-17 | The clients name an owner's kind in the instance's words (the CLI honours Terminology); the default-term scenario reads "Team" / "Portfolio" because the fake answers the seeded terms, and a renamed-terms scenario pins "Squad" / "Programme" | The CLI already renders configured terms elsewhere |

## AC → scenario traceability

Files: **BE01** `Slice01EveryReportInOnePlaceScenarios.cs`, **BE02a** `Slice02aCreateForSeveralTeamsScenarios.cs`,
**BE03** `Slice03PortfoliosJoinTheSelectionScenarios.cs`, **BE05** `Slice05RemoveReportsInBulkScenarios.cs`,
**BEall** `BulkReportsWhereEveryoneIsAdminScenarios.cs`, **BEpg** `BulkReportOwnerDeletedMidRunOnPostgresTests.cs`,
**BEuse** `BulkReportsUsageEventTests.cs`; **FE01** `ReportsPage.test.tsx`, **FEnav** `Header.reportsNav.test.tsx`,
**FEuse** `ReportsPage.usageData.test.tsx`, **FEcon** `ReportsPage.usageData.consent.test.tsx`, **FEwiz**
`BulkReportWizard.test.tsx`, **FEown** `BulkReportWizard.ownerStepLooks.test.tsx`, **FEpor**
`BulkReportWizard.portfolios.test.tsx`, **FElim** `BulkReportWizard.ownerLimits.test.tsx`, **FErem**
`ReportsPage.removal.test.tsx`, **FEpdf** `ReportsPage.combinedPdf.test.tsx`, **FEbun** `bundleState.test.ts`;
**E2E** `BulkReports.spec.ts`; **CLI** `reportListAll.test.ts`, **CLIuse** `reportListAllUsageData.test.ts`, **MCP**
`reportsAcrossOwners.test.ts`.

| AC | Scenarios (file::name) |
|---|---|
| AC-1.1 | FEnav::Reports sits between Features and System Settings; FEnav::Reports opens the Reports page; FEnav::a reader who is Admin of nothing still finds Reports |
| AC-1.2 | BE01::A_lead_reads_every_report_he_can_read_with_its_owner_on_a_Community_instance; BE01::Each_row_names_its_owners_kind_id_and_name_beside_the_report; FE01::a coach finds the reports of two Teams and a Portfolio…; FE01::the owner cell shows the name alone, is announced with its kind…; FE01::the owner's kind is announced in the instance's own word; FE01::two reports of the same name…; FE01::the filter narrows the rows by owner name; FE01::the filter still matches an owner's kind… |
| AC-1.3 | FE01::a coach finds the reports … and opens one; E2E::a coach creates one comparison … opens one |
| AC-1.4 | BE01::A_report_on_a_Portfolio_he_cannot_read_is_never_sent; BE01::A_report_on_a_Team_he_cannot_read_is_never_sent; BE01::A_caller_who_can_read_no_owner_gets_an_empty_list |
| AC-1.5 | BE01::An_instance_without_reports_answers_an_empty_list_not_a_failure; FE01::with no report anywhere it says where reports are created; FE01::while the reports are on their way…; FE01::when the reports cannot be loaded… Retry loads them; FEwiz::with no report anywhere yet, the empty page also offers Create reports… |
| AC-1.6 | BE01::A_report_of_a_type_this_version_cannot_show_is_still_listed; FE01::a report of a type this version cannot show is listed and says so |
| AC-1.7 | BE01::A_lead_reads_every_report … on_a_Community_instance; BEall::Every_report_on_every_owner_is_listed (auth off, roles not enforced) |
| AC-1.8 | BEuse::The_event_travels_with_its_name_and_nothing_else("AllReportsListed"); BEuse::The_event_carrying_anything_beside_its_name_is_refused (2 rows); BEuse::The_event_is_appended_after_the_PDF_export_never_inserted; BEuse::The_usage_data_page_has_one_row…("Every report was listed"); FEuse::showing the page is told once…; FEuse::a page whose reports could not be loaded tells nothing; FEcon::counts the page…; FEcon::counts nothing from a browser that said no… |
| AC-1.9 | FErem::Select all picks every row the filter shows, on every page; FErem::above 50 picked…; FErem::exactly 50 picked… |
| AC-1.10 | FErem::changing the filter clears what was picked; FErem::sorting keeps what was picked; FErem::Select all picks every row the filter shows… |
| AC-1.11 | FE01::Edit and Delete show only on rows the reader is Admin of; PDF shows on every row; FE01::while permissions are on their way no row offers Edit or Delete…; FE01::Delete on a row asks the owner tab's confirmation…; FEuse::deleting one report from its row is told as one deleted report… |
| (AC-1.x rebuild flag) | BE01::Each_row_says_whether_its_Then_can_still_be_rebuilt; FE01::a report whose Then can no longer be rebuilt is marked on its row |
| AC-2.1 | BE02a::A_Community_instance_is_refused_the_whole_run_and_nothing_is_written; BEall::A_Community_instance_is_still_refused_the_whole_run; FEwiz::on Community it is shown but disabled, with the Premium licence tooltip; FEwiz::with no report anywhere yet, the empty page also offers Create reports, on Premium and Community alike |
| AC-2.2 | FEwiz::with %s left as it opens, Then ends the day before Now starts (3 rows); FEwiz::once it has arrived the wizard opens at its first step, filled; FEpor::Then offers 30, 90 and 180 days… |
| AC-2.3 | BE02a::The_creation_options_offer_the_type_its_metrics_the_window_bounds_and_every_readable_owner; BE02a::The_creation_options_leave_out_an_owner_the_caller_cannot_read; FEown::a coach who only reads Pulsar is offered Gravity and Zenith…; BEall::Every_Team_may_be_created_on_in_one_run |
| AC-2.4 | BE02a::Each_report_made_in_bulk_equals_the_one_the_single_wizard_makes_from_the_same_inputs; FEwiz::every Team picked gets the same comparison… |
| AC-2.5 | BE02a::A_Team_whose_kept_history_is_too_short_is_skipped_with_the_day_its_history_starts; FEwiz::a Team refused is named with its reason…; FEwiz::a Team refused for %s gets its own line (2 rows); FEwiz::when every Team is refused … each line says why; BE02a::A_created_row_names_the_new_report_which_opens_at_its_owner |
| AC-2.6 | FEwiz::when no answer arrives it says it cannot confirm what was created… |
| AC-2.7 | BE02a::A_Team_she_only_reads_is_not_created_on_and_the_others_are; BE02a::Changes_during_the_run_are_refused_owner_by_owner (3 rows); BE02a::An_owner_deleted_between_lookup_and_save_fails_only_its_own_row; BEpg::An_owner_deleted_between_lookup_and_save_fails_only_its_own_row_on_Postgres; BE02a::A_Community_instance_is_refused_the_whole_run… |
| AC-2.8 | BE02a::Changes_during_the_run_are_refused_owner_by_owner (licence-lapse row); FEwiz::a Team refused for the Community cap after the licence lapsed… |
| AC-2.9 | BE02a::Running_the_same_creation_twice_gives_every_Team_a_second_copy; FEwiz::creating the same report twice creates it twice, with no warning |
| AC-2.10 | BEuse::(ReportBulkCreationStarted rows); BEuse::The_usage_data_page_says_what_the_bulk_actions_add_to_an_existing_event("A report was created"); FEuse::a bulk creation is told once, and one created report per report actually created |
| AC-2.11 | BE02a::A_run_where_every_Team_is_refused_answers_each_with_its_reason_and_writes_nothing; FEwiz::when every Team is refused the headline says none was created… |
| AC-2.12 | BE02a::More_than_50_owners_in_one_run_are_refused_whole; BE02a::Exactly_50_owners_are_answered_owner_by_owner; BE02a::A_malformed_run_is_refused_whole_and_nothing_is_written (3 rows); BE02a::A_Team_named_twice_is_created_once…; FEwiz::Select all picks all 60 Teams, the button counts them, and Create says at most 50; FEwiz::exactly 50 picked may still be created… |
| AC-2b.1 | FEwiz::for someone who is Admin of no Team or Portfolio it is disabled and says why; FEown::a coach whose Admin right went meanwhile is told why there is nothing to pick… |
| AC-2b.2 | FEwiz::while permissions are on their way it is disabled |
| AC-2b.3 | FEwiz::when permissions cannot be loaded it stays enabled…; FEown::when permissions cannot be loaded the owner step shows its could-not-load look…; FEown::when the owners cannot be loaded it says so…; FEown::while the owners are on their way… |
| AC-2b.4 | FEwiz::while it loads, Create reports spins and is disabled, and no wizard opens; FEwiz::once it has arrived the wizard opens at its first step, filled; FEwiz::when it cannot be loaded the wizard says so…; FEwiz::Retry asks again…; FEwiz::Cancel closes it and creates nothing |
| AC-3.1 | FEpor::Portfolios are listed after Teams, alphabetically, with their own Select all; FEown::Select all picks every Team in its group… |
| AC-3.2 | BE03::A_Portfolio_she_only_reads_is_not_created_on; BE03::A_run_of_Portfolios_only_creates_on_each; FEpor::only Portfolios may be picked… |
| AC-3.3 | FEpor::Then offers 30, 90 and 180 days or a custom length…; FEpor::a custom Then shorter than 14 days is refused…; FEpor::a custom Then of exactly 14 days is accepted; FEpor::a custom Now length travels with the request… |
| AC-3.4 | BE03::Teams_and_a_Portfolio_get_the_same_comparison_in_one_run; BE03::A_Portfolio_whose_kept_history_is_too_short…; BE03::A_Portfolio_deleted_while_the_run_is_under_way_is_one_row; FEpor::a coach creates one comparison for two Teams and Ocean Explorer in one go |
| AC-4.1 | FElim::Equinox is listed but cannot be picked, and says why and where to change it |
| AC-4.2 | dropped after DESIGN — no scenario |
| AC-4.3 | FElim::Select all picks every Team that can hold the window and leaves Equinox out; FElim::when every owner picked cannot hold the window nothing can be created |
| AC-4.4 | FElim::a shorter window that stays inside Equinox's kept history makes it pickable |
| AC-4.5 | BE03::Each_owners_history_start_in_the_creation_options_is_the_one_its_own_refusal_names |
| AC-4.6 | FElim::a Then starting on the very day the kept history starts can be held |
| AC-4.7 | FEown::while the owners are on their way…; FEown::when the owners cannot be loaded… |
| AC-5.1 | FErem::with one picked report on an owner she only reads, Remove is disabled and says why |
| AC-5.2 | FErem::a coach removes three trial reports…; FErem::the confirmation warns when one of them holds a Then…; FErem::with every Then still rebuildable the confirmation carries no warning; FErem::Cancel removes nothing |
| AC-5.3 | FErem::a report the server did not remove is named with its reason and stays listed; FErem::a report somebody else removed first counts as removed; FErem::while the removal is under way the dialog cannot be closed |
| AC-5.4 | BE05::A_report_on_an_owner_she_only_reads_is_not_removed_and_stays; BE05::Losing_Admin_while_the_run_is_under_way_is_answered_for_that_report_alone; BE05::A_report_somebody_else_removed_first_counts_as_removed_and_says_it_was_already_gone; BE05::A_report_whose_owner_was_deleted_counts_as_already_gone; BE05::More_than_50_reports_are_refused_whole…; BE05::A_removal_naming_no_report_is_refused; BE05::A_report_named_twice_is_removed_once… |
| AC-5.5 | BE05::A_coach_removes_reports_on_two_Teams_and_a_Portfolio_in_one_go_on_a_Community_instance |
| AC-5.6 | BEuse::(ReportBulkRemovalStarted rows); BEuse::…existing_event("A report was deleted"); FEuse::a bulk removal is told once, and one deleted report per report this run removed |
| AC-5.7 | FErem::when no answer arrives it says it cannot confirm what was removed |
| AC-5.8 | BEall::Every_report_may_be_removed_in_one_run (auth off, roles not enforced) |
| AC-6.1 | CLI::passes on every report exactly as Lighthouse sent it…; CLI::sums the list up by report…; CLI::names each owner's kind in the instance's own words; CLI::says there are no reports…; MCP::offers the tool…; MCP::hands over every report…; MCP::says there are none… |
| AC-6.2 | CLI::only ever reads, and reads once; MCP::hands over every report… (reads `GET /reports` once) |
| AC-6.3 | CLI::tells the reader to upgrade…; CLI::says the reader is not signed in…; MCP::tells the assistant to upgrade…; CLI::a list naming one Team still reads only that Team's reports |
| AC-6.4 | release mechanics: the `lighthouse` skill drift test already in the clients' CI; no new scenario |
| AC-6.5 | release mechanics (changesets, manual version bump); no scenario |
| AC-6.6 | CLIuse::reports AllReportsListed by name alone; CLIuse::reports nothing for a reader who said no; CLIuse::reports nothing for a list Lighthouse refused; CLIuse::reports nothing for a list of one Team's reports; MCP::reports AllReportsListed by name alone, with source Mcp; MCP::reports nothing for a list Lighthouse refused |
| AC-7.1 | FEpdf::a coach exports the reports of two Teams as one PDF…; FEpdf::a reader who is Admin of none of them may still export them; FEpdf::with nothing picked Export PDF is disabled and says why |
| AC-7.2 | FEpdf::a coach exports the reports of two Teams as one PDF, each on a page of its own; FEbun::%j reads as %j (6 rows) |
| AC-7.3 | FEpdf::the reports print in the table's order, as the reader sorted it |
| AC-7.4 | FEpdf::a report that cannot be loaded stops the PDF and is named; FEpdf::Leave it out unpicks the report that failed…; FEpdf::Leave it out is not offered when … nothing to print; FEpdf::Retry asks for the report again…; FEpdf::when part of the page cannot be loaded…; FEbun::a bundle of no reports stays preparing… |
| AC-7.5 | FEpdf::above 50 picked, Export PDF is disabled and says at most 50 |
| AC-7.6 | inherited unchanged from Bug 6425 (its own tests on `main`); no new scenario, as the AC says slice 07 adds no macOS handling |
| AC-7.7 | FEpdf::while the reports load, Export PDF shows it is preparing and nothing prints yet; FEpdf::when part of the page cannot be loaded… |
| AC-7.8 | BEuse::(ReportCombinedPdfExportStarted rows); BEuse::…existing_event("A report was exported as a PDF"); FEuse::one PDF of several reports is told once…; FEuse::exporting exactly one picked report is told as the single export… |

Every testable AC maps to at least one scenario. Not mapped by design: AC-4.2 (dropped), AC-6.4 and AC-6.5 (release
mechanics), AC-7.6 (unchanged inherited behaviour).

## AT-completeness self-audit (15 items)

C1a happy paths per story ✓ · C1b walking skeleton per slice ✓ · C2a RBAC states (Admin / Viewer / none / loading / failed / roles off / auth off) ✓ ·
C2b licence states (Premium / Community / lapses mid-run) ✓ · C3 boundaries (50 / 51 / 60, 0 ids, 14-day Then, kept-history day, 1 vs 2 selected) ✓ ·
C4a load / fail looks on every new view, the wizard's options included ✓ · C4b mid-run changes ✓ · C5a usage events consented / declined ✓ ·
C5b nothing personal travels ✓ · C6a per-owner outcomes and NMD-12 lines ✓ · C6b whole-request refusals ✓ · C6c older server / client ✓ ·
C7a SQLite + Postgres ✓ · C7b renamed terms (browser Owner link, clients list) ✓ · C7c concurrent Admins (already-gone, second copy) ✓.

**15 / 15 — COMPLETE.**

## Step-reuse ratio

Not measured (DS-13).
