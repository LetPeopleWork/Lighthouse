import { Checkbox, FormControlLabel, Stack, Typography } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import type { IRefinementStateSetting } from "../../../models/Refinement/Refinement";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";
import InputGroup from "../InputGroup/InputGroup";

interface RefinementSettingsSectionProps {
	toDoStates: string[];
	doingStates: string[];
	chosenStates: IRefinementStateSetting[];
	onChange: (states: IRefinementStateSetting[]) => void;
}

interface RefinementCandidate {
	state: string;
	category: "To Do" | "Doing";
}

interface RefinementOptionProps {
	label: string;
	checked: boolean;
	note?: string;
	noteColor?: string;
	onToggle: () => void;
}

// The note sits beside the label rather than inside it, so the option is named by its state alone.
const RefinementOption: React.FC<RefinementOptionProps> = ({
	label,
	checked,
	note,
	noteColor = "text.secondary",
	onToggle,
}) => (
	<Stack>
		<FormControlLabel
			control={<Checkbox checked={checked} onChange={onToggle} />}
			label={label}
		/>
		{note && (
			<Typography variant="caption" color={noteColor} sx={{ ml: 4 }}>
				{note}
			</Typography>
		)}
	</Stack>
);

const RefinementSettingsSection: React.FC<RefinementSettingsSectionProps> = ({
	toDoStates,
	doingStates,
	chosenStates,
	onChange,
}) => {
	const { getTerm } = useTerminology();

	const candidates: RefinementCandidate[] = [
		...toDoStates.map((state) => ({ state, category: "To Do" as const })),
		...doingStates.map((state) => ({ state, category: "Doing" as const })),
	];

	const chosen = chosenStates.map((entry) => entry.state);

	// A state the Team stopped mapping is no longer offered, so it is shown from what was stored;
	// hiding it would silently throw away the admin's choice.
	const offered = new Set(candidates.map((candidate) => candidate.state));
	const flagged = chosenStates.filter(
		(entry) => entry.isMapped === false && !offered.has(entry.state),
	);

	// Only the state names go back: whether a state is still mapped is the server's verdict to give.
	const toggle = (state: string) => {
		const next = chosen.includes(state)
			? chosen.filter((name) => name !== state)
			: [...chosen, state];
		onChange(next.map((name) => ({ state: name })));
	};

	const doingNote = `already counts in ${getTerm(TERMINOLOGY_KEYS.WIP)} and ${getTerm(TERMINOLOGY_KEYS.CYCLE_TIME)}`;
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);

	return (
		<InputGroup title={getTerm(TERMINOLOGY_KEYS.REFINEMENT)}>
			<Grid size={{ xs: 12 }}>
				{candidates.map(({ state, category }) => (
					<RefinementOption
						key={`${category}-${state}`}
						label={`${state} (${category})`}
						checked={chosen.includes(state)}
						note={category === "Doing" ? doingNote : undefined}
						onToggle={() => toggle(state)}
					/>
				))}
				{flagged.map(({ state }) => (
					<RefinementOption
						key={`flagged-${state}`}
						label={state}
						checked
						note={`${state} is no longer mapped; its ${workItemsTerm} cannot appear`}
						noteColor="warning.main"
						onToggle={() => toggle(state)}
					/>
				))}
			</Grid>
		</InputGroup>
	);
};

export default RefinementSettingsSection;
