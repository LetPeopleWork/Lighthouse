import { Box } from "@mui/material";
import type React from "react";
import { useCallback, useEffect, useMemo, useState } from "react";
import {
	type BundlePartState,
	bundleStateOf,
	type PrintedReport,
} from "./bundleState";
import type { PrintPageState } from "./printPageState";
import type { PrintReadPort } from "./printReadPort";
import ReportPrintContent, {
	type PrintContentEffects,
	type PrintPageLook,
	type PrintTerms,
} from "./ReportPrintContent";

/** The name the PDF of several reports is offered under. */
export const SEVERAL_REPORTS_TITLE = "Lighthouse reports";

const PREPARING_LOOK: PrintPageLook = {
	state: "preparing",
	failure: { retry: false },
};

const READY_LOOK: PrintPageLook = { state: "ready", failure: { retry: false } };

const ON_A_PAGE_OF_ITS_OWN = { breakBefore: "page" };

const partStateOf = (state: PrintPageState): BundlePartState => {
	if (state === "ready" || state === "preparing") return state;
	return "failed";
};

const keyOf = ({ owner, reportId }: PrintedReport) =>
	`${owner.kind}-${owner.id}-${reportId}`;

interface PartProps {
	readonly partKey: string;
	readonly look: PrintPageLook;
	readonly tell: (partKey: string, look: PrintPageLook) => void;
	readonly children: React.ReactNode;
}

/** Passes on where one report stands once it has drawn, since the bundle cannot be changed while a report draws. */
const Part: React.FC<PartProps> = ({ partKey, look, tell, children }) => {
	const { state, failure } = look;
	useEffect(() => {
		tell(partKey, {
			state,
			failure: { message: failure.message, retry: failure.retry },
		});
	}, [partKey, state, failure.message, failure.retry, tell]);
	return <>{children}</>;
};

const isAReportReadFailure = (state: PrintPageState): boolean =>
	state === "failedRetryable" || state === "failedFinal";

/**
 * Where the whole PDF stands: ready when every report is. A failure waits until no report is still preparing, so the
 * reader is told of every report that could not be read, in the given order, all at once, and the PDF takes on the
 * first one's look. With no report unread, it fails as the first part of the page that failed.
 */
const bundleLookOf = (
	reports: readonly PrintedReport[],
	looks: Readonly<Record<string, PrintPageLook>>,
): PrintPageLook => {
	const partLooks = reports.map(
		(report) => looks[keyOf(report)] ?? PREPARING_LOOK,
	);
	const bundle = bundleStateOf(
		reports.map((report, index) => ({
			reportId: report.reportId,
			state: partStateOf(partLooks[index].state),
		})),
	);
	if (bundle.look === "ready") return READY_LOOK;
	if (partLooks.some((look) => look.state === "preparing"))
		return PREPARING_LOOK;
	const failedReports = reports.filter((_, index) =>
		isAReportReadFailure(partLooks[index].state),
	);
	const firstFailed =
		partLooks.find((look) => isAReportReadFailure(look.state)) ??
		partLooks.find((look) => partStateOf(look.state) === "failed");
	if (firstFailed === undefined) return PREPARING_LOOK;
	if (failedReports.length === 0) return firstFailed;
	return {
		state: firstFailed.state,
		failure: { ...firstFailed.failure, failedReports },
	};
};

interface ReportPrintBundleProps {
	readonly reports: readonly PrintedReport[];
	readonly reads: PrintReadPort;
	readonly terms: PrintTerms;
	readonly effects: PrintContentEffects;
	readonly timedOut?: boolean;
	/** The one sheet every report is drawn on, told where the whole PDF stands. */
	readonly renderSheet: (
		look: PrintPageLook,
		reports: React.ReactNode,
	) => React.ReactNode;
}

/**
 * Several reports as one document, in the given order, each from a page of its own with its own running line and
 * footer. A single report keeps its own title; several share one, so the PDF is not named after whichever drew last.
 */
const ReportPrintBundle: React.FC<ReportPrintBundleProps> = ({
	reports,
	reads,
	terms,
	effects,
	timedOut = false,
	renderSheet,
}) => {
	const [looks, setLooks] = useState<Readonly<Record<string, PrintPageLook>>>(
		{},
	);
	const several = reports.length > 1;
	const { setTitle } = effects;
	const contentEffects: PrintContentEffects = useMemo(
		() => (several ? { ...effects, setTitle: () => () => undefined } : effects),
		[several, effects],
	);

	useEffect(() => {
		if (several) return setTitle(SEVERAL_REPORTS_TITLE);
	}, [several, setTitle]);

	const tell = useCallback(
		(partKey: string, look: PrintPageLook) =>
			setLooks((current) => ({ ...current, [partKey]: look })),
		[],
	);

	const drawn = reports.map((report, index) => {
		const key = keyOf(report);
		return (
			<Box key={key} sx={index === 0 ? undefined : ON_A_PAGE_OF_ITS_OWN}>
				<ReportPrintContent
					owner={report.owner}
					reportId={report.reportId}
					reads={reads}
					terms={terms}
					effects={contentEffects}
					timedOut={timedOut}
					renderSheet={(look, drawnReport) => (
						<Part partKey={key} look={look} tell={tell}>
							{drawnReport}
						</Part>
					)}
				/>
			</Box>
		);
	});

	return renderSheet(bundleLookOf(reports, looks), drawn);
};

export default ReportPrintBundle;
