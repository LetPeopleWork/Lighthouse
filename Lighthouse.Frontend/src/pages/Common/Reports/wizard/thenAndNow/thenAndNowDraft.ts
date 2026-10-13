import { format, subDays } from "date-fns";
import type {
	IReportOwner,
	IReportTemplates,
	ReportMetricKey,
} from "../../../../../models/Reports/Report";
import { parseLocalDate } from "../../../../../utils/date/localDate";
import { panelFor } from "../../panels/reportPanelRegistry";

const DEFAULT_LENGTH_DAYS = 90;

/** What the user has entered so far; a null field still follows its default, which comes from the server's day. */
export interface ThenAndNowDraft {
	thenEnd: string | null;
	thenLength: string;
	name: string | null;
	shownMetricKeys: ReportMetricKey[] | null;
}

export const newThenAndNowDraft = (): ThenAndNowDraft => ({
	thenEnd: null,
	thenLength: String(DEFAULT_LENGTH_DAYS),
	name: null,
	shownMetricKeys: null,
});

export const lengthPresetsFor = (owner: IReportOwner): readonly number[] =>
	owner.kind === "team" ? [30, 90] : [90, 180];

export const thenEndOf = (
	draft: ThenAndNowDraft,
	templates: IReportTemplates,
): string => draft.thenEnd ?? templates.today;

export const lengthOf = (text: string): number | null => {
	const days = Number(text);
	return text.trim() !== "" && Number.isInteger(days) && days > 0 ? days : null;
};

const LONG_DAY = "d MMM yyyy";

/** "5 Jul – 2 Oct 2026", or with both years when the days fall in different years. */
export const resolvedThenDays = (
	end: string,
	lengthDays: number | null,
): string | null => {
	const endDay = parseLocalDate(end);
	if (endDay === null || lengthDays === null) return null;

	const startDay = subDays(endDay, lengthDays - 1);
	const startFormat =
		startDay.getFullYear() === endDay.getFullYear() ? "d MMM" : LONG_DAY;
	return `${format(startDay, startFormat)} – ${format(endDay, LONG_DAY)}`;
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

/** The metrics the server offers that this version has a panel for. */
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
