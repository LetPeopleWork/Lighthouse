import { Box, Typography } from "@mui/material";
import type React from "react";

/**
 * How a key's patch is painted, which has to be how the thing it stands for is painted.
 *
 * A Team wears its colour as a fill, so its patch is filled. A bar says what it has to say about
 * the target date with a cap at one end, so its patch is capped. A filled square standing for a
 * cap would tell the reader to look for the wrong thing on the chart.
 */
export type LegendSwatch = "fill" | "capStart" | "capEnd" | "capBothEnds";

export interface LegendEntry {
	id: string;
	label: string;
	color: string;
	swatch: LegendSwatch;
}

const CAP_WIDTH_PX = 3;

const swatchPainting = (entry: LegendEntry) => {
	if (entry.swatch === "fill") {
		return { backgroundColor: entry.color };
	}

	const insets: string[] = [];

	if (entry.swatch !== "capEnd") {
		insets.push(`inset ${CAP_WIDTH_PX}px 0 0 0 ${entry.color}`);
	}

	if (entry.swatch !== "capStart") {
		insets.push(`inset -${CAP_WIDTH_PX}px 0 0 0 ${entry.color}`);
	}

	return {
		boxShadow: insets.join(", "),
		border: "1px solid",
		borderColor: "divider",
	};
};

/**
 * Which patch stands for what.
 *
 * Not decoration, and for two different reasons depending on what is being named. A Team's lane is
 * only as wide as that Team's span, so at the widths this chart actually gets the name written
 * along it is routinely cut to a few characters; here every Team is named in full, once, at a width
 * nothing competes for. And a cap has no name written on it at all — a colour at the end of a bar
 * says nothing by itself to a reader who has not been told what it means.
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
						...swatchPainting(entry),
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
