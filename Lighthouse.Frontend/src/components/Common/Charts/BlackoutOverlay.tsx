import { useTheme } from "@mui/material";
import { useDrawingArea, useXScale } from "@mui/x-charts/hooks";
import type React from "react";
import { appColors } from "../../../utils/theme/colors";

type BlackoutOverlayProps = {
	readonly blackoutDayLabels: readonly string[];
};

const BlackoutOverlay: React.FC<BlackoutOverlayProps> = ({
	blackoutDayLabels,
}) => {
	const { top, height } = useDrawingArea();
	const xScale = useXScale() as unknown as {
		bandwidth?: () => number;
		step?: () => number;
		(value: string): number | undefined;
	};
	const theme = useTheme();

	if (blackoutDayLabels.length === 0) {
		return null;
	}

	const hatchColor =
		theme.palette.mode === "dark"
			? appColors.dark.text.secondary
			: appColors.light.text.secondary;

	const bandwidthValue =
		typeof xScale.bandwidth === "function" ? xScale.bandwidth() : 0;
	const stepValue = typeof xScale.step === "function" ? xScale.step() : 0;
	const columnWidth = bandwidthValue > 0 ? bandwidthValue : stepValue;
	const columnOffset = bandwidthValue > 0 ? 0 : columnWidth / 2;

	return (
		<g data-testid="blackout-overlay">
			<defs>
				<pattern
					id="blackout-hatch"
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
			{blackoutDayLabels.map((label) => {
				const x = xScale(label);
				if (x == null) return null;
				return (
					<g key={`blackout-${label}`}>
						<rect
							x={x - columnOffset}
							y={top}
							width={columnWidth}
							height={height}
							fill={hatchColor}
							fillOpacity={0.08}
						/>
						<rect
							x={x - columnOffset}
							y={top}
							width={columnWidth}
							height={height}
							fill="url(#blackout-hatch)"
						/>
					</g>
				);
			})}
		</g>
	);
};

export default BlackoutOverlay;
