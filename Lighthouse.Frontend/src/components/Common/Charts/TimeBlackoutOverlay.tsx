import { useTheme } from "@mui/material";
import { useDrawingArea, useXScale } from "@mui/x-charts/hooks";
import type React from "react";
import type { IBlackoutPeriod } from "../../../models/BlackoutPeriod";
import { appColors } from "../../../utils/theme/colors";

type TimeBlackoutOverlayProps = {
	readonly blackoutPeriods: readonly IBlackoutPeriod[];
	readonly axisId?: string;
};

const TimeBlackoutOverlay: React.FC<TimeBlackoutOverlayProps> = ({
	blackoutPeriods,
	axisId,
}) => {
	const { top, height } = useDrawingArea();
	const xScale = useXScale(axisId) as unknown as {
		(value: number): number | undefined;
		domain: () => [number, number];
		range: () => [number, number];
	};
	const theme = useTheme();

	if (blackoutPeriods.length === 0) {
		return null;
	}

	const hatchColor =
		theme.palette.mode === "dark"
			? appColors.dark.text.secondary
			: appColors.light.text.secondary;

	const [domainMin, domainMax] = xScale.domain();

	const dayBands: Array<{ x: number; width: number }> = [];

	for (const period of blackoutPeriods) {
		// Expand each period into individual days
		const startDate = new Date(`${period.start}T00:00:00Z`);
		const endDate = new Date(`${period.end}T00:00:00Z`);

		for (
			let d = new Date(startDate);
			d <= endDate;
			d.setUTCDate(d.getUTCDate() + 1)
		) {
			const dayStart = new Date(d).getTime();
			const dayEnd = dayStart + 24 * 60 * 60 * 1000;

			// Skip if this day is entirely outside the visible domain
			if (dayEnd < domainMin || dayStart > domainMax) continue;

			const clampedStart = Math.max(dayStart, domainMin);
			const clampedEnd = Math.min(dayEnd, domainMax);

			const x1 = xScale(clampedStart);
			const x2 = xScale(clampedEnd);

			if (x1 == null || x2 == null) continue;

			dayBands.push({ x: x1, width: x2 - x1 });
		}
	}

	if (dayBands.length === 0) {
		return null;
	}

	return (
		<g data-testid="blackout-overlay">
			<defs>
				<pattern
					id="time-blackout-hatch"
					patternUnits="userSpaceOnUse"
					width={6}
					height={6}
					patternTransform="rotate(45)"
				>
					<line
						x1={0}
						y1={0}
						x2={0}
						y2={6}
						stroke={hatchColor}
						strokeWidth={1.5}
						strokeOpacity={0.5}
					/>
				</pattern>
			</defs>
			{dayBands.map((band) => (
				<g key={`time-blackout-${band.x}-${band.width}`}>
					<rect
						x={band.x}
						y={top}
						width={band.width}
						height={height}
						fill={hatchColor}
						fillOpacity={0.08}
					/>
					<rect
						x={band.x}
						y={top}
						width={band.width}
						height={height}
						fill="url(#time-blackout-hatch)"
					/>
				</g>
			))}
		</g>
	);
};

export default TimeBlackoutOverlay;
