import { describe, expect, it } from "vitest";
import type { IFeature } from "../../../../../../models/Feature";
import type { IFeatureDependency } from "../../../../../../models/FeatureDependency";
import { WhenForecast } from "../../../../../../models/Forecasts/WhenForecast";
import { buildDependencyOverlay } from "./deliveryDependencyOverlay";
import {
	buildDeliveryTimeline,
	DEFAULT_TIMELINE_PERCENTILE,
} from "./deliveryTimelineModel";

const october = (day: number) => new Date(2026, 9, day);

const spreadFrom = (day: number) => [
	WhenForecast.new(70, october(day)),
	WhenForecast.new(85, october(day + 1)),
	WhenForecast.new(95, october(day + 2)),
];

const feature = (overrides: Partial<IFeature>): IFeature =>
	({
		id: 1,
		referenceId: "OE-001",
		name: "Deep Sea Mapping Initiative",
		startForecast: { source: "Forecast", percentiles: spreadFrom(10) },
		forecasts: spreadFrom(20),
		teamsWithoutForecast: [],
		dependsOn: [],
		...overrides,
	}) as IFeature;

const dependency = (
	overrides: Partial<IFeatureDependency>,
): IFeatureDependency => ({
	referenceId: "OE-001",
	name: "whatever the row happens to call it",
	url: null,
	source: "TrackerLink",
	notHonouredReason: null,
	blockerPositionedBelow: false,
	isWithheld: false,
	...overrides,
});

const overlayFor = (features: IFeature[]) =>
	buildDependencyOverlay(
		features,
		buildDeliveryTimeline(features, DEFAULT_TIMELINE_PERCENTILE),
	);

/** The blocker is id 3 / OE-001; the Feature waiting on it is id 7 / OE-004. */
const aBlocker = (overrides: Partial<IFeature> = {}) =>
	feature({
		id: 3,
		referenceId: "OE-001",
		name: "Hydrothermal Vent Survey",
		...overrides,
	});

const aWaiter = (
	dependencies: IFeatureDependency[],
	overrides: Partial<IFeature> = {},
) =>
	feature({
		id: 7,
		referenceId: "OE-004",
		name: "Abyssal Plain Charting",
		dependsOn: dependencies,
		...overrides,
	});

describe("buildDependencyOverlay", () => {
	it("connects a Feature to the blocker it is waiting on in this Delivery", () => {
		const overlay = overlayFor([
			aBlocker(),
			aWaiter([dependency({ referenceId: "OE-001" })]),
		]);

		expect(overlay.edges).toEqual([
			{ blockerFeatureId: 3, waitingFeatureId: 7 },
		]);
	});

	it("finds the blocker by its reference id even when the names disagree", () => {
		const overlay = overlayFor([
			aBlocker({ name: "Hydrothermal Vent Survey" }),
			aWaiter([
				dependency({
					referenceId: "OE-001",
					name: "a name nobody kept in step",
				}),
			]),
		]);

		expect(overlay.edges).toHaveLength(1);
	});

	it("does not find the blocker by name when the reference ids disagree", () => {
		const sharedName = "Hydrothermal Vent Survey";
		const overlay = overlayFor([
			aBlocker({ referenceId: "OE-002", name: sharedName }),
			aWaiter([dependency({ referenceId: "OE-001", name: sharedName })]),
		]);

		expect(overlay.edges).toEqual([]);
	});

	it("draws the edge when the reference ids match exactly", () => {
		const overlay = overlayFor([
			aBlocker({ referenceId: "OE-001" }),
			aWaiter([dependency({ referenceId: "OE-001" })]),
		]);

		expect(overlay.edges).toEqual([
			{ blockerFeatureId: 3, waitingFeatureId: 7 },
		]);
	});

	it("draws nothing when the reference ids differ only by case", () => {
		const overlay = overlayFor([
			aBlocker({ referenceId: "oe-001" }),
			aWaiter([dependency({ referenceId: "OE-001" })]),
		]);

		expect(overlay.edges).toEqual([]);
	});

	it("gives each Feature waiting on one blocker an edge of its own", () => {
		const overlay = overlayFor([
			aBlocker(),
			aWaiter([dependency({ referenceId: "OE-001" })]),
			aWaiter([dependency({ referenceId: "OE-001" })], {
				id: 9,
				referenceId: "OE-005",
				name: "Trench Sediment Sampling",
			}),
		]);

		expect(overlay.edges).toEqual([
			{ blockerFeatureId: 3, waitingFeatureId: 7 },
			{ blockerFeatureId: 3, waitingFeatureId: 9 },
		]);
	});

	it("joins nothing to a withheld dependency, which carries no reference id", () => {
		const overlay = overlayFor([
			aBlocker({ referenceId: "" }),
			aWaiter([
				dependency({
					referenceId: "",
					name: "Withheld",
					isWithheld: true,
				}),
			]),
		]);

		expect(overlay.edges).toEqual([]);
	});

	it("leaves a Feature with no dependencies out of the marks entirely", () => {
		const overlay = overlayFor([aBlocker(), aWaiter([])]);

		expect(overlay.marks.has(7)).toBe(false);
		expect(overlay.marks.has(3)).toBe(false);
	});

	it("leaves a Feature whose every dependency is drawn out of the marks entirely", () => {
		const overlay = overlayFor([
			aBlocker(),
			aWaiter([dependency({ referenceId: "OE-001" })]),
		]);

		expect(overlay.edges).toHaveLength(1);
		expect(overlay.marks.has(7)).toBe(false);
	});
});
