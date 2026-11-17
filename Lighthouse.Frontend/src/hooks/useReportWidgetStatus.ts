import { createContext, useContext, useLayoutEffect } from "react";

export type WidgetStatus = "loading" | "error" | "ready";

/** Where a chart's own request stands, and whether it still shows something the frame can dim. */
export type ChartReport = {
	readonly status: WidgetStatus;
	readonly hasContentToDim: boolean;
};

/** How a chart that fetches for itself tells its frame where its own request stands. */
type ReportWidgetStatus = (report: ChartReport | undefined) => void;

export const WidgetStatusReporterContext =
	createContext<ReportWidgetStatus | null>(null);

// A layout effect lands before the browser paints, so a frame is never painted ready for a chart
// whose request has not started yet. Outside a frame there is nobody to tell.
export function useReportWidgetStatus(
	status: WidgetStatus,
	hasContentToDim = false,
): void {
	const report = useContext(WidgetStatusReporterContext);

	useLayoutEffect(() => {
		if (!report) return;
		report({ status, hasContentToDim });
		return () => report(undefined);
	}, [report, status, hasContentToDim]);
}
