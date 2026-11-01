import { describe, expect, it } from "vitest";
import {
	LOCAL_WORKER_FRACTION,
	resolveTestWorkers,
	TEST_WORKERS_VARIABLE,
} from "./testWorkers";

const localCores = 12;

describe("resolveTestWorkers", () => {
	it("is importable and pins the agreed fraction and variable name", () => {
		expect(LOCAL_WORKER_FRACTION).toBe(0.5);
		expect(TEST_WORKERS_VARIABLE).toBe("LIGHTHOUSE_TEST_WORKERS");
	});

	describe("on a developer machine", () => {
		it("uses the share of cores when nothing is set", () => {
			expect(resolveTestWorkers({}, localCores)).toBe(6);
		});

		it("treats an empty count as not set", () => {
			expect(
				resolveTestWorkers({ LIGHTHOUSE_TEST_WORKERS: "" }, localCores),
			).toBe(6);
		});

		it.each([
			["1", 1],
			["3", 3],
			["16", 16],
		])("uses the explicit count %s", (value, expected) => {
			expect(
				resolveTestWorkers({ LIGHTHOUSE_TEST_WORKERS: value }, localCores),
			).toBe(expected);
		});

		it.each([
			["CI is false", { CI: "false" }],
			["CI is empty", { CI: "" }],
			["CI is some other value", { CI: "1" }],
		])("still caps the workers when %s", (_name, env) => {
			expect(resolveTestWorkers(env, localCores)).toBe(6);
		});

		it.each([
			[1, 1],
			[2, 1],
			[4, 2],
			[7, 3],
			[12, 6],
			[24, 12],
		])("on %i cores uses %i workers", (cores, expected) => {
			expect(resolveTestWorkers({}, cores)).toBe(expected);
		});

		it("always returns a whole number of at least one, never above the cores", () => {
			for (let cores = 1; cores <= 128; cores++) {
				const workers = resolveTestWorkers({}, cores);
				expect(Number.isInteger(workers)).toBe(true);
				expect(workers).toBeGreaterThanOrEqual(1);
				expect(workers).toBeLessThanOrEqual(cores);
			}
		});
	});

	describe("on the build server", () => {
		it("leaves the worker count to the test runner when nothing is set", () => {
			expect(resolveTestWorkers({ CI: "true" }, 4)).toBeUndefined();
		});

		it.each([["TRUE"], ["True"]])("recognises CI written as %s", (ci) => {
			expect(resolveTestWorkers({ CI: ci }, 4)).toBeUndefined();
		});

		it("treats an empty count as not set", () => {
			expect(
				resolveTestWorkers({ CI: "true", LIGHTHOUSE_TEST_WORKERS: "" }, 4),
			).toBeUndefined();
		});

		it("leaves the worker count to the test runner whatever the core count", () => {
			expect(resolveTestWorkers({ CI: "true" }, 1)).toBeUndefined();
			expect(resolveTestWorkers({ CI: "true" }, 64)).toBeUndefined();
		});

		it("lets an explicit count win", () => {
			expect(
				resolveTestWorkers({ CI: "true", LIGHTHOUSE_TEST_WORKERS: "2" }, 4),
			).toBe(2);
		});

		it("lets an explicit count win when CI is written as TRUE", () => {
			expect(
				resolveTestWorkers({ CI: "TRUE", LIGHTHOUSE_TEST_WORKERS: "2" }, 4),
			).toBe(2);
		});
	});

	describe("with an invalid count", () => {
		it.each([
			["zero", "0"],
			["negative", "-2"],
			["fractional", "1.5"],
			["non-numeric", "abc"],
			["a percentage", "50%"],
			["with a unit", "4x"],
			["with a leading space", " 4"],
			["with a trailing space", "4 "],
			["with a leading plus", "+4"],
		])("rejects a count that is %s, naming the variable", (_name, value) => {
			expect(() =>
				resolveTestWorkers({ LIGHTHOUSE_TEST_WORKERS: value }, localCores),
			).toThrow(/LIGHTHOUSE_TEST_WORKERS/);
		});

		it("rejects an invalid count on the build server too", () => {
			expect(() =>
				resolveTestWorkers({ CI: "true", LIGHTHOUSE_TEST_WORKERS: "0" }, 4),
			).toThrow(/LIGHTHOUSE_TEST_WORKERS/);
		});
	});
});
