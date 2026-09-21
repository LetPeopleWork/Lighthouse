import { useCallback, useState } from "react";

const SHOW_TEAMS_KEY = "lighthouse:deliveryTimeline:showTeams";

/**
 * Whether this reader has asked to see the Teams behind each bar, remembered for them across
 * visits and across Deliveries.
 *
 * One key for the reader rather than one per Delivery: "show me Teams" is a property of the person
 * reading, the same call column visibility already makes. Three things about how it is read are
 * each the difference between working and quietly wrong.
 *
 * The stored value is **compared as a string and never coerced**. `localStorage` hands back the
 * text "false", and that text is truthy — so a coerced read turns the preference on and then can
 * never turn it off again, for as long as the key exists.
 *
 * It is read while the state is first created rather than in an effect, because an effect applies
 * the stored value one render late. That is invisible when what changes is a colour and very
 * visible when it is the height of a chart that roughly doubles.
 *
 * And every access is wrapped. Private browsing and blocked site data make these calls throw, and
 * an unguarded read takes the whole Portfolio accordion down with it.
 */
export function useShowTeams(): {
	showTeams: boolean;
	toggleShowTeams: () => void;
} {
	const [showTeams, setShowTeams] = useState<boolean>(() => {
		try {
			return localStorage.getItem(SHOW_TEAMS_KEY) === "true";
		} catch {
			return false;
		}
	});

	const toggleShowTeams = useCallback(() => {
		setShowTeams((previous) => {
			const next = !previous;

			try {
				localStorage.setItem(SHOW_TEAMS_KEY, String(next));
			} catch {
				// Storage that will not take the choice costs this reader the memory of it, not the view.
			}

			return next;
		});
	}, []);

	return { showTeams, toggleShowTeams };
}
