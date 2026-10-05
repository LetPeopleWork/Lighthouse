import type { RefinementStage } from "../../../../models/Refinement/Refinement";

export const STAGE_WORDS: Readonly<Record<RefinementStage, string>> = {
	Ready: "Ready",
	BeingRefined: "Being refined",
	Waiting: "Waiting",
};

export const describeDisagreement = (stage: RefinementStage): string =>
	stage === "Ready"
		? "The stage says Ready, but the votes don't agree yet."
		: `The votes say Ready, but the stage is still ${STAGE_WORDS[stage]}.`;
