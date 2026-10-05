import { useCallback, useContext, useEffect, useRef, useState } from "react";
import type {
	ISizingLogEntry,
	ISizingVoters,
} from "../../../../models/Refinement/Refinement";
import { ApiServiceContext } from "../../../../services/Api/ApiServiceContext";

export type SizingLogState =
	| { status: "reading" }
	| { status: "read"; entries: ISizingLogEntry[]; voters: ISizingVoters }
	| { status: "failed"; error: unknown };

const READING: SizingLogState = { status: "reading" };

/**
 * The log of the Work Item whose votes and comments are open, read when it opens and again on request.
 * An answer that arrives after another Work Item was opened, after the log was closed, or after a newer
 * read, is dropped.
 */
export const useSizingLog = (
	teamId: number,
	referenceId: string | null,
	readerKey: string | null,
) => {
	const { sizingLogService } = useContext(ApiServiceContext);
	const [log, setLog] = useState<SizingLogState>(READING);
	const latestRead = useRef(0);
	const openReference = useRef(referenceId);
	// Naming yourself to comment gives this browser a key, but the log on screen is still the right one;
	// the read that follows the comment picks the key up, so a new key alone is no reason to read again.
	const currentReaderKey = useRef(readerKey);

	useEffect(() => {
		currentReaderKey.current = readerKey;
	}, [readerKey]);

	const read = useCallback(
		(workItemReference: string) => {
			latestRead.current += 1;
			const thisRead = latestRead.current;
			const isLatest = () => thisRead === latestRead.current;
			sizingLogService
				.getLog(teamId, workItemReference, currentReaderKey.current)
				.then(({ entries, voters }) => {
					if (isLatest()) {
						setLog({ status: "read", entries, voters });
					}
				})
				.catch((error: unknown) => {
					if (isLatest()) {
						setLog({ status: "failed", error });
					}
				});
		},
		[sizingLogService, teamId],
	);

	useEffect(() => {
		openReference.current = referenceId;
		latestRead.current += 1;
		setLog(READING);
		if (referenceId !== null) {
			read(referenceId);
		}
	}, [read, referenceId]);

	/** Reads the log again, unless the Work Item it was asked for is no longer the one open. */
	const readAgain = useCallback(
		(workItemReference: string) => {
			if (workItemReference === openReference.current) {
				read(workItemReference);
			}
		},
		[read],
	);

	return { log, readAgain };
};
