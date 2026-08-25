import { Alert, AlertTitle, Button } from "@mui/material";
import type React from "react";
import type { DeliverySourceUnavailableReason } from "../../../../../models/Delivery/DeliverySource";

interface DeliverySourceUnavailableNoticeProps {
	reason: DeliverySourceUnavailableReason;
	/** What the connection calls this kind of source, e.g. "Jira Release". */
	sourceLabel: string;
	lastSyncedOn: string | null;
	onUnbind?: () => void;
}

/**
 * Shown on a Delivery whose source has stopped answering for good. The values on the row beside it
 * are real values that were true when they were last read, and they are still worth reading - what
 * this says is that nothing is maintaining them any more, and since when.
 *
 * Each cause gets its own sentence. Reusing one wording would send somebody looking for a deleted
 * Release when the Release is sitting there and has simply lost its date, which is the likelier of
 * the two and the one a person can put right in a minute.
 */
export const NOTICE_TITLE = "Source unavailable";

function whatWentWrong(
	reason: DeliverySourceUnavailableReason,
	sourceLabel: string,
): string {
	switch (reason) {
		case "SourceNotFound":
			return `The ${sourceLabel} this follows no longer exists.`;
		case "SourceHasNoDate":
			return `The ${sourceLabel} this follows no longer has a date.`;
		default:
			return `This connection no longer offers the ${sourceLabel}.`;
	}
}

function sinceWhen(lastSyncedOn: string | null): string {
	if (lastSyncedOn === null) {
		return "It has not been read successfully since it was set up.";
	}

	const day = new Date(lastSyncedOn).toLocaleDateString(undefined, {
		timeZone: "UTC",
	});

	return `Showing the values it last gave, from ${day}.`;
}

const DeliverySourceUnavailableNotice: React.FC<
	DeliverySourceUnavailableNoticeProps
> = ({ reason, sourceLabel, lastSyncedOn, onUnbind }) => {
	return (
		<Alert
			severity="warning"
			action={
				onUnbind ? (
					<Button color="inherit" size="small" onClick={onUnbind}>
						Stop following
					</Button>
				) : undefined
			}
			sx={{ borderRadius: 0 }}
		>
			<AlertTitle>{NOTICE_TITLE}</AlertTitle>
			{whatWentWrong(reason, sourceLabel)} {sinceWhen(lastSyncedOn)}
		</Alert>
	);
};

export default DeliverySourceUnavailableNotice;
