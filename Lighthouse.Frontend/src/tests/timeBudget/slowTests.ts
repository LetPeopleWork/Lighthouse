// A test over its budget is listed here with why it is slow and a ceiling in seconds on a quiet machine.
// An entry whose test now runs well under budget, or no longer exists, fails the run until it is removed.
import type { AllowListEntry } from "./compareToAllowList.ts";

const refinementPagingFile =
	"src/pages/Teams/Detail/Refinement/RefinementView.paging.test.tsx";
const reportsRemovalFile = "src/pages/Reports/ReportsPage.removal.test.tsx";
const teamQueryCommitFile =
	"src/components/Common/Team/ModifyTeamSettings.queryCommit.test.tsx";
const teamRefinementNeedFile =
	"src/components/Common/Team/ModifyTeamSettings.refinementNeed.test.tsx";

export const slowTests: readonly AllowListEntry[] = [
	{
		file: "src/components/Common/DataGrid/DataGridBase.paging.test.tsx",
		name: "Turning a page brings the top of the list into view never scrolls for a page change the reader did not ask for with an arrow",
		reason:
			"Renders a long paged list and steps through its pages with the keyboard, checking where the view scrolls after each step.",
		ceilingSeconds: 3.0,
	},
	{
		file: "src/components/Common/ProjectSettings/ModifyProjectSettings.queryCommit.test.tsx",
		name: "ModifyProjectSettings query field saves the edited query once, after the field is left",
		reason:
			"Renders the whole project settings form and types a query, then leaves the field to see it save once.",
		ceilingSeconds: 3.0,
	},
	{
		file: teamQueryCommitFile,
		name: "ModifyTeamSettings query field saves a changed name after the debounce without anyone leaving a field",
		reason:
			"Renders the whole team settings form and waits out the autosave delay with real timers before the save happens.",
		ceilingSeconds: 2.5,
	},
	{
		file: teamQueryCommitFile,
		name: "ModifyTeamSettings query field saves the edited query once, after the field is left",
		reason:
			"Renders the whole team settings form, types a query and leaves the field to see it save once.",
		ceilingSeconds: 3.0,
	},
	{
		file: teamQueryCommitFile,
		name: "ModifyTeamSettings query field saves the latest edit after the save already on its way, when the page is left meanwhile",
		reason:
			"Renders the whole team settings form and edits twice while a save is in flight, then leaves the page to see the latest edit saved.",
		ceilingSeconds: 3.0,
	},
	{
		file: teamRefinementNeedFile,
		name: "Stage rules in the Refinement section of a Team's settings keeps an edited Ready rule when the Yes votes needed change before it is saved",
		reason:
			"Renders the team settings form with its stage rules and edits a rule's votes needed before saving.",
		ceilingSeconds: 2.5,
	},
	{
		file: teamRefinementNeedFile,
		name: "Stage rules in the Refinement section of a Team's settings saves a changed Ready rule and leaves the Being refined rule as it was",
		reason:
			"Renders the team settings form with its stage rules, edits one rule and saves, checking the other rule is untouched.",
		ceilingSeconds: 2.5,
	},
	{
		file: "src/components/Common/WorkItemsDialog/WorkItemsDialog.paging.test.tsx",
		name: "A Work Items dialog with more than 100 Work Items Priya, on the second page of 243, filters to 64 and sees all of them with no footer",
		reason:
			"Renders a dialog with 243 Work Items, moves to the second page and filters the list down to 64.",
		ceilingSeconds: 3.5,
	},
	{
		file: "src/pages/Common/MetricsView/DashboardHeader.test.tsx",
		name: "DashboardHeader shows label and formatted date range and opens popover which calls handlers",
		reason:
			"Renders the dashboard header and opens its date popover, which loads the date picker the first time.",
		ceilingSeconds: 2.5,
	},
	{
		file: reportsRemovalFile,
		name: "Remove several reports at once a coach removes three trial reports on two Teams and a Portfolio, and they leave the list",
		reason:
			"Renders the Reports page, picks three reports across Teams and a Portfolio and removes them, waiting for the list to update.",
		ceilingSeconds: 2.5,
	},
	{
		file: "src/pages/Reports/ReportsPage.usageData.test.tsx",
		name: "What the Reports page tells the usage data a Retry and a Leave it out on one PDF of several reports count nothing more",
		reason:
			"Renders the Reports page, picks several reports and walks through a failed export, a retry and leaving a report out.",
		ceilingSeconds: 2.5,
	},
	{
		file: "src/pages/Settings/SystemInfo/ThirdPartyPackagesSection.paging.test.tsx",
		name: "Third-party packages pages like every other list keeps the reader's sort and column filter when a search opens the first page",
		reason:
			"Renders a long list of packages and walks through sorting, a column filter and a search that returns to the first page.",
		ceilingSeconds: 2.5,
	},
	{
		file: refinementPagingFile,
		name: "Team Gravity's 143 Work Items in refinement can all be reached Jonas pages to GR-0131, the 131st Work Item in refinement, and votes on it",
		reason:
			"Renders 143 Work Items in refinement, pages through them to the 131st and casts a vote on it.",
		ceilingSeconds: 5.0,
	},
	{
		file: refinementPagingFile,
		name: "Team Gravity's 143 Work Items in refinement can all be reached with 0 needed, the line sits where it falls in the whole list (page 1: { side: 'above', by: 'GR-0001', …(1) }, page 2: null)",
		reason:
			"Renders 143 Work Items in refinement and checks where the threshold line falls with 0 votes needed.",
		ceilingSeconds: 2.5,
	},
	{
		file: refinementPagingFile,
		name: "Team Gravity's 143 Work Items in refinement can all be reached with 100 needed, the line sits where it falls in the whole list (page 1: { side: 'below', by: 'GR-0100', …(1) }, page 2: null)",
		reason:
			"Renders 143 Work Items in refinement and checks where the threshold line falls with 100 votes needed.",
		ceilingSeconds: 2.5,
	},
	{
		file: refinementPagingFile,
		name: "Team Gravity's 143 Work Items in refinement can all be reached with 101 needed, the line sits where it falls in the whole list (page 1: null, page 2: { side: 'below', by: 'GR-0101', …(1) })",
		reason:
			"Renders 143 Work Items in refinement and checks where the threshold line falls with 101 votes needed.",
		ceilingSeconds: 2.5,
	},
	{
		file: refinementPagingFile,
		name: "Team Gravity's 143 Work Items in refinement can all be reached with 120 needed, a State filter on the second page that leaves 64 shows them all, the line under the last",
		reason:
			"Renders 143 Work Items in refinement, moves to the second page and applies a State filter that leaves 64.",
		ceilingSeconds: 4.5,
	},
	{
		file: refinementPagingFile,
		name: "Team Gravity's 143 Work Items in refinement can all be reached with 120 needed, numbers the first page 1 to 100 and continues at 101 on the second, up to 120",
		reason:
			"Renders 143 Work Items in refinement and checks the numbering across two pages with 120 needed.",
		ceilingSeconds: 2.5,
	},
	{
		file: refinementPagingFile,
		name: "Team Gravity's 143 Work Items in refinement can all be reached with 120 needed, the line sits where it falls in the whole list (page 1: null, page 2: { side: 'below', by: 'GR-0120', …(1) })",
		reason:
			"Renders 143 Work Items in refinement and checks where the threshold line falls with 120 votes needed.",
		ceilingSeconds: 2.5,
	},
	{
		file: refinementPagingFile,
		name: "Team Gravity's 143 Work Items in refinement can all be reached with 143 needed, the line sits where it falls in the whole list (page 1: null, page 2: { side: 'below', by: 'GR-0143', …(1) })",
		reason:
			"Renders 143 Work Items in refinement and checks where the threshold line falls with 143 votes needed.",
		ceilingSeconds: 2.5,
	},
	{
		file: refinementPagingFile,
		name: "Team Gravity's 143 Work Items in refinement can all be reached with 150 needed, the line sits where it falls in the whole list (page 1: null, page 2: { side: 'below', by: 'GR-0143', …(1) })",
		reason:
			"Renders 143 Work Items in refinement and checks where the threshold line falls with 150 votes needed.",
		ceilingSeconds: 2.5,
	},
	{
		file: refinementPagingFile,
		name: "Team Gravity's 143 Work Items in refinement can all be reached with 20 needed, the line sits where it falls in the whole list (page 1: { side: 'below', by: 'GR-0020', …(1) }, page 2: null)",
		reason:
			"Renders 143 Work Items in refinement and checks where the threshold line falls with 20 votes needed.",
		ceilingSeconds: 2.5,
	},
	{
		file: "src/pages/Teams/Detail/Refinement/RefinementView.test.tsx",
		name: "The Refinement tab lists the Work Items in refinement handles three hundred Work Items, counting all of them and starting at the top of the backlog",
		reason:
			"Renders the Refinement tab with three hundred Work Items and counts all of them.",
		ceilingSeconds: 3.0,
	},
];
