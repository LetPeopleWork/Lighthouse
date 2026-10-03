import { render, screen, waitFor } from "@testing-library/react";
import { BrowserRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import {
	ApiServiceContext,
	type IApiServiceContext,
} from "../../../services/Api/ApiServiceContext";
import EditTeamPage from "./EditTeam";

const mockNavigate = vi.fn();
let mockParams: { id?: string } = { id: undefined };
vi.mock("react-router", async () => {
	const actual = await vi.importActual("react-router");
	return {
		...actual,
		useNavigate: () => mockNavigate,
		useParams: () => mockParams,
	};
});

let mockRbacGate: { allowed: boolean; isLoading: boolean } = {
	allowed: true,
	isLoading: false,
};
vi.mock("../../../hooks/useRbacGate", () => ({
	useRbacGate: () => mockRbacGate,
}));

// Mock CreateTeamWizard
vi.mock("../../../components/Common/CreateWizards/CreateTeamWizard", () => ({
	default: () => <div data-testid="create-team-wizard">CreateTeamWizard</div>,
}));

const { renderedForm, reportUsage } = vi.hoisted(() => ({
	renderedForm: {
		getTeamSettings: undefined as undefined | (() => Promise<ITeamSettings>),
		saveTeamSettings: undefined as
			| undefined
			| ((settings: ITeamSettings) => Promise<ITeamSettings>),
	},
	reportUsage: vi.fn(),
}));

vi.mock(
	"../../../services/UsageData/usageDataReporter",
	async (importOriginal) => ({
		...(await importOriginal<
			typeof import("../../../services/UsageData/usageDataReporter")
		>()),
		useUsageDataReporter: () => reportUsage,
	}),
);

vi.mock("../../../components/Common/Team/ModifyTeamSettings", () => ({
	default: (props: {
		getTeamSettings: () => Promise<ITeamSettings>;
		saveTeamSettings: (settings: ITeamSettings) => Promise<ITeamSettings>;
	}) => {
		renderedForm.getTeamSettings = props.getTeamSettings;
		renderedForm.saveTeamSettings = props.saveTeamSettings;
		return <div data-testid="modify-team-settings">ModifyTeamSettings</div>;
	},
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
	updateTeamData: vi.fn(),
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
		mockRbacGate = { allowed: true, isLoading: false };
		renderedForm.getTeamSettings = undefined;
		renderedForm.saveTeamSettings = undefined;
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

	it("renders CreateTeamWizard when cloneFrom is not a number", async () => {
		globalThis.location.search = "?cloneFrom=abc";
		mockGet.mockReturnValue("abc");
		renderEditTeamWithContext();
		await waitFor(() => {
			expect(screen.getByTestId("create-team-wizard")).toBeInTheDocument();
		});
		expect(
			screen.queryByTestId("modify-team-settings"),
		).not.toBeInTheDocument();
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

	describe("settings loaded into the form", () => {
		const sourceSettings = {
			id: 5,
			name: "Platform Team",
			throughputHistory: 45,
			featureWIP: 2,
		} as ITeamSettings;

		const loadSettingsThroughForm = async () => {
			await waitFor(() => {
				expect(renderedForm.getTeamSettings).toBeDefined();
			});
			return renderedForm.getTeamSettings?.();
		};

		it("loads the stored settings of the Team being edited unchanged", async () => {
			mockParams = { id: "7" };
			mockTeamService.getTeamSettings.mockResolvedValue(sourceSettings);

			renderEditTeamWithContext();
			const loaded = await loadSettingsThroughForm();

			expect(mockTeamService.getTeamSettings).toHaveBeenCalledWith(7);
			expect(loaded).toEqual(sourceSettings);
		});

		it("loads a copy of the clone source as a new unsaved Team", async () => {
			globalThis.location.search = "?cloneFrom=5";
			mockGet.mockImplementation((key: string) =>
				key === "cloneFrom" ? "5" : null,
			);
			mockTeamService.getTeamSettings.mockResolvedValue(sourceSettings);

			renderEditTeamWithContext();
			const loaded = await loadSettingsThroughForm();

			expect(mockTeamService.getTeamSettings).toHaveBeenCalledWith(5);
			expect(loaded).toEqual({
				...sourceSettings,
				id: 0,
				name: "Copy of Platform Team",
			});
		});
	});

	describe("reporting that a Team's refinement was set up", () => {
		const TEAM_REFINEMENT_CONFIGURED = "TeamRefinementConfigured";

		const storedSettings = {
			id: 7,
			name: "Team Gravity",
			toDoStates: ["Backlog"],
			doingStates: ["Next"],
			doneStates: ["Done"],
			refinement: null,
		} as unknown as ITeamSettings;

		const withBacklogChosen = (settings: ITeamSettings): ITeamSettings => ({
			...settings,
			refinement: { states: [{ state: "Backlog" }] },
		});

		const saveBacklogThroughForm = async () => {
			await waitFor(() => {
				expect(renderedForm.saveTeamSettings).toBeDefined();
			});
			const loaded = await renderedForm.getTeamSettings?.();
			if (loaded === undefined) {
				throw new Error("The form loaded no settings.");
			}
			return renderedForm.saveTeamSettings?.(withBacklogChosen(loaded));
		};

		const reportsOfRefinementSetUp = () =>
			reportUsage.mock.calls.filter(
				([use]) => use.name === TEAM_REFINEMENT_CONFIGURED,
			);

		it("reports it when the edit form saves the first refinement states of a Team", async () => {
			mockParams = { id: "7" };
			mockTeamService.getTeamSettings.mockResolvedValue(storedSettings);
			mockTeamService.updateTeam.mockImplementation(
				async (settings: ITeamSettings) => settings,
			);

			renderEditTeamWithContext();
			await saveBacklogThroughForm();

			expect(reportsOfRefinementSetUp()).toHaveLength(1);
		});

		// Copying a Team that has refinement states copies a choice somebody already made.
		it("reports nothing when a clone is created with refinement states", async () => {
			globalThis.location.search = "?cloneFrom=7";
			mockGet.mockImplementation((key: string) =>
				key === "cloneFrom" ? "7" : null,
			);
			mockTeamService.getTeamSettings.mockResolvedValue(storedSettings);
			mockTeamService.createTeam.mockImplementation(
				async (settings: ITeamSettings) => ({ ...settings, id: 8 }),
			);

			renderEditTeamWithContext();
			await saveBacklogThroughForm();

			expect(mockTeamService.createTeam).toHaveBeenCalled();
			expect(reportsOfRefinementSetUp()).toHaveLength(0);
		});
	});

	describe("RBAC guard", () => {
		it("renders no-access alert and hides wizard when user is not SystemAdmin", async () => {
			mockRbacGate = { allowed: false, isLoading: false };
			renderEditTeamWithContext();
			await waitFor(() => {
				expect(
					screen.getByTestId("team-edit-no-access-alert"),
				).toBeInTheDocument();
			});
			expect(
				screen.queryByTestId("create-team-wizard"),
			).not.toBeInTheDocument();
			expect(
				screen.queryByTestId("modify-team-settings"),
			).not.toBeInTheDocument();
			const backLink = screen.getByRole("link", { name: /back to overview/i });
			expect(backLink).toHaveAttribute("href", "/");
		});

		it("renders wizard form and hides alert when user is allowed", async () => {
			mockRbacGate = { allowed: true, isLoading: false };
			renderEditTeamWithContext();
			await waitFor(() => {
				expect(screen.getByTestId("create-team-wizard")).toBeInTheDocument();
			});
			expect(
				screen.queryByTestId("team-edit-no-access-alert"),
			).not.toBeInTheDocument();
		});

		it("renders neither alert nor form while RBAC summary is loading", () => {
			mockRbacGate = { allowed: false, isLoading: true };
			renderEditTeamWithContext();
			expect(
				screen.queryByTestId("team-edit-no-access-alert"),
			).not.toBeInTheDocument();
			expect(
				screen.queryByTestId("create-team-wizard"),
			).not.toBeInTheDocument();
			expect(
				screen.queryByTestId("modify-team-settings"),
			).not.toBeInTheDocument();
		});

		it("renders wizard form in PERMISSIVE_SUMMARY case where allowed defaults to true", async () => {
			mockRbacGate = { allowed: true, isLoading: false };
			renderEditTeamWithContext();
			await waitFor(() => {
				expect(screen.getByTestId("create-team-wizard")).toBeInTheDocument();
			});
			expect(
				screen.queryByTestId("team-edit-no-access-alert"),
			).not.toBeInTheDocument();
		});
	});
});
