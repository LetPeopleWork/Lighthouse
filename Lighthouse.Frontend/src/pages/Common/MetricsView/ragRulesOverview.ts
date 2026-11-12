import type { RagResult, RagTerms } from "./ragTypes";

export function computeWipOverviewRag(
	wipCount: number,
	systemWipLimit: number | undefined,
	terms: RagTerms,
): RagResult {
	if (!systemWipLimit || systemWipLimit <= 0) {
		return {
			ragStatus: "red",
			tipText: `Define System ${terms.wip} Limit in settings.`,
		};
	}
	if (wipCount > systemWipLimit) {
		return {
			ragStatus: "red",
			tipText: `${terms.wip} is ${wipCount}, exceeding the limit of ${systemWipLimit}. Close items to bring it down.`,
		};
	}
	if (wipCount < systemWipLimit) {
		return {
			ragStatus: "amber",
			tipText: `${terms.wip} is ${wipCount} of ${systemWipLimit}. Start more items to operate at full capacity.`,
		};
	}
	return {
		ragStatus: "green",
		tipText: `${terms.wip} matches the System ${terms.wip} Limit of ${systemWipLimit}.`,
	};
}

export function computeBlockedOverviewRag(
	blockedCount: number,
	hasBlockedConfig: boolean,
	terms: RagTerms,
): RagResult {
	if (!hasBlockedConfig) {
		return {
			ragStatus: "red",
			tipText: `Define ${terms.blocked} indicators in settings to track blocked work.`,
		};
	}
	if (blockedCount >= 2) {
		return {
			ragStatus: "red",
			tipText: `${blockedCount} ${terms.blocked} ${terms.workItems}. Focus on unblocking them before starting new work.`,
		};
	}
	if (blockedCount === 1) {
		return {
			ragStatus: "amber",
			tipText: `1 ${terms.blocked} ${terms.workItem}. Do not ignore it.`,
		};
	}
	return {
		ragStatus: "green",
		tipText: `No ${terms.blocked} ${terms.workItems}.`,
	};
}

export function computeStaleOverviewRag(
	staleCount: number,
	hasStaleConfig: boolean,
	terms: RagTerms,
): RagResult {
	if (!hasStaleConfig) {
		return {
			ragStatus: "red",
			tipText: "Define a staleness threshold in settings to track stale work.",
		};
	}
	if (staleCount >= 2) {
		return {
			ragStatus: "red",
			tipText: `${staleCount} stale ${terms.workItems}. Focus on unsticking them before starting new work.`,
		};
	}
	if (staleCount === 1) {
		return {
			ragStatus: "amber",
			tipText: `1 stale ${terms.workItem}. Do not ignore it.`,
		};
	}
	return {
		ragStatus: "green",
		tipText: `No stale ${terms.workItems}.`,
	};
}

export function computeFeaturesWorkedOnRag(
	featureCount: number,
	featureWip: number | undefined,
	terms: RagTerms,
): RagResult {
	if (!featureWip || featureWip <= 0) {
		return {
			ragStatus: "red",
			tipText: `Define ${terms.feature} ${terms.wip} in settings.`,
		};
	}
	if (featureCount > featureWip) {
		return {
			ragStatus: "red",
			tipText: `Working on ${featureCount} ${terms.features}, exceeding the limit of ${featureWip}. Focus to finish faster.`,
		};
	}
	if (featureCount < featureWip) {
		return {
			ragStatus: "amber",
			tipText: `Working on ${featureCount} of ${featureWip} ${terms.features}. Consider starting another.`,
		};
	}
	return {
		ragStatus: "green",
		tipText: `Working on ${featureCount} ${terms.features}, matching the ${terms.feature} ${terms.wip} limit.`,
	};
}

export function computePredictabilityScoreRag(
	score: number | null,
	terms: RagTerms,
): RagResult | undefined {
	if (score === null) {
		return undefined;
	}
	const pct = (score * 100).toFixed(1);
	if (score < 0.4) {
		return {
			ragStatus: "red",
			tipText: `Predictability score is ${pct}% (below 40%). ${terms.throughput} is highly variable; forecasts will be unreliable.`,
		};
	}
	if (score <= 0.6) {
		return {
			ragStatus: "amber",
			tipText: `Predictability score is ${pct}% (between 40–60%). Analyze bulk closings to improve stability.`,
		};
	}
	return {
		ragStatus: "green",
		tipText: `Predictability score is ${pct}% (above 60%). Forecasts are trustworthy.`,
	};
}

const FLOW_EFFICIENCY_AMBER_PERCENT = 40;
const FLOW_EFFICIENCY_GREEN_PERCENT = 60;

export function computeFlowEfficiencyRag(
	efficiencyPercent: number,
	_terms: RagTerms,
): RagResult {
	const rounded = efficiencyPercent.toFixed(0);

	if (efficiencyPercent < FLOW_EFFICIENCY_AMBER_PERCENT) {
		return {
			ragStatus: "red",
			tipText: `Flow efficiency is ${rounded}% (below 40%). Act on the wait states draining your value-adding time.`,
		};
	}

	if (efficiencyPercent < FLOW_EFFICIENCY_GREEN_PERCENT) {
		return {
			ragStatus: "amber",
			tipText: `Flow efficiency is ${rounded}% (40–60%). Observe which wait states hold work the longest.`,
		};
	}

	return {
		ragStatus: "green",
		tipText: `Flow efficiency is ${rounded}% (at or above 60%). Sustain the balance between active and waiting time.`,
	};
}
