import { Box, Card, CardContent, Typography, useTheme } from "@mui/material";
import type React from "react";
import { appColors } from "../../../utils/theme/colors";

type LoadBalanceMatrixPoint = {
	readonly date: Date;
	readonly dayOffset: number;
	readonly dateLabel: string;
	readonly wip: number;
	readonly totalWorkItemAge: number;
	readonly opacity: number;
};

type LoadBalanceMatrixData = {
	readonly baselineAvailable: boolean;
	readonly averageWip: number | null;
	readonly averageTotalWorkItemAge: number | null;
	readonly points: ReadonlyArray<LoadBalanceMatrixPoint>;
};

interface LoadBalanceMatrixChartProps {
	data: LoadBalanceMatrixData;
}

const chartGeometry = {
	width: 760,
	height: 360,
	marginLeft: 52,
	marginRight: 20,
	marginTop: 20,
	marginBottom: 46,
} as const;

function getAxisMax(
	values: ReadonlyArray<number>,
	minimum: number,
	paddingFactor: number,
	extraHeadroom: number,
): number {
	const rawMax = values.length > 0 ? Math.max(...values) : 0;
	const base = Math.max(minimum, rawMax);
	const padded = base * (1 + paddingFactor);
	return Math.max(base + extraHeadroom, padded);
}

function formatTick(value: number): string {
	if (value >= 100) {
		return `${Math.round(value)}`;
	}
	if (value >= 10) {
		return value.toFixed(0);
	}
	if (value >= 1) {
		return value.toFixed(1).replace(".0", "");
	}
	return value.toFixed(2).replace(/0+$/, "").replace(/\.$/, "");
}

function clamp(value: number, min: number, max: number): number {
	return Math.max(min, Math.min(value, max));
}

function clamp01(value: number): number {
	return clamp(value, 0, 1);
}

function getCenteredFraction(
	value: number,
	baseline: number,
	upperBound: number,
): number {
	if (value <= baseline) {
		if (baseline <= 0) {
			return 0.5;
		}
		return 0.5 * (value / baseline);
	}

	const upperSpan = Math.max(upperBound - baseline, 1e-6);
	return 0.5 + 0.5 * ((value - baseline) / upperSpan);
}

function getTickValueFromFraction(
	fraction: number,
	maxValue: number,
	baseline: number,
	centerBaseline: boolean,
): number {
	if (!centerBaseline) {
		return fraction * maxValue;
	}

	if (fraction <= 0.5) {
		if (baseline <= 0) {
			return 0;
		}
		return (fraction / 0.5) * baseline;
	}

	return baseline + ((fraction - 0.5) / 0.5) * (maxValue - baseline);
}

function formatPointLabel(point: LoadBalanceMatrixPoint): string {
	const dayLabel =
		point.dayOffset === 0 ? "Selected End Date" : `+${point.dayOffset} day(s)`;
	return `${dayLabel} (${point.dateLabel}) | WIP: ${point.wip} | TWIA: ${point.totalWorkItemAge}`;
}

const LoadBalanceMatrixChart: React.FC<LoadBalanceMatrixChartProps> = ({
	data,
}) => {
	const theme = useTheme();

	const points = data.points;
	const wipValues = points.map((p) => p.wip);
	const totalAgeValues = points.map((p) => p.totalWorkItemAge);

	const averageWip = data.averageWip ?? 0;
	const averageTotalAge = data.averageTotalWorkItemAge ?? 0;
	const maxWipSource = data.baselineAvailable
		? [...wipValues, averageWip]
		: wipValues;
	const maxAgeSource = data.baselineAvailable
		? [...totalAgeValues, averageTotalAge]
		: totalAgeValues;

	const yMax = getAxisMax(maxWipSource, 5, 0.2, 2);
	const xMax = getAxisMax(maxAgeSource, 10, 0.2, 5);
	const centerBaseline = data.baselineAvailable;

	const plotLeft = chartGeometry.marginLeft;
	const plotTop = chartGeometry.marginTop;
	const plotRight = chartGeometry.width - chartGeometry.marginRight;
	const plotBottom = chartGeometry.height - chartGeometry.marginBottom;
	const plotWidth = plotRight - plotLeft;
	const plotHeight = plotBottom - plotTop;

	const toX = (value: number): number => {
		const fraction = centerBaseline
			? getCenteredFraction(value, averageTotalAge, xMax)
			: value / xMax;
		return plotLeft + clamp01(fraction) * plotWidth;
	};
	const toY = (value: number): number => {
		const fraction = centerBaseline
			? getCenteredFraction(value, averageWip, yMax)
			: value / yMax;
		return plotBottom - clamp01(fraction) * plotHeight;
	};

	const baselineX = centerBaseline
		? plotLeft + plotWidth / 2
		: clamp(toX(averageTotalAge), plotLeft, plotRight);
	const baselineY = centerBaseline
		? plotTop + plotHeight / 2
		: clamp(toY(averageWip), plotTop, plotBottom);

	const tickFractions = [0, 0.25, 0.5, 0.75, 1];
	const xTicks = tickFractions.map((fraction) => ({
		fraction,
		value: getTickValueFromFraction(
			fraction,
			xMax,
			averageTotalAge,
			centerBaseline,
		),
	}));
	const yTicks = tickFractions.map((fraction) => ({
		fraction,
		value: getTickValueFromFraction(fraction, yMax, averageWip, centerBaseline),
	}));

	const projectionCollapsed =
		points.length > 1 &&
		points.every(
			(p) =>
				p.wip === points[0].wip &&
				p.totalWorkItemAge === points[0].totalWorkItemAge,
		);

	const dotColor =
		theme.palette.mode === "dark"
			? appColors.primary.light
			: appColors.primary.main;

	return (
		<Card sx={{ p: 2, borderRadius: 2, height: "100%" }}>
			<CardContent
				sx={{
					p: 0,
					height: "100%",
					display: "flex",
					flexDirection: "column",
					"&:last-child": { pb: 0 },
				}}
			>
				<Typography variant="h6" sx={{ mb: 1 }}>
					Load Balance Matrix
				</Typography>
				<Box sx={{ width: "100%", flex: 1, minHeight: 0 }}>
					<svg
						data-testid="load-balance-matrix-svg"
						viewBox={`0 0 ${chartGeometry.width} ${chartGeometry.height}`}
						width="100%"
						height="100%"
						preserveAspectRatio="none"
						aria-hidden="true"
					>
						{yTicks.map((tick) => {
							const y = plotBottom - tick.fraction * plotHeight;
							return (
								<line
									key={`grid-y-${tick.fraction}`}
									x1={plotLeft}
									y1={y}
									x2={plotRight}
									y2={y}
									stroke={theme.palette.divider}
									strokeWidth={1}
								/>
							);
						})}
						{xTicks.map((tick) => {
							const x = plotLeft + tick.fraction * plotWidth;
							return (
								<line
									key={`grid-x-${tick.fraction}`}
									x1={x}
									y1={plotTop}
									x2={x}
									y2={plotBottom}
									stroke={theme.palette.divider}
									strokeWidth={0.5}
								/>
							);
						})}

						{data.baselineAvailable && (
							<>
								<line
									data-testid="load-balance-baseline-x"
									x1={baselineX}
									y1={plotTop}
									x2={baselineX}
									y2={plotBottom}
									stroke={theme.palette.text.secondary}
									strokeWidth={2}
									strokeDasharray="6 6"
								/>
								<line
									data-testid="load-balance-baseline-y"
									x1={plotLeft}
									y1={baselineY}
									x2={plotRight}
									y2={baselineY}
									stroke={theme.palette.text.secondary}
									strokeWidth={2}
									strokeDasharray="6 6"
								/>
								<text
									x={plotLeft + 4}
									y={Math.max(plotTop + 12, baselineY - 6)}
									fontSize="13"
									fill={theme.palette.text.secondary}
								>
									{`WIP Avg: ${formatTick(averageWip)}`}
								</text>
								<text
									x={baselineX}
									y={plotTop + 16}
									fontSize="13"
									textAnchor="middle"
									fill={theme.palette.text.secondary}
								>
									{`TWIA Average: ${formatTick(averageTotalAge)}`}
								</text>
							</>
						)}

						<line
							x1={plotLeft}
							y1={plotBottom}
							x2={plotRight}
							y2={plotBottom}
							stroke={theme.palette.text.primary}
							strokeWidth={1.6}
						/>
						<line
							x1={plotLeft}
							y1={plotTop}
							x2={plotLeft}
							y2={plotBottom}
							stroke={theme.palette.text.primary}
							strokeWidth={1.6}
						/>

						{xTicks.map((tick) => {
							const x = plotLeft + tick.fraction * plotWidth;
							return (
								<text
									key={`x-tick-${tick.fraction}`}
									x={x}
									y={plotBottom + 18}
									fontSize="12"
									textAnchor="middle"
									fill={theme.palette.text.secondary}
								>
									{formatTick(tick.value)}
								</text>
							);
						})}

						{yTicks.map((tick) => {
							const y = plotBottom - tick.fraction * plotHeight;
							return (
								<text
									key={`y-tick-${tick.fraction}`}
									x={plotLeft - 10}
									y={y + 4}
									fontSize="12"
									textAnchor="end"
									fill={theme.palette.text.secondary}
								>
									{formatTick(tick.value)}
								</text>
							);
						})}

						{points.map((point) => {
							const cx = toX(point.totalWorkItemAge);
							const cy = toY(point.wip);

							if (projectionCollapsed && point.dayOffset > 0) {
								return (
									<circle
										key={`${point.dayOffset}-${point.dateLabel}`}
										cx={cx}
										cy={cy}
										r={4 + point.dayOffset * 1.3}
										fill={dotColor}
										stroke={dotColor}
										strokeWidth={1.4}
										opacity={point.opacity}
									>
										<title>{formatPointLabel(point)}</title>
									</circle>
								);
							}

							return (
								<circle
									key={`${point.dayOffset}-${point.dateLabel}`}
									cx={cx}
									cy={cy}
									r={point.dayOffset === 0 ? 6 : 4}
									fill={dotColor}
									stroke={dotColor}
									strokeWidth={point.dayOffset === 0 ? 2 : 1}
									opacity={point.opacity}
								>
									<title>{formatPointLabel(point)}</title>
								</circle>
							);
						})}

						<text
							x={plotLeft + plotWidth / 2}
							y={chartGeometry.height - 10}
							fontSize="16"
							textAnchor="middle"
							fill={theme.palette.text.secondary}
						>
							Total Work Item Age
						</text>
						<text
							x={16}
							y={plotTop + plotHeight / 2}
							fontSize="16"
							textAnchor="middle"
							transform={`rotate(-90 16 ${plotTop + plotHeight / 2})`}
							fill={theme.palette.text.secondary}
						>
							WIP
						</text>

						{projectionCollapsed && (
							<text
								x={plotLeft + 8}
								y={plotTop + 18}
								fontSize="12"
								fill={theme.palette.text.secondary}
							>
								No projected movement (WIP = 0)
							</text>
						)}
					</svg>
				</Box>
			</CardContent>
		</Card>
	);
};

export default LoadBalanceMatrixChart;
