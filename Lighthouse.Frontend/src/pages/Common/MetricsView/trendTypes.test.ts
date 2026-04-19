import { describe, expect, it } from "vitest";
import type {
	TrendDetailRow,
	TrendDirection,
	TrendPayload,
} from "./trendTypes";

describe("trendTypes", () => {
	it("TrendDirection accepts valid direction values", () => {
		const directions: TrendDirection[] = ["up", "down", "flat", "none"];
		expect(directions).toHaveLength(4);
	});

	it("TrendPayload with full comparison data is structurally valid", () => {
		const payload: TrendPayload = {
			direction: "up",
			metricLabel: "Work Items in Progress",
			currentLabel: "2026-04-19",
			currentValue: "5",
			previousLabel: "2026-04-05",
			previousValue: "3",
			percentageDelta: "+66.7%",
			detailRows: [
				{
					label: "50th percentile",
					currentValue: "4 days",
					previousValue: "6 days",
				},
			],
		};

		expect(payload.direction).toBe("up");
		expect(payload.metricLabel).toBe("Work Items in Progress");
		expect(payload.detailRows).toHaveLength(1);
	});

	it("TrendPayload with minimal data is structurally valid", () => {
		const payload: TrendPayload = {
			direction: "none",
			metricLabel: "Blocked Items",
		};

		expect(payload.direction).toBe("none");
		expect(payload.currentLabel).toBeUndefined();
		expect(payload.previousLabel).toBeUndefined();
		expect(payload.percentageDelta).toBeUndefined();
		expect(payload.detailRows).toBeUndefined();
	});

	it("TrendPayload for snapshot comparison uses single-date labels", () => {
		const payload: TrendPayload = {
			direction: "down",
			metricLabel: "Total Work Item Age",
			currentLabel: "2026-04-19",
			currentValue: "42 days",
			previousLabel: "2026-04-05",
			previousValue: "55 days",
		};

		expect(payload.percentageDelta).toBeUndefined();
	});

	it("TrendPayload for previous-period uses date range labels", () => {
		const payload: TrendPayload = {
			direction: "flat",
			metricLabel: "Predictability Score",
			currentLabel: "2026-04-05 – 2026-04-19",
			currentValue: "78%",
			previousLabel: "2026-03-22 – 2026-04-04",
			previousValue: "76%",
		};

		expect(payload.direction).toBe("flat");
	});

	it("TrendDetailRow captures per-percentile comparison", () => {
		const row: TrendDetailRow = {
			label: "85th percentile",
			currentValue: "12 days",
			previousValue: "14 days",
		};

		expect(row.label).toBe("85th percentile");
		expect(row.currentValue).toBe("12 days");
		expect(row.previousValue).toBe("14 days");
	});
});
