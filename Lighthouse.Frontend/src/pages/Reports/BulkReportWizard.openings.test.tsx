import { onlineManager } from "@tanstack/react-query";
import { act, screen, within } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import {
	anOwnerToCreateOn,
	creationOptionsFor,
	everyReport,
	GRAVITY,
	neverAnswered,
	type Owner,
	PULSAR,
	pickOwner,
	renderTheReportsPage,
	toTheOwnerStep,
	ZENITH,
} from "../../tests/BulkReportsTestKit";
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
