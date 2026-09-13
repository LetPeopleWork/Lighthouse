import {
	createContext,
	useCallback,
	useContext,
	useEffect,
	useMemo,
	useState,
} from "react";
import type { UsageDataSendingState } from "../components/UsageData/UsageDataIndicator";
import type { UsageDataDecisionValue } from "../models/UsageData/UsageData";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import { forgetWhatWasNoticed } from "../services/UsageData/usageDataBuffer";

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
export const readUsageDataConsentToken = (): string | null => {
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
 * application starts, would be the bug it looks like it is not: this is a single-page application,
 * so a tab left open for weeks never starts again, and consent would quietly age out from under
 * somebody who is using Lighthouse every day.
 */
const REFRESH_INTERVAL_MS = 60 * 60 * 1000;

const UsageDataConsentContext = createContext<UsageDataConsent | null>(null);

/**
 * Holds this browser's answer about usage data, once, for everything that needs it.
 *
 * Several parts of the application ask the same question at the same time - the footer draws the
 * indicator, and a detector elsewhere decides whether to collect anything at all - and they have to
 * agree from one instant to the next. Giving each of them its own copy is the defect this replaces:
 * agreeing in the footer left the detector reading the previous answer until its own hourly refresh
 * came round, so the indicator said data was being sent and, for up to an hour, none was.
 */
export function UsageDataConsentProvider({
	children,
}: {
	readonly children: React.ReactNode;
}) {
	const { usageDataService } = useContext(ApiServiceContext);

	const [indicatorState, setIndicatorState] =
		useState<UsageDataSendingState>("unknown");
	const [willAskAgain, setWillAskAgain] = useState(false);
	const [isDialogOpen, setIsDialogOpen] = useState(false);
	const [failedToRecord, setFailedToRecord] = useState(false);
	const [decision, setDecision] = useState<string | null>(null);

	// Stryker disable ArrayDeclaration: the two dependency lists below only show themselves when a dependency changes, and a test contrived to change one would be watching React re-run an effect rather than anything this feature promises.
	const refresh = useCallback(async () => {
		try {
			const state = await usageDataService.getState(
				readUsageDataConsentToken(),
			);
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
	// Stryker restore ArrayDeclaration

	const decide = useCallback(
		async (next: UsageDataDecisionValue) => {
			const token = readUsageDataConsentToken();

			try {
				// Saying no to something already agreed to is a withdrawal, not a fresh refusal.
				// Recording it as a new row would leave the original grant untouched and still live,
				// so the instance would keep sending for the rest of the liveness window while this
				// browser showed the opposite - the user having done exactly what they were told
				// would stop it.
				if (next === "declined" && decision === "Granted" && token) {
					// Anything noticed but not yet handed in goes before the request does, not after it
					// comes back. Waiting for the answer leaves a gap in which the clock that hands
					// pages in can fire against a consent already taken back - the server would turn
					// that batch away, so nothing would look wrong while this browser carried on doing
					// the one thing it was asked to stop. Doing it first is also what makes a
					// withdrawal that never arrives safe: the network failing is no reason to resume.
					forgetWhatWasNoticed();
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

	// Stryker disable next-line ArrayDeclaration: a dependency list only shows itself when a dependency changes, and a test contrived to change one would be watching React re-run an effect rather than anything this feature promises.
	const openDialog = useCallback(() => setIsDialogOpen(true), []);
	// Stryker disable next-line ArrayDeclaration: a dependency list only shows itself when a dependency changes, and a test contrived to change one would be watching React re-run an effect rather than anything this feature promises.
	const closeDialog = useCallback(() => setIsDialogOpen(false), []);

	const consent = useMemo(
		() => ({
			indicatorState,
			willAskAgain,
			isDialogOpen,
			failedToRecord,
			openDialog,
			closeDialog,
			decide,
		}),
		[
			indicatorState,
			willAskAgain,
			isDialogOpen,
			failedToRecord,
			openDialog,
			closeDialog,
			decide,
		],
	);

	return (
		<UsageDataConsentContext.Provider value={consent}>
			{children}
		</UsageDataConsentContext.Provider>
	);
}

export const useUsageDataConsent = (): UsageDataConsent => {
	const consent = useContext(UsageDataConsentContext);

	if (consent === null) {
		// Falling back to a private copy here is exactly what the provider exists to prevent: two
		// parts of the screen would hold two answers to the same question, and agreeing in one of
		// them would not reach the other for an hour. Refusing loudly on the first render is the
		// only version of this that anybody finds out about.
		throw new Error(
			"Usage data consent was asked for outside UsageDataConsentProvider. Mount the provider above anything that asks, so every part of the screen sees the same answer at the same moment.",
		);
	}

	return consent;
};
