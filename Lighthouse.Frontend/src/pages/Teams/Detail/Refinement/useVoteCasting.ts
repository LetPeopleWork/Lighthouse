import { useCallback, useContext, useRef, useState } from "react";
import type { useVoterIdentity } from "../../../../hooks/useVoterIdentity";
import type {
	IRefinementRow,
	RowReadiness,
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
 * Whether a vote moved a Work Item to Ready. "Before" is the row as this browser last showed it, so a
 * Work Item that was Ready already is never counted a second time.
 */
export const tipsToReady = (
	before: RowReadiness | undefined,
	after: RowReadiness | undefined,
): boolean => before !== "Ready" && after === "Ready";

/**
 * Casting a vote from the tab. A voter without sign-in who has not named themselves yet is asked first, and
 * the vote they chose waits until they have.
 */
export const useVoteCasting = (
	teamId: number,
	{ voter, asksForName, declareName, ballotFor }: VoterIdentity,
	readinessShownFor: (referenceId: string) => RowReadiness | undefined,
	onAnswered: (answeredRow: IRefinementRow) => void,
	onFailure: (error: unknown) => void,
) => {
	const { sizingLogService } = useContext(ApiServiceContext);
	const reportUsage = useUsageDataReporter();
	const [pendingVote, setPendingVote] = useState<IPendingVote | null>(null);
	const sending = useRef(new Set<string>());
	const [votesBeingSent, setVotesBeingSent] = useState<ReadonlySet<string>>(
		() => new Set(),
	);

	// Two votes on one row in flight could land in either order, leaving the grid showing an answer
	// the server does not hold; the row waits for its answer before it takes another.
	const showSending = useCallback(() => {
		setVotesBeingSent(new Set(sending.current));
	}, []);

	const castVote = useCallback(
		({ referenceId, answer }: IPendingVote, declared: IStoredVoter | null) => {
			if (sending.current.has(referenceId)) {
				return;
			}
			sending.current.add(referenceId);
			showSending();

			const readinessBefore = readinessShownFor(referenceId);
			const { vote, voterKey } = ballotFor(answer, declared);
			sizingLogService
				.castVote(teamId, referenceId, vote, voterKey)
				.then((answeredRow) => {
					onAnswered(answeredRow);
					reportUsage({
						name: UsageDataEventName.TeamSizingVoteCast,
						sizingMoment: sizingMomentOfAVote(),
					});
					if (tipsToReady(readinessBefore, answeredRow.readiness)) {
						reportUsage({
							name: UsageDataEventName.TeamSizingReadinessReached,
							sizingMoment: sizingMomentOfAVote(),
						});
					}
				})
				.catch(onFailure)
				.finally(() => {
					sending.current.delete(referenceId);
					showSending();
				});
		},
		[
			ballotFor,
			readinessShownFor,
			sizingLogService,
			teamId,
			onAnswered,
			onFailure,
			reportUsage,
			showSending,
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

	return {
		onVote,
		votesBeingSent,
		isAskingForName: pendingVote !== null,
		voteUnderName,
		cancelVote: () => setPendingVote(null),
	};
};
