import { useCallback, useContext, useState } from "react";
import type { useVoterIdentity } from "../../../../hooks/useVoterIdentity";
import type {
	IRefinementRow,
	SizingAnswer,
} from "../../../../models/Refinement/Refinement";
import { UsageDataSizingMoment } from "../../../../models/UsageData/UsageData";
import { ApiServiceContext } from "../../../../services/Api/ApiServiceContext";
import { UsageDataEventName } from "../../../../services/Api/UsageDataService";
import type { IStoredVoter } from "../../../../services/Refinement/voterStore";
import { useUsageDataReporter } from "../../../../services/UsageData/usageDataReporter";

export interface IPendingVote {
	referenceId: string;
	answer: SizingAnswer;
}

type VoterIdentity = Pick<
	ReturnType<typeof useVoterIdentity>,
	"voter" | "asksForName" | "declareName" | "ballotFor"
>;

/** A Team cannot have a refinement cadence yet, so every vote is cast without one. */
const sizingMomentOfAVote = (): UsageDataSizingMoment =>
	UsageDataSizingMoment.NoCadence;

/**
 * Casting a vote from the tab. A voter without sign-in who has not named themselves yet is asked first, and
 * the vote they chose waits until they have.
 */
export const useVoteCasting = (
	teamId: number,
	{ voter, asksForName, declareName, ballotFor }: VoterIdentity,
	onAnswered: (answeredRow: IRefinementRow) => void,
	onFailure: (error: unknown) => void,
) => {
	const { sizingLogService } = useContext(ApiServiceContext);
	const reportUsage = useUsageDataReporter();
	const [pendingVote, setPendingVote] = useState<IPendingVote | null>(null);

	const castVote = useCallback(
		({ referenceId, answer }: IPendingVote, declared: IStoredVoter | null) => {
			const { vote, voterKey } = ballotFor(answer, declared);
			sizingLogService
				.castVote(teamId, referenceId, vote, voterKey)
				.then((answeredRow) => {
					onAnswered(answeredRow);
					reportUsage({
						name: UsageDataEventName.TeamSizingVoteCast,
						sizingMoment: sizingMomentOfAVote(),
					});
				})
				.catch(onFailure);
		},
		[ballotFor, sizingLogService, teamId, onAnswered, onFailure, reportUsage],
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

	return {
		onVote,
		isAskingForName: pendingVote !== null,
		voteUnderName,
		cancelVote: () => setPendingVote(null),
	};
};
