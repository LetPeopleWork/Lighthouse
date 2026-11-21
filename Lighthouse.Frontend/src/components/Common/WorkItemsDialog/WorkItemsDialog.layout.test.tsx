import { within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
	givenANarrowWindow,
	givenTextIsWiderThanItsColumn,
	resetTheCut,
	resetTheWindow,
	thenALongTextIsCutToItsColumn,
	thenTheColumnIsShownWhole,
	thenTheFullTextIsInATooltip,
	thenTheTableSaysColumnsAreOutOfSightOnItsRight,
	whenThePersonHoversOver,
	whenThePersonReachesWithTheKeyboard,
} from "../../../tests/SharedGridFitKit";
import {
	aFeature,
	aWorkItem,
	contextColumnsShown,
	day,
	estimateOf,
	openTheDialog,
	resetTheLayout,
	theDialog,
	turnColumn,
} from "../../../tests/WorkItemsDialogTestKit";

vi.mock("@mui/x-data-grid", async () =>
	(await import("../../../tests/SharedGridFitKit")).dataGridWithItsBoxStated(),
);

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
	it(
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

	it(
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

	it(
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

	it(
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

	it(
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

	it(
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

	it(
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

describe("the Work Item dialog fits its columns and says when some are out of sight", () => {
	const LONG_NAME =
		"Replatform the order management stack onto the shared event backbone for every region";
	const LONG_TEAM =
		"Platform Engineering Delivery Squad for the Northern Region";

	beforeEach(() => {
		givenANarrowWindow();
	});

	afterEach(() => {
		resetTheCut();
		resetTheWindow();
		localStorage.clear();
	});

	const whenTheDialogOpensOnALongNamedFeature = () =>
		openTheDialog({
			context: "closedItems",
			ownerKind: "team",
			items: [
				aFeature({
					referenceId: "FTR-1",
					name: LONG_NAME,
					owningTeam: LONG_TEAM,
					state: "Done",
				}),
			],
		});

	it("cuts a long Work Item name and a long owning Team to their columns and shows the type, state and closed date whole", () => {
		givenTextIsWiderThanItsColumn();
		whenTheDialogOpensOnALongNamedFeature();
		const dialog = theDialog();

		thenALongTextIsCutToItsColumn(dialog, "name");
		thenTheColumnIsShownWhole(dialog, "type");
		thenTheColumnIsShownWhole(dialog, "state");
		thenTheColumnIsShownWhole(dialog, "closedDate");
		thenALongTextIsCutToItsColumn(dialog, "owningTeam");
	});

	it("says columns are out of sight when the dialog is narrow", () => {
		whenTheDialogOpensOnALongNamedFeature();

		thenTheTableSaysColumnsAreOutOfSightOnItsRight(theDialog());
	});

	it("shows the full name in a tooltip when the person hovers over a cut name", async () => {
		givenTextIsWiderThanItsColumn();
		whenTheDialogOpensOnALongNamedFeature();

		whenThePersonHoversOver(within(theDialog()).getByText(LONG_NAME));

		await thenTheFullTextIsInATooltip(LONG_NAME);
	});

	it("shows the full name in a tooltip when the person reaches a cut name with the keyboard", async () => {
		givenTextIsWiderThanItsColumn();
		whenTheDialogOpensOnALongNamedFeature();

		whenThePersonReachesWithTheKeyboard(
			within(theDialog()).getByText(LONG_NAME),
		);

		await thenTheFullTextIsInATooltip(LONG_NAME);
	});
});
