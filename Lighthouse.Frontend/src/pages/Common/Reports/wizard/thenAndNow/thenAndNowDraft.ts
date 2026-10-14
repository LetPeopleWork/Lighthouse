import { format, isValid, subDays } from "date-fns";
import type {
	INewReport,
	IReport,
	IReportTemplates,
	ReportChange,
	ReportMetricKey,
} from "../../../../../models/Reports/Report";
import { parseLocalDate } from "../../../../../utils/date/localDate";
import { panelFor } from "../../panels/reportPanelRegistry";
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
	nowLength: string | null;
	name: string | null;
	shownMetricKeys: ReportMetricKey[] | null;
	/** The metrics a report being edited captured when it was created; a new report captures every one offered. */
	capturedMetricKeys?: ReportMetricKey[];
}

export const newThenAndNowDraft = (): ThenAndNowDraft => ({
	thenEnd: null,
	thenLength: String(DEFAULT_LENGTH_DAYS),
	nowLength: null,
	name: null,
	shownMetricKeys: null,
});

/** The report as it was read, with every field entered, so nothing follows a default that the server's today would move. */
export const thenAndNowDraftOf = (report: IReport): ThenAndNowDraft => ({
	thenEnd: report.thenAndNow.thenWindow.end,
	thenLength: String(report.thenAndNow.thenWindow.lengthDays),
	nowLength: String(report.thenAndNow.nowWindow.lengthDays),
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

/** Now is as long as Then until somebody changes it. */
export const nowLengthOf = (draft: ThenAndNowDraft): string =>
	draft.nowLength ?? draft.thenLength;

/** Why the server would refuse the Now entered, or null while it has no whole number of days yet or would be accepted. */
export const nowRefusalOf = (
	draft: ThenAndNowDraft,
	templates: IReportTemplates,
): ReportWindowRefusal | null =>
	refusalOnceWhole(
		nowLengthOf(draft),
		(lengthDays) => ({ side: "now", lengthDays }),
		templates,
	);

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
		nowLengthDays: lengthOf(nowLengthOf(draft)),
	},
});

/** What saving an edit changes; Then is left out, it stays as it was frozen. Only called once every step is complete. */
export const reportChangeOf = (
	draft: ThenAndNowDraft,
	templates: IReportTemplates,
): ReportChange => ({
	name: nameOf(draft, templates),
	shownMetricKeys: shownMetricsOf(draft, templates),
	thenAndNow: { nowLengthDays: lengthOf(nowLengthOf(draft)) ?? 0 },
});
