import { screen, waitFor, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type {
	IRefinementRow,
	IRefinementView,
	RefinementVerdict,
} from "../../../../models/Refinement/Refinement";
import {
	aBrowserThatVotedBefore,
	aNeedOfFiveToEight,
	aRow,
	aSizingLogService,
	defaultRefinementTerms,
	gravitysRefinement,
	gravitysSixWorkItems,
	noNeedBecause,
	renderTheRefinementTab,
	SUNDAY_THE_FOURTH,
	THURSDAY_THE_EIGHTH,
	theButton,
	theRowOf,
} from "../../../../tests/RefinementTabTestKit";
import { INSUFFICIENT_FORECAST_DATA_MESSAGE } from "../../../../utils/forecast/insufficientForecastData";
import { refinementDayVerdict } from "./useVerdictShownReporter";

/**
 * Under the heading the tab says whether to refine more or stop, in one message that keeps its size and
 * place whatever it says. Below the range and above it are equally loud - stopping matters as much as
 * refining more - and in range reads as settled; each says so with an icon as well as a colour. The
 * words come from the server's facts: the ready count, the range, the Team and the date. With too little
 * history the message is the one forecasts already give; without a cadence there is no message, because
 * the heading's hint already says what is missing. A tab opened on a Refinement day reports which verdict
 * it showed, once.
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

const VERDICT_SHOWN = "TeamRefinementDayVerdictShown";

// The icons the message shows for "act" and for "settled"; colour alone would not reach everybody.
const ACT_ICON = "ReportProblemOutlinedIcon";
const SETTLED_ICON = "SuccessOutlinedIcon";
// Too little history is information, not a call to act.
const INFO_ICON = "InfoOutlinedIcon";

const gravityWithReady = (
	readyCount: number,
	verdict: RefinementVerdict,
	overrides: Partial<IRefinementView> = {},
) =>
	gravitysRefinement(
		{
			stagesConfigured: true,
			readySource: "Stages",
			readyCount,
			nextRefinementDate: THURSDAY_THE_EIGHTH,
			daysUntilNextRefinement: 4,
			isRefinementDay: false,
			need: aNeedOfFiveToEight({ verdict }),
			...overrides,
		},
		gravitysSixWorkItems(),
	);

const theVerdict = async () => {
	const message = await screen.findByRole("alert");
	return message;
};

describe("The Refinement tab says whether to refine more or stop", () => {
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

	// @us-05 @slice-05 @driving_port @kpi-OUT-5510-K3-in-range-on-refinement-day @contract-shape:pure-function
	it("says how many more to refine when fewer are ready than the Team is likely to pull", async () => {
		renderTheRefinementTab(gravityWithReady(3, "Below"));

		const message = await theVerdict();
		expect(message).toHaveTextContent(
			/^3 ready — below the range of 5–8 Work Items Team Gravity is likely to pull before Thu 8 Oct\. Refine 2 to 5 more\.$/,
		);
		expect(within(message).getByTestId(ACT_ICON)).toBeInTheDocument();
		expect(
			within(message).getByRole("button", {
				name: /^Based on Team Gravity's .+: a How Many forecast for the 4 working days until Thu 8 Oct\./,
			}),
		).toBeInTheDocument();
	});

	// @us-05 @slice-05 @driving_port @contract-shape:pure-function
	it("says nothing more needs refining when the ready count is in range", async () => {
		renderTheRefinementTab(gravityWithReady(6, "In"));

		const message = await theVerdict();
		expect(message).toHaveTextContent(
			/^6 ready — in the range of 5–8\. Nothing more needs refining before Thu 8 Oct\.$/,
		);
		expect(within(message).getByTestId(SETTLED_ICON)).toBeInTheDocument();
	});

	// @us-05 @slice-05 @driving_port @contract-shape:pure-function
	// Stopping is as much an action as refining more, so it is said as loudly.
	it("says stop refining, as loudly as refine more, when more are ready than the Team is likely to pull", async () => {
		renderTheRefinementTab(gravityWithReady(11, "Above"));

		const message = await theVerdict();
		expect(message).toHaveTextContent(
			/^11 ready — above the range of 5–8\. Stop refining: nothing more is needed before Thu 8 Oct\.$/,
		);
		expect(within(message).getByTestId(ACT_ICON)).toBeInTheDocument();
	});

	// @us-05 @slice-05 @boundary @contract-shape:pure-function
	it("says the range ends inclusive: five ready is in range", async () => {
		renderTheRefinementTab(gravityWithReady(5, "In"));

		expect(await theVerdict()).toHaveTextContent(
			/^5 ready — in the range of 5–8\./,
		);
	});

	// @us-05 @slice-05 @boundary @contract-shape:pure-function
	it("says the Team's own words for Work Items, the Team and Refinement", async () => {
		terms.current = {
			...defaultRefinementTerms,
			workItems: "Tickets",
			team: "Squad",
		};
		renderTheRefinementTab(gravityWithReady(3, "Below"));

		expect(await theVerdict()).toHaveTextContent(
			/below the range of 5–8 Tickets Team Gravity is likely to pull before Thu 8 Oct/,
		);
	});

	// @us-05 @slice-05 @error @contract-shape:pure-function
	it("gives the message forecasts give when the Team has too little history", async () => {
		renderTheRefinementTab(
			gravityWithReady(2, "Below", { need: noNeedBecause("InsufficientData") }),
		);

		const message = await theVerdict();
		expect(message).toHaveTextContent(INSUFFICIENT_FORECAST_DATA_MESSAGE);
		expect(within(message).getByTestId(INFO_ICON)).toBeInTheDocument();
		expect(screen.queryByText(/ ready — /)).not.toBeInTheDocument();
	});

	// @us-05 @us-04 @slice-05 @error @contract-shape:pure-function
	it("shows no message without a cadence; the heading's hint says what is missing", async () => {
		renderTheRefinementTab(
			gravityWithReady(2, "Below", {
				nextRefinementDate: null,
				daysUntilNextRefinement: null,
				need: noNeedBecause("NoCadence"),
			}),
		);

		expect(await screen.findByText(/^No Refinement cadence$/)).toBeVisible();
		expect(screen.queryByRole("alert")).not.toBeInTheDocument();
	});

	// @us-05 @us-13 @slice-05 @contract-shape:bounded-change
	// Without stage rules the votes say what is ready, so the vote that makes a Work Item Ready moves the
	// message; the tab reads the need again rather than working it out itself.
	it("moves the message on a Team without stages when a vote makes a Work Item Ready", async () => {
		aBrowserThatVotedBefore("Jonas Weber");
		const before = gravitysRefinement(
			{
				stagesConfigured: false,
				readySource: "Votes",
				readyCount: 4,
				readyByVotesCount: 4,
				nextRefinementDate: THURSDAY_THE_EIGHTH,
				daysUntilNextRefinement: 4,
				need: aNeedOfFiveToEight({ verdict: "Below" }),
			},
			[
				aRow("GR-073", "Configuration management", "Backlog", {
					voteCount: 2,
					missingVotes: 1,
				}),
			],
		);
		const after: IRefinementView = {
			...before,
			readyCount: 5,
			readyByVotesCount: 5,
			need: aNeedOfFiveToEight({ verdict: "In" }),
		};
		const sizingLogService = aSizingLogService({
			castVote: vi.fn().mockResolvedValue({
				...aRow("GR-073", "Configuration management", "Backlog", {
					voteCount: 3,
					readiness: "Ready",
					missingVotes: null,
				}),
				madeReady: true,
			} satisfies IRefinementRow & { madeReady: boolean }),
		});
		const { user, refinementService } = renderTheRefinementTab(
			before,
			sizingLogService,
		);
		vi.mocked(refinementService.getRefinement).mockResolvedValue(after);

		expect(await theVerdict()).toHaveTextContent(/^4 ready — below/);
		await user.click(theButton(await theRowOf("GR-073"), "Yes"));

		await waitFor(async () =>
			expect(await theVerdict()).toHaveTextContent(
				/^5 ready — in the range of 5–8\./,
			),
		);
	});

	// @us-05 @us-13 @slice-05 @contract-shape:bounded-change
	// A vote can take a Work Item out of Ready as well as into it, and the ready count moves either way.
	it("moves the message on a Team without stages when a vote takes a Work Item out of Ready", async () => {
		aBrowserThatVotedBefore("Jonas Weber");
		const before = gravitysRefinement(
			{
				stagesConfigured: false,
				readySource: "Votes",
				readyCount: 5,
				readyByVotesCount: 5,
				nextRefinementDate: THURSDAY_THE_EIGHTH,
				daysUntilNextRefinement: 4,
				need: aNeedOfFiveToEight({ verdict: "In" }),
			},
			[
				aRow("GR-073", "Configuration management", "Backlog", {
					voteCount: 3,
					readiness: "Ready",
					missingVotes: null,
				}),
			],
		);
		const after: IRefinementView = {
			...before,
			readyCount: 4,
			readyByVotesCount: 4,
			need: aNeedOfFiveToEight({ verdict: "Below" }),
		};
		const sizingLogService = aSizingLogService({
			castVote: vi.fn().mockResolvedValue({
				...aRow("GR-073", "Configuration management", "Backlog", {
					voteCount: 4,
					myVote: "No",
					readiness: "NeedsDiscussion",
					missingVotes: null,
				}),
				madeReady: false,
			} satisfies IRefinementRow & { madeReady: boolean }),
		});
		const { user, refinementService } = renderTheRefinementTab(
			before,
			sizingLogService,
		);
		vi.mocked(refinementService.getRefinement).mockResolvedValue(after);

		expect(await theVerdict()).toHaveTextContent(/^5 ready — in the range/);
		await user.click(theButton(await theRowOf("GR-073"), "No"));

		await waitFor(async () =>
			expect(await theVerdict()).toHaveTextContent(
				/^4 ready — below the range of 5–8/,
			),
		);
	});
});

describe("A Refinement day reports which verdict the tab showed", () => {
	beforeEach(() => {
		localStorage.clear();
		terms.current = { ...defaultRefinementTerms };
		reporter.current = vi.fn();
		mockUseLicenseRestrictions.mockReturnValue({
			licenseStatus: { canUsePremiumFeatures: true },
			isLoading: false,
		});
	});

	// @us-05 @slice-05 @kpi-OUT-5510-K3-in-range-on-refinement-day @contract-shape:bounded-change
	it.each([
		["Below", 3],
		["In", 6],
		["Above", 11],
	] as const)(
		"reports %s once when the tab is opened on a Refinement day",
		async (verdict, readyCount) => {
			renderTheRefinementTab(
				gravityWithReady(readyCount, verdict, { isRefinementDay: true }),
			);

			await theVerdict();
			await waitFor(() =>
				expect(reporter.current).toHaveBeenCalledWith({
					name: VERDICT_SHOWN,
					refinementVerdict: verdict,
				}),
			);
			expect(
				reporter.current.mock.calls.filter(
					([event]) => event.name === VERDICT_SHOWN,
				),
			).toHaveLength(1);
		},
	);

	// @us-05 @slice-05 @boundary @contract-shape:bounded-change
	it("reports None on a Refinement day when there is no number to judge against", async () => {
		renderTheRefinementTab(
			gravityWithReady(2, "Below", {
				isRefinementDay: true,
				need: noNeedBecause("InsufficientData"),
			}),
		);

		await theVerdict();
		await waitFor(() =>
			expect(reporter.current).toHaveBeenCalledWith({
				name: VERDICT_SHOWN,
				refinementVerdict: "None",
			}),
		);
	});

	// @us-05 @slice-05 @error @contract-shape:unbounded-preservation
	it("reports nothing about the verdict on any other day", async () => {
		renderTheRefinementTab(
			gravityWithReady(3, "Below", { isRefinementDay: false }),
		);

		await theVerdict();
		await theRowOf("GR-058");
		expect(
			reporter.current.mock.calls.filter(
				([event]) => event.name === VERDICT_SHOWN,
			),
		).toHaveLength(0);
	});
});

describe("Which verdict a Refinement day reports", () => {
	it.each([
		[
			"Below",
			"a Refinement day showing Below",
			true,
			aNeedOfFiveToEight({ verdict: "Below" }),
		],
		[
			"In",
			"a Refinement day showing In",
			true,
			aNeedOfFiveToEight({ verdict: "In" }),
		],
		[
			"Above",
			"a Refinement day showing Above",
			true,
			aNeedOfFiveToEight({ verdict: "Above" }),
		],
		[
			"None",
			"a Refinement day without a number",
			true,
			noNeedBecause("InsufficientData"),
		],
		[
			"None",
			"a Refinement day the server said nothing about the need on",
			true,
			undefined,
		],
		[
			undefined,
			"another day showing Below",
			false,
			aNeedOfFiveToEight({ verdict: "Below" }),
		],
		[
			undefined,
			"another day showing In",
			false,
			aNeedOfFiveToEight({ verdict: "In" }),
		],
		[
			undefined,
			"another day showing Above",
			false,
			aNeedOfFiveToEight({ verdict: "Above" }),
		],
		[
			undefined,
			"another day without a number",
			false,
			noNeedBecause("InsufficientData"),
		],
		[undefined, "a Team without a cadence", false, noNeedBecause("NoCadence")],
		[
			undefined,
			"a day the server did not say was a Refinement day",
			undefined,
			aNeedOfFiveToEight({ verdict: "In" }),
		],
	] as const)("reports %s for %s", (expected, _day, isRefinementDay, need) => {
		expect(refinementDayVerdict(isRefinementDay, need)).toBe(expected);
	});
});
