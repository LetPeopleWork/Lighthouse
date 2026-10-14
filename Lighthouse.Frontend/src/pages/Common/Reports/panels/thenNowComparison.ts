import type {
	IReportValue,
	ReportAbsentReason,
} from "../../../../models/Reports/Report";

export type DirectionOfGood = "lower" | "higher";

export type ChangeVerdict = "better" | "worse" | "neutral";

export interface ThenNowChange {
	delta: number;
	percent: number | null;
	arrow: "▲" | "▼" | "";
	verdict: ChangeVerdict;
}

export type ReportSide = "Then" | "Now";

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

const arrowOf = (delta: number): ThenNowChange["arrow"] => {
	if (delta > 0) return "▲";
	if (delta < 0) return "▼";
	return "";
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
		arrow: arrowOf(delta),
		verdict: verdictOf(delta, good),
	};
};

/** "▼ 3 days (−20 %)", where the magnitude already carries its unit; a change of nothing reads "No change". */
export const changeText = (
	change: ThenNowChange,
	magnitude: string,
): string => {
	if (change.delta === 0) return "No change";
	const percent =
		change.percent === null ? "Then was 0" : `${signed(change.percent)} %`;
	return `${change.arrow} ${magnitude} (${percent})`;
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
