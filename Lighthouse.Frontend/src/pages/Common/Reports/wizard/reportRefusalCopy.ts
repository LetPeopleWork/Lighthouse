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
	| "report-cap-reached"
	| "owner-not-found"
	| "not-admin"
	| "not-saved";

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
const MADE_BY_A_NEWER_VERSION =
	"This report was made by a newer version of Lighthouse and can't be shown here.";

const isMadeByANewerVersion = (error: unknown): boolean =>
	error instanceof ApiError &&
	error.code === 422 &&
	error.problemCode === "template-not-supported";

/** What to say about a report read that asking again will not change; nothing for one that might. */
export const finalFailureOf = (error: unknown): string | undefined => {
	if (isMadeByANewerVersion(error)) return MADE_BY_A_NEWER_VERSION;
	if (error instanceof ApiError && error.code === 404) return NO_LONGER_EXISTS;
	return undefined;
};

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
		"owner-not-found": (words) => `This ${words.ownerTerm} no longer exists.`,
		"not-admin": (words) =>
			`You are no longer Admin of this ${words.ownerTerm}.`,
		"not-saved": () => COULD_NOT_SAVE,
	};

/** What one owner's line of a bulk run names: the refusal's words, and that owner. */
export interface NotCreatedWords extends RefusalWords {
	ownerName: string;
}

const noLongerAdminOf = (ownerName: string): string =>
	`you are no longer Admin of ${ownerName}.`;

const historyStartSentence = (words: NotCreatedWords): string =>
	`its kept history starts ${oldestKeptDay(words.earliestStart)}. Keep more history under Closed Items Cutoff in its settings.`;

/** Where a bulk line words a refusal for its owner rather than repeat the single wizard's sentence. */
const NOT_CREATED_REASON: Partial<
	Record<ReportRefusalCode, (words: NotCreatedWords) => string>
> = {
	"not-admin": (words) => noLongerAdminOf(words.ownerName),
	"owner-not-found": (words) => `${words.ownerName} no longer exists.`,
	"report-cap-reached": (words) =>
		`${words.ownerName} already has its one Then & Now report; Premium lifts this limit.`,
	"then-before-cutoff": historyStartSentence,
	"now-before-cutoff": historyStartSentence,
	"not-saved": () => "it couldn't be saved. Try again.",
};

const notCreatedReasonOf = (
	code: string | undefined,
	words: NotCreatedWords,
): string => {
	if (code === undefined || !isRefusalCode(code)) return COULD_NOT_CREATE;
	return (NOT_CREATED_REASON[code] ?? REFUSAL_COPY[code])(words);
};

/** The line for an owner a bulk run did not create a report on, saying why in plain words. */
export const notCreatedLineFor = (
	code: string | undefined,
	words: NotCreatedWords,
): string => `Not created: ${notCreatedReasonOf(code, words)}`;

export const COULD_NOT_DELETE = "This report couldn't be deleted. Try again.";

/** The line for a report a bulk removal left in place; anything but lost Admin reads as removing one report does. */
export const notRemovedLineFor = (
	code: string | undefined,
	words: Readonly<{ ownerName: string }>,
): string =>
	code === "not-admin"
		? `Not removed: ${noLongerAdminOf(words.ownerName)}`
		: `Not removed: ${COULD_NOT_DELETE}`;

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
