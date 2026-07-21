import { describe, expect, it } from "vitest";
import { formatLikelihood } from "./formatLikelihood";

describe("formatLikelihood", () => {
	it.each([
		{ value: 94.9, hasRemainingWork: true, expected: "95%" },
		{ value: 95, hasRemainingWork: true, expected: "95%" },
		{ value: 95.01, hasRemainingWork: true, expected: ">95%" },
		{ value: 100, hasRemainingWork: true, expected: ">95%" },
		{ value: 94.9, hasRemainingWork: false, expected: "95%" },
		{ value: 95, hasRemainingWork: false, expected: "95%" },
		{ value: 95.01, hasRemainingWork: false, expected: "95%" },
		{ value: 100, hasRemainingWork: false, expected: "100%" },
	])(
		"formats $value with hasRemainingWork=$hasRemainingWork as $expected at round precision",
		({ value, hasRemainingWork, expected }) => {
			expect(
				formatLikelihood(value, { hasRemainingWork, precision: "round" }),
			).toBe(expected);
		},
	);

	it.each([
		{ value: 94.9, hasRemainingWork: true, expected: "94.90%" },
		{ value: 95, hasRemainingWork: true, expected: "95.00%" },
		{ value: 95.01, hasRemainingWork: true, expected: ">95%" },
		{ value: 100, hasRemainingWork: true, expected: ">95%" },
		{ value: 94.9, hasRemainingWork: false, expected: "94.90%" },
		{ value: 95, hasRemainingWork: false, expected: "95.00%" },
		{ value: 95.01, hasRemainingWork: false, expected: "95.01%" },
		{ value: 100, hasRemainingWork: false, expected: "100.00%" },
	])(
		"formats $value with hasRemainingWork=$hasRemainingWork as $expected at fixed2 precision",
		({ value, hasRemainingWork, expected }) => {
			expect(
				formatLikelihood(value, { hasRemainingWork, precision: "fixed2" }),
			).toBe(expected);
		},
	);
});
