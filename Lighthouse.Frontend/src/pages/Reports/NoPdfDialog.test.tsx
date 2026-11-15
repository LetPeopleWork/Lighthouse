import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { IReportAcrossOwners } from "../../models/Reports/ReportsAcrossOwners";
import {
	aReportOn,
	CANCEL,
	couldNotLoadTheReportOf,
	LEAVE_IT_OUT,
	MONTH,
	NO_PDF_WAS_MADE,
	OCEAN_EXPLORER,
	PART_OF_THE_PAGE_FAILED,
	QUARTER,
	RETRY,
	VOYAGER,
	ZENITH,
} from "../../tests/BulkReportsTestKit";
import NoPdfDialog from "./NoPdfDialog";

/**
 * The word that no PDF was made, rendered alone: which reports it names, which buttons it offers and which handler
 * each of them calls.
 */

const voyagersQuarter = aReportOn(VOYAGER, QUARTER, { id: 903 });
const oceanExplorersMonth = aReportOn(OCEAN_EXPLORER, MONTH, { id: 906 });
const zenithsQuarter = aReportOn(ZENITH, QUARTER, { id: 901 });

const handlersOf = () => ({
	onCancel: vi.fn(),
	onLeaveItOut: vi.fn(),
	onRetry: vi.fn(),
});

const renderTheDialog = (
	failedReports: readonly IReportAcrossOwners[],
	handlers: ReturnType<typeof handlersOf> = handlersOf(),
	leaveItOutOffered = true,
) => {
	render(
		<NoPdfDialog
			failedReports={failedReports}
			onLeaveItOut={leaveItOutOffered ? handlers.onLeaveItOut : undefined}
			onRetry={handlers.onRetry}
			onCancel={handlers.onCancel}
		/>,
	);
	return { user: userEvent.setup(), handlers };
};

const theDialog = () => screen.getByRole("dialog", { name: NO_PDF_WAS_MADE });

const theButtonsOffered = () =>
	within(theDialog())
		.getAllByRole("button")
		.map((button) => button.textContent);

const timesEachHandlerWasCalled = (
	handlers: ReturnType<typeof handlersOf>,
) => ({
	onCancel: handlers.onCancel.mock.calls.length,
	onLeaveItOut: handlers.onLeaveItOut.mock.calls.length,
	onRetry: handlers.onRetry.mock.calls.length,
});

describe("the word that no PDF was made", () => {
	it("names each report that could not be read, in print order, with its owner's kind", () => {
		renderTheDialog([voyagersQuarter, oceanExplorersMonth, zenithsQuarter]);

		const lines = within(theDialog())
			.getAllByRole("listitem")
			.map((line) => line.textContent);
		expect(lines).toEqual([
			couldNotLoadTheReportOf(VOYAGER, QUARTER),
			couldNotLoadTheReportOf(OCEAN_EXPLORER, MONTH),
			couldNotLoadTheReportOf(ZENITH, QUARTER),
		]);
		expect(theDialog()).toHaveAccessibleDescription(
			[
				couldNotLoadTheReportOf(VOYAGER, QUARTER),
				couldNotLoadTheReportOf(OCEAN_EXPLORER, MONTH),
				couldNotLoadTheReportOf(ZENITH, QUARTER),
			].join(" "),
		);
	});

	it("says that part of the page could not be loaded when no report is named", () => {
		renderTheDialog([], handlersOf(), false);

		expect(
			within(theDialog()).getByText(PART_OF_THE_PAGE_FAILED, { exact: true }),
		).toBeInTheDocument();
		expect(theDialog()).toHaveAccessibleDescription(PART_OF_THE_PAGE_FAILED);
		expect(within(theDialog()).queryByRole("list")).not.toBeInTheDocument();
		expect(theButtonsOffered()).toEqual([CANCEL, RETRY]);
	});

	it("offers Cancel, Leave it out and Retry when leaving the failed reports out is offered", () => {
		renderTheDialog([voyagersQuarter]);

		expect(theButtonsOffered()).toEqual([CANCEL, LEAVE_IT_OUT, RETRY]);
	});

	it("offers only Cancel and Retry when leaving the failed reports out is not offered", () => {
		renderTheDialog([voyagersQuarter], handlersOf(), false);

		expect(theButtonsOffered()).toEqual([CANCEL, RETRY]);
	});

	it("Cancel calls its own handler once and no other", async () => {
		const { user, handlers } = renderTheDialog([voyagersQuarter]);

		await user.click(
			within(theDialog()).getByRole("button", { name: /^Cancel$/ }),
		);

		expect(timesEachHandlerWasCalled(handlers)).toEqual({
			onCancel: 1,
			onLeaveItOut: 0,
			onRetry: 0,
		});
	});

	it("Leave it out calls its own handler once and no other", async () => {
		const { user, handlers } = renderTheDialog([voyagersQuarter]);

		await user.click(
			within(theDialog()).getByRole("button", { name: /^Leave it out$/ }),
		);

		expect(timesEachHandlerWasCalled(handlers)).toEqual({
			onCancel: 0,
			onLeaveItOut: 1,
			onRetry: 0,
		});
	});

	it("Retry calls its own handler once and no other", async () => {
		const { user, handlers } = renderTheDialog([voyagersQuarter]);

		await user.click(
			within(theDialog()).getByRole("button", { name: /^Retry$/ }),
		);

		expect(timesEachHandlerWasCalled(handlers)).toEqual({
			onCancel: 0,
			onLeaveItOut: 0,
			onRetry: 1,
		});
	});

	it("Escape cancels, once, and calls no other handler", async () => {
		const { user, handlers } = renderTheDialog([voyagersQuarter]);

		await user.keyboard("{Escape}");

		expect(timesEachHandlerWasCalled(handlers)).toEqual({
			onCancel: 1,
			onLeaveItOut: 0,
			onRetry: 0,
		});
	});
});
