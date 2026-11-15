import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type {
	IReportAcrossOwners,
	IReportRemovalOutcome,
} from "../../models/Reports/ReportsAcrossOwners";
import { ApiServiceContext } from "../../services/Api/ApiServiceContext";
import {
	aReportOn,
	CANCEL,
	confirmLineOf,
	GRAVITY,
	LOST_FOR_GOOD_WARNING,
	MONTH,
	NOT_CONFIRMED_REMOVED,
	neverAnswered,
	notRemoved,
	notRemovedBecause,
	OCEAN_EXPLORER,
	QUARTER,
	removeCount,
	removed,
	removedHeadline,
	ZENITH,
} from "../../tests/BulkReportsTestKit";
import { createMockApiServiceContext } from "../../tests/MockApiServiceProvider";
import { aReportService } from "../../tests/ReportsTestKit";
import RemoveSelectedDialog from "./RemoveSelectedDialog";

/**
 * The confirmation a reader gets before the picked reports are removed, and the result that follows. Rendered alone
 * with the reports it is asked about; the server is stood in for at the report service.
 */

const zenithsQuarter = aReportOn(ZENITH, QUARTER, { id: 801 });
const gravitysQuarter = aReportOn(GRAVITY, QUARTER, { id: 802 });
const oceanExplorersMonth = aReportOn(OCEAN_EXPLORER, MONTH, { id: 803 });
const theThree = [zenithsQuarter, gravitysQuarter, oceanExplorersMonth];

const withoutRebuildableThen = (report: IReportAcrossOwners) => ({
	...report,
	thenAndNow: { thenRebuildable: false },
});

const renderTheDialog = (
	reports: readonly IReportAcrossOwners[],
	removeReportsInBulk: () => Promise<IReportRemovalOutcome[]> = vi.fn(),
) => {
	const onCancel = vi.fn();
	const onDone = vi.fn();
	const removeInBulk = vi.fn(removeReportsInBulk);
	const queryClient = new QueryClient({
		defaultOptions: { queries: { retry: false } },
	});
	const services = createMockApiServiceContext({
		reportService: aReportService({ removeReportsInBulk: removeInBulk }),
	});
	render(
		<QueryClientProvider client={queryClient}>
			<ApiServiceContext.Provider value={services}>
				<RemoveSelectedDialog
					reports={reports}
					onCancel={onCancel}
					onDone={onDone}
				/>
			</ApiServiceContext.Provider>
		</QueryClientProvider>,
	);
	return { user: userEvent.setup(), onCancel, onDone, removeInBulk };
};

const theDialog = () => screen.getByRole("dialog");

const removeAllPicked = async (
	user: ReturnType<typeof userEvent.setup>,
	count: number,
) => {
	await user.click(
		within(theDialog()).getByRole("button", { name: removeCount(count) }),
	);
};

describe("Confirming the removal of picked reports", () => {
	it("lists every picked report with its owner, in the order picked", () => {
		renderTheDialog(theThree);

		expect(
			within(theDialog())
				.getAllByRole("listitem")
				.map((item) => item.textContent),
		).toEqual([
			confirmLineOf(QUARTER, ZENITH),
			confirmLineOf(QUARTER, GRAVITY),
			confirmLineOf(MONTH, OCEAN_EXPLORER),
		]);
	});

	it("warns when one of them holds a Then that can no longer be rebuilt", () => {
		renderTheDialog([
			zenithsQuarter,
			withoutRebuildableThen(gravitysQuarter),
			oceanExplorersMonth,
		]);

		expect(
			within(theDialog()).getByText(LOST_FOR_GOOD_WARNING, { exact: true }),
		).toBeInTheDocument();
	});

	it("warns in the plural when two Thens would be lost", () => {
		renderTheDialog([
			withoutRebuildableThen(zenithsQuarter),
			withoutRebuildableThen(gravitysQuarter),
			oceanExplorersMonth,
		]);

		expect(
			within(theDialog()).getByText(
				"2 of them have a Then that can no longer be rebuilt from the kept history; removing them loses them for good.",
				{ exact: true },
			),
		).toBeInTheDocument();
	});

	it("carries no warning when every Then can still be rebuilt", () => {
		renderTheDialog(theThree);

		expect(within(theDialog()).queryByRole("alert")).not.toBeInTheDocument();
		expect(
			within(theDialog()).queryByText(
				/^\d+ of them (has|have) a Then that can no longer be rebuilt from the kept history; removing (it|them) loses (it|them) for good\.$/,
			),
		).not.toBeInTheDocument();
	});

	it("carries no warning for a report without a Then & Now and offers to remove one report", () => {
		renderTheDialog([{ ...zenithsQuarter, thenAndNow: undefined }]);

		expect(within(theDialog()).queryByRole("alert")).not.toBeInTheDocument();
		expect(
			within(theDialog()).getByRole("button", { name: "Remove 1 report" }),
		).toBeInTheDocument();
	});

	it("Remove sends the picked ids in order and says how many reports were removed", async () => {
		const { user, removeInBulk } = renderTheDialog(
			theThree,
			vi.fn().mockResolvedValue([removed(801), removed(802), removed(803)]),
		);

		await removeAllPicked(user, 3);

		expect(
			await screen.findByText(removedHeadline(3), { exact: true }),
		).toBeInTheDocument();
		expect(removeInBulk).toHaveBeenCalledWith([801, 802, 803]);
	});

	it("a report somebody else removed first counts as removed", async () => {
		const { user } = renderTheDialog(
			theThree,
			vi
				.fn()
				.mockResolvedValue([
					removed(801),
					removed(802, "already-gone"),
					removed(803),
				]),
		);

		await removeAllPicked(user, 3);

		expect(
			await screen.findByText(removedHeadline(3), { exact: true }),
		).toBeInTheDocument();
	});

	it("says it cannot confirm what was removed when no answer arrives", async () => {
		const { user } = renderTheDialog(
			theThree,
			vi.fn().mockRejectedValue(new Error("the network dropped")),
		);

		await removeAllPicked(user, 3);

		expect(
			await screen.findByText(NOT_CONFIRMED_REMOVED, { exact: true }),
		).toBeInTheDocument();
	});

	it("while the removal is under way it shows progress and cannot be closed", async () => {
		const { user, onCancel, onDone } = renderTheDialog(
			theThree,
			vi.fn(() => neverAnswered()),
		);

		await removeAllPicked(user, 3);
		// The Remove button that held focus is gone by now, so Escape is pressed on the dialog itself.
		fireEvent.keyDown(await within(theDialog()).findByRole("progressbar"), {
			key: "Escape",
		});

		expect(theDialog()).toBeInTheDocument();
		expect(within(theDialog()).getByRole("progressbar")).toBeInTheDocument();
		expect(onCancel).not.toHaveBeenCalled();
		expect(onDone).not.toHaveBeenCalled();
	});

	it("the result counts only the reports removed and names only the one that was not, with its reason", async () => {
		const { user } = renderTheDialog(
			theThree,
			vi
				.fn()
				.mockResolvedValue([
					removed(801),
					notRemoved(802, "not-admin"),
					removed(803),
				]),
		);

		await removeAllPicked(user, 3);

		expect(
			await screen.findByText(removedHeadline(2), { exact: true }),
		).toBeInTheDocument();
		expect(
			within(theDialog())
				.getAllByRole("listitem")
				.map((item) => item.textContent),
		).toEqual([`${QUARTER}${notRemovedBecause.notAdmin(GRAVITY)}`]);
	});

	it("Escape at the confirmation asks to cancel and nothing else", () => {
		const { onCancel, onDone, removeInBulk } = renderTheDialog(theThree);

		fireEvent.keyDown(
			within(theDialog()).getByRole("button", { name: CANCEL }),
			{ key: "Escape" },
		);

		expect(onCancel).toHaveBeenCalledTimes(1);
		expect(onDone).not.toHaveBeenCalled();
		expect(removeInBulk).not.toHaveBeenCalled();
	});

	it("Escape on the result is the reader being done", async () => {
		const { user, onCancel, onDone } = renderTheDialog(
			theThree,
			vi.fn().mockResolvedValue([removed(801), removed(802), removed(803)]),
		);
		await removeAllPicked(user, 3);
		await screen.findByText(removedHeadline(3), { exact: true });

		fireEvent.keyDown(screen.getByRole("button", { name: "Done" }), {
			key: "Escape",
		});

		expect(onDone).toHaveBeenCalledTimes(1);
		expect(onCancel).not.toHaveBeenCalled();
	});

	it("Done on the result is the reader being done", async () => {
		const { user, onCancel, onDone } = renderTheDialog(
			theThree,
			vi.fn().mockResolvedValue([removed(801), removed(802), removed(803)]),
		);
		await removeAllPicked(user, 3);
		await screen.findByText(removedHeadline(3), { exact: true });

		await user.click(screen.getByRole("button", { name: "Done" }));

		expect(onDone).toHaveBeenCalledTimes(1);
		expect(onCancel).not.toHaveBeenCalled();
	});
});
