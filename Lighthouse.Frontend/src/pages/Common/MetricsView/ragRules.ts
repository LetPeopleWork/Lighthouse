type RagResult = {
	readonly ragStatus: "red" | "amber" | "green";
	readonly tipText: string;
};

export type RagTerms = {
	readonly workItem: string;
	readonly workItems: string;
	readonly feature: string;
	readonly features: string;
	readonly cycleTime: string;
	readonly throughput: string;
	readonly wip: string;
	readonly workItemAge: string;
	readonly blocked: string;
	readonly sle: string;
};

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

function calculateSLEStats(
	sle: { percentile: number; value: number },
	inputNumbers: ReadonlyArray<number>,
): { totalItems: number; percentageWithinSLE: number } {
	const totalItems = inputNumbers.length;
	if (totalItems === 0) {
		return { totalItems: 0, percentageWithinSLE: 0 };
	}
	const itemsWithinSLE = inputNumbers.filter((v) => v <= sle.value).length;
	const percentageWithinSLE = (itemsWithinSLE / totalItems) * 100;
	return { totalItems, percentageWithinSLE };
}

export function computeCycleTimePercentilesRag(
	sle: { percentile: number; value: number } | null,
	cycleTimes: ReadonlyArray<number>,
	terms: RagTerms,
): RagResult {
	if (!sle) {
		return {
			ragStatus: "red",
			tipText: `Define a ${terms.sle} in settings based on historical ${terms.cycleTime} data.`,
		};
	}

	const { totalItems, percentageWithinSLE } = calculateSLEStats(
		sle,
		cycleTimes,
	);

	if (totalItems === 0) {
		return {
			ragStatus: "red",
			tipText: `Define a ${terms.sle} in settings based on historical ${terms.cycleTime} data.`,
		};
	}

	if (percentageWithinSLE >= sle.percentile) {
		return {
			ragStatus: "green",
			tipText: `${percentageWithinSLE.toFixed(1)}% of ${terms.workItems} are within the ${terms.sle} target of ${sle.percentile}% @ ${sle.value} days. Consider tightening the target.`,
		};
	}

	const difference = sle.percentile - percentageWithinSLE;

	if (difference > 20) {
		return {
			ragStatus: "red",
			tipText: `Only ${percentageWithinSLE.toFixed(1)}% within the ${terms.sle} target (${sle.percentile}% @ ${sle.value} days) — ${difference.toFixed(1)}pp below. Focus on the oldest items.`,
		};
	}

	return {
		ragStatus: "amber",
		tipText: `${percentageWithinSLE.toFixed(1)}% within the ${terms.sle} target (${sle.percentile}% @ ${sle.value} days) — ${difference.toFixed(1)}pp below. Focus on items approaching the limit.`,
	};
}

export function computeStartedVsClosedRag(
	startedTotal: number,
	closedTotal: number,
	systemWipLimit: number | undefined,
	terms: RagTerms,
): RagResult {
	if (!systemWipLimit || systemWipLimit <= 0) {
		return {
			ragStatus: "red",
			tipText: `Define System ${terms.wip} Limit in settings.`,
		};
	}

	if (startedTotal === 0 && closedTotal === 0) {
		return {
			ragStatus: "green",
			tipText: `Started and closed are both 0. Flow is balanced.`,
		};
	}

	const absDiff = Math.abs(startedTotal - closedTotal);
	if (absDiff < 2) {
		return {
			ragStatus: "green",
			tipText: `Started (${startedTotal}) and closed (${closedTotal}) are balanced.`,
		};
	}

	const larger = Math.max(startedTotal, closedTotal);
	const difference = (absDiff / larger) * 100;

	if (difference <= 5) {
		return {
			ragStatus: "green",
			tipText: `Started (${startedTotal}) and closed (${closedTotal}) are within 5% of each other.`,
		};
	}

	if (startedTotal > closedTotal) {
		return {
			ragStatus: "red",
			tipText: `Started (${startedTotal}) exceeds closed (${closedTotal}) by ${difference.toFixed(1)}%. Focus on finishing in-progress ${terms.workItems}.`,
		};
	}

	return {
		ragStatus: "amber",
		tipText: `Closed (${closedTotal}) significantly exceeds started (${startedTotal}). The process may be starving.`,
	};
}

export function computeTotalWorkItemAgeRag(
	totalAge: number,
	currentWip: number,
	systemWipLimit: number | undefined,
	sleDays: number | undefined,
	terms: RagTerms,
): RagResult {
	if (!systemWipLimit || systemWipLimit <= 0 || !sleDays || sleDays <= 0) {
		return {
			ragStatus: "red",
			tipText: `Define System ${terms.wip} Limit and ${terms.sle} to enable ${terms.workItemAge} analysis.`,
		};
	}

	const referenceValue = systemWipLimit * sleDays;

	if (totalAge > referenceValue) {
		return {
			ragStatus: "red",
			tipText: `Total ${terms.workItemAge} is ${totalAge} days, exceeding the reference value of ${referenceValue} (${terms.wip} limit ${systemWipLimit} × ${terms.sle} ${sleDays} days).`,
		};
	}

	const tomorrowProjection = totalAge + currentWip;
	if (tomorrowProjection > referenceValue) {
		return {
			ragStatus: "amber",
			tipText: `Total ${terms.workItemAge} is ${totalAge}. Tomorrow's projection (${tomorrowProjection}) would exceed the reference value of ${referenceValue}.`,
		};
	}

	return {
		ragStatus: "green",
		tipText: `Total ${terms.workItemAge} is ${totalAge}, within the reference value of ${referenceValue} (${terms.wip} limit ${systemWipLimit} × ${terms.sle} ${sleDays} days).`,
	};
}

export function computeThroughputRag(
	periodValues: ReadonlyArray<number>,
	blackoutDayIndices: ReadonlyArray<number>,
	terms: RagTerms,
): RagResult {
	if (periodValues.length === 0) {
		return {
			ragStatus: "green",
			tipText: `Stable, predictable ${terms.throughput}.`,
		};
	}

	const blackoutSet = new Set(blackoutDayIndices);
	let zeroRuns = 0;

	for (let i = 0; i <= periodValues.length - 3; i++) {
		const window = [i, i + 1, i + 2];
		const allZero = window.every(
			(idx) => !blackoutSet.has(idx) && periodValues[idx] === 0,
		);
		if (allZero) {
			zeroRuns++;
		}
	}

	if (zeroRuns >= 2) {
		return {
			ragStatus: "red",
			tipText: `${zeroRuns} runs of 3+ consecutive zero-${terms.throughput} days detected. Check for blockers or excessive ${terms.wip}.`,
		};
	}
	if (zeroRuns === 1) {
		return {
			ragStatus: "amber",
			tipText: `1 run of 3 consecutive zero-${terms.throughput} days detected. Inspect the PBC to understand what happened.`,
		};
	}
	return {
		ragStatus: "green",
		tipText: `No extended zero-${terms.throughput} runs detected. ${terms.throughput} is stable.`,
	};
}

export function computeCycleTimeScatterplotRag(
	sle: { percentile: number; value: number } | null,
	cycleTimes: ReadonlyArray<number>,
	terms: RagTerms,
): RagResult {
	if (!sle) {
		return {
			ragStatus: "red",
			tipText: `Define a ${terms.sle} based on historical ${terms.cycleTime} data.`,
		};
	}

	if (cycleTimes.length === 0) {
		return {
			ragStatus: "green",
			tipText: `No completed ${terms.workItems} to analyze. ${terms.cycleTime} behavior looks healthy.`,
		};
	}

	const aboveCount = cycleTimes.filter((ct) => ct > sle.value).length;
	const abovePercent = (aboveCount / cycleTimes.length) * 100;
	const allowedAbove = 100 - sle.percentile;
	const redThreshold = allowedAbove + 10;

	if (abovePercent >= redThreshold) {
		return {
			ragStatus: "red",
			tipText: `${abovePercent.toFixed(1)}% of ${terms.workItems} exceed the ${terms.sle} of ${sle.value} days (allowed: ${allowedAbove}%, threshold: ${redThreshold}%). Analyze the oldest items.`,
		};
	}
	if (abovePercent > allowedAbove) {
		return {
			ragStatus: "amber",
			tipText: `${abovePercent.toFixed(1)}% of ${terms.workItems} exceed the ${terms.sle} of ${sle.value} days (allowed: ${allowedAbove}%). Focus on the oldest items first.`,
		};
	}
	return {
		ragStatus: "green",
		tipText: `${abovePercent.toFixed(1)}% of ${terms.workItems} exceed the ${terms.sle} of ${sle.value} days, within the allowed ${allowedAbove}%.`,
	};
}

export function computeWorkItemAgeChartRag(
	sle: { percentile: number; value: number } | null,
	hasBlockedConfig: boolean,
	items: ReadonlyArray<{ workItemAge: number; isBlocked: boolean }>,
	terms: RagTerms,
): RagResult {
	if (!sle) {
		return {
			ragStatus: "red",
			tipText: `Define a ${terms.sle} in settings to enable ${terms.workItemAge} analysis.`,
		};
	}
	if (!hasBlockedConfig) {
		return {
			ragStatus: "red",
			tipText: `Define ${terms.blocked} indicators in settings to track blocked ${terms.workItems}.`,
		};
	}

	if (items.length === 0) {
		return {
			ragStatus: "green",
			tipText: `No ${terms.workItems} in progress. All ${terms.workItemAge}s are within healthy ranges.`,
		};
	}

	const aboveSle = items.filter((i) => i.workItemAge > sle.value);
	const abovePercent = (aboveSle.length / items.length) * 100;
	const allowedAbove = 100 - sle.percentile;
	const hasBlocked = items.some((i) => i.isBlocked);
	const anyAbove = aboveSle.length > 0;

	if (abovePercent > allowedAbove || (anyAbove && hasBlocked)) {
		return {
			ragStatus: "red",
			tipText: `${aboveSle.length} ${terms.workItem}(s) exceed the ${terms.sle} of ${sle.value} days (${abovePercent.toFixed(1)}%, allowed: ${allowedAbove}%)${hasBlocked ? `, and some are ${terms.blocked}` : ""}. Resolve immediately.`,
		};
	}
	if (anyAbove || hasBlocked) {
		return {
			ragStatus: "amber",
			tipText: `${aboveSle.length} ${terms.workItem}(s) exceed the ${terms.sle} of ${sle.value} days${hasBlocked ? `, and some are ${terms.blocked}` : ""}. Monitor closely.`,
		};
	}
	return {
		ragStatus: "green",
		tipText: `All ${terms.workItems} are within the ${terms.sle} of ${sle.value} days.`,
	};
}

export function computeWipOverTimeRag(
	wipValues: ReadonlyArray<number>,
	systemWipLimit: number | undefined,
	terms: RagTerms,
): RagResult {
	if (!systemWipLimit || systemWipLimit <= 0) {
		return {
			ragStatus: "red",
			tipText: `Define System ${terms.wip} Limit in settings.`,
		};
	}

	if (wipValues.length === 0) {
		return {
			ragStatus: "green",
			tipText: `No ${terms.wip} data available. Operating within boundaries.`,
		};
	}

	let above = 0;
	let at = 0;
	let below = 0;

	for (const value of wipValues) {
		if (value > systemWipLimit) above++;
		else if (value === systemWipLimit) at++;
		else below++;
	}

	const total = wipValues.length;
	const atPercent = (at / total) * 100;

	if (atPercent > 50) {
		return {
			ragStatus: "green",
			tipText: `${atPercent.toFixed(1)}% of days at the ${terms.wip} limit of ${systemWipLimit}. Flow is consistent.`,
		};
	}

	if (above > at + below) {
		return {
			ragStatus: "red",
			tipText: `${terms.wip} exceeded the limit of ${systemWipLimit} on ${above} of ${total} days. Reduce work in progress.`,
		};
	}

	if (below > above + at) {
		return {
			ragStatus: "amber",
			tipText: `${terms.wip} was below the limit of ${systemWipLimit} on ${below} of ${total} days. Consider starting more work.`,
		};
	}

	return {
		ragStatus: "amber",
		tipText: `${terms.wip} distribution is uneven across the limit of ${systemWipLimit}. Aim for consistency.`,
	};
}

export function computeTotalWorkItemAgeOverTimeRag(
	startValue: number,
	endValue: number,
	terms: RagTerms,
): RagResult {
	if (startValue === 0 && endValue === 0) {
		return {
			ragStatus: "green",
			tipText: `Total ${terms.workItemAge} is 0 throughout the period.`,
		};
	}

	if (startValue === 0 && endValue > 0) {
		return {
			ragStatus: "red",
			tipText: `Total ${terms.workItemAge} grew from 0 to ${endValue}. Investigate root causes.`,
		};
	}

	const change = (endValue - startValue) / startValue;
	const changePct = (change * 100).toFixed(1);

	if (change > 0.1) {
		return {
			ragStatus: "red",
			tipText: `Total ${terms.workItemAge} grew by ${changePct}% (${startValue} → ${endValue}). Investigate root causes.`,
		};
	}

	if (change < -0.1) {
		return {
			ragStatus: "amber",
			tipText: `Total ${terms.workItemAge} dropped by ${Math.abs(Number(changePct))}% (${startValue} → ${endValue}). Verify items are still in progress.`,
		};
	}

	return {
		ragStatus: "green",
		tipText: `Total ${terms.workItemAge} is stable at ${endValue} days (${changePct}% change).`,
	};
}

export function computeSimplifiedCfdRag(
	startedTotal: number,
	closedTotal: number,
	systemWipLimit: number | undefined,
	terms: RagTerms,
): RagResult {
	return computeStartedVsClosedRag(
		startedTotal,
		closedTotal,
		systemWipLimit,
		terms,
	);
}

export function computeWorkDistributionRag(
	unlinkedCount: number,
	totalCount: number,
	featureWip: number | undefined,
	distributionRate: number,
	terms: RagTerms,
): RagResult {
	if (totalCount === 0) {
		return {
			ragStatus: "green",
			tipText: `No ${terms.workItems} to evaluate. Distribution is healthy.`,
		};
	}

	const unlinkedPercent = (unlinkedCount / totalCount) * 100;
	if (unlinkedPercent >= 20) {
		return {
			ragStatus: "red",
			tipText: `${unlinkedPercent.toFixed(1)}% of ${terms.workItems} (${unlinkedCount} of ${totalCount}) are not linked to a ${terms.feature}. Link them to improve visibility.`,
		};
	}

	if (!featureWip || featureWip <= 0) {
		return {
			ragStatus: "red",
			tipText: `Define ${terms.feature} ${terms.wip} in settings.`,
		};
	}

	const overRatio = (distributionRate - featureWip) / featureWip;

	if (overRatio > 0.2) {
		return {
			ragStatus: "red",
			tipText: `Work is spread across ${distributionRate.toFixed(1)} ${terms.features}, exceeding the ${terms.feature} ${terms.wip} of ${featureWip} by more than 20%. Focus on fewer initiatives.`,
		};
	}

	if (overRatio > 0) {
		return {
			ragStatus: "amber",
			tipText: `Work is spread across ${distributionRate.toFixed(1)} ${terms.features}, slightly above the ${terms.feature} ${terms.wip} of ${featureWip}. Consider reducing scope.`,
		};
	}

	return {
		ragStatus: "green",
		tipText: `Work is spread across ${distributionRate.toFixed(1)} ${terms.features}, within the ${terms.feature} ${terms.wip} of ${featureWip}.`,
	};
}

export function computeFeatureSizeRag(
	featureSizeTarget: { percentile: number; value: number } | null,
	itemSizes: ReadonlyArray<number>,
	terms: RagTerms,
): RagResult {
	if (!featureSizeTarget) {
		return {
			ragStatus: "red",
			tipText: `Define a ${terms.feature} Size Target in settings.`,
		};
	}

	if (itemSizes.length === 0) {
		return {
			ragStatus: "green",
			tipText: `No ${terms.features} to evaluate. Sizes are within target range.`,
		};
	}

	const { percentageWithinSLE } = calculateSLEStats(
		featureSizeTarget,
		itemSizes,
	);
	const difference = featureSizeTarget.percentile - percentageWithinSLE;

	if (difference > 20) {
		return {
			ragStatus: "red",
			tipText: `Only ${percentageWithinSLE.toFixed(1)}% of ${terms.features} are within the size target of ${featureSizeTarget.value} (target: ${featureSizeTarget.percentile}%, gap: ${difference.toFixed(1)}pp). Break down large ${terms.features}.`,
		};
	}
	if (difference > 0) {
		return {
			ragStatus: "amber",
			tipText: `${percentageWithinSLE.toFixed(1)}% of ${terms.features} are within the size target of ${featureSizeTarget.value} (target: ${featureSizeTarget.percentile}%, gap: ${difference.toFixed(1)}pp). Monitor trends.`,
		};
	}
	return {
		ragStatus: "green",
		tipText: `${percentageWithinSLE.toFixed(1)}% of ${terms.features} are within the size target of ${featureSizeTarget.value}, meeting the ${featureSizeTarget.percentile}% target.`,
	};
}

export function computeEstimationVsCycleTimeRag(
	estimationStatus: string,
	dataPoints: ReadonlyArray<{ estimate: number; cycleTime: number }>,
	terms: RagTerms,
): RagResult {
	if (estimationStatus === "NotConfigured") {
		return {
			ragStatus: "red",
			tipText: `Configure estimation settings to enable correlation with ${terms.cycleTime}.`,
		};
	}

	if (dataPoints.length < 2) {
		return {
			ragStatus: "green",
			tipText: `Not enough data points to compute correlation yet.`,
		};
	}

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
			tipText: `Spearman correlation is ${rho.toFixed(2)} (≥ 0.6). Estimates correlate well with actual ${terms.cycleTime}.`,
		};
	}
	if (rho >= 0.3) {
		return {
			ragStatus: "amber",
			tipText: `Spearman correlation is ${rho.toFixed(2)} (0.3–0.6). Weak correlation between estimates and ${terms.cycleTime}. Review your estimation approach.`,
		};
	}
	return {
		ragStatus: "red",
		tipText: `Spearman correlation is ${rho.toFixed(2)} (< 0.3). No meaningful correlation between estimates and ${terms.cycleTime}.`,
	};
}

type PbcInput = {
	readonly status: string;
	readonly baselineConfigured: boolean;
	readonly dataPoints: ReadonlyArray<{ specialCauses: ReadonlyArray<string> }>;
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
			tipText:
				"Signal detected: Large-magnitude change in process behaviour. Investigate recent changes.",
		};
	}

	if (allCauses.has("ModerateChange")) {
		return {
			ragStatus: "amber",
			tipText:
				"Signal detected: Moderate change in process behaviour. Investigate before it escalates.",
		};
	}

	return {
		ragStatus: "green",
		tipText:
			"Process behaviour is within expected limits. No signals detected.",
	};
}
