import { Box, Paper, Typography } from "@mui/material";
import type React from "react";
import { Fragment } from "react";
import type {
	IReportMetric,
	IReportWindow,
	IThenAndNow,
	ReportMetricKey,
	ReportMetricSide,
} from "../../../models/Reports/Report";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";
import {
	type ChangeKind,
	type ShownMetricLines,
	summariseChange,
} from "./panels/changeSummary";
import { inCatalogOrder, panelFor } from "./panels/reportPanelRegistry";
import { verdictColours, visuallyHidden } from "./panels/ThenNowLineParts";
import { ThenNowColumn, ThenNowColumns } from "./panels/ThenNowPanelFrame";
import {
	DASH,
	lineComparisonsOf,
	NO_CHANGE_MARK,
} from "./panels/thenNowComparison";
import { windowDaysOf } from "./wizard/thenAndNow/thenAndNowDraft";

const CYCLE_TIME_PERCENTILES = ["p85", "p70", "p50"] as const;

/** How each kind of change is marked and named. Only the mark is coloured; the word carries the meaning. */
const CHANGE_KIND_LOOK: Record<
	ChangeKind,
	{ readonly mark: string; readonly word: string; readonly colour: string }
> = {
	improved: { mark: "✔", word: "Improved", colour: verdictColours.better },
	worse: { mark: "✘", word: "Got worse", colour: verdictColours.worse },
	mixed: { mark: "±", word: "Mixed", colour: verdictColours.neutral },
	unchanged: {
		mark: NO_CHANGE_MARK,
		word: "Unchanged",
		colour: verdictColours.neutral,
	},
};

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

/** Each shown metric that has a panel and was captured, its lines judged as its panel shows them. */
const shownMetricLinesOf = (
	thenAndNow: IThenAndNow,
	shownMetricKeys: readonly ReportMetricKey[],
): ShownMetricLines[] =>
	inCatalogOrder(shownMetricKeys).flatMap((key) => {
		const panel = panelFor(key);
		const metric = thenAndNow.metrics[key];
		if (panel === undefined || metric === null || metric === undefined)
			return [];
		return [{ key, lines: lineComparisonsOf(metric, panel) }];
	});

/** Ends in a space so the summary reads as separate lines to a screen reader or anything else taking its text. */
const SummaryLine: React.FC<{ readonly text: string | null }> = ({ text }) =>
	text === null ? null : (
		<>
			<Typography component="div">{text}</Typography>{" "}
		</>
	);

/** The shown metrics grouped by which way they moved, named as their panels are titled; a dash when none compares. */
const SummaryChange: React.FC<{
	readonly thenAndNow: IThenAndNow;
	readonly shownMetricKeys: readonly ReportMetricKey[];
}> = ({ thenAndNow, shownMetricKeys }) => {
	const { getTerm } = useTerminology();
	const groups = summariseChange(
		shownMetricLinesOf(thenAndNow, shownMetricKeys),
	);
	if (groups.length === 0) return <SummaryLine text={DASH} />;
	const nameOf = (key: ReportMetricKey) =>
		panelFor(key)?.titleOf(getTerm) ?? key;
	return groups.map(({ kind, keys }) => {
		const { mark, word, colour } = CHANGE_KIND_LOOK[kind];
		return (
			<Fragment key={kind}>
				<Typography component="div" data-change-kind={kind}>
					<Box component="span" aria-hidden sx={{ color: colour }}>
						{mark}
					</Box>{" "}
					{`${word}: ${keys.map(nameOf).join(", ")}`}
				</Typography>{" "}
			</Fragment>
		);
	});
};

/**
 * Each side's window, its length and how many Work Items closed in it, so two windows of different lengths are not
 * misread, and which of the shown metrics changed, so a reader knows before the first panel.
 */
const ThenAndNowSummary: React.FC<{
	readonly thenAndNow: IThenAndNow;
	readonly shownMetricKeys: readonly ReportMetricKey[];
}> = ({ thenAndNow, shownMetricKeys }) => {
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
			{/* Names the summary for anyone moving through the page by headings. */}
			<Typography component="h2" sx={visuallyHidden}>
				Summary
			</Typography>
			<ThenNowColumns count={3}>
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
				<ThenNowColumn
					data-summary-side="change"
					heading="Change"
					first={false}
				>
					<SummaryChange
						thenAndNow={thenAndNow}
						shownMetricKeys={shownMetricKeys}
					/>
				</ThenNowColumn>
			</ThenNowColumns>
		</Paper>
	);
};

export default ThenAndNowSummary;
