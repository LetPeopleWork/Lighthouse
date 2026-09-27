import { within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ForecastGrade } from "../../../models/Forecasts/RealityCheckResult";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import {
	aRealityCheckAnswer,
	type EvaluableCheck,
	expectALine,
	expectTheLine,
	HORIZON_DAYS,
	type LevelColumn,
	levelCell,
	linesMatching,
	periodGroup,
	periodGroupsOf,
	type RealityCheckAnswerOptions,
	STANDARD_WINDOW_DAYS,
	setMatchMedia,
	theDialogWithTheAnswer,
	thePeriodChecked,
	theTableIn,
	windowRow,
	windowRowsOf,
} from "../../../tests/RealityCheckFixture";
import { appColors } from "../../../utils/theme/colors";

/**
 * How close each forecast landed, on Nick Brown's scale. Every graded cell is shaded
 * green when it held and red when it did not - green deepens the closer it landed, red the further it
 * missed - and carries the percentage of what the period delivered beside its miss in Work Items; each
 * level's line adds how many of its checks landed within 10% - held or not - and, when more than half of
 * them share one grade, how it usually landed.
 *
 * A cell's accessible name reads level, forecast, held or not, the miss in words, and the percentage. The
 * percentage and every grade word are pinned whole, because "within 10%" is the start of both a held and a
 * not-held grade.
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

// Renders the slot the reality check's trigger moves into, so the button is found wherever it sits.
vi.mock("./BacktestForecaster", () => ({
	default: ({ realityCheck }: { realityCheck?: React.ReactNode }) => (
		<div data-testid="backtest-forecaster">{realityCheck}</div>
	),
}));

const MINUS = "−";

const runRealityCheck = vi.fn();

const theDialogFor = (options: RealityCheckAnswerOptions) =>
	theDialogWithTheAnswer(runRealityCheck, aRealityCheckAnswer(options));

const gradeFill = (grade: ForecastGrade): string =>
	appColors.forecastGrade[grade];

const LEVEL_KEY = { 50: "at50", 70: "at70", 85: "at85", 95: "at95" } as const;

/**
 * The sixteen checks of the standard ladder, every one against an actual of 42, one level's forecasts
 * taken in turn from `forecasts`. The other levels sit at 20, well clear of anything asserted.
 */
const theSixteenChecksAt = (
	level: LevelColumn,
	forecasts: readonly number[],
): EvaluableCheck[] =>
	STANDARD_WINDOW_DAYS.flatMap((window, windowIndex) =>
		HORIZON_DAYS.map((horizon, horizonIndex) => {
			const check: EvaluableCheck = {
				window,
				horizon,
				at95: 20,
				at85: 20,
				at70: 20,
				at50: 20,
				actual: 42,
			};
			check[LEVEL_KEY[level]] = forecasts[windowIndex * 4 + horizonIndex];
			return check;
		}),
	);

const times = (count: number, forecast: number): number[] =>
	Array.from({ length: count }, () => forecast);

// Against an actual of 42, one forecast per grade.
const HELD_WITHIN_10 = 40;
const HELD_10_TO_25 = 36;
const HELD_OVER_25 = 31;
const NOT_HELD_WITHIN_10 = 45;
const NOT_HELD_10_TO_25 = 48;
const NOT_HELD_OVER_25 = 60;

const oceanExplorerOverEightWeeks = (): RealityCheckAnswerOptions => ({
	checks: thePeriodChecked(56, {
		at95: 31,
		at85: 36,
		at70: 40,
		at50: 48,
		actual: 42,
	}),
});

beforeEach(() => {
	vi.clearAllMocks();
	setMatchMedia(false);
	terms.clear();
	terms.set(TERMINOLOGY_KEYS.TEAM, "Team");
	terms.set(TERMINOLOGY_KEYS.WORK_ITEMS, "Work Items");
	terms.set(TERMINOLOGY_KEYS.WORK_ITEM, "Work Item");
});

describe("@us-05 @slice-05 each forecast graded by how close it landed", () => {
	it(`@kpi-OUT-6094-no-percentage-without-its-work-items a forecast that held is shaded by how close it landed - Ocean Explorer's 30-day row over 8 weeks`, async () => {
		const dialog = await theDialogFor(oceanExplorerOverEightWeeks());
		const row = windowRow(periodGroup(theTableIn(dialog), 56), 30);

		expect(levelCell(row, 95)).toHaveAccessibleName(
			"95th: 31, held, 11 more delivered, 26% of the actual",
		);
		expect(levelCell(row, 85)).toHaveAccessibleName(
			"85th: 36, held, 6 more delivered, 14% of the actual",
		);
		expect(levelCell(row, 70)).toHaveAccessibleName(
			"70th: 40, held, 2 more delivered, 5% of the actual",
		);
		expect(within(levelCell(row, 95)).getByText("26%")).toBeInTheDocument();
		expect(within(levelCell(row, 95)).getByText("+11")).toBeInTheDocument();
		expect(levelCell(row, 95)).toHaveStyle({
			backgroundColor: gradeFill("HeldOver25"),
		});
		expect(levelCell(row, 85)).toHaveStyle({
			backgroundColor: gradeFill("Held10To25"),
		});
		expect(levelCell(row, 70)).toHaveStyle({
			backgroundColor: gradeFill("HeldWithin10"),
		});
	});

	it(`@kpi-OUT-6094-no-percentage-without-its-work-items a forecast that did not hold is graded by how far it fell short - the same row's 50th`, async () => {
		const dialog = await theDialogFor(oceanExplorerOverEightWeeks());
		const cell = levelCell(
			windowRow(periodGroup(theTableIn(dialog), 56), 30),
			50,
		);

		expect(cell).toHaveAccessibleName(
			"50th: 48, did not hold, 6 fewer delivered, 14% of the actual",
		);
		expect(within(cell).getByText(`${MINUS}6`)).toBeInTheDocument();
		expect(within(cell).getByText("14%")).toBeInTheDocument();
		expect(cell).toHaveStyle({ backgroundColor: gradeFill("NotHeld10To25") });
	});

	it(`@error @kpi-OUT-6094-no-percentage-without-its-work-items a one-Work-Item miss on a small period shows the Work Item beside the percentage - Coastal Survey's week`, async () => {
		const dialog = await theDialogFor({
			teamName: "Coastal Survey",
			checks: [
				{
					window: 30,
					horizon: 7,
					at95: 1,
					at85: 2,
					at70: 3,
					at50: 4,
					actual: 3,
				},
			],
		});
		const cell = levelCell(
			windowRow(periodGroup(theTableIn(dialog), 7), 30),
			50,
		);

		expect(cell).toHaveAccessibleName(
			"50th: 4, did not hold, 1 fewer delivered, 33% of the actual",
		);
		expect(within(cell).getByText(`${MINUS}1`)).toBeInTheDocument();
		expect(within(cell).getByText("33%")).toBeInTheDocument();
		expect(cell).toHaveStyle({ backgroundColor: gradeFill("NotHeldOver25") });
	});

	it(`@error @kpi-OUT-6094-no-percentage-without-its-work-items a period in which nothing was delivered has no percentage: 0 against 0 held exactly, 2 against 0 did not hold by the most - Harbour Pilots' week`, async () => {
		const dialog = await theDialogFor({
			teamName: "Harbour Pilots",
			checks: [
				{
					window: 30,
					horizon: 7,
					at95: 0,
					at85: 1,
					at70: 1,
					at50: 2,
					actual: 0,
				},
			],
		});
		const row = windowRow(periodGroup(theTableIn(dialog), 7), 30);

		expect(levelCell(row, 95)).toHaveAccessibleName(
			"95th: 0, held, exactly as forecast",
		);
		expect(levelCell(row, 50)).toHaveAccessibleName(
			"50th: 2, did not hold, 2 fewer delivered",
		);
		expect(
			within(levelCell(row, 50)).getByText(`${MINUS}2`),
		).toBeInTheDocument();
		expect(levelCell(row, 95)).not.toHaveTextContent("%");
		expect(levelCell(row, 50)).not.toHaveTextContent("%");
		expect(levelCell(row, 95)).toHaveStyle({
			backgroundColor: gradeFill("HeldWithin10"),
		});
		expect(levelCell(row, 50)).toHaveStyle({
			backgroundColor: gradeFill("NotHeldOver25"),
		});
	});

	it.each<{
		actual: number;
		forecast: number;
		shown: string;
		grade: ForecastGrade;
	}>([
		{ actual: 40, forecast: 36, shown: "10%", grade: "HeldWithin10" },
		{ actual: 39, forecast: 35, shown: "11%", grade: "Held10To25" },
		{ actual: 100, forecast: 75, shown: "25%", grade: "Held10To25" },
		{ actual: 99, forecast: 74, shown: "26%", grade: "HeldOver25" },
		{ actual: 300, forecast: 299, shown: "1%", grade: "HeldWithin10" },
	])(
		`@boundary shows $shown for a forecast of $forecast against $actual, never a number its shade contradicts`,
		async ({ actual, forecast, shown, grade }) => {
			const dialog = await theDialogFor({
				checks: [
					{
						window: 30,
						horizon: 28,
						at95: forecast,
						at85: forecast,
						at70: forecast,
						at50: forecast,
						actual,
					},
				],
			});
			const cell = levelCell(
				windowRow(periodGroup(theTableIn(dialog), 28), 30),
				70,
			);

			expect(within(cell).getByText(shown)).toBeInTheDocument();
			expect(cell).toHaveAccessibleName(
				`70th: ${forecast}, held, ${actual - forecast} more delivered, ${shown} of the actual`,
			);
			expect(cell).toHaveStyle({ backgroundColor: gradeFill(grade) });
		},
	);

	it(`@kpi-OUT-6094-colour-never-alone every graded cell says in words whether it held and by how much, so colour is never the only sign`, async () => {
		const dialog = await theDialogFor({});
		const namePattern =
			/^(50|70|85|95)th: \d+, (held|did not hold), (exactly as forecast|\d+ (more|fewer) delivered), \d+% of the actual$/;

		const cells = periodGroupsOf(theTableIn(dialog))
			.flatMap(windowRowsOf)
			.flatMap((row) => within(row).getAllByRole("cell"));
		expect(cells).toHaveLength(64);
		for (const cell of cells) {
			expect(cell).toHaveAccessibleName(namePattern);
			expect(cell).toHaveTextContent(/[✓✗]/);
			expect(cell).toHaveTextContent(/\d+%/);
		}
	});

	it(`@kpi-OUT-6094-colour-never-alone spaces a graded cell's words apart, so the miss never runs into the percentage - the same row's 95th and 50th`, async () => {
		const dialog = await theDialogFor(oceanExplorerOverEightWeeks());
		const row = windowRow(periodGroup(theTableIn(dialog), 56), 30);

		expect(levelCell(row, 95).textContent).toBe("31 +11 26% ✓ held");
		expect(levelCell(row, 50).textContent).toBe(
			`48 ${MINUS}6 14% ✗ did not hold`,
		);
	});

	it(`@error a check that could not run keeps its words and takes no grade colour`, async () => {
		const dialog = await theDialogFor({
			unevaluable: HORIZON_DAYS.map((horizon) => ({
				window: 14,
				horizon,
				reason: "TooFewActiveDays" as const,
				daysWithCompletedWork: 3,
			})),
		});
		const [cell] = within(
			windowRow(periodGroup(theTableIn(dialog), 14), 14),
		).getAllByRole("cell");

		expect(cell).toHaveTextContent(/not enough history in this window/i);
		for (const grade of [
			"HeldWithin10",
			"Held10To25",
			"HeldOver25",
			"NotHeldWithin10",
			"NotHeld10To25",
			"NotHeldOver25",
		] as const) {
			expect(cell).not.toHaveStyle({ backgroundColor: gradeFill(grade) });
		}
	});
});

describe("@us-05 @slice-05 @kpi-OUT-6094-caution-is-visible each level's line says how close it usually landed", () => {
	it(`Maria's 85th: held 15 of 16, 3 within 10%, and usually low by more than a quarter`, async () => {
		const dialog = await theDialogFor({
			heldCounts: { 85: 15 },
			checks: theSixteenChecksAt(85, [
				...times(9, HELD_OVER_25),
				...times(3, HELD_WITHIN_10),
				...times(3, HELD_10_TO_25),
				...times(1, NOT_HELD_10_TO_25),
			]),
		});

		expectTheLine(
			dialog,
			"85th: held 15 of 16 (should be about 14), within 10% in 3. Usually low by more than a quarter.",
		);
	});

	it(`counts the checks that fell short by under 10% as within 10% too`, async () => {
		const dialog = await theDialogFor({
			heldCounts: { 50: 12 },
			checks: theSixteenChecksAt(50, [
				...times(2, HELD_WITHIN_10),
				...times(4, NOT_HELD_WITHIN_10),
				...times(5, HELD_10_TO_25),
				...times(5, HELD_OVER_25),
			]),
		});

		expectTheLine(
			dialog,
			"50th: held 12 of 16 (should be about 8), within 10% in 6.",
		);
	});

	it(`@boundary adds no "usually" when the checks within 10% are split between held and not held`, async () => {
		const dialog = await theDialogFor({
			heldCounts: { 70: 8 },
			checks: theSixteenChecksAt(70, [
				...times(5, HELD_WITHIN_10),
				...times(5, NOT_HELD_WITHIN_10),
				...times(3, HELD_10_TO_25),
				...times(3, NOT_HELD_10_TO_25),
			]),
		});

		expectTheLine(
			dialog,
			"70th: held 8 of 16 (should be about 11), within 10% in 10.",
		);
	});

	it(`@boundary adds no "usually" when one grade holds exactly half the checks, not more`, async () => {
		const dialog = await theDialogFor({
			heldCounts: { 95: 16 },
			checks: theSixteenChecksAt(95, [
				...times(8, HELD_OVER_25),
				...times(8, HELD_10_TO_25),
			]),
		});

		expectTheLine(
			dialog,
			"95th: held 16 of 16 (should be about 15), within 10% in 0.",
		);
	});

	it.each<{
		grade: ForecastGrade;
		level: LevelColumn;
		forecast: number;
		line: string;
	}>([
		{
			grade: "HeldWithin10",
			level: 95,
			forecast: HELD_WITHIN_10,
			line: "95th: held 16 of 16 (should be about 15), within 10% in 16. Usually within 10%.",
		},
		{
			grade: "Held10To25",
			level: 95,
			forecast: HELD_10_TO_25,
			line: "95th: held 16 of 16 (should be about 15), within 10% in 0. Usually low by 10-25%.",
		},
		{
			grade: "HeldOver25",
			level: 95,
			forecast: HELD_OVER_25,
			line: "95th: held 16 of 16 (should be about 15), within 10% in 0. Usually low by more than a quarter.",
		},
		{
			grade: "NotHeldWithin10",
			level: 50,
			forecast: NOT_HELD_WITHIN_10,
			line: "50th: held 0 of 16 (should be about 8), within 10% in 16 — it never held, which is over-forecasting. Usually high by up to 10%.",
		},
		{
			grade: "NotHeld10To25",
			level: 50,
			forecast: NOT_HELD_10_TO_25,
			line: "50th: held 0 of 16 (should be about 8), within 10% in 0 — it never held, which is over-forecasting. Usually high by 10-25%.",
		},
		{
			grade: "NotHeldOver25",
			level: 50,
			forecast: NOT_HELD_OVER_25,
			line: "50th: held 0 of 16 (should be about 8), within 10% in 0 — it never held, which is over-forecasting. Usually high by more than a quarter.",
		},
	])(
		`says how a level usually landed in the words of its grade - $grade`,
		async ({ grade, level, forecast, line }) => {
			const held = grade.startsWith("Held");
			const dialog = await theDialogFor({
				heldCounts: { [level]: held ? 16 : 0 },
				readings: held ? {} : { [level]: "NeverHeld" as const },
				checks: theSixteenChecksAt(level, times(16, forecast)),
			});

			expectTheLine(dialog, line);
		},
	);

	it(`@error leaves the checks that could not run out of "within 10%" and of "usually"`, async () => {
		const dialog = await theDialogFor({
			heldCounts: { 85: 12 },
			unevaluatedWindowDays: [14],
			unevaluable: HORIZON_DAYS.map((horizon) => ({
				window: 14,
				horizon,
				reason: "TooFewActiveDays" as const,
				daysWithCompletedWork: 3,
			})),
			checks: theSixteenChecksAt(85, times(16, HELD_WITHIN_10)),
		});

		expectTheLine(
			dialog,
			"85th: held 12 of 12 (should be about 10), within 10% in 12. Usually within 10%.",
		);
	});

	it(`@error keeps a grade usual when it is usual among the checks that ran, however many others could not run`, async () => {
		const dialog = await theDialogFor({
			heldCounts: { 85: 12 },
			unevaluatedWindowDays: [14],
			unevaluable: HORIZON_DAYS.map((horizon) => ({
				window: 14,
				horizon,
				reason: "TooFewActiveDays" as const,
				daysWithCompletedWork: 3,
			})),
			checks: theSixteenChecksAt(85, [
				...times(4, HELD_WITHIN_10),
				...times(7, HELD_WITHIN_10),
				...times(5, HELD_OVER_25),
			]),
		});

		expectTheLine(
			dialog,
			"85th: held 12 of 12 (should be about 10), within 10% in 7. Usually within 10%.",
		);
	});

	it(`@error a level no check could test says so, with no count and no "usually"`, async () => {
		const dialog = await theDialogFor({
			soundWindowDays: [],
			determination: "NotEnoughEvidence",
			standing: "NotDetermined",
			unevaluatedWindowDays: [...STANDARD_WINDOW_DAYS],
			readings: { 85: "NotEvaluated" },
			unevaluable: STANDARD_WINDOW_DAYS.flatMap((window) =>
				HORIZON_DAYS.map((horizon) => ({
					window,
					horizon,
					reason: "TooFewActiveDays" as const,
					daysWithCompletedWork: 1,
				})),
			),
		});

		expectTheLine(
			dialog,
			"85th: no check could be run, so this level was not tested.",
		);
		expect(linesMatching(dialog, /within 10% in|usually/i)).toHaveLength(0);
	});

	it(`@kpi-OUT-6094-no-window-ranked no level's line names, counts or ranks a sampling window`, async () => {
		const dialog = await theDialogFor({
			heldCounts: { 85: 15 },
			checks: theSixteenChecksAt(85, [
				...times(9, HELD_OVER_25),
				...times(7, HELD_WITHIN_10),
			]),
		});

		const levelLines = linesMatching(
			dialog,
			/^(50|70|85|95)th: (held|no check)/,
		);
		expect(levelLines).toHaveLength(4);
		for (const line of levelLines) {
			expect(line).not.toHaveTextContent(/\bdays?\b|window/i);
		}
	});
});

describe("@us-05 @slice-05 the legend and the credit", () => {
	it(`@kpi-OUT-6094-colour-never-alone names the six grades and the not-checked state in words`, async () => {
		const dialog = await theDialogFor({});

		for (const words of [
			"Held, within 10%",
			"Held, by 10-25%",
			"Held, by more than 25%",
			"Did not hold, within 10%",
			"Did not hold, by 10-25%",
			"Did not hold, by more than 25%",
			"Not checked",
		]) {
			expectTheLine(dialog, words);
		}
	});

	it(`credits Nick Brown's held-or-not grading and shading by closeness, and names what this product added`, async () => {
		const dialog = await theDialogFor({});

		expectALine(dialog, /Nick Brown/);
		expectALine(dialog, /The Full Monte/);
		expectALine(
			dialog,
			/(always held.*under-forecasting.*this product|this product.*always held.*under-forecasting)/i,
		);
		expectALine(dialog, /(95th.*this product|this product.*95th)/i);
	});
});
