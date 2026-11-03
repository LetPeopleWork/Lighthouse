import AddIcon from "@mui/icons-material/Add";
import DeleteIcon from "@mui/icons-material/Delete";
import {
	Alert,
	Autocomplete,
	Button,
	Chip,
	IconButton,
	Stack,
	TextField,
	Typography,
} from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import { useRef, useState } from "react";
import type { IStateMapping } from "../../../models/Common/StateMapping";
import { foldKey } from "../../../utils/stateMappingView";
import InputGroup from "../InputGroup/InputGroup";
import ReloadDependentDataAction from "./ReloadDependentDataAction";

interface StateMappingsEditorProps {
	stateMappings: IStateMapping[];
	/** Current Doing states from which source states can be selected. */
	doingStates: string[];
	onChange: (mappings: IStateMapping[]) => void;
	validationErrors?: string[];
	refreshFailed?: boolean;
	onReloadDependentData?: () => void;
}

const StateMappingsEditor: React.FC<StateMappingsEditorProps> = ({
	stateMappings,
	doingStates,
	onChange,
	validationErrors = [],
	refreshFailed = false,
	onReloadDependentData,
}) => {
	const [newStateInputs, setNewStateInputs] = useState<{
		[index: number]: string;
	}>({});

	const mappingIds = useRef<string[]>([]);

	/** The Doing states a mapping can still add. A state belongs to one mapping only and a mapping cannot
	 *  hold another, so the states and names of the other mappings are left out, as are the mapping's own
	 *  states. A mapping's own name stays offered, so it can group the state it is named after.
	 */
	const getAvailableSourceOptions = (mappingIndex: number): string[] => {
		const taken = new Set(
			stateMappings.flatMap((mapping, i) =>
				i === mappingIndex
					? mapping.states.map(foldKey)
					: [mapping.name, ...mapping.states].map(foldKey),
			),
		);
		return doingStates.filter((state) => !taken.has(foldKey(state)));
	};

	const handleAddMapping = () => {
		mappingIds.current.push(crypto.randomUUID());
		onChange([...stateMappings, { name: "", states: [] }]);
	};

	const handleRemoveMapping = (index: number) => {
		mappingIds.current.splice(index, 1);

		const updated = stateMappings.filter((_, i) => i !== index);
		onChange(updated);
	};

	const handleNameChange = (index: number, name: string) => {
		const updated = stateMappings.map((m, i) =>
			i === index ? { ...m, name } : m,
		);
		onChange(updated);
	};

	const handleAddState = (index: number, state: string) => {
		if (!state.trim()) return;
		const updated = stateMappings.map((m, i) =>
			i === index ? { ...m, states: [...m.states, state.trim()] } : m,
		);
		onChange(updated);
		setNewStateInputs((prev) => ({ ...prev, [index]: "" }));
	};

	const handleRemoveState = (mappingIndex: number, state: string) => {
		const updated = stateMappings.map((m, i) =>
			i === mappingIndex
				? { ...m, states: m.states.filter((s) => s !== state) }
				: m,
		);
		onChange(updated);
	};

	const getId = (index: number) => {
		if (!mappingIds.current[index]) {
			mappingIds.current[index] = crypto.randomUUID();
		}
		return mappingIds.current[index];
	};

	return (
		<InputGroup title="State Mappings">
			<Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
				Combine several Doing states into one named group. The group shows as
				one chip in your Doing list and in the charts; its states are still read
				from your tracker. Delete the group to see its states separately again.
			</Typography>
			{validationErrors.length > 0 && (
				<Stack spacing={1} sx={{ mb: 2 }}>
					{validationErrors.map((error) => (
						<Alert key={error} severity="error" variant="outlined">
							{error}
						</Alert>
					))}
				</Stack>
			)}

			{stateMappings.map((mapping, index) => (
				<Grid
					container
					spacing={2}
					key={getId(index)}
					sx={{
						mb: 2,
						p: 2,
						border: 1,
						borderColor: "divider",
						borderRadius: 1,
					}}
				>
					<Grid
						size={{ xs: 11 }}
						sx={{ display: "flex", alignItems: "center", gap: 2 }}
					>
						<TextField
							label="Mapping Name"
							value={mapping.name}
							onChange={(e) => handleNameChange(index, e.target.value)}
							size="small"
							sx={{ minWidth: 200 }}
						/>
					</Grid>
					<Grid
						size={{ xs: 1 }}
						sx={{ display: "flex", justifyContent: "flex-end" }}
					>
						<IconButton
							aria-label="remove mapping"
							onClick={() => handleRemoveMapping(index)}
							color="error"
							size="small"
						>
							<DeleteIcon />
						</IconButton>
					</Grid>
					<Grid size={{ xs: 12 }}>
						<Stack
							direction="row"
							spacing={1}
							sx={{ flexWrap: "wrap", gap: 1, mb: 1 }}
						>
							{mapping.states.map((state) => (
								<Chip
									key={state}
									label={state}
									onDelete={() => handleRemoveState(index, state)}
									color="primary"
									variant="outlined"
									size="small"
								/>
							))}
						</Stack>
						<Autocomplete
							options={getAvailableSourceOptions(index)}
							value={null}
							inputValue={newStateInputs[index] ?? ""}
							onInputChange={(_, val) => {
								setNewStateInputs((prev) => ({ ...prev, [index]: val }));
							}}
							onChange={(_, selected) => {
								if (selected) handleAddState(index, selected);
							}}
							blurOnSelect
							disableCloseOnSelect={false}
							noOptionsText="No Doing states available"
							renderInput={(params) => (
								<TextField
									{...params}
									label="Add Doing State to This Group"
									size="small"
									fullWidth
									helperText="Select from your current Doing states"
								/>
							)}
						/>
					</Grid>
				</Grid>
			))}

			<Button
				startIcon={<AddIcon />}
				onClick={handleAddMapping}
				variant="outlined"
				size="small"
			>
				Add State Mapping
			</Button>

			<ReloadDependentDataAction
				visible={refreshFailed && onReloadDependentData !== undefined}
				label="Reload now"
				onReload={() => onReloadDependentData?.()}
			/>
		</InputGroup>
	);
};

export default StateMappingsEditor;
