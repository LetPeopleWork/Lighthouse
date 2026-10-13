import type { ReportAbsentReason } from "../../../../models/Reports/Report";

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

/** Halves round away from zero, so a fall and a rise of the same size read the same. */
export const roundForDisplay = (value: number, decimals: number): number => {
	const scale = 10 ** decimals;
	return (Math.sign(value) * Math.round(Math.abs(value) * scale)) / scale;
};

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

const reasonTexts: Record<
	ReportAbsentReason,
	(side: ReportSide, workItems: string) => string
> = {
	"no-finished-items": (side, workItems) =>
		`No finished ${workItems} in ${side}`,
	"too-few-points": (side) => `${side} has too few points for limits`,
	"no-process": () => "Then never varied from 0",
	"no-wip": (_side, workItems) => `No ${workItems} in progress`,
	"lower-limit-zero": () => "Then's lower limit is 0",
};

export const absentReasonText = (
	reason: ReportAbsentReason,
	side: ReportSide,
	workItems: string,
): string => reasonTexts[reason](side, workItems);
