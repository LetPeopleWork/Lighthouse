import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { mappingAutoSaveBlockers } from "../../../utils/stateMappingValidation";
import { refinementSettingsBlockers } from "./RefinementSettingsSection";

// Autosave is the only save trigger, so an invalid form has to name what blocks it.
export function teamAutoSaveBlockers(
	s: ITeamSettings,
	system: IWorkTrackingSystemConnection | null,
	isDefault: boolean,
	getTerm: (key: string) => string,
): string[] {
	const schema = s.dataRetrievalSchema;
	const reasons: string[] = [];
	if (s.name === "") {
		reasons.push("Enter a Name");
	}
	if ((s.throughputHistory ?? 0) <= 0) {
		reasons.push(
			`Set a ${getTerm(TERMINOLOGY_KEYS.THROUGHPUT)} History of at least one day`,
		);
	}
	if (s.featureWIP === undefined) {
		reasons.push(
			`Set a ${getTerm(TERMINOLOGY_KEYS.FEATURE)} ${getTerm(TERMINOLOGY_KEYS.WIP)}`,
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
	if (
		schema?.isWorkItemTypesRequired !== false &&
		s.workItemTypes.length === 0
	) {
		reasons.push(
			`Add at least one ${getTerm(TERMINOLOGY_KEYS.WORK_ITEM)} Type`,
		);
	}
	reasons.push(
		...refinementSettingsBlockers(
			s.refinement,
			getTerm(TERMINOLOGY_KEYS.REFINEMENT),
		),
		...mappingAutoSaveBlockers(s.stateMappings, s),
	);
	if (isDefault) {
		return reasons;
	}
	if (system === null) {
		reasons.push(`Select a ${getTerm(TERMINOLOGY_KEYS.WORK_TRACKING_SYSTEM)}`);
	} else if (
		schema?.isRequired !== false &&
		(s.dataRetrievalValue ?? "") === ""
	) {
		reasons.push(
			`Enter a ${schema?.displayLabel ?? system.workTrackingSystemGetDataRetrievalDisplayName()}`,
		);
	}
	return reasons;
}
