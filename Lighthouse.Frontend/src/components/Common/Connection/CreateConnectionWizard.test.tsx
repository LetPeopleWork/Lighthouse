import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ILicenseStatus } from "../../../models/ILicenseStatus";
import { WorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiError } from "../../../services/Api/ApiError";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { ILicensingService } from "../../../services/Api/LicensingService";
import type { IOAuthService } from "../../../services/Api/OAuthService";
import type { IPortfolioService } from "../../../services/Api/PortfolioService";
import type { ITeamService } from "../../../services/Api/TeamService";
import type { IWorkTrackingSystemService } from "../../../services/Api/WorkTrackingSystemService";
import { TerminologyProvider } from "../../../services/TerminologyContext";
import {
	createMockApiServiceContext,
	createMockOAuthService,
	createMockSystemInfoService,
	createMockTerminologyService,
} from "../../../tests/MockApiServiceProvider";
import CreateConnectionWizard from "./CreateConnectionWizard";

vi.mock("../../../pages/Settings/Connections/AdditionalFieldsEditor", () => ({
	default: () => <div data-testid="additional-fields-editor" />,
}));

const openOAuthPopupMock = vi.fn();

vi.mock("../../../hooks/useOAuthPopup", () => ({
	useOAuthPopup: () => ({ openOAuthPopup: openOAuthPopupMock }),
}));

const mockAdoSystem = new WorkTrackingSystemConnection({
	id: 0,
	name: "Azure DevOps",
	workTrackingSystem: "AzureDevOps",
	options: [
		{
			key: "Url",
			value: "",
			isSecret: false,
			isOptional: false,
		},
		{
			key: "AccessToken",
			value: "",
			isSecret: true,
			isOptional: false,
		},
		{
			key: "RequestTimeoutInSeconds",
			value: "100",
			isSecret: false,
			isOptional: true,
		},
	],
	availableAuthenticationMethods: [
		{
			key: "ado.pat",
			displayName: "Personal Access Token",
			options: [
				{
					key: "Url",
					displayName: "Organization URL",
					isSecret: false,
					isOptional: false,
				},
				{
					key: "AccessToken",
					displayName: "Access Token",
					isSecret: true,
					isOptional: false,
				},
			],
		},
	],
	additionalFieldDefinitions: [],
	authenticationMethodKey: "ado.pat",
});

const mockJiraSystem = new WorkTrackingSystemConnection({
	id: 0,
	name: "Jira",
	workTrackingSystem: "Jira",
	options: [
		{
			key: "Url",
			value: "",
			isSecret: false,
			isOptional: false,
		},
		{
			key: "Username",
			value: "",
			isSecret: false,
			isOptional: true,
		},
		{
			key: "ApiToken",
			value: "",
			isSecret: true,
			isOptional: false,
		},
		{
			key: "RequestTimeoutInSeconds",
			value: "100",
			isSecret: false,
			isOptional: true,
		},
	],
	availableAuthenticationMethods: [
		{
			key: "jira.cloud",
			displayName: "Jira Cloud",
			options: [
				{
					key: "Url",
					displayName: "Jira URL",
					isSecret: false,
					isOptional: false,
				},
				{
					key: "Username",
					displayName: "Username",
					isSecret: false,
					isOptional: false,
				},
				{
					key: "ApiToken",
					displayName: "API Token",
					isSecret: true,
					isOptional: false,
				},
			],
		},
		{
			key: "jira.datacenter",
			displayName: "Jira Data Center",
			options: [
				{
					key: "Url",
					displayName: "Jira URL",
					isSecret: false,
					isOptional: false,
				},
				{
					key: "PersonalAccessToken",
					displayName: "Personal Access Token",
					isSecret: true,
					isOptional: false,
				},
			],
		},
	],
	additionalFieldDefinitions: [],
	authenticationMethodKey: "jira.cloud",
});

const mockJiraSystemWithOAuth = new WorkTrackingSystemConnection({
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
					key: "Jira Url",
					displayName: "Jira URL",
					isSecret: false,
					isOptional: false,
				},
				{
					key: "oauth.clientId",
					displayName: "Client ID",
					isSecret: false,
					isOptional: false,
				},
				{
					key: "oauth.clientSecret",
					displayName: "Client Secret",
					isSecret: true,
					isOptional: false,
				},
			],
		},
	],
	additionalFieldDefinitions: [],
	authenticationMethodKey: "jira.cloud",
});

const createLicensingService = (
	canUsePremiumFeatures: boolean,
): ILicensingService => {
	const status: ILicenseStatus = {
		hasLicense: canUsePremiumFeatures,
		isValid: canUsePremiumFeatures,
		canUsePremiumFeatures,
	};
	return {
		getLicenseStatus: vi.fn().mockResolvedValue(status),
		importLicense: vi.fn(),
		clearLicense: vi.fn(),
	};
};

const mockLinearSystem = new WorkTrackingSystemConnection({
	id: 0,
	name: "Linear",
	workTrackingSystem: "Linear",
	options: [],
	availableAuthenticationMethods: [
		{
			key: "linear.apikey",
			displayName: "API Key",
			options: [
				{
					key: "ApiKey",
					displayName: "API Key",
					isSecret: true,
					isOptional: false,
				},
			],
		},
	],
	additionalFieldDefinitions: [],
	authenticationMethodKey: "linear.apikey",
});

const createQueryClient = () =>
	new QueryClient({
		defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
	});

interface RenderOptions {
	supportedSystems?: WorkTrackingSystemConnection[];
	validateConnection?: (conn: unknown) => Promise<boolean>;
	saveConnection?: (
		conn: WorkTrackingSystemConnection,
	) => Promise<WorkTrackingSystemConnection>;
	onCancel?: () => void;
	canUsePremiumFeatures?: boolean;
	baseUrl?: string | null;
	oauthService?: IOAuthService;
	configuredConnections?: WorkTrackingSystemConnection[];
	initialUrl?: string;
	deleteWorkTrackingSystemConnection?: (id: number) => Promise<void>;
}

const renderWizard = (options: RenderOptions = {}) => {
	const {
		supportedSystems = [mockAdoSystem, mockJiraSystem, mockLinearSystem],
		validateConnection = vi.fn().mockResolvedValue(true),
		saveConnection = vi
			.fn()
			.mockImplementation(async (conn: WorkTrackingSystemConnection) => conn),
		onCancel = vi.fn(),
		canUsePremiumFeatures = true,
		baseUrl,
		oauthService = createMockOAuthService(),
		configuredConnections = [],
		initialUrl = "/settings/connections/new",
		deleteWorkTrackingSystemConnection = vi.fn().mockResolvedValue(undefined),
	} = options;

	const getSupportedSystems = vi.fn().mockResolvedValue(supportedSystems);

	const mockTerminologyService = createMockTerminologyService();
	vi.mocked(mockTerminologyService.getAllTerminology).mockResolvedValue([
		{
			id: 1,
			key: "workTrackingSystem",
			defaultValue: "Work Tracking System",
			description: "",
			value: "Work Tracking System",
		},
	]);

	const mockSystemInfoService = createMockSystemInfoService();
	vi.mocked(mockSystemInfoService.getSystemInfo).mockResolvedValue({
		os: "test",
		runtime: "test",
		architecture: "test",
		processId: 0,
		databaseProvider: "sqlite",
		databaseConnection: null,
		logPath: null,
		baseUrl: baseUrl ?? undefined,
	});

	const mockTeamService = {
		getTeams: vi.fn().mockResolvedValue([]),
	} as unknown as ITeamService;
	const mockPortfolioService = {
		getPortfolios: vi.fn().mockResolvedValue([]),
	} as unknown as IPortfolioService;

	const mockWorkTrackingSystemService = {
		getConfiguredWorkTrackingSystems: vi
			.fn()
			.mockResolvedValue(configuredConnections),
		deleteWorkTrackingSystemConnection,
	} as unknown as IWorkTrackingSystemService;

	const mockApiServiceContext = createMockApiServiceContext({
		terminologyService: mockTerminologyService,
		licensingService: createLicensingService(canUsePremiumFeatures),
		systemInfoService: mockSystemInfoService,
		teamService: mockTeamService,
		portfolioService: mockPortfolioService,
		oauthService,
		workTrackingSystemService: mockWorkTrackingSystemService,
	});

	render(
		<QueryClientProvider client={createQueryClient()}>
			<MemoryRouter initialEntries={[initialUrl]}>
				<ApiServiceContext.Provider value={mockApiServiceContext}>
					<TerminologyProvider>
						<CreateConnectionWizard
							getSupportedSystems={getSupportedSystems}
							validateConnection={validateConnection}
							saveConnection={saveConnection}
							onCancel={onCancel}
						/>
					</TerminologyProvider>
				</ApiServiceContext.Provider>
			</MemoryRouter>
		</QueryClientProvider>,
	);

	return {
		getSupportedSystems,
		validateConnection,
		saveConnection,
		onCancel,
		deleteWorkTrackingSystemConnection,
	};
};

describe("CreateConnectionWizard", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		openOAuthPopupMock.mockReset();
	});

	describe("Step 1: Choose Type", () => {
		it("renders a stepper with three steps", async () => {
			renderWizard();
			await waitFor(() => {
				expect(screen.getByText("Choose Type")).toBeInTheDocument();
				expect(screen.getByText("Configuration")).toBeInTheDocument();
				expect(screen.getByText("Name & Create")).toBeInTheDocument();
			});
			expect(screen.queryByText("Validate")).not.toBeInTheDocument();
		});

		it("starts on step 1 with system type selection", async () => {
			renderWizard();
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /Azure DevOps/i }),
				).toBeInTheDocument();
				expect(
					screen.getByRole("button", { name: /Jira/i }),
				).toBeInTheDocument();
				expect(
					screen.getByRole("button", { name: /Linear/i }),
				).toBeInTheDocument();
			});
		});

		it("does not show a Back button on step 1", async () => {
			renderWizard();
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /Azure DevOps/i }),
				).toBeInTheDocument();
			});
			expect(
				screen.queryByRole("button", { name: /Back/i }),
			).not.toBeInTheDocument();
		});

		it("advances to step 2 when a system type is selected", async () => {
			const user = userEvent.setup();
			renderWizard();
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /Azure DevOps/i }),
				).toBeInTheDocument();
			});
			await user.click(screen.getByRole("button", { name: /Azure DevOps/i }));
			await waitFor(() => {
				expect(screen.getByLabelText("Organization URL")).toBeInTheDocument();
				expect(screen.getByLabelText("Access Token")).toBeInTheDocument();
			});
		});
	});

	describe("Step 2: Configuration & Options", () => {
		const goToStep2 = async () => {
			const user = userEvent.setup();
			const mocks = renderWizard();
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /Azure DevOps/i }),
				).toBeInTheDocument();
			});
			await user.click(screen.getByRole("button", { name: /Azure DevOps/i }));
			await waitFor(() => {
				expect(screen.getByLabelText("Access Token")).toBeInTheDocument();
			});
			return { user, ...mocks };
		};

		it("renders auth option fields for the selected system", async () => {
			await goToStep2();
			expect(screen.getByLabelText("Access Token")).toBeInTheDocument();
		});

		it("shows a Back button that returns to step 1", async () => {
			const { user } = await goToStep2();
			const backButton = screen.getByRole("button", { name: /Back/i });
			expect(backButton).toBeInTheDocument();
			await user.click(backButton);
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /Azure DevOps/i }),
				).toBeInTheDocument();
			});
		});

		it("disables Next button when required auth fields are empty", async () => {
			await goToStep2();
			// Auth field should be empty initially — rendered but unfilled
			const nextButton = screen.getByRole("button", { name: /Next/i });
			expect(nextButton).toBeDisabled();
		});

		it("enables Next button when all required auth fields are filled", async () => {
			const { user } = await goToStep2();
			const urlInput = screen.getByLabelText("Organization URL");
			const tokenInput = screen.getByLabelText("Access Token");
			await user.type(urlInput, "https://dev.azure.com/myorg");
			await user.type(tokenInput, "my-secret-token");
			const nextButton = screen.getByRole("button", { name: /Next/i });
			expect(nextButton).toBeEnabled();
		});

		it("does not render optional non-auth options like RequestTimeout", async () => {
			await goToStep2();
			expect(
				screen.queryByLabelText(/Request.*Timeout/i),
			).not.toBeInTheDocument();
		});

		it("renders required non-auth options in configuration step", async () => {
			const systemWithRequiredNonAuth = new WorkTrackingSystemConnection({
				id: 0,
				name: "Custom System",
				workTrackingSystem: "AzureDevOps",
				options: [
					{ key: "AccessToken", value: "", isSecret: true, isOptional: false },
					{
						key: "RequiredSetting",
						value: "",
						isSecret: false,
						isOptional: false,
					},
					{
						key: "OptionalSetting",
						value: "default",
						isSecret: false,
						isOptional: true,
					},
				],
				availableAuthenticationMethods: [
					{
						key: "ado.pat",
						displayName: "Personal Access Token",
						options: [
							{
								key: "AccessToken",
								displayName: "Access Token",
								isSecret: true,
								isOptional: false,
							},
						],
					},
				],
				additionalFieldDefinitions: [],
				authenticationMethodKey: "ado.pat",
			});
			const user = userEvent.setup();
			renderWizard({ supportedSystems: [systemWithRequiredNonAuth] });
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /Custom System/i }),
				).toBeInTheDocument();
			});
			await user.click(screen.getByRole("button", { name: /Custom System/i }));
			await waitFor(() => {
				expect(screen.getByLabelText("Access Token")).toBeInTheDocument();
			});
			// Required non-auth option should be shown
			expect(screen.getByLabelText("RequiredSetting")).toBeInTheDocument();
			// Optional non-auth option should NOT be shown
			expect(
				screen.queryByLabelText("OptionalSetting"),
			).not.toBeInTheDocument();
		});

		it("validates connection on Next and advances to Name & Create on success", async () => {
			const user = userEvent.setup();
			const validateConnection = vi.fn().mockResolvedValue(true);
			renderWizard({ validateConnection });
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /Azure DevOps/i }),
				).toBeInTheDocument();
			});
			await user.click(screen.getByRole("button", { name: /Azure DevOps/i }));
			await waitFor(() => {
				expect(screen.getByLabelText("Organization URL")).toBeInTheDocument();
			});
			await user.type(
				screen.getByLabelText("Organization URL"),
				"https://dev.azure.com/org",
			);
			await user.type(screen.getByLabelText("Access Token"), "my-token");
			await user.click(screen.getByRole("button", { name: /Next/i }));
			await waitFor(() => {
				expect(screen.getByLabelText("Connection Name")).toBeInTheDocument();
			});
			expect(validateConnection).toHaveBeenCalledTimes(1);
		});

		it("shows validation error inline and stays on Auth step when validation fails", async () => {
			const user = userEvent.setup();
			const validateConnection = vi.fn().mockResolvedValue(false);
			renderWizard({ validateConnection });
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /Azure DevOps/i }),
				).toBeInTheDocument();
			});
			await user.click(screen.getByRole("button", { name: /Azure DevOps/i }));
			await waitFor(() => {
				expect(screen.getByLabelText("Organization URL")).toBeInTheDocument();
			});
			await user.type(
				screen.getByLabelText("Organization URL"),
				"https://dev.azure.com/org",
			);
			await user.type(screen.getByLabelText("Access Token"), "my-token");
			await user.click(screen.getByRole("button", { name: /Next/i }));
			await waitFor(() => {
				expect(screen.getByText(/could not validate/i)).toBeInTheDocument();
			});
			// Should still be on Auth step
			expect(screen.getByLabelText("Organization URL")).toBeInTheDocument();
			expect(screen.getByLabelText("Access Token")).toBeInTheDocument();
		});

		it("shows detailed backend validation message when validation throws ApiError", async () => {
			const user = userEvent.setup();
			const validateConnection = vi
				.fn()
				.mockRejectedValue(
					new ApiError(
						400,
						"Authentication failed for Azure DevOps.",
						"Check your Personal Access Token permissions and make sure it is still valid.",
					),
				);
			renderWizard({ validateConnection });
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /Azure DevOps/i }),
				).toBeInTheDocument();
			});
			await user.click(screen.getByRole("button", { name: /Azure DevOps/i }));
			await waitFor(() => {
				expect(screen.getByLabelText("Organization URL")).toBeInTheDocument();
			});
			await user.type(
				screen.getByLabelText("Organization URL"),
				"https://dev.azure.com/org",
			);
			await user.type(screen.getByLabelText("Access Token"), "my-token");
			await user.click(screen.getByRole("button", { name: /Next/i }));
			await waitFor(() => {
				expect(
					screen.getByText("Authentication failed for Azure DevOps."),
				).toBeInTheDocument();
				expect(
					screen.getByText(
						/Check your Personal Access Token permissions and make sure it is still valid\./i,
					),
				).toBeInTheDocument();
			});
		});

		it("renders auth method selector when system has multiple auth methods", async () => {
			const user = userEvent.setup();
			renderWizard();
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /Jira/i }),
				).toBeInTheDocument();
			});
			await user.click(screen.getByRole("button", { name: /Jira/i }));
			await waitFor(() => {
				// Should see auth method selector with Jira Cloud as default
				expect(screen.getByLabelText("Username")).toBeInTheDocument();
			});
		});
	});

	describe("Step 2: OAuth premium-gated authentication method", () => {
		const goToOAuthStep2 = async (options: RenderOptions = {}) => {
			const user = userEvent.setup();
			renderWizard({
				supportedSystems: [mockJiraSystemWithOAuth],
				...options,
			});
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /Jira/i }),
				).toBeInTheDocument();
			});
			await user.click(screen.getByRole("button", { name: /Jira/i }));
			const select = await screen.findByRole("combobox");
			await user.click(select);
			await user.click(await screen.findByRole("option", { name: /OAuth/i }));
		};

		it("renders read-only Callback URL field derived from server BaseUrl when Premium", async () => {
			await goToOAuthStep2({
				canUsePremiumFeatures: true,
				baseUrl: "https://lighthouse.example.com",
			});

			const callback =
				await screen.findByLabelText<HTMLInputElement>("Callback URL");
			expect(callback).toHaveAttribute("readonly");
			expect(callback.value).toBe(
				"https://lighthouse.example.com/api/oauth/callback",
			);
			expect(
				screen.queryByText(/Your callback URL may be incorrect/i),
			).not.toBeInTheDocument();
		});

		it("renders the BaseUrl warning when Premium but no BaseUrl configured", async () => {
			await goToOAuthStep2({
				canUsePremiumFeatures: true,
				baseUrl: null,
			});

			expect(
				await screen.findByText(/Your callback URL may be incorrect/i),
			).toBeInTheDocument();
			expect(screen.getByText(/Set Lighthouse:BaseUrl/i)).toBeInTheDocument();
		});

		it("advances to step 3 when the OAuth popup resolves with success", async () => {
			const user = userEvent.setup();
			const validateConnection = vi.fn().mockResolvedValue(true);
			const savedDraft = new WorkTrackingSystemConnection({
				id: 88,
				name: "Jira",
				workTrackingSystem: "Jira",
				options: [],
				availableAuthenticationMethods:
					mockJiraSystemWithOAuth.availableAuthenticationMethods,
				additionalFieldDefinitions: [],
				authenticationMethodKey: "jira.oauth",
			});
			const saveConnection = vi.fn().mockResolvedValue(savedDraft);
			const oauthService = createMockOAuthService();
			vi.mocked(oauthService.initiateConnect).mockResolvedValue({
				authorizationUrl: "https://auth.atlassian.com/authorize?state=xyz",
			});

			openOAuthPopupMock.mockResolvedValue({
				status: "success",
				connectionId: 88,
			});

			renderWizard({
				supportedSystems: [mockJiraSystemWithOAuth],
				canUsePremiumFeatures: true,
				baseUrl: "https://lighthouse.example.com",
				validateConnection,
				saveConnection,
				oauthService,
			});
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /Jira/i }),
				).toBeInTheDocument();
			});
			await user.click(screen.getByRole("button", { name: /Jira/i }));
			const select = await screen.findByRole("combobox");
			await user.click(select);
			await user.click(await screen.findByRole("option", { name: /OAuth/i }));

			await user.type(
				await screen.findByLabelText("Jira URL"),
				"https://example.atlassian.net",
			);
			await user.type(
				await screen.findByLabelText("Client ID"),
				"stub-client-id",
			);
			await user.type(
				await screen.findByLabelText("Client Secret"),
				"stub-client-secret",
			);

			await user.click(screen.getByRole("button", { name: /Next/i }));

			await waitFor(() => {
				expect(saveConnection).toHaveBeenCalledTimes(1);
			});
			expect(oauthService.initiateConnect).toHaveBeenCalledWith(
				"jira.oauth",
				88,
			);

			await waitFor(() => {
				expect(openOAuthPopupMock).toHaveBeenCalledWith(
					"https://auth.atlassian.com/authorize?state=xyz",
				);
			});

			expect(
				await screen.findByLabelText(/Connection Name/i),
			).toBeInTheDocument();
			expect(validateConnection).not.toHaveBeenCalled();
		});

		it("renders the popup-blocked alert on step 2 and re-enables Connect when the OAuth popup is blocked", async () => {
			const user = userEvent.setup();
			const savedDraft = new WorkTrackingSystemConnection({
				id: 88,
				name: "Jira",
				workTrackingSystem: "Jira",
				options: [],
				availableAuthenticationMethods:
					mockJiraSystemWithOAuth.availableAuthenticationMethods,
				additionalFieldDefinitions: [],
				authenticationMethodKey: "jira.oauth",
			});
			const saveConnection = vi.fn().mockResolvedValue(savedDraft);
			const oauthService = createMockOAuthService();
			vi.mocked(oauthService.initiateConnect).mockResolvedValue({
				authorizationUrl: "https://auth.atlassian.com/authorize?state=xyz",
			});
			const deleteWorkTrackingSystemConnection = vi
				.fn()
				.mockResolvedValue(undefined);

			openOAuthPopupMock.mockResolvedValue({ status: "popup_blocked" });

			renderWizard({
				supportedSystems: [mockJiraSystemWithOAuth],
				canUsePremiumFeatures: true,
				baseUrl: "https://lighthouse.example.com",
				saveConnection,
				oauthService,
				deleteWorkTrackingSystemConnection,
			});
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /Jira/i }),
				).toBeInTheDocument();
			});
			await user.click(screen.getByRole("button", { name: /Jira/i }));
			const select = await screen.findByRole("combobox");
			await user.click(select);
			await user.click(await screen.findByRole("option", { name: /OAuth/i }));

			await user.type(
				await screen.findByLabelText("Jira URL"),
				"https://example.atlassian.net",
			);
			await user.type(
				await screen.findByLabelText("Client ID"),
				"stub-client-id",
			);
			await user.type(
				await screen.findByLabelText("Client Secret"),
				"stub-client-secret",
			);

			await user.click(screen.getByRole("button", { name: /Next/i }));

			expect(await screen.findByRole("alert")).toHaveTextContent(
				/blocked the OAuth popup/i,
			);

			expect(
				screen.queryByLabelText(/Connection Name/i),
			).not.toBeInTheDocument();
			expect(screen.getByLabelText("Jira URL")).toBeInTheDocument();
			expect(screen.getByRole("button", { name: /Next/i })).toBeEnabled();

			await waitFor(() => {
				expect(deleteWorkTrackingSystemConnection).toHaveBeenCalledWith(88);
			});
		});

		it("deletes the draft connection when the OAuth popup resolves with cancelled", async () => {
			const user = userEvent.setup();
			const savedDraft = new WorkTrackingSystemConnection({
				id: 99,
				name: "Jira",
				workTrackingSystem: "Jira",
				options: [],
				availableAuthenticationMethods:
					mockJiraSystemWithOAuth.availableAuthenticationMethods,
				additionalFieldDefinitions: [],
				authenticationMethodKey: "jira.oauth",
			});
			const saveConnection = vi.fn().mockResolvedValue(savedDraft);
			const oauthService = createMockOAuthService();
			vi.mocked(oauthService.initiateConnect).mockResolvedValue({
				authorizationUrl: "https://auth.atlassian.com/authorize?state=xyz",
			});
			const deleteWorkTrackingSystemConnection = vi
				.fn()
				.mockResolvedValue(undefined);

			openOAuthPopupMock.mockResolvedValue({ status: "cancelled" });

			renderWizard({
				supportedSystems: [mockJiraSystemWithOAuth],
				canUsePremiumFeatures: true,
				baseUrl: "https://lighthouse.example.com",
				saveConnection,
				oauthService,
				deleteWorkTrackingSystemConnection,
			});
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /Jira/i }),
				).toBeInTheDocument();
			});
			await user.click(screen.getByRole("button", { name: /Jira/i }));
			const select = await screen.findByRole("combobox");
			await user.click(select);
			await user.click(await screen.findByRole("option", { name: /OAuth/i }));

			await user.type(
				await screen.findByLabelText("Jira URL"),
				"https://example.atlassian.net",
			);
			await user.type(
				await screen.findByLabelText("Client ID"),
				"stub-client-id",
			);
			await user.type(
				await screen.findByLabelText("Client Secret"),
				"stub-client-secret",
			);

			await user.click(screen.getByRole("button", { name: /Next/i }));

			expect(await screen.findByRole("alert")).toHaveTextContent(
				/OAuth was cancelled/i,
			);

			await waitFor(() => {
				expect(deleteWorkTrackingSystemConnection).toHaveBeenCalledWith(99);
			});
		});

		it("deletes the draft connection when the OAuth popup resolves with error", async () => {
			const user = userEvent.setup();
			const savedDraft = new WorkTrackingSystemConnection({
				id: 77,
				name: "Jira",
				workTrackingSystem: "Jira",
				options: [],
				availableAuthenticationMethods:
					mockJiraSystemWithOAuth.availableAuthenticationMethods,
				additionalFieldDefinitions: [],
				authenticationMethodKey: "jira.oauth",
			});
			const saveConnection = vi.fn().mockResolvedValue(savedDraft);
			const oauthService = createMockOAuthService();
			vi.mocked(oauthService.initiateConnect).mockResolvedValue({
				authorizationUrl: "https://auth.atlassian.com/authorize?state=xyz",
			});
			const deleteWorkTrackingSystemConnection = vi
				.fn()
				.mockResolvedValue(undefined);

			openOAuthPopupMock.mockResolvedValue({
				status: "error",
				reason: "access_denied",
			});

			renderWizard({
				supportedSystems: [mockJiraSystemWithOAuth],
				canUsePremiumFeatures: true,
				baseUrl: "https://lighthouse.example.com",
				saveConnection,
				oauthService,
				deleteWorkTrackingSystemConnection,
			});
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /Jira/i }),
				).toBeInTheDocument();
			});
			await user.click(screen.getByRole("button", { name: /Jira/i }));
			const select = await screen.findByRole("combobox");
			await user.click(select);
			await user.click(await screen.findByRole("option", { name: /OAuth/i }));

			await user.type(
				await screen.findByLabelText("Jira URL"),
				"https://example.atlassian.net",
			);
			await user.type(
				await screen.findByLabelText("Client ID"),
				"stub-client-id",
			);
			await user.type(
				await screen.findByLabelText("Client Secret"),
				"stub-client-secret",
			);

			await user.click(screen.getByRole("button", { name: /Next/i }));

			await waitFor(() => {
				expect(deleteWorkTrackingSystemConnection).toHaveBeenCalledWith(77);
			});
		});

		it("creates a fresh draft on the next Connect click after a non-success popup result", async () => {
			const user = userEvent.setup();
			const firstDraft = new WorkTrackingSystemConnection({
				id: 101,
				name: "Jira",
				workTrackingSystem: "Jira",
				options: [],
				availableAuthenticationMethods:
					mockJiraSystemWithOAuth.availableAuthenticationMethods,
				additionalFieldDefinitions: [],
				authenticationMethodKey: "jira.oauth",
			});
			const secondDraft = new WorkTrackingSystemConnection({
				id: 202,
				name: "Jira",
				workTrackingSystem: "Jira",
				options: [],
				availableAuthenticationMethods:
					mockJiraSystemWithOAuth.availableAuthenticationMethods,
				additionalFieldDefinitions: [],
				authenticationMethodKey: "jira.oauth",
			});
			const saveConnection = vi
				.fn()
				.mockResolvedValueOnce(firstDraft)
				.mockResolvedValueOnce(secondDraft);
			const oauthService = createMockOAuthService();
			vi.mocked(oauthService.initiateConnect).mockResolvedValue({
				authorizationUrl: "https://auth.atlassian.com/authorize?state=xyz",
			});
			const deleteWorkTrackingSystemConnection = vi
				.fn()
				.mockResolvedValue(undefined);

			openOAuthPopupMock.mockResolvedValueOnce({ status: "cancelled" });
			openOAuthPopupMock.mockResolvedValueOnce({
				status: "success",
				connectionId: 202,
			});

			renderWizard({
				supportedSystems: [mockJiraSystemWithOAuth],
				canUsePremiumFeatures: true,
				baseUrl: "https://lighthouse.example.com",
				saveConnection,
				oauthService,
				deleteWorkTrackingSystemConnection,
			});
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /Jira/i }),
				).toBeInTheDocument();
			});
			await user.click(screen.getByRole("button", { name: /Jira/i }));
			const select = await screen.findByRole("combobox");
			await user.click(select);
			await user.click(await screen.findByRole("option", { name: /OAuth/i }));

			await user.type(
				await screen.findByLabelText("Jira URL"),
				"https://example.atlassian.net",
			);
			await user.type(
				await screen.findByLabelText("Client ID"),
				"stub-client-id",
			);
			await user.type(
				await screen.findByLabelText("Client Secret"),
				"stub-client-secret",
			);

			await user.click(screen.getByRole("button", { name: /Next/i }));

			await waitFor(() => {
				expect(deleteWorkTrackingSystemConnection).toHaveBeenCalledWith(101);
			});

			await user.click(screen.getByRole("button", { name: /Next/i }));

			await waitFor(() => {
				expect(saveConnection).toHaveBeenCalledTimes(2);
			});
			expect(oauthService.initiateConnect).toHaveBeenLastCalledWith(
				"jira.oauth",
				202,
			);
		});

		it("renders Upgrade affordance and hides Client ID / Secret when not Premium", async () => {
			await goToOAuthStep2({ canUsePremiumFeatures: false });

			expect(
				await screen.findByText(/Upgrade to Premium/i),
			).toBeInTheDocument();
			expect(screen.queryByLabelText("Client ID")).not.toBeInTheDocument();
			expect(screen.queryByLabelText("Client Secret")).not.toBeInTheDocument();
			expect(screen.queryByLabelText("Callback URL")).not.toBeInTheDocument();
		});
	});

	describe("Step 3: Name & Create", () => {
		const goToStep3 = async () => {
			const user = userEvent.setup();
			const validateConnection = vi.fn().mockResolvedValue(true);
			const saveConnection = vi.fn().mockResolvedValue(undefined);
			renderWizard({ validateConnection, saveConnection });
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /Azure DevOps/i }),
				).toBeInTheDocument();
			});
			// Step 1
			await user.click(screen.getByRole("button", { name: /Azure DevOps/i }));
			await waitFor(() => {
				expect(screen.getByLabelText("Organization URL")).toBeInTheDocument();
			});
			// Step 2 — fill auth and click Next (validates inline)
			await user.type(
				screen.getByLabelText("Organization URL"),
				"https://dev.azure.com/org",
			);
			await user.type(screen.getByLabelText("Access Token"), "my-token");
			await user.click(screen.getByRole("button", { name: /Next/i }));
			// Step 3 — Name & Create
			await waitFor(() => {
				expect(screen.getByLabelText("Connection Name")).toBeInTheDocument();
			});
			return { user, validateConnection, saveConnection };
		};

		it("renders name input with default name from selected system", async () => {
			await goToStep3();
			const nameInput =
				screen.getByLabelText<HTMLInputElement>("Connection Name");
			expect(nameInput.value).toBe("Azure DevOps");
		});

		it("disables Create button when name is empty", async () => {
			const { user } = await goToStep3();
			const nameInput = screen.getByLabelText("Connection Name");
			await user.clear(nameInput);
			const createButton = screen.getByRole("button", { name: /Create/i });
			expect(createButton).toBeDisabled();
		});

		it("enables Create button when name is non-empty", async () => {
			await goToStep3();
			const createButton = screen.getByRole("button", { name: /Create/i });
			expect(createButton).toBeEnabled();
		});

		it("calls saveConnection with assembled DTO on Create click", async () => {
			const { user, saveConnection } = await goToStep3();
			await user.click(screen.getByRole("button", { name: /Create/i }));
			await waitFor(() => {
				expect(saveConnection).toHaveBeenCalledTimes(1);
			});
			const savedConnection = saveConnection.mock.calls[0][0];
			expect(savedConnection.workTrackingSystem).toBe("AzureDevOps");
			expect(savedConnection.name).toBe("Azure DevOps");
			expect(savedConnection.authenticationMethodKey).toBe("ado.pat");
		});

		it("includes non-auth options with defaults in create DTO", async () => {
			const { user, saveConnection } = await goToStep3();
			await user.click(screen.getByRole("button", { name: /Create/i }));
			await waitFor(() => {
				expect(saveConnection).toHaveBeenCalledTimes(1);
			});
			const savedConnection = saveConnection.mock.calls[0][0];
			const timeoutOption = savedConnection.options.find(
				(o: { key: string }) => o.key === "RequestTimeoutInSeconds",
			);
			expect(timeoutOption).toBeDefined();
			expect(timeoutOption.value).toBe("100");
		});

		it("does not include additional fields or write-back mappings in create DTO", async () => {
			const { user, saveConnection } = await goToStep3();
			await user.click(screen.getByRole("button", { name: /Create/i }));
			await waitFor(() => {
				expect(saveConnection).toHaveBeenCalledTimes(1);
			});
			const savedConnection = saveConnection.mock.calls[0][0];
			expect(savedConnection.additionalFieldDefinitions).toEqual([]);
			expect(savedConnection.writeBackMappingDefinitions).toEqual([]);
		});

		it("shows Back button that returns to Configuration step", async () => {
			const { user } = await goToStep3();
			await user.click(screen.getByRole("button", { name: /Back/i }));
			await waitFor(() => {
				expect(screen.getByLabelText("Organization URL")).toBeInTheDocument();
			});
		});
	});

	describe("Cancel", () => {
		it("calls onCancel when Cancel button is clicked", async () => {
			const onCancel = vi.fn();
			renderWizard({ onCancel });
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /Azure DevOps/i }),
				).toBeInTheDocument();
			});
			const cancelButton = screen.getByRole("button", { name: /Cancel/i });
			await userEvent.click(cancelButton);
			expect(onCancel).toHaveBeenCalledTimes(1);
		});
	});

	describe("No standalone Validate button", () => {
		it("does not render a standalone Validate button at any step", async () => {
			const user = userEvent.setup();
			const validateConnection = vi.fn().mockResolvedValue(true);
			renderWizard({ validateConnection });

			// Step 1
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /Azure DevOps/i }),
				).toBeInTheDocument();
			});
			expect(
				screen.queryByRole("button", { name: /^Validate$/i }),
			).not.toBeInTheDocument();

			// Step 2
			await user.click(screen.getByRole("button", { name: /Azure DevOps/i }));
			await waitFor(() => {
				expect(screen.getByLabelText("Access Token")).toBeInTheDocument();
			});
			expect(
				screen.queryByRole("button", { name: /^Validate$/i }),
			).not.toBeInTheDocument();
		});
	});
});
