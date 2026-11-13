import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
	act,
	fireEvent,
	render,
	screen,
	waitFor,
	within,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type React from "react";
import { Suspense } from "react";
import {
	MemoryRouter,
	Route,
	Routes,
	useLocation,
	useNavigationType,
} from "react-router";
import { afterEach, describe, expect, it, vi } from "vitest";
import CreateConnectionWizard from "../../../components/Common/Connection/CreateConnectionWizard";
import OAuthPremiumNotice from "../../../components/Common/Connection/OAuthPremiumNotice";
import LicenseStatusIcon from "../../../components/Common/LicenseStatus/LicenseStatusIcon";
import type { ILicenseStatus } from "../../../models/ILicenseStatus";
import { WorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { ILicensingService } from "../../../services/Api/LicensingService";
import type { IPortfolioService } from "../../../services/Api/PortfolioService";
import type { ITeamService } from "../../../services/Api/TeamService";
import type { IWorkTrackingSystemService } from "../../../services/Api/WorkTrackingSystemService";
import { TerminologyProvider } from "../../../services/TerminologyContext";
import {
	createMockApiServiceContext,
	createMockRbacService,
	createMockSystemInfoService,
	createMockTerminologyService,
	createMockUpdateSubscriptionService,
} from "../../../tests/MockApiServiceProvider";
import { settingsRoutes } from "../settingsRoutes";

// Other tabs are not under test: they only need to not fetch or crash while the address is resolved.
vi.mock("../SystemInfo/SystemInfoSettings", () => ({
	default: () => <div>system info panel content</div>,
}));
vi.mock("../LogSettings/LogSettings", () => ({
	default: () => <div>logs panel content</div>,
}));
vi.mock("../SystemInfo/RefreshHistorySection", () => ({
	default: () => <div>refresh history panel content</div>,
}));
vi.mock("../DemoData/DemoDataSettings", () => ({
	default: () => <div>demo data panel content</div>,
}));

// ---------------------------------------------------------------------------------------------
// People and licenses
// ---------------------------------------------------------------------------------------------

interface Access {
	isSystemAdmin: boolean;
	isRbacEnabled: boolean;
}

const ADMIN: Access = { isSystemAdmin: true, isRbacEnabled: true };
const READER: Access = { isSystemAdmin: false, isRbacEnabled: true };

const aSummary = (access: Access) => ({
	...access,
	canCreateTeam: false,
	canCreatePortfolio: false,
});

const NORTHWIND_LICENSE: ILicenseStatus = {
	hasLicense: true,
	isValid: true,
	canUsePremiumFeatures: true,
	name: "Tomasz Nowak",
	organization: "Northwind Logistics",
	expiryDate: new Date("2027-06-15T12:00:00Z"),
};

const RENEWED_LICENSE: ILicenseStatus = {
	...NORTHWIND_LICENSE,
	expiryDate: new Date("2028-09-15T12:00:00Z"),
};

/** What a reader is given: the status and expiry, without the licensee's details. */
const NORTHWIND_LICENSE_AS_A_READER_SEES_IT: ILicenseStatus = {
	hasLicense: true,
	isValid: true,
	canUsePremiumFeatures: true,
	expiryDate: new Date("2027-06-15T12:00:00Z"),
};

const NO_LICENSE: ILicenseStatus = {
	hasLicense: false,
	isValid: false,
	canUsePremiumFeatures: false,
};

const aLicensingService = (
	status: ILicenseStatus | Promise<ILicenseStatus> | "fails",
): ILicensingService => ({
	getLicenseStatus: vi
		.fn()
		.mockImplementation(() =>
			status === "fails"
				? Promise.reject(new Error("the license call failed"))
				: Promise.resolve(status),
		),
	importLicense: vi.fn(),
	clearLicense: vi.fn(),
});

const aSlowAnswer = () => {
	let answer: (status: ILicenseStatus) => void = () => {};
	const promise = new Promise<ILicenseStatus>((resolveAnswer) => {
		answer = resolveAnswer;
	});
	return { promise, answer };
};

// ---------------------------------------------------------------------------------------------
// Steps
// ---------------------------------------------------------------------------------------------

const WhereWeAre = () => {
	const location = useLocation();
	const navigationType = useNavigationType();
	return (
		<>
			<output data-testid="where-we-are">{location.pathname}</output>
			<output data-testid="how-we-got-there">{navigationType}</output>
		</>
	);
};

const whereWeAre = () => screen.getByTestId("where-we-are").textContent;

interface TheApp {
	address: string;
	access: Access;
	licensingService: ILicensingService;
	/** A page elsewhere in the app, opened at /elsewhere, that holds a license link. */
	elsewhere?: React.ReactElement;
	/** The header's license icon, which shares the license status with the tab. */
	withTheHeaderIcon?: boolean;
	extraServices?: Parameters<typeof createMockApiServiceContext>[0];
}

const renderTheApp = ({
	address,
	access,
	licensingService,
	elsewhere,
	withTheHeaderIcon = false,
	extraServices = {},
}: TheApp) => {
	const rbacService = createMockRbacService();
	rbacService.getAuthorizationSummary = vi
		.fn()
		.mockResolvedValue(aSummary(access));
	const queryClient = new QueryClient({
		defaultOptions: { queries: { retry: false } },
	});
	return render(
		<QueryClientProvider client={queryClient}>
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({
					rbacService,
					licensingService,
					updateSubscriptionService: createMockUpdateSubscriptionService(),
					...extraServices,
				})}
			>
				<TerminologyProvider>
					<MemoryRouter initialEntries={[address]}>
						{withTheHeaderIcon && <LicenseStatusIcon />}
						<Suspense fallback={null}>
							<Routes>
								{settingsRoutes()}
								{elsewhere && <Route path="/elsewhere" element={elsewhere} />}
							</Routes>
						</Suspense>
						<WhereWeAre />
					</MemoryRouter>
				</TerminologyProvider>
			</ApiServiceContext.Provider>
		</QueryClientProvider>,
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

const theOpenPanel = async () => within(await screen.findByRole("tabpanel"));

/** The license tab is open: System area, License tab selected and last, address its own. */
const expectTheLicenseTabIsOpen = async () => {
	await theHeading("Settings › System");
	await waitFor(() => expect(whereWeAre()).toBe("/settings/system/license"));
	expect(
		theSelectedTab(),
		"The License tab must be the open one, because the link or address asked for the license",
	).toEqual(["License"]);
	expect(
		theTabNames()[theTabNames().length - 1],
		"License is the last tab of System, so it does not push the settings people use more often",
	).toBe("License");
};

const theLicenseButtons = (panel: ReturnType<typeof within>) =>
	panel.queryAllByRole("button", { name: /^(add|update|clear) license$/i });

afterEach(() => {
	vi.restoreAllMocks();
});

// ---------------------------------------------------------------------------------------------
// The License tab
// ---------------------------------------------------------------------------------------------

describe("the License tab in System", () => {
	it("shows a System Admin the license status with import and clear, in one License section", async () => {
		renderTheApp({
			address: "/settings/system/license",
			access: ADMIN,
			licensingService: aLicensingService(NORTHWIND_LICENSE),
		});

		await expectTheLicenseTabIsOpen();
		const panel = await theOpenPanel();
		expect(
			await panel.findByText(/Northwind Logistics/),
			"The organization the license was issued to",
		).toBeVisible();
		expect(panel.getByText(/Tomasz Nowak/)).toBeVisible();
		expect(
			panel.getByText(/2027/),
			"The expiry date of the license",
		).toBeVisible();
		expect(
			panel.getByRole("heading", { name: "License" }),
			"The content sits in one section titled License",
		).toBeVisible();
		expect(panel.getByRole("button", { name: "Update License" })).toBeEnabled();
		expect(panel.getByRole("button", { name: "Clear License" })).toBeEnabled();
	});

	it("shows a reader the status and expiry but neither import nor clear", async () => {
		renderTheApp({
			address: "/settings/system/license",
			access: READER,
			licensingService: aLicensingService(
				NORTHWIND_LICENSE_AS_A_READER_SEES_IT,
			),
		});

		await expectTheLicenseTabIsOpen();
		const panel = await theOpenPanel();
		expect(await panel.findByText("Licensed")).toBeVisible();
		expect(panel.getByText(/2027/)).toBeVisible();
		expect(
			theLicenseButtons(panel),
			"A reader may see the license but not change it, so no import or clear action is offered",
		).toEqual([]);
	});

	it("shows a loading look while the status is on its way and the status once it has arrived", async () => {
		const slow = aSlowAnswer();
		renderTheApp({
			address: "/settings/system/license",
			access: ADMIN,
			licensingService: aLicensingService(slow.promise),
		});

		await theHeading("Settings › System");
		const panel = await theOpenPanel();
		expect(await panel.findByText("Loading...")).toBeVisible();
		expect(panel.queryByText(/Could not load the license/)).toBeNull();
		expect(
			panel.queryByText("No License"),
			"A license that is still loading must not look like no license",
		).toBeNull();

		await act(async () => slow.answer(NORTHWIND_LICENSE));

		expect(await panel.findByText(/Northwind Logistics/)).toBeVisible();
		expect(panel.queryByText("Loading...")).toBeNull();
	});

	it("says the license could not be loaded when the call fails, and never shows an empty or loading area", async () => {
		renderTheApp({
			address: "/settings/system/license",
			access: ADMIN,
			licensingService: aLicensingService("fails"),
		});

		await theHeading("Settings › System");
		const panel = await theOpenPanel();
		expect(await panel.findByText("Could not load the license.")).toBeVisible();
		expect(panel.queryByText("Loading...")).toBeNull();
		expect(
			panel.queryByText("No License"),
			"A failed call is not the same as having no license",
		).toBeNull();
	});

	it("lets an admin clear the license after confirming, and then shows no license", async () => {
		const licensingService = aLicensingService(NORTHWIND_LICENSE);
		vi.mocked(licensingService.clearLicense).mockResolvedValue(undefined);
		vi.mocked(licensingService.getLicenseStatus).mockResolvedValueOnce(
			NORTHWIND_LICENSE,
		);
		vi.mocked(licensingService.getLicenseStatus).mockResolvedValue(NO_LICENSE);
		renderTheApp({
			address: "/settings/system/license",
			access: ADMIN,
			licensingService,
		});
		const panel = await theOpenPanel();

		fireEvent.click(
			await panel.findByRole("button", { name: "Clear License" }),
		);
		fireEvent.click(
			within(await screen.findByRole("dialog")).getByRole("button", {
				name: "Clear License",
			}),
		);

		expect(await panel.findByText("No License")).toBeVisible();
		expect(licensingService.clearLicense).toHaveBeenCalledTimes(1);
	});

	it("shows a new expiry in the License tab and in the header icon after an admin imports a license file", async () => {
		const licensingService = aLicensingService(NORTHWIND_LICENSE);
		vi.mocked(licensingService.importLicense).mockResolvedValue(
			RENEWED_LICENSE,
		);
		vi.mocked(licensingService.getLicenseStatus).mockResolvedValueOnce(
			NORTHWIND_LICENSE,
		);
		vi.mocked(licensingService.getLicenseStatus).mockResolvedValue(
			RENEWED_LICENSE,
		);
		const { container } = renderTheApp({
			address: "/settings/system/license",
			access: ADMIN,
			licensingService,
			withTheHeaderIcon: true,
		});
		const panel = await theOpenPanel();
		expect(await panel.findByText(/2027/)).toBeVisible();
		const renewedFile = new File(['{"license":"renewed"}'], "license.json", {
			type: "application/json",
		});

		await userEvent.upload(
			container.querySelector('input[type="file"]') as HTMLInputElement,
			renewedFile,
		);

		expect(
			await panel.findByText(/2028/),
			"The tab shows the renewed expiry",
		).toBeVisible();
		expect(panel.queryByText(/2027/)).toBeNull();
		fireEvent.click(screen.getByTestId("license-status-button"));
		const popover = within(await screen.findByTestId("license-status-popover"));
		expect(
			popover.getByText(/2028/),
			"The header icon must show the renewed expiry too, because the tab and the icon read one license status",
		).toBeVisible();
		expect(popover.queryByText(/2027/)).toBeNull();
	});
});

// ---------------------------------------------------------------------------------------------
// Ways in
// ---------------------------------------------------------------------------------------------

describe("the ways into the License tab", () => {
	it.each([
		{ who: "a System Admin", access: ADMIN },
		{ who: "a reader", access: READER },
	])(
		"the old license address opens System, License for $who, replacing the address",
		async ({ access }) => {
			renderTheApp({
				address: "/settings/license",
				access,
				licensingService: aLicensingService(
					NORTHWIND_LICENSE_AS_A_READER_SEES_IT,
				),
			});

			await expectTheLicenseTabIsOpen();
			expect(
				screen.getByTestId("how-we-got-there").textContent,
				"The address must be replaced, so Back does not return to the old address",
			).toBe("REPLACE");
			expect(await (await theOpenPanel()).findByText("Licensed")).toBeVisible();
		},
	);

	it("the license link in the OAuth premium notice of a connection form opens System, License", async () => {
		renderTheApp({
			address: "/elsewhere",
			access: ADMIN,
			licensingService: aLicensingService(NO_LICENSE),
			elsewhere: <OAuthPremiumNotice />,
		});

		const link = screen.getByRole("link", { name: "View license options" });
		expect(
			link.getAttribute("href"),
			"The link used to lead to an address no page answers; it must lead to the License tab",
		).toBe("/settings/system/license");
		fireEvent.click(link);

		await expectTheLicenseTabIsOpen();
		expect(await (await theOpenPanel()).findByText("No License")).toBeVisible();
	});

	it("the license link in the connection wizard opens System, License", async () => {
		const wizardOfASystemWithOAuth = new WorkTrackingSystemConnection({
			id: 0,
			name: "Jira",
			workTrackingSystem: "Jira",
			options: [],
			availableAuthenticationMethods: [
				{
					key: "jira.cloud",
					displayName: "Jira Cloud (API Token)",
					options: [
						{
							key: "ApiToken",
							displayName: "API Token",
							isSecret: true,
							isOptional: false,
						},
					],
				},
				{
					key: "jira.oauth",
					displayName: "Jira Cloud (OAuth)",
					isPremium: true,
					options: [
						{
							key: "oauth.clientId",
							displayName: "Client ID",
							isSecret: false,
							isOptional: false,
						},
					],
				},
			],
			additionalFieldDefinitions: [],
			authenticationMethodKey: "jira.cloud",
		});
		const terminologyService = createMockTerminologyService();
		vi.mocked(terminologyService.getAllTerminology).mockResolvedValue([
			{
				id: 1,
				key: "workTrackingSystem",
				defaultValue: "Work Tracking System",
				description: "",
				value: "Work Tracking System",
			},
		]);
		const systemInfoService = createMockSystemInfoService();
		vi.mocked(systemInfoService.getSystemInfo).mockResolvedValue({
			os: "test",
			runtime: "test",
			architecture: "test",
			processId: 0,
			databaseProvider: "sqlite",
			databaseConnection: null,
			logPath: null,
		});
		renderTheApp({
			address: "/elsewhere",
			access: ADMIN,
			licensingService: aLicensingService(NO_LICENSE),
			elsewhere: (
				<CreateConnectionWizard
					getSupportedSystems={vi
						.fn()
						.mockResolvedValue([wizardOfASystemWithOAuth])}
					validateConnection={vi.fn().mockResolvedValue({ isValid: true })}
					saveConnection={vi.fn()}
					onCancel={vi.fn()}
					onComplete={vi.fn()}
				/>
			),
			extraServices: {
				terminologyService,
				systemInfoService,
				teamService: {
					getTeams: vi.fn().mockResolvedValue([]),
				} as unknown as ITeamService,
				portfolioService: {
					getPortfolios: vi.fn().mockResolvedValue([]),
				} as unknown as IPortfolioService,
				workTrackingSystemService: {
					getConfiguredWorkTrackingSystems: vi.fn().mockResolvedValue([]),
				} as unknown as IWorkTrackingSystemService,
			},
		});
		const user = userEvent.setup();
		await user.click(await screen.findByRole("button", { name: /Jira/i }));
		await user.click(await screen.findByRole("combobox"));
		await user.click(await screen.findByRole("option", { name: /OAuth/i }));

		const link = await screen.findByRole("link", {
			name: "View license options",
		});
		expect(
			link.getAttribute("href"),
			"The wizard's license link must lead to the License tab",
		).toBe("/settings/system/license");
		await user.click(link);

		await expectTheLicenseTabIsOpen();
		expect(await (await theOpenPanel()).findByText("No License")).toBeVisible();
	});
});
