/**
 * What an empty over-time chart says, shared by both over-time widgets.
 *
 * An empty series comes back for four different reasons, and nothing in the answer
 * says which: the range predates everything held for this owner, nothing is held at
 * all, syncing stopped before the range began, or the past days are still being
 * filled in the background after this first open. The range the widget asked for
 * does not separate them either — every one of them can happen for a range that
 * ends today and for one that ended in the past. So there is one sentence, and it
 * has to be true whichever of the four is behind it.
 */
export const OVER_TIME_EMPTY_COPY =
	"Nothing to show for the selected range. Days the stored history covers can fill in on a later visit; days it does not cover stay empty.";
