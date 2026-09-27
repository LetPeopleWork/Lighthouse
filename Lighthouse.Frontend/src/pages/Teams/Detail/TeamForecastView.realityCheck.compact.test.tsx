import { act, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { ForecastGrade } from "../../../models/Forecasts/RealityCheckResult";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import {
	aRealityCheckAnswer,
	LEVEL_COLUMNS,
	levelCell,
	linesMatching,
	linesReading,
	periodGroup,
	periodGroupsOf,
	periodHeaderText,
	pressRunRealityCheck,
	type RealityCheckWireAnswer,
	readingOf,
	renderTheForecastTab,
	setMatchMedia,
	tabUntilFocused,
	theDialogWithTheAnswer,
	thePeriodChecked,
	theRealityCheckDialog,
	theTableIn,
	windowRow,
	windowRowsOf,
} from "../../../tests/RealityCheckFixture";
import { appColors } from "../../../utils/theme/colors";

/**
 * The reality check reads at a glance, starting with its table. A cell shows only
 * whether the forecast held and the forecast itself, on its grade colour; how it compared with what was
 * completed is one hover or one keyboard focus away, and that same sentence is the cell's accessible
 * name. Each period is one line with its dates in the reader's own numeric format; the legend is two
 * titled rows; the caption is gone from view but still names the scrolling region; and while a check runs
 * the dialog says "Crunching the numbers…".
 *
 * What the specs need from the markup, and nothing more: a graded or "—" cell is the table cell itself,
 * focusable, and wrapped by the tooltip that names it; the loading state is a `status` region holding a
 * progress indicator; the scrolling region is a `region` named "Forecasts checked for {Team name}, by
 * forecast horizon and sampling window"; the period day is formatted through
 * `Date.prototype.toLocaleDateString`, as the back-test date pickers get their digits, which is where
 * these specs pin the reader's locale.
 */

const PENDING = "06a: the compact table is not built yet";

const LOADING = "Crunching the numbers…";

const MINUS = "−";

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

const toLocaleDateStringAsShipped = Date.prototype.toLocaleDateString;

/**
 * The hour of every day the dialog formatted. A calendar day read in the reader's own time zone is local
 * midnight; one read through UTC is two hours past it in Zurich, where the suite runs, yet still prints
 * as the same day there, so the printed digits alone cannot tell the two apart.
 */
const hoursOfTheDaysFormatted: number[] = [];

/** The reader's browser formats days in `locale`, whatever locale the call itself names. */
const aReaderWhoseBrowserUses = (locale: string) => {
	vi.spyOn(Date.prototype, "toLocaleDateString").mockImplementation(function (
		this: Date,
		_locales?: Intl.LocalesArgument,
		options?: Intl.DateTimeFormatOptions,
	) {
		hoursOfTheDaysFormatted.push(this.getHours());
		return toLocaleDateStringAsShipped.call(this, locale, options);
	});
};

const gradeFill = (grade: ForecastGrade): string =>
	appColors.forecastGrade[grade];

/** The colour a browser reports for a fill, so a hex token can be compared with a computed style. */
const asComputed = (fill: string): string => {
	const probe = document.createElement("div");
	probe.style.backgroundColor = fill;
	return probe.style.backgroundColor;
};

/** Ocean Explorer's week of the story: 14 completed, its 14-day forecasts 21 / 17 / 12 / 9. */
const oceanExplorersWeek = () =>
	aRealityCheckAnswer({
		checks: [
			{
				window: 14,
				horizon: 7,
				at95: 9,
				at85: 12,
				at70: 17,
				at50: 21,
				actual: 14,
			},
		],
	});

const theFirstGradedCell = (dialog: HTMLElement): HTMLElement =>
	levelCell(windowRow(periodGroup(theTableIn(dialog), 7), 14), 50);

/** The sixty-four evaluable cells of the standard ladder, in reading order. */
const everyGradedCell = (dialog: HTMLElement): HTMLElement[] =>
	periodGroupsOf(theTableIn(dialog))
		.flatMap(windowRowsOf)
		.flatMap((row) => within(row).getAllByRole("cell"));

const MORE =
	/^Closed \d+ Work Items? more \(\d+\) than forecasted \(\d+\)\. Forecast off by \+\d+%$/;
const FEWER =
	/^Closed \d+ Work Items? fewer \(\d+\) than forecasted \(\d+\)\. Forecast off by −\d+%$/;
const EXACTLY = /^Closed exactly the forecasted \d+ Work Items?\.$/;
const NOTHING_COMPLETED =
	/^Closed \d+ Work Items? fewer \(0\) than forecasted \(\d+\)\. No percentage — nothing was completed\.$/;
const NOTHING_AS_FORECAST = /^Closed no Work Items, exactly as forecasted\.$/;
const CELL_NAMES = [
	MORE,
	FEWER,
	EXACTLY,
	NOTHING_COMPLETED,
	NOTHING_AS_FORECAST,
];

const HELD_TITLE = "Forecast held";
const MISSED_TITLE = "Forecast missed";
const BAND_LABELS = ["within 10%", "10–25% off", "more than 25% off"] as const;

/** The legend's six swatches in reading order: the held row's three bands, then the missed row's. */
const LEGEND_GRADES: readonly ForecastGrade[] = [
	"HeldWithin10",
	"Held10To25",
	"HeldOver25",
	"NotHeldWithin10",
	"NotHeld10To25",
	"NotHeldOver25",
];

const follows = (earlier: Element, later: Element): boolean =>
	(earlier.compareDocumentPosition(later) &
		Node.DOCUMENT_POSITION_FOLLOWING) !==
	0;

const inReadingOrder = (elements: HTMLElement[]): HTMLElement[] =>
	[...elements].sort((a, b) => (follows(a, b) ? -1 : 1));

beforeEach(() => {
	vi.clearAllMocks();
	setMatchMedia(false);
	terms.clear();
	terms.set(TERMINOLOGY_KEYS.TEAM, "Team");
	terms.set(TERMINOLOGY_KEYS.WORK_ITEMS, "Work Items");
	terms.set(TERMINOLOGY_KEYS.WORK_ITEM, "Work Item");
	hoursOfTheDaysFormatted.length = 0;
	aReaderWhoseBrowserUses("de-CH");
});

afterEach(() => {
	vi.restoreAllMocks();
});

describe("@us-06 @slice-06a @driving_port every run shows that it is working", () => {
	it(`the first run shows a spinner and "${LOADING}" in a polite status, and the answer arriving moves no focus`, async () => {
		const answerArrives = anAnswerStillOnItsWay();
		const group = renderTheForecastTab(runRealityCheck);

		await pressRunRealityCheck(group);
		const dialog = await theRealityCheckDialog();
		const status = within(dialog).getByRole("status");

		expect(readingOf(status)).toBe(LOADING);
		expect(
			within(status).getByRole("progressbar", { hidden: true }),
		).toBeInTheDocument();
		expect(status).not.toHaveAttribute("aria-live", "assertive");

		const focusedWhileRunning = document.activeElement;
		await answerArrives(aRealityCheckAnswer());
		await within(dialog).findByRole("table");
		expect(document.activeElement).toBe(focusedWhileRunning);
	});

	it(`Run again shows the same spinner and "${LOADING}" while the fresh check runs`, async () => {
		const dialog = await theDialogFor(aRealityCheckAnswer());
		const answerArrives = anAnswerStillOnItsWay();

		await userEvent.click(
			within(dialog).getByRole("button", { name: /^run again$/i }),
		);
		const status = await within(dialog).findByRole("status");

		expect(readingOf(status)).toBe(LOADING);
		expect(
			within(status).getByRole("progressbar", { hidden: true }),
		).toBeInTheDocument();
		await answerArrives(aRealityCheckAnswer());
	});

	it(`@error a check that fails replaces the spinner and "${LOADING}" with its plain message`, async () => {
		const failure = "The reality check could not be run. Please try again.";
		let fail: (error: Error) => void = () => {};
		runRealityCheck.mockReturnValue(
			new Promise((_resolve, reject) => {
				fail = reject;
			}),
		);
		const group = renderTheForecastTab(runRealityCheck);

		await pressRunRealityCheck(group);
		const dialog = await theRealityCheckDialog();
		expect(readingOf(within(dialog).getByRole("status"))).toBe(LOADING);

		await act(async () => fail(new Error(failure)));

		expect(await within(dialog).findByRole("alert")).toHaveTextContent(failure);
		expect(linesMatching(dialog, /Crunching the numbers/)).toHaveLength(0);
	});
});

describe("@us-06 @slice-06a each period is one line, its dates in the reader's own format", () => {
	it.skip.each([
		{
			locale: "de-CH",
			headers: [
				"Forecast Horizon: 1 week (16.09.2026 – 22.09.2026) – 6 Work Items completed",
				"Forecast Horizon: 2 weeks (09.09.2026 – 22.09.2026) – 11 Work Items completed",
				"Forecast Horizon: 4 weeks (26.08.2026 – 22.09.2026) – 22 Work Items completed",
				"Forecast Horizon: 8 weeks (29.07.2026 – 22.09.2026) – 44 Work Items completed",
			],
		},
		{
			locale: "en-US",
			headers: [
				"Forecast Horizon: 1 week (09/16/2026 – 09/22/2026) – 6 Work Items completed",
				"Forecast Horizon: 2 weeks (09/09/2026 – 09/22/2026) – 11 Work Items completed",
				"Forecast Horizon: 4 weeks (08/26/2026 – 09/22/2026) – 22 Work Items completed",
				"Forecast Horizon: 8 weeks (07/29/2026 – 09/22/2026) – 44 Work Items completed",
			],
		},
	])(
		`@kpi-OUT-6094-how-far-each-forecast-landed in $locale every period header reads its horizon, its first and last day in digits, and what was completed (${PENDING})`,
		async ({ locale, headers }) => {
			aReaderWhoseBrowserUses(locale);
			const dialog = await theDialogFor(aRealityCheckAnswer());

			expect(periodGroupsOf(theTableIn(dialog)).map(periodHeaderText)).toEqual(
				headers,
			);
			expect(hoursOfTheDaysFormatted).not.toHaveLength(0);
			expect(hoursOfTheDaysFormatted.filter((hour) => hour !== 0)).toEqual([]);
		},
	);

	it.skip(`@boundary a period in which the Team completed one Work Item says so in the singular (${PENDING})`, async () => {
		const dialog = await theDialogFor(
			aRealityCheckAnswer({
				checks: thePeriodChecked(7, {
					at95: 1,
					at85: 1,
					at70: 1,
					at50: 2,
					actual: 1,
				}),
			}),
		);

		expect(periodHeaderText(periodGroup(theTableIn(dialog), 7))).toBe(
			"Forecast Horizon: 1 week (16.09.2026 – 22.09.2026) – 1 Work Item completed",
		);
	});

	it.skip(`@error a period the answer carries no scored period for is headed by its horizon alone (${PENDING})`, async () => {
		const answer = aRealityCheckAnswer();
		const dialog = await theDialogFor({
			...answer,
			scoredPeriods: answer.scoredPeriods.filter(
				(period) => period.horizonDays !== 7,
			),
		});

		expect(periodHeaderText(periodGroup(theTableIn(dialog), 7))).toBe(
			"Forecast Horizon: 1 week",
		);
	});
});

describe("@us-06 @slice-06a a cell shows whether it held and the forecast, and explains itself on demand", () => {
	it.skip(`@kpi-OUT-6094-how-far-each-forecast-landed Ocean Explorer's 30-day row over 8 weeks reads ✗ 48, ✓ 40, ✓ 36, ✓ 31 on its grade colours, each named by how it compared (${PENDING})`, async () => {
		const dialog = await theDialogFor(
			aRealityCheckAnswer({
				checks: thePeriodChecked(56, {
					at95: 31,
					at85: 36,
					at70: 40,
					at50: 48,
					actual: 42,
				}),
			}),
		);
		const row = windowRow(periodGroup(theTableIn(dialog), 56), 30);

		expect(
			LEVEL_COLUMNS.map((level) => readingOf(levelCell(row, level))),
		).toEqual(["✗ 48", "✓ 40", "✓ 36", "✓ 31"]);
		expect(levelCell(row, 50)).toHaveAccessibleName(
			`Closed 6 Work Items fewer (42) than forecasted (48). Forecast off by ${MINUS}14%`,
		);
		expect(levelCell(row, 70)).toHaveAccessibleName(
			"Closed 2 Work Items more (42) than forecasted (40). Forecast off by +5%",
		);
		expect(levelCell(row, 85)).toHaveAccessibleName(
			"Closed 6 Work Items more (42) than forecasted (36). Forecast off by +14%",
		);
		expect(levelCell(row, 95)).toHaveAccessibleName(
			"Closed 11 Work Items more (42) than forecasted (31). Forecast off by +26%",
		);
		expect(levelCell(row, 50)).toHaveStyle({
			backgroundColor: gradeFill("NotHeld10To25"),
		});
		expect(levelCell(row, 70)).toHaveStyle({
			backgroundColor: gradeFill("HeldWithin10"),
		});
		expect(levelCell(row, 85)).toHaveStyle({
			backgroundColor: gradeFill("Held10To25"),
		});
		expect(levelCell(row, 95)).toHaveStyle({
			backgroundColor: gradeFill("HeldOver25"),
		});
	});

	it.skip.each([
		{
			comparison: "more completed, by one Work Item",
			actual: 15,
			forecast: 14,
			shows: "✓ 14",
			name: "Closed 1 Work Item more (15) than forecasted (14). Forecast off by +7%",
		},
		{
			comparison: "fewer completed, by one Work Item - Coastal Survey's week",
			actual: 3,
			forecast: 4,
			shows: "✗ 4",
			name: `Closed 1 Work Item fewer (3) than forecasted (4). Forecast off by ${MINUS}33%`,
		},
		{
			comparison: "exactly as forecast",
			actual: 22,
			forecast: 22,
			shows: "✓ 22",
			name: "Closed exactly the forecasted 22 Work Items.",
		},
		{
			comparison: "exactly as forecast, one Work Item",
			actual: 1,
			forecast: 1,
			shows: "✓ 1",
			name: "Closed exactly the forecasted 1 Work Item.",
		},
		{
			comparison:
				"nothing forecast and nothing completed - Harbour Pilots' 95th",
			actual: 0,
			forecast: 0,
			shows: "✓ 0",
			name: "Closed no Work Items, exactly as forecasted.",
		},
		{
			comparison:
				"nothing completed against a forecast of 2 - Harbour Pilots' 50th",
			actual: 0,
			forecast: 2,
			shows: "✗ 2",
			name: "Closed 2 Work Items fewer (0) than forecasted (2). No percentage — nothing was completed.",
		},
		{
			comparison: "nothing completed against a forecast of 1",
			actual: 0,
			forecast: 1,
			shows: "✗ 1",
			name: "Closed 1 Work Item fewer (0) than forecasted (1). No percentage — nothing was completed.",
		},
	])(
		`@kpi-OUT-6094-no-percentage-without-its-work-items $comparison: the cell shows "$shows" and is named "$name" (${PENDING})`,
		async ({ actual, forecast, shows, name }) => {
			const dialog = await theDialogFor(
				aRealityCheckAnswer({
					checks: [
						{
							window: 30,
							horizon: 7,
							at95: forecast,
							at85: forecast,
							at70: forecast,
							at50: forecast,
							actual,
						},
					],
				}),
			);
			const cell = levelCell(
				windowRow(periodGroup(theTableIn(dialog), 7), 30),
				95,
			);

			expect(readingOf(cell)).toBe(shows);
			expect(cell).toHaveAccessibleName(name);
		},
	);

	it.skip(`@kpi-OUT-6094-how-far-each-forecast-landed every graded cell is a keyboard stop that shows only its glyph and forecast and is named by how far off it was (${PENDING})`, async () => {
		const dialog = await theDialogFor(aRealityCheckAnswer());

		const cells = everyGradedCell(dialog);
		expect(cells).toHaveLength(64);
		for (const cell of cells) {
			expect(cell).toHaveAttribute("tabindex", "0");
			expect(readingOf(cell)).toMatch(/^[✓✗] \d+$/);
			const name = cell.getAttribute("aria-label") ?? "";
			expect(
				CELL_NAMES.some((pattern) => pattern.test(name)),
				`"${name}" is none of the cell wordings`,
			).toBe(true);
		}
	});

	it.skip(`on keyboard focus a cell shows its comparison in a tooltip whose words are the cell's name - Ocean Explorer's 14-day 50th, ✗ 21 against 14 (${PENDING})`, async () => {
		const dialog = await theDialogFor(oceanExplorersWeek());
		const cell = theFirstGradedCell(dialog);
		const comparison = `Closed 7 Work Items fewer (14) than forecasted (21). Forecast off by ${MINUS}50%`;

		expect(readingOf(cell)).toBe("✗ 21");
		await tabUntilFocused(cell);
		const tooltip = await screen.findByRole("tooltip");

		expect(readingOf(tooltip)).toBe(comparison);
		expect(cell).toHaveAccessibleName(comparison);
	});

	it.skip(`on hover a cell shows the same tooltip (${PENDING})`, async () => {
		const dialog = await theDialogFor(oceanExplorersWeek());
		const cell = theFirstGradedCell(dialog);

		await userEvent.hover(cell);
		const tooltip = await screen.findByRole("tooltip");

		expect(readingOf(tooltip)).toBe(
			`Closed 7 Work Items fewer (14) than forecasted (21). Forecast off by ${MINUS}50%`,
		);
	});

	it.skip(`@error a level the check left out shows "—", is named "Not checked at this confidence level." and is still a keyboard stop (${PENDING})`, async () => {
		const answer = oceanExplorersWeek();
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
		const notChecked = levelCell(row, 70);

		expect(within(row).getAllByRole("cell")).toHaveLength(4);
		expect(readingOf(notChecked)).toBe("—");
		expect(notChecked).toHaveAccessibleName(
			"Not checked at this confidence level.",
		);
		expect(notChecked).toHaveAttribute("tabindex", "0");
		expect(readingOf(levelCell(row, 85))).toMatch(/^[✓✗] \d+$/);
	});
});

describe("@us-06 @slice-06a a cell's words never trap the reader", () => {
	it.skip(`@error Escape dismisses a cell's tooltip and leaves the dialog open (${PENDING})`, async () => {
		const dialog = await theDialogFor(oceanExplorersWeek());

		await tabUntilFocused(theFirstGradedCell(dialog));
		await screen.findByRole("tooltip");
		await userEvent.keyboard("{Escape}");

		await waitFor(() => expect(screen.queryByRole("tooltip")).toBeNull());
		expect(
			screen.getByRole("dialog", { name: /reality check/i }),
		).toBeInTheDocument();
		// A closing dialog lingers through its exit animation, so its presence proves nothing; focus
		// staying on the cell does, because closing the dialog hands focus back to its trigger.
		expect(theFirstGradedCell(dialog)).toHaveFocus();
	});

	// Already true and kept as it is: a check that could not run is one muted reason across the row, never
	// a graded cell, a "—" or a keyboard stop.
	it(`@error a check that could not run keeps its reason across the four columns, and is neither graded nor a keyboard stop`, async () => {
		const dialog = await theDialogFor(
			aRealityCheckAnswer({
				unevaluable: [
					{
						window: 14,
						horizon: 7,
						reason: "TooFewActiveDays",
						daysWithCompletedWork: 3,
					},
				],
			}),
		);
		const cells = within(
			windowRow(periodGroup(theTableIn(dialog), 7), 14),
		).getAllByRole("cell");

		expect(cells).toHaveLength(1);
		expect(readingOf(cells[0])).toBe(
			"Not enough history in this window to check: 3 days with completed Work Items, 5 needed.",
		);
		expect(cells[0]).not.toHaveAttribute("tabindex");
	});
});

describe("@us-06 @slice-06a the legend, the table's name and the Backtesting group", () => {
	it.skip(`@kpi-OUT-6094-colour-never-alone the legend is two titled rows - "${HELD_TITLE}" then "${MISSED_TITLE}" - each with its three bands on the grade fills, in band order (${PENDING})`, async () => {
		const dialog = await theDialogFor(aRealityCheckAnswer());
		const titles = [HELD_TITLE, MISSED_TITLE].map((title) => {
			const matching = linesReading(dialog, title);
			expect(matching, `one "${title}"`).toHaveLength(1);
			return matching[0];
		});
		const labels = inReadingOrder(
			BAND_LABELS.flatMap((label) => {
				const matching = linesReading(dialog, label);
				expect(matching, `"${label}" once per row`).toHaveLength(2);
				return matching;
			}),
		);

		expect(inReadingOrder([...titles, ...labels]).map(readingOf)).toEqual([
			HELD_TITLE,
			...BAND_LABELS,
			MISSED_TITLE,
			...BAND_LABELS,
		]);

		// Each label's swatch is the nearest grade-filled element before it, outside the table.
		const fills = LEGEND_GRADES.map((grade) => asComputed(gradeFill(grade)));
		const swatches = Array.from(dialog.querySelectorAll<HTMLElement>("*"))
			.filter((element) => element.closest("table") === null)
			.filter((element) =>
				fills.includes(getComputedStyle(element).backgroundColor),
			)
			.reverse();
		const swatchBefore = (label: HTMLElement): string => {
			const swatch = swatches.find((element) => follows(element, label));
			return swatch === undefined
				? "no swatch"
				: getComputedStyle(swatch).backgroundColor;
		};
		expect(labels.map(swatchBefore)).toEqual(fills);
	});

	it.skip(`the credit line and the "Not checked" entry are gone from under the legend (${PENDING})`, async () => {
		const dialog = await theDialogFor(aRealityCheckAnswer());

		expect(linesMatching(dialog, /Nick Brown|Full Monte/)).toHaveLength(0);
		expect(linesReading(dialog, "Not checked")).toHaveLength(0);
	});

	it.skip(`no caption sits above the table; its scrolling region is named for the Team, takes keyboard focus, and the table keeps its period, window and level headers (${PENDING})`, async () => {
		const dialog = await theDialogFor(aRealityCheckAnswer());
		const table = theTableIn(dialog);
		const region = within(dialog).getByRole("region", {
			name: "Forecasts checked for Ocean Explorer, by forecast horizon and sampling window",
		});

		expect(table.querySelector("caption")).toBeNull();
		expect(
			linesMatching(dialog, /Every forecast checked for Ocean Explorer/),
		).toHaveLength(0);
		expect(region).toContainElement(table);
		expect(region).toHaveAttribute("tabindex", "0");
		expect(table.querySelectorAll('th[scope="rowgroup"]')).toHaveLength(4);
		expect(table.querySelectorAll('th[scope="row"]')).toHaveLength(16);
		expect(within(table).getAllByRole("columnheader")).toHaveLength(5);
	});

	it(`the Forecast Backtesting group holds the trigger and its explanation, and nothing of the answer once the dialog is closed`, async () => {
		runRealityCheck.mockResolvedValue(aRealityCheckAnswer());
		const group = renderTheForecastTab(runRealityCheck);
		await pressRunRealityCheck(group);
		const dialog = await theRealityCheckDialog();
		await within(dialog).findByRole("table");

		await userEvent.keyboard("{Escape}");
		await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());

		expect(within(group).queryAllByRole("table")).toHaveLength(0);
		const buttons = within(group).getAllByRole("button");
		expect(buttons).toHaveLength(2);
		expect(buttons[0]).toHaveAccessibleName("What does the reality check do?");
		expect(buttons[1]).toHaveAccessibleName("Run reality check");
	});
});

describe("@us-06 @slice-06a Terminology", () => {
	it.skip(`speaks the instance's own words in the period headers and the cells, and names the region by the Team's own name (${PENDING})`, async () => {
		terms.set(TERMINOLOGY_KEYS.TEAM, "Squad");
		terms.set(TERMINOLOGY_KEYS.WORK_ITEMS, "Tickets");
		terms.set(TERMINOLOGY_KEYS.WORK_ITEM, "Ticket");
		const dialog = await theDialogFor(oceanExplorersWeek());

		expect(periodHeaderText(periodGroup(theTableIn(dialog), 7))).toBe(
			"Forecast Horizon: 1 week (16.09.2026 – 22.09.2026) – 14 Tickets completed",
		);
		expect(theFirstGradedCell(dialog)).toHaveAccessibleName(
			`Closed 7 Tickets fewer (14) than forecasted (21). Forecast off by ${MINUS}50%`,
		);
		expect(
			within(dialog).getByRole("region", {
				name: "Forecasts checked for Ocean Explorer, by forecast horizon and sampling window",
			}),
		).toBeInTheDocument();
		const everyName = everyGradedCell(dialog).map(
			(cell) => cell.getAttribute("aria-label") ?? "",
		);
		expect(everyName.filter((name) => /work items?/i.test(name))).toEqual([]);
		expect(linesMatching(dialog, /work items?/i)).toHaveLength(0);
		expect(
			linesMatching(dialog, /\b(throughput|Epic|Initiative|Story)\b/i),
		).toHaveLength(0);
	});
});
