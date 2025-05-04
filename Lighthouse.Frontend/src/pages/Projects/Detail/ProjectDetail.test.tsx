import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { Feature } from "../../../models/Feature";
import { Milestone } from "../../../models/Project/Milestone";
import { Project } from "../../../models/Project/Project";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IOptionalFeatureService } from "../../../services/Api/OptionalFeatureService";
import type { IProjectService } from "../../../services/Api/ProjectService";
import type { ITeamService } from "../../../services/Api/TeamService";
import type { IUpdateSubscriptionService } from "../../../services/UpdateSubscriptionService";
import {
	createMockApiServiceContext,
	createMockOptionalFeatureService,
	createMockProjectService,
	createMockTeamService,
	createMockUpdateSubscriptionService,
} from "../../../tests/MockApiServiceProvider";
import ProjectDetail from "./ProjectDetail";

vi.mock("../../../components/Common/LoadingAnimation/LoadingAnimation", () => ({
	default: ({
		children,
		hasError,
		isLoading,
	}: { children: React.ReactNode; hasError: boolean; isLoading: boolean }) => (
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

vi.mock("./ProjectFeatureList", () => ({
	default: ({ project }: { project: Project }) => (
		<div data-testid="project-feature-list">
			{project.features.length} features
		</div>
	),
}));

vi.mock("./InvolvedTeamsList", () => ({
	default: ({ teams }: { teams: ITeamSettings[] }) => (
		<div data-testid="involved-teams-list">{teams.length} teams</div>
	),
}));

vi.mock("../../../components/Common/Milestones/MilestonesComponent", () => ({
	default: ({ milestones }: { milestones: Milestone[] }) => (
		<div data-testid="milestone-component">{milestones.length} milestones</div>
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

const mockProjectService: IProjectService = createMockProjectService();
const mockTeamService: ITeamService = createMockTeamService();
const mockOptionalFeatureService: IOptionalFeatureService =
	createMockOptionalFeatureService();
const mockUpdateSubscriptionService: IUpdateSubscriptionService =
	createMockUpdateSubscriptionService();

const mockGetProject = vi.fn();
const mockGetProjectSettings = vi.fn();

const mockSubscribeToFeatureUpdates = vi.fn();
const mockSubscribeToForecastUpdates = vi.fn();
const mockGetUpdateStatus = vi.fn();

mockProjectService.getProject = mockGetProject;
mockProjectService.getProjectSettings = mockGetProjectSettings;

mockUpdateSubscriptionService.subscribeToFeatureUpdates =
	mockSubscribeToFeatureUpdates;
mockUpdateSubscriptionService.subscribeToForecastUpdates =
	mockSubscribeToForecastUpdates;
mockUpdateSubscriptionService.getUpdateStatus = mockGetUpdateStatus;

const MockApiServiceProvider = ({
	children,
}: { children: React.ReactNode }) => {
	const mockContext = createMockApiServiceContext({
		projectService: mockProjectService,
		teamService: mockTeamService,
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
			<MemoryRouter initialEntries={["/projects/2"]}>
				<Routes>
					<Route path="/projects/:id" element={<ProjectDetail />} />
				</Routes>
			</MemoryRouter>
		</MockApiServiceProvider>,
	);
};

describe("ProjectDetail component", () => {
	beforeEach(() => {
		const project = new Project();
		project.id = 2;
		project.name = "Release Codename Daniel";

		const feature1 = new Feature();
		feature1.id = 0;
		feature1.name = "Feature 1";
		feature1.workItemReference = "FTR-1";

		const feature2 = new Feature();
		feature2.id = 1;
		feature2.name = "Feature 2";
		feature2.workItemReference = "FTR-2";

		project.features = [feature1, feature2];
		project.milestones = [Milestone.new(1, "Milestone", new Date())];

		mockGetProject.mockResolvedValue(project);
		mockGetProjectSettings.mockResolvedValue({
			id: 2,
			name: "Release Codename Daniel",
			workItemTypes: [],
			milestones: [],
			workItemQuery: "Query",
			unparentedItemsQuery: "Unparented Query",
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

	it("should render project details after loading", async () => {
		renderWithMockApiProvider();

		expect(screen.getByText("Loading...")).toBeInTheDocument();

		await waitFor(() => {
			expect(screen.getByText("Release Codename Daniel")).toBeInTheDocument();
		});

		expect(screen.getByTestId("project-feature-list")).toHaveTextContent(
			"2 features",
		);
	});

	it("should refresh features on button click", async () => {
		renderWithMockApiProvider();

		await waitFor(() => {
			expect(screen.getByText("Release Codename Daniel")).toBeInTheDocument();
		});

		const refreshButton = screen.getByText("Refresh Features");
		fireEvent.click(refreshButton);

		await waitFor(() => {
			expect(refreshButton).toBeDisabled();
			expect(refreshButton).toHaveTextContent("Refresh Features");
		});
	});

	it("should render involved teams", async () => {
		renderWithMockApiProvider();

		await waitFor(() => {
			expect(screen.getByText("Release Codename Daniel")).toBeInTheDocument();
		});

		expect(screen.getByTestId("involved-teams-list")).toHaveTextContent(
			"0 teams",
		);
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
			expect(await screen.findByText("Refresh Features")).toBeEnabled();
		});
	});

	it("should set Refresh Button to Enabled if no Update In Progress", async () => {
		mockGetUpdateStatus.mockResolvedValueOnce(null);
		renderWithMockApiProvider();

		await waitFor(async () => {
			expect(await screen.findByText("Refresh Features")).toBeEnabled();
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
			expect(await screen.findByText("Refresh Features")).toBeDisabled();
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
			expect(await screen.findByText("Refresh Features")).toBeDisabled();
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
			expect(await screen.findByText("Refresh Features")).toBeDisabled();
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
			expect(await screen.findByText("Refresh Features")).toBeDisabled();
		});
	});

	afterEach(() => {
		vi.clearAllMocks();
	});
});
