import { Box } from "@mui/material";
import type React from "react";
import getAgeInDaysFromStart from "../../../utils/date/age";

type TimeInStateBadgeProps = {
	currentStateEnteredAt: Date | null;
	currentStateName: string;
	stalenessThresholdDays?: number;
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
	stalenessThresholdDays,
	now = new Date(),
}) => {
	if (currentStateEnteredAt === null) {
		return <span>—</span>;
	}

	const days = daysInState(currentStateEnteredAt, now);
	const label = `${days}d in ${currentStateName}`;

	const isStale =
		stalenessThresholdDays !== undefined &&
		stalenessThresholdDays > 0 &&
		days > stalenessThresholdDays;

	if (isStale) {
		return (
			<Box
				component="span"
				data-testid="time-in-state-stale"
				sx={{ color: "error.main" }}
			>
				{label}
			</Box>
		);
	}

	return <span>{label}</span>;
};

export default TimeInStateBadge;
