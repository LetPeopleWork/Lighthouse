import { describe, expect, it } from "vitest";
import type { DayOfWeek } from "../../../models/RecurringBlackoutRule";
import { cadenceErrors, cadenceOf } from "./RefinementCadenceSettings";

describe("What the Refinement cadence fields save as", () => {
	it.each<{
		case: string;
		weekdays: DayOfWeek[];
		intervalWeeks: number;
		anchorWeek: string | null;
		saved: {
			weekdays: DayOfWeek[];
			intervalWeeks: number;
			anchorWeek: string | null;
		};
	}>([
		{
			case: "one weekday every week names no starting week",
			weekdays: ["Thursday"],
			intervalWeeks: 1,
			anchorWeek: null,
			saved: { weekdays: ["Thursday"], intervalWeeks: 1, anchorWeek: null },
		},
		{
			case: "a starting week left over from a longer repeat is dropped at every week",
			weekdays: ["Tuesday"],
			intervalWeeks: 1,
			anchorWeek: "2026-10-05",
			saved: { weekdays: ["Tuesday"], intervalWeeks: 1, anchorWeek: null },
		},
		{
			case: "weekdays ticked in any order save Monday to Sunday",
			weekdays: ["Sunday", "Thursday", "Monday"],
			intervalWeeks: 1,
			anchorWeek: null,
			saved: {
				weekdays: ["Monday", "Thursday", "Sunday"],
				intervalWeeks: 1,
				anchorWeek: null,
			},
		},
		{
			case: "every second week keeps its starting week",
			weekdays: ["Tuesday"],
			intervalWeeks: 2,
			anchorWeek: "2026-10-05",
			saved: {
				weekdays: ["Tuesday"],
				intervalWeeks: 2,
				anchorWeek: "2026-10-05",
			},
		},
		{
			case: "no weekday ticked saves no weekdays",
			weekdays: [],
			intervalWeeks: 1,
			anchorWeek: null,
			saved: { weekdays: [], intervalWeeks: 1, anchorWeek: null },
		},
	])("$case", ({ weekdays, intervalWeeks, anchorWeek, saved }) => {
		expect(cadenceOf(weekdays, intervalWeeks, anchorWeek)).toEqual(saved);
	});
});

describe("Which Refinement cadence fields are marked invalid", () => {
	it.each<{
		case: string;
		intervalWeeks: number;
		anchorWeek: string | null;
		invalid: { intervalWeeks: boolean; anchorWeek: boolean };
	}>([
		{
			case: "every week needs no starting week",
			intervalWeeks: 1,
			anchorWeek: null,
			invalid: { intervalWeeks: false, anchorWeek: false },
		},
		{
			case: "every second week with a starting week is complete",
			intervalWeeks: 2,
			anchorWeek: "2026-10-05",
			invalid: { intervalWeeks: false, anchorWeek: false },
		},
		{
			case: "every second week without a starting week marks the starting week",
			intervalWeeks: 2,
			anchorWeek: null,
			invalid: { intervalWeeks: false, anchorWeek: true },
		},
		{
			case: "fewer than one week apart marks the weeks",
			intervalWeeks: 0,
			anchorWeek: null,
			invalid: { intervalWeeks: true, anchorWeek: false },
		},
		{
			case: "an emptied number of weeks marks the weeks",
			intervalWeeks: Number.NaN,
			anchorWeek: "2026-10-05",
			invalid: { intervalWeeks: true, anchorWeek: false },
		},
	])("$case", ({ intervalWeeks, anchorWeek, invalid }) => {
		expect(
			cadenceErrors({ weekdays: ["Tuesday"], intervalWeeks, anchorWeek }),
		).toEqual(invalid);
	});
});
