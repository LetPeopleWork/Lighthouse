import { describe, expect, it } from "vitest";
import boundaries from "../../../../tests/reportWindowBoundaries.json";
import {
	type EnteredReportWindow,
	type ReportWindowBounds,
	type ReportWindowRefusal,
	reportWindowCheck,
} from "./reportWindowCheck";

/**
 * The wizard refuses a window before anything is sent, but the server stays the rule's only authority. Both
 * read the same table of edges - one day short of the shortest window, the last day today and tomorrow, the
 * first day on the oldest kept day and the day before, and an owner that keeps every finished item and so
 * has no oldest day - so the browser's check cannot drift from the server's without one of the two suites
 * going red. The backend runs the same rows through the create endpoint.
 */

interface BoundaryRow {
	case: string;
	side: "then" | "now";
	end?: string;
	lengthDays: number;
	keepsEveryFinishedItem?: boolean;
	refusal: ReportWindowRefusal | null;
}

const rows = boundaries.rows as BoundaryRow[];

const entered = (row: BoundaryRow): EnteredReportWindow =>
	row.side === "then"
		? { side: "then", end: row.end ?? "", lengthDays: row.lengthDays }
		: { side: "now", lengthDays: row.lengthDays };

const boundsFor = (row: BoundaryRow): ReportWindowBounds =>
	row.keepsEveryFinishedItem
		? { ...boundaries.bounds, earliestStart: null }
		: boundaries.bounds;

const answer = (refusal: ReportWindowRefusal | null) => refusal ?? "accepted";

describe("the edges of a report window, checked as the server checks them", () => {
	// @us-02 @slice-02 @boundary @contract-shape:pure-function
	it.each(rows.filter((row) => row.side === "then"))(
		"answers $case as the server does",
		(row) => {
			expect(answer(reportWindowCheck(entered(row), boundsFor(row)))).toBe(
				answer(row.refusal),
			);
		},
	);

	// @us-07 @slice-07 @boundary @contract-shape:pure-function
	it.skip.each(rows.filter((row) => row.side === "now"))(
		"answers $case as the server does",
		(row) => {
			expect(answer(reportWindowCheck(entered(row), boundsFor(row)))).toBe(
				answer(row.refusal),
			);
		},
	);
});
