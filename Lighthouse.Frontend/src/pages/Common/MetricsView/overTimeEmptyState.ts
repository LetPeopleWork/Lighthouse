/**
 * What an empty over-time chart says, shared by both over-time widgets.
 *
 * An empty series comes back for several reasons, and nothing in the answer says which:
 * the range predates everything held for this owner, nothing is held at all, syncing
 * stopped before the range began, or past days are still being filled in from the stored
 * history. The range the widget asked for does not separate them either, since each can
 * happen for a range that ends today and for one that ended in the past. So there is one
 * sentence, and it has to be true whichever is behind it.
 *
 * A filled-in day is written into the same store as a day recorded live, so to the chart
 * both are days Lighthouse has recorded, and the sentence names neither. It still promises
 * nothing about a later visit, because a range the stored history does not reach is never
 * filled in.
 */
export const OVER_TIME_EMPTY_COPY =
	"Nothing to show for the selected range. Days appear here as Lighthouse records them.";
