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
import { useId, useRef } from "react";
import { useLicenseRestrictions } from "../../../hooks/useLicenseRestrictions";
import type { IStateMapping } from "../../../models/Common/StateMapping";
import type { ICycleTimeDefinition } from "../../../models/Metrics/NamedCycleTime";
import {
	cycleTimeBoundaryIndex,
	isCycleTimeDefinitionValid,
	resolveWorkflowStates,
} from "../../../utils/isCycleTimeDefinitionValid";
import InputGroup from "../InputGroup/InputGroup";

interface CycleTimesEditorProps {
	cycleTimeDefinitions: ICycleTimeDefinition[];
	toDoStates: string[];
	doingStates: string[];
	doneStates: string[];
	stateMappings: IStateMapping[];
	onChange: (definitions: ICycleTimeDefinition[]) => void;
	validationErrors?: string[];
	cycleTimeTerm?: string;
}

const CycleTimesEditor: React.FC<CycleTimesEditorProps> = ({
	cycleTimeDefinitions,
	toDoStates,
	doingStates,
	doneStates,
	stateMappings,
	onChange,
	validationErrors = [],
	cycleTimeTerm = "Cycle Time",
}) => {
	const { licenseStatus } = useLicenseRestrictions();
	const labelId = useId();
	const nextTempId = useRef(-1);

	const mappingNames = stateMappings
		.filter((mapping) => mapping.name.trim() !== "")
		.map((mapping) => mapping.name);

	const workflowStates = resolveWorkflowStates(
		toDoStates,
		doingStates,
		doneStates,
		stateMappings,
	);

	const suggestions = [...workflowStates, ...mappingNames].filter(
		(state, index, all) => all.indexOf(state) === index,
	);

	if (!licenseStatus?.canUsePremiumFeatures) {
		return null;
	}

	const updateDefinition = (
		index: number,
		change: Partial<ICycleTimeDefinition>,
	) => {
		onChange(
			cycleTimeDefinitions.map((definition, current) =>
				current === index ? { ...definition, ...change } : definition,
			),
		);
	};

	const addDefinition = () => {
		onChange([
			...cycleTimeDefinitions,
			{ id: nextTempId.current--, name: "", startState: "", endState: "" },
		]);
	};

	const removeDefinition = (index: number) => {
		onChange(cycleTimeDefinitions.filter((_, current) => current !== index));
	};

	const duplicateNames = new Set(
		cycleTimeDefinitions
			.map((definition) => definition.name.trim().toLowerCase())
			.filter((name, index, all) => name !== "" && all.indexOf(name) !== index),
	);

	const rowError = (definition: ICycleTimeDefinition): string | null => {
		if (definition.name.trim() === "") {
			return "A cycle time name is required.";
		}
		if (duplicateNames.has(definition.name.trim().toLowerCase())) {
			return `Duplicate cycle time name '${definition.name.trim()}'.`;
		}
		if (
			definition.startState.trim() === "" ||
			definition.endState.trim() === ""
		) {
			return null;
		}
		if (
			!isCycleTimeDefinitionValid(definition, workflowStates, stateMappings)
		) {
			return "Both boundary states must exist in the current workflow.";
		}
		const startIndex = cycleTimeBoundaryIndex(
			definition.startState,
			workflowStates,
			stateMappings,
		);
		const endIndex = cycleTimeBoundaryIndex(
			definition.endState,
			workflowStates,
			stateMappings,
		);
		if (endIndex <= startIndex) {
			return "End state must come after the start state in the workflow";
		}
		return null;
	};

	return (
		<InputGroup title="Cycle Times">
			<Grid size={{ xs: 12 }}>
				<Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
					The default {cycleTimeTerm} measures the full started-to-closed
					duration. Add named cycle times to measure a sub-window of the
					workflow — pick a start and an end state (raw states or State-Mapping
					names, in workflow order). The end state must come after the start
					state.
				</Typography>
				<Chip
					label={`Default ${cycleTimeTerm}`}
					variant="outlined"
					sx={{ mb: 1 }}
				/>
			</Grid>

			{validationErrors.length > 0 && (
				<Grid size={{ xs: 12 }}>
					<Alert severity="error" data-testid="cycle-times-validation-errors">
						{validationErrors.map((error) => (
							<div key={error}>{error}</div>
						))}
					</Alert>
				</Grid>
			)}

			{cycleTimeDefinitions.map((definition, index) => {
				const hasBoundaries =
					definition.startState.trim() !== "" &&
					definition.endState.trim() !== "";
				const error = rowError(definition);

				return (
					<Grid size={{ xs: 12 }} key={definition.id}>
						<Stack
							direction="row"
							spacing={1}
							sx={{ alignItems: "flex-start" }}
						>
							<TextField
								label="Name"
								size="small"
								value={definition.name}
								onChange={(event) =>
									updateDefinition(index, { name: event.target.value })
								}
								sx={{ minWidth: 180 }}
							/>
							<Autocomplete
								freeSolo
								size="small"
								options={suggestions}
								value={definition.startState}
								onChange={(_, value) =>
									updateDefinition(index, { startState: value ?? "" })
								}
								onInputChange={(_, value) =>
									updateDefinition(index, { startState: value })
								}
								sx={{ minWidth: 200 }}
								renderInput={(params) => (
									<TextField
										{...params}
										label="Start state"
										id={`${labelId}-start-${index}`}
									/>
								)}
							/>
							<Autocomplete
								freeSolo
								size="small"
								options={suggestions}
								value={definition.endState}
								onChange={(_, value) =>
									updateDefinition(index, { endState: value ?? "" })
								}
								onInputChange={(_, value) =>
									updateDefinition(index, { endState: value })
								}
								sx={{ minWidth: 200 }}
								renderInput={(params) => (
									<TextField
										{...params}
										label="End state"
										id={`${labelId}-end-${index}`}
									/>
								)}
							/>
							<IconButton
								aria-label={`Delete cycle time ${definition.name || index + 1}`}
								onClick={() => removeDefinition(index)}
							>
								<DeleteIcon />
							</IconButton>
						</Stack>
						{hasBoundaries && (
							<Typography variant="caption" color="text.secondary">
								{definition.startState} → {definition.endState}
							</Typography>
						)}
						{error && (
							<Typography
								variant="caption"
								color="error"
								sx={{ display: "block" }}
							>
								{error}
							</Typography>
						)}
					</Grid>
				);
			})}

			<Grid size={{ xs: 12 }}>
				<Button variant="outlined" size="small" onClick={addDefinition}>
					Add Cycle Time
				</Button>
			</Grid>
		</InputGroup>
	);
};

export default CycleTimesEditor;
