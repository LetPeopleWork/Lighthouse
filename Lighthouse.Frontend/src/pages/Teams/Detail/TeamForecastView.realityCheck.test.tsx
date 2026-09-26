import { beforeEach, describe, expect, it, vi } from "vitest";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import {
	aRealityCheckAnswer,
	expectALine,
	linesMatching,
	type RealityCheckWireAnswer,
	setMatchMedia,
	theDialogWithTheAnswer,
} from "../../../tests/RealityCheckFixture";

/**
 * The words the Forecast Reality Check answers in: the sentence about the sampling windows, the two
 * findings and the denominator. They are read inside the dialog the answer opens in; how the dialog opens,
 * closes and runs is specified in TeamForecastView.realityCheck.dialog.test.tsx.
 *
 * The answer is handed in as the server sends it, so these specs pin the words the browser composes
 * from facts. The server never sends a sentence, because every sentence here carries a word the
 * instance may have renamed. The group's other forecasters are stood in for, so the only controls left
 * in it are the check's own.
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

const runRealityCheck = vi.fn();

/**
 * A level that is neither extreme is shown by its two counts alone: until the maintainer settles what that
 * middle reading is called, no calibration adjective is put on it. So every verdict is checked for that
 * before the spec asserts anything of its own.
 */
const theVerdictFor = async (
	answer: RealityCheckWireAnswer,
): Promise<HTMLElement> => {
	const dialog = await theDialogWithTheAnswer(runRealityCheck, answer);
	expect(linesMatching(dialog, /about right/i)).toHaveLength(0);
	return dialog;
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

beforeEach(() => {
	vi.clearAllMocks();
	setMatchMedia(false);
	terms.clear();
	terms.set(TERMINOLOGY_KEYS.TEAM, "Team");
	terms.set(TERMINOLOGY_KEYS.WORK_ITEMS, "Work Items");
	terms.set(TERMINOLOGY_KEYS.WORK_ITEM, "Work Item");
});

describe("@us-01 @driving_port one sentence about your sampling window, read in the dialog", () => {
	it("every window behaving alike reads as an answer: the whole range, and the setting is fine", async () => {
		const dialog = await theVerdictFor(aRealityCheckAnswer());

		expectALine(dialog, /between 14 and 90 days/i);
		expectALine(dialog, /current 30\b.*inside that range/i);
		expectALine(dialog, /this setting is fine/i);
		expect(
			linesMatching(dialog, /\b(best|recommended|optimal)\b/i),
		).toHaveLength(0);
	});

	it("Deep Current's 14 days sits outside a range and is told so", async () => {
		const dialog = await theVerdictFor(
			aRealityCheckAnswer({
				teamName: "Deep Current",
				currentSettingDays: 14,
				soundWindowDays: [30, 60, 90],
				determination: "SomeWindowsSound",
				standing: "Outside",
			}),
		);

		expectALine(dialog, /between 30 and 90 days/i);
		expectALine(dialog, /current 14\b.*not inside that range/i);
	});

	it("@error a region with a hole in it is listed window by window, never as a span that would claim the gap", async () => {
		const dialog = await theVerdictFor(
			aRealityCheckAnswer({
				currentSettingDays: 45,
				soundWindowDays: [14, 30, 60, 90],
				determination: "SomeWindowsSound",
				standing: "Outside",
			}),
		);

		expect(linesMatching(dialog, /between 14 and 90 days/i)).toHaveLength(0);
		expectALine(dialog, /14\D+30\D+60\D+90 days/i);
		expectALine(dialog, /current 45\b.*not inside/i);
	});

	it("@error a window that could not be checked in the middle of the ladder breaks the range, and is named as not checked rather than as not holding up", async () => {
		const dialog = await theVerdictFor(
			aRealityCheckAnswer({
				currentSettingDays: 60,
				soundWindowDays: [14, 60, 90],
				determination: "SomeWindowsSound",
				unevaluable: fourChecksOfTheWindow(30, "DegenerateForecast", 12),
			}),
		);

		expect(linesMatching(dialog, /between 14 and 90 days/i)).toHaveLength(0);
		expectALine(dialog, /14\D+60\D+90 days/i);
		expectALine(dialog, /30-day.*could not/i);
		expect(linesMatching(dialog, /30-day.*not inside/i)).toHaveLength(0);
	});

	it("@error when no window held up the sentence says so and names no least-bad window", async () => {
		const dialog = await theVerdictFor(
			aRealityCheckAnswer({
				teamName: "Deep Current",
				currentSettingDays: 14,
				soundWindowDays: [],
				determination: "NoWindowSound",
				standing: "Outside",
			}),
		);

		expect(linesMatching(dialog, /between \d+ and \d+ days/i)).toHaveLength(0);
		expect(
			linesMatching(dialog, /\b(best|closest|least bad|recommended)\b/i),
		).toHaveLength(0);
	});

	it.each([
		{ ownWindow: 30, runs: 16, scores: 64 },
		{ ownWindow: 45, runs: 20, scores: 80 },
	])(
		"states its denominator and why the checks cannot be ranked, on screen and never behind a tooltip - $runs runs",
		async ({ ownWindow, runs, scores }) => {
			const dialog = await theVerdictFor(
				aRealityCheckAnswer({ currentSettingDays: ownWindow }),
			);

			const denominator = linesMatching(
				dialog,
				new RegExp(`${runs} forecast runs were checked`, "i"),
			);
			expect(denominator).not.toHaveLength(0);
			expect(denominator[0]).toBeVisible();
			expect(denominator[0].closest('[role="tooltip"]')).toBeNull();
			expectALine(dialog, new RegExp(`${scores} scores in all`, "i"));
			expectALine(dialog, /4 confidence levels/i);
			expectALine(dialog, /same simulation.*not independent/i);
			expectALine(dialog, /different stretch of real time/i);
			expectALine(dialog, /should not be ranked/i);
		},
	);

	it("@error checks that could not run are named with their reason and left out of every count", async () => {
		const dialog = await theVerdictFor(coastalSurvey());

		expectALine(dialog, /(4|four) of the (16|sixteen) checks could not run/i);
		expectALine(dialog, /14-day.*fewer than 5 days with completed Work Items/i);
		expectALine(dialog, /12 forecast runs were checked/i);
		expectALine(dialog, /48 scores in all/i);
	});

	it("@error a Team whose history supports no check is told nothing could be concluded rather than shown an empty result", async () => {
		const dialog = await theVerdictFor(
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
		);

		expectALine(
			dialog,
			/(16|sixteen) of the (16|sixteen) checks could not run/i,
		);
		expect(linesMatching(dialog, /between \d+ and \d+ days/i)).toHaveLength(0);
	});

	it("@error a Team forecasting from fixed dates still gets the region, and no claim about a setting that was not tested", async () => {
		const dialog = await theVerdictFor(
			aRealityCheckAnswer({
				currentSettingDays: 45,
				currentSettingWasTested: false,
				standing: "NotTested",
				notTestedReason: "UsesFixedDates",
			}),
		);

		expectALine(dialog, /between 14 and 90 days/i);
		expect(linesMatching(dialog, /inside that range/i)).toHaveLength(0);
		expect(linesMatching(dialog, /\bcurrent 45\b/i)).toHaveLength(0);
	});

	it("reports the two findings as two findings: the window is a setting, the confidence level is not", async () => {
		const dialog = await theVerdictFor(aRealityCheckAnswer());

		expectALine(dialog, /sampling window is a setting on this Team/i);
		expectALine(dialog, /confidence level is not a setting/i);
	});

	it("speaks the instance's own words for Team and Work Item", async () => {
		terms.set(TERMINOLOGY_KEYS.TEAM, "Squad");
		terms.set(TERMINOLOGY_KEYS.WORK_ITEMS, "Tickets");
		terms.set(TERMINOLOGY_KEYS.WORK_ITEM, "Ticket");

		const dialog = await theVerdictFor(coastalSurvey());

		expectALine(dialog, /completed Tickets/);
		expectALine(dialog, /setting on this Squad/);
		expect(linesMatching(dialog, /work items?/i)).toHaveLength(0);
	});
});
