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

/**
 * The Team's Refinement as the server last told it, null until it has. A row a vote answered replaces the
 * row shown, so the tab does not read everything again after each vote. A vote that takes a Work Item into
 * or out of Ready moves the ready count, and what that means against the need is the server's to say, so
 * that vote reads the Refinement again; the shown one stays until the answer arrives, so the grid does not
 * flash.
 */
export const useRefinement = (teamId: number) => {
	const { refinementService } = useContext(ApiServiceContext);
	const { showError } = useErrorSnackbar();
	const [refinement, setRefinement] = useState<IRefinementView | null>(null);
	const shownTeamId = useRef(teamId);

	const read = useCallback(
		(isCurrent: () => boolean) => {
			refinementService
				.getRefinement(teamId)
				.then((answer) => {
					if (isCurrent()) {
						setRefinement(answer);
					}
				})
				.catch((error: unknown) => {
					if (isCurrent()) {
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
			setRefinement((current) =>
				current === null ? current : withAnsweredRow(current, answeredRow),
			);
			if (movesTheVerdict(refinement, answeredRow)) {
				read(() => shownTeamId.current === teamId);
			}
		},
		[read, teamId, refinement],
	);

	return { refinement, showAnsweredRow };
};
