import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockLicensingService,
} from "../../../tests/MockApiServiceProvider";
import RbacSettings from "./RbacSettings";

describe("RbacSettings", () => {
	const mockRbacService = {
		getStatus: vi.fn(),
		getUsers: vi.fn(),
		bootstrapCurrentUserAsSystemAdmin: vi.fn(),
		grantSystemAdmin: vi.fn(),
		revokeSystemAdmin: vi.fn(),
		getAuthorizationSummary: vi.fn(),
		getTeamMembers: vi.fn(),
		upsertTeamMember: vi.fn(),
		removeTeamMember: vi.fn(),
		getPortfolioMembers: vi.fn(),
		upsertPortfolioMember: vi.fn(),
		removePortfolioMember: vi.fn(),
	};

	const mockLicensingService = createMockLicensingService();

	const renderSubject = () => {
		const mockContext = createMockApiServiceContext({
			rbacService: mockRbacService,
			licensingService: mockLicensingService,
		});

		const queryClient = new QueryClient({
			defaultOptions: {
				queries: { retry: false },
				mutations: { retry: false },
			},
		});

		return render(
			<QueryClientProvider client={queryClient}>
				<ApiServiceContext.Provider value={mockContext}>
					<RbacSettings />
				</ApiServiceContext.Provider>
			</QueryClientProvider>,
		);
	};

	beforeEach(() => {
		vi.resetAllMocks();

		mockRbacService.getStatus.mockResolvedValue({
			enabled: false,
			premiumGateSatisfied: true,
			hasSystemAdmin: false,
			hasEmergencyAdminConfigured: true,
			readyForEnablement: false,
		});

		mockRbacService.getUsers.mockResolvedValue([
			{
				id: 1,
				subject: "auth0|admin",
				displayName: "Admin User",
				email: "admin@example.com",
				isSystemAdmin: true,
			},
			{
				id: 2,
				subject: "auth0|viewer",
				displayName: "Viewer User",
				email: "viewer@example.com",
				isSystemAdmin: false,
			},
		]);

		mockRbacService.bootstrapCurrentUserAsSystemAdmin.mockResolvedValue(
			undefined,
		);
		mockRbacService.grantSystemAdmin.mockResolvedValue(undefined);
		mockRbacService.revokeSystemAdmin.mockResolvedValue(undefined);
	});

	it("should render RBAC status cards", async () => {
		renderSubject();

		await waitFor(() => {
			expect(screen.getByTestId("rbac-status-enabled")).toHaveTextContent(
				"Disabled",
			);
			expect(screen.getByTestId("rbac-status-premium-gate")).toHaveTextContent(
				"Ready",
			);
			expect(
				screen.getByTestId("rbac-status-emergency-admin"),
			).toHaveTextContent("Configured");
		});
	});

	it("should allow first-admin bootstrap when no system admin exists", async () => {
		renderSubject();

		const bootstrapButton = await screen.findByTestId("rbac-bootstrap-button");
		fireEvent.click(bootstrapButton);

		await waitFor(() => {
			expect(
				mockRbacService.bootstrapCurrentUserAsSystemAdmin,
			).toHaveBeenCalledOnce();
		});
	});

	it("should render users and support grant/revoke actions", async () => {
		mockRbacService.getStatus.mockResolvedValue({
			enabled: true,
			premiumGateSatisfied: true,
			hasSystemAdmin: true,
			hasEmergencyAdminConfigured: false,
			readyForEnablement: true,
		});

		renderSubject();

		await waitFor(() => {
			expect(screen.getByTestId("rbac-user-row-1")).toBeInTheDocument();
			expect(screen.getByTestId("rbac-user-row-2")).toBeInTheDocument();
		});

		fireEvent.click(screen.getByTestId("rbac-revoke-system-admin-1"));
		fireEvent.click(screen.getByTestId("rbac-grant-system-admin-2"));

		await waitFor(() => {
			expect(mockRbacService.revokeSystemAdmin).toHaveBeenCalledWith(1);
			expect(mockRbacService.grantSystemAdmin).toHaveBeenCalledWith(2);
		});
	});
});
