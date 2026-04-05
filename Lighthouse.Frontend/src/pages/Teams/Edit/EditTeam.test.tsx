import { render, screen, waitFor } from "@testing-library/react";
import { BrowserRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
	ApiServiceContext,
	type IApiServiceContext,
} from "../../../services/Api/ApiServiceContext";
import EditTeamPage from "./EditTeam";

const mockNavigate = vi.fn();
let mockParams: { id?: string } = { id: undefined };
vi.mock("react-router-dom", async () => {
	const actual = await vi.importActual("react-router-dom");
	return {
		...actual,
		useNavigate: () => mockNavigate,
		useParams: () => mockParams,
	};
});

// Mock CreateTeamWizard
vi.mock("../../../components/Common/CreateWizards/CreateTeamWizard", () => ({
	default: () => <div data-testid="create-team-wizard">CreateTeamWizard</div>,
}));

// Mock ModifyTeamSettings
vi.mock("../../../components/Common/Team/ModifyTeamSettings", () => ({
	default: () => (
		<div data-testid="modify-team-settings">ModifyTeamSettings</div>
	),
}));

// Mock URLSearchParams and window.location
const mockGet = vi.fn();

Object.defineProperty(globalThis, "location", {
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

globalThis.URLSearchParams =
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
		mockParams = { id: undefined };
		// Reset globalThis.location.search
		globalThis.location.search = "";
		mockSettingsService.getDefaultTeamSettings.mockResolvedValue({
			id: 0,
			name: "",
			dataRetrievalValue: "",
			workItemTypes: [],
			toDoStates: [],
			doingStates: [],
			doneStates: [],
			tags: [],
			throughputHistory: 5,
			featureWIP: 1,
			parentOverrideAdditionalFieldDefinitionId: null,
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
			processBehaviourChartBaselineStartDate: null,
			processBehaviourChartBaselineEndDate: null,
		});
		mockWorkTrackingSystemService.getConfiguredWorkTrackingSystems.mockResolvedValue(
			[],
		);
		mockWorkTrackingSystemService.getWorkTrackingSystems.mockResolvedValue([]);
		mockSuggestionService.getTags.mockResolvedValue([]);
		mockSuggestionService.getWorkItemTypesForTeams.mockResolvedValue([]);
		mockSuggestionService.getStatesForTeams.mockResolvedValue([]);
	});

	it("renders CreateTeamWizard for new team without cloneFrom", async () => {
		renderEditTeamWithContext();
		await waitFor(() => {
			expect(screen.getByTestId("create-team-wizard")).toBeInTheDocument();
		});
		expect(
			screen.queryByTestId("modify-team-settings"),
		).not.toBeInTheDocument();
	});

	it("renders ModifyTeamSettings for edit mode", async () => {
		mockParams = { id: "42" };
		renderEditTeamWithContext();
		await waitFor(() => {
			expect(screen.getByTestId("modify-team-settings")).toBeInTheDocument();
		});
		expect(screen.queryByTestId("create-team-wizard")).not.toBeInTheDocument();
	});

	it("renders ModifyTeamSettings when cloneFrom param is present", async () => {
		globalThis.location.search = "?cloneFrom=5";
		mockGet.mockReturnValue("5");
		renderEditTeamWithContext();
		await waitFor(() => {
			expect(screen.getByTestId("modify-team-settings")).toBeInTheDocument();
		});
		expect(screen.queryByTestId("create-team-wizard")).not.toBeInTheDocument();
	});
});
