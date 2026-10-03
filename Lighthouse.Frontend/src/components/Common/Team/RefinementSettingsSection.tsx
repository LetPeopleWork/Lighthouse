import { Typography } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import type { IRefinementStateSetting } from "../../../models/Refinement/Refinement";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";
import InputGroup from "../InputGroup/InputGroup";
import ItemListManager from "../ItemListManager/ItemListManager";

interface RefinementSettingsSectionProps {
	toDoStates: string[];
	doingStates: string[];
	chosenStates: IRefinementStateSetting[];
	onChange: (states: IRefinementStateSetting[]) => void;
}

const RefinementSettingsSection: React.FC<RefinementSettingsSectionProps> = ({
	toDoStates,
	doingStates,
	chosenStates,
	onChange,
}) => {
	const { getTerm } = useTerminology();

	const suggestions = [...toDoStates, ...doingStates];
	const chosen = chosenStates.map((entry) => entry.state);

	// Only the state names go back: whether a state is still mapped is the server's verdict to give.
	const save = (states: string[]) =>
		onChange(states.map((state) => ({ state })));

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

	// A state the Team stopped mapping stays chosen until the admin removes it; dropping it quietly
	// would throw away their choice.
	const offered = new Set(suggestions);
	const noLongerMapped = chosen.filter((state) => !offered.has(state));

	const refinementTerm = getTerm(TERMINOLOGY_KEYS.REFINEMENT);
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const doingNote = `A Doing state already counts in ${getTerm(TERMINOLOGY_KEYS.WIP)} and ${getTerm(TERMINOLOGY_KEYS.CYCLE_TIME)}`;

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
				{doingStates.length > 0 && (
					<Typography variant="body2" color="text.secondary">
						{doingNote}
					</Typography>
				)}
				{noLongerMapped.map((state) => (
					<Typography key={state} variant="body2" color="warning.main">
						{`${state} is no longer mapped; its ${workItemsTerm} cannot appear`}
					</Typography>
				))}
			</Grid>
		</InputGroup>
	);
};

export default RefinementSettingsSection;
