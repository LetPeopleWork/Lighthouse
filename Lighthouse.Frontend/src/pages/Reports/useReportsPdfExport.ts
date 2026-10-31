import type React from "react";
import { createElement, Fragment, useState } from "react";
import type { IReportListItem } from "../../models/Reports/Report";
import type { IReportAcrossOwners } from "../../models/Reports/ReportsAcrossOwners";
import { UsageDataEventName } from "../../services/Api/UsageDataService";
import { useUsageDataReporter } from "../../services/UsageData/usageDataReporter";
import type { PrintedReport } from "../Common/Reports/print/bundleState";
import PrintFrameHost from "../Common/Reports/print/PrintFrameHost";
import type { PrintOutcome } from "../Common/Reports/print/usePrintOnce";
import { ownerOfReport } from "../Common/Reports/reportOwner";
import {
	type PrintFrameAttempt,
	usePrintFrameAttempts,
} from "../Common/Reports/usePrintFrameAttempts";
import { useReportPdfExport } from "../Common/Reports/useReportPdfExport";
import NoPdfDialog from "./NoPdfDialog";

export interface ReportsPdfExport {
	/** Prints the reports as one PDF, in the order given; a second choice while one is prepared does nothing. */
	readonly exportPdf: (reports: readonly IReportAcrossOwners[]) => void;
	/** Prints one report as its own PDF, as its row's icon does; nothing while any PDF is prepared. */
	readonly exportOne: (report: IReportListItem) => void;
	/** Whether a PDF is being prepared, until its print dialog has been asked for. */
	readonly preparing: boolean;
	/** The report printed as its own PDF while that is prepared. */
	readonly preparingId: number | undefined;
	/** The hidden frame the reports are printed from, and the word that no PDF was made, while there is either. */
	readonly frame: React.ReactNode;
}

type Exporting = PrintFrameAttempt<readonly IReportAcrossOwners[]>;

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
 * read and offers to leave them out, which unpicks them, or to ask again. Exactly one report is the report's own
 * export, printed and counted as that. One instance serves the whole page, so only one PDF is ever prepared at once.
 */
export const useReportsPdfExport = (
	unpick: (reports: readonly IReportAcrossOwners[]) => void,
): ReportsPdfExport => {
	const reportUsage = useUsageDataReporter();
	const single = useReportPdfExport(undefined);
	const frames = usePrintFrameAttempts<readonly IReportAcrossOwners[]>();
	const [preparing, setPreparing] = useState(false);
	const [noPdf, setNoPdf] = useState<NoPdf | undefined>();

	const printFromAFreshFrame = (reports: readonly IReportAcrossOwners[]) => {
		setNoPdf(undefined);
		setPreparing(true);
		frames.start(reports);
	};

	const isPreparing = preparing || single.preparingId !== undefined;

	const exportOne = (report: IReportListItem) => {
		if (isPreparing) return;
		single.exportPdf(report, ownerOfReport(report));
	};

	const exportPdf = (reports: readonly IReportAcrossOwners[]) => {
		if (isPreparing || reports.length === 0) return;
		if (reports.length === 1) {
			exportOne(reports[0]);
			return;
		}
		reportUsage({ name: UsageDataEventName.ReportCombinedPdfExportStarted });
		printFromAFreshFrame(reports);
	};

	const tellFor =
		({ printing: reports, attempt }: Exporting) =>
		(outcome: PrintOutcome): void => {
			setPreparing(false);
			if (outcome.kind === "printing") return;
			frames.removeTheFrameOf(attempt);
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

	const exporting = frames.current;
	const frameHost =
		exporting === undefined
			? null
			: createElement(PrintFrameHost, {
					key: exporting.attempt,
					reports: exporting.printing.map(printedReportOf),
					tell: tellFor(exporting),
				});

	return {
		exportPdf,
		exportOne,
		preparing: isPreparing,
		preparingId: single.preparingId,
		frame: createElement(Fragment, null, frameHost, dialog, single.frame),
	};
};
