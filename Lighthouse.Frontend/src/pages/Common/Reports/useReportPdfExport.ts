import type React from "react";
import { createElement, useCallback, useEffect, useRef, useState } from "react";
import {
	type SnackbarAction,
	type TakeTheMessageBack,
	useErrorSnackbar,
} from "../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import type { IReport, IReportOwner } from "../../../models/Reports/Report";
import { UsageDataEventName } from "../../../services/Api/UsageDataService";
import {
	usageDataReportTemplateOf,
	useUsageDataReporter,
} from "../../../services/UsageData/usageDataReporter";
import PrintFrameHost from "./print/PrintFrameHost";
import type { PrintOutcome } from "./print/usePrintOnce";

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

const RETRY = "Retry";

/** The export's message in the app's error snackbar: one at a time, and taken back when the page it belongs to goes. */
const useExportMessage = () => {
	const { showError } = useErrorSnackbar();
	const takeTheMessageBack = useRef<TakeTheMessageBack | undefined>(undefined);

	useEffect(() => () => takeTheMessageBack.current?.(), []);

	const takeBack = () => {
		takeTheMessageBack.current?.();
		takeTheMessageBack.current = undefined;
	};

	return {
		takeBack,
		say: (text: string, action: SnackbarAction | undefined) => {
			takeBack();
			takeTheMessageBack.current = showError(text, { action });
		},
	};
};

/**
 * Exports one of an owner's reports as a PDF without the reader leaving the page they are on: the export is counted,
 * then the report is printed from a hidden frame, which goes again a turn after its dialog closes. When no PDF can be
 * made the frame goes too, and the error snackbar says why, offering Retry where asking again may help.
 */
export const useReportPdfExport = (owner: IReportOwner): ReportPdfExport => {
	const countTheExport = useExportCounter();
	const message = useExportMessage();
	const attempts = useRef(0);
	const [exporting, setExporting] = useState<Exporting | undefined>();
	const [preparingId, setPreparingId] = useState<number | undefined>();

	// Retry finishes the export the reader already asked for, so it starts a fresh frame and counts nothing.
	const printFromAFreshFrame = (reportId: number) => {
		attempts.current += 1;
		setPreparingId(reportId);
		setExporting({ reportId, attempt: attempts.current });
	};

	const exportPdf = (report: Exported) => {
		if (preparingId !== undefined) return;
		message.takeBack();
		countTheExport(report);
		printFromAFreshFrame(report.id);
	};

	// Never inside the dialog's own closing: some browsers say it closed before print() has returned.
	const removeTheFrameOf = (attempt: number) =>
		setTimeout(() =>
			setExporting((current) =>
				current?.attempt === attempt ? undefined : current,
			),
		);

	const tellFor =
		({ reportId, attempt }: Exporting) =>
		(outcome: PrintOutcome): void => {
			setPreparingId(undefined);
			if (outcome.kind !== "printing") removeTheFrameOf(attempt);
			if (outcome.kind !== "failed") return;
			message.say(
				outcome.message,
				outcome.retry
					? { label: RETRY, onClick: () => printFromAFreshFrame(reportId) }
					: undefined,
			);
		};

	const frame =
		exporting === undefined
			? null
			: createElement(PrintFrameHost, {
					key: exporting.attempt,
					owner,
					reportId: exporting.reportId,
					tell: tellFor(exporting),
				});

	return { exportPdf, preparingId, frame };
};
