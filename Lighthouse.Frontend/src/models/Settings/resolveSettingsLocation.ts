import {
	areaBySlug,
	mayOpen,
	type SettingsAccess,
	type SettingsAreaSlug,
	type SettingsTab,
	type SettingsTabSlug,
	settingsTabPath,
	tabByAddress,
	tabOfLegacyKey,
	visibleAreasOf,
	visibleTabsOf,
} from "./settingsAreas";

export interface SettingsRequest {
	readonly area?: string;
	readonly tab?: string;
	/** The value of the old ?tab= parameter. */
	readonly legacyKey?: string;
}

export type SettingsLocation =
	| {
			readonly kind: "show";
			readonly area: SettingsAreaSlug;
			readonly tab: SettingsTabSlug;
	  }
	| { readonly kind: "redirect"; readonly path: string }
	| { readonly kind: "wait" };

const WAIT: SettingsLocation = { kind: "wait" };

const redirectToTab = (tab: SettingsTab): SettingsLocation => ({
	kind: "redirect",
	path: settingsTabPath(tab.area, tab.slug),
});

/** The tab to land on when the one asked for is not open to the person: the nearest one they may open. */
const landingNear = (
	areaSlug: string | undefined,
	access: SettingsAccess,
): SettingsLocation => {
	const area = areaBySlug(areaSlug);
	const inArea = area ? visibleTabsOf(area.slug, access) : [];
	const [first] =
		inArea.length > 0
			? inArea
			: visibleAreasOf(access).flatMap((each) =>
					visibleTabsOf(each.slug, access),
				);
	return redirectToTab(first);
};

/**
 * Decides what a settings address opens for a person's access. It decides nothing while access is still loading, and
 * what it shows or redirects to is always a tab that person may open.
 */
export const resolveSettingsLocation = (
	request: SettingsRequest,
	access: SettingsAccess,
): SettingsLocation => {
	if (access.isLoading) return WAIT;

	const legacyTab =
		request.legacyKey === undefined
			? undefined
			: tabOfLegacyKey(request.legacyKey);
	if (legacyTab) {
		return mayOpen(legacyTab, access)
			? redirectToTab(legacyTab)
			: landingNear(legacyTab.area, access);
	}

	const area = areaBySlug(request.area);
	const asked = area && tabByAddress(area.slug, request.tab);
	if (!asked || !mayOpen(asked.tab, access)) {
		return landingNear(request.area, access);
	}
	return asked.isEarlierSlug
		? redirectToTab(asked.tab)
		: { kind: "show", area: asked.tab.area, tab: asked.tab.slug };
};
