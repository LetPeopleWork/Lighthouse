import type { RagTerms } from "./ragRules";

export const __SCAFFOLD__ = true;

export type BlockedRagStatus = "red" | "amber" | "green" | "none";

export type BlockedRagResult = {
	readonly ragStatus: BlockedRagStatus;
	readonly tipText: string;
};

/**
 * B2 (slice-07): drive the Blocked overview widget RAG from the MAX blocked age across the
 * currently-blocked items, calibrated on the existing blockedStalenessThresholdDays:
 *   RED   = an item is blocked past the threshold,
 *   AMBER = an item is aging toward it (within the aging band, a default fraction of the threshold),
 *   GREEN = nothing is aging,
 *   none  = threshold is 0 (RAG disabled / neutral).
 * Items with no established blockedSince baseline are excluded from the max-age computation by the
 * caller. DELIVER wires this result into the existing computeBlockedOverviewRag call site in
 * BaseMetricsView (re-driving the widget RAG from count to max age).
 *
 * RED scaffold — authored by DISTILL (ADR-025); implemented in DELIVER slice-07.
 */
export function computeBlockedMaxAgeRag(
	_maxBlockedAgeDays: number | null,
	_thresholdDays: number,
	_terms: RagTerms,
): BlockedRagResult {
	throw new Error("Not yet implemented — RED scaffold (DISTILL slice-07)");
}
