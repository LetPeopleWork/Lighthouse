import type { RagResult, RagTerms } from "./ragTypes";

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

/**
 * How much of the board is at risk, judged against the allowance the team's own target implies.
 *
 * The allowance is derived rather than configured separately: a team promising 85% has accepted
 * that 15% will miss, and a team promising 70% has accepted 30%. Asking them to set a second
 * number would let the two disagree, and there is only one promise.
 */
export function computeSleRiskRag(
	atRiskCount: number,
	wipCount: number,
	sle: { percentile: number; value: number } | null,
	terms: RagTerms,
): RagResult {
	// Checked before the empty board, because a team with neither a target nor any work in progress
	// still needs the target. The missing setting outlives today's board.
	if (!sle) {
		return {
			ragStatus: "red",
			tipText: `Define a ${terms.sle} in settings to see which ${terms.workItems} are at risk of missing it.`,
		};
	}

	const allowedShare = (100 - sle.percentile) / 100;
	const allowedPercentText = `${100 - sle.percentile}%`;

	if (wipCount === 0) {
		return {
			ragStatus: "green",
			tipText: `No ${terms.workItems} in progress, so none are at risk.`,
		};
	}

	// Both sides stay fractions. Rounding the share to a whole percentage first would let 14.6%
	// read as 15 and trip a 15% allowance — the status would flip on how the number is written
	// rather than on the work.
	if (atRiskCount / wipCount >= allowedShare) {
		return {
			ragStatus: "red",
			tipText: `${atRiskCount} of ${wipCount} ${terms.workItems} are at risk, at or above the ${allowedPercentText} this ${terms.sle} allows. Focus on them before starting new work.`,
		};
	}

	if (atRiskCount >= 1) {
		return {
			ragStatus: "amber",
			tipText: `${atRiskCount} of ${wipCount} ${terms.workItems} are at risk, within the ${allowedPercentText} this ${terms.sle} allows. Keep an eye on them.`,
		};
	}

	return {
		ragStatus: "green",
		tipText: `No ${terms.workItems} are at risk of missing the ${terms.sle}.`,
	};
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
			tipText: `${abovePercent.toFixed(1)}% of ${terms.workItems} exceed the ${terms.sle} of ${sle.value} days (expected: ${allowedAbove}%, threshold: ${redThreshold}%). Analyze the oldest items.`,
		};
	}
	if (abovePercent > allowedAbove) {
		return {
			ragStatus: "amber",
			tipText: `${abovePercent.toFixed(1)}% of ${terms.workItems} exceed the ${terms.sle} of ${sle.value} days (expected: ${allowedAbove}%). Focus on the oldest items first.`,
		};
	}
	return {
		ragStatus: "green",
		tipText: `${abovePercent.toFixed(1)}% of ${terms.workItems} exceed the ${terms.sle} of ${sle.value} days, within the expected ${allowedAbove}%.`,
	};
}

export function computeWorkItemAgeChartRag(
	sle: { percentile: number; value: number } | null,
	hasBlockedConfig: boolean,
	items: ReadonlyArray<{
		workItemAge: number;
		isBlocked: boolean;
		isStale: boolean;
	}>,
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
	const hasStale = items.some((i) => i.isStale);
	const hasFlagged = hasBlocked || hasStale;
	const anyAbove = aboveSle.length > 0;

	const flaggedSuffix = hasFlagged
		? `, and some are ${terms.blocked} or stale`
		: "";

	if (abovePercent > allowedAbove || (anyAbove && hasFlagged)) {
		return {
			ragStatus: "red",
			tipText: `${aboveSle.length} ${terms.workItem}(s) exceed the ${terms.sle} of ${sle.value} days (${abovePercent.toFixed(1)}%, expected: ${allowedAbove}%)${flaggedSuffix}. Resolve immediately.`,
		};
	}
	if (anyAbove || hasFlagged) {
		return {
			ragStatus: "amber",
			tipText: `${aboveSle.length} ${terms.workItem}(s) exceed the ${terms.sle} of ${sle.value} days${flaggedSuffix}. Monitor closely.`,
		};
	}
	return {
		ragStatus: "green",
		tipText: `All ${terms.workItems} are within the ${terms.sle} of ${sle.value} days.`,
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

const CUMULATIVE_STATE_AMBER_SHARE = 0.4;
const CUMULATIVE_STATE_RED_SHARE = 0.6;

export function computeCumulativeStateTimeRag(
	states: ReadonlyArray<{ state: string; totalDays: number }>,
	terms: RagTerms,
): RagResult {
	const total = states.reduce((sum, state) => sum + state.totalDays, 0);

	if (states.length === 0 || total <= 0) {
		return {
			ragStatus: "red",
			tipText: `No ${terms.workItems} contribute time across states. Adjust the date range or filter to bring data into scope.`,
		};
	}

	const dominant = states.reduce((max, state) =>
		state.totalDays > max.totalDays ? state : max,
	);
	const dominantShare = dominant.totalDays / total;
	const dominantPercent = (dominantShare * 100).toFixed(1);

	if (dominantShare > CUMULATIVE_STATE_RED_SHARE) {
		return {
			ragStatus: "red",
			tipText: `${dominant.state} holds ${dominantPercent}% of total time across states (above 60%). Investigate this bottleneck.`,
		};
	}

	if (dominantShare >= CUMULATIVE_STATE_AMBER_SHARE) {
		return {
			ragStatus: "amber",
			tipText: `${dominant.state} holds ${dominantPercent}% of total time across states (40–60%). Watch this state for a forming bottleneck.`,
		};
	}

	return {
		ragStatus: "green",
		tipText: `Time is balanced across states; no state holds 40% or more of the total.`,
	};
}

/**
 * Age of the IN-PROGRESS population against the SLE, as of the last day of the selected range (D3).
 *
 * D6-REVISED (product-owner decision, 2026-07-19) — bands on ABSOLUTE COUNTS against the SLE's day
 * value, evaluated in this order:
 *
 *   no SLE configured                               → red (define-an-SLE tip)
 *   count(age > value) > 1                          → red   (Act)
 *   count(age > value) === 1 || count(age === value) → amber (Observe)
 *   otherwise — all ages below, or nothing in progress → green (Sustain)
 *
 * The original share-based design (band on the PERCENTAGE within the SLE, reusing
 * `computeCycleTimePercentilesRag`'s 20pp bands) was withdrawn when its human validation gate fired:
 * it read green on a period the coach had nominated as deteriorated, because a percentage over a
 * population of 1-4 items is noise. See `docs/feature/widget-loose-ends/feature-delta.md` →
 * "D6 gate RESULT and D6-REVISED".
 *
 * Three deliberate properties, confirmed by the product owner — do not "fix" them:
 *  1. The SLE percentile is discarded; only the day value is used. A probability is meaningless at
 *     the WIP sizes this widget sees.
 *  2. The counts do not scale with WIP. Two breaching items read red whether WIP is 3 or 40.
 *  3. An empty population is green, not a neutral state. Nothing in progress is not a bad state, and
 *     the WIP RAG already signals an empty board — this widget must not say it twice.
 *
 * DESIGN D19 (extract a band helper shared with `computeCycleTimePercentilesRag`) is WITHDRAWN with
 * the old design: that rule bands on a share against the percentile, this one on counts against the
 * day value. There is no longer one piece of knowledge to place in one location.
 */
export function computeWorkItemAgePercentilesRag(
	sle: { percentile: number; value: number } | null,
	workItemAges: ReadonlyArray<number>,
	terms: RagTerms,
): RagResult {
	if (!sle) {
		return {
			ragStatus: "red",
			tipText: `Define a ${terms.sle} in settings based on historical ${terms.cycleTime} data.`,
		};
	}

	const limit = sle.value;
	const olderThanLimit = workItemAges.filter((age) => age > limit).length;
	const atLimit = workItemAges.filter((age) => age === limit).length;

	if (olderThanLimit > 1) {
		return {
			ragStatus: "red",
			tipText: `${olderThanLimit} in-progress ${terms.workItems} are older than the ${terms.sle} of ${limit} days. Act on the oldest ones first.`,
		};
	}

	if (olderThanLimit === 1) {
		return {
			ragStatus: "amber",
			tipText: `1 in-progress ${terms.workItem} is older than the ${terms.sle} of ${limit} days. Observe it before a second one joins it.`,
		};
	}

	if (atLimit > 0) {
		return {
			ragStatus: "amber",
			tipText:
				atLimit === 1
					? `1 in-progress ${terms.workItem} sits exactly on the ${terms.sle} of ${limit} days. Observe it before the limit is passed.`
					: `${atLimit} in-progress ${terms.workItems} sit exactly on the ${terms.sle} of ${limit} days. Observe them before the limit is passed.`,
		};
	}

	if (workItemAges.length === 0) {
		return {
			ragStatus: "green",
			tipText:
				"Nothing is in progress in this range, so no work is ageing. Sustain the flow.",
		};
	}

	const inProgressCount = workItemAges.length;

	return {
		ragStatus: "green",
		tipText:
			inProgressCount === 1
				? `The in-progress ${terms.workItem} is younger than the ${terms.sle} of ${limit} days. Sustain the pace.`
				: `All ${inProgressCount} in-progress ${terms.workItems} are younger than the ${terms.sle} of ${limit} days. Sustain the pace.`,
	};
}
