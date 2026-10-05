import { act, screen, waitFor, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type {
	IRefinementRow,
	IRefinementView,
	IVotedRow,
	RefinementVerdict,
} from "../../../../models/Refinement/Refinement";
import { Team } from "../../../../models/Team/Team";
import type { IRefinementService } from "../../../../services/Api/RefinementService";
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

const CONFIGURATION_MANAGEMENT = "GR-073";
const LOAD_TESTING = "GR-074";

// Without stage rules the votes say what is ready.
const aVotesTeamShowing = (
	readyCount: number,
	verdict: RefinementVerdict,
	rows: IRefinementRow[],
): IRefinementView =>
	gravitysRefinement(
		{
			stagesConfigured: false,
			readySource: "Votes",
			readyCount,
			readyByVotesCount: readyCount,
			nextRefinementDate: THURSDAY_THE_EIGHTH,
			daysUntilNextRefinement: 4,
			need: aNeedOfFiveToEight({ verdict }),
		},
		rows,
	);

const aVotedRow = (
	referenceId: string,
	name: string,
	answer: Partial<IVotedRow>,
): IVotedRow => ({
	...aRow(referenceId, name, "Backlog", answer),
	madeReady: answer.madeReady ?? false,
});

const teamNebula = () => {
	const team = new Team();
	team.id = 8;
	team.name = "Team Nebula";
	return team;
};

/** Which verdicts the tab reported, in order. */
const verdictsReported = () =>
	reporter.current.mock.calls
		.filter(([event]) => event.name === VERDICT_SHOWN)
		.map(([event]) => event.refinementVerdict);

/** The next read of the Refinement, held back until the test lets the server answer it. */
const aReadStillOnItsWay = (
	refinementService: Pick<IRefinementService, "getRefinement">,
) => {
	let answer: (view: IRefinementView) => void = () => {};
	vi.mocked(refinementService.getRefinement).mockReturnValueOnce(
		new Promise<IRefinementView>((resolve) => {
			answer = resolve;
		}),
	);
	return {
		answer: (view: IRefinementView) =>
			act(() => {
				answer(view);
			}),
	};
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
	it("says five ready against 5–8 in the in-range words", async () => {
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

	// @us-05 @us-13 @slice-05 @contract-shape:unbounded-preservation
	// With stage rules the stage says what is ready, so no vote can move the message and none reads again.
	it("reads nothing again on a Team with stages when a vote makes a Work Item Ready", async () => {
		aBrowserThatVotedBefore("Jonas Weber");
		const sizingLogService = aSizingLogService({
			castVote: vi.fn().mockResolvedValue(
				aVotedRow(CONFIGURATION_MANAGEMENT, "Configuration management", {
					voteCount: 3,
					readiness: "Ready",
					missingVotes: null,
					madeReady: true,
				}),
			),
		});
		const { user, refinementService } = renderTheRefinementTab(
			gravityWithReady(3, "Below"),
			sizingLogService,
		);

		await theVerdict();
		await user.click(
			theButton(await theRowOf(CONFIGURATION_MANAGEMENT), "Yes"),
		);
		await waitFor(async () =>
			expect(await theRowOf(CONFIGURATION_MANAGEMENT)).toHaveTextContent(
				"3 votes",
			),
		);

		expect(refinementService.getRefinement).toHaveBeenCalledTimes(1);
	});

	// @us-05 @us-13 @slice-05 @contract-shape:bounded-change
	// The server read the Refinement before the second vote reached it, so its answer still holds that
	// Work Item's old votes; the vote answered since then is the newer word on it.
	it("keeps a vote answered while the Refinement is read again", async () => {
		aBrowserThatVotedBefore("Jonas Weber");
		const before = aVotesTeamShowing(4, "Below", [
			aRow(CONFIGURATION_MANAGEMENT, "Configuration management", "Backlog", {
				voteCount: 2,
				missingVotes: 1,
			}),
			aRow(LOAD_TESTING, "Load testing framework", "Backlog"),
		]);
		const sizingLogService = aSizingLogService({
			castVote: vi.fn((_teamId: number, referenceId: string) =>
				Promise.resolve(
					referenceId === CONFIGURATION_MANAGEMENT
						? aVotedRow(CONFIGURATION_MANAGEMENT, "Configuration management", {
								voteCount: 3,
								readiness: "Ready",
								missingVotes: null,
								madeReady: true,
							})
						: aVotedRow(LOAD_TESTING, "Load testing framework", {
								voteCount: 1,
								missingVotes: 2,
								madeReady: false,
							}),
				),
			),
		});
		const { user, refinementService } = renderTheRefinementTab(
			before,
			sizingLogService,
		);
		const reread = aReadStillOnItsWay(refinementService);

		expect(await theVerdict()).toHaveTextContent(/^4 ready — below/);
		await user.click(
			theButton(await theRowOf(CONFIGURATION_MANAGEMENT), "Yes"),
		);
		await waitFor(() =>
			expect(refinementService.getRefinement).toHaveBeenCalledTimes(2),
		);
		await user.click(theButton(await theRowOf(LOAD_TESTING), "Yes"));
		await waitFor(async () =>
			expect(await theRowOf(LOAD_TESTING)).toHaveTextContent("1 vote"),
		);

		await reread.answer(
			aVotesTeamShowing(5, "In", [
				aRow(CONFIGURATION_MANAGEMENT, "Configuration management", "Backlog", {
					voteCount: 3,
					readiness: "Ready",
					missingVotes: null,
				}),
				aRow(LOAD_TESTING, "Load testing framework", "Backlog"),
			]),
		);

		await waitFor(async () =>
			expect(await theVerdict()).toHaveTextContent(/^5 ready — in the range/),
		);
		expect(await theRowOf(LOAD_TESTING)).toHaveTextContent("1 vote");
	});

	// @us-05 @us-13 @slice-05 @contract-shape:bounded-change
	it("keeps the newer read when an older one answers after it", async () => {
		aBrowserThatVotedBefore("Jonas Weber");
		const nearlyReady = { voteCount: 2, missingVotes: 1 };
		const madeReady = {
			voteCount: 3,
			readiness: "Ready",
			missingVotes: null,
		} as const;
		const before = aVotesTeamShowing(4, "Below", [
			aRow(
				CONFIGURATION_MANAGEMENT,
				"Configuration management",
				"Backlog",
				nearlyReady,
			),
			aRow(LOAD_TESTING, "Load testing framework", "Backlog", nearlyReady),
		]);
		const sizingLogService = aSizingLogService({
			castVote: vi.fn((_teamId: number, referenceId: string) =>
				Promise.resolve(
					aVotedRow(referenceId, "Some Work Item", {
						...madeReady,
						madeReady: true,
					}),
				),
			),
		});
		const { user, refinementService } = renderTheRefinementTab(
			before,
			sizingLogService,
		);
		const olderRead = aReadStillOnItsWay(refinementService);
		const newerRead = aReadStillOnItsWay(refinementService);

		expect(await theVerdict()).toHaveTextContent(/^4 ready — below/);
		await user.click(
			theButton(await theRowOf(CONFIGURATION_MANAGEMENT), "Yes"),
		);
		await waitFor(() =>
			expect(refinementService.getRefinement).toHaveBeenCalledTimes(2),
		);
		await user.click(theButton(await theRowOf(LOAD_TESTING), "Yes"));
		await waitFor(() =>
			expect(refinementService.getRefinement).toHaveBeenCalledTimes(3),
		);

		await newerRead.answer(
			aVotesTeamShowing(6, "In", [
				aRow(
					CONFIGURATION_MANAGEMENT,
					"Configuration management",
					"Backlog",
					madeReady,
				),
				aRow(LOAD_TESTING, "Load testing framework", "Backlog", madeReady),
			]),
		);
		await waitFor(async () =>
			expect(await theVerdict()).toHaveTextContent(/^6 ready — in the range/),
		);
		await olderRead.answer(
			aVotesTeamShowing(5, "In", [
				aRow(
					CONFIGURATION_MANAGEMENT,
					"Configuration management",
					"Backlog",
					madeReady,
				),
				aRow(LOAD_TESTING, "Load testing framework", "Backlog", nearlyReady),
			]),
		);
		await act(async () => {
			await Promise.resolve();
		});

		expect(await theVerdict()).toHaveTextContent(/^6 ready — in the range/);
	});
});

describe("A Refinement day reports which verdict the tab showed", () => {
	beforeEach(() => {
		localStorage.clear();
		terms.current = { ...defaultRefinementTerms };
		// A browser that agreed, so every report goes.
		reporter.current = vi.fn(() => true);
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

	// @us-05 @slice-05 @boundary @contract-shape:bounded-change
	// Usage data counts openings, not reads: the vote that moves the message reads the Refinement again,
	// and that read reports nothing more.
	it("reports once when a vote on a Refinement day reads the Refinement again", async () => {
		aBrowserThatVotedBefore("Jonas Weber");
		const nearlyReady = aRow(
			CONFIGURATION_MANAGEMENT,
			"Configuration management",
			"Backlog",
			{ voteCount: 2, missingVotes: 1 },
		);
		const madeReady = {
			voteCount: 3,
			readiness: "Ready",
			missingVotes: null,
		} as const;
		const sizingLogService = aSizingLogService({
			castVote: vi.fn().mockResolvedValue(
				aVotedRow(CONFIGURATION_MANAGEMENT, "Configuration management", {
					...madeReady,
					madeReady: true,
				}),
			),
		});
		const { user, refinementService } = renderTheRefinementTab(
			{
				...aVotesTeamShowing(4, "Below", [nearlyReady]),
				isRefinementDay: true,
			},
			sizingLogService,
		);
		vi.mocked(refinementService.getRefinement).mockResolvedValue({
			...aVotesTeamShowing(5, "In", [{ ...nearlyReady, ...madeReady }]),
			isRefinementDay: true,
		});

		expect(await theVerdict()).toHaveTextContent(/^4 ready — below/);
		await user.click(
			theButton(await theRowOf(CONFIGURATION_MANAGEMENT), "Yes"),
		);
		await waitFor(async () =>
			expect(await theVerdict()).toHaveTextContent(/^5 ready — in the range/),
		);

		expect(refinementService.getRefinement).toHaveBeenCalledTimes(2);
		expect(verdictsReported()).toEqual(["Below"]);
	});

	// @us-05 @slice-05 @boundary @contract-shape:bounded-change
	// The Team page keeps the tab mounted when the address moves to another Team, so a vote cast on the
	// Team before can be answered while the next Team's Refinement is still being read.
	it("reports the next Team's verdict, not the last one's, when a vote cast before the move is answered after it", async () => {
		aBrowserThatVotedBefore("Jonas Weber");
		let answerTheVote: (row: IVotedRow) => void = () => {};
		const sizingLogService = aSizingLogService({
			castVote: vi.fn(
				() =>
					new Promise<IVotedRow>((resolve) => {
						answerTheVote = resolve;
					}),
			),
		});
		const { user, refinementService, moveToTeam } = renderTheRefinementTab(
			{
				...aVotesTeamShowing(4, "Below", [
					aRow(CONFIGURATION_MANAGEMENT, "Configuration management", "Backlog"),
				]),
				isRefinementDay: true,
			},
			sizingLogService,
		);
		const nebulasRead = aReadStillOnItsWay(refinementService);

		await theVerdict();
		await user.click(
			theButton(await theRowOf(CONFIGURATION_MANAGEMENT), "Yes"),
		);
		moveToTeam(teamNebula());
		await act(async () => {
			answerTheVote(
				aVotedRow(CONFIGURATION_MANAGEMENT, "Configuration management", {
					voteCount: 1,
					missingVotes: 2,
				}),
			);
		});
		await nebulasRead.answer({
			...aVotesTeamShowing(6, "In", [
				aRow("NB-012", "Telemetry export", "Backlog"),
			]),
			isRefinementDay: true,
		});

		await waitFor(() => expect(verdictsReported()).toEqual(["Below", "In"]));
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
