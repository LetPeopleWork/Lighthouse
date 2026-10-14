import { format, isValid, subDays } from "date-fns";
import type {
	INewReport,
	IReportTemplates,
	ReportMetricKey,
} from "../../../../../models/Reports/Report";
import { parseLocalDate } from "../../../../../utils/date/localDate";
import {
	type ReportWindowRefusal,
	reportWindowCheck,
} from "../reportWindowCheck";

const DEFAULT_LENGTH_DAYS = 90;

/** What the user has entered so far; a null field still follows its default, which comes from the server's day. */
export interface ThenAndNowDraft {
	thenEnd: string | null;
	thenLength: string;
	nowLength: string | null;
	name: string | null;
	shownMetricKeys: ReportMetricKey[] | null;
}

export const newThenAndNowDraft = (): ThenAndNowDraft => ({
	thenEnd: null,
	thenLength: String(DEFAULT_LENGTH_DAYS),
	nowLength: null,
	name: null,
	shownMetricKeys: null,
});

export const thenEndOf = (
	draft: ThenAndNowDraft,
	templates: IReportTemplates,
): string => draft.thenEnd ?? templates.today;

const wholeDaysOf = (text: string): number | null => {
	const days = Number(text);
	return text.trim() !== "" && Number.isInteger(days) ? days : null;
};

export const lengthOf = (text: string): number | null => {
	const days = wholeDaysOf(text);
	return days !== null && days > 0 ? days : null;
};

/** Why the server would refuse the Then entered, or null while it has no whole number of days yet or would be accepted. */
export const thenRefusalOf = (
	draft: ThenAndNowDraft,
	templates: IReportTemplates,
): ReportWindowRefusal | null => {
	const lengthDays = wholeDaysOf(draft.thenLength);
	return lengthDays === null
		? null
		: reportWindowCheck(
				{ side: "then", end: thenEndOf(draft, templates), lengthDays },
				templates,
			);
};

/** Now is as long as Then until somebody changes it. */
export const nowLengthOf = (draft: ThenAndNowDraft): string =>
	draft.nowLength ?? draft.thenLength;

const LONG_DAY = "d MMM yyyy";

const daysFromTo = (startDay: Date, endDay: Date): string => {
	const startFormat =
		startDay.getFullYear() === endDay.getFullYear() ? "d MMM" : LONG_DAY;
	return `${format(startDay, startFormat)} – ${format(endDay, LONG_DAY)}`;
};

/** "5 Jul – 2 Oct 2026", or with both years when the days fall in different years; null when no date can name the first day. */
export const resolvedThenDays = (
	end: string,
	lengthDays: number | null,
): string | null => {
	const endDay = parseLocalDate(end);
	if (endDay === null || lengthDays === null) return null;

	const startDay = subDays(endDay, lengthDays - 1);
	if (!isValid(startDay)) return null;

	return daysFromTo(startDay, endDay);
};

/** A window's days as resolvedThenDays writes them, from its first and last day; null when either is not a day. */
export const windowDaysOf = (start: string, end: string): string | null => {
	const startDay = parseLocalDate(start);
	const endDay = parseLocalDate(end);
	return startDay === null || endDay === null
		? null
		: daysFromTo(startDay, endDay);
};

export const defaultNameOf = (
	draft: ThenAndNowDraft,
	templates: IReportTemplates,
): string => {
	const endDay = parseLocalDate(thenEndOf(draft, templates));
	const lengthDays = lengthOf(draft.thenLength);
	if (endDay === null || lengthDays === null) return "Then & Now";
	return `Then & Now — ${lengthDays} days to ${format(endDay, LONG_DAY)}`;
};

export const nameOf = (
	draft: ThenAndNowDraft,
	templates: IReportTemplates,
): string => draft.name ?? defaultNameOf(draft, templates);

/** The metrics the server offers for Then & Now. */
export const offeredMetricsOf = (
	templates: IReportTemplates,
): ReportMetricKey[] =>
	(
		templates.templates.find(
			(template) => template.templateKey === "then-and-now",
		)?.metrics ?? []
	).map((metric) => metric.key);

export const shownMetricsOf = (
	draft: ThenAndNowDraft,
	templates: IReportTemplates,
): ReportMetricKey[] => draft.shownMetricKeys ?? offeredMetricsOf(templates);

/** The report as the server is asked to create it; only called once every step is complete. */
export const newReportOf = (
	draft: ThenAndNowDraft,
	templates: IReportTemplates,
): INewReport => ({
	templateKey: "then-and-now",
	name: nameOf(draft, templates),
	shownMetricKeys: shownMetricsOf(draft, templates),
	thenAndNow: {
		thenEndDate: thenEndOf(draft, templates),
		thenLengthDays: lengthOf(draft.thenLength) ?? 0,
		nowLengthDays: lengthOf(nowLengthOf(draft)),
	},
});
