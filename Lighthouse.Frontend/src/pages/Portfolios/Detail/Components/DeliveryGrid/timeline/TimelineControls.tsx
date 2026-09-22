import {
	Box,
	Divider,
	ToggleButton,
	ToggleButtonGroup,
	Typography,
} from "@mui/material";
import type React from "react";
import { useId } from "react";
import {
	TIMELINE_PERCENTILES,
	type TimelinePercentile,
} from "./deliveryTimelineModel";
import type { TimelineView } from "./timelineView";

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
 * A named row of buttons, exactly one of which is pressed.
 *
 * **It never lets go of the last one.** Clicking the pressed button reports an empty group, and an
 * empty group here would mean a control claiming the chart is showing nothing while the chart shows
 * something. Both groups on this row reach that state by a button of their own instead - the
 * probability has no "off" to reach, and the views have "Nothing".
 *
 * Buttons rather than a dropdown because the whole value of both of these is flicking between the
 * options and watching the picture change; a dropdown hides every option but one behind a click.
 */
function ChoiceGroup<T extends string | number>({
	name,
	chosen,
	options,
	onChosen,
}: Readonly<{
	name: string;
	chosen: T;
	options: { value: T; label: string }[];
	onChosen: (next: T) => void;
}>) {
	const labelId = useId();

	return (
		<>
			<Typography variant="body2" color="text.secondary" id={labelId}>
				{name}
			</Typography>
			<ToggleButtonGroup
				exclusive
				size="small"
				value={chosen}
				onChange={(_, next: T | null) => {
					if (next !== null) {
						onChosen(next);
					}
				}}
				aria-labelledby={labelId}
			>
				{options.map((option) => (
					<ToggleButton key={option.value} value={option.value}>
						{option.label}
					</ToggleButton>
				))}
			</ToggleButtonGroup>
		</>
	);
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
			{/* "Probability" is what Settings already calls this number. */}
			<ChoiceGroup
				name="Probability"
				chosen={percentile}
				onChosen={onPercentileChosen}
				options={TIMELINE_PERCENTILES.map((option) => ({
					value: option,
					label: `${option}%`,
				}))}
			/>

			{/* Offered only where there is a choice to make. With nothing this Delivery can say
			    about its bars, the group is one button reading "Nothing" - which is not a control,
			    it is a statement dressed as one. */}
			{offered.length > 1 && (
				<>
					<Divider orientation="vertical" flexItem sx={{ mx: 1.5, my: 0.5 }} />

					<ChoiceGroup
						name="Show"
						chosen={view}
						onChosen={onViewChosen}
						options={offered.map((option) => ({
							value: option.view,
							label: option.label,
						}))}
					/>
				</>
			)}
		</Box>
	);
};

export default TimelineControls;
