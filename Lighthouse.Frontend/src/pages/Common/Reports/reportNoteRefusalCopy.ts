import { ApiError } from "../../../services/Api/ApiError";

const NEEDS_TEXT = "A note needs some text.";
const AT_MOST_2000 = "A note can have at most 2000 characters.";
const COULD_NOT_SAVE = "This note couldn't be saved. Try again.";

/** What to tell the user when the server did not keep a note's text. */
export const refusalCopyOf = (error: unknown): string => {
	if (error instanceof ApiError) {
		if (error.problemCode === "note-text-missing") return NEEDS_TEXT;
		if (error.problemCode === "note-text-too-long") return AT_MOST_2000;
	}
	return COULD_NOT_SAVE;
};
