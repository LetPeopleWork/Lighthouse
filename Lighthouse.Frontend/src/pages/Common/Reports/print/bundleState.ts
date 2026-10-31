/** How far one report in a PDF of several has got. */
export type BundlePartState = "preparing" | "ready" | "failed";

export interface IBundlePart {
	reportId: number;
	state: BundlePartState;
}

/** The PDF prints only once every report is ready; any report that failed stops it and is named. */
export interface IBundleState {
	look: BundlePartState;
	failedReportIds: number[];
}

export const bundleStateOf = (parts: readonly IBundlePart[]): IBundleState => {
	const failedReportIds = parts
		.filter((part) => part.state === "failed")
		.map((part) => part.reportId);
	if (failedReportIds.length > 0) {
		return { look: "failed", failedReportIds };
	}
	const everyReady =
		parts.length > 0 && parts.every((part) => part.state === "ready");
	return { look: everyReady ? "ready" : "preparing", failedReportIds };
};
