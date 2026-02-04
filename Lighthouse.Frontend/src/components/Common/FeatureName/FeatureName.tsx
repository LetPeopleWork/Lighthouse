import EngineeringIcon from "@mui/icons-material/Engineering";
import GppMaybeOutlinedIcon from "@mui/icons-material/GppMaybeOutlined";
import { IconButton, Link, Tooltip } from "@mui/material";
import React from "react";
import type { IEntityReference } from "../../../models/EntityReference";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";
import StyledLink from "../StyledLink/StyledLink";

interface FeatureNameProps {
	name: string;
	url: string;
	isUsingDefaultFeatureSize: boolean;
	teamsWorkIngOnFeature: IEntityReference[];
}

interface TeamLinksListProps {
	teams: IEntityReference[];
}

const TeamLinksList: React.FC<TeamLinksListProps> = ({ teams }) => {
	return (
		<>
			{teams.map((team, index) => (
				<React.Fragment key={team.id}>
					{index > 0 && ", "}
					<StyledLink to={`/teams/${team.id}`} variant="body2">
						{team.name}
					</StyledLink>
				</React.Fragment>
			))}
		</>
	);
};

const FeatureName: React.FC<FeatureNameProps> = ({
	name,
	url,
	isUsingDefaultFeatureSize,
	teamsWorkIngOnFeature,
}) => {
	const { getTerm } = useTerminology();
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);

	return (
		<span>
			{url ? (
				<Link
					href={url}
					target="_blank"
					rel="noopener noreferrer"
					sx={{
						textDecoration: "none",
						color: (theme) => theme.palette.primary.main,
						fontWeight: 500,
						"&:hover": {
							textDecoration: "underline",
							opacity: 0.9,
						},
					}}
				>
					{name}
				</Link>
			) : (
				name
			)}
			{isUsingDefaultFeatureSize && (
				<Tooltip
					title={`No child ${workItemsTerm} were found for this ${featureTerm}. The remaining ${workItemsTerm} displayed are based on the default ${featureTerm} size specified in the advanced project settings.`}
				>
					<IconButton size="small" sx={{ ml: 1 }}>
						<GppMaybeOutlinedIcon sx={{ color: "warning.main" }} />
					</IconButton>
				</Tooltip>
			)}
			{teamsWorkIngOnFeature.length > 0 && (
				<Tooltip
					title={
						<div>
							This feature is actively being worked on by:{" "}
							<TeamLinksList teams={teamsWorkIngOnFeature} />
						</div>
					}
				>
					<IconButton size="small" sx={{ ml: 1 }}>
						<EngineeringIcon />
					</IconButton>
				</Tooltip>
			)}
		</span>
	);
};

export default FeatureName;
