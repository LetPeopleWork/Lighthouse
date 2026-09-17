import { useCallback, useEffect, useState } from "react";

/**
 * The key predates the third mode and is kept as it is, because changing it would silently reset
 * every chart that has one stored.
 */
export const AGING_BACKGROUND_STORAGE_KEY = "workItemAgingPaceBandsEnabled";

/**
 * What the aging chart paints behind its dots. The two modes are mutually exclusive by
 * construction: they share one background, and a chart carrying two of them at once would be
 * asking a reader to hold two meanings for the same colour.
 */
export type AgingBackground = "off" | "pace" | "risk";

interface UseAgingBackgroundResult {
	background: AgingBackground;
	/** Applies the choice and remembers it, so it becomes the default on every chart. */
	chooseBackground: (next: AgingBackground) => void;
}

/**
 * Reads what was stored before this was a choice of three. `"true"` meant the pace bands were on
 * and `"false"` meant nothing was, so both keep meaning that — nobody's chart changes appearance
 * because the control grew a third option.
 */
const storedBackground = (stored: string | null): AgingBackground | null => {
	// The one value that has to be translated. `"false"` needs no branch of its own: it meant
	// nothing was painted, and anything this does not recognise leaves the chart painting nothing.
	if (stored === "true") return "pace";
	if (stored === "off" || stored === "pace" || stored === "risk") return stored;
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
