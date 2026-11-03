import { type DataGridProps, GridFooter } from "@mui/x-data-grid";
import { screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { IRefinementRow } from "../../../../models/Refinement/Refinement";
import {
	aNeedOfFiveToEight,
	aRow,
	defaultRefinementTerms,
	gravitysRefinement,
	renderTheRefinementTab,
	SUNDAY_THE_FOURTH,
	THURSDAY_THE_EIGHTH,
	theRowOf,
} from "../../../../tests/RefinementTabTestKit";

/**
 * When more Work Items are needed before the next Refinement than the list holds, the line under the
 * list's own last row says all of them are needed, and it counts the whole list, not the page it is
 * on. The page is shortened to ten rows so a second page needs only fifteen Work Items.
 */

// The grid starts on a page of PAGE_SIZE rows by MUI's own default, and the footer decides whether to show itself
// against a constant private to its module, so the exported constant alone shortens nothing. The grid's starting
// page is set to ten rows and the footer is shown whenever there is a list.
const { PAGE } = vi.hoisted(() => ({ PAGE: 10 }));

const { terms, mockUseLicenseRestrictions, reporter } = vi.hoisted(() => ({
	terms: { current: {} as Record<string, string> },
	mockUseLicenseRestrictions: vi.fn(),
	reporter: { current: vi.fn() },
}));

vi.mock(
	"../../../../components/Common/DataGrid/RowRangeFooter",
	async (original) => ({
		...(await original<
			typeof import("../../../../components/Common/DataGrid/RowRangeFooter")
		>()),
		PAGE_SIZE: PAGE,
		RowRangeFooter: () => <GridFooter />,
	}),
);

vi.mock("@mui/x-data-grid", async (original) => {
	const mui = await original<typeof import("@mui/x-data-grid")>();
	const InitiallyOnAShortPage = (props: DataGridProps) => (
		<mui.DataGrid
			{...props}
			initialState={{
				...props.initialState,
				pagination: { paginationModel: { page: 0, pageSize: PAGE } },
			}}
		/>
	);
	return { ...mui, DataGrid: InitiallyOnAShortPage };
});

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

const reference = (position: number) =>
	`GR-${String(position).padStart(4, "0")}`;

const workItemsInRefinement = (count: number): IRefinementRow[] =>
	Array.from({ length: count }, (_, index) =>
		aRow(reference(index + 1), `Work Item ${index + 1}`, "Next"),
	);

const gravityNeeding = (high: number, rows: IRefinementRow[]) =>
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

const THE_ALL_NEEDED_LINE =
	/^All \d+ Work Items in Refinement are needed before the next Refinement\.$/;

const referencesOnThisPage = () =>
	screen.getAllByRole("row").flatMap((row) => {
		const link = row.querySelector("a[href]");
		return link === null ? [] : [link.textContent?.split(":")[0] ?? ""];
	});

describe("The line saying all are needed counts the whole list, whichever page it is on", () => {
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

	it("with 15 Work Items in refinement and 20 needed, the first page has no such line and the second says all 15 under its last row", async () => {
		const { user } = renderTheRefinementTab(
			gravityNeeding(20, workItemsInRefinement(15)),
		);
		await theRowOf(reference(1));

		expect(referencesOnThisPage()).toHaveLength(PAGE);
		expect(screen.queryByText(THE_ALL_NEEDED_LINE)).not.toBeInTheDocument();

		await user.click(screen.getByLabelText("Go to next page"));
		await screen.findByText("11–15 of 15");

		expect(referencesOnThisPage()).toEqual([
			reference(11),
			reference(12),
			reference(13),
			reference(14),
			reference(15),
		]);
		const line = screen.getByText(THE_ALL_NEEDED_LINE);
		expect(line).toHaveTextContent(
			"All 15 Work Items in Refinement are needed before the next Refinement.",
		);
		const lastRow = await theRowOf(reference(15));
		expect(
			(lastRow.compareDocumentPosition(line) &
				Node.DOCUMENT_POSITION_FOLLOWING) !==
				0,
		).toBe(true);
	});
});
