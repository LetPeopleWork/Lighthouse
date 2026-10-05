import { Box, type SxProps, type Theme, Typography } from "@mui/material";
import type React from "react";
import { useCallback, useState } from "react";
import { useErrorSnackbar } from "../../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import { useRbac } from "../../../../hooks/useRbac";
import { useVoterIdentity } from "../../../../hooks/useVoterIdentity";
import type { IRefinementView } from "../../../../models/Refinement/Refinement";
import type { Team } from "../../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import { useTerminology } from "../../../../services/TerminologyContext";
import NeedVerdict from "./NeedVerdict";
import NextRefinement, { NEXT_REFINEMENT_SLOT } from "./NextRefinement";
import RefinementGrid from "./RefinementGrid";
import { describeStageBreakdown } from "./stageBreakdown";
import { useRefinement } from "./useRefinement";
import { useVoteCasting } from "./useVoteCasting";
import VoterNamePrompt from "./VoterNamePrompt";
import VotesAndCommentsDialog from "./VotesAndCommentsDialog";
import { describeVoteRefusal } from "./voteWording";

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
	const { refinement, showAnsweredRow } = useRefinement(team.id);
	const [votesShownFor, setVotesShownFor] = useState<string | null>(null);
	const [isChangingName, setIsChangingName] = useState(false);
	const voterIdentity = useVoterIdentity(refinement?.voterIdentity);
	const { changeableName, declareName } = voterIdentity;

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

	return (
		<Box sx={TAB_LAYOUT}>
			<Typography variant="h6" component="h2">
				{describeHeading(refinement, getTerm)}
			</Typography>
			<NextRefinement
				nextRefinementDate={refinement.nextRefinementDate}
				daysUntilNextRefinement={refinement.daysUntilNextRefinement}
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
				teamName={team.name}
				terms={{
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
