import type { RagResult, RagTerms } from "./ragTypes";

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
	sizePercentileValues: ReadonlyArray<{ percentile: number; value: number }>,
	activeItemSizes: ReadonlyArray<number>,
	terms: RagTerms,
): RagResult {
	if (activeItemSizes.length === 0) {
		return {
			ragStatus: "green",
			tipText: `No active ${terms.features} to evaluate.`,
		};
	}

	const p85 = sizePercentileValues.find((p) => p.percentile === 85)?.value;
	const p70 = sizePercentileValues.find((p) => p.percentile === 70)?.value;

	if (p85 !== undefined && activeItemSizes.some((s) => s > p85)) {
		const count = activeItemSizes.filter((s) => s > p85).length;
		return {
			ragStatus: "red",
			tipText: `${count} ${terms.feature}(s) exceed the 85th percentile size of ${p85} items. These are bigger than most other ${terms.features} and still in progress — look to split or close as soon as possible.`,
		};
	}

	if (p70 !== undefined && activeItemSizes.some((s) => s > p70)) {
		const count = activeItemSizes.filter((s) => s > p70).length;
		return {
			ragStatus: "amber",
			tipText: `${count} ${terms.feature}(s) exceed the 70th percentile size of ${p70} items. They are approaching the upper limit — if they grow further, try to split and deliver in smaller slices.`,
		};
	}

	return {
		ragStatus: "green",
		tipText: `All active ${terms.features} are within the expected size range.`,
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

export function computeLoadBalanceMatrixRag(
	baselineAvailable: boolean,
	currentWip: number,
	currentTotalWorkItemAge: number,
	averageWip: number | null,
	averageTotalWorkItemAge: number | null,
	terms: RagTerms,
): RagResult {
	if (
		!baselineAvailable ||
		averageWip === null ||
		averageTotalWorkItemAge === null
	) {
		return {
			ragStatus: "red",
			tipText: "Configure a baseline period to enable load balance analysis.",
		};
	}

	if (currentTotalWorkItemAge > averageTotalWorkItemAge) {
		return {
			ragStatus: "red",
			tipText: `${terms.workItemAge} is above baseline (${currentTotalWorkItemAge.toFixed(0)} > ${averageTotalWorkItemAge.toFixed(0)}). Close ongoing work before starting new items.`,
		};
	}

	if (currentWip > averageWip) {
		return {
			ragStatus: "green",
			tipText: `${terms.wip} is above baseline (${currentWip.toFixed(0)} > ${averageWip.toFixed(0)}) while ${terms.workItemAge} is at or below baseline (${currentTotalWorkItemAge.toFixed(0)} <= ${averageTotalWorkItemAge.toFixed(0)}).`,
		};
	}

	return {
		ragStatus: "amber",
		tipText: `${terms.wip} is at or below baseline (${currentWip.toFixed(0)} <= ${averageWip.toFixed(0)}) while ${terms.workItemAge} is at or below baseline. Consider starting more work.`,
	};
}

export function computeArrivalsRunChartRag(
	arrivalsValues: ReadonlyArray<number>,
	blackoutDayIndices: ReadonlyArray<number>,
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

	// Primary signal: arrivals-vs-departures balance (reuse Started vs. Closed thresholds)
	const balanceResult = computeStartedVsClosedRag(
		startedTotal,
		closedTotal,
		systemWipLimit,
		terms,
	);

	if (balanceResult.ragStatus === "red") {
		return balanceResult;
	}

	// Secondary signal: batching/continuity of arrivals
	if (arrivalsValues.length > 0) {
		const blackoutSet = new Set(blackoutDayIndices);
		let zeroRuns = 0;

		for (let i = 0; i <= arrivalsValues.length - 3; i++) {
			const window = [i, i + 1, i + 2];
			const allZero = window.every(
				(idx) => !blackoutSet.has(idx) && arrivalsValues[idx] === 0,
			);
			if (allZero) {
				zeroRuns++;
			}
		}

		if (zeroRuns >= 2 && balanceResult.ragStatus === "green") {
			return {
				ragStatus: "amber",
				tipText: `Arrivals are balanced overall, but ${zeroRuns} runs of 3+ consecutive zero-arrival days suggest work is starting in batches rather than continuously.`,
			};
		}
	}

	return balanceResult;
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
