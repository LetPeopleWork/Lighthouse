import {
	Box,
	Paper,
	type SxProps,
	TableContainer,
	type Theme,
	Typography,
} from "@mui/material";
import type React from "react";
import { useCallback, useContext, useEffect, useMemo, useState } from "react";
import DataGridBase from "../../../../components/Common/DataGrid/DataGridBase";
import { useErrorSnackbar } from "../../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import { useParentWorkItems } from "../../../../hooks/useParentWorkItems";
import { useRbac } from "../../../../hooks/useRbac";
import { useVoterIdentity } from "../../../../hooks/useVoterIdentity";
import type {
	IRefinementRow,
	IRefinementView,
} from "../../../../models/Refinement/Refinement";
import type { Team } from "../../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../../services/TerminologyContext";
import NextRefinement, { NEXT_REFINEMENT_SLOT } from "./NextRefinement";
import {
	createRefinementColumns,
	type RefinementGridRow,
} from "./refinementColumns";
import { describeStageBreakdown } from "./stageBreakdown";
import { useVoteCasting } from "./useVoteCasting";
import VoterNamePrompt from "./VoterNamePrompt";
import VotesAndCommentsDialog from "./VotesAndCommentsDialog";
import { describeVoteRefusal } from "./voteWording";
import { askYardstick } from "./YardstickQuestion";

const NO_ROWS: IRefinementRow[] = [];

// The heading and the next Refinement share the first row while every other part of the tab spans the
// full width beneath them; a grid does that without wrapping the two in a row of their own.
const TAB_LAYOUT: SxProps<Theme> = {
	display: "grid",
	gridTemplateColumns: "1fr auto",
	alignItems: "baseline",
	columnGap: 2,
	rowGap: 2,
	"& > *": { gridColumn: "1 / -1", minWidth: 0 },
	"& > h2": { gridColumn: "1", gridRow: "1" },
	[`& > [${NEXT_REFINEMENT_SLOT}]`]: {
		gridColumn: "2",
		gridRow: "1",
		justifySelf: "end",
	},
};

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

const describeReadyByVotes = (refinement: IRefinementView): string =>
	refinement.readyByVotesCount === undefined
		? ""
		: ` · ${refinement.readyByVotesCount} ready by votes`;

interface RefinementViewProps {
	team: Team;
}

const RefinementView: React.FC<Readonly<RefinementViewProps>> = ({ team }) => {
	const { refinementService } = useContext(ApiServiceContext);
	const { showError } = useErrorSnackbar();
	const { getTerm } = useTerminology();
	const { isTeamAdmin } = useRbac();
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
			current === null ? current : withAnsweredRow(current, answeredRow),
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
	const stagesConfigured = refinement?.stagesConfigured ?? false;
	const yardstick = refinement?.yardstick;
	const voteQuestion =
		yardstick === undefined ? undefined : askYardstick(yardstick, getTerm);
	const question = voteQuestion?.question;
	const questionTooltip = voteQuestion?.tooltip;
	const columns = useMemo(
		() =>
			createRefinementColumns(
				workItemTerm,
				parentMap,
				onVote,
				votesBeingSent,
				setVotesShownFor,
				stagesConfigured,
				question === undefined || questionTooltip === undefined
					? undefined
					: { question, tooltip: questionTooltip },
			),
		[
			workItemTerm,
			parentMap,
			onVote,
			votesBeingSent,
			stagesConfigured,
			question,
			questionTooltip,
		],
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
		<Box sx={TAB_LAYOUT}>
			<Typography variant="h6" component="h2">
				{refinement.stagesConfigured
					? describeStageBreakdown(workItems)
					: `${count} ${workItemsTerm} in ${refinementTerm}${describeReadyByVotes(refinement)}`}
			</Typography>
			<NextRefinement
				nextRefinementDate={refinement.nextRefinementDate}
				terms={{
					team: getTerm(TERMINOLOGY_KEYS.TEAM),
					refinement: refinementTerm,
					workItems: getTerm(TERMINOLOGY_KEYS.WORK_ITEMS),
				}}
				canChangeSettings={isTeamAdmin(team.id)}
			/>
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
		</Box>
	);
};

export default RefinementView;
