import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { expect, type Mock, vi } from "vitest";
import SnackbarErrorHandler from "../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import type { Team } from "../models/Team/Team";
import TeamForecastView from "../pages/Teams/Detail/TeamForecastView";
import type { IApiServiceContext } from "../services/Api/ApiServiceContext";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import { createMockApiServiceContext } from "./MockApiServiceProvider";

/**
 * The Forecast Reality Check's answer, shaped exactly as it travels on the wire, for the Team Forecast
 * view's acceptance tests. The client has no model for it yet, so this describes the wire rather than
 * importing a type: if the client's own model ends up reading dates into `Date`s, this is the one place
 * the tests change.
 *
 * Every check ends on the same pinned day and reaches back by its own horizon; its history sits
 * immediately before what it scores.
 */

export const REALITY_CHECK_TODAY = "2026-09-22";

export const STANDARD_WINDOW_DAYS = [14, 30, 60, 90] as const;

export const HORIZON_DAYS = [7, 14, 28, 56] as const;

export const CONFIDENCE_LEVELS = [50, 70, 85, 95] as const;

export type Determination =
	| "AllWindowsAlike"
	| "SomeWindowsSound"
	| "NoWindowSound"
	| "NotEnoughEvidence";

export type Standing = "Inside" | "Outside" | "NotDetermined" | "NotTested";

export type NotTestedReason = "UsesFixedDates" | "NotAPositiveLength";

export type SufficiencyReason =
	| "Sufficient"
	| "TooFewActiveDays"
	| "DegenerateForecast";

export type CellOutcome = "OverForecast" | "WithinBand" | "UnderForecast";

export type LevelReading =
	| "SometimesHeld"
	| "NeverHeld"
	| "AlwaysHeld"
	| "NotEvaluated";

export interface RealityCheckWireCell {
	horizonDays: number;
	samplingWindowDays: number;
	scoredPeriodStart: string;
	scoredPeriodEnd: string;
	historyWindowStart: string;
	historyWindowEnd: string;
	sufficiency: {
		isSufficient: boolean;
		reason: SufficiencyReason;
		daysWithCompletedWork: number;
	};
	forecast: { probability: number; value: number }[] | null;
	actualCompleted: number | null;
	outcome: CellOutcome | null;
	levelOutcomes:
		| { confidenceLevel: number; forecastValue: number; held: boolean }[]
		| null;
}

export interface RealityCheckWireAnswer {
	teamId: number;
	teamName: string;
	anchorDate: string;
	standardWindowDays: number[];
	sampledWindowDays: number[];
	sampledHorizonDays: number[];
	confidenceLevels: number[];
	filterApplied: boolean;
	excludedSummary: string | null;
	minimumActiveDays: number;
	denominator: {
		runsAttempted: number;
		runsEvaluated: number;
		levelsPerRun: number;
		scoresEvaluated: number;
	};
	soundWindow: {
		soundWindowDays: number[];
		unevaluatedWindowDays: number[];
		determination: Determination;
		currentSettingDays: number;
		currentSettingWasTested: boolean;
		currentSettingStanding: Standing;
		currentSettingNotTestedReason: NotTestedReason | null;
	};
	levelCoverage: {
		confidenceLevel: number;
		heldCount: number;
		expectedHeldCount: number;
		reading: LevelReading;
	}[];
	cells: RealityCheckWireCell[];
}

export interface UnevaluableCheck {
	window: number;
	horizon: number;
	reason: Exclude<SufficiencyReason, "Sufficient">;
	daysWithCompletedWork: number;
}

export interface EvaluableCheck {
	window: number;
	horizon: number;
	at95: number;
	at85: number;
	at70: number;
	at50: number;
	actual: number;
}

export interface RealityCheckAnswerOptions {
	teamName?: string;
	currentSettingDays?: number;
	currentSettingWasTested?: boolean;
	standing?: Standing;
	/** Why the setting was not tested. Defaults to fixed dates whenever it was not, and to none when it was. */
	notTestedReason?: NotTestedReason;
	/** The windows none of whose checks could run. Defaults to exactly those, read off the checks. */
	unevaluatedWindowDays?: number[];
	determination?: Determination;
	soundWindowDays?: number[];
	/** How many of the evaluable checks each level held in; every other level is scored as held in none. */
	heldCounts?: Partial<Record<50 | 70 | 85 | 95, number>>;
	readings?: Partial<Record<50 | 70 | 85 | 95, LevelReading>>;
	unevaluable?: UnevaluableCheck[];
	checks?: EvaluableCheck[];
}

const shiftDay = (day: string, days: number): string => {
	const [year, month, date] = day.split("-").map(Number);
	const shifted = new Date(Date.UTC(year, month - 1, date + days));
	return shifted.toISOString().slice(0, 10);
};

const sweptWindows = (
	currentSettingDays: number,
	wasTested: boolean,
): number[] => {
	const windows = new Set<number>(STANDARD_WINDOW_DAYS);
	if (wasTested && currentSettingDays > 0) {
		windows.add(currentSettingDays);
	}
	return [...windows].sort((a, b) => a - b);
};

const aCell = (
	window: number,
	horizon: number,
	options: RealityCheckAnswerOptions,
): RealityCheckWireCell => {
	const scoredPeriodStart = shiftDay(REALITY_CHECK_TODAY, -horizon);
	const dates = {
		horizonDays: horizon,
		samplingWindowDays: window,
		scoredPeriodStart,
		scoredPeriodEnd: REALITY_CHECK_TODAY,
		historyWindowStart: shiftDay(scoredPeriodStart, -window),
		historyWindowEnd: scoredPeriodStart,
	};

	const unevaluable = options.unevaluable?.find(
		(check) => check.window === window && check.horizon === horizon,
	);
	if (unevaluable) {
		return {
			...dates,
			sufficiency: {
				isSufficient: false,
				reason: unevaluable.reason,
				daysWithCompletedWork: unevaluable.daysWithCompletedWork,
			},
			forecast: null,
			actualCompleted: null,
			outcome: null,
			levelOutcomes: null,
		};
	}

	const scale = horizon / 7;
	const check = options.checks?.find(
		(candidate) => candidate.window === window && candidate.horizon === horizon,
	) ?? {
		window,
		horizon,
		at95: Math.round(4 * scale),
		at85: Math.round(5 * scale),
		at70: Math.round(6 * scale),
		at50: Math.round(8 * scale),
		actual: Math.round(5.5 * scale),
	};
	const byLevel: Record<number, number> = {
		95: check.at95,
		85: check.at85,
		70: check.at70,
		50: check.at50,
	};

	let outcome: CellOutcome = "WithinBand";
	if (check.actual < check.at95) {
		outcome = "OverForecast";
	} else if (check.actual > check.at50) {
		outcome = "UnderForecast";
	}

	return {
		...dates,
		sufficiency: {
			isSufficient: true,
			reason: "Sufficient",
			daysWithCompletedWork: window,
		},
		forecast: CONFIDENCE_LEVELS.map((level) => ({
			probability: level,
			value: byLevel[level],
		})),
		actualCompleted: check.actual,
		outcome,
		levelOutcomes: CONFIDENCE_LEVELS.map((level) => ({
			confidenceLevel: level,
			forecastValue: byLevel[level],
			held: check.actual >= byLevel[level],
		})),
	};
};

/**
 * A complete answer: every window swept against every horizon, unevaluable checks included with their
 * reason, and a denominator and nominal-rate lines that agree with the checks.
 */
export const aRealityCheckAnswer = (
	options: RealityCheckAnswerOptions = {},
): RealityCheckWireAnswer => {
	const currentSettingDays = options.currentSettingDays ?? 30;
	const currentSettingWasTested = options.currentSettingWasTested ?? true;
	const sampledWindowDays = sweptWindows(
		currentSettingDays,
		currentSettingWasTested,
	);
	const cells = sampledWindowDays.flatMap((window) =>
		HORIZON_DAYS.map((horizon) => aCell(window, horizon, options)),
	);
	const runsEvaluated = cells.filter(
		(cell) => cell.sufficiency.isSufficient,
	).length;

	return {
		teamId: 42,
		teamName: options.teamName ?? "Ocean Explorer",
		anchorDate: REALITY_CHECK_TODAY,
		standardWindowDays: [...STANDARD_WINDOW_DAYS],
		sampledWindowDays,
		sampledHorizonDays: [...HORIZON_DAYS],
		confidenceLevels: [...CONFIDENCE_LEVELS],
		filterApplied: false,
		excludedSummary: null,
		minimumActiveDays: 5,
		denominator: {
			runsAttempted: cells.length,
			runsEvaluated,
			levelsPerRun: 4,
			scoresEvaluated: runsEvaluated * 4,
		},
		soundWindow: {
			soundWindowDays: options.soundWindowDays ?? sampledWindowDays,
			unevaluatedWindowDays:
				options.unevaluatedWindowDays ??
				sampledWindowDays.filter((window) =>
					cells
						.filter((cell) => cell.samplingWindowDays === window)
						.every((cell) => !cell.sufficiency.isSufficient),
				),
			determination: options.determination ?? "AllWindowsAlike",
			currentSettingDays,
			currentSettingWasTested,
			currentSettingStanding:
				options.standing ?? (currentSettingWasTested ? "Inside" : "NotTested"),
			currentSettingNotTestedReason: currentSettingWasTested
				? null
				: (options.notTestedReason ?? "UsesFixedDates"),
		},
		levelCoverage: CONFIDENCE_LEVELS.map((level) => ({
			confidenceLevel: level,
			heldCount: options.heldCounts?.[level] ?? 0,
			expectedHeldCount: (runsEvaluated * level) / 100,
			reading:
				options.readings?.[level] ??
				(runsEvaluated === 0 ? "NotEvaluated" : "SometimesHeld"),
		})),
		cells,
	};
};

export const OCEAN_EXPLORER: Team = {
	id: 42,
	name: "Ocean Explorer",
	workItemTypes: ["User Story", "Bug"],
} as Team;

/**
 * The Team Forecast tab with the check's request answered by `runRealityCheck`. Returns the Forecast
 * Backtesting group, where the check lives. The test file stands in for the group's other forecasters,
 * so every control left in the group is the check's own.
 */
export const renderTheForecastTab = (runRealityCheck: Mock): HTMLElement => {
	const forecastService = {
		runManualForecast: vi.fn(),
		runItemPrediction: vi.fn().mockResolvedValue({}),
		runBacktest: vi.fn().mockResolvedValue({}),
		runRealityCheck,
	};
	const teamMetricsService = {
		getForecastInputCandidates: vi.fn().mockResolvedValue({
			currentWipCount: 3,
			backlogCount: 12,
			features: [],
		}),
	};

	render(
		<SnackbarErrorHandler>
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({
					forecastService:
						forecastService as unknown as IApiServiceContext["forecastService"],
					teamMetricsService:
						teamMetricsService as unknown as IApiServiceContext["teamMetricsService"],
				})}
			>
				<TeamForecastView team={OCEAN_EXPLORER} />
			</ApiServiceContext.Provider>
		</SnackbarErrorHandler>,
	);

	return screen.getByRole("region", { name: "Forecast Backtesting" });
};

export const pressRunRealityCheck = async (group: HTMLElement) => {
	await userEvent.click(
		within(group).getByRole("button", { name: /^run reality check$/i }),
	);
};

/**
 * The group once the check has answered, recognised by the first line the answer puts on screen.
 */
export const theAnswerIn = async (
	runRealityCheck: Mock,
	answer: RealityCheckWireAnswer,
	onceItReads: RegExp = /forecast runs were checked/i,
): Promise<HTMLElement> => {
	runRealityCheck.mockResolvedValue(answer);
	const group = renderTheForecastTab(runRealityCheck);
	await pressRunRealityCheck(group);
	await waitFor(() => expectALine(group, onceItReads));
	return group;
};

export const expectALine = (group: HTMLElement, pattern: RegExp) => {
	expect(
		linesMatching(group, pattern),
		`no line reads ${pattern}`,
	).not.toHaveLength(0);
};

/**
 * The deepest elements whose text matches: the line a reader would point at, not every ancestor that
 * happens to contain it.
 */
export const linesMatching = (
	container: HTMLElement,
	pattern: RegExp,
): HTMLElement[] =>
	Array.from(container.querySelectorAll<HTMLElement>("*")).filter(
		(element) =>
			pattern.test(element.textContent ?? "") &&
			!Array.from(element.children).some((child) =>
				pattern.test(child.textContent ?? ""),
			),
	);
