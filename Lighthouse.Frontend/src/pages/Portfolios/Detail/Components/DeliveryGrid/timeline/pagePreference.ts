import { useCallback, useSyncExternalStore } from "react";

/**
 * One thing this reader has asked to see, remembered in their browser.
 *
 * **One answer for the whole page, not one per component.** A Portfolio can have several Deliveries
 * open at once, each drawing the same switch, and a preference held in component state gives them
 * one truth each: flick one and the others sit there contradicting it while storage agrees with
 * none of them. The choice is a property of the person reading, so it is held once, here, and every
 * switch on the page is a view of the same value.
 *
 * `localStorage` is where it survives a reload. Telling the other switches is done by hand, because
 * the browser's own `storage` event deliberately does not fire in the document that did the
 * writing - it exists to tell *other* tabs - so a listener on it would leave this exactly as broken
 * as it was.
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
 *
 * Each call to this function builds its own remembered value and its own set of listeners. That is
 * the whole reason it is a factory rather than a module holding one of each: several preferences
 * sharing one variable would move together, and three switches that all flick at once look like a
 * decision rather than a fault.
 */
export interface PagePreference {
	subscribe: (listener: () => void) => () => void;
	read: () => boolean;
	set: (next: boolean) => void;
	/**
	 * Forgets what the page was showing, so the next reader starts where a first-time one does.
	 * This value outlives anything React owns, which is the point of it and also the reason a test
	 * clearing storage alone would inherit the previous one's choice.
	 */
	forget: () => void;
}

export function pagePreference(key: string): PagePreference {
	/** What the page is showing, once anything has asked or answered. */
	let shownNow: boolean | undefined;

	const listeners = new Set<() => void>();

	const readStored = (): boolean => {
		try {
			return localStorage.getItem(key) === "true";
		} catch {
			return false;
		}
	};

	const read = (): boolean => (shownNow ??= readStored());

	const set = (next: boolean): void => {
		shownNow = next;

		try {
			localStorage.setItem(key, String(next));
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

	return {
		subscribe,
		read,
		set,
		forget: () => {
			shownNow = undefined;
		},
	};
}

/** The preference as React reads it, and the one way to move it. */
export function usePagePreference(preference: PagePreference): {
	shown: boolean;
	toggle: () => void;
} {
	const shown = useSyncExternalStore(
		preference.subscribe,
		preference.read,
		preference.read,
	);

	const toggle = useCallback(() => {
		preference.set(!preference.read());
	}, [preference]);

	return { shown, toggle };
}
