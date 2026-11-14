import { useDrawingArea } from "@mui/x-charts";
import type React from "react";
import type { MarkerSpec } from "../../../utils/charts/offScaleLines";

export const MARKER_ROW_HEIGHT = 16;
const MARKER_GAP = 6;

interface OffScaleLineMarkersProps {
	readonly markers: readonly MarkerSpec[];
}

/**
 * Names, at the top edge of the plot, every percentile or SLE line that lies above the axis. The
 * popover says the same in words, so the markers are hidden from assistive technology.
 */
const OffScaleLineMarkers: React.FC<OffScaleLineMarkersProps> = ({
	markers,
}) => {
	const { left, top, width } = useDrawingArea();

	if (markers.length === 0) return null;

	return (
		// biome-ignore lint/a11y/noAriaHiddenOnFocusable: an SVG group of plain text is not focusable
		<g aria-hidden="true">
			{markers.map((marker) => (
				<text
					key={marker.key}
					x={left + width}
					y={top - MARKER_GAP - marker.row * MARKER_ROW_HEIGHT}
					textAnchor="end"
					fontSize={12}
					fill={marker.color}
				>
					<tspan>{"▲ "}</tspan>
					<tspan>{marker.text}</tspan>
				</text>
			))}
		</g>
	);
};

export default OffScaleLineMarkers;
