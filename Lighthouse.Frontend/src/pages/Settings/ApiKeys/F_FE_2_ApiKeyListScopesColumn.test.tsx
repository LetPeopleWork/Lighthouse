import { render, screen, waitFor, within } from "@testing-library/react";
import type React from "react";
import { beforeEach, describe, expect, test, vi } from "vitest";
import type { IApiKeyInfo } from "../../../models/ApiKey/ApiKey";
import { AuthMode } from "../../../models/Auth/AuthModels";
import type { UserAuthorizationSummary } from "../../../models/Authorization/RbacModels";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import {
	createMockApiKeyService,
	createMockApiServiceContext,
} from "../../../tests/MockApiServiceProvider";
import ApiKeysSettings from "./ApiKeysSettings";

const PLATFORM_TEAM_ID = 42;
const PLATFORM_TEAM_NAME = "Platform";
const PORTFOLIO_ID = 7;

const buildSummary = (
	overrides: Partial<UserAuthorizationSummary> = {},
): UserAuthorizationSummary => ({
	isRbacEnabled: false,
	isSystemAdmin: true,
	canCreateTeam: true,
	canCreatePortfolio: true,
	adminTeamIds: [],
	adminPortfolioIds: [],
	...overrides,
});

interface BuildContextOptions {
	keys: IApiKeyInfo[];
	authorizationSummary?:
		| Promise<UserAuthorizationSummary>
		| UserAuthorizationSummary;
	teamsResolution?:
		| Promise<{ id: number; name: string }[]>
		| { id: number; name: string }[];
	portfoliosResolution?:
		| Promise<{ id: number; name: string }[]>
		| { id: number; name: string }[];
	portfoliosRejects?: boolean;
}

const buildContext = (options: BuildContextOptions) => {
	const apiKeyService = {
		...createMockApiKeyService(),
		getApiKeys: vi.fn().mockResolvedValue(options.keys),
	};
	const authService = {
		getRuntimeAuthStatus: vi.fn().mockResolvedValue({ mode: AuthMode.Enabled }),
		getSession: vi.fn(),
		getCurrentUserProfile: vi.fn(),
		getLoginUrl: vi.fn(),
		logout: vi.fn(),
	};

	const teamService = {
		getTeams: vi.fn(),
	};
	if (options.teamsResolution instanceof Promise) {
		teamService.getTeams.mockReturnValue(options.teamsResolution);
	} else {
		teamService.getTeams.mockResolvedValue(options.teamsResolution ?? []);
	}

	const portfolioService = {
		getPortfolios: vi.fn(),
	};
	if (options.portfoliosRejects) {
		portfolioService.getPortfolios.mockRejectedValue(
			new Error("portfolio lookup failed"),
		);
	} else if (options.portfoliosResolution instanceof Promise) {
		portfolioService.getPortfolios.mockReturnValue(
			options.portfoliosResolution,
		);
	} else {
		portfolioService.getPortfolios.mockResolvedValue(
			options.portfoliosResolution ?? [],
		);
	}

	const getAuthorizationSummary = vi.fn();
	if (options.authorizationSummary instanceof Promise) {
		getAuthorizationSummary.mockReturnValue(options.authorizationSummary);
	} else {
		getAuthorizationSummary.mockResolvedValue(
			options.authorizationSummary ?? buildSummary(),
		);
	}

	const rbacService = {
		getStatus: vi.fn(),
		getUsers: vi.fn(),
		getAuthorizationSummary,
		bootstrapCurrentUserAsSystemAdmin: vi.fn(),
		grantSystemAdmin: vi.fn(),
		revokeSystemAdmin: vi.fn(),
		getTeamMembers: vi.fn().mockResolvedValue([]),
		upsertTeamMember: vi.fn(),
		removeTeamMember: vi.fn(),
		getPortfolioMembers: vi.fn().mockResolvedValue([]),
		upsertPortfolioMember: vi.fn(),
		removePortfolioMember: vi.fn(),
		getGroupMappings: vi.fn().mockResolvedValue([]),
		getTeamGroupMappings: vi.fn().mockResolvedValue([]),
		getPortfolioGroupMappings: vi.fn().mockResolvedValue([]),
		createGroupMapping: vi.fn(),
		removeGroupMapping: vi.fn(),
		deleteUser: vi.fn(),
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
		rbacService,
	});

	const Wrapper: React.FC<{ children: React.ReactNode }> = ({ children }) => (
		<ApiServiceContext.Provider value={context}>
			{children}
		</ApiServiceContext.Provider>
	);

	return { Wrapper };
};

const waitForTableRow = async (rowId: number) => {
	await waitFor(() => {
		expect(screen.getByTestId(`api-key-row-${rowId}`)).toBeInTheDocument();
	});
};

describe("F-FE-2 — API Keys listing Scopes column", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	test("WS + M1.4 RBAC on with team scope renders Scopes header and resolved team name", async () => {
		const keys: IApiKeyInfo[] = [
			{
				id: 1,
				name: "CI Key",
				description: "for ci",
				createdAt: "2026-01-01T00:00:00Z",
				lastUsedAt: null,
				scopes: [
					{ role: "TeamAdmin", scopeType: "Team", scopeId: PLATFORM_TEAM_ID },
				],
			},
		];
		const { Wrapper } = buildContext({
			keys,
			authorizationSummary: buildSummary({ isRbacEnabled: true }),
			teamsResolution: [{ id: PLATFORM_TEAM_ID, name: PLATFORM_TEAM_NAME }],
		});

		render(<ApiKeysSettings />, { wrapper: Wrapper });

		await waitForTableRow(1);
		expect(
			screen.getByRole("columnheader", { name: "Scopes" }),
		).toBeInTheDocument();
		expect(
			screen.queryByRole("columnheader", { name: "Created By" }),
		).not.toBeInTheDocument();

		const row = screen.getByTestId("api-key-row-1");
		await within(row).findByText(/Team Admin/);
		await within(row).findByText(new RegExp(PLATFORM_TEAM_NAME));
	});

	test("M1.1 RBAC off omits Created By header", async () => {
		const keys: IApiKeyInfo[] = [
			{
				id: 1,
				name: "CI Key",
				description: "for ci",
				createdAt: "2026-01-01T00:00:00Z",
				lastUsedAt: null,
				scopes: [],
			},
		];
		const { Wrapper } = buildContext({
			keys,
			authorizationSummary: buildSummary({ isRbacEnabled: false }),
		});

		render(<ApiKeysSettings />, { wrapper: Wrapper });

		await waitForTableRow(1);
		expect(
			screen.queryByRole("columnheader", { name: "Created By" }),
		).not.toBeInTheDocument();
	});

	test("M1.2 RBAC on without scope rows still omits Created By header", async () => {
		const keys: IApiKeyInfo[] = [
			{
				id: 1,
				name: "CI Key",
				description: "for ci",
				createdAt: "2026-01-01T00:00:00Z",
				lastUsedAt: null,
				scopes: [],
			},
		];
		const { Wrapper } = buildContext({
			keys,
			authorizationSummary: buildSummary({ isRbacEnabled: true }),
		});

		render(<ApiKeysSettings />, { wrapper: Wrapper });

		await waitForTableRow(1);
		expect(
			screen.queryByRole("columnheader", { name: "Created By" }),
		).not.toBeInTheDocument();
	});

	test("M1.3 RBAC off hides the Scopes header", async () => {
		const keys: IApiKeyInfo[] = [
			{
				id: 1,
				name: "CI Key",
				description: "for ci",
				createdAt: "2026-01-01T00:00:00Z",
				lastUsedAt: null,
				scopes: [],
			},
		];
		const { Wrapper } = buildContext({
			keys,
			authorizationSummary: buildSummary({ isRbacEnabled: false }),
		});

		render(<ApiKeysSettings />, { wrapper: Wrapper });

		await waitForTableRow(1);
		expect(
			screen.queryByRole("columnheader", { name: "Scopes" }),
		).not.toBeInTheDocument();
	});

	test("M1.5 RBAC on with zero scope rows renders Unrestricted label", async () => {
		const keys: IApiKeyInfo[] = [
			{
				id: 9,
				name: "Legacy Key",
				description: "no scopes",
				createdAt: "2026-01-01T00:00:00Z",
				lastUsedAt: null,
				scopes: [],
			},
		];
		const { Wrapper } = buildContext({
			keys,
			authorizationSummary: buildSummary({ isRbacEnabled: true }),
		});

		render(<ApiKeysSettings />, { wrapper: Wrapper });

		await waitForTableRow(9);
		const row = screen.getByTestId("api-key-row-9");
		expect(within(row).getByText(/Unrestricted/)).toBeInTheDocument();
	});

	test("M1.7 authorization summary pending hides the Scopes header (loading default)", async () => {
		const pendingSummary = new Promise<UserAuthorizationSummary>(() => {});
		const keys: IApiKeyInfo[] = [
			{
				id: 1,
				name: "CI Key",
				description: "for ci",
				createdAt: "2026-01-01T00:00:00Z",
				lastUsedAt: null,
				scopes: [],
			},
		];
		const { Wrapper } = buildContext({
			keys,
			authorizationSummary: pendingSummary,
		});

		render(<ApiKeysSettings />, { wrapper: Wrapper });

		await waitForTableRow(1);
		expect(
			screen.queryByRole("columnheader", { name: "Scopes" }),
		).not.toBeInTheDocument();
	});

	test("M1.8 portfolio lookup failure falls back to id-formatted label while team name still resolves", async () => {
		const keys: IApiKeyInfo[] = [
			{
				id: 3,
				name: "Mixed Key",
				description: "team + portfolio",
				createdAt: "2026-01-01T00:00:00Z",
				lastUsedAt: null,
				scopes: [
					{ role: "TeamAdmin", scopeType: "Team", scopeId: PLATFORM_TEAM_ID },
					{
						role: "PortfolioAdmin",
						scopeType: "Portfolio",
						scopeId: PORTFOLIO_ID,
					},
				],
			},
		];
		const { Wrapper } = buildContext({
			keys,
			authorizationSummary: buildSummary({ isRbacEnabled: true }),
			teamsResolution: [{ id: PLATFORM_TEAM_ID, name: PLATFORM_TEAM_NAME }],
			portfoliosRejects: true,
		});

		render(<ApiKeysSettings />, { wrapper: Wrapper });

		await waitForTableRow(3);
		const row = screen.getByTestId("api-key-row-3");

		await within(row).findByText(/Team Admin/);
		await within(row).findByText(new RegExp(PLATFORM_TEAM_NAME));
		expect(within(row).queryByText(/Team #42/)).not.toBeInTheDocument();

		await within(row).findByText(/Portfolio Admin/);
		await within(row).findByText(/Portfolio #7/);
	});
});
