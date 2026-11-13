import {
	act,
	fireEvent,
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
import type { ILicenseStatus } from "../../../models/ILicenseStatus";
import type { IOptionalFeature } from "../../../models/OptionalFeatures/OptionalFeature";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { TerminologyProvider } from "../../../services/TerminologyContext";
import {
	createMockApiServiceContext,
	createMockBlackoutPeriodService,
	createMockLicensingService,
	createMockOptionalFeatureService,
	createMockRbacService,
	createMockRecurringBlackoutRuleService,
	createMockSettingsService,
	createMockTerminologyService,
} from "../../../tests/MockApiServiceProvider";
import { renderWithQueryClient } from "../../../tests/renderWithQueryClient";
import { settingsRoutes } from "../settingsRoutes";

// The tabs outside this file's concern are not what is under test: each only has to be there to leave for.
vi.mock("../SystemInfo/SystemInfoSettings", () => ({
	default: () => <div>system info panel content</div>,
}));
vi.mock("../DemoData/DemoDataSettings", () => ({
	default: () => <div>demo data panel content</div>,
}));
vi.mock("../Rbac/RbacSettings", () => ({
	default: () => <div>system-wide access panel content</div>,
}));
vi.mock("../DatabaseManagement/DatabaseManagementSettings", () => ({
	default: () => <div>database panel content</div>,
}));
vi.mock("../ApiKeys/ApiKeysSettings", () => ({
	default: () => <div>api keys panel content</div>,
}));
vi.mock("../Encryption/EncryptionPanel", () => ({
	default: () => <div>encryption panel content</div>,
}));

// ---------------------------------------------------------------------------------------------
// The instance the person is looking at
// ---------------------------------------------------------------------------------------------

/** What a call to the server is going to do: answer, fail, or take its time. */
type Answer<T> = T | "fails" | Promise<T>;

interface Instance {
	features: Answer<IOptionalFeature[]>;
	license: Answer<ILicenseStatus>;
}

/** Both rows carry id 0, the identity the seeder really writes, so only the key tells them apart. */
const theUsageDataSetting: IOptionalFeature = {
	id: 0,
	key: "UsageData",
	name: "Never send usage data",
	description:
		"While this is on, Lighthouse sends no usage data from this instance.",
	enabled: false,
	isPremium: false,
	isPreview: false,
};

const theOrderingSetting: IOptionalFeature = {
	id: 0,
	key: "FeatureOrdering",
	name: "Let Lighthouse own the order of your {{features}}",
	description:
		"Turn this on to arrange your {{features}} yourself. Turning it off hands the order back to your work tracking system.",
	enabled: false,
	isPremium: true,
	isPreview: false,
};

const WITH_PREMIUM: ILicenseStatus = {
	hasLicense: true,
	isValid: true,
	canUsePremiumFeatures: true,
};
const WITHOUT_PREMIUM: ILicenseStatus = {
	hasLicense: true,
	isValid: true,
	canUsePremiumFeatures: false,
};

const aHealthyInstance = (): Instance => ({
	features: [theUsageDataSetting, theOrderingSetting],
	license: WITH_PREMIUM,
});

const USAGE_DATA_DOCS =
	"https://docs.lighthouse.letpeople.work/settings/usagedata.html";

const COULD_NOT_LOAD_BEHAVIOUR = "Could not load the behaviour settings.";
const COULD_NOT_LOAD_LICENSE = "Could not load the license.";
const PREMIUM_REQUIRED = /requires a premium license/i;

const aDeferred = <T,>() => {
	let resolveIt: (value: T) => void = () => {};
	const promise = new Promise<T>((resolvePromise) => {
		resolveIt = resolvePromise;
	});
	return { promise, resolve: resolveIt };
};

const theCallOutcome = <T,>(answer: Answer<T>): Promise<T> =>
	answer === "fails"
		? Promise.reject(new Error("the call failed"))
		: Promise.resolve(answer);

// ---------------------------------------------------------------------------------------------
// Steps
// ---------------------------------------------------------------------------------------------

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

/**
 * A person who is a System Admin opens the address. The instance behaves like a real one: a switch that is
 * written is stored, and the next read of the settings returns what was stored.
 */
const openSettingsAt = (address: string, instance: Instance) => {
	const rbacService = createMockRbacService();
	rbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
		isRbacEnabled: true,
		isSystemAdmin: true,
		canCreateTeam: false,
		canCreatePortfolio: false,
	});

	let stored = Array.isArray(instance.features)
		? instance.features.map((setting) => ({ ...setting }))
		: [];
	const optionalFeatureService = createMockOptionalFeatureService();
	optionalFeatureService.getAllFeatures = vi.fn(() =>
		Array.isArray(instance.features)
			? Promise.resolve(stored.map((setting) => ({ ...setting })))
			: theCallOutcome(instance.features),
	);
	optionalFeatureService.updateFeature = vi.fn((written: IOptionalFeature) => {
		stored = stored.map((setting) =>
			setting.key === written.key
				? { ...setting, enabled: written.enabled }
				: setting,
		);
		return Promise.resolve();
	});

	const licensingService = createMockLicensingService();
	licensingService.getLicenseStatus = vi.fn(() =>
		theCallOutcome(instance.license),
	);

	const terminologyService = createMockTerminologyService();
	terminologyService.getAllTerminology = vi.fn().mockResolvedValue([
		{
			id: 1,
			key: "feature",
			defaultValue: "Feature",
			description: "Term used for a single feature",
			value: "Initiative",
		},
		{
			id: 2,
			key: "features",
			defaultValue: "Features",
			description: "Term used for multiple features",
			value: "Initiatives",
		},
	]);

	const settingsService = createMockSettingsService();
	settingsService.getRefreshSettings = vi
		.fn()
		.mockResolvedValue({ interval: 10, refreshAfter: 20, startDelay: 1 });

	renderWithQueryClient(
		<ApiServiceContext.Provider
			value={createMockApiServiceContext({
				rbacService,
				optionalFeatureService,
				licensingService,
				terminologyService,
				settingsService,
				blackoutPeriodService: createMockBlackoutPeriodService(),
				recurringBlackoutRuleService: createMockRecurringBlackoutRuleService(),
			})}
		>
			<TerminologyProvider>
				<MemoryRouter initialEntries={[address]}>
					<Suspense fallback={null}>
						<Routes>{settingsRoutes()}</Routes>
					</Suspense>
					<WhereWeAre />
				</MemoryRouter>
			</TerminologyProvider>
		</ApiServiceContext.Provider>,
	);

	return { optionalFeatureService, licensingService };
};

const theTabNames = () =>
	screen.getAllByRole("tab").map((tab) => tab.textContent);

const theSelectedTab = () =>
	screen
		.getAllByRole("tab")
		.filter((tab) => tab.getAttribute("aria-selected") === "true")
		.map((tab) => tab.textContent);

const theTab = (name: string) => screen.getByRole("tab", { name });

const theSwitchFor = async (key: string) =>
	(await screen.findByTestId(`${key}-toggle`)).querySelector(
		"input",
	) as HTMLInputElement;

afterEach(() => {
	vi.restoreAllMocks();
});

// ---------------------------------------------------------------------------------------------
// Organization: Terminology, Calendar, Refresh & ordering
// ---------------------------------------------------------------------------------------------

describe("Organization holds only how the organisation works", () => {
	it("offers Terminology, Calendar and Refresh & ordering with their icons, opens on Terminology and keeps the other tabs' sections out of it", async () => {
		openSettingsAt("/settings/organization", aHealthyInstance());

		expect(await screen.findByText("Terminology Configuration")).toBeVisible();
		expect(
			theTabNames(),
			"Organization lists exactly these three tabs, in this order, because the old Configuration tab held three separate jobs",
		).toEqual(["Terminology", "Calendar", "Refresh & ordering"]);
		expect(theSelectedTab()).toEqual(["Terminology"]);
		expect(whereWeAre()).toBe("/settings/organization/terminology");
		expect(
			within(theTab("Terminology")).getByTestId("TranslateIcon"),
		).toBeInTheDocument();
		expect(
			within(theTab("Calendar")).getByTestId("EventBusyIcon"),
		).toBeInTheDocument();
		expect(
			within(theTab("Refresh & ordering")).getByTestId("SyncIcon"),
		).toBeInTheDocument();

		expect(screen.queryByText("Blackout Periods & Recurring Rules")).toBeNull();
		expect(screen.queryByText("Initiative Refresh")).toBeNull();
		expect(screen.queryByText("Behaviour Settings")).toBeNull();
		expect(screen.queryByText("Never send usage data")).toBeNull();
	});

	it("the old Configuration link opens Terminology, with the address replaced", async () => {
		openSettingsAt("/settings?tab=configuration", aHealthyInstance());

		expect(await screen.findByText("Terminology Configuration")).toBeVisible();
		await waitFor(() =>
			expect(whereWeAre()).toBe("/settings/organization/terminology"),
		);
		expect(theSelectedTab()).toEqual(["Terminology"]);
		expect(screen.getByTestId("how-we-got-there").textContent).toBe("REPLACE");
	});
});

describe("Calendar tab", () => {
	it("shows the blackout periods with their Add buttons usable on a Premium licence, asking for the licence once", async () => {
		const { licensingService } = openSettingsAt(
			"/settings/organization/calendar",
			aHealthyInstance(),
		);

		expect(
			await screen.findByText("Blackout Periods & Recurring Rules"),
		).toBeVisible();
		await waitFor(() =>
			expect(screen.getByTestId("add-blackout-period-button")).toBeEnabled(),
		);
		expect(screen.queryByText(PREMIUM_REQUIRED)).toBeNull();
		expect(screen.queryByText("Terminology Configuration")).toBeNull();
		expect(screen.queryByText("Behaviour Settings")).toBeNull();
		expect(
			licensingService.getLicenseStatus,
			"The tab reads the licence through the one shared question, not a second one of its own",
		).toHaveBeenCalledTimes(1);
	});

	it("shows a spinner, not Premium required, while the licence is still on its way", async () => {
		const slowLicense = aDeferred<ILicenseStatus>();
		openSettingsAt("/settings/organization/calendar", {
			...aHealthyInstance(),
			license: slowLicense.promise,
		});

		expect(
			await screen.findByText("Blackout Periods & Recurring Rules"),
		).toBeVisible();
		expect(
			screen.getByRole("progressbar"),
			"While the licence is unknown the Premium-gated controls wait behind a spinner",
		).toBeInTheDocument();
		expect(
			screen.queryByText(PREMIUM_REQUIRED),
			"An unread licence must not look like no Premium",
		).toBeNull();

		await act(async () => slowLicense.resolve(WITH_PREMIUM));

		await waitFor(() =>
			expect(screen.getByTestId("add-blackout-period-button")).toBeEnabled(),
		);
		expect(screen.queryByRole("progressbar")).toBeNull();
	});

	it("says the licence could not be loaded in place of Premium required when the licence call fails", async () => {
		openSettingsAt("/settings/organization/calendar", {
			...aHealthyInstance(),
			license: "fails",
		});

		expect(await screen.findByText(COULD_NOT_LOAD_LICENSE)).toBeVisible();
		expect(
			screen.queryByText(PREMIUM_REQUIRED),
			"A failed licence call is not the same as no Premium, so it must not say Premium is required",
		).toBeNull();
		expect(screen.queryByRole("progressbar")).toBeNull();
	});
});

describe("Refresh & ordering tab", () => {
	it("shows the refresh sections and only the ordering row in the instance's own words, usable on a Premium licence", async () => {
		openSettingsAt(
			"/settings/organization/refresh-ordering",
			aHealthyInstance(),
		);

		expect(await screen.findByText("Initiative Refresh")).toBeVisible();
		expect(screen.getByText("Team Refresh")).toBeVisible();
		expect(screen.getByText("Behaviour Settings")).toBeVisible();
		expect(
			await screen.findByText(
				/Let Lighthouse own the order of your Initiatives/,
			),
		).toBeVisible();
		expect(await theSwitchFor("FeatureOrdering")).toBeEnabled();

		expect(
			screen.queryByTestId("feature-row-UsageData"),
			"The usage-data row lives under System, not here, even though the server sends both rows with the same id",
		).toBeNull();
		expect(screen.queryByText("Never send usage data")).toBeNull();
		expect(screen.queryByText("Terminology Configuration")).toBeNull();
		expect(screen.queryByText("Blackout Periods & Recurring Rules")).toBeNull();
		expect(theSelectedTab()).toEqual(["Refresh & ordering"]);
	});

	it("keeps the ordering switch locked, as before, on an instance without Premium", async () => {
		openSettingsAt("/settings/organization/refresh-ordering", {
			...aHealthyInstance(),
			license: WITHOUT_PREMIUM,
		});

		expect(await theSwitchFor("FeatureOrdering")).toBeDisabled();
	});

	it("shows a spinner in the behaviour section while the settings are on their way, then the ordering row", async () => {
		const slowFeatures = aDeferred<IOptionalFeature[]>();
		openSettingsAt("/settings/organization/refresh-ordering", {
			...aHealthyInstance(),
			features: slowFeatures.promise,
		});

		await screen.findByTestId("refresh-interval-Team");
		expect(screen.getByText("Behaviour Settings")).toBeVisible();
		expect(
			await screen.findByRole("progressbar"),
			"The behaviour section says it is loading rather than showing nothing",
		).toBeInTheDocument();
		expect(screen.queryByTestId("FeatureOrdering-toggle")).toBeNull();

		await act(async () =>
			slowFeatures.resolve([theUsageDataSetting, theOrderingSetting]),
		);

		expect(await theSwitchFor("FeatureOrdering")).toBeEnabled();
		expect(screen.queryByRole("progressbar")).toBeNull();
	});

	it("says the behaviour settings could not be loaded when that call fails, never an empty tab or a locked switch", async () => {
		openSettingsAt("/settings/organization/refresh-ordering", {
			...aHealthyInstance(),
			features: "fails",
		});

		expect(await screen.findByText(COULD_NOT_LOAD_BEHAVIOUR)).toBeVisible();
		expect(screen.getByText("Behaviour Settings")).toBeVisible();
		expect(screen.queryByTestId("FeatureOrdering-toggle")).toBeNull();
		expect(screen.queryByTestId("optional-features-table")).toBeNull();
		expect(screen.queryByRole("progressbar")).toBeNull();
	});

	it("says the behaviour settings could not be loaded, not Premium required, when the licence call fails", async () => {
		openSettingsAt("/settings/organization/refresh-ordering", {
			...aHealthyInstance(),
			license: "fails",
		});

		expect(await screen.findByText(COULD_NOT_LOAD_BEHAVIOUR)).toBeVisible();
		expect(
			screen.queryByTestId("FeatureOrdering-toggle"),
			"An unread licence must not show the ordering switch locked as if the instance had no Premium",
		).toBeNull();
		expect(screen.queryByText(PREMIUM_REQUIRED)).toBeNull();
	});
});

// ---------------------------------------------------------------------------------------------
// System: Usage Data
// ---------------------------------------------------------------------------------------------

describe("Usage Data tab", () => {
	it("holds the usage-data switch and the link to what is sent, usable without Premium, and no ordering row", async () => {
		openSettingsAt("/settings/system/usage-data", {
			...aHealthyInstance(),
			license: WITHOUT_PREMIUM,
		});

		expect(await screen.findByText("Never send usage data")).toBeVisible();
		expect(await theSwitchFor("UsageData")).toBeEnabled();
		expect(theSelectedTab()).toEqual(["Usage Data"]);
		expect(
			screen
				.getAllByRole("link")
				.map((link) => link.getAttribute("href"))
				.filter((href) => href === USAGE_DATA_DOCS),
			"The tab links to the page that says what Lighthouse sends",
		).toHaveLength(1);

		expect(screen.queryByTestId("feature-row-FeatureOrdering")).toBeNull();
		expect(
			screen.queryByText(/footer/i),
			"The tab shows the row and the link only, with no sentence about the footer choice",
		).toBeNull();
	});

	it("keeps the new value when the person leaves the tab and comes back", async () => {
		const { optionalFeatureService } = openSettingsAt(
			"/settings/system/usage-data",
			aHealthyInstance(),
		);
		const theSwitch = await theSwitchFor("UsageData");
		expect(theSwitch).not.toBeChecked();

		fireEvent.click(theSwitch);
		await waitFor(() =>
			expect(optionalFeatureService.updateFeature).toHaveBeenCalledWith(
				expect.objectContaining({ key: "UsageData", enabled: true }),
			),
		);

		fireEvent.click(theTab("Database"));
		await screen.findByText("database panel content");
		fireEvent.click(theTab("Usage Data"));

		const theSwitchAgain = await theSwitchFor("UsageData");
		expect(
			theSwitchAgain,
			"The first switch shown on return already reads the stored value, not the one from before the change",
		).toBeChecked();
		expect(whereWeAre()).toBe("/settings/system/usage-data");
	});

	it("shows a spinner while the settings are on their way, then the switch", async () => {
		const slowFeatures = aDeferred<IOptionalFeature[]>();
		openSettingsAt("/settings/system/usage-data", {
			...aHealthyInstance(),
			features: slowFeatures.promise,
		});

		// The tab row only appears once access has loaded, so a spinner seen after it is the section's own.
		await waitFor(() => expect(theSelectedTab()).toEqual(["Usage Data"]));
		expect(screen.getByRole("progressbar")).toBeInTheDocument();
		expect(screen.queryByTestId("UsageData-toggle")).toBeNull();

		await act(async () =>
			slowFeatures.resolve([theUsageDataSetting, theOrderingSetting]),
		);

		expect(await theSwitchFor("UsageData")).toBeEnabled();
		expect(screen.queryByRole("progressbar")).toBeNull();
	});

	it("says the behaviour settings could not be loaded when that call fails, never an empty tab", async () => {
		openSettingsAt("/settings/system/usage-data", {
			...aHealthyInstance(),
			features: "fails",
		});

		expect(await screen.findByText(COULD_NOT_LOAD_BEHAVIOUR)).toBeVisible();
		expect(screen.queryByTestId("UsageData-toggle")).toBeNull();
		expect(screen.queryByRole("progressbar")).toBeNull();
	});
});
