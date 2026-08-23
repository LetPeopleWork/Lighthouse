import { z } from "zod";
import { Feature, FeatureSchema } from "../Feature";

/**
 * One way this Portfolio's connection lets a Delivery take its date from the work tracking system.
 * A connection that offers none of these answers with an empty list, which is a normal answer and
 * not a failure — most connections have nothing a date could be bound to.
 */
export const DeliverySourceSchema = z.object({
	key: z.string(),
	displayName: z.string(),
});

export type IDeliverySource = z.infer<typeof DeliverySourceSchema>;

/**
 * Why an offered source cannot be bound. Two members rather than one flag, because they send the
 * reader somewhere different: one is fixed by giving the Release a date in the tracker, the other by
 * picking a different Release. A value outside this set is rejected here, so no screen can end up
 * printing a raw server word at a user.
 */
export const SourceOptionBlockReasonSchema = z.enum([
	"NoDateSet",
	"RetiredAtSource",
]);

export type SourceOptionBlockReason = z.infer<
	typeof SourceOptionBlockReasonSchema
>;

/**
 * Why a preview came back with nothing in it. An empty list on its own leaves the reader guessing
 * between two problems fixed in completely different places — one on the board, by tagging the work,
 * and one in Lighthouse, by widening what this Portfolio covers.
 */
export const DeliverySourcePreviewEmptyReasonSchema = z.enum([
	"None",
	"NothingTaggedAgainstTheSource",
	"TaggedWorkNotTrackedByThisPortfolio",
]);

export type DeliverySourcePreviewEmptyReason = z.infer<
	typeof DeliverySourcePreviewEmptyReasonSchema
>;

const wireDate = z
	.string()
	.refine((value) => !Number.isNaN(Date.parse(value)))
	.transform((value) => new Date(value));

/**
 * A date the work tracking system may simply not hold. It is read by hand rather than coerced, and
 * that is deliberate: the usual coercion turns an absent value into 1 January 1970, so a Release
 * nobody has dated would read as one that shipped decades ago instead of one still waiting for
 * someone to give it a date. On a real board most Releases look like this, so it is the common case
 * rather than an edge one. A missing date has to survive this boundary as a missing date.
 */
const absentableWireDate = wireDate
	.nullish()
	.transform((value) => value ?? null);

const absentableBlockReason = SourceOptionBlockReasonSchema.nullish().transform(
	(value) => value ?? null,
);

/**
 * One thing a Delivery could bind its date to. The project travels with it because two projects on
 * one connection routinely name a Release the same thing, and a picker showing bare names would
 * offer two identical rows with no way to tell them apart. Selectability is the server's verdict and
 * is never worked out again here.
 */
export const DeliverySourceOptionSchema = z.object({
	id: z.string(),
	name: z.string(),
	date: absentableWireDate,
	projectKey: z.string(),
	projectName: z.string(),
	isSelectable: z.boolean(),
	blockedBecause: absentableBlockReason,
});

export type IDeliverySourceOption = z.infer<typeof DeliverySourceOptionSchema>;

/**
 * What binding a Delivery to this source would mean right now: the name and date it would take, and
 * the Features that would come along with it. The rows are ordinary Features, so the existing
 * Feature grid renders a preview with no grid of its own.
 */
export const DeliverySourcePreviewSchema = z.object({
	name: z.string(),
	date: wireDate,
	features: z
		.array(FeatureSchema)
		.transform((rows) => rows.map(Feature.fromParsed)),
	emptyBecause: DeliverySourcePreviewEmptyReasonSchema,
});

export type IDeliverySourcePreview = z.infer<
	typeof DeliverySourcePreviewSchema
>;
