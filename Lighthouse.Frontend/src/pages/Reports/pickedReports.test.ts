import { describe, expect, it } from "vitest";
import {
	aReportOn,
	moreReportsThanTheCeiling,
	ZENITH,
} from "../../tests/BulkReportsTestKit";
import { pickedReports, selectedSentence } from "./pickedReports";

describe("The reports picked", () => {
	it("returns every listed report when every id is picked", () => {
		const { reports } = moreReportsThanTheCeiling(ZENITH, 51);

		const picked = pickedReports(
			reports,
			reports.map((report) => report.id),
		);

		expect(picked).toEqual(reports);
		expect(selectedSentence(picked.length)).toBe("101 selected");
	});

	it("come in the order they were picked, not the order they are listed", () => {
		const first = aReportOn(ZENITH, "First");
		const second = aReportOn(ZENITH, "Second");

		expect(pickedReports([first, second], [second.id, first.id])).toEqual([
			second,
			first,
		]);
	});

	it("leave out an id that is not listed any more", () => {
		const listed = aReportOn(ZENITH);

		expect(pickedReports([listed], [listed.id, 987_654])).toEqual([listed]);
	});

	it("are none while the list has not arrived", () => {
		expect(pickedReports(undefined, [1, 2])).toEqual([]);
	});

	it("are none when nothing is picked", () => {
		expect(pickedReports([aReportOn(ZENITH)], [])).toEqual([]);
	});
});

describe("What the page says about the picked count", () => {
	it.each([
		[0, "0 selected"],
		[3, "3 selected"],
		[101, "101 selected"],
	])("for %i picked", (picked, sentence) => {
		expect(selectedSentence(picked)).toBe(sentence);
	});
});
