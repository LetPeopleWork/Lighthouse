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
 * On paper the line and the footer are fixed to the page, so the browser prints them on every sheet. The report runs
 * as a table whose header and footer groups the browser also repeats on every sheet; those groups are as tall as the
 * line and the footer, so no panel runs underneath either of them.
 */
const LINE_HEIGHT = "10mm";
const FOOTER_HEIGHT = "16mm";
const GAP_TO_THE_REPORT = "4mm";

/** The summary is read as one piece, so a page break never lands inside it. */
const SUMMARY_KEPT_WHOLE = {
	"& [data-report-summary]": { breakInside: "avoid" },
};

const FIXED_TO_THE_PAGE = {
	position: "fixed",
	left: 0,
	right: 0,
	boxSizing: "border-box",
	bgcolor: "background.paper",
} as const;

const RUNNING_LINE = {
	"@media print": {
		...FIXED_TO_THE_PAGE,
		top: 0,
		height: LINE_HEIGHT,
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
	"@media print": { ...FIXED_TO_THE_PAGE, bottom: 0, height: FOOTER_HEIGHT },
};

const THE_REPORT = { "@media print": { display: "table", width: "100%" } };
const THE_REPORT_BODY = { "@media print": { display: "table-row-group" } };

const roomFor = (
	group: "table-header-group" | "table-footer-group",
	height: string,
	gap: { readonly marginTop: string } | { readonly marginBottom: string },
) => ({
	group: { display: "none", "@media print": { display: group } },
	spacer: { "@media print": { height, ...gap } },
});

const ROOM_FOR_THE_LINE = roomFor("table-header-group", LINE_HEIGHT, {
	marginBottom: GAP_TO_THE_REPORT,
});
const ROOM_FOR_THE_FOOTER = roomFor("table-footer-group", FOOTER_HEIGHT, {
	marginTop: GAP_TO_THE_REPORT,
});

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
		<Stack spacing={2} sx={SUMMARY_KEPT_WHOLE}>
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
			<Box sx={THE_REPORT}>
				<Box sx={ROOM_FOR_THE_LINE.group}>
					<Box sx={ROOM_FOR_THE_LINE.spacer} />
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
				<Box sx={ROOM_FOR_THE_FOOTER.group}>
					<Box sx={ROOM_FOR_THE_FOOTER.spacer} />
				</Box>
			</Box>
			<Box component="footer" sx={CREDIT_FOOTER}>
				<LighthouseLogo linksHome={false} />
				<Typography variant="body2">{BRANDING}</Typography>
			</Box>
		</Stack>
	);
};

// Printing rewrites the page's history entry, which draws the page again; the report on it has not changed.
export default memo(ReportPrintDocument);
