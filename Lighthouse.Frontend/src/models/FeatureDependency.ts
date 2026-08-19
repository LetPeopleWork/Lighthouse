import { z } from "zod";
import {
	EntityReferenceSchema,
	type IEntityReference,
} from "./EntityReference";

// Why Lighthouse will not act on a dependency. The set is closed on the server; a reader meeting a
// value nobody has heard of would have to guess, and the guess this exists to prevent is "it's fine".
export const NOT_HONOURED_REASONS = [
	"OutsideThisPortfolio",
	"InALoop",
	"BlockerCannotBeForecast",
] as const;

export type NotHonouredReason = (typeof NOT_HONOURED_REASONS)[number];

export type DependencySource = "TrackerLink" | "PortfolioField";

// One thing worth telling the reader about a dependency, as codes and a name. Every sentence a user
// reads is built here in their own instance's words, so nothing arrives pre-written.
export interface IFeatureDependencyWarning {
	blockerReferenceId: string;
	blockerName: string;
	isWithheld: boolean;
	notHonouredReason: NotHonouredReason | null;
	blockerPositionedBelow: boolean;
}

export const FeatureDependencyWarningSchema = z.object({
	blockerReferenceId: z.string(),
	blockerName: z.string(),
	isWithheld: z.boolean().optional().default(false),
	notHonouredReason: z
		.enum(NOT_HONOURED_REASONS)
		.nullable()
		.optional()
		.transform((reason) => reason ?? null),
	blockerPositionedBelow: z.boolean().optional().default(false),
});

// One Feature another is waiting on. A withheld entry is one the reader may not see: it says that
// something is being waited on and nothing else, and is listed rather than dropped so the list keeps
// accounting for the count on the row.
export interface IFeatureDependency {
	id: number;
	referenceId: string;
	name: string;
	state: string;
	url: string | null;
	source: DependencySource;
	notHonouredReason: NotHonouredReason | null;
	isWithheld: boolean;
	portfolios: IEntityReference[];
}

export const FeatureDependencySchema = z.object({
	id: z.number(),
	referenceId: z.string(),
	name: z.string(),
	state: z.string(),
	url: z.string().nullable().optional(),
	source: z.enum(["TrackerLink", "PortfolioField"]),
	notHonouredReason: z.enum(NOT_HONOURED_REASONS).nullable().optional(),
	isWithheld: z.boolean().optional().default(false),
	portfolios: z.array(EntityReferenceSchema).optional().default([]),
});

export const deserializeFeatureDependencies = (
	data: unknown,
): IFeatureDependency[] =>
	z
		.array(FeatureDependencySchema)
		.parse(data)
		.map((entry) => ({
			...entry,
			url: entry.url ?? null,
			notHonouredReason: entry.notHonouredReason ?? null,
		}));
