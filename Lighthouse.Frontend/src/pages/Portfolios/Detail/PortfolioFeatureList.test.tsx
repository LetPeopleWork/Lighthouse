import { screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { Delivery } from "../../../models/Delivery";
import { Feature, type IFeature } from "../../../models/Feature";
import type { IForecast } from "../../../models/Forecasts/IForecast";
import { WhenForecast } from "../../../models/Forecasts/WhenForecast";
import { Portfolio } from "../../../models/Portfolio/Portfolio";
import { Team } from "../../../models/Team/Team";
import type { IWorkItem } from "../../../models/WorkItem";
import { DeliverySelectionMode } from "../../../models/WorkItemRules";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IFeatureService } from "../../../services/Api/FeatureService";
import type { ITeamMetricsService } from "../../../services/Api/MetricsService";
import {
	aFeatureServiceAnswering,
	aList,
	aRow,
	RenamedTerms,
	renderAFeatureList,
} from "../../../tests/FeatureListTestKit";
import {
	createMockApiServiceContext,
	createMockTeamMetricsService,
} from "../../../tests/MockApiServiceProvider";
import { renderWithQueryClient } from "../../../tests/renderWithQueryClient";
import { featureWarningSentences } from "../../../utils/features/featureWarningSentences";
import { buildDeliveryExportTable } from "./Components/DeliveryGrid/deliveryExportTable";
import {
	anythingToWarnAbout,
	barMarksFor,
	warningsColumnFor,
} from "./Components/DeliveryGrid/timeline/deliveryBarMarks";
import PortfolioFeatureList from "./PortfolioFeatureList";

vi.mock("../../../components/Common/Forecasts/ForecastInfoList", () => ({
	default: ({
		title,
		forecasts,
	}: {
		title: string;
		forecasts: IForecast[];
	}) => (
		<div data-testid={`forecast-info-list-${title}`}>
			{forecasts.map((forecast: IForecast) => (
				<div key={forecast.probability}>{forecast.probability}%</div>
			))}
		</div>
	),
}));

vi.mock(
	"../../../components/Common/LocalDateTimeDisplay/LocalDateTimeDisplay",
	() => ({
		default: ({ utcDate }: { utcDate: Date }) => (
			<span data-testid="local-date-time-display">{utcDate.toString()}</span>
		),
	}),
);

vi.mock("../../../components/Common/Forecasts/ForecastLikelihood", () => ({
	default: ({ likelihood }: { likelihood: number }) => (
		<div data-testid="forecast-likelihood">{likelihood}%</div>
	),
}));

// Mock @mui/x-data-grid CSS import
vi.mock("@mui/x-data-grid", async () => {
	const actual = await vi.importActual("@mui/x-data-grid");
	return {
		...actual,
	};
});

// Mock matchMedia for MUI DataGrid
beforeEach(() => {
	Object.defineProperty(globalThis, "matchMedia", {
		writable: true,
		value: vi.fn().mockImplementation((query) => ({
			matches: false,
			media: query,
			onchange: null,
			addListener: vi.fn(),
			removeListener: vi.fn(),
			addEventListener: vi.fn(),
			removeEventListener: vi.fn(),
			dispatchEvent: vi.fn(),
		})),
	});

	// Clear localStorage before each test
	localStorage.clear();
});

const mockTeamMetricsService: ITeamMetricsService = {
	...createMockTeamMetricsService(),
	getFeaturesInProgress: vi.fn().mockResolvedValue([]),
};

// Create mock features for the service
const mockFeature1 = new Feature();
mockFeature1.id = 1;
mockFeature1.name = "Feature 1";
mockFeature1.referenceId = "FTR-1";
mockFeature1.forecasts = [];
mockFeature1.lastUpdated = new Date();
mockFeature1.isUsingDefaultFeatureSize = false;
mockFeature1.stateCategory = "ToDo";
mockFeature1.remainingWork = { 1: 5, 2: 5 };
mockFeature1.totalWork = { 1: 5, 2: 5 };

const mockFeature2 = new Feature();
mockFeature2.id = 2;
mockFeature2.name = "Feature 2";
mockFeature2.referenceId = "FTR-2";
mockFeature2.forecasts = [];
mockFeature2.lastUpdated = new Date();
mockFeature2.isUsingDefaultFeatureSize = true;
mockFeature2.stateCategory = "Doing";
mockFeature2.remainingWork = { 1: 10, 2: 5 };
mockFeature2.totalWork = { 1: 10, 2: 5 };

const mockFeature3 = new Feature();
mockFeature3.id = 3;
mockFeature3.name = "Feature 3";
mockFeature3.referenceId = "FTR-3";
mockFeature3.forecasts = [];
mockFeature3.lastUpdated = new Date();
mockFeature3.isUsingDefaultFeatureSize = false;
mockFeature3.stateCategory = "Done";
mockFeature3.remainingWork = { 1: 0, 2: 0 };
mockFeature3.totalWork = { 1: 5, 2: 5 };

const MockApiServiceProvider = ({
	children,
}: {
	children: React.ReactNode;
}) => {
	const mockFeatureWorkItems: IWorkItem[] = [
		{
			id: 201,
			referenceId: "STORY-201",
			name: "Story 201",
			type: "Story",
			state: "Doing",
			stateCategory: "Doing",
			url: "https://example.com/story/201",
			startedDate: new Date("2023-07-01"),
			closedDate: new Date("2023-07-03"),
			cycleTime: 3,
			workItemAge: 5,
			parentWorkItemReference: "FTR-1",
			isBlocked: false,
		},
	];

	const mockFeatureService: Partial<IFeatureService> = {
		getFeatureList: vi
			.fn()
			.mockResolvedValue(aList([mockFeature1, mockFeature2, mockFeature3])),
		getFeatureWorkItems: vi.fn().mockResolvedValue(mockFeatureWorkItems),
	};

	const mockContext = createMockApiServiceContext({
		teamMetricsService: mockTeamMetricsService,
		featureService: mockFeatureService as IFeatureService,
	});

	return (
		<ApiServiceContext.Provider value={mockContext}>
			{children}
		</ApiServiceContext.Provider>
	);
};

describe("PortfolioFeatureList component", () => {
	const team1: Team = (() => {
		const team = new Team();
		team.name = "Team A";
		team.id = 1;
		team.portfolios = [];
		team.features = [];
		team.featureWip = 1;
		team.lastUpdated = new Date();
		team.useFixedDatesForThroughput = false;
		team.throughputStartDate = new Date(
			new Date().setDate(new Date().getDate() - [1].length),
		);
		team.throughputEndDate = new Date();
		return team;
	})();
	const team2: Team = (() => {
		const team = new Team();
		team.name = "Team B";
		team.id = 2;
		team.portfolios = [];
		team.features = [];
		team.featureWip = 1;
		team.lastUpdated = new Date();
		team.useFixedDatesForThroughput = false;
		team.throughputStartDate = new Date(
			new Date().setDate(new Date().getDate() - [1].length),
		);
		team.throughputEndDate = new Date();
		return team;
	})();

	const pastDate = new Date();
	pastDate.setDate(pastDate.getDate() - 1);

	const todayDate = new Date();
	todayDate.setHours(0, 0, 0, 0);

	const futureDate = new Date();
	futureDate.setDate(futureDate.getDate() + 1);

	const feature1: Feature = (() => {
		const feature = new Feature();
		feature.name = "Feature 1";
		feature.id = 1;
		feature.referenceId = "FTR-1";
		feature.stateCategory = "ToDo";
		feature.isUsingDefaultFeatureSize = false;
		feature.lastUpdated = new Date();
		feature.projects = [{ id: 10, name: "" }];
		feature.remainingWork = { 1: 5, 2: 5 };
		feature.totalWork = { 1: 5, 2: 5 };
		feature.forecasts = [
			(() => {
				const forecast = new WhenForecast();
				forecast.probability = 80;
				forecast.expectedDate = new Date();
				return forecast;
			})(),
		];
		feature.startedDate = new Date("2023-07-01");
		feature.closedDate = new Date("2023-07-10");
		feature.cycleTime = 9;
		feature.workItemAge = 10;
		return feature;
	})();
	const feature2: Feature = (() => {
		const feature = new Feature();
		feature.name = "Feature 2";
		feature.id = 2;
		feature.referenceId = "FTR-2";
		feature.stateCategory = "Doing";
		feature.isUsingDefaultFeatureSize = true;
		feature.lastUpdated = new Date();
		feature.projects = [{ id: 15, name: "" }];
		feature.remainingWork = { 1: 10, 2: 5 };
		feature.totalWork = { 1: 10, 2: 5 };
		feature.forecasts = [
			(() => {
				const forecast = new WhenForecast();
				forecast.probability = 60;
				forecast.expectedDate = new Date();
				return forecast;
			})(),
		];
		feature.startedDate = new Date("2023-07-01");
		feature.closedDate = new Date("2023-07-09");
		feature.cycleTime = 8;
		feature.workItemAge = 9;
		return feature;
	})();
	const feature3: Feature = (() => {
		const feature = new Feature();
		feature.name = "Feature 3";
		feature.id = 3;
		feature.referenceId = "FTR-3";
		feature.stateCategory = "Done";
		feature.isUsingDefaultFeatureSize = false;
		feature.lastUpdated = new Date();
		feature.projects = [{ id: 20, name: "" }];
		feature.remainingWork = { 1: 0, 2: 0 };
		feature.totalWork = { 1: 5, 2: 5 };
		feature.forecasts = [
			(() => {
				const forecast = new WhenForecast();
				forecast.probability = 100;
				forecast.expectedDate = new Date();
				return forecast;
			})(),
		];
		feature.startedDate = new Date("2023-07-01");
		feature.closedDate = new Date("2023-07-08");
		feature.cycleTime = 7;
		feature.workItemAge = 8;
		return feature;
	})();

	const portfolio: Portfolio = (() => {
		const portfolio = new Portfolio();
		portfolio.name = "Project 1";
		portfolio.id = 1;
		portfolio.involvedTeams = [team1, team2];
		portfolio.features = [feature1, feature2, feature3];
		portfolio.lastUpdated = new Date();
		return portfolio;
	})();

	it("should render all features with correct data", async () => {
		const { default: userEvent } = await import("@testing-library/user-event");
		const user = userEvent.setup();

		renderWithQueryClient(
			<MockApiServiceProvider>
				<MemoryRouter>
					<PortfolioFeatureList portfolio={portfolio} />
				</MemoryRouter>
			</MockApiServiceProvider>,
		);

		// Wait for the grid to render
		const grid = await screen.findByRole("grid");
		expect(grid).toBeInTheDocument();

		// Uncheck the toggle to show all features including completed ones
		const toggle = screen.getByTestId("hide-completed-features-toggle");
		const switchInput = toggle.querySelector('input[type="checkbox"]');
		await user.click(switchInput as HTMLElement);

		// Check that features are rendered (text might be split across elements)
		expect(await screen.findByText(/FTR-1/)).toBeInTheDocument();
		expect(await screen.findByText(/Feature 1/)).toBeInTheDocument();
		expect(await screen.findByText(/FTR-2/)).toBeInTheDocument();
		expect(await screen.findByText(/Feature 2/)).toBeInTheDocument();
		expect(await screen.findByText(/FTR-3/)).toBeInTheDocument();
		expect(await screen.findByText(/Feature 3/)).toBeInTheDocument();

		// Check for forecast info lists
		const forecastInfoListElements = screen.getAllByTestId(
			"forecast-info-list-",
		);
		expect(forecastInfoListElements.length).toBeGreaterThanOrEqual(
			portfolio.features.length,
		);
	});

	it("should render the correct number of features", async () => {
		const { default: userEvent } = await import("@testing-library/user-event");
		const user = userEvent.setup();

		renderWithQueryClient(
			<MockApiServiceProvider>
				<MemoryRouter>
					<PortfolioFeatureList portfolio={portfolio} />
				</MemoryRouter>
			</MockApiServiceProvider>,
		);

		// Wait for the grid to render
		const grid = await screen.findByRole("grid");
		expect(grid).toBeInTheDocument();

		// Uncheck the toggle to show all features including completed ones
		const toggle = screen.getByTestId("hide-completed-features-toggle");
		const switchInput = toggle.querySelector('input[type="checkbox"]');
		await user.click(switchInput as HTMLElement);

		// Check for all feature IDs and names
		expect(await screen.findByText(/FTR-1/)).toBeInTheDocument();
		expect(await screen.findByText(/Feature 1/)).toBeInTheDocument();
		expect(await screen.findByText(/FTR-2/)).toBeInTheDocument();
		expect(await screen.findByText(/Feature 2/)).toBeInTheDocument();
		expect(await screen.findByText(/FTR-3/)).toBeInTheDocument();
		expect(await screen.findByText(/Feature 3/)).toBeInTheDocument();
	});

	it("should display the warning icon for features using the default feature size", async () => {
		renderWithQueryClient(
			<MockApiServiceProvider>
				<MemoryRouter>
					<PortfolioFeatureList portfolio={portfolio} />
				</MemoryRouter>
			</MockApiServiceProvider>,
		);

		// Wait for the grid to render
		const grid = await screen.findByRole("grid");
		expect(grid).toBeInTheDocument();

		// Feature 2 uses default feature size
		expect(await screen.findByText(/FTR-2/)).toBeInTheDocument();
		expect(await screen.findByText(/Feature 2/)).toBeInTheDocument();
	});

	it("should render toggle for hide completed features", async () => {
		renderWithQueryClient(
			<MockApiServiceProvider>
				<MemoryRouter>
					<PortfolioFeatureList portfolio={portfolio} />
				</MemoryRouter>
			</MockApiServiceProvider>,
		);

		// Wait for component to render
		await screen.findByTestId("hide-completed-features-toggle");

		expect(
			screen.getByTestId("hide-completed-features-toggle"),
		).toBeInTheDocument();
	});

	it("should open work items dialog when Overall Progress is clicked", async () => {
		const { default: userEvent } = await import("@testing-library/user-event");
		const user = userEvent.setup();

		renderWithQueryClient(
			<MockApiServiceProvider>
				<MemoryRouter>
					<PortfolioFeatureList portfolio={portfolio} />
				</MemoryRouter>
			</MockApiServiceProvider>,
		);

		await screen.findByText(/FTR-1/);
		await user.click(screen.getByRole("button", { name: "Overall Progress" }));

		expect(await screen.findByText("Story 201")).toBeInTheDocument();
	});
});

// The words a tenant that renamed Feature and Portfolio reads, so a hardcoded default word cannot pass.
const RENAMED = {
	workItemsTerm: "Work Items",
	featureTerm: "Bet",
	portfolioTerm: "Program",
};

const NO_CHILDREN_NOTE =
	"No child Work Items yet for this Bet. It counts as 0 because default Bet sizing is off in this Program's settings.";

const DEFAULT_SIZE_SENTENCE =
	"No child Work Items were found for this Bet. The remaining Work Items displayed are based on the default Bet size specified in the advanced project settings.";

const theRowOf = (name: RegExp) => screen.findByRole("row", { name });

describe("a Feature without child Work Items says so in the Feature list", () => {
	it("with default sizing off, the row says it has no child Work Items yet and no default-size sentence, while a Feature with real counts or merely no work counted says nothing", async () => {
		await renderAFeatureList(
			<PortfolioFeatureList portfolio={{ ...new Portfolio(), id: 2 }} />,
			{
				terms: RenamedTerms,
				featureService: aFeatureServiceAnswering(
					Promise.resolve(
						aList([
							aRow(31, "NW-31", "Fresh idea", { hasNoChildWorkItemsYet: true }),
							aRow(32, "NW-32", "Zero but unflagged", { noWorkAtAll: true }),
							aRow(33, "NW-33", "Real counts"),
						]),
					),
				),
			},
		);

		const childless = await theRowOf(/Fresh idea/);
		const icon = within(childless).getByTestId("warnings");
		expect(
			icon.getAttribute("aria-label"),
			"The childless row's warning icon must carry exactly the note: the sentence about no child Work Items yet, in the tenant's own words, and no default-size sentence.",
		).toBe(NO_CHILDREN_NOTE);

		await userEvent.hover(icon);
		expect(
			(await screen.findByRole("tooltip")).textContent,
			"Hovering the icon must show the note in the tooltip.",
		).toBe(NO_CHILDREN_NOTE);

		for (const name of [/Zero but unflagged/, /Real counts/]) {
			expect(
				within(screen.getByRole("row", { name })).getByTestId("no-warnings"),
				`${name} must show no warning: a note keyed on total work alone would wrongly mark it.`,
			).toBeInTheDocument();
		}
	});

	it("with default sizing on, a Feature counted by the default size keeps its default-size sentence only", async () => {
		await renderAFeatureList(
			<PortfolioFeatureList portfolio={{ ...new Portfolio(), id: 2 }} />,
			{
				terms: RenamedTerms,
				featureService: aFeatureServiceAnswering(
					Promise.resolve(
						aList([
							aRow(34, "NW-34", "Guessed size", { usesDefaultSize: true }),
						]),
					),
				),
			},
		);

		const row = await theRowOf(/Guessed size/);
		expect(
			within(row).getByTestId("warnings").getAttribute("aria-label"),
			"With default sizing on, today's default-size sentence must stay the only sentence, with no note about child Work Items.",
		).toBe(DEFAULT_SIZE_SENTENCE);
	});
});

describe("a Feature without child Work Items says so on the Delivery timeline and in the export", () => {
	const childlessFeature = (overrides: Partial<IFeature> = {}): IFeature =>
		Object.assign(new Feature(), {
			id: 1,
			name: "Fresh idea",
			referenceId: "NW-31",
			state: "In Progress",
			stateCategory: "Doing",
			isUsingDefaultFeatureSize: false,
			hasNoChildWorkItemsYet: true,
			remainingWork: {},
			totalWork: {},
			forecasts: [],
			dependsOn: [],
			...overrides,
		});

	const aDeliveryOf = (features: IFeature[]) =>
		Delivery.fromBackend({
			id: 1,
			name: "Q3",
			date: "2026-09-12T00:00:00",
			portfolioId: 2,
			features: features.map((feature) => feature.id),
			likelihoodPercentage: 0,
			progress: 0,
			remainingWork: 0,
			totalWork: 0,
			featureLikelihoods: [],
			completionDates: [],
			selectionMode: DeliverySelectionMode.Manual,
			metricSnapshotCount: 0,
		} as unknown as ConstructorParameters<typeof Delivery>[0]);

	it("the sentence owner gives the note alone for a childless Feature and the default-size sentence alone for a default-sized one", () => {
		const notDone = { isDoneWithRemainingWork: false };

		expect(
			featureWarningSentences(
				{
					...notDone,
					isUsingDefaultFeatureSize: false,
					hasNoChildWorkItemsYet: true,
				},
				RENAMED,
			),
		).toEqual([NO_CHILDREN_NOTE]);
		expect(
			featureWarningSentences(
				{
					...notDone,
					isUsingDefaultFeatureSize: true,
					hasNoChildWorkItemsYet: false,
				},
				RENAMED,
			),
		).toEqual([DEFAULT_SIZE_SENTENCE]);
		expect(
			featureWarningSentences(
				{
					...notDone,
					isUsingDefaultFeatureSize: false,
					hasNoChildWorkItemsYet: false,
				},
				RENAMED,
			),
		).toEqual([]);
	});

	it("the timeline bar carries the note as a warning, the warnings column lists it, and the Delivery has something to warn about", () => {
		const features = [childlessFeature()];

		const marks = barMarksFor(features, new Map(), RENAMED, new Map(), true);

		expect(
			marks.get(1)?.notes,
			"The childless Feature's bar must carry the note as a warning, where today's default-size warning sits.",
		).toEqual([{ text: NO_CHILDREN_NOTE, isWarning: true }]);
		expect(
			warningsColumnFor(features, RENAMED).warningsFor({
				id: 1,
			} as unknown as Parameters<
				ReturnType<typeof warningsColumnFor>["warningsFor"]
			>[0]),
			"The Delivery's Work Items warnings column must list the same note.",
		).toEqual([NO_CHILDREN_NOTE]);
		expect(anythingToWarnAbout(features, RENAMED)).toBe(true);
		expect(
			barMarksFor(
				[childlessFeature({ hasNoChildWorkItemsYet: false })],
				new Map(),
				RENAMED,
				new Map(),
				true,
			).has(1),
			"A Feature with no work counted but no childless flag must stay unmarked on the bar.",
		).toBe(false);
	});

	it("the exported table marks the childless Feature as having a warning", () => {
		const features = [childlessFeature()];

		const table = buildDeliveryExportTable(
			aDeliveryOf(features),
			features,
			[],
			{ ...RENAMED, deliveryTerm: "Delivery", teamTerm: "Team" },
		);

		expect(
			table.rows[0][table.headers.indexOf("Warnings")],
			"The export's Warnings cell must say Yes for a Feature that has the note.",
		).toBe("Yes");
	});
});
