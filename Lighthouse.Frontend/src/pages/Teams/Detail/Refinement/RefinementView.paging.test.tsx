import { screen, waitFor, within } from "@testing-library/react";
import type { UserEvent } from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type {
	IRefinementRow,
	IRefinementView,
} from "../../../../models/Refinement/Refinement";
import {
	aBrowserThatVotedBefore,
	aNeedOfFiveToEight,
	aRow,
	aSizingLogService,
	defaultRefinementTerms,
	GRAVITY_TEAM_ID,
	gravitysRefinement,
	renderTheRefinementTab,
	SUNDAY_THE_FOURTH,
	THURSDAY_THE_EIGHTH,
	theButton,
	theRowOf,
} from "../../../../tests/RefinementTabTestKit";

/**
 * Team Gravity has 143 Work Items in refinement, more than one page of the list holds. Every one of
 * them can be reached and voted on, and the line marking how many are needed before the next
 * Refinement sits where it falls in the whole list: on whichever page holds that row, with nothing
 * on the other page, and "All N … are needed" only under the list's own last row.
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

const RENDER_HEAVY = 20000;

const reference = (position: number) =>
	`GR-${String(position).padStart(4, "0")}`;

/** Work Items GR-0001 … in backlog order; the first 64 are Next and the rest Analysing. */
const workItemsInRefinement = (count: number): IRefinementRow[] =>
	Array.from({ length: count }, (_, index) =>
		aRow(
			reference(index + 1),
			`Work Item ${index + 1}`,
			index < 64 ? "Next" : "Analysing",
		),
	);

const references = (from: number, to: number) =>
	Array.from({ length: to - from + 1 }, (_, offset) =>
		reference(from + offset),
	);

const gravityNeeding = (
	high: number,
	rows: IRefinementRow[],
): IRefinementView =>
	gravitysRefinement(
		{
			stagesConfigured: true,
			readySource: "Stages",
			readyCount: 2,
			nextRefinementDate: THURSDAY_THE_EIGHTH,
			daysUntilNextRefinement: 4,
			isRefinementDay: false,
			need: aNeedOfFiveToEight({ low: Math.min(5, high), high }),
		},
		rows,
	);

/** The Work Items on this page, top to bottom, each with the number in its "#" cell or "". */
const theRowsOnThisPage = () =>
	screen
		.getAllByRole("row")
		.filter((row) => within(row).queryAllByRole("link").length > 0)
		.map((row) => ({
			row,
			referenceId:
				within(row).getAllByRole("link")[0].textContent?.split(":")[0] ?? "",
			number: within(row).getAllByRole("gridcell")[0].textContent?.trim() ?? "",
		}));

const goToNextPage = async (user: UserEvent, rangeThere: string) => {
	await user.click(screen.getByRole("button", { name: "Go to next page" }));
	await screen.findByText(rangeThere);
};

const ENOUGH_FOR =
	"enough for the next Refinement (85%) · not needed before then";
const ALL_143_NEEDED =
	"All 143 Work Items in Refinement are needed before the next Refinement.";

const THE_LINE =
	/^(enough for the next Refinement \(85%\) · not needed before then|All \d+ Work Items in Refinement are needed before the next Refinement\.|The only Work Item in Refinement is needed before the next Refinement\.)$/;

interface LineOnThePage {
	side: "above" | "below";
	by: string;
	says: string;
}

const comesBefore = (earlier: Element, later: Element) =>
	(earlier.compareDocumentPosition(later) &
		Node.DOCUMENT_POSITION_FOLLOWING) !==
	0;

/**
 * The rows on this page that keep space for the line, as "reference:side". The grid keeps that space
 * as a row margin, so a margin with no line beside it is the blank gap a reader would see.
 */
const rowsKeepingSpace = () =>
	theRowsOnThisPage().flatMap(({ row, referenceId }) => [
		...(row.style.marginTop === "36px" ? [`${referenceId}:above`] : []),
		...(row.style.marginBottom === "36px" ? [`${referenceId}:below`] : []),
	]);

/**
 * Exactly the expected line on this page, beside the expected row, saying the expected words, with
 * space kept for it there and nowhere else; or, when none is expected, no line and no space at all.
 */
const expectOnThisPage = (expected: LineOnThePage | null) => {
	const lines = screen.queryAllByText(THE_LINE);
	const expectedSpace =
		expected === null ? [] : [`${expected.by}:${expected.side}`];
	expect(rowsKeepingSpace()).toEqual(expectedSpace);
	expect(lines.map((line) => line.textContent)).toEqual(
		expected === null ? [] : [expected.says],
	);
	if (expected === null) return;
	const rows = theRowsOnThisPage();
	const at = rows.findIndex((shown) => shown.referenceId === expected.by);
	const neighbour = rows[expected.side === "below" ? at + 1 : at - 1];
	const [earlier, later] =
		expected.side === "below"
			? [rows[at].row, lines[0]]
			: [lines[0], rows[at].row];
	expect(comesBefore(earlier, later)).toBe(true);
	expect(
		neighbour === undefined ||
			(expected.side === "below"
				? comesBefore(lines[0], neighbour.row)
				: comesBefore(neighbour.row, lines[0])),
	).toBe(true);
};

describe("Team Gravity's 143 Work Items in refinement can all be reached", () => {
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

	// @us-01 @slice-01 @driving_port @kpi-OUT-6202-no-hidden-rows @contract-shape:bounded-change
	it.skip(
		"Jonas pages to GR-0131, the 131st Work Item in refinement, and votes on it",
		async () => {
			const key = aBrowserThatVotedBefore("Jonas");
			const sizingLogService = aSizingLogService({
				castVote: vi.fn().mockResolvedValue(
					aRow(reference(131), "Work Item 131", "Analysing", {
						voteCount: 1,
						myVote: "Yes",
					}),
				),
			});
			const { user } = renderTheRefinementTab(
				gravitysRefinement({}, workItemsInRefinement(143)),
				sizingLogService,
			);
			await theRowOf(reference(1));

			expect(screen.getByText("1–100 of 143")).toBeInTheDocument();
			expect(theRowsOnThisPage().map((shown) => shown.referenceId)).toEqual(
				references(1, 100),
			);

			await goToNextPage(user, "101–143 of 143");
			expect(theRowsOnThisPage().map((shown) => shown.referenceId)).toEqual(
				references(101, 143),
			);

			await user.click(theButton(await theRowOf(reference(131)), "Yes"));

			await waitFor(() =>
				expect(sizingLogService.castVote).toHaveBeenCalledWith(
					GRAVITY_TEAM_ID,
					reference(131),
					{ answer: "Yes", channel: "Web", voterName: "Jonas" },
					key,
				),
			);
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01 @regression @contract-shape:unbounded-preservation
	it(
		"a Team with 38 Work Items in refinement sees all of them, with no footer",
		async () => {
			renderTheRefinementTab(gravitysRefinement({}, workItemsInRefinement(38)));
			await theRowOf(reference(1));

			expect(theRowsOnThisPage().map((shown) => shown.referenceId)).toEqual(
				references(1, 38),
			);
			expect(screen.queryByText(/^\d+–\d+ of \d+$/)).not.toBeInTheDocument();
			expect(
				screen.queryByRole("button", { name: "Go to next page" }),
			).not.toBeInTheDocument();
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01 @boundary @contract-shape:bounded-change
	// Where the line lands on each page for every placement that matters around the page edge.
	it.skip.each<[number, LineOnThePage | null, LineOnThePage | null]>([
		[0, { side: "above", by: reference(1), says: ENOUGH_FOR }, null],
		[20, { side: "below", by: reference(20), says: ENOUGH_FOR }, null],
		[100, { side: "below", by: reference(100), says: ENOUGH_FOR }, null],
		[101, null, { side: "below", by: reference(101), says: ENOUGH_FOR }],
		[120, null, { side: "below", by: reference(120), says: ENOUGH_FOR }],
		[143, null, { side: "below", by: reference(143), says: ENOUGH_FOR }],
		[150, null, { side: "below", by: reference(143), says: ALL_143_NEEDED }],
	])(
		"with %i needed, the line sits where it falls in the whole list (page 1: %o, page 2: %o)",
		async (high, onPageOne, onPageTwo) => {
			const { user } = renderTheRefinementTab(
				gravityNeeding(high, workItemsInRefinement(143)),
			);
			await theRowOf(reference(1));

			expectOnThisPage(onPageOne);

			await goToNextPage(user, "101–143 of 143");

			expectOnThisPage(onPageTwo);
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01 @error @contract-shape:bounded-change
	// The first page must not call its own 100 rows all needed while 143 are in refinement.
	it.skip(
		"with 120 needed, numbers the first page 1 to 100 and continues at 101 on the second, up to 120",
		async () => {
			const { user } = renderTheRefinementTab(
				gravityNeeding(120, workItemsInRefinement(143)),
			);
			await theRowOf(reference(1));

			expect(theRowsOnThisPage().map((shown) => shown.number)).toEqual(
				references(1, 100).map((_, index) => String(index + 1)),
			);
			expect(
				screen.getByRole("img", {
					name: "100 of 120 needed before the next Refinement",
				}),
			).toBeInTheDocument();
			expect(screen.queryAllByText(THE_LINE)).toEqual([]);

			await goToNextPage(user, "101–143 of 143");

			expect(theRowsOnThisPage().map((shown) => shown.number)).toEqual([
				...references(101, 120).map((_, index) => String(index + 101)),
				...references(121, 143).map(() => ""),
			]);
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01 @error @contract-shape:bounded-change
	it.skip(
		"with 120 needed, a State filter on the second page that leaves 64 shows them all, the line under the last",
		async () => {
			const { user } = renderTheRefinementTab(
				gravityNeeding(120, workItemsInRefinement(143)),
			);
			await theRowOf(reference(1));
			await goToNextPage(user, "101–143 of 143");

			await user.click(screen.getByLabelText("State column menu"));
			await user.click(await screen.findByRole("menuitem", { name: "Filter" }));
			await user.type(
				await screen.findByRole("textbox", { name: "Value" }),
				"Next",
			);

			await waitFor(() =>
				expect(theRowsOnThisPage().map((shown) => shown.referenceId)).toEqual(
					references(1, 64),
				),
			);
			expectOnThisPage({
				side: "below",
				by: reference(64),
				says: "All 64 Work Items in Refinement are needed before the next Refinement.",
			});
			expect(screen.queryByText(/^\d+–\d+ of \d+$/)).not.toBeInTheDocument();
		},
		RENDER_HEAVY,
	);
});
