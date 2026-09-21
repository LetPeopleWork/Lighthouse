import {
	type PagePreference,
	pagePreference,
	usePagePreference,
} from "./pagePreference";

/**
 * What this reader has asked the Delivery timeline to show them.
 *
 * One store per question rather than one holding all of them, so that turning any of them on says
 * nothing about the others. Each is exposed on its own as well as through its hook, because what a
 * store does with a value it does not recognise, with storage that refuses it, and with a reader
 * who has stopped listening are questions about the store and not about any chart drawn from it.
 */

export const showTeamsStore: PagePreference = pagePreference(
	"lighthouse:deliveryTimeline:showTeams",
);

export function useShowTeams(): {
	showTeams: boolean;
	toggleShowTeams: () => void;
} {
	const { shown, toggle } = usePagePreference(showTeamsStore);

	return { showTeams: shown, toggleShowTeams: toggle };
}
