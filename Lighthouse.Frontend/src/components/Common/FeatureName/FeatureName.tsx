import EngineeringIcon from "@mui/icons-material/Engineering";
import GppMaybeOutlinedIcon from "@mui/icons-material/GppMaybeOutlined";
import HelpOutlineOutlinedIcon from "@mui/icons-material/HelpOutlineOutlined";
import PauseCircleOutlineOutlinedIcon from "@mui/icons-material/PauseCircleOutlineOutlined";
import PlayCircleFilledWhiteOutlinedIcon from "@mui/icons-material/PlayCircleFilledWhiteOutlined";
import StopCircleOutlinedIcon from "@mui/icons-material/StopCircleOutlined";
import { IconButton, Link, Tooltip } from "@mui/material";
import React from "react";
import type { ITeam } from "../../../models/Team/Team";
import type { StateCategory } from "../../../models/WorkItem";
import StyledLink from "../StyledLink/StyledLink";

interface FeatureNameProps {
	name: string;
	url: string;
	stateCategory: StateCategory;
	isUsingDefaultFeatureSize: boolean;
	teamsWorkIngOnFeature: ITeam[];
}

const FeatureName: React.FC<FeatureNameProps> = ({
	name,
	url,
	stateCategory,
	isUsingDefaultFeatureSize,
	teamsWorkIngOnFeature,
}) => {
	const teamLinks = teamsWorkIngOnFeature.map((team) => (
		<StyledLink key={team.id} to={`/teams/${team.id}`} variant="body2">
			{team.name}
		</StyledLink>
	));

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
				<Tooltip title="No child items were found for this Feature. The remaining items displayed are based on the default feature size specified in the advanced project settings.">
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
							{teamLinks.reduce((acc, curr, index) => (
								<React.Fragment key={teamLinks[index].key}>
									{acc}
									{index > 0 && ", "}
									{curr}
								</React.Fragment>
							))}{" "}
						</div>
					}
				>
					<IconButton size="small" sx={{ ml: 1 }}>
						<EngineeringIcon />
					</IconButton>
				</Tooltip>
			)}

			<Tooltip title={`Feature State: ${stateCategory}`}>
				<IconButton size="small" sx={{ ml: 1 }}>
					{stateCategory === "ToDo" && <PauseCircleOutlineOutlinedIcon />}
					{stateCategory === "Doing" && <PlayCircleFilledWhiteOutlinedIcon />}
					{stateCategory === "Done" && <StopCircleOutlinedIcon />}
					{stateCategory === "Unknown" && <HelpOutlineOutlinedIcon />}
				</IconButton>
			</Tooltip>
		</span>
	);
};

export default FeatureName;
