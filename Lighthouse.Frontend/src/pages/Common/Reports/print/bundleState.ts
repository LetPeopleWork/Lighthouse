export const __SCAFFOLD__ = true;

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

export const bundleStateOf = (_parts: readonly IBundlePart[]): IBundleState => {
	throw new Error("SCAFFOLD: the combined PDF is not built yet");
};
