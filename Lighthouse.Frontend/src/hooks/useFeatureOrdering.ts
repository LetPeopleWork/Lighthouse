import type { FeatureOrderingPolicy } from "../models/FeatureOrdering";

export interface FeatureOrderingState {
	/** Who decides the order this instance forecasts in. */
	policy: FeatureOrderingPolicy;
	/** What the position column calls itself under that policy. */
	positionColumnLabel: string;
	isLoading: boolean;
}

/**
 * The single place the ordering policy is read on the client (ADR-134 SA-12). Four scattered `if`s over
 * the same question is the frontend twin of the five-`if` backend failure this epic exists to prevent.
 */
// __SCAFFOLD__ — Epic 5375 slice 02
export const useFeatureOrdering = (): FeatureOrderingState => {
	throw new Error("Not yet implemented — RED scaffold");
};
