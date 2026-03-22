import { useTheme } from "@mui/material";
import { useDrawingArea, useXScale } from "@mui/x-charts/hooks";
import type React from "react";
import type { ProcessBehaviourChartDataPoint } from "../../../models/Metrics/ProcessBehaviourChartData";
import { appColors } from "../../../utils/theme/colors";

type PbcBlackoutOverlayProps = {
	readonly dataPoints: readonly ProcessBehaviourChartDataPoint[];
	readonly useEqualSpacing: boolean;
	readonly axisId?: string;
};

const PbcBlackoutOverlay: React.FC<PbcBlackoutOverlayProps> = ({
	dataPoints,
	useEqualSpacing,
	axisId = "xAxis",
}) => {
	const { top, width, height } = useDrawingArea();
	const xScale = useXScale(axisId) as unknown as {
		bandwidth?: () => number;
		step?: () => number;
		(value: number): number | undefined;
	};
	const theme = useTheme();

	const blackoutPoints = dataPoints
		.map((p, index) => ({ point: p, index }))
		.filter(({ point }) => point.isBlackout);

	if (blackoutPoints.length === 0) {
		return null;
	}

	const hatchColor =
		theme.palette.mode === "dark"
			? appColors.dark.text.secondary
			: appColors.light.text.secondary;

	const bandwidthValue =
		typeof xScale.bandwidth === "function" ? xScale.bandwidth() : 0;
	const stepValue = typeof xScale.step === "function" ? xScale.step() : 0;

	const getColumnLayout = () => {
		if (bandwidthValue > 0) {
			return { columnWidth: bandwidthValue, columnOffset: 0 };
		}
		if (stepValue > 0) {
			return { columnWidth: stepValue, columnOffset: stepValue / 2 };
		}
		// Time scale fallback: estimate day width from drawing area
		const dayWidth = dataPoints.length > 1 ? width / dataPoints.length : width;
		return { columnWidth: dayWidth, columnOffset: dayWidth / 2 };
	};

	const { columnWidth, columnOffset } = getColumnLayout();

	return (
		<g data-testid="blackout-overlay">
			<defs>
				<pattern
					id="pbc-blackout-hatch"
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
			{blackoutPoints.map(({ point, index }) => {
				const xValue = useEqualSpacing
					? index
					: new Date(point.xValue).getTime();
				const x = xScale(xValue);
				if (x == null) return null;
				return (
					<g key={`pbc-blackout-${index}`}>
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
							fill="url(#pbc-blackout-hatch)"
						/>
					</g>
				);
			})}
		</g>
	);
};

export default PbcBlackoutOverlay;
