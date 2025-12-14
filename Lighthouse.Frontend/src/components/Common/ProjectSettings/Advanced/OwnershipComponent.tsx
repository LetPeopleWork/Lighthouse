import {
	FormControl,
	InputLabel,
	MenuItem,
	Select,
	TextField,
} from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import type { IPortfolioSettings } from "../../../../models/Project/PortfolioSettings";
import type { ITeam } from "../../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import { useTerminology } from "../../../../services/TerminologyContext";
import InputGroup from "../../InputGroup/InputGroup";

interface OwnershipComponentProps {
	projectSettings: IPortfolioSettings | null;
	onProjectSettingsChange: (
		key: keyof IPortfolioSettings,
		value: string | ITeam | null,
	) => void;
	currentInvolvedTeams: ITeam[];
}

const OwnershipComponent: React.FC<OwnershipComponentProps> = ({
	projectSettings,
	onProjectSettingsChange,
	currentInvolvedTeams,
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
				<TextField
					label={`${featureTerm} Owner Field`}
					fullWidth
					margin="normal"
					value={projectSettings?.featureOwnerField ?? ""}
					onChange={(e) =>
						onProjectSettingsChange("featureOwnerField", e.target.value)
					}
				/>
			</Grid>
		</InputGroup>
	);
};

export default OwnershipComponent;
