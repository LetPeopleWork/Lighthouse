import { render, screen, waitFor } from "@testing-library/react";
import { BrowserRouter } from "react-router-dom";
import type { MockedFunction } from "vitest";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IPortfolioSettings } from "../../../models/Portfolio/PortfolioSettings";
import {
	ApiServiceContext,
	type IApiServiceContext,
} from "../../../services/Api/ApiServiceContext";
import EditPortfolio from "./EditPortfolio";

// Mock the router navigation
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

let mockRbacGate: { allowed: boolean; isLoading: boolean } = {
	allowed: true,
	isLoading: false,
};
vi.mock("../../../hooks/useRbacGate", () => ({
	useRbacGate: () => mockRbacGate,
}));

// Mock CreatePortfolioWizard
vi.mock(
	"../../../components/Common/CreateWizards/CreatePortfolioWizard",
	() => ({
		default: () => (
			<div data-testid="create-portfolio-wizard">CreatePortfolioWizard</div>
		),
	}),
);

// Mock ModifyProjectSettings
vi.mock(
	"../../../components/Common/ProjectSettings/ModifyProjectSettings",
	() => ({
		default: () => (
			<div data-testid="modify-project-settings">ModifyProjectSettings</div>
		),
	}),
);

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

const mockLicenseRestrictions = {
	canCreatePortfolio: true,
	canUpdatePortfolioSettings: true,
	createPortfolioTooltip: "",
	updatePortfolioSettingsTooltip: "",
};

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => mockLicenseRestrictions,
}));

describe("EditPortfolio", () => {
	let mockPortfolioService: {
		getPortfolioSettings: MockedFunction<() => Promise<IPortfolioSettings>>;
		validatePortfolioSettings: MockedFunction<() => Promise<boolean>>;
		createPortfolio: MockedFunction<() => Promise<IPortfolioSettings>>;
		updatePortfolio: MockedFunction<() => Promise<IPortfolioSettings>>;
		refreshFeaturesForPortfolio: MockedFunction<() => Promise<void>>;
	};

	let mockSettingsService: {
		getDefaultProjectSettings: MockedFunction<
			() => Promise<IPortfolioSettings>
		>;
	};

	let mockWorkTrackingSystemService: {
		getConfiguredWorkTrackingSystems: MockedFunction<() => Promise<unknown[]>>;
	};

	let mockTeamService: {
		getTeams: MockedFunction<() => Promise<unknown[]>>;
	};

	const renderEditPortfolioWithContext = () => {
		const mockApiServiceContext = {
			portfolioService: mockPortfolioService,
			settingsService: mockSettingsService,
			workTrackingSystemService: mockWorkTrackingSystemService,
			teamService: mockTeamService,
		} as unknown as IApiServiceContext;

		return render(
			<BrowserRouter>
				<ApiServiceContext.Provider value={mockApiServiceContext}>
					<EditPortfolio />
				</ApiServiceContext.Provider>
			</BrowserRouter>,
		);
	};

	beforeEach(() => {
		vi.clearAllMocks();
		mockGet.mockReturnValue(null);
		mockParams = { id: undefined };
		mockRbacGate = { allowed: true, isLoading: false };
		// Reset window.location.search
		globalThis.location.search = "";

		mockPortfolioService = {
			getPortfolioSettings: vi.fn(),
			validatePortfolioSettings: vi.fn(),
			createPortfolio: vi.fn(),
			updatePortfolio: vi.fn(),
			refreshFeaturesForPortfolio: vi.fn(),
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
			dataRetrievalValue: "",
			workItemTypes: [],
			defaultAmountOfWorkItemsPerFeature: 5,
			toDoStates: [],
			doingStates: [],
			doneStates: [],
			workTrackingSystemConnectionId: 1,
			serviceLevelExpectationProbability: 70,
			serviceLevelExpectationRange: 7,
			systemWIPLimit: 6,
			parentOverrideAdditionalFieldDefinitionId: null,
			sizeEstimateAdditionalFieldDefinitionId: null,
			featureOwnerAdditionalFieldDefinitionId: null,
			blockedStates: [],
			blockedTags: [],
			stateMappings: [],
			overrideRealChildCountStates: [],
			usePercentileToCalculateDefaultAmountOfWorkItems: false,
			defaultWorkItemPercentile: 85,
			percentileHistoryInDays: 30,
			doneItemsCutoffDays: 0,
			processBehaviourChartBaselineStartDate: null,
			processBehaviourChartBaselineEndDate: null,
			estimationAdditionalFieldDefinitionId: null,
			estimationUnit: null,
			useNonNumericEstimation: false,
			estimationCategoryValues: [],
			stalenessThresholdDays: 0,
			blockedStalenessThresholdDays: 0,
		});
		mockWorkTrackingSystemService.getConfiguredWorkTrackingSystems.mockResolvedValue(
			[],
		);
		mockTeamService.getTeams.mockResolvedValue([]);
	});

	it("renders CreatePortfolioWizard for new portfolio without cloneFrom", async () => {
		renderEditPortfolioWithContext();
		await waitFor(() => {
			expect(screen.getByTestId("create-portfolio-wizard")).toBeInTheDocument();
		});
		expect(
			screen.queryByTestId("modify-project-settings"),
		).not.toBeInTheDocument();
	});

	it("renders ModifyProjectSettings for edit mode", async () => {
		mockParams = { id: "42" };
		renderEditPortfolioWithContext();
		await waitFor(() => {
			expect(screen.getByTestId("modify-project-settings")).toBeInTheDocument();
		});
		expect(
			screen.queryByTestId("create-portfolio-wizard"),
		).not.toBeInTheDocument();
	});

	it("renders ModifyProjectSettings when cloneFrom param is present", async () => {
		globalThis.location.search = "?cloneFrom=5";
		mockGet.mockReturnValue("5");
		renderEditPortfolioWithContext();
		await waitFor(() => {
			expect(screen.getByTestId("modify-project-settings")).toBeInTheDocument();
		});
		expect(
			screen.queryByTestId("create-portfolio-wizard"),
		).not.toBeInTheDocument();
	});

	describe("RBAC guard", () => {
		it("renders no-access alert and hides wizard when user is not SystemAdmin", async () => {
			mockRbacGate = { allowed: false, isLoading: false };
			renderEditPortfolioWithContext();
			await waitFor(() => {
				expect(
					screen.getByTestId("portfolio-edit-no-access-alert"),
				).toBeInTheDocument();
			});
			expect(
				screen.queryByTestId("create-portfolio-wizard"),
			).not.toBeInTheDocument();
			expect(
				screen.queryByTestId("modify-project-settings"),
			).not.toBeInTheDocument();
			const backLink = screen.getByRole("link", { name: /back to overview/i });
			expect(backLink).toHaveAttribute("href", "/");
		});

		it("renders wizard form and hides alert when user is allowed", async () => {
			mockRbacGate = { allowed: true, isLoading: false };
			renderEditPortfolioWithContext();
			await waitFor(() => {
				expect(
					screen.getByTestId("create-portfolio-wizard"),
				).toBeInTheDocument();
			});
			expect(
				screen.queryByTestId("portfolio-edit-no-access-alert"),
			).not.toBeInTheDocument();
		});

		it("renders neither alert nor form while RBAC summary is loading", () => {
			mockRbacGate = { allowed: false, isLoading: true };
			renderEditPortfolioWithContext();
			expect(
				screen.queryByTestId("portfolio-edit-no-access-alert"),
			).not.toBeInTheDocument();
			expect(
				screen.queryByTestId("create-portfolio-wizard"),
			).not.toBeInTheDocument();
			expect(
				screen.queryByTestId("modify-project-settings"),
			).not.toBeInTheDocument();
		});

		it("renders wizard form in PERMISSIVE_SUMMARY case where allowed defaults to true", async () => {
			mockRbacGate = { allowed: true, isLoading: false };
			renderEditPortfolioWithContext();
			await waitFor(() => {
				expect(
					screen.getByTestId("create-portfolio-wizard"),
				).toBeInTheDocument();
			});
			expect(
				screen.queryByTestId("portfolio-edit-no-access-alert"),
			).not.toBeInTheDocument();
		});
	});
});
