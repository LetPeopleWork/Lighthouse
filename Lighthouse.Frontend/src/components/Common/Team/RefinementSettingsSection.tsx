import Grid from "@mui/material/Grid";
import type React from "react";
import { useEffect, useRef } from "react";
import type {
	IRefinementSettings,
	IRefinementStateSetting,
} from "../../../models/Refinement/Refinement";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";
import InputGroup from "../InputGroup/InputGroup";
import ItemListManager from "../ItemListManager/ItemListManager";
import ReadinessSettings, {
	DEFAULT_READINESS,
	hasReadinessErrors,
} from "./ReadinessSettings";
import RefinementBandSettings from "./RefinementBandSettings";
import RefinementCadenceSettings, {
	hasCadenceErrors,
} from "./RefinementCadenceSettings";
import { DEFAULT_BAND, hasBandErrors } from "./refinementBand";
import StageRulesSettings, {
	hasIncompleteStageRule,
	NO_STAGE_RULES,
} from "./StageRulesSettings";

/** Why the Refinement settings cannot be saved as they are; empty when they can. */
export const refinementSettingsBlockers = (
	refinement: IRefinementSettings | null | undefined,
	refinementTerm: string,
): string[] => {
	const reasons: string[] = [];
	const readiness = refinement?.readiness;
	if (readiness && hasReadinessErrors(readiness)) {
		reasons.push("Correct Readiness by votes");
	}
	if (hasIncompleteStageRule(refinement?.stageRules)) {
		reasons.push("Complete the stage rules");
	}
	const cadence = refinement?.cadence;
	if (cadence && hasCadenceErrors(cadence)) {
		reasons.push(`Complete the ${refinementTerm} cadence`);
	}
	const band = refinement?.band;
	if (band && hasBandErrors(band)) {
		reasons.push("Correct the low and high end likelihoods");
	}
	return reasons;
};

interface RefinementSettingsSectionProps {
	teamId: number;
	toDoStates: string[];
	doingStates: string[];
	refinement: IRefinementSettings | null | undefined;
	/** Receives only the part of the settings that changed. */
	onChange: (change: Partial<IRefinementSettings>) => void;
}

const toSettings = (states: string[]): IRefinementStateSetting[] =>
	states.map((state) => ({ state }));

const RefinementSettingsSection: React.FC<RefinementSettingsSectionProps> = ({
	teamId,
	toDoStates,
	doingStates,
	refinement,
	onChange,
}) => {
	const { getTerm } = useTerminology();

	const chosenStates = refinement?.states ?? [];
	const suggestions = [...toDoStates, ...doingStates];
	const offered = new Set(suggestions.map((state) => state.toLowerCase()));
	const chosen = chosenStates
		.map((entry) => entry.state)
		.filter((state) => offered.has(state.toLowerCase()));

	const save = (states: string[]) => onChange({ states: toSettings(states) });

	// Only To Do and Doing states can mean refinement, so a chosen state leaves as soon as the admin stops
	// offering it, and the same save that changes the states carries the shorter list. Opening the form is
	// not an edit: a stored state that is not offered is only hidden, and the server drops it on any save.
	const offeredKey = [...offered].join("\n");
	const lastOfferedKey = useRef(offeredKey);
	const anyNoLongerOffered = chosen.length < chosenStates.length;
	useEffect(() => {
		if (lastOfferedKey.current === offeredKey) {
			return;
		}
		lastOfferedKey.current = offeredKey;
		if (anyNoLongerOffered) {
			onChange({ states: toSettings(chosen) });
		}
	}, [offeredKey, anyNoLongerOffered, chosen, onChange]);

	// The list accepts free text, but the server refuses anything that is not a To Do or Doing state,
	// so a typed value only counts when it names one of the suggestions.
	const add = (typed: string) => {
		const wanted = typed.trim().toLowerCase();
		const match = suggestions.find((state) => state.toLowerCase() === wanted);
		if (match && !chosen.includes(match)) {
			save([...chosen, match]);
		}
	};

	const remove = (state: string) =>
		save(chosen.filter((name) => name !== state));

	const refinementTerm = getTerm(TERMINOLOGY_KEYS.REFINEMENT);

	return (
		<InputGroup title={refinementTerm}>
			<Grid size={{ xs: 12 }}>
				<ItemListManager
					title={`${refinementTerm} State`}
					items={chosen}
					onAddItem={add}
					onRemoveItem={remove}
					suggestions={suggestions}
					isLoading={false}
				/>
			</Grid>
			<ReadinessSettings
				readiness={refinement?.readiness ?? DEFAULT_READINESS}
				onChange={(readiness) => onChange({ readiness })}
			/>
			<StageRulesSettings
				teamId={teamId}
				stageRules={refinement?.stageRules ?? NO_STAGE_RULES}
				onChange={(stageRules) => onChange({ stageRules })}
			/>
			<RefinementCadenceSettings
				cadence={refinement?.cadence ?? null}
				onChange={(cadence) => onChange({ cadence })}
			/>
			<RefinementBandSettings
				band={refinement?.band ?? DEFAULT_BAND}
				onChange={(band) => onChange({ band })}
			/>
		</InputGroup>
	);
};

export default RefinementSettingsSection;
