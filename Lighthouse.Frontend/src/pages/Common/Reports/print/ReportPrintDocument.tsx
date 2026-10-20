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

/** The report as it goes on paper: who it is about and when it was made, the screen's summary and panels, and whose tool made it. */
const ReportPrintDocument: React.FC<ReportPrintDocumentProps> = ({
	report,
	ownerLine,
	producedAt,
}) => {
	const { thenWindow, nowWindow } = report.thenAndNow;

	return (
		<Stack spacing={2}>
			<Box component="header">
				<Typography>{ownerLine}</Typography>
				<Typography variant="h4" component="h1">
					{report.name}
				</Typography>
				<Typography>
					{`Then ${daysOf(thenWindow)} · Now ${daysOf(nowWindow)}`}
				</Typography>
				<Typography>{`Produced ${format(producedAt, PRODUCED_AT)}`}</Typography>
				<Divider sx={{ mt: 2 }} />
			</Box>
			<ThenAndNowSummary
				thenAndNow={report.thenAndNow}
				shownMetricKeys={report.shownMetricKeys}
			/>
			<ReportPanels report={report} />
			<Box
				component="footer"
				sx={{ display: "flex", alignItems: "center", gap: 2 }}
			>
				<LighthouseLogo linksHome={false} />
				<Typography variant="body2">{BRANDING}</Typography>
			</Box>
		</Stack>
	);
};

// Printing rewrites the page's history entry, which draws the page again; the report on it has not changed.
export default memo(ReportPrintDocument);
