import { Typography } from "@mui/material";
import type React from "react";
import type {
	IRefinementRow,
	RefinementStage,
} from "../../../../models/Refinement/Refinement";

export const STAGE_WORDS: Readonly<Record<RefinementStage, string>> = {
	Ready: "Ready",
	BeingRefined: "Being refined",
	Waiting: "Waiting",
};

const STAGES_IN_ORDER: readonly RefinementStage[] = [
	"Ready",
	"BeingRefined",
	"Waiting",
];

export const countByStage = (
	rows: readonly IRefinementRow[],
): Record<RefinementStage, number> => {
	const counts: Record<RefinementStage, number> = {
		Ready: 0,
		BeingRefined: 0,
		Waiting: 0,
	};
	for (const row of rows) {
		if (row.stage) {
			counts[row.stage] += 1;
		}
	}
	return counts;
};

export const describeStageBreakdown = (
	rows: readonly IRefinementRow[],
): string => {
	const counts = countByStage(rows);
	return STAGES_IN_ORDER.map(
		(stage) => `${counts[stage]} ${STAGE_WORDS[stage]}`,
	).join(" · ");
};

interface StageBreakdownProps {
	rows: readonly IRefinementRow[];
}

const StageBreakdown: React.FC<Readonly<StageBreakdownProps>> = ({ rows }) => (
	<Typography variant="body2" color="text.secondary">
		{describeStageBreakdown(rows)}
	</Typography>
);

export default StageBreakdown;
