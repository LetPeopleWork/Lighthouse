import { Alert, type AlertColor, AlertTitle } from "@mui/material";
import type React from "react";
import type {
	IRefinementNeed,
	NeedUnavailableReason,
	RefinementVerdict,
} from "../../../../models/Refinement/Refinement";
import { parseLocalDate } from "../../../../utils/date/localDate";
import { INSUFFICIENT_FORECAST_DATA_MESSAGE } from "../../../../utils/forecast/insufficientForecastData";
import InfoTooltip from "./InfoTooltip";
import { describeNeed, describeNeedOrigin } from "./needWording";

// Too many ready is as much a reason to act as too few, so both warn; only in range reads as settled.
const SEVERITY: Record<RefinementVerdict, AlertColor> = {
	Below: "warning",
	In: "success",
	Above: "warning",
};

// Too little history gets the message forecasts give. Without a cadence the heading already says what is
// missing, and a Team without Refinement states never sees this tab, so neither gets a message here.
const UNAVAILABLE_MESSAGE: Record<NeedUnavailableReason, string | null> = {
	InsufficientData: INSUFFICIENT_FORECAST_DATA_MESSAGE,
	NoCadence: null,
	NoRefinementStates: null,
};

/** A need the server gave a verdict and a range for. */
type JudgedNeed = IRefinementNeed & {
	verdict: RefinementVerdict;
	low: number;
	high: number;
	lowPercentile: number;
	highPercentile: number;
	horizonWorkingDays: number;
};

const isJudged = (need: IRefinementNeed | undefined): need is JudgedNeed =>
	need?.verdict != null &&
	need.low != null &&
	need.high != null &&
	need.lowPercentile != null &&
	need.highPercentile != null &&
	need.horizonWorkingDays != null;

const refinementDayOf = (nextRefinementDate: string | null | undefined) =>
	nextRefinementDate == null ? null : parseLocalDate(nextRefinementDate);

/** Whether the tab shows a message about the need; one that shows carries the next Refinement as its title. */
export const showsNeedMessage = (
	need: IRefinementNeed | undefined,
	readyCount: number | undefined,
	nextRefinementDate: string | null | undefined,
): boolean => {
	if (need?.unavailableReason != null) {
		return UNAVAILABLE_MESSAGE[need.unavailableReason] !== null;
	}
	return (
		isJudged(need) &&
		readyCount !== undefined &&
		refinementDayOf(nextRefinementDate) !== null
	);
};

interface NeedMessageProps {
	severity: AlertColor;
	title: string | null;
	children: React.ReactNode;
}

const NeedMessage: React.FC<Readonly<NeedMessageProps>> = ({
	severity,
	title,
	children,
}) => (
	<Alert severity={severity}>
		{title !== null && <AlertTitle>{title}</AlertTitle>}
		{children}
	</Alert>
);

/** The words a Team has renamed that the verdict uses. */
export interface NeedVerdictTerms {
	workItem: string;
	workItems: string;
	team: string;
	throughput: string;
}

interface NeedVerdictProps {
	need: IRefinementNeed | undefined;
	readyCount: number | undefined;
	nextRefinementDate: string | null | undefined;
	/** The next Refinement as the tab names it, so the sentences can say "by then". */
	title: string | null;
	teamName: string;
	terms: NeedVerdictTerms;
}

/** Whether to refine more or stop, against the range the Team is likely to pull before its next Refinement. */
const NeedVerdict: React.FC<Readonly<NeedVerdictProps>> = ({
	need,
	readyCount,
	nextRefinementDate,
	title,
	teamName,
	terms,
}) => {
	if (need?.unavailableReason != null) {
		const message = UNAVAILABLE_MESSAGE[need.unavailableReason];
		return message === null ? null : (
			<NeedMessage severity="info" title={title}>
				{message}
			</NeedMessage>
		);
	}

	const refinementDay = refinementDayOf(nextRefinementDate);
	if (!isJudged(need) || readyCount === undefined || refinementDay === null) {
		return null;
	}

	const sentence = describeNeed({
		verdict: need.verdict,
		readyCount,
		low: need.low,
		high: need.high,
		teamName,
		workItemTerm: terms.workItem,
		workItemsTerm: terms.workItems,
	});
	const origin = describeNeedOrigin({
		teamName,
		horizonWorkingDays: need.horizonWorkingDays,
		refinementDay,
		lowPercentile: need.lowPercentile,
		highPercentile: need.highPercentile,
		teamTerm: terms.team,
		throughputTerm: terms.throughput,
	});

	return (
		<NeedMessage severity={SEVERITY[need.verdict]} title={title}>
			<span>{sentence}</span> <InfoTooltip text={origin} />
		</NeedMessage>
	);
};

export default NeedVerdict;
