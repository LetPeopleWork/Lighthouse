import { describe, expect, it } from "vitest";
import {
	DEFAULT_DISCUSS_WHEN,
	initialRememberedThresholds,
	isUsableThreshold,
	rememberOnSwitchOff,
	thresholdAfterToggle,
	withThreshold,
} from "./discussionRules";

describe("the threshold a discussion rule returns to", () => {
	it("starts at the threshold each rule has now", () => {
		expect(initialRememberedThresholds({ no: 4, yesIf: 3 })).toEqual({
			no: 4,
			yesIf: 3,
		});
	});

	it("starts a rule that is stored as off at its default", () => {
		expect(initialRememberedThresholds({ no: null, yesIf: null })).toEqual({
			no: 1,
			yesIf: 2,
		});
		expect(DEFAULT_DISCUSS_WHEN).toEqual({ no: 1, yesIf: 2 });
	});

	it("remembers the threshold a rule was changed to when it is switched off", () => {
		const remembered = rememberOnSwitchOff({ no: 1, yesIf: 2 }, "no", 3);

		expect(remembered).toEqual({ no: 3, yesIf: 2 });
		expect(thresholdAfterToggle(remembered, "no", true)).toBe(3);
	});

	it("keeps the last allowed threshold when the rule is switched off at one that is refused", () => {
		const last = rememberOnSwitchOff({ no: 1, yesIf: 2 }, "no", 4);

		expect(rememberOnSwitchOff(last, "no", 0)).toEqual({ no: 4, yesIf: 2 });
		expect(rememberOnSwitchOff(last, "no", Number.NaN)).toEqual({
			no: 4,
			yesIf: 2,
		});
		expect(rememberOnSwitchOff(last, "no", -2)).toEqual({ no: 4, yesIf: 2 });
	});

	it("keeps what it remembered when a rule that is already off is switched off again", () => {
		expect(rememberOnSwitchOff({ no: 4, yesIf: 2 }, "no", null)).toEqual({
			no: 4,
			yesIf: 2,
		});
	});

	it("leaves the other rule's memory alone", () => {
		expect(rememberOnSwitchOff({ no: 1, yesIf: 5 }, "no", 3)).toEqual({
			no: 3,
			yesIf: 5,
		});
	});

	it("switches a rule off to no threshold, and on to the remembered one", () => {
		const remembered = { no: 4, yesIf: 2 };

		expect(thresholdAfterToggle(remembered, "no", false)).toBeNull();
		expect(thresholdAfterToggle(remembered, "yesIf", true)).toBe(2);
	});
});

describe("which thresholds a discussion rule accepts", () => {
	it.each([
		[1, true],
		[7, true],
		[0, false],
		[-1, false],
		[Number.NaN, false],
		[1.5, false],
	])("%s is usable: %s", (threshold, usable) => {
		expect(isUsableThreshold(threshold)).toBe(usable);
	});
});

describe("setting one discussion rule's threshold", () => {
	it("changes that rule and leaves the other as it was", () => {
		const before = { no: 1, yesIf: 2 };

		expect(withThreshold(before, "yesIf", null)).toEqual({
			no: 1,
			yesIf: null,
		});
		expect(before).toEqual({ no: 1, yesIf: 2 });
	});
});
