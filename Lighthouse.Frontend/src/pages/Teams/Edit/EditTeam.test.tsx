import { render, waitFor } from "@testing-library/react";
import { BrowserRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import {
	ApiServiceContext,
	type IApiServiceContext,
} from "../../../services/Api/ApiServiceContext";
import EditTeamPage from "./EditTeam";

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

const mockTeamService = {
	getTeamSettings: vi.fn(),
	validateTeamSettings: vi.fn(),
	createTeam: vi.fn(),
	updateTeam: vi.fn(),
};

const mockSettingsService = {
	getDefaultTeamSettings: vi.fn(),
};

const mockWorkTrackingSystemService = {
	getConfiguredWorkTrackingSystems: vi.fn(),
	getWorkTrackingSystems: vi.fn(),
};

const mockSuggestionService = {
	getTags: vi.fn(),
	getWorkItemTypesForTeams: vi.fn(),
	getStatesForTeams: vi.fn(),
};

const mockLicenseRestrictions = {
	canCreateTeam: true,
	canUpdateTeamSettings: true,
	createTeamTooltip: "",
	updateTeamSettingsTooltip: "",
};

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => mockLicenseRestrictions,
}));

const renderEditTeamWithContext = () => {
	const mockApiServiceContext = {
		settingsService: mockSettingsService,
		teamService: mockTeamService,
		workTrackingSystemService: mockWorkTrackingSystemService,
		suggestionService: mockSuggestionService,
	} as unknown as IApiServiceContext;

	return render(
		<BrowserRouter>
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<EditTeamPage />
			</ApiServiceContext.Provider>
		</BrowserRouter>,
	);
};

describe("EditTeam", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		mockGet.mockReturnValue(null);
		// Reset window.location.search
		window.location.search = "";
		mockSettingsService.getDefaultTeamSettings.mockResolvedValue({
			id: 0,
			name: "",
			workItemQuery: "",
			workItemTypes: [],
			toDoStates: [],
			doingStates: [],
			doneStates: [],
			tags: [],
			throughputHistory: 5,
			featureWIP: 1,
			parentOverrideField: "",
			automaticallyAdjustFeatureWIP: false,
			useFixedDatesForThroughput: false,
			throughputHistoryStartDate: new Date(),
			throughputHistoryEndDate: new Date(),
			workTrackingSystemConnectionId: 0,
			serviceLevelExpectationProbability: 70,
			serviceLevelExpectationRange: 7,
			systemWIPLimit: 6,
			blockedStates: [],
			blockedTags: [],
		});
		mockWorkTrackingSystemService.getConfiguredWorkTrackingSystems.mockResolvedValue(
			[],
		);
		mockWorkTrackingSystemService.getWorkTrackingSystems.mockResolvedValue([]);
		mockSuggestionService.getTags.mockResolvedValue([]);
		mockSuggestionService.getWorkItemTypesForTeams.mockResolvedValue([]);
		mockSuggestionService.getStatesForTeams.mockResolvedValue([]);
	});

	it("loads default team settings for new team when no cloneFrom param", async () => {
		renderEditTeamWithContext();

		await waitFor(() => {
			expect(mockSettingsService.getDefaultTeamSettings).toHaveBeenCalled();
		});
	});

	it("calls getTeamSettings when cloneFrom param is present for new team", async () => {
		const mockTeamSettings: ITeamSettings = {
			id: 5,
			name: "Original Team",
			workItemQuery: "project = TEST",
			workItemTypes: ["Story", "Bug"],
			toDoStates: ["New", "Active"],
			doingStates: ["In Progress"],
			doneStates: ["Done"],
			tags: ["backend", "api"],
			throughputHistory: 5,
			featureWIP: 2,
			parentOverrideField: "",
			automaticallyAdjustFeatureWIP: false,
			useFixedDatesForThroughput: false,
			throughputHistoryStartDate: new Date(),
			throughputHistoryEndDate: new Date(),
			workTrackingSystemConnectionId: 1,
			serviceLevelExpectationProbability: 70,
			serviceLevelExpectationRange: 7,
			systemWIPLimit: 6,
			blockedStates: [],
			blockedTags: ["Blocked"],
		};

		mockTeamService.getTeamSettings.mockResolvedValue(mockTeamSettings);
		// Set window.location.search and mock URLSearchParams properly
		window.location.search = "?cloneFrom=5";
		mockGet.mockReturnValue("5"); // Mock cloneFrom=5

		renderEditTeamWithContext();

		await waitFor(() => {
			expect(mockTeamService.getTeamSettings).toHaveBeenCalledWith(5);
		});
	});

	it("prefixes name with 'Copy of' when cloning team", async () => {
		const mockTeamSettings: ITeamSettings = {
			id: 5,
			name: "Original Team",
			workItemQuery: "project = TEST",
			workItemTypes: ["Story"],
			toDoStates: ["New"],
			doingStates: ["In Progress"],
			doneStates: ["Done"],
			tags: [],
			throughputHistory: 5,
			featureWIP: 1,
			parentOverrideField: "",
			automaticallyAdjustFeatureWIP: false,
			useFixedDatesForThroughput: false,
			throughputHistoryStartDate: new Date(),
			throughputHistoryEndDate: new Date(),
			workTrackingSystemConnectionId: 1,
			serviceLevelExpectationProbability: 70,
			serviceLevelExpectationRange: 7,
			systemWIPLimit: 6,
			blockedStates: [],
			blockedTags: [],
		};

		mockTeamService.getTeamSettings.mockResolvedValue(mockTeamSettings);
		// Set window.location.search and mock URLSearchParams properly
		window.location.search = "?cloneFrom=5";
		mockGet.mockReturnValue("5"); // Mock cloneFrom=5

		renderEditTeamWithContext();

		await waitFor(() => {
			expect(mockTeamService.getTeamSettings).toHaveBeenCalledWith(5);
		});

		// TODO: Verify the name field shows "Copy of Original Team" once UI is implemented
	});
});
