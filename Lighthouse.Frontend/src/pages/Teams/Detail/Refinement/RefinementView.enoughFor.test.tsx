import { cleanup, screen, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { IRefinementView } from "../../../../models/Refinement/Refinement";
import {
	aNeedOfFiveToEight,
	defaultRefinementTerms,
	gravitysRefinement,
	gravitysSixWorkItems,
	noNeedBecause,
	renderTheRefinementTab,
	SUNDAY_THE_FOURTH,
	THURSDAY_THE_EIGHTH,
	theRowOf,
} from "../../../../tests/RefinementTabTestKit";

/**
 * The list shows which Work Items are needed before the next Refinement: a "#" column numbers the first
 * rows, as many as the high end of the range, and a line after them says what that many is enough for.
 * The rows below the line are not numbered. The numbering follows the order the rows are shown in -
 * backlog order unless somebody sorts the list another way, because a Team cannot always fix its
 * backlog order - and a sort is not kept for the next visit. When fewer Work Items are in refinement
 * than are needed, the line after the last row says all of them are needed. Without a number there is
 * no "#" column and no line.
 */

const { terms, mockUseLicenseRestrictions, reporter } = vi.hoisted(() => ({
	terms: { current: {} as Record<string, string> },
	mockUseLicenseRestrictions: vi.fn(),
	reporter: { current: vi.fn() },
}));

vi.mock("../../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => terms.current[key] ?? key,
		isLoading: false,
		error: null,
		refetchTerminology: () => {},
	}),
}));

vi.mock("../../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: mockUseLicenseRestrictions,
}));

vi.mock("../../../../services/UsageData/usageDataReporter", () => ({
	useUsageDataReporter: () => reporter.current,
}));

const ENOUGH_FOR_THE_NEXT_REFINEMENT =
	/^enough for the next Refinement \(85%\) · not needed before then$/;

const gravityNeeding = (
	high: number,
	overrides: Partial<IRefinementView> = {},
) =>
	gravitysRefinement(
		{
			stagesConfigured: true,
			readySource: "Stages",
			readyCount: 2,
			nextRefinementDate: THURSDAY_THE_EIGHTH,
			daysUntilNextRefinement: 4,
			isRefinementDay: false,
			need: aNeedOfFiveToEight({ low: Math.min(5, high), high }),
			...overrides,
		},
		gravitysSixWorkItems(),
	);

/** The Work Items in the order the list shows them, each with the number in its "#" cell or "". */
const theListAsShown = async () => {
	await theRowOf("GR-058");
	return screen
		.getAllByRole("row")
		.filter((row) => within(row).queryAllByRole("link").length > 0)
		.map((row) => ({
			row,
			referenceId:
				within(row).getAllByRole("link")[0].textContent?.split(":")[0] ?? "",
			number: within(row).getAllByRole("gridcell")[0].textContent?.trim() ?? "",
		}));
};

const comesBefore = (earlier: Element, later: Element) =>
	(earlier.compareDocumentPosition(later) &
		Node.DOCUMENT_POSITION_FOLLOWING) !==
	0;

describe("The Refinement tab marks the Work Items needed before the next Refinement", () => {
	beforeEach(() => {
		localStorage.clear();
		terms.current = { ...defaultRefinementTerms };
		reporter.current = vi.fn();
		mockUseLicenseRestrictions.mockReturnValue({
			licenseStatus: { canUsePremiumFeatures: true },
			isLoading: false,
		});
		vi.useFakeTimers({ toFake: ["Date"] });
		vi.setSystemTime(SUNDAY_THE_FOURTH);
	});

	afterEach(() => {
		vi.useRealTimers();
	});

	// @us-06 @slice-06 @driving_port @contract-shape:pure-function
	it("numbers the first rows up to the high end and draws the line after them", async () => {
		renderTheRefinementTab(gravityNeeding(3));

		const list = await theListAsShown();
		const line = screen.getByText(ENOUGH_FOR_THE_NEXT_REFINEMENT);

		expect(screen.getByRole("columnheader", { name: "#" })).toBeInTheDocument();
		expect(list.map((shown) => shown.number)).toEqual([
			"1",
			"2",
			"3",
			"",
			"",
			"",
		]);
		expect(comesBefore(list[2].row, line)).toBe(true);
		expect(comesBefore(line, list[3].row)).toBe(true);
	});

	// @us-06 @slice-06 @driving_port @contract-shape:pure-function
	// Somebody sorting by State sees the first three rows as sorted numbered, not the backlog's first three.
	it.skip("numbers the rows in the order they are shown when somebody sorts the list", async () => {
		const { user } = renderTheRefinementTab(gravityNeeding(3));
		await theListAsShown();

		await user.click(screen.getByRole("columnheader", { name: "State" }));

		const list = await theListAsShown();
		const line = screen.getByText(ENOUGH_FOR_THE_NEXT_REFINEMENT);
		expect(list.map((shown) => shown.referenceId).slice(0, 3)).toEqual([
			"GR-051",
			"GR-054",
			"GR-073",
		]);
		expect(list.map((shown) => shown.number)).toEqual([
			"1",
			"2",
			"3",
			"",
			"",
			"",
		]);
		expect(comesBefore(list[2].row, line)).toBe(true);
		expect(comesBefore(line, list[3].row)).toBe(true);
	});

	// @us-06 @slice-06 @boundary @contract-shape:unbounded-preservation
	it.skip("shows backlog order again the next time the tab opens after a sort", async () => {
		const { user } = renderTheRefinementTab(gravityNeeding(3));
		await theListAsShown();
		await user.click(screen.getByRole("columnheader", { name: "State" }));
		const sorted = await theListAsShown();
		expect(sorted.map((shown) => shown.number)).toEqual([
			"1",
			"2",
			"3",
			"",
			"",
			"",
		]);
		expect(sorted[0].referenceId).toBe("GR-051");
		cleanup();

		renderTheRefinementTab(gravityNeeding(3));

		const list = await theListAsShown();
		expect(list.map((shown) => shown.referenceId).slice(0, 3)).toEqual([
			"GR-058",
			"GR-059",
			"GR-051",
		]);
	});

	// @us-06 @slice-06 @boundary @contract-shape:pure-function
	it("says all of them are needed, after the last row, when fewer are in refinement than needed", async () => {
		renderTheRefinementTab(gravityNeeding(8));

		const list = await theListAsShown();
		const line = screen.getByText(
			/^All 6 Work Items in Refinement are needed before the next Refinement\.$/,
		);

		expect(list.map((shown) => shown.number)).toEqual([
			"1",
			"2",
			"3",
			"4",
			"5",
			"6",
		]);
		expect(comesBefore(list[5].row, line)).toBe(true);
		expect(
			screen.queryByText(ENOUGH_FOR_THE_NEXT_REFINEMENT),
		).not.toBeInTheDocument();
	});

	// @us-06 @slice-06 @boundary @contract-shape:pure-function
	it("draws the line after the last row when exactly as many are listed as needed", async () => {
		renderTheRefinementTab(gravityNeeding(6));

		const list = await theListAsShown();
		const line = screen.getByText(ENOUGH_FOR_THE_NEXT_REFINEMENT);

		expect(list.map((shown) => shown.number)).toEqual([
			"1",
			"2",
			"3",
			"4",
			"5",
			"6",
		]);
		expect(comesBefore(list[5].row, line)).toBe(true);
	});

	// @us-06 @slice-06 @boundary @contract-shape:pure-function
	it("numbers nothing and draws the line before the first row when nothing is needed", async () => {
		renderTheRefinementTab(gravityNeeding(0, { readyCount: 2 }));

		const list = await theListAsShown();
		const line = screen.getByText(ENOUGH_FOR_THE_NEXT_REFINEMENT);

		expect(list.map((shown) => shown.number)).toEqual(["", "", "", "", "", ""]);
		expect(comesBefore(line, list[0].row)).toBe(true);
	});

	// @us-06 @slice-06 @error @contract-shape:pure-function
	// The tab says why there is no number, so the missing column and line are not a tab that failed to load.
	it.skip.each([
		[
			"no cadence",
			noNeedBecause("NoCadence"),
			null,
			null,
			/^No Refinement cadence$/,
		],
		[
			"too little history",
			noNeedBecause("InsufficientData"),
			THURSDAY_THE_EIGHTH,
			4,
			/^Next Refinement: Thu 8 Oct · in 4 days$/,
		],
	])(
		"shows no # column and no line when there is no number because of %s",
		async (_why, need, nextRefinementDate, daysUntilNextRefinement, whatTheTabSays) => {
			renderTheRefinementTab(
				gravityNeeding(3, {
					need,
					nextRefinementDate,
					daysUntilNextRefinement,
				}),
			);

			await theListAsShown();
			expect(screen.getByText(whatTheTabSays)).toBeVisible();
			expect(
				screen.queryByRole("columnheader", { name: "#" }),
			).not.toBeInTheDocument();
			expect(screen.queryByText(/^enough for /)).not.toBeInTheDocument();
			expect(screen.queryByText(/ are needed before /)).not.toBeInTheDocument();
		},
	);

	// @us-06 @slice-06 @boundary @contract-shape:pure-function
	it("says the Team's own words on the line", async () => {
		terms.current = {
			...defaultRefinementTerms,
			workItems: "Tickets",
			refinement: "Grooming",
		};
		renderTheRefinementTab(gravityNeeding(8));

		expect(
			await screen.findByText(
				/^All 6 Tickets in Grooming are needed before the next Grooming\.$/,
			),
		).toBeInTheDocument();
	});
});
