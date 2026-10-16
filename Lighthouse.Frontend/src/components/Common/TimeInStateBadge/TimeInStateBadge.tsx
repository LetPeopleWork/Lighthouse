import { Box, Tooltip } from "@mui/material";
import type React from "react";
import type { IStaleReason } from "../../../models/WorkItem";
import getAgeInDaysFromStart from "../../../utils/date/age";

type TimeInStateBadgeProps = {
	currentStateEnteredAt: Date | null;
	currentStateName: string;
	now?: Date;
	isStale?: boolean;
	staleReasons?: IStaleReason[];
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

const describeReason = (reason: IStaleReason): string =>
	reason.kind === "BlockedDuration"
		? `Blocked ${reason.days}d`
		: `${reason.days}d in ${reason.stateName}`;

const TimeInStateBadge: React.FC<TimeInStateBadgeProps> = ({
	currentStateEnteredAt,
	currentStateName,
	now = new Date(),
	isStale = false,
	staleReasons = [],
}) => {
	if (currentStateEnteredAt === null) {
		return <span>—</span>;
	}

	const days = daysInState(currentStateEnteredAt, now);
	const label = `${days}d in ${currentStateName}`;

	if (isStale) {
		const tooltip = `${label} — ${staleReasons.map(describeReason).join("; ")}`;

		return (
			<Tooltip title={tooltip} placement="top">
				<Box
					component="span"
					data-testid="time-in-state-stale"
					aria-label={tooltip}
					sx={{ color: "error.main" }}
				>
					{label}
				</Box>
			</Tooltip>
		);
	}

	return <span>{label}</span>;
};

export default TimeInStateBadge;
