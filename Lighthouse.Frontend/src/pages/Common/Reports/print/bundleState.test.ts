import { describe, expect, it } from "vitest";
import { pendingEpic6350 } from "../../../../tests/BulkReportsTestKit";
import { type BundlePartState, bundleStateOf } from "./bundleState";

/**
 * A PDF of several reports prints only once every report is ready. Any report that failed stops it, and the
 * look names every one that failed, in the order they were picked; until then it is preparing.
 */

const pending = pendingEpic6350("07");

const parts = (...states: BundlePartState[]) =>
	states.map((state, index) => ({ reportId: index + 1, state }));

describe("How far a PDF of several reports has got", () => {
	it.skip.each([
		[["ready", "ready"], { look: "ready", failedReportIds: [] }],
		[["ready", "preparing"], { look: "preparing", failedReportIds: [] }],
		[["preparing", "preparing"], { look: "preparing", failedReportIds: [] }],
		[["ready", "failed"], { look: "failed", failedReportIds: [2] }],
		[["failed", "preparing"], { look: "failed", failedReportIds: [1] }],
		[
			["failed", "ready", "failed"],
			{ look: "failed", failedReportIds: [1, 3] },
		],
	] as const)(`%j reads as %j (${pending})`, (states, expected) => {
		expect(bundleStateOf(parts(...states))).toEqual(expected);
	});

	it.skip(`a bundle of no reports stays preparing and never prints; the page cannot ask for one, since Export PDF needs a selection (${pending})`, () => {
		expect(bundleStateOf([])).toEqual({
			look: "preparing",
			failedReportIds: [],
		});
	});
});
