import type React from "react";
import getAgeInDaysFromStart from "../../../utils/date/age";

type TimeInStateBadgeProps = {
	currentStateEnteredAt: Date | null;
	currentStateName: string;
	now?: Date;
};

export const daysInState = (
	currentStateEnteredAt: Date | null,
	now: Date = new Date(),
): number => {
	if (currentStateEnteredAt === null) {
		return 0;
	}

	return getAgeInDaysFromStart(currentStateEnteredAt, now);
};

const TimeInStateBadge: React.FC<TimeInStateBadgeProps> = ({
	currentStateEnteredAt,
	currentStateName,
	now = new Date(),
}) => {
	if (currentStateEnteredAt === null) {
		return <span>—</span>;
	}

	const days = daysInState(currentStateEnteredAt, now);

	return <span>{`${days}d in ${currentStateName}`}</span>;
};

export default TimeInStateBadge;
