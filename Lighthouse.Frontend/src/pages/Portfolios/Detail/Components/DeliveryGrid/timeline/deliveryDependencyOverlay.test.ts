import { describe, expect, it } from "vitest";
import type { IFeature } from "../../../../../../models/Feature";
import type { IFeatureDependency } from "../../../../../../models/FeatureDependency";
import { WhenForecast } from "../../../../../../models/Forecasts/WhenForecast";
import type { DependencyTerms } from "../../../../../../utils/dependencies/dependencySentences";
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

const terms: DependencyTerms = {
	featureTerm: "Feature",
	portfolioTerm: "Portfolio",
};

// Every word a reader can rename is a word this chart has to render as theirs, so the sentences are
// asked for in the instance's vocabulary rather than written out here.
const renamedTerms: DependencyTerms = {
	featureTerm: "Initiative",
	portfolioTerm: "Programme",
};

const overlayFor = (features: IFeature[], words: DependencyTerms = terms) =>
	buildDependencyOverlay(
		features,
		buildDeliveryTimeline(features, DEFAULT_TIMELINE_PERCENTILE),
		words,
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

	const notesOn = (features: IFeature[]) =>
		overlayFor(features).marks.get(7)?.notes;

	const notInThisDelivery = [
		aWaiter([
			dependency({
				referenceId: "OE-001",
				name: "Hydrothermal Vent Survey",
			}),
		]),
	];

	const inThisDeliveryButUnplaceable = [
		aBlocker({ forecasts: [] }),
		aWaiter([dependency({ referenceId: "OE-001" })]),
	];

	it("marks a blocker that is not in this Delivery, and does not warn about it", () => {
		expect(notesOn(notInThisDelivery)).toEqual([
			{
				text: "Waiting on Hydrothermal Vent Survey, which is not on this timeline.",
				isWarning: false,
			},
		]);
	});

	it("gives a blocker in this Delivery that could not be placed its own reason", () => {
		expect(notesOn(inThisDeliveryButUnplaceable)).toEqual([
			{
				text: "Waiting on Hydrothermal Vent Survey, which has no forecast to place on this timeline.",
				isWarning: false,
			},
		]);

		expect(notesOn(inThisDeliveryButUnplaceable)).not.toEqual(
			notesOn(notInThisDelivery),
		);
	});

	it("says the reason the forecast gives rather than that there is no bar", () => {
		const notes = notesOn([
			aBlocker({ forecasts: [] }),
			aWaiter([
				dependency({
					referenceId: "OE-001",
					notHonouredReason: "BlockerCannotBeForecast",
				}),
				dependency({
					referenceId: "OE-009",
					name: "Seamount Ridge Mapping",
				}),
			]),
		]);

		expect(notes).toEqual([
			{
				text: "Hydrothermal Vent Survey has no measured delivery to forecast from, so the wait cannot be given a date. That dependency is not included in the forecast.",
				isWarning: true,
			},
			{
				text: "Waiting on Seamount Ridge Mapping, which is not on this timeline.",
				isWarning: false,
			},
		]);
	});

	it("marks a withheld blocker without naming it", () => {
		const withheldName = "Classified Hull Retrofit";
		const overlay = overlayFor([
			aWaiter([
				dependency({
					referenceId: "",
					name: withheldName,
					isWithheld: true,
				}),
			]),
		]);

		expect(overlay.marks.get(7)?.notes).toEqual([
			{
				text: "Waiting on a Feature you do not have access to.",
				isWarning: false,
			},
		]);
		expect(JSON.stringify([...overlay.marks.values()])).not.toContain(
			withheldName,
		);
	});

	it("calls a withheld blocker by the word this instance uses for one", () => {
		const overlay = overlayFor(
			[
				aWaiter([
					dependency({
						referenceId: "",
						name: "Classified Hull Retrofit",
						isWithheld: true,
					}),
				]),
			],
			renamedTerms,
		);

		expect(overlay.marks.get(7)?.notes[0].text).toContain("Initiative");
	});

	// One sentence serving all three reasons tells the reader only that something is wrong, which is the
	// one thing they could already see.
	it.each([
		[
			"InALoop" as const,
			"This Feature and Hydrothermal Vent Survey are waiting on each other. That dependency is not included in the forecast.",
		],
		[
			"BlockerCannotBeForecast" as const,
			"Hydrothermal Vent Survey has no measured delivery to forecast from, so the wait cannot be given a date. That dependency is not included in the forecast.",
		],
		[
			"OutsideThisPortfolio" as const,
			"This Feature depends on Hydrothermal Vent Survey, which is in no Portfolio they share. That dependency is not included in the forecast.",
		],
	])(
		"draws no line for %s and warns the waiting bar in that reason's own words",
		(reason, sentence) => {
			const overlay = overlayFor([
				aBlocker(),
				aWaiter([
					dependency({ referenceId: "OE-001", notHonouredReason: reason }),
				]),
			]);

			expect(overlay.edges).toEqual([]);
			expect(overlay.marks.get(7)?.notes).toEqual([
				{ text: sentence, isWarning: true },
			]);
		},
	);

	const setAside = (referenceId: string) =>
		dependency({ referenceId, notHonouredReason: "IgnoredByPortfolio" });

	it("raises no mark on any bar when the Portfolio has set its dependencies aside", () => {
		const overlay = overlayFor([
			aBlocker(),
			aWaiter([setAside("OE-001")]),
			aWaiter([setAside("OE-001")], {
				id: 9,
				referenceId: "OE-005",
				name: "Trench Sediment Sampling",
			}),
		]);

		expect(overlay.edges).toEqual([]);
		expect(overlay.marks.size).toBe(0);
	});

	it("says once above the chart that dependencies have been set aside", () => {
		const overlay = overlayFor([aBlocker(), aWaiter([setAside("OE-001")])]);

		expect(overlay.chartNote).toBe("Portfolio is set to ignore dependencies.");
	});

	it("says that in the word this instance uses for a Portfolio", () => {
		const overlay = overlayFor(
			[aBlocker(), aWaiter([setAside("OE-001")])],
			renamedTerms,
		);

		expect(overlay.chartNote).toBe("Programme is set to ignore dependencies.");
	});

	it("leaves the chart with nothing to say when nothing was set aside", () => {
		const overlay = overlayFor([
			aBlocker(),
			aWaiter([dependency({ referenceId: "OE-001" })]),
		]);

		expect(overlay.chartNote).toBeNull();
	});

	it("draws the line to a blocker that sits below, and marks the bar as well", () => {
		const overlay = overlayFor([
			aBlocker(),
			aWaiter([
				dependency({ referenceId: "OE-001", blockerPositionedBelow: true }),
			]),
		]);

		expect(overlay.edges).toEqual([
			{ blockerFeatureId: 3, waitingFeatureId: 7 },
		]);
		expect(overlay.marks.get(7)?.notes).toEqual([
			{
				text: "This Feature depends on Hydrothermal Vent Survey, which sits below it in the order.",
				isWarning: true,
			},
		]);
	});
});
