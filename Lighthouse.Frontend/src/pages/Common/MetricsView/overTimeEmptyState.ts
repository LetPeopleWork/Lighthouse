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
 * It also has to hold whether filling in past days is switched on or off, and the widgets
 * cannot tell which. They do not need to: a filled-in day is written into the same store
 * as a day recorded live, so to the chart both are days Lighthouse has recorded. That is
 * why the sentence promises nothing about a later visit. With filling switched off, no
 * past day ever arrives.
 */
export const OVER_TIME_EMPTY_COPY =
	"Nothing to show for the selected range. Days appear here as Lighthouse records them.";
