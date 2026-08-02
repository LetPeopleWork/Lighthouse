import { describe, expect, it } from "vitest";
import { parseDeliveryMetricsHistory } from "../../../models/Delivery/DeliveryMetricsHistory";
import { deliveryEpicColors } from "./deliveryEpicColors";

type RawEpic = {
	referenceId: string;
	name: string;
	completion: number;
	likelihood: number | null;
	totalItems?: number;
};

const point = (date: string, featureBreakdown: RawEpic[]) => ({
	date,
	totalWork: 20,
	doneWork: 4,
	remainingWork: 16,
	estimatedItemCount: null,
	forecastHowMany: null,
	likelihoodPercentage: 70,
	whenDistribution: null,
	featureBreakdown,
});

const history = (points: ReturnType<typeof point>[]) =>
	parseDeliveryMetricsHistory({
		deliveryDate: "2026-06-10T00:00:00Z",
		firstSnapshotDate: "2026-06-01T00:00:00Z",
		points,
	});

const sized = (referenceId: string): RawEpic => ({
	referenceId,
	name: `Epic ${referenceId}`,
	completion: 10,
	likelihood: 50,
	totalItems: 8,
});

describe("deliveryEpicColors", () => {
	it("colours an epic that carries no recorded size", () => {
		const colors = deliveryEpicColors(
			history([
				point("2026-06-01T00:00:00Z", [
					sized("EPIC-A"),
					{
						referenceId: "EPIC-B",
						name: "Epic B",
						completion: 10,
						likelihood: 50,
					},
				]),
			]),
		);

		expect(colors["EPIC-B"]).toBeDefined();
		expect(colors["EPIC-B"]).not.toBe(colors["EPIC-A"]);
	});

	it("colours an epic that cannot be forecast", () => {
		const colors = deliveryEpicColors(
			history([
				point("2026-06-01T00:00:00Z", [
					sized("EPIC-A"),
					{ ...sized("EPIC-B"), likelihood: null },
				]),
			]),
		);

		expect(colors["EPIC-B"]).toBeDefined();
		expect(colors["EPIC-B"]).not.toBe(colors["EPIC-A"]);
	});

	it("takes every day in the window into account, not just the first", () => {
		const colors = deliveryEpicColors(
			history([
				point("2026-06-01T00:00:00Z", [sized("EPIC-A")]),
				point("2026-06-02T00:00:00Z", [sized("EPIC-A"), sized("EPIC-B")]),
			]),
		);

		expect(colors["EPIC-B"]).toBeDefined();
	});

	// The two delivery charts filter the breakdown differently, so they meet an epic's days in a
	// different order. Colour has to fall out of the set, or the same epic is two colours on one tab.
	it("depends on which epics are in the window, not the order they turn up", () => {
		const firstSeenAFirst = deliveryEpicColors(
			history([
				point("2026-06-01T00:00:00Z", [sized("EPIC-A")]),
				point("2026-06-02T00:00:00Z", [sized("EPIC-B"), sized("EPIC-A")]),
			]),
		);
		const firstSeenBFirst = deliveryEpicColors(
			history([
				point("2026-06-01T00:00:00Z", [sized("EPIC-B")]),
				point("2026-06-02T00:00:00Z", [sized("EPIC-A"), sized("EPIC-B")]),
			]),
		);

		expect(firstSeenAFirst).toEqual(firstSeenBFirst);
	});

	it("has nothing to colour when no epic was ever recorded", () => {
		expect(
			deliveryEpicColors(history([point("2026-06-01T00:00:00Z", [])])),
		).toEqual({});
	});
});
