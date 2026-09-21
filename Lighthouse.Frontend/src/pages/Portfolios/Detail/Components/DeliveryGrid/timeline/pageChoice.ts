import { useCallback, useSyncExternalStore } from "react";

/**
 * One choice this reader has made, remembered in their browser.
 *
 * **One answer for the whole page, not one per component.** A Portfolio can have several Deliveries
 * open at once, each drawing the same control, and a choice held in component state gives them one
 * truth each: move one and the others sit there contradicting it while storage agrees with none of
 * them. The choice belongs to the person reading, so it is held once, here, and every control on
 * the page is a view of the same value.
 *
 * `localStorage` is where it survives a reload. Telling the other controls is done by hand, because
 * the browser's own `storage` event deliberately does not fire in the document that did the
 * writing - it exists to tell *other* tabs - so a listener on it would leave this exactly as broken
 * as it was.
 *
 * Three things about reading it are each the difference between working and quietly wrong.
 *
 * **A stored value is only honoured if it is one of the choices on offer.** Storage outlives the
 * code that wrote it: a choice that is renamed or withdrawn leaves readers holding a word this
 * version does not know, and handing that word back would select nothing at all.
 *
 * It is read before the first paint rather than in an effect, because an effect applies the stored
 * value one render late. That is invisible when what changes is a colour and very visible when it
 * is the height of a chart that roughly doubles.
 *
 * And every access is wrapped. Private browsing and blocked site data make these calls throw, and
 * an unguarded read takes the whole Portfolio accordion down with it. A choice that cannot be
 * written is still honoured for this visit - the reader loses the memory of it, not the view.
 */
export interface PageChoice<T extends string> {
	subscribe: (listener: () => void) => () => void;
	read: () => T;
	set: (next: T) => void;
	/**
	 * Forgets what the page was showing, so the next reader starts where a first-time one does.
	 * This value outlives anything React owns, which is the point of it and also the reason a test
	 * clearing storage alone would inherit the previous one's choice.
	 */
	forget: () => void;
}

export function pageChoice<T extends string>(
	key: string,
	offered: readonly T[],
	whenNeverAnswered: T,
): PageChoice<T> {
	let chosenNow: T | undefined;

	const listeners = new Set<() => void>();

	const readStored = (): T => {
		try {
			const stored = localStorage.getItem(key);

			return offered.find((choice) => choice === stored) ?? whenNeverAnswered;
		} catch {
			return whenNeverAnswered;
		}
	};

	const read = (): T => (chosenNow ??= readStored());

	const set = (next: T): void => {
		chosenNow = next;

		try {
			localStorage.setItem(key, next);
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
			chosenNow = undefined;
		},
	};
}

/** The choice as React reads it, and the one way to move it. */
export function usePageChoice<T extends string>(
	choice: PageChoice<T>,
): { chosen: T; choose: (next: T) => void } {
	const chosen = useSyncExternalStore(
		choice.subscribe,
		choice.read,
		choice.read,
	);

	const choose = useCallback((next: T) => choice.set(next), [choice]);

	return { chosen, choose };
}
