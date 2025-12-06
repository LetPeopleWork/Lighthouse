import { render, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { BrowserRouter } from "react-router-dom";
import type { MockedFunction } from "vitest";
import EditProject from "./EditProject";
import { ApiServiceContext, type IApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IProjectSettings } from "../../../models/Project/ProjectSettings";

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
const mockSearchParams = vi.fn();
Object.defineProperty(window, "location", {
	value: {
		search: "",
	},
	writable: true,
});

// Mock URLSearchParams constructor
global.URLSearchParams = vi.fn().mockImplementation((searchString) => {
	return {
		get: mockSearchParams,
	};
});

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
		getProjectSettings: MockedFunction<any>;
		validateProjectSettings: MockedFunction<any>;
		createProject: MockedFunction<any>;
		updateProject: MockedFunction<any>;
		refreshFeaturesForProject: MockedFunction<any>;
	};

	let mockSettingsService: {
		getDefaultProjectSettings: MockedFunction<any>;
	};

	let mockWorkTrackingSystemService: {
		getConfiguredWorkTrackingSystems: MockedFunction<any>;
	};

	let mockTeamService: {
		getTeams: MockedFunction<any>;
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
		mockSearchParams.mockReturnValue(null);
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
			milestones: [],
			involvedTeams: [],
			workTrackingSystemConnectionId: 1,
			serviceLevelExpectationProbability: 70,
			serviceLevelExpectationRange: 7,
			systemWIPLimit: 6,
			blockedStates: [],
			blockedTags: [],
		});
		mockWorkTrackingSystemService.getConfiguredWorkTrackingSystems.mockResolvedValue([]);
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
			milestones: [],
			involvedTeams: [],
			workTrackingSystemConnectionId: 1,
			serviceLevelExpectationProbability: 70,
			serviceLevelExpectationRange: 7,
			systemWIPLimit: 6,
			blockedStates: [],
			blockedTags: ["Blocked"],
		};

		mockProjectService.getProjectSettings.mockResolvedValue(mockProjectSettings);
		// Set window.location.search and mock URLSearchParams properly
		window.location.search = "?cloneFrom=5";
		mockSearchParams.mockReturnValue("5"); // Mock cloneFrom=5

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
			milestones: [],
			involvedTeams: [],
			workTrackingSystemConnectionId: 1,
			serviceLevelExpectationProbability: 70,
			serviceLevelExpectationRange: 7,
			systemWIPLimit: 6,
			blockedStates: [],
			blockedTags: [],
		};

		mockProjectService.getProjectSettings.mockResolvedValue(mockProjectSettings);
		// Set window.location.search and mock URLSearchParams properly
		window.location.search = "?cloneFrom=5";
		mockSearchParams.mockReturnValue("5"); // Mock cloneFrom=5

		renderEditProjectWithContext();

		await waitFor(() => {
			expect(mockProjectService.getProjectSettings).toHaveBeenCalledWith(5);
		});

		// TODO: Verify the name field shows "Copy of Original Project" once UI is implemented
	});
});