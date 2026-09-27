import { screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import {
	aRealityCheckAnswer,
	type EvaluableCheck,
	expectTheLine,
	HORIZON_DAYS,
	linesMatching,
	linesReading,
	type RealityCheckAnswerOptions,
	type RealityCheckWireAnswer,
	readingOf,
	STANDARD_WINDOW_DAYS,
	setMatchMedia,
	tabUntilFocused,
	theDialogWithTheAnswer,
	theTableIn,
} from "../../../tests/RealityCheckFixture";

/**
 * The reality check opens on a summary a reader takes in at a glance, in place of the paragraphs that
 * used to sit above the table: a headline that counts the scenarios and forecasts backtested, a badge that
 * says how the Team's own sampling window stood, and one row per confidence level whose bar is filled to
 * the share of checks the level held in, against a tick at the rate it should hold at.
 *
 * What the specs need from the markup, and nothing more: the headline's explanation opens from a button
 * named "About these numbers" into a `dialog` of the same name, holding a link named "The Full Monte";
 * the badge is a focusable MUI chip whose visible words are its name and whose tooltip describes it; each
 * level's bar is an `img` named by its text alternative, holding a hidden progress bar whose value is the
 * held share.
 */

const PENDING = "06b: the summary is not built yet";

const EXPLANATION_ICON = "About these numbers";

const HELD_UP_MEANS =
	"A sampling window held up when its 95th forecast held in more than half of the checks that could be run on it.";

const ACCURATE_MEANS =
	"Accurate means within 10% of what was completed, whether the forecast held or not.";

const FULL_MONTE =
	"https://medium.com/asos-techblog/the-full-monte-901d721b8532";

const RANKING =
	/\b(best|better|worse|worst|recommend(?:ed)?|optimal|winner|rank(?:ed|ing)?|most accurate|least accurate)\b/i;

const RETIRED_SENTENCES = [
	/How often each confidence level held/,
	/should be about/,
	/\bUsually\b/,
	/Anything between/,
	/held up for/,
	/The sampling window is a setting/,
	/The confidence level is not a setting/,
	/forecast runs? (?:were|was) checked/,
	/over-forecasting|under-forecasting/,
];

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

const precedes = (earlier: Node, later: Node): boolean =>
	(earlier.compareDocumentPosition(later) &
		Node.DOCUMENT_POSITION_FOLLOWING) !==
	0;

/** Every visible word between the dialog's title and its table: what a reader must get through first. */
const wordsAboveTheTable = (dialog: HTMLElement): string[] => {
	const table = theTableIn(dialog);
	const title = document.getElementById(
		dialog.getAttribute("aria-labelledby") ?? "",
	);
	const walker = document.createTreeWalker(dialog, NodeFilter.SHOW_TEXT);
	const words: string[] = [];
	for (let node = walker.nextNode(); node !== null; node = walker.nextNode()) {
		const hidden = node.parentElement?.closest('[aria-hidden="true"]') ?? null;
		const inTitle = title?.contains(node) ?? false;
		if (hidden === null && !inTitle && precedes(node, table)) {
			words.push(
				...(node.textContent ?? "")
					.split(/\s+/)
					.filter((word) => /[\p{L}\p{N}]/u.test(word)),
			);
		}
	}
	return words;
};

/** The badge, found by its words: a chip when the words are on one. */
const theBadgeReading = (dialog: HTMLElement, words: string): HTMLElement => {
	const matching = linesReading(dialog, words);
	expect(matching, `no badge reads "${words}"`).toHaveLength(1);
	return matching[0].closest<HTMLElement>(".MuiChip-root") ?? matching[0];
};

const theLevelBars = (dialog: HTMLElement): HTMLElement[] =>
	within(dialog)
		.queryAllByRole("img")
		.filter((image) =>
			/^\d{2}th /.test(image.getAttribute("aria-label") ?? ""),
		);

const theExplanationIcon = (dialog: HTMLElement): HTMLElement =>
	within(dialog).getByRole("button", { name: EXPLANATION_ICON });

const theOpenExplanation = (): Promise<HTMLElement> =>
	screen.findByRole("dialog", { name: EXPLANATION_ICON });

/** Reading `text` out of `container` leaves nothing but what the other sentences account for. */
const withoutSentences = (container: HTMLElement, sentences: string[]) =>
	sentences
		.reduce(
			(rest, sentence) => rest.replace(sentence, ""),
			readingOf(container),
		)
		.trim();

const LEVEL_KEY = { 50: "at50", 70: "at70", 85: "at85", 95: "at95" } as const;

/** The sixteen checks of the ladder against an actual of 42, one level's forecasts taken in turn. */
const theSixteenChecksAt = (
	level: 50 | 70 | 85 | 95,
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

/** Maria's 85th against an actual of 42: nine held by more than a quarter, three within 10%, three by 10-25%, one missed. */
const mariasEightyFifth = (): RealityCheckAnswerOptions => ({
	heldCounts: { 85: 15 },
	checks: theSixteenChecksAt(85, [
		...times(9, 31),
		...times(3, 40),
		...times(3, 36),
		...times(1, 48),
	]),
});

const fourChecksOfTheWindow = (window: number, daysWithCompletedWork: number) =>
	HORIZON_DAYS.map((horizon) => ({
		window,
		horizon,
		reason: "TooFewActiveDays" as const,
		daysWithCompletedWork,
	}));

const coastalSurvey = () =>
	aRealityCheckAnswer({
		teamName: "Coastal Survey",
		soundWindowDays: [30, 60, 90],
		determination: "SomeWindowsSound",
		unevaluatedWindowDays: [14],
		unevaluable: fourChecksOfTheWindow(14, 3),
	});

const kelpFarm = () =>
	aRealityCheckAnswer({
		teamName: "Kelp Farm",
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
			fourChecksOfTheWindow(window, 1),
		),
		periodActuals: { 7: 1, 14: 1, 28: 1, 56: 2 },
	});

const deepCurrent = () =>
	aRealityCheckAnswer({
		teamName: "Deep Current",
		currentSettingDays: 14,
		soundWindowDays: [30, 60, 90],
		determination: "SomeWindowsSound",
		standing: "Outside",
		heldCounts: { 50: 6, 70: 9, 85: 11, 95: 12 },
	});

const EVERY_SCENARIO_EXPLAINED =
	"Each scenario replays one forecast: a recent period (1 week, 2 weeks, 4 weeks and 8 weeks, each ending today), forecast from one sampling window of the history before it, then compared with what the Team actually completed. Each is read at 4 confidence levels, so 16 scenarios give 64 forecasts.";

const CREDIT = /^Inspired by Nick Brown's article The Full Monte\.?$/;

beforeEach(() => {
	vi.clearAllMocks();
	setMatchMedia(false);
	terms.clear();
	terms.set(TERMINOLOGY_KEYS.TEAM, "Team");
	terms.set(TERMINOLOGY_KEYS.WORK_ITEMS, "Work Items");
	terms.set(TERMINOLOGY_KEYS.WORK_ITEM, "Work Item");
});

describe("@us-06 @slice-06b @driving_port the dialog opens on a summary, not on paragraphs", () => {
	it.skip(`@kpi-OUT-6094-words-above-the-table above the table sit only the headline, the window badge and four level rows - no more than 50 words (${PENDING})`, async () => {
		const dialog = await theDialogFor(
			aRealityCheckAnswer({ heldCounts: { 50: 12, 70: 14, 85: 15, 95: 16 } }),
		);

		expectTheLine(dialog, "Backtested 16 scenarios · 64 forecasts");
		theBadgeReading(dialog, "Your 30-day sampling window: fine");
		expect(theLevelBars(dialog)).toHaveLength(4);
		const words = wordsAboveTheTable(dialog);
		expect(
			words.length,
			`${words.length} words above the table: ${words.join(" ")}`,
		).toBeLessThanOrEqual(50);
	});

	it.skip(`none of the sentences the summary replaces is said anywhere in the dialog (${PENDING})`, async () => {
		const dialog = await theDialogFor(coastalSurvey());

		for (const retired of RETIRED_SENTENCES) {
			expect(linesMatching(dialog, retired), String(retired)).toHaveLength(0);
		}
	});

	it.each([
		{
			case: "every one of 16 scenarios ran",
			answer: () => aRealityCheckAnswer(),
			headline: "Backtested 16 scenarios · 64 forecasts",
		},
		{
			case: "a fifth window makes 20",
			answer: () => aRealityCheckAnswer({ currentSettingDays: 45 }),
			headline: "Backtested 20 scenarios · 80 forecasts",
		},
		{
			case: "some scenarios could not run - Coastal Survey",
			answer: coastalSurvey,
			headline: "Backtested 12 of 16 scenarios · 48 forecasts",
		},
		{
			case: "no scenario could run - Kelp Farm",
			answer: kelpFarm,
			headline: "None of the 16 scenarios could be backtested",
		},
		{
			case: "one scenario read at one level",
			answer: () => ({
				...aRealityCheckAnswer(),
				denominator: {
					runsAttempted: 1,
					runsEvaluated: 1,
					levelsPerRun: 1,
					scoresEvaluated: 1,
				},
			}),
			headline: "Backtested 1 scenario · 1 forecast",
		},
	])(
		`@kpi-OUT-6094-caution-is-visible when $case the headline reads "$headline"`,
		async ({ answer, headline }) => {
			const dialog = await theDialogFor(answer());

			expectTheLine(dialog, headline);
		},
	);
});

describe("@us-06 @slice-06b each confidence level is a bar against the rate it should hold at", () => {
	it(`@kpi-OUT-6094-caution-is-visible Maria's 85th held in 15 of 16 checks, 3 of them within 10%: a bar at 94% against a tick at 85%, reading "94% (15 of 16) · 3 accurate"`, async () => {
		const dialog = await theDialogFor(aRealityCheckAnswer(mariasEightyFifth()));
		const bar = within(dialog).getByRole("img", {
			name: "85th Confident: held in 94% of checks (15 of 16), expected about 85%; 3 accurate within 10%",
		});

		expect(
			within(bar).getByRole("progressbar", { hidden: true }),
		).toHaveAttribute("aria-valuenow", "94");
		expectTheLine(dialog, "94% (15 of 16) · 3 accurate");
	});

	it(`one row per level in ascending order, each labelled like its column and reading its held share, counts and accurate checks`, async () => {
		const dialog = await theDialogFor(
			aRealityCheckAnswer({ heldCounts: { 50: 12, 70: 14, 85: 15, 95: 16 } }),
		);

		expect(
			theLevelBars(dialog).map((bar) => bar.getAttribute("aria-label")),
		).toEqual([
			"50th Risky: held in 75% of checks (12 of 16), expected about 50%; 0 accurate within 10%",
			"70th Realistic: held in 88% of checks (14 of 16), expected about 70%; 16 accurate within 10%",
			"85th Confident: held in 94% of checks (15 of 16), expected about 85%; 12 accurate within 10%",
			"95th Certain: held in 100% of checks (16 of 16), expected about 95%; 0 accurate within 10%",
		]);
		for (const line of [
			"75% (12 of 16) · 0 accurate",
			"88% (14 of 16) · 16 accurate",
			"94% (15 of 16) · 12 accurate",
			"100% (16 of 16) · 0 accurate",
		]) {
			expectTheLine(dialog, line);
		}
		for (const bar of theLevelBars(dialog)) {
			expect(precedes(bar, theTableIn(dialog))).toBe(true);
		}
	});

	it.skip(`@error a level that never held and one that always held get no words beyond their bar and numbers (${PENDING})`, async () => {
		const dialog = await theDialogFor(
			aRealityCheckAnswer({
				heldCounts: { 50: 0, 70: 9, 85: 12, 95: 16 },
				readings: { 50: "NeverHeld", 95: "AlwaysHeld" },
			}),
		);

		expectTheLine(dialog, "0% (0 of 16) · 0 accurate");
		expectTheLine(dialog, "100% (16 of 16) · 0 accurate");
		expect(
			linesMatching(
				dialog,
				/never held|every time|over-forecasting|under-forecasting/i,
			),
		).toHaveLength(0);
	});

	it(`@error a Team with no checkable history - Kelp Farm - gets a plain headline, a could-not-be-checked badge and four levels not tested, each an empty bar`, async () => {
		const dialog = await theDialogFor(kelpFarm());

		expectTheLine(dialog, "None of the 16 scenarios could be backtested");
		theBadgeReading(
			dialog,
			"Your 30-day sampling window: could not be checked",
		);
		expect(
			linesReading(dialog, "Not tested — no check could run"),
		).toHaveLength(4);
		expect(
			theLevelBars(dialog).map((bar) => bar.getAttribute("aria-label")),
		).toEqual([
			"50th Risky: not tested, no check could run",
			"70th Realistic: not tested, no check could run",
			"85th Confident: not tested, no check could run",
			"95th Certain: not tested, no check could run",
		]);
		for (const bar of theLevelBars(dialog)) {
			expect(
				within(bar).getByRole("progressbar", { hidden: true }),
			).toHaveAttribute("aria-valuenow", "0");
		}
	});
});

describe("@us-06 @slice-06b the sampling window is one badge that never ranks windows", () => {
	it.each([
		{
			state: "every window held up, the Team's own among them",
			options: {},
			badge: "Your 30-day sampling window: fine",
			tone: "Success",
		},
		{
			state: "some windows held up, the Team's own among them",
			options: {
				currentSettingDays: 60,
				determination: "SomeWindowsSound",
				soundWindowDays: [30, 60, 90],
				standing: "Inside",
			},
			badge: "Your 60-day sampling window: fine",
			tone: "Success",
		},
		{
			state: "some windows held up, the Team's own not - Deep Current",
			options: {
				teamName: "Deep Current",
				currentSettingDays: 14,
				determination: "SomeWindowsSound",
				soundWindowDays: [30, 60, 90],
				standing: "Outside",
			},
			badge: "Your 14-day sampling window: did not hold up",
			tone: "Warning",
		},
		{
			state: "no window held up",
			options: {
				determination: "NoWindowSound",
				soundWindowDays: [],
				standing: "Outside",
			},
			badge: "Your 30-day sampling window: did not hold up (no window did)",
			tone: "Warning",
		},
		{
			state: "nothing could be concluded",
			options: {
				determination: "NotEnoughEvidence",
				soundWindowDays: [],
				standing: "NotDetermined",
			},
			badge: "Your 30-day sampling window: could not be checked",
			tone: "Default",
		},
		{
			state: "the Team's own window could not be checked",
			options: {
				determination: "SomeWindowsSound",
				soundWindowDays: [60, 90],
				standing: "NotDetermined",
			},
			badge: "Your 30-day sampling window: could not be checked",
			tone: "Default",
		},
		{
			state: "the Team forecasts from fixed dates - Harbour Pilots",
			options: { currentSettingWasTested: false },
			badge: "Your Team forecasts from fixed dates: sampling window not tested",
			tone: "Default",
		},
		{
			state: "the Team's window is not a positive length",
			options: {
				currentSettingDays: 0,
				currentSettingWasTested: false,
				notTestedReason: "NotAPositiveLength",
			},
			badge: "Your sampling window: not tested (not a positive number of days)",
			tone: "Default",
		},
		{
			state:
				"every window alike yet the Team's own outside, which cannot happen but must not read as fine",
			options: { determination: "AllWindowsAlike", standing: "Outside" },
			badge: "Your 30-day sampling window: did not hold up",
			tone: "Warning",
		},
	] satisfies {
		state: string;
		options: RealityCheckAnswerOptions;
		badge: string;
		tone: string;
	}[])(
		`@kpi-OUT-6094-no-window-ranked when $state the badge reads "$badge" in the $tone tone`,
		async ({ options, badge, tone }) => {
			const dialog = await theDialogFor(aRealityCheckAnswer(options));

			expect(theBadgeReading(dialog, badge)).toHaveClass(
				`MuiChip-color${tone}`,
			);
		},
	);

	it(`on keyboard focus the badge keeps its words as its name and says what held up means and which windows could not be checked, naming none that held up - Coastal Survey`, async () => {
		const dialog = await theDialogFor(coastalSurvey());
		const badge = theBadgeReading(dialog, "Your 30-day sampling window: fine");
		const notChecked =
			"The 14-day sampling window could not be checked, so it is not counted either way.";

		await tabUntilFocused(badge);
		const tooltip = await screen.findByRole("tooltip");

		expect(badge).toHaveAccessibleName("Your 30-day sampling window: fine");
		expect(linesReading(tooltip, HELD_UP_MEANS)).not.toHaveLength(0);
		expect(linesReading(tooltip, notChecked)).not.toHaveLength(0);
		expect(withoutSentences(tooltip, [HELD_UP_MEANS, notChecked])).toBe("");
	});

	it(`@error when the Team's own window could not be checked the badge's tooltip names that window among the ones not counted`, async () => {
		const dialog = await theDialogFor(
			aRealityCheckAnswer({
				determination: "SomeWindowsSound",
				soundWindowDays: [60, 90],
				standing: "NotDetermined",
				unevaluable: fourChecksOfTheWindow(30, 2),
			}),
		);
		const badge = theBadgeReading(
			dialog,
			"Your 30-day sampling window: could not be checked",
		);
		const notChecked =
			"The 30-day sampling window could not be checked, so it is not counted either way.";

		await userEvent.hover(badge);
		const tooltip = await screen.findByRole("tooltip");

		expect(withoutSentences(tooltip, [HELD_UP_MEANS, notChecked])).toBe("");
		expect(linesReading(tooltip, HELD_UP_MEANS)).not.toHaveLength(0);
		expect(linesReading(tooltip, notChecked)).not.toHaveLength(0);
	});

	it(`@boundary when every window could be checked the badge's tooltip only says what held up means`, async () => {
		const dialog = await theDialogFor(aRealityCheckAnswer());
		const badge = theBadgeReading(dialog, "Your 30-day sampling window: fine");

		await userEvent.hover(badge);
		const tooltip = await screen.findByRole("tooltip");

		expect(readingOf(tooltip)).toBe(HELD_UP_MEANS);
	});

	it.skip(`@kpi-OUT-6094-no-window-ranked nothing in the summary, the badge's tooltip or the explanation names, orders or scores a window - Deep Current, whose 14 days did not hold up (${PENDING})`, async () => {
		const dialog = await theDialogFor(deepCurrent());
		const badge = theBadgeReading(
			dialog,
			"Your 14-day sampling window: did not hold up",
		);

		const windowsNamedAboveTheTable = wordsAboveTheTable(dialog)
			.join(" ")
			.match(/\b\d+-day\b/g);
		expect(windowsNamedAboveTheTable).toEqual(["14-day"]);

		await userEvent.hover(badge);
		const badgeTooltip = readingOf(await screen.findByRole("tooltip"));
		await userEvent.unhover(badge);

		await userEvent.click(theExplanationIcon(dialog));
		const explanation = readingOf(await theOpenExplanation());

		for (const words of [readingOf(dialog), badgeTooltip, explanation]) {
			expect(words).not.toMatch(RANKING);
		}
		expect(badgeTooltip).not.toMatch(/\b(30|60|90)-day\b/);
		expect(explanation).not.toMatch(/\b\d+-day\b/);
	});
});

describe("@us-06 @slice-06b the explanation, the credit and the source are one press away", () => {
	it.skip(`hovering the headline's info icon names it "${EXPLANATION_ICON}" and opens nothing (${PENDING})`, async () => {
		const dialog = await theDialogFor(aRealityCheckAnswer());

		await userEvent.hover(theExplanationIcon(dialog));
		const tooltip = await screen.findByRole("tooltip");

		expect(readingOf(tooltip)).toBe(EXPLANATION_ICON);
		expect(screen.queryByRole("dialog", { name: EXPLANATION_ICON })).toBeNull();
	});

	it.skip.each([
		{ how: "a click", open: (icon: HTMLElement) => userEvent.click(icon) },
		{
			how: "Enter",
			open: async (icon: HTMLElement) => {
				icon.focus();
				await userEvent.keyboard("{Enter}");
			},
		},
		{
			how: "Space",
			open: async (icon: HTMLElement) => {
				icon.focus();
				await userEvent.keyboard(" ");
			},
		},
	])(
		`$how on the info icon opens the explanation: what the scenarios count, what accurate means, and Nick Brown's article linked, and nothing else (${PENDING})`,
		async ({ open }) => {
			const dialog = await theDialogFor(aRealityCheckAnswer());

			await open(theExplanationIcon(dialog));
			const explanation = await theOpenExplanation();
			const credit = linesMatching(explanation, CREDIT);

			expectTheLine(explanation, EVERY_SCENARIO_EXPLAINED);
			expectTheLine(explanation, ACCURATE_MEANS);
			expect(credit, "the credit line").toHaveLength(1);
			expect(
				withoutSentences(explanation, [
					EVERY_SCENARIO_EXPLAINED,
					ACCURATE_MEANS,
					readingOf(credit[0]),
				]),
			).toBe("");
			expect(readingOf(explanation)).not.toMatch(/could not run/);
		},
	);

	it.skip(`the article is the next Tab stop and opens The Full Monte in a new tab (${PENDING})`, async () => {
		const dialog = await theDialogFor(aRealityCheckAnswer());

		await userEvent.click(theExplanationIcon(dialog));
		const explanation = await theOpenExplanation();
		const link = within(explanation).getByRole("link", {
			name: "The Full Monte",
		});

		await userEvent.tab();
		expect(link).toHaveFocus();
		expect(link).toHaveAttribute("href", FULL_MONTE);
		expect(link).toHaveAttribute("target", "_blank");
		expect(link.getAttribute("rel")?.split(/\s+/)).toEqual(
			expect.arrayContaining(["noopener", "noreferrer"]),
		);
	});

	it.skip(`Escape closes the explanation first and puts focus back on its icon; a second Escape closes the dialog (${PENDING})`, async () => {
		const dialog = await theDialogFor(aRealityCheckAnswer());
		const icon = theExplanationIcon(dialog);

		await userEvent.click(icon);
		await theOpenExplanation();
		await userEvent.keyboard("{Escape}");

		await waitFor(() =>
			expect(
				screen.queryByRole("dialog", { name: EXPLANATION_ICON }),
			).toBeNull(),
		);
		expect(
			screen.getByRole("dialog", { name: /reality check/i }),
		).toBeInTheDocument();
		expect(icon).toHaveFocus();

		await userEvent.keyboard("{Escape}");
		await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
	});

	it.skip(`@error when no scenario could run the explanation says all of them were left out, and why - Kelp Farm (${PENDING})`, async () => {
		const dialog = await theDialogFor(kelpFarm());

		await userEvent.click(theExplanationIcon(dialog));
		const explanation = await theOpenExplanation();

		expect(
			linesMatching(
				explanation,
				/16 of the 16 scenarios could not run and are left out of every count\./,
			),
		).not.toHaveLength(0);
		expect(
			linesMatching(
				explanation,
				/16 checks on the 14-day, 30-day, 60-day and 90-day sampling windows had fewer than 5 days with completed Work Items to draw on\./,
			),
		).not.toHaveLength(0);
	});

	it.skip(`@error when some scenarios could not run the explanation says how many and why - Coastal Survey (${PENDING})`, async () => {
		const dialog = await theDialogFor(coastalSurvey());

		await userEvent.click(theExplanationIcon(dialog));
		const explanation = await theOpenExplanation();

		expectTheLine(
			explanation,
			"Each scenario replays one forecast: a recent period (1 week, 2 weeks, 4 weeks and 8 weeks, each ending today), forecast from one sampling window of the history before it, then compared with what the Team actually completed. Each is read at 4 confidence levels, so 12 scenarios give 48 forecasts.",
		);
		expect(
			linesMatching(
				explanation,
				/4 of the 16 scenarios could not run and are left out of every count\./,
			),
		).not.toHaveLength(0);
		expect(
			linesMatching(
				explanation,
				/4 checks on the 14-day sampling window had fewer than 5 days with completed Work Items to draw on\./,
			),
		).not.toHaveLength(0);
	});
});

describe("@us-06 @slice-06b the summary holds to what the check promised", () => {
	it.skip(`@kpi-OUT-4172-read-only the only control the summary adds is its explanation's icon; nothing could change a Team setting (${PENDING})`, async () => {
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
		const names = within(dialog)
			.getAllByRole("button")
			.map((button) => button.getAttribute("aria-label") ?? readingOf(button));
		expect(names.filter((name) => name === EXPLANATION_ICON)).toHaveLength(1);
		for (const name of names) {
			expect(name).toMatch(/^(Run again|Close|About these numbers)$/);
		}
	});

	it(`a Team forecasting from fixed dates is told so in the instance's own word for Team`, async () => {
		terms.set(TERMINOLOGY_KEYS.TEAM, "Squad");
		const dialog = await theDialogFor(
			aRealityCheckAnswer({ currentSettingWasTested: false }),
		);

		theBadgeReading(
			dialog,
			"Your Squad forecasts from fixed dates: sampling window not tested",
		);
	});

	it.skip(`the explanation and its reasons speak the instance's own words, and no tracker's (${PENDING})`, async () => {
		terms.set(TERMINOLOGY_KEYS.TEAM, "Squad");
		terms.set(TERMINOLOGY_KEYS.WORK_ITEMS, "Tickets");
		terms.set(TERMINOLOGY_KEYS.WORK_ITEM, "Ticket");
		const dialog = await theDialogFor(coastalSurvey());
		await userEvent.click(theExplanationIcon(dialog));
		const explanation = await theOpenExplanation();

		expect(readingOf(explanation)).toMatch(
			/then compared with what the Squad actually completed\./,
		);
		expect(readingOf(explanation)).toMatch(
			/fewer than 5 days with completed Tickets to draw on\./,
		);
		for (const words of [readingOf(dialog), readingOf(explanation)]) {
			expect(words).not.toMatch(/work items?/i);
			expect(words).not.toMatch(/\b(throughput|Epic|Initiative|Story)\b/i);
		}
	});
});
