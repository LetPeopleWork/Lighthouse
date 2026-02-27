import Grid from "@mui/material/Grid";
import type React from "react";
import type { IPortfolioSettings } from "../../../../models/Portfolio/PortfolioSettings";
import type { ITeam } from "../../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import type { IAdditionalFieldDefinition } from "../../../../models/WorkTracking/AdditionalFieldDefinition";
import { useTerminology } from "../../../../services/TerminologyContext";
import FormSelectField from "../../FormSelectField/FormSelectField";
import InputGroup from "../../InputGroup/InputGroup";

interface OwnershipComponentProps {
	projectSettings: IPortfolioSettings | null;
	onProjectSettingsChange: (
		key: keyof IPortfolioSettings,
		value: string | ITeam | number | null,
	) => void;
	availableTeams: ITeam[];
	additionalFieldDefinitions?: IAdditionalFieldDefinition[];
}

const OwnershipComponent: React.FC<OwnershipComponentProps> = ({
	projectSettings,
	onProjectSettingsChange,
	availableTeams,
	additionalFieldDefinitions = [],
}) => {
	const { getTerm } = useTerminology();
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const teamTerm = getTerm(TERMINOLOGY_KEYS.TEAM);

	return (
		<InputGroup title="Ownership Settings" initiallyExpanded={false}>
			<Grid size={{ xs: 12 }}>
				<FormSelectField
					label={`Owning ${teamTerm}`}
					value={projectSettings?.owningTeam?.id ?? ""}
					onChange={(teamId) => {
						const team = availableTeams.find((t) => t.id === teamId);
						onProjectSettingsChange("owningTeam", team || null);
					}}
					options={availableTeams.map((team) => ({
						id: team.id,
						label: team.name,
					}))}
				/>
			</Grid>
			<Grid size={{ xs: 12 }}>
				<FormSelectField
					label={`${featureTerm} Owner Field`}
					value={projectSettings?.featureOwnerAdditionalFieldDefinitionId ?? ""}
					onChange={(value) => {
						onProjectSettingsChange(
							"featureOwnerAdditionalFieldDefinitionId",
							value,
						);
					}}
					options={additionalFieldDefinitions.map((field) => ({
						id: field.id,
						label: field.displayName,
					}))}
				/>
			</Grid>
		</InputGroup>
	);
};

export default OwnershipComponent;
