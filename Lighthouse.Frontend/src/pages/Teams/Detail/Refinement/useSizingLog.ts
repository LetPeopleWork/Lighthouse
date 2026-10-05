import { useCallback, useContext, useEffect, useRef, useState } from "react";
import type { ISizingLogEntry } from "../../../../models/Refinement/Refinement";
import { ApiServiceContext } from "../../../../services/Api/ApiServiceContext";

export type SizingLogState =
	| { status: "reading" }
	| { status: "read"; entries: ISizingLogEntry[] }
	| { status: "failed"; message: string };

const READING: SizingLogState = { status: "reading" };

const messageOf = (error: unknown): string =>
	error instanceof Error ? error.message : String(error);

/**
 * The log of the Work Item whose votes and comments are open, read when it opens and again on request.
 * An answer that arrives after another Work Item was opened, or after a newer read, is dropped.
 */
export const useSizingLog = (
	teamId: number,
	referenceId: string | null,
	voterKey: string | null,
) => {
	const { sizingLogService } = useContext(ApiServiceContext);
	const [log, setLog] = useState<SizingLogState>(READING);
	const latestRead = useRef(0);

	const read = useCallback(
		(workItemReference: string) => {
			latestRead.current += 1;
			const thisRead = latestRead.current;
			const isLatest = () => thisRead === latestRead.current;
			sizingLogService
				.getLog(teamId, workItemReference, voterKey)
				.then(({ entries }) => {
					if (isLatest()) {
						setLog({ status: "read", entries });
					}
				})
				.catch((error: unknown) => {
					if (isLatest()) {
						setLog({ status: "failed", message: messageOf(error) });
					}
				});
		},
		[sizingLogService, teamId, voterKey],
	);

	useEffect(() => {
		setLog(READING);
		if (referenceId !== null) {
			read(referenceId);
		}
	}, [read, referenceId]);

	const readAgain = useCallback(() => {
		if (referenceId !== null) {
			read(referenceId);
		}
	}, [read, referenceId]);

	return { log, readAgain };
};
