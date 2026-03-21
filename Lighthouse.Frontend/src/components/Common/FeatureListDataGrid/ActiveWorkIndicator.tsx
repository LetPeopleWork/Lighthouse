import EngineeringIcon from "@mui/icons-material/Engineering";
import HourglassEmptyIcon from "@mui/icons-material/HourglassEmpty";
import { IconButton, Tooltip } from "@mui/material";
import React from "react";
import type { IEntityReference } from "../../../models/EntityReference";
import StyledLink from "../StyledLink/StyledLink";

type ActiveWorkIndicatorProps = {
	teams: IEntityReference[];
};

const TeamLinksList: React.FC<{ teams: IEntityReference[] }> = ({ teams }) => (
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

const ARIA_LABEL = "This feature is actively being worked on";

const ActiveWorkIndicator: React.FC<ActiveWorkIndicatorProps> = ({ teams }) => {
	if (teams.length === 0) {
		return (
			<Tooltip title="No teams are currently working on this feature">
				<IconButton
					size="small"
					sx={{ ml: 1 }}
					aria-label="No active work in progress"
					data-testid="no-active-work"
				>
					<HourglassEmptyIcon />
				</IconButton>
			</Tooltip>
		);
	}

	return (
		<Tooltip
			title={
				<div>
					This feature is actively being worked on by:{" "}
					<TeamLinksList teams={teams} />
				</div>
			}
		>
			<IconButton
				size="small"
				sx={{ ml: 1 }}
				aria-label={ARIA_LABEL}
				data-testid="active-work-indicator"
			>
				<EngineeringIcon />
			</IconButton>
		</Tooltip>
	);
};

export default ActiveWorkIndicator;
