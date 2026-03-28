import AddIcon from "@mui/icons-material/Add";
import DeleteIcon from "@mui/icons-material/Delete";
import {
	Alert,
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
import InputGroup from "../InputGroup/InputGroup";

interface StateMappingsEditorProps {
	stateMappings: IStateMapping[];
	onChange: (mappings: IStateMapping[]) => void;
	validationErrors?: string[];
}

const StateMappingsEditor: React.FC<StateMappingsEditorProps> = ({
	stateMappings,
	onChange,
	validationErrors = [],
}) => {
	const [newStateInputs, setNewStateInputs] = useState<{
		[index: number]: string;
	}>({});

	const mappingIds = useRef<string[]>([]);

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
			<Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
				Group one or more provider states under a single name. Use mapped names
				in To Do, Doing, or Done configuration.
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
						<TextField
							label="New Source State"
							value={newStateInputs[index] ?? ""}
							onChange={(e) =>
								setNewStateInputs((prev) => ({
									...prev,
									[index]: e.target.value,
								}))
							}
							onKeyDown={(e) => {
								if (e.key === "Enter") {
									e.preventDefault();
									handleAddState(index, newStateInputs[index] ?? "");
								}
							}}
							size="small"
							fullWidth
							helperText="Press Enter to add a source state"
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
		</InputGroup>
	);
};

export default StateMappingsEditor;
