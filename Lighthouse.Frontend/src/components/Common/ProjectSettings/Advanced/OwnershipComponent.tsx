import { FormControl, InputLabel, MenuItem, Select } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import type { IPortfolioSettings } from "../../../../models/Portfolio/PortfolioSettings";
import type { ITeam } from "../../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import type { IAdditionalFieldDefinition } from "../../../../models/WorkTracking/AdditionalFieldDefinition";
import { useTerminology } from "../../../../services/TerminologyContext";
import InputGroup from "../../InputGroup/InputGroup";

interface OwnershipComponentProps {
	projectSettings: IPortfolioSettings | null;
	onProjectSettingsChange: (
		key: keyof IPortfolioSettings,
		value: string | ITeam | number | null,
	) => void;
	currentInvolvedTeams: ITeam[];
	additionalFieldDefinitions?: IAdditionalFieldDefinition[];
}

const OwnershipComponent: React.FC<OwnershipComponentProps> = ({
	projectSettings,
	onProjectSettingsChange,
	currentInvolvedTeams,
	additionalFieldDefinitions = [],
}) => {
	const { getTerm } = useTerminology();
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const teamTerm = getTerm(TERMINOLOGY_KEYS.TEAM);

	return (
		<InputGroup title={"Ownership Settings"} initiallyExpanded={false}>
			<Grid size={{ xs: 12 }}>
				<FormControl fullWidth margin="normal">
					<InputLabel>Owning Team</InputLabel>
					<Select
						value={projectSettings?.owningTeam?.id ?? ""}
						label={`Owning ${teamTerm}`}
						onChange={(e) => {
							const teamId = e.target.value;
							const team = currentInvolvedTeams.find((t) => t.id === teamId);
							onProjectSettingsChange("owningTeam", team || null);
						}}
					>
						<MenuItem value="">
							<em>None</em>
						</MenuItem>
						{currentInvolvedTeams.map((team) => (
							<MenuItem key={team.id} value={team.id}>
								{team.name}
							</MenuItem>
						))}
					</Select>
				</FormControl>
			</Grid>
			<Grid size={{ xs: 12 }}>
				<FormControl fullWidth margin="normal">
					<InputLabel>{`${featureTerm} Owner Field`}</InputLabel>
					<Select<number | "">
						value={
							projectSettings?.featureOwnerAdditionalFieldDefinitionId ?? ""
						}
						label={`${featureTerm} Owner Field`}
						onChange={(e) => {
							const value = e.target.value;
							onProjectSettingsChange(
								"featureOwnerAdditionalFieldDefinitionId",
								value === "" ? null : value,
							);
						}}
					>
						<MenuItem value="">
							<em>None</em>
						</MenuItem>
						{additionalFieldDefinitions.map((field) => (
							<MenuItem key={field.id} value={field.id}>
								{field.displayName}
							</MenuItem>
						))}
					</Select>
				</FormControl>
			</Grid>
		</InputGroup>
	);
};

export default OwnershipComponent;
