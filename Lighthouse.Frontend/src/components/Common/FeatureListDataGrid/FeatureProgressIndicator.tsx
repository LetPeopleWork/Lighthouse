import { Box } from "@mui/material";
import type React from "react";
import type { IEntityReference } from "../../../models/EntityReference";
import type { IFeature } from "../../../models/Feature";
import ProgressIndicator from "../ProgressIndicator/ProgressIndicator";
import ProgressTitle from "../ProgressIndicator/ProgressTitle";
import StyledLink from "../StyledLink/StyledLink";

interface FeatureProgressIndicatorProps {
	feature: IFeature;
	teams: IEntityReference[];
	/** Label for the aggregate bar. Defaults to "Overall Progress". */
	overallTitle?: string | React.ReactNode;
	/** Called when the overall progress title is clicked. When provided a clickable ProgressTitle is rendered. */
	onShowDetails?: () => Promise<void>;
	/** Whether the feature is using the default feature size (shows warning icon). */
	isUsingDefaultFeatureSize?: boolean;
}

/**
 * Renders progress bars for a feature across one or more teams.
 *
 * When there is exactly one team and its work equals the feature total,
 * only the team bar is shown (avoiding duplicate information).
 * If onShowDetails is provided in this case, it is attached to the team bar title.
 * Otherwise an aggregate bar is shown followed by per-team bars.
 */
const FeatureProgressIndicator: React.FC<FeatureProgressIndicatorProps> = ({
	feature,
	teams,
	overallTitle = "Overall Progress",
	onShowDetails,
	isUsingDefaultFeatureSize = false,
}) => {
	const teamsWithWork = teams.filter(
		(team) => feature.getTotalWorkForTeam(team.id) > 0,
	);

	const totalWork = feature.getTotalWorkForFeature();
	const remainingWork = feature.getRemainingWorkForFeature();

	const isSingleTeamEquivalent =
		teamsWithWork.length === 1 &&
		feature.getTotalWorkForTeam(teamsWithWork[0].id) === totalWork;

	if (isSingleTeamEquivalent) {
		const team = teamsWithWork[0];
		const title = onShowDetails ? (
			<ProgressTitle
				title={team.name}
				isUsingDefaultFeatureSize={isUsingDefaultFeatureSize}
				onShowDetails={onShowDetails}
			/>
		) : (
			<StyledLink to={`/teams/${team.id}`}>{team.name}</StyledLink>
		);
		return (
			<Box sx={{ width: "100%" }}>
				<ProgressIndicator
					title={title}
					progressableItem={{
						remainingWork: feature.getRemainingWorkForTeam(team.id),
						totalWork: feature.getTotalWorkForTeam(team.id),
					}}
				/>
			</Box>
		);
	}

	return (
		<Box sx={{ width: "100%" }}>
			<ProgressIndicator
				title={
					onShowDetails ? (
						<ProgressTitle
							title={
								typeof overallTitle === "string"
									? overallTitle
									: "Overall Progress"
							}
							isUsingDefaultFeatureSize={isUsingDefaultFeatureSize}
							onShowDetails={onShowDetails}
						/>
					) : (
						overallTitle
					)
				}
				progressableItem={{ remainingWork, totalWork }}
			/>
			{teamsWithWork.map((team) => (
				<Box key={team.id}>
					<ProgressIndicator
						title={
							<StyledLink to={`/teams/${team.id}`}>{team.name}</StyledLink>
						}
						progressableItem={{
							remainingWork: feature.getRemainingWorkForTeam(team.id),
							totalWork: feature.getTotalWorkForTeam(team.id),
						}}
					/>
				</Box>
			))}
		</Box>
	);
};

export default FeatureProgressIndicator;
