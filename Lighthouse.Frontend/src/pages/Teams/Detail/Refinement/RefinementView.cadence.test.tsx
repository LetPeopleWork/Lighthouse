import { screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { IRefinementRow } from "../../../../models/Refinement/Refinement";
import {
	aBrowserThatVotedBefore,
	aRow,
	aSizingLogService,
	defaultRefinementTerms,
	gravitysRefinement,
	noNeedBecause,
	renderTheRefinementTab,
	SUNDAY_THE_FOURTH,
	THURSDAY_THE_EIGHTH,
	theButton,
	theRowOf,
} from "../../../../tests/RefinementTabTestKit";

/**
 * The Refinement tab names the Team's next Refinement on the heading's row, to the right of the count:
 * the day and date, and how far off it is in calendar days - "tomorrow" for one day, never "today",
 * because the next Refinement is always after today. Without a cadence the same spot says how to get
 * one: a Team admin is pointed to Settings, anybody else is told a Team admin can set it. The two sizing
 * events say whether they happened on a Refinement day or another day once the Team has a cadence.
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

const EDITOR_HINT =
	/^Set a Refinement cadence in Settings to see how many Work Items are needed$/;
const READER_HINT =
	/^A Team admin can set a Refinement cadence to see how many Work Items are needed$/;
const VOTE_CAST = "TeamSizingVoteCast";
const READINESS_REACHED = "TeamSizingReadinessReached";
const CONFIGURATION_MANAGEMENT = "GR-073";

const refiningOnThursdayTheEighth = (isRefinementDay = false) =>
	gravitysRefinement({
		nextRefinementDate: THURSDAY_THE_EIGHTH,
		isRefinementDay,
	});

const withoutACadence = () =>
	gravitysRefinement({
		nextRefinementDate: null,
		isRefinementDay: false,
		need: noNeedBecause("NoCadence"),
	});

const configurationManagement = (votes: Partial<IRefinementRow>) =>
	aRow(CONFIGURATION_MANAGEMENT, "Configuration management", "Backlog", votes);

const aYesThatMakesConfigurationManagementReady = async (
	nextRefinementDate: string | null,
	isRefinementDay: boolean,
) => {
	aBrowserThatVotedBefore("Jonas Weber");
	const sizingLogService = aSizingLogService({
		castVote: vi.fn().mockResolvedValue(
			configurationManagement({
				voteCount: 3,
				readiness: "Ready",
				missingVotes: null,
				madeReady: true,
			} as Partial<IRefinementRow>),
		),
	});
	const { user } = renderTheRefinementTab(
		gravitysRefinement({ nextRefinementDate, isRefinementDay }, [
			configurationManagement({ voteCount: 2, missingVotes: 1 }),
		]),
		sizingLogService,
	);

	await user.click(theButton(await theRowOf(CONFIGURATION_MANAGEMENT), "Yes"));
	await waitFor(() =>
		expect(sizingLogService.castVote).toHaveBeenCalledTimes(1),
	);
};

describe("The Refinement tab names the next Refinement", () => {
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

	// @us-04 @slice-04 @driving_port @contract-shape:pure-function
	it.skip("names the day and date of the next Refinement and how many days away it is", async () => {
		renderTheRefinementTab(refiningOnThursdayTheEighth());

		expect(
			await screen.findByText(/^Next Refinement: Thu 8 Oct · in 4 days$/),
		).toBeVisible();
	});

	// @us-04 @slice-04 @boundary @contract-shape:pure-function
	it.skip("says tomorrow when the next Refinement is one day away", async () => {
		vi.setSystemTime(new Date(2026, 9, 7, 9, 0, 0));
		renderTheRefinementTab(refiningOnThursdayTheEighth());

		expect(
			await screen.findByText(/^Next Refinement: Thu 8 Oct · tomorrow$/),
		).toBeVisible();
	});

	// @us-04 @slice-04 @boundary @contract-shape:pure-function
	// On a Refinement day the server already names the following one; the tab never says "today".
	it.skip("counts a week ahead on a Refinement day and never says today", async () => {
		vi.setSystemTime(new Date(2026, 9, 8, 9, 0, 0));
		renderTheRefinementTab(
			gravitysRefinement({
				nextRefinementDate: "2026-10-15",
				isRefinementDay: true,
			}),
		);

		expect(
			await screen.findByText(/^Next Refinement: Thu 15 Oct · in 7 days$/),
		).toBeVisible();
		expect(screen.queryByText(/today/i)).not.toBeInTheDocument();
	});

	// @us-04 @slice-04 @boundary @contract-shape:pure-function
	it.skip("names the next Refinement on the heading's row, beside the count", async () => {
		renderTheRefinementTab(refiningOnThursdayTheEighth());

		const heading = await screen.findByRole("heading", {
			name: /^3 Work Items in Refinement/,
		});
		const nextRefinement = screen.getByText(/^Next Refinement: /);
		expect(heading.parentElement).toContainElement(nextRefinement);
	});

	// @us-04 @slice-04 @error @contract-shape:pure-function
	it.skip("points a Team admin to Settings when the Team has no cadence, and still lists the Work Items", async () => {
		renderTheRefinementTab(withoutACadence(), aSizingLogService(), "TeamAdmin");

		expect(await screen.findByText(EDITOR_HINT)).toBeVisible();
		expect(await theRowOf("GR-058")).toBeVisible();
		expect(screen.queryByText(/^Next Refinement: /)).not.toBeInTheDocument();
	});

	// @us-04 @slice-04 @error @contract-shape:pure-function
	it.skip("tells somebody who cannot change the settings that a Team admin can set a cadence", async () => {
		renderTheRefinementTab(withoutACadence(), aSizingLogService(), "Reader");

		expect(await screen.findByText(READER_HINT)).toBeVisible();
		expect(screen.queryByText(EDITOR_HINT)).not.toBeInTheDocument();
	});

	// @us-04 @slice-04 @boundary @contract-shape:pure-function
	it.skip("says the Team's own words in the next Refinement and in the hint", async () => {
		terms.current = {
			...defaultRefinementTerms,
			workItems: "Tickets",
			refinement: "Grooming",
			team: "Squad",
		};
		renderTheRefinementTab(withoutACadence(), aSizingLogService(), "Reader");

		expect(
			await screen.findByText(
				/^A Squad admin can set a Grooming cadence to see how many Tickets are needed$/,
			),
		).toBeVisible();
	});
});

describe("The sizing events say whether they happened on a Refinement day", () => {
	beforeEach(() => {
		localStorage.clear();
		terms.current = { ...defaultRefinementTerms };
		reporter.current = vi.fn();
		mockUseLicenseRestrictions.mockReturnValue({
			licenseStatus: { canUsePremiumFeatures: true },
			isLoading: false,
		});
	});

	// @us-04 @us-11 @us-13 @slice-04 @kpi-OUT-5510-K4-votes-outside-the-meeting @contract-shape:bounded-change
	it.skip.each([
		[THURSDAY_THE_EIGHTH, true, "OnRefinementDay"],
		[THURSDAY_THE_EIGHTH, false, "OnOtherDay"],
		[null, false, "NoCadence"],
	])(
		"reports a vote that made a Work Item Ready with the next Refinement on %s and a Refinement day %s as %s",
		async (nextRefinementDate, isRefinementDay, sizingMoment) => {
			await aYesThatMakesConfigurationManagementReady(
				nextRefinementDate,
				isRefinementDay,
			);

			await waitFor(() =>
				expect(reporter.current).toHaveBeenCalledWith({
					name: VOTE_CAST,
					sizingMoment,
				}),
			);
			expect(reporter.current).toHaveBeenCalledWith({
				name: READINESS_REACHED,
				sizingMoment,
			});
		},
	);
});
