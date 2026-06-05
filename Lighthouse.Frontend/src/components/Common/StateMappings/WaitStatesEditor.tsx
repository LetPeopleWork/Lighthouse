import { Checkbox, FormControlLabel, Typography } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import { useState } from "react";
import type { IStateMapping } from "../../../models/Common/StateMapping";
import InputGroup from "../InputGroup/InputGroup";
import ItemListManager from "../ItemListManager/ItemListManager";

interface WaitStatesEditorProps {
	waitStates: string[];
	doingStates: string[];
	stateMappings: IStateMapping[];
	onChange: (waitStates: string[]) => void;
}

const WaitStatesEditor: React.FC<WaitStatesEditorProps> = ({
	waitStates,
	doingStates,
	stateMappings,
	onChange,
}) => {
	const [isConfigured, setIsConfigured] = useState(waitStates.length > 0);

	const mappingNames = stateMappings
		.filter((mapping) => mapping.name.trim() !== "")
		.map((mapping) => mapping.name);

	const suggestions = [...doingStates, ...mappingNames];

	const handleToggle = (checked: boolean) => {
		setIsConfigured(checked);
		if (!checked) {
			onChange([]);
		}
	};

	const handleAddWaitState = (state: string) => {
		if (state.trim()) {
			onChange([...waitStates, state.trim()]);
		}
	};

	const handleRemoveWaitState = (state: string) => {
		onChange(waitStates.filter((item) => item !== state));
	};

	return (
		<InputGroup title="Wait States">
			<Grid size={{ xs: 12 }}>
				<FormControlLabel
					control={
						<Checkbox
							checked={isConfigured}
							onChange={(e) => handleToggle(e.target.checked)}
						/>
					}
					label="Configure Wait States"
				/>
			</Grid>
			{isConfigured && (
				<Grid size={{ xs: 12 }}>
					<Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
						Wait states capture time where work is idle, not active — queued
						behind a person or a hand-off. Flow efficiency is the share of time
						spent actively working versus waiting, so marking these states
						drives the efficiency calculation.
					</Typography>
					<ItemListManager
						title="Wait State"
						items={waitStates}
						onAddItem={handleAddWaitState}
						onRemoveItem={handleRemoveWaitState}
						suggestions={suggestions}
						isLoading={false}
					/>
				</Grid>
			)}
		</InputGroup>
	);
};

export default WaitStatesEditor;
