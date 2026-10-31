import type { IReportOwner } from "../../../../models/Reports/Report";

/** One report to print, by its owner and id. */
export interface PrintedReport {
	readonly owner: IReportOwner;
	readonly reportId: number;
}

/**
 * How far one report in a PDF of several has got: still preparing, ready, the report itself could not be read, or it
 * was read but a part of its page was not.
 */
export type BundlePartState = "preparing" | "ready" | "failed" | "failedPart";

export interface IBundlePart extends PrintedReport {
	readonly state: BundlePartState;
}

/**
 * Where the whole PDF stands. A failure waits until no report is still preparing, so every report that could not be
 * read is named at once, in the given order. The PDF fails as the first of them, or, when every report was read, as
 * the first whose page had a part that failed.
 */
export type IBundleState<P extends IBundlePart = IBundlePart> =
	| { readonly look: "preparing" }
	| { readonly look: "ready" }
	| {
			readonly look: "failed";
			readonly failedReports: readonly PrintedReport[];
			readonly failingAs: P;
	  };

const printedReportOf = ({
	owner,
	reportId,
}: PrintedReport): PrintedReport => ({
	owner,
	reportId,
});

export const bundleStateOf = <P extends IBundlePart>(
	parts: readonly P[],
): IBundleState<P> => {
	if (parts.length === 0 || parts.some((part) => part.state === "preparing")) {
		return { look: "preparing" };
	}
	const failingAs =
		parts.find((part) => part.state === "failed") ??
		parts.find((part) => part.state === "failedPart");
	if (failingAs === undefined) return { look: "ready" };
	return {
		look: "failed",
		failedReports: parts
			.filter((part) => part.state === "failed")
			.map(printedReportOf),
		failingAs,
	};
};
