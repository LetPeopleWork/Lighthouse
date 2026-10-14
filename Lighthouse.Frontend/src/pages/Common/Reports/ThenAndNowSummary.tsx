import { Paper, Typography } from "@mui/material";
import type React from "react";
import type {
	IReportMetric,
	IReportWindow,
	IThenAndNow,
	ReportMetricSide,
} from "../../../models/Reports/Report";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";
import { ThenNowColumn, ThenNowColumns } from "./panels/ThenNowPanelFrame";
import { windowDaysOf } from "./wizard/thenAndNow/thenAndNowDraft";

const CYCLE_TIME_PERCENTILES = ["p85", "p70", "p50"] as const;

/** Off screen but read aloud: it names the summary for anyone moving through the page by headings. */
const VISUALLY_HIDDEN = {
	position: "absolute",
	width: "1px",
	height: "1px",
	overflow: "hidden",
	clip: "rect(0 0 0 0)",
	whiteSpace: "nowrap",
} as const;

/** The Work Items behind the Cycle Time percentiles: those that closed in the window with a Cycle Time. */
const closedCountOf = (side: ReportMetricSide): number | null => {
	const values = CYCLE_TIME_PERCENTILES.map((key) => side[key]);
	const size = values
		.map((value) => value?.sampleSize)
		.find((sampleSize) => sampleSize !== null && sampleSize !== undefined);
	if (size !== undefined) return size;
	return values.some((value) => value?.absentReason === "no-finished-items")
		? 0
		: null;
};

const windowLineOf = (reportWindow: IReportWindow): string => {
	const length = `${reportWindow.lengthDays} ${reportWindow.lengthDays === 1 ? "day" : "days"}`;
	const days = windowDaysOf(reportWindow.start, reportWindow.end);
	return days === null ? length : `${days} · ${length}`;
};

/** Ends in a space so the summary reads as separate lines to a screen reader or anything else taking its text. */
const SummaryLine: React.FC<{ readonly text: string | null }> = ({ text }) =>
	text === null ? null : (
		<>
			<Typography component="div">{text}</Typography>{" "}
		</>
	);

/** Each side's window, its length and how many Work Items closed in it, so two windows of different lengths are not misread. */
const ThenAndNowSummary: React.FC<{ readonly thenAndNow: IThenAndNow }> = ({
	thenAndNow,
}) => {
	const { getTerm } = useTerminology();
	const cycleTime: IReportMetric | null | undefined =
		thenAndNow.metrics["cycle-time"];

	const closedLineOf = (side: ReportMetricSide | undefined): string | null => {
		const count = side === undefined ? null : closedCountOf(side);
		if (count === null) return null;
		const term = getTerm(
			count === 1 ? TERMINOLOGY_KEYS.WORK_ITEM : TERMINOLOGY_KEYS.WORK_ITEMS,
		);
		return `${count} ${term} closed`;
	};

	return (
		<Paper
			variant="outlined"
			data-report-summary
			sx={{ p: 2, width: "100%", position: "relative" }}
		>
			<Typography component="h2" sx={VISUALLY_HIDDEN}>
				Summary
			</Typography>
			<ThenNowColumns count={2}>
				<ThenNowColumn data-summary-side="then" heading="Then" first>
					<SummaryLine text={windowLineOf(thenAndNow.thenWindow)} />
					<SummaryLine text={closedLineOf(cycleTime?.atThen)} />
				</ThenNowColumn>
				<ThenNowColumn data-summary-side="now" heading="Now" first={false}>
					<SummaryLine text={windowLineOf(thenAndNow.nowWindow)} />
					<SummaryLine text={closedLineOf(cycleTime?.atNow)} />
				</ThenNowColumn>
			</ThenNowColumns>
		</Paper>
	);
};

export default ThenAndNowSummary;
