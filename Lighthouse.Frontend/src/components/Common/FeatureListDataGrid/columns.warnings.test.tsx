import { describe, expect, it } from "vitest";
import type { IFeature } from "../../../models/Feature";
import type {
	IFeatureDependency,
	NotHonouredReason,
} from "../../../models/FeatureDependency";
import { createWarningsColumn } from "./columns";

const aDependency = (
	overrides: Partial<IFeatureDependency> = {},
): IFeatureDependency => ({
	referenceId: "FTR-9",
	name: "Warehouse sync",
	url: "https://tracker.example/FTR-9",
	source: "TrackerLink",
	notHonouredReason: null,
	blockerPositionedBelow: false,
	isWithheld: false,
	...overrides,
});

const aRow = (overrides: Partial<IFeature> = {}): IFeature =>
	({
		referenceId: "FTR-1",
		stateCategory: "InProgress",
		isUsingDefaultFeatureSize: false,
		dependsOn: [],
		getRemainingWorkForFeature: () => 0,
		...overrides,
	}) as unknown as IFeature;

const sortsAsWarning = (row: IFeature): boolean =>
	createWarningsColumn().valueGetter?.(undefined, row) as boolean;

// The column is sorted on, and the sort reads this rather than the icons. A row whose only warning is
// about a dependency has to sort with the warned rows, or the column quietly stops being a way to find
// them - and a row whose dependencies were deliberately set aside has to sort with the clear ones, or
// setting them aside would have made every row in the Portfolio look like it needed attention.
describe("createWarningsColumn sorts on everything the icons show", () => {
	it("sorts a row with nothing wrong as clear", () => {
		expect(sortsAsWarning(aRow())).toBe(false);
	});

	it("sorts a Feature marked done with work still on it as warned", () => {
		expect(
			sortsAsWarning(
				aRow({
					stateCategory: "Done",
					getRemainingWorkForFeature: () => 3,
				}),
			),
		).toBe(true);
	});

	it("sorts a Feature marked done with no work left as clear", () => {
		expect(
			sortsAsWarning(
				aRow({
					stateCategory: "Done",
					getRemainingWorkForFeature: () => 0,
				}),
			),
		).toBe(false);
	});

	it("sorts a Feature falling back on the default size as warned", () => {
		expect(sortsAsWarning(aRow({ isUsingDefaultFeatureSize: true }))).toBe(
			true,
		);
	});

	it("sorts a row with an ordinary dependency as clear", () => {
		expect(sortsAsWarning(aRow({ dependsOn: [aDependency()] }))).toBe(false);
	});

	it.each<NotHonouredReason>([
		"OutsideThisPortfolio",
		"InALoop",
		"BlockerCannotBeForecast",
	])("sorts a dependency that is %s as warned", (notHonouredReason) => {
		expect(
			sortsAsWarning(aRow({ dependsOn: [aDependency({ notHonouredReason })] })),
		).toBe(true);
	});

	it("sorts a dependency waiting on one positioned below it as warned", () => {
		expect(
			sortsAsWarning(
				aRow({ dependsOn: [aDependency({ blockerPositionedBelow: true })] }),
			),
		).toBe(true);
	});

	it("sorts a dependency the Portfolio set aside as clear", () => {
		expect(
			sortsAsWarning(
				aRow({
					dependsOn: [
						aDependency({ notHonouredReason: "IgnoredByPortfolio" }),
						aDependency({
							referenceId: "FTR-8",
							notHonouredReason: "IgnoredByPortfolio",
							blockerPositionedBelow: true,
						}),
					],
				}),
			),
		).toBe(false);
	});

	it("sorts a row as warned when one dependency is worth reporting and another is set aside", () => {
		expect(
			sortsAsWarning(
				aRow({
					dependsOn: [
						aDependency({ notHonouredReason: "IgnoredByPortfolio" }),
						aDependency({ referenceId: "FTR-8", notHonouredReason: "InALoop" }),
					],
				}),
			),
		).toBe(true);
	});

	it("sorts a row that has no dependency list at all as clear", () => {
		expect(sortsAsWarning(aRow({ dependsOn: undefined }))).toBe(false);
	});
});
