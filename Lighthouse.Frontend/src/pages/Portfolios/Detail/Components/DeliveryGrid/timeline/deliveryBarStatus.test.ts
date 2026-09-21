import { describe, expect, it } from "vitest";
import { buildDeliveryBarCaps } from "./deliveryBarStatus";
import type { TimelineBar } from "./deliveryTimelineModel";

const october = (day: number, hour = 0) => new Date(2026, 9, day, hour);

/**
 * The target as the backend stores it: an instant this product reads as a UTC day.
 *
 * Late in the UTC day on purpose. These tests run in Europe/Zurich, where that instant falls on the
 * *following* local day - which is the asymmetry the whole comparison has to survive, and which a
 * fixture built from a local midnight would hide.
 */
const targetOn = (day: number) => new Date(Date.UTC(2026, 9, day, 22, 30));

const bar = (overrides: Partial<TimelineBar> = {}): TimelineBar => ({
	featureId: 1,
	name: "Deep Sea Mapping Initiative",
	start: october(10),
	end: october(20),
	startIsObserved: false,
	endIsObserved: false,
	...overrides,
});

const capsFor = (one: TimelineBar, targetDate?: Date) =>
	buildDeliveryBarCaps([one], targetDate).get(one.featureId);

describe("which end of a bar crossed the target", () => {
	it("marks the end of a bar that finishes after the date, and not its start", () => {
		// Both halves. The second is what stops this passing against a module that marks
		// everything it is handed: this bar begins two weeks before the date.
		const caps = capsFor(
			bar({ start: october(10), end: october(25) }),
			targetOn(20),
		);

		expect(caps?.end).toBe("endsAfterTarget");
		expect(caps?.start).toBeUndefined();
	});

	it("marks both ends of a bar that has not begun by the date", () => {
		// The precedence this replaced would have marked the start only, on the grounds that a
		// bar which has not been reached is not merely late. It is both, and it says both.
		const caps = capsFor(
			bar({ start: october(22), end: october(28) }),
			targetOn(20),
		);

		expect(caps?.start).toBe("startsAfterTarget");
		expect(caps?.end).toBe("endsAfterTarget");
	});

	it("leaves a bar that fits alone, on a chart where another does not", () => {
		// Asserted in the same call as a bar that *is* marked. On its own an absence passes
		// against a module that returns nothing at all.
		const caps = buildDeliveryBarCaps(
			[
				bar({ featureId: 1, start: october(5), end: october(15) }),
				bar({ featureId: 2, start: october(5), end: october(25) }),
			],
			targetOn(20),
		);

		expect(caps.get(1)).toBeUndefined();
		expect(caps.get(2)?.end).toBe("endsAfterTarget");
	});

	it("treats the target day itself as in time, and the day after as not", () => {
		// The boundary the whole slice turns on, and the quietest mutation available: `>=` for `>`
		// moves every bar that lands exactly on the date into the late column. Both sides of it
		// here, so a module that never marks and a module that always marks both fail.
		expect(
			capsFor(bar({ end: october(20) }), targetOn(20))?.end,
		).toBeUndefined();
		expect(capsFor(bar({ end: october(21) }), targetOn(20))?.end).toBe(
			"endsAfterTarget",
		);
	});

	it("treats a bar beginning on the target day as begun in time", () => {
		// The same boundary at the other end, where an off-by-one is likelier because the start
		// test is the one written second. Its end is marked, because it ends on a later day - that
		// is a later *day* and not a later hour, which is the next test's question.
		const caps = capsFor(
			bar({ start: october(20), end: october(21) }),
			targetOn(20),
		);

		expect(caps?.start).toBeUndefined();
		expect(caps?.end).toBe("endsAfterTarget");
	});

	it("asks which day, never which hour", () => {
		// Two probes, because there are two ways to get this wrong and each survives the other's
		// test.
		//
		// A bar ending late *on* the target day is in time. Comparing the bar's instant against the
		// reduced target marks it, because eleven at night is after midnight.
		expect(
			capsFor(bar({ end: october(20, 23) }), targetOn(20))?.end,
		).toBeUndefined();

		// And a bar ending at midnight on the day *after* is late. Comparing raw instants misses
		// it: the stored target is late in the UTC day, which in this timezone is half an hour into
		// the following local day, so the bar lands before it and reads as in time.
		expect(capsFor(bar({ end: october(21, 0) }), targetOn(20))?.end).toBe(
			"endsAfterTarget",
		);
	});
});

describe("a bar that has already finished", () => {
	it("says so at both ends, whatever the date says", () => {
		// Three cases. A module that treats "done" as a verdict that outranks "late" fails the
		// second; one that looks at the target before asking whether the work is over fails the
		// third.
		const finished = bar({ endIsObserved: true });

		expect(capsFor(finished, targetOn(25))).toEqual({
			start: "finished",
			end: "finished",
		});
		expect(capsFor(finished, targetOn(1))).toEqual({
			start: "finished",
			end: "finished",
		});
		expect(capsFor(finished, undefined)).toEqual({
			start: "finished",
			end: "finished",
		});
	});
});

describe("a Delivery with no date to be late against", () => {
	it("marks what has finished and nothing else", () => {
		// Both halves, and the criterion has no falsifier without them: a module that returns an
		// empty map whenever there is no target satisfies the first on its own and is wrong.
		const caps = buildDeliveryBarCaps(
			[
				bar({ featureId: 1, start: october(30), end: october(31) }),
				bar({ featureId: 2, endIsObserved: true }),
			],
			undefined,
		);

		expect(caps.get(1)).toBeUndefined();
		expect(caps.get(2)?.end).toBe("finished");
	});
});

describe("the shape of the answer", () => {
	it("keeps each bar's verdict under its own Feature", () => {
		// Keyed by Feature rather than by position, which is right until the board is re-ordered
		// and then silently wrong for every reader.
		const caps = buildDeliveryBarCaps(
			[
				bar({ featureId: 11, start: october(22), end: october(28) }),
				bar({ featureId: 22, start: october(5), end: october(25) }),
				bar({ featureId: 33, endIsObserved: true }),
			],
			targetOn(20),
		);

		expect(caps.get(11)?.start).toBe("startsAfterTarget");
		expect(caps.get(22)?.start).toBeUndefined();
		expect(caps.get(33)?.start).toBe("finished");
	});

	it("leaves out a bar with nothing to say, rather than handing over an empty one", () => {
		// An entry with no ends in it is drawn as a mark with nothing behind it. Paired with a bar
		// that does belong, so this cannot pass against a module that returns an empty map.
		const caps = buildDeliveryBarCaps(
			[
				bar({ featureId: 1, start: october(5), end: october(15) }),
				bar({ featureId: 2, start: october(5), end: october(25) }),
			],
			targetOn(20),
		);

		expect(caps.has(1)).toBe(false);
		expect(caps.has(2)).toBe(true);
	});
});
