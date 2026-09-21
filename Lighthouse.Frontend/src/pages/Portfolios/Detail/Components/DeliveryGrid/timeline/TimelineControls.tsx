import {
	Box,
	Divider,
	FormControlLabel,
	Switch,
	ToggleButton,
	ToggleButtonGroup,
	Typography,
} from "@mui/material";
import type React from "react";
import {
	TIMELINE_PERCENTILES,
	type TimelinePercentile,
} from "./deliveryTimelineModel";

const PROBABILITY_LABEL_ID = "delivery-timeline-probability";

/**
 * One thing the reader can choose to be shown.
 *
 * `offered` is not the same as unchecked. A control the chart cannot act on is withheld entirely
 * rather than shown inert: it is still worth the click that proves it does nothing, and a reader
 * who gets nothing back concludes the chart is broken rather than that the question does not apply
 * to this Delivery.
 */
export interface TimelineToggle {
	id: string;
	label: string;
	offered: boolean;
	shown: boolean;
	toggle: () => void;
}

export interface TimelineControlsProps {
	percentile: TimelinePercentile;
	onPercentileChosen: (chosen: TimelinePercentile) => void;
	toggles: TimelineToggle[];
}

/**
 * The row above the chart: how confident to be, and what to be shown.
 *
 * It wraps rather than overflows. Four controls do not fit the narrowest width a Delivery is shown
 * at, and a row the reader has to scroll sideways hides whichever control ends up last.
 */
const TimelineControls: React.FC<TimelineControlsProps> = ({
	percentile,
	onPercentileChosen,
	toggles,
}) => {
	const offered = toggles.filter((toggle) => toggle.offered);

	return (
		<Box
			sx={{
				display: "flex",
				flexWrap: "wrap",
				alignItems: "center",
				columnGap: 1,
				rowGap: 0.5,
				mb: 2,
			}}
		>
			{/* "Probability" is what Settings already calls this number. Three buttons rather than a
			    dropdown because the whole value here is flicking between them and watching every bar
			    move; a dropdown hides two of the three behind a click. */}
			<Typography
				variant="body2"
				color="text.secondary"
				id={PROBABILITY_LABEL_ID}
			>
				Probability
			</Typography>
			<ToggleButtonGroup
				exclusive
				size="small"
				value={percentile}
				onChange={(_, chosen: TimelinePercentile | null) => {
					// Null arrives when the active button is clicked again. A timeline with no
					// percentile selected would have nothing to draw, so the choice stands.
					if (chosen !== null) {
						onPercentileChosen(chosen);
					}
				}}
				aria-labelledby={PROBABILITY_LABEL_ID}
			>
				{TIMELINE_PERCENTILES.map((option) => (
					<ToggleButton key={option} value={option}>
						{`${option}%`}
					</ToggleButton>
				))}
			</ToggleButtonGroup>

			{/* Switches rather than more buttons beside the three: that group means "pick one of
			    these", and each of these is on or off - which is also why they are set apart from it
			    rather than sitting flush against it as further members of it. */}
			{offered.length > 0 && (
				<Divider orientation="vertical" flexItem sx={{ mx: 1.5, my: 0.5 }} />
			)}

			{offered.map((toggle) => (
				<FormControlLabel
					key={toggle.id}
					control={
						<Switch
							size="small"
							checked={toggle.shown}
							onChange={toggle.toggle}
						/>
					}
					label={toggle.label}
				/>
			))}
		</Box>
	);
};

export default TimelineControls;
