import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type {
	ConnectionDefaultName,
	WorkTrackingSystemType,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import type { FieldInfo } from "../FormSelectField/FormSelectField";

/** The settings whose field is read off one end of a link, so the tip has to say which end. */
type DirectionTipField = Extract<
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
	none: string;
	learnMoreUrl: string;
}

const parentOverrideExamples: TrackerExamples = {
	Jira: 'In Jira, pick the link phrase read towards the parent, such as "is caused by".',
	AzureDevOps: "In Azure DevOps, pick a field holding the parent's id.",
};

const parentOverrideNone =
	"With None, the parent the tracker itself records is used.";

const directions: Record<DirectionTipField, FieldDirection> = {
	parentOverrideForTeams: {
		whichEnd: ({ workItem }) =>
			`Read on each ${workItem}, this names the link or field that points to its parent.`,
		examples: parentOverrideExamples,
		none: parentOverrideNone,
		learnMoreUrl: `${DOCS}/teams/edit.html#parent-override-field`,
	},
	parentOverrideForPortfolios: {
		whichEnd: ({ feature }) =>
			`Read on each ${feature}, this names the link or field that points to its parent.`,
		examples: parentOverrideExamples,
		none: parentOverrideNone,
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
	const direction = directions[setting];
	const example = tracker ? direction.examples[tracker] : undefined;
	return {
		text: [direction.whichEnd(terms), example, direction.none]
			.filter(Boolean)
			.join(" "),
		learnMoreUrl: direction.learnMoreUrl,
	};
};
