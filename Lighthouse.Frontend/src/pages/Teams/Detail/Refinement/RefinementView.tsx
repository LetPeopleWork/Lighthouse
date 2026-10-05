import { Box, type SxProps, type Theme, Typography } from "@mui/material";
import type React from "react";
import { useCallback, useMemo, useState } from "react";
import { useErrorSnackbar } from "../../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import { useRbac } from "../../../../hooks/useRbac";
import { useVoterIdentity } from "../../../../hooks/useVoterIdentity";
import type { IRefinementView } from "../../../../models/Refinement/Refinement";
import type { Team } from "../../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import { useTerminology } from "../../../../services/TerminologyContext";
import { markEnoughFor } from "./EnoughForLine";
import NeedVerdict, { shownVerdict, showsNeedMessage } from "./NeedVerdict";
import NextRefinement, { NEXT_REFINEMENT_SLOT } from "./NextRefinement";
import { describeNextRefinement } from "./nextRefinementWording";
import RefinementGrid from "./RefinementGrid";
import { describeLogFailure } from "./sizingLogWording";
import { describeStageBreakdown } from "./stageBreakdown";
import { useCommentAdding } from "./useCommentAdding";
import { useRefinement } from "./useRefinement";
import { useSizingLog } from "./useSizingLog";
import { useVerdictShownReporter } from "./useVerdictShownReporter";
import { useVoteCasting } from "./useVoteCasting";
import VoterNamePrompt from "./VoterNamePrompt";
import VotesAndCommentsDialog from "./VotesAndCommentsDialog";
import { describeVoteRefusal } from "./voteWording";

// The heading and the next Refinement (or the lack of a cadence) share the first row while every other
// part of the tab spans the full width beneath them; a grid does that without wrapping the two in a row of
// their own.
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

type GetTerm = (key: string) => string;

const describeReadyByVotes = (refinement: IRefinementView): string =>
	refinement.readyByVotesCount === undefined
		? ""
		: ` · ${refinement.readyByVotesCount} ready by votes`;

// A Team with stage rules reads its Work Items by stage; any other Team by how many there are.
const describeHeading = (
	refinement: IRefinementView,
	getTerm: GetTerm,
): string => {
	if (refinement.stagesConfigured) {
		return describeStageBreakdown(refinement.workItems);
	}
	const count = refinement.workItems.length;
	const workItemsTerm = getTerm(
		count === 1 ? TERMINOLOGY_KEYS.WORK_ITEM : TERMINOLOGY_KEYS.WORK_ITEMS,
	);
	return `${count} ${workItemsTerm} in ${getTerm(TERMINOLOGY_KEYS.REFINEMENT)}${describeReadyByVotes(refinement)}`;
};

interface RefinementViewProps {
	team: Team;
}

const RefinementView: React.FC<Readonly<RefinementViewProps>> = ({ team }) => {
	const { showError } = useErrorSnackbar();
	const { getTerm } = useTerminology();
	const { isTeamAdmin } = useRbac();
	const { refinement, shown, showAnsweredRow } = useRefinement(team.id);
	useVerdictShownReporter(team.id, shown);
	const [votesShownFor, setVotesShownFor] = useState<string | null>(null);
	const [isChangingName, setIsChangingName] = useState(false);
	const voterIdentity = useVoterIdentity(refinement?.voterIdentity);
	const { changeableName, declareName, readerKey } = voterIdentity;
	const { log, readAgain } = useSizingLog(team.id, votesShownFor, readerKey);

	const showVoteRefusal = useCallback(
		(error: unknown) => showError(describeVoteRefusal(error, getTerm)),
		[showError, getTerm],
	);

	const { onVote, votesBeingSent, isAskingForName, voteUnderName, cancelVote } =
		useVoteCasting(
			team.id,
			voterIdentity,
			showAnsweredRow,
			showVoteRefusal,
			refinement,
		);

	const {
		addComment,
		commentsBeingSent,
		isAskingForName: isAskingWhoComments,
		commentUnderName,
		cancelComment,
	} = useCommentAdding(
		team.id,
		voterIdentity,
		showAnsweredRow,
		showVoteRefusal,
	);

	const marking = useMemo(
		() =>
			refinement === null
				? null
				: markEnoughFor(
						shownVerdict(
							refinement.need,
							refinement.readyCount,
							refinement.nextRefinementDate,
						),
						{
							workItem: getTerm(TERMINOLOGY_KEYS.WORK_ITEM),
							workItems: getTerm(TERMINOLOGY_KEYS.WORK_ITEMS),
							refinement: getTerm(TERMINOLOGY_KEYS.REFINEMENT),
						},
					),
		[refinement, getTerm],
	);

	if (refinement === null) {
		return null;
	}

	const refinementTerm = getTerm(TERMINOLOGY_KEYS.REFINEMENT);
	const workItems = refinement.workItems;

	if (workItems.length === 0) {
		return (
			<Typography>
				{`No ${getTerm(TERMINOLOGY_KEYS.WORK_ITEMS)} in ${refinementTerm} states right now`}
			</Typography>
		);
	}

	const changeNameTo = (name: string) => {
		declareName(name);
		setIsChangingName(false);
	};

	const votesShownOn = workItems.find(
		(row) => row.referenceId === votesShownFor,
	);

	const nextRefinement = describeNextRefinement(
		refinement.nextRefinementDate,
		refinement.daysUntilNextRefinement,
		refinementTerm,
	);

	return (
		<Box sx={TAB_LAYOUT}>
			<Typography variant="h6" component="h2">
				{describeHeading(refinement, getTerm)}
			</Typography>
			<NextRefinement
				nextRefinement={nextRefinement}
				titlesTheNeedMessage={showsNeedMessage(
					refinement.need,
					refinement.readyCount,
					refinement.nextRefinementDate,
				)}
				terms={{
					team: getTerm(TERMINOLOGY_KEYS.TEAM),
					refinement: refinementTerm,
					workItems: getTerm(TERMINOLOGY_KEYS.WORK_ITEMS),
				}}
				canChangeSettings={isTeamAdmin(team.id)}
			/>
			<NeedVerdict
				need={refinement.need}
				readyCount={refinement.readyCount}
				nextRefinementDate={refinement.nextRefinementDate}
				title={nextRefinement}
				teamName={team.name}
				terms={{
					workItem: getTerm(TERMINOLOGY_KEYS.WORK_ITEM),
					workItems: getTerm(TERMINOLOGY_KEYS.WORK_ITEMS),
					team: getTerm(TERMINOLOGY_KEYS.TEAM),
					throughput: getTerm(TERMINOLOGY_KEYS.THROUGHPUT),
				}}
			/>
			<RefinementGrid
				teamId={team.id}
				workItems={workItems}
				yardstick={refinement.yardstick}
				stagesConfigured={refinement.stagesConfigured ?? false}
				marking={marking}
				votesBeingSent={votesBeingSent}
				onVote={onVote}
				onOpenVotes={setVotesShownFor}
			/>
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
					log={log}
					describeFailure={(error) => describeLogFailure(error, getTerm)}
					isSendingAComment={commentsBeingSent.has(votesShownOn.referenceId)}
					voterName={changeableName}
					onChangeName={() => setIsChangingName(true)}
					onAddComment={(comment, onSent) => {
						const { referenceId } = votesShownOn;
						addComment({
							referenceId,
							comment,
							onSent: () => {
								onSent();
								readAgain(referenceId);
							},
						});
					}}
					onClose={() => setVotesShownFor(null)}
				/>
			)}
			{isAskingWhoComments && (
				<VoterNamePrompt
					confirmLabel="Send"
					onCancel={cancelComment}
					onConfirm={commentUnderName}
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
