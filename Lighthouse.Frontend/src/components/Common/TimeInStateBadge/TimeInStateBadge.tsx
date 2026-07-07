import { Box, Tooltip } from "@mui/material";
import type React from "react";
import getAgeInDaysFromStart from "../../../utils/date/age";
import {
	deriveStaleness,
	type StalenessResult,
} from "../../../utils/staleness/deriveStaleness";

type TimeInStateBadgeProps = {
	currentStateEnteredAt: Date | null;
	currentStateName: string;
	stalenessThresholdDays?: number;
	blockedStalenessThresholdDays?: number;
	isBlocked?: boolean;
	blockedSince?: string | null;
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
	blockedStalenessThresholdDays,
	isBlocked = false,
	blockedSince = null,
	now = new Date(),
}) => {
	if (currentStateEnteredAt === null) {
		return <span>—</span>;
	}

	const days = daysInState(currentStateEnteredAt, now);
	const label = `${days}d in ${currentStateName}`;

	const staleResult: StalenessResult = deriveStaleness(
		{ currentStateEnteredAt, isBlocked, currentStateName, blockedSince },
		stalenessThresholdDays,
		blockedStalenessThresholdDays ?? 0,
		now,
	);

	if (staleResult.isStale) {
		const reasonsText = staleResult.reasons
			.map((r) => {
				if (r.kind === "blocked-duration") return `Blocked ${r.days}d`;
				return `${r.days}d in ${r.stateName}`;
			})
			.join("; ");
		const tooltip = `${label} — ${reasonsText}`;

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
