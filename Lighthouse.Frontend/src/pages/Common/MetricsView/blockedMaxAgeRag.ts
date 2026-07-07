import type { RagTerms } from "./ragRules";

/**
 * Aging band as a fraction of the staleness threshold: at/above this fraction (but below the
 * threshold) the oldest blocker is AMBER. Fixed default per OQ-EB-D1 — not a user setting.
 */
const AMBER_BAND_FRACTION = 0.75;

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
	maxBlockedAgeDays: number | null,
	thresholdDays: number,
	terms: RagTerms,
): BlockedRagResult {
	if (thresholdDays <= 0) {
		return {
			ragStatus: "none",
			tipText: `Define a ${terms.blocked} staleness threshold in settings to track aging ${terms.workItems}.`,
		};
	}

	if (maxBlockedAgeDays === null) {
		return {
			ragStatus: "green",
			tipText: `No ${terms.blocked} ${terms.workItems}.`,
		};
	}

	// At-threshold counts as past, consistent with the shipped blocked-duration staleness `>=` rule.
	if (maxBlockedAgeDays >= thresholdDays) {
		return {
			ragStatus: "red",
			tipText: `Oldest ${terms.blocked} ${terms.workItem} has been blocked ${maxBlockedAgeDays} days, past the ${thresholdDays}-day threshold. Unblock it now.`,
		};
	}

	if (maxBlockedAgeDays >= AMBER_BAND_FRACTION * thresholdDays) {
		return {
			ragStatus: "amber",
			tipText: `Oldest ${terms.blocked} ${terms.workItem} has been blocked ${maxBlockedAgeDays} days, aging toward the ${thresholdDays}-day threshold. Keep it moving.`,
		};
	}

	return {
		ragStatus: "green",
		tipText: `Oldest ${terms.blocked} ${terms.workItem} has been blocked ${maxBlockedAgeDays} days, well within the ${thresholdDays}-day threshold.`,
	};
}
