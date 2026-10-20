import type {
	IReportMetric,
	IReportValue,
	ReportAbsentReason,
	ReportMetricSide,
} from "../../../../models/Reports/Report";

export type DirectionOfGood = "lower" | "higher";

export type ChangeVerdict = "better" | "worse" | "neutral";

/** A line rose, fell or did not change; the view decides how that is drawn. */
export type LineMarkKind = "up" | "down" | "none";

export interface ThenNowChange {
	delta: number;
	percent: number | null;
	mark: LineMarkKind;
	verdict: ChangeVerdict;
}

export type ReportSide = "Then" | "Now";

export type ReportMetricSideKey = keyof ReportMetricSide;

/** A line of a panel that carries a change mark: the value it shows, its label and the decimals it is shown to. */
export interface ChangeLine {
	readonly key: ReportMetricSideKey;
	readonly label: string;
	readonly decimals: number;
}

const MINUS = "−";

/** What a value that cannot be shown reads as. */
export const DASH = "—";

const NOT_CAPTURED = "Not captured for this report";

const REASON_FROM_A_NEWER_VERSION =
	"Can't be shown in this version of Lighthouse";

/** Halves round away from zero, so a fall and a rise of the same size read the same. */
export const roundForDisplay = (value: number, decimals: number): number => {
	const scale = 10 ** decimals;
	return (Math.sign(value) * Math.round(Math.abs(value) * scale)) / scale;
};

/** The value as a panel shows it, or null for one that reads as a dash. */
export const shownValue = (
	value: IReportValue | null | undefined,
	decimals: number,
): number | null =>
	value?.value === null || value?.value === undefined
		? null
		: roundForDisplay(value.value, decimals);

/** The value in its unit, or a dash for one that cannot be shown. */
export const shownText = (
	value: IReportValue | null | undefined,
	decimals: number,
	valueText: (value: number) => string,
): string => {
	const shown = shownValue(value, decimals);
	return shown === null ? DASH : valueText(shown);
};

const dayWord = (value: number) => (Math.abs(value) === 1 ? "day" : "days");

export const daysText = (value: number) => `${value} ${dayWord(value)}`;

export const signed = (value: number): string => {
	if (value > 0) return `+${value}`;
	if (value < 0) return `${MINUS}${Math.abs(value)}`;
	return "0";
};

const verdictOf = (delta: number, good: DirectionOfGood): ChangeVerdict => {
	if (delta === 0) return "neutral";
	const fell = delta < 0;
	return fell === (good === "lower") ? "better" : "worse";
};

const markOf = (delta: number): LineMarkKind => {
	if (delta > 0) return "up";
	if (delta < 0) return "down";
	return "none";
};

export const compareThenNow = (
	then: number,
	now: number,
	good: DirectionOfGood,
	decimals: number,
): ThenNowChange => {
	const shownThen = roundForDisplay(then, decimals);
	const delta = roundForDisplay(
		roundForDisplay(now, decimals) - shownThen,
		decimals,
	);
	return {
		delta,
		percent:
			shownThen === 0 ? null : roundForDisplay((delta / shownThen) * 100, 0),
		mark: markOf(delta),
		verdict: verdictOf(delta, good),
	};
};

/** One change line of a panel as the panel judges it; no change where either side reads as a dash. */
export interface LineComparison {
	readonly key: ReportMetricSideKey;
	readonly change: ThenNowChange | null;
}

/** What a panel compares: its change lines and which way is better. */
export interface ComparedLines {
	readonly changeLines: readonly ChangeLine[];
	readonly directionOfGood: DirectionOfGood;
}

/** Each change line of the panel, in its row order, compared as the panel shows it. */
export const lineComparisonsOf = (
	metric: IReportMetric,
	panel: ComparedLines,
): readonly LineComparison[] =>
	panel.changeLines.map(({ key, decimals }) => {
		const then = shownValue(metric.atThen[key], decimals);
		const now = shownValue(metric.atNow[key], decimals);
		return {
			key,
			change:
				then === null || now === null
					? null
					: compareThenNow(then, now, panel.directionOfGood, decimals),
		};
	});

/** "3 days (−20 %)", where the magnitude already carries its unit; a change of nothing reads "No change". Its mark is drawn apart. */
export const changeText = (
	change: ThenNowChange,
	magnitude: string,
): string => {
	if (change.delta === 0) return "No change";
	const percent =
		change.percent === null ? "Then was 0" : `${signed(change.percent)} %`;
	return `${magnitude} (${percent})`;
};

const reasonTexts: Record<
	ReportAbsentReason,
	(side: ReportSide, workItems: string) => string
> = {
	"no-finished-items": (side, workItems) =>
		`No finished ${workItems} in ${side}`,
	unknown: () => REASON_FROM_A_NEWER_VERSION,
};

export const absentReasonText = (
	reason: ReportAbsentReason,
	side: ReportSide,
	workItems: string,
): string => reasonTexts[reason](side, workItems);

/** Why a value reads as a dash; one the server gave no reason for was never captured by the report. */
export const absenceTextOf = (
	value: IReportValue | null | undefined,
	side: ReportSide,
	workItems: string,
): string =>
	value?.absentReason
		? absentReasonText(value.absentReason, side, workItems)
		: NOT_CAPTURED;

/** Each distinct reason once, so several dashes for the same cause do not say it several times. */
export const distinctAbsenceNotesOf = (
	lines: readonly ChangeLine[],
	side: ReportMetricSide,
	sideName: ReportSide,
	workItems: string,
): string[] => {
	const notes = lines
		.filter(({ key, decimals }) => shownValue(side[key], decimals) === null)
		.map(({ key }) => absenceTextOf(side[key], sideName, workItems));
	return [...new Set(notes)];
};
