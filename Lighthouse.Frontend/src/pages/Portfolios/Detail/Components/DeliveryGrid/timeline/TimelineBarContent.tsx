import { Box, Tooltip } from "@mui/material";
import type React from "react";
import type { TimelineBar } from "./deliveryTimelineModel";
import { barTooltip } from "./ganttShapes";

/**
 * The inside of one bar on the timeline.
 *
 * The chart library lets a caller render a bar's content, and this is it — which is what keeps the
 * hover text and the click in ordinary React rather than in the library's own event system. That
 * system is a trap here: its props carry an `on${string}` index signature, so a misremembered
 * event name typechecks, compiles and silently never fires.
 *
 * It lives in its own file so it can be rendered on its own in a test. Reached only through the
 * chart, everything it decides — whether the bar is clickable, what the hover says, what happens
 * to a bar the chart asks for and we do not have — is behind a library that draws to a canvas this
 * environment does not provide.
 */
export interface TimelineBarContentProps {
	bar?: TimelineBar;
	/** Absent means the bar is inert: no pointer, no click, and nothing promised on hover. */
	onSelect?: (featureId: number) => void;
}

const TimelineBarContent: React.FC<TimelineBarContentProps> = ({
	bar,
	onSelect,
}) => {
	// The chart asked for a bar we do not have. Rendering an empty one would put a nameless
	// clickable box on the timeline; rendering nothing leaves the chart's own bar as it was.
	if (!bar) {
		return null;
	}

	const isClickable = onSelect !== undefined;

	return (
		<Tooltip title={barTooltip(bar, isClickable)} followCursor>
			<Box
				component={isClickable ? "button" : "div"}
				type={isClickable ? "button" : undefined}
				data-testid="timeline-bar-content"
				onClick={isClickable ? () => onSelect(bar.featureId) : undefined}
				sx={{
					width: "100%",
					height: "100%",
					display: "flex",
					alignItems: "center",
					justifyContent: "center",
					overflow: "hidden",
					whiteSpace: "nowrap",
					textOverflow: "ellipsis",
					px: 1,
					background: "none",
					border: "none",
					font: "inherit",
					color: "inherit",
					cursor: isClickable ? "pointer" : "default",
				}}
			>
				{bar.name}
			</Box>
		</Tooltip>
	);
};

export default TimelineBarContent;
