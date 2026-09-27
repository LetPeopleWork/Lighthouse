import { Box, LinearProgress, Stack, Typography } from "@mui/material";
import type React from "react";
import { ForecastLevel } from "../../../components/Common/Forecasts/ForecastLevel";
import type {
	RealityCheckCell,
	RealityCheckLevelCoverage,
} from "../../../models/Forecasts/RealityCheckResult";
import RealityCheckLevelLabel from "./RealityCheckLevelLabel";
import {
	type LevelRowFacts,
	levelBarName,
	levelRowText,
} from "./realityCheckCopy";
import {
	gradedChecksAt,
	heldShare,
	levelCloseness,
} from "./realityCheckGrading";

// Neither the level's own colour nor a grade fill: either would read as a judgement of the level.
const barSx = {
	height: 8,
	borderRadius: 1,
	bgcolor: "action.disabledBackground",
	"& .MuiLinearProgress-bar": { bgcolor: "primary.main", transition: "none" },
};

// The tick overhangs the bar so it stays visible whatever part of the bar it crosses.
const tickSx = (confidenceLevel: number) => ({
	position: "absolute",
	left: `${confidenceLevel}%`,
	top: -4,
	bottom: -4,
	width: 2,
	bgcolor: "text.primary",
});

interface RealityCheckLevelRowProps {
	level: RealityCheckLevelCoverage;
	runsEvaluated: number;
	cells: readonly RealityCheckCell[];
}

const RealityCheckLevelRow: React.FC<Readonly<RealityCheckLevelRowProps>> = ({
	level,
	runsEvaluated,
	cells,
}) => {
	const { confidenceLevel, heldCount, reading } = level;
	const facts: LevelRowFacts = {
		confidenceLevel,
		levelName: new ForecastLevel(confidenceLevel).level,
		heldCount,
		runsEvaluated,
		share:
			reading === "NotEvaluated" ? null : heldShare(heldCount, runsEvaluated),
		withinTenPercent: levelCloseness(gradedChecksAt(confidenceLevel, cells))
			.withinTenPercent,
	};

	return (
		<Stack
			direction={{ xs: "column", sm: "row" }}
			spacing={{ xs: 0.5, sm: 2 }}
			sx={{ alignItems: { sm: "center" } }}
		>
			<Box sx={{ minWidth: 150 }}>
				<RealityCheckLevelLabel confidenceLevel={confidenceLevel} />
			</Box>
			<Box
				role="img"
				aria-label={levelBarName(facts)}
				sx={{ position: "relative", width: { xs: "100%", sm: 240 } }}
			>
				<LinearProgress
					variant="determinate"
					value={facts.share ?? 0}
					aria-hidden
					sx={barSx}
				/>
				<Box aria-hidden sx={tickSx(confidenceLevel)} />
			</Box>
			<Typography variant="body2">{levelRowText(facts)}</Typography>
		</Stack>
	);
};

export default RealityCheckLevelRow;
