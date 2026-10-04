import {
	Button,
	Paper,
	Stack,
	TableContainer,
	Typography,
} from "@mui/material";
import type { GridValidRowModel } from "@mui/x-data-grid";
import type React from "react";
import { useCallback, useContext, useEffect, useMemo, useState } from "react";
import DataGridBase from "../../../../components/Common/DataGrid/DataGridBase";
import type { DataGridColumn } from "../../../../components/Common/DataGrid/types";
import { createNameColumn } from "../../../../components/Common/FeatureListDataGrid/columns";
import ParentWorkItemCell from "../../../../components/Common/ParentWorkItemCell/ParentWorkItemCell";
import { useErrorSnackbar } from "../../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import {
	type ParentWorkItem,
	useParentWorkItems,
} from "../../../../hooks/useParentWorkItems";
import { useVoterIdentity } from "../../../../hooks/useVoterIdentity";
import type {
	IRefinementRow,
	IRefinementView,
	SizingAnswer,
} from "../../../../models/Refinement/Refinement";
import type { Team } from "../../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import { UsageDataSizingMoment } from "../../../../models/UsageData/UsageData";
import { ApiServiceContext } from "../../../../services/Api/ApiServiceContext";
import { UsageDataEventName } from "../../../../services/Api/UsageDataService";
import type { IStoredVoter } from "../../../../services/Refinement/voterStore";
import { useTerminology } from "../../../../services/TerminologyContext";
import { useUsageDataReporter } from "../../../../services/UsageData/usageDataReporter";
import VoteControl from "./VoteControl";
import VoterNamePrompt from "./VoterNamePrompt";
import VotesAndCommentsDialog from "./VotesAndCommentsDialog";
import { describeVoteCount } from "./voteWording";
import YardstickQuestion from "./YardstickQuestion";

type RefinementGridRow = IRefinementRow & GridValidRowModel;

const NO_ROWS: IRefinementRow[] = [];

/** A Team cannot have a refinement cadence yet, so every vote is cast without one. */
const sizingMomentOfAVote = (): UsageDataSizingMoment =>
	UsageDataSizingMoment.NoCadence;

interface IPendingVote {
	referenceId: string;
	answer: SizingAnswer;
}

const messageOf = (error: unknown): string =>
	error instanceof Error ? error.message : String(error);

const createRefinementColumns = (
	workItemTerm: string,
	parentMap: Map<string, ParentWorkItem>,
	onVote: (vote: IPendingVote) => void,
	onOpenVotes: (referenceId: string) => void,
): DataGridColumn<RefinementGridRow>[] => [
	createNameColumn<RefinementGridRow>(workItemTerm),
	{
		field: "parentReferenceId",
		headerName: "Parent",
		width: 300,
		sortable: false,
		renderCell: ({ row }) => (
			<ParentWorkItemCell
				parentReference={row.parentReferenceId}
				parentMap={parentMap}
			/>
		),
	},
	{
		field: "state",
		headerName: "State",
		width: 160,
	},
	{
		field: "myVote",
		headerName: "Your vote",
		width: 240,
		sortable: false,
		renderCell: ({ row }) => (
			<VoteControl
				myVote={row.myVote ?? null}
				onVote={(answer) => onVote({ referenceId: row.referenceId, answer })}
			/>
		),
	},
	{
		field: "voteCount",
		headerName: "Votes",
		width: 120,
		renderCell: ({ row }) => {
			const voteCount = describeVoteCount(row.voteCount ?? 0);
			return (
				<Button
					size="small"
					aria-label={`${voteCount} - Votes and comments`}
					onClick={() => onOpenVotes(row.referenceId)}
				>
					{voteCount}
				</Button>
			);
		},
	},
];

interface RefinementViewProps {
	team: Team;
}

const RefinementView: React.FC<Readonly<RefinementViewProps>> = ({ team }) => {
	const { refinementService, sizingLogService } = useContext(ApiServiceContext);
	const { showError } = useErrorSnackbar();
	const reportUsage = useUsageDataReporter();
	const { getTerm } = useTerminology();
	const [refinement, setRefinement] = useState<IRefinementView | null>(null);
	const [pendingVote, setPendingVote] = useState<IPendingVote | null>(null);
	const [votesShownFor, setVotesShownFor] = useState<string | null>(null);
	const [isChangingName, setIsChangingName] = useState(false);
	const { voter, asksForName, changeableName, declareName, ballotFor } =
		useVoterIdentity(refinement?.voterIdentity);

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
					showError(messageOf(error));
				}
			});

		return () => {
			isCurrent = false;
		};
	}, [team.id, refinementService, showError]);

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

	const castVote = useCallback(
		({ referenceId, answer }: IPendingVote, declared: IStoredVoter | null) => {
			const { vote, voterKey } = ballotFor(answer, declared);
			sizingLogService
				.castVote(team.id, referenceId, vote, voterKey)
				.then((answeredRow) => {
					showAnsweredRow(answeredRow);
					reportUsage({
						name: UsageDataEventName.TeamSizingVoteCast,
						sizingMoment: sizingMomentOfAVote(),
					});
				})
				.catch((error: unknown) => showError(messageOf(error)));
		},
		[
			ballotFor,
			sizingLogService,
			team.id,
			showAnsweredRow,
			showError,
			reportUsage,
		],
	);

	const onVote = useCallback(
		(chosen: IPendingVote) => {
			if (asksForName) {
				setPendingVote(chosen);
				return;
			}

			castVote(chosen, voter);
		},
		[asksForName, castVote, voter],
	);

	const voteUnderName = (name: string) => {
		if (pendingVote !== null) {
			castVote(pendingVote, declareName(name));
		}
		setPendingVote(null);
	};

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
				setVotesShownFor,
			),
		[workItemTerm, parentMap, onVote],
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

	return (
		<Stack spacing={2}>
			<Typography variant="h6" component="h2">
				{`${count} ${workItemsTerm} in ${refinementTerm}`}
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
			{pendingVote !== null && (
				<VoterNamePrompt
					confirmLabel="Vote"
					onCancel={() => setPendingVote(null)}
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
