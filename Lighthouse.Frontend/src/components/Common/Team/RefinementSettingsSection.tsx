import {
	Checkbox,
	FormControlLabel,
	TextField,
	Typography,
} from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import { useEffect, useRef } from "react";
import type {
	IReadinessSetting,
	IRefinementStateSetting,
} from "../../../models/Refinement/Refinement";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";
import InputGroup from "../InputGroup/InputGroup";
import ItemListManager from "../ItemListManager/ItemListManager";

interface RefinementSettingsSectionProps {
	toDoStates: string[];
	doingStates: string[];
	chosenStates: IRefinementStateSetting[];
	onChange: (states: IRefinementStateSetting[]) => void;
	readiness: IReadinessSetting;
	onReadinessChange: (readiness: IReadinessSetting) => void;
}

export const DEFAULT_READINESS: IReadinessSetting = {
	minYes: 3,
	minVoters: 3,
	veto: null,
};

export const MIN_YES_ERROR = "At least one Yes vote is needed";
export const MIN_VOTERS_ERROR =
	"Voters needed cannot be fewer than Yes votes needed";

// The same rule the server enforces, so a save it would refuse is never sent.
export const readinessErrors = (
	readiness: IReadinessSetting,
): { minYes: string | null; minVoters: string | null } => {
	const enoughYes = Number.isInteger(readiness.minYes) && readiness.minYes >= 1;
	const enoughVoters =
		Number.isInteger(readiness.minVoters) &&
		readiness.minVoters >= readiness.minYes;
	return {
		minYes: enoughYes ? null : MIN_YES_ERROR,
		minVoters: enoughYes && !enoughVoters ? MIN_VOTERS_ERROR : null,
	};
};

const toSettings = (states: string[]): IRefinementStateSetting[] =>
	states.map((state) => ({ state }));

const RefinementSettingsSection: React.FC<RefinementSettingsSectionProps> = ({
	toDoStates,
	doingStates,
	chosenStates,
	onChange,
	readiness,
	onReadinessChange,
}) => {
	const { getTerm } = useTerminology();

	const suggestions = [...toDoStates, ...doingStates];
	const offered = new Set(suggestions.map((state) => state.toLowerCase()));
	const chosen = chosenStates
		.map((entry) => entry.state)
		.filter((state) => offered.has(state.toLowerCase()));

	const save = (states: string[]) => onChange(toSettings(states));

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
			onChange(toSettings(chosen));
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
	const errors = readinessErrors(readiness);

	const changeNumber =
		(key: "minYes" | "minVoters") =>
		(event: React.ChangeEvent<HTMLInputElement>) =>
			onReadinessChange({
				...readiness,
				[key]: Number.parseInt(event.target.value, 10),
			});

	const shown = (value: number) => (Number.isNaN(value) ? "" : value);

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
			<Grid size={{ xs: 12 }}>
				<Typography variant="subtitle1">Readiness by votes</Typography>
			</Grid>
			<Grid size={{ xs: 12, sm: 6 }}>
				<TextField
					label="Yes votes needed"
					type="number"
					fullWidth
					value={shown(readiness.minYes)}
					onChange={changeNumber("minYes")}
					error={errors.minYes !== null}
					helperText={errors.minYes}
					slotProps={{ htmlInput: { min: 1, step: 1 } }}
				/>
			</Grid>
			<Grid size={{ xs: 12, sm: 6 }}>
				<TextField
					label="Voters needed"
					type="number"
					fullWidth
					value={shown(readiness.minVoters)}
					onChange={changeNumber("minVoters")}
					error={errors.minVoters !== null}
					helperText={errors.minVoters}
					slotProps={{ htmlInput: { min: 1, step: 1 } }}
				/>
			</Grid>
			<Grid size={{ xs: 12 }}>
				<FormControlLabel
					control={<Checkbox checked={readiness.veto !== null} disabled />}
					label="Send to discussion"
				/>
			</Grid>
		</InputGroup>
	);
};

export default RefinementSettingsSection;
