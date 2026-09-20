import { Box, Typography, useTheme } from "@mui/material";
import type React from "react";
import { markerColors } from "./timelineMarkers";

/**
 * Names the two marked days, and says which dates they are.
 *
 * A shaded column is not self-explanatory: shown one, a reader can see that a day is special and
 * has no way to learn which special thing it is. That was the state this replaces. A legend answers
 * it without hovering and without hunting — and carrying the dates means a reader whose target has
 * been scrolled out of view still knows when it is.
 */
export interface TimelineLegendProps {
	targetDate?: Date;
	today: Date;
}

const Swatch: React.FC<{ color: string; filled: boolean }> = ({
	color,
	filled,
}) => (
	<Box
		aria-hidden
		sx={{
			width: 14,
			height: 14,
			borderRadius: 0.5,
			flexShrink: 0,
			// The swatch is drawn the way the column is, so the two are recognisable as each
			// other: the target a filled band, today a ruled line.
			backgroundColor: filled ? color : "transparent",
			borderLeft: filled ? undefined : `3px solid ${color}`,
		}}
	/>
);

const Entry: React.FC<{
	color: string;
	filled: boolean;
	label: string;
	date: Date;
}> = ({ color, filled, label, date }) => (
	<Box sx={{ display: "flex", alignItems: "center", gap: 0.75 }}>
		<Swatch color={color} filled={filled} />
		<Typography variant="caption" color="text.secondary">
			{`${label} · ${date.toLocaleDateString()}`}
		</Typography>
	</Box>
);

const TimelineLegend: React.FC<TimelineLegendProps> = ({
	targetDate,
	today,
}) => {
	const marks = markerColors(useTheme());

	return (
		<Box
			data-testid="timeline-legend"
			sx={{ display: "flex", flexWrap: "wrap", gap: 2, mb: 1 }}
		>
			<Entry color={marks.today} filled={false} label="Today" date={today} />
			{targetDate && (
				<Entry
					color={marks.target}
					filled
					label="Target date"
					date={targetDate}
				/>
			)}
		</Box>
	);
};

export default TimelineLegend;
