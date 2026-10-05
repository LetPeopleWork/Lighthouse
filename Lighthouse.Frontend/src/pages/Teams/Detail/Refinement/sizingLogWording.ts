import type {
	ISizingLogEntry,
	SizingAnswer,
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

export const describeAnswer = (answer: SizingAnswer): string =>
	ANSWER_WORDING[answer];

/**
 * Whether an entry is something somebody wrote: a comment, or a vote that came with words. The list
 * leaves out votes without words and votes taken back, since the split above it already says how
 * everybody stands.
 */
export const isWritten = ({ kind, comment }: ISizingLogEntry): boolean =>
	kind !== "Revocation" && comment !== null;

/**
 * Who wrote it, as "Ana Lima · Yes, if…" for a vote and "Ana Lima" for a comment. Where it came from is
 * deliberately never worded: every reader sees the same list.
 */
export const describeWriter = ({
	voterName,
	answer,
}: ISizingLogEntry): string =>
	answer === null ? voterName : `${voterName} · ${describeAnswer(answer)}`;

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
