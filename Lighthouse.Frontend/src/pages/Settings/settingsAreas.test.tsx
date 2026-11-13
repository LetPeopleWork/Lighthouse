import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
	act,
	fireEvent,
	render,
	screen,
	waitFor,
	within,
} from "@testing-library/react";
import { Suspense } from "react";
import {
	MemoryRouter,
	Routes,
	useLocation,
	useNavigationType,
} from "react-router";
import { afterEach, describe, expect, it, vi } from "vitest";
import Header from "../../components/App/Header/Header";
import RecentProblemsSection from "../../components/App/Header/TaskManager/RecentProblemsSection";
import OAuthPremiumNotice from "../../components/Common/Connection/OAuthPremiumNotice";
import DataOverviewTable from "../../components/Common/DataOverviewTable/DataOverviewTable";
import { ApiServiceContext } from "../../services/Api/ApiServiceContext";
import type { ILicensingService } from "../../services/Api/LicensingService";
import { usageDataPageOpeningFor } from "../../services/UsageData/usageDataRouteKeys";
import {
	createMockApiServiceContext,
	createMockRbacService,
	createMockUpdateSubscriptionService,
} from "../../tests/MockApiServiceProvider";
import { renderWithQueryClient } from "../../tests/renderWithQueryClient";

// The panels are not what is under test here: each tab has to open its own panel, whatever the panel shows.
vi.mock("./SystemInfo/SystemInfoSettings", () => ({
	default: () => <div>system info panel content</div>,
}));
vi.mock("./DemoData/DemoDataSettings", () => ({
	default: () => <div>demo data panel content</div>,
}));
vi.mock("./Organization/TerminologyTab", () => ({
	default: () => <div>terminology panel content</div>,
}));
vi.mock("./Organization/CalendarTab", () => ({
	default: () => <div>calendar panel content</div>,
}));
vi.mock("./Organization/RefreshAndOrderingTab", () => ({
	default: () => <div>refresh and ordering panel content</div>,
}));
vi.mock("./System/UsageDataTab", () => ({
	default: () => <div>usage data panel content</div>,
}));
vi.mock("./Rbac/RbacSettings", () => ({
	default: () => <div>system-wide access panel content</div>,
}));
vi.mock("./DatabaseManagement/DatabaseManagementSettings", () => ({
	default: () => <div>database panel content</div>,
}));
vi.mock("./ApiKeys/ApiKeysSettings", () => ({
	default: () => <div>api keys panel content</div>,
}));
vi.mock("./Encryption/EncryptionPanel", () => ({
	default: () => <div>encryption panel content</div>,
}));
vi.mock("../../hooks/useUpdateAll", () => ({
	useUpdateAll: () => ({
		handleUpdateAll: vi.fn(),
		globalUpdateStatus: { hasActiveUpdates: false, activeCount: 0 },
		isLoading: false,
		hasError: false,
	}),
}));

// ---------------------------------------------------------------------------------------------
// What a person's access looks like, and what the settings areas promise each of them
// ---------------------------------------------------------------------------------------------

type AreaSlug = "organization" | "access" | "system";

interface Access {
	isLoading: boolean;
	isSystemAdmin: boolean;
	isRbacEnabled: boolean;
}

interface SettingsRequest {
	area?: string;
	tab?: string;
	legacyKey?: string;
}

type Outcome =
	| { kind: "show"; area: string; tab: string }
	| { kind: "redirect"; path: string }
	| { kind: "wait" };

type ResolveSettingsLocation = (
	request: SettingsRequest,
	access: Access,
) => Outcome;

const ADMIN: Access = {
	isLoading: false,
	isSystemAdmin: true,
	isRbacEnabled: true,
};
const ADMIN_WITHOUT_RBAC: Access = {
	isLoading: false,
	isSystemAdmin: true,
	isRbacEnabled: false,
};
const READER: Access = {
	isLoading: false,
	isSystemAdmin: false,
	isRbacEnabled: true,
};

/** Every combination of the three facts the resolver is given, the whole of its input domain. */
const EVERY_ACCESS: Access[] = [true, false].flatMap((isLoading) =>
	[true, false].flatMap((isSystemAdmin) =>
		[true, false].map((isRbacEnabled) => ({
			isLoading,
			isSystemAdmin,
			isRbacEnabled,
		})),
	),
);

const AREAS: readonly AreaSlug[] = ["organization", "access", "system"];

/** The tabs of each area in the order a person reads them. */
const TABS_OF: Readonly<Record<AreaSlug, readonly string[]>> = {
	organization: ["terminology", "calendar", "refresh-ordering"],
	access: ["system-wide", "api-keys"],
	system: ["demo-data", "database", "encryption", "usage-data", "system-info"],
};

/** Who may open a tab: everybody reads API Keys and System Info; the rest is for System Admins, and System-wide access also needs RBAC on. */
const mayOpen = (tab: string, access: Access): boolean => {
	if (tab === "api-keys" || tab === "system-info") return true;
	if (tab === "system-wide")
		return access.isSystemAdmin && access.isRbacEnabled;
	return access.isSystemAdmin;
};

const visibleTabsOf = (area: AreaSlug, access: Access): string[] =>
	TABS_OF[area].filter((tab) => mayOpen(tab, access));

const visibleAreasOf = (access: Access): AreaSlug[] =>
	AREAS.filter((area) => visibleTabsOf(area, access).length > 0);

/** The 14 keys the old page opened by, and where each one leads now. */
const LEGACY_KEYS: ReadonlyArray<readonly [string, string]> = [
	["configuration", "/settings/organization/terminology"],
	["system", "/settings/organization/terminology"],
	["demodata", "/settings/system/demo-data"],
	["demo-data", "/settings/system/demo-data"],
	["database", "/settings/system/database"],
	["database-management", "/settings/system/database"],
	["api-keys", "/settings/access/api-keys"],
	["apikeys", "/settings/access/api-keys"],
	["encryption", "/settings/system/encryption"],
	["rbac", "/settings/access/system-wide"],
	["authorization", "/settings/access/system-wide"],
	["logs", "/settings/system/system-info"],
	["systeminfo", "/settings/system/system-info"],
	["system-info", "/settings/system/system-info"],
];

/** The slug the Access tab had before it became System-wide access; it must keep opening that tab. */
const EARLIER_ACCESS_SLUG = "system-admins";

// ---------------------------------------------------------------------------------------------
// Steps
// ---------------------------------------------------------------------------------------------

const theDirectoryOfThisTest = dirname(fileURLToPath(import.meta.url));

/**
 * Loads a module the settings areas are built from. A module that is not there yet fails the test with what to build,
 * rather than breaking the whole file before any test runs.
 */
const theBuiltModule = async <T,>(
	file: string,
	whatItIs: string,
): Promise<T> => {
	try {
		return (await import(
			/* @vite-ignore */ resolve(theDirectoryOfThisTest, file)
		)) as T;
	} catch (error) {
		const notThere =
			(error as { code?: string }).code === "ERR_MODULE_NOT_FOUND" ||
			/cannot find module|failed to load url|does not exist|no such file/i.test(
				(error as Error).message,
			);
		if (!notThere) {
			throw error;
		}
		return expect.fail(
			`${whatItIs} is missing (${file}). Settings addresses cannot be resolved without it, so old links and the area pages cannot work. Create it as the settings feature describes.`,
		);
	}
};

const theResolver = async (): Promise<ResolveSettingsLocation> => {
	const built = await theBuiltModule<{
		resolveSettingsLocation: ResolveSettingsLocation;
	}>(
		"../../models/Settings/resolveSettingsLocation.ts",
		"The function that decides what a settings address shows",
	);
	return built.resolveSettingsLocation;
};

const theSettingsRoutes = async () => {
	const built = await theBuiltModule<{
		settingsRoutes: () => React.ReactElement;
	}>("settingsRoutes.tsx", "The settings routes");
	return built.settingsRoutes;
};

const theRequestFor = (path: string): SettingsRequest => {
	const [, , area, tab] = path.split("/");
	return { area, tab };
};

/** Follows redirects the way the page does, until the address shows something or waits. */
const whereItEnds = (
	resolve: ResolveSettingsLocation,
	request: SettingsRequest,
	access: Access,
): Outcome => {
	let outcome = resolve(request, access);
	for (let hops = 0; outcome.kind === "redirect" && hops < 3; hops++) {
		outcome = resolve(theRequestFor(outcome.path), access);
	}
	return outcome;
};

const expectItShows = (
	outcome: Outcome,
	area: string,
	tab: string,
	why: string,
) => {
	expect(
		outcome,
		`${why}. The address must end on the ${area} area, ${tab} tab, because a person has to land where the setting lives. Resolve it to that tab for this access.`,
	).toMatchObject({ kind: "show", area, tab });
};

const aSummary = (access: Access) => ({
	isRbacEnabled: access.isRbacEnabled,
	isSystemAdmin: access.isSystemAdmin,
	canCreateTeam: false,
	canCreatePortfolio: false,
});

type Summary = ReturnType<typeof aSummary>;

const aSlowAnswer = () => {
	let answer: (summary: Summary) => void = () => {};
	const promise = new Promise<Summary>((resolveAnswer) => {
		answer = resolveAnswer;
	});
	return { promise, answer };
};

const WhereWeAre = () => {
	const location = useLocation();
	const navigationType = useNavigationType();
	return (
		<>
			<output data-testid="where-we-are">
				{location.pathname + location.search}
			</output>
			<output data-testid="how-we-got-there">{navigationType}</output>
		</>
	);
};

const whereWeAre = () => screen.getByTestId("where-we-are").textContent;

/** A redirect or a tab choice replaces the address, so Back never returns to one that bounced. */
const expectTheAddressWasReplaced = () => {
	expect(
		screen.getByTestId("how-we-got-there").textContent,
		"The address must be replaced, not pushed, or Back loops through the old address",
	).toBe("REPLACE");
};

const renderSettingsAt = async (
	address: string,
	summary: Summary | Promise<Summary> | "fails",
) => {
	const settingsRoutes = await theSettingsRoutes();
	const rbacService = createMockRbacService();
	rbacService.getAuthorizationSummary = vi
		.fn()
		.mockImplementation(() =>
			summary === "fails"
				? Promise.reject(new Error("the access read failed"))
				: Promise.resolve(summary),
		);
	renderWithQueryClient(
		<ApiServiceContext.Provider
			value={createMockApiServiceContext({ rbacService })}
		>
			<MemoryRouter initialEntries={[address]}>
				<Suspense fallback={null}>
					<Routes>{settingsRoutes()}</Routes>
				</Suspense>
				<WhereWeAre />
			</MemoryRouter>
		</ApiServiceContext.Provider>,
	);
};

const theHeading = (name: string) =>
	screen.findByRole("heading", { level: 1, name });

const theTabNames = () =>
	screen.getAllByRole("tab").map((tab) => tab.textContent);

const theSelectedTab = () =>
	screen
		.getAllByRole("tab")
		.filter((tab) => tab.getAttribute("aria-selected") === "true")
		.map((tab) => tab.textContent);

const theEndOfThe = async (address: string) => {
	await waitFor(() => expect(whereWeAre()).toBe(address));
};

const setMobileScreen = (matches: boolean) => {
	vi.stubGlobal("matchMedia", (query: string) => ({
		matches,
		media: query,
		onchange: null,
		addListener: () => {},
		removeListener: () => {},
		addEventListener: () => {},
		removeEventListener: () => {},
		dispatchEvent: () => false,
	}));
};

const renderHeaderWithAccess = (
	summary: Summary | Promise<Summary>,
	address = "/",
) => {
	const rbacService = createMockRbacService();
	rbacService.getAuthorizationSummary = vi
		.fn()
		.mockReturnValue(Promise.resolve(summary));
	const licensingService: ILicensingService = {
		getLicenseStatus: vi
			.fn()
			.mockResolvedValue({ hasLicense: true, isValid: true }),
		importLicense: vi.fn(),
		clearLicense: vi.fn(),
	};
	const queryClient = new QueryClient({
		defaultOptions: { queries: { retry: false } },
	});
	render(
		<QueryClientProvider client={queryClient}>
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({
					licensingService,
					rbacService,
					updateSubscriptionService: createMockUpdateSubscriptionService(),
				})}
			>
				<MemoryRouter initialEntries={[address]}>
					<Header />
					<WhereWeAre />
				</MemoryRouter>
			</ApiServiceContext.Provider>
		</QueryClientProvider>,
	);
};

const openTheSettingsMenu = () => {
	fireEvent.click(screen.getByRole("button", { name: "Settings" }));
};

const theMenuEntries = () =>
	screen.queryAllByRole("menuitem").map((entry) => entry.textContent ?? "");

afterEach(() => {
	vi.unstubAllGlobals();
	vi.restoreAllMocks();
});

// ---------------------------------------------------------------------------------------------
// The deciding rule, on its function: what an address shows for a person's access
// ---------------------------------------------------------------------------------------------

describe("which settings area and tab an address opens", () => {
	it.each(LEGACY_KEYS)(
		"the old key %s, in lower and in capital letters, redirects to %s",
		async (key, path) => {
			const resolve = await theResolver();

			for (const spelled of [key, key.toUpperCase()]) {
				expect(
					resolve({ legacyKey: spelled }, ADMIN),
					`The old link ?tab=${spelled} must send a System Admin to ${path}, because bookmarks and runbooks use it. Map every old key, matched without regard to case, to the tab that now holds the setting, as a redirect.`,
				).toMatchObject({ kind: "redirect", path });
			}
		},
	);

	it("the earlier address of System-wide access still opens that tab for a System Admin", async () => {
		const resolve = await theResolver();

		expectItShows(
			whereItEnds(resolve, { area: "access", tab: EARLIER_ACCESS_SLUG }, ADMIN),
			"access",
			"system-wide",
			"The tab that was called System Admins keeps its old address working",
		);
	});

	it("a bare settings address lands on Organization for a System Admin and on the first area a reader may see otherwise", async () => {
		const resolve = await theResolver();

		expectItShows(
			whereItEnds(resolve, {}, ADMIN),
			"organization",
			"terminology",
			"A System Admin opening Settings",
		);
		expectItShows(
			whereItEnds(resolve, {}, ADMIN_WITHOUT_RBAC),
			"organization",
			"terminology",
			"Anyone opening Settings when RBAC is off",
		);
		expectItShows(
			whereItEnds(resolve, {}, READER),
			"access",
			"api-keys",
			"A reader opening Settings",
		);
	});

	it("an area, tab or key the person may not open falls back to what they may", async () => {
		const resolve = await theResolver();

		expectItShows(
			whereItEnds(
				resolve,
				{ area: "organization", tab: "terminology" },
				READER,
			),
			"access",
			"api-keys",
			"A reader asking for Organization",
		);
		expectItShows(
			whereItEnds(resolve, { area: "system", tab: "database" }, READER),
			"system",
			"system-info",
			"A reader asking for the Database tab",
		);
		expectItShows(
			whereItEnds(resolve, { legacyKey: "database" }, READER),
			"system",
			"system-info",
			"A reader following the old database link",
		);
		expectItShows(
			whereItEnds(
				resolve,
				{ area: "access", tab: "system-wide" },
				ADMIN_WITHOUT_RBAC,
			),
			"access",
			"api-keys",
			"A System Admin asking for System-wide access when RBAC is off",
		);
		expectItShows(
			whereItEnds(
				resolve,
				{ area: "access", tab: EARLIER_ACCESS_SLUG },
				READER,
			),
			"access",
			"api-keys",
			"A reader following the earlier System-wide access address",
		);
	});

	it("an unknown area or tab falls back to the first one a System Admin may open", async () => {
		const resolve = await theResolver();

		expectItShows(
			whereItEnds(resolve, { area: "nonsense" }, ADMIN),
			"organization",
			"terminology",
			"An address naming no area",
		);
		expectItShows(
			whereItEnds(resolve, { area: "system", tab: "nonsense" }, ADMIN),
			"system",
			"demo-data",
			"An address naming no tab of System",
		);
		expectItShows(
			whereItEnds(resolve, { area: "access" }, ADMIN),
			"access",
			"system-wide",
			"The Access area opened without a tab",
		);
	});

	it("never decides while the person's access is still loading", async () => {
		const resolve = await theResolver();
		const requests: SettingsRequest[] = [
			{},
			{ legacyKey: "database" },
			{ area: "system", tab: "database" },
			{ area: "access", tab: EARLIER_ACCESS_SLUG },
			{ area: "nonsense", tab: "nonsense" },
		];

		for (const access of EVERY_ACCESS.filter((each) => each.isLoading)) {
			for (const request of requests) {
				expect(
					resolve(request, access),
					`With access still loading (${JSON.stringify(access)}) the address ${JSON.stringify(request)} must wait. Deciding early sends a System Admin to the first tab before their access is known.`,
				).toEqual({ kind: "wait" });
			}
		}
	});
});

// ---------------------------------------------------------------------------------------------
// The same rule over the whole input space, generated
// ---------------------------------------------------------------------------------------------

const seededRandom = (seed: number) => {
	let state = seed;
	return () => {
		state = (state + 0x6d2b79f5) | 0;
		let mixed = Math.imul(state ^ (state >>> 15), 1 | state);
		mixed = (mixed + Math.imul(mixed ^ (mixed >>> 7), 61 | mixed)) ^ mixed;
		return ((mixed ^ (mixed >>> 14)) >>> 0) / 4294967296;
	};
};

const SEED = 6473;
const CASES = 500;
const JUNK_ALPHABET = "abcxyz-_09/.? ";

type CaseKind =
	| "visible-tab"
	| "hidden-tab"
	| "earlier-slug"
	| "legacy-key"
	| "unknown-area"
	| "unknown-tab"
	| "bare";

const CASE_KINDS: readonly CaseKind[] = [
	"visible-tab",
	"hidden-tab",
	"earlier-slug",
	"legacy-key",
	"unknown-area",
	"unknown-tab",
	"bare",
];

const EVERY_TAB = AREAS.flatMap((area) =>
	TABS_OF[area].map((tab) => ({ area, tab })),
);

/** An address of the given kind, built so the kind is certain, with an access it can meet. */
const aGeneratedCase = (
	kind: CaseKind,
	next: () => number,
): { access: Access; request: SettingsRequest } => {
	const pick = <T,>(items: readonly T[]): T =>
		items[Math.floor(next() * items.length)];
	const junk = () =>
		Array.from(
			{ length: 1 + Math.floor(next() * 12) },
			() => JUNK_ALPHABET[Math.floor(next() * JUNK_ALPHABET.length)],
		).join("");
	const inRandomCase = (word: string) =>
		[...word]
			.map((letter) => (next() < 0.5 ? letter.toUpperCase() : letter))
			.join("");
	const access =
		kind === "hidden-tab"
			? pick(
					EVERY_ACCESS.filter(
						(each) => !each.isSystemAdmin || !each.isRbacEnabled,
					),
				)
			: pick(EVERY_ACCESS);
	const requests: Record<CaseKind, () => SettingsRequest> = {
		"visible-tab": () =>
			pick(EVERY_TAB.filter(({ tab }) => mayOpen(tab, access))),
		"hidden-tab": () =>
			pick(EVERY_TAB.filter(({ tab }) => !mayOpen(tab, access))),
		"earlier-slug": () => ({ area: "access", tab: EARLIER_ACCESS_SLUG }),
		"legacy-key": () => ({ legacyKey: inRandomCase(pick(LEGACY_KEYS)[0]) }),
		"unknown-area": () => ({
			area: junk(),
			tab: next() < 0.5 ? junk() : pick(EVERY_TAB).tab,
		}),
		"unknown-tab": () => ({ area: pick(AREAS), tab: junk() }),
		bare: () => ({}),
	};
	return { access, request: requests[kind]() };
};

/** A redirect target shows at once, and what is shown is an area and tab the person may open. */
const expectItConverges = (
	resolve: ResolveSettingsLocation,
	request: SettingsRequest,
	access: Access,
	outcome: Outcome,
	replay: string,
) => {
	if (outcome.kind === "redirect") {
		expect(
			resolve(theRequestFor(outcome.path), access).kind,
			`The target ${outcome.path} of a redirect must show at once (${replay})`,
		).toBe("show");
		expect(
			outcome.path,
			`A redirect target carries no ?tab= (${replay})`,
		).not.toContain("?");
	}
	const landed = whereItEnds(resolve, request, access);
	expect(landed.kind, `The address must end on a tab (${replay})`).toBe("show");
	if (landed.kind === "show") {
		expect(
			visibleAreasOf(access),
			`${landed.area} is shown to a person who may open none of its tabs (${replay})`,
		).toContain(landed.area);
		expect(
			visibleTabsOf(landed.area as AreaSlug, access),
			`${landed.area}/${landed.tab} is shown to a person who may not open it (${replay})`,
		).toContain(landed.tab);
	}
};

describe("every settings address ends on a tab the person may open", () => {
	it("a redirect leads to an address that shows, a shown tab is one the person may open, and a visible tab's own address shows that tab", async () => {
		const resolve = await theResolver();
		const random = seededRandom(SEED);

		for (let index = 0; index < CASES; index++) {
			const kind = CASE_KINDS[index % CASE_KINDS.length];
			const { access, request } = aGeneratedCase(kind, random);
			const replay = `seed ${SEED}, case ${index}, ${kind}, request ${JSON.stringify(request)}, access ${JSON.stringify(access)}`;

			const outcome = resolve(request, access);

			if (access.isLoading) {
				expect(
					outcome,
					`Access is loading, so it must wait (${replay})`,
				).toEqual({
					kind: "wait",
				});
				continue;
			}
			expect(outcome.kind, `Loaded access never waits (${replay})`).not.toBe(
				"wait",
			);

			expectItConverges(resolve, request, access, outcome, replay);
			if (kind === "visible-tab" && request.area !== undefined) {
				expect(
					outcome,
					`A tab the person may open shows as it is addressed (${replay})`,
				).toMatchObject({ kind: "show", area: request.area, tab: request.tab });
			}
		}
	});
});

// ---------------------------------------------------------------------------------------------
// The pages a person sees
// ---------------------------------------------------------------------------------------------

describe("the three settings area pages", () => {
	it.each([
		{
			address: "/settings",
			ends: "/settings/organization/terminology",
			heading: "Settings › Organization",
			tabs: ["Terminology", "Calendar", "Refresh & ordering"],
			panel: "terminology panel content",
		},
		{
			address: "/settings/access",
			ends: "/settings/access/system-wide",
			heading: "Settings › Access",
			tabs: ["System-wide access", "API Keys"],
			panel: "system-wide access panel content",
		},
		{
			address: "/settings/system",
			ends: "/settings/system/demo-data",
			heading: "Settings › System",
			tabs: [
				"Demo Data",
				"Database",
				"Encryption",
				"Usage Data",
				"System Info",
			],
			panel: "demo data panel content",
		},
	])(
		"a System Admin opening $address sees $heading with its own tabs",
		async ({ address, ends, heading, tabs, panel }) => {
			await renderSettingsAt(address, aSummary(ADMIN));

			await theHeading(heading);
			await theEndOfThe(ends);
			expect(theTabNames(), `${heading} lists its tabs in this order`).toEqual(
				tabs,
			);
			expect(theSelectedTab(), "The first tab is the one open").toEqual([
				tabs[0],
			]);
			expect(screen.getByText(panel)).toBeInTheDocument();
		},
	);

	it("an old tab key opens the area and tab that now hold that setting, with no ?tab= left in the address", async () => {
		await renderSettingsAt("/settings?tab=database", aSummary(ADMIN));

		await theHeading("Settings › System");
		await theEndOfThe("/settings/system/database");
		expect(theSelectedTab()).toEqual(["Database"]);
		expect(screen.getByText("database panel content")).toBeInTheDocument();
		expectTheAddressWasReplaced();
	});

	it("a deep link to System-wide access shows a spinner until access has loaded, then opens that tab and not the first one", async () => {
		const slow = aSlowAnswer();
		await renderSettingsAt("/settings/access/system-admins", slow.promise);

		expect(await screen.findByRole("progressbar")).toBeInTheDocument();
		expect(
			screen.queryAllByRole("tab"),
			"No tab row is drawn before access is known, because the tabs a person sees depend on it",
		).toEqual([]);
		expect(
			whereWeAre(),
			"The address must not be changed before access is known, or a System Admin is bounced to the first tab",
		).toBe("/settings/access/system-admins");

		await act(async () => slow.answer(aSummary(ADMIN)));

		await theHeading("Settings › Access");
		await theEndOfThe("/settings/access/system-wide");
		expect(theSelectedTab()).toEqual(["System-wide access"]);
		expect(screen.getByTestId("rbac-tab")).toHaveAttribute(
			"aria-selected",
			"true",
		);
		expect(
			screen.getByText("system-wide access panel content"),
		).toBeInTheDocument();
		expect(screen.queryByRole("progressbar")).not.toBeInTheDocument();
	});

	it("a reader opening Settings lands on Access with API Keys and cannot reach Organization", async () => {
		await renderSettingsAt("/settings", aSummary(READER));

		await theHeading("Settings › Access");
		await theEndOfThe("/settings/access/api-keys");
		expect(theTabNames()).toEqual(["API Keys"]);
		expect(screen.getByText("api keys panel content")).toBeInTheDocument();
	});

	it("a reader asking for the Database tab is sent to what they may open", async () => {
		await renderSettingsAt("/settings/system/database", aSummary(READER));

		await theHeading("Settings › System");
		await theEndOfThe("/settings/system/system-info");
		expect(theTabNames()).toEqual(["System Info"]);
		expect(screen.getByText("system info panel content")).toBeInTheDocument();
		expectTheAddressWasReplaced();
	});

	it("a failed access read locks nobody out: the person sees what the page shows with RBAC off, without System-wide access", async () => {
		await renderSettingsAt("/settings/access/system-wide", "fails");

		await theHeading("Settings › Access");
		await theEndOfThe("/settings/access/api-keys");
		expect(theTabNames()).toEqual(["API Keys"]);
		expect(screen.getByText("api keys panel content")).toBeInTheDocument();
		expect(screen.queryByRole("progressbar")).not.toBeInTheDocument();
	});

	it("a failed access read still opens Organization from the bare address", async () => {
		await renderSettingsAt("/settings", "fails");

		await theHeading("Settings › Organization");
		await theEndOfThe("/settings/organization/terminology");
		expect(theTabNames()).toEqual([
			"Terminology",
			"Calendar",
			"Refresh & ordering",
		]);
	});

	it("choosing another tab opens its panel at its own address", async () => {
		await renderSettingsAt("/settings/system", aSummary(ADMIN));
		await theHeading("Settings › System");

		fireEvent.click(await screen.findByTestId("database-tab"));

		await theEndOfThe("/settings/system/database");
		expect(theSelectedTab()).toEqual(["Database"]);
		expect(screen.getByText("database panel content")).toBeInTheDocument();
		expectTheAddressWasReplaced();
	});

	it("the old license address, which led nowhere, opens System", async () => {
		await renderSettingsAt("/settings/license", aSummary(ADMIN));

		await theHeading("Settings › System");
		await waitFor(() =>
			expect(whereWeAre()).toMatch(/^\/settings\/system(\/|$)/),
		);
	});
});

// ---------------------------------------------------------------------------------------------
// The header
// ---------------------------------------------------------------------------------------------

describe("the header's Settings menu", () => {
	it("offers Organization, Access and System, each saying whose it is, and opens the area chosen", async () => {
		renderHeaderWithAccess(aSummary(ADMIN));

		expect(
			screen.queryByText("System Settings"),
			"The header entry is Settings, not System Settings, because it now leads to three areas",
		).not.toBeInTheDocument();
		openTheSettingsMenu();

		await screen.findByRole("menuitem", { name: /^Organization/ });
		const entries = screen.getAllByRole("menuitem");
		expect(entries.map((entry) => entry.textContent)).toEqual([
			expect.stringMatching(/^Organization.*how we work/i),
			expect.stringMatching(/^Access.*who may do what/i),
			expect.stringMatching(/^System.*running this server/i),
		]);

		fireEvent.click(entries[1]);

		await waitFor(() =>
			expect(whereWeAre()).toMatch(/^\/settings\/access(\/|$)/),
		);
	});

	it("shows a reader Access and System but no Organization", async () => {
		renderHeaderWithAccess(aSummary(READER));

		openTheSettingsMenu();

		await screen.findByRole("menu");
		await screen.findByRole("menuitem", { name: /^Access/ });
		expect(theMenuEntries()).toEqual([
			expect.stringMatching(/^Access/),
			expect.stringMatching(/^System/),
		]);
	});

	it("shows no Organization while access is still loading", async () => {
		renderHeaderWithAccess(aSlowAnswer().promise);

		openTheSettingsMenu();

		await screen.findByRole("menu");

		expect(
			theMenuEntries().filter((entry) => entry.startsWith("Organization")),
			"Organization belongs to System Admins, so it must stay out of the menu until access has loaded",
		).toEqual([]);
	});

	it("on a small screen lists the three areas under a Settings caption that is not a link", async () => {
		setMobileScreen(true);
		renderHeaderWithAccess(aSummary(ADMIN));

		fireEvent.click(screen.getByRole("button", { name: /open drawer/i }));

		const drawer = await screen.findByRole("presentation");
		const caption = within(drawer).getByText("Settings");
		await within(drawer).findByRole("link", { name: /^Organization/ });
		expect(
			caption.closest("a"),
			"The caption names the group; it leads nowhere",
		).toBeNull();
		expect(caption.closest("button")).toBeNull();
		const areaLinks = ["Organization", "Access", "System"].map((area) =>
			within(drawer).getByRole("link", { name: new RegExp(`^${area}`) }),
		);
		expect(areaLinks.map((link) => link.getAttribute("href"))).toEqual([
			expect.stringMatching(/^\/settings\/organization(\/|$)/),
			expect.stringMatching(/^\/settings\/access(\/|$)/),
			expect.stringMatching(/^\/settings\/system(\/|$)/),
		]);
	});
});

// ---------------------------------------------------------------------------------------------
// Links elsewhere in the app that lead into settings
// ---------------------------------------------------------------------------------------------

describe("links into settings from elsewhere in the app", () => {
	it("the license link in a connection form leads to the System area", () => {
		render(
			<MemoryRouter>
				<OAuthPremiumNotice />
			</MemoryRouter>,
		);

		expect(
			screen
				.getByRole("link", { name: "View license options" })
				.getAttribute("href"),
			"The license link used to lead to an address no page answers; it must lead to the System area",
		).toMatch(/^\/settings\/system(\/|$)/);
	});

	it("the full log link in the task manager opens System Info", () => {
		render(
			<MemoryRouter>
				<RecentProblemsSection problems={[]} />
				<WhereWeAre />
			</MemoryRouter>,
		);

		fireEvent.click(screen.getByRole("button", { name: "Open the full log" }));

		expect(whereWeAre()).toBe("/settings/system/system-info");
	});

	it("the load demo data link in an empty list opens System Demo Data", () => {
		renderWithQueryClient(
			<ApiServiceContext.Provider value={createMockApiServiceContext({})}>
				<MemoryRouter>
					<DataOverviewTable
						data={[]}
						title="Test Items"
						api="teams"
						onDelete={vi.fn()}
						filterText=""
					/>
				</MemoryRouter>
			</ApiServiceContext.Provider>,
		);

		expect(
			screen.getByRole("link", { name: "Load Demo Data" }).getAttribute("href"),
		).toBe("/settings/system/demo-data");
	});

	it.each([
		"/settings/organization/terminology",
		"/settings/organization/calendar",
		"/settings/organization/refresh-ordering",
		"/settings/access/system-wide",
		"/settings/access/api-keys",
		"/settings/system/demo-data",
		"/settings/system/database",
		"/settings/system/encryption",
		"/settings/system/usage-data",
		"/settings/system/system-info",
	])("opening %s is not counted as a page anyone used", (address) => {
		expect(
			usageDataPageOpeningFor(address),
			"Settings addresses raise no usage-data page event, because the feature adds none",
		).toBeUndefined();
	});
});
