// A test over its budget is listed here with why it is slow and a ceiling in seconds on a quiet machine.
// An entry whose test now runs under a quarter of the budget, or no longer exists, fails the run until it is removed.
import type { AllowListEntry } from "./compareToAllowList.ts";

export const slowTests: readonly AllowListEntry[] = [
	{
		file: "src/components/Common/WorkItemsDialog/WorkItemsDialog.paging.test.tsx",
		name: "A Work Items dialog with more than 100 Work Items Priya, on the second page of 243, filters to 64 and sees all of them with no footer",
		reason:
			"Renders a dialog with 243 Work Items, moves to the second page and filters the list down to 64.",
		ceilingSeconds: 3.5,
	},
	{
		file: "src/pages/Settings/SystemInfo/ThirdPartyPackagesSection.paging.test.tsx",
		name: "Third-party packages pages like every other list keeps the reader's sort and column filter when a search opens the first page",
		reason:
			"Renders a long list of packages and walks through sorting, a column filter and a search that returns to the first page.",
		ceilingSeconds: 2.5,
	},
	{
		file: "src/pages/Teams/Detail/Refinement/RefinementView.paging.test.tsx",
		name: "Team Gravity's 143 Work Items in refinement can all be reached Jonas pages to GR-0131, the 131st Work Item in refinement, and votes on it",
		reason:
			"Renders 143 Work Items in refinement, pages through them to the 131st and casts a vote on it.",
		ceilingSeconds: 5.0,
	},
];
