import { describe, expect, it } from "vitest";
import type { IReportOwner } from "../../models/Reports/Report";
import {
	aReportOn,
	GRAVITY,
	MONTH,
	OCEAN_EXPLORER,
	QUARTER,
	VOYAGER,
	ZENITH,
} from "../../tests/BulkReportsTestKit";
import type { PrintedReport } from "../Common/Reports/print/bundleState";
import {
	failedAmong,
	isTheSameReport,
	leaveOutTheFailed,
} from "./printFailures";

/**
 * What the reader is told when a combined PDF fails, and what Leave it out prints: which of the picked reports failed,
 * and which are still printed when the failed ones are left out.
 */

const zenithsQuarter = aReportOn(ZENITH, QUARTER, { id: 901 });
const zenithsMonth = aReportOn(ZENITH, MONTH, { id: 905 });
const gravitysMonth = aReportOn(GRAVITY, MONTH, { id: 902 });
const voyagersQuarter = aReportOn(VOYAGER, QUARTER, { id: 903 });

const failedReportOf = (
	owner: IReportOwner,
	reportId: number,
): PrintedReport => ({ owner, reportId });

const teamOwner = (id: number): IReportOwner => ({ kind: "team", id });

describe("which of the picked reports failed", () => {
	it("names only the failed reports, in the order the reports are listed", () => {
		const listed = [zenithsQuarter, gravitysMonth, voyagersQuarter];
		const failed = [
			failedReportOf(teamOwner(VOYAGER.id), voyagersQuarter.id),
			failedReportOf(teamOwner(ZENITH.id), zenithsQuarter.id),
		];

		expect(failedAmong(listed, failed)).toEqual([
			zenithsQuarter,
			voyagersQuarter,
		]);
	});

	it("names none when nothing failed", () => {
		expect(failedAmong([zenithsQuarter, gravitysMonth], [])).toEqual([]);
	});

	it("matches a failed report by its id and by its owner together", () => {
		const sameIdOnAnotherTeam = aReportOn(GRAVITY, QUARTER, {
			id: zenithsQuarter.id,
		});
		const sameIdOnAPortfolio = aReportOn(OCEAN_EXPLORER, QUARTER, {
			id: zenithsQuarter.id,
			ownerId: ZENITH.id,
		});
		const sameTeamOtherId = aReportOn(ZENITH, MONTH, { id: 906 });
		const listed = [
			sameIdOnAnotherTeam,
			zenithsQuarter,
			sameIdOnAPortfolio,
			sameTeamOtherId,
		];

		expect(
			failedAmong(listed, [
				failedReportOf(teamOwner(ZENITH.id), zenithsQuarter.id),
			]),
		).toEqual([zenithsQuarter]);
	});

	it("takes a report for the same report only when its id, its owner kind and its owner id all agree", () => {
		const failed = failedReportOf(teamOwner(ZENITH.id), zenithsQuarter.id);

		expect(isTheSameReport(zenithsQuarter, failed)).toBe(true);
		expect(isTheSameReport(zenithsMonth, failed)).toBe(false);
		expect(
			isTheSameReport(aReportOn(GRAVITY, QUARTER, { id: 901 }), failed),
		).toBe(false);
		expect(
			isTheSameReport({ ...zenithsQuarter, ownerKind: "portfolio" }, failed),
		).toBe(false);
	});
});

describe("what Leave it out prints", () => {
	it("prints the reports that did not fail, in their order, and leaves out every failed one", () => {
		const listed = [zenithsQuarter, gravitysMonth, voyagersQuarter];

		expect(
			leaveOutTheFailed(listed, [zenithsQuarter, voyagersQuarter]),
		).toEqual([gravitysMonth]);
		expect(leaveOutTheFailed(listed, [gravitysMonth])).toEqual([
			zenithsQuarter,
			voyagersQuarter,
		]);
	});

	it("is not offered when nothing failed", () => {
		expect(
			leaveOutTheFailed([zenithsQuarter, gravitysMonth], []),
		).toBeUndefined();
	});

	it("is not offered when leaving the failed reports out would leave nothing to print", () => {
		const listed = [zenithsQuarter, voyagersQuarter];

		expect(leaveOutTheFailed(listed, listed)).toBeUndefined();
	});

	it("leaves out only the report that failed, not the other report of the same Team", () => {
		const listed = [zenithsQuarter, zenithsMonth];
		const failed = failedAmong(listed, [
			failedReportOf(teamOwner(ZENITH.id), zenithsMonth.id),
		]);

		expect(leaveOutTheFailed(listed, failed)).toEqual([zenithsQuarter]);
	});
});
