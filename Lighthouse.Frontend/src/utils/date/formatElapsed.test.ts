import { describe, expect, it } from "vitest";
import { formatElapsed } from "./formatElapsed";

/**
 * DISTILL specifications (Epic #5511 — Task Manager), slice 03 / #5841. AC-03.5: the row reads as a
 * duration rather than a timestamp.
 *
 * The unit is chosen per value rather than per list. These rows are independent of one another, and an
 * operator deciding whether to wait or to intervene is reading one of them — "0.003 d" is a correct
 * answer to a question nobody asked.
 */
describe("formatElapsed", () => {
	it("says seconds for something that has only just started", () => {
		expect(formatElapsed(12_000)).toBe("12s");
	});

	it("says minutes once seconds stop being the useful unit", () => {
		expect(formatElapsed(3 * 60_000)).toBe("3m");
	});

	it("keeps the minutes when it reaches hours, because that is the part that is still moving", () => {
		expect(formatElapsed(64 * 60_000)).toBe("1h 4m");
	});

	it("says days and hours for a refresh that has been going far too long", () => {
		expect(formatElapsed(51 * 60 * 60_000)).toBe("2d 3h");
	});

	// A refresh admitted in the same tick the list was read is a real case, and "just started" is the
	// honest answer to it. It must not come out blank, which would read as a row with no duration at all.
	it("says a duration rather than nothing for work admitted a moment ago", () => {
		expect(formatElapsed(0)).toBe("0s");
	});

	// The server clamps a skewed moment to zero, so this should not arrive. A formatter that renders it
	// as "-3s" turns somebody else's bug into a row that reads as broken rather than as new.
	it("never renders a negative duration, whatever it is handed", () => {
		expect(formatElapsed(-3_000)).toBe("0s");
	});

	// Rounds rather than truncates at the boundary: 59.6 seconds shown as "59s" and then as "1m" a tick
	// later is fine, but shown as "0m" is not.
	it("does not round a value down into a unit that reads as nothing", () => {
		expect(formatElapsed(59_600)).not.toBe("0m");
	});
});
