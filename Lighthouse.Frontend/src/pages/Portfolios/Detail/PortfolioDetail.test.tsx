import {
	act,
	fireEvent,
	render,
	screen,
	waitFor,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { Feature } from "../../../models/Feature";
import { Portfolio } from "../../../models/Portfolio/Portfolio";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { ILicensingService } from "../../../services/Api/LicensingService";
import type { IOptionalFeatureService } from "../../../services/Api/OptionalFeatureService";
import type { IPortfolioService } from "../../../services/Api/PortfolioService";
import type { ITeamService } from "../../../services/Api/TeamService";
import type {
	IUpdateStatus,
	IUpdateSubscriptionService,
} from "../../../services/UpdateSubscriptionService";
import {
	createMockApiServiceContext,
	createMockLicensingService,
	createMockOptionalFeatureService,
	createMockPortfolioService,
	createMockRbacService,
	createMockTeamService,
	createMockUpdateSubscriptionService,
} from "../../../tests/MockApiServiceProvider";
import PortfolioDetail from "./PortfolioDetail";

vi.mock("../../../components/Common/LoadingAnimation/LoadingAnimation", () => ({
	default: ({
		children,
		hasError,
		isLoading,
	}: {
		children: React.ReactNode;
		hasError: boolean;
		isLoading: boolean;
	}) => (
		<>
			{isLoading && <div>Loading...</div>}
			{hasError && <div>Error loading data</div>}
			{!isLoading && !hasError && children}
		</>
	),
}));

vi.mock(
	"../../../components/Common/LocalDateTimeDisplay/LocalDateTimeDisplay",
	() => ({
		default: ({ utcDate }: { utcDate: Date }) => (
			<span>{utcDate.toString()}</span>
		),
	}),
);

vi.mock("./PortfolioFeatureList", () => ({
	default: ({ portfolio }: { portfolio: Portfolio }) => (
		<div data-testid="portfolio-feature-list">
			{portfolio.features.length} features
		</div>
	),
}));

vi.mock("./InvolvedTeamsList", () => ({
	default: ({ teams }: { teams: ITeamSettings[] }) => (
		<div data-testid="involved-teams-list">{teams.length} teams</div>
	),
}));

vi.mock("../../../components/Common/ActionButton/ActionButton", () => ({
	default: ({
		buttonText,
		onClickHandler,
		externalIsWaiting,
	}: {
		buttonText: string;
		onClickHandler: () => Promise<void>;
		externalIsWaiting: boolean;
	}) => (
		<button type="button" onClick={onClickHandler} disabled={externalIsWaiting}>
			{buttonText}
		</button>
	),
}));

vi.mock(
	"../../../components/Common/ProjectSettings/ModifyProjectSettings",
	() => ({
		default: () => (
			<div data-testid="portfolio-settings-editor">Settings Editor</div>
		),
	}),
);

const mockPortfolioService: IPortfolioService = createMockPortfolioService();
const mockTeamService: ITeamService = createMockTeamService();
const mockLicensingService: ILicensingService = createMockLicensingService();
const mockOptionalFeatureService: IOptionalFeatureService =
	createMockOptionalFeatureService();
const mockUpdateSubscriptionService: IUpdateSubscriptionService =
	createMockUpdateSubscriptionService();

const mockGetPortfolio = vi.fn();
const mockGetPortfolioSettings = vi.fn();

const mockSubscribeToFeatureUpdates = vi.fn();
const mockSubscribeToForecastUpdates = vi.fn();
const mockGetUpdateStatus = vi.fn();

mockPortfolioService.getPortfolio = mockGetPortfolio;
mockPortfolioService.getPortfolioSettings = mockGetPortfolioSettings;

mockUpdateSubscriptionService.subscribeToFeatureUpdates =
	mockSubscribeToFeatureUpdates;
mockUpdateSubscriptionService.subscribeToForecastUpdates =
	mockSubscribeToForecastUpdates;
mockUpdateSubscriptionService.getUpdateStatus = mockGetUpdateStatus;

const MockApiServiceProvider = ({
	children,
}: {
	children: React.ReactNode;
}) => {
	const mockContext = createMockApiServiceContext({
		portfolioService: mockPortfolioService,
		teamService: mockTeamService,
		licensingService: mockLicensingService,
		optionalFeatureService: mockOptionalFeatureService,
		updateSubscriptionService: mockUpdateSubscriptionService,
	});

	return (
		<ApiServiceContext.Provider value={mockContext}>
			{children}
		</ApiServiceContext.Provider>
	);
};

const renderWithMockApiProvider = () => {
	render(
		<MockApiServiceProvider>
			<MemoryRouter initialEntries={["/portfolios/2"]}>
				<Routes>
					<Route path="/portfolios/:id" element={<PortfolioDetail />} />
				</Routes>
			</MemoryRouter>
		</MockApiServiceProvider>,
	);
};

describe("PortfolioDetail component", () => {
	beforeEach(() => {
		const portfolio = new Portfolio();
		portfolio.id = 2;
		portfolio.name = "Release Codename Daniel";

		const feature1 = new Feature();
		feature1.id = 0;
		feature1.name = "Feature 1";
		feature1.referenceId = "FTR-1";

		const feature2 = new Feature();
		feature2.id = 1;
		feature2.name = "Feature 2";
		feature2.referenceId = "FTR-2";

		portfolio.features = [feature1, feature2];

		mockGetPortfolio.mockResolvedValue(portfolio);
		mockGetPortfolioSettings.mockResolvedValue({
			id: 2,
			name: "Release Codename Daniel",
			workItemTypes: [],
			dataRetrievalValue: "Query",
			usePercentileToCalculateDefaultAmountOfWorkItems: false,
			defaultAmountOfWorkItemsPerFeature: 10,
			defaultWorkItemPercentile: 85,
			historicalFeaturesWorkItemQuery: "",
			workTrackingSystemConnectionId: 0,
			sizeEstimateField: "SizeEstimate",
			toDoStates: ["New"],
			doingStates: ["Active"],
			doneStates: ["Done"],
		});
	});

	it("should render portfolio details after loading", async () => {
		renderWithMockApiProvider();

		expect(screen.getByText("Loading...")).toBeInTheDocument();

		await waitFor(() => {
			expect(screen.getByText("Release Codename Daniel")).toBeInTheDocument();
		});

		expect(screen.getByTestId("portfolio-feature-list")).toHaveTextContent(
			"2 features",
		);
	});

	it("should refresh features on button click", async () => {
		renderWithMockApiProvider();

		await waitFor(() => {
			expect(screen.getByText("Release Codename Daniel")).toBeInTheDocument();
		});

		const refreshButton = screen.getByRole("button", {
			name: "Refresh Features",
		});
		fireEvent.click(refreshButton);

		await waitFor(() => {
			expect(refreshButton).toBeDisabled();
		});
	});

	it("shows no-access guidance when portfolio details are unavailable", async () => {
		mockGetPortfolio.mockResolvedValueOnce(null);

		renderWithMockApiProvider();

		await waitFor(() => {
			expect(
				screen.getByTestId("portfolio-no-access-alert"),
			).toBeInTheDocument();
		});
	});

	it("should subscribe to feature and forecast updates on mount", async () => {
		renderWithMockApiProvider();

		await waitFor(() => {
			expect(mockSubscribeToFeatureUpdates).toHaveBeenCalled();
			expect(mockSubscribeToForecastUpdates).toHaveBeenCalled();
		});
	});

	it("should set Refresh Button to Enabled if Feature Update Completed", async () => {
		mockGetUpdateStatus.mockResolvedValueOnce({
			status: "Completed",
			updateType: "Features",
			id: 2,
		});
		renderWithMockApiProvider();

		await waitFor(async () => {
			expect(
				await screen.findByRole("button", { name: "Refresh Features" }),
			).toBeEnabled();
		});
	});

	it("should set Refresh Button to Enabled if no Update In Progress", async () => {
		mockGetUpdateStatus.mockResolvedValueOnce(null);
		renderWithMockApiProvider();

		await waitFor(async () => {
			expect(
				await screen.findByRole("button", { name: "Refresh Features" }),
			).toBeEnabled();
		});
	});

	it("should set Refresh Button to Disabled if Feature Update Queued", async () => {
		mockGetUpdateStatus.mockResolvedValueOnce({
			status: "Queued",
			updateType: "Features",
			id: 2,
		});
		renderWithMockApiProvider();

		await waitFor(async () => {
			expect(
				await screen.findByRole("button", { name: "Refresh Features" }),
			).toBeDisabled();
		});
	});

	it("should set Refresh Button to Disabled if Feature Update In Progress", async () => {
		mockGetUpdateStatus.mockResolvedValueOnce({
			status: "InProgress",
			updateType: "Features",
			id: 2,
		});
		renderWithMockApiProvider();

		await waitFor(async () => {
			expect(
				await screen.findByRole("button", { name: "Refresh Features" }),
			).toBeDisabled();
		});
	});

	it("should set Refresh Button to Disabled if Forecast Update Queued", async () => {
		mockGetUpdateStatus.mockResolvedValueOnce({
			status: "Queued",
			updateType: "Forecasts",
			id: 2,
		});
		renderWithMockApiProvider();

		await waitFor(async () => {
			expect(
				await screen.findByRole("button", { name: "Refresh Features" }),
			).toBeDisabled();
		});
	});

	it("should set Refresh Button to Disabled if Forecast Update In Progress", async () => {
		mockGetUpdateStatus.mockResolvedValueOnce({
			status: "InProgress",
			updateType: "Forecasts",
			id: 2,
		});
		renderWithMockApiProvider();

		await waitFor(async () => {
			expect(
				await screen.findByRole("button", { name: "Refresh Features" }),
			).toBeDisabled();
		});
	});

	afterEach(() => {
		vi.clearAllMocks();
	});

	describe("settings tab update deferral", () => {
		beforeEach(() => {
			// Capture the subscription callback so tests can fire SignalR events
			mockSubscribeToFeatureUpdates.mockImplementation(
				async (_id: number, callback: (update: IUpdateStatus) => void) => {
					capturedFeatureCallback = callback;
				},
			);

			capturedFeatureCallback = null;
		});

		let capturedFeatureCallback: ((update: IUpdateStatus) => void) | null =
			null;

		const renderOnSettingsTab = () => {
			render(
				<MockApiServiceProvider>
					<MemoryRouter initialEntries={["/portfolios/2/settings"]}>
						<Routes>
							<Route
								path="/portfolios/:id/:tab"
								element={<PortfolioDetail />}
							/>
							<Route path="/portfolios/:id" element={<PortfolioDetail />} />
						</Routes>
					</MemoryRouter>
				</MockApiServiceProvider>,
			);
		};

		it("does not reload portfolio when SignalR fires Completed while settings tab is active", async () => {
			renderOnSettingsTab();

			await waitFor(() =>
				expect(
					screen.getByTestId("portfolio-settings-editor"),
				).toBeInTheDocument(),
			);

			const callsBefore = mockGetPortfolio.mock.calls.length;

			// Background update completes while the user is on the settings tab
			await act(async () => {
				await capturedFeatureCallback?.({
					status: "Completed",
					updateType: "Features",
					id: 2,
				});
			});

			// Portfolio data must NOT have been reloaded
			expect(mockGetPortfolio).toHaveBeenCalledTimes(callsBefore);
		});

		it("reloads portfolio when leaving settings tab after a deferred update", async () => {
			const user = userEvent.setup();
			renderOnSettingsTab();

			await waitFor(() =>
				expect(
					screen.getByTestId("portfolio-settings-editor"),
				).toBeInTheDocument(),
			);

			// Background update completes while on settings
			await act(async () => {
				await capturedFeatureCallback?.({
					status: "Completed",
					updateType: "Features",
					id: 2,
				});
			});

			const callsAfterDeferred = mockGetPortfolio.mock.calls.length;

			// User navigates away from settings
			await user.click(screen.getByRole("tab", { name: "Features" }));

			// The deferred refresh must now fire
			await waitFor(() => {
				expect(mockGetPortfolio).toHaveBeenCalledTimes(callsAfterDeferred + 1);
			});
		});
	});
});

describe("PortfolioDetail - RBAC Tab Visibility", () => {
	const renderWithRbac = (
		isRbacEnabled: boolean,
		canCreatePortfolio: boolean,
		isSystemAdmin: boolean,
		rbacOverrides?: Partial<ReturnType<typeof createMockRbacService>>,
	) => {
		const mockPortfolioSvc = createMockPortfolioService();
		const portfolio = new Portfolio();
		portfolio.id = 1;
		portfolio.name = "Test Portfolio";
		portfolio.features = [];
		(
			mockPortfolioSvc.getPortfolio as ReturnType<typeof vi.fn>
		).mockResolvedValue(portfolio);
		(
			mockPortfolioSvc.getPortfolioSettings as ReturnType<typeof vi.fn>
		).mockResolvedValue({ id: 1, name: "Test Portfolio" });

		const mockRbacService = createMockRbacService();
		mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
			isRbacEnabled,
			isSystemAdmin,
			canCreateTeam: false,
			canCreatePortfolio,
			adminPortfolioIds: canCreatePortfolio ? [1] : [],
		});
		if (rbacOverrides) {
			Object.assign(mockRbacService, rbacOverrides);
		}

		const mockContext = createMockApiServiceContext({
			portfolioService: mockPortfolioSvc,
			teamService: createMockTeamService(),
			rbacService: mockRbacService,
			updateSubscriptionService: createMockUpdateSubscriptionService(),
		});

		render(
			<ApiServiceContext.Provider value={mockContext}>
				<MemoryRouter initialEntries={["/portfolios/1"]}>
					<Routes>
						<Route path="/portfolios/:id" element={<PortfolioDetail />} />
						<Route path="/portfolios/:id/:tab" element={<PortfolioDetail />} />
					</Routes>
				</MemoryRouter>
			</ApiServiceContext.Provider>,
		);

		return { mockRbacService };
	};

	it("should show Deliveries and Settings tabs when RBAC is disabled", async () => {
		renderWithRbac(false, false, false);

		await waitFor(() => {
			expect(
				screen.getByRole("tab", { name: "Deliveries" }),
			).toBeInTheDocument();
			expect(screen.getByRole("tab", { name: "Settings" })).toBeInTheDocument();
		});
	});

	it("should show Deliveries and Settings tabs when user is system admin", async () => {
		renderWithRbac(true, false, true);

		await waitFor(() => {
			expect(
				screen.getByRole("tab", { name: "Deliveries" }),
			).toBeInTheDocument();
			expect(screen.getByRole("tab", { name: "Settings" })).toBeInTheDocument();
		});
	});

	it("should show Deliveries and Settings tabs when user is admin of this portfolio", async () => {
		renderWithRbac(true, true, false);

		await waitFor(() => {
			expect(
				screen.getByRole("tab", { name: "Deliveries" }),
			).toBeInTheDocument();
			expect(screen.getByRole("tab", { name: "Settings" })).toBeInTheDocument();
		});
	});

	it("should show Deliveries tab but hide Settings tab when RBAC is enabled and user is a Viewer (DD-08)", async () => {
		renderWithRbac(true, false, false);

		await waitFor(() => {
			expect(
				screen.getByRole("tab", { name: "Deliveries" }),
			).toBeInTheDocument();
		});
		expect(
			screen.queryByRole("tab", { name: "Settings" }),
		).not.toBeInTheDocument();
	});

	it("loads and renders portfolio membership manager on access tab", async () => {
		const getPortfolioMembers = vi.fn().mockResolvedValue([
			{
				userProfileId: 23,
				subject: "auth0|member",
				displayName: "Member",
				email: "member@example.com",
				role: "Viewer",
			},
		]);

		renderWithRbac(true, true, false, { getPortfolioMembers });

		await waitFor(() => {
			expect(screen.getByRole("tab", { name: "Access" })).toBeInTheDocument();
		});

		fireEvent.click(screen.getByRole("tab", { name: "Access" }));

		await waitFor(() => {
			expect(screen.getByText("Portfolio Access")).toBeInTheDocument();
			expect(screen.getByTestId("scoped-member-row-23")).toBeInTheDocument();
		});

		expect(getPortfolioMembers).toHaveBeenCalledWith(1);
	});

	it("creates portfolio-scoped group mappings on access tab", async () => {
		const user = userEvent.setup();
		const getPortfolioGroupMappings = vi.fn().mockResolvedValue([
			{
				id: 31,
				groupValue: "portfolio-viewers",
				role: "Viewer",
				scopeType: "Portfolio",
				scopeId: 1,
			},
		]);
		const createGroupMapping = vi.fn().mockResolvedValue(undefined);

		renderWithRbac(true, true, false, {
			getPortfolioGroupMappings,
			createGroupMapping,
		});

		await waitFor(() => {
			expect(screen.getByRole("tab", { name: "Access" })).toBeInTheDocument();
		});

		fireEvent.click(screen.getByRole("tab", { name: "Access" }));

		await waitFor(() => {
			expect(screen.getByText("Portfolio Group Access")).toBeInTheDocument();
			expect(screen.getByTestId("scoped-group-row-31")).toBeInTheDocument();
		});

		expect(getPortfolioGroupMappings).toHaveBeenCalledWith(1);

		await user.type(screen.getByLabelText("Group value"), "portfolio-admins");
		await user.click(screen.getByTestId("scoped-group-add-button"));

		await waitFor(() => {
			expect(createGroupMapping).toHaveBeenCalledWith({
				groupValue: "portfolio-admins",
				role: "PortfolioAdmin",
				scopeType: "Portfolio",
				scopeId: 1,
			});
		});
	});
});

describe("PortfolioDetail - RBAC Access Tab and Write Controls Visibility", () => {
	const renderForRbac = (summary: {
		isRbacEnabled: boolean;
		isSystemAdmin: boolean;
		adminPortfolioIds?: number[];
	}) => {
		const portfolio = new Portfolio();
		portfolio.id = 1;
		portfolio.name = "Test Portfolio";
		portfolio.features = [];

		const mockPortfolioSvc = createMockPortfolioService();
		(
			mockPortfolioSvc.getPortfolio as ReturnType<typeof vi.fn>
		).mockResolvedValue(portfolio);
		(
			mockPortfolioSvc.getPortfolioSettings as ReturnType<typeof vi.fn>
		).mockResolvedValue({ id: 1, name: "Test Portfolio" });

		const mockRbacService = createMockRbacService();
		mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
			...summary,
			canCreateTeam: false,
			canCreatePortfolio: false,
		});

		const mockContext = createMockApiServiceContext({
			portfolioService: mockPortfolioSvc,
			teamService: createMockTeamService(),
			rbacService: mockRbacService,
			updateSubscriptionService: createMockUpdateSubscriptionService(),
		});

		render(
			<ApiServiceContext.Provider value={mockContext}>
				<MemoryRouter initialEntries={["/portfolios/1"]}>
					<Routes>
						<Route path="/portfolios/:id" element={<PortfolioDetail />} />
						<Route path="/portfolios/:id/:tab" element={<PortfolioDetail />} />
					</Routes>
				</MemoryRouter>
			</ApiServiceContext.Provider>,
		);
	};

	it("shows Settings and Access tabs for Portfolio Admin of own portfolio", async () => {
		renderForRbac({
			isRbacEnabled: true,
			isSystemAdmin: false,
			adminPortfolioIds: [1],
		});

		await waitFor(() => {
			expect(screen.getByRole("tab", { name: "Settings" })).toBeInTheDocument();
		});
		expect(screen.getByRole("tab", { name: "Access" })).toBeInTheDocument();
	});

	it("hides Settings and Access tabs for Portfolio Admin of a different portfolio", async () => {
		renderForRbac({
			isRbacEnabled: true,
			isSystemAdmin: false,
			adminPortfolioIds: [99],
		});

		await waitFor(() => {
			expect(screen.getByRole("tab", { name: "Features" })).toBeInTheDocument();
		});
		expect(
			screen.queryByRole("tab", { name: "Settings" }),
		).not.toBeInTheDocument();
		expect(
			screen.queryByRole("tab", { name: "Access" }),
		).not.toBeInTheDocument();
	});

	it("hides Settings and Access tabs for Viewer of this portfolio but shows Deliveries (DD-08)", async () => {
		renderForRbac({
			isRbacEnabled: true,
			isSystemAdmin: false,
			adminPortfolioIds: [],
		});

		await waitFor(() => {
			expect(screen.getByRole("tab", { name: "Features" })).toBeInTheDocument();
		});
		expect(screen.getByRole("tab", { name: "Deliveries" })).toBeInTheDocument();
		expect(
			screen.queryByRole("tab", { name: "Settings" }),
		).not.toBeInTheDocument();
		expect(
			screen.queryByRole("tab", { name: "Access" }),
		).not.toBeInTheDocument();
	});

	it("hides Access tab when isRbacEnabled is false but keeps Settings tab (DD-10)", async () => {
		renderForRbac({
			isRbacEnabled: false,
			isSystemAdmin: false,
			adminPortfolioIds: [],
		});

		await waitFor(() => {
			expect(screen.getByRole("tab", { name: "Settings" })).toBeInTheDocument();
		});
		expect(
			screen.queryByRole("tab", { name: "Access" }),
		).not.toBeInTheDocument();
	});

	it("does not render Access tab on initial render before portfolio loads in non-RBAC mode (DD-07)", () => {
		const mockPortfolioSvc = createMockPortfolioService();
		let resolveGetPortfolio: ((value: unknown) => void) | undefined;
		(
			mockPortfolioSvc.getPortfolio as ReturnType<typeof vi.fn>
		).mockImplementation(
			() =>
				new Promise((resolve) => {
					resolveGetPortfolio = resolve;
				}),
		);

		const mockRbacService = createMockRbacService();
		mockRbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
			isRbacEnabled: false,
			isSystemAdmin: false,
			canCreateTeam: false,
			canCreatePortfolio: false,
			adminPortfolioIds: [],
		});

		const mockContext = createMockApiServiceContext({
			portfolioService: mockPortfolioSvc,
			teamService: createMockTeamService(),
			rbacService: mockRbacService,
			updateSubscriptionService: createMockUpdateSubscriptionService(),
		});

		render(
			<ApiServiceContext.Provider value={mockContext}>
				<MemoryRouter initialEntries={["/portfolios/1"]}>
					<Routes>
						<Route path="/portfolios/:id" element={<PortfolioDetail />} />
						<Route path="/portfolios/:id/:tab" element={<PortfolioDetail />} />
					</Routes>
				</MemoryRouter>
			</ApiServiceContext.Provider>,
		);

		expect(
			screen.queryByRole("tab", { name: "Access" }),
		).not.toBeInTheDocument();

		resolveGetPortfolio?.(undefined);
	});

	it("shows Refresh Features button and QuickSettingsBar for Portfolio Admin of own portfolio", async () => {
		renderForRbac({
			isRbacEnabled: true,
			isSystemAdmin: false,
			adminPortfolioIds: [1],
		});

		await waitFor(() => {
			expect(
				screen.getByRole("button", { name: "Refresh Features" }),
			).toBeInTheDocument();
		});
		expect(
			screen.getByRole("button", { name: "System WIP Limit" }),
		).toBeInTheDocument();
	});

	it("hides Refresh Features button and QuickSettingsBar for Viewer", async () => {
		renderForRbac({
			isRbacEnabled: true,
			isSystemAdmin: false,
			adminPortfolioIds: [],
		});

		await waitFor(() => {
			expect(screen.getByRole("tab", { name: "Features" })).toBeInTheDocument();
		});
		expect(
			screen.queryByRole("button", { name: "Refresh Features" }),
		).not.toBeInTheDocument();
		expect(
			screen.queryByRole("button", { name: "System WIP Limit" }),
		).not.toBeInTheDocument();
	});

	it("hides Refresh Features button and QuickSettingsBar for Portfolio Admin of a different portfolio", async () => {
		renderForRbac({
			isRbacEnabled: true,
			isSystemAdmin: false,
			adminPortfolioIds: [99],
		});

		await waitFor(() => {
			expect(screen.getByRole("tab", { name: "Features" })).toBeInTheDocument();
		});
		expect(
			screen.queryByRole("button", { name: "Refresh Features" }),
		).not.toBeInTheDocument();
		expect(
			screen.queryByRole("button", { name: "System WIP Limit" }),
		).not.toBeInTheDocument();
	});

	it("shows Refresh Features button and QuickSettingsBar when RBAC is disabled (PERMISSIVE_SUMMARY)", async () => {
		renderForRbac({
			isRbacEnabled: false,
			isSystemAdmin: false,
			adminPortfolioIds: [],
		});

		await waitFor(() => {
			expect(
				screen.getByRole("button", { name: "Refresh Features" }),
			).toBeInTheDocument();
		});
		expect(
			screen.getByRole("button", { name: "System WIP Limit" }),
		).toBeInTheDocument();
	});
});
