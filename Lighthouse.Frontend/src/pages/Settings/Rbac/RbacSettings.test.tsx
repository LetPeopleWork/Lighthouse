import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
	fireEvent,
	render,
	screen,
	waitFor,
	within,
} from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { RbacUser } from "../../../models/Authorization/RbacModels";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IAuthService } from "../../../services/Api/AuthService";
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
		deleteUser: vi.fn(),
		getAuthorizationSummary: vi.fn(),
		getTeamMembers: vi.fn(),
		upsertTeamMember: vi.fn(),
		removeTeamMember: vi.fn(),
		getPortfolioMembers: vi.fn(),
		upsertPortfolioMember: vi.fn(),
		removePortfolioMember: vi.fn(),
		getGroupMappings: vi.fn(),
		getTeamGroupMappings: vi.fn(),
		getPortfolioGroupMappings: vi.fn(),
		getTeamSystemWideRules: vi.fn().mockResolvedValue([]),
		getPortfolioSystemWideRules: vi.fn().mockResolvedValue([]),
		getEveryoneSignedIn: vi.fn(),
		setEveryoneSignedIn: vi.fn(),
		createGroupMapping: vi.fn(),
		removeGroupMapping: vi.fn(),
	};

	const mockAuthService: IAuthService = {
		getRuntimeAuthStatus: vi.fn(),
		getSession: vi.fn(),
		getCurrentUserProfile: vi.fn(),
		getLoginUrl: vi.fn().mockReturnValue("/api/latest/auth/login"),
		logout: vi.fn(),
	};

	const mockLicensingService = createMockLicensingService();

	const statusWithASystemAdmin = {
		enabled: true,
		premiumGateSatisfied: true,
		hasSystemAdmin: true,
		hasEmergencyAdminConfigured: false,
		readyForEnablement: true,
		groupClaimName: "groups",
	};

	const shownUserRowIds = () =>
		within(screen.getByTestId("rbac-users-table"))
			.queryAllByTestId(/^rbac-user-row-/)
			.map((row) => row.dataset.testid);

	const renderSubject = () => {
		const mockContext = createMockApiServiceContext({
			rbacService: mockRbacService,
			authService: mockAuthService,
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
		mockRbacService.getEveryoneSignedIn.mockResolvedValue({
			everyoneCanView: false,
			concurrencyToken: "token-1",
		});

		vi.mocked(mockAuthService.getCurrentUserProfile).mockResolvedValue({
			subject: "auth0|some-other-user",
			displayName: "Other User",
			email: "other@example.com",
		});
		vi.mocked(mockAuthService.getLoginUrl).mockReturnValue(
			"/api/latest/auth/login",
		);
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

	it("should allow filtering to users with no direct role only", async () => {
		mockRbacService.getStatus.mockResolvedValue(statusWithASystemAdmin);

		renderSubject();

		await waitFor(() => {
			expect(screen.getByTestId("rbac-user-row-1")).toBeInTheDocument();
			expect(screen.getByTestId("rbac-user-row-2")).toBeInTheDocument();
		});

		fireEvent.click(
			screen.getByLabelText("Show users with no direct role only"),
		);

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
		expect(mockRbacService.getUsers).not.toHaveBeenCalled();
		expect(mockRbacService.getGroupMappings).not.toHaveBeenCalled();
		expect(shownUserRowIds()).toEqual([]);
	});

	it("lists no users while the configuration is on its way", async () => {
		mockRbacService.getStatus.mockReturnValue(new Promise(() => undefined));

		renderSubject();

		await waitFor(() => expect(mockRbacService.getStatus).toHaveBeenCalled());
		expect(shownUserRowIds()).toEqual([]);
	});

	it("says the configuration could not load when the status cannot be read", async () => {
		mockRbacService.getStatus.mockRejectedValue(new Error("offline"));

		renderSubject();

		expect(await screen.findByRole("alert")).toHaveTextContent(
			/^Failed to load RBAC configuration\.$/,
		);
	});

	it("searches users by name or email, ignoring case and surrounding spaces", async () => {
		mockRbacService.getStatus.mockResolvedValue(statusWithASystemAdmin);
		renderSubject();
		await screen.findByTestId("rbac-user-row-2");
		const search = (text: string) =>
			fireEvent.change(screen.getByLabelText("Search by name or email"), {
				target: { value: text },
			});

		search("vIEWER uSER");
		expect(shownUserRowIds()).toEqual(["rbac-user-row-2"]);

		search("ADMIN@");
		expect(shownUserRowIds()).toEqual(["rbac-user-row-1"]);

		search("   ");
		expect(shownUserRowIds()).toEqual(["rbac-user-row-1", "rbac-user-row-2"]);
	});

	it("empties the user list when the last System Admin is revoked", async () => {
		mockRbacService.getStatus
			.mockResolvedValueOnce(statusWithASystemAdmin)
			.mockResolvedValue({ ...statusWithASystemAdmin, hasSystemAdmin: false });
		renderSubject();

		fireEvent.click(await screen.findByTestId("rbac-revoke-system-admin-1"));

		expect(
			await screen.findByText(/^No System Admin is assigned yet\.$/),
		).toBeInTheDocument();
		expect(shownUserRowIds()).toEqual([]);
	});

	it("should render users and support grant/revoke actions", async () => {
		mockRbacService.getStatus.mockResolvedValue(statusWithASystemAdmin);

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

	it("should create group mapping, asking for a group first and clearing the field once it is added", async () => {
		mockRbacService.getStatus.mockResolvedValue(statusWithASystemAdmin);

		renderSubject();

		const groupValue = await screen.findByLabelText("Group value");
		expect(groupValue).toHaveValue("");

		fireEvent.click(screen.getByTestId("rbac-create-group-mapping"));
		expect(await screen.findByRole("alert")).toHaveTextContent(
			/^Group value is required\.$/,
		);

		fireEvent.change(groupValue, {
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
		await waitFor(() => expect(groupValue).toHaveValue(""));
		await waitFor(() =>
			expect(screen.queryByRole("alert")).not.toBeInTheDocument(),
		);
	});

	it("should remove group mapping", async () => {
		mockRbacService.getStatus.mockResolvedValue(statusWithASystemAdmin);

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

	it("says when a group mapping could not be added or removed", async () => {
		mockRbacService.getStatus.mockResolvedValue(statusWithASystemAdmin);
		mockRbacService.createGroupMapping.mockRejectedValue(new Error("down"));
		mockRbacService.removeGroupMapping.mockRejectedValue(new Error("down"));
		renderSubject();

		fireEvent.change(await screen.findByLabelText("Group value"), {
			target: { value: "lh-readers" },
		});
		fireEvent.click(screen.getByTestId("rbac-create-group-mapping"));
		expect(await screen.findByRole("alert")).toHaveTextContent(
			/^Failed to create group mapping\.$/,
		);

		fireEvent.click(screen.getByTestId("rbac-remove-group-mapping-1"));
		await waitFor(() =>
			expect(screen.getByRole("alert")).toHaveTextContent(
				/^Failed to remove group mapping\.$/,
			),
		);
	});

	it("renders emergency admin row with lock icon and no Revoke button", async () => {
		mockRbacService.getStatus.mockResolvedValue({
			...statusWithASystemAdmin,
			hasEmergencyAdminConfigured: true,
		});

		mockRbacService.getUsers.mockResolvedValue([
			{
				id: 10,
				subject: "auth0|emergency",
				displayName: "Emergency Admin User",
				email: "emergency@example.com",
				isSystemAdmin: true,
				isUnassigned: false,
				isEmergencyAdmin: true,
			},
		]);

		renderSubject();

		await waitFor(() => {
			expect(screen.getByTestId("rbac-user-row-10")).toBeInTheDocument();
		});

		expect(screen.getByText("Emergency Admin")).toBeInTheDocument();
		expect(
			screen.queryByTestId("rbac-revoke-system-admin-10"),
		).not.toBeInTheDocument();
	});

	it("renders collapsed RBAC Status accordion visible on page load", async () => {
		renderSubject();

		await waitFor(() => {
			expect(screen.getByTestId("rbac-status-enabled")).toBeInTheDocument();
		});

		// Accordion summary must be visible without expanding (collapsed by default)
		expect(screen.getByText("RBAC Status")).toBeInTheDocument();
	});

	const aNormalUser: RbacUser = {
		id: 5,
		subject: "auth0|normaluser",
		displayName: "Normal User",
		email: "normal@example.com",
		isSystemAdmin: false,
		isUnassigned: false,
		isEmergencyAdmin: false,
	};

	it("renders Remove button on non-emergency-admin rows and confirm dialog triggers deleteUser once, then closes", async () => {
		mockRbacService.getStatus.mockResolvedValue(statusWithASystemAdmin);
		mockRbacService.getUsers.mockResolvedValue([aNormalUser]);
		mockRbacService.deleteUser.mockResolvedValue(undefined);

		renderSubject();

		fireEvent.click(await screen.findByTestId("rbac-remove-user-5"));
		const confirm = await screen.findByRole("button", { name: "Confirm" });
		fireEvent.click(confirm);
		fireEvent.click(confirm);

		await waitFor(() => {
			expect(mockRbacService.deleteUser).toHaveBeenCalledWith(5);
		});
		await waitFor(() =>
			expect(screen.queryByRole("dialog")).not.toBeInTheDocument(),
		);
		expect(mockRbacService.deleteUser).toHaveBeenCalledTimes(1);
	});

	it("says when a user could not be removed", async () => {
		mockRbacService.getStatus.mockResolvedValue(statusWithASystemAdmin);
		mockRbacService.getUsers.mockResolvedValue([aNormalUser]);
		mockRbacService.deleteUser.mockRejectedValue(new Error("down"));

		renderSubject();

		fireEvent.click(await screen.findByTestId("rbac-remove-user-5"));
		fireEvent.click(await screen.findByRole("button", { name: "Confirm" }));

		expect(await screen.findByRole("alert")).toHaveTextContent(
			/^Failed to remove user\.$/,
		);
	});

	it("Cancel on Remove dialog closes without triggering deleteUser", async () => {
		mockRbacService.getStatus.mockResolvedValue(statusWithASystemAdmin);

		mockRbacService.getUsers.mockResolvedValue([
			{
				id: 6,
				subject: "auth0|anotheruser",
				displayName: "Another User",
				email: "another@example.com",
				isSystemAdmin: false,
				isUnassigned: false,
				isEmergencyAdmin: false,
			},
		]);
		mockRbacService.deleteUser.mockResolvedValue(undefined);

		renderSubject();

		await waitFor(() => {
			expect(screen.getByTestId("rbac-remove-user-6")).toBeInTheDocument();
		});

		fireEvent.click(screen.getByTestId("rbac-remove-user-6"));

		await waitFor(() => {
			expect(
				screen.getByRole("button", { name: "Cancel" }),
			).toBeInTheDocument();
		});

		fireEvent.click(screen.getByRole("button", { name: "Cancel" }));

		await waitFor(() => {
			expect(
				screen.queryByRole("button", { name: "Confirm" }),
			).not.toBeInTheDocument();
		});

		expect(mockRbacService.deleteUser).not.toHaveBeenCalled();
	});

	it("does not render Remove button on the current user's own row", async () => {
		mockRbacService.getStatus.mockResolvedValue(statusWithASystemAdmin);

		mockRbacService.getUsers.mockResolvedValue([
			{
				id: 7,
				subject: "auth0|currentuser",
				displayName: "Current User",
				email: "current@example.com",
				isSystemAdmin: true,
				isUnassigned: false,
				isEmergencyAdmin: false,
			},
			{
				id: 8,
				subject: "auth0|otheruser",
				displayName: "Other User",
				email: "other@example.com",
				isSystemAdmin: false,
				isUnassigned: false,
				isEmergencyAdmin: false,
			},
		]);

		vi.mocked(mockAuthService.getCurrentUserProfile).mockResolvedValue({
			subject: "auth0|currentuser",
			displayName: "Current User",
			email: "current@example.com",
		});

		renderSubject();

		await waitFor(() => {
			expect(screen.getByTestId("rbac-user-row-7")).toBeInTheDocument();
			expect(screen.getByTestId("rbac-user-row-8")).toBeInTheDocument();
		});

		await waitFor(() => {
			expect(
				screen.queryByTestId("rbac-remove-user-7"),
			).not.toBeInTheDocument();
		});

		expect(screen.getByTestId("rbac-remove-user-8")).toBeInTheDocument();
	});
});
