import { useCallback, useSyncExternalStore } from "react";

/**
 * Whether this reader has asked to see the Teams behind each bar.
 *
 * **One answer for the whole page, not one per component.** A Portfolio can have several Deliveries
 * open at once, each with a switch of its own, and a preference held in component state gives them
 * one truth each: flick one and the others sit there contradicting it while storage agrees with
 * none of them. The choice is a property of the person reading, so it is held once, here, and every
 * switch on the page is a view of the same value.
 *
 * `localStorage` is where it survives a reload. Telling the other switches is done by hand, because
 * the browser's own `storage` event deliberately does not fire in the document that did the
 * writing - it exists to tell *other* tabs - so a listener on it would have left this exactly as
 * broken as it was.
 *
 * Three things about reading it are each the difference between working and quietly wrong.
 *
 * The stored value is **compared as a string and never coerced**. `localStorage` hands back the
 * text "false", and that text is truthy — so a coerced read turns the preference on and then can
 * never turn it off again, for as long as the key exists.
 *
 * It is read before the first paint rather than in an effect, because an effect applies the stored
 * value one render late. That is invisible when what changes is a colour and very visible when it
 * is the height of a chart that roughly doubles.
 *
 * And every access is wrapped. Private browsing and blocked site data make these calls throw, and
 * an unguarded read takes the whole Portfolio accordion down with it. A choice that cannot be
 * written is still honoured for this visit — the reader loses the memory of it, not the view.
 */

const SHOW_TEAMS_KEY = "lighthouse:deliveryTimeline:showTeams";

/** What the page is showing, once anything has asked or answered. */
let shownNow: boolean | undefined;

const listeners = new Set<() => void>();

const readStored = (): boolean => {
	try {
		return localStorage.getItem(SHOW_TEAMS_KEY) === "true";
	} catch {
		return false;
	}
};

const currentlyShown = (): boolean => (shownNow ??= readStored());

const show = (next: boolean): void => {
	shownNow = next;

	try {
		localStorage.setItem(SHOW_TEAMS_KEY, String(next));
	} catch {
		// Storage that will not take the choice costs this reader the memory of it, not the view.
	}

	for (const listener of listeners) {
		listener();
	}
};

const subscribe = (listener: () => void): (() => void) => {
	listeners.add(listener);

	return () => {
		listeners.delete(listener);
	};
};

/**
 * The choice itself, apart from React.
 *
 * It is a store rather than a hook's innards because that is what it is: one value for the whole
 * page, several readers, and a way to be told when it moves. Exposed so it can be exercised on its
 * own - what it does on blocked storage, on a value it does not recognise, and when a reader stops
 * listening are each a question about the store and not about any component that draws from it.
 */
export const showTeamsStore = {
	subscribe,
	read: currentlyShown,
	set: show,
	/**
	 * Forgets what the page was showing, so the next reader starts where a first-time one does.
	 * This value outlives anything React owns, which is the point of it and also the reason a test
	 * clearing storage alone would inherit the previous one's choice.
	 */
	forget: (): void => {
		shownNow = undefined;
	},
};

export function useShowTeams(): {
	showTeams: boolean;
	toggleShowTeams: () => void;
} {
	const showTeams = useSyncExternalStore(
		showTeamsStore.subscribe,
		showTeamsStore.read,
		showTeamsStore.read,
	);

	const toggleShowTeams = useCallback(() => {
		showTeamsStore.set(!showTeamsStore.read());
	}, []);

	return { showTeams, toggleShowTeams };
}
