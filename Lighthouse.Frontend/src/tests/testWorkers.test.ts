import { afterEach, describe, expect, it, vi } from "vitest";
import {
	LOCAL_WORKER_FRACTION,
	resolveTestWorkers,
	TEST_WORKERS_VARIABLE,
} from "./testWorkers";

const localCores = 12;

describe("resolveTestWorkers", () => {
	it("gives a local run half of the cores and reads its override from LIGHTHOUSE_TEST_WORKERS", () => {
		expect(LOCAL_WORKER_FRACTION).toBe(0.5);
		expect(TEST_WORKERS_VARIABLE).toBe("LIGHTHOUSE_TEST_WORKERS");
	});

	describe("on a developer machine", () => {
		it.each([
			["nothing is set", {}],
			["the count is empty", { LIGHTHOUSE_TEST_WORKERS: "" }],
			["CI is false", { CI: "false" }],
			["CI is empty", { CI: "" }],
			["CI is some other value", { CI: "1" }],
		])("uses the share of cores when %s", (_name, env) => {
			expect(resolveTestWorkers(env, localCores)).toBe(6);
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
		it.each([
			["nothing is set", { CI: "true" }, 4],
			["CI is written as TRUE", { CI: "TRUE" }, 4],
			["CI is written as True", { CI: "True" }, 4],
			["the count is empty", { CI: "true", LIGHTHOUSE_TEST_WORKERS: "" }, 4],
			["there is one core", { CI: "true" }, 1],
			["there are 64 cores", { CI: "true" }, 64],
		])(
			"leaves the worker count to the test runner when %s",
			(_name, env, cores) => {
				expect(resolveTestWorkers(env, cores)).toBeUndefined();
			},
		);

		it.each([["true"], ["TRUE"]])(
			"lets an explicit count win when CI is written as %s",
			(ci) => {
				expect(
					resolveTestWorkers({ CI: ci, LIGHTHOUSE_TEST_WORKERS: "2" }, 4),
				).toBe(2);
			},
		);
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
			expect(() =>
				resolveTestWorkers({ CI: "true", LIGHTHOUSE_TEST_WORKERS: value }, 4),
			).toThrow(/LIGHTHOUSE_TEST_WORKERS/);
		});
	});

	describe("in the vitest configuration", () => {
		afterEach(() => {
			vi.unstubAllEnvs();
			vi.resetModules();
		});

		it.each([
			["an explicit count is set", { LIGHTHOUSE_TEST_WORKERS: "3", CI: "" }, 3],
			[
				"CI is set and nothing else",
				{ LIGHTHOUSE_TEST_WORKERS: "", CI: "true" },
				undefined,
			],
		])(
			"applies the resolved worker count when %s",
			async (_name, env, expected) => {
				vi.resetModules();
				for (const [key, value] of Object.entries(env)) {
					vi.stubEnv(key, value);
				}

				// The config sits outside the app tsconfig, so it is imported by a computed path.
				const configPath = "../../vitest.config.ts";
				const config: { test?: { maxWorkers?: number } } = (
					await import(/* @vite-ignore */ configPath)
				).default;

				expect(config.test?.maxWorkers).toBe(expected);
			},
		);
	});
});
