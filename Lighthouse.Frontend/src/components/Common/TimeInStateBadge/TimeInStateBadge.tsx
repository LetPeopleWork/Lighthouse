import type React from "react";

type TimeInStateBadgeProps = {
	currentStateEnteredAt: Date | null;
	currentStateName: string;
	now?: Date;
};

const MILLISECONDS_PER_DAY = 1000 * 60 * 60 * 24;

const wholeDaysBetween = (from: Date, to: Date): number => {
	const fromDateOnly = Date.UTC(
		from.getUTCFullYear(),
		from.getUTCMonth(),
		from.getUTCDate(),
	);
	const toDateOnly = Date.UTC(
		to.getUTCFullYear(),
		to.getUTCMonth(),
		to.getUTCDate(),
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

	const days = wholeDaysBetween(currentStateEnteredAt, now);

	return <span>{`${days}d in ${currentStateName}`}</span>;
};

export default TimeInStateBadge;
