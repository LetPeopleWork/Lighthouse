import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { renderHook, waitFor } from "@testing-library/react";
import { createElement, type ReactNode } from "react";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { IBlackoutPeriod } from "../models/BlackoutPeriod";
import type { IFeatureOwner } from "../models/IFeatureOwner";
import { getMetricsFetchKeys } from "../pages/Common/MetricsView/categoryMetadata";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import type { IMetricsService } from "../services/Api/MetricsService";
import { createMockApiServiceContext } from "../tests/MockApiServiceProvider";
import { applicabilityFor } from "./metricsApplicability";
import { type MetricsOwnerRequest, metricsOwnerKeyOf } from "./metricsOwnerKey";
import { useMetricsData } from "./useMetricsData";
import { metricsQueryOptions, useMetricsQuery } from "./useMetricsQuery";

// A service is told apart by the methods it has, so a fake is just its method names.
function serviceWith(...methods: string[]): object {
	return Object.fromEntries(methods.map((method) => [method, vi.fn()]));
}

const featureSizeMethods = [
	"getAllFeaturesForSizeChart",
	"getSizePercentiles",
	"getFeatureSizePercentilesInfo",
];
const portfolioOnlySizeMethods = [
	"getFeatureSizePbc",
	"getFeatureSizeEstimation",
];

const teamService = serviceWith(
	"getFeaturesInProgress",
	"getFeaturesWorkedOnInfo",
	"getSleRisk",
	...featureSizeMethods,
);
const teamServiceThatAlsoHasPortfolioMethods = serviceWith(
	"getFeaturesInProgress",
	"getFeaturesWorkedOnInfo",
	"getSleRisk",
	...featureSizeMethods,
	...portfolioOnlySizeMethods,
);
const portfolioService = serviceWith(
	"getFeaturesWorkedOnInfo",
	...featureSizeMethods,
	...portfolioOnlySizeMethods,
);
const featureSizesOnlyService = serviceWith(...featureSizeMethods);
const bareService = serviceWith();

const noChoices = { selection: false, scope: false };

function keysNotAsked(
	service: object,
	choices: { selection: boolean; scope: boolean } = noChoices,
): string[] {
	return Object.entries(applicabilityFor(service, choices))
		.filter(([, asked]) => !asked)
		.map(([key]) => key)
		.sort((a, b) => a.localeCompare(b));
}

describe("Which metrics questions an owner can ask", () => {
	it("answers for every question the metrics view knows, and for no other", () => {
		const answered = Object.keys(applicabilityFor(bareService, noChoices));

		expect(answered.sort((a, b) => a.localeCompare(b))).toEqual(
			[...getMetricsFetchKeys()].sort((a, b) => a.localeCompare(b)),
		);
	});

	it.each([
		{
			owner: "a team",
			service: teamService,
			notAsked: [
				"cumulativeStateTimeScope",
				"cumulativeStateTimeSelection",
				"featureSizeEstimation",
				"featureSizePbc",
			],
		},
		{
			owner: "a team whose service also carries the portfolio-only methods",
			service: teamServiceThatAlsoHasPortfolioMethods,
			notAsked: [
				"cumulativeStateTimeScope",
				"cumulativeStateTimeSelection",
				"featureSizeEstimation",
				"featureSizePbc",
			],
		},
		{
			owner: "a portfolio",
			service: portfolioService,
			notAsked: [
				"cumulativeStateTimeScope",
				"cumulativeStateTimeSelection",
				"sleRisk",
			],
		},
		{
			owner: "a service with feature sizes only",
			service: featureSizesOnlyService,
			notAsked: [
				"cumulativeStateTimeScope",
				"cumulativeStateTimeSelection",
				"featureSizeEstimation",
				"featureSizePbc",
				"featuresWorkedOnInfo",
				"sleRisk",
			],
		},
		{
			owner: "a bare service",
			service: bareService,
			notAsked: [
				"cumulativeStateTimeScope",
				"cumulativeStateTimeSelection",
				"featureSizeData",
				"featureSizeEstimation",
				"featureSizePbc",
				"featureSizePercentilesInfo",
				"featuresWorkedOnInfo",
				"sleRisk",
			],
		},
	])(
		"leaves $owner without exactly the questions it cannot ask",
		({ service, notAsked }) => {
			expect(keysNotAsked(service)).toEqual(notAsked);
		},
	);

	it.each([
		{ chosen: "some Work Items", choices: { selection: true, scope: false } },
		{ chosen: "one stretch", choices: { selection: false, scope: true } },
	])(
		"asks the narrowed Cumulative Time per State only for $chosen",
		({ choices }) => {
			const applicable = applicabilityFor(teamService, choices);

			expect(applicable.cumulativeStateTimeSelection).toBe(choices.selection);
			expect(applicable.cumulativeStateTimeScope).toBe(choices.scope);
		},
	);
});

function ownerAt(
	overrides: Partial<Pick<IFeatureOwner, "id" | "lastUpdated" | "name">>,
): Pick<IFeatureOwner, "id" | "lastUpdated" | "name"> {
	return {
		id: 7,
		name: "Ocean Explorers",
		lastUpdated: new Date(Date.UTC(2024, 2, 1, 8, 30)),
		...overrides,
	};
}

describe("Whose metrics a request asks for", () => {
	it("names the owner and the moment of its last update, and nothing else", () => {
		expect(metricsOwnerKeyOf(ownerAt({}))).toStrictEqual({
			ownerId: 7,
			ownerUpdatedAt: Date.UTC(2024, 2, 1, 8, 30),
		});
	});

	it("gives the same key for the same owner asked twice, whatever else it carries", () => {
		const first = metricsOwnerKeyOf(ownerAt({}));
		const again = metricsOwnerKeyOf(ownerAt({ name: "Renamed" }));

		expect(again).toStrictEqual(first);
	});

	it("gives another key once the owner has been updated", () => {
		const before = metricsOwnerKeyOf(ownerAt({}));
		const after = metricsOwnerKeyOf(
			ownerAt({ lastUpdated: new Date(Date.UTC(2024, 2, 1, 8, 31)) }),
		);

		expect(after.ownerId).toBe(before.ownerId);
		expect(after.ownerUpdatedAt).not.toBe(before.ownerUpdatedAt);
	});
});

type WindowRequest = MetricsOwnerRequest & { readonly from: string };

const teamRequest: WindowRequest = {
	ownerType: "team",
	ownerId: 7,
	ownerUpdatedAt: 1,
	from: "2024-01-01",
};

function inWindow(request: WindowRequest, from: string): WindowRequest {
	return { ...request, from };
}

function deferred<V>() {
	let settle: (answer: V) => void = () => {};
	let fail: (error: Error) => void = () => {};
	const promise = new Promise<V>((resolve, reject) => {
		settle = resolve;
		fail = reject;
	});
	return { promise, settle, fail };
}

function renderQuery(
	initial: {
		request: WindowRequest;
		ask: (request: WindowRequest) => Promise<string | undefined>;
		enabled?: boolean;
		failureMessage?: string;
	},
	client: QueryClient = new QueryClient(),
) {
	return renderHook(
		(props: typeof initial) =>
			useMetricsQuery(
				"getSomething",
				props.request,
				props.ask,
				props.enabled ?? true,
				props.failureMessage,
			),
		{
			initialProps: initial,
			wrapper: ({ children }: { children: ReactNode }) =>
				createElement(QueryClientProvider, { client }, children),
		},
	);
}

afterEach(() => {
	vi.restoreAllMocks();
});

describe("The metrics query helper", () => {
	it("asks fresh, never retries and keeps no cache, as every metrics request does", () => {
		expect(metricsQueryOptions).toStrictEqual({
			staleTime: 0,
			gcTime: 0,
			retry: false,
			refetchOnWindowFocus: false,
			refetchOnReconnect: false,
		});
	});

	it("hands the request it was given to the question and shows the answer", async () => {
		const ask = vi.fn().mockResolvedValue("answer");

		const { result } = renderQuery({ request: teamRequest, ask });

		expect(result.current.progress.isPending).toBe(true);
		await waitFor(() => expect(result.current.answer).toBe("answer"));
		expect(ask).toHaveBeenCalledExactlyOnceWith(teamRequest);
		expect(result.current.progress).toMatchObject({
			isPending: false,
			isError: false,
			isPlaceholderData: false,
			data: "answer",
		});
	});

	it("reads an empty answer as an answer, not as still on its way", async () => {
		const { result } = renderQuery({
			request: teamRequest,
			ask: () => Promise.resolve(undefined),
		});

		await waitFor(() => expect(result.current.progress.isPending).toBe(false));
		expect(result.current.answer).toBeNull();
		expect(result.current.progress.data).toBeNull();
	});

	it("does not ask while the question is not wanted", () => {
		const ask = vi.fn().mockResolvedValue("answer");

		const { result } = renderQuery({
			request: teamRequest,
			ask,
			enabled: false,
		});

		expect(ask).not.toHaveBeenCalled();
		expect(result.current.progress.isPending).toBe(true);
		expect(result.current.answer).toBeNull();
	});

	it("asks once for a failing question and says so in the console", async () => {
		const failure = new Error("server down");
		const ask = vi.fn().mockRejectedValue(failure);
		const logged = vi.spyOn(console, "error").mockImplementation(() => {});

		const { result } = renderQuery({
			request: teamRequest,
			ask,
			failureMessage: "Error getting something:",
		});

		await waitFor(() => expect(result.current.progress.isError).toBe(true));
		expect(ask).toHaveBeenCalledTimes(1);
		expect(logged).toHaveBeenCalledExactlyOnceWith(
			"Error getting something:",
			failure,
		);
	});

	it("keeps the previous window's answer, marked as such, while the next window loads", async () => {
		const first = vi.fn().mockResolvedValue("first window");
		const nextWindow = deferred<string>();
		const { result, rerender } = renderQuery({
			request: teamRequest,
			ask: first,
		});
		await waitFor(() => expect(result.current.answer).toBe("first window"));

		rerender({
			request: inWindow(teamRequest, "2024-02-01"),
			ask: () => nextWindow.promise,
		});

		await waitFor(() =>
			expect(result.current.progress.isPlaceholderData).toBe(true),
		);
		expect(result.current.answer).toBe("first window");
		nextWindow.settle("second window");
		await waitFor(() => expect(result.current.answer).toBe("second window"));
		expect(result.current.progress.isPlaceholderData).toBe(false);
	});

	it("keeps the last answer drawn for the same owner when the next window fails", async () => {
		vi.spyOn(console, "error").mockImplementation(() => {});
		const { result, rerender } = renderQuery({
			request: teamRequest,
			ask: () => Promise.resolve("first window"),
		});
		await waitFor(() => expect(result.current.answer).toBe("first window"));

		rerender({
			request: inWindow(teamRequest, "2024-02-01"),
			ask: () => Promise.reject(new Error("server down")),
		});

		await waitFor(() => expect(result.current.progress.isError).toBe(true));
		expect(result.current.answer).toBe("first window");
		expect(result.current.progress.data).toBe("first window");
	});

	it.each([
		{ other: "another owner id", request: { ...teamRequest, ownerId: 8 } },
		{
			other: "the same id on another owner type",
			request: { ...teamRequest, ownerType: "portfolio" as const },
		},
	])(
		"shows no answer when $other fails, though the first owner drew one",
		async ({ request }) => {
			vi.spyOn(console, "error").mockImplementation(() => {});
			const { result, rerender } = renderQuery({
				request: teamRequest,
				ask: () => Promise.resolve("first owner's answer"),
			});
			await waitFor(() =>
				expect(result.current.answer).toBe("first owner's answer"),
			);

			rerender({
				request,
				ask: () => Promise.reject(new Error("server down")),
			});

			await waitFor(() => expect(result.current.progress.isError).toBe(true));
			expect(result.current.answer).toBeNull();
			expect(result.current.progress.data).toBeUndefined();
		},
	);
});

// The order in which requests start is what the reader's connection sees, so it is pinned by
// name: the first time each service call is made, in the order the calls are first made.
describe("The metrics view's requests", () => {
	it("start in the order the view has always started them", async () => {
		const started: string[] = [];
		const recording = <A,>(name: string, answer: A) =>
			vi.fn(() => {
				if (!started.includes(name)) started.push(name);
				return Promise.resolve(answer);
			});
		const methods = [
			"getTotalWorkItemAge",
			"getThroughput",
			"getInProgressItems",
			"getBlockedItemsAtDate",
			"getWorkInProgressOverTime",
			"getCycleTimeData",
			"getCycleTimePercentiles",
			"getWorkItemAgePercentiles",
			"getAgeInStatePercentiles",
			"getSleRisk",
			"getCumulativeStateTimeForTeam",
			"getArrivals",
			"getBlockedCountHistory",
			"getWipPbc",
			"getTotalWorkItemAgePbc",
			"getMultiItemForecastPredictabilityScore",
			"getSizePercentiles",
			"getAllFeaturesForSizeChart",
			"getEstimationVsCycleTimeData",
			"getThroughputInfo",
			"getArrivalsInfo",
			"getWipOverviewInfo",
			"getTotalWorkItemAgeInfo",
			"getPredictabilityScoreInfo",
			"getCycleTimePercentilesInfo",
			"getFeaturesWorkedOnInfo",
			"getFeatureSizePercentilesInfo",
			"getFlowEfficiencyInfoForTeam",
			"getCycleTimePbc",
			"getArrivalsPbc",
			"getThroughputPbc",
			"getFeaturesInProgress",
		];
		const service = Object.fromEntries(
			methods.map((method) => [method, recording(method, [])]),
		) as unknown as IMetricsService<never>;
		const blackoutPeriodService = {
			getAll: recording<IBlackoutPeriod[]>("getAllBlackoutPeriods", []),
			create: vi.fn(),
			update: vi.fn(),
			delete: vi.fn(),
		};
		const services = createMockApiServiceContext({ blackoutPeriodService });
		const client = new QueryClient();

		renderHook(
			() =>
				useMetricsData(
					{
						id: 7,
						name: "Ocean Explorers",
						lastUpdated: new Date(Date.UTC(2024, 2, 1)),
						features: [],
						remainingFeatures: 0,
						tags: [],
						serviceLevelExpectationProbability: 0,
						serviceLevelExpectationRange: 0,
						systemWIPLimit: 0,
					},
					service,
					new Date(2024, 0, 1),
					new Date(2024, 5, 30),
				),
			{
				wrapper: ({ children }: { children: ReactNode }) =>
					createElement(
						ApiServiceContext.Provider,
						{ value: services },
						createElement(QueryClientProvider, { client }, children),
					),
			},
		);

		await waitFor(() => expect(started).toContain("getThroughputPbc"));
		expect(started).toEqual([
			"getAllBlackoutPeriods",
			"getTotalWorkItemAge",
			"getThroughput",
			"getInProgressItems",
			"getBlockedItemsAtDate",
			"getWorkInProgressOverTime",
			"getCycleTimeData",
			"getCycleTimePercentiles",
			"getWorkItemAgePercentiles",
			"getFlowEfficiencyInfoForTeam",
			"getAgeInStatePercentiles",
			"getSleRisk",
			"getCumulativeStateTimeForTeam",
			"getArrivals",
			"getBlockedCountHistory",
			"getWipPbc",
			"getTotalWorkItemAgePbc",
			"getMultiItemForecastPredictabilityScore",
			"getSizePercentiles",
			"getAllFeaturesForSizeChart",
			"getFeatureSizePercentilesInfo",
			"getEstimationVsCycleTimeData",
			"getThroughputInfo",
			"getArrivalsInfo",
			"getWipOverviewInfo",
			"getTotalWorkItemAgeInfo",
			"getPredictabilityScoreInfo",
			"getCycleTimePercentilesInfo",
			"getFeaturesWorkedOnInfo",
			"getCycleTimePbc",
			"getArrivalsPbc",
			"getThroughputPbc",
		]);
	});
});
