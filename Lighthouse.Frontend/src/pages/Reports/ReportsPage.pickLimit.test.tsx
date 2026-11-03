import type { DataGridProps } from "@mui/x-data-grid";
import { screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IReportService } from "../../services/Api/ReportService";
import {
	AT_MOST_50,
	exportButton,
	moreReportsThanTheCeiling,
	removeButton,
	renderTheReportsPage,
	selectedCount,
	theReportsTable,
	ZENITH,
} from "../../tests/BulkReportsTestKit";
import { aReportService } from "../../tests/ReportsTestKit";

// The grid starts on a page of PAGE_SIZE rows by MUI's own default, so the constant alone does not shrink the page.
// Both the constant and the grid's starting page are set to ten rows, which keeps these tests to a handful of
// rendered rows while 50 and 51 reports are listed.
const { PAGE } = vi.hoisted(() => ({ PAGE: 10 }));

vi.mock(
	"../../components/Common/DataGrid/RowRangeFooter",
	async (original) => ({
		...(await original<
			typeof import("../../components/Common/DataGrid/RowRangeFooter")
		>()),
		PAGE_SIZE: PAGE,
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

const listing = (
	getAllReports: IReportService["getAllReports"],
	removeReportsInBulk: IReportService["removeReportsInBulk"] = vi.fn(),
) => aReportService({ getAllReports, removeReportsInBulk });

const listed = (beyond: number) =>
	listing(vi.fn().mockResolvedValue(moreReportsThanTheCeiling(ZENITH, beyond)));

const selectAll = async (
	user: Awaited<ReturnType<typeof renderTheReportsPage>>["user"],
) => {
	await theReportsTable();
	await user.click(screen.getByRole("checkbox", { name: /^Select all/ }));
};

const tooltipSaysAtMost50 = async () =>
	expect(await screen.findByRole("tooltip")).toHaveTextContent(
		new RegExp(`^${AT_MOST_50.replaceAll(".", "\\.")}$`),
	);

describe("Picking reports", () => {
	it("Select all picks every row, on every page", async () => {
		const { user } = renderTheReportsPage({ reportService: listed(1) });

		await selectAll(user);

		expect(
			screen.getByText(selectedCount(51), { exact: true }),
		).toBeInTheDocument();
	});

	it("above 50 picked, Remove is disabled and says at most 50", async () => {
		const { user } = renderTheReportsPage({
			reportService: listed(1),
			permissions: { adminOf: [ZENITH] },
		});

		await selectAll(user);

		const remove = screen.getByRole("button", { name: removeButton(51) });
		expect(remove).toBeDisabled();
		await user.hover(remove.parentElement ?? remove);
		await tooltipSaysAtMost50();
	});

	it("exactly 50 picked may still be removed", async () => {
		const { user } = renderTheReportsPage({
			reportService: listed(0),
			permissions: { adminOf: [ZENITH] },
		});

		await selectAll(user);

		expect(
			screen.getByRole("button", { name: removeButton(50) }),
		).toBeEnabled();
	});
});

describe("One PDF of several reports", () => {
	it("above 50 picked, Export PDF is disabled and says at most 50", async () => {
		const { user } = renderTheReportsPage({ reportService: listed(1) });

		await selectAll(user);

		const exportSeveral = screen.getByRole("button", {
			name: exportButton(51),
		});
		expect(exportSeveral).toBeDisabled();
		await user.hover(exportSeveral.parentElement ?? exportSeveral);
		await tooltipSaysAtMost50();
	});

	it("exactly 50 picked may still be exported as one PDF", async () => {
		const { user } = renderTheReportsPage({ reportService: listed(0) });

		await selectAll(user);

		expect(
			screen.getByRole("button", { name: exportButton(50) }),
		).toBeEnabled();
	});
});
