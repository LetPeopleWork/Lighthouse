import type {
	ISizingLogEntry,
	SizingAnswer,
	SizingEntryKind,
} from "../../../../models/Refinement/Refinement";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import { ApiError } from "../../../../services/Api/ApiError";
import { formatDayAndDate } from "./nextRefinementWording";

const NOT_FOUND = 404;

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

/** Why a Work Item's log could not be read; the server finds no log for a Work Item that left refinement. */
export const describeLogFailure = (
	error: unknown,
	getTerm: (key: string) => string,
): string => {
	if (error instanceof ApiError && error.code === NOT_FOUND) {
		return `That ${getTerm(TERMINOLOGY_KEYS.WORK_ITEM).toLowerCase()} is no longer in ${getTerm(TERMINOLOGY_KEYS.REFINEMENT).toLowerCase()}.`;
	}

	return error instanceof Error ? error.message : String(error);
};
