import Grid from "@mui/material/Grid";
import type React from "react";
import type { IPortfolioSettings } from "../../../../models/Portfolio/PortfolioSettings";
import type { ITeam } from "../../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import type { IAdditionalFieldDefinition } from "../../../../models/WorkTracking/AdditionalFieldDefinition";
import type { IWorkTrackingSystemConnection } from "../../../../models/WorkTracking/WorkTrackingSystemConnection";
import { useTerminology } from "../../../../services/TerminologyContext";
import ConnectionDefaultFieldSelect from "../../ConnectionDefault/ConnectionDefaultFieldSelect";
import { followOrOwn } from "../../ConnectionDefault/connectionDefaultEntry";
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
	connection?: IWorkTrackingSystemConnection | null;
}

const OwnershipComponent: React.FC<OwnershipComponentProps> = ({
	projectSettings,
	onProjectSettingsChange,
	availableTeams,
	additionalFieldDefinitions = [],
	connection,
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
				<ConnectionDefaultFieldSelect
					label={`${featureTerm} Owner Field`}
					settings={projectSettings}
					connection={connection}
					theDefault="featureOwnerFieldForPortfolios"
					defaultNameTheSettingsCarry={projectSettings?.featureOwnerDefaultName}
					source={projectSettings?.featureOwnerSource}
					ownField={projectSettings?.featureOwnerAdditionalFieldDefinitionId}
					additionalFieldDefinitions={additionalFieldDefinitions}
					{...followOrOwn<IPortfolioSettings>(
						onProjectSettingsChange,
						"featureOwnerSource",
						"featureOwnerAdditionalFieldDefinitionId",
					)}
				/>
			</Grid>
		</InputGroup>
	);
};

export default OwnershipComponent;
