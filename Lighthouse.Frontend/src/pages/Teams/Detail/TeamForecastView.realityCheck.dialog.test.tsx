import { act, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { createMockTeamService } from "../../../tests/MockApiServiceProvider";
import {
	aRealityCheckAnswer,
	expectALine,
	expectTheLine,
	HORIZON_DAYS,
	LEVEL_COLUMNS,
	levelCell,
	linesMatching,
	linesReading,
	periodGroup,
	periodGroupsOf,
	periodHeaderOf,
	periodHeaderText,
	pressRunRealityCheck,
	type RealityCheckWireAnswer,
	renderTheForecastTab,
	STANDARD_WINDOW_DAYS,
	setMatchMedia,
	theDialogWithTheAnswer,
	thePeriodChecked,
	theRealityCheckDialog,
	theTableIn,
	windowDaysOf,
	windowHeaderText,
	windowRow,
	windowRowsOf,
} from "../../../tests/RealityCheckFixture";
import { dayInWords } from "./realityCheckCopy";

/**
 * The Forecast Reality Check once its answer opens in a dialog: the button stays in
 * the Forecast Backtesting group, the dialog opens at once, and fills in with the answer in words - one
 * line per confidence level first - and beneath them a table of every check, grouped by period, each
 * period's actual printed once and each forecast beside it with its miss in Work Items and whether it held.
 *
 * What the specs need from the markup, and nothing more: the dialog is named for the reality check; the
 * table is a real table, one row group per period headed by a row-group header, one row per sampling window
 * headed by a row header that starts "N days"; the period's first and last day are `time` elements; a
 * graded cell's accessible name reads level, forecast, held or not, and the miss in words. The visible miss
 * carries a sign - "+2", "0", and the typographic minus "−6" (U+2212), never a hyphen.
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

const MINUS = "−";

const runRealityCheck = vi.fn();

const theDialogFor = (answer: RealityCheckWireAnswer) =>
	theDialogWithTheAnswer(runRealityCheck, answer);

const anAnswerStillOnItsWay = () => {
	let answer: (value: unknown) => void = () => {};
	runRealityCheck.mockReturnValue(
		new Promise((resolve) => {
			answer = resolve;
		}),
	);
	return (value: unknown) => act(async () => answer(value));
};

/** The eight-week group of the stories: Ocean Explorer delivered 42, its forecasts were 48 / 40 / 36 / 31. */
const oceanExplorerOverEightWeeks = () =>
	aRealityCheckAnswer({
		checks: thePeriodChecked(56, {
			at95: 31,
			at85: 36,
			at70: 40,
			at50: 48,
			actual: 42,
		}),
	});

const fourChecksOfTheWindow = (
	window: number,
	reason: "TooFewActiveDays" | "DegenerateForecast",
	daysWithCompletedWork: number,
) =>
	HORIZON_DAYS.map((horizon) => ({
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

const nothingCouldBeChecked = () =>
	aRealityCheckAnswer({
		soundWindowDays: [],
		determination: "NotEnoughEvidence",
		standing: "NotDetermined",
		unevaluatedWindowDays: [...STANDARD_WINDOW_DAYS],
		readings: {
			50: "NotEvaluated",
			70: "NotEvaluated",
			85: "NotEvaluated",
			95: "NotEvaluated",
		},
		unevaluable: STANDARD_WINDOW_DAYS.flatMap((window) =>
			fourChecksOfTheWindow(window, "TooFewActiveDays", 1),
		),
		periodActuals: { 7: 1, 14: 1, 28: 1, 56: 2 },
	});

const follows = (earlier: Element, later: Element): boolean =>
	(earlier.compareDocumentPosition(later) &
		Node.DOCUMENT_POSITION_FOLLOWING) !==
	0;

const theButtonNamed = (container: HTMLElement, name: RegExp) =>
	within(container).getByRole("button", { name });

const closeWithEscape = async () => {
	await userEvent.keyboard("{Escape}");
	await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
};

beforeEach(() => {
	vi.clearAllMocks();
	setMatchMedia(false);
	terms.clear();
	terms.set(TERMINOLOGY_KEYS.TEAM, "Team");
	terms.set(TERMINOLOGY_KEYS.WORK_ITEMS, "Work Items");
	terms.set(TERMINOLOGY_KEYS.WORK_ITEM, "Work Item");
});

describe("@us-04 @slice-04 @driving_port the answer opens in a dialog, words first", () => {
	it(`pressing Run reality check opens the dialog at once, saying the check is running, and it fills in without asking for a date`, async () => {
		const answerArrives = anAnswerStillOnItsWay();
		const group = renderTheForecastTab(runRealityCheck);

		await pressRunRealityCheck(group);
		const dialog = await theRealityCheckDialog();

		expect(within(dialog).getByRole("status")).toHaveTextContent(
			/checking Ocean Explorer's forecasts against what happened/i,
		);
		await answerArrives(aRealityCheckAnswer());
		await waitFor(() => expectALine(dialog, /16 forecast runs were checked/i));
		expect(runRealityCheck).toHaveBeenCalledTimes(1);
		expect(runRealityCheck.mock.calls[0][0]).toBe(42);
		expect(
			runRealityCheck.mock.calls[0].some(
				(argument: unknown) => argument instanceof Date,
			),
		).toBe(false);
		expect(within(dialog).queryAllByRole("textbox")).toHaveLength(0);
	});

	it(`a Team with a forecast filter runs the check with the backtest's filter choice, which is only known once the Team's settings have come in`, async () => {
		const teamService = createMockTeamService();
		const settings = Promise.resolve({
			forecastFilterRuleSetJson: JSON.stringify({ conditions: [{}] }),
		} as ITeamSettings);
		vi.mocked(teamService.getTeamSettings).mockReturnValue(settings);
		runRealityCheck.mockResolvedValue(aRealityCheckAnswer());
		const group = renderTheForecastTab(runRealityCheck, teamService);
		await act(async () => {
			await settings;
		});

		await pressRunRealityCheck(group);
		const dialog = await theRealityCheckDialog();
		await waitFor(() => expectALine(dialog, /16 forecast runs were checked/i));

		expect(runRealityCheck).toHaveBeenCalledWith(42, true);
	});

	it(`opens on one line per confidence level - held against should-have-held - then the window sentence, the two findings and the denominator, in that order and above the table`, async () => {
		const dialog = await theDialogFor(
			aRealityCheckAnswer({ heldCounts: { 50: 8, 70: 11, 85: 14, 95: 15 } }),
		);

		expectTheLine(
			dialog,
			"50th: held 8 of 16 (should be about 8), within 10% in 0. Usually high by more than a quarter.",
		);
		expectTheLine(
			dialog,
			"70th: held 11 of 16 (should be about 11), within 10% in 16. Usually high by up to 10%.",
		);
		expectTheLine(
			dialog,
			"85th: held 14 of 16 (should be about 14), within 10% in 12. Usually within 10%.",
		);
		expectTheLine(
			dialog,
			"95th: held 15 of 16 (should be about 15), within 10% in 0. Usually low by more than a quarter.",
		);

		const inReadingOrder = [
			linesReading(
				dialog,
				"50th: held 8 of 16 (should be about 8), within 10% in 0. Usually high by more than a quarter.",
			)[0],
			linesReading(
				dialog,
				"95th: held 15 of 16 (should be about 15), within 10% in 0. Usually low by more than a quarter.",
			)[0],
			linesMatching(dialog, /between 14 and 90 days/i)[0],
			linesMatching(dialog, /sampling window is a setting on this Team/i)[0],
			linesMatching(dialog, /confidence level is not a setting/i)[0],
			linesMatching(dialog, /16 forecast runs were checked/i)[0],
			theTableIn(dialog),
		];
		for (const [index, line] of inReadingOrder.entries()) {
			expect(line, `line ${index} is missing`).toBeDefined();
		}
		for (let index = 1; index < inReadingOrder.length; index++) {
			expect(
				follows(inReadingOrder[index - 1], inReadingOrder[index]),
				`line ${index} comes after line ${index - 1}`,
			).toBe(true);
		}
		expect(linesMatching(dialog, /\bbeaten\b|about right/i)).toHaveLength(0);
	});

	it(`@error a level that never held reads as over-forecasting and one that always held when misses were expected reads as under-forecasting`, async () => {
		const dialog = await theDialogFor(
			aRealityCheckAnswer({
				heldCounts: { 50: 0, 70: 9, 85: 16, 95: 16 },
				readings: { 50: "NeverHeld", 85: "AlwaysHeld" },
			}),
		);

		expectTheLine(
			dialog,
			"50th: held 0 of 16 (should be about 8), within 10% in 0 — it never held, which is over-forecasting. Usually high by more than a quarter.",
		);
		expectTheLine(
			dialog,
			"85th: held 16 of 16 (should be about 14), within 10% in 12 — it held every time, which is under-forecasting. Usually within 10%.",
		);
		expectTheLine(
			dialog,
			"95th: held 16 of 16 (should be about 15), within 10% in 0. Usually low by more than a quarter.",
		);
	});

	it(`@error a Team whose history supports no check is told in words that no level was tested, and the table still stands`, async () => {
		const dialog = await theDialogFor(nothingCouldBeChecked());

		for (const level of LEVEL_COLUMNS) {
			expectTheLine(
				dialog,
				`${level}th: no check could be run, so this level was not tested.`,
			);
		}
		expectALine(
			dialog,
			/(16|sixteen) of the (16|sixteen) checks could not run/i,
		);
		expect(periodGroupsOf(theTableIn(dialog))).toHaveLength(4);
	});

	it(`keeps nothing behind a toggle, a tooltip or a disclosure`, async () => {
		const dialog = await theDialogFor(aRealityCheckAnswer());

		const denominator = linesMatching(dialog, /16 forecast runs were checked/i);
		expect(denominator[0]).toBeVisible();
		expect(denominator[0].closest('[role="tooltip"]')).toBeNull();
		expect(
			within(dialog)
				.queryAllByRole("button")
				.filter((button) => button.hasAttribute("aria-expanded")),
		).toHaveLength(0);
		expect(
			within(dialog).queryByRole("button", { name: /evidence/i }),
		).toBeNull();
	});
});

describe("@us-04 @slice-04 every forecast next to what the Team delivered", () => {
	it.each([
		{ ownWindow: 30, rows: 16, windows: [14, 30, 60, 90] },
		{ ownWindow: 45, rows: 20, windows: [14, 30, 45, 60, 90] },
	])(
		`@kpi-OUT-6094-how-far-each-forecast-landed has one row per check - $rows - grouped by period in horizon order, the windows in ladder order in every group`,
		async ({ ownWindow, rows, windows }) => {
			const dialog = await theDialogFor(
				aRealityCheckAnswer({ currentSettingDays: ownWindow }),
			);
			const table = theTableIn(dialog);

			const groups = periodGroupsOf(table);
			expect(groups.map(periodHeaderText)).toEqual([
				expect.stringMatching(/\b1 week\b/),
				expect.stringMatching(/\b2 weeks\b/),
				expect.stringMatching(/\b4 weeks\b/),
				expect.stringMatching(/\b8 weeks\b/),
			]);
			for (const group of groups) {
				expect(windowRowsOf(group).map(windowDaysOf)).toEqual(windows);
			}
			expect(groups.flatMap(windowRowsOf)).toHaveLength(rows);
		},
	);

	it(`@kpi-OUT-6094-how-far-each-forecast-landed prints each period's actual once, in its header, with the period's first and last day`, async () => {
		const answer = oceanExplorerOverEightWeeks();
		const dialog = await theDialogFor(answer);
		const eightWeeks = periodGroup(theTableIn(dialog), 56);
		const period = answer.scoredPeriods[3];

		expect(periodHeaderText(eightWeeks)).toMatch(
			/(^|\D)42 Work Items completed\b/,
		);
		expect(
			Array.from(periodHeaderOf(eightWeeks).querySelectorAll("time")).map(
				(day) => day.getAttribute("datetime"),
			),
		).toEqual([period.scoredPeriodStart, period.scoredPeriodEnd]);
		expect(linesMatching(eightWeeks, /Work Items completed/)).toHaveLength(1);
	});

	it(`@kpi-OUT-6094-how-far-each-forecast-landed heads each period with its length, its first to its last day, and what was completed`, async () => {
		const answer = oceanExplorerOverEightWeeks();
		const dialog = await theDialogFor(answer);
		const period = answer.scoredPeriods[3];

		expect(periodHeaderText(periodGroup(theTableIn(dialog), 56))).toBe(
			`8 weeks, ${dayInWords(period.scoredPeriodStart)} to ${dayInWords(period.scoredPeriodEnd)}: 42 Work Items completed`,
		);
	});

	it(`@kpi-OUT-6094-how-far-each-forecast-landed shows every forecast with its value, its miss in Work Items and whether it held - Ocean Explorer's 30-day row over 8 weeks`, async () => {
		const dialog = await theDialogFor(oceanExplorerOverEightWeeks());
		const row = windowRow(periodGroup(theTableIn(dialog), 56), 30);

		expect(levelCell(row, 50)).toHaveAccessibleName(
			"50th: 48, did not hold, 6 fewer delivered, 14% of the actual",
		);
		expect(levelCell(row, 70)).toHaveAccessibleName(
			"70th: 40, held, 2 more delivered, 5% of the actual",
		);
		expect(levelCell(row, 85)).toHaveAccessibleName(
			"85th: 36, held, 6 more delivered, 14% of the actual",
		);
		expect(levelCell(row, 95)).toHaveAccessibleName(
			"95th: 31, held, 11 more delivered, 26% of the actual",
		);

		expect(within(levelCell(row, 50)).getByText("48")).toBeInTheDocument();
		expect(
			within(levelCell(row, 50)).getByText(`${MINUS}6`),
		).toBeInTheDocument();
		expect(levelCell(row, 50)).toHaveTextContent("✗");
		expect(levelCell(row, 50)).toHaveTextContent(/did not hold/);
		expect(within(levelCell(row, 70)).getByText("+2")).toBeInTheDocument();
		expect(levelCell(row, 70)).toHaveTextContent("✓");
		expect(levelCell(row, 70)).not.toHaveTextContent(/did not hold/);
		expect(within(levelCell(row, 95)).getByText("+11")).toBeInTheDocument();
	});

	it(`puts each forecast under its own level's column, whatever order the check lists its levels in`, async () => {
		const answer = oceanExplorerOverEightWeeks();
		const dialog = await theDialogFor({
			...answer,
			cells: answer.cells.map((cell) => ({
				...cell,
				levelOutcomes:
					cell.levelOutcomes === null
						? null
						: [...cell.levelOutcomes].reverse(),
			})),
		});
		const row = windowRow(periodGroup(theTableIn(dialog), 56), 30);

		expect(levelCell(row, 50)).toHaveAccessibleName(
			"50th: 48, did not hold, 6 fewer delivered, 14% of the actual",
		);
		expect(levelCell(row, 95)).toHaveAccessibleName(
			"95th: 31, held, 11 more delivered, 26% of the actual",
		);
	});

	it(`@error a level the check left out stays an empty cell, and every other forecast keeps its own column`, async () => {
		const answer = oceanExplorerOverEightWeeks();
		const dialog = await theDialogFor({
			...answer,
			cells: answer.cells.map((cell) =>
				cell.horizonDays === 56 && cell.samplingWindowDays === 30
					? {
							...cell,
							levelOutcomes: (cell.levelOutcomes ?? []).filter(
								(outcome) => outcome.confidenceLevel !== 70,
							),
						}
					: cell,
			),
		});
		const row = windowRow(periodGroup(theTableIn(dialog), 56), 30);

		expect(within(row).getAllByRole("cell")).toHaveLength(4);
		expect(levelCell(row, 70)).toBeEmptyDOMElement();
		expect(levelCell(row, 70)).not.toHaveAttribute("aria-label");
		expect(levelCell(row, 50)).toHaveAccessibleName(
			"50th: 48, did not hold, 6 fewer delivered, 14% of the actual",
		);
		expect(levelCell(row, 85)).toHaveAccessibleName(
			"85th: 36, held, 6 more delivered, 14% of the actual",
		);
		expect(levelCell(row, 95)).toHaveAccessibleName(
			"95th: 31, held, 11 more delivered, 26% of the actual",
		);
	});

	it(`@boundary a forecast the Team delivered exactly shows a miss of 0 and held`, async () => {
		const dialog = await theDialogFor(
			aRealityCheckAnswer({
				checks: [
					{
						window: 30,
						horizon: 28,
						at95: 18,
						at85: 20,
						at70: 22,
						at50: 26,
						actual: 22,
					},
				],
			}),
		);
		const cell = levelCell(
			windowRow(periodGroup(theTableIn(dialog), 28), 30),
			70,
		);

		expect(cell).toHaveAccessibleName(
			"70th: 22, held, exactly as forecast, 0% of the actual",
		);
		expect(within(cell).getByText("0")).toBeInTheDocument();
		expect(cell).not.toHaveTextContent(/[+−-]0\b/);
	});

	it(`@error a check that could not run says why across all four level columns, never blank and never graded - Coastal Survey's 14-day row over 2 weeks`, async () => {
		const dialog = await theDialogFor(coastalSurvey());
		const row = windowRow(periodGroup(theTableIn(dialog), 14), 14);

		const cells = within(row).getAllByRole("cell");
		expect(cells).toHaveLength(1);
		expect(cells[0]).toHaveAttribute("colspan", "4");
		expectTheLine(
			row,
			"Not enough history in this window to check: 3 days with completed Work Items, 5 needed.",
		);
		expect(row).not.toHaveTextContent(/[✓✗]/);
	});

	it(`@error a check that could not run is set apart from the graded ones in muted italics`, async () => {
		const dialog = await theDialogFor(coastalSurvey());
		const [reason] = within(
			windowRow(periodGroup(theTableIn(dialog), 14), 14),
		).getAllByRole("cell");

		expect(reason).toHaveStyle({
			fontStyle: "italic",
			color: "rgba(0, 0, 0, 0.6)",
		});
	});

	it(`@error a check whose forecast could not be worked out gives its own reason, not the thin-history one`, async () => {
		const dialog = await theDialogFor(
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
		const row = windowRow(periodGroup(theTableIn(dialog), 28), 30);

		expectTheLine(
			row,
			"No forecast could be worked out from the history in this window.",
		);
		expect(row).not.toHaveTextContent(/days with completed/i);
		expect(row).not.toHaveTextContent(/[✓✗]/);
	});

	it(`@error @kpi-OUT-6094-how-far-each-forecast-landed a period in which no window could be checked still shows what the Team delivered, and every row its reason`, async () => {
		const dialog = await theDialogFor(
			aRealityCheckAnswer({
				unevaluable: STANDARD_WINDOW_DAYS.map((window) => ({
					window,
					horizon: 7,
					reason: "TooFewActiveDays" as const,
					daysWithCompletedWork: 2,
				})),
				periodActuals: { 7: 3 },
			}),
		);
		const oneWeek = periodGroup(theTableIn(dialog), 7);

		expect(periodHeaderText(oneWeek)).toMatch(/(^|\D)3 Work Items completed\b/);
		const rows = windowRowsOf(oneWeek);
		expect(rows).toHaveLength(4);
		for (const row of rows) {
			expect(row).toHaveTextContent(/not enough history in this window/i);
		}
	});

	it(`the Team's own window is called "your setting" in every period, and no other row is`, async () => {
		const dialog = await theDialogFor(
			aRealityCheckAnswer({ currentSettingDays: 45 }),
		);

		for (const group of periodGroupsOf(theTableIn(dialog))) {
			const labelled = windowRowsOf(group).filter((row) =>
				/your setting/i.test(windowHeaderText(row)),
			);
			expect(labelled.map(windowDaysOf)).toEqual([45]);
		}
	});

	it(`@error a Team whose own setting was not tested has no row called "your setting"`, async () => {
		const dialog = await theDialogFor(
			aRealityCheckAnswer({
				currentSettingDays: 45,
				currentSettingWasTested: false,
				standing: "NotTested",
				notTestedReason: "UsesFixedDates",
			}),
		);

		expect(linesMatching(dialog, /your setting/i)).toHaveLength(0);
	});

	it(`@kpi-OUT-6094-no-window-ranked keeps the order the check was run in, even when the checks arrive shuffled`, async () => {
		const dialog = await theDialogFor(
			aRealityCheckAnswer({
				currentSettingDays: 45,
				cellOrder: (cells) => [...cells].reverse(),
			}),
		);
		const groups = periodGroupsOf(theTableIn(dialog));

		expect(groups.map(periodHeaderText)[0]).toMatch(/\b1 week\b/);
		expect(groups.map(periodHeaderText)[3]).toMatch(/\b8 weeks\b/);
		for (const group of groups) {
			expect(windowRowsOf(group).map(windowDaysOf)).toEqual([
				14, 30, 45, 60, 90,
			]);
		}
	});

	it(`heads the four level columns 50th, 70th, 85th and 95th, each with its confidence name`, async () => {
		const dialog = await theDialogFor(aRealityCheckAnswer());

		const levelHeaders = within(theTableIn(dialog))
			.getAllByRole("columnheader")
			.map((header) => (header.textContent ?? "").trim())
			.filter((text) => /^\d+th\b/.test(text));
		expect(levelHeaders).toEqual([
			expect.stringMatching(/^50th\b.*Risky/),
			expect.stringMatching(/^70th\b.*Realistic/),
			expect.stringMatching(/^85th\b.*Confident/),
			expect.stringMatching(/^95th\b.*Certain/),
		]);
	});
});

describe("@us-04 @slice-04 the dialog holds to what the check promised", () => {
	it(`is a real table: a caption naming the Team, a header for each period and one for each window`, async () => {
		const dialog = await theDialogFor(aRealityCheckAnswer());
		const table = theTableIn(dialog);

		expect(table.querySelector("caption")).not.toBeNull();
		expect(table).toHaveAccessibleName(/Ocean Explorer/);
		expect(table.querySelectorAll('th[scope="rowgroup"]')).toHaveLength(4);
		expect(table.querySelectorAll('th[scope="row"]')).toHaveLength(16);
	});

	it(`@kpi-OUT-6094-no-window-ranked says nothing that tallies, orders or picks out a sampling window, and offers no way to sort the table`, async () => {
		const dialog = await theDialogFor(
			aRealityCheckAnswer({ heldCounts: { 50: 8, 70: 11, 85: 14, 95: 15 } }),
		);
		const table = theTableIn(dialog);

		expect(
			linesMatching(
				dialog,
				/\b(best|recommend(ed)?|optimal|winner|most accurate|least accurate)\b/i,
			),
		).toHaveLength(0);
		expect(linesMatching(dialog, /^\d+th: .*\bdays?\b/)).toHaveLength(0);
		expect(table.querySelectorAll("[aria-sort]")).toHaveLength(0);
		expect(within(table).queryAllByRole("button")).toHaveLength(0);
	});

	it(`@kpi-OUT-4172-read-only offers no control that could change a Team setting`, async () => {
		const dialog = await theDialogFor(aRealityCheckAnswer());

		for (const role of [
			"textbox",
			"spinbutton",
			"combobox",
			"checkbox",
			"switch",
			"radio",
			"slider",
		]) {
			expect(within(dialog).queryAllByRole(role), role).toHaveLength(0);
		}
		for (const button of within(dialog).getAllByRole("button")) {
			expect(button).toHaveAccessibleName(/^(run again|close)$/i);
		}
	});

	it(`speaks the instance's own words for Team and Work Item, and no tracker's`, async () => {
		terms.set(TERMINOLOGY_KEYS.TEAM, "Squad");
		terms.set(TERMINOLOGY_KEYS.WORK_ITEMS, "Tickets");
		terms.set(TERMINOLOGY_KEYS.WORK_ITEM, "Ticket");

		const dialog = await theDialogFor(coastalSurvey());

		expect(periodHeaderText(periodGroup(theTableIn(dialog), 56))).toMatch(
			/Tickets completed/,
		);
		expectALine(dialog, /setting on this Squad/);
		expect(linesMatching(dialog, /work items?/i)).toHaveLength(0);
		expect(
			linesMatching(dialog, /\b(throughput|Epic|Initiative|Story)\b/i),
		).toHaveLength(0);
	});

	it(`the Forecast Backtesting group keeps only the button once the dialog is closed`, async () => {
		runRealityCheck.mockResolvedValue(aRealityCheckAnswer());
		const group = renderTheForecastTab(runRealityCheck);
		await pressRunRealityCheck(group);
		const dialog = await theRealityCheckDialog();
		await waitFor(() => expectALine(dialog, /forecast runs were checked/i));

		await closeWithEscape();

		expect(linesMatching(group, /forecast runs were checked/i)).toHaveLength(0);
		expect(linesMatching(group, /\d+th: held/)).toHaveLength(0);
		expect(within(group).queryAllByRole("table")).toHaveLength(0);
		expect(
			within(group)
				.getAllByRole("button")
				.map((button) => button.textContent?.trim()),
		).toEqual(["Run reality check"]);
	});
});

describe("@us-04 @slice-04 running again, closing and reopening", () => {
	it(`Run again asks again and the dialog fills in with the fresh answer`, async () => {
		const dialog = await theDialogFor(
			aRealityCheckAnswer({ heldCounts: { 50: 8 } }),
		);
		runRealityCheck.mockResolvedValue(
			aRealityCheckAnswer({ heldCounts: { 50: 9 } }),
		);

		await userEvent.click(theButtonNamed(dialog, /^run again$/i));

		await waitFor(() =>
			expectTheLine(
				dialog,
				"50th: held 9 of 16 (should be about 8), within 10% in 0. Usually high by more than a quarter.",
			),
		);
		expect(runRealityCheck).toHaveBeenCalledTimes(2);
	});

	it(`closing and pressing Run reality check again asks again`, async () => {
		runRealityCheck.mockResolvedValue(aRealityCheckAnswer());
		const group = renderTheForecastTab(runRealityCheck);
		await pressRunRealityCheck(group);
		const first = await theRealityCheckDialog();
		await waitFor(() => expectALine(first, /forecast runs were checked/i));
		await closeWithEscape();

		await pressRunRealityCheck(group);
		const second = await theRealityCheckDialog();
		await waitFor(() => expectALine(second, /forecast runs were checked/i));

		expect(runRealityCheck).toHaveBeenCalledTimes(2);
	});

	it(`@error while a check is running Run again cannot be pressed and reopening starts no second check`, async () => {
		anAnswerStillOnItsWay();
		const group = renderTheForecastTab(runRealityCheck);
		await pressRunRealityCheck(group);
		const dialog = await theRealityCheckDialog();

		expect(theButtonNamed(dialog, /^run again$/i)).toBeDisabled();
		await closeWithEscape();
		await pressRunRealityCheck(group);
		const reopened = await theRealityCheckDialog();

		expect(within(reopened).getByRole("status")).toBeInTheDocument();
		expect(theButtonNamed(reopened, /^run again$/i)).toBeDisabled();
		expect(runRealityCheck).toHaveBeenCalledTimes(1);
	});

	it(`@error a check that fails to come back leaves the dialog open with a plain message and Run again, and no answer`, async () => {
		runRealityCheck.mockRejectedValue(
			new Error("The reality check could not be run"),
		);
		const group = renderTheForecastTab(runRealityCheck);

		await pressRunRealityCheck(group);
		const dialog = await theRealityCheckDialog();

		expect(await within(dialog).findByRole("alert")).toHaveTextContent(
			"The reality check could not be run",
		);
		expect(theButtonNamed(dialog, /^run again$/i)).toBeInTheDocument();
		expect(linesMatching(dialog, /forecast runs were checked/i)).toHaveLength(
			0,
		);
		expect(within(dialog).queryByRole("table")).toBeNull();
	});

	it(`@error a failure that carries no message of its own still says plainly that the check could not run`, async () => {
		runRealityCheck.mockRejectedValue({ status: 503 });
		const group = renderTheForecastTab(runRealityCheck);

		await pressRunRealityCheck(group);
		const dialog = await theRealityCheckDialog();

		expect(await within(dialog).findByRole("alert")).toHaveTextContent(
			/^The reality check could not be run\. Please try again\.$/,
		);
	});

	it(`@error Run again after a failure can still bring the answer`, async () => {
		runRealityCheck
			.mockRejectedValueOnce(new Error("The reality check could not be run"))
			.mockResolvedValueOnce(aRealityCheckAnswer());
		const group = renderTheForecastTab(runRealityCheck);
		await pressRunRealityCheck(group);
		const dialog = await theRealityCheckDialog();
		await within(dialog).findByText(/the reality check could not be run/i);

		await userEvent.click(theButtonNamed(dialog, /^run again$/i));

		await waitFor(() => expectALine(dialog, /16 forecast runs were checked/i));
		expect(
			within(dialog).queryByText(/the reality check could not be run/i),
		).toBeNull();
	});
});

describe("@us-04 @slice-04 the dialog from the keyboard and on a small screen", () => {
	it(`Escape closes the dialog and puts focus back on Run reality check`, async () => {
		runRealityCheck.mockResolvedValue(aRealityCheckAnswer());
		const group = renderTheForecastTab(runRealityCheck);
		const button = theButtonNamed(group, /^run reality check$/i);
		button.focus();

		await userEvent.keyboard("{Enter}");
		const dialog = await theRealityCheckDialog();
		await waitFor(() => expectALine(dialog, /forecast runs were checked/i));
		await closeWithEscape();

		await waitFor(() => expect(button).toHaveFocus());
	});

	it(`the visible close control closes it too`, async () => {
		const dialog = await theDialogFor(aRealityCheckAnswer());

		await userEvent.click(
			within(dialog).getAllByRole("button", { name: /^close$/i })[0],
		);

		await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
	});

	it(`focus moves into the dialog when it opens, and the answer arriving moves it nowhere`, async () => {
		const answerArrives = anAnswerStillOnItsWay();
		const group = renderTheForecastTab(runRealityCheck);
		await pressRunRealityCheck(group);
		const dialog = await theRealityCheckDialog();
		await waitFor(() =>
			expect(dialog).toContainElement(document.activeElement as HTMLElement),
		);
		const focusedWhileRunning = document.activeElement;

		await answerArrives(aRealityCheckAnswer());
		await waitFor(() => expectALine(dialog, /forecast runs were checked/i));

		expect(document.activeElement).toBe(focusedWhileRunning);
	});

	it(`the table's scrolling region takes keyboard focus and is named by the table's caption`, async () => {
		const dialog = await theDialogFor(aRealityCheckAnswer());

		const region = within(dialog).getByRole("region", {
			name: /^Every forecast checked for Ocean Explorer\b/,
		});
		expect(region).toHaveAttribute("tabindex", "0");
		expect(region).toContainElement(theTableIn(dialog));
	});

	it(`on a narrow screen the dialog takes the whole screen and the table drops nothing`, async () => {
		setMatchMedia(true);

		const dialog = await theDialogFor(aRealityCheckAnswer());
		const table = theTableIn(dialog);

		expect(dialog).toHaveClass("MuiDialog-paperFullScreen");
		expect(table.querySelectorAll('th[scope="row"]')).toHaveLength(16);
		for (const row of periodGroupsOf(table).flatMap(windowRowsOf)) {
			expect(within(row).getAllByRole("cell")).toHaveLength(4);
		}
	});
});
