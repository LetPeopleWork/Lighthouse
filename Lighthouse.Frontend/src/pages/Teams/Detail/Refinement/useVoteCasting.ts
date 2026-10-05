import { useCallback, useContext, useRef, useState } from "react";
import type { useVoterIdentity } from "../../../../hooks/useVoterIdentity";
import type {
	IRefinementView,
	IVotedRow,
	SizingAnswer,
} from "../../../../models/Refinement/Refinement";
import { UsageDataSizingMoment } from "../../../../models/UsageData/UsageData";
import { ApiServiceContext } from "../../../../services/Api/ApiServiceContext";
import { UsageDataEventName } from "../../../../services/Api/UsageDataService";
import type { IStoredVoter } from "../../../../services/Refinement/voterStore";
import { useUsageDataReporter } from "../../../../services/UsageData/usageDataReporter";
import { useNameFirst } from "./useNameFirst";

export interface IPendingVote {
	referenceId: string;
	answer: SizingAnswer;
}

type VoterIdentity = Pick<
	ReturnType<typeof useVoterIdentity>,
	"voter" | "asksForName" | "declareName" | "ballotFor"
>;

export type RefinementFacts = Pick<
	IRefinementView,
	"nextRefinementDate" | "isRefinementDay"
> | null;

/**
 * Whether a vote was cast on a Refinement day is the server's answer, not this browser's clock: the
 * server counts days in the instance's time zone and knows which cadence days are blacked out.
 */
export const sizingMomentOf = (
	facts: RefinementFacts,
): UsageDataSizingMoment => {
	if ((facts?.nextRefinementDate ?? null) === null) {
		return UsageDataSizingMoment.NoCadence;
	}
	return facts?.isRefinementDay
		? UsageDataSizingMoment.OnRefinementDay
		: UsageDataSizingMoment.OnOtherDay;
};

/**
 * Casting a vote from the tab.
 */
export const useVoteCasting = (
	teamId: number,
	{ ballotFor, ...naming }: VoterIdentity,
	onAnswered: (answeredRow: IVotedRow) => void,
	onFailure: (error: unknown) => void,
	refinementFacts: RefinementFacts,
) => {
	const { sizingLogService } = useContext(ApiServiceContext);
	const reportUsage = useUsageDataReporter();
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

			const { vote, voterKey } = ballotFor(answer, declared);
			sizingLogService
				.castVote(teamId, referenceId, vote, voterKey)
				.then((answeredRow) => {
					onAnswered(answeredRow);
					const sizingMoment = sizingMomentOf(refinementFacts);
					reportUsage({
						name: UsageDataEventName.TeamSizingVoteCast,
						sizingMoment,
					});
					// Only the server knows which vote moved the row to Ready; this browser's copy of the
					// row may be older than other people's votes, and the event must be counted once.
					if (answeredRow.madeReady) {
						reportUsage({
							name: UsageDataEventName.TeamSizingReadinessReached,
							sizingMoment,
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
			sizingLogService,
			teamId,
			onAnswered,
			onFailure,
			reportUsage,
			showSending,
			refinementFacts,
		],
	);

	const { submit, isAskingForName, submitUnderName, cancel } = useNameFirst(
		naming,
		castVote,
	);

	return {
		onVote: submit,
		votesBeingSent,
		isAskingForName,
		voteUnderName: submitUnderName,
		cancelVote: cancel,
	};
};
