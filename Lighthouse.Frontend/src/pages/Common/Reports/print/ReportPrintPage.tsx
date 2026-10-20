import type React from "react";
import { useParams } from "react-router";
import type { IReportOwner } from "../../../../models/Reports/Report";
import { reportsPathOf } from "../reportOwner";
import PrintSheet from "./PrintSheet";
import type { PrintReadPort } from "./printReadPort";
import ReportPrintContent, { type PrintTerms } from "./ReportPrintContent";
import type { PrintEffectPort } from "./usePrintOnce";

interface ReportPrintPageProps {
	readonly kind: IReportOwner["kind"];
	readonly reads: PrintReadPort;
	readonly terms: PrintTerms;
	readonly effects: PrintEffectPort;
}

/**
 * The print page of a report, opened by its address: the report on its sheet, and the way to the owner's Reports
 * list. It never asks for the print dialog, because Export PDF prints from a hidden frame on the reader's own page,
 * and nothing sends a reader here.
 */
const ReportPrintPage: React.FC<ReportPrintPageProps> = ({
	kind,
	reads,
	terms,
	effects,
}) => {
	const params = useParams();
	const owner: IReportOwner = { kind, id: Number(params.id) };
	const reportsPath = reportsPathOf(owner);

	return (
		<ReportPrintContent
			owner={owner}
			reportId={Number(params.reportId)}
			reads={reads}
			terms={terms}
			effects={effects}
			renderSheet={(look, report) => (
				<PrintSheet ready={look.state === "ready"} reportsPath={reportsPath}>
					{report}
				</PrintSheet>
			)}
		/>
	);
};

export default ReportPrintPage;
