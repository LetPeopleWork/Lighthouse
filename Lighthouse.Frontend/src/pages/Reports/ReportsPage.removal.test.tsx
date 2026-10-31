import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IReportService } from "../../services/Api/ReportService";
import {
	AT_MOST_50,
	aReportOn,
	CANCEL,
	confirmLineOf,
	EMPTY,
	everyReport,
	FILTER,
	GRAVITY,
	LOST_FOR_GOOD_WARNING,
	MONTH,
	moreReportsThanTheCeiling,
	NOT_CONFIRMED_REMOVED,
	neverAnswered,
	notAdminOfSome,
	notRemoved,
	notRemovedBecause,
	OCEAN_EXPLORER,
	PERMISSIONS_COULD_NOT_LOAD,
	PULSAR,
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
	VOYAGER,
	ZENITH,
} from "../../tests/BulkReportsTestKit";
import { aReportService } from "../../tests/ReportsTestKit";
import { ownerOfReport, reportQueryKeyOf } from "../Common/Reports/reportOwner";

/**
 * Remove several reports at once, from the Reports page. The reader picks rows, confirms a dialog that lists every
 * one of them and warns when one holds a Then that can no longer be rebuilt, and the server answers each report on
 * its own. Remove is offered only when the reader is Admin of every picked report's owner.
 */

const zenithsQuarter = aReportOn(ZENITH, QUARTER, { id: 801 });
const gravitysQuarter = aReportOn(GRAVITY, QUARTER, { id: 802 });
const oceanExplorersMonth = aReportOn(OCEAN_EXPLORER, MONTH, { id: 803 });
const pulsarsQuarter = aReportOn(PULSAR, QUARTER, { id: 804 });

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

	it(`Cancel removes nothing`, async () => {
		const removeReportsInBulk = vi.fn();
		const { user } = renderTheReportsPage({
			reportService: listing(theThreeOfThem(), removeReportsInBulk),
		});

		await pickTheThree(user);
		await user.click(screen.getByRole("button", { name: removeButton(3) }));
		await user.click(
			within(await theConfirmation()).getByRole("button", { name: CANCEL }),
		);

		expect(removeReportsInBulk).not.toHaveBeenCalled();
		// Portfolios are listed before Teams, so Ocean Explorer's report comes first.
		expect(theReportNamesListed()).toEqual([MONTH, QUARTER, QUARTER]);
	});

	it(`the confirmation warns when one of them holds a Then that can no longer be rebuilt`, async () => {
		const { user } = renderTheReportsPage({
			reportService: listing(
				vi
					.fn()
					.mockResolvedValue(
						everyReport(
							zenithsQuarter,
							{ ...gravitysQuarter, thenAndNow: { thenRebuildable: false } },
							oceanExplorersMonth,
						),
					),
			),
		});

		await pickTheThree(user);
		await user.click(screen.getByRole("button", { name: removeButton(3) }));

		expect(
			within(await theConfirmation()).getByText(LOST_FOR_GOOD_WARNING, {
				exact: true,
			}),
		).toBeInTheDocument();
	});

	it(`with every Then still rebuildable the confirmation carries no warning`, async () => {
		const { user } = renderTheReportsPage({
			reportService: listing(theThreeOfThem()),
		});

		await pickTheThree(user);
		await user.click(screen.getByRole("button", { name: removeButton(3) }));

		expect(
			within(await theConfirmation()).queryByText(
				/^\d+ of them (has|have) a Then that can no longer be rebuilt from the kept history; removing (it|them) loses (it|them) for good\.$/,
			),
		).not.toBeInTheDocument();
	});

	it(`with one picked report on an owner she only reads, Remove is disabled and says why`, async () => {
		const { user } = renderTheReportsPage({
			reportService: listing(
				vi
					.fn()
					.mockResolvedValue(
						everyReport(
							zenithsQuarter,
							gravitysQuarter,
							oceanExplorersMonth,
							pulsarsQuarter,
						),
					),
			),
			permissions: { adminOf: [ZENITH, GRAVITY, OCEAN_EXPLORER] },
		});

		await pickTheThree(user);
		await selectTheRowOf(user, QUARTER, PULSAR);

		const remove = screen.getByRole("button", { name: removeButton(4) });
		expect(remove).toBeDisabled();
		await user.hover(remove.parentElement ?? remove);
		expect(await screen.findByRole("tooltip")).toHaveTextContent(
			new RegExp(`^${notAdminOfSome(1, 4).replaceAll(".", "\\.")}$`),
		);
	});

	it(`with two picked reports on owners she only reads, the reason says they are`, async () => {
		const voyagersQuarter = aReportOn(VOYAGER, QUARTER, { id: 805 });
		const { user } = renderTheReportsPage({
			reportService: listing(
				vi
					.fn()
					.mockResolvedValue(
						everyReport(zenithsQuarter, pulsarsQuarter, voyagersQuarter),
					),
			),
			permissions: { adminOf: [ZENITH] },
		});
		await theReportsTable();

		await selectTheRowOf(user, QUARTER, ZENITH);
		await selectTheRowOf(user, QUARTER, PULSAR);
		await selectTheRowOf(user, QUARTER, VOYAGER);

		const remove = screen.getByRole("button", { name: removeButton(3) });
		expect(remove).toBeDisabled();
		await user.hover(remove.parentElement ?? remove);
		expect(await screen.findByRole("tooltip")).toHaveTextContent(
			new RegExp(`^${notAdminOfSome(2, 3).replaceAll(".", "\\.")}$`),
		);
	});

	it(`when her permissions could not be loaded, Remove is disabled and says to reload`, async () => {
		const { user } = renderTheReportsPage({
			reportService: listing(theThreeOfThem()),
			permissions: "failing",
		});
		await theReportsTable();

		await selectTheRowOf(user, QUARTER, ZENITH);

		const remove = screen.getByRole("button", { name: removeButton(1) });
		expect(remove).toBeDisabled();
		await user.hover(remove.parentElement ?? remove);
		expect(await screen.findByRole("tooltip")).toHaveTextContent(
			new RegExp(`^${PERMISSIONS_COULD_NOT_LOAD.replaceAll(".", "\\.")}$`),
		);
	});

	it(`when her permissions could not be loaded, pressing Remove asks nothing`, async () => {
		const removeReportsInBulk = vi.fn();
		const { user } = renderTheReportsPage({
			reportService: listing(theThreeOfThem(), removeReportsInBulk),
			permissions: "failing",
		});

		await pickTheThree(user);
		fireEvent.click(screen.getByRole("button", { name: removeButton(3) }));

		expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
		expect(removeReportsInBulk).not.toHaveBeenCalled();
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
	});

	it(`a report somebody else removed first counts as removed`, async () => {
		const { user } = renderTheReportsPage({
			reportService: listing(
				theThreeOfThem(),
				vi
					.fn()
					.mockResolvedValue([
						removed(801),
						removed(802, "already-gone"),
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
			await screen.findByText(removedHeadline(3), { exact: true }),
		).toBeInTheDocument();
	});

	it(`when no answer arrives it says it cannot confirm what was removed`, async () => {
		const { user } = renderTheReportsPage({
			reportService: listing(
				theThreeOfThem(),
				vi.fn().mockRejectedValue(new Error("the network dropped")),
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
			await screen.findByText(NOT_CONFIRMED_REMOVED, { exact: true }),
		).toBeInTheDocument();
	});

	it(`while the removal is under way the dialog cannot be closed`, async () => {
		const { user } = renderTheReportsPage({
			reportService: listing(
				theThreeOfThem(),
				vi.fn(() => neverAnswered()),
			),
		});

		await pickTheThree(user);
		await user.click(screen.getByRole("button", { name: removeButton(3) }));
		await user.click(
			within(await theConfirmation()).getByRole("button", {
				name: removeCount(3),
			}),
		);
		// The Remove button that held focus is gone by now, so Escape is pressed on the dialog itself.
		fireEvent.keyDown(
			within(await theConfirmation()).getByRole("progressbar"),
			{ key: "Escape" },
		);

		expect(screen.getByRole("dialog")).toBeInTheDocument();
		expect(
			within(screen.getByRole("dialog")).getByRole("progressbar"),
		).toBeInTheDocument();
	});

	it(`the result counts only the reports removed, and names only the one that was not`, async () => {
		const { user } = renderTheReportsPage({
			reportService: listing(
				theThreeOfThem(),
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
			await screen.findByText(removedHeadline(2), { exact: true }),
		).toBeInTheDocument();
		expect(
			within(screen.getByRole("dialog"))
				.getAllByRole("listitem")
				.map((item) => item.textContent),
		).toEqual([`${QUARTER}${notRemovedBecause.notAdmin(GRAVITY)}`]);
	});

	it(`the confirmation warns in the plural when two Thens would be lost`, async () => {
		const { user } = renderTheReportsPage({
			reportService: listing(
				vi
					.fn()
					.mockResolvedValue(
						everyReport(
							{ ...zenithsQuarter, thenAndNow: { thenRebuildable: false } },
							{ ...gravitysQuarter, thenAndNow: { thenRebuildable: false } },
							oceanExplorersMonth,
						),
					),
			),
		});

		await pickTheThree(user);
		await user.click(screen.getByRole("button", { name: removeButton(3) }));

		expect(
			within(await theConfirmation()).getByText(
				"2 of them have a Then that can no longer be rebuilt from the kept history; removing them loses them for good.",
				{ exact: true },
			),
		).toBeInTheDocument();
	});

	it(`a picked report without a Then & Now carries no warning`, async () => {
		const { user } = renderTheReportsPage({
			reportService: listing(
				vi
					.fn()
					.mockResolvedValue(
						everyReport({ ...zenithsQuarter, thenAndNow: undefined }),
					),
			),
		});
		await theReportsTable();

		await selectTheRowOf(user, QUARTER, ZENITH);
		await user.click(screen.getByRole("button", { name: removeButton(1) }));

		const confirmation = await theConfirmation();
		expect(within(confirmation).queryByRole("alert")).not.toBeInTheDocument();
		expect(
			within(confirmation).getByRole("button", { name: "Remove 1 report" }),
		).toBeInTheDocument();
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
		expect(removeReportsInBulk).not.toHaveBeenCalled();
		expect(
			screen.getByText(selectedCount(3), { exact: true }),
		).toBeInTheDocument();
	});

	it(`Escape on the result closes it and clears what was picked`, async () => {
		const { user } = renderTheReportsPage({
			reportService: listing(
				theThreeOfThem(),
				vi.fn().mockResolvedValue([removed(801), removed(802), removed(803)]),
			),
		});

		await pickTheThree(user);
		await user.click(screen.getByRole("button", { name: removeButton(3) }));
		await user.click(
			within(await theConfirmation()).getByRole("button", {
				name: removeCount(3),
			}),
		);
		await screen.findByText(removedHeadline(3), { exact: true });
		fireEvent.keyDown(screen.getByRole("button", { name: "Done" }), {
			key: "Escape",
		});

		await waitFor(() =>
			expect(screen.queryByRole("dialog")).not.toBeInTheDocument(),
		);
		expect(
			screen.getByText(selectedCount(0), { exact: true }),
		).toBeInTheDocument();
	});

	it(`each report the removal took, or found already gone, is forgotten, and one it could not remove is kept`, async () => {
		const removeReportsInBulk = vi
			.fn()
			.mockResolvedValue([
				removed(801),
				removed(802, "already-gone"),
				notRemoved(803, "not-admin"),
			]);
		const { user, queryClient } = renderTheReportsPage({
			reportService: listing(theThreeOfThem(), removeReportsInBulk),
		});
		const readBefore = [zenithsQuarter, gravitysQuarter, oceanExplorersMonth];
		const keyOf = (report: (typeof readBefore)[number]) =>
			reportQueryKeyOf(ownerOfReport(report), report.id);
		for (const report of readBefore) {
			queryClient.setQueryData(keyOf(report), report);
		}

		await pickTheThree(user);
		await user.click(screen.getByRole("button", { name: removeButton(3) }));
		await user.click(
			within(await theConfirmation()).getByRole("button", {
				name: removeCount(3),
			}),
		);
		await screen.findByText(removedHeadline(2), { exact: true });

		expect(
			readBefore.map(
				(report) => queryClient.getQueryData(keyOf(report)) !== undefined,
			),
		).toEqual([false, false, true]);
	});

	it(`the report forgotten is the one removed, not one picked before it`, async () => {
		const removeReportsInBulk = vi
			.fn()
			.mockResolvedValue([
				notRemoved(801, "not-admin"),
				removed(802),
				removed(803),
			]);
		const { user, queryClient } = renderTheReportsPage({
			reportService: listing(theThreeOfThem(), removeReportsInBulk),
		});
		const readBefore = [zenithsQuarter, gravitysQuarter, oceanExplorersMonth];
		const keyOf = (report: (typeof readBefore)[number]) =>
			reportQueryKeyOf(ownerOfReport(report), report.id);
		for (const report of readBefore) {
			queryClient.setQueryData(keyOf(report), report);
		}

		await pickTheThree(user);
		await user.click(screen.getByRole("button", { name: removeButton(3) }));
		await user.click(
			within(await theConfirmation()).getByRole("button", {
				name: removeCount(3),
			}),
		);
		await screen.findByText(removedHeadline(2), { exact: true });

		expect(
			readBefore.map(
				(report) => queryClient.getQueryData(keyOf(report)) !== undefined,
			),
		).toEqual([true, false, false]);
	});
});

// Every test here renders a page of 50 to 101 grid rows, which takes seconds in jsdom on a loaded runner.
describe("Picking reports", { timeout: 15_000 }, () => {
	it("above 50 picked, Remove is disabled and says at most 50", async () => {
		const { user } = renderTheReportsPage({
			reportService: listing(
				vi.fn().mockResolvedValue(moreReportsThanTheCeiling(ZENITH)),
			),
			permissions: { adminOf: [ZENITH] },
		});
		await theReportsTable();

		await user.click(screen.getByRole("checkbox", { name: /^Select all/ }));

		const remove = screen.getByRole("button", { name: removeButton(51) });
		expect(remove).toBeDisabled();
		await user.hover(remove.parentElement ?? remove);
		expect(await screen.findByRole("tooltip")).toHaveTextContent(
			new RegExp(`^${AT_MOST_50.replaceAll(".", "\\.")}$`),
		);
	});

	it(`exactly 50 picked may still be removed`, async () => {
		const { user } = renderTheReportsPage({
			reportService: listing(
				vi.fn().mockResolvedValue(moreReportsThanTheCeiling(ZENITH, 0)),
			),
			permissions: { adminOf: [ZENITH] },
		});
		await theReportsTable();

		await user.click(screen.getByRole("checkbox", { name: /^Select all/ }));

		expect(
			screen.getByRole("button", { name: removeButton(50) }),
		).toBeEnabled();
	});

	it(`changing the filter clears what was picked`, async () => {
		const { user } = renderTheReportsPage({
			reportService: listing(theThreeOfThem()),
		});
		await pickTheThree(user);

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
		await pickTheThree(user);

		await user.click(screen.getByRole("columnheader", { name: /^Name/ }));

		expect(
			screen.getByText(selectedCount(3), { exact: true }),
		).toBeInTheDocument();
	});

	it("Select all picks every row, on every page", async () => {
		const { user } = renderTheReportsPage({
			reportService: listing(
				vi.fn().mockResolvedValue(moreReportsThanTheCeiling(ZENITH, 51)),
			),
			permissions: { adminOf: [ZENITH] },
		});
		await theReportsTable();

		await user.click(screen.getByRole("checkbox", { name: /^Select all/ }));

		expect(
			screen.getByText(selectedCount(101), { exact: true }),
		).toBeInTheDocument();
	});
});
