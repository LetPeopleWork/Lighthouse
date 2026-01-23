import { render, waitFor } from "@testing-library/react";
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
			tags: [],
			involvedTeams: [],
			workTrackingSystemConnectionId: 1,
			serviceLevelExpectationProbability: 70,
			serviceLevelExpectationRange: 7,
			systemWIPLimit: 6,
			parentOverrideAdditionalFieldDefinitionId: null,
			sizeEstimateAdditionalFieldDefinitionId: null,
			featureOwnerAdditionalFieldDefinitionId: null,
			blockedStates: [],
			blockedTags: [],
			overrideRealChildCountStates: [],
			usePercentileToCalculateDefaultAmountOfWorkItems: false,
			defaultWorkItemPercentile: 85,
			percentileHistoryInDays: 30,
			doneItemsCutoffDays: 0,
		});
		mockWorkTrackingSystemService.getConfiguredWorkTrackingSystems.mockResolvedValue(
			[],
		);
		mockTeamService.getTeams.mockResolvedValue([]);
	});

	it("calls getPortfolioSettings when cloneFrom param is present for new Portfolio", async () => {
		const mockPortfolioSettings: IPortfolioSettings = {
			id: 5,
			name: "Original Project",
			dataRetrievalValue: "project = TEST",
			workItemTypes: ["Story", "Bug"],
			defaultAmountOfWorkItemsPerFeature: 10,
			toDoStates: ["New", "Active"],
			doingStates: ["In Progress"],
			doneStates: ["Done"],
			tags: [],
			involvedTeams: [],
			workTrackingSystemConnectionId: 1,
			serviceLevelExpectationProbability: 70,
			serviceLevelExpectationRange: 7,
			systemWIPLimit: 6,
			parentOverrideAdditionalFieldDefinitionId: null,
			sizeEstimateAdditionalFieldDefinitionId: null,
			featureOwnerAdditionalFieldDefinitionId: null,
			blockedStates: [],
			blockedTags: ["Blocked"],
			overrideRealChildCountStates: [],
			usePercentileToCalculateDefaultAmountOfWorkItems: false,
			defaultWorkItemPercentile: 85,
			percentileHistoryInDays: 30,
			doneItemsCutoffDays: 0,
		};

		mockPortfolioService.getPortfolioSettings.mockResolvedValue(
			mockPortfolioSettings,
		);
		// Set window.location.search and mock URLSearchParams properly
		globalThis.location.search = "?cloneFrom=5";
		mockGet.mockReturnValue("5"); // Mock cloneFrom=5

		renderEditPortfolioWithContext();

		await waitFor(() => {
			expect(mockPortfolioService.getPortfolioSettings).toHaveBeenCalledWith(5);
		});
	});

	it("prefixes name with 'Copy of' when cloning Portfolio", async () => {
		const mockPortfolioSettings: IPortfolioSettings = {
			id: 5,
			name: "Original Project",
			dataRetrievalValue: "project = TEST",
			workItemTypes: ["Story"],
			defaultAmountOfWorkItemsPerFeature: 8,
			toDoStates: ["New"],
			doingStates: ["In Progress"],
			doneStates: ["Done"],
			tags: [],
			involvedTeams: [],
			workTrackingSystemConnectionId: 1,
			serviceLevelExpectationProbability: 70,
			serviceLevelExpectationRange: 7,
			systemWIPLimit: 6,
			parentOverrideAdditionalFieldDefinitionId: null,
			sizeEstimateAdditionalFieldDefinitionId: null,
			featureOwnerAdditionalFieldDefinitionId: null,
			blockedStates: [],
			blockedTags: [],
			overrideRealChildCountStates: [],
			usePercentileToCalculateDefaultAmountOfWorkItems: false,
			defaultWorkItemPercentile: 85,
			percentileHistoryInDays: 30,
			doneItemsCutoffDays: 0,
		};

		mockPortfolioService.getPortfolioSettings.mockResolvedValue(
			mockPortfolioSettings,
		);
		// Set window.location.search and mock URLSearchParams properly
		globalThis.location.search = "?cloneFrom=5";
		mockGet.mockReturnValue("5"); // Mock cloneFrom=5

		renderEditPortfolioWithContext();

		await waitFor(() => {
			expect(mockPortfolioService.getPortfolioSettings).toHaveBeenCalledWith(5);
		});
	});
});
