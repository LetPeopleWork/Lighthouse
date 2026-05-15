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
import { TerminologyProvider } from "../../../services/TerminologyContext";
import {
	createMockApiServiceContext,
	createMockSystemInfoService,
	createMockTerminologyService,
} from "../../../tests/MockApiServiceProvider";
import ModifyConnectionSettings from "./ModifyConnectionSettings";

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

const createEmptyTeamService = (): Partial<ITeamService> => ({
	getTeams: vi.fn().mockResolvedValue([]),
});

const createEmptyPortfolioService = (): Partial<IPortfolioService> => ({
	getPortfolios: vi.fn().mockResolvedValue([]),
});

const createMockOAuthService = (): IOAuthService => ({
	initiateConnect: vi.fn(),
	disconnect: vi.fn(),
});

vi.mock("../../../pages/Settings/Connections/AdditionalFieldsEditor", () => ({
	default: () => <div data-testid="additional-fields-editor" />,
}));

const mockSystem = new WorkTrackingSystemConnection({
	id: 0,
	name: "Azure DevOps",
	workTrackingSystem: "AzureDevOps",
	options: [],
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

const mockJiraSystem = new WorkTrackingSystemConnection({
	id: 0,
	name: "Jira",
	workTrackingSystem: "Jira",
	options: [],
	availableAuthenticationMethods: [
		{
			key: "jira.cloud",
			displayName: "Jira Cloud",
			options: [
				{
					key: "ApiToken",
					displayName: "API Token",
					isSecret: true,
					isOptional: false,
				},
			],
		},
	],
	additionalFieldDefinitions: [],
	authenticationMethodKey: "jira.cloud",
});

// System with one auth method that has no options — selectedAuthMethod is non-null but
// allOptions stays empty, so inputsValid=true and handleValidate can fire.
const mockSystemNoAuth = new WorkTrackingSystemConnection({
	id: 0,
	name: "Linear",
	workTrackingSystem: "Linear",
	options: [],
	availableAuthenticationMethods: [
		{
			key: "linear.noop",
			displayName: "No Authentication",
			options: [],
		},
	],
	additionalFieldDefinitions: [],
	authenticationMethodKey: "linear.noop",
});

const mockJiraSystemWithOAuth = new WorkTrackingSystemConnection({
	id: 0,
	name: "Jira",
	workTrackingSystem: "Jira",
	options: [],
	availableAuthenticationMethods: [
		{
			key: "jira.cloud",
			displayName: "Jira Cloud",
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
			displayName: "OAuth 2.0",
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
			isPremium: true,
		},
	],
	additionalFieldDefinitions: [],
	authenticationMethodKey: "jira.cloud",
});

const mockExistingOAuthConnection = new WorkTrackingSystemConnection({
	id: 99,
	name: "My Jira OAuth Connection",
	workTrackingSystem: "Jira",
	options: [
		{
			key: "Jira Url",
			value: "https://example.atlassian.net",
			isSecret: false,
			isOptional: false,
		},
		{
			key: "oauth.clientId",
			value: "persisted-client-id",
			isSecret: false,
			isOptional: false,
		},
		{
			key: "oauth.clientSecret",
			value: "",
			isSecret: true,
			isOptional: false,
		},
	],
	availableAuthenticationMethods:
		mockJiraSystemWithOAuth.availableAuthenticationMethods,
	additionalFieldDefinitions: [],
	authenticationMethodKey: "jira.oauth",
});

const mockExistingConnection = new WorkTrackingSystemConnection({
	id: 7,
	name: "My Existing Connection",
	workTrackingSystem: "AzureDevOps",
	options: [
		{ key: "AccessToken", value: "", isSecret: true, isOptional: false },
	],
	availableAuthenticationMethods: mockSystem.availableAuthenticationMethods,
	additionalFieldDefinitions: [],
	authenticationMethodKey: "ado.pat",
});

const createQueryClient = () =>
	new QueryClient({
		defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
	});

const defaultProps = {
	title: "Test Connection Title",
	getSupportedSystems: vi.fn(),
	getConnectionSettings: vi.fn(),
	saveConnectionSettings: vi.fn(),
	validateConnectionSettings: vi.fn(),
	disableSave: false,
};

const renderComponent = (
	propsOverride: Partial<typeof defaultProps> = {},
	contextOverrides: {
		canUsePremiumFeatures?: boolean;
		oauthService?: IOAuthService;
		baseUrl?: string | null;
	} = {},
) => {
	const props = { ...defaultProps, ...propsOverride };

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
		baseUrl: contextOverrides.baseUrl ?? undefined,
	});

	const mockApiServiceContext = createMockApiServiceContext({
		terminologyService: mockTerminologyService,
		licensingService: createLicensingService(
			contextOverrides.canUsePremiumFeatures ?? true,
		),
		teamService: createEmptyTeamService() as ITeamService,
		portfolioService: createEmptyPortfolioService() as IPortfolioService,
		oauthService: contextOverrides.oauthService ?? createMockOAuthService(),
		systemInfoService: mockSystemInfoService,
	});

	return render(
		<QueryClientProvider client={createQueryClient()}>
			<MemoryRouter>
				<ApiServiceContext.Provider value={mockApiServiceContext}>
					<TerminologyProvider>
						<ModifyConnectionSettings {...props} />
					</TerminologyProvider>
				</ApiServiceContext.Provider>
			</MemoryRouter>
		</QueryClientProvider>,
	);
};

describe("ModifyConnectionSettings", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		defaultProps.getSupportedSystems.mockResolvedValue([mockSystem]);
		defaultProps.getConnectionSettings.mockResolvedValue(null);
		defaultProps.saveConnectionSettings.mockResolvedValue(undefined);
		defaultProps.validateConnectionSettings.mockResolvedValue(true);
	});

	describe("Loading state", () => {
		it("shows loading indicator initially", () => {
			renderComponent();
			expect(
				screen.getByTestId("loading-animation-progress-indicator"),
			).toBeInTheDocument();
		});
	});

	describe("Create mode", () => {
		it("renders the title", async () => {
			renderComponent();
			await waitFor(() => {
				expect(screen.getByText("Test Connection Title")).toBeInTheDocument();
			});
		});

		it("calls getSupportedSystems on mount", async () => {
			renderComponent();
			await waitFor(() => {
				expect(defaultProps.getSupportedSystems).toHaveBeenCalled();
			});
		});

		it("calls getConnectionSettings on mount", async () => {
			renderComponent();
			await waitFor(() => {
				expect(defaultProps.getConnectionSettings).toHaveBeenCalled();
			});
		});

		it("shows system type dropdown in create mode", async () => {
			renderComponent();
			await waitFor(() => {
				// MUI Select renders as a combobox role
				expect(screen.getByRole("combobox")).toBeInTheDocument();
				expect(
					screen.getAllByText("Select Work Tracking System").length,
				).toBeGreaterThan(0);
			});
		});

		it("pre-fills connection name from the first supported system", async () => {
			renderComponent();
			await waitFor(() => {
				const nameInput =
					screen.getByLabelText<HTMLInputElement>("Connection Name");
				expect(nameInput.value).toBe("Azure DevOps");
			});
		});

		it("renders AdditionalFieldsEditor", async () => {
			renderComponent();
			await waitFor(() => {
				expect(
					screen.getByTestId("additional-fields-editor"),
				).toBeInTheDocument();
			});
		});

		it("renders Save button without standalone Validate button", async () => {
			renderComponent();
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /Save/i }),
				).toBeInTheDocument();
			});
			expect(
				screen.queryByRole("button", { name: /^Validate$/i }),
			).not.toBeInTheDocument();
		});

		it("updates connection name when user types in the name field", async () => {
			const user = userEvent.setup();
			renderComponent();
			await waitFor(() => {
				expect(screen.getByLabelText("Connection Name")).toBeInTheDocument();
			});
			const nameInput = screen.getByLabelText("Connection Name");
			await user.clear(nameInput);
			await user.type(nameInput, "My Custom Name");
			expect((nameInput as HTMLInputElement).value).toBe("My Custom Name");
		});

		it("populates system dropdown with all supported systems", async () => {
			renderComponent({
				getSupportedSystems: vi
					.fn()
					.mockResolvedValue([mockSystem, mockJiraSystem]),
			});
			await waitFor(() => {
				// MUI Select renders as a combobox role
				expect(screen.getByRole("combobox")).toBeInTheDocument();
			});
		});
	});

	describe("Edit mode", () => {
		it("shows readonly system type field in edit mode", async () => {
			renderComponent({
				getConnectionSettings: vi
					.fn()
					.mockResolvedValue(mockExistingConnection),
			});
			await waitFor(() => {
				const systemField = screen.getByDisplayValue("AzureDevOps");
				expect(systemField).toHaveAttribute("readonly");
			});
		});

		it("pre-fills connection name from the existing connection", async () => {
			renderComponent({
				getConnectionSettings: vi
					.fn()
					.mockResolvedValue(mockExistingConnection),
			});
			await waitFor(() => {
				const nameInput =
					screen.getByLabelText<HTMLInputElement>("Connection Name");
				expect(nameInput.value).toBe("My Existing Connection");
			});
		});

		it("does not show system dropdown in edit mode", async () => {
			renderComponent({
				getConnectionSettings: vi
					.fn()
					.mockResolvedValue(mockExistingConnection),
			});
			await waitFor(() => {
				// Wait for loading to complete
				expect(screen.getByLabelText("Connection Name")).toBeInTheDocument();
			});
			expect(
				screen.queryByLabelText(/Select Work Tracking System/i),
			).not.toBeInTheDocument();
		});
	});

	describe("Validate on Save", () => {
		it("validates then saves when Save is clicked with valid inputs", async () => {
			const user = userEvent.setup();
			renderComponent({
				getSupportedSystems: vi.fn().mockResolvedValue([mockSystemNoAuth]),
			});
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /Save/i }),
				).not.toBeDisabled();
			});
			await user.click(screen.getByRole("button", { name: /Save/i }));
			await waitFor(() => {
				expect(defaultProps.validateConnectionSettings).toHaveBeenCalled();
			});
			await waitFor(() => {
				expect(defaultProps.saveConnectionSettings).toHaveBeenCalled();
			});
		});

		it("shows error and does not save when validation fails", async () => {
			const user = userEvent.setup();
			defaultProps.validateConnectionSettings.mockResolvedValue(false);
			renderComponent({
				getSupportedSystems: vi.fn().mockResolvedValue([mockSystemNoAuth]),
			});
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /Save/i }),
				).not.toBeDisabled();
			});
			await user.click(screen.getByRole("button", { name: /Save/i }));
			await waitFor(() => {
				expect(defaultProps.validateConnectionSettings).toHaveBeenCalled();
			});
			await waitFor(() => {
				expect(
					screen.getByText(
						"Could not connect to the Work Tracking System with the provided settings. Please review and try again.",
					),
				).toBeInTheDocument();
			});
			expect(defaultProps.saveConnectionSettings).not.toHaveBeenCalled();
		});

		it("shows API error details when validation throws ApiError", async () => {
			const user = userEvent.setup();
			defaultProps.validateConnectionSettings.mockRejectedValue(
				new ApiError(
					400,
					"Authentication failed for Azure DevOps.",
					"Check your Personal Access Token permissions and make sure it is still valid.",
				),
			);

			renderComponent({
				getSupportedSystems: vi.fn().mockResolvedValue([mockSystemNoAuth]),
			});

			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /Save/i }),
				).not.toBeDisabled();
			});

			await user.click(screen.getByRole("button", { name: /Save/i }));

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

			expect(defaultProps.saveConnectionSettings).not.toHaveBeenCalled();
		});
	});

	describe("disableSave prop", () => {
		it("keeps Save button disabled when disableSave is true", async () => {
			renderComponent({
				disableSave: true,
				getSupportedSystems: vi.fn().mockResolvedValue([mockSystemNoAuth]),
			});
			await waitFor(() => {
				expect(screen.getByRole("button", { name: /Save/i })).toBeDisabled();
			});
		});
	});

	describe("OAuth premium-gated authentication method", () => {
		it("renders OAuthAuthForm when an .oauth method is selected and user has premium", async () => {
			renderComponent(
				{
					getConnectionSettings: vi
						.fn()
						.mockResolvedValue(mockExistingOAuthConnection),
				},
				{ canUsePremiumFeatures: true },
			);

			await waitFor(() => {
				expect(screen.getByLabelText("Client ID")).toBeInTheDocument();
			});
			expect(screen.getByLabelText("Client Secret")).toBeInTheDocument();
			expect(screen.queryByText(/Upgrade to Premium/i)).not.toBeInTheDocument();
		});

		it("renders Upgrade affordance and not OAuthAuthForm when .oauth is selected and user lacks premium", async () => {
			renderComponent(
				{
					getConnectionSettings: vi
						.fn()
						.mockResolvedValue(mockExistingOAuthConnection),
				},
				{ canUsePremiumFeatures: false },
			);

			await waitFor(() => {
				expect(screen.getByText(/Upgrade to Premium/i)).toBeInTheDocument();
			});
			expect(screen.queryByLabelText("Client ID")).not.toBeInTheDocument();
			expect(screen.queryByLabelText("Client Secret")).not.toBeInTheDocument();
		});

		it("prefills schema-driven Client ID and Jira URL fields from the persisted OAuth connection", async () => {
			renderComponent(
				{
					getConnectionSettings: vi
						.fn()
						.mockResolvedValue(mockExistingOAuthConnection),
				},
				{ canUsePremiumFeatures: true },
			);

			const clientIdInput = await screen.findByLabelText<HTMLInputElement>(
				"Client ID",
			);
			expect(clientIdInput.value).toBe("persisted-client-id");

			const jiraUrlInput = await screen.findByLabelText<HTMLInputElement>(
				"Jira URL",
			);
			expect(jiraUrlInput.value).toBe("https://example.atlassian.net");
		});

		it("renders OAuth callback URL derived from server BaseUrl when systemInfo provides one", async () => {
			renderComponent(
				{
					getConnectionSettings: vi
						.fn()
						.mockResolvedValue(mockExistingOAuthConnection),
				},
				{
					canUsePremiumFeatures: true,
					baseUrl: "https://lighthouse.example.com",
				},
			);

			const callback =
				await screen.findByLabelText<HTMLInputElement>("Callback URL");
			expect(callback.value).toBe(
				"https://lighthouse.example.com/api/oauth/callback",
			);
			expect(
				screen.queryByText(/Your callback URL may be incorrect/i),
			).not.toBeInTheDocument();
		});

		it("renders the legacy auth form and not OAuthAuthForm when a non-oauth method is selected", async () => {
			renderComponent(
				{
					getSupportedSystems: vi
						.fn()
						.mockResolvedValue([mockJiraSystemWithOAuth]),
				},
				{ canUsePremiumFeatures: true },
			);

			await waitFor(() => {
				expect(screen.getByLabelText("API Token")).toBeInTheDocument();
			});
			expect(screen.queryByLabelText("Client ID")).not.toBeInTheDocument();
			expect(screen.queryByText(/Upgrade to Premium/i)).not.toBeInTheDocument();
		});
	});
});
