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
	openDialog: () => void;
	closeDialog: () => void;
	decide: (decision: UsageDataDecisionValue) => Promise<void>;
}

export const useUsageDataConsent = (): UsageDataConsent => {
	const { usageDataService } = useContext(ApiServiceContext);

	const [indicatorState, setIndicatorState] =
		useState<UsageDataSendingState>("unknown");
	const [willAskAgain, setWillAskAgain] = useState(false);
	const [isDialogOpen, setIsDialogOpen] = useState(false);

	const refresh = useCallback(async () => {
		try {
			const state = await usageDataService.getState(readToken());
			setIndicatorState(state.sending ? "sending" : "not-sending");
			setWillAskAgain(state.willAskAgain);
		} catch {
			// The indicator fails closed. Guessing "sending" when we cannot tell would be alarming
			// and wrong; guessing "not sending" is only wrong.
			setIndicatorState("unknown");
		}
	}, [usageDataService]);

	useEffect(() => {
		void refresh();
	}, [refresh]);

	const decide = useCallback(
		async (decision: UsageDataDecisionValue) => {
			const token = await usageDataService.recordDecision(decision);
			writeToken(token);
			setIsDialogOpen(false);
			await refresh();
		},
		[usageDataService, refresh],
	);

	return {
		indicatorState,
		willAskAgain,
		isDialogOpen,
		openDialog: () => setIsDialogOpen(true),
		closeDialog: () => setIsDialogOpen(false),
		decide,
	};
};
