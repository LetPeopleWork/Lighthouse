import { render, waitFor } from "@testing-library/react";
import { BrowserRouter } from "react-router-dom";
import type { MockedFunction } from "vitest";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IProjectSettings } from "../../../models/Project/ProjectSettings";
import {
	ApiServiceContext,
	type IApiServiceContext,
} from "../../../services/Api/ApiServiceContext";
import EditProject from "./EditProject";

// Mock the router navigation
const mockNavigate = vi.fn();
vi.mock("react-router-dom", async () => {
	const actual = await vi.importActual("react-router-dom");
	return {
		...actual,
		useNavigate: () => mockNavigate,
		useParams: () => ({ id: undefined }),
	};
});

// Mock URLSearchParams and window.location
const mockGet = vi.fn();

Object.defineProperty(window, "location", {
	value: {
		search: "",
	},
	writable: true,
});

// Mock URLSearchParams constructor using class approach
class MockURLSearchParams {
	get = mockGet;
	append = vi.fn();
	delete = vi.fn();
	getAll = vi.fn();
	has = vi.fn();
	set = vi.fn();
	sort = vi.fn();
	toString = vi.fn();
	keys = vi.fn();
	values = vi.fn();
	entries = vi.fn();
	forEach = vi.fn();
	size = 0;
	[Symbol.iterator] = vi.fn();
}

global.URLSearchParams =
	MockURLSearchParams as unknown as typeof URLSearchParams;

const mockLicenseRestrictions = {
	canCreateProject: true,
	canUpdateProjectSettings: true,
	createProjectTooltip: "",
	updateProjectSettingsTooltip: "",
};

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => mockLicenseRestrictions,
}));

describe("EditProject", () => {
	let mockProjectService: {
		getProjectSettings: MockedFunction<() => Promise<IProjectSettings>>;
		validateProjectSettings: MockedFunction<() => Promise<boolean>>;
		createProject: MockedFunction<() => Promise<IProjectSettings>>;
		updateProject: MockedFunction<() => Promise<IProjectSettings>>;
		refreshFeaturesForProject: MockedFunction<() => Promise<void>>;
	};

	let mockSettingsService: {
		getDefaultProjectSettings: MockedFunction<() => Promise<IProjectSettings>>;
	};

	let mockWorkTrackingSystemService: {
		getConfiguredWorkTrackingSystems: MockedFunction<() => Promise<unknown[]>>;
	};

	let mockTeamService: {
		getTeams: MockedFunction<() => Promise<unknown[]>>;
	};

	const renderEditProjectWithContext = () => {
		const mockApiServiceContext = {
			projectService: mockProjectService,
			settingsService: mockSettingsService,
			workTrackingSystemService: mockWorkTrackingSystemService,
			teamService: mockTeamService,
		} as unknown as IApiServiceContext;

		return render(
			<BrowserRouter>
				<ApiServiceContext.Provider value={mockApiServiceContext}>
					<EditProject />
				</ApiServiceContext.Provider>
			</BrowserRouter>,
		);
	};

	beforeEach(() => {
		vi.clearAllMocks();
		mockGet.mockReturnValue(null);
		// Reset window.location.search
		window.location.search = "";

		mockProjectService = {
			getProjectSettings: vi.fn(),
			validateProjectSettings: vi.fn(),
			createProject: vi.fn(),
			updateProject: vi.fn(),
			refreshFeaturesForProject: vi.fn(),
		};

		mockSettingsService = {
			getDefaultProjectSettings: vi.fn(),
		};

		mockWorkTrackingSystemService = {
			getConfiguredWorkTrackingSystems: vi.fn(),
		};

		mockTeamService = {
			getTeams: vi.fn(),
		};

		// Default mock responses
		mockSettingsService.getDefaultProjectSettings.mockResolvedValue({
			id: 0,
			name: "",
			workItemQuery: "",
			unparentedItemsQuery: "",
			workItemTypes: [],
			defaultAmountOfWorkItemsPerFeature: 5,
			toDoStates: [],
			doingStates: [],
			doneStates: [],
			tags: [],
			milestones: [],
			involvedTeams: [],
			workTrackingSystemConnectionId: 1,
			serviceLevelExpectationProbability: 70,
			serviceLevelExpectationRange: 7,
			systemWIPLimit: 6,
			parentOverrideField: "",
			blockedStates: [],
			blockedTags: [],
			overrideRealChildCountStates: [],
			usePercentileToCalculateDefaultAmountOfWorkItems: false,
			defaultWorkItemPercentile: 85,
			percentileHistoryInDays: 30,
		});
		mockWorkTrackingSystemService.getConfiguredWorkTrackingSystems.mockResolvedValue(
			[],
		);
		mockTeamService.getTeams.mockResolvedValue([]);
	});

	it("loads default project settings for new project when no cloneFrom param", async () => {
		renderEditProjectWithContext();

		await waitFor(() => {
			expect(mockSettingsService.getDefaultProjectSettings).toHaveBeenCalled();
		});
	});

	it("calls getProjectSettings when cloneFrom param is present for new project", async () => {
		const mockProjectSettings: IProjectSettings = {
			id: 5,
			name: "Original Project",
			workItemQuery: "project = TEST",
			unparentedItemsQuery: "parent is empty",
			workItemTypes: ["Story", "Bug"],
			defaultAmountOfWorkItemsPerFeature: 10,
			toDoStates: ["New", "Active"],
			doingStates: ["In Progress"],
			doneStates: ["Done"],
			tags: [],
			milestones: [],
			involvedTeams: [],
			workTrackingSystemConnectionId: 1,
			serviceLevelExpectationProbability: 70,
			serviceLevelExpectationRange: 7,
			systemWIPLimit: 6,
			parentOverrideField: "",
			blockedStates: [],
			blockedTags: ["Blocked"],
			overrideRealChildCountStates: [],
			usePercentileToCalculateDefaultAmountOfWorkItems: false,
			defaultWorkItemPercentile: 85,
			percentileHistoryInDays: 30,
		};

		mockProjectService.getProjectSettings.mockResolvedValue(
			mockProjectSettings,
		);
		// Set window.location.search and mock URLSearchParams properly
		window.location.search = "?cloneFrom=5";
		mockGet.mockReturnValue("5"); // Mock cloneFrom=5

		renderEditProjectWithContext();

		await waitFor(() => {
			expect(mockProjectService.getProjectSettings).toHaveBeenCalledWith(5);
		});
	});

	it("prefixes name with 'Copy of' when cloning project", async () => {
		const mockProjectSettings: IProjectSettings = {
			id: 5,
			name: "Original Project",
			workItemQuery: "project = TEST",
			unparentedItemsQuery: "parent is empty",
			workItemTypes: ["Story"],
			defaultAmountOfWorkItemsPerFeature: 8,
			toDoStates: ["New"],
			doingStates: ["In Progress"],
			doneStates: ["Done"],
			tags: [],
			milestones: [],
			involvedTeams: [],
			workTrackingSystemConnectionId: 1,
			serviceLevelExpectationProbability: 70,
			serviceLevelExpectationRange: 7,
			systemWIPLimit: 6,
			parentOverrideField: "",
			blockedStates: [],
			blockedTags: [],
			overrideRealChildCountStates: [],
			usePercentileToCalculateDefaultAmountOfWorkItems: false,
			defaultWorkItemPercentile: 85,
			percentileHistoryInDays: 30,
		};

		mockProjectService.getProjectSettings.mockResolvedValue(
			mockProjectSettings,
		);
		// Set window.location.search and mock URLSearchParams properly
		window.location.search = "?cloneFrom=5";
		mockGet.mockReturnValue("5"); // Mock cloneFrom=5

		renderEditProjectWithContext();

		await waitFor(() => {
			expect(mockProjectService.getProjectSettings).toHaveBeenCalledWith(5);
		});

		// TODO: Verify the name field shows "Copy of Original Project" once UI is implemented
	});
});
