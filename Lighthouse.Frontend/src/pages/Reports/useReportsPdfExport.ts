import type React from "react";
import { createElement, useEffect, useRef, useState } from "react";
import { useErrorSnackbar } from "../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import type { IReportAcrossOwners } from "../../models/Reports/ReportsAcrossOwners";
import PrintFrameHost from "../Common/Reports/print/PrintFrameHost";
import type { PrintedReport } from "../Common/Reports/print/ReportPrintBundle";
import type { PrintOutcome } from "../Common/Reports/print/usePrintOnce";
import { ownerOfReport } from "../Common/Reports/reportOwner";

export interface ReportsPdfExport {
	/** Prints the reports as one PDF, in the order given; a second choice while one is prepared does nothing. */
	readonly exportPdf: (reports: readonly IReportAcrossOwners[]) => void;
	/** Whether a PDF is being prepared, until its print dialog has been asked for. */
	readonly preparing: boolean;
	/** The hidden frame the reports are printed from, while there is one. */
	readonly frame: React.ReactNode;
}

interface Exporting {
	readonly reports: readonly PrintedReport[];
	/** Each export draws into a frame of its own. */
	readonly attempt: number;
}

/**
 * Exports several reports, of any owners, as one PDF from a hidden frame on the Reports page. The frame goes again a
 * turn after its dialog closes, or as soon as no PDF can be made, which the error snackbar then says.
 */
export const useReportsPdfExport = (): ReportsPdfExport => {
	const { showError } = useErrorSnackbar();
	const attempts = useRef(0);
	const [exporting, setExporting] = useState<Exporting | undefined>();
	const [preparing, setPreparing] = useState(false);
	const frameRemoval = useRef<ReturnType<typeof setTimeout> | undefined>(
		undefined,
	);

	useEffect(() => () => clearTimeout(frameRemoval.current), []);

	const exportPdf = (reports: readonly IReportAcrossOwners[]) => {
		if (preparing || reports.length === 0) return;
		attempts.current += 1;
		setPreparing(true);
		setExporting({
			reports: reports.map((report) => ({
				owner: ownerOfReport(report),
				reportId: report.id,
			})),
			attempt: attempts.current,
		});
	};

	// Never inside the dialog's own closing: some browsers say it closed before print() has returned.
	const removeTheFrameOf = (attempt: number) => {
		frameRemoval.current = setTimeout(() =>
			setExporting((current) =>
				current?.attempt === attempt ? undefined : current,
			),
		);
	};

	const tellFor =
		(attempt: number) =>
		(outcome: PrintOutcome): void => {
			setPreparing(false);
			if (outcome.kind === "printing") return;
			removeTheFrameOf(attempt);
			if (outcome.kind === "failed") showError(outcome.message);
		};

	const frame =
		exporting === undefined
			? null
			: createElement(PrintFrameHost, {
					key: exporting.attempt,
					reports: exporting.reports,
					tell: tellFor(exporting.attempt),
				});

	return { exportPdf, preparing, frame };
};
