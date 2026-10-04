import { useCallback, useContext, useEffect, useState } from "react";
import { useErrorSnackbar } from "../../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import type {
	IRefinementRow,
	IRefinementView,
} from "../../../../models/Refinement/Refinement";
import { ApiServiceContext } from "../../../../services/Api/ApiServiceContext";

const messageOf = (error: unknown): string =>
	error instanceof Error ? error.message : String(error);

const readyCount = (row: IRefinementRow | undefined): number =>
	row?.readiness === "Ready" ? 1 : 0;

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
				readyCount(shownRow) +
				readyCount(answeredRow);

	return {
		...current,
		readyByVotesCount,
		workItems: current.workItems.map((row) =>
			row === shownRow ? answeredRow : row,
		),
	};
};

/**
 * The Team's Refinement as the server last told it, null until it has. A row a vote answered replaces the
 * row shown, so the tab does not read everything again after each vote.
 */
export const useRefinement = (teamId: number) => {
	const { refinementService } = useContext(ApiServiceContext);
	const { showError } = useErrorSnackbar();
	const [refinement, setRefinement] = useState<IRefinementView | null>(null);

	useEffect(() => {
		let isCurrent = true;

		refinementService
			.getRefinement(teamId)
			.then((answer) => {
				if (isCurrent) {
					setRefinement(answer);
				}
			})
			.catch((error: unknown) => {
				if (isCurrent) {
					showError(messageOf(error));
				}
			});

		return () => {
			isCurrent = false;
		};
	}, [teamId, refinementService, showError]);

	const showAnsweredRow = useCallback((answeredRow: IRefinementRow) => {
		setRefinement((current) =>
			current === null ? current : withAnsweredRow(current, answeredRow),
		);
	}, []);

	return { refinement, showAnsweredRow };
};
