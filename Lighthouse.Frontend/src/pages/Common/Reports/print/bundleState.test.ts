import { describe, expect, it } from "vitest";
import { type BundlePartState, bundleStateOf } from "./bundleState";

/**
 * A PDF of several reports prints only once every report is ready, and is preparing while any report still is. Once
 * none is, a report that could not be read stops it, and the look names every one that could not be read, by owner
 * and report, in the order they were picked; the PDF fails as the first of them, or as the first report whose page
 * had a part that failed when every report was read.
 */

// Every report shares one id, as reports of different owners can, so a report is told apart by its owner too.
const reportOn = (team: number) => ({
	owner: { kind: "team" as const, id: team },
	reportId: 7,
});

const parts = (...states: BundlePartState[]) =>
	states.map((state, index) => ({ ...reportOn(index + 1), state }));

describe("How far a PDF of several reports has got", () => {
	it.each([
		[["ready", "ready"], { look: "ready" }],
		[["ready", "preparing"], { look: "preparing" }],
		[["preparing", "preparing"], { look: "preparing" }],
		[["failed", "preparing"], { look: "preparing" }],
		[
			["ready", "failed"],
			{
				look: "failed",
				failedReports: [reportOn(2)],
				failingAs: { ...reportOn(2), state: "failed" },
			},
		],
		[
			["failed", "ready", "failed"],
			{
				look: "failed",
				failedReports: [reportOn(1), reportOn(3)],
				failingAs: { ...reportOn(1), state: "failed" },
			},
		],
		[
			["failedPart", "failed"],
			{
				look: "failed",
				failedReports: [reportOn(2)],
				failingAs: { ...reportOn(2), state: "failed" },
			},
		],
		[
			["ready", "failedPart"],
			{
				look: "failed",
				failedReports: [],
				failingAs: { ...reportOn(2), state: "failedPart" },
			},
		],
	] as const)("%j reads as %j", (states, expected) => {
		expect(bundleStateOf(parts(...states))).toEqual(expected);
	});

	it("a bundle of no reports stays preparing and never prints; the page cannot ask for one, since Export PDF needs a selection", () => {
		expect(bundleStateOf([])).toEqual({ look: "preparing" });
	});
});
