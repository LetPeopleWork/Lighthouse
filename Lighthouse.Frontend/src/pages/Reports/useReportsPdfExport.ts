import type React from "react";
import { createElement, Fragment, useEffect, useRef, useState } from "react";
import type { IReportAcrossOwners } from "../../models/Reports/ReportsAcrossOwners";
import type { PrintedReport } from "../Common/Reports/print/bundleState";
import PrintFrameHost from "../Common/Reports/print/PrintFrameHost";
import type { PrintOutcome } from "../Common/Reports/print/usePrintOnce";
import { ownerOfReport } from "../Common/Reports/reportOwner";
import NoPdfDialog from "./NoPdfDialog";

export interface ReportsPdfExport {
	/** Prints the reports as one PDF, in the order given; a second choice while one is prepared does nothing. */
	readonly exportPdf: (reports: readonly IReportAcrossOwners[]) => void;
	/** Whether a PDF is being prepared, until its print dialog has been asked for. */
	readonly preparing: boolean;
	/** The hidden frame the reports are printed from, and the word that no PDF was made, while there is either. */
	readonly frame: React.ReactNode;
}

interface Exporting {
	readonly reports: readonly IReportAcrossOwners[];
	/** Each export draws into a frame of its own. */
	readonly attempt: number;
}

interface NoPdf {
	readonly reports: readonly IReportAcrossOwners[];
	/** Those that could not be read, in print order; none when a part of the page failed. */
	readonly failed: readonly IReportAcrossOwners[];
}

const printedReportOf = (report: IReportAcrossOwners): PrintedReport => ({
	owner: ownerOfReport(report),
	reportId: report.id,
});

const isTheSameReport = (
	report: IReportAcrossOwners,
	{ owner, reportId }: PrintedReport,
): boolean =>
	report.id === reportId &&
	report.ownerKind === owner.kind &&
	report.ownerId === owner.id;

const failedAmong = (
	reports: readonly IReportAcrossOwners[],
	failedReports: readonly PrintedReport[],
): IReportAcrossOwners[] =>
	reports.filter((report) =>
		failedReports.some((failed) => isTheSameReport(report, failed)),
	);

/**
 * Exports several reports, of any owners, as one PDF from a hidden frame on the Reports page. The frame goes again a
 * turn after its dialog closes, or as soon as no PDF can be made; then a dialog names each report that could not be
 * read and offers to leave them out, which unpicks them, or to ask again.
 */
export const useReportsPdfExport = (
	unpick: (reports: readonly IReportAcrossOwners[]) => void,
): ReportsPdfExport => {
	const attempts = useRef(0);
	const [exporting, setExporting] = useState<Exporting | undefined>();
	const [preparing, setPreparing] = useState(false);
	const [noPdf, setNoPdf] = useState<NoPdf | undefined>();
	const frameRemoval = useRef<ReturnType<typeof setTimeout> | undefined>(
		undefined,
	);

	useEffect(() => () => clearTimeout(frameRemoval.current), []);

	const printFromAFreshFrame = (reports: readonly IReportAcrossOwners[]) => {
		attempts.current += 1;
		setNoPdf(undefined);
		setPreparing(true);
		setExporting({ reports, attempt: attempts.current });
	};

	const exportPdf = (reports: readonly IReportAcrossOwners[]) => {
		if (preparing || reports.length === 0) return;
		printFromAFreshFrame(reports);
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
		({ reports, attempt }: Exporting) =>
		(outcome: PrintOutcome): void => {
			setPreparing(false);
			if (outcome.kind === "printing") return;
			removeTheFrameOf(attempt);
			if (outcome.kind === "failed")
				setNoPdf({
					reports,
					failed: failedAmong(reports, outcome.failedReports ?? []),
				});
		};

	const leaveOutTheFailedOf = ({ reports, failed }: NoPdf) => {
		const rest = reports.filter((report) => !failed.includes(report));
		if (failed.length === 0 || rest.length === 0) return undefined;
		return () => {
			unpick(failed);
			printFromAFreshFrame(rest);
		};
	};

	const dialog =
		noPdf === undefined
			? null
			: createElement(NoPdfDialog, {
					failedReports: noPdf.failed,
					onLeaveItOut: leaveOutTheFailedOf(noPdf),
					onRetry: () => printFromAFreshFrame(noPdf.reports),
					onCancel: () => setNoPdf(undefined),
				});

	const frameHost =
		exporting === undefined
			? null
			: createElement(PrintFrameHost, {
					key: exporting.attempt,
					reports: exporting.reports.map(printedReportOf),
					tell: tellFor(exporting),
				});

	return {
		exportPdf,
		preparing,
		frame: createElement(Fragment, null, frameHost, dialog),
	};
};
