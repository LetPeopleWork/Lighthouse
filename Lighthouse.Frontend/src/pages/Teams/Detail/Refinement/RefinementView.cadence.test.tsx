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
 * because the next Refinement is always after today. Without a cadence the same spot says there is no
 * cadence, and the info icon beside it says how to get one: a Team admin is pointed to Settings, anybody
 * else is told a Team admin can set it. The two sizing
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
const NO_CADENCE = /^No Refinement cadence$/;
const VOTE_CAST = "TeamSizingVoteCast";
const READINESS_REACHED = "TeamSizingReadinessReached";
const CONFIGURATION_MANAGEMENT = "GR-073";

const refiningOnThursdayTheEighth = (isRefinementDay = false) =>
	gravitysRefinement({
		nextRefinementDate: THURSDAY_THE_EIGHTH,
		daysUntilNextRefinement: 4,
		isRefinementDay,
	});

const withoutACadence = () =>
	gravitysRefinement({
		nextRefinementDate: null,
		daysUntilNextRefinement: null,
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
	it("names the day and date of the next Refinement and how many days away it is", async () => {
		renderTheRefinementTab(refiningOnThursdayTheEighth());

		expect(
			await screen.findByText(/^Next Refinement: Thu 8 Oct · in 4 days$/),
		).toBeVisible();
	});

	// @us-04 @slice-04 @boundary @contract-shape:pure-function
	it("says tomorrow when the next Refinement is one day away", async () => {
		vi.setSystemTime(new Date(2026, 9, 7, 9, 0, 0));
		renderTheRefinementTab(
			gravitysRefinement({
				nextRefinementDate: THURSDAY_THE_EIGHTH,
				daysUntilNextRefinement: 1,
			}),
		);

		expect(
			await screen.findByText(/^Next Refinement: Thu 8 Oct · tomorrow$/),
		).toBeVisible();
	});

	// @us-04 @slice-04 @boundary @contract-shape:pure-function
	// The server counts from the instance's today, which in another time zone is not the viewer's.
	it("counts the days the server counted, not the days the browser's clock would count", async () => {
		vi.setSystemTime(new Date(2026, 9, 7, 9, 0, 0));
		renderTheRefinementTab(
			gravitysRefinement({
				nextRefinementDate: THURSDAY_THE_EIGHTH,
				daysUntilNextRefinement: 2,
			}),
		);

		expect(
			await screen.findByText(/^Next Refinement: Thu 8 Oct · in 2 days$/),
		).toBeVisible();
	});

	// @us-04 @slice-04 @boundary @contract-shape:pure-function
	it("says tomorrow when the server counts one day even though the browser's clock is already on that day", async () => {
		vi.setSystemTime(new Date(2026, 9, 8, 9, 0, 0));
		renderTheRefinementTab(
			gravitysRefinement({
				nextRefinementDate: THURSDAY_THE_EIGHTH,
				daysUntilNextRefinement: 1,
			}),
		);

		expect(
			await screen.findByText(/^Next Refinement: Thu 8 Oct · tomorrow$/),
		).toBeVisible();
		expect(screen.queryByText(/in 0 days/)).not.toBeInTheDocument();
	});

	// @us-04 @slice-04 @boundary @contract-shape:pure-function
	// On a Refinement day the server already names the following one; the tab never says "today".
	it("counts a week ahead on a Refinement day and never says today", async () => {
		vi.setSystemTime(new Date(2026, 9, 8, 9, 0, 0));
		renderTheRefinementTab(
			gravitysRefinement({
				nextRefinementDate: "2026-10-15",
				daysUntilNextRefinement: 7,
				isRefinementDay: true,
			}),
		);

		expect(
			await screen.findByText(/^Next Refinement: Thu 15 Oct · in 7 days$/),
		).toBeVisible();
		expect(screen.queryByText(/today/i)).not.toBeInTheDocument();
	});

	// @us-04 @slice-04 @boundary @contract-shape:pure-function
	it("names the next Refinement on the heading's row, beside the count", async () => {
		renderTheRefinementTab(refiningOnThursdayTheEighth());

		const heading = await screen.findByRole("heading", {
			name: /^3 Work Items in Refinement/,
		});
		const nextRefinement = screen.getByText(/^Next Refinement: /);
		expect(heading.parentElement).toContainElement(nextRefinement);
	});

	// @us-04 @slice-04 @boundary @contract-shape:pure-function
	it("reads the heading first and the next Refinement after it", async () => {
		renderTheRefinementTab(refiningOnThursdayTheEighth());

		const heading = await screen.findByRole("heading", {
			name: /^3 Work Items in Refinement/,
		});
		const nextRefinement = screen.getByText(/^Next Refinement: /);
		expect(
			heading.compareDocumentPosition(nextRefinement) &
				Node.DOCUMENT_POSITION_FOLLOWING,
		).toBeTruthy();
	});

	// @us-04 @slice-04 @error @contract-shape:pure-function
	it("says a Team without a cadence has none, points a Team admin to Settings in the tooltip, and still lists the Work Items", async () => {
		const { user } = renderTheRefinementTab(
			withoutACadence(),
			aSizingLogService(),
			"TeamAdmin",
		);

		expect(await screen.findByText(NO_CADENCE)).toBeVisible();
		expect(screen.queryByText(EDITOR_HINT)).not.toBeInTheDocument();
		await user.hover(screen.getByRole("button", { name: EDITOR_HINT }));
		expect(await screen.findByRole("tooltip")).toHaveTextContent(EDITOR_HINT);
		expect(await theRowOf("GR-058")).toBeVisible();
		expect(screen.queryByText(/^Next Refinement: /)).not.toBeInTheDocument();
	});

	// @us-04 @slice-04 @error @contract-shape:pure-function
	it("tells somebody who cannot change the settings that a Team admin can set a cadence", async () => {
		const { user } = renderTheRefinementTab(
			withoutACadence(),
			aSizingLogService(),
			"Reader",
		);

		expect(await screen.findByText(NO_CADENCE)).toBeVisible();
		await user.hover(screen.getByRole("button", { name: READER_HINT }));
		expect(await screen.findByRole("tooltip")).toHaveTextContent(READER_HINT);
		expect(
			screen.queryByRole("button", { name: EDITOR_HINT }),
		).not.toBeInTheDocument();
	});

	// @us-04 @slice-04 @error @contract-shape:pure-function
	it("opens the hint when the icon beside No cadence is reached with the keyboard", async () => {
		const { user } = renderTheRefinementTab(
			withoutACadence(),
			aSizingLogService(),
			"Reader",
		);
		await screen.findByText(NO_CADENCE);
		const icon = screen.getByRole("button", { name: READER_HINT });

		for (
			let presses = 0;
			presses < 20 && icon !== document.activeElement;
			presses++
		) {
			await user.tab();
		}

		expect(icon).toHaveFocus();
		expect(await screen.findByRole("tooltip")).toHaveTextContent(READER_HINT);
	});

	// @us-04 @slice-04 @boundary @contract-shape:pure-function
	it("says the Team's own words in the next Refinement and in the hint", async () => {
		terms.current = {
			...defaultRefinementTerms,
			workItems: "Tickets",
			refinement: "Grooming",
			team: "Squad",
		};
		renderTheRefinementTab(withoutACadence(), aSizingLogService(), "Reader");

		expect(await screen.findByText(/^No Grooming cadence$/)).toBeVisible();
		expect(
			screen.getByRole("button", {
				name: /^A Squad admin can set a Grooming cadence to see how many Tickets are needed$/,
			}),
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
	it.each([
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
