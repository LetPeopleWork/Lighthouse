type RagResult = {
	readonly ragStatus: "red" | "amber" | "green";
	readonly tipText: string;
};

export function computeWipOverviewRag(
	wipCount: number,
	systemWipLimit: number | undefined,
): RagResult {
	if (!systemWipLimit || systemWipLimit <= 0) {
		return { ragStatus: "red", tipText: "Define System WIP Limit." };
	}
	if (wipCount > systemWipLimit) {
		return { ragStatus: "red", tipText: "Close items to bring WIP down." };
	}
	if (wipCount < systemWipLimit) {
		return {
			ragStatus: "amber",
			tipText: "Start more items to operate at best capacity.",
		};
	}
	return { ragStatus: "green", tipText: "You match your System WIP Limit." };
}

export function computeBlockedOverviewRag(
	blockedCount: number,
	hasBlockedConfig: boolean,
): RagResult {
	if (!hasBlockedConfig) {
		return {
			ragStatus: "red",
			tipText: "Define blocked indicators in settings.",
		};
	}
	if (blockedCount >= 2) {
		return {
			ragStatus: "red",
			tipText: "Focus on unblocking blocked work.",
		};
	}
	if (blockedCount === 1) {
		return { ragStatus: "amber", tipText: "Do not ignore blocked items." };
	}
	return { ragStatus: "green", tipText: "No blockers." };
}

export function computeFeaturesWorkedOnRag(
	featureCount: number,
	featureWip: number | undefined,
): RagResult {
	if (!featureWip || featureWip <= 0) {
		return { ragStatus: "red", tipText: "Define Feature WIP in settings." };
	}
	if (featureCount > featureWip) {
		return {
			ragStatus: "red",
			tipText: "Focus your work to get features done more quickly.",
		};
	}
	if (featureCount < featureWip) {
		return {
			ragStatus: "amber",
			tipText: "Consider starting work for another feature.",
		};
	}
	return { ragStatus: "green", tipText: "Working at capacity." };
}

export function computePredictabilityScoreRag(
	score: number | null,
): RagResult | undefined {
	if (score === null) {
		return undefined;
	}
	if (score < 0.4) {
		return {
			ragStatus: "red",
			tipText: "Throughput is highly variable; forecasts will be unreliable.",
		};
	}
	if (score <= 0.6) {
		return {
			ragStatus: "amber",
			tipText: "Moderate predictability; analyze bulk closings.",
		};
	}
	return {
		ragStatus: "green",
		tipText: "Process is reasonably stable; forecasts are trustworthy.",
	};
}
