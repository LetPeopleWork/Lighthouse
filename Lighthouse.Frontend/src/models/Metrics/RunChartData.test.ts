import { describe, expect, it } from "vitest";
import { generateWorkItemMapForRunChart } from "../../tests/TestDataProvider";
import { RunChartData } from "../Metrics/RunChartData";

describe("Throughput", () => {
	it("should create a Throughput instance with the correct properties", () => {
		const throughputData = [10, 20, 30, 40, 50];
		const workItems = generateWorkItemMapForRunChart(throughputData);

		const throughput = new RunChartData(workItems, throughputData.length, 150);

		expect(throughput.workItemsPerUnitOfTime).toBe(workItems);
	});

	it("should return the correct history length", () => {
		const throughputData = [10, 20, 30, 40, 50];
		const throughput = new RunChartData(
			generateWorkItemMapForRunChart(throughputData),
			throughputData.length,
			150,
		);

		expect(throughput.history).toBe(throughputData.length);
	});

	it("should return the correct throughput on a given day", () => {
		const throughputData = [10, 20, 30, 40, 50];
		const throughput = new RunChartData(
			generateWorkItemMapForRunChart(throughputData),
			throughputData.length,
			150,
		);

		expect(throughput.getValueOnDay(0)).toBe(10);
		expect(throughput.getValueOnDay(1)).toBe(20);
		expect(throughput.getValueOnDay(2)).toBe(30);
		expect(throughput.getValueOnDay(3)).toBe(40);
		expect(throughput.getValueOnDay(4)).toBe(50);
	});

	it("should handle requests for throughput on an invalid day gracefully", () => {
		const throughputData = [10, 20, 30, 40, 50];
		const throughput = new RunChartData(
			generateWorkItemMapForRunChart(throughputData),
			throughputData.length,
			150,
		);

		expect(() => throughput.getValueOnDay(-1)).toThrow(RangeError);
		expect(() => throughput.getValueOnDay(throughputData.length)).toThrow(
			RangeError,
		);
	});
});
