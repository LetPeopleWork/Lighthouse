import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, render, screen, within } from "@testing-library/react";
import userEvent, { type UserEvent } from "@testing-library/user-event";
import type { ReactElement } from "react";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { Delivery } from "../../../models/Delivery";
import { Feature } from "../../../models/Feature";
import { Portfolio } from "../../../models/Portfolio/Portfolio";
import { Team } from "../../../models/Team/Team";
import type { IWorkItem } from "../../../models/WorkItem";
import DeliverySection from "../../../pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySection";
import PortfolioFeatureList from "../../../pages/Portfolios/Detail/PortfolioFeatureList";
import TeamFeatureList from "../../../pages/Teams/Detail/TeamFeatureList";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { deferred } from "../../../tests/HeldMetricsService";
import {
	createMockApiServiceContext,
	createMockFeatureService,
	createMockTeamMetricsService,
} from "../../../tests/MockApiServiceProvider";
import {
	aWorkItem,
	contextColumnsShown,
	day,
	estimateOf,
	rowOrder,
} from "../../../tests/WorkItemsDialogTestKit";

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		licenseStatus: { canUsePremiumFeatures: false },
		isLoading: false,
	}),
}));

const RENDER_HEAVY = 20000;

const zenith = (() => {
	const team = new Team();
	team.name = "Team Zenith";
	team.id = 1;
	team.portfolios = [];
	team.features = [
		{ id: 1, name: "Checkout revamp" },
		{ id: 2, name: "Search rebuild" },
	];
	team.featureWip = 2;
	team.lastUpdated = new Date();
	return team;
})();

const aFeatureInFlight = (id: number, referenceId: string, name: string) => {
	const feature = new Feature();
	feature.id = id;
	feature.referenceId = referenceId;
	feature.name = name;
	feature.state = "In Progress";
	feature.stateCategory = "Doing";
	feature.lastUpdated = new Date();
	feature.isUsingDefaultFeatureSize = false;
	feature.projects = [];
	feature.remainingWork = { 1: 3 };
	feature.totalWork = { 1: 5 };
	feature.forecasts = [];
	feature.startedDate = day("2026-09-01");
	feature.closedDate = new Date(0);
	feature.url = "";
	return feature;
};

const checkoutRevamp = aFeatureInFlight(1, "FTR-1", "Checkout revamp");
const searchRebuild = aFeatureInFlight(2, "FTR-2", "Search rebuild");
const bothFeatures = [checkoutRevamp, searchRebuild];

const oceanExplorer = (() => {
	const portfolio = new Portfolio();
	portfolio.name = "Ocean Explorer";
	portfolio.id = 1;
	portfolio.involvedTeams = [zenith];
	portfolio.features = [
		{ id: 1, name: "Checkout revamp" },
		{ id: 2, name: "Search rebuild" },
	];
	portfolio.lastUpdated = new Date();
	return portfolio;
})();

const springRelease = (() => {
	const delivery = new Delivery();
	delivery.id = 1;
	delivery.name = "Spring release";
	delivery.date = new Date("2026-12-01").toISOString();
	delivery.features = [1, 2];
	delivery.likelihoodPercentage = 75;
	delivery.progress = 40;
	delivery.remainingWork = 6;
	delivery.totalWork = 10;
	delivery.featureLikelihoods = [
		{ featureId: 1, likelihoodPercentage: 75 },
		{ featureId: 2, likelihoodPercentage: 80 },
	];
	delivery.completionDates = [];
	return delivery;
})();

const checkoutChildren: IWorkItem[] = [
	aWorkItem({
		referenceId: "ST-11",
		name: "Pay by invoice",
		cycleTime: 6,
		estimate: estimateOf(5),
	}),
	aWorkItem({
		referenceId: "ST-12",
		name: "Address autocomplete",
		state: "In Progress",
		stateCategory: "Doing",
		closedDate: new Date(0),
		cycleTime: 0,
		workItemAge: 14,
		estimate: estimateOf(13),
	}),
];
const searchChildren: IWorkItem[] = [
	aWorkItem({
		referenceId: "ST-21",
		name: "Synonyms",
		estimate: estimateOf(2),
	}),
];

let childItemRequests: ReturnType<typeof deferred<IWorkItem[]>>[] = [];
let featureService: ReturnType<typeof createMockFeatureService>;

beforeEach(() => {
	localStorage.clear();
	childItemRequests = [];
	featureService = createMockFeatureService();
	featureService.getFeaturesByIds = vi.fn().mockResolvedValue(bothFeatures);
	featureService.getFeaturesByReferences = vi.fn().mockResolvedValue([]);
	featureService.getFeatureWorkItems = vi.fn(() => {
		const request = deferred<IWorkItem[]>();
		childItemRequests.push(request);
		return request.promise;
	});
});

function showWithin(list: ReactElement): void {
	const teamMetricsService = createMockTeamMetricsService();
	teamMetricsService.getFeaturesInProgress = vi
		.fn()
		.mockResolvedValue(bothFeatures);
	render(
		<QueryClientProvider
			client={
				new QueryClient({ defaultOptions: { queries: { retry: false } } })
			}
		>
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({
					featureService,
					teamMetricsService,
				})}
			>
				<MemoryRouter>{list}</MemoryRouter>
			</ApiServiceContext.Provider>
		</QueryClientProvider>,
	);
}

/** The three places a Feature's progress bar opens its child items. */
const everyFeatureList: [string, () => void][] = [
	[
		"the Team's Feature list",
		() => showWithin(<TeamFeatureList team={zenith} />),
	],
	[
		"the Portfolio's Feature list",
		() => showWithin(<PortfolioFeatureList portfolio={oceanExplorer} />),
	],
	[
		"a Delivery's Features",
		() =>
			showWithin(
				<DeliverySection
					delivery={springRelease}
					features={bothFeatures}
					isExpanded
					isLoadingFeatures={false}
					onToggleExpanded={() => {}}
					onDelete={() => {}}
					onEdit={() => {}}
					teams={[{ id: zenith.id, name: zenith.name }]}
				/>,
			),
	],
];

async function theLeadOpensTheChildItemsOf(
	user: UserEvent,
	featureReference: string,
): Promise<void> {
	const row = (
		await screen.findByText(new RegExp(`^${featureReference}\\b`))
	).closest('[role="row"]') as HTMLElement;
	await user.click(within(row).getByRole("button", { name: "Team Zenith" }));
}

async function theLeadClosesTheDialog(user: UserEvent): Promise<void> {
	await user.click(
		within(screen.getByRole("dialog")).getByRole("button", { name: "Close" }),
	);
}

describe("a Feature's child items, from every list that opens them", () => {
	// @us-03 @slice-03 @driving_port @error @contract-shape:pure-function
	it.skip.each(everyFeatureList)(
		"%s: they open at once in their loading look, never as an empty list",
		async (_, showTheList) => {
			const user = userEvent.setup();
			showTheList();

			await theLeadOpensTheChildItemsOf(user, "FTR-1");

			const dialog = screen.getByRole("dialog");
			expect(within(dialog).getByRole("progressbar")).toBeInTheDocument();
			expect(within(dialog).queryByText("No items to display")).toBeNull();
		},
		RENDER_HEAVY,
	);

	// @us-03 @slice-03 @driving_port @contract-shape:pure-function
	it.skip.each(everyFeatureList)(
		"%s: they show Started, Closed, Age / Cycle Time and Estimate once they arrive, oldest first",
		async (_, showTheList) => {
			const user = userEvent.setup();
			showTheList();

			await theLeadOpensTheChildItemsOf(user, "FTR-1");
			await act(async () => childItemRequests[0].resolve(checkoutChildren));

			expect(contextColumnsShown()).toEqual([
				"startedDate",
				"closedDate",
				"ageOrCycleTime",
				"estimate",
			]);
			expect(rowOrder()).toEqual(["ST-12", "ST-11"]);
		},
		RENDER_HEAVY,
	);

	// @us-03 @slice-03 @error @contract-shape:pure-function
	it.skip.each(everyFeatureList)(
		"%s: they say they couldn't be loaded when the request fails",
		async (_, showTheList) => {
			const user = userEvent.setup();
			showTheList();

			await theLeadOpensTheChildItemsOf(user, "FTR-1");
			await act(async () =>
				childItemRequests[0].reject(new Error("child items failed")),
			);

			expect(
				within(screen.getByRole("dialog")).getByText(
					"These Work Items couldn't be loaded. Close and reopen to try again.",
				),
			).toBeInTheDocument();
		},
		RENDER_HEAVY,
	);

	// @us-03 @slice-03 @error @contract-shape:bounded-change
	it.skip.each(everyFeatureList)(
		"%s: they are asked for again when the lead closes and reopens after a failure",
		async (_, showTheList) => {
			const user = userEvent.setup();
			showTheList();
			await theLeadOpensTheChildItemsOf(user, "FTR-1");
			await act(async () =>
				childItemRequests[0].reject(new Error("child items failed")),
			);
			await theLeadClosesTheDialog(user);

			await theLeadOpensTheChildItemsOf(user, "FTR-1");
			await act(async () => childItemRequests[1].resolve(checkoutChildren));

			expect(rowOrder()).toEqual(["ST-12", "ST-11"]);
		},
		RENDER_HEAVY,
	);

	// @us-03 @slice-03 @error @contract-shape:pure-function
	it.skip.each(everyFeatureList)(
		"%s: the previous Feature's items never show when their answer arrives late",
		async (_, showTheList) => {
			const user = userEvent.setup();
			showTheList();
			await theLeadOpensTheChildItemsOf(user, "FTR-1");
			await theLeadClosesTheDialog(user);
			await theLeadOpensTheChildItemsOf(user, "FTR-2");

			await act(async () => childItemRequests[0].resolve(checkoutChildren));

			const dialog = screen.getByRole("dialog");
			expect(within(dialog).queryByText("Pay by invoice")).toBeNull();
			expect(within(dialog).getByRole("progressbar")).toBeInTheDocument();

			await act(async () => childItemRequests[1].resolve(searchChildren));

			expect(rowOrder()).toEqual(["ST-21"]);
		},
		RENDER_HEAVY,
	);
});
