import { Box, Typography } from "@mui/material";
import type React from "react";
import type {
	RealityCheckForecastLevel,
	RealityCheckSufficiency,
} from "../../../models/Forecasts/RealityCheckResult";
import { useTerminology } from "../../../services/TerminologyContext";
import { getPercentileColor } from "../../../utils/forecast/percentileColor";
import {
	actualDescription,
	bandDescription,
	horizonLabel,
	unevaluableRowCopy,
} from "./realityCheckCopy";

export interface RowExtent {
	min: number;
	max: number;
}

const EDGE_PADDING_SHARE = 0.1;
const MINIMUM_EDGE_PADDING = 1;

/**
 * Each row is scaled to itself, because horizons of different lengths complete very different counts and
 * a shared scale would invite comparing them. The actual widens the extent when it lands outside the band,
 * so a mark beyond either end stays on screen.
 */
export const rowExtent = (
	bandLow: number,
	bandHigh: number,
	actual: number,
): RowExtent => {
	const low = Math.min(bandLow, actual);
	const high = Math.max(bandHigh, actual);
	const padding = Math.max(
		(high - low) * EDGE_PADDING_SHARE,
		MINIMUM_EDGE_PADDING,
	);
	return { min: low - padding, max: high + padding };
};

export const positionIn = ({ min, max }: RowExtent, value: number): number =>
	((value - min) / (max - min)) * 100;

const visuallyHidden = {
	position: "absolute",
	width: 1,
	height: 1,
	overflow: "hidden",
	clip: "rect(0 0 0 0)",
	whiteSpace: "nowrap",
} as const;

interface BandProps {
	levels: RealityCheckForecastLevel[];
	actual: number;
	getTerm: (key: string) => string;
}

const Band: React.FC<Readonly<BandProps>> = ({ levels, actual, getTerm }) => {
	const values = levels.map(({ value }) => value);
	const bandLow = Math.min(...values);
	const bandHigh = Math.max(...values);
	const extent = rowExtent(bandLow, bandHigh, actual);
	const at = (value: number) => `${positionIn(extent, value)}%`;
	const inBandOrder = [...levels].sort((a, b) => b.probability - a.probability);

	return (
		<Box sx={{ position: "relative", height: 48, minWidth: 0 }}>
			<Box component="span" sx={visuallyHidden}>
				{`${bandDescription(inBandOrder)} ${actualDescription(actual, getTerm)}`}
			</Box>
			<Box
				aria-hidden
				sx={{
					position: "absolute",
					top: 18,
					height: 8,
					left: at(bandLow),
					width: `${positionIn(extent, bandHigh) - positionIn(extent, bandLow)}%`,
					bgcolor: "action.selected",
					borderRadius: 1,
				}}
			/>
			{inBandOrder.map(({ probability, value }) => (
				<Box
					key={probability}
					aria-hidden
					sx={{
						position: "absolute",
						top: 0,
						left: at(value),
						transform: "translateX(-50%)",
						display: "flex",
						flexDirection: "column",
						alignItems: "center",
						color: getPercentileColor(probability),
					}}
				>
					<Typography variant="caption" sx={{ lineHeight: 1.2 }}>
						{`${probability}%`}
					</Typography>
					<Box sx={{ width: 2, height: 16, bgcolor: "currentColor" }} />
				</Box>
			))}
			<Box
				aria-hidden
				sx={{
					position: "absolute",
					top: 16,
					left: at(actual),
					transform: "translateX(-50%)",
					display: "flex",
					flexDirection: "column",
					alignItems: "center",
				}}
			>
				<Box
					sx={{
						width: 12,
						height: 12,
						borderRadius: "50%",
						bgcolor: "text.primary",
					}}
				/>
				<Typography variant="caption" sx={{ fontWeight: 600, lineHeight: 1.2 }}>
					{actual}
				</Typography>
			</Box>
		</Box>
	);
};

interface RealityCheckBandRowProps {
	horizonDays: number;
	sufficiency: RealityCheckSufficiency;
	forecast: RealityCheckForecastLevel[] | null;
	actualCompleted: number | null;
	minimumActiveDays: number;
}

const RealityCheckBandRow: React.FC<Readonly<RealityCheckBandRowProps>> = ({
	horizonDays,
	sufficiency,
	forecast,
	actualCompleted,
	minimumActiveDays,
}) => {
	const { getTerm } = useTerminology();
	const label = horizonLabel(horizonDays);
	const levels = sufficiency.isSufficient ? (forecast ?? []) : [];

	return (
		<Box
			component="fieldset"
			aria-label={label}
			sx={{
				border: 0,
				margin: 0,
				padding: 0,
				minWidth: 0,
				display: "grid",
				gridTemplateColumns: "5.5rem minmax(0, 1fr)",
				alignItems: "center",
				columnGap: 2,
			}}
		>
			<Typography variant="body2">{label}</Typography>
			{levels.length === 0 || actualCompleted === null ? (
				<Typography
					variant="body2"
					color="text.secondary"
					sx={{ fontStyle: "italic" }}
				>
					{unevaluableRowCopy[sufficiency.reason]({
						daysWithCompletedWork: sufficiency.daysWithCompletedWork,
						minimumActiveDays,
						getTerm,
					})}
				</Typography>
			) : (
				<Band levels={levels} actual={actualCompleted} getTerm={getTerm} />
			)}
		</Box>
	);
};

export default RealityCheckBandRow;
