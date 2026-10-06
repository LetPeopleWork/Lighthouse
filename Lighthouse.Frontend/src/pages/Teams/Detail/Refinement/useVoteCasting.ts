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
	/** What has to be true for a "Yes, if…" to become a Yes; asked for before such a vote is cast. */
	condition?: string;
}

interface IVoteAwaitingCondition {
	pending: IPendingVote;
	declared: IStoredVoter | null;
	teamId: number;
}

const awaitsCondition = ({ answer, condition }: IPendingVote): boolean =>
	answer === "YesBut" && condition === undefined;

type VoterIdentity = Pick<
	ReturnType<typeof useVoterIdentity>,
	"voter" | "asksForName" | "declareName" | "ballotFor" | "readerKey"
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
 * Casting a vote from the tab, and taking one's own vote back.
 */
export const useVoteCasting = (
	teamId: number,
	{ ballotFor, readerKey, ...naming }: VoterIdentity,
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

	const sendOnce = useCallback(
		(referenceId: string, send: () => Promise<void>) => {
			if (sending.current.has(referenceId)) {
				return;
			}
			sending.current.add(referenceId);
			showSending();

			send()
				.catch(onFailure)
				.finally(() => {
					sending.current.delete(referenceId);
					showSending();
				});
		},
		[onFailure, showSending],
	);

	const castVote = useCallback(
		(
			{ referenceId, answer, condition }: IPendingVote,
			declared: IStoredVoter | null,
		) => {
			const { vote, voterKey } = ballotFor(answer, declared);
			const conditioned =
				condition === undefined ? vote : { ...vote, comment: condition };
			sendOnce(referenceId, () =>
				sizingLogService
					.castVote(teamId, referenceId, conditioned, voterKey)
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
					}),
			);
		},
		[
			ballotFor,
			sizingLogService,
			teamId,
			onAnswered,
			reportUsage,
			sendOnce,
			refinementFacts,
		],
	);

	// Only a browser that holds a vote is offered this, so nobody is asked for a name to take one back.
	// Taking a vote back is not a vote: it reports nothing and never counts as reaching Ready.
	const takeBack = useCallback(
		(referenceId: string) => {
			sendOnce(referenceId, () =>
				sizingLogService
					.takeBackMyVote(teamId, referenceId, readerKey)
					.then((row) => onAnswered({ ...row, madeReady: false })),
			);
		},
		[sendOnce, sizingLogService, teamId, readerKey, onAnswered],
	);

	// The name comes first and the condition second, so a voter meets one dialog at a time. A vote still
	// waiting for its condition was meant for the Team it was asked on, so another Team drops it.
	const [heldForCondition, setHeldForCondition] =
		useState<IVoteAwaitingCondition | null>(null);
	const awaitingCondition =
		heldForCondition?.teamId === teamId ? heldForCondition : null;

	const castOnceConditioned = useCallback(
		(pending: IPendingVote, declared: IStoredVoter | null) => {
			if (awaitsCondition(pending)) {
				setHeldForCondition({ pending, declared, teamId });
				return;
			}
			castVote(pending, declared);
		},
		[castVote, teamId],
	);

	const { submit, isAskingForName, submitUnderName, cancel } = useNameFirst(
		naming,
		castOnceConditioned,
		teamId,
	);

	const voteWithCondition = (condition: string) => {
		if (awaitingCondition !== null) {
			const { pending, declared } = awaitingCondition;
			castVote({ ...pending, condition }, declared);
		}
		setHeldForCondition(null);
	};

	return {
		onVote: submit,
		takeBack,
		votesBeingSent,
		isAskingForName,
		voteUnderName: submitUnderName,
		cancelVote: cancel,
		conditionAskedOn: awaitingCondition?.pending.referenceId ?? null,
		voteWithCondition,
		cancelCondition: () => setHeldForCondition(null),
	};
};
