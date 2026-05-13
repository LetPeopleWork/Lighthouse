import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import type React from "react";
import { describe, expect, test, vi } from "vitest";
import { AuthMode } from "../../../models/Auth/AuthModels";
import type { UserAuthorizationSummary } from "../../../models/Authorization/RbacModels";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import {
	createMockApiKeyService,
	createMockApiServiceContext,
} from "../../../tests/MockApiServiceProvider";
import ApiKeysSettings from "./ApiKeysSettings";

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
	authorizationSummary?:
		| Promise<UserAuthorizationSummary>
		| UserAuthorizationSummary;
	authorizationSummaryError?: unknown;
}

const buildContext = (options: BuildContextOptions = {}) => {
	const apiKeyService = {
		...createMockApiKeyService(),
		createApiKey: vi.fn().mockResolvedValue({
			id: 1,
			name: "key",
			description: "",
			createdByUser: "alice",
			createdAt: "2026-01-01T00:00:00Z",
			plainTextKey: "lh_secret",
		}),
	};
	const authService = {
		getRuntimeAuthStatus: vi.fn().mockResolvedValue({ mode: AuthMode.Enabled }),
		getSession: vi.fn(),
		getCurrentUserProfile: vi.fn(),
		getLoginUrl: vi.fn(),
		logout: vi.fn(),
	};

	const getAuthorizationSummary = vi.fn();
	if (options.authorizationSummaryError !== undefined) {
		getAuthorizationSummary.mockRejectedValue(
			options.authorizationSummaryError,
		);
	} else if (options.authorizationSummary instanceof Promise) {
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
		rbacService,
	});

	const Wrapper: React.FC<{ children: React.ReactNode }> = ({ children }) => (
		<ApiServiceContext.Provider value={context}>
			{children}
		</ApiServiceContext.Provider>
	);

	return { Wrapper, apiKeyService, rbacService };
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

describe("CreateApiKeyDialog scope visibility — RBAC off", () => {
	test("M1.1 renders scope accordion when RBAC is enabled", async () => {
		const { Wrapper } = buildContext({
			authorizationSummary: buildSummary({ isRbacEnabled: true }),
		});

		await openDialog(Wrapper);

		await waitFor(() => {
			expect(screen.getByTestId("scope-accordion")).toBeInTheDocument();
		});
	});

	test("M1.2 / WS hides scope accordion when RBAC is disabled", async () => {
		const { Wrapper } = buildContext({
			authorizationSummary: buildSummary({ isRbacEnabled: false }),
		});

		await openDialog(Wrapper);

		expect(screen.queryByTestId("scope-accordion")).not.toBeInTheDocument();
		expect(
			screen.queryByTestId("scope-accordion-summary"),
		).not.toBeInTheDocument();
	});

	test("M1.3 hides scope accordion when authorization summary fetch fails", async () => {
		const { Wrapper } = buildContext({
			authorizationSummaryError: new Error("summary fetch failed"),
		});

		await openDialog(Wrapper);

		await waitFor(() => {
			expect(screen.queryByTestId("scope-accordion")).not.toBeInTheDocument();
		});
	});

	test("M1.4 hides scope accordion while authorization summary is loading", async () => {
		const pendingSummary = new Promise<UserAuthorizationSummary>(() => {});
		const { Wrapper } = buildContext({ authorizationSummary: pendingSummary });

		await openDialog(Wrapper);

		expect(screen.queryByTestId("scope-accordion")).not.toBeInTheDocument();
	});

	test("M1.5 submits with undefined scope when RBAC is disabled", async () => {
		const { Wrapper, apiKeyService } = buildContext({
			authorizationSummary: buildSummary({ isRbacEnabled: false }),
		});

		await openDialog(Wrapper);

		fireEvent.change(screen.getByTestId("api-key-name-input"), {
			target: { value: "CLI key" },
		});
		fireEvent.click(screen.getByTestId("create-api-key-submit-button"));

		await waitFor(() => {
			expect(apiKeyService.createApiKey).toHaveBeenCalledTimes(1);
		});
		const request = apiKeyService.createApiKey.mock.calls[0][0];
		expect(request.scope).toBeUndefined();
	});
});
