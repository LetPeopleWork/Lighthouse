import { type QueryClient, useQueryClient } from "@tanstack/react-query";
import { useEffect } from "react";
import { useLocation } from "react-router";
import { UsageDataEventName } from "../../services/Api/UsageDataService";
import { useUsageDataReporter } from "../../services/UsageData/usageDataReporter";

/**
 * The history entries whose listing was already counted. Kept with the app's query cache, which lives as long as the
 * app does, rather than in this module: a newly made router's first entry always carries the same key, so a record
 * shared by every router would take one router's count for another's.
 */
const countedListings = new WeakMap<QueryClient, Set<string>>();

const listingsCountedIn = (app: QueryClient): Set<string> => {
	const known = countedListings.get(app);
	if (known !== undefined) return known;
	const counted = new Set<string>();
	countedListings.set(app, counted);
	return counted;
};

/**
 * Reports that the Reports page showed its list, by name alone, as soon as it is shown. One listing is one entry in
 * the reader's history: reading the list again after a deletion is the same listing. A list that could not be loaded
 * was not shown, so it reports nothing.
 */
export const useAllReportsListedReporter = (
	isShowingTheList: boolean,
): void => {
	const reportUsage = useUsageDataReporter();
	const listing = useLocation().key;
	const counted = listingsCountedIn(useQueryClient());

	useEffect(() => {
		if (!isShowingTheList || counted.has(listing)) {
			return;
		}

		// Consent can arrive after the list is shown; until the event could go, the listing is not used up.
		const sent = reportUsage({ name: UsageDataEventName.AllReportsListed });
		if (sent) counted.add(listing);
	}, [counted, listing, isShowingTheList, reportUsage]);
};
