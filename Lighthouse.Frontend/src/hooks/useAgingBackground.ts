import { useCallback, useEffect, useState } from "react";

/**
 * The key is older than the choice it now stores and is kept as it is, because changing it would
 * silently reset every chart that has one stored.
 */
export const AGING_BACKGROUND_STORAGE_KEY = "workItemAgingPaceBandsEnabled";

/**
 * What the aging chart paints behind its dots. The modes are mutually exclusive by construction:
 * they share one background, and a chart carrying two of them at once would be asking a reader to
 * hold two meanings for the same colour.
 */
export type AgingBackground = "off" | "pace";

interface UseAgingBackgroundResult {
	background: AgingBackground;
	/** Applies the choice and remembers it, so it becomes the default on every chart. */
	chooseBackground: (next: AgingBackground) => void;
}

/**
 * Translates what earlier versions of this control wrote, so that nobody's chart changes
 * appearance across an upgrade.
 */
const storedBackground = (stored: string | null): AgingBackground | null => {
	// From when the control was a checkbox: "true" meant the pace bands were on. "false" needs no
	// branch of its own, because it meant nothing was painted and that is already what an
	// unrecognised value gets.
	if (stored === "true") return "pace";

	// "risk" selected a background that painted where an item's odds of missing its target crossed
	// 25, 50, 75 and 100 percent. It was withdrawn: below the first crossing it painted nothing,
	// and an unpainted region on this chart already means the history cannot say. The value is left
	// in storage rather than rewritten, so rolling back to a build that still has the mode returns
	// the reader to the chart they left. Do not reuse this word for a new mode without retiring the
	// stored value first — a browser holding it would silently opt in.
	if (stored === "risk") return "off";

	if (stored === "off" || stored === "pace") return stored;
	return null;
};

export const useAgingBackground = (): UseAgingBackgroundResult => {
	const [background, setBackground] = useState<AgingBackground>("off");

	useEffect(() => {
		let stored: string | null = null;
		try {
			stored = localStorage.getItem(AGING_BACKGROUND_STORAGE_KEY);
		} catch {
			// A browser with site data blocked still gets a chart, just not a remembered choice.
			return;
		}

		const resolved = storedBackground(stored);
		if (resolved !== null) {
			setBackground(resolved);
		}
	}, []);

	const chooseBackground = useCallback((next: AgingBackground): void => {
		setBackground(next);
		try {
			localStorage.setItem(AGING_BACKGROUND_STORAGE_KEY, next);
		} catch {
			// The choice still applies to this chart; it just will not outlive the tab.
		}
	}, []);

	return { background, chooseBackground };
};
