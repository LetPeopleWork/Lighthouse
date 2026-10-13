import type { IReportTemplates } from "../../../../models/Reports/Report";

/** Why a window entered in the wizard would be refused, by the same codes the server refuses it with. */
export type ReportWindowRefusal =
	| "then-too-short"
	| "then-too-long"
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

const millisecondsPerDay = 86_400_000;

// Days are counted on the server's yyyy-MM-dd strings read as UTC, so the viewer's own zone can never shift a
// window by a day.
const dayNumber = (day: string): number => {
	const [year, month, date] = day.split("-").map(Number);
	return Date.UTC(year, month - 1, date) / millisecondsPerDay;
};

// 1 Jan of the year 1, the first day the server's calendar can name, counted the same way.
const FIRST_CALENDAR_DAY = -719_162;

const startDayNumber = (end: string, lengthDays: number): number =>
	dayNumber(end) - (lengthDays - 1);

const startsBefore = (
	earliestStart: string | null,
	end: string,
	lengthDays: number,
): boolean =>
	earliestStart !== null &&
	startDayNumber(end, lengthDays) < dayNumber(earliestStart);

const thenRefusal = (
	end: string,
	lengthDays: number,
	bounds: ReportWindowBounds,
): ReportWindowRefusal | null => {
	if (lengthDays < bounds.minimumLengthDays) {
		return "then-too-short";
	}

	if (startDayNumber(end, lengthDays) < FIRST_CALENDAR_DAY) {
		return "then-too-long";
	}

	if (dayNumber(end) > dayNumber(bounds.today)) {
		return "then-ends-in-the-future";
	}

	return startsBefore(bounds.earliestStart, end, lengthDays)
		? "then-before-cutoff"
		: null;
};

export const reportWindowCheck = (
	entered: EnteredReportWindow,
	bounds: ReportWindowBounds,
): ReportWindowRefusal | null => {
	if (entered.side === "then") {
		return thenRefusal(entered.end, entered.lengthDays, bounds);
	}

	throw new Error("A Now window is not checked in the wizard yet");
};
