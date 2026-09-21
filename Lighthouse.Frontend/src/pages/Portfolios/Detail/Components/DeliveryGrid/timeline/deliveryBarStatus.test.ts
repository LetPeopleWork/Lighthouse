import { describe, expect, it } from "vitest";
import { buildDeliveryBarStatuses } from "./deliveryBarStatus";
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

const statusOf = (one: TimelineBar, targetDate?: Date) =>
	buildDeliveryBarStatuses([one], targetDate).get(one.featureId);

describe("what a bar says about the target date", () => {
	it("says it finishes late when it ends after the date", () => {
		expect(statusOf(bar({ end: october(25) }), targetOn(20))).toBe(
			"endsAfterTarget",
		);
	});

	it("says it was never started in time when it begins after the date", () => {
		// Every bar that starts after the date also ends after it, so without this ranking the
		// sharper case would never be seen at all - and it is a different conversation, about what
		// the Delivery contains rather than about how fast anyone is going.
		expect(
			statusOf(bar({ start: october(22), end: october(28) }), targetOn(20)),
		).toBe("startsAfterTarget");
	});

	it("says nothing about a bar that fits, on a chart where another does not", () => {
		// Asserted in the same call as a bar that does have something to say. On its own an
		// absence passes against a module that returns nothing at all.
		const statuses = buildDeliveryBarStatuses(
			[
				bar({ featureId: 1, start: october(5), end: october(15) }),
				bar({ featureId: 2, start: october(5), end: october(25) }),
			],
			targetOn(20),
		);

		expect(statuses.get(1)).toBeUndefined();
		expect(statuses.get(2)).toBe("endsAfterTarget");
	});

	it("treats the target day itself as in time, and the day after as not", () => {
		// The boundary the whole slice turns on, and the quietest mutation available: `>=` for `>`
		// moves every bar that lands exactly on the date into the late column. Both sides of it
		// here, so a module that never marks and a module that always marks both fail.
		expect(statusOf(bar({ end: october(20) }), targetOn(20))).toBeUndefined();
		expect(statusOf(bar({ end: october(21) }), targetOn(20))).toBe(
			"endsAfterTarget",
		);
	});

	it("treats a bar beginning on the target day as begun in time", () => {
		// The same boundary at the other end, where an off-by-one is likelier because the start
		// test is the one written second. It still finishes late, because it ends on a later day -
		// a later *day* and not a later hour, which is the next test's question.
		expect(
			statusOf(bar({ start: october(20), end: october(21) }), targetOn(20)),
		).toBe("endsAfterTarget");
	});

	it("asks which day, never which hour", () => {
		// Two probes, because there are two ways to get this wrong and each survives the other's
		// test.
		//
		// A bar ending late *on* the target day is in time. Comparing the bar's instant against the
		// reduced target marks it, because eleven at night is after midnight.
		expect(
			statusOf(bar({ end: october(20, 23) }), targetOn(20)),
		).toBeUndefined();

		// And a bar ending at midnight on the day *after* is late. Comparing raw instants misses
		// it: the stored target is late in the UTC day, which in this timezone is half an hour into
		// the following local day, so the bar lands before it and reads as in time.
		expect(statusOf(bar({ end: october(21, 0) }), targetOn(20))).toBe(
			"endsAfterTarget",
		);
	});
});

describe("a bar that has already finished", () => {
	it("says so whatever the date says", () => {
		// Three cases. A module that ranks "done" below "late" fails the second; one that looks at
		// the target before asking whether the work is over fails the third.
		const finished = bar({ endIsObserved: true });

		expect(statusOf(finished, targetOn(25))).toBe("finished");
		expect(statusOf(finished, targetOn(1))).toBe("finished");
		expect(statusOf(finished, undefined)).toBe("finished");
	});

	it("says so even when it has not begun by the date either", () => {
		// The one case where both rankings are in play at once. Finished outranks everything, so
		// a module applying the start test first gets this wrong and only this test would know.
		const finishedLate = bar({
			start: october(22),
			end: october(28),
			endIsObserved: true,
		});

		expect(statusOf(finishedLate, targetOn(20))).toBe("finished");
	});
});

describe("a Delivery with no date to be late against", () => {
	it("speaks for what has finished and nothing else", () => {
		// Both halves, and the criterion has no falsifier without them: a module that returns an
		// empty map whenever there is no target satisfies the first on its own and is wrong.
		const statuses = buildDeliveryBarStatuses(
			[
				bar({ featureId: 1, start: october(30), end: october(31) }),
				bar({ featureId: 2, endIsObserved: true }),
			],
			undefined,
		);

		expect(statuses.get(1)).toBeUndefined();
		expect(statuses.get(2)).toBe("finished");
	});
});

describe("the shape of the answer", () => {
	it("keeps each bar's verdict under its own Feature", () => {
		// Keyed by Feature rather than by position, which is right until the board is re-ordered
		// and then silently wrong for every reader.
		const statuses = buildDeliveryBarStatuses(
			[
				bar({ featureId: 11, start: october(22), end: october(28) }),
				bar({ featureId: 22, start: october(5), end: october(25) }),
				bar({ featureId: 33, endIsObserved: true }),
			],
			targetOn(20),
		);

		expect(statuses.get(11)).toBe("startsAfterTarget");
		expect(statuses.get(22)).toBe("endsAfterTarget");
		expect(statuses.get(33)).toBe("finished");
	});

	it("leaves out a bar with nothing to say", () => {
		// On track is the ordinary case, and a colour worn by nearly every bar tells the reader
		// nothing about any of them. Paired with a bar that does belong, so this cannot pass
		// against a module that returns an empty map.
		const statuses = buildDeliveryBarStatuses(
			[
				bar({ featureId: 1, start: october(5), end: october(15) }),
				bar({ featureId: 2, start: october(5), end: october(25) }),
			],
			targetOn(20),
		);

		expect(statuses.has(1)).toBe(false);
		expect(statuses.has(2)).toBe(true);
	});
});
