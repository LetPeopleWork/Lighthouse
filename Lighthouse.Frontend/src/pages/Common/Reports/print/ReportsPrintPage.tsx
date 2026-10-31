import { Typography } from "@mui/material";
import type React from "react";
import { useMemo } from "react";
import { useSearchParams } from "react-router";
import PrintSheet from "./PrintSheet";
import { printedReportsOf } from "./printedReportsQuery";
import type { PrintReadPort } from "./printReadPort";
import ReportPrintBundle from "./ReportPrintBundle";
import type { PrintContentEffects, PrintTerms } from "./ReportPrintContent";

const NOTHING_NAMED = "No reports were named to print.";
const REPORTS_PATH = "/reports";

interface ReportsPrintPageProps {
	readonly reads: PrintReadPort;
	readonly terms: PrintTerms;
	readonly effects: PrintContentEffects;
}

/**
 * Several reports as one PDF would print them, opened by its address, such as `/print/reports?reports=team-3-12`. Like
 * the print page of one report, it never asks for the print dialog.
 */
const ReportsPrintPage: React.FC<ReportsPrintPageProps> = ({
	reads,
	terms,
	effects,
}) => {
	const [searchParams] = useSearchParams();
	const query = searchParams.get("reports");
	const reports = useMemo(() => printedReportsOf(query), [query]);

	if (reports.length === 0) {
		return (
			<PrintSheet ready={false} reportsPath={REPORTS_PATH}>
				<Typography color="text.secondary">{NOTHING_NAMED}</Typography>
			</PrintSheet>
		);
	}

	return (
		<ReportPrintBundle
			reports={reports}
			reads={reads}
			terms={terms}
			effects={effects}
			renderSheet={(look, drawn) => (
				<PrintSheet ready={look.state === "ready"} reportsPath={REPORTS_PATH}>
					{drawn}
				</PrintSheet>
			)}
		/>
	);
};

export default ReportsPrintPage;
