export type RefreshFindingKind =
	| "NamedLinkMatchedNothing"
	| "NamedLinkPointsBothWays"
	| "RelationTypeHasNoDirection"
	| "RelationTypeNamedWithoutItsEnd"
	| "RelationTypeCannotBeReadHere"
	| "ParentSourceMatchedNothing";

export type RefreshFindingSetting =
	| "ParentOverrideField"
	| "DependencyField"
	| "SizeEstimateField"
	| "FeatureOwnerField"
	| "EstimationField";

// Something the latest full download of an entity had to say about a link or relation type one of its
// settings names: which setting, what it named, and what was seen instead.
export interface RefreshFinding {
	kind: RefreshFindingKind;
	setting: RefreshFindingSetting;
	named: string;
	seen: string[];
	// Whether a parent source had another source after it in the run that raised the finding. Optional because
	// a finding stored before this was recorded has none, and then nothing is claimed about what came next.
	hadANextSource?: boolean;
}

export interface RefreshLog {
	id: number;
	type: "Team" | "Portfolio" | "Forecast";
	entityId: number;
	entityName: string;
	itemCount: number;
	durationMs: number;
	executedAt: string;
	success: boolean;
	cancelled: boolean;
	// Optional because a backend older than this field sends a payload without it, and a newer UI
	// still has to render that payload rather than fail on it.
	recordsWhoseLinksNamedMoreThanOneParent?: number;
	// Optional for the same reason as the field above.
	recordsPlacedFromLinksNamingMoreThanOneParent?: number;
	// The findings of the entity's latest full download and when it ran, the same on every row of that
	// entity: a refresh that downloads only what moved sees too little to say anything is missing.
	// Optional for the same reason as the fields above.
	latestFullRunFindings?: RefreshFinding[];
	latestFullRunAt?: string;
	// A forecast whose inputs had not changed since the last one, so nothing was computed. Optional for the
	// same reason as the fields above.
	skipped?: boolean;
}
