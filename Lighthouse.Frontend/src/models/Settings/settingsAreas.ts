export type SettingsAreaSlug = "organization" | "access" | "system";

export type SettingsTabSlug =
	| "terminology"
	| "calendar"
	| "refresh-ordering"
	| "system-wide"
	| "api-keys"
	| "demo-data"
	| "database"
	| "encryption"
	| "usage-data"
	| "system-info"
	| "logs"
	| "refresh-history"
	| "license";

/** The three facts about a person's access that decide what Settings shows them. */
export interface SettingsAccess {
	readonly isLoading: boolean;
	readonly isSystemAdmin: boolean;
	readonly isRbacEnabled: boolean;
}

type WhoMayOpen = "everyone" | "system-admin" | "system-admin-with-rbac";

export interface SettingsArea {
	readonly slug: SettingsAreaSlug;
	readonly label: string;
	/** One line saying whose area it is, shown beside the entry in the header menu. */
	readonly description: string;
	readonly menuTestId: string;
}

export interface SettingsTab {
	readonly area: SettingsAreaSlug;
	readonly slug: SettingsTabSlug;
	readonly label: string;
	readonly testId: string;
	readonly panelTestId: string;
	readonly whoMayOpen: WhoMayOpen;
	/** Addresses this tab answered to before, which keep opening it. */
	readonly earlierSlugs: readonly string[];
}

const SETTINGS_ROOT = "/settings";

export const SETTINGS_AREAS: readonly SettingsArea[] = [
	{
		slug: "organization",
		label: "Organization",
		description: "How we work",
		menuTestId: "settings-area-organization",
	},
	{
		slug: "access",
		label: "Access",
		description: "Who may do what",
		menuTestId: "settings-area-access",
	},
	{
		slug: "system",
		label: "System",
		description: "Running this server",
		menuTestId: "settings-area-system",
	},
];

/** Every tab, grouped by area and in the order a person reads them. */
export const SETTINGS_TABS: readonly SettingsTab[] = [
	{
		area: "organization",
		slug: "terminology",
		label: "Terminology",
		testId: "terminology-tab",
		panelTestId: "terminology-panel",
		whoMayOpen: "system-admin",
		earlierSlugs: ["configuration"],
	},
	{
		area: "organization",
		slug: "calendar",
		label: "Calendar",
		testId: "calendar-tab",
		panelTestId: "calendar-panel",
		whoMayOpen: "system-admin",
		earlierSlugs: [],
	},
	{
		area: "organization",
		slug: "refresh-ordering",
		label: "Refresh & ordering",
		testId: "refresh-ordering-tab",
		panelTestId: "refresh-ordering-panel",
		whoMayOpen: "system-admin",
		earlierSlugs: [],
	},
	{
		area: "access",
		slug: "system-wide",
		label: "System-wide access",
		testId: "rbac-tab",
		panelTestId: "rbac-panel",
		whoMayOpen: "system-admin-with-rbac",
		earlierSlugs: ["system-admins"],
	},
	{
		area: "access",
		slug: "api-keys",
		label: "API Keys",
		testId: "api-keys-tab",
		panelTestId: "api-keys-panel",
		whoMayOpen: "everyone",
		earlierSlugs: [],
	},
	{
		area: "system",
		slug: "demo-data",
		label: "Demo Data",
		testId: "demo-data-tab",
		panelTestId: "demo-data-panel",
		whoMayOpen: "system-admin",
		earlierSlugs: [],
	},
	{
		area: "system",
		slug: "database",
		label: "Database",
		testId: "database-tab",
		panelTestId: "database-panel",
		whoMayOpen: "system-admin",
		earlierSlugs: [],
	},
	{
		area: "system",
		slug: "encryption",
		label: "Encryption",
		testId: "encryption-tab",
		panelTestId: "encryption-panel",
		whoMayOpen: "system-admin",
		earlierSlugs: [],
	},
	{
		area: "system",
		slug: "usage-data",
		label: "Usage Data",
		testId: "usage-data-tab",
		panelTestId: "usage-data-panel",
		whoMayOpen: "system-admin",
		earlierSlugs: [],
	},
	{
		area: "system",
		slug: "system-info",
		label: "System Info",
		testId: "system-info-tab",
		panelTestId: "system-info-panel",
		whoMayOpen: "everyone",
		earlierSlugs: [],
	},
	{
		area: "system",
		slug: "logs",
		label: "Logs",
		testId: "logs-tab",
		panelTestId: "logs-panel",
		whoMayOpen: "everyone",
		earlierSlugs: [],
	},
	{
		area: "system",
		slug: "refresh-history",
		label: "Refresh History",
		testId: "refresh-history-tab",
		panelTestId: "refresh-history-panel",
		whoMayOpen: "everyone",
		earlierSlugs: [],
	},
	{
		area: "system",
		slug: "license",
		label: "License",
		testId: "license-tab",
		panelTestId: "license-panel",
		whoMayOpen: "everyone",
		earlierSlugs: [],
	},
];

/** The keys the old single page opened tabs by (?tab=), matched without regard to case. */
const LEGACY_TAB_KEYS: ReadonlyMap<string, SettingsTabSlug> = new Map([
	["configuration", "terminology"],
	["system", "terminology"],
	["demodata", "demo-data"],
	["demo-data", "demo-data"],
	["database", "database"],
	["database-management", "database"],
	["api-keys", "api-keys"],
	["apikeys", "api-keys"],
	["encryption", "encryption"],
	["rbac", "system-wide"],
	["authorization", "system-wide"],
	["logs", "logs"],
	["systeminfo", "system-info"],
	["system-info", "system-info"],
]);

export const settingsAreaPath = (area: SettingsAreaSlug): string =>
	`${SETTINGS_ROOT}/${area}`;

export const settingsTabPath = (
	area: SettingsAreaSlug,
	tab: SettingsTabSlug,
): string => `${settingsAreaPath(area)}/${tab}`;

export const isSettingsPath = (pathname: string): boolean =>
	pathname === SETTINGS_ROOT || pathname.startsWith(`${SETTINGS_ROOT}/`);

export const areaBySlug = (
	slug: string | undefined,
): SettingsArea | undefined =>
	SETTINGS_AREAS.find((area) => area.slug === slug);

/** The tab an address names within an area, by its current or an earlier slug. */
export const tabByAddress = (
	area: SettingsAreaSlug,
	slug: string | undefined,
): { tab: SettingsTab; isEarlierSlug: boolean } | undefined => {
	for (const tab of SETTINGS_TABS) {
		if (tab.area !== area) continue;
		if (tab.slug === slug) return { tab, isEarlierSlug: false };
		if (slug !== undefined && tab.earlierSlugs.includes(slug)) {
			return { tab, isEarlierSlug: true };
		}
	}
	return undefined;
};

export const tabOfLegacyKey = (key: string): SettingsTab | undefined => {
	const slug = LEGACY_TAB_KEYS.get(key.toLowerCase());
	return SETTINGS_TABS.find((tab) => tab.slug === slug);
};

export const mayOpen = (tab: SettingsTab, access: SettingsAccess): boolean => {
	switch (tab.whoMayOpen) {
		case "everyone":
			return true;
		case "system-admin":
			return access.isSystemAdmin;
		case "system-admin-with-rbac":
			return access.isSystemAdmin && access.isRbacEnabled;
	}
};

export const visibleTabsOf = (
	area: SettingsAreaSlug,
	access: SettingsAccess,
): SettingsTab[] =>
	SETTINGS_TABS.filter((tab) => tab.area === area && mayOpen(tab, access));

export const visibleAreasOf = (access: SettingsAccess): SettingsArea[] =>
	SETTINGS_AREAS.filter((area) => visibleTabsOf(area.slug, access).length > 0);
