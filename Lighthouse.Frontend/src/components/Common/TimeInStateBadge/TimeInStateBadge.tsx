import type React from "react";

type TimeInStateBadgeProps = {
	currentStateEnteredAt: Date | null;
	currentStateName: string;
	now?: Date;
};

const MILLISECONDS_PER_DAY = 1000 * 60 * 60 * 24;

export const wholeDaysInState = (
	currentStateEnteredAt: Date | null,
	now: Date = new Date(),
): number => {
	if (currentStateEnteredAt === null) {
		return 0;
	}

	const fromDateOnly = Date.UTC(
		currentStateEnteredAt.getUTCFullYear(),
		currentStateEnteredAt.getUTCMonth(),
		currentStateEnteredAt.getUTCDate(),
	);
	const toDateOnly = Date.UTC(
		now.getUTCFullYear(),
		now.getUTCMonth(),
		now.getUTCDate(),
	);

	return Math.max(
		0,
		Math.floor((toDateOnly - fromDateOnly) / MILLISECONDS_PER_DAY),
	);
};

const TimeInStateBadge: React.FC<TimeInStateBadgeProps> = ({
	currentStateEnteredAt,
	currentStateName,
	now = new Date(),
}) => {
	if (currentStateEnteredAt === null) {
		return <span>—</span>;
	}

	const days = wholeDaysInState(currentStateEnteredAt, now);

	return <span>{`${days}d in ${currentStateName}`}</span>;
};

export default TimeInStateBadge;
