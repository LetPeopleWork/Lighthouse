import type React from "react";
import type {
	INewReport,
	IReport,
	IReportOwner,
	IReportTemplates,
	ReportChange,
	ReportTemplateKey,
} from "../../../../models/Reports/Report";
import type { IReportCreationOptions } from "../../../../models/Reports/ReportsAcrossOwners";
import { parseLocalDate } from "../../../../utils/date/localDate";
import MetricsAndNameStep from "./thenAndNow/MetricsAndNameStep";
import NowStep from "./thenAndNow/NowStep";
import ThenStep from "./thenAndNow/ThenStep";
import {
	lengthOf,
	nameOf,
	newBackToBackDraft,
	newReportForOwnersOf,
	newReportOf,
	newThenAndNowDraft,
	nowLengthOf,
	nowRefusalOf,
	reportChangeOf,
	shownMetricsOf,
	type ThenAndNowDraft,
	thenAndNowDraftOf,
	thenEndOf,
	thenRefusalOf,
} from "./thenAndNow/thenAndNowDraft";

export interface ReportTypeStepProps<TDraft> {
	owner: IReportOwner;
	templates: IReportTemplates;
	draft: TDraft;
	onChange: (draft: TDraft) => void;
	disabled: boolean;
	/** Shown as the report was created and never changed, because the report is being edited. */
	frozen?: boolean;
	/** The window lengths offered with one click, when they are not the owner's own. */
	windowPresets?: readonly number[];
}

export interface ReportTypeStep<TDraft> {
	label: string;
	component: React.FC<Readonly<ReportTypeStepProps<TDraft>>>;
	isComplete: (draft: TDraft, templates: IReportTemplates) => boolean;
	frozenOnEdit?: boolean;
}

/** A type of report: how the first step offers it, and the steps it adds after that one. */
export interface ReportTypeDefinition<TDraft> {
	title: string;
	purpose: string;
	newDraft: () => TDraft;
	steps: readonly ReportTypeStep<TDraft>[];
	toNewReport: (draft: TDraft, templates: IReportTemplates) => INewReport;
	draftOf: (report: IReport) => TDraft;
	toChange: (draft: TDraft, templates: IReportTemplates) => ReportChange;
	/** What the type configures before any owner is picked, when one report is created for several owners. */
	forOwners: {
		newDraft: () => TDraft;
		windowPresets: readonly number[];
		toNewReport: (draft: TDraft, templates: IReportTemplates) => INewReport;
	};
}

interface DraftByType {
	"then-and-now": ThenAndNowDraft;
}

export const reportTypes: {
	[K in ReportTemplateKey]: ReportTypeDefinition<DraftByType[K]>;
} = {
	"then-and-now": {
		title: "Then & Now",
		purpose: "Set today's flow against a window that stays as it was.",
		newDraft: newThenAndNowDraft,
		steps: [
			{
				label: "Then",
				component: ThenStep,
				frozenOnEdit: true,
				isComplete: (draft, templates) =>
					parseLocalDate(thenEndOf(draft, templates)) !== null &&
					lengthOf(draft.thenLength) !== null &&
					thenRefusalOf(draft, templates) === null,
			},
			{
				label: "Now",
				component: NowStep,
				isComplete: (draft, templates) =>
					lengthOf(nowLengthOf(draft)) !== null &&
					nowRefusalOf(draft, templates) === null,
			},
			{
				label: "Metrics & name",
				component: MetricsAndNameStep,
				isComplete: (draft, templates) =>
					nameOf(draft, templates).trim() !== "" &&
					shownMetricsOf(draft, templates).length > 0,
			},
		],
		toNewReport: newReportOf,
		draftOf: thenAndNowDraftOf,
		toChange: reportChangeOf,
		forOwners: {
			newDraft: newBackToBackDraft,
			windowPresets: [30, 90, 180],
			toNewReport: newReportForOwnersOf,
		},
	},
};

/**
 * What a report needs, read for several owners at once, in the shape the steps read for one owner. No owner's
 * kept history is known yet, so none bounds the windows here; every metric any kind of owner offers is offered.
 */
export const templatesForOwnersOf = (
	options: IReportCreationOptions,
): IReportTemplates => ({
	today: options.today,
	earliestStart: null,
	minimumLengthDays: options.minimumLengthDays,
	templates: options.templates.map((template) => ({
		templateKey: template.templateKey,
		metrics: [
			...new Set([
				...template.metricKeysByOwnerKind.team,
				...template.metricKeysByOwnerKind.portfolio,
			]),
		].map((key) => ({ key })),
	})),
});

/** The title of a Type as the server names it, or undefined for one this version does not know. */
export const reportTypeTitleOf = (templateKey: string): string | undefined =>
	Object.keys(reportTypes).includes(templateKey)
		? reportTypes[templateKey as ReportTemplateKey].title
		: undefined;

/** Whether this version can show a report of the Type, and so edit or print it. */
export const isShownByThisVersion = (templateKey: string): boolean =>
	reportTypeTitleOf(templateKey) !== undefined;
