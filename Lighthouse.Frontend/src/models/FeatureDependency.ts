import { z } from "zod";

// Why Lighthouse will not act on a dependency. The set is closed on the server; a reader meeting a
// value nobody has heard of would have to guess, and the guess this exists to prevent is "it's fine".
export const NOT_HONOURED_REASONS = [
	"OutsideThisPortfolio",
	"InALoop",
	"BlockerCannotBeForecast",
	"IgnoredByPortfolio",
] as const;

export type NotHonouredReason = (typeof NOT_HONOURED_REASONS)[number];

export type DependencySource = "TrackerLink" | "PortfolioField";

// One Feature another is waiting on, as the row names it. A withheld entry is one the reader may not
// see: it says that something is being waited on and nothing else, and is listed rather than dropped,
// because a shorter list is one the reader has no way of telling is short.
export interface IFeatureDependency {
	referenceId: string;
	name: string;
	url: string | null;
	source: DependencySource;
	notHonouredReason: NotHonouredReason | null;
	blockerPositionedBelow: boolean;
	isWithheld: boolean;
}

export const FeatureDependencySchema = z.object({
	referenceId: z.string(),
	name: z.string(),
	url: z
		.string()
		.nullable()
		.optional()
		.transform((url) => url ?? null),
	source: z.enum(["TrackerLink", "PortfolioField"]),
	notHonouredReason: z
		.enum(NOT_HONOURED_REASONS)
		.nullable()
		.optional()
		.transform((reason) => reason ?? null),
	blockerPositionedBelow: z.boolean().optional().default(false),
	isWithheld: z.boolean().optional().default(false),
});

// Nothing is wrong with a dependency when Lighthouse can act on it and it does not sit below the
// Feature waiting on it. Asked here so the row and the warnings column cannot disagree about it.
export const hasNothingWrongWithIt = (
	dependency: IFeatureDependency,
): boolean =>
	dependency.notHonouredReason === null && !dependency.blockerPositionedBelow;

// A Portfolio that has set its dependencies aside made a choice; it did not break a link. Warning about
// every Feature in it would teach the reader to stop looking at a column built to be worth looking at,
// so this is the one reason that says nothing on the row and says it on the entry instead.
export const isSetAside = (dependency: IFeatureDependency): boolean =>
	dependency.notHonouredReason === "IgnoredByPortfolio";

export const isWorthWarningAbout = (dependency: IFeatureDependency): boolean =>
	!isSetAside(dependency) && !hasNothingWrongWithIt(dependency);
