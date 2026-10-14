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

const cycleTimeSampleSizeOf = (side: ReportMetricSide): number | null => {
	const values = CYCLE_TIME_PERCENTILES.map((key) => side[key]);
	const size = values
		.map((value) => value?.sampleSize)
		.find((sampleSize) => sampleSize !== null && sampleSize !== undefined);
	if (size !== undefined) return size;
	return values.some((value) => value?.absentReason === "no-finished-items")
		? 0
		: null;
};

/**
 * How many Work Items closed in the window. Throughput counts every one that finished, so a report that captured it
 * reads its total and agrees with the Throughput panel. An older report falls back to the Work Items behind its
 * Cycle Time percentiles, which leaves out any whose Cycle Time could not be measured.
 */
const closedCountOf = (
	throughputSide: ReportMetricSide | undefined,
	cycleTimeSide: ReportMetricSide | undefined,
): number | null => {
	const total = throughputSide?.total?.value;
	if (total !== null && total !== undefined) return total;
	return cycleTimeSide === undefined
		? null
		: cycleTimeSampleSizeOf(cycleTimeSide);
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
	const throughput: IReportMetric | null | undefined =
		thenAndNow.metrics.throughput;
	const cycleTime: IReportMetric | null | undefined =
		thenAndNow.metrics["cycle-time"];

	const closedLineOf = (
		throughputSide: ReportMetricSide | undefined,
		cycleTimeSide: ReportMetricSide | undefined,
	): string | null => {
		const count = closedCountOf(throughputSide, cycleTimeSide);
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
					<SummaryLine
						text={closedLineOf(throughput?.atThen, cycleTime?.atThen)}
					/>
				</ThenNowColumn>
				<ThenNowColumn data-summary-side="now" heading="Now" first={false}>
					<SummaryLine text={windowLineOf(thenAndNow.nowWindow)} />
					<SummaryLine
						text={closedLineOf(throughput?.atNow, cycleTime?.atNow)}
					/>
				</ThenNowColumn>
			</ThenNowColumns>
		</Paper>
	);
};

export default ThenAndNowSummary;
