import type { IReportTemplates } from "../../../../models/Reports/Report";

// Placeholder until the wizard's window check is built; replaced with the real function.
export const __SCAFFOLD__ = true;

/** Why a window entered in the wizard would be refused, by the same codes the server refuses it with. */
export type ReportWindowRefusal =
	| "then-too-short"
	| "then-ends-in-the-future"
	| "then-before-cutoff"
	| "now-too-short"
	| "now-before-cutoff";

/** The bounds the server answered with the report types; the wizard holds no copy of them. */
export type ReportWindowBounds = Pick<
	IReportTemplates,
	"today" | "earliestStart" | "minimumLengthDays"
>;

/** A window as entered: Then by its last day and length, Now by its length alone, as it always ends today. */
export type EnteredReportWindow =
	| { side: "then"; end: string; lengthDays: number }
	| { side: "now"; lengthDays: number };

export const reportWindowCheck = (
	entered: EnteredReportWindow,
	bounds: ReportWindowBounds,
): ReportWindowRefusal | null => {
	throw new Error(
		`Not yet implemented -- RED scaffold: checking a ${entered.side} window of ${entered.lengthDays} days against ${bounds.today}`,
	);
};
