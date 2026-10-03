import CheckBoxIcon from "@mui/icons-material/CheckBox";
import CheckBoxOutlineBlankIcon from "@mui/icons-material/CheckBoxOutlineBlank";
import {
	List,
	ListItemButton,
	ListItemIcon,
	ListItemText,
} from "@mui/material";
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

	// Only the state names go back: whether a state is still mapped is the server's verdict to give.
	const toggle = (state: string) => {
		const next = chosen.includes(state)
			? chosen.filter((name) => name !== state)
			: [...chosen, state];
		onChange(next.map((name) => ({ state: name })));
	};

	const doingNote = `already counts in ${getTerm(TERMINOLOGY_KEYS.WIP)} and ${getTerm(TERMINOLOGY_KEYS.CYCLE_TIME)}`;

	return (
		<InputGroup title={getTerm(TERMINOLOGY_KEYS.REFINEMENT)}>
			<Grid size={{ xs: 12 }}>
				<List dense disablePadding>
					{candidates.map(({ state, category }) => {
						const isChosen = chosen.includes(state);
						return (
							<ListItemButton
								key={`${category}-${state}`}
								role="checkbox"
								aria-checked={isChosen}
								aria-label={`${state} (${category})`}
								onClick={() => toggle(state)}
							>
								<ListItemIcon>
									{isChosen ? (
										<CheckBoxIcon color="primary" />
									) : (
										<CheckBoxOutlineBlankIcon />
									)}
								</ListItemIcon>
								<ListItemText
									primary={`${state} (${category})`}
									secondary={category === "Doing" ? doingNote : undefined}
								/>
							</ListItemButton>
						);
					})}
				</List>
			</Grid>
		</InputGroup>
	);
};

export default RefinementSettingsSection;
