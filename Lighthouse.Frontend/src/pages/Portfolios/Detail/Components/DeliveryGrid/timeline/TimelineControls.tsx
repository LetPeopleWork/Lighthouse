import {
	Box,
	Divider,
	ToggleButton,
	ToggleButtonGroup,
	Typography,
} from "@mui/material";
import type React from "react";
import {
	TIMELINE_PERCENTILES,
	type TimelinePercentile,
} from "./deliveryTimelineModel";
import type { TimelineView } from "./timelineView";

const PROBABILITY_LABEL_ID = "delivery-timeline-probability";
const VIEW_LABEL_ID = "delivery-timeline-view";

/**
 * One thing the chart can be asked to show, and whether this Delivery can answer it.
 *
 * A view the chart cannot act on is left out of the group entirely rather than offered and inert.
 * It is still worth the click that proves it does nothing, and a reader who gets nothing back
 * concludes the chart is broken rather than that the question does not apply here.
 */
export interface TimelineViewOption {
	view: TimelineView;
	label: string;
	offered: boolean;
}

export interface TimelineControlsProps {
	percentile: TimelinePercentile;
	onPercentileChosen: (chosen: TimelinePercentile) => void;
	view: TimelineView;
	onViewChosen: (chosen: TimelineView) => void;
	views: TimelineViewOption[];
}

/**
 * The row above the chart: how confident to be, and what to be told.
 *
 * Two groups of the same kind, because they are two questions of the same kind - pick one of these,
 * and the picture changes. The first was here already; the second reads as its sibling rather than
 * as a row of independent switches, which is what it looked like when each thing the chart could
 * say had a switch of its own.
 *
 * It wraps rather than overflows. Two groups do not fit the narrowest width a Delivery is shown at,
 * and a row the reader has to scroll sideways hides whichever control ends up last.
 */
const TimelineControls: React.FC<TimelineControlsProps> = ({
	percentile,
	onPercentileChosen,
	view,
	onViewChosen,
	views,
}) => {
	const offered = views.filter((option) => option.offered);

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

			{/* Offered only where there is a choice to make. With nothing this Delivery can say
			    about its bars, the group is one button reading "Nothing" - which is not a control,
			    it is a statement dressed as one. */}
			{offered.length > 1 && (
				<>
					<Divider orientation="vertical" flexItem sx={{ mx: 1.5, my: 0.5 }} />

					<Typography variant="body2" color="text.secondary" id={VIEW_LABEL_ID}>
						Show
					</Typography>
					<ToggleButtonGroup
						exclusive
						size="small"
						value={view}
						onChange={(_, chosen: TimelineView | null) => {
							// Clicking the active button reports null. "Nothing" is already one of
							// the buttons, so the useful thing to do with that is nothing at all -
							// otherwise the group would have two ways to reach one state and a
							// reader could empty it by accident.
							if (chosen !== null) {
								onViewChosen(chosen);
							}
						}}
						aria-labelledby={VIEW_LABEL_ID}
					>
						{offered.map((option) => (
							<ToggleButton key={option.view} value={option.view}>
								{option.label}
							</ToggleButton>
						))}
					</ToggleButtonGroup>
				</>
			)}
		</Box>
	);
};

export default TimelineControls;
