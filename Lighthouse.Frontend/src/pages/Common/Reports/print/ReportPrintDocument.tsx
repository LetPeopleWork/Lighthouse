import { Box, Divider, Stack, Typography } from "@mui/material";
import { format } from "date-fns";
import type React from "react";
import { memo } from "react";
import LighthouseLogo from "../../../../components/App/LetPeopleWork/LighthouseLogo";
import type {
	IReport,
	IReportOwner,
	IReportWindow,
} from "../../../../models/Reports/Report";
import ReportPanels from "../panels/ReportPanels";
import { DASH } from "../panels/thenNowComparison";
import { ownerTermKeyOf } from "../reportOwner";
import ThenAndNowSummary from "../ThenAndNowSummary";
import { windowDaysOf } from "../wizard/thenAndNow/thenAndNowDraft";

const BRANDING = "Made with Lighthouse by LetPeopleWork · letpeople.work";
const PRODUCED_AT = "d MMM yyyy, HH:mm";

/**
 * On paper the report is a table, with the line as its header group and the footer as its footer group. The browser
 * repeats a table's header and footer groups at the top and bottom of every sheet the table runs over, and only
 * those, so when several reports share one document each sheet carries the line and footer of its own report. Each
 * group keeps a fixed height, so a report's pages all look alike.
 */
const LINE_HEIGHT = "10mm";
const FOOTER_HEIGHT = "16mm";
const GAP_TO_THE_REPORT = "4mm";

/** The summary is read as one piece, so a page break never lands inside it. */
const SUMMARY_KEPT_WHOLE = {
	"& [data-report-summary]": { breakInside: "avoid" },
};

const RUNNING_LINE = {
	"@media print": {
		boxSizing: "border-box",
		height: LINE_HEIGHT,
		marginBottom: GAP_TO_THE_REPORT,
		display: "flex",
		flexDirection: "column",
		justifyContent: "flex-end",
	},
};

const LINE_PARTS = { display: "flex", alignItems: "baseline", gap: 3 };

/** A name may be cut short so the line stays one line; the windows and the date never are. */
const NAME_PART = {
	whiteSpace: "nowrap",
	overflow: "hidden",
	textOverflow: "ellipsis",
	minWidth: 0,
	"@media print": { fontSize: "8pt" },
};
const DATE_PART = { ...NAME_PART, flexShrink: 0 };

const CREDIT_FOOTER = {
	display: "flex",
	alignItems: "center",
	gap: 2,
	borderTop: 1,
	borderColor: "divider",
	"@media print": {
		boxSizing: "border-box",
		height: FOOTER_HEIGHT,
		marginTop: GAP_TO_THE_REPORT,
	},
};

/** On the screen the line, the report and the footer simply stack, a little apart. */
const THE_REPORT = {
	...SUMMARY_KEPT_WHOLE,
	display: "flex",
	flexDirection: "column",
	gap: 2,
	"@media print": { display: "table", width: "100%" },
};
const THE_REPORT_BODY = { "@media print": { display: "table-row-group" } };

// Chrome repeats a header or footer group on every sheet only when the group may not break; without that it prints
// the line or footer on the report's first sheet alone, and the next sheet's panels start at the very top.
const repeatedAs = (group: "table-header-group" | "table-footer-group") => ({
	"@media print": { display: group, breakInside: "avoid" },
});

const THE_LINES_GROUP = repeatedAs("table-header-group");
const THE_FOOTERS_GROUP = repeatedAs("table-footer-group");

/** The owner as the header names it, in the instance's own word for its kind: "Team Lightspeed". */
export const ownerLineOf = (
	owner: IReportOwner,
	name: string,
	getTerm: (key: string) => string,
): string => `${getTerm(ownerTermKeyOf(owner))} ${name}`;

/** The browser offers the page's title as the PDF's file name. */
export const printTitleOf = (reportName: string, ownerLine: string): string =>
	`${reportName} — ${ownerLine}`;

const daysOf = (reportWindow: IReportWindow): string =>
	windowDaysOf(reportWindow.start, reportWindow.end) ?? DASH;

interface ReportPrintDocumentProps {
	readonly report: IReport;
	readonly ownerLine: string;
	/** When the report was read, in the reader's local time. */
	readonly producedAt: number;
}

/**
 * The report as it goes on paper: a line on every page saying whose report it is, which windows it compares and when
 * it was made; the report's name as the first page's title; the screen's summary and panels; and on every page the
 * mark of the tool that made it.
 */
const ReportPrintDocument: React.FC<ReportPrintDocumentProps> = ({
	report,
	ownerLine,
	producedAt,
}) => {
	const { thenWindow, nowWindow } = report.thenAndNow;

	return (
		<Box sx={THE_REPORT}>
			<Box sx={THE_LINES_GROUP}>
				<Box component="header" sx={RUNNING_LINE}>
					<Box sx={LINE_PARTS}>
						<Typography variant="caption" component="p" sx={NAME_PART}>
							{ownerLine}
						</Typography>
						<Typography variant="caption" component="p" sx={NAME_PART}>
							{report.name}
						</Typography>
						<Typography variant="caption" component="p" sx={DATE_PART}>
							{`Then ${daysOf(thenWindow)} · Now ${daysOf(nowWindow)}`}
						</Typography>
						<Typography variant="caption" component="p" sx={DATE_PART}>
							{`Produced ${format(producedAt, PRODUCED_AT)}`}
						</Typography>
					</Box>
					<Divider sx={{ mt: 0.5 }} />
				</Box>
			</Box>
			<Stack spacing={2} sx={THE_REPORT_BODY}>
				<Typography variant="h4" component="h1">
					{report.name}
				</Typography>
				<ThenAndNowSummary
					thenAndNow={report.thenAndNow}
					shownMetricKeys={report.shownMetricKeys}
				/>
				<ReportPanels report={report} />
			</Stack>
			<Box sx={THE_FOOTERS_GROUP}>
				<Box component="footer" sx={CREDIT_FOOTER}>
					<LighthouseLogo linksHome={false} />
					<Typography variant="body2">{BRANDING}</Typography>
				</Box>
			</Box>
		</Box>
	);
};

// Whatever holds the printed report draws again as the export moves on, such as when the dialog is asked for; the
// report itself has not changed, so its charts are not drawn again under the print dialog.
export default memo(ReportPrintDocument);
