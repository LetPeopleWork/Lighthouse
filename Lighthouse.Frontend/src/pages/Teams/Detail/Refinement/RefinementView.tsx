import { Paper, Stack, TableContainer, Typography } from "@mui/material";
import type React from "react";
import { useCallback, useContext, useEffect, useMemo, useState } from "react";
import DataGridBase from "../../../../components/Common/DataGrid/DataGridBase";
import { useErrorSnackbar } from "../../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import { useParentWorkItems } from "../../../../hooks/useParentWorkItems";
import { useVoterIdentity } from "../../../../hooks/useVoterIdentity";
import type {
	IRefinementRow,
	IRefinementView,
} from "../../../../models/Refinement/Refinement";
import type { Team } from "../../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../../services/TerminologyContext";
import {
	createRefinementColumns,
	type RefinementGridRow,
} from "./refinementColumns";
import { useVoteCasting } from "./useVoteCasting";
import VoterNamePrompt from "./VoterNamePrompt";
import VotesAndCommentsDialog from "./VotesAndCommentsDialog";
import { describeVoteRefusal } from "./voteWording";
import YardstickQuestion from "./YardstickQuestion";

const NO_ROWS: IRefinementRow[] = [];

const messageOf = (error: unknown): string =>
	error instanceof Error ? error.message : String(error);

interface RefinementViewProps {
	team: Team;
}

const RefinementView: React.FC<Readonly<RefinementViewProps>> = ({ team }) => {
	const { refinementService } = useContext(ApiServiceContext);
	const { showError } = useErrorSnackbar();
	const { getTerm } = useTerminology();
	const [refinement, setRefinement] = useState<IRefinementView | null>(null);
	const [votesShownFor, setVotesShownFor] = useState<string | null>(null);
	const [isChangingName, setIsChangingName] = useState(false);
	const voterIdentity = useVoterIdentity(refinement?.voterIdentity);
	const { changeableName, declareName } = voterIdentity;

	const showFailure = useCallback(
		(error: unknown) => showError(messageOf(error)),
		[showError],
	);

	useEffect(() => {
		let isCurrent = true;

		refinementService
			.getRefinement(team.id)
			.then((answer) => {
				if (isCurrent) {
					setRefinement(answer);
				}
			})
			.catch((error: unknown) => {
				if (isCurrent) {
					showFailure(error);
				}
			});

		return () => {
			isCurrent = false;
		};
	}, [team.id, refinementService, showFailure]);

	const showAnsweredRow = useCallback((answeredRow: IRefinementRow) => {
		setRefinement((current) =>
			current === null
				? current
				: {
						...current,
						workItems: current.workItems.map((row) =>
							row.referenceId === answeredRow.referenceId ? answeredRow : row,
						),
					},
		);
	}, []);

	const showVoteRefusal = useCallback(
		(error: unknown) => showError(describeVoteRefusal(error, getTerm)),
		[showError, getTerm],
	);

	const { onVote, votesBeingSent, isAskingForName, voteUnderName, cancelVote } =
		useVoteCasting(team.id, voterIdentity, showAnsweredRow, showVoteRefusal);

	const changeNameTo = (name: string) => {
		declareName(name);
		setIsChangingName(false);
	};

	const workItems = refinement?.workItems ?? NO_ROWS;
	const parentReferences = useMemo(
		() =>
			workItems.map((workItem) => ({
				parentWorkItemReference: workItem.parentReferenceId,
			})),
		[workItems],
	);
	const parentMap = useParentWorkItems(parentReferences);
	const workItemTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEM);
	const columns = useMemo(
		() =>
			createRefinementColumns(
				workItemTerm,
				parentMap,
				onVote,
				votesBeingSent,
				setVotesShownFor,
			),
		[workItemTerm, parentMap, onVote, votesBeingSent],
	);

	if (refinement === null) {
		return null;
	}

	const refinementTerm = getTerm(TERMINOLOGY_KEYS.REFINEMENT);
	const count = workItems.length;

	if (count === 0) {
		return (
			<Typography>
				{`No ${getTerm(TERMINOLOGY_KEYS.WORK_ITEMS)} in ${refinementTerm} states right now`}
			</Typography>
		);
	}

	const votesShownOn = workItems.find(
		(row) => row.referenceId === votesShownFor,
	);
	const workItemsTerm =
		count === 1 ? workItemTerm : getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const readyByVotes =
		refinement.readyByVotesCount === undefined
			? ""
			: ` · ${refinement.readyByVotesCount} ready by votes`;

	return (
		<Stack spacing={2}>
			<Typography variant="h6" component="h2">
				{`${count} ${workItemsTerm} in ${refinementTerm}${readyByVotes}`}
			</Typography>
			{refinement.yardstick && (
				<YardstickQuestion yardstick={refinement.yardstick} getTerm={getTerm} />
			)}
			<TableContainer component={Paper}>
				<DataGridBase<RefinementGridRow>
					rows={workItems as RefinementGridRow[]}
					columns={columns}
					idField="referenceId"
					storageKey={`team-refinement-${team.id}`}
				/>
			</TableContainer>
			{isAskingForName && (
				<VoterNamePrompt
					confirmLabel="Vote"
					onCancel={cancelVote}
					onConfirm={voteUnderName}
				/>
			)}
			{votesShownOn !== undefined && (
				<VotesAndCommentsDialog
					workItem={votesShownOn}
					voterName={changeableName}
					onChangeName={() => setIsChangingName(true)}
					onClose={() => setVotesShownFor(null)}
				/>
			)}
			{isChangingName && changeableName !== null && (
				<VoterNamePrompt
					initialName={changeableName}
					confirmLabel="Save"
					onCancel={() => setIsChangingName(false)}
					onConfirm={changeNameTo}
				/>
			)}
		</Stack>
	);
};

export default RefinementView;
