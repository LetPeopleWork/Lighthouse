import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
	aWorkItem,
	contextColumnsShown,
	day,
	estimateOf,
	openTheDialog,
	resetTheLayout,
	turnColumn,
} from "../../../tests/WorkItemsDialogTestKit";

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		licenseStatus: { canUsePremiumFeatures: false },
		isLoading: false,
	}),
}));

beforeEach(() => {
	localStorage.clear();
});

const RENDER_HEAVY = 20000;

const finishedStories = [
	aWorkItem({
		referenceId: "ST-1",
		cycleTime: 20,
		closedDate: day("2026-09-20"),
		estimate: estimateOf(3),
	}),
	aWorkItem({
		referenceId: "ST-2",
		cycleTime: 4,
		closedDate: day("2026-09-30"),
		estimate: estimateOf(8),
	}),
];

/** The Estimation dialog after the coach hid Closed in it, then closed it. */
async function aCoachHidesClosedInTheTeamsEstimationDialog() {
	const user = userEvent.setup();
	const view = openTheDialog({
		context: "estimation",
		ownerKind: "team",
		items: finishedStories,
	});
	await turnColumn(user, "closedDate", false);
	view.unmount();
	return user;
}

describe("each context remembers its own layout", () => {
	// @us-01 @slice-01b @kpi @contract-shape:bounded-change
	it.skip(
		"a column hidden in the Estimation dialog is still hidden the next time it opens",
		async () => {
			await aCoachHidesClosedInTheTeamsEstimationDialog();

			openTheDialog({
				context: "estimation",
				ownerKind: "team",
				items: finishedStories,
			});

			expect(contextColumnsShown()).toEqual(["estimate", "cycleTime"]);
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01b @kpi @contract-shape:bounded-change
	it.skip(
		"hiding Closed in the Estimation dialog leaves the Cycle Time scatter's dialog as it was",
		async () => {
			await aCoachHidesClosedInTheTeamsEstimationDialog();

			openTheDialog({
				context: "closedItems",
				ownerKind: "team",
				items: finishedStories,
			});

			expect(contextColumnsShown()).toEqual(["closedDate", "cycleTime"]);
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01b @kpi @contract-shape:bounded-change
	it.skip(
		"hiding Closed in a Team's Estimation dialog leaves every Portfolio's Estimation dialog as it was",
		async () => {
			await aCoachHidesClosedInTheTeamsEstimationDialog();

			openTheDialog({
				context: "estimation",
				ownerKind: "portfolio",
				items: finishedStories,
			});

			expect(contextColumnsShown()).toEqual([
				"estimate",
				"cycleTime",
				"closedDate",
			]);
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01b @kpi @contract-shape:bounded-change
	it.skip(
		"a column turned on stays on in its own context only",
		async () => {
			const user = await aCoachHidesClosedInTheTeamsEstimationDialog();
			const reopened = openTheDialog({
				context: "estimation",
				ownerKind: "team",
				items: finishedStories,
			});
			await turnColumn(user, "startedDate", true);
			reopened.unmount();

			const again = openTheDialog({
				context: "estimation",
				ownerKind: "team",
				items: finishedStories,
			});
			expect(contextColumnsShown()).toEqual([
				"estimate",
				"cycleTime",
				"startedDate",
			]);
			again.unmount();

			openTheDialog({
				context: "closedItems",
				ownerKind: "team",
				items: finishedStories,
			});
			expect(contextColumnsShown()).toEqual(["closedDate", "cycleTime"]);
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01b @kpi @contract-shape:bounded-change
	it.skip(
		"Reset layout brings the context back to its defaults, not to every column shown",
		async () => {
			const user = await aCoachHidesClosedInTheTeamsEstimationDialog();
			openTheDialog({
				context: "estimation",
				ownerKind: "team",
				items: finishedStories,
			});

			await resetTheLayout(user);

			expect(contextColumnsShown()).toEqual([
				"estimate",
				"cycleTime",
				"closedDate",
			]);
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01b @error @contract-shape:unbounded-preservation
	it.skip(
		"a layout saved before the dialog knew its contexts does not reach any context",
		() => {
			localStorage.setItem(
				"lighthouse:datagrid:work-items-dialog:state",
				JSON.stringify({ columnVisibilityModel: { closedDate: false } }),
			);

			openTheDialog({
				context: "estimation",
				ownerKind: "team",
				items: finishedStories,
			});

			expect(contextColumnsShown()).toEqual([
				"estimate",
				"cycleTime",
				"closedDate",
			]);
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01b @kpi @contract-shape:bounded-change
	it.skip(
		"the same dialog switched to another context while open shows that context's layout",
		async () => {
			await aCoachHidesClosedInTheTeamsEstimationDialog();
			const dialog = openTheDialog({
				context: "closedItems",
				ownerKind: "team",
				items: finishedStories,
			});

			dialog.showInstead({
				context: "estimation",
				ownerKind: "team",
				items: finishedStories,
			});

			expect(contextColumnsShown()).toEqual(["estimate", "cycleTime"]);
		},
		RENDER_HEAVY,
	);
});
