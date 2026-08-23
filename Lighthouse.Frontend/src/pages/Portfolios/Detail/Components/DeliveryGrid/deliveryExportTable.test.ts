import { describe, expect, it } from "vitest";
import { Delivery, type IDelivery } from "../../../../../models/Delivery";
import type { IEntityReference } from "../../../../../models/EntityReference";
import { Feature } from "../../../../../models/Feature";
import type { IFeatureDependency } from "../../../../../models/FeatureDependency";
import { WhenForecast } from "../../../../../models/Forecasts/WhenForecast";
import { DeliverySelectionMode } from "../../../../../models/WorkItemRules";
import { buildDeliveryExportTable } from "./deliveryExportTable";

const TERMS = {
	deliveryTerm: "Delivery",
	workItemsTerm: "Work Items",
	featureTerm: "Feature",
	portfolioTerm: "Portfolio",
	teamTerm: "Team",
};

const TEAMS: IEntityReference[] = [
	{ id: 1, name: "Team Alpha" },
	{ id: 2, name: "Team Beta" },
];

const HEADERS = [
	"Name",
	"Team",
	"Progress",
	"Forecast 50%",
	"Forecast 70%",
	"Forecast 85%",
	"Forecast 95%",
	"Likelihood",
	"State",
	"Dependencies",
	"Warnings",
];

const column = (name: string): number => HEADERS.indexOf(name);

const makeDelivery = (overrides: Partial<IDelivery> = {}): Delivery => {
	const delivery = Delivery.fromBackend({
		id: 1,
		name: "Q3 Platform",
		date: "2026-09-12T00:00:00",
		portfolioId: 7,
		features: [],
		likelihoodPercentage: 81.6,
		progress: 0.4,
		remainingWork: 48,
		totalWork: 120,
		featureLikelihoods: [],
		completionDates: [],
		selectionMode: DeliverySelectionMode.Manual,
		metricSnapshotCount: 3,
		...overrides,
	} as IDelivery);

	if (overrides.completionDates) {
		delivery.completionDates = overrides.completionDates as WhenForecast[];
	}

	return delivery;
};

const makeFeature = (overrides: Partial<Feature> = {}): Feature => {
	const feature = new Feature();
	feature.id = 11;
	feature.name = "Checkout redesign";
	feature.referenceId = "FTR-11";
	feature.state = "Active";
	feature.stateCategory = "Doing";
	feature.lastUpdated = new Date();
	feature.isUsingDefaultFeatureSize = false;
	feature.projects = [];
	feature.remainingWork = { 1: 4 };
	feature.totalWork = { 1: 10 };
	feature.forecasts = [];
	feature.teamsWithoutForecast = [];
	feature.dependsOn = [];
	feature.url = "";
	return Object.assign(feature, overrides);
};

const aDependency = (
	overrides: Partial<IFeatureDependency> = {},
): IFeatureDependency => ({
	referenceId: "FTR-90",
	name: "Warehouse sync",
	url: null,
	source: "TrackerLink",
	notHonouredReason: null,
	blockerPositionedBelow: false,
	isWithheld: false,
	...overrides,
});

const allFourForecasts = () => [
	WhenForecast.new(50, new Date(2026, 8, 1)),
	WhenForecast.new(70, new Date(2026, 8, 5)),
	WhenForecast.new(85, new Date(2026, 8, 12)),
	WhenForecast.new(95, new Date(2026, 8, 26)),
];

describe("buildDeliveryExportTable", () => {
	it("names the same eleven columns in the same order every time", () => {
		const table = buildDeliveryExportTable(makeDelivery(), [], TEAMS, TERMS);

		expect(table.headers).toEqual(HEADERS);
	});

	describe("the Delivery's own row", () => {
		it("comes first, and says which of the rows is the Delivery", () => {
			const table = buildDeliveryExportTable(
				makeDelivery({ name: "Q3 Platform" }),
				[makeFeature()],
				TEAMS,
				TERMS,
			);

			expect(table.rows[0][column("Name")]).toBe("Q3 Platform (Delivery)");
		});

		it("calls it what this instance calls it", () => {
			const table = buildDeliveryExportTable(
				makeDelivery({ name: "Q3 Platform" }),
				[],
				TEAMS,
				{ ...TERMS, deliveryTerm: "Milestone" },
			);

			expect(table.rows[0][column("Name")]).toBe("Q3 Platform (Milestone)");
		});

		it("counts the work done against the work there is", () => {
			const table = buildDeliveryExportTable(
				makeDelivery({ totalWork: 120, remainingWork: 48 }),
				[],
				TEAMS,
				TERMS,
			);

			expect(table.rows[0][column("Progress")]).toBe("72/120");
		});

		it("dates each of its four forecasts as a calendar day", () => {
			const table = buildDeliveryExportTable(
				makeDelivery({ completionDates: allFourForecasts() }),
				[],
				TEAMS,
				TERMS,
			);

			expect(table.rows[0][column("Forecast 50%")]).toBe("2026-09-01");
			expect(table.rows[0][column("Forecast 70%")]).toBe("2026-09-05");
			expect(table.rows[0][column("Forecast 85%")]).toBe("2026-09-12");
			expect(table.rows[0][column("Forecast 95%")]).toBe("2026-09-26");
		});

		it("rounds its chance of landing to a whole percentage", () => {
			const table = buildDeliveryExportTable(
				makeDelivery({ likelihoodPercentage: 81.6 }),
				[],
				TEAMS,
				TERMS,
			);

			expect(table.rows[0][column("Likelihood")]).toBe("82%");
		});

		it("leaves the columns that only mean something for a Feature empty", () => {
			const table = buildDeliveryExportTable(makeDelivery(), [], TEAMS, TERMS);

			for (const header of ["Team", "State", "Dependencies", "Warnings"]) {
				expect(table.rows[0][column(header)]).toBe("");
			}
		});

		it("leaves a chance nobody computed empty rather than reporting a zero", () => {
			const table = buildDeliveryExportTable(
				makeDelivery({ likelihoodPercentage: null }),
				[],
				TEAMS,
				TERMS,
			);

			expect(table.rows[0][column("Likelihood")]).toBe("");
			for (const cell of table.rows[0]) {
				expect(cell).not.toMatch(/null|undefined|NaN/i);
			}
		});

		it("leaves a forecast nobody computed empty rather than inventing a date", () => {
			const table = buildDeliveryExportTable(
				makeDelivery({
					completionDates: [WhenForecast.new(85, new Date(2026, 8, 12))],
				}),
				[],
				TEAMS,
				TERMS,
			);

			expect(table.rows[0][column("Forecast 85%")]).toBe("2026-09-12");
			expect(table.rows[0][column("Forecast 50%")]).toBe("");
			expect(table.rows[0][column("Forecast 70%")]).toBe("");
			expect(table.rows[0][column("Forecast 95%")]).toBe("");
		});
	});

	describe("a Feature's row", () => {
		const featureRow = (
			feature: Feature,
			delivery = makeDelivery({
				featureLikelihoods: [
					{ featureId: feature.id, likelihoodPercentage: 63.2 },
				],
			}),
		) => buildDeliveryExportTable(delivery, [feature], TEAMS, TERMS).rows[1];

		it("names the Feature the way the screen names it", () => {
			const row = featureRow(
				makeFeature({ name: "Checkout redesign", referenceId: "FTR-11" }),
			);

			expect(row[column("Name")]).toBe("FTR-11: Checkout redesign");
		});

		it("lists every team with work on it", () => {
			const row = featureRow(
				makeFeature({ totalWork: { 1: 10, 2: 6 }, remainingWork: { 1: 4 } }),
			);

			expect(row[column("Team")]).toBe("Team Alpha; Team Beta");
		});

		it("says nobody is on it rather than leaving the reader to guess", () => {
			const row = featureRow(makeFeature({ totalWork: {}, remainingWork: {} }));

			expect(row[column("Team")]).toBe("Unassigned");
		});

		it("counts the work done against the work there is", () => {
			const row = featureRow(
				makeFeature({ totalWork: { 1: 10, 2: 6 }, remainingWork: { 1: 4 } }),
			);

			expect(row[column("Progress")]).toBe("12/16");
		});

		it("dates each of its four forecasts as a calendar day", () => {
			const row = featureRow(makeFeature({ forecasts: allFourForecasts() }));

			expect(row[column("Forecast 50%")]).toBe("2026-09-01");
			expect(row[column("Forecast 70%")]).toBe("2026-09-05");
			expect(row[column("Forecast 85%")]).toBe("2026-09-12");
			expect(row[column("Forecast 95%")]).toBe("2026-09-26");
		});

		it("says it cannot forecast in all four columns when no team can be forecast", () => {
			const row = featureRow(
				makeFeature({
					forecasts: allFourForecasts(),
					teamsWithoutForecast: ["Team Pulsar"],
				}),
			);

			for (const header of [
				"Forecast 50%",
				"Forecast 70%",
				"Forecast 85%",
				"Forecast 95%",
			]) {
				expect(row[column(header)]).toBe("Cannot forecast");
			}
		});

		it("reads its chance of landing off the Delivery it belongs to", () => {
			const row = featureRow(makeFeature({ id: 11 }));

			expect(row[column("Likelihood")]).toBe("63%");
		});

		it("leaves the chance empty for a Feature the Delivery has no number for", () => {
			const row = featureRow(makeFeature({ id: 11 }), makeDelivery());

			expect(row[column("Likelihood")]).toBe("");
		});

		it("reports the state the tracker has it in", () => {
			const row = featureRow(makeFeature({ state: "In Review" }));

			expect(row[column("State")]).toBe("In Review");
		});

		it("names everything the Feature is waiting on", () => {
			const row = featureRow(
				makeFeature({
					dependsOn: [
						aDependency({ referenceId: "FTR-90", name: "Warehouse sync" }),
						aDependency({ referenceId: "FTR-91", name: "Card vault" }),
					],
				}),
			);

			expect(row[column("Dependencies")]).toBe(
				"FTR-90: Warehouse sync; FTR-91: Card vault",
			);
		});

		it("still counts a dependency the reader may not see, without naming it", () => {
			const row = featureRow(
				makeFeature({
					dependsOn: [
						aDependency({ isWithheld: true, name: "Secret programme" }),
					],
				}),
			);

			expect(row[column("Dependencies")]).toBe(
				"a Feature you do not have access to",
			);
			expect(row[column("Dependencies")]).not.toContain("Secret programme");
		});

		it("says a Feature needs attention without repeating what the screen says", () => {
			const row = featureRow(makeFeature({ isUsingDefaultFeatureSize: true }));

			expect(row[column("Warnings")]).toBe("Yes");
		});

		it("says so when there is nothing to attend to", () => {
			const row = featureRow(makeFeature());

			expect(row[column("Warnings")]).toBe("No");
		});
	});

	it("follows the order the caller hands the Features in, not the order they were added", () => {
		const table = buildDeliveryExportTable(
			makeDelivery({ features: [11, 12, 13] }),
			[
				makeFeature({ id: 13, name: "Third", referenceId: "FTR-13" }),
				makeFeature({ id: 11, name: "First", referenceId: "FTR-11" }),
				makeFeature({ id: 12, name: "Second", referenceId: "FTR-12" }),
			],
			TEAMS,
			TERMS,
		);

		expect(table.rows.slice(1).map((row) => row[column("Name")])).toEqual([
			"FTR-13: Third",
			"FTR-11: First",
			"FTR-12: Second",
		]);
	});

	it("gives every row a cell for every column, so nothing shifts left in the file", () => {
		const table = buildDeliveryExportTable(
			makeDelivery(),
			[makeFeature(), makeFeature({ id: 12, referenceId: "FTR-12" })],
			TEAMS,
			TERMS,
		);

		expect(table.rows).toHaveLength(3);
		for (const row of table.rows) {
			expect(row).toHaveLength(HEADERS.length);
		}
	});
});
