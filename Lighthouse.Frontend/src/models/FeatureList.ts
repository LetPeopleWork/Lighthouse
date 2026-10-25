import { z } from "zod";
import { FeatureSchema, type IFeature } from "./Feature";

/** How the list arrives from the backend; the rows are turned into Features once they are checked. */
export const FeatureListSchema = z.object({
	features: z.array(FeatureSchema),
	parents: z.array(
		z.object({ referenceId: z.string(), name: z.string(), url: z.string() }),
	),
	activeWork: z.array(
		z.object({ teamId: z.number(), featureReferenceIds: z.array(z.string()) }),
	),
});

/** Whose Features a list shows: a Portfolio's, or the ones a Team works on. */
export type FeatureListOwner =
	| { kind: "portfolio"; id: number }
	| { kind: "team"; id: number };

/** A parent the list's rows point at, named so the list does not have to read it separately. */
export interface IFeatureListParent {
	referenceId: string;
	name: string;
	url: string;
}

/** The Features one Team is working on right now, among the list's rows. */
export interface IFeatureListActiveWork {
	teamId: number;
	featureReferenceIds: string[];
}

/** Everything a Features tab shows, in one answer. */
export interface IFeatureList {
	features: IFeature[];
	parents: IFeatureListParent[];
	activeWork: IFeatureListActiveWork[];
}
