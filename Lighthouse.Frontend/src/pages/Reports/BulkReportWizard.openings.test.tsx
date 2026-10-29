import { onlineManager } from "@tanstack/react-query";
import { act, screen, waitFor, within } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { IReportCreationOptions } from "../../models/Reports/ReportsAcrossOwners";
import {
	anOwnerToCreateOn,
	CANCEL,
	creationOptionsFor,
	everyReport,
	fromTheTypeStepToTheOwnerStep,
	GRAVITY,
	neverAnswered,
	type Owner,
	PULSAR,
	pickOwner,
	pressCreateReports,
	RETRY,
	renderTheReportsPage,
	toTheOwnerStep,
	ZENITH,
} from "../../tests/BulkReportsTestKit";
import { deferred } from "../../tests/HeldMetricsService";
import { aReportService } from "../../tests/ReportsTestKit";

/**
 * Each press of Create reports reads what a report needs once. From then on the wizard belongs to the reader
 * until they close it: nothing read again in the background takes it, its draft or its run away.
 */

const forZenithGravityAndPulsar = creationOptionsFor(
	anOwnerToCreateOn(ZENITH),
	anOwnerToCreateOn(GRAVITY),
	anOwnerToCreateOn(PULSAR),
);

const answeringOnceThenNever = () =>
	vi
		.fn()
		.mockResolvedValueOnce(forZenithGravityAndPulsar)
		.mockImplementation(() => neverAnswered());

/** TanStack Query tells the page of a change on the next timer tick, so the page is read only after it. */
const whileThePageHears = (change: () => unknown) =>
	act(async () => {
		change();
		await new Promise((resolve) => setTimeout(resolve, 0));
	});

/**
 * A wizard button keeps its spinner for at least 300 ms after a press, and its Next is gone the moment the
 * step changes, so nothing on the page shows when that wait is over. Each test sits it out before it ends;
 * otherwise the last press comes due after the page has been taken down and fails the whole run.
 */
const theLastPressSettles = () =>
	act(() => new Promise((resolve) => setTimeout(resolve, 350)));

afterEach(theLastPressSettles);

const theWizardStillHolds = (owner: Owner) => {
	const wizard = screen.getByRole("dialog");
	expect(
		within(wizard).getByRole("checkbox", { name: owner.name }),
	).toBeChecked();
};

describe("Once open, the wizard stays until it is closed", () => {
	afterEach(() => onlineManager.setOnline(true));

	it("the network dropping and coming back keeps the wizard and the Team picked, and asks nothing again", async () => {
		const getCreationOptions = answeringOnceThenNever();
		const { user } = renderTheReportsPage({
			reportService: aReportService({
				getAllReports: vi.fn().mockResolvedValue(everyReport()),
				getCreationOptions,
			}),
		});
		await toTheOwnerStep(user);
		await pickOwner(user, ZENITH);

		await whileThePageHears(() => {
			onlineManager.setOnline(false);
			onlineManager.setOnline(true);
		});

		theWizardStillHolds(ZENITH);
		expect(getCreationOptions).toHaveBeenCalledTimes(1);
	});

	it("what a report needs being read again while it is open keeps the wizard and the Team picked", async () => {
		const getCreationOptions = answeringOnceThenNever();
		const { user, queryClient } = renderTheReportsPage({
			reportService: aReportService({
				getAllReports: vi.fn().mockResolvedValue(everyReport()),
				getCreationOptions,
			}),
		});
		await toTheOwnerStep(user);
		await pickOwner(user, ZENITH);

		// Not awaited: the second read never answers, so the wizard is looked at while it is under way.
		await whileThePageHears(() =>
			queryClient.refetchQueries({ queryKey: ["reportCreationOptions"] }),
		);

		expect(getCreationOptions).toHaveBeenCalledTimes(2);
		theWizardStillHolds(ZENITH);
	});
});

describe("Every press of Create reports is its own opening", () => {
	it("an answer to an earlier, cancelled opening never fills the wizard opened after it", async () => {
		const earlier = deferred<IReportCreationOptions>();
		const later = deferred<IReportCreationOptions>();
		const getCreationOptions = vi
			.fn()
			.mockRejectedValueOnce(new Error("the network dropped"))
			.mockReturnValueOnce(earlier.promise)
			.mockReturnValueOnce(later.promise);
		const { user } = renderTheReportsPage({
			reportService: aReportService({
				getAllReports: vi.fn().mockResolvedValue(everyReport()),
				getCreationOptions,
			}),
		});
		await pressCreateReports(user);
		const couldNotLoad = await screen.findByRole("dialog");
		await user.click(within(couldNotLoad).getByRole("button", { name: RETRY }));
		await user.click(
			within(couldNotLoad).getByRole("button", { name: CANCEL }),
		);
		await waitFor(() =>
			expect(screen.queryByRole("dialog")).not.toBeInTheDocument(),
		);

		await pressCreateReports(user);
		await whileThePageHears(() => {
			earlier.resolve(creationOptionsFor(anOwnerToCreateOn(GRAVITY)));
			later.resolve(creationOptionsFor(anOwnerToCreateOn(ZENITH)));
		});
		await fromTheTypeStepToTheOwnerStep(user);

		const wizard = screen.getByRole("dialog");
		expect(
			within(wizard).queryByRole("checkbox", { name: GRAVITY.name }),
		).not.toBeInTheDocument();
		expect(
			within(wizard).getByRole("checkbox", { name: ZENITH.name }),
		).toBeInTheDocument();
		expect(getCreationOptions).toHaveBeenCalledTimes(3);
	});
});
