import { describe, expect, it } from "vitest";
import type { IReportOwner } from "../../../models/Reports/Report";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { reportWindowPresetsFor } from "./panels/reportWindowPresets";
import { ownerTermKeyOf } from "./reportOwner";

const team: IReportOwner = { kind: "team", id: 7 };
const portfolio: IReportOwner = { kind: "portfolio", id: 7 };

describe("a report's owner", () => {
	it.each([
		[team, TERMINOLOGY_KEYS.TEAM],
		[portfolio, TERMINOLOGY_KEYS.PORTFOLIO],
	])("is named by the term for %j", (owner, termKey) => {
		expect(ownerTermKeyOf(owner)).toBe(termKey);
	});

	it.each([
		[team, [30, 90]],
		[portfolio, [90, 180]],
	])("offers %j its own window presets", (owner, presets) => {
		expect(reportWindowPresetsFor(owner)).toEqual(presets);
	});
});
