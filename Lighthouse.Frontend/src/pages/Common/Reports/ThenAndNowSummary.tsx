import { Box, Paper, Stack, Typography } from "@mui/material";
import type React from "react";
import type {
	IReportMetric,
	IReportWindow,
	IThenAndNow,
	ReportMetricSide,
} from "../../../models/Reports/Report";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";
import { resolvedThenDays } from "./wizard/thenAndNow/thenAndNowDraft";

const CYCLE_TIME_PERCENTILES = ["p85", "p70", "p50"] as const;

type SummarySide = "then" | "now";

/** Every Work Item that closed in a window has a Cycle Time, so the percentiles' sample size is the count closed. */
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

const windowLineOf = (window: IReportWindow): string => {
	const length = `${window.lengthDays} ${window.lengthDays === 1 ? "day" : "days"}`;
	const days = resolvedThenDays(window.end, window.lengthDays);
	return days === null ? length : `${days} · ${length}`;
};

interface SideProps {
	side: SummarySide;
	heading: string;
	window: IReportWindow;
	closedLine: string | null;
}

/** Each line ends in a space, as a panel's do, so the summary reads as separate lines. */
const Side: React.FC<Readonly<SideProps>> = ({
	side,
	heading,
	window,
	closedLine,
}) => {
	const first = side === "then";
	return (
		<Stack
			spacing={0.5}
			data-summary-side={side}
			sx={{
				minWidth: 0,
				pl: { md: first ? 0 : 2 },
				borderLeft: { md: first ? 0 : 1 },
				borderColor: "divider",
			}}
		>
			<Typography
				variant="overline"
				color="text.secondary"
				component="div"
				sx={{ lineHeight: 1.5 }}
			>
				{heading}
			</Typography>{" "}
			<Typography component="div">{windowLineOf(window)}</Typography>{" "}
			{closedLine !== null && (
				<>
					<Typography component="div">{closedLine}</Typography>{" "}
				</>
			)}
		</Stack>
	);
};

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
		<Paper variant="outlined" data-report-summary sx={{ p: 2, width: "100%" }}>
			<Box
				sx={{
					display: "grid",
					gridTemplateColumns: { xs: "1fr", md: "repeat(2, minmax(0, 1fr))" },
					gap: 2,
				}}
			>
				<Side
					side="then"
					heading="Then"
					window={thenAndNow.thenWindow}
					closedLine={closedLineOf(cycleTime?.atThen)}
				/>
				<Side
					side="now"
					heading="Now"
					window={thenAndNow.nowWindow}
					closedLine={closedLineOf(cycleTime?.atNow)}
				/>
			</Box>
		</Paper>
	);
};

export default ThenAndNowSummary;
