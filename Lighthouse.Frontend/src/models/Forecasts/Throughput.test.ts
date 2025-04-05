import { describe, expect, it } from "vitest";
import { Throughput } from "./Throughput";

describe("Throughput", () => {
	it("should create a Throughput instance with the correct properties", () => {
		const throughputData = [10, 20, 30, 40, 50];
		const throughput = new Throughput(
			throughputData,
			throughputData.length,
			150,
		);

		expect(throughput.throughputPerUnitOfTime).toBe(throughputData);
	});

	it("should return the correct history length", () => {
		const throughputData = [10, 20, 30, 40, 50];
		const throughput = new Throughput(
			throughputData,
			throughputData.length,
			150,
		);

		expect(throughput.history).toBe(throughputData.length);
	});

	it("should return the correct throughput on a given day", () => {
		const throughputData = [10, 20, 30, 40, 50];
		const throughput = new Throughput(
			throughputData,
			throughputData.length,
			150,
		);

		expect(throughput.getThroughputOnDay(0)).toBe(10);
		expect(throughput.getThroughputOnDay(1)).toBe(20);
		expect(throughput.getThroughputOnDay(2)).toBe(30);
		expect(throughput.getThroughputOnDay(3)).toBe(40);
		expect(throughput.getThroughputOnDay(4)).toBe(50);
	});

	it("should handle requests for throughput on an invalid day gracefully", () => {
		const throughputData = [10, 20, 30, 40, 50];
		const throughput = new Throughput(
			throughputData,
			throughputData.length,
			150,
		);

		expect(() => throughput.getThroughputOnDay(-1)).toThrow(RangeError);
		expect(() => throughput.getThroughputOnDay(throughputData.length)).toThrow(
			RangeError,
		);
	});
});
