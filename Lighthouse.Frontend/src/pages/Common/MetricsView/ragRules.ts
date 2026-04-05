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

export function computeCycleTimePercentilesRag(
	sle: { percentile: number; value: number } | null,
	percentileValues: ReadonlyArray<{ percentile: number; value: number }>,
): RagResult {
	if (!sle) {
		return {
			ragStatus: "red",
			tipText: "Define SLE in settings based on historical data.",
		};
	}

	const matching = percentileValues.find(
		(p) => p.percentile === sle.percentile,
	);
	if (!matching) {
		return {
			ragStatus: "green",
			tipText: "Consider lowering the SLE target.",
		};
	}

	const deviation =
		sle.value > 0 ? (matching.value - sle.value) / sle.value : 0;

	if (deviation > 0.15) {
		return {
			ragStatus: "red",
			tipText: "Look at oldest items in progress.",
		};
	}
	if (deviation > 0) {
		return {
			ragStatus: "amber",
			tipText: "Focus on items approaching the SLE.",
		};
	}
	return {
		ragStatus: "green",
		tipText: "Consider lowering the SLE target.",
	};
}

export function computeStartedVsClosedRag(
	startedTotal: number,
	closedTotal: number,
	systemWipLimit: number | undefined,
): RagResult {
	if (!systemWipLimit || systemWipLimit <= 0) {
		return { ragStatus: "red", tipText: "Define System WIP Limit." };
	}

	if (startedTotal === 0 && closedTotal === 0) {
		return {
			ragStatus: "green",
			tipText: "You close as much as you start.",
		};
	}

	const absDiff = Math.abs(startedTotal - closedTotal);
	if (absDiff < 2) {
		return {
			ragStatus: "green",
			tipText: "You close as much as you start.",
		};
	}

	const larger = Math.max(startedTotal, closedTotal);
	const difference = (absDiff / larger) * 100;

	if (difference <= 5) {
		return {
			ragStatus: "green",
			tipText: "You close as much as you start.",
		};
	}

	if (startedTotal > closedTotal) {
		return {
			ragStatus: "red",
			tipText: "You start more than you finish; focus on in-progress work.",
		};
	}

	return {
		ragStatus: "amber",
		tipText: "Process may be starving; keep an eye on it.",
	};
}

export function computeTotalWorkItemAgeRag(
	totalAge: number,
	currentWip: number,
	systemWipLimit: number | undefined,
	sleDays: number | undefined,
): RagResult {
	if (!systemWipLimit || systemWipLimit <= 0 || !sleDays || sleDays <= 0) {
		return {
			ragStatus: "red",
			tipText: "Define System WIP Limit and SLE.",
		};
	}

	const referenceValue = systemWipLimit * sleDays;

	if (totalAge > referenceValue) {
		return {
			ragStatus: "red",
			tipText: "Total age exceeds reference value.",
		};
	}

	const tomorrowProjection = totalAge + currentWip;
	if (tomorrowProjection > referenceValue) {
		return {
			ragStatus: "amber",
			tipText: "Approaching reference value tomorrow.",
		};
	}

	return {
		ragStatus: "green",
		tipText: "Total age is within healthy range.",
	};
}

export function computeThroughputRag(
	periodValues: ReadonlyArray<number>,
	blackoutDayIndices: ReadonlyArray<number>,
): RagResult {
	if (periodValues.length === 0) {
		return { ragStatus: "green", tipText: "Stable, predictable delivery." };
	}

	const blackoutSet = new Set(blackoutDayIndices);
	let maxConsecutiveZeros = 0;
	let currentRun = 0;

	for (let i = 0; i < periodValues.length; i++) {
		if (blackoutSet.has(i)) {
			currentRun = 0;
			continue;
		}
		if (periodValues[i] === 0) {
			currentRun++;
			maxConsecutiveZeros = Math.max(maxConsecutiveZeros, currentRun);
		} else {
			currentRun = 0;
		}
	}

	if (maxConsecutiveZeros >= 2) {
		return {
			ragStatus: "red",
			tipText: "Nothing is finishing; check for blockers or excessive WIP.",
		};
	}
	if (maxConsecutiveZeros === 1) {
		return {
			ragStatus: "amber",
			tipText: "Analyze what happened — inspect the PBC.",
		};
	}
	return { ragStatus: "green", tipText: "Stable, predictable delivery." };
}

export function computeCycleTimeScatterplotRag(
	sle: { percentile: number; value: number } | null,
	cycleTimes: ReadonlyArray<number>,
): RagResult {
	if (!sle) {
		return {
			ragStatus: "red",
			tipText: "Define a Service Level Expectation based on historical data.",
		};
	}

	if (cycleTimes.length === 0) {
		return {
			ragStatus: "green",
			tipText: "Healthy cycle time behavior.",
		};
	}

	const aboveCount = cycleTimes.filter((ct) => ct > sle.value).length;
	const abovePercent = (aboveCount / cycleTimes.length) * 100;

	// SLE says X% should be within Y days → allowed above = (100-X)%
	const allowedAbove = 100 - sle.percentile;
	const redThreshold = allowedAbove + 10;

	if (abovePercent >= redThreshold) {
		return {
			ragStatus: "red",
			tipText:
				"Consider whether your SLE is realistic. Analyze the oldest items.",
		};
	}
	if (abovePercent > allowedAbove) {
		return {
			ragStatus: "amber",
			tipText: "Focus on the oldest item first, and actively unblock items.",
		};
	}
	return {
		ragStatus: "green",
		tipText: "Healthy cycle time behavior.",
	};
}
