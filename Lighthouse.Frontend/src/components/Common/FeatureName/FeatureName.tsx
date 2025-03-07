import EngineeringIcon from "@mui/icons-material/Engineering";
import GppMaybeOutlinedIcon from "@mui/icons-material/GppMaybeOutlined";
import HelpOutlineOutlinedIcon from "@mui/icons-material/HelpOutlineOutlined";
import PauseCircleOutlineOutlinedIcon from "@mui/icons-material/PauseCircleOutlineOutlined";
import PlayCircleFilledWhiteOutlinedIcon from "@mui/icons-material/PlayCircleFilledWhiteOutlined";
import StopCircleOutlinedIcon from "@mui/icons-material/StopCircleOutlined";
import { IconButton, Tooltip } from "@mui/material";
import React from "react";
import { Link } from "react-router-dom";
import type { StateCategory } from "../../../models/Feature";
import type { ITeam } from "../../../models/Team/Team";

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
		<Link key={team.id} to={`/teams/${team.id}`}>
			{team.name}
		</Link>
	));

	return (
		<span>
			{url ? (
				<Link to={url} target="_blank" rel="noopener noreferrer">
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
							{/* Initial value set to an empty fragment */}
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
