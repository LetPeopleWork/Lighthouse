import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { RbacUser } from "../../../models/Authorization/RbacModels";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IAuthService } from "../../../services/Api/AuthService";
import type { IRbacService } from "../../../services/Api/RbacService";
import {
	createMockApiServiceContext,
	createMockLicensingService,
} from "../../../tests/MockApiServiceProvider";
import RbacSettings from "./RbacSettings";

vi.mock(
	"../../../services/UsageData/usageDataReporter",
	async (importOriginal) => ({
		...(await importOriginal<
			typeof import("../../../services/UsageData/usageDataReporter")
		>()),
		useUsageDataReporter: () => vi.fn(),
	}),
);

const sam: RbacUser = {
	id: 1,
	subject: "oidc|sam-weber",
	displayName: "Sam Weber",
	email: "sam@example.com",
	isSystemAdmin: false,
	isUnassigned: false,
};

const noor: RbacUser = {
	id: 2,
	subject: "oidc|noor-haddad",
	displayName: "Noor Haddad",
	email: "noor@example.com",
	isSystemAdmin: false,
	isUnassigned: true,
};

const rbacService = {
	getStatus: vi.fn(),
	getUsers: vi.fn(),
	getGroupMappings: vi.fn(),
	createGroupMapping: vi.fn(),
	removeGroupMapping: vi.fn(),
	bootstrapCurrentUserAsSystemAdmin: vi.fn(),
	grantSystemAdmin: vi.fn(),
	revokeSystemAdmin: vi.fn(),
	deleteUser: vi.fn(),
};

const authService = {
	getCurrentUserProfile: vi.fn(),
} as unknown as IAuthService;

const openAccessSettings = () =>
	render(
		<QueryClientProvider
			client={
				new QueryClient({ defaultOptions: { queries: { retry: false } } })
			}
		>
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({
					rbacService: rbacService as unknown as IRbacService,
					authService,
					licensingService: createMockLicensingService(),
				})}
			>
				<RbacSettings />
			</ApiServiceContext.Provider>
		</QueryClientProvider>,
	);

describe("Settings > Access, people who hold no role of their own", () => {
	beforeEach(() => {
		vi.resetAllMocks();
		rbacService.getStatus.mockResolvedValue({
			enabled: true,
			premiumGateSatisfied: true,
			hasSystemAdmin: true,
			hasEmergencyAdminConfigured: false,
			readyForEnablement: true,
			unassignedUserCount: 1,
			groupClaimName: "groups",
		});
		rbacService.getUsers.mockResolvedValue([sam, noor]);
		rbacService.getGroupMappings.mockResolvedValue([]);
		vi.mocked(authService.getCurrentUserProfile).mockResolvedValue({
			subject: "oidc|alex-chen",
			displayName: "Alex Chen",
			email: "alex@example.com",
		});
	});

	afterEach(() => {
		vi.restoreAllMocks();
	});

	it("says No direct role in the column, the chip and the filter, never Unassigned, and the filter still narrows to those people", async () => {
		openAccessSettings();

		const table = await screen.findByTestId("rbac-users-table");
		expect(
			within(table)
				.getAllByRole("columnheader")
				.map((header) => header.textContent),
		).toEqual([
			"Display Name",
			"Email",
			"Subject",
			"System Admin",
			"No direct role",
			"Actions",
		]);
		expect(
			screen.getByTestId("rbac-status-unassigned-count"),
		).toHaveTextContent(/^No direct role: 1$/);
		const filter = screen.getByLabelText(
			/^Show users with no direct role only$/,
		);
		expect(screen.queryByText(/unassigned/i)).not.toBeInTheDocument();

		fireEvent.click(filter);

		expect(screen.queryByTestId("rbac-user-row-1")).not.toBeInTheDocument();
		expect(screen.getByTestId("rbac-user-row-2")).toBeInTheDocument();
	});
});
