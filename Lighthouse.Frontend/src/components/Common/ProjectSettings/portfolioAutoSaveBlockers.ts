import type { IPortfolioSettings } from "../../../models/Portfolio/PortfolioSettings";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import {
	type IStoredStateSettings,
	mappingAutoSaveBlockers,
} from "../../../utils/stateMappingValidation";

// Autosave is the only save trigger, so an invalid form has to name what blocks it.
export function portfolioAutoSaveBlockers(
	s: IPortfolioSettings,
	system: IWorkTrackingSystemConnection | null,
	isDefault: boolean,
	getTerm: (key: string) => string,
	stored: IStoredStateSettings | undefined,
): string[] {
	const schema = s.dataRetrievalSchema;
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const reasons: string[] = [];
	if (s.name === "") {
		reasons.push("Enter a Name");
	}
	if (s.defaultAmountOfWorkItemsPerFeature === undefined) {
		reasons.push(
			`Set a Default Number of ${getTerm(TERMINOLOGY_KEYS.WORK_ITEMS)} per ${featureTerm}`,
		);
	}
	if (
		s.usePercentileToCalculateDefaultAmountOfWorkItems &&
		!(s.defaultWorkItemPercentile > 0 && s.percentileHistoryInDays >= 30)
	) {
		reasons.push(
			`Set a ${featureTerm} Size Percentile above 0 and a History in Days of at least 30`,
		);
	}
	if (
		schema?.isWorkItemTypesRequired !== false &&
		s.workItemTypes.length === 0
	) {
		reasons.push(
			`Add at least one ${getTerm(TERMINOLOGY_KEYS.WORK_ITEM)} Type`,
		);
	}
	if (s.toDoStates.length === 0) {
		reasons.push("Add at least one To Do State");
	}
	if (s.doingStates.length === 0) {
		reasons.push("Add at least one Doing State");
	}
	if (s.doneStates.length === 0) {
		reasons.push("Add at least one Done State");
	}
	reasons.push(...mappingAutoSaveBlockers(s.stateMappings, s, stored));
	if (isDefault) {
		return reasons;
	}
	reasons.push(...connectionBlockers(s, system, getTerm));
	return reasons;
}

function connectionBlockers(
	s: IPortfolioSettings,
	system: IWorkTrackingSystemConnection | null,
	getTerm: (key: string) => string,
): string[] {
	if (system === null) {
		return [`Select a ${getTerm(TERMINOLOGY_KEYS.WORK_TRACKING_SYSTEM)}`];
	}
	const schema = s.dataRetrievalSchema;
	if (schema?.isRequired !== false && (s.dataRetrievalValue ?? "") === "") {
		return [
			`Enter a ${schema?.displayLabel ?? system.workTrackingSystemGetDataRetrievalDisplayName()}`,
		];
	}
	return [];
}
