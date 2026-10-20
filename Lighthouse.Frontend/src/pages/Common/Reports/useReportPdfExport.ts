import type React from "react";
import { createElement, useCallback, useRef, useState } from "react";
import { useNavigate } from "react-router";
import type { IReport, IReportOwner } from "../../../models/Reports/Report";
import { UsageDataEventName } from "../../../services/Api/UsageDataService";
import {
	usageDataReportTemplateOf,
	useUsageDataReporter,
} from "../../../services/UsageData/usageDataReporter";
import PrintFrameHost from "./print/PrintFrameHost";
import { printRequestFor } from "./print/printRequest";
import type { PrintOrigin, PrintOutcome } from "./print/usePrintOnce";
import { reportPathOf } from "./reportOwner";

type Exported = Pick<IReport, "id" | "templateKey">;

/** Counts one export with the report's Type, before anything is made for it; a Type usage data has no word for counts nothing. */
const useExportCounter = () => {
	const reportUsage = useUsageDataReporter();
	return useCallback(
		(report: Exported) => {
			const template = usageDataReportTemplateOf(report.templateKey);
			if (template !== undefined) {
				reportUsage({
					name: UsageDataEventName.ReportPdfExportStarted,
					reportTemplate: template,
				});
			}
		},
		[reportUsage],
	);
};

export interface ReportPdfExport {
	/** Prints the report from a hidden frame on this page; a second choice while one is prepared does nothing. */
	readonly exportPdf: (report: Exported) => void;
	/** The report whose PDF is being prepared, until its print dialog has been asked for. */
	readonly preparingId: number | undefined;
	/** The hidden frame the report is printed from, while there is one. */
	readonly frame: React.ReactNode;
}

interface Exporting {
	readonly reportId: number;
	/** Each export draws into a frame of its own, even for the same report. */
	readonly attempt: number;
}

/**
 * Exports one of an owner's reports as a PDF without the reader leaving the page they are on: the export is counted,
 * then the report is printed from a hidden frame, which goes again a turn after its dialog closes.
 */
export const useReportPdfExport = (owner: IReportOwner): ReportPdfExport => {
	const countTheExport = useExportCounter();
	const attempts = useRef(0);
	const [exporting, setExporting] = useState<Exporting | undefined>();
	const [preparingId, setPreparingId] = useState<number | undefined>();

	const exportPdf = (report: Exported) => {
		if (preparingId !== undefined) return;
		countTheExport(report);
		attempts.current += 1;
		setPreparingId(report.id);
		setExporting({ reportId: report.id, attempt: attempts.current });
	};

	// Never inside the dialog's own closing: some browsers say it closed before print() has returned.
	const removeTheFrameOf = (attempt: number) =>
		setTimeout(() =>
			setExporting((current) =>
				current?.attempt === attempt ? undefined : current,
			),
		);

	const tellFor =
		(attempt: number) =>
		(outcome: PrintOutcome): void => {
			setPreparingId(undefined);
			if (outcome.kind !== "printing") removeTheFrameOf(attempt);
		};

	const frame =
		exporting === undefined
			? null
			: createElement(PrintFrameHost, {
					key: exporting.attempt,
					owner,
					reportId: exporting.reportId,
					tell: tellFor(exporting.attempt),
				});

	return { exportPdf, preparingId, frame };
};

/**
 * Starts a report's PDF export from the Reports list: the export is counted with the report's Type, then the reader
 * goes to the report's print page in the same window. The wish to print travels in the history entry, never in the
 * address, so a pasted link never prints.
 */
export const useReportPdfExportByNavigating = () => {
	const navigate = useNavigate();
	const countTheExport = useExportCounter();

	return useCallback(
		(owner: IReportOwner, report: Exported, from: PrintOrigin) => {
			countTheExport(report);
			navigate(`/print${reportPathOf(owner, report.id)}`, {
				state: printRequestFor(from),
			});
		},
		[navigate, countTheExport],
	);
};
