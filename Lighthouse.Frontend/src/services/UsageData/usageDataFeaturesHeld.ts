import { useEffect } from "react";
import { matchPath } from "react-router";
import { featuresHeldOf } from "../../models/UsageData/featuresHeldOf";
import type { UsageDataFeaturesHeld } from "../../models/UsageData/UsageData";

type OwnerKind = "Team" | "Portfolio";

/**
 * The band of Features each detail page currently on screen holds, for the tab opening the
 * detector counts on that page.
 *
 * The page's id is only the key that matches a page to its opening, inside this browser. What a
 * page hands in is already a band: the count stays on the page, and neither the count nor the id
 * is ever part of an opening.
 */
const held = new Map<string, UsageDataFeaturesHeld>();
const listeners = new Set<() => void>();

const keyOf = (kind: OwnerKind, id: string): string => `${kind}:${id}`;

const owners = [
	{ route: "/teams/:id/*", kind: "Team" },
	{ route: "/portfolios/:id/*", kind: "Portfolio" },
] as const;

const ownerKeyFor = (pathname: string): string | undefined => {
	for (const owner of owners) {
		const id = matchPath(owner.route, pathname)?.params.id;

		if (id !== undefined) {
			return keyOf(owner.kind, id);
		}
	}

	return undefined;
};

/** The band the page at this path has handed in, or nothing while it has not. */
export const featuresHeldOnPage = (
	pathname: string,
): UsageDataFeaturesHeld | undefined => {
	const key = ownerKeyFor(pathname);
	return key === undefined ? undefined : held.get(key);
};

/** Calls back each time a page hands in a band, until the returned function is called. */
export const whenAPageHandsInFeaturesHeld = (
	listener: () => void,
): (() => void) => {
	listeners.add(listener);
	return () => {
		listeners.delete(listener);
	};
};

/**
 * Hands the band of the Features a detail page holds to the tab openings counted on it.
 *
 * Nothing is handed while the owner is still loading. Once loading is over, an owner that did not
 * arrive is handed as Unknown, so an opening waiting on it does not wait for ever.
 */
export const useFeaturesHeldForUsageData = (
	kind: OwnerKind,
	id: number,
	isLoading: boolean,
	featuresCount: number | undefined,
): void => {
	useEffect(() => {
		if (isLoading) {
			return;
		}

		const key = keyOf(kind, String(id));
		held.set(key, featuresHeldOf(featuresCount));

		for (const listener of listeners) {
			listener();
		}

		return () => {
			held.delete(key);
		};
	}, [kind, id, isLoading, featuresCount]);
};
