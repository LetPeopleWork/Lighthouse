import { describe, expect, test } from "vitest";
import type { IWorkItem } from "../../models/WorkItem";
import { deriveStaleness, type StalenessCandidate } from "./deriveStaleness";

describe("deriveStaleness", () => {
	const now = new Date("2026-05-25T12:00:00Z");

	const itemEnteredAt = (
		enteredAt: string | null,
		overrides: Partial<IWorkItem> = {},
	): IWorkItem => ({
		id: 1,
		name: "Item",
		referenceId: "ITEM-1",
		url: null,
		state: "In Progress",
		stateCategory: "Doing",
		type: "Story",
		startedDate: new Date("2026-05-01T00:00:00Z"),
		closedDate: new Date("2026-05-01T00:00:00Z"),
		cycleTime: 0,
		workItemAge: 0,
		parentWorkItemReference: "",
		isBlocked: false,
		currentStateEnteredAt: enteredAt === null ? null : new Date(enteredAt),
		...overrides,
	});

	const mkCandidate = (
		overrides: Partial<StalenessCandidate> = {},
	): StalenessCandidate => ({
		currentStateEnteredAt: null,
		isBlocked: false,
		currentStateName: "",
		...overrides,
	});

	// ── EXISTING TESTS (updated to StalenessResult) ──────────────────────

	test.each([
		// [enteredAt, stalenessThresholdDays, expectedIsStale, expectedDriverKind]
		["2026-05-23T23:00:00Z", 0, false],
		["2026-05-23T23:00:00Z", 3, false],
		["2026-05-23T23:00:00Z", 2, true],
	])("daysInState=3 for %s with stalenessThreshold=%d → isStale=%s (off / at-threshold / one-over)", (enteredAt, stalenessThresholdDays, expectedIsStale) => {
		const result = deriveStaleness(
			itemEnteredAt(enteredAt),
			stalenessThresholdDays as number,
			undefined,
			now,
		);
		expect(result.isStale).toBe(expectedIsStale);
		if (expectedIsStale) {
			expect(result.reasons).toHaveLength(1);
			expect(result.reasons[0].kind).toBe("time-in-state");
		} else {
			expect(result.reasons).toHaveLength(0);
		}
	});

	test("a blocked item over the time-in-state threshold is not stale from time-in-state (ADR-026 preserved)", () => {
		const blockedOverThreshold = itemEnteredAt("2026-05-23T23:00:00Z", {
			isBlocked: true,
		});

		// blocked item with stalenessThreshold=1 (daysInState=3 > 1): isStale=false,
		// no time-in-state driver, but context-time-in-state fires
		const result = deriveStaleness(blockedOverThreshold, 1, undefined, now);
		expect(result.isStale).toBe(false);
		expect(result.reasons.some((r) => r.kind === "time-in-state")).toBe(false);

		// unblocked same item: time-in-state driver fires
		const unblockedResult = deriveStaleness(
			{ ...blockedOverThreshold, isBlocked: false },
			1,
			undefined,
			now,
		);
		expect(unblockedResult.isStale).toBe(true);
		expect(
			unblockedResult.reasons.some((r) => r.kind === "time-in-state"),
		).toBe(true);
	});

	test("an item with no entered date is never stale", () => {
		const result = deriveStaleness(itemEnteredAt(null), 1, undefined, now);
		expect(result.isStale).toBe(false);
		expect(result.reasons).toHaveLength(0);
	});

	// ── ADR-070 EARNED TRUST PROBES ──────────────────────────────────────

	describe("ADR-070 earned-trust probes", () => {
		const now = new Date("2026-05-25T12:00:00Z");
		// blockedSince=2026-05-21T12:00:00Z → blockedDays = 5 (via getAgeInDaysFromStart: UTC date diff floor + 1)
		const blockedSince = new Date("2026-05-21T12:00:00Z");
		const enteredAt = new Date("2026-05-23T23:00:00Z"); // daysInState = 3

		test("ADR-026 preservation: blocked item NEVER emits time-in-state driver", () => {
			const result = deriveStaleness(
				mkCandidate({
					currentStateEnteredAt: enteredAt,
					isBlocked: true,
					blockedSince,
					currentStateName: "Review",
				}),
				2, // stalenessThreshold (daysInState=3 > 2 → would fire if not blocked)
				10, // blockedStalenessThreshold high enough not to fire
				now,
			);
			expect(result.isStale).toBe(false);
			expect(result.reasons.some((r) => r.kind === "time-in-state")).toBe(
				false,
			);
		});

		test("new trigger: blocked item over blocked-duration threshold → isStale=true with blocked-duration driver", () => {
			const result = deriveStaleness(
				mkCandidate({
					currentStateEnteredAt: enteredAt,
					isBlocked: true,
					blockedSince,
					currentStateName: "Review",
				}),
				undefined, // stalenessThreshold disabled
				5, // blockedStalenessThreshold (blockedDays=5 ≥ 5 → fires)
				now,
			);
			expect(result.isStale).toBe(true);
			expect(result.reasons).toHaveLength(1);
			expect(result.reasons[0].kind).toBe("blocked-duration");
			expect(result.reasons[0].days).toBe(5);
		});

		test("blocked item under blocked-duration threshold is NOT stale from blocked-duration", () => {
			const result = deriveStaleness(
				mkCandidate({
					currentStateEnteredAt: enteredAt,
					isBlocked: true,
					blockedSince,
					currentStateName: "Review",
				}),
				undefined,
				10, // blockedStalenessThreshold (blockedDays=5 < 10 → no fire)
				now,
			);
			expect(result.isStale).toBe(false);
			expect(result.reasons.some((r) => r.kind === "blocked-duration")).toBe(
				false,
			);
		});

		test("driver+context UC-1: blocked item over both thresholds → blocked-duration driver + context-time-in-state, NO time-in-state driver", () => {
			const result = deriveStaleness(
				mkCandidate({
					currentStateEnteredAt: enteredAt,
					isBlocked: true,
					blockedSince,
					currentStateName: "Review",
				}),
				2, // stalenessThreshold (daysInState=3 > 2)
				5, // blockedStalenessThreshold (blockedDays=5 ≥ 5)
				now,
			);
			expect(result.isStale).toBe(true);

			// blocked-duration driver present
			const blockedDriver = result.reasons.find(
				(r) => r.kind === "blocked-duration",
			);
			expect(blockedDriver).toBeDefined();

			// context-time-in-state present
			const context = result.reasons.find(
				(r) => r.kind === "context-time-in-state",
			);
			expect(context).toBeDefined();
			if (context) {
				expect(context.days).toBe(3);
				expect((context as Record<string, unknown>).stateName).toBe("Review");
			}

			// NO time-in-state driver
			expect(result.reasons.some((r) => r.kind === "time-in-state")).toBe(
				false,
			);
		});

		test("boundary: blocked-duration uses ≥ (exactly at threshold = stale)", () => {
			const result = deriveStaleness(
				mkCandidate({
					currentStateEnteredAt: enteredAt,
					isBlocked: true,
					blockedSince,
					currentStateName: "Review",
				}),
				undefined,
				5, // blockedDays=5 ≥ 5 → stale
				now,
			);
			expect(result.isStale).toBe(true);
			expect(result.reasons[0].kind).toBe("blocked-duration");
		});

		test("boundary: blocked-duration one-below threshold is NOT stale", () => {
			const result = deriveStaleness(
				mkCandidate({
					currentStateEnteredAt: enteredAt,
					isBlocked: true,
					blockedSince,
					currentStateName: "Review",
				}),
				undefined,
				6, // blockedDays=5 < 6 → not stale
				now,
			);
			expect(result.isStale).toBe(false);
			expect(result.reasons.some((r) => r.kind === "blocked-duration")).toBe(
				false,
			);
		});

		test("boundary: time-in-state uses > (exactly at threshold = NOT stale)", () => {
			const result = deriveStaleness(
				mkCandidate({
					currentStateEnteredAt: enteredAt,
					isBlocked: false,
					currentStateName: "In Progress",
				}),
				3, // daysInState=3 > 3 → false, NOT stale
				undefined,
				now,
			);
			expect(result.isStale).toBe(false);
			expect(result.reasons.some((r) => r.kind === "time-in-state")).toBe(
				false,
			);
		});

		test("boundary: time-in-state one-over threshold IS stale", () => {
			const result = deriveStaleness(
				mkCandidate({
					currentStateEnteredAt: enteredAt,
					isBlocked: false,
					currentStateName: "In Progress",
				}),
				2, // daysInState=3 > 2 → stale
				undefined,
				now,
			);
			expect(result.isStale).toBe(true);
			expect(result.reasons[0].kind).toBe("time-in-state");
		});

		test("disabled probe: blockedStalenessThresholdDays=0 → never stale from blocked-duration", () => {
			const result = deriveStaleness(
				mkCandidate({
					currentStateEnteredAt: enteredAt,
					isBlocked: true,
					blockedSince,
					currentStateName: "Review",
				}),
				undefined,
				0, // disabled
				now,
			);
			expect(result.isStale).toBe(false);
			expect(result.reasons.some((r) => r.kind === "blocked-duration")).toBe(
				false,
			);
		});

		test("disabled probe: stalenessThresholdDays=0 → never stale from time-in-state", () => {
			const result = deriveStaleness(
				mkCandidate({
					currentStateEnteredAt: enteredAt,
					isBlocked: false,
					currentStateName: "In Progress",
				}),
				0,
				undefined,
				now,
			);
			expect(result.isStale).toBe(false);
			expect(result.reasons.some((r) => r.kind === "time-in-state")).toBe(
				false,
			);
		});

		test("disabled probe: undefined stalenessThresholdDays → never stale", () => {
			const result = deriveStaleness(
				mkCandidate({
					currentStateEnteredAt: enteredAt,
					isBlocked: false,
					currentStateName: "In Progress",
				}),
				undefined,
				undefined,
				now,
			);
			expect(result.isStale).toBe(false);
			expect(result.reasons).toHaveLength(0);
		});

		test("stale-once: item with both driver and context reasons still has isStale=true once", () => {
			const result = deriveStaleness(
				mkCandidate({
					currentStateEnteredAt: enteredAt,
					isBlocked: true,
					blockedSince,
					currentStateName: "Review",
				}),
				2,
				5,
				now,
			);
			expect(result.isStale).toBe(true);
			// has exactly 2 reasons: blocked-duration (driver) + context-time-in-state (context)
			expect(result.reasons).toHaveLength(2);
			expect(result.reasons.map((r) => r.kind).sort()).toEqual([
				"blocked-duration",
				"context-time-in-state",
			]);
		});

		test("edge: blockedSince=null → never stale from blocked-duration", () => {
			const result = deriveStaleness(
				mkCandidate({
					currentStateEnteredAt: enteredAt,
					isBlocked: true,
					blockedSince: null,
					currentStateName: "Review",
				}),
				undefined,
				1,
				now,
			);
			expect(result.isStale).toBe(false);
			expect(result.reasons.some((r) => r.kind === "blocked-duration")).toBe(
				false,
			);
		});

		test("edge: blockedSince=undefined → never stale from blocked-duration", () => {
			const result = deriveStaleness(
				mkCandidate({
					currentStateEnteredAt: enteredAt,
					isBlocked: true,
					// blockedSince not set at all
					currentStateName: "Review",
				}),
				undefined,
				1,
				now,
			);
			expect(result.isStale).toBe(false);
			expect(result.reasons.some((r) => r.kind === "blocked-duration")).toBe(
				false,
			);
		});

		test("context-time-in-state emits when blocked item exceeds stalenessThresholdDays even without blocked-duration trigger", () => {
			const result = deriveStaleness(
				mkCandidate({
					currentStateEnteredAt: enteredAt,
					isBlocked: true,
					blockedSince,
					currentStateName: "Review",
				}),
				2, // daysInState=3 > 2 → context fires
				10, // blockedDays=5 < 10 → blocked-duration does NOT fire
				now,
			);
			expect(result.isStale).toBe(false);
			const context = result.reasons.find(
				(r) => r.kind === "context-time-in-state",
			);
			expect(context).toBeDefined();
			expect(result.reasons.some((r) => r.kind === "blocked-duration")).toBe(
				false,
			);
		});

		test("context-time-in-state does NOT emit when daysInState ≤ stalenessThresholdDays", () => {
			const result = deriveStaleness(
				mkCandidate({
					currentStateEnteredAt: enteredAt,
					isBlocked: true,
					blockedSince,
					currentStateName: "Review",
				}),
				5, // daysInState=3 ≤ 5 → no context
				10,
				now,
			);
			expect(result.isStale).toBe(false);
			expect(result.reasons).toHaveLength(0);
		});

		test("time-in-state reason includes stateName from candidate", () => {
			const result = deriveStaleness(
				mkCandidate({
					currentStateEnteredAt: enteredAt,
					isBlocked: false,
					currentStateName: "QA Review",
				}),
				2,
				undefined,
				now,
			);
			expect(result.isStale).toBe(true);
			expect(result.reasons[0].kind).toBe("time-in-state");
			expect(result.reasons[0].days).toBe(3);
			expect((result.reasons[0] as Record<string, unknown>).stateName).toBe(
				"QA Review",
			);
		});
	});
});
