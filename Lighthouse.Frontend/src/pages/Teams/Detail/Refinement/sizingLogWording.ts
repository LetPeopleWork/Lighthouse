import type {
	ISizingLogEntry,
	SizingAnswer,
	SizingEntryKind,
} from "../../../../models/Refinement/Refinement";
import { formatDayAndDate } from "./nextRefinementWording";

const ANSWER_WORDING: Record<SizingAnswer, string> = {
	Yes: "Yes",
	YesBut: "Yes, if…",
	No: "No",
};

const describeVote = ({ voterName, answer }: ISizingLogEntry): string =>
	answer === null ? voterName : `${voterName} voted ${ANSWER_WORDING[answer]}`;

/** Where an entry came from is deliberately never worded: every reader sees the same log. */
const ENTRY_WORDING: Record<
	SizingEntryKind,
	(entry: ISizingLogEntry) => string
> = {
	Vote: describeVote,
	Comment: ({ voterName }) => voterName,
	Revocation: ({ voterName }) => `${voterName} took back their vote`,
};

/** Who did what, as "Ana Lima voted Yes, if…"; a comment's own text is shown beneath, not here. */
export const describeLogEntry = (entry: ISizingLogEntry): string =>
	ENTRY_WORDING[entry.kind](entry);

/** The day an entry was recorded, as "Wed 7 Oct". */
export const describeLogDay = ({ recordedAt }: ISizingLogEntry): string =>
	formatDayAndDate(new Date(recordedAt));
