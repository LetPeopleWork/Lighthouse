import { FormControl, MenuItem, Select } from "@mui/material";
import type React from "react";
import { useEffect } from "react";
import type { INamedCycleTimeDefinition } from "../../../models/Metrics/NamedCycleTime";

const DEFAULT_SELECTION = "default";

interface CumulativeStateTimeScopeControlProps {
	namedCycleTimeDefinitions: INamedCycleTimeDefinition[];
	scopeDefinitionId: number | null;
	onScopeChange: (definitionId: number | null) => void;
}

const CumulativeStateTimeScopeControl: React.FC<
	CumulativeStateTimeScopeControlProps
> = ({ namedCycleTimeDefinitions, scopeDefinitionId, onScopeChange }) => {
	useEffect(() => {
		if (scopeDefinitionId === null) {
			return;
		}
		const selected = namedCycleTimeDefinitions.find(
			(definition) => definition.id === scopeDefinitionId,
		);
		if (!selected || selected.isValid === false) {
			onScopeChange(null);
		}
	}, [scopeDefinitionId, namedCycleTimeDefinitions, onScopeChange]);

	if (namedCycleTimeDefinitions.length === 0) {
		return null;
	}

	return (
		<FormControl size="small" sx={{ minWidth: 200 }}>
			<Select
				value={
					scopeDefinitionId === null
						? DEFAULT_SELECTION
						: String(scopeDefinitionId)
				}
				onChange={(event) => {
					const value = event.target.value;
					onScopeChange(value === DEFAULT_SELECTION ? null : Number(value));
				}}
				SelectDisplayProps={{ "aria-label": "Cycle time scope" }}
			>
				<MenuItem value={DEFAULT_SELECTION}>Default</MenuItem>
				{namedCycleTimeDefinitions.map((definition) => (
					<MenuItem
						key={definition.id}
						value={String(definition.id)}
						disabled={definition.isValid === false}
					>
						{definition.isValid === false
							? `${definition.name} (invalid — fix its states)`
							: definition.name}
					</MenuItem>
				))}
			</Select>
		</FormControl>
	);
};

export default CumulativeStateTimeScopeControl;
