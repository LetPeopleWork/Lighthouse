import { Box, Typography } from "@mui/material";
import type React from "react";

export interface LegendEntry {
	id: string;
	label: string;
	color: string;
}

/**
 * Which patch stands for what.
 *
 * Not decoration, and for two different reasons depending on what is being named. A Team's lane is
 * only as wide as that Team's span, so at the widths this chart actually gets the name written
 * along it is routinely cut to a few characters; here every Team is named in full, once, at a width
 * nothing competes for. And a bar wearing a status colour has no name written on it at all - the
 * colour says nothing by itself to a reader who has not been told what it means, and with two
 * greens on the chart it is the key that tells finished from on track.
 *
 * It sits in its own file for the same reason the bar's content does - so that what is drawn can be
 * rendered and read on its own, without standing up the tab that composes it.
 */
const TimelineLegend: React.FC<{ entries: LegendEntry[]; testId?: string }> = ({
	entries,
	testId = "timeline-legend",
}) => (
	<Box
		data-testid={testId}
		sx={{ display: "flex", flexWrap: "wrap", alignItems: "center", gap: 1.5 }}
	>
		{entries.map((entry) => (
			<Box
				key={entry.id}
				sx={{ display: "flex", alignItems: "center", gap: 0.5 }}
			>
				{/* The name beside it carries the meaning, so the patch itself is shown to the eye
				    and hidden from anything reading the page aloud. */}
				<Box
					aria-hidden="true"
					data-testid={`${testId}-swatch`}
					sx={{
						width: 12,
						height: 12,
						borderRadius: 0.5,
						flexShrink: 0,
						backgroundColor: entry.color,
					}}
				/>
				<Typography variant="caption" color="text.secondary">
					{entry.label}
				</Typography>
			</Box>
		))}
	</Box>
);

export default TimelineLegend;
