import { useCallback, useContext, useEffect, useRef, useState } from "react";
import { useErrorSnackbar } from "../../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import type {
	IRefinementRow,
	IRefinementView,
	IVotedRow,
} from "../../../../models/Refinement/Refinement";
import { ApiServiceContext } from "../../../../services/Api/ApiServiceContext";

const messageOf = (error: unknown): string =>
	error instanceof Error ? error.message : String(error);

const isReady = (row: IRefinementRow): boolean => row.readiness === "Ready";

const withAnsweredRow = (
	current: IRefinementView,
	answeredRow: IRefinementRow,
): IRefinementView => {
	const shownRow = current.workItems.find(
		(row) => row.referenceId === answeredRow.referenceId,
	);
	if (shownRow === undefined) {
		return current;
	}
	const readyByVotesCount =
		current.readyByVotesCount === undefined
			? undefined
			: current.readyByVotesCount -
				Number(isReady(shownRow)) +
				Number(isReady(answeredRow));

	return {
		...current,
		readyByVotesCount,
		workItems: current.workItems.map((row) =>
			row === shownRow ? answeredRow : row,
		),
	};
};

/**
 * Whether the vote moved the ready count the shown verdict is judged on: the Work Item went into or out of
 * Ready. Without a verdict on screen the count decides nothing, so there is nothing to read again for.
 */
const movesTheVerdict = (
	shown: IRefinementView | null,
	answeredRow: IVotedRow,
): boolean => {
	if (shown?.need?.verdict == null) {
		return false;
	}
	const shownRow = shown.workItems.find(
		(row) => row.referenceId === answeredRow.referenceId,
	);
	return (
		answeredRow.madeReady ||
		(shownRow !== undefined && isReady(shownRow) !== isReady(answeredRow))
	);
};

/** A Refinement together with the Team it was read for. */
export interface ShownRefinement {
	teamId: number;
	view: IRefinementView;
}

/**
 * The Team's Refinement as the server last told it, null until it has. A row a vote answered replaces the
 * row shown, so the tab does not read everything again after each vote. A vote that takes a Work Item into
 * or out of Ready moves the ready count, and what that means against the need is the server's to say, so
 * that vote reads the Refinement again; the shown one stays until the answer arrives, so the grid does not
 * flash.
 *
 * Reads can answer out of order, so only the latest one is shown. A vote answered after that read set off
 * may be missing from its answer, so the row that vote answered is laid over the answer again.
 *
 * Moving to another Team keeps the last Team's Refinement on screen until the next one is read, so what is
 * shown carries the Team it belongs to.
 */
export const useRefinement = (teamId: number) => {
	const { refinementService } = useContext(ApiServiceContext);
	const { showError } = useErrorSnackbar();
	const [shown, setShown] = useState<ShownRefinement | null>(null);
	const refinement = shown?.view ?? null;
	const shownTeamId = useRef(teamId);
	const latestRead = useRef(0);
	const answeredSinceRead = useRef(new Map<string, IRefinementRow>());

	const read = useCallback(
		(isCurrent: () => boolean) => {
			latestRead.current += 1;
			const thisRead = latestRead.current;
			answeredSinceRead.current = new Map();
			const isLatest = () => isCurrent() && thisRead === latestRead.current;

			refinementService
				.getRefinement(teamId)
				.then((answer) => {
					if (isLatest()) {
						setShown({
							teamId,
							view: [...answeredSinceRead.current.values()].reduce(
								withAnsweredRow,
								answer,
							),
						});
					}
				})
				.catch((error: unknown) => {
					if (isLatest()) {
						showError(messageOf(error));
					}
				});
		},
		[teamId, refinementService, showError],
	);

	useEffect(() => {
		let isCurrent = true;
		shownTeamId.current = teamId;
		read(() => isCurrent);

		return () => {
			isCurrent = false;
		};
	}, [teamId, read]);

	// A vote answered after the tab moved on to another Team must not bring the old Team's Refinement back.
	const showAnsweredRow = useCallback(
		(answeredRow: IVotedRow) => {
			if (shownTeamId.current !== teamId) {
				return;
			}
			answeredSinceRead.current.set(answeredRow.referenceId, answeredRow);
			setShown((current) =>
				current?.teamId === teamId
					? { teamId, view: withAnsweredRow(current.view, answeredRow) }
					: current,
			);
			if (movesTheVerdict(refinement, answeredRow)) {
				read(() => shownTeamId.current === teamId);
			}
		},
		[read, teamId, refinement],
	);

	return { refinement, shown, showAnsweredRow };
};
