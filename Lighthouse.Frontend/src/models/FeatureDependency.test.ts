import { describe, expect, it } from "vitest";
import {
	FeatureDependencySchema,
	hasNothingWrongWithIt,
	type IFeatureDependency,
	isSetAside,
	isWorthWarningAbout,
	NOT_HONOURED_REASONS,
} from "./FeatureDependency";

const theServerSaid = (extra: Record<string, unknown> = {}) => ({
	referenceId: "FTR-9",
	name: "Warehouse sync",
	source: "TrackerLink",
	...extra,
});

const aDependency = (
	overrides: Partial<IFeatureDependency> = {},
): IFeatureDependency => ({
	referenceId: "FTR-9",
	name: "Warehouse sync",
	url: null,
	source: "TrackerLink",
	notHonouredReason: null,
	blockerPositionedBelow: false,
	isWithheld: false,
	...overrides,
});

describe("FeatureDependencySchema", () => {
	// Everything a payload may leave out has to read as the harmless answer. Any of these arriving as
	// undefined would otherwise make a row claim something is wrong with a dependency that is fine.
	it("reads a payload carrying only the required fields as an ordinary dependency", () => {
		const dependency = FeatureDependencySchema.parse(theServerSaid());

		expect(dependency).toEqual({
			referenceId: "FTR-9",
			name: "Warehouse sync",
			url: null,
			source: "TrackerLink",
			notHonouredReason: null,
			blockerPositionedBelow: false,
			isWithheld: false,
		});
	});

	it("keeps every value a payload does carry", () => {
		const dependency = FeatureDependencySchema.parse(
			theServerSaid({
				url: "https://tracker.example/FTR-9",
				source: "PortfolioField",
				notHonouredReason: "IgnoredByPortfolio",
				blockerPositionedBelow: true,
				isWithheld: true,
			}),
		);

		expect(dependency).toEqual({
			referenceId: "FTR-9",
			name: "Warehouse sync",
			url: "https://tracker.example/FTR-9",
			source: "PortfolioField",
			notHonouredReason: "IgnoredByPortfolio",
			blockerPositionedBelow: true,
			isWithheld: true,
		});
	});

	it("reads an explicit null url as no url rather than as undefined", () => {
		expect(
			FeatureDependencySchema.parse(theServerSaid({ url: null })).url,
		).toBe(null);
	});

	// A reader meeting a reason nobody has heard of would have to guess, and the guess this exists to
	// prevent is "it's fine".
	it("refuses a reason the server has no business sending", () => {
		expect(() =>
			FeatureDependencySchema.parse(
				theServerSaid({ notHonouredReason: "ProbablyFine" }),
			),
		).toThrow();
	});

	it("refuses a source the server has no business sending", () => {
		expect(() =>
			FeatureDependencySchema.parse(theServerSaid({ source: "Guesswork" })),
		).toThrow();
	});
});

describe("hasNothingWrongWithIt", () => {
	it("holds only for a dependency with no reason against it and nothing odd about the order", () => {
		expect(hasNothingWrongWithIt(aDependency())).toBe(true);
		expect(
			hasNothingWrongWithIt(aDependency({ blockerPositionedBelow: true })),
		).toBe(false);
		expect(
			hasNothingWrongWithIt(aDependency({ notHonouredReason: "InALoop" })),
		).toBe(false);
	});
});

describe("isSetAside", () => {
	it("holds for the one reason that is a choice rather than a fault", () => {
		expect(
			isSetAside(aDependency({ notHonouredReason: "IgnoredByPortfolio" })),
		).toBe(true);

		for (const reason of NOT_HONOURED_REASONS.filter(
			(candidate) => candidate !== "IgnoredByPortfolio",
		)) {
			expect(isSetAside(aDependency({ notHonouredReason: reason }))).toBe(
				false,
			);
		}

		expect(isSetAside(aDependency())).toBe(false);
	});
});

describe("isWorthWarningAbout", () => {
	it("says nothing about a dependency that is fine, or one somebody set aside", () => {
		expect(isWorthWarningAbout(aDependency())).toBe(false);
		expect(
			isWorthWarningAbout(
				aDependency({ notHonouredReason: "IgnoredByPortfolio" }),
			),
		).toBe(false);
		expect(
			isWorthWarningAbout(
				aDependency({
					notHonouredReason: "IgnoredByPortfolio",
					blockerPositionedBelow: true,
				}),
			),
		).toBe(false);
	});

	it("warns about every reason that is a fault, and about an odd order", () => {
		for (const reason of NOT_HONOURED_REASONS.filter(
			(candidate) => candidate !== "IgnoredByPortfolio",
		)) {
			expect(
				isWorthWarningAbout(aDependency({ notHonouredReason: reason })),
			).toBe(true);
		}

		expect(
			isWorthWarningAbout(aDependency({ blockerPositionedBelow: true })),
		).toBe(true);
	});
});
