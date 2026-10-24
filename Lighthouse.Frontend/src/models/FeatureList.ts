import type { IFeature } from "./Feature";

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
