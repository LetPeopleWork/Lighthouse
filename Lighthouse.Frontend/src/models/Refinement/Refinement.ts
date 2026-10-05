import type { DayOfWeek } from "../RecurringBlackoutRule";
import type { IWorkItemRuleSet } from "../WorkItemRules";

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

/**
 * The optional rules that put a Work Item in refinement into a stage. Null switches that stage's rule
 * off; a Work Item neither rule matches is Waiting.
 */
export interface IStageRulesSetting {
	ready: IWorkItemRuleSet | null;
	beingRefined: IWorkItemRuleSet | null;
}

/** The days a Team refines on: these weekdays, every so many weeks, counted from the starting week. */
export interface IRefinementCadenceSetting {
	weekdays: DayOfWeek[];
	intervalWeeks: number;
	/** The Monday of the starting week; only needed when the Team refines less often than weekly. */
	anchorWeek?: string | null;
}

/** The likelihoods the Team's forecast is read at for the low and high end of the range. */
export interface IRefinementBandSetting {
	lowPercentile: number;
	highPercentile: number;
}

export interface IRefinementSettings {
	states: IRefinementStateSetting[];
	/** Absent from a save means "leave as it is". */
	readiness?: IReadinessSetting;
	/** Absent from a save means "leave as it is". */
	stageRules?: IStageRulesSetting | null;
	/** Absent from a save means "leave as it is"; null when the Team has no cadence. */
	cadence?: IRefinementCadenceSetting | null;
	/** Absent from a save means "leave as it is". */
	band?: IRefinementBandSetting;
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
	/** The stage the Team's rules give the row; null when the Team sets no stage rule. */
	stage?: RefinementStage | null;
	/** Whether the stage and the votes tell a different story about the row. */
	signalsDisagree?: boolean;
}

export type RefinementStage = "Waiting" | "BeingRefined" | "Ready";

/** Which signal the ready count follows: the votes, or the stages once the Team sets a stage rule. */
export type ReadySource = "Votes" | "Stages";

export type RefinementVerdict = "Below" | "In" | "Above";

/** Why the tab cannot say how many Work Items are needed. */
export type NeedUnavailableReason =
	| "NoCadence"
	| "InsufficientData"
	| "NoRefinementStates";

/**
 * How many Work Items the Team is likely to pull before its next Refinement, and what the ready count
 * makes of it. Facts only; the words are the browser's.
 */
export interface IRefinementNeed {
	verdict: RefinementVerdict | null;
	unavailableReason: NeedUnavailableReason | null;
	low: number | null;
	high: number | null;
	lowPercentile: number | null;
	highPercentile: number | null;
	horizonWorkingDays: number | null;
}

/** The row as a vote left it, and whether that vote is the one that moved it to Ready. */
export interface IVotedRow extends IRefinementRow {
	madeReady: boolean;
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
	yardstick: IYardstick;
	voterIdentity?: VoterIdentity;
	readyByVotesCount?: number;
	stagesConfigured?: boolean;
	readyCount?: number;
	readySource?: ReadySource;
	/** The calendar day of the next Refinement; null when the Team has no cadence. */
	nextRefinementDate?: string | null;
	/** Calendar days from the instance's today to the next Refinement, at least 1; null without a cadence. */
	daysUntilNextRefinement?: number | null;
	isRefinementDay?: boolean;
	need?: IRefinementNeed;
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
	/** The server's call: whether this comment still waits for its author to vote. */
	isOpenQuestion: boolean;
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
