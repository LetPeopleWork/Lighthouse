import { Box, Tooltip } from "@mui/material";
import type React from "react";
import type { IStaleReason, StaleReasonKind } from "../../../models/WorkItem";
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

	// Count to the reader's calendar day, the one Lighthouse is asked about: a range end arrives as
	// its local midnight, which east of Greenwich is still the day before in UTC.
	const readersDay = new Date(
		Date.UTC(now.getFullYear(), now.getMonth(), now.getDate(), 12),
	);
	return getAgeInDaysFromStart(currentStateEnteredAt, readersDay);
};

const knownReasonKinds: ReadonlySet<StaleReasonKind> = new Set([
	"TimeInState",
	"BlockedDuration",
	"TimeInStateWhileBlocked",
]);

const describeReason = (reason: IStaleReason, stateName: string): string =>
	reason.kind === "BlockedDuration"
		? `Blocked ${reason.days}d`
		: `${reason.days}d in ${reason.stateName ?? stateName}`;

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
		const reasons = staleReasons
			.filter((reason) => knownReasonKinds.has(reason.kind))
			.map((reason) => describeReason(reason, currentStateName))
			.join("; ");
		const tooltip = `${label} — ${reasons}`;

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
