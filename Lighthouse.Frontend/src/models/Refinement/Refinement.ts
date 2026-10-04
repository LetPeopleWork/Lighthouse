/** One state a Team admin chose as a refinement state. */
export interface IRefinementStateSetting {
	state: string;
}

/**
 * How many No votes, and how many "Yes, if…" votes, send a Work Item to discussion. Either one is
 * enough; null switches that rule off.
 */
export interface IDiscussWhenSetting {
	no: number | null;
	yesIf: number | null;
}

/** How many votes make a Work Item Ready, and what sends it to discussion instead. */
export interface IReadinessSetting {
	minYes: number;
	minVoters: number;
	/** Absent when stored before the discussion rules existed: both rules apply at their defaults. */
	discussWhen?: IDiscussWhenSetting;
}

export interface IRefinementSettings {
	states: IRefinementStateSetting[];
	/** Absent from a save means "leave as it is". */
	readiness?: IReadinessSetting;
}

/** The three answers to "doable within our SLE?". A reader sees YesBut labelled "Yes, if…". */
export type SizingAnswer = "Yes" | "YesBut" | "No";

/** Where an entry was cast from, as the caller declares it. */
export type SizingChannel = "Web" | "LiveSession" | "Cli" | "Assistant";

/** What a row's votes make of it. */
export type RowReadiness =
	| "Ready"
	| "MoreYesNeeded"
	| "MoreVotersNeeded"
	| "NeedsDiscussion";

/** How the votes on a row split; every reader is told, voted or not. */
export interface ISizingSplit {
	yes: number;
	yesBut: number;
	no: number;
}

export interface IRefinementRow {
	referenceId: string;
	name: string;
	url: string | null;
	state: string;
	/** The parent's reference, or an empty string when the Work Item has no parent. */
	parentReferenceId: string;
	voteCount?: number;
	myVote?: SizingAnswer | null;
	split?: ISizingSplit;
	readiness?: RowReadiness;
	/** How many more Yes votes or voters the row needs; null when Ready or in discussion. */
	missingVotes?: number | null;
	hasComments?: boolean;
	hasOpenQuestion?: boolean;
}

/** What the votes are cast against: the Team's SLE, a fallback from its cycle time, or nothing. */
export type YardstickSource = "Sle" | "CycleTimeFallback" | "Unavailable";

export interface IYardstick {
	source: YardstickSource;
	days: number | null;
	probability: number | null;
}

/** How this instance knows a voter: by the account signed in, or by a name the voter declares. */
export type VoterIdentity = "Account" | "SelfDeclared";

export interface IRefinementView {
	refinementConfigured: boolean;
	workItems: IRefinementRow[];
	yardstick?: IYardstick;
	voterIdentity?: VoterIdentity;
	readyByVotesCount?: number;
}

export type SizingEntryKind = "Vote" | "Comment" | "Revocation";

export interface ISizingLogEntry {
	kind: SizingEntryKind;
	answer: SizingAnswer | null;
	comment: string | null;
	voterName: string;
	channel: SizingChannel;
	recordedAt: string;
	isMine: boolean;
}

/** One Work Item's log, oldest first, open to every reader whether they voted or not. */
export interface ISizingLog {
	entries: ISizingLogEntry[];
}

export interface ISizingVote {
	answer: SizingAnswer;
	channel: SizingChannel;
	comment?: string;
	voterName?: string;
}

export interface ISizingComment {
	comment: string;
	channel: SizingChannel;
	voterName?: string;
}
