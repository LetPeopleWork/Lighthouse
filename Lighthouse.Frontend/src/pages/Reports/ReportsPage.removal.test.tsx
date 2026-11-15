import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IReportService } from "../../services/Api/ReportService";
import {
	aReportOn,
	CANCEL,
	confirmLineOf,
	EMPTY,
	everyReport,
	FILTER,
	GRAVITY,
	MONTH,
	notRemoved,
	notRemovedBecause,
	OCEAN_EXPLORER,
	PERMISSIONS_COULD_NOT_LOAD,
	QUARTER,
	removeButton,
	removeCount,
	removed,
	removedHeadline,
	renderTheReportsPage,
	rowOf,
	selectedCount,
	selectTheRowOf,
	theReportNamesListed,
	theReportsTable,
	ZENITH,
} from "../../tests/BulkReportsTestKit";
import { aReportService } from "../../tests/ReportsTestKit";

/**
 * Remove several reports at once, from the Reports page. Each rule of the confirmation, the removal hooks and the
 * Remove button is proven on its own unit; these tests keep one page render per wiring.
 */

const zenithsQuarter = aReportOn(ZENITH, QUARTER, { id: 801 });
const gravitysQuarter = aReportOn(GRAVITY, QUARTER, { id: 802 });
const oceanExplorersMonth = aReportOn(OCEAN_EXPLORER, MONTH, { id: 803 });

const listing = (
	getAllReports: IReportService["getAllReports"],
	removeReportsInBulk: IReportService["removeReportsInBulk"] = vi.fn(),
) => aReportService({ getAllReports, removeReportsInBulk });

const theThreeOfThem = () =>
	vi
		.fn()
		.mockResolvedValue(
			everyReport(zenithsQuarter, gravitysQuarter, oceanExplorersMonth),
		);

const pickTheThree = async (
	user: Awaited<ReturnType<typeof renderTheReportsPage>>["user"],
) => {
	await theReportsTable();
	await selectTheRowOf(user, QUARTER, ZENITH);
	await selectTheRowOf(user, QUARTER, GRAVITY);
	await selectTheRowOf(user, MONTH, OCEAN_EXPLORER);
};

const pickTwo = async (
	user: Awaited<ReturnType<typeof renderTheReportsPage>>["user"],
) => {
	await theReportsTable();
	await selectTheRowOf(user, QUARTER, ZENITH);
	await selectTheRowOf(user, QUARTER, GRAVITY);
};

const theConfirmation = () => screen.findByRole("dialog");

describe("Remove several reports at once", () => {
	it(`a coach removes three trial reports on two Teams and a Portfolio, and they leave the list`, async () => {
		const getAllReports = theThreeOfThem()
			.mockResolvedValueOnce(
				everyReport(zenithsQuarter, gravitysQuarter, oceanExplorersMonth),
			)
			.mockResolvedValue(everyReport());
		const removeReportsInBulk = vi
			.fn()
			.mockResolvedValue([removed(801), removed(802), removed(803)]);
		const { user } = renderTheReportsPage({
			reportService: listing(getAllReports, removeReportsInBulk),
		});

		await pickTheThree(user);
		await user.click(screen.getByRole("button", { name: removeButton(3) }));
		const confirmation = await theConfirmation();
		expect(
			within(confirmation)
				.getAllByRole("listitem")
				.map((item) => item.textContent),
		).toEqual([
			confirmLineOf(QUARTER, ZENITH),
			confirmLineOf(QUARTER, GRAVITY),
			confirmLineOf(MONTH, OCEAN_EXPLORER),
		]);
		await user.click(
			within(confirmation).getByRole("button", { name: removeCount(3) }),
		);

		expect(
			await screen.findByText(removedHeadline(3), { exact: true }),
		).toBeInTheDocument();
		expect(removeReportsInBulk).toHaveBeenCalledWith([801, 802, 803]);
		await user.click(screen.getByRole("button", { name: "Done" }));
		expect(await screen.findByText(EMPTY, { exact: true })).toBeInTheDocument();
	});

	it(`Escape at the confirmation closes it, removes nothing and keeps what was picked`, async () => {
		const removeReportsInBulk = vi.fn();
		const { user } = renderTheReportsPage({
			reportService: listing(theThreeOfThem(), removeReportsInBulk),
		});

		await pickTheThree(user);
		await user.click(screen.getByRole("button", { name: removeButton(3) }));
		fireEvent.keyDown(
			within(await theConfirmation()).getByRole("button", { name: CANCEL }),
			{ key: "Escape" },
		);
		await waitFor(() =>
			expect(screen.queryByRole("dialog")).not.toBeInTheDocument(),
		);
		await user.click(screen.getByRole("button", { name: removeButton(3) }));
		await user.click(
			within(await theConfirmation()).getByRole("button", { name: CANCEL }),
		);

		await waitFor(() =>
			expect(screen.queryByRole("dialog")).not.toBeInTheDocument(),
		);
		expect(removeReportsInBulk).not.toHaveBeenCalled();
		// Portfolios are listed before Teams, so Ocean Explorer's report comes first.
		expect(theReportNamesListed()).toEqual([MONTH, QUARTER, QUARTER]);
		expect(
			screen.getByText(selectedCount(3), { exact: true }),
		).toBeInTheDocument();
	});

	it(`a report the server did not remove is named with its reason and stays listed`, async () => {
		const getAllReports = vi
			.fn()
			.mockResolvedValueOnce(
				everyReport(zenithsQuarter, gravitysQuarter, oceanExplorersMonth),
			)
			.mockResolvedValue(everyReport(gravitysQuarter));
		const { user } = renderTheReportsPage({
			reportService: listing(
				getAllReports,
				vi
					.fn()
					.mockResolvedValue([
						removed(801),
						notRemoved(802, "not-admin"),
						removed(803),
					]),
			),
		});

		await pickTheThree(user);
		await user.click(screen.getByRole("button", { name: removeButton(3) }));
		await user.click(
			within(await theConfirmation()).getByRole("button", {
				name: removeCount(3),
			}),
		);

		expect(
			await screen.findByText(notRemovedBecause.notAdmin(GRAVITY), {
				exact: true,
			}),
		).toBeInTheDocument();
		await user.click(screen.getByRole("button", { name: "Done" }));
		await waitFor(() => expect(rowOf(QUARTER, GRAVITY)).toBeInTheDocument());
		expect(
			screen.getByText(selectedCount(0), { exact: true }),
		).toBeInTheDocument();
	});

	it(`when her permissions could not be loaded, Remove is disabled and says to reload`, async () => {
		const removeReportsInBulk = vi.fn();
		const { user } = renderTheReportsPage({
			reportService: listing(theThreeOfThem(), removeReportsInBulk),
			permissions: "failing",
		});

		await pickTheThree(user);

		const remove = screen.getByRole("button", { name: removeButton(3) });
		expect(remove).toBeDisabled();
		await user.hover(remove.parentElement ?? remove);
		expect(await screen.findByRole("tooltip")).toHaveTextContent(
			new RegExp(`^${PERMISSIONS_COULD_NOT_LOAD.replaceAll(".", "\\.")}$`),
		);
		fireEvent.click(remove);
		expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
		expect(removeReportsInBulk).not.toHaveBeenCalled();
	});
});

describe("Picking reports", () => {
	it(`changing the filter clears what was picked`, async () => {
		const { user } = renderTheReportsPage({
			reportService: listing(theThreeOfThem()),
		});
		await pickTwo(user);

		await user.type(screen.getByRole("textbox", { name: FILTER }), "Quarter");

		expect(
			screen.getByText(selectedCount(0), { exact: true }),
		).toBeInTheDocument();
		expect(
			screen.getByRole("button", { name: removeButton(0) }),
		).toBeDisabled();
	});

	it(`sorting keeps what was picked`, async () => {
		const { user } = renderTheReportsPage({
			reportService: listing(theThreeOfThem()),
		});
		await pickTwo(user);

		await user.click(screen.getByRole("columnheader", { name: /^Name/ }));

		expect(
			screen.getByText(selectedCount(2), { exact: true }),
		).toBeInTheDocument();
	});
});
