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
