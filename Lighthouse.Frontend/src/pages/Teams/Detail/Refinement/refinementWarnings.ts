import type { IRefinementRow } from "../../../../models/Refinement/Refinement";
import { describeDisagreement } from "./stageWording";

const OPEN_QUESTION = "Somebody asked a question and has not voted yet.";

/** Every reason a row needs attention, one sentence each; empty when there is nothing to warn about. */
export const describeRefinementWarnings = (row: IRefinementRow): string[] => [
	...(row.signalsDisagree && row.stage
		? [describeDisagreement(row.stage)]
		: []),
	...(row.hasOpenQuestion ? [OPEN_QUESTION] : []),
];
