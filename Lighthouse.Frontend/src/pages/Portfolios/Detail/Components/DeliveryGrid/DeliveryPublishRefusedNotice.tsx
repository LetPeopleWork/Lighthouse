import { Alert, AlertTitle, Typography } from "@mui/material";
import type React from "react";

interface DeliveryPublishRefusedNoticeProps {
	/** What the source said, in its own words. */
	reason: string;
	/** What the connection calls this kind of source, e.g. "Jira Release". */
	sourceLabel: string;
	refusedOn: string | null;
	/** The tenant's own word for a Delivery - it may well be "Launch". */
	deliveryTerm: string;
}

export const PUBLISH_REFUSED_TITLE = "Forecast not published";

/**
 * Shown on a Delivery whose forecast the source refused to take. It exists because the alternative is
 * a switch that was turned on and appears to do nothing at all: the Release simply never changes, and
 * nothing anywhere says why.
 *
 * Deliberately milder than the source-unavailable notice beside it, and deliberately separate. That
 * one says the values on the row are frozen and nothing is maintaining them; this one says the
 * Delivery is perfectly healthy and one optional thing it was asked to do elsewhere did not happen.
 * Merging them would have a working Delivery read as a broken one.
 */
const DeliveryPublishRefusedNotice: React.FC<
	DeliveryPublishRefusedNoticeProps
> = ({ reason, sourceLabel, refusedOn, deliveryTerm }) => {
	const named = sourceLabel.trim();
	const target = named ? `the ${named}` : "the source this follows";

	return (
		<Alert severity="info" sx={{ borderRadius: 0 }}>
			<AlertTitle>{PUBLISH_REFUSED_TITLE}</AlertTitle>
			{`This forecast could not be written to ${target}${sinceWhen(refusedOn)}. Everything else about this ${deliveryTerm.toLowerCase()} is up to date.`}
			{/* The remote's own sentence, quoted rather than paraphrased: it names what to fix in the
			    words the reader will search for, and it is not ours to rewrite. */}
			<Typography
				variant="body2"
				component="p"
				// The remote chose these words, so they can contain anything - a url, an id, a stack
				// fragment with no space in it. The box around this clips rather than scrolls, and the
				// part that gets cut off is exactly the part naming what to fix.
				sx={{ mt: 1, fontStyle: "italic", overflowWrap: "anywhere" }}
			>
				{reason}
			</Typography>
		</Alert>
	);
};

/**
 * Read in UTC, because the day stored is the instance's day rather than this browser's. Read as a
 * local day it would name the day before west of UTC.
 */
function sinceWhen(refusedOn: string | null): string {
	if (refusedOn === null) {
		return "";
	}

	const day = new Date(refusedOn).toLocaleDateString(undefined, {
		timeZone: "UTC",
	});

	return `, last tried on ${day}`;
}

export default DeliveryPublishRefusedNotice;
