import { screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import {
	aRealityCheckAnswer,
	expectALine,
	linesMatching,
	pressRunRealityCheck,
	type RealityCheckWireAnswer,
	renderTheForecastTab,
	theAnswerIn,
} from "../../../tests/RealityCheckFixture";

/**
 * The Forecast Reality Check as the forecaster meets it: one button in the Forecast Backtesting group,
 * the sentence that comes back (slice 01) and the evidence behind it (slice 02). The one-pager that
 * was slice 03 is deferred while the maintainer re-evaluates how the check is reported, so it has no
 * specs here.
 *
 * The answer is handed in as the server sends it, so these specs pin the words the browser composes
 * from facts. The server never sends a sentence, because every sentence here carries a word the
 * instance may have renamed. The group's other forecasters are stood in for, so the only controls left
 * in it are the check's own.
 *
 * What the specs need from the markup, and nothing more: the evidence panels and their rows are
 * labelled groups - "Sampling window: 30 days", "8 weeks" - so a panel and a row can be found by the
 * words a reader sees on them. Every spec is pending until the check exists.
 */

const { terms } = vi.hoisted(() => ({
	terms: new Map<string, string>(),
}));

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => terms.get(key) ?? key,
		isLoading: false,
		error: null,
		refetchTerminology: () => {},
	}),
}));

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		canUseNewItemForecaster: true,
		newItemForecasterTooltip: "",
		isLoading: false,
		licenseStatus: {
			hasLicense: true,
			isValid: true,
			canUsePremiumFeatures: true,
		},
	}),
}));

vi.mock("../../../components/Common/InputGroup/InputGroup", () => ({
	default: ({
		title,
		children,
	}: {
		title: string;
		children: React.ReactNode;
	}) => (
		<section aria-label={title}>
			<h2>{title}</h2>
			{children}
		</section>
	),
}));

vi.mock("./ManualForecaster", () => ({
	default: () => <div data-testid="manual-forecaster" />,
}));

vi.mock("./NewItemForecaster", () => ({
	default: () => <div data-testid="new-item-forecaster" />,
}));

vi.mock("./BacktestForecaster", () => ({
	default: () => <div data-testid="backtest-forecaster" />,
}));

const PENDING = "the Forecast Reality Check is not built yet";

const runRealityCheck = vi.fn();

/**
 * Every answer here renders its levels, and a level that is neither extreme is shown by its two counts
 * alone: until the maintainer settles what that middle reading is called, no calibration adjective is
 * put on it. So every verdict is checked for that before the spec asserts anything of its own.
 */
const theVerdictFor = async (
	answer: RealityCheckWireAnswer,
	onceItReads?: RegExp,
): Promise<HTMLElement> => {
	const group = await theAnswerIn(runRealityCheck, answer, onceItReads);
	expect(linesMatching(group, /about right/i)).toHaveLength(0);
	return group;
};

const fourChecksOfTheWindow = (
	window: number,
	reason: "TooFewActiveDays" | "DegenerateForecast",
	daysWithCompletedWork: number,
) =>
	[7, 14, 28, 56].map((horizon) => ({
		window,
		horizon,
		reason,
		daysWithCompletedWork,
	}));

const coastalSurvey = () =>
	aRealityCheckAnswer({
		teamName: "Coastal Survey",
		soundWindowDays: [30, 60, 90],
		determination: "SomeWindowsSound",
		heldCounts: { 50: 3, 70: 7, 85: 10, 95: 12 },
		unevaluatedWindowDays: [14],
		unevaluable: fourChecksOfTheWindow(14, "TooFewActiveDays", 3),
	});

const expandTheEvidence = async (group: HTMLElement) => {
	await userEvent.click(
		within(group).getByRole("button", { name: /show the evidence/i }),
	);
};

const panelFor = (group: HTMLElement, windowDays: number) =>
	within(group).getByRole("group", {
		name: new RegExp(`sampling window: ${windowDays} days`, "i"),
	});

const rowFor = (panel: HTMLElement, horizon: string) =>
	within(panel).getByRole("group", { name: new RegExp(`^${horizon}`, "i") });

const panelTitlesInOrder = (group: HTMLElement): number[] =>
	within(group)
		.getAllByRole("group", { name: /sampling window: \d+ days/i })
		.map((panel) =>
			Number(/(\d+) days/i.exec(panel.getAttribute("aria-label") ?? "")?.[1]),
		);

beforeEach(() => {
	vi.clearAllMocks();
	terms.clear();
	terms.set(TERMINOLOGY_KEYS.TEAM, "Team");
	terms.set(TERMINOLOGY_KEYS.WORK_ITEMS, "Work Items");
	terms.set(TERMINOLOGY_KEYS.WORK_ITEM, "Work Item");
});

describe("@us-01 @driving_port slice 01 - one sentence about your sampling window", () => {
	it(`@walking_skeleton pressing Run reality check answers in the Forecast Backtesting group without asking for a date (${PENDING})`, async () => {
		const group = await theVerdictFor(aRealityCheckAnswer());

		expect(runRealityCheck).toHaveBeenCalledTimes(1);
		expect(runRealityCheck.mock.calls[0][0]).toBe(42);
		expect(
			runRealityCheck.mock.calls[0].some(
				(argument: unknown) => argument instanceof Date,
			),
		).toBe(false);
		expect(within(group).queryAllByRole("textbox")).toHaveLength(0);
	});

	it(`every window behaving alike reads as an answer: the whole range, and the setting is fine (${PENDING})`, async () => {
		const group = await theVerdictFor(aRealityCheckAnswer());

		expectALine(group, /between 14 and 90 days/i);
		expectALine(group, /current 30\b.*inside that range/i);
		expectALine(group, /this setting is fine/i);
		expect(
			linesMatching(group, /\b(best|recommended|optimal)\b/i),
		).toHaveLength(0);
	});

	it(`Deep Current's 14 days sits outside a range and is told so (${PENDING})`, async () => {
		const group = await theVerdictFor(
			aRealityCheckAnswer({
				teamName: "Deep Current",
				currentSettingDays: 14,
				soundWindowDays: [30, 60, 90],
				determination: "SomeWindowsSound",
				standing: "Outside",
			}),
		);

		expectALine(group, /between 30 and 90 days/i);
		expectALine(group, /current 14\b.*not inside that range/i);
	});

	it(`@error a region with a hole in it is listed window by window, never as a span that would claim the gap (${PENDING})`, async () => {
		const group = await theVerdictFor(
			aRealityCheckAnswer({
				currentSettingDays: 45,
				soundWindowDays: [14, 30, 60, 90],
				determination: "SomeWindowsSound",
				standing: "Outside",
			}),
		);

		expect(linesMatching(group, /between 14 and 90 days/i)).toHaveLength(0);
		expectALine(group, /14\D+30\D+60\D+90 days/i);
		expectALine(group, /current 45\b.*not inside/i);
	});

	it(`@error a window that could not be checked in the middle of the ladder breaks the range, and is named as not checked rather than as not holding up (${PENDING})`, async () => {
		const group = await theVerdictFor(
			aRealityCheckAnswer({
				currentSettingDays: 60,
				soundWindowDays: [14, 60, 90],
				determination: "SomeWindowsSound",
				unevaluable: fourChecksOfTheWindow(30, "DegenerateForecast", 12),
			}),
		);

		expect(linesMatching(group, /between 14 and 90 days/i)).toHaveLength(0);
		expectALine(group, /14\D+60\D+90 days/i);
		expectALine(group, /30-day.*could not/i);
		expect(linesMatching(group, /30-day.*not inside/i)).toHaveLength(0);
	});

	it(`@error when no window held up the sentence says so and names no least-bad window (${PENDING})`, async () => {
		const group = await theVerdictFor(
			aRealityCheckAnswer({
				teamName: "Deep Current",
				currentSettingDays: 14,
				soundWindowDays: [],
				determination: "NoWindowSound",
				standing: "Outside",
			}),
		);

		expect(linesMatching(group, /between \d+ and \d+ days/i)).toHaveLength(0);
		expect(
			linesMatching(group, /\b(best|closest|least bad|recommended)\b/i),
		).toHaveLength(0);
	});

	it.each([
		{ ownWindow: 30, runs: 16, scores: 64 },
		{ ownWindow: 45, runs: 20, scores: 80 },
	])(
		`states its denominator and why the checks cannot be ranked, on screen and never behind a tooltip - $runs runs (${PENDING})`,
		async ({ ownWindow, runs, scores }) => {
			const group = await theVerdictFor(
				aRealityCheckAnswer({ currentSettingDays: ownWindow }),
			);

			const denominator = linesMatching(
				group,
				new RegExp(`${runs} forecast runs were checked`, "i"),
			);
			expect(denominator).not.toHaveLength(0);
			expect(denominator[0]).toBeVisible();
			expect(denominator[0].closest('[role="tooltip"]')).toBeNull();
			expectALine(group, new RegExp(`${scores} scores in all`, "i"));
			expectALine(group, /4 confidence levels/i);
			expectALine(group, /same simulation.*not independent/i);
			expectALine(group, /different stretch of real time/i);
			expectALine(group, /should not be ranked/i);
		},
	);

	it(`each level is reported as how often it held against its own percentage of the checks (${PENDING})`, async () => {
		const group = await theVerdictFor(
			aRealityCheckAnswer({ heldCounts: { 50: 8, 70: 11, 85: 14, 95: 15 } }),
		);

		expectALine(group, /50%.*held in 8\b.*about 8 expected/i);
		expectALine(group, /85%.*held in 14\b.*about 14 expected/i);
		expect(linesMatching(group, /\bbeaten\b/i)).toHaveLength(0);
	});

	it(`@error a level that never held is called over-forecasting, beside how often it should have held (${PENDING})`, async () => {
		const group = await theVerdictFor(
			aRealityCheckAnswer({
				readings: {
					50: "NeverHeld",
					70: "NeverHeld",
					85: "NeverHeld",
					95: "NeverHeld",
				},
			}),
		);

		expectALine(
			group,
			/95%.*held in 0\b.*about 15 expected.*over-forecasting/i,
		);
		expect(linesMatching(group, /\bexcellen/i)).toHaveLength(0);
	});

	it(`@error checks that could not run are named with their reason and left out of every count (${PENDING})`, async () => {
		const group = await theVerdictFor(coastalSurvey());

		expectALine(group, /(4|four) of the (16|sixteen) checks could not run/i);
		expectALine(group, /14-day.*fewer than 5 days with completed Work Items/i);
		expectALine(group, /12 forecast runs were checked/i);
		expectALine(group, /48 scores in all/i);
	});

	it(`@error a Team whose history supports no check is told nothing could be concluded rather than shown an empty result (${PENDING})`, async () => {
		const group = await theVerdictFor(
			aRealityCheckAnswer({
				soundWindowDays: [],
				determination: "NotEnoughEvidence",
				standing: "NotDetermined",
				unevaluatedWindowDays: [14, 30, 60, 90],
				readings: {
					50: "NotEvaluated",
					70: "NotEvaluated",
					85: "NotEvaluated",
					95: "NotEvaluated",
				},
				unevaluable: [14, 30, 60, 90].flatMap((window) =>
					fourChecksOfTheWindow(window, "TooFewActiveDays", 1),
				),
			}),
			/could not run/i,
		);

		expectALine(
			group,
			/(16|sixteen) of the (16|sixteen) checks could not run/i,
		);
		expect(linesMatching(group, /between \d+ and \d+ days/i)).toHaveLength(0);
	});

	it(`@error a Team forecasting from fixed dates still gets the region, and no claim about a setting that was not tested (${PENDING})`, async () => {
		const group = await theVerdictFor(
			aRealityCheckAnswer({
				currentSettingDays: 45,
				currentSettingWasTested: false,
				standing: "NotTested",
				notTestedReason: "UsesFixedDates",
			}),
		);

		expectALine(group, /between 14 and 90 days/i);
		expect(linesMatching(group, /inside that range/i)).toHaveLength(0);
		expect(linesMatching(group, /\bcurrent 45\b/i)).toHaveLength(0);
	});

	it(`reports the two findings as two findings: the window is a setting, the confidence level is not (${PENDING})`, async () => {
		const group = await theVerdictFor(aRealityCheckAnswer());

		expectALine(group, /sampling window is a setting on this Team/i);
		expectALine(group, /confidence level is not a setting/i);
	});

	it(`@kpi-OUT-4172-read-only offers no control that could change a Team setting (${PENDING})`, async () => {
		const group = await theVerdictFor(aRealityCheckAnswer());

		for (const role of [
			"textbox",
			"spinbutton",
			"combobox",
			"checkbox",
			"switch",
			"radio",
			"slider",
		]) {
			expect(within(group).queryAllByRole(role), role).toHaveLength(0);
		}
		const buttons = within(group).getAllByRole("button");
		expect(buttons.length).toBeGreaterThan(0);
		for (const button of buttons) {
			expect(button).toHaveAccessibleName(/reality check|evidence/i);
		}
	});

	it(`speaks the instance's own words for Team and Work Item (${PENDING})`, async () => {
		terms.set(TERMINOLOGY_KEYS.TEAM, "Squad");
		terms.set(TERMINOLOGY_KEYS.WORK_ITEMS, "Tickets");
		terms.set(TERMINOLOGY_KEYS.WORK_ITEM, "Ticket");

		const group = await theVerdictFor(coastalSurvey());

		expectALine(group, /completed Tickets/);
		expectALine(group, /setting on this Squad/);
		expect(linesMatching(group, /work items?/i)).toHaveLength(0);
	});

	it(`@error a check that fails to come back leaves no verdict and says what went wrong (${PENDING})`, async () => {
		runRealityCheck.mockRejectedValue(
			new Error("The reality check could not be run"),
		);
		const group = renderTheForecastTab(runRealityCheck);

		await pressRunRealityCheck(group);

		expect(
			await screen.findByText(/the reality check could not be run/i),
		).toBeInTheDocument();
		expect(within(group).queryByText(/forecast runs were checked/i)).toBeNull();
	});

	it(`@error pressing again while a check is running does not start a second one (${PENDING})`, async () => {
		runRealityCheck.mockReturnValue(new Promise(() => {}));
		const group = renderTheForecastTab(runRealityCheck);

		await pressRunRealityCheck(group);
		const pressAgain = within(group).queryByRole("button", {
			name: /^run reality check$/i,
		});
		if (pressAgain !== null && !pressAgain.hasAttribute("disabled")) {
			await userEvent.click(pressAgain);
		}

		expect(runRealityCheck).toHaveBeenCalledTimes(1);
	});
});

describe("@us-02 slice 02 - the evidence you can look at", () => {
	it.skip(`expanding shows one panel per sampling window checked, in order of length, and no confidence-level control (${PENDING})`, async () => {
		const group = await theVerdictFor(aRealityCheckAnswer());

		await expandTheEvidence(group);

		expect(panelTitlesInOrder(group)).toEqual([14, 30, 60, 90]);
		for (const role of ["combobox", "radio", "switch", "tab", "slider"]) {
			expect(within(group).queryAllByRole(role), role).toHaveLength(0);
		}
	});

	it.skip(`a Team whose own window is off the ladder gets a fifth panel, in its place by length (${PENDING})`, async () => {
		const group = await theVerdictFor(
			aRealityCheckAnswer({ currentSettingDays: 45 }),
		);

		await expandTheEvidence(group);

		expect(panelTitlesInOrder(group)).toEqual([14, 30, 45, 60, 90]);
	});

	it.skip(`a row draws the forecast as a band with its four levels and marks where the Team's actual landed (${PENDING})`, async () => {
		const group = await theVerdictFor(
			aRealityCheckAnswer({
				checks: [
					{
						window: 30,
						horizon: 56,
						at95: 31,
						at85: 36,
						at70: 41,
						at50: 48,
						actual: 42,
					},
				],
			}),
		);

		await expandTheEvidence(group);
		const row = rowFor(panelFor(group, 30), "8 weeks");

		for (const level of ["95%", "85%", "70%", "50%"]) {
			expect(within(row).getByText(level)).toBeInTheDocument();
		}
		expect(within(row).getByText("42")).toBeInTheDocument();
	});

	it.skip(`@error a check that could not run says so in words where the band would be, never blank (${PENDING})`, async () => {
		const group = await theVerdictFor(coastalSurvey());

		await expandTheEvidence(group);
		const row = rowFor(panelFor(group, 14), "2 weeks");

		expect(row).toHaveTextContent(/not enough history in this window/i);
		expect(row).toHaveTextContent(
			/3 days with completed Work Items, 5 needed/i,
		);
		expect(within(row).queryByText("95%")).toBeNull();
	});

	it.skip(`@error a check whose forecast could not be worked out gives its own reason, not the thin-history one (${PENDING})`, async () => {
		const group = await theVerdictFor(
			aRealityCheckAnswer({
				unevaluable: [
					{
						window: 30,
						horizon: 28,
						reason: "DegenerateForecast",
						daysWithCompletedWork: 20,
					},
				],
			}),
		);

		await expandTheEvidence(group);
		const row = rowFor(panelFor(group, 30), "4 weeks");

		expect(row.textContent?.trim()).not.toBe("4 weeks");
		expect(row).not.toHaveTextContent(/days with completed/i);
		expect(within(row).queryByText("95%")).toBeNull();
	});

	it.skip(`@error a panel none of whose checks could run still appears, every row carrying its reason (${PENDING})`, async () => {
		const group = await theVerdictFor(
			aRealityCheckAnswer({
				unevaluable: fourChecksOfTheWindow(90, "TooFewActiveDays", 2),
			}),
		);

		await expandTheEvidence(group);
		const panel = panelFor(group, 90);

		for (const horizon of ["1 week", "2 weeks", "4 weeks", "8 weeks"]) {
			expect(rowFor(panel, horizon)).toHaveTextContent(
				/not enough history in this window/i,
			);
		}
	});

	it.skip(`below the panels each level says how often it held against how often it should have (${PENDING})`, async () => {
		const group = await theVerdictFor(coastalSurvey());

		await expandTheEvidence(group);

		expectALine(group, /50%.*held in 3\b.*about 6 expected/i);
		expectALine(group, /85%.*held in 10\b.*about 10 expected/i);
		expect(linesMatching(group, /\bbeaten\b/i)).toHaveLength(0);
	});

	it.skip(`the denominator and non-comparability statements stay on screen whether or not the evidence is open (${PENDING})`, async () => {
		const group = await theVerdictFor(aRealityCheckAnswer());

		expectALine(group, /should not be ranked/i);
		await expandTheEvidence(group);
		expectALine(group, /should not be ranked/i);
		expectALine(group, /16 forecast runs were checked/i);
	});
});
