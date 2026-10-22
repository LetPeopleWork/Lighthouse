import { FormControlLabel, Switch, Typography } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import type { SettingSource } from "../../../../models/Common/BaseSettings";
import type { IPortfolioSettings } from "../../../../models/Portfolio/PortfolioSettings";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import type { IAdditionalFieldDefinition } from "../../../../models/WorkTracking/AdditionalFieldDefinition";
import type { IWorkTrackingSystemConnection } from "../../../../models/WorkTracking/WorkTrackingSystemConnection";
import { useTerminology } from "../../../../services/TerminologyContext";
import ConnectionDefaultFieldSelect from "../../ConnectionDefault/ConnectionDefaultFieldSelect";
import { followOrOwn } from "../../ConnectionDefault/connectionDefaultEntry";
import { directionTip } from "../../ConnectionDefault/fieldDirectionTips";
import InputGroup from "../../InputGroup/InputGroup";

interface DependenciesComponentProps {
	projectSettings: IPortfolioSettings | null;
	onProjectSettingsChange: (
		key: keyof IPortfolioSettings,
		value: number | boolean | SettingSource | null,
	) => void;
	additionalFieldDefinitions?: IAdditionalFieldDefinition[];
	connection?: IWorkTrackingSystemConnection | null;
}

// Both settings live here rather than on a team's form, because a dependency runs between two Features
// and Features are fetched per Portfolio - a team-level copy of either would have nothing to act on.
const DependenciesComponent: React.FC<DependenciesComponentProps> = ({
	projectSettings,
	onProjectSettingsChange,
	additionalFieldDefinitions = [],
	connection,
}) => {
	const { getTerm } = useTerminology();
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const dependencyTip = directionTip(
		"dependencyFieldForPortfolios",
		connection?.workTrackingSystem ?? projectSettings?.workTrackingSystem,
		{ feature: featureTerm, workItem: getTerm(TERMINOLOGY_KEYS.WORK_ITEM) },
	);
	return (
		<InputGroup title="Dependency Settings" initiallyExpanded={false}>
			<Grid size={{ xs: 12 }}>
				<ConnectionDefaultFieldSelect
					label="Dependency Field"
					settings={projectSettings}
					connection={connection}
					theDefault="dependencyFieldForPortfolios"
					defaultNameTheSettingsCarry={
						projectSettings?.dependencyFieldDefaultName
					}
					source={projectSettings?.dependencyFieldSource}
					ownField={
						projectSettings?.dependencyOverrideAdditionalFieldDefinitionId
					}
					additionalFieldDefinitions={additionalFieldDefinitions}
					{...followOrOwn<IPortfolioSettings>(
						onProjectSettingsChange,
						"dependencyFieldSource",
						"dependencyOverrideAdditionalFieldDefinitionId",
					)}
					info={dependencyTip}
				/>
				<Typography variant="caption" color="text.secondary">
					{`Read what each ${featureTerm} waits on from this field, separated by commas or semicolons, instead of from the links in your work tracking system.`}
				</Typography>
			</Grid>
			<Grid size={{ xs: 12 }}>
				<FormControlLabel
					control={
						<Switch
							checked={projectSettings?.ignoreDependencies ?? false}
							onChange={(event) =>
								onProjectSettingsChange(
									"ignoreDependencies",
									event.target.checked,
								)
							}
							slotProps={{ input: { "aria-label": "Ignore Dependencies" } }}
						/>
					}
					label="Ignore Dependencies"
				/>
			</Grid>
		</InputGroup>
	);
};

export default DependenciesComponent;
