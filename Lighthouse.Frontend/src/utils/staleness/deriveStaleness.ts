import { daysInState } from "../../components/Common/TimeInStateBadge/TimeInStateBadge";

export type StalenessCandidate = {
	currentStateEnteredAt?: Date | null;
	isBlocked: boolean;
};

export const deriveStaleness = (
	item: StalenessCandidate,
	thresholdDays: number | undefined,
	now: Date = new Date(),
): boolean => {
	if (thresholdDays === undefined || thresholdDays <= 0) {
		return false;
	}
	if (item.isBlocked) {
		return false;
	}
	if (!item.currentStateEnteredAt) {
		return false;
	}

	return daysInState(item.currentStateEnteredAt, now) > thresholdDays;
};
