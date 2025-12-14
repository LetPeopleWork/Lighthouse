import { TextField } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import StyledLink from "../../../components/Common/StyledLink/StyledLink";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";

interface InvolvedTeamsListProps {
	teams: ITeamSettings[];
	initiallyExpanded?: boolean;
	onTeamUpdated: (updatedTeam: ITeamSettings) => Promise<void>;
}

const InvolvedTeamsList: React.FC<InvolvedTeamsListProps> = ({
	teams,
	onTeamUpdated,
	initiallyExpanded = false,
}) => {
	const { getTerm } = useTerminology();
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const teamTerm = getTerm(TERMINOLOGY_KEYS.TEAM);
	const wipTerm = getTerm(TERMINOLOGY_KEYS.WIP);

	if (teams.length === 0) {
		return null;
	}

	const onTeamFeatureWIPUpdated = async (
		teamSetting: ITeamSettings,
		newFeatureWip: number,
	) => {
		teamSetting.featureWIP = newFeatureWip;
		await onTeamUpdated(teamSetting);
	};

	return (
		<InputGroup
			title={`Involved ${teamTerm}s (${featureTerm} ${wipTerm})`}
			initiallyExpanded={initiallyExpanded}
		>
			<Grid container spacing={2}>
				{teams.map((team) => (
					<Grid size={{ xs: 3 }} key={team.id}>
						<TextField
							variant="outlined"
							margin="normal"
							label={
								<StyledLink to={`/teams/${team.id}`}>{team.name}</StyledLink>
							}
							type="number"
							onChange={(e) =>
								onTeamFeatureWIPUpdated(
									team,
									Number.parseInt(e.target.value, 10),
								)
							}
							defaultValue={team.featureWIP}
							slotProps={{
								htmlInput: {
									min: 1,
								},
							}}
						/>
					</Grid>
				))}
			</Grid>
		</InputGroup>
	);
};

export default InvolvedTeamsList;
