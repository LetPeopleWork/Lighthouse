import {
	fireEvent,
	render,
	screen,
	waitFor,
	within,
} from "@testing-library/react";
import type React from "react";
import { beforeEach, describe, expect, test, vi } from "vitest";
import { AuthMode } from "../../../models/Auth/AuthModels";
import { ApiError } from "../../../services/Api/ApiError";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import {
	createMockApiKeyService,
	createMockApiServiceContext,
} from "../../../tests/MockApiServiceProvider";
import ApiKeysSettings from "./ApiKeysSettings";

const ROADMAP_PORTFOLIO_ID = 7;
const ROADMAP_PORTFOLIO_NAME = "Roadmap 2026";
const ALPHA_TEAM_ID = 11;
const ALPHA_TEAM_NAME = "Team Alpha";
const BETA_TEAM_ID = 12;
const BETA_TEAM_NAME = "Team Beta";

const buildContext = (
	apiKeyServiceOverrides: Partial<
		ReturnType<typeof createMockApiKeyService>
	> = {},
) => {
	const apiKeyService = {
		...createMockApiKeyService(),
		createApiKey: vi.fn().mockResolvedValue({
			id: 1,
			name: "key",
			description: "",
			createdAt: "2026-01-01T00:00:00Z",
			plainTextKey: "lh_secret",
		}),
		...apiKeyServiceOverrides,
	};
	const authService = {
		getRuntimeAuthStatus: vi.fn().mockResolvedValue({ mode: AuthMode.Enabled }),
		getSession: vi.fn(),
		getCurrentUserProfile: vi.fn(),
		getLoginUrl: vi.fn(),
		logout: vi.fn(),
	};
	const teamService = {
		getTeams: vi.fn().mockResolvedValue([
			{ id: ALPHA_TEAM_ID, name: ALPHA_TEAM_NAME },
			{ id: BETA_TEAM_ID, name: BETA_TEAM_NAME },
		]),
	};
	const portfolioService = {
		getPortfolios: vi
			.fn()
			.mockResolvedValue([
				{ id: ROADMAP_PORTFOLIO_ID, name: ROADMAP_PORTFOLIO_NAME },
			]),
	};

	const context = createMockApiServiceContext({
		apiKeyService,
		authService: authService as unknown as ReturnType<
			typeof createMockApiServiceContext
		>["authService"],
		teamService: teamService as unknown as ReturnType<
			typeof createMockApiServiceContext
		>["teamService"],
		portfolioService: portfolioService as unknown as ReturnType<
			typeof createMockApiServiceContext
		>["portfolioService"],
	});

	const Wrapper: React.FC<{ children: React.ReactNode }> = ({ children }) => (
		<ApiServiceContext.Provider value={context}>
			{children}
		</ApiServiceContext.Provider>
	);

	return { Wrapper, apiKeyService, teamService, portfolioService };
};

const openDialog = async (Wrapper: React.FC<{ children: React.ReactNode }>) => {
	render(<ApiKeysSettings />, { wrapper: Wrapper });
	await waitFor(() => {
		expect(screen.getByTestId("create-api-key-button")).toBeEnabled();
	});
	fireEvent.click(screen.getByTestId("create-api-key-button"));
	await waitFor(() => {
		expect(screen.getByTestId("api-key-name-input")).toBeInTheDocument();
	});
};

const typeName = (value: string) => {
	fireEvent.change(screen.getByTestId("api-key-name-input"), {
		target: { value },
	});
};

const typeDescription = (value: string) => {
	fireEvent.change(screen.getByTestId("api-key-description-input"), {
		target: { value },
	});
};

const expandScopeAccordion = async () => {
	fireEvent.click(screen.getByTestId("scope-accordion-summary"));
	await waitFor(() => {
		expect(screen.getByTestId("scope-row-list")).toBeInTheDocument();
	});
};

const addScopeRow = () => {
	fireEvent.click(screen.getByTestId("scope-row-list-add-button"));
};

const selectRowOption = (
	rowIndex: number,
	field: "role" | "scope-type" | "scope-id",
	value: string,
) => {
	fireEvent.change(screen.getByTestId(`scope-row-${rowIndex}-${field}`), {
		target: { value },
	});
};

const fillCompleteScopeRow = async (
	rowIndex: number,
	accessLabel: "Read access" | "Write access" | "System administrator",
	scopeType: "Team" | "Portfolio" | "System",
	scopeId: number,
) => {
	selectRowOption(rowIndex, "scope-type", scopeType);
	await waitFor(() => {
		expect(screen.getByTestId(`scope-row-${rowIndex}-role`)).not.toBeDisabled();
	});
	selectRowOption(rowIndex, "role", accessLabel);
	await waitFor(() => {
		expect(
			screen.getByTestId(`scope-row-${rowIndex}-scope-id`),
		).not.toBeDisabled();
	});
	selectRowOption(rowIndex, "scope-id", String(scopeId));
};

describe("F-FE-1 — CreateApiKeyDialog scope UI", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	test("FE-1.1 collapsed scope section submits without scope field", async () => {
		const { Wrapper, apiKeyService } = buildContext();
		await openDialog(Wrapper);

		typeName("ci-key");
		typeDescription("for ci");
		fireEvent.click(screen.getByTestId("create-api-key-submit-button"));

		await waitFor(() => {
			expect(apiKeyService.createApiKey).toHaveBeenCalledTimes(1);
		});
		expect(apiKeyService.createApiKey).toHaveBeenCalledWith({
			name: "ci-key",
			description: "for ci",
		});
	});

	test("FE-1.2 single read-access portfolio scope row sends one scope entry", async () => {
		const { Wrapper, apiKeyService } = buildContext();
		await openDialog(Wrapper);

		typeName("roadmap-ci");
		await expandScopeAccordion();
		addScopeRow();
		await fillCompleteScopeRow(
			0,
			"Read access",
			"Portfolio",
			ROADMAP_PORTFOLIO_ID,
		);

		fireEvent.click(screen.getByTestId("create-api-key-submit-button"));

		await waitFor(() => {
			expect(apiKeyService.createApiKey).toHaveBeenCalledTimes(1);
		});
		expect(apiKeyService.createApiKey).toHaveBeenCalledWith({
			name: "roadmap-ci",
			scope: [
				{
					role: "Viewer",
					scopeType: "Portfolio",
					scopeId: ROADMAP_PORTFOLIO_ID,
				},
			],
		});
	});

	test("FE-1.3 two scope rows send a scope array of length two with correct wire values", async () => {
		const { Wrapper, apiKeyService } = buildContext();
		await openDialog(Wrapper);

		typeName("multi-scope-key");
		await expandScopeAccordion();
		addScopeRow();
		await fillCompleteScopeRow(0, "Read access", "Team", ALPHA_TEAM_ID);
		addScopeRow();
		await fillCompleteScopeRow(
			1,
			"Write access",
			"Portfolio",
			ROADMAP_PORTFOLIO_ID,
		);

		fireEvent.click(screen.getByTestId("create-api-key-submit-button"));

		await waitFor(() => {
			expect(apiKeyService.createApiKey).toHaveBeenCalledTimes(1);
		});
		expect(apiKeyService.createApiKey).toHaveBeenCalledWith({
			name: "multi-scope-key",
			scope: [
				{ role: "Viewer", scopeType: "Team", scopeId: ALPHA_TEAM_ID },
				{
					role: "PortfolioAdmin",
					scopeType: "Portfolio",
					scopeId: ROADMAP_PORTFOLIO_ID,
				},
			],
		});
	});

	test("FE-1.4 server 403 keeps dialog open and surfaces error message", async () => {
		const errorMessage =
			"Cannot issue API key with scope exceeding caller's permissions";
		const createApiKey = vi
			.fn()
			.mockRejectedValue(new ApiError(403, errorMessage));
		const { Wrapper } = buildContext({ createApiKey });
		await openDialog(Wrapper);

		typeName("forbidden-key");
		await expandScopeAccordion();
		addScopeRow();
		await fillCompleteScopeRow(
			0,
			"Read access",
			"Portfolio",
			ROADMAP_PORTFOLIO_ID,
		);

		fireEvent.click(screen.getByTestId("create-api-key-submit-button"));

		await waitFor(() => {
			expect(createApiKey).toHaveBeenCalledTimes(1);
		});
		expect(screen.getByTestId("api-key-name-input")).toBeInTheDocument();
		const errorAlert = await screen.findByRole("alert");
		expect(within(errorAlert).getByText(errorMessage)).toBeInTheDocument();
		expect(
			screen.queryByTestId("created-api-key-value"),
		).not.toBeInTheDocument();
	});

	test("FE-1.5 incomplete scope row prevents submission", async () => {
		const { Wrapper, apiKeyService } = buildContext();
		await openDialog(Wrapper);

		typeName("incomplete-key");
		await expandScopeAccordion();
		addScopeRow();
		selectRowOption(0, "scope-type", "Portfolio");

		expect(screen.getByTestId("create-api-key-submit-button")).toBeDisabled();
		expect(apiKeyService.createApiKey).not.toHaveBeenCalled();
	});
});
