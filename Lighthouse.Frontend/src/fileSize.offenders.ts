/**
 * Production files that are still over the size limit, listed by name. The list only shrinks:
 * an entry is deleted in the same change that brings its file under the limit.
 */
export interface TooWideEntry {
	/** Path relative to src. */
	path: string;
	/** Why the file still waits, in plain words. */
	reason: string;
}

export const TOO_WIDE_TODAY: TooWideEntry[] = [
	{
		path: "pages/Common/MetricsView/BaseMetricsView.tsx",
		reason: "One view holds the chart, the drill-down and the table together.",
	},
];
