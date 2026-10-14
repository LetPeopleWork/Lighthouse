import type React from "react";
import type {
	INewReport,
	IReport,
	IReportOwner,
	IReportTemplates,
	ReportChange,
	ReportTemplateKey,
} from "../../../../models/Reports/Report";
import { parseLocalDate } from "../../../../utils/date/localDate";
import MetricsAndNameStep from "./thenAndNow/MetricsAndNameStep";
import NowStep from "./thenAndNow/NowStep";
import ThenStep from "./thenAndNow/ThenStep";
import {
	lengthOf,
	nameOf,
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
	},
};

/** The title of a Type as the server names it, or undefined for one this version does not know. */
export const reportTypeTitleOf = (templateKey: string): string | undefined =>
	Object.keys(reportTypes).includes(templateKey)
		? reportTypes[templateKey as ReportTemplateKey].title
		: undefined;
