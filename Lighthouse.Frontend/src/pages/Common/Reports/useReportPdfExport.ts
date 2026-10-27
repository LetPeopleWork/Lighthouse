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
	readonly exportPdf: (report: Exported, forOwner?: IReportOwner) => void;
	/** The report whose PDF is being prepared, until its print dialog has been asked for. */
	readonly preparingId: number | undefined;
	/** The hidden frame the report is printed from, while there is one. */
	readonly frame: React.ReactNode;
}

interface Exporting {
	readonly owner: IReportOwner;
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
export const useReportPdfExport = (
	owner: IReportOwner | undefined,
): ReportPdfExport => {
	const countTheExport = useExportCounter();
	const message = useExportMessage();
	const attempts = useRef(0);
	const [exporting, setExporting] = useState<Exporting | undefined>();
	const [preparingId, setPreparingId] = useState<number | undefined>();
	const frameRemoval = useRef<ReturnType<typeof setTimeout> | undefined>(
		undefined,
	);

	useEffect(() => () => clearTimeout(frameRemoval.current), []);

	// Retry lives in the snackbar, which goes as soon as it is chosen; without this a keyboard reader lands on the page.
	const exportStarter = useRef<HTMLElement | null>(null);

	// Retry finishes the export the reader already asked for, so it starts a fresh frame and counts nothing.
	const printFromAFreshFrame = (
		reportOwner: IReportOwner,
		reportId: number,
	) => {
		attempts.current += 1;
		setPreparingId(reportId);
		setExporting({
			owner: reportOwner,
			reportId,
			attempt: attempts.current,
		});
	};

	const exportPdf = (report: Exported, forOwner = owner) => {
		if (forOwner === undefined || preparingId !== undefined) return;
		message.takeBack();
		exportStarter.current =
			document.activeElement instanceof HTMLElement
				? document.activeElement
				: null;
		countTheExport(report);
		printFromAFreshFrame(forOwner, report.id);
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
		({ owner: reportOwner, reportId, attempt }: Exporting) =>
		(outcome: PrintOutcome): void => {
			setPreparingId(undefined);
			if (outcome.kind !== "printing") removeTheFrameOf(attempt);
			if (outcome.kind !== "failed") return;
			message.say(
				outcome.message,
				outcome.retry
					? {
							label: RETRY,
							onClick: () => {
								exportStarter.current?.focus();
								printFromAFreshFrame(reportOwner, reportId);
							},
						}
					: undefined,
			);
		};

	const frame =
		exporting === undefined
			? null
			: createElement(PrintFrameHost, {
					key: exporting.attempt,
					owner: exporting.owner,
					reportId: exporting.reportId,
					tell: tellFor(exporting),
				});

	return { exportPdf, preparingId, frame };
};
