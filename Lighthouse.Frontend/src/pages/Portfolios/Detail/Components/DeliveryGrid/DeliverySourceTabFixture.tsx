/**
 * The Releases a connection offers, the Portfolio they belong to, and the two screens that show
 * them: the source tab on its own, and the create form that hosts it. Every suite here starts from
 * the same board, so a Release that changes shape has one place to change rather than eight.
 */
import type { RenderResult } from "@testing-library/react";
import { render, screen, waitFor, within } from "@testing-library/react";
import type { UserEvent } from "@testing-library/user-event";
import { expect, vi } from "vitest";
import type { IDeliverySourceOption } from "../../../../../models/Delivery/DeliverySource";
import type { IFeature } from "../../../../../models/Feature";
import type { Portfolio } from "../../../../../models/Portfolio/Portfolio";
import { ApiServiceContext } from "../../../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockDeliveryService,
	createMockFeatureService,
} from "../../../../../tests/MockApiServiceProvider";
import { DeliveryCreateModal } from "./DeliveryCreateModal";
import { DeliverySourceTab } from "./DeliverySourceTab";

export const createFeature = (id: number, name: string): IFeature =>
	({
		id,
		name,
		referenceId: `FTR-${id}`,
		stateCategory: "ToDo",
		state: "ToDo",
		type: "Feature",
		size: 1,
		owningTeam: "Team A",
		lastUpdated: new Date(),
		isUsingDefaultFeatureSize: false,
		parentWorkItemReference: "",
		projects: [],
		remainingWork: {},
		totalWork: {},
		forecasts: [],
		startedDate: new Date(),
		closedDate: new Date(),
		cycleTime: 0,
		workItemAge: 0,
		url: "",
		isBlocked: false,
		getRemainingWorkForFeature: () => 0,
		getRemainingWorkForTeam: () => 0,
		getTotalWorkForFeature: () => 0,
		getTotalWorkForTeam: () => 0,
	}) as IFeature;

export const mockPortfolio = {
	id: 1,
	name: "Test Portfolio",
	features: [],
	involvedTeams: [],
	tags: [],
	totalWorkItems: 0,
	remainingWorkItems: 0,
	forecasts: [],
	lastUpdated: new Date(),
	serviceLevelExpectationProbability: 0,
	serviceLevelExpectationRange: 0,
	systemWIPLimit: 0,
	remainingFeatures: 0,
	featureSizeTargetProbability: 0,
	featureSizeTargetRange: 0,
	fromBackend: vi.fn(),
} as unknown as Portfolio;

export const JIRA_RELEASE_SOURCE = {
	key: "jira-release",
	displayName: "Jira Release",
};
export const JIRA_FIX_VERSION_SOURCE = {
	key: "jira-fix-version",
	displayName: "Jira Fix Version",
};

export const datedInJustATest: IDeliverySourceOption = {
	id: "10044",
	name: "Release 44",
	date: new Date("2026-09-30T00:00:00Z"),
	projectKey: "JUSTATEST",
	// A real project on the live board is called this. The brackets inside the brackets are ugly and
	// they are still the point: the row stays unambiguous, which two bare "Release 44"s never were.
	projectName: "Project (Test)",
	isSelectable: true,
	blockedBecause: null,
};

export const datedInProject: IDeliverySourceOption = {
	id: "10144",
	name: "Release 44",
	date: new Date("2026-10-15T00:00:00Z"),
	projectKey: "PROJ",
	projectName: "Project X",
	isSelectable: true,
	blockedBecause: null,
};

export const datelessOption: IDeliverySourceOption = {
	id: "10045",
	name: "Release 45",
	date: null,
	projectKey: "PROJ",
	projectName: "Project X",
	isSelectable: false,
	blockedBecause: "NoDateSet",
};

export const allOptions = [datedInJustATest, datedInProject, datelessOption];

// Kept out of allOptions so the tests that count rows keep counting the same three.
export const retiredOption: IDeliverySourceOption = {
	id: "10040",
	name: "Release 40",
	date: new Date("2026-08-01T12:00:00Z"),
	projectKey: "PROJ",
	projectName: "Project X",
	isSelectable: false,
	blockedBecause: "RetiredAtSource",
};

export const fixVersionOption: IDeliverySourceOption = {
	id: "20001",
	name: "Sprint 9 hotfix",
	date: new Date("2026-11-20T12:00:00Z"),
	projectKey: "PROJ",
	projectName: "Project X",
	isSelectable: true,
	blockedBecause: null,
};

export const datedOnASingleDigitDay: IDeliverySourceOption = {
	id: "10005",
	name: "Release 5",
	date: new Date("2027-01-05T12:00:00Z"),
	projectKey: "PROJ",
	projectName: "Project X",
	isSelectable: true,
	blockedBecause: null,
};

// A row reads "<name> (<project>)", and both halves are matched when the reader types. Spelled out
// here because "Release 44" alone names two of the three rows, so a loose query finds two and throws.
export const RELEASE_44_IN_PROJECT_TEST = "Release 44 (Project (Test))";
export const RELEASE_44_IN_PROJECT_X = "Release 44 (Project X)";
export const THE_DATELESS_RELEASE = /^Release 45 \(Project X\)/;

type MockDeliveryService = ReturnType<typeof createMockDeliveryService>;

/** What a suite about the publishing switch has to be able to set and watch. */
export interface TabOverrides {
	publishForecast?: boolean;
	onPublishForecastChange?: (publish: boolean) => void;
}

export const renderTab = (
	deliveryService: MockDeliveryService = createMockDeliveryService(),
	overrides: TabOverrides = {},
): RenderResult & { deliveryService: MockDeliveryService } => {
	const context = createMockApiServiceContext({ deliveryService });

	const result = render(
		<ApiServiceContext.Provider value={context}>
			<DeliverySourceTab
				portfolioId={1}
				sourceKey={JIRA_RELEASE_SOURCE.key}
				sourceName={JIRA_RELEASE_SOURCE.displayName}
				featuresTerm="Deliverables"
				portfolioTerm="Value Stream"
				currentSelection={null}
				onOptionPicked={vi.fn()}
				publishForecast={overrides.publishForecast ?? false}
				onPublishForecastChange={overrides.onPublishForecastChange ?? vi.fn()}
			/>
		</ApiServiceContext.Provider>,
	);

	return { ...result, deliveryService };
};

export const publishSwitch = () =>
	screen.getByRole("switch", { name: "Publish forecast to the Jira Release" });

export const renderModal = (
	deliveryService: MockDeliveryService = createMockDeliveryService(),
): RenderResult & { deliveryService: MockDeliveryService } => {
	const context = createMockApiServiceContext({
		deliveryService,
		featureService: createMockFeatureService(),
	});

	const result = render(
		<ApiServiceContext.Provider value={context}>
			<DeliveryCreateModal
				open={true}
				portfolio={mockPortfolio}
				onClose={vi.fn()}
				onSave={vi.fn()}
			/>
		</ApiServiceContext.Provider>,
	);

	return { ...result, deliveryService };
};

export const selectionModeButtons = () =>
	within(screen.getByRole("group", { name: "Selection Mode" })).getAllByRole(
		"button",
	);

export const openSourceList = async (user: UserEvent) => {
	await waitFor(() => {
		expect(
			screen.getByRole("combobox", { name: "Jira Release" }),
		).toBeInTheDocument();
	});
	await user.click(screen.getByRole("combobox", { name: "Jira Release" }));
};
