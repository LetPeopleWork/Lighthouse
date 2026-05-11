import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import {
	ApiServiceContext,
	type IApiServiceContext,
} from "../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockRbacService,
} from "../../tests/MockApiServiceProvider";
import Settings from "./Settings";

// Mock the components used in the tabs
vi.mock("./SystemInfo/SystemInfoSettings", () => ({
	default: () => <div>System Info Settings</div>,
}));
vi.mock("./DemoData/DemoDataSettings", () => ({
	default: () => <div>Demo Data Settings</div>,
}));
vi.mock("./System/SystemSettingsTab", () => ({
	default: () => <div>System Settings</div>,
}));
vi.mock("./Rbac/RbacSettings", () => ({
	default: () => <div>RBAC Settings</div>,
}));
vi.mock("./DatabaseManagement/DatabaseManagementSettings", () => ({
	default: () => <div>Database Management Settings</div>,
}));
vi.mock("../../components/App/LetPeopleWork/Tutorial/TutorialButton", () => ({
	default: () => <button type="button">Tutorial Button</button>,
}));
vi.mock(
	"../../components/App/LetPeopleWork/Tutorial/Tutorials/SettingsTutorial",
	() => ({
		default: () => <div>Settings Tutorial</div>,
	}),
);

describe("Settings Component", () => {
	const renderWithRouter = (
		initialEntries = ["/settings"],
		contextOverrides: Partial<IApiServiceContext> = {},
	) => {
		const mockApiServiceContext = createMockApiServiceContext(contextOverrides);
		return render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<MemoryRouter initialEntries={initialEntries}>
					<Settings />
				</MemoryRouter>
			</ApiServiceContext.Provider>,
		);
	};

	afterEach(() => {
		vi.resetAllMocks();
	});

	it("should render the initial tab correctly", () => {
		renderWithRouter();
		expect(screen.getByTestId("configuration-panel")).toBeVisible();
	});

	it("should render the heading as System Settings", () => {
		renderWithRouter();
		expect(
			screen.getByRole("heading", { name: "System Settings" }),
		).toBeInTheDocument();
	});

	it("should switch to demo data tab when demodata query parameter is provided", () => {
		renderWithRouter(["/settings?tab=demodata"]);

		expect(screen.getByTestId("demo-data-panel")).toBeVisible();
		expect(screen.getByTestId("configuration-panel")).not.toBeVisible();
	});

	it("should switch to Configuration tab via query parameter", () => {
		renderWithRouter(["/settings?tab=configuration"]);

		expect(screen.getByTestId("configuration-panel")).toBeVisible();
	});

	it("should switch to System Info tab when clicked", () => {
		renderWithRouter();
		fireEvent.click(screen.getByTestId("system-info-tab"));
		expect(screen.getByTestId("system-info-panel")).toBeVisible();
		expect(screen.getByTestId("configuration-panel")).not.toBeVisible();
	});

	it("should switch to Database tab when clicked", () => {
		renderWithRouter();
		fireEvent.click(screen.getByTestId("database-tab"));
		expect(screen.getByTestId("database-panel")).toBeVisible();
		expect(screen.getByTestId("configuration-panel")).not.toBeVisible();
	});

	it("should switch to Database tab via query parameter", () => {
		renderWithRouter(["/settings?tab=database"]);
		expect(screen.getByTestId("database-panel")).toBeVisible();
		expect(screen.getByTestId("configuration-panel")).not.toBeVisible();
	});

	it("should switch to RBAC tab via query parameter", async () => {
		const mockRbacService = createMockRbacService();
		mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
			isRbacEnabled: true,
			isSystemAdmin: true,
			canCreateTeam: true,
			canCreatePortfolio: true,
		});

		renderWithRouter(["/settings?tab=rbac"], { rbacService: mockRbacService });

		await waitFor(() => {
			expect(screen.getByTestId("rbac-tab")).toBeInTheDocument();
		});
		fireEvent.click(screen.getByTestId("rbac-tab"));

		expect(screen.getByTestId("rbac-panel")).toBeVisible();
		expect(screen.getByTestId("configuration-panel")).not.toBeVisible();
	});

	it("should switch to RBAC tab when clicked", async () => {
		const mockRbacService = createMockRbacService();
		mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
			isRbacEnabled: true,
			isSystemAdmin: true,
			canCreateTeam: true,
			canCreatePortfolio: true,
		});

		renderWithRouter(["/settings"], { rbacService: mockRbacService });

		await waitFor(() => {
			expect(screen.getByTestId("rbac-tab")).toBeInTheDocument();
		});
		fireEvent.click(screen.getByTestId("rbac-tab"));

		expect(screen.getByTestId("rbac-panel")).toBeVisible();
		expect(screen.getByTestId("configuration-panel")).not.toBeVisible();
	});

	it("shows API Keys tab when user is not system admin (other system-admin tabs still hidden)", async () => {
		const mockRbacService = createMockRbacService();
		mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
			isRbacEnabled: true,
			isSystemAdmin: false,
			canCreateTeam: false,
			canCreatePortfolio: false,
			systemAdminDisplayNames: ["Admin User"],
		});

		renderWithRouter(["/settings"], { rbacService: mockRbacService });

		await waitFor(() => {
			expect(screen.queryByTestId("configuration-tab")).not.toBeInTheDocument();
			expect(screen.queryByTestId("demo-data-tab")).not.toBeInTheDocument();
			expect(screen.queryByTestId("database-tab")).not.toBeInTheDocument();
			expect(screen.queryByTestId("rbac-tab")).not.toBeInTheDocument();
			expect(screen.getByTestId("api-keys-tab")).toBeInTheDocument();
			expect(screen.getByTestId("system-info-tab")).toBeInTheDocument();
		});
	});

	it("shows API Keys tab to team admin who is not system admin", async () => {
		const mockRbacService = createMockRbacService();
		mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
			isRbacEnabled: true,
			isSystemAdmin: false,
			canCreateTeam: true,
			canCreatePortfolio: false,
			adminTeamIds: [42],
			adminPortfolioIds: [],
		});

		renderWithRouter(["/settings"], { rbacService: mockRbacService });

		await waitFor(() => {
			expect(screen.getByTestId("api-keys-tab")).toBeInTheDocument();
			expect(screen.getByTestId("system-info-tab")).toBeInTheDocument();
			expect(screen.queryByTestId("configuration-tab")).not.toBeInTheDocument();
			expect(screen.queryByTestId("rbac-tab")).not.toBeInTheDocument();
		});
	});

	it("shows API Keys tab to portfolio admin who is not system admin", async () => {
		const mockRbacService = createMockRbacService();
		mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
			isRbacEnabled: true,
			isSystemAdmin: false,
			canCreateTeam: false,
			canCreatePortfolio: true,
			adminTeamIds: [],
			adminPortfolioIds: [7],
		});

		renderWithRouter(["/settings"], { rbacService: mockRbacService });

		await waitFor(() => {
			expect(screen.queryByTestId("configuration-tab")).not.toBeInTheDocument();
		});
		expect(screen.getByTestId("api-keys-tab")).toBeInTheDocument();
	});

	it("shows API Keys tab to viewer with no admin role", async () => {
		const mockRbacService = createMockRbacService();
		mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
			isRbacEnabled: true,
			isSystemAdmin: false,
			canCreateTeam: false,
			canCreatePortfolio: false,
			adminTeamIds: [],
			adminPortfolioIds: [],
		});

		renderWithRouter(["/settings"], { rbacService: mockRbacService });

		await waitFor(() => {
			expect(screen.getByTestId("api-keys-tab")).toBeInTheDocument();
			expect(screen.getByTestId("system-info-tab")).toBeInTheDocument();
			expect(screen.queryByTestId("configuration-tab")).not.toBeInTheDocument();
			expect(screen.queryByTestId("rbac-tab")).not.toBeInTheDocument();
		});
	});

	it("lands on the first visible tab (API Keys) when the default tab is hidden for non-system admins", async () => {
		const mockRbacService = createMockRbacService();
		mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
			isRbacEnabled: true,
			isSystemAdmin: false,
			canCreateTeam: false,
			canCreatePortfolio: false,
			systemAdminDisplayNames: ["Admin User"],
		});

		renderWithRouter(["/settings"], { rbacService: mockRbacService });

		await waitFor(() => {
			expect(screen.getByTestId("api-keys-tab")).toBeInTheDocument();
			expect(screen.getByTestId("api-keys-panel")).toBeVisible();
			expect(
				screen.queryByTestId("configuration-panel"),
			).not.toBeInTheDocument();
		});
	});

	it("hides System Admins tab when isRbacEnabled is false", async () => {
		const mockRbacService = createMockRbacService();
		mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
			isRbacEnabled: false,
			isSystemAdmin: true,
			canCreateTeam: true,
			canCreatePortfolio: true,
		});

		renderWithRouter(["/settings"], { rbacService: mockRbacService });

		await waitFor(() => {
			expect(screen.getByTestId("configuration-tab")).toBeInTheDocument();
		});
		expect(screen.queryByTestId("rbac-tab")).not.toBeInTheDocument();
		expect(screen.getByTestId("api-keys-tab")).toBeInTheDocument();
		expect(screen.getByTestId("system-info-tab")).toBeInTheDocument();
	});

	it("should show all tabs when user is system admin", async () => {
		const mockRbacService = createMockRbacService();
		mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
			isRbacEnabled: true,
			isSystemAdmin: true,
			canCreateTeam: true,
			canCreatePortfolio: true,
		});

		renderWithRouter(["/settings"], { rbacService: mockRbacService });

		await waitFor(() => {
			expect(screen.getByTestId("configuration-tab")).toBeInTheDocument();
			expect(screen.getByTestId("demo-data-tab")).toBeInTheDocument();
			expect(screen.getByTestId("database-tab")).toBeInTheDocument();
			expect(screen.getByTestId("rbac-tab")).toBeInTheDocument();
			expect(screen.getByTestId("api-keys-tab")).toBeInTheDocument();
			expect(screen.getByTestId("system-info-tab")).toBeInTheDocument();
		});
	});
});
