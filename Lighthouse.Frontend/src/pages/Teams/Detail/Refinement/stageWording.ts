import type { RefinementStage } from "../../../../models/Refinement/Refinement";

export const STAGE_WORDS: Readonly<Record<RefinementStage, string>> = {
	Ready: "Ready",
	BeingRefined: "Being refined",
	Waiting: "Waiting",
};
