import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type {
	ConnectionDefaultName,
	WorkTrackingSystemType,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import type { FieldInfo } from "../FormSelectField/FormSelectField";
import { nameOfParentSource, nameOfTracker } from "./parentSources";

/** The settings whose field is read off one end of a link, so the tip has to say which end. */
type DirectionTipField = Extract<
	ConnectionDefaultName,
	| "parentOverrideForTeams"
	| "parentOverrideForPortfolios"
	| "dependencyFieldForPortfolios"
>;

type ParentSourcesTipField = Exclude<
	DirectionTipField,
	"dependencyFieldForPortfolios"
>;

/** The tenant's own words the tips speak in. */
export interface DirectionTipTerms {
	feature: string;
	workItem: string;
}

export const directionTipTermsFrom = (
	getTerm: (key: string) => string,
): DirectionTipTerms => ({
	feature: getTerm(TERMINOLOGY_KEYS.FEATURE),
	workItem: getTerm(TERMINOLOGY_KEYS.WORK_ITEM),
});

/** The field list's own info, with the docs link a direction tip always carries. */
type DirectionTip = Required<FieldInfo>;

const DOCS = "https://docs.lighthouse.letpeople.work";

type TrackerExamples = Partial<Record<WorkTrackingSystemType, string>>;

interface FieldDirection {
	whichEnd: (terms: DirectionTipTerms) => string;
	examples: TrackerExamples;
	none?: string;
	learnMoreUrl: string;
}

const parentOverrideExamples: TrackerExamples = {
	Jira: 'In Jira, pick the link phrase read towards the parent, such as "is caused by".',
	AzureDevOps: "In Azure DevOps, pick a field holding the parent's id.",
};

const directions: Record<DirectionTipField, FieldDirection> = {
	parentOverrideForTeams: {
		whichEnd: ({ workItem }) =>
			`Read on each ${workItem}, this names the link or field that points to its parent.`,
		examples: parentOverrideExamples,
		learnMoreUrl: `${DOCS}/teams/edit.html#parent-override-field`,
	},
	parentOverrideForPortfolios: {
		whichEnd: ({ feature }) =>
			`Read on each ${feature}, this names the link or field that points to its parent.`,
		examples: parentOverrideExamples,
		learnMoreUrl: `${DOCS}/portfolios/edit.html#parent-override-field`,
	},
	dependencyFieldForPortfolios: {
		whichEnd: ({ feature }) =>
			`Read on the ${feature} that waits, this names what it waits on.`,
		examples: {
			Jira: 'In Jira, pick the phrase on the waiting end, "is blocked by", not "blocks".',
			AzureDevOps:
				"In Azure DevOps, pick the Reverse end of the dependency link, Predecessor.",
		},
		none: "With None, the tracker's own links are read; a field you choose replaces them rather than adding to them.",
		learnMoreUrl: `${DOCS}/portfolios/edit.html#dependency-field`,
	},
};

const readsOneEndOfALink = (
	setting: ConnectionDefaultName,
): setting is DirectionTipField => Object.keys(directions).includes(setting);

const tipOf = (
	direction: FieldDirection,
	tracker: WorkTrackingSystemType | undefined,
	terms: DirectionTipTerms,
	lastSentence: string | undefined,
): DirectionTip => {
	const example = tracker ? direction.examples[tracker] : undefined;
	return {
		text: [direction.whichEnd(terms), example, lastSentence]
			.filter(Boolean)
			.join(" "),
		learnMoreUrl: direction.learnMoreUrl,
	};
};

/**
 * Which end of a link the setting's field reads, with the tracker's own example where the tracker is known, and
 * where the docs explain it; undefined for a setting whose field reads no link. One place for the copy, because
 * the connection page and the Team and Portfolio settings both show it and must never drift apart.
 */
export const directionTip = (
	setting: ConnectionDefaultName,
	tracker: WorkTrackingSystemType | undefined,
	terms: DirectionTipTerms,
): DirectionTip | undefined => {
	if (!readsOneEndOfALink(setting)) {
		return undefined;
	}
	return tipOf(directions[setting], tracker, terms, directions[setting].none);
};

/** The tip beside the ordered list, which has no None: the tracker's own parent is a row of it. */
export const parentSourcesTip = (
	setting: ParentSourcesTipField,
	tracker: WorkTrackingSystemType,
	terms: DirectionTipTerms,
): DirectionTip =>
	tipOf(
		directions[setting],
		tracker,
		terms,
		`${nameOfParentSource({ kind: "TrackerParent" }, tracker, [])} is the parent ${nameOfTracker(tracker)} itself records.`,
	);
