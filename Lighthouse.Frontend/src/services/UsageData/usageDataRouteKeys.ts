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
	{ route: "/teams/:id/:tab?", views: teamViews },
	{ route: "/portfolios/:id/:tab?", views: portfolioViews },
] as const;

/**
 * Which page this path is, or nothing at all.
 *
 * Nothing is the answer for every page that has no name on the list, and it has to stay that way: a
 * page answered with some stand-in member would be counted as a page somebody never opened.
 */
export function usageDataRouteKeyFor(
	pathname: string,
): UsageDataRouteKey | undefined {
	for (const page of detailPages) {
		const view = matchPath(page.route, pathname)?.params.tab;

		if (view !== undefined) {
			return page.views.get(view);
		}
	}

	return undefined;
}
