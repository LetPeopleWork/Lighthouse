import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { type RenderResult, render, waitFor } from "@testing-library/react";
import type { ReactElement } from "react";
import { MemoryRouter } from "react-router";
import { expect, vi } from "vitest";
import { Feature, type IFeature } from "../models/Feature";
import type { IFeatureList, IFeatureListParent } from "../models/FeatureList";
import { Portfolio } from "../models/Portfolio/Portfolio";
import { Team } from "../models/Team/Team";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import type { IFeatureService } from "../services/Api/FeatureService";
import type { ITeamMetricsService } from "../services/Api/MetricsService";
import { TerminologyProvider } from "../services/TerminologyContext";
import {
	createMockApiServiceContext,
	createMockFeatureService,
	createMockTeamMetricsService,
	createMockTerminologyService,
} from "./MockApiServiceProvider";

/** The words the instance ships with, as `TerminologySeeder` seeds them. */
export const DefaultTerms: Record<string, string> = {
	feature: "Feature",
	features: "Features",
	team: "Team",
	teams: "Teams",
	portfolio: "Portfolio",
	portfolios: "Portfolios",
	workItem: "Work Item",
	workItems: "Work Items",
};

/** A tenant that renamed the terms a Features list prints, so a hardcoded default word cannot pass unnoticed. */
export const RenamedTerms: Record<string, string> = {
	...DefaultTerms,
	feature: "Bet",
	features: "Bets",
	team: "Squad",
	teams: "Squads",
	portfolio: "Program",
	portfolios: "Programs",
};

export const Atlas = { id: 1, name: "Atlas" };
export const Borealis = { id: 2, name: "Borealis" };

export const NorthwindPlatform = Object.assign(new Portfolio(), {
	id: 2,
	name: "Northwind Platform",
	features: [
		{ id: 11, name: "Checkout redesign" },
		{ id: 12, name: "Search revamp" },
		{ id: 13, name: "Legacy cleanup" },
	],
	involvedTeams: [Atlas, Borealis],
});

export const NorthwindMobile = Object.assign(new Portfolio(), {
	id: 5,
	name: "Northwind Mobile",
	features: [],
	involvedTeams: [],
});

export const TeamBorealis = Object.assign(new Team(), {
	id: Borealis.id,
	name: Borealis.name,
	features: [{ id: 21, name: "Payment provider switch" }],
});

export const TeamWithNothingYet = Object.assign(new Team(), {
	id: 9,
	name: "Cobalt",
	features: [],
});

export const aRow = (
	id: number,
	referenceId: string,
	name: string,
	{
		completed = false,
		parent = "",
	}: { completed?: boolean; parent?: string } = {},
): IFeature =>
	Object.assign(new Feature(), {
		id,
		referenceId,
		name,
		type: "Epic",
		state: completed ? "Done" : "In Progress",
		stateCategory: completed ? "Done" : "Doing",
		parentWorkItemReference: parent,
		forecasts: [],
		lastUpdated: new Date("2026-10-24T09:00:00Z"),
		isUsingDefaultFeatureSize: false,
		remainingWork: completed ? { 1: 0 } : { 1: 5 },
		totalWork: { 1: 8 },
		projects: [],
	});

export const PlatformRows = [
	aRow(11, "NW-1", "Checkout redesign", { parent: "ST-1" }),
	aRow(12, "NW-2", "Search revamp"),
	aRow(13, "NW-3", "Legacy cleanup", { completed: true }),
];

export const Platform2027: IFeatureListParent = {
	referenceId: "ST-1",
	name: "Platform 2027",
	url: "https://northwind.example/browse/ST-1",
};

export const aList = (
	features: IFeature[],
	{
		parents = [],
		activeWork = [],
	}: Partial<Pick<IFeatureList, "parents" | "activeWork">> = {},
): IFeatureList => ({ features, parents, activeWork });

export const NorthwindPlatformsList = aList(PlatformRows, {
	parents: [Platform2027],
	activeWork: [{ teamId: Atlas.id, featureReferenceIds: ["NW-1"] }],
});

/** A Features service whose list answers are chosen by the scenario, one call at a time. */
export const aFeatureServiceAnswering = (
	...answers: Promise<IFeatureList>[]
): IFeatureService => {
	const getFeatureList = vi.fn();
	for (const answer of answers) {
		// A refusal chosen up front is not a stray one: the list reads it when it asks.
		answer.catch(() => undefined);
		getFeatureList.mockReturnValueOnce(answer);
	}
	return {
		...createMockFeatureService(),
		getFeaturesByIds: vi.fn().mockResolvedValue([]),
		getFeaturesByReferences: vi.fn().mockResolvedValue([]),
		getFeatureList,
	};
};

export const aTeamMetricsService = (): ITeamMetricsService => ({
	...createMockTeamMetricsService(),
	getFeaturesInProgress: vi.fn().mockResolvedValue([]),
});

const theTermsAnswer = (terms: Record<string, string>) =>
	Object.entries(terms).map(([key, value], index) => ({
		id: index + 1,
		key,
		defaultValue: value,
		description: "",
		value,
	}));

export interface RenderedFeatureList extends Omit<RenderResult, "rerender"> {
	rerender: (next: ReactElement) => void;
	queryClient: QueryClient;
}

/**
 * Renders a Features list the way its detail page does, under the instance's terms, and returns once those terms
 * have arrived, so the first assertion already reads the tenant's words.
 */
export const renderAFeatureList = async (
	ui: ReactElement,
	{
		featureService,
		teamMetricsService = aTeamMetricsService(),
		terms = DefaultTerms,
	}: {
		featureService: IFeatureService;
		teamMetricsService?: ITeamMetricsService;
		terms?: Record<string, string>;
	},
): Promise<RenderedFeatureList> => {
	const queryClient = new QueryClient({
		defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
	});
	const services = createMockApiServiceContext({
		featureService,
		teamMetricsService,
		terminologyService: {
			...createMockTerminologyService(),
			getAllTerminology: vi.fn().mockResolvedValue(theTermsAnswer(terms)),
		},
	});
	const wrap = (element: ReactElement) => (
		<QueryClientProvider client={queryClient}>
			<MemoryRouter>
				<ApiServiceContext.Provider value={services}>
					<TerminologyProvider>{element}</TerminologyProvider>
				</ApiServiceContext.Provider>
			</MemoryRouter>
		</QueryClientProvider>
	);

	const result = render(wrap(ui));
	await waitFor(() =>
		expect(queryClient.getQueryData(["terminology-database"])).toBeDefined(),
	);

	return {
		...result,
		rerender: (next: ReactElement) => result.rerender(wrap(next)),
		queryClient,
	};
};

/** The data grid asks the browser about the screen size, which jsdom cannot answer. */
export const stubTheScreenSizeTheGridAsksFor = () => {
	Object.defineProperty(globalThis, "matchMedia", {
		writable: true,
		value: vi.fn().mockImplementation((query: string) => ({
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
};

/** The look the list is in, as the page object reads it. */
export const theListStatus = (container: HTMLElement): string | null =>
	container.querySelector<HTMLElement>("[data-widget-status]")?.dataset
		.widgetStatus ?? null;

/** The part of the list that is dimmed, and refuses the pointer, while an answer is on its way. */
export const theListBody = (container: HTMLElement): HTMLElement => {
	const body = container.querySelector<HTMLElement>(
		'[data-testid^="widget-shell-body-"]',
	);
	if (!body) throw new Error("The list has no body to dim.");
	return body;
};
