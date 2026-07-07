import { getAgeInDaysFromStart } from "../date/age";

export type StalenessCandidate = {
	currentStateEnteredAt?: Date | null;
	isBlocked: boolean;
	blockedSince?: Date | string | null;
	currentStateName?: string;
};

export type StalenessReason =
	| { kind: "time-in-state"; days: number; stateName: string }
	| { kind: "blocked-duration"; days: number }
	| { kind: "context-time-in-state"; days: number; stateName: string };

export type StalenessResult = {
	isStale: boolean;
	reasons: StalenessReason[];
};

/**
 * Compute days between blockedSince and now using UTC date-only arithmetic.
 * Mirrors `daysInState` (TimeInStateBadge) but operates on blockedSince.
 */
const blockedDays = (
	blockedSince: Date | string | null | undefined,
	now: Date,
): number => {
	if (blockedSince === null || blockedSince === undefined) return 0;
	return getAgeInDaysFromStart(blockedSince, now);
};

/**
 * Derive staleness from two independent triggers:
 * - time-in-state DRIVER: daysInState > stalenessThresholdDays, guarded by !isBlocked
 * - blocked-duration DRIVER: blockedDays ≥ blockedStalenessThresholdDays
 * - context-time-in-state (UC-1): emitted when blocked item exceeds stalenessThresholdDays
 * - isStale = reasons.some(r => r.kind !== "context-time-in-state")
 */
export const deriveStaleness = (
	item: StalenessCandidate,
	stalenessThresholdDays: number | undefined,
	blockedStalenessThresholdDays: number | undefined,
	now: Date = new Date(),
): StalenessResult => {
	const reasons: StalenessReason[] = [];

	const stalenessThreshold =
		stalenessThresholdDays !== undefined && stalenessThresholdDays > 0
			? stalenessThresholdDays
			: 0;

	const blockedThreshold =
		blockedStalenessThresholdDays !== undefined &&
		blockedStalenessThresholdDays > 0
			? blockedStalenessThresholdDays
			: 0;

	const hasEnteredDate =
		item.currentStateEnteredAt !== null &&
		item.currentStateEnteredAt !== undefined;
	const days =
		hasEnteredDate && item.currentStateEnteredAt
			? getAgeInDaysFromStart(item.currentStateEnteredAt, now)
			: 0;

	// time-in-state DRIVER: ADR-026 preserved — !isBlocked guard
	if (stalenessThreshold > 0 && !item.isBlocked && days > stalenessThreshold) {
		reasons.push({
			kind: "time-in-state",
			days,
			stateName: item.currentStateName ?? "",
		});
	}

	// blocked-duration DRIVER: ≥ boundary (OQ1)
	if (blockedThreshold > 0 && item.isBlocked) {
		const bDays = blockedDays(item.blockedSince, now);
		if (bDays >= blockedThreshold) {
			reasons.push({ kind: "blocked-duration", days: bDays });
		}
	}

	// context-time-in-state (UC-1): blocked item exceeding stalenessThresholdDays
	if (
		stalenessThreshold > 0 &&
		item.isBlocked &&
		days > stalenessThreshold &&
		hasEnteredDate
	) {
		reasons.push({
			kind: "context-time-in-state",
			days,
			stateName: item.currentStateName ?? "",
		});
	}

	return {
		isStale: reasons.some((r) => r.kind !== "context-time-in-state"),
		reasons,
	};
};
