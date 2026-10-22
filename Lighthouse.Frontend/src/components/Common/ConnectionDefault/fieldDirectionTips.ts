import type {
	ConnectionDefaultName,
	WorkTrackingSystemType,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";

export const __SCAFFOLD__ = true;

/** The settings whose field is read off one end of a link, so the tip has to say which end. */
export type DirectionTipField = Extract<
	ConnectionDefaultName,
	| "parentOverrideForTeams"
	| "parentOverrideForPortfolios"
	| "dependencyFieldForPortfolios"
>;

/** The tenant's own words the tips speak in. */
export interface DirectionTipTerms {
	feature: string;
	workItem: string;
}

export interface DirectionTip {
	text: string;
	learnMoreUrl: string;
}

/**
 * Which end of a link the field reads, with the tracker's own example where the tracker is known, and where the
 * docs explain it. One place for the copy, because the connection page and the Team and Portfolio settings both
 * show it and must never drift apart.
 */
export const directionTip = (
	_field: DirectionTipField,
	_tracker: WorkTrackingSystemType | undefined,
	_terms: DirectionTipTerms,
): DirectionTip => {
	throw new Error("Not yet implemented -- RED scaffold");
};
