import type React from "react";
import type {
	INewReport,
	IReportOwner,
	IReportTemplates,
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
	shownMetricsOf,
	type ThenAndNowDraft,
	thenEndOf,
} from "./thenAndNow/thenAndNowDraft";

export interface ReportTypeStepProps<TDraft> {
	owner: IReportOwner;
	templates: IReportTemplates;
	draft: TDraft;
	onChange: (draft: TDraft) => void;
	disabled: boolean;
}

export interface ReportTypeStep<TDraft> {
	label: string;
	component: React.FC<Readonly<ReportTypeStepProps<TDraft>>>;
	isComplete: (draft: TDraft, templates: IReportTemplates) => boolean;
}

/** A type of report: how the first step offers it, and the steps it adds after that one. */
export interface ReportTypeDefinition<TDraft> {
	title: string;
	purpose: string;
	newDraft: () => TDraft;
	steps: readonly ReportTypeStep<TDraft>[];
	toNewReport: (draft: TDraft, templates: IReportTemplates) => INewReport;
}

interface DraftByType {
	"then-and-now": ThenAndNowDraft;
}

export const reportTypeSteps: {
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
				isComplete: (draft, templates) =>
					parseLocalDate(thenEndOf(draft, templates)) !== null &&
					lengthOf(draft.thenLength) !== null,
			},
			{
				label: "Now",
				component: NowStep,
				isComplete: (draft) => lengthOf(nowLengthOf(draft)) !== null,
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
	},
};
