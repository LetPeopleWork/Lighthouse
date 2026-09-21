import { Box, Typography } from "@mui/material";
import type React from "react";
import type { TeamColour } from "./deliveryTeamLanes";

/**
 * Which colour stands for which Team.
 *
 * Not decoration, and not redundant with the names written along the lanes: a lane is only as wide
 * as its Team's span, so at the widths this chart actually gets those names are routinely cut to a
 * few characters. Here every Team is named in full, once, at a width nothing competes for.
 *
 * It sits in its own file for the same reason the bar's content does - so that what is drawn can
 * be rendered and read on its own, without standing up the tab that composes it.
 */
const TimelineTeamLegend: React.FC<{ teams: TeamColour[] }> = ({ teams }) => (
	<Box
		data-testid="timeline-team-legend"
		sx={{ display: "flex", flexWrap: "wrap", alignItems: "center", gap: 1.5 }}
	>
		{teams.map((team) => (
			<Box
				key={team.teamId}
				sx={{ display: "flex", alignItems: "center", gap: 0.5 }}
			>
				{/* The name beside it carries the meaning, so the patch itself is shown to the eye
				    and hidden from anything reading the page aloud. */}
				<Box
					aria-hidden="true"
					sx={{
						width: 12,
						height: 12,
						borderRadius: 0.5,
						flexShrink: 0,
						backgroundColor: team.color,
					}}
				/>
				<Typography variant="caption" color="text.secondary">
					{team.teamName}
				</Typography>
			</Box>
		))}
	</Box>
);

export default TimelineTeamLegend;
