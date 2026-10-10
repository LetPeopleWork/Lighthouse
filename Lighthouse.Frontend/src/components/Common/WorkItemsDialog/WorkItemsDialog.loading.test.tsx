import { act, screen, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IFeature } from "../../../models/Feature";
import { deferred } from "../../../tests/HeldMetricsService";
import { createMockFeatureService } from "../../../tests/MockApiServiceProvider";
import {
	aFeature,
	aWorkItem,
	cellText,
	columnsShown,
	openTheDialog,
	theDialog,
} from "../../../tests/WorkItemsDialogTestKit";

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		licenseStatus: { canUsePremiumFeatures: false },
		isLoading: false,
	}),
}));

const productWords: Record<string, string> = {
	workItem: "Work Item",
	workItems: "Work Items",
	cycleTime: "Cycle Time",
	workItemAge: "Work Item Age",
	blocked: "Blocked",
};
const instanceWords: Record<string, string> = {};

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => instanceWords[key] ?? productWords[key] ?? key,
	}),
}));

beforeEach(() => {
	localStorage.clear();
	for (const key of Object.keys(instanceWords)) delete instanceWords[key];
});

const RENDER_HEAVY = 20000;

describe("a dialog whose items are still on their way", () => {
	// @us-03 @slice-03 @driving_port @contract-shape:pure-function
	it.skip(
		"opens at once with its title and the context's headers over the grid's loading overlay",
		() => {
			openTheDialog({
				title: "FTR-1: Checkout revamp Work Items",
				context: "featureChildren",
				items: [],
				status: "loading",
			});

			expect(
				within(theDialog()).getByRole("heading", { level: 2 }),
			).toHaveTextContent(/^FTR-1: Checkout revamp Work Items$/);
			expect(columnsShown()).toEqual([
				"referenceId",
				"name",
				"type",
				"state",
				"startedDate",
				"closedDate",
				"ageOrCycleTime",
			]);
			expect(within(theDialog()).getByRole("progressbar")).toBeInTheDocument();
		},
		RENDER_HEAVY,
	);

	// @us-03 @slice-03 @error @contract-shape:pure-function
	it.skip(
		"never says there are no items while they are still on their way",
		() => {
			openTheDialog({
				context: "featureChildren",
				items: [],
				status: "loading",
			});

			expect(within(theDialog()).getByRole("progressbar")).toBeInTheDocument();
			expect(screen.queryByText("No items to display")).toBeNull();
		},
		RENDER_HEAVY,
	);

	// @us-03 @slice-03 @error @contract-shape:pure-function
	it.skip(
		"says the items couldn't be loaded, with a warning and no retry button, when they cannot be loaded",
		() => {
			openTheDialog({ context: "featureChildren", items: [], status: "error" });

			expect(
				within(theDialog()).getByText(
					"These Work Items couldn't be loaded. Close and reopen to try again.",
				),
			).toBeInTheDocument();
			expect(
				within(theDialog()).getByTestId("WarningAmberIcon"),
			).toBeInTheDocument();
			expect(
				within(theDialog())
					.getAllByRole("button")
					.map((button) => button.getAttribute("aria-label")),
			).toEqual(["Close", "Enlarge"]);
			expect(screen.queryByText("No items to display")).toBeNull();
		},
		RENDER_HEAVY,
	);

	// @us-03 @slice-03 @error @contract-shape:pure-function
	it.skip(
		"the could-not-load message uses the instance's word for Work Items",
		() => {
			instanceWords.workItems = "Tickets";

			openTheDialog({
				context: "cumulativeStateTime",
				items: [],
				status: "error",
			});

			expect(
				within(theDialog()).getByText(
					"These Tickets couldn't be loaded. Close and reopen to try again.",
				),
			).toBeInTheDocument();
		},
		RENDER_HEAVY,
	);

	// @us-03 @slice-03 @regression @contract-shape:unbounded-preservation
	it(
		"a dialog left at its defaults with nothing to list still says so, as today",
		() => {
			openTheDialog({ items: [] });

			expect(screen.getByText("No items to display")).toBeInTheDocument();
		},
		RENDER_HEAVY,
	);
});

describe("Parent shows the parent's name, looked up only while it is shown", () => {
	const checkoutRevamp = aFeature({
		referenceId: "FTR-1",
		name: "Checkout revamp",
	});
	const searchRebuild = aFeature({
		referenceId: "FTR-2",
		name: "Search rebuild",
	});
	const childOfCheckout = aWorkItem({
		referenceId: "ST-1",
		parentWorkItemReference: "FTR-1",
		cycleTime: 9,
	});
	const childOfSearch = aWorkItem({
		referenceId: "ST-2",
		parentWorkItemReference: "FTR-2",
		cycleTime: 4,
	});

	function parentLookups() {
		const featureService = createMockFeatureService();
		const answers: ReturnType<typeof deferred<IFeature[]>>[] = [];
		featureService.getFeaturesByReferences = vi.fn(() => {
			const answer = deferred<IFeature[]>();
			answers.push(answer);
			return answer.promise;
		});
		return { featureService, answers };
	}

	function theLookup(
		answers: ReturnType<typeof deferred<IFeature[]>>[],
		index: number,
	) {
		expect(answers[index], "a lookup of the parents' names").toBeDefined();
		return answers[index];
	}

	// @us-02 @slice-02 @driving_port @contract-shape:pure-function
	it.skip(
		"shows the parent's name once it is known",
		async () => {
			const { featureService, answers } = parentLookups();
			openTheDialog({
				context: "workDistribution",
				items: [childOfCheckout],
				featureService,
			});

			await act(async () => theLookup(answers, 0).resolve([checkoutRevamp]));

			expect(cellText("parent", "ST-1")).toBe("FTR-1: Checkout revamp");
		},
		RENDER_HEAVY,
	);

	// @us-02 @slice-02 @boundary @contract-shape:pure-function
	it.skip(
		"shows the parent's reference while its name is on its way",
		() => {
			const { featureService } = parentLookups();
			openTheDialog({
				context: "workDistribution",
				items: [childOfCheckout],
				featureService,
			});

			expect(cellText("parent", "ST-1")).toBe("FTR-1");
		},
		RENDER_HEAVY,
	);

	// @us-02 @slice-02 @error @contract-shape:pure-function
	it.skip(
		"shows the parent's reference when its name cannot be looked up",
		async () => {
			const { featureService, answers } = parentLookups();
			openTheDialog({
				context: "workDistribution",
				items: [childOfCheckout],
				featureService,
			});

			await act(async () =>
				theLookup(answers, 0).reject(new Error("lookup failed")),
			);

			expect(cellText("parent", "ST-1")).toBe("FTR-1");
		},
		RENDER_HEAVY,
	);

	// The lookup is a request to the server for every parent on screen, so whether it is made is the
	// cost being guarded: these two read the request itself, and the cell it fills.

	// @us-02 @slice-02 @driving_port @contract-shape:pure-function
	it.skip(
		"asks once for the names of the shown rows' parents while Parent is shown, and shows them",
		async () => {
			const { featureService, answers } = parentLookups();
			openTheDialog({
				context: "workDistribution",
				items: [childOfSearch, childOfCheckout],
				featureService,
			});

			expect(featureService.getFeaturesByReferences).toHaveBeenCalledTimes(1);
			const [asked] = vi.mocked(featureService.getFeaturesByReferences).mock
				.calls[0] ?? [[]];
			expect([...asked].sort()).toEqual(["FTR-1", "FTR-2"]);

			await act(async () =>
				theLookup(answers, 0).resolve([searchRebuild, checkoutRevamp]),
			);

			expect(cellText("parent", "ST-1")).toBe("FTR-1: Checkout revamp");
			expect(cellText("parent", "ST-2")).toBe("FTR-2: Search rebuild");
		},
		RENDER_HEAVY,
	);

	// @us-02 @slice-02 @regression @contract-shape:unbounded-preservation
	it(
		"asks for no parent names while Parent is hidden",
		() => {
			const { featureService } = parentLookups();
			openTheDialog({
				context: "estimation",
				items: [childOfCheckout, childOfSearch],
				featureService,
			});

			expect(featureService.getFeaturesByReferences).not.toHaveBeenCalled();
		},
		RENDER_HEAVY,
	);

	// @us-02 @slice-02 @error @contract-shape:pure-function
	it.skip(
		"a late answer for the previous dialog's parents never names the next dialog's",
		async () => {
			const { featureService, answers } = parentLookups();
			const dialog = openTheDialog({
				context: "workDistribution",
				items: [childOfCheckout, childOfSearch],
				featureService,
			});

			dialog.showInstead({
				context: "workDistribution",
				items: [childOfSearch],
				featureService,
			});
			await act(async () =>
				theLookup(answers, 0).resolve([checkoutRevamp, searchRebuild]),
			);

			expect(cellText("parent", "ST-2")).toBe("FTR-2");
		},
		RENDER_HEAVY,
	);
});
