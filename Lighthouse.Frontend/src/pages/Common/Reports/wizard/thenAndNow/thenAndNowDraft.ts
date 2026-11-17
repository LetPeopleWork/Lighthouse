import { format, isValid, subDays } from "date-fns";
import type {
	INewReport,
	IReport,
	IReportTemplates,
	ReportChange,
	ReportMetricKey,
} from "../../../../../models/Reports/Report";
import {
	formatLocalDate,
	parseLocalDate,
} from "../../../../../utils/date/localDate";
import { panelFor } from "../../panels/reportPanelRegistry";
import { LONG_DAY } from "../longDay";
import {
	type EnteredReportWindow,
	type ReportWindowRefusal,
	reportWindowCheck,
} from "../reportWindowCheck";

const DEFAULT_LENGTH_DAYS = 90;

/** What the user has entered so far; a null field still follows its default, which comes from the server's day. */
export interface ThenAndNowDraft {
	thenEnd: string | null;
	thenLength: string;
	name: string | null;
	shownMetricKeys: ReportMetricKey[] | null;
	/** The metrics a report being edited captured when it was created; a new report captures every one offered. */
	capturedMetricKeys?: ReportMetricKey[];
}

export const newThenAndNowDraft = (): ThenAndNowDraft => ({
	thenEnd: null,
	thenLength: String(DEFAULT_LENGTH_DAYS),
	name: null,
	shownMetricKeys: null,
});

/** The report as it was read, with every field entered, so nothing follows a default that the server's today would move. */
export const thenAndNowDraftOf = (report: IReport): ThenAndNowDraft => ({
	thenEnd: report.thenAndNow.thenWindow.end,
	thenLength: String(report.thenAndNow.thenWindow.lengthDays),
	name: report.name,
	shownMetricKeys: [...report.shownMetricKeys],
	capturedMetricKeys: (
		Object.keys(report.thenAndNow.metrics) as ReportMetricKey[]
	).filter((key) => (report.thenAndNow.metrics[key] ?? null) !== null),
});

export const isCaptured = (
	draft: ThenAndNowDraft,
	key: ReportMetricKey,
): boolean =>
	draft.capturedMetricKeys === undefined ||
	draft.capturedMetricKeys.includes(key);

// Now is as long as Then and ends today, so the day before it starts lies Then's length before today.
const dayBeforeNowStarts = (
	draft: ThenAndNowDraft,
	templates: IReportTemplates,
): string => {
	const today = parseLocalDate(templates.today);
	const lengthDays = lengthOf(draft.thenLength);
	if (today === null || lengthDays === null) return templates.today;
	const day = subDays(today, lengthDays);
	return isValid(day) ? formatLocalDate(day) : templates.today;
};

/** The day Then ends: the one entered, or until then the day before Now starts, so the windows sit back to back. */
export const thenEndOf = (
	draft: ThenAndNowDraft,
	templates: IReportTemplates,
): string => draft.thenEnd ?? dayBeforeNowStarts(draft, templates);

const wholeDaysOf = (text: string): number | null => {
	const days = Number(text);
	return text.trim() !== "" && Number.isInteger(days) ? days : null;
};

export const lengthOf = (text: string): number | null => {
	const days = wholeDaysOf(text);
	return days !== null && days > 0 ? days : null;
};

const refusalOnceWhole = (
	lengthText: string,
	windowOf: (lengthDays: number) => EnteredReportWindow,
	templates: IReportTemplates,
): ReportWindowRefusal | null => {
	const lengthDays = wholeDaysOf(lengthText);
	return lengthDays === null
		? null
		: reportWindowCheck(windowOf(lengthDays), templates);
};

/** Why the server would refuse the Then entered, or null while it has no whole number of days yet or would be accepted. */
export const thenRefusalOf = (
	draft: ThenAndNowDraft,
	templates: IReportTemplates,
): ReportWindowRefusal | null =>
	refusalOnceWhole(
		draft.thenLength,
		(lengthDays) => ({
			side: "then",
			end: thenEndOf(draft, templates),
			lengthDays,
		}),
		templates,
	);

/** A window by its first and last day, as yyyy-MM-dd, and its length. */
export interface ReportWindow {
	start: string;
	end: string;
	lengthDays: number;
}

const windowEndingOn = (
	endDay: Date,
	lengthDays: number,
): ReportWindow | null => {
	const startDay = subDays(endDay, lengthDays - 1);
	return isValid(startDay)
		? {
				start: formatLocalDate(startDay),
				end: formatLocalDate(endDay),
				lengthDays,
			}
		: null;
};

/**
 * Then as entered, and Now as long as Then ending on the server's today; null while Then's end or length is no
 * day or whole number of days, or a window would start before the calendar does.
 */
export const windowsOf = (
	draft: ThenAndNowDraft,
	templates: IReportTemplates,
): { then: ReportWindow; now: ReportWindow } | null => {
	const lengthDays = lengthOf(draft.thenLength);
	const thenEnd = parseLocalDate(thenEndOf(draft, templates));
	const today = parseLocalDate(templates.today);
	if (lengthDays === null || thenEnd === null || today === null) return null;

	const then = windowEndingOn(thenEnd, lengthDays);
	const now = windowEndingOn(today, lengthDays);
	return then === null || now === null ? null : { then, now };
};

/** How Then sits beside Now: on the same days, sharing days up to Then's last one, or apart. */
export type WindowsRelation =
	| { kind: "apart" }
	| { kind: "sameDays" }
	| { kind: "overlapUntil"; until: string };

export const windowsRelationOf = (
	draft: ThenAndNowDraft,
	templates: IReportTemplates,
): WindowsRelation | null => {
	const windows = windowsOf(draft, templates);
	if (windows === null) return null;

	// yyyy-MM-dd days compare as text in the order of the days they name.
	const { then, now } = windows;
	if (then.end === now.end) return { kind: "sameDays" };
	return then.end >= now.start
		? { kind: "overlapUntil", until: then.end }
		: { kind: "apart" };
};

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

/** The metrics the server offers for Then & Now that this version has a panel to show. */
export const offeredMetricsOf = (
	templates: IReportTemplates,
): ReportMetricKey[] =>
	(
		templates.templates.find(
			(template) => template.templateKey === "then-and-now",
		)?.metrics ?? []
	)
		.map((metric) => metric.key)
		.filter((key) => panelFor(key) !== undefined);

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
	},
});

/** The report as several owners get it: each owner's Now is as long as its Then, as for one owner. */
export const newReportForOwnersOf = newReportOf;

/**
 * What saving an edit changes; Then is left out, it stays as it was frozen. Now's length is sent as Then's, the
 * only length Now can have. Only called once every step is complete.
 */
export const reportChangeOf = (
	draft: ThenAndNowDraft,
	templates: IReportTemplates,
): ReportChange => ({
	name: nameOf(draft, templates),
	shownMetricKeys: shownMetricsOf(draft, templates),
	thenAndNow: { nowLengthDays: lengthOf(draft.thenLength) ?? 0 },
});
