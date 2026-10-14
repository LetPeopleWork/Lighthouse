import { format } from "date-fns";
import type { IReportOwner } from "../../../../models/Reports/Report";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import { ApiError } from "../../../../services/Api/ApiError";
import { useTerminology } from "../../../../services/TerminologyContext";
import { parseLocalDate } from "../../../../utils/date/localDate";
import { ownerTermKeyOf } from "../reportOwner";
import type {
	ReportWindowBounds,
	ReportWindowRefusal,
} from "./reportWindowCheck";

/** Every reason the server gives for refusing a report it was asked to create or change. */
export type ReportRefusalCode =
	| ReportWindowRefusal
	| "name-missing"
	| "name-too-long"
	| "no-metric-shown"
	| "unknown-metric"
	| "metric-not-captured"
	| "template-not-supported"
	| "report-cap-reached";

/** What a refusal's sentence may name: the server's bounds and the instance's own words. */
export interface RefusalWords {
	minimumLengthDays: number;
	earliestStart: string | null;
	ownerTerm: string;
	workItemsTerm: string;
}

/** The words a refusal names, from the bounds the server answered and the instance's own terms. */
export const useRefusalWords = (
	owner: IReportOwner,
): ((bounds: ReportWindowBounds) => RefusalWords) => {
	const { getTerm } = useTerminology();
	return (bounds) => ({
		minimumLengthDays: bounds.minimumLengthDays,
		earliestStart: bounds.earliestStart,
		ownerTerm: getTerm(ownerTermKeyOf(owner)),
		workItemsTerm: getTerm(TERMINOLOGY_KEYS.WORK_ITEMS),
	});
};

const COULD_NOT_CREATE = "The report couldn't be created. Try again.";
export const COULD_NOT_SAVE = "The report couldn't be saved. Try again.";
export const NO_LONGER_EXISTS = "This report no longer exists.";
export const COULD_NOT_LOAD_REPORT =
	"This report couldn't be loaded. Reload to try again.";

/** Why a Community instance creates no further report for this Team or Portfolio. */
export const capSentenceFor = (ownerTerm: string): string =>
	`Community includes 1 Then & Now report per ${ownerTerm}. Delete it, or use Premium for unlimited reports.`;

const oldestKeptDay = (earliestStart: string | null): string => {
	const day = earliestStart === null ? null : parseLocalDate(earliestStart);
	return day === null ? "the oldest kept day" : format(day, "d MMM yyyy");
};

const cutoffRefusal = (side: "Then" | "Now", words: RefusalWords): string =>
	`The ${side} window must start on or after ${oldestKeptDay(words.earliestStart)} — older finished ${words.workItemsTerm} are no longer kept.`;

const REFUSAL_COPY: Record<ReportRefusalCode, (words: RefusalWords) => string> =
	{
		"then-too-short": (words) =>
			`Then needs at least ${words.minimumLengthDays} days.`,
		"then-too-long": () => "Then can't reach back that far.",
		"then-ends-in-the-future": () => "Then cannot end after today.",
		"then-before-cutoff": (words) => cutoffRefusal("Then", words),
		"now-too-short": (words) =>
			`Now needs at least ${words.minimumLengthDays} days.`,
		"now-too-long": () => "Now can't reach back that far.",
		"now-before-cutoff": (words) => cutoffRefusal("Now", words),
		"name-missing": () => "Give the report a name.",
		"name-too-long": () => "A report name can be at most 200 characters.",
		"no-metric-shown": () => "Choose at least one metric",
		"unknown-metric": () =>
			"One of the chosen metrics isn't known to this version of Lighthouse.",
		"metric-not-captured": () =>
			"One of the chosen metrics was not captured for this report.",
		"template-not-supported": () =>
			"This Type of report can't be created by this version of Lighthouse.",
		"report-cap-reached": (words) => capSentenceFor(words.ownerTerm),
	};

/** The sentence for a window the wizard itself refuses, worded as the server's refusal would be. */
export const windowRefusalCopyFor = (
	refusal: ReportWindowRefusal,
	words: RefusalWords,
): string => REFUSAL_COPY[refusal](words);

const isRefusalCode = (code: string): code is ReportRefusalCode =>
	Object.keys(REFUSAL_COPY).includes(code);

/**
 * The sentence for a failed create or save: the refusal's own copy, or a plain retry for anything else. A day
 * the refusal names itself wins over the one the wizard was opened with, which may be a day old by now.
 */
export const refusalCopyFor = (
	error: unknown,
	words: RefusalWords,
	couldNot: string = COULD_NOT_CREATE,
): string => {
	if (!(error instanceof ApiError)) return couldNot;

	const code = error.problemCode;
	if (code === undefined || !isRefusalCode(code)) return couldNot;

	return REFUSAL_COPY[code]({
		...words,
		earliestStart:
			error.earliestStart === undefined
				? words.earliestStart
				: error.earliestStart,
	});
};
