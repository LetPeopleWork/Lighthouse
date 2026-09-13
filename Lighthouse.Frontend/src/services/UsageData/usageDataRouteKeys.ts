import { matchPath } from "react-router";
import { UsageDataRouteKey } from "../../models/UsageData/UsageData";

/**
 * The one place a page address is turned into something that may be sent, and the address is only
 * ever read to make that choice - nothing derived from it is kept or passed on.
 *
 * The caller hands in the path the router is currently showing. Asking the browser for it here
 * instead would be the same one line every time somebody is in a hurry, and it is the line that
 * turns this into a general-purpose tracker.
 */

const teamViews = new Map<string, UsageDataRouteKey>([
	["features", UsageDataRouteKey.TeamDetail_Features],
	["forecasts", UsageDataRouteKey.TeamDetail_Forecasts],
	["metrics", UsageDataRouteKey.TeamDetail_Metrics],
	["settings", UsageDataRouteKey.TeamDetail_Settings],
	["access", UsageDataRouteKey.TeamDetail_Access],
]);

const portfolioViews = new Map<string, UsageDataRouteKey>([
	["features", UsageDataRouteKey.PortfolioDetail_Features],
	["metrics", UsageDataRouteKey.PortfolioDetail_Metrics],
	["deliveries", UsageDataRouteKey.PortfolioDetail_Deliveries],
	["settings", UsageDataRouteKey.PortfolioDetail_Settings],
	["access", UsageDataRouteKey.PortfolioDetail_Access],
]);

const detailPages = [
	{
		route: "/teams/:id/:tab?",
		views: teamViews,
		shownWhenNoTabIsNamed: UsageDataRouteKey.TeamDetail_Features,
	},
	{
		route: "/portfolios/:id/:tab?",
		views: portfolioViews,
		shownWhenNoTabIsNamed: UsageDataRouteKey.PortfolioDetail_Features,
	},
] as const;

/**
 * Whether what sits where a Team or Portfolio is named could be one, rather than a word.
 *
 * Asked only of an address that names no tab, and only there. The page for creating a Team is at
 * /teams/new, which is that exact shape, so without this, creating one would be counted as opening
 * the first tab of a Team nobody has. An address that does name a tab needs no such question: the
 * only other pages shaped like one are /teams/edit/:id and its Portfolio twin, where the tab
 * position holds a number that is on no list of tabs.
 */
const couldBeOneOfTheirs = (id: string | undefined): boolean =>
	id !== undefined && /^\d+$/.test(id);

/**
 * Which page this path is, or nothing at all.
 *
 * Nothing is the answer for every page that has no name on the list, and it has to stay that way: a
 * page answered with some stand-in member would be counted as a page somebody never opened.
 *
 * An address that names a Team or Portfolio and no tab is the first tab of it. Most links into
 * these pages are that shape - the Details button on the overview, and every place a Team's name is
 * a link - so reading it as no page at all would leave the tab people arrive on uncounted while
 * counting every tab they moved to afterwards.
 */
export function usageDataRouteKeyFor(
	pathname: string,
): UsageDataRouteKey | undefined {
	for (const page of detailPages) {
		const match = matchPath(page.route, pathname);

		if (match === null) {
			continue;
		}

		const view = match.params.tab;

		if (view !== undefined) {
			return page.views.get(view);
		}

		if (couldBeOneOfTheirs(match.params.id)) {
			return page.shownWhenNoTabIsNamed;
		}
	}

	return undefined;
}
