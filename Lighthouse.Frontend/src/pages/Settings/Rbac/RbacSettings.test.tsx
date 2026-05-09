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
		getGroupMappings: vi.fn(),
		createGroupMapping: vi.fn(),
		removeGroupMapping: vi.fn(),
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
			unassignedUserCount: 1,
			groupClaimName: "groups",
		});

		mockRbacService.getUsers.mockResolvedValue([
			{
				id: 1,
				subject: "auth0|admin",
				displayName: "Admin User",
				email: "admin@example.com",
				isSystemAdmin: true,
				isUnassigned: false,
			},
			{
				id: 2,
				subject: "auth0|viewer",
				displayName: "Viewer User",
				email: "viewer@example.com",
				isSystemAdmin: false,
				isUnassigned: true,
			},
		]);

		mockRbacService.bootstrapCurrentUserAsSystemAdmin.mockResolvedValue(
			undefined,
		);
		mockRbacService.grantSystemAdmin.mockResolvedValue(undefined);
		mockRbacService.revokeSystemAdmin.mockResolvedValue(undefined);
		mockRbacService.getGroupMappings.mockResolvedValue([
			{
				id: 1,
				groupValue: "lighthouse-system-admins",
				role: "SystemAdmin",
				scopeType: "System",
				scopeId: null,
			},
		]);
		mockRbacService.createGroupMapping.mockResolvedValue(undefined);
		mockRbacService.removeGroupMapping.mockResolvedValue(undefined);
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
			expect(
				screen.getByTestId("rbac-status-unassigned-count"),
			).toHaveTextContent("1");
			expect(screen.getByTestId("rbac-status-group-claim")).toHaveTextContent(
				"groups",
			);
		});
	});

	it("should allow filtering to unassigned users only", async () => {
		mockRbacService.getStatus.mockResolvedValue({
			enabled: true,
			premiumGateSatisfied: true,
			hasSystemAdmin: true,
			hasEmergencyAdminConfigured: false,
			readyForEnablement: true,
			unassignedUserCount: 1,
		});

		renderSubject();

		await waitFor(() => {
			expect(screen.getByTestId("rbac-user-row-1")).toBeInTheDocument();
			expect(screen.getByTestId("rbac-user-row-2")).toBeInTheDocument();
		});

		fireEvent.click(screen.getByLabelText("Show unassigned users only"));

		await waitFor(() => {
			expect(screen.queryByTestId("rbac-user-row-1")).not.toBeInTheDocument();
			expect(screen.getByTestId("rbac-user-row-2")).toBeInTheDocument();
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
			groupClaimName: "groups",
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

	it("should create group mapping", async () => {
		mockRbacService.getStatus.mockResolvedValue({
			enabled: true,
			premiumGateSatisfied: true,
			hasSystemAdmin: true,
			hasEmergencyAdminConfigured: false,
			readyForEnablement: true,
			groupClaimName: "groups",
		});

		renderSubject();

		await waitFor(() => {
			expect(
				screen.getByTestId("rbac-create-group-mapping"),
			).toBeInTheDocument();
		});

		fireEvent.change(screen.getByLabelText("Group value"), {
			target: { value: "portfolio-9-admins" },
		});

		fireEvent.click(screen.getByTestId("rbac-create-group-mapping"));

		await waitFor(() => {
			expect(mockRbacService.createGroupMapping).toHaveBeenCalledWith({
				groupValue: "portfolio-9-admins",
				role: "SystemAdmin",
				scopeType: "System",
				scopeId: null,
			});
		});
	});

	it("should remove group mapping", async () => {
		mockRbacService.getStatus.mockResolvedValue({
			enabled: true,
			premiumGateSatisfied: true,
			hasSystemAdmin: true,
			hasEmergencyAdminConfigured: false,
			readyForEnablement: true,
			groupClaimName: "groups",
		});

		renderSubject();

		await waitFor(() => {
			expect(
				screen.getByTestId("rbac-remove-group-mapping-1"),
			).toBeInTheDocument();
		});

		fireEvent.click(screen.getByTestId("rbac-remove-group-mapping-1"));

		await waitFor(() => {
			expect(mockRbacService.removeGroupMapping).toHaveBeenCalledWith(1);
		});
	});
});
