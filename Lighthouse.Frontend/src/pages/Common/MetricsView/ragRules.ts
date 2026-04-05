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

// -----------------------------------------------------------------------
// M4 — Aging and Flow Stability
// -----------------------------------------------------------------------

export function computeWorkItemAgeChartRag(
	sle: { percentile: number; value: number } | null,
	hasBlockedConfig: boolean,
	items: ReadonlyArray<{ workItemAge: number; isBlocked: boolean }>,
): RagResult {
	if (!sle) {
		return { ragStatus: "red", tipText: "Define SLE in settings." };
	}
	if (!hasBlockedConfig) {
		return {
			ragStatus: "red",
			tipText: "Define blocked indicators in settings.",
		};
	}

	const aboveSle = items.some((i) => i.workItemAge > sle.value);
	if (aboveSle) {
		return {
			ragStatus: "red",
			tipText: "Items exceed the SLE; focus on resolving them.",
		};
	}

	const threshold = sle.value * 0.85;
	const approachingSle = items.some((i) => i.workItemAge >= threshold);
	const hasBlocked = items.some((i) => i.isBlocked);

	if (approachingSle || hasBlocked) {
		return {
			ragStatus: "amber",
			tipText: "Items are nearing the SLE or are blocked.",
		};
	}

	return {
		ragStatus: "green",
		tipText: "All items are within healthy age ranges.",
	};
}

export function computeWipOverTimeRag(
	wipValues: ReadonlyArray<number>,
	systemWipLimit: number | undefined,
): RagResult {
	if (!systemWipLimit || systemWipLimit <= 0) {
		return { ragStatus: "red", tipText: "Define System WIP Limit." };
	}

	if (wipValues.length === 0) {
		return {
			ragStatus: "green",
			tipText: "Operating within WIP boundaries.",
		};
	}

	let above = 0;
	let at = 0;
	let below = 0;

	for (const value of wipValues) {
		if (value > systemWipLimit) {
			above++;
		} else if (value === systemWipLimit) {
			at++;
		} else {
			below++;
		}
	}

	const total = wipValues.length;
	const atPercent = (at / total) * 100;

	if (atPercent > 50) {
		return {
			ragStatus: "green",
			tipText: "Operating within WIP boundaries.",
		};
	}

	if (above > at + below && above !== at + below) {
		return {
			ragStatus: "red",
			tipText: "WIP frequently exceeds the limit; reduce work in progress.",
		};
	}

	if (below > above + at) {
		return {
			ragStatus: "amber",
			tipText: "WIP is often below capacity; consider starting more work.",
		};
	}

	return {
		ragStatus: "amber",
		tipText: "WIP distribution is uneven; aim for consistency.",
	};
}

export function computeTotalWorkItemAgeOverTimeRag(
	startValue: number,
	endValue: number,
): RagResult {
	if (startValue === 0 && endValue === 0) {
		return {
			ragStatus: "green",
			tipText: "Total age is stable over the period.",
		};
	}

	if (startValue === 0 && endValue > 0) {
		return {
			ragStatus: "red",
			tipText: "Total age is growing; investigate root causes.",
		};
	}

	const change = (endValue - startValue) / startValue;

	if (change > 0.1) {
		return {
			ragStatus: "red",
			tipText: "Total age is growing; investigate root causes.",
		};
	}

	if (change < -0.1) {
		return {
			ragStatus: "amber",
			tipText:
				"Total age is decreasing significantly; verify items are still in progress.",
		};
	}

	return {
		ragStatus: "green",
		tipText: "Total age is stable over the period.",
	};
}

export function computeSimplifiedCfdRag(
	startedTotal: number,
	closedTotal: number,
	systemWipLimit: number | undefined,
): RagResult {
	return computeStartedVsClosedRag(startedTotal, closedTotal, systemWipLimit);
}

// -----------------------------------------------------------------------
// M5 — Portfolio and Correlation
// -----------------------------------------------------------------------

export function computeWorkDistributionRag(
	unlinkedCount: number,
	totalCount: number,
	featureWip: number | undefined,
	distributionRate: number,
): RagResult {
	if (totalCount === 0) {
		return {
			ragStatus: "green",
			tipText: "Distribution is healthy.",
		};
	}

	const unlinkedPercent = (unlinkedCount / totalCount) * 100;
	if (unlinkedPercent >= 20) {
		return {
			ragStatus: "red",
			tipText: "Too many items not linked to a feature; link work items.",
		};
	}

	if (!featureWip || featureWip <= 0) {
		return {
			ragStatus: "red",
			tipText: "Define Feature WIP in settings.",
		};
	}

	const overRatio =
		featureWip > 0 ? (distributionRate - featureWip) / featureWip : 0;

	if (overRatio > 0.2) {
		return {
			ragStatus: "red",
			tipText: "Spread too thin across features; focus on fewer initiatives.",
		};
	}

	if (overRatio > 0) {
		return {
			ragStatus: "amber",
			tipText: "Slightly above Feature WIP; consider reducing scope.",
		};
	}

	return {
		ragStatus: "green",
		tipText: "Distribution is healthy.",
	};
}

export function computeFeatureSizeRag(
	featureSizeTarget: { percentile: number; value: number } | null,
	percentileValues: ReadonlyArray<{ percentile: number; value: number }>,
): RagResult {
	if (!featureSizeTarget) {
		return {
			ragStatus: "red",
			tipText: "Define Feature Size Target in settings.",
		};
	}

	const matching = percentileValues.find(
		(p) => p.percentile === featureSizeTarget.percentile,
	);
	if (!matching) {
		return {
			ragStatus: "green",
			tipText: "Feature sizes are within target range.",
		};
	}

	const deviation =
		featureSizeTarget.value > 0
			? (matching.value - featureSizeTarget.value) / featureSizeTarget.value
			: 0;

	if (deviation > 0.15) {
		return {
			ragStatus: "red",
			tipText:
				"Feature sizes significantly exceed target; break down features.",
		};
	}
	if (deviation > 0) {
		return {
			ragStatus: "amber",
			tipText: "Feature sizes slightly above target; monitor trends.",
		};
	}
	return {
		ragStatus: "green",
		tipText: "Feature sizes are within target range.",
	};
}

export function computeEstimationVsCycleTimeRag(
	estimationStatus: string,
	dataPoints: ReadonlyArray<{ estimate: number; cycleTime: number }>,
): RagResult {
	if (estimationStatus !== "Configured") {
		return {
			ragStatus: "red",
			tipText: "Configure estimation settings to enable correlation.",
		};
	}

	if (dataPoints.length < 2) {
		return {
			ragStatus: "green",
			tipText: "Estimates correlate with actual cycle times.",
		};
	}

	// Spearman rank correlation coefficient
	const ranked = (arr: ReadonlyArray<number>): number[] => {
		const sorted = [...arr].map((v, i) => ({ v, i })).sort((a, b) => a.v - b.v);
		const ranks = new Array<number>(arr.length);
		for (let i = 0; i < sorted.length; i++) {
			ranks[sorted[i].i] = i + 1;
		}
		return ranks;
	};

	const estimates = dataPoints.map((d) => d.estimate);
	const cycleTimes = dataPoints.map((d) => d.cycleTime);
	const rankE = ranked(estimates);
	const rankC = ranked(cycleTimes);

	const n = dataPoints.length;
	let sumDSq = 0;
	for (let i = 0; i < n; i++) {
		const d = rankE[i] - rankC[i];
		sumDSq += d * d;
	}

	const rho = 1 - (6 * sumDSq) / (n * (n * n - 1));

	if (rho >= 0.6) {
		return {
			ragStatus: "green",
			tipText: "Estimates correlate with actual cycle times.",
		};
	}
	if (rho >= 0.3) {
		return {
			ragStatus: "amber",
			tipText:
				"Weak correlation between estimates and cycle times; review estimation approach.",
		};
	}
	return {
		ragStatus: "red",
		tipText: "No meaningful correlation; estimates do not predict cycle time.",
	};
}

// -----------------------------------------------------------------------
// M6 — PBC Widget Family
// -----------------------------------------------------------------------

type PbcInput = {
	readonly status: string;
	readonly baselineConfigured: boolean;
	readonly dataPoints: ReadonlyArray<{
		specialCauses: ReadonlyArray<string>;
	}>;
};

export function computePbcRag(data: PbcInput): RagResult {
	if (
		!data.baselineConfigured ||
		data.status === "BaselineMissing" ||
		data.status === "BaselineInvalid" ||
		data.status === "InsufficientData"
	) {
		return {
			ragStatus: "red",
			tipText: "Configure a baseline period to enable PBC analysis.",
		};
	}

	const allCauses = new Set(data.dataPoints.flatMap((dp) => dp.specialCauses));

	if (allCauses.has("LargeChange")) {
		return {
			ragStatus: "red",
			tipText: "Signal detected: Large-magnitude change in process behaviour.",
		};
	}

	if (allCauses.has("ModerateChange")) {
		return {
			ragStatus: "amber",
			tipText:
				"Signal detected: Moderate change in process behaviour; investigate.",
		};
	}

	return {
		ragStatus: "green",
		tipText: "Process behaviour is within expected limits.",
	};
}
