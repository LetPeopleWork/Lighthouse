import { useCallback, useContext, useEffect, useState } from "react";
import type { UsageDataSendingState } from "../components/UsageData/UsageDataIndicator";
import type { UsageDataDecisionValue } from "../models/UsageData/UsageData";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";

/**
 * Where this browser keeps its consent token. The token is the only handle on the consent record -
 * the server holds a digest and cannot reproduce it - so losing this means losing the ability to
 * withdraw.
 */
const TOKEN_STORAGE_KEY = "lighthouse:usagedata:consent";

/**
 * Reading the token is not writing one. Nothing here touches storage until somebody has pressed a
 * button, which is what keeps the token inside the "strictly necessary" exemption: it exists only
 * to carry out the choice the person just made, and a browser that has only read the dialog leaves
 * no trace at all.
 */
const readToken = (): string | null => {
	try {
		return localStorage.getItem(TOKEN_STORAGE_KEY);
	} catch {
		// Private windows and locked-down browsers throw rather than return null. A browser that
		// cannot hold a token simply has not consented, which is the safe reading.
		return null;
	}
};

const writeToken = (token: string): void => {
	try {
		localStorage.setItem(TOKEN_STORAGE_KEY, token);
	} catch {
		// The decision is already recorded server-side; what is lost is this browser's ability to
		// revoke later. Failing the interaction over it would be worse than continuing.
	}
};

export interface UsageDataConsent {
	indicatorState: UsageDataSendingState;
	willAskAgain: boolean;
	isDialogOpen: boolean;
	failedToRecord: boolean;
	openDialog: () => void;
	closeDialog: () => void;
	decide: (decision: UsageDataDecisionValue) => Promise<void>;
}

/**
 * How often a tab that stays open re-asks for the state.
 *
 * This request is also what keeps this browser's consent alive, and the server only refreshes the
 * stamp a few times a window, so asking hourly costs almost no writes. Asking only once, when the
 * footer first mounts, would be the bug it looks like it is not: this is a single-page application,
 * so a tab left open for weeks never mounts the footer again, and consent would quietly age out
 * from under somebody who is using Lighthouse every day.
 */
const REFRESH_INTERVAL_MS = 60 * 60 * 1000;

export const useUsageDataConsent = (): UsageDataConsent => {
	const { usageDataService } = useContext(ApiServiceContext);

	const [indicatorState, setIndicatorState] =
		useState<UsageDataSendingState>("unknown");
	const [willAskAgain, setWillAskAgain] = useState(false);
	const [isDialogOpen, setIsDialogOpen] = useState(false);
	const [failedToRecord, setFailedToRecord] = useState(false);
	const [decision, setDecision] = useState<string | null>(null);

	const refresh = useCallback(async () => {
		try {
			const state = await usageDataService.getState(readToken());
			setIndicatorState(state.sending ? "sending" : "not-sending");
			setWillAskAgain(state.willAskAgain);
			setDecision(state.decision);
		} catch {
			// The indicator fails closed. Guessing "sending" when we cannot tell would be alarming
			// and wrong; guessing "not sending" is only wrong.
			setIndicatorState("unknown");
		}
	}, [usageDataService]);

	useEffect(() => {
		void refresh();

		const timer = setInterval(() => void refresh(), REFRESH_INTERVAL_MS);
		return () => clearInterval(timer);
	}, [refresh]);

	const decide = useCallback(
		async (next: UsageDataDecisionValue) => {
			const token = readToken();

			try {
				// Saying no to something already agreed to is a withdrawal, not a fresh refusal.
				// Recording it as a new row would leave the original grant untouched and still live,
				// so the instance would keep sending for the rest of the liveness window while this
				// browser showed the opposite - the user having done exactly what they were told
				// would stop it.
				if (next === "declined" && decision === "Granted" && token) {
					await usageDataService.revoke(token);
				} else {
					writeToken(await usageDataService.recordDecision(next));
				}

				setFailedToRecord(false);
				setIsDialogOpen(false);
				await refresh();
			} catch {
				// The dialog stays open and says so. Closing it would tell somebody their choice had
				// been taken when it had not.
				setFailedToRecord(true);
			}
		},
		[usageDataService, refresh, decision],
	);

	return {
		indicatorState,
		willAskAgain,
		isDialogOpen,
		failedToRecord,
		openDialog: () => setIsDialogOpen(true),
		closeDialog: () => setIsDialogOpen(false),
		decide,
	};
};
