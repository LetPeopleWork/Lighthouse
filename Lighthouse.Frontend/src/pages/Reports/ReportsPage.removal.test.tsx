import { screen, waitFor, within } from "@testing-library/react";
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
	PULSAR,
	pendingEpic6350,
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
 * Remove several reports at once, from the Reports page. The reader picks rows, confirms a dialog that lists every
 * one of them and warns when one holds a Then that can no longer be rebuilt, and the server answers each report on
 * its own. Remove is offered only when the reader is Admin of every picked report's owner.
 */

const pending = pendingEpic6350("05");

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
	it(`a coach removes three trial reports on two Teams and a Portfolio, and they leave the list (${pending})`, async () => {
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

	it(`Cancel removes nothing (${pending})`, async () => {
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

	it.skip(`the confirmation warns when one of them holds a Then that can no longer be rebuilt (${pending})`, async () => {
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

	it.skip(`with every Then still rebuildable the confirmation carries no warning (${pending})`, async () => {
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

	it.skip(`with one picked report on an owner she only reads, Remove is disabled and says why (${pending})`, async () => {
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

	it.skip(`a report the server did not remove is named with its reason and stays listed (${pending})`, async () => {
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
		await waitFor(() => expect(rowOf(QUARTER, GRAVITY)).toBeInTheDocument());
	});

	it.skip(`a report somebody else removed first counts as removed (${pending})`, async () => {
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

	it.skip(`when no answer arrives it says it cannot confirm what was removed (${pending})`, async () => {
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

	it.skip(`while the removal is under way the dialog cannot be closed (${pending})`, async () => {
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
		await user.keyboard("{Escape}");

		expect(screen.getByRole("dialog")).toBeInTheDocument();
		expect(
			within(screen.getByRole("dialog")).getByRole("progressbar"),
		).toBeInTheDocument();
	});
});

describe("Picking reports", () => {
	const pendingSelection = pendingEpic6350("05");

	it.skip(`above 50 picked, Remove and Export PDF are disabled and say at most 50 (${pendingSelection})`, async () => {
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

	it.skip(`exactly 50 picked may still be removed (${pendingSelection})`, async () => {
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

	it.skip(`changing the filter clears what was picked (${pendingSelection})`, async () => {
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

	it.skip(`sorting keeps what was picked (${pendingSelection})`, async () => {
		const { user } = renderTheReportsPage({
			reportService: listing(theThreeOfThem()),
		});
		await pickTheThree(user);

		await user.click(screen.getByRole("columnheader", { name: /^Name/ }));

		expect(
			screen.getByText(selectedCount(3), { exact: true }),
		).toBeInTheDocument();
	});

	it.skip(`Select all picks every row the filter shows, on every page (${pendingSelection})`, async () => {
		const { user } = renderTheReportsPage({
			reportService: listing(
				vi.fn().mockResolvedValue(moreReportsThanTheCeiling(ZENITH, 70)),
			),
			permissions: { adminOf: [ZENITH] },
		});
		await theReportsTable();

		await user.click(screen.getByRole("checkbox", { name: /^Select all/ }));

		expect(
			screen.getByText(selectedCount(120), { exact: true }),
		).toBeInTheDocument();
	});
});
