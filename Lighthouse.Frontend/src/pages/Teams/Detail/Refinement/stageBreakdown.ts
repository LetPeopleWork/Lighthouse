import type {
	IRefinementRow,
	RefinementStage,
} from "../../../../models/Refinement/Refinement";
import { STAGE_WORDS } from "./stageWording";

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
